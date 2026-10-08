"""Behavioral checks for the trusted automation boundary; no real API calls."""
import argparse
import contextlib
import datetime as dt
import hashlib
import importlib.util
import io
import json
import os
from pathlib import Path
import subprocess
import tempfile
import unittest
import urllib.error
from unittest import mock


SOURCE = Path(__file__).resolve().parents[2] / "tools" / "automation" / "main.py"
SPEC = importlib.util.spec_from_file_location("rpg_automation", SOURCE)
automation = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(automation)


def command(root, *args, env=None):
    result = subprocess.run(["git", *args], cwd=root, stdout=subprocess.PIPE,
                            stderr=subprocess.PIPE, check=True, env=env)
    return result.stdout.decode().strip()


class IsolatedActionTestCase(unittest.TestCase):
    def setUp(self):
        # Helpers legitimately append GITHUB_OUTPUT in production. Tests must
        # never append to the hosting workflow's real step-output transport.
        temporary = tempfile.TemporaryDirectory(prefix="rpg-test-action-output-")
        self.addCleanup(temporary.cleanup)
        root = Path(temporary.name)
        patcher = mock.patch.dict(os.environ, {"GITHUB_OUTPUT": str(root / "output"),
                                              "GITHUB_ENV": str(root / "env"),
                                              "GITHUB_STEP_SUMMARY": str(root / "summary")})
        patcher.start()
        self.addCleanup(patcher.stop)


class ClockAndPathTests(IsolatedActionTestCase):
    def setUp(self):
        IsolatedActionTestCase.setUp(self)
        self.now = dt.datetime(2026, 10, 8, 12, 0, 0, tzinfo=dt.timezone.utc)

    def test_hourly_gate_does_not_reset_at_midnight(self):
        state = {"schema_version": 1, "last_started_at": "2026-10-07T22:17:00Z"}
        before = dt.datetime(2026, 10, 8, 3, 16, 59, tzinfo=dt.timezone.utc)
        at_due = before + dt.timedelta(seconds=1)
        self.assertFalse(automation.evaluate_state(state, before)[0])
        self.assertTrue(automation.evaluate_state(state, at_due)[0])

    def test_failed_iteration_still_waits_five_hours(self):
        state = {"schema_version": 1, "last_started_at": "2026-10-08T10:00:00Z",
                 "status": "failed"}
        due, reason, deadline = automation.evaluate_state(state, self.now)
        self.assertFalse(due)
        self.assertEqual(reason, "interval_not_elapsed")
        self.assertEqual(deadline, "2026-10-08T15:00:00Z")

    def test_future_and_corrupt_state_fail_closed(self):
        for state in [{}, {"schema_version": 2}, {"schema_version": 1, "last_started_at": "bad"},
                      {"schema_version": 1, "last_started_at": "2026-10-08T12:00:01Z"}]:
            with self.subTest(state=state), self.assertRaises(automation.AutomationError):
                automation.evaluate_state(state, self.now)

    def test_missing_state_is_initially_due(self):
        self.assertEqual(automation.evaluate_state(None, self.now),
                         (True, "ready", "2026-10-08T12:00:00Z"))

    def test_unsafe_paths_are_rejected_without_reading_contents(self):
        for path in ["../id_rsa", "/tmp/key", "Assets/../key", ".git/config", ".ssh/id_rsa",
                     "Assets/link\\escape.cs", "Assets/key.pem", ".env.local", "Library/cache",
                     "Assets/a\nb.cs", "C:/file", "Assets//Player.cs"]:
            with self.subTest(path=path), self.assertRaises(automation.AutomationError):
                automation.safe_path(path)
        self.assertEqual(automation.safe_path("Assets/Actions/Acts Walk_Up.png"),
                         "Assets/Actions/Acts Walk_Up.png")

    def test_risks_are_conservative(self):
        changes = [{"path": "Assets/Scripts/Player/PlayerMovement.cs", "status": "M"}]
        self.assertEqual(automation.risk_report(changes, b"+speed = 4;")["risk"], "low")
        self.assertEqual(automation.risk_report(changes, b"+Physics2D.Raycast(start, end);")["risk"], "high")
        result = automation.risk_report([{"path": ".github/workflows/ci.yml", "status": "M"}], b"+run")
        self.assertTrue(result["requires_manual_review"])
        self.assertEqual(result["risk"], "high")

    def test_native_test_bypasses_and_new_modules_are_high_risk(self):
        covered = [{"path": "Assets/Scripts/Player/PlayerMovement.cs", "status": "M"}]
        for snippet in [b"+Environment.Exit(0);", b"+File.WriteAllText(path, data);",
                        b"+[DllImport(\"libc\")]", b"+new HttpClient();", b"+Console.WriteLine(\"PASS\");"]:
            with self.subTest(snippet=snippet):
                self.assertEqual(automation.risk_report(covered, snippet)["risk"], "high")
        new_module = [{"path": "Assets/Scripts/NewFeature.cs", "status": "A"}]
        self.assertEqual(automation.risk_report(new_module, b"+class NewFeature {}")["risk"], "high")


