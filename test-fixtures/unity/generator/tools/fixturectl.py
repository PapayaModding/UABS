#!/usr/bin/env python3
"""Generate, verify, and explicitly publish UABS Unity fixture collections.

The publishing path is deliberately two-phase. ``publish --dry-run`` writes an
immutable plan. ``publish --plan ...`` revalidates that plan and requires an
interactive, plan-specific confirmation before touching the Git index.
"""

from __future__ import annotations

import argparse
import datetime as dt
import hashlib
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
from dataclasses import dataclass
from pathlib import Path, PurePosixPath
from typing import Any, Iterable


GENERATOR_VERSION = "1.0.0"
EXPECTED_OWNER_REPO = "PapayaModding/UABS"
EXPECTED_BRANCH = "v2-recovery"
DEFAULT_MAX_BUNDLES = 30
DEFAULT_MAX_INDIVIDUAL_BYTES = 20 * 1024 * 1024
DEFAULT_MAX_TOTAL_BYTES = 200 * 1024 * 1024
UNITY_BUILD_TARGET_ARGUMENTS = {
    "StandaloneWindows64": "Win64",
    "StandaloneLinux64": "Linux64",
    "StandaloneOSX": "OSXUniversal",
}
COLLECTION_RE = re.compile(r"^[a-z0-9][a-z0-9._-]{0,63}$")
SHA256_RE = re.compile(r"^[0-9a-f]{64}$")

SCRIPT_PATH = Path(__file__).resolve()
GENERATOR_ROOT = SCRIPT_PATH.parents[1]
UNITY_FIXTURE_ROOT = SCRIPT_PATH.parents[2]
REPO_ROOT = SCRIPT_PATH.parents[4]
GENERATED_ROOT = GENERATOR_ROOT / "Generated" / "working"
PUBLISHED_ROOT = UNITY_FIXTURE_ROOT / "published"
INDEX_PATH = UNITY_FIXTURE_ROOT / "manifest.json"
NATIVE_PROBE_SOURCE = SCRIPT_PATH.with_name("NativeBundleProbe.cs")


class FixtureError(RuntimeError):
    pass


@dataclass(frozen=True)
class Limits:
    max_bundles: int = DEFAULT_MAX_BUNDLES
    max_individual_bytes: int = DEFAULT_MAX_INDIVIDUAL_BYTES
    max_total_bytes: int = DEFAULT_MAX_TOTAL_BYTES


def load_json(path: Path) -> dict[str, Any]:
    try:
        value = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise FixtureError(f"Cannot read JSON '{path}': {exc}") from exc
    if not isinstance(value, dict):
        raise FixtureError(f"Expected a JSON object in '{path}'.")
    return value


def canonical_json(value: Any) -> bytes:
    return (json.dumps(value, ensure_ascii=False, sort_keys=True, indent=2) + "\n").encode("utf-8")


def sha256_bytes(value: bytes) -> str:
    return hashlib.sha256(value).hexdigest()


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def parse_unityfs_header(path: Path) -> dict[str, Any]:
    """Decode the uncompressed prefix shared by UnityFS bundle format 6."""
    data = path.read_bytes()
    offset = 0

    def cstring(label: str) -> str:
        nonlocal offset
        end = data.find(b"\0", offset)
        if end < 0:
            raise FixtureError(f"UnityFS {label} is not null terminated in '{path}'")
        try:
            value = data[offset:end].decode("utf-8")
        except UnicodeDecodeError as exc:
            raise FixtureError(f"UnityFS {label} is not UTF-8 in '{path}'") from exc
        offset = end + 1
        return value

    def unsigned(length: int, label: str) -> int:
        nonlocal offset
        end = offset + length
        if end > len(data):
            raise FixtureError(f"UnityFS header ends before {label} in '{path}'")
        value = int.from_bytes(data[offset:end], byteorder="big", signed=False)
        offset = end
        return value

    signature = cstring("signature")
    if signature != "UnityFS":
        raise FixtureError(f"Expected UnityFS signature in '{path}', got {signature!r}")
    return {
        "signature": signature,
        "formatVersion": unsigned(4, "format version"),
        "unityVersion": cstring("Unity version"),
        "unityRevision": cstring("Unity revision"),
        "declaredFileSize": unsigned(8, "declared file size"),
        "compressedBlocksInfoSize": unsigned(4, "compressed blocks-info size"),
        "uncompressedBlocksInfoSize": unsigned(4, "uncompressed blocks-info size"),
        "flags": unsigned(4, "flags"),
        "actualFileSize": len(data),
        "sha256": sha256_bytes(data),
    }


def require_contained(path: Path, root: Path, label: str) -> Path:
    resolved = path.resolve(strict=False)
    resolved_root = root.resolve(strict=False)
    try:
        resolved.relative_to(resolved_root)
    except ValueError as exc:
        raise FixtureError(f"{label} must stay inside '{resolved_root}', got '{resolved}'.") from exc
    return resolved


