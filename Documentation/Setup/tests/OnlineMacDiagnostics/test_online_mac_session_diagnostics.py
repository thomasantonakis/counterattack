import contextlib
import importlib.util
import io
import json
import os
from pathlib import Path
import sys
import tempfile
import time
import unittest
from unittest import mock

MODULE = Path(__file__).resolve().parents[2] / "online_mac_session_diagnostics.py"
sys.path.insert(0, str(MODULE.parent))
spec = importlib.util.spec_from_file_location("online_mac_diagnostics", MODULE)
diag = importlib.util.module_from_spec(spec)
spec.loader.exec_module(diag)

class MacSessionDiagnosticsTests(unittest.TestCase):
    def session(self, home, name, modified, room=True):
        path=home/"Library/Application Support/DefaultCompany/CA from Chatgpt/OnlineSessions"/name
        path.mkdir(parents=True)
        if room:
            (path/"room-bootstrap.json").write_text('{"roomSessionId":"'+name+'"}')
            os.utime(path/"room-bootstrap.json",(modified,modified))
        (path/"frontend-session-report.json").write_text('{"status":"failed","message":"InvalidAuth"}')
        os.utime(path/"frontend-session-report.json",(modified,modified))
        return path

    def test_latest_room_session_is_selected_and_account_config_is_excluded(self):
        with tempfile.TemporaryDirectory() as temporary:
            home=Path(temporary)
            self.session(home,"older",100)
            latest=self.session(home,"active",200)
            self.session(home,"later-editor-probe",300,False)
            (latest/"account-config.json").write_text('{"clientSecret":"never collect"}')
            result=diag.collect(home,400)
            self.assertEqual(result["session_directory"],str(latest))
            self.assertEqual(len(result["artifacts"]),2)
            self.assertNotIn("never collect",json.dumps(result))
            self.assertTrue(all("active" in a["path"] for a in result["artifacts"]))

    def test_only_recent_matching_crashes_are_collected_and_capped(self):
        with tempfile.TemporaryDirectory() as temporary:
            home=Path(temporary);folder=home/"Library/Logs/DiagnosticReports";folder.mkdir(parents=True)
            for name,modified in [("CA from Chatgpt-a.ips",99999),("Counterattack-b.crash",99998),("CA from Chatgpt-c.ips",99997),("CA from Chatgpt-old.ips",1),("OtherApp.ips",100000)]:
                path=folder/name;path.write_text(name);os.utime(path,(modified,modified))
            result=diag.collect(home,100000)
            self.assertEqual(result["crash_reports_found"],2)
            self.assertEqual([Path(a["path"]).name for a in result["artifacts"]],["CA from Chatgpt-a.ips","Counterattack-b.crash"])

    def test_log_collection_is_bounded_to_tail_and_redacts_tokens(self):
        with tempfile.TemporaryDirectory() as temporary:
            home=Path(temporary);path=home/"Library/Logs/DefaultCompany/CA from Chatgpt/Player.log";path.parent.mkdir(parents=True)
            path.write_text("discard-start\n"+"x"*300000+"\naccessToken=private-value\nidToken=private-id\neyJabc.def.signature\n[Licensing::Client] do not collect\nfinal-crash-marker",encoding="utf-8")
            before=path.read_bytes();result=diag.collect(home);entry=result["artifacts"][0]
            self.assertTrue(entry["truncated"])
            self.assertEqual(entry["slice"],"tail")
            self.assertIn("final-crash-marker",entry["text"])
            for secret in ["discard-start","private-value","private-id","eyJabc.def.signature","Licensing::Client"]:
                self.assertNotIn(secret,entry["text"])
            self.assertEqual(path.read_bytes(),before)

    def test_missing_evidence_is_reported_without_creating_home_files(self):
        with tempfile.TemporaryDirectory() as temporary:
            home=Path(temporary);result=diag.collect(home)
            self.assertEqual(result["status"],"no_matching_evidence")
            self.assertEqual(list(home.iterdir()),[])

    def test_artifact_limits_crash_head_and_preserves_unicode(self):
        with tempfile.TemporaryDirectory() as temporary:
            path=Path(temporary)/"crash.ips";path.write_text("\u03b1"*1000,encoding="utf-8")
            entry=diag.artifact(path,100)
            self.assertTrue(entry["truncated"])
            self.assertEqual(entry["text"],"\u03b1"*50)

    def test_publication_failure_keeps_local_report_without_unity_commands(self):
        with tempfile.TemporaryDirectory() as temporary:
            root=Path(temporary);home=root/"home";home.mkdir();self.session(home,"active",time.time())
            with mock.patch.object(diag,"__file__",str(root/"Documentation/Setup/collector.py")),mock.patch.object(diag.sys,"platform","darwin"),mock.patch.object(diag.Path,"home",return_value=home),mock.patch.object(diag.verification,"checked",return_value="a"*40) as checked,mock.patch.object(diag.verification,"source_fingerprint",return_value="b"*64),mock.patch.object(diag.verification,"publish_report",side_effect=RuntimeError("offline")),contextlib.redirect_stdout(io.StringIO()):
                self.assertEqual(diag.main(),1)
            checked.assert_called_once_with(["git","rev-parse","HEAD"],root)
            paths=list((root/"TestResults/GameplayRegression/Runs").glob("*/report.json"))
            self.assertEqual(len(paths),1)
            self.assertEqual(json.loads(paths[0].read_text())["publication_error"],"offline")

    def test_wrong_platform_refuses_before_collection(self):
        with mock.patch.object(diag.sys,"platform","win32"),mock.patch.object(diag,"collect") as collect,contextlib.redirect_stderr(io.StringIO()):
            self.assertEqual(diag.main(),1)
            collect.assert_not_called()