class FakeGitHub:
    state = None
    state_sha = None
    requests = []
    writes = []
    branch_created = False

    def __init__(self, repository):
        self.repository = repository

    def request(self, method, path, *args, **kwargs):
        self.requests.append((method, path))
        if path == "":
            return {"default_branch": "master"}
        if path == "/actions/workflows/rpg-continuous-improvement.yml":
            return {"state": "active"}
        if path.startswith("/git/ref/heads/"):
            return {"object": {"sha": "a" * 40}}
        raise AssertionError(path)

    def read_state(self, branch):
        return self.state, self.state_sha

    def ensure_state_branch(self, branch, sha):
        type(self).branch_created = True

    def write_state(self, branch, state, sha):
        self.writes.append((branch, state, sha))


class GateTests(IsolatedActionTestCase):
    def setUp(self):
        IsolatedActionTestCase.setUp(self)
        FakeGitHub.state = None
        FakeGitHub.state_sha = None
        FakeGitHub.requests = []
        FakeGitHub.writes = []
        FakeGitHub.branch_created = False
        self.args = argparse.Namespace(repository="jinze909/2D-RPG-Project", branch="master",
                                       state_branch="rpg-automation-state", force=False, dry_run=False)
        self.now = dt.datetime(2026, 10, 8, 12, 0, 0, tzinfo=dt.timezone.utc)

    def invoke(self, presence="true"):
        output = io.StringIO()
        with mock.patch.object(automation, "GitHub", FakeGitHub), \
                mock.patch.object(automation, "utc_now", return_value=self.now), \
                mock.patch.dict(os.environ, {"OPENAI_API_KEY_PRESENT": presence, "GITHUB_RUN_ID": "123"}, clear=True), \
                contextlib.redirect_stdout(output):
            automation.gate(self.args)
        return json.loads(output.getvalue())

    def test_key_presence_is_only_boolean_and_no_key_blocks_claim(self):
        result = self.invoke("false")
        self.assertFalse(result["due"])
        self.assertEqual(result["reason"], "missing_api_key")
        self.assertFalse(FakeGitHub.writes)
        self.assertFalse(FakeGitHub.branch_created)
        with self.assertRaises(automation.AutomationError):
            self.invoke("not-a-boolean")

    def test_dry_run_does_not_mutate_persistent_state(self):
        self.args.dry_run = True
        result = self.invoke()
        self.assertTrue(result["due"])
        self.assertTrue(result["dry_run"])
        self.assertFalse(FakeGitHub.writes)
        self.assertFalse(FakeGitHub.branch_created)

    def test_claim_records_start_before_agent_and_preserves_prior_result(self):
        FakeGitHub.state = {"schema_version": 1, "last_started_at": "2026-10-08T06:00:00Z",
                            "last_result": {"branch": "rpg/previous", "commit_sha": "b" * 40,
                                            "published_to_master": False}}
        FakeGitHub.state_sha = "c" * 40
        result = self.invoke()
        self.assertTrue(result["due"])
        self.assertEqual(result["next_due"], "2026-10-08T17:00:00Z")
        branch, claimed, old_sha = FakeGitHub.writes[0]
        self.assertEqual(claimed["run_id"], "123")
        self.assertEqual(claimed["last_started_at"], "2026-10-08T12:00:00Z")
        self.assertEqual(claimed["last_result"], FakeGitHub.state["last_result"])
        self.assertEqual(old_sha, "c" * 40)
        self.assertEqual(result["last_candidate_branch"], "rpg/previous")

    def test_force_cannot_bypass_corrupt_state(self):
        FakeGitHub.state = {"schema_version": 1, "last_started_at": "tomorrow"}
        self.args.force = True
        with self.assertRaises(automation.AutomationError):
            self.invoke()
        self.assertFalse(FakeGitHub.writes)


