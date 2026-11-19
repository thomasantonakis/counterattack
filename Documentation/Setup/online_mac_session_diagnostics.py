"""Collect a normal-game Mac session/crash report; never launch Unity or a game."""
import datetime as dt
import json
import os
from pathlib import Path
import re
import sys
import time
import online_verification as verification

SESSION_FILES = ("frontend-session-report.json", "room-bootstrap.json", "room-authority.json")

def redact(value):
    text = verification.clean(value)
    if isinstance(text, str):
        text = re.sub(r"\beyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+", "[REDACTED JWT]", text)
        text = re.sub(r'(?i)((?:id.?token|device.?secret)\s*[\"\s:=]+)[^\s\",<>]+', r'\1[REDACTED]', text)
    return text

def artifact(path, limit, tail=False):
    size = path.stat().st_size
    with path.open("rb") as stream:
        if tail:
            stream.seek(max(0, size-limit))
        text = stream.read(limit).decode("utf-8-sig", errors="replace")
    return {"path": str(path), "size_bytes": size, "truncated": size > limit,
            "slice": "tail" if tail else "head", "text": redact(text)}

def collect(home, now=None):
    home = Path(home)
    now = time.time() if now is None else now
    sessions = home / "Library/Application Support/DefaultCompany/CA from Chatgpt/OnlineSessions"
    folders = [path for path in sessions.iterdir() if path.is_dir()] if sessions.is_dir() else []
    # A Room launch identifies a normal-game match; do not pick an unrelated
    # one-use Editor/login probe just because it wrote a later report.
    folders = [path for path in folders if (path / "room-bootstrap.json").is_file()]
    selected = max(folders, key=lambda path: max((path/name).stat().st_mtime for name in SESSION_FILES if (path/name).is_file()), default=None)
    report = {"schema": "counterattack-mac-session-diagnostics-v1", "collected_utc": verification.utc(),
              "scope": "Latest normal-game Room session, bounded player logs and matching recent crash reports",
              "session_directory": str(selected) if selected else None, "artifacts": [],
              "limitations": ["Collection commit identifies the collector checkout; it does not independently prove the executable build commit.",
                              "No Unity Editor commands, game launch, save mutation or account-configuration collection."]}
    if selected:
        for name in SESSION_FILES:
            path = selected / name
            if path.is_file():
                report["artifacts"].append(artifact(path, 2*1024*1024))
    for relative in ("Library/Logs/DefaultCompany/CA from Chatgpt/Player.log", "Library/Logs/Unity/Player.log"):
        path = home / relative
        if path.is_file():
            report["artifacts"].append(artifact(path, 256*1024, tail=True))
    crashes = home / "Library/Logs/DiagnosticReports"
    candidates = []
    if crashes.is_dir():
        for path in crashes.iterdir():
            name = path.name.lower().replace(" ", "")
            if path.is_file() and path.suffix.lower() in (".ips", ".crash") and ("cafromchatgpt" in name or "counterattack" in name) and path.stat().st_mtime >= now-6*3600:
                candidates.append(path)
    for path in sorted(candidates, key=lambda item: item.stat().st_mtime, reverse=True)[:2]:
        report["artifacts"].append(artifact(path, 2*1024*1024))
    report["crash_reports_found"] = sum(path.suffix.lower() in (".ips", ".crash") for path in candidates[:2])
    report["status"] = "collected" if selected or report["artifacts"] else "no_matching_evidence"
    return report

def main():
    if sys.platform != "darwin":
        print("Run this collector on the son's Mac; no Unity or game launch is needed.", file=sys.stderr)
        return 1
    root = Path(__file__).resolve().parents[2]
    run_id = dt.datetime.now(dt.timezone.utc).strftime("%Y%m%dT%H%M%SZ") + "-session-" + str(os.getpid())
    folder = root / "TestResults/GameplayRegression/Runs" / (run_id+"-MacSessionDiagnostics")
    folder.mkdir(parents=True)
    report = collect(Path.home())
    report["run_id"] = run_id
    report["source_commit"] = verification.checked(["git", "rev-parse", "HEAD"], root)
    report["source_fingerprint"] = verification.source_fingerprint(root)
    target = folder / "report.json"
    target.write_text(json.dumps(report, indent=2)+"\n", encoding="utf-8")
    print("Retained diagnostics: " + str(target), flush=True)
    try:
        published = verification.publish_report(root, report, run_id)
        print("Report pushed: " + published["branch"] + "\n" + published["report_path"], flush=True)
    except Exception as error:
        report["publication_error"] = redact(str(error))
        target.write_text(json.dumps(report, indent=2)+"\n", encoding="utf-8")
        print("Publish failed; local report retained: " + str(target), flush=True)
        return 1
    print("Crash reports found: " + str(report["crash_reports_found"]), flush=True)
    return 0

if __name__ == "__main__":
    sys.exit(main())
