#!/usr/bin/env python3
"""Verify a source-project ZIP against a clean source checkout and commit.

Usage: python3 rpg-verify-iteration-004-archive.py SOURCE_ROOT ZIP EXPECTED_COMMIT_SHA
This is distribution verification; it does not run Unity or establish native acceptance.
The ZIP filename and its digest are discovered, never assumed.
"""
from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
from pathlib import Path, PurePosixPath
import re
import stat
import subprocess
import sys
import zipfile

sys.dont_write_bytecode = True


class ArchiveVerificationError(ValueError):
    pass


def require(condition: bool, message: str) -> None:
    if not condition:
        raise ArchiveVerificationError(message)


def git(root: Path, *args: str) -> bytes:
    result = subprocess.run(["git", "-C", str(root), *args], capture_output=True, check=False)
    require(result.returncode == 0, "Source checkout Git verification failed for " + args[0])
    return result.stdout


def verify(root: Path, archive_path: Path, expected_commit: str) -> dict:
    root, archive_path = root.resolve(), archive_path.resolve()
    require(root.is_dir(), "Source root is not a directory")
    require(archive_path.is_file(), "ZIP does not exist")
    require(re.fullmatch(r"[0-9a-f]{40}", expected_commit) is not None,
            "Expected commit must be a full lowercase 40-character SHA")

    # Selection comes from the trusted repository packager, not a ZIP-controlled manifest.
    packager_path = root / "tools/package_unity_project.py"
    require(packager_path.is_file(), "Trusted source packager is absent")
    spec = importlib.util.spec_from_file_location("rpg_trusted_archive_packager", packager_path)
    require(spec is not None and spec.loader is not None, "Trusted packager cannot be loaded")
    packager = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(packager)
    files = packager.files_for_archive(root)
    selected = {path.relative_to(root).as_posix(): path for path in files}
    require(len(selected) == len(files), "Source selection contains duplicate paths")

    source_commit = git(root, "rev-parse", "HEAD").decode("ascii").strip()
    require(source_commit == expected_commit, "Source HEAD differs from the expected commit")
    require(not git(root, "diff", "HEAD", "--name-only", "-z"), "Source checkout has tracked local changes")
    untracked = {
        value.decode("utf-8", "surrogateescape")
        for value in git(root, "ls-files", "--others", "--exclude-standard", "-z").split(b"\0") if value
    }
    require(not (untracked & selected.keys()), "Source selection contains uncommitted files")

    version_path = root / "ProjectSettings/ProjectVersion.txt"
    require(version_path.is_file(), "Source Unity version file is absent")
    version_match = re.search(r"^m_EditorVersion:\s*(\S+)\s*$", version_path.read_text(), re.MULTILINE)
    require(version_match is not None, "Source Unity editor version is unreadable")
    unity_version = version_match.group(1)
    require(unity_version == "2022.3.53f1", "Source editor differs from the required Unity 2022.3.53f1")

    meta_count = 0
    for relative, source in selected.items():
        if PurePosixPath(relative).parts[0] != "Assets":
            continue
        if source.suffix == ".meta":
            require(source.with_suffix("").exists(), "Orphan source asset metadata: " + relative)
            meta_count += 1
        else:
            require(relative + ".meta" in selected and source.with_name(source.name + ".meta").is_file(),
                    "Source asset metadata missing: " + relative)

    prefix = "2D-RPG-Project/"
    info_name = prefix + "ARCHIVE-INFO.json"
    expected_names = {prefix + relative for relative in selected} | {info_name}
    required = (
        "ProjectSettings/EditorBuildSettings.asset", "ProjectSettings/ProjectVersion.txt",
        "Packages/manifest.json", "Packages/packages-lock.json",
    )
    require(all(name in selected for name in required), "Required Unity source settings or package locks are absent")

    compared_bytes = 0
    manifest = []
    with zipfile.ZipFile(archive_path) as archive:
        entries = archive.infolist()
        names = [entry.filename for entry in entries]
        require(len(names) == len(set(names)), "ZIP contains duplicate member names")
        for entry in entries:
            parts = PurePosixPath(entry.filename).parts
            require(not entry.filename.startswith("/") and "\\" not in entry.filename
                    and ".." not in parts and entry.filename.startswith(prefix),
                    "Unsafe ZIP member path: " + entry.filename)
            require(not entry.is_dir(), "Unexpected directory entry: " + entry.filename)
            require(not stat.S_ISLNK(entry.external_attr >> 16), "ZIP symlink refused: " + entry.filename)
            require(not (entry.flag_bits & 1), "Encrypted ZIP member refused: " + entry.filename)
            require(not any(part in packager.EXCLUDED for part in parts),
                    "Cache or credential directory in ZIP: " + entry.filename)
            filename = PurePosixPath(entry.filename).name.lower()
            require(not filename.startswith(".env")
                    and filename not in {"id_rsa", "id_ed25519", "unity_lic.ulf"}
                    and PurePosixPath(filename).suffix not in {".key", ".pem", ".p12", ".ulf", ".pyc"},
                    "Credential/cache file in ZIP: " + entry.filename)
        require(set(names) == expected_names,
                "ZIP file set differs from trusted selection: missing="
                + repr(sorted(expected_names - set(names)))
                + ", extra=" + repr(sorted(set(names) - expected_names)))
        corrupt = archive.testzip()
        require(corrupt is None, "ZIP CRC failure: " + str(corrupt))
        metadata = json.loads(archive.read(info_name))
        require(isinstance(metadata, dict), "ARCHIVE-INFO.json is not an object")
        require(metadata.get("project") == "2D-RPG-Project", "Archive project name differs")
        require(metadata.get("commit") == expected_commit, "Archive commit differs from expected source SHA")
        require(metadata.get("working_tree_has_local_changes") is False, "Archive does not record a clean working tree")
        require(metadata.get("unity") == unity_version, "Archive Unity version differs from its source")
        require(type(metadata.get("file_count")) is int and metadata["file_count"] == len(selected),
                "Archive file count differs from trusted source selection")
        require(metadata.get("native_unity_validation") is False,
                "Archive metadata does not preserve the native-validation limitation")

        for relative, source in sorted(selected.items()):
            source_bytes = source.read_bytes()
            archived_bytes = archive.read(prefix + relative)
            require(not source_bytes.startswith(b"version https://git-lfs.github.com/spec/v1"),
                    "LFS pointer remains in source: " + relative)
            require(not archived_bytes.startswith(b"version https://git-lfs.github.com/spec/v1"),
                    "LFS pointer remains in ZIP: " + relative)
            require(source_bytes == archived_bytes, "ZIP bytes differ from source: " + relative)
            source_digest = hashlib.sha256(source_bytes).hexdigest()
            require(hashlib.sha256(archived_bytes).hexdigest() == source_digest,
                    "ZIP member SHA-256 differs from source: " + relative)
            compared_bytes += len(source_bytes)
            manifest.append([relative, source_digest])

    # Recheck the checkout after reading source bytes to catch concurrent tracked edits.
    require(git(root, "rev-parse", "HEAD").decode("ascii").strip() == expected_commit,
            "Source HEAD changed during verification")
    require(not git(root, "diff", "HEAD", "--name-only", "-z"), "Source changed during verification")
    require({path.relative_to(root).as_posix() for path in packager.files_for_archive(root)} == set(selected),
            "Source file selection changed during verification")
    return {
        "verified": True,
        "commit": expected_commit,
        "unity": unity_version,
        "zip": str(archive_path),
        "zip_bytes": archive_path.stat().st_size,
        "zip_sha256": hashlib.sha256(archive_path.read_bytes()).hexdigest(),
        "source_file_count": len(selected),
        "zip_member_count": len(expected_names),
        "asset_meta_count": meta_count,
        "compared_source_bytes": compared_bytes,
        "file_manifest_sha256": hashlib.sha256(json.dumps(manifest, separators=(",", ":")).encode()).hexdigest(),
        "crc": "passed",
        "exact_file_set": "passed",
        "source_bytes_and_member_sha256": "passed",
        "lfs_pointers": "none",
        "cache_or_credential_members": "none",
        "native_unity_validation": False,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source_root", type=Path)
    parser.add_argument("zip", type=Path)
    parser.add_argument("expected_commit_sha")
    args = parser.parse_args()
    try:
        result = verify(args.source_root, args.zip, args.expected_commit_sha)
    except (ArchiveVerificationError, ValueError, OSError, zipfile.BadZipFile, RuntimeError) as error:
        print(json.dumps({"verified": False, "error": str(error)}), file=sys.stderr)
        return 1
    print(json.dumps(result, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