def reject_symlink_chain(path: Path, root: Path) -> None:
    root = root.resolve(strict=True)
    path = path.absolute()
    try:
        relative = path.relative_to(root)
    except ValueError as exc:
        raise FixtureError(f"Path escapes fixture root: {path}") from exc
    cursor = root
    for part in relative.parts:
        cursor /= part
        if cursor.is_symlink():
            raise FixtureError(f"Symlinks are not publishable: {cursor}")


def safe_relative_file(value: str) -> PurePosixPath:
    if not isinstance(value, str) or not value:
        raise FixtureError("Allowlisted fixture paths must be non-empty strings.")
    path = PurePosixPath(value)
    if path.is_absolute() or ".." in path.parts or "." in path.parts:
        raise FixtureError(f"Unsafe fixture path: {value!r}")
    if not path.parts or path.parts[0] != "bundles":
        raise FixtureError(f"Only bundle paths below 'bundles/' may be allowlisted: {value!r}")
    if "\\" in value or value.endswith("/"):
        raise FixtureError(f"Fixture paths must use normalized POSIX file names: {value!r}")
    return path


def validate_generator_config(config: dict[str, Any]) -> None:
    profile = config.get("profile")
    if profile not in {"smoke", "regression", "stress"}:
        raise FixtureError("profile must be smoke, regression, or stress")
    for name, minimum, maximum in (("bundleCount", 1, 5000), ("assetsPerBundle", 1, 10000)):
        value = config.get(name)
        if not isinstance(value, int) or not minimum <= value <= maximum:
            raise FixtureError(f"{name} must be an integer in [{minimum}, {maximum}]")
    if config.get("compression") not in {"lz4", "lzma", "uncompressed"}:
        raise FixtureError("compression must be lz4, lzma, or uncompressed")
    if config.get("buildTarget") not in UNITY_BUILD_TARGET_ARGUMENTS:
        raise FixtureError("buildTarget must be StandaloneWindows64, StandaloneLinux64, or StandaloneOSX")
    for name in ("dependencyDensity", "duplicateNameFrequency"):
        value = config.get(name)
        if not isinstance(value, (int, float)) or not 0 <= value <= 1:
            raise FixtureError(f"{name} must be between 0 and 1")
    if not isinstance(config.get("seed"), int):
        raise FixtureError("seed must be an integer")
    output = config.get("outputDirectory")
    if not isinstance(output, str) or not output.startswith("Generated/working/"):
        raise FixtureError("outputDirectory must be below Generated/working/")
    allowed_types = {
        "Texture2D", "Sprite", "Material", "Prefab", "TextAsset",
        "ScriptableObject", "AnimationClip", "SpriteAtlas",
    }
    asset_types = config.get("assetTypes")
    if not isinstance(asset_types, list) or not asset_types or not set(asset_types) <= allowed_types:
        raise FixtureError("assetTypes contains an unsupported or empty selection")


def validate_manifest_shape(manifest: dict[str, Any]) -> None:
    required = {
        "schemaVersion", "generatorVersion", "unityVersion", "buildTarget",
        "profile", "seed", "configuration", "bundles", "knownCharacteristics",
    }
    missing = sorted(required - manifest.keys())
    if missing:
        raise FixtureError(f"Manifest is missing required fields: {', '.join(missing)}")
    if manifest["schemaVersion"] != 1:
        raise FixtureError("Unsupported fixture manifest schemaVersion")
    if manifest["profile"] not in {"smoke", "regression", "stress"}:
        raise FixtureError("Manifest has an invalid profile")
    if not isinstance(manifest["bundles"], list):
        raise FixtureError("Manifest bundles must be an array")
    seen_names: set[str] = set()
    seen_files: set[str] = set()
    seen_assignments: set[str] = set()
    for bundle in manifest["bundles"]:
        if not isinstance(bundle, dict):
            raise FixtureError("Every manifest bundle must be an object")
        for name in ("name", "file", "sizeBytes", "sha256", "sourceAssignments", "unityDependencies", "unityReportedContents"):
            if name not in bundle:
                raise FixtureError(f"Manifest bundle is missing {name}")
        file_path = str(safe_relative_file(bundle["file"]))
        if bundle["name"] in seen_names or file_path in seen_files:
            raise FixtureError(f"Duplicate bundle identity in manifest: {bundle['name']} / {file_path}")
        seen_names.add(bundle["name"])
        seen_files.add(file_path)
        if bundle["name"] != PurePosixPath(file_path).name:
            raise FixtureError(f"Bundle name and file disagree: {bundle['name']} / {file_path}")
        if not isinstance(bundle["sizeBytes"], int) or bundle["sizeBytes"] <= 0:
            raise FixtureError(f"Invalid bundle size for {file_path}")
        if not isinstance(bundle["sha256"], str) or not SHA256_RE.fullmatch(bundle["sha256"]):
            raise FixtureError(f"Invalid SHA-256 for {file_path}")
        if (not isinstance(bundle["sourceAssignments"], list)
                or not isinstance(bundle["unityDependencies"], list)
                or not isinstance(bundle["unityReportedContents"], list)):
            raise FixtureError(f"Invalid assignment/dependency arrays for {file_path}")
        for assignment in bundle["sourceAssignments"]:
            if not isinstance(assignment, dict) or not all(
                    isinstance(assignment.get(field), str) and assignment[field]
                    for field in ("assetPath", "assetName", "assetType")):
                raise FixtureError(f"Invalid source assignment for {file_path}")
            asset_path = assignment["assetPath"]
            if asset_path in seen_assignments:
                raise FixtureError(f"Source asset is assigned more than once: {asset_path}")
            seen_assignments.add(asset_path)
        if not all(isinstance(value, str) and value for value in bundle["unityDependencies"]):
            raise FixtureError(f"Invalid Unity dependency name for {file_path}")
        if not all(isinstance(value, str) and value for value in bundle["unityReportedContents"]):
            raise FixtureError(f"Invalid Unity-reported content for {file_path}")
    for bundle in manifest["bundles"]:
        dependencies = bundle["unityDependencies"]
        if bundle["name"] in dependencies:
            raise FixtureError(f"Bundle cannot depend on itself: {bundle['name']}")
        unknown = sorted(set(dependencies) - seen_names)
        if unknown:
            raise FixtureError(f"Bundle {bundle['name']} has unknown dependencies: {', '.join(unknown)}")