class APIBoundaryTests(IsolatedActionTestCase):
    def test_state_update_uses_compare_and_swap_file_sha(self):
        with mock.patch.dict(os.environ, {}, clear=True):
            client = automation.GitHub("owner/project", token="dummy-noncredential")
        with mock.patch.object(client, "request") as request:
            state = {"schema_version": 1, "last_started_at": "2026-10-08T12:00:00Z"}
            client.write_state("rpg-automation-state", state, "d" * 40)
        method, path, body = request.call_args.args
        self.assertEqual(method, "PUT")
        self.assertEqual(path, "/contents/.rpg-automation/state.json")
        self.assertEqual(body["sha"], "d" * 40)
        self.assertEqual(body["branch"], "rpg-automation-state")

    def test_cas_rejection_stops_without_retry_or_printing_response(self):
        client = automation.GitHub("owner/project", token="dummy-noncredential")
        opener = mock.Mock()
        opener.open.side_effect = urllib.error.HTTPError("unused", 409, "Conflict", {}, None)
        with mock.patch.object(automation.urllib.request, "build_opener", return_value=opener):
            with self.assertRaisesRegex(automation.AutomationError, "HTTP 409"):
                client.write_state("rpg-automation-state", {"schema_version": 1}, "d" * 40)
        self.assertEqual(opener.open.call_count, 1)

    def test_unexpected_api_host_rejected_before_token_transport(self):
        with mock.patch.dict(os.environ, {"GITHUB_API_URL": "https://attacker.invalid"}, clear=True):
            with self.assertRaises(automation.AutomationError):
                automation.GitHub("owner/project", token="dummy-noncredential")


