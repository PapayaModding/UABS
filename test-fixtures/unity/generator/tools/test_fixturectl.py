import importlib.util
import json
import struct
import sys
import tempfile
import unittest
from pathlib import Path
from unittest import mock


MODULE_PATH = Path(__file__).with_name("fixturectl.py")
GENERATOR_SOURCE = MODULE_PATH.parents[1] / "Assets" / "UabsFixtureGenerator" / "Editor" / "FixtureGenerator.cs"
SPEC = importlib.util.spec_from_file_location("fixturectl", MODULE_PATH)
fixturectl = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
sys.modules[SPEC.name] = fixturectl
SPEC.loader.exec_module(fixturectl)


class FixtureCtlTests(unittest.TestCase):
    def test_configs_are_valid_and_bounded(self):
        for profile in ("smoke", "regression", "stress"):
            config = fixturectl.load_json(MODULE_PATH.parents[1] / "config" / f"{profile}.json")
            fixturectl.validate_generator_config(config)
            self.assertEqual(profile, config["profile"])

    def test_unity_targets_have_command_line_mappings_without_nographics(self):
        self.assertEqual({"Win64", "Linux64", "OSXUniversal"}, set(fixturectl.UNITY_BUILD_TARGET_ARGUMENTS.values()))
        command = fixturectl.unity_command(
            Path("C:/Unity/Unity.exe"), Path("smoke.json"), Path("output"), Path("unity.log"),
            "StandaloneWindows64")
        self.assertNotIn("-nographics", command)
        self.assertEqual("Win64", command[command.index("-buildTarget") + 1])

    def test_module_diagnostic_command_is_explicit_and_isolated(self):
        command = fixturectl.unity_command(
            Path("C:/Unity/Unity.exe"), Path("smoke.json"), Path("diagnostic"), Path("diagnostic.log"),
            "StandaloneWindows64", "UabsFixtures.Editor.FixtureGenerator.DiagnoseModuleStripping",
            ("-fixtureStripEngineCode", "false"))
        self.assertEqual(
            "UabsFixtures.Editor.FixtureGenerator.DiagnoseModuleStripping",
            command[command.index("-executeMethod") + 1])
        self.assertEqual("false", command[command.index("-fixtureStripEngineCode") + 1])
        self.assertNotIn("-nographics", command)

    def test_module_diagnostic_cli_requires_unity(self):
        args = fixturectl.parser().parse_args(["diagnose-modules", "--unity", "C:/Unity/Unity.exe"])
        self.assertIs(args.handler, fixturectl.diagnose_modules)

    def test_native_pipeline_diagnostic_cli_requires_unity(self):
        args = fixturectl.parser().parse_args(
            ["diagnose-native-pipeline", "--unity", "C:/Unity/Unity.exe"])
        self.assertIs(args.handler, fixturectl.diagnose_native_pipeline)

    def test_unityfs_header_parser_decodes_version_metadata(self):
        payload = (
            b"UnityFS\0" + struct.pack(">I", 6) + b"2021.3.45f2\0" + b"88f88f591b2e\0"
            + struct.pack(">QIII", 442, 71, 93, 0x243))
        payload += bytes(442 - len(payload))
        with tempfile.TemporaryDirectory() as temporary:
            bundle = Path(temporary) / "probe.bundle"
            bundle.write_bytes(payload)
            header = fixturectl.parse_unityfs_header(bundle)
        self.assertEqual("UnityFS", header["signature"])
        self.assertEqual(6, header["formatVersion"])
        self.assertEqual("2021.3.45f2", header["unityVersion"])
        self.assertEqual("88f88f591b2e", header["unityRevision"])
        self.assertEqual(442, header["declaredFileSize"])
        self.assertEqual(442, header["actualFileSize"])

    def test_native_probe_projects_differ_only_by_assetbundle_module(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            omitted = root / "omitted"
            explicit = root / "explicit"
            fixturectl.prepare_native_probe_project(omitted, False)
            fixturectl.prepare_native_probe_project(explicit, True)
            omitted_manifest = fixturectl.load_json(omitted / "Packages" / "manifest.json")
            explicit_manifest = fixturectl.load_json(explicit / "Packages" / "manifest.json")
        self.assertNotIn("com.unity.modules.assetbundle", omitted_manifest["dependencies"])
        self.assertEqual("1.0.0", explicit_manifest["dependencies"]["com.unity.modules.assetbundle"])
        explicit_manifest["dependencies"].pop("com.unity.modules.assetbundle")
        self.assertEqual(omitted_manifest, explicit_manifest)

    def test_generator_declares_required_native_modules(self):
        generator_root = MODULE_PATH.parents[1]
        manifest = fixturectl.load_json(generator_root / "Packages" / "manifest.json")
        lock = fixturectl.load_json(generator_root / "Packages" / "packages-lock.json")
        for package in ("com.unity.modules.assetbundle", "com.unity.modules.animation"):
            with self.subTest(package=package):
                self.assertEqual("1.0.0", manifest["dependencies"].get(package))
                self.assertEqual({
                    "version": "1.0.0",
                    "depth": 0,
                    "source": "builtin",
                    "dependencies": {},
                }, lock["dependencies"].get(package))

    def test_editor_generator_pins_standalone_player_subtarget(self):
        source = GENERATOR_SOURCE.read_text(encoding="utf-8")
        self.assertIn(
            "EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Player;", source)
        self.assertIn("ConfigureStandalonePlayerSubtarget();", source)

    def test_unsafe_allowlist_paths_are_rejected(self):
        for value in ("../escape.bundle", "/absolute.bundle", "bundles/../escape", "other/a.bundle", "bundles\\a.bundle"):
            with self.subTest(value=value), self.assertRaises(fixturectl.FixtureError):
                fixturectl.safe_relative_file(value)

    def test_remote_identity_is_exact(self):
        self.assertEqual("PapayaModding/UABS", fixturectl.canonical_remote("https://github.com/PapayaModding/UABS.git"))
        self.assertEqual("PapayaModding/UABS", fixturectl.canonical_remote("git@github.com:PapayaModding/UABS.git"))
        with self.assertRaises(fixturectl.FixtureError):
            fixturectl.canonical_remote("https://example.com/PapayaModding/UABS.git")

    def test_manifest_hash_validation_and_selection(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            bundles = root / "bundles"
            bundles.mkdir()
            payload = b"UnityFS-original-test-bytes"
            bundle = bundles / "one.bundle"
            bundle.write_bytes(payload)
            manifest = {
                "schemaVersion": 1,
                "generatorVersion": "test",
                "unityVersion": "2021.3.45f1",
                "buildTarget": "StandaloneWindows64",
                "profile": "smoke",
                "seed": 7,
                "configuration": {},
                "bundles": [{
                    "name": "one.bundle",
                    "file": "bundles/one.bundle",
                    "sizeBytes": len(payload),
                    "sha256": fixturectl.sha256_bytes(payload),
                    "sourceAssignments": [],
                    "unityDependencies": [],
                    "unityReportedContents": []
                }],
                "knownCharacteristics": []
            }
            manifest_path = root / "fixture-manifest.json"
            manifest_path.write_text(json.dumps(manifest), encoding="utf-8")
            verified = fixturectl.verify_manifest_files(manifest_path)
            self.assertEqual("bundles/one.bundle", verified[0]["relative"])
            bundle.write_bytes(payload + b"changed")
            with self.assertRaises(fixturectl.FixtureError):
                fixturectl.verify_manifest_files(manifest_path)

    def test_manifest_rejects_unknown_bundle_dependency(self):
        manifest = {
            "schemaVersion": 1,
            "generatorVersion": "test",
            "unityVersion": "2021.3.45f1",
            "buildTarget": "StandaloneWindows64",
            "profile": "smoke",
            "seed": 7,
            "configuration": {},
            "bundles": [{
                "name": "one.bundle",
                "file": "bundles/one.bundle",
                "sizeBytes": 1,
                "sha256": "0" * 64,
                "sourceAssignments": [],
                "unityDependencies": ["missing.bundle"],
                "unityReportedContents": []
            }],
            "knownCharacteristics": []
        }
        with self.assertRaisesRegex(fixturectl.FixtureError, "unknown dependencies"):
            fixturectl.validate_manifest_shape(manifest)

    def test_symlinked_bundle_is_rejected(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            bundles = root / "bundles"
            bundles.mkdir()
            target = root / "target.bundle"
            target.write_bytes(b"payload")
            link = bundles / "linked.bundle"
            try:
                link.symlink_to(target)
            except OSError:
                self.skipTest("symlinks are unavailable")
            manifest = {
                "schemaVersion": 1,
                "generatorVersion": "test",
                "unityVersion": "2021.3.45f1",
                "buildTarget": "StandaloneWindows64",
                "profile": "smoke",
                "seed": 7,
                "configuration": {},
                "bundles": [{
                    "name": "linked.bundle",
                    "file": "bundles/linked.bundle",
                    "sizeBytes": 7,
                    "sha256": fixturectl.sha256_bytes(b"payload"),
                    "sourceAssignments": [],
                    "unityDependencies": [],
                    "unityReportedContents": []
                }],
                "knownCharacteristics": []
            }
            manifest_path = root / "fixture-manifest.json"
            manifest_path.write_text(json.dumps(manifest), encoding="utf-8")
            with self.assertRaises(fixturectl.FixtureError):
                fixturectl.verify_manifest_files(manifest_path)

    def test_conservative_default_limits(self):
        limits = fixturectl.read_limits({})
        self.assertEqual(30, limits.max_bundles)
        self.assertEqual(20 * 1024 * 1024, limits.max_individual_bytes)
        self.assertEqual(200 * 1024 * 1024, limits.max_total_bytes)

    def test_git_preflight_requires_exact_clean_synchronized_recovery_branch(self):
        repo = Path("/example/UABS")
        head = "a" * 40
        answers = {
            ("rev-parse", "--show-toplevel"): str(repo),
            ("branch", "--show-current"): "v2-recovery",
            ("remote", "get-url", "origin"): "https://github.com/PapayaModding/UABS.git",
            ("status", "--porcelain=v1", "--untracked-files=all"): "",
            ("rev-parse", "HEAD"): head,
            ("ls-remote", "origin", "refs/heads/v2-recovery"): f"{head}\trefs/heads/v2-recovery",
        }
        with mock.patch.object(fixturectl, "run_git", side_effect=lambda unused_repo, *args: answers[args]):
            result = fixturectl.verify_git_preflight(repo)
        self.assertEqual(head, result["head"])
        self.assertEqual("v2-recovery", result["branch"])

    def test_git_preflight_rejects_dirty_or_wrong_branch(self):
        repo = Path("/example/UABS")

        def wrong_branch(unused_repo, *args):
            return str(repo) if args == ("rev-parse", "--show-toplevel") else "main"

        with mock.patch.object(fixturectl, "run_git", side_effect=wrong_branch):
            with self.assertRaisesRegex(fixturectl.FixtureError, "requires branch"):
                fixturectl.verify_git_preflight(repo)

        answers = {
            ("rev-parse", "--show-toplevel"): str(repo),
            ("branch", "--show-current"): "v2-recovery",
            ("remote", "get-url", "origin"): "git@github.com:PapayaModding/UABS.git",
            ("status", "--porcelain=v1", "--untracked-files=all"): "?? unrelated.txt",
        }
        with mock.patch.object(fixturectl, "run_git", side_effect=lambda unused_repo, *args: answers[args]):
            with self.assertRaisesRegex(fixturectl.FixtureError, "completely clean"):
                fixturectl.verify_git_preflight(repo)

    def test_publication_plan_contains_only_allowlisted_verified_files(self):
        with tempfile.TemporaryDirectory() as temporary:
            repo = Path(temporary)
            fixture_root = repo / "test-fixtures" / "unity"
            generator_root = fixture_root / "generator"
            generated_root = generator_root / "Generated" / "working"
            source_root = generated_root / "regression"
            (source_root / "bundles").mkdir(parents=True)
            (generator_root / "config").mkdir(parents=True)
            payload = b"approved-bundle"
            bundle_path = source_root / "bundles" / "one.bundle"
            bundle_path.write_bytes(payload)
            manifest = {
                "schemaVersion": 1,
                "generatorVersion": "test",
                "unityVersion": "2021.3.45f1",
                "buildTarget": "StandaloneWindows64",
                "profile": "regression",
                "seed": 7,
                "configuration": {},
                "bundles": [{
                    "name": "one.bundle",
                    "file": "bundles/one.bundle",
                    "sizeBytes": len(payload),
                    "sha256": fixturectl.sha256_bytes(payload),
                    "sourceAssignments": [],
                    "unityDependencies": [],
                    "unityReportedContents": []
                }],
                "knownCharacteristics": []
            }
            manifest_path = source_root / "fixture-manifest.json"
            manifest_path.write_text(json.dumps(manifest), encoding="utf-8")
            allowlist_path = generator_root / "config" / "publish.json"
            allowlist_path.write_text(json.dumps({
                "schemaVersion": 1,
                "collection": "approved",
                "manifest": "fixture-manifest.json",
                "files": ["bundles/one.bundle"],
                "allowStress": False
            }), encoding="utf-8")
            index_path = fixture_root / "manifest.json"
            index_path.write_text(json.dumps({"schemaVersion": 1, "collections": []}), encoding="utf-8")
            with mock.patch.multiple(
                    fixturectl,
                    REPO_ROOT=repo,
                    UNITY_FIXTURE_ROOT=fixture_root,
                    GENERATOR_ROOT=generator_root,
                    GENERATED_ROOT=generated_root,
                    PUBLISHED_ROOT=fixture_root / "published",
                    INDEX_PATH=index_path), mock.patch.object(
                        fixturectl, "verify_git_preflight", return_value={"head": "a" * 40}):
                plan = fixturectl.build_plan(manifest_path, allowlist_path, False)
            self.assertEqual("approved", plan["collection"])
            self.assertEqual(len(payload), plan["totalBytes"])
            self.assertEqual(["bundles/one.bundle"], [item["relative"] for item in plan["files"]])
            self.assertEqual({"A", "M"}, {change["status"] for change in plan["changes"]})


if __name__ == "__main__":
    unittest.main()