def verify_manifest_files(manifest_path: Path, selected: Iterable[str] | None = None) -> list[dict[str, Any]]:
    manifest_path = manifest_path.absolute()
    reject_symlink_chain(manifest_path, manifest_path.parent)
    if not manifest_path.is_file():
        raise FixtureError(f"Manifest is missing: {manifest_path}")
    manifest_path = manifest_path.resolve(strict=True)
    manifest = load_json(manifest_path)
    validate_manifest_shape(manifest)
    wanted = set(selected) if selected is not None else {bundle["file"] for bundle in manifest["bundles"]}
    bundles_by_file = {bundle["file"]: bundle for bundle in manifest["bundles"]}
    unknown = sorted(wanted - bundles_by_file.keys())
    if unknown:
        raise FixtureError(f"Allowlist references files absent from manifest: {', '.join(unknown)}")
    verified: list[dict[str, Any]] = []
    for relative in sorted(wanted):
        safe = safe_relative_file(relative)
        source_candidate = manifest_path.parent / Path(*safe.parts)
        reject_symlink_chain(source_candidate, manifest_path.parent)
        source = require_contained(source_candidate, manifest_path.parent, "bundle")
        if not source.is_file():
            raise FixtureError(f"Manifest bundle is missing: {source}")
        actual_size = source.stat().st_size
        actual_hash = sha256_file(source)
        expected = bundles_by_file[relative]
        if actual_size != expected["sizeBytes"] or actual_hash != expected["sha256"]:
            raise FixtureError(
                f"Bundle verification failed for {relative}: expected "
                f"{expected['sizeBytes']} bytes/{expected['sha256']}, got {actual_size} bytes/{actual_hash}"
            )
        verified.append({"relative": relative, "source": str(source), "sizeBytes": actual_size, "sha256": actual_hash})
    return verified


def run_git(repo: Path, *arguments: str, check: bool = True) -> str:
    result = subprocess.run(
        ["git", "-C", str(repo), *arguments],
        check=False,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
        encoding="utf-8",
    )
    if check and result.returncode != 0:
        raise FixtureError(f"git {' '.join(arguments)} failed: {result.stderr.strip()}")
    return result.stdout.strip()