class BundleTests(IsolatedActionTestCase):
    def setUp(self):
        IsolatedActionTestCase.setUp(self)
        self.temporary = tempfile.TemporaryDirectory(prefix="rpg-automation-test-")
        self.base = Path(self.temporary.name)
        self.root = self.base / "source"
        self.root.mkdir()
        command(self.root, "init", "-b", "master")
        command(self.root, "config", "user.name", "Test")
        command(self.root, "config", "user.email", "test@example.invalid")
        self.file = self.root / "Assets/Scripts/Player/PlayerMovement.cs"
        self.file.parent.mkdir(parents=True)
        self.file.write_text("class PlayerMovement { public int speed = 3; }\n")
        command(self.root, "add", ".")
        command(self.root, "commit", "-m", "baseline")
        self.sha = command(self.root, "rev-parse", "HEAD")
        self.bundle = self.base / "bundle"

    def tearDown(self):
        self.temporary.cleanup()

    def export(self):
        with contextlib.redirect_stdout(io.StringIO()):
            automation.export(argparse.Namespace(root=str(self.root), base_sha=self.sha,
                                                 output_dir=str(self.bundle)))
        return json.loads((self.bundle / "manifest.json").read_text())

    def clone(self, name="candidate"):
        target = self.base / name
        # Do not require a real LFS server (or a particular version's local-file
        # transfer adapter) for a temporary test repository. apply_bundle must
        # materialize and hash-check the candidate's actual object itself.
        env = dict(os.environ, GIT_LFS_SKIP_SMUDGE="1")
        command(self.base, "clone", "--no-local", str(self.root), str(target), env=env)
        return target

    def test_round_trip_captures_untracked_files_without_changing_source_index(self):
        self.file.write_text("class PlayerMovement { public int speed = 4; }\n")
        added = self.root / "tests/test_speed.py"
        added.parent.mkdir()
        added.write_text("assert 4 > 3\n")
        manifest = self.export()
        self.assertEqual(command(self.root, "diff", "--cached", "--name-only"), "")
        self.assertEqual(len(manifest["files"]), 2)
        target = self.clone()
        applied = automation.apply_bundle(target, self.bundle, self.sha)
        self.assertEqual(applied["risk"], "low")
        self.assertEqual((target / self.file.relative_to(self.root)).read_text(), self.file.read_text())
        self.assertTrue((target / "tests/test_speed.py").exists())

    def test_tampered_patch_and_wrong_manifest_paths_are_rejected(self):
        self.file.write_text("class PlayerMovement { public int speed = 4; }\n")
        manifest = self.export()
        patch = self.bundle / "candidate.patch"
        original = patch.read_bytes()
        patch.write_bytes(original + b"bad")
        with self.assertRaises(automation.AutomationError):
            automation.load_bundle(self.bundle)
        patch.write_bytes(original)
        manifest["files"][0]["path"] = "Assets/Different.cs"
        automation.write_json(self.bundle / "manifest.json", manifest)
        with self.assertRaises(automation.AutomationError):
            automation.load_bundle(self.bundle)

    def test_uncommitted_user_work_is_never_overwritten(self):
        self.file.write_text("class PlayerMovement { public int speed = 4; }\n")
        self.export()
        target = self.clone()
        target_file = target / self.file.relative_to(self.root)
        target_file.write_text("user changes\n")
        with self.assertRaises(automation.AutomationError):
            automation.apply_bundle(target, self.bundle, self.sha)
        self.assertEqual(target_file.read_text(), "user changes\n")

    def test_symbolic_link_cannot_be_exported(self):
        link = self.root / "Assets/secret-link"
        link.symlink_to("/tmp")
        with self.assertRaises(automation.AutomationError):
            self.export()

    def test_risk_is_recomputed_instead_of_trusting_manifest(self):
        self.file.write_text("class PlayerMovement { void Update() { Physics2D.Raycast(); } }\n")
        manifest = self.export()
        manifest.update(risk="low", risks=[], requires_manual_review=False)
        automation.write_json(self.bundle / "manifest.json", manifest)
        checked, _ = automation.load_bundle(self.bundle)
        self.assertEqual(checked["risk"], "high")

    def test_validation_report_cannot_seal_a_different_baseline(self):
        self.file.write_text("class PlayerMovement { public int speed = 4; }\n")
        self.export()
        report = self.base / "report.json"
        automation.write_json(report, {"base_sha": "f" * 40, "passed": True, "status": "passed",
                                       "checks": [{"name": "offline", "passed": True}]})
        args = argparse.Namespace(bundle_dir=str(self.bundle), project_report=str(report),
                                  output=str(self.base / "sealed.json"), generation_failed=False)
        with self.assertRaises(automation.AutomationError):
            automation.seal_validation(args)

    def test_failed_generation_cannot_be_sealed_as_passed(self):
        self.file.write_text("class PlayerMovement { public int speed = 4; }\n")
        self.export()
        report = self.base / "report.json"
        automation.write_json(report, {"base_sha": self.sha, "passed": True, "status": "passed",
                                       "native_unity_run": False,
                                       "checks": [{"name": "offline", "passed": True}]})
        sealed = self.base / "sealed.json"
        args = argparse.Namespace(bundle_dir=str(self.bundle), project_report=str(report),
                                  output=str(sealed), generation_failed=True)
        with contextlib.redirect_stdout(io.StringIO()):
            automation.seal_validation(args)
        self.assertFalse(json.loads(sealed.read_text())["passed"])

    def test_lfs_patch_transfers_exact_asset_and_rejects_tampering(self):
        probe = subprocess.run(["git", "lfs", "version"], stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        if probe.returncode:
            self.skipTest("Git LFS unavailable")
        # Match actions/checkout's mandatory global LFS filter, even on machines
        # where Git LFS is otherwise configured only for the source repository.
        configuration = self.base / "mandatory-lfs.gitconfig"
        configuration.write_text('[filter "lfs"]\n'
                                 '\tclean = git-lfs clean -- %f\n'
                                 '\tsmudge = git-lfs smudge -- %f\n'
                                 '\tprocess = git-lfs filter-process\n'
                                 '\trequired = true\n')
        patcher = mock.patch.dict(os.environ, {"GIT_CONFIG_GLOBAL": str(configuration),
                                              "GIT_CONFIG_SYSTEM": "/dev/null"})
        patcher.start()
        self.addCleanup(patcher.stop)
        command(self.root, "lfs", "install", "--local")
        (self.root / ".gitattributes").write_text("*.png filter=lfs diff=lfs merge=lfs -text\n")
        image = self.root / "Assets/Sprite.png"
        image.write_bytes(b"original-image\x00" * 100)
        command(self.root, "add", ".")
        command(self.root, "commit", "-m", "LFS baseline")
        self.sha = command(self.root, "rev-parse", "HEAD")
        image.write_bytes(b"changed-image\x01" * 101)
        manifest = self.export()
        self.assertEqual(len(manifest["lfs_objects"]), 1)
        item = manifest["lfs_objects"][0]
        target = self.clone()
        # A legitimate native Git LFS local adapter can create shared cache
        # inodes. Exercise that condition explicitly, without copying an
        # untrusted bundle file by hard link or weakening its input checks.
        oid = item["oid"]
        source_object = self.root / ".git/lfs/objects" / oid[:2] / oid[2:4] / oid
        target_object = target / ".git/lfs/objects" / oid[:2] / oid[2:4] / oid
        target_object.parent.mkdir(parents=True)
        os.link(source_object, target_object)
        self.assertGreater(target_object.stat().st_nlink, 1)
        automation.apply_bundle(target, self.bundle, self.sha)
        self.assertEqual((target / "Assets/Sprite.png").read_bytes(), image.read_bytes())
        pointer = command(target, "show", ":Assets/Sprite.png")
        self.assertIn("oid sha256:" + hashlib.sha256(image.read_bytes()).hexdigest(), pointer)
        self.assertEqual(hashlib.sha256(target_object.read_bytes()).hexdigest(), oid)
        (self.bundle / "lfs" / item["oid"]).write_bytes(b"corruption")
        with self.assertRaises(automation.AutomationError):
            automation.load_bundle(self.bundle)


class PublishingTests(IsolatedActionTestCase):
    # Reuse fixture construction without redundantly rerunning bundle checks.
    setUp = BundleTests.setUp
    tearDown = BundleTests.tearDown
    export = BundleTests.export
    def publish_candidate(self, *, high_risk=False, user_push=False, passed=True):
        self.file.write_text("class PlayerMovement { public int speed = 4; }\n")
        if high_risk:
            scene = self.root / "Assets/Scenes/New.unity"
            scene.parent.mkdir()
            scene.write_text("%YAML 1.1\n")
        manifest = self.export()
        remote = self.base / "remote.git"
        command(self.base, "clone", "--bare", str(self.root), str(remote))
        target = self.base / "publisher"
        command(self.base, "clone", str(remote), str(target))
        user_sha = self.sha
        if user_push:
            user = self.base / "user"
            command(self.base, "clone", str(remote), str(user))
            (user / "README.md").write_text("user contribution\n")
            command(user, "add", ".")
            command(user, "-c", "user.name=User", "-c", "user.email=user@example.invalid",
                    "commit", "-m", "user contribution")
            command(user, "push", "origin", "master")
            user_sha = command(user, "rev-parse", "HEAD")
        validation = self.base / "validation.json"
        automation.write_json(validation, {"schema_version": 1, "passed": passed,
                                           "base_sha": self.sha, "patch_sha256": manifest["patch_sha256"],
                                           "native_unity_tested": False})
        args = argparse.Namespace(root=str(target), bundle_dir=str(self.bundle),
                                  validation_file=str(validation), branch="master", repository=None,
                                  state_branch="rpg-automation-state", preserve_failed=not passed)
        output = io.StringIO()
        with mock.patch.dict(os.environ, {"GITHUB_RUN_ID": "456"}, clear=True), \
                contextlib.redirect_stdout(output):
            automation.publish(args)
        return json.loads(output.getvalue()), remote, user_sha

    def test_valid_low_risk_candidate_pushes_without_force(self):
        result, remote, _ = self.publish_candidate()
        self.assertTrue(result["published_to_master"])
        self.assertEqual(command(self.base, "--git-dir", str(remote), "rev-parse", "master"), result["commit_sha"])

    def test_user_push_is_preserved_and_candidate_goes_to_branch(self):
        result, remote, user_sha = self.publish_candidate(user_push=True)
        self.assertFalse(result["published_to_master"])
        self.assertEqual(command(self.base, "--git-dir", str(remote), "rev-parse", "master"), user_sha)
        self.assertEqual(command(self.base, "--git-dir", str(remote), "rev-parse", result["branch"]), result["commit_sha"])

    def test_unverified_visual_candidate_goes_to_branch(self):
        result, remote, _ = self.publish_candidate(high_risk=True)
        self.assertFalse(result["published_to_master"])
        self.assertEqual(command(self.base, "--git-dir", str(remote), "rev-parse", "master"), self.sha)

    def test_failed_validation_is_preserved_without_changing_master(self):
        result, remote, _ = self.publish_candidate(passed=False)
        self.assertFalse(result["published_to_master"])
        self.assertEqual(command(self.base, "--git-dir", str(remote), "rev-parse", "master"), self.sha)

    def test_covered_resource_fix_can_publish_with_actual_git(self):
        health = self.root / "Assets/Scripts/Player/PlayerHealth.cs"
        health.write_text("class PlayerHealth { public int maximumHealth = 3; }\n")
        command(self.root, "add", ".")
        command(self.root, "commit", "-m", "baseline resource module")
        self.sha = command(self.root, "rev-parse", "HEAD")
        health.write_text("class PlayerHealth { public int maximumHealth = 4; }\n")
        result, remote, _ = self.publish_candidate()
        self.assertTrue(result["published_to_master"])
        self.assertEqual(command(self.base, "--git-dir", str(remote), "show", "master:Assets/Scripts/Player/PlayerHealth.cs"),
                         "class PlayerHealth { public int maximumHealth = 4; }")


if __name__ == "__main__":
    unittest.main()
