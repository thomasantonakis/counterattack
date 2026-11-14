"""One Mac run, native Unity evidence, coordinated Windows probes, one Git report.

Standard library only. Importing this module performs no operations.
"""
import argparse
import base64
import datetime as dt
import json
import hashlib
import os
from pathlib import Path
import re
import shutil
import signal
import subprocess
import sys
import tempfile
import time
import xml.etree.ElementTree as ET

REPOSITORY = "repos/thomasantonakis/counterattack"
SOURCE_BRANCH = "codex/online-foundation-personal-mac"
QUEUE_BRANCH = "verification/queue"
QUEUE_PATH = "Documentation/Testing/VerificationQueue"
SECRET_KEYS = re.compile(r"^(authorization|password|client.?secret|access.?token|refresh.?token|id.?token|device.?secret)$", re.I)


def utc():
    return dt.datetime.now(dt.timezone.utc).isoformat()


def clean(value):
    if isinstance(value, dict):
        return {k: "[REDACTED]" if SECRET_KEYS.match(k) else clean(v) for k, v in value.items()}
    if isinstance(value, list):
        return [clean(v) for v in value]
    if isinstance(value, str):
        value = "\n".join(line for line in value.splitlines() if "[Licensing" not in line)
        value = re.sub(r"(?i)(Bearer\s+)[A-Za-z0-9._~+/-]+=*", r"\1[REDACTED]", value)
        return re.sub(r'(?i)((?:client.?secret|access.?token|refresh.?token|authorization|password)\s*[\"\s:=]+)[^\s\",<>]+', r'\1[REDACTED]', value)
    return value


def execute(args, root, timeout=90, input_text=None, env=None):
    process = subprocess.Popen([str(x) for x in args], cwd=root, env=env,
                               stdin=subprocess.PIPE if input_text is not None else subprocess.DEVNULL,
                               stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True,
                               encoding="utf-8", errors="replace", start_new_session=os.name != "nt")
    try:
        output, _ = process.communicate(input_text, timeout=timeout)
    except KeyboardInterrupt:
        if os.name != "nt":
            os.killpg(process.pid, signal.SIGTERM)
        else:
            process.terminate()
        try:
            process.communicate(timeout=10)
        except subprocess.TimeoutExpired:
            if os.name != "nt":
                os.killpg(process.pid, signal.SIGKILL)
            else:
                process.kill()
            process.communicate()
        raise
    except subprocess.TimeoutExpired:
        # Only this owned CLI/player process group. Never the Unity Editor.
        if os.name != "nt":
            os.killpg(process.pid, signal.SIGTERM)
        else:
            process.terminate()
        try:
            output, _ = process.communicate(timeout=10)
        except subprocess.TimeoutExpired:
            if os.name != "nt":
                os.killpg(process.pid, signal.SIGKILL)
            else:
                process.kill()
            output, _ = process.communicate()
        return {"exit_code": 124, "timed_out": True, "output": output}
    return {"exit_code": process.returncode, "output": output}


def checked(args, root, **kwargs):
    result = execute(args, root, **kwargs)
    if result["exit_code"]:
        raise RuntimeError(clean(f"Command failed ({result['exit_code']}): {' '.join(map(str,args))}\n{result['output'][-16000:]}"))
    return result["output"].strip()


def discover_fixtures(root):
    plan = []
    for mode, folder in (("editor", "Assets/Editor/FeatureTests"), ("playmode", "Assets/Tests/PlayMode")):
        for path in sorted((root / folder).rglob("*.cs")):
            source = path.read_text(encoding="utf-8-sig")
            categories = sorted(set(re.findall(r'Category\(\s*"(Online[^\"]+)"\s*\)', source)))
            if not categories:
                continue
            # Fixture filters avoid substring overlap between online categories.
            for name in re.findall(r"public\s+(?:sealed\s+)?(?:partial\s+)?class\s+(\w+Tests)\b", source):
                plan.append({"mode": mode, "fixture": name, "categories": categories,
                             "definition_sha256": hashlib.sha256(source.encode("utf-8")).hexdigest(),
                             "route": "packaged-eos" if name in {"EOSConnectLoginEditorTests", "EOSPrivateLobbyEditorTests", "EOSTwoMacLobbyEditorTests", "EOSTwoMacP2PEditorTests"} else "native",
                             "source": path.relative_to(root).as_posix()})
    return list({(p["mode"], p["fixture"]): p for p in plan}.values())