def run_git_bytes(repo: Path, *arguments: str) -> bytes:
    result = subprocess.run(["git", "-C", str(repo), *arguments], check=False, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    if result.returncode != 0:
        raise FixtureError(f"git {' '.join(arguments)} failed: {result.stderr.decode(errors='replace').strip()}")
    return result.stdout


def canonical_remote(value: str) -> str:
    value = value.strip()
    match = re.fullmatch(r"(?:https://github\.com/|git@github\.com:)([^/]+/[^/]+?)(?:\.git)?", value, re.I)
    if not match:
        raise FixtureError(f"origin is not a supported GitHub URL: {value!r}")
    return match.group(1)


def verify_git_preflight(repo: Path) -> dict[str, str]:
    if run_git(repo, "rev-parse", "--show-toplevel") != str(repo.resolve()):
        raise FixtureError("Publishing must run from the UABS repository root")
    branch = run_git(repo, "branch", "--show-current")
    if branch != EXPECTED_BRANCH:
        raise FixtureError(f"Publishing requires branch {EXPECTED_BRANCH!r}; current branch is {branch!r}")
    remote = run_git(repo, "remote", "get-url", "origin")
    if canonical_remote(remote).casefold() != EXPECTED_OWNER_REPO.casefold():
        raise FixtureError(f"origin must be {EXPECTED_OWNER_REPO}, got {remote!r}")
    status = run_git(repo, "status", "--porcelain=v1", "--untracked-files=all")
    if status:
        raise FixtureError("Publishing requires a completely clean checkout; preserve or remove unrelated changes first:\n" + status)
    head = run_git(repo, "rev-parse", "HEAD")
    remote_line = run_git(repo, "ls-remote", "origin", f"refs/heads/{EXPECTED_BRANCH}")
    fields = remote_line.split()
    if len(fields) != 2 or fields[1] != f"refs/heads/{EXPECTED_BRANCH}":
        raise FixtureError(f"Cannot resolve origin/{EXPECTED_BRANCH}")
    remote_head = fields[0]
    if head != remote_head:
        raise FixtureError(f"Local {EXPECTED_BRANCH} is not synchronized: local {head}, remote {remote_head}")
    return {"branch": branch, "head": head, "remote": remote, "remoteHead": remote_head}


def read_limits(allowlist: dict[str, Any]) -> Limits:
    values = allowlist.get("limits", {})
    if not isinstance(values, dict):
        raise FixtureError("allowlist limits must be an object")
    limits = Limits(
        int(values.get("maxBundles", DEFAULT_MAX_BUNDLES)),
        int(values.get("maxIndividualBytes", DEFAULT_MAX_INDIVIDUAL_BYTES)),
        int(values.get("maxTotalBytes", DEFAULT_MAX_TOTAL_BYTES)),
    )
    if limits.max_bundles < 1 or limits.max_individual_bytes < 1 or limits.max_total_bytes < 1:
        raise FixtureError("All publication limits must be positive")
    return limits


def proposed_index(manifest: dict[str, Any], collection: str, published_manifest: bytes, files: list[dict[str, Any]]) -> dict[str, Any]:
    current = load_json(INDEX_PATH)
    collections = current.get("collections")
    if current.get("schemaVersion") != 1 or not isinstance(collections, list):
        raise FixtureError(f"Invalid fixture collection index: {INDEX_PATH}")
    entry = {
        "name": collection,
        "manifest": f"published/{collection}/fixture-manifest.json",
        "manifestSha256": sha256_bytes(published_manifest),
        "bundleCount": len(files),
        "totalBytes": sum(item["sizeBytes"] for item in files),
        "profile": manifest["profile"],
        "unityVersion": manifest["unityVersion"],
        "buildTarget": manifest["buildTarget"],
    }
    retained = [item for item in collections if isinstance(item, dict) and item.get("name") != collection]
    retained.append(entry)
    retained.sort(key=lambda item: item["name"])
    return {"schemaVersion": 1, "description": current.get("description", "Approved Unity fixture collections."), "collections": retained}


def ensure_destination_safe(destination: Path, allowed_names: set[str]) -> None:
    require_contained(destination, PUBLISHED_ROOT.parent, "publication destination")
    if destination.is_symlink():
        raise FixtureError(f"Publication destination cannot be a symlink: {destination}")
    if not destination.exists():
        return
    for path in destination.rglob("*"):
        if path.is_symlink():
            raise FixtureError(f"Publication destination contains a symlink: {path}")
        if path.is_file():
            relative = path.relative_to(destination).as_posix()
            if relative not in allowed_names:
                raise FixtureError(f"Publication destination contains unexpected file: {relative}")


def build_plan(manifest_path: Path, allowlist_path: Path, approve_stress: bool) -> dict[str, Any]:
    git = verify_git_preflight(REPO_ROOT)
    manifest_candidate = manifest_path.absolute()
    allowlist_candidate = allowlist_path.absolute()
    reject_symlink_chain(manifest_candidate, GENERATED_ROOT)
    manifest_path = require_contained(manifest_candidate, GENERATED_ROOT, "generated manifest")
    allowlist_roots = (GENERATOR_ROOT / "config", GENERATOR_ROOT / "Generated" / "publication-allowlists")
    allowlist_root: Path | None = None
    for candidate_root in allowlist_roots:
        try:
            allowlist_candidate.resolve(strict=False).relative_to(candidate_root.resolve(strict=False))
            allowlist_root = candidate_root
            break
        except ValueError:
            continue
    if allowlist_root is None:
        raise FixtureError("publish allowlist must be in config/ or Generated/publication-allowlists/")
    reject_symlink_chain(allowlist_candidate, allowlist_root)
    allowlist_path = require_contained(allowlist_candidate, allowlist_root, "publish allowlist")
    manifest = load_json(manifest_path)
    validate_manifest_shape(manifest)
    allowlist = load_json(allowlist_path)
    if allowlist.get("schemaVersion") != 1:
        raise FixtureError("Unsupported publish allowlist schemaVersion")
    collection = allowlist.get("collection")
    if not isinstance(collection, str) or not COLLECTION_RE.fullmatch(collection):
        raise FixtureError("allowlist collection must be a conservative lowercase identifier")
    if allowlist.get("manifest") != manifest_path.name:
        raise FixtureError("allowlist manifest does not match the selected manifest")
    raw_files = allowlist.get("files")
    if not isinstance(raw_files, list) or not raw_files or len(raw_files) != len(set(raw_files)):
        raise FixtureError("allowlist files must be a non-empty list without duplicates")
    selected = [str(safe_relative_file(value)) for value in raw_files]
    if manifest["profile"] == "stress" and not (allowlist.get("allowStress") is True and approve_stress):
        raise FixtureError("Stress output is not publishable without allowStress=true and --approve-stress")
    files = verify_manifest_files(manifest_path, selected)
    limits = read_limits(allowlist)
    total = sum(item["sizeBytes"] for item in files)
    if len(files) > limits.max_bundles:
        raise FixtureError(f"Bundle count {len(files)} exceeds limit {limits.max_bundles}")
    oversized = [item for item in files if item["sizeBytes"] > limits.max_individual_bytes]
    if oversized:
        raise FixtureError(f"Bundle exceeds individual size limit: {oversized[0]['relative']}")
    if total > limits.max_total_bytes:
        raise FixtureError(f"Total size {total} exceeds limit {limits.max_total_bytes}")

    selected_set = set(selected)
    selected_bundles = [bundle for bundle in manifest["bundles"] if bundle["file"] in selected_set]
    selected_names = {bundle["name"] for bundle in selected_bundles}
    for bundle in selected_bundles:
        omitted_dependencies = sorted(set(bundle["unityDependencies"]) - selected_names)
        if omitted_dependencies:
            raise FixtureError(
                f"Allowlist omits dependencies required by {bundle['name']}: {', '.join(omitted_dependencies)}"
            )
    published_manifest_object = dict(manifest)
    published_manifest_object["bundles"] = selected_bundles
    published_manifest_object["scopeNotice"] = (
        "sourceAssignments list generator-known inputs only; Unity may add internal serialized objects. "
        "UABS-extracted records require separate validation."
    )
    published_manifest = canonical_json(published_manifest_object)
    destination = PUBLISHED_ROOT / collection
    allowed_destination_files = {item["relative"] for item in files} | {"fixture-manifest.json"}
    ensure_destination_safe(destination, allowed_destination_files)

    changes: list[dict[str, str]] = []
    for item in files:
        target = destination / Path(*PurePosixPath(item["relative"]).parts)
        status = "A" if not target.exists() else ("M" if sha256_file(target) != item["sha256"] else "=")
        changes.append({"status": status, "path": target.relative_to(REPO_ROOT).as_posix()})
    target_manifest = destination / "fixture-manifest.json"
    manifest_status = "A" if not target_manifest.exists() else (
        "M" if sha256_file(target_manifest) != sha256_bytes(published_manifest) else "="
    )
    changes.append({"status": manifest_status, "path": target_manifest.relative_to(REPO_ROOT).as_posix()})
    index = proposed_index(manifest, collection, published_manifest, files)
    index_bytes = canonical_json(index)
    changes.append({"status": "M" if INDEX_PATH.read_bytes() != index_bytes else "=", "path": INDEX_PATH.relative_to(REPO_ROOT).as_posix()})

    payload: dict[str, Any] = {
        "schemaVersion": 1,
        "repository": EXPECTED_OWNER_REPO,
        "branch": EXPECTED_BRANCH,
        "head": git["head"],
        "collection": collection,
        "profile": manifest["profile"],
        "manifestSource": str(manifest_path),
        "allowlistSource": str(allowlist_path),
        "destination": destination.relative_to(REPO_ROOT).as_posix(),
        "limits": {
            "maxBundles": limits.max_bundles,
            "maxIndividualBytes": limits.max_individual_bytes,
            "maxTotalBytes": limits.max_total_bytes,
        },
        "totalBytes": total,
        "files": files,
        "publishedManifest": published_manifest_object,
        "index": index,
        "changes": changes,
        "approveStress": approve_stress,
    }
    payload["planId"] = sha256_bytes(canonical_json(payload))[:16]
    return payload


def print_plan(plan: dict[str, Any]) -> None:
    print(f"Publication plan {plan['planId']}")
    print(f"  repository: {plan['repository']}")
    print(f"  branch/head: {plan['branch']} @ {plan['head']}")
    print(f"  collection: {plan['collection']} ({plan['profile']})")
    print(f"  destination: {plan['destination']}")
    print(f"  bundles: {len(plan['files'])}; total: {plan['totalBytes']} bytes")
    print("  files:")
    for item in plan["files"]:
        destination = f"{plan['destination']}/{item['relative']}"
        print(f"    {item['relative']} -> {destination} | {item['sizeBytes']} bytes | {item['sha256']}")
    print("  proposed Git changes:")
    for change in plan["changes"]:
        print(f"    {change['status']} {change['path']}")


def dry_run(args: argparse.Namespace) -> int:
    plan = build_plan(Path(args.manifest), Path(args.allowlist), args.approve_stress)
    print_plan(plan)
    plan_dir = GENERATOR_ROOT / "Generated" / "publication-plans"
    plan_dir.mkdir(parents=True, exist_ok=True)
    plan_path = Path(args.plan_output) if args.plan_output else plan_dir / f"{plan['planId']}.json"
    plan_candidate = plan_path.absolute()
    reject_symlink_chain(plan_candidate, GENERATOR_ROOT / "Generated")
    plan_path = require_contained(plan_candidate, GENERATOR_ROOT / "Generated", "publication plan")
    plan_with_time = dict(plan)
    plan_with_time["createdUtc"] = dt.datetime.now(dt.timezone.utc).isoformat()
    plan_path.write_bytes(canonical_json(plan_with_time))
    print(f"Dry run only; Git was not modified. Plan written to: {plan_path}")
    return 0


def copy_publication(plan: dict[str, Any]) -> list[str]:
    destination = REPO_ROOT / plan["destination"]
    destination.mkdir(parents=True, exist_ok=True)
    staged: list[str] = []
    for item in plan["files"]:
        source = Path(item["source"])
        target = destination / Path(*PurePosixPath(item["relative"]).parts)
        target.parent.mkdir(parents=True, exist_ok=True)
        with source.open("rb") as input_stream, tempfile.NamedTemporaryFile(dir=target.parent, delete=False) as output_stream:
            shutil.copyfileobj(input_stream, output_stream)
            temporary = Path(output_stream.name)
        os.replace(temporary, target)
        staged.append(target.relative_to(REPO_ROOT).as_posix())
    manifest_target = destination / "fixture-manifest.json"
    manifest_target.write_bytes(canonical_json(plan["publishedManifest"]))
    staged.append(manifest_target.relative_to(REPO_ROOT).as_posix())
    INDEX_PATH.write_bytes(canonical_json(plan["index"]))
    staged.append(INDEX_PATH.relative_to(REPO_ROOT).as_posix())
    return sorted(staged)


def publish(args: argparse.Namespace) -> int:
    plan_candidate = Path(args.plan).absolute()
    reject_symlink_chain(plan_candidate, GENERATOR_ROOT / "Generated")
    plan_file = require_contained(plan_candidate, GENERATOR_ROOT / "Generated", "publication plan")
    stored = load_json(plan_file)
    stored.pop("createdUtc", None)
    if not isinstance(stored.get("manifestSource"), str) or not isinstance(stored.get("allowlistSource"), str):
        raise FixtureError("Publication plan is missing its recorded source paths")
    rebuilt = build_plan(Path(stored["manifestSource"]), Path(stored["allowlistSource"]), bool(stored.get("approveStress")))
    if canonical_json(stored) != canonical_json(rebuilt):
        raise FixtureError("Publication plan is stale or its inputs changed; run publish --dry-run again")
    print_plan(rebuilt)
    if not sys.stdin.isatty():
        raise FixtureError("Publication confirmation requires an interactive terminal")
    phrase = f"publish {rebuilt['planId']}"
    entered = input(f"Type '{phrase}' to commit and push this exact plan: ").strip()
    if entered != phrase:
        raise FixtureError("Publication cancelled; confirmation did not match")

    paths = copy_publication(rebuilt)
    run_git(REPO_ROOT, "add", "--", *paths)
    staged = run_git(REPO_ROOT, "diff", "--cached", "--name-only").splitlines()
    if sorted(staged) != paths:
        run_git(REPO_ROOT, "restore", "--staged", "--", *paths, check=False)
        raise FixtureError(f"Refusing commit because staged paths differ from the plan: {staged}")
    if not staged:
        raise FixtureError("Publication plan produces no Git changes")
    message = args.message or f"test(fixtures): publish {rebuilt['collection']}"
    run_git(REPO_ROOT, "commit", "-m", message)
    commit = run_git(REPO_ROOT, "rev-parse", "HEAD")
    run_git(REPO_ROOT, "push", "origin", f"HEAD:refs/heads/{EXPECTED_BRANCH}")
    remote_line = run_git(REPO_ROOT, "ls-remote", "origin", f"refs/heads/{EXPECTED_BRANCH}")
    if not remote_line.startswith(commit + "\t"):
        raise FixtureError(f"Push returned but remote verification did not resolve to {commit}")
    manifest_relative = f"{rebuilt['destination']}/fixture-manifest.json"
    remote_manifest = run_git_bytes(REPO_ROOT, "show", f"{commit}:{manifest_relative}")
    expected_manifest = canonical_json(rebuilt["publishedManifest"])
    if sha256_bytes(remote_manifest) != sha256_bytes(expected_manifest):
        raise FixtureError("Remote commit manifest does not match the approved publication plan")
    print(f"Published and verified {commit} on origin/{EXPECTED_BRANCH}")
    print(f"Verified manifest: {manifest_relative} ({sha256_bytes(remote_manifest)})")
    return 0


def unity_command(
        unity: Path,
        config_path: Path,
        output: Path,
        log_path: Path,
        build_target: str,
        execute_method: str = "UabsFixtures.Editor.FixtureGenerator.Generate",
        extra_arguments: Iterable[str] = ()) -> list[str]:
    return [
        str(unity), "-batchmode", "-quit",
        "-buildTarget", UNITY_BUILD_TARGET_ARGUMENTS[build_target],
        "-projectPath", str(GENERATOR_ROOT),
        "-executeMethod", execute_method,
        "-fixtureConfig", str(config_path),
        "-fixtureOutput", str(output),
        "-logFile", str(log_path),
        *extra_arguments,
    ]


def diagnose_modules(args: argparse.Namespace) -> int:
    config_path = GENERATOR_ROOT / "config" / f"{args.profile}.json"
    config = load_json(config_path)
    validate_generator_config(config)
    unity = Path(args.unity).resolve(strict=True)
    diagnostic_root = GENERATED_ROOT / "module-diagnostic"
    failures: list[str] = []
    for stripping in (True, False):
        label = "strip-enabled" if stripping else "strip-disabled"
        output = diagnostic_root / label
        log_path = diagnostic_root / f"{label}.unity-generator.log"
        output.parent.mkdir(parents=True, exist_ok=True)
        command = unity_command(
            unity,
            config_path,
            output,
            log_path,
            config["buildTarget"],
            "UabsFixtures.Editor.FixtureGenerator.DiagnoseModuleStripping",
            ("-fixtureStripEngineCode", str(stripping).lower()),
        )
        result = subprocess.run(command, check=False)
        report_path = output / "module-diagnostic.json"
        if report_path.is_file():
            report = load_json(report_path)
            print(f"{label}: {json.dumps(report, sort_keys=True)}")
            if report.get("reopened") is not True:
                failures.append(f"{label} did not reopen its bundle")
        else:
            failures.append(f"{label} produced no diagnostic report")
        if result.returncode != 0:
            failures.append(f"{label} Unity exit code {result.returncode}; see {log_path}")
    if failures:
        raise FixtureError("Module diagnostic found failures:\n" + "\n".join(failures))
    print("Both controlled module diagnostics built and reopened successfully.")
    return 0


def prepare_native_probe_project(project: Path, include_assetbundle_module: bool) -> None:
    if project.exists():
        shutil.rmtree(project)
    (project / "Assets" / "Editor").mkdir(parents=True)
    shutil.copy2(NATIVE_PROBE_SOURCE, project / "Assets" / "Editor" / NATIVE_PROBE_SOURCE.name)
    shutil.copytree(GENERATOR_ROOT / "ProjectSettings", project / "ProjectSettings")
    manifest = load_json(GENERATOR_ROOT / "Packages" / "manifest.json")
    dependencies = manifest.get("dependencies")
    if not isinstance(dependencies, dict):
        raise FixtureError("Generator Packages/manifest.json has no dependencies object")
    if include_assetbundle_module:
        dependencies["com.unity.modules.assetbundle"] = "1.0.0"
    else:
        dependencies.pop("com.unity.modules.assetbundle", None)
    (project / "Packages").mkdir()
    (project / "Packages" / "manifest.json").write_bytes(canonical_json(manifest))


def read_probe_result(path: Path) -> dict[str, Any]:
    result: dict[str, Any] = {}
    for line in path.read_text(encoding="utf-8").splitlines():
        key, separator, value = line.partition("=")
        if not separator or not key:
            raise FixtureError(f"Malformed native probe result line in '{path}': {line!r}")
        result[key] = value
    return result


def diagnose_native_pipeline(args: argparse.Namespace) -> int:
    unity = Path(args.unity).resolve(strict=True)
    root = GENERATED_ROOT / "native-pipeline-diagnostic"
    results: dict[str, Any] = {}
    for include_module in (False, True):
        label = "module-omitted" if not include_module else "module-explicit"
        condition = root / label
        project = condition / "project"
        output = condition / "output"
        log_path = condition / "unity.log"
        prepare_native_probe_project(project, include_module)
        if output.exists():
            shutil.rmtree(output)
        if log_path.exists():
            log_path.unlink()
        output.mkdir(parents=True)
        command = [
            str(unity), "-batchmode", "-quit", "-buildTarget", "Win64",
            "-projectPath", str(project),
            "-executeMethod", "UabsFixturesProbe.Editor.NativeBundleProbe.Run",
            "-probeOutput", str(output),
            "-logFile", str(log_path),
        ]
        completed = subprocess.run(command, check=False)
        condition_result: dict[str, Any] = {
            "assetBundleModuleDeclared": include_module,
            "unityExitCode": completed.returncode,
            "log": str(log_path),
        }
        report_path = output / "probe-result.txt"
        if report_path.is_file():
            condition_result["editorReport"] = read_probe_result(report_path)
        bundle_path = output / "bundles" / "uabs-native-probe.bundle"
        if bundle_path.is_file():
            try:
                condition_result["unityFsHeader"] = parse_unityfs_header(bundle_path)
            except FixtureError as exc:
                condition_result["unityFsHeaderError"] = str(exc)
        if log_path.is_file():
            condition_result["moduleWarnings"] = [
                line.strip() for line in log_path.read_text(encoding="utf-8", errors="replace").splitlines()
                if "module" in line.lower() and "disable" in line.lower()
            ]
        results[label] = condition_result
        print(f"{label}: {json.dumps(condition_result, sort_keys=True)}")

    summary_path = root / "diagnostic-summary.json"
    summary_path.write_bytes(canonical_json(results))
    explicit_report = results["module-explicit"].get("editorReport", {})
    if explicit_report.get("reopened") != "True":
        raise FixtureError(
            "The explicit AssetBundle-module probe did not reopen; the isolated result does not support a "
            f"project-manifest correction. See {summary_path}")
    print(
        "The explicit AssetBundle-module probe reopened successfully. Compare module-omitted and "
        f"module-explicit details in {summary_path}")
    return 0


def generate(args: argparse.Namespace) -> int:
    config_path = Path(args.config) if args.config else GENERATOR_ROOT / "config" / f"{args.profile}.json"
    config_path = require_contained(config_path, GENERATOR_ROOT / "config", "generator config")
    config = load_json(config_path)
    validate_generator_config(config)
    configured_output = GENERATOR_ROOT / config["outputDirectory"]
    output = Path(args.output) if args.output else configured_output
    output = require_contained(output, GENERATED_ROOT, "generator output")
    if output == GENERATED_ROOT.resolve(strict=False):
        raise FixtureError("generator output must be a profile directory below Generated/working")
    print(f"Validated {config['profile']} configuration: {config_path}")
    print(f"Output: {output}")
    if args.validate_only:
        return 0
    unity = Path(args.unity).resolve(strict=True)
    log_path = output.parent / f"{output.name}.unity-generator.log"
    command = unity_command(unity, config_path, output, log_path, config["buildTarget"])
    output.parent.mkdir(parents=True, exist_ok=True)
    result = subprocess.run(command, check=False)
    if result.returncode != 0:
        raise FixtureError(f"Unity generation failed with exit code {result.returncode}; see {log_path}")
    manifest = output / "fixture-manifest.json"
    verify_manifest_files(manifest)
    print(f"Generated and verified: {manifest}")
    return 0


def verify(args: argparse.Namespace) -> int:
    bundles = verify_manifest_files(Path(args.manifest))
    print(f"Verified {len(bundles)} bundles from {Path(args.manifest).resolve()}")
    for item in bundles:
        print(f"{item['relative']} {item['sizeBytes']} {item['sha256']}")
    return 0


def parser() -> argparse.ArgumentParser:
    root = argparse.ArgumentParser(description=__doc__)
    sub = root.add_subparsers(dest="command", required=True)
    generation = sub.add_parser("generate", help="validate configuration and invoke Unity locally")
    generation.add_argument("--profile", choices=("smoke", "regression", "stress"), default="smoke")
    generation.add_argument("--config")
    generation.add_argument("--output")
    generation.add_argument("--unity", help="path to Unity Editor executable")
    generation.add_argument("--validate-only", action="store_true")
    generation.set_defaults(handler=generate)

    verification = sub.add_parser("verify", help="validate a generated or published manifest and hashes")
    verification.add_argument("--manifest", required=True)
    verification.set_defaults(handler=verify)

    diagnostic = sub.add_parser(
        "diagnose-modules", help="compare minimal bundle loading with engine stripping enabled and disabled")
    diagnostic.add_argument("--profile", choices=("smoke", "regression", "stress"), default="smoke")
    diagnostic.add_argument("--unity", required=True, help="path to Unity Editor executable")
    diagnostic.set_defaults(handler=diagnose_modules)

    native_diagnostic = sub.add_parser(
        "diagnose-native-pipeline",
        help="compare isolated Unity projects with the AssetBundle built-in module omitted and explicit")
    native_diagnostic.add_argument("--unity", required=True, help="path to Unity Editor executable")
    native_diagnostic.set_defaults(handler=diagnose_native_pipeline)

    publication = sub.add_parser("publish", help="plan or explicitly publish an allowlisted collection")
    mode = publication.add_mutually_exclusive_group(required=True)
    mode.add_argument("--dry-run", action="store_true")
    mode.add_argument("--plan", help="approved dry-run plan; requires interactive confirmation")
    publication.add_argument("--manifest", help="generated fixture-manifest.json (dry-run only)")
    publication.add_argument("--allowlist", help="explicit allowlist JSON (dry-run only)")
    publication.add_argument("--plan-output")
    publication.add_argument("--approve-stress", action="store_true")
    publication.add_argument("--message")
    publication.set_defaults(handler=lambda args: dry_run(args) if args.dry_run else publish(args))
    return root


def main(argv: list[str] | None = None) -> int:
    args = parser().parse_args(argv)
    if args.command == "generate" and not args.validate_only and not args.unity:
        raise FixtureError("--unity is required unless --validate-only is used")
    if args.command == "publish" and args.dry_run and (not args.manifest or not args.allowlist):
        raise FixtureError("publish --dry-run requires --manifest and --allowlist")
    if args.command == "publish" and not args.dry_run and (args.manifest or args.allowlist or args.plan_output):
        raise FixtureError("publish execution accepts only --plan, --message, and the recorded stress approval")
    return args.handler(args)


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except FixtureError as exc:
        print(f"error: {exc}", file=sys.stderr)
        raise SystemExit(2)
