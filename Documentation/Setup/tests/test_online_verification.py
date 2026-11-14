import importlib.util
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest import mock
import contextlib
import io

MODULE = Path(__file__).resolve().parents[1] / "online_verification.py"
spec = importlib.util.spec_from_file_location("online_verification", MODULE)
verification = importlib.util.module_from_spec(spec)
spec.loader.exec_module(verification)


class VerificationTests(unittest.TestCase):
    def test_credential_redaction_preserves_gameplay_token_identity(self):
        redacted = verification.clean({"clientSecret": "secret", "access_token": "private",
                                       "actorId": "home:9", "log": 'Bearer abc.def\n"clientSecret": "secret"\n[Licensing::Client] private'})
        self.assertEqual(redacted["actorId"], "home:9")
        self.assertNotIn("secret", json.dumps(redacted))
        self.assertNotIn("private", json.dumps(redacted))
        self.assertNotIn("abc.def", json.dumps(redacted))

    def test_native_xml_preserves_parameter_case_failures(self):
        parsed = verification.parse_xml('<test-run><test-suite><test-case fullname="Fixture.Test(1)" result="Passed"/>'
                                        '<test-case fullname="Fixture.Test(-1)" result="Failed"><failure><message>wrong possession</message></failure></test-case>'
                                        '<test-case fullname="Fixture.Skip" result="Skipped"/></test-suite></test-run>')
        self.assertEqual(parsed["counts"], {"Passed": 1, "Failed": 1, "Skipped": 1, "Inconclusive": 0})
        self.assertEqual(parsed["cases"][1]["message"], "wrong possession")

    def test_legacy_live_markers_are_replaced_by_packaged_eos_checks(self):
        with tempfile.TemporaryDirectory(prefix="counterattack-verifier-eos-plan-") as temporary:
            root=Path(temporary)
            folder=root/"Assets/Editor/FeatureTests";folder.mkdir(parents=True)
            (folder/"Legacy.cs").write_text('[Category("OnlineEOSConnectivity")] public sealed class EOSTwoMacP2PEditorTests {}')
            (folder/"Reuse.cs").write_text('[Category("OnlineEOSConnectivity")] public sealed class EOSProbeSessionReuseEditorTests {}')
            routes={p["fixture"]:p["route"] for p in verification.discover_fixtures(root)}
            self.assertEqual(routes["EOSTwoMacP2PEditorTests"],"packaged-eos")
            self.assertEqual(routes["EOSProbeSessionReuseEditorTests"],"native")

    def test_plan_selects_only_online_fixtures_and_preserves_modes(self):
        with tempfile.TemporaryDirectory(prefix="counterattack-verifier-plan-") as temporary:
            root = Path(temporary)
            editor = root / "Assets/Editor/FeatureTests"; editor.mkdir(parents=True)
            play = root / "Assets/Tests/PlayMode"; play.mkdir(parents=True)
            (editor / "Editor.cs").write_text('[Category("OnlineGroundPass")] public sealed class GroundRulesTests {}')
            (play / "Gameplay.cs").write_text('[Category("OnlineGroundPassNetwork")] public sealed class NetworkTests {}')
            (play / "Other.cs").write_text('[Category("Legacy")] public sealed class LegacyTests {}')
            plan = verification.discover_fixtures(root)
            self.assertEqual([(p["mode"], p["fixture"]) for p in plan], [("editor", "GroundRulesTests"), ("playmode", "NetworkTests")])

    def test_report_only_commit_preserves_head_index_and_local_edits(self):
        with tempfile.TemporaryDirectory(prefix="counterattack-verifier-git-") as temporary:
            root = Path(temporary)
            def git(*args):
                return verification.checked(["git", *args], root)
            git("init", "--quiet")
            git("config", "user.name", "Verification fixture")
            git("config", "user.email", "fixture@example.invalid")
            (root / "source.txt").write_text("source\n")
            git("add", "source.txt"); git("commit", "--quiet", "-m", "fixture source")
            source = git("rev-parse", "HEAD")
            (root / "source.txt").write_text("staged user edit\n")
            git("add", "source.txt")
            (root / "source.txt").write_text("unstaged user edit\n")
            (root / "untracked.txt").write_text("preserve\n")
            status = git("status", "--porcelain")
            staged = git("diff", "--cached")
            result = verification.publish_report(root, {"source_commit": source, "status": "failed", "clientSecret": "secret"}, "fixture-001", push=False)
            self.assertEqual(git("rev-parse", "HEAD"), source)
            self.assertEqual(git("status", "--porcelain"), status)
            self.assertEqual(git("diff", "--cached"), staged)
            changed = git("diff-tree", "--no-commit-id", "--name-only", "-r", result["commit"])
            self.assertEqual(changed, result["report_path"])
            published = json.loads(git("show", result["commit"] + ":" + result["report_path"]))
            self.assertEqual(published["status"], "failed")
            self.assertEqual(published["clientSecret"], "[REDACTED]")
            with self.assertRaises(RuntimeError):
                verification.publish_report(root, {"source_commit": source}, "fixture-001", push=False)
            self.assertEqual(git("rev-parse", "refs/heads/" + result["branch"]), result["commit"])

    def test_timeout_retires_only_the_owned_command(self):
        result = verification.execute([sys.executable, "-c", "import time; time.sleep(30)"], Path.cwd(), timeout=.1)
        self.assertEqual(result["exit_code"], 124)
        self.assertTrue(result["timed_out"])

    def test_invalid_report_branch_id_does_not_run_git(self):
        with self.assertRaises(ValueError):
            verification.publish_report(Path.cwd(), {"source_commit": "unused"}, "../../unsafe", push=False)

    def test_lobby_commits_preserve_source_identity_but_gameplay_changes_do_not(self):
        with tempfile.TemporaryDirectory(prefix="counterattack-verifier-fingerprint-") as temporary:
            root=Path(temporary)
            def git(*args):return verification.checked(["git",*args],root)
            git("init","--quiet");git("config","user.name","Fixture");git("config","user.email","fixture@example.invalid")
            for folder in ("Assets","Packages","ProjectSettings"):
                path=root/folder;path.mkdir();(path/"fixture.txt").write_text("baseline")
            git("add",".");git("commit","--quiet","-m","baseline")
            baseline=verification.source_fingerprint(root)
            (root/"temporary-lobby.json").write_text("{}")
            git("add","temporary-lobby.json");git("commit","--quiet","-m","lobby")
            self.assertEqual(verification.source_fingerprint(root),baseline)
            (root/"Assets/fixture.txt").write_text("changed gameplay")
            git("add","Assets");git("commit","--quiet","-m","gameplay")
            self.assertNotEqual(verification.source_fingerprint(root),baseline)

    def test_failed_stage_retains_native_evidence_and_redacts_it(self):
        with tempfile.TemporaryDirectory(prefix="counterattack-verifier-stage-") as temporary:
            with mock.patch.object(verification,"checked",return_value="a"*40):
                runner=verification.Runner(Path(temporary),object())
            def fail():
                raise verification.UnityUnavailable("dialog",{"artifacts":[{"text":"accessToken=private"}]})
            with contextlib.redirect_stdout(io.StringIO()):
                stage=runner.stage("native",fail)
            self.assertEqual(stage["status"],"failed")
            self.assertTrue(stage["unity_unavailable"])
            report=(runner.output/"report.json").read_text()
            self.assertIn("artifacts",report)
            self.assertNotIn("private",report)

    def test_nested_host_guest_evidence_is_included_but_config_files_are_not(self):
        with tempfile.TemporaryDirectory(prefix="counterattack-verifier-artifacts-") as temporary:
            with mock.patch.object(verification,"checked",return_value="a"*40):
                runner=verification.Runner(Path(temporary),object())
            before=set(runner.runs.iterdir())
            host=runner.runs/"native-transport/host";host.mkdir(parents=True)
            (host/"gameplay-evidence.json").write_text('{"digest":"actual"}')
            (host/"account-config.json").write_text('{"clientSecret":"never collect"}')
            artifacts=runner.artifacts(before)
            self.assertEqual(len(artifacts),1)
            self.assertTrue(artifacts[0]["path"].endswith("host/gameplay-evidence.json"))

    def test_fatal_preflight_still_publishes_failed_report(self):
        with tempfile.TemporaryDirectory(prefix="counterattack-verifier-fatal-") as temporary:
            with mock.patch.object(verification,"checked",return_value="a"*40):
                runner=verification.Runner(Path(temporary),object())
            publication={"branch":"verification/fixture","commit":"b"*40,"report_path":"report.json"}
            with mock.patch.object(verification.sys,"platform","unsupported"), mock.patch.object(verification.shutil,"which",return_value="tool"), mock.patch.object(verification,"publish_report",return_value=publication) as publish, contextlib.redirect_stdout(io.StringIO()):
                self.assertEqual(runner.run(),1)
            self.assertEqual(publish.call_args.args[1]["status"],"failed")
            self.assertIn("Run this verifier on the Mac",(runner.output/"report.json").read_text())


if __name__ == "__main__":
    unittest.main()