def load_fixture_manifest(root):
    path = root / "Documentation/Setup/OnlineMacTestManifest.json"
    manifest = json.loads(path.read_text(encoding="utf-8-sig"))
    if manifest.get("schema") != "counterattack-online-mac-tests-v1":
        raise RuntimeError("Unsupported son's Mac test manifest")
    discovered = {(f["mode"], f["fixture"]): f for f in discover_fixtures(root)}
    registered = {(f["mode"], f["fixture"]): f for f in manifest["fixtures"]}
    if set(discovered) != set(registered):
        raise RuntimeError("Online fixtures changed; add/update their entries in OnlineMacTestManifest.json before verification")
    for key, fixture in discovered.items():
        if registered[key].get("definition_sha256") != fixture["definition_sha256"]:
            raise RuntimeError("Tests changed in " + fixture["fixture"] + "; append new case entries to OnlineMacTestManifest.json and refresh its definition hash")
    ids = [entry["id"] for entry in manifest.get("test_additions", [])]
    if len(ids) != len(set(ids)):
        raise RuntimeError("Duplicate case entries in son's Mac manifest")
    return manifest, list(discovered.values())


def parse_xml(text):
    root = ET.fromstring(text)
    cases = []
    for case in root.iter("test-case"):
        failure = case.find("failure")
        cases.append({"name": case.get("fullname", case.get("name")), "result": case.get("result"),
                      "duration": case.get("duration"),
                      "message": failure.findtext("message", "") if failure is not None else ""})
    return {"cases": cases, "counts": {name: sum(c["result"] == name for c in cases)
                                        for name in ("Passed", "Failed", "Skipped", "Inconclusive")}}


def publish_report(root, report, run_id, push=True):
    """Create a report-only commit with a private index. Preserve HEAD and user's index."""
    if not re.fullmatch(r"[A-Za-z0-9_-]+", run_id):
        raise ValueError("Invalid run ID")
    branch = "verification/sons-mac-" + run_id
    path = "Documentation/Testing/MacReports/" + run_id + ".json"
    payload = clean(dict(report, publication={"branch": branch, "report_path": path}))
    source = report["source_commit"]
    with tempfile.TemporaryDirectory(prefix="counterattack-report-") as temporary:
        env = dict(os.environ, GIT_INDEX_FILE=str(Path(temporary) / "index"))
        checked(["git", "read-tree", source], root, env=env)
        blob = checked(["git", "hash-object", "-w", "--stdin"], root, input_text=json.dumps(payload, indent=2) + "\n")
        checked(["git", "update-index", "--add", "--cacheinfo", "100644," + blob + "," + path], root, env=env)
        tree = checked(["git", "write-tree"], root, env=env)
        commit = checked(["git", "commit-tree", tree, "-p", source], root,
                         input_text="Record son's Mac online verification " + run_id + "\n")
    # Never replace an existing verification branch.
    checked(["git", "update-ref", "refs/heads/" + branch, commit, "0" * 40], root)
    if push:
        checked(["git", "push", "origin", commit + ":refs/heads/" + branch], root, timeout=120)
    return {"branch": branch, "commit": commit, "report_path": path}


def source_fingerprint(root):
    # Temporary lobby/report commits must not invalidate otherwise identical
    # builds. Bind all committed gameplay/assets/packages/settings instead.
    trees = [checked(["git", "rev-parse", "HEAD:" + folder], root) for folder in ("Assets", "Packages", "ProjectSettings")]
    return hashlib.sha256("\n".join(trees).encode("utf-8")).hexdigest()


class UnityUnavailable(RuntimeError):
    def __init__(self, message, details=None):
        super().__init__(message)
        self.details = details or {}


class Runner:
    def __init__(self, root, options):
        self.root, self.options = root, options
        self.run_id = dt.datetime.now(dt.timezone.utc).strftime("%Y%m%dT%H%M%SZ") + "-" + str(os.getpid())
        self.runs = root / "TestResults/GameplayRegression/Runs"
        self.output = self.runs / (self.run_id + "-MacVerification")
        self.output.mkdir(parents=True)
        self.source = checked(["git", "rev-parse", "HEAD"], root)
        self.fingerprint = source_fingerprint(root)
        self.report = {"schema": "counterattack-mac-verification-v1", "run_id": self.run_id,
                       "started_utc": utc(), "source_commit": self.source, "source_fingerprint": self.fingerprint, "stages": [],
                       "scope": "Implemented online fixtures, Mac builds/local/login, paired automatic Room and prepared-board probes",
                       "limitations": ["Paired automatic probes cover the ground-pass/pause/recovery exchange; other gameplay families have native framework coverage, not paired human acceptance.",
                                       "Continuous full-roster Room and remaining duel/restart families are not complete."]}

    def save(self):
        target = self.output / "report.json"
        temporary = self.output / "report.json.tmp"
        temporary.write_text(json.dumps(clean(self.report), indent=2) + "\n", encoding="utf-8")
        temporary.replace(target)

    def stage(self, name, action):
        item = {"name": name, "started_utc": utc()}
        self.report["stages"].append(item)
        print("Running " + name, flush=True)
        try:
            data = action()
            item.update(data or {})
            item.setdefault("status", "passed")
        except BaseException as error:
            item.update(status="failed", error=clean(str(error)))
            if isinstance(error, UnityUnavailable):
                item["unity_unavailable"] = True
                item.update(error.details)
            if isinstance(error, KeyboardInterrupt):
                raise
        if item.get("status") == "failed" and (name == "compile" or name.startswith(("editor:", "playmode:"))):
            item["editor_log_tail"] = self.editor_log_tail()
        item["completed_utc"] = utc()
        self.save()
        print(name + ": " + item["status"], flush=True)
        return item

    def editor_log_tail(self):
        for path in (self.root / "Logs/Editor.log", Path.home() / "Library/Logs/Unity/Editor.log"):
            if path.is_file():
                with path.open("rb") as stream:
                    stream.seek(max(0, path.stat().st_size - 65536))
                    text = stream.read().decode("utf-8", errors="replace")
                return {"path": str(path), "text": clean(text)}
        return {"unavailable": True}

    def pipeline(self, command, *extra):
        try:
            output = checked(["unity", "command", command, *extra, "--project-path", str(self.root),
                              "--caller", "codex", "--skill", "unity-cli", "--format", "json"], self.root)
        except Exception as error:
            raise UnityUnavailable(str(error)) from error
        envelope = json.loads(output)
        if not envelope.get("success"):
            raise UnityUnavailable(clean(str(envelope.get("errors"))))
        return envelope["data"]["result"]

    def ready(self):
        status = self.pipeline("editor_status")
        if status.get("compiling") or status.get("domainReloadInProgress") or status.get("playMode") != "stopped":
            raise UnityUnavailable("Editor must be stopped and finished compiling/reloading; resolve any dialog manually.")
        if self.pipeline("test_status").get("status") == "running":
            raise UnityUnavailable("A framework run is pending; do not start another run or reload.")
        scenes = self.pipeline("list_open_scenes")["scenes"]
        if any(s["isDirty"] for s in scenes):
            self.pipeline("save_all")
            if any(s["isDirty"] for s in self.pipeline("list_open_scenes")["scenes"]):
                raise UnityUnavailable("Scenes remain dirty after save_all.")

    def wait_ready(self, seconds=120):
        deadline = time.monotonic() + seconds
        while True:
            try:
                self.ready()
                return
            except Exception:
                if time.monotonic() >= deadline:
                    raise UnityUnavailable("Editor did not finish framework cleanup/reload. Inspect its dialog/log manually.")
                time.sleep(2)

    def compile(self):
        envelope = json.loads(checked(["unity", "status", "--project-path", str(self.root), "--format", "json"], self.root))
        if not envelope.get("success") or not any(i.get("state") == "ready" for i in envelope.get("data", {}).get("instances", [])):
            raise UnityUnavailable("Open the correct checkout manually in Unity and resolve any modal; CLI is not ready.")
        self.ready()
        result = json.loads(checked(["unity", "recompile", "--project-path", str(self.root), "--format", "json"], self.root))
        data = result.get("data") or {}
        if not result.get("success") or data.get("failed") or data.get("compilationFailed") or data.get("summary", {}).get("errors", 0):
            raise UnityUnavailable(clean(json.dumps(result)))
        self.wait_ready()
        return {"compilation": result}

    def artifacts(self, before):
        artifacts = []
        for folder in sorted(set(self.runs.iterdir()) - before):
            if not folder.is_dir() or folder == self.output:
                continue
            names = {"results.xml", "progress.jsonl", "result.json", "gameplay-evidence.json", "final-state.json", "build-report.json", "Player.log", "stdout.log"}
            for path in sorted(folder.rglob("*")):
                if path.is_file() and path.name in names:
                    name = path.name
                    limit = 128000 if name.endswith(".log") else 16 * 1024 * 1024
                    with path.open("rb") as stream:
                        if name.endswith(".log"):
                            stream.seek(max(0, path.stat().st_size - limit))
                        text = stream.read(limit).decode("utf-8-sig", errors="replace")
                    artifacts.append({"path": path.relative_to(self.root).as_posix(), "text": text,
                                      "truncated": path.stat().st_size > limit})
        return artifacts

    def fixture(self, fixture):
        self.ready()
        before = set(self.runs.iterdir())
        self.pipeline("run_tests", "--mode", fixture["mode"], "--filter", fixture["fixture"],
                      "--filter_type", "testName", "--async_tests", "true")
        deadline = time.monotonic() + self.options.test_timeout
        failures = 0
        while True:
            try:
                result = self.pipeline("test_status")
                failures = 0
            except Exception:
                failures += 1
                if failures >= 10:
                    raise UnityUnavailable("Lost the Editor during native tests; run may still be pending.", {"artifacts": self.artifacts(before)})
                result = {"status": "running"}
            if result.get("status") != "running":
                break
            if time.monotonic() >= deadline:
                raise UnityUnavailable("Native test timeout; run remains pending. Further Editor stages will be skipped.", {"artifacts": self.artifacts(before), "last_native_status": result})
            time.sleep(2)
        evidence = self.artifacts(before)
        parsed = [parse_xml(a["text"]) for a in evidence if a["path"].endswith("results.xml") and not a["truncated"]]
        summary = result.get("summary") or {}
        passed = result.get("status") == "completed" and summary.get("total", 0) > 0 and summary.get("failed", 0) == 0 and bool(parsed)
        details = {"status": "passed" if passed else "failed", "fixture": fixture,
                   "native_result": result, "artifacts": evidence, "parsed_xml": parsed}
        try:
            self.wait_ready()
        except UnityUnavailable as error:
            details.update(status="failed", unity_unavailable=True, cleanup_error=str(error))
        return details

    def command_stage(self, command, timeout=1200):
        before = set(self.runs.iterdir())
        result = execute(command, self.root, timeout=timeout)
        artifacts = self.artifacts(before)
        return {"status": "passed" if result["exit_code"] == 0 else "failed", "command": command,
                "exit_code": result["exit_code"], "output": result["output"][-128000:], "artifacts": artifacts}

    def gh(self, endpoint, *extra, optional=False):
        result = execute(["gh", "api", endpoint, *extra], self.root)
        if result["exit_code"]:
            if optional:
                return None
            raise RuntimeError(clean(result["output"][-8000:]))
        return json.loads(result["output"] or "null")

    def assert_source(self):
        current = checked(["git", "rev-parse", "HEAD"], self.root)
        dirty = checked(["git", "status", "--porcelain", "--untracked-files=all", "--", "Assets/Scripts", "Assets/Editor/FeatureTests", "Assets/Tests", "Documentation/Setup",
                         "Assets/Scenes/OnlineRoomProof.unity", "Assets/Scenes/OnlineGroundPassProof.unity", "Assets/Scenes/EOSStandaloneProof.unity"], self.root)
        if current != self.source or dirty:
            raise RuntimeError("Verification code changed or has uncommitted edits; preserve them and commit/synchronize before comparing builds.\n" + dirty)

    def queue_branch(self):
        endpoint = REPOSITORY + "/git/ref/heads/" + QUEUE_BRANCH
        if self.gh(endpoint, optional=True) is None:
            self.gh(REPOSITORY + "/git/refs", "-X", "POST", "-f", "ref=refs/heads/" + QUEUE_BRANCH, "-f", "sha=" + self.source)

    def paired(self, mode, attempt):
        self.assert_source()
        self.queue_branch()
        request_id = self.run_id + "-" + mode + "-" + str(attempt)
        created = int(time.time())
        request = {"kind": "online-verification-request-v1", "requestId": request_id,
                   "runId": self.run_id, "mode": mode, "sourceCommit": self.source,
                   "sourceFingerprint": self.fingerprint,
                   "createdUnix": created, "status": "requested"}
        endpoint = REPOSITORY + "/contents/" + QUEUE_PATH + "/" + request_id + ".json"
        self.gh(endpoint, "-X", "PUT", "-f", "branch=" + QUEUE_BRANCH,
                "-f", "message=Request Windows " + mode + " verification",
                "-f", "content=" + base64.b64encode(json.dumps(request).encode()).decode())
        response_endpoint = REPOSITORY + "/contents/" + QUEUE_PATH + "/" + request_id + "-host.json?ref=" + QUEUE_BRANCH
        handoff_name = "ActiveEOSRoomLobby.json" if mode == "room" else "ActiveEOSGameplayLobby.json"
        handoff_kind = "room-gameplay-v5" if mode == "room" else "ground-gameplay-v7"
        deadline = time.monotonic() + self.options.host_wait
        host = None
        while time.monotonic() < deadline:
            response = self.gh(response_endpoint, optional=True)
            if response:
                host = json.loads(base64.b64decode(response["content"]))
                if host.get("status") in ("failed", "rejected"):
                    return {"status": "failed", "request": request, "host": host}
                handoff = self.gh(REPOSITORY + "/contents/Documentation/Setup/" + handoff_name + "?ref=" + SOURCE_BRANCH, optional=True)
                if handoff:
                    lobby = json.loads(base64.b64decode(handoff["content"]))
                    if lobby.get("kind") == handoff_kind and lobby.get("requestId") == request_id:
                        break
            time.sleep(5)
        else:
            return {"status": "skipped", "reason": "No matching Windows host became ready before the deadline.", "request": request, "host": host}
        result = self.command_stage(["bash", "Documentation/Setup/RunEOSStandaloneMac.sh", "guest", mode], timeout=300)
        deadline = time.monotonic() + 90
        while time.monotonic() < deadline:
            response = self.gh(response_endpoint, optional=True)
            if response:
                host = json.loads(base64.b64decode(response["content"]))
                if host.get("status") in ("passed", "failed", "rejected"):
                    break
            time.sleep(3)
        result.update(request=request, host=host)
        if not host or host.get("status") != "passed":
            result["status"] = "failed"
        # Require both retained reports and equal digests, not just two exit codes.
        guest_evidence = [json.loads(a["text"]) for a in result.get("artifacts", []) if a["path"].endswith("gameplay-evidence.json")]
        host_evidence = (host or {}).get("evidence") or {}
        if not guest_evidence or guest_evidence[-1].get("digest") != host_evidence.get("digest") or not guest_evidence[-1].get("guestRulesInactive"):
            result["status"] = "failed"
            result["comparison_error"] = "Missing paired evidence, unequal digests, or guest rules were active."
        return result

    def run(self):
        try:
            for tool in ("git", "unity", "gh", "jq", "bash"):
                if not shutil.which(tool):
                    raise RuntimeError("Missing tool: " + tool + "; follow PersonalMac.md")
            if sys.platform != "darwin":
                raise RuntimeError("Run this verifier on the Mac; Windows uses the host supervisor.")
            self.report["platform"] = checked(["sw_vers"], self.root)
            self.report["working_tree"] = checked(["git", "status", "--short"], self.root)
            self.report["free_bytes"] = shutil.disk_usage(self.root).free
            if self.report["free_bytes"] < 2 * 1024 ** 3:
                raise RuntimeError("Less than 2 GiB free; free disk space before Unity testing/building to avoid the low-space modal.")
            self.assert_source()
            checked(["git", "var", "GIT_AUTHOR_IDENT"], self.root)
            checked(["gh", "auth", "status"], self.root)
            manifest, plan = load_fixture_manifest(self.root)
            if not plan:
                raise RuntimeError("No implemented online fixtures discovered")
            self.report["fixture_plan"] = plan
            self.report["test_manifest"] = manifest
            compiled = self.stage("compile", self.compile)
            usable = compiled["status"] == "passed"
            for fixture in plan:
                if fixture["route"] == "packaged-eos":
                    self.report["stages"].append({"name": fixture["fixture"], "status": "superseded",
                                                  "reason": "Legacy one-use Editor marker probe; verified by the packaged login/private-lobby/forced-relay stages instead. No local marker was changed."})
                    continue
                if not usable:
                    self.report["stages"].append({"name": fixture["mode"] + ":" + fixture["fixture"], "status": "skipped", "reason": "Editor unavailable or native run pending"})
                    continue
                result = self.stage(fixture["mode"] + ":" + fixture["fixture"], lambda f=fixture: self.fixture(f))
                usable = not result.get("unity_unavailable", False)
            builds = {}
            for mode in ("room", "gameplay"):
                if not usable:
                    builds[mode] = False
                    self.report["stages"].append({"name": "build:" + mode, "status": "skipped", "reason": "Editor unavailable or run pending"})
                    continue
                try:
                    self.ready()
                except Exception:
                    usable = False
                    builds[mode] = False
                    self.report["stages"].append({"name": "build:" + mode, "status": "skipped", "reason": "Editor not ready"})
                    continue
                builds[mode] = self.stage("build:" + mode, lambda m=mode: self.command_stage(["bash", "Documentation/Setup/BuildEOSStandaloneMac.sh", m]))["status"] == "passed"
                # A timed-out build can still be active; never queue another.
                if self.report["stages"][-1].get("exit_code") == 124:
                    usable = False
            for mode in ("room", "gameplay"):
                if not builds.get(mode):
                    self.report["stages"].append({"name": "packaged:" + mode, "status": "skipped", "reason": "Fresh build unavailable; stale exports were not used"})
                    continue
                if mode == "room":
                    self.stage("login:room", lambda: self.command_stage(["bash", "Documentation/Setup/RunEOSStandaloneMac.sh", "login", "room"], 180))
                self.stage("local:" + mode, lambda m=mode: self.command_stage(["bash", "Documentation/Setup/RunEOSStandaloneMac.sh", "local", m], 180))
                if self.options.no_host:
                    self.report["stages"].append({"name": "paired:" + mode, "status": "skipped", "reason": "Host stage explicitly disabled"})
                else:
                    for attempt in range(1, self.options.attempts + 1):
                        result = self.stage("paired:" + mode + ":attempt-" + str(attempt), lambda m=mode, a=attempt: self.paired(m, a))
                        if result["status"] == "passed":
                            break
        except BaseException as error:
            self.report["fatal_error"] = clean(str(error))
        finally:
            stages = self.report["stages"]
            self.report["completed_utc"] = utc()
            cases = {}
            for stage in stages:
                for native in stage.get("parsed_xml", []):
                    for case in native["cases"]:
                        cases[case["name"]] = case
            self.report["distinct_final_native_counts"] = {name: sum(c["result"] == name for c in cases.values()) for name in ("Passed", "Failed", "Skipped", "Inconclusive")}
            failed = bool(self.report.get("fatal_error")) or any(s.get("status") == "failed" for s in stages)
            skipped = any(s.get("status") == "skipped" for s in stages) or any(self.report["distinct_final_native_counts"][name] for name in ("Skipped", "Inconclusive"))
            self.report["status"] = "failed" if failed else "incomplete" if skipped else "passed"
            self.save()
            try:
                publication = publish_report(self.root, self.report, self.run_id)
                (self.output / "publication.json").write_text(json.dumps(publication, indent=2), encoding="utf-8")
                print("Report pushed: " + publication["branch"] + "\n" + publication["report_path"], flush=True)
            except Exception as error:
                self.report["publication_error"] = clean(str(error))
                self.save()
                print("Publication failed; retained complete local report: " + str(self.output / "report.json"), flush=True)
            print("Verification " + self.report["status"] + ": " + str(self.output / "report.json"), flush=True)
        return 1 if self.report["status"] == "failed" or self.report.get("publication_error") else 2 if self.report["status"] == "incomplete" else 0


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--no-host", action="store_true", help="Record paired probes as skipped")
    parser.add_argument("--host-wait", type=int, default=600)
    parser.add_argument("--attempts", type=int, choices=range(1, 4), default=2)
    parser.add_argument("--test-timeout", type=int, default=1200)
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[2]
    return Runner(root, args).run()


if __name__ == "__main__":
    sys.exit(main())
