#!/usr/bin/env python3
"""Trusted orchestration for RPG iterations. Run this copy from the baseline.

Only ``gate`` and ``publish`` need GitHub credentials. Neither runs project
code. Candidate code is tested in a separate, read-only-permission job.
"""
from __future__ import annotations

import argparse
import base64
import datetime as dt
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import shutil
import stat
import subprocess
import tempfile
import urllib.error
import urllib.parse
import urllib.request


MAX_PATCH_BYTES = 32 * 1024 * 1024
MAX_FILE_BYTES = 64 * 1024 * 1024
MAX_FILES = 2048
MAX_LFS_BUNDLE_BYTES = 256 * 1024 * 1024
STATE_PATH = ".rpg-automation/state.json"
INTERVAL_SECONDS = 5 * 60 * 60
HEX40 = re.compile(r"[0-9a-f]{40}\Z")
HEX64 = re.compile(r"[0-9a-f]{64}\Z")
FORBIDDEN_PARTS = {".git", ".ssh", ".aws", ".azure", ".gnupg", ".codex", ".claude",
                   "Library", "Temp", "Logs", "Obj", "obj", "node_modules"}
SECRET_NAMES = {".env", "id_rsa", "id_ed25519", "credentials", "credentials.json",
                "auth.json", "Unity_lic.ulf"}
PROTECTED_PREFIXES = (".github/", "tools/automation/", "tests/automation/",
                      ".agents/", "skills/", ".rpg-automation/")
PROTECTED_NAMES = {"AGENTS.md", "AGENTS.override.md", ".gitattributes", ".gitignore",
                   ".lfsconfig", ".gitmodules",
                   "tools/validate_project.py", "tools/package_unity_project.py"}
LOW_RISK_DOCUMENTS = {"DEVELOPMENT_PROGRESS.md", "KNOWN_ISSUES.md", "NEXT_ITERATION.md",
                      "SKILLS_USAGE.md", "README.md"}
# These modules have trusted, directly executed movement/animation regression
# checks. A new module must earn equivalent coverage before automated promotion.
OFFLINE_COVERED_CS = {"Assets/Scripts/Player/PlayerMovement.cs",
                      "Assets/Scripts/Player/PlayerAnimations.cs",
                      "Assets/Scripts/Player/PlayerHealth.cs",
                      "Assets/Scripts/Player/PlayerMana.cs"}


class AutomationError(Exception):
    pass


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def utc_now() -> dt.datetime:
    return dt.datetime.now(dt.timezone.utc).replace(microsecond=0)


def timestamp(value: dt.datetime) -> str:
    return value.astimezone(dt.timezone.utc).isoformat().replace("+00:00", "Z")


def parse_timestamp(value: object) -> dt.datetime:
    if not isinstance(value, str) or not re.fullmatch(r"\d{4}-\d\d-\d\dT\d\d:\d\d:\d\dZ", value):
        raise AutomationError("State timestamp must be an explicit UTC timestamp")
    try:
        return dt.datetime.fromisoformat(value.replace("Z", "+00:00"))
    except ValueError as exc:
        raise AutomationError("State timestamp is invalid") from exc


def safe_path(value: object) -> str:
    if not isinstance(value, str) or not value or len(value) > 512:
        raise AutomationError("Invalid candidate path")
    if "\\" in value or any(ord(c) < 32 for c in value) or ":" in value:
        raise AutomationError("Candidate path contains unsafe characters")
    parts = value.split("/")
    if PurePosixPath(value).is_absolute() or any(p in {"", ".", ".."} for p in parts):
        raise AutomationError("Candidate path escapes the project")
    if any(p in FORBIDDEN_PARTS for p in parts):
        raise AutomationError("Candidate includes a forbidden directory")
    if any(p.lower().startswith(".env") or p in SECRET_NAMES for p in parts):
        raise AutomationError("Candidate includes a credential path")
    if value.lower().endswith((".pem", ".key", ".pfx", ".p12", ".ulf")):
        raise AutomationError("Candidate includes a credential file")
    return value


def ensure_regular(path: Path, *, limit: int | None = None) -> bytes:
    if any(parent.is_symlink() for parent in path.parents):
        raise AutomationError("Bundle parent directories cannot be symbolic links")
    try:
        info = path.lstat()
    except OSError as exc:
        raise AutomationError("Required bundle file is missing") from exc
    if not stat.S_ISREG(info.st_mode) or info.st_nlink != 1:
        raise AutomationError("Bundle files must be ordinary files without hard links")
    if limit is not None and info.st_size > limit:
        raise AutomationError("Bundle file exceeds the size limit")
    return path.read_bytes()


def read_json(path: Path, limit: int = 512 * 1024) -> dict:
    try:
        result = json.loads(ensure_regular(path, limit=limit))
    except (UnicodeError, ValueError) as exc:
        raise AutomationError("JSON file is malformed") from exc
    if not isinstance(result, dict):
        raise AutomationError("JSON file must contain an object")
    return result


def write_json(path: Path, value: dict) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def outputs(values: dict) -> None:
    destination = os.environ.get("GITHUB_OUTPUT")
    if destination:
        with open(destination, "a", encoding="utf-8") as stream:
            for name, value in values.items():
                text = str(value).lower() if isinstance(value, bool) else str(value)
                if not re.fullmatch(r"[a-z_]+", name) or "\n" in text or "\r" in text:
                    raise AutomationError("Unsafe GitHub output")
                stream.write(f"{name}={text}\n")
    print(json.dumps(values, sort_keys=True))


def git(root: Path, *args: str, env: dict | None = None, input_data: bytes | None = None) -> bytes:
    command = ["git", "-c", "core.hooksPath=/dev/null", "-c", "core.fsmonitor=false", *args]
    process = subprocess.run(command, cwd=root, input=input_data, stdout=subprocess.PIPE,
                             stderr=subprocess.PIPE, env=env, check=False)
    if process.returncode:
        # Do not include stderr: authentication errors can contain credential URLs.
        raise AutomationError(f"Git operation {args[0]} failed (exit {process.returncode})")
    return process.stdout


def check_sha(value: object, regex=HEX40) -> str:
    if not isinstance(value, str) or not regex.fullmatch(value):
        raise AutomationError("Invalid SHA")
    return value


def check_branch(branch: str) -> str:
    if (not re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9._/-]*", branch) or ".." in branch or "//" in branch
            or branch.endswith(("/", ".", ".lock")) or "@{" in branch):
        raise AutomationError("Invalid branch name")
    return branch


def repository_name(value: str) -> str:
    if not re.fullmatch(r"[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+", value):
        raise AutomationError("Invalid repository identity")
    return value


class GitHub:
    def __init__(self, repository: str, token: str | None = None):
        self.repository = repository_name(repository)
        self.token = token or os.environ.get("GITHUB_TOKEN") or os.environ.get("GH_TOKEN")
        if not self.token:
            raise AutomationError("GitHub token is not configured")
        # Hosted GitHub is the configured project host. Do not send tokens to a
        # candidate-controlled endpoint or silently use an enterprise override.
        endpoint = os.environ.get("GITHUB_API_URL", "https://api.github.com")
        if endpoint.rstrip("/") != "https://api.github.com":
            raise AutomationError("Unexpected GitHub API endpoint")
        self.base = endpoint.rstrip("/") + "/repos/" + self.repository

    def request(self, method: str, path: str, data: dict | None = None, *, missing=False):
        body = json.dumps(data).encode() if data is not None else None
        request = urllib.request.Request(self.base + path, data=body, method=method, headers={
            "Authorization": "Bearer " + self.token,
            "Accept": "application/vnd.github+json",
            "X-GitHub-Api-Version": "2022-11-28",
            "Content-Type": "application/json",
            "User-Agent": "rpg-continuous-improvement",
        })
        try:
            # Never forward a bearer token through an HTTP redirect.
            class NoRedirect(urllib.request.HTTPRedirectHandler):
                def redirect_request(self, req, fp, code, msg, headers, newurl):
                    return None
            opener = urllib.request.build_opener(NoRedirect())
            with opener.open(request, timeout=30) as response:
                raw = response.read(1024 * 1024 + 1)
        except urllib.error.HTTPError as exc:
            if missing and exc.code == 404:
                return None
            raise AutomationError(f"GitHub API request failed (HTTP {exc.code})") from None
        except (urllib.error.URLError, TimeoutError) as exc:
            raise AutomationError("GitHub API is unreachable") from None
        if len(raw) > 1024 * 1024:
            raise AutomationError("GitHub API response exceeded the limit")
        try:
            return json.loads(raw)
        except ValueError as exc:
            raise AutomationError("GitHub API returned malformed JSON") from exc

    def read_state(self, state_branch: str) -> tuple[dict | None, str | None]:
        route = "/contents/" + STATE_PATH + "?ref=" + urllib.parse.quote(state_branch, safe="")
        result = self.request("GET", route, missing=True)
        if result is None:
            return None, None
        if not isinstance(result, dict) or result.get("type") != "file" or result.get("encoding") != "base64":
            raise AutomationError("Persistent state is not a regular GitHub file")
        if not isinstance(result.get("content"), str):
            raise AutomationError("Persistent state content is malformed")
        try:
            encoded = "".join(result["content"].split())
            data = base64.b64decode(encoded, validate=True)
            state = json.loads(data)
        except (KeyError, ValueError, TypeError) as exc:
            raise AutomationError("Persistent state is malformed") from exc
        if len(data) > 65536 or not isinstance(state, dict):
            raise AutomationError("Persistent state is invalid")
        return state, check_sha(result.get("sha"))

    def ensure_state_branch(self, state_branch: str, base_sha: str) -> None:
        path = "/git/ref/heads/" + urllib.parse.quote(state_branch, safe="")
        if self.request("GET", path, missing=True) is not None:
            return
        self.request("POST", "/git/refs", {"ref": "refs/heads/" + state_branch, "sha": base_sha})

    def write_state(self, state_branch: str, state: dict, file_sha: str | None) -> None:
        data = {"branch": state_branch, "message": "Record RPG automation iteration state",
                "content": base64.b64encode((json.dumps(state, sort_keys=True) + "\n").encode()).decode()}
        if file_sha:
            data["sha"] = file_sha
        # GitHub's contents API rejects a stale file SHA rather than overwriting.
        self.request("PUT", "/contents/" + STATE_PATH, data)


def evaluate_state(state: dict | None, now: dt.datetime) -> tuple[bool, str, str]:
    if state is None:
        return True, "ready", timestamp(now)
    if state.get("schema_version") != 1:
        raise AutomationError("Unsupported persistent state schema")
    previous = parse_timestamp(state.get("last_started_at"))
    if previous > now:
        raise AutomationError("Persistent state timestamp is in the future; refusing to run")
    next_due = previous + dt.timedelta(seconds=INTERVAL_SECONDS)
    due = now >= next_due
    return due, "ready" if due else "interval_not_elapsed", timestamp(next_due)


def gate(args) -> None:
    branch = check_branch(args.branch)
    state_branch = check_branch(args.state_branch)
    if state_branch == branch:
        raise AutomationError("Scheduling state must use a separate branch")
    api = GitHub(args.repository)
    metadata = api.request("GET", "")
    if not isinstance(metadata, dict) or metadata.get("default_branch") != branch:
        raise AutomationError("The target branch must be the repository default branch for scheduling")
    # Repository-wide /actions/permissions requires administration privileges
    # unavailable to normal GITHUB_TOKENs. The workflow's actual active state is
    # readable with actions:read and directly answers whether this schedule runs.
    workflow = api.request("GET", "/actions/workflows/rpg-continuous-improvement.yml")
    ref = api.request("GET", "/git/ref/heads/" + urllib.parse.quote(branch, safe=""))
    if not isinstance(ref, dict) or not isinstance(ref.get("object"), dict):
        raise AutomationError("GitHub returned an invalid branch reference")
    base_sha = check_sha(ref["object"].get("sha"))
    state, file_sha = api.read_state(state_branch)
    now = utc_now()
    due, reason, next_due = evaluate_state(state, now)
    present = os.environ.get("OPENAI_API_KEY_PRESENT", "false").lower()
    if present not in {"true", "false"}:
        raise AutomationError("API-key presence must be a boolean, never a secret value")
    if not isinstance(workflow, dict) or workflow.get("state") != "active":
        due, reason = False, "actions_disabled"
    elif present != "true":
        due, reason = False, "missing_api_key"
    elif args.force:
        due, reason = True, "ready"
    last_result = state.get("last_result", {}) if state else {}
    if not isinstance(last_result, dict):
        raise AutomationError("Persistent iteration result is malformed")
    previous_branch = last_result.get("branch", "")
    previous_sha = last_result.get("commit_sha", "")
    if last_result and type(last_result.get("published_to_master")) is not bool:
        raise AutomationError("Persistent publication result is not an explicit boolean")
    if previous_branch:
        check_branch(previous_branch)
        check_sha(previous_sha)
    run_id = os.environ.get("GITHUB_RUN_ID", "local")
    if not re.fullmatch(r"[A-Za-z0-9_-]+", run_id):
        raise AutomationError("Invalid run identity")
    if due and not args.dry_run:
        api.ensure_state_branch(state_branch, base_sha)
        claimed = {"schema_version": 1, "last_started_at": timestamp(now), "run_id": run_id,
                   "baseline_sha": base_sha, "status": "started"}
        if last_result:
            claimed["last_result"] = last_result
        api.write_state(state_branch, claimed, file_sha)
        next_due = timestamp(now + dt.timedelta(seconds=INTERVAL_SECONDS))
    outputs({"due": due, "reason": reason, "base_sha": base_sha, "next_due": next_due,
             "run_id": run_id, "dry_run": args.dry_run,
             "last_candidate_branch": previous_branch, "last_candidate_sha": previous_sha,
             "last_published_to_master": last_result.get("published_to_master", "")})


def diff_entries(root: Path, env: dict | None = None) -> list[dict]:
    data = git(root, "diff", "--cached", "--name-status", "-z", "--no-renames", "HEAD", env=env)
    fields = data.decode("utf-8").split("\0")
    entries = []
    for offset in range(0, len(fields) - 1, 2):
        if offset + 1 >= len(fields) or fields[offset] not in {"A", "M", "D", "T"}:
            raise AutomationError("Unsupported candidate change")
        entries.append({"path": safe_path(fields[offset + 1]), "status": fields[offset]})
    if len(entries) > MAX_FILES:
        raise AutomationError("Candidate modifies too many files")
    return sorted(entries, key=lambda entry: entry["path"])


def risk_report(entries: list[dict], patch: bytes) -> dict:
    risks = []
    manual = False
    for entry in entries:
        path = safe_path(entry["path"])
        if path in PROTECTED_NAMES or path.startswith(PROTECTED_PREFIXES) or Path(path).name.startswith("AGENTS"):
            risks.append(f"trusted_configuration:{path}")
            manual = True
        elif path.startswith(("Packages/", "ProjectSettings/")):
            risks.append(f"unity_configuration:{path}")
        elif path in LOW_RISK_DOCUMENTS or path.startswith("docs/") and path.endswith(".md"):
            pass
        elif path.startswith("tests/") and path.endswith(".py"):
            if entry["status"] in {"D", "T"}:
                risks.append(f"test_removed:{path}")
                manual = True
        elif path.endswith(".cs") and path.startswith("Assets/"):
            if path not in OFFLINE_COVERED_CS or entry["status"] != "M":
                risks.append(f"csharp_without_trusted_behavior_coverage:{path}")
            if entry["status"] in {"D", "T"}:
                risks.append(f"code_removed:{path}")
            if re.search(r"(Save|Serialization|Inventory|Quest|Combat|Damage|World|Core)(?:/|\.cs)", path, re.I):
                risks.append(f"core_game_system:{path}")
        elif path.endswith(".meta"):
            owner = path[:-5]
            if owner in OFFLINE_COVERED_CS and entry["status"] == "M":
                pass
            else:
                risks.append(f"unity_asset_metadata:{path}")
        else:
            risks.append(f"native_or_visual_verification:{path}")
    # Native engine lifecycle, persistence and physics changes need a real editor.
    changed = b"\n".join(line for line in patch.splitlines() if line[:1] in {b"+", b"-"})
    if re.search(rb"\b(Physics2D|Collider2D|Rigidbody2D|Animator|OnGUI|PlayerPrefs|JsonUtility|SceneManager|AssetDatabase|Resources\.Load)\b", changed):
        risks.append("native_unity_behavior_changed")
    if any(entry["path"].endswith(".cs") for entry in entries):
        if re.search(rb"\b(Process|File|Directory|Environment|Reflection|Assembly|Activator|DllImport|Marshal|unsafe|extern|HttpClient|WebRequest|Socket|TcpClient|UdpClient|Thread|Console|StackTrace)\b|System\.(IO|Net)|Type\.GetType|Task\.Run|\[\s*(assembly|module)\s*:", changed):
            risks.append("external_side_effect_or_test_bypass_api")
        if re.search(rb"\\[uU][0-9a-fA-F]{4}|(?m:^[+-]\s*#(?:if|elif|define|undef|pragma))", changed):
            risks.append("conditional_or_escaped_csharp_requires_review")
        if len(changed.splitlines()) > 250:
            risks.append("large_csharp_change_requires_native_review")
    # Removing existing assertions can invalidate a previously green baseline.
    if any(entry["path"].startswith("tests/") and entry["status"] == "M" for entry in entries):
        if re.search(rb"(?m)^-.*\b(assert|Assert|unittest|test_)\b", patch):
            risks.append("existing_test_assertion_removed")
            manual = True
    return {"risk": "high" if risks else "low", "risks": sorted(set(risks)),
            "requires_manual_review": manual}


def lfs_pointer(data: bytes) -> tuple[str, int] | None:
    if len(data) > 1024 or not data.startswith(b"version https://git-lfs.github.com/spec/v1\n"):
        return None
    match = re.fullmatch(rb"version https://git-lfs.github.com/spec/v1\noid sha256:([0-9a-f]{64})\nsize ([0-9]+)\n?", data)
    if not match:
        raise AutomationError("Malformed LFS pointer")
    return match.group(1).decode(), int(match.group(2))


def ensure_project_file(root: Path, path: str, *, missing_ok=False) -> Path:
    safe_path(path)
    result = root / path
    for parent in [result, *result.parents]:
        if parent == root.parent:
            break
        if parent.is_symlink():
            raise AutomationError("Candidate paths cannot contain symbolic links")
    if result.exists():
        info = result.lstat()
        if not stat.S_ISREG(info.st_mode):
            raise AutomationError("Candidate paths must be ordinary files")
        if info.st_size > MAX_FILE_BYTES:
            raise AutomationError("Candidate file exceeds the size limit")
    elif not missing_ok:
        raise AutomationError("Candidate file is missing")
    return result


def export(args) -> None:
    root = Path(args.root).resolve()
    base_sha = check_sha(args.base_sha)
    if git(root, "rev-parse", "HEAD").decode().strip() != base_sha:
        raise AutomationError("Generation checkout does not match the claimed baseline")
    bundle = Path(args.output_dir).resolve()
    if bundle == root or root in bundle.parents:
        raise AutomationError("The bundle must be written outside the candidate project")
    bundle.mkdir(parents=True, exist_ok=True)
    if any(bundle.iterdir()):
        raise AutomationError("Bundle output directory must be empty")
    status = git(root, "-c", "status.renames=false", "status", "--porcelain=v1", "-z", "--untracked-files=all")
    changed_count = 0
    changed_bytes = 0
    for row in status.decode("utf-8").split("\0"):
        if row:
            changed_count += 1
            path = ensure_project_file(root, row[3:], missing_ok=True)
            changed_bytes += path.stat().st_size if path.exists() else 0
            if changed_count > MAX_FILES or changed_bytes > MAX_LFS_BUNDLE_BYTES:
                raise AutomationError("Candidate changes exceed the total size or file-count limit")
    with tempfile.TemporaryDirectory(prefix="rpg-export-index-") as directory:
        env = dict(os.environ, GIT_INDEX_FILE=str(Path(directory) / "index"))
        git(root, "read-tree", "HEAD", env=env)
        git(root, "add", "--all", "--", ".", env=env)
        entries = diff_entries(root, env)
        patch = git(root, "diff", "--cached", "--binary", "--no-ext-diff", "--no-textconv", "--no-renames", "HEAD", env=env)
        if len(patch) > MAX_PATCH_BYTES:
            raise AutomationError("Candidate patch exceeds the size limit")
        objects = []
        object_bytes = 0
        for entry in entries:
            if entry["status"] == "D":
                continue
            path = ensure_project_file(root, entry["path"])
            mode = git(root, "ls-files", "--stage", "--", entry["path"], env=env).split(b" ", 1)[0]
            if mode not in {b"100644", b"100755"}:
                raise AutomationError("Symlinks and submodules are not exportable")
            pointer = lfs_pointer(git(root, "show", ":" + entry["path"], env=env))
            if pointer:
                oid, length = pointer
                object_bytes += length
                if object_bytes > MAX_LFS_BUNDLE_BYTES:
                    raise AutomationError("Candidate LFS bundle exceeds the total size limit")
                raw = path.read_bytes()
                if sha256(raw) != oid or len(raw) != length:
                    raise AutomationError("Changed LFS asset is not materialized or its hash is invalid")
                target = bundle / "lfs" / oid
                target.parent.mkdir(exist_ok=True)
                target.write_bytes(raw)
                objects.append({"path": entry["path"], "oid": oid, "size": length})
    (bundle / "candidate.patch").write_bytes(patch)
    manifest = {"schema_version": 1, "base_sha": base_sha, "patch_sha256": sha256(patch),
                "patch_bytes": len(patch), "files": entries, "lfs_objects": objects,
                **risk_report(entries, patch)}
    write_json(bundle / "manifest.json", manifest)
    outputs({"candidate_sha": manifest["patch_sha256"], "risk": manifest["risk"],
             "changed_files": len(entries), "patch_bytes": len(patch)})


def load_bundle(directory: Path) -> tuple[dict, bytes]:
    if directory.is_symlink():
        raise AutomationError("Bundle directory cannot be a symbolic link")
    manifest = read_json(directory / "manifest.json")
    if manifest.get("schema_version") != 1:
        raise AutomationError("Unsupported bundle schema")
    check_sha(manifest.get("base_sha"))
    check_sha(manifest.get("patch_sha256"), HEX64)
    patch = ensure_regular(directory / "candidate.patch", limit=MAX_PATCH_BYTES)
    if sha256(patch) != manifest["patch_sha256"] or len(patch) != manifest.get("patch_bytes"):
        raise AutomationError("Candidate patch hash or size does not match the manifest")
    entries = manifest.get("files")
    if not isinstance(entries, list) or len(entries) > MAX_FILES:
        raise AutomationError("Invalid candidate file manifest")
    paths = []
    for entry in entries:
        if not isinstance(entry, dict) or entry.get("status") not in {"A", "M", "D", "T"}:
            raise AutomationError("Invalid candidate file entry")
        paths.append(safe_path(entry.get("path")))
    if len(set(paths)) != len(paths):
        raise AutomationError("Duplicate candidate paths")
    if re.search(rb"(?m)^(?:new file mode|old mode|new mode|deleted file mode) (?:120000|160000)$", patch):
        raise AutomationError("Candidate cannot add symlinks or submodules")
    if re.search(rb"(?m)^(?:rename from|rename to|copy from|copy to) ", patch):
        raise AutomationError("Candidate must describe explicit additions and deletions")
    if patch:
        with tempfile.TemporaryDirectory(prefix="rpg-patch-check-") as temporary:
            patch_path = Path(temporary) / "candidate.patch"
            patch_path.write_bytes(patch)
            numstat = git(Path(temporary), "apply", "--numstat", "-z", "--", str(patch_path))
        actual = []
        for row in numstat.decode("utf-8").split("\0"):
            if row:
                fields = row.split("\t", 2)
                if len(fields) != 3:
                    raise AutomationError("Candidate patch has an unsupported path representation")
                actual.append(safe_path(fields[2]))
        if sorted(actual) != sorted(paths):
            raise AutomationError("Candidate patch paths do not match the manifest")
    elif entries:
        raise AutomationError("Empty candidate patch lists changed files")
    objects = manifest.get("lfs_objects", [])
    if not isinstance(objects, list) or len(objects) > len(entries):
        raise AutomationError("Invalid LFS object manifest")
    seen = set()
    total_lfs_bytes = 0
    for item in objects:
        if not isinstance(item, dict) or item.get("path") not in paths or item["path"] in seen:
            raise AutomationError("Invalid or duplicate LFS object path")
        seen.add(item["path"])
        oid = check_sha(item.get("oid"), HEX64)
        if type(item.get("size")) is not int or not 0 <= item["size"] <= MAX_FILE_BYTES:
            raise AutomationError("Invalid LFS object size")
        total_lfs_bytes += item["size"]
        if total_lfs_bytes > MAX_LFS_BUNDLE_BYTES:
            raise AutomationError("Candidate LFS bundle exceeds the total size limit")
        raw = ensure_regular(directory / "lfs" / oid, limit=MAX_FILE_BYTES)
        if sha256(raw) != oid or len(raw) != item["size"]:
            raise AutomationError("Bundled LFS asset failed its integrity check")
    # Risks are recalculated, never trusted from a model-editable manifest.
    manifest.update(risk_report(entries, patch))
    return manifest, patch


def apply_bundle(root: Path, directory: Path, expected_base: str) -> dict:
    manifest, patch = load_bundle(directory)
    if manifest["base_sha"] != check_sha(expected_base):
        raise AutomationError("Candidate belongs to a different baseline")
    if git(root, "rev-parse", "HEAD").decode().strip() != expected_base:
        raise AutomationError("Checkout does not match the validated baseline")
    if git(root, "status", "--porcelain=v1", "-z", "--untracked-files=all"):
        raise AutomationError("Refusing to apply over uncommitted work")
    for entry in manifest["files"]:
        ensure_project_file(root, entry["path"], missing_ok=True)
    if patch:
        patch_file = str((directory / "candidate.patch").resolve())
        git(root, "apply", "--check", "--index", "--binary", "--", patch_file)
        git(root, "apply", "--index", "--binary", "--whitespace=nowarn", "--", patch_file)
        if diff_entries(root) != sorted(manifest["files"], key=lambda item: item["path"]):
            raise AutomationError("Applied candidate differs from the file manifest")
        object_by_path = {item["path"]: item for item in manifest.get("lfs_objects", [])}
        for entry in manifest["files"]:
            if entry["status"] == "D":
                continue
            pointer = lfs_pointer(git(root, "show", ":" + entry["path"]))
            if pointer:
                item = object_by_path.get(entry["path"])
                if item is None or pointer != (item["oid"], item["size"]):
                    raise AutomationError("Changed LFS pointer lacks its exact asset")
        git_dir = Path(git(root, "rev-parse", "--absolute-git-dir").decode().strip())
        for item in manifest.get("lfs_objects", []):
            source = directory / "lfs" / item["oid"]
            target = git_dir / "lfs" / "objects" / item["oid"][:2] / item["oid"][2:4] / item["oid"]
            target.parent.mkdir(parents=True, exist_ok=True)
            if target.exists() and sha256(ensure_regular(target, limit=MAX_FILE_BYTES)) != item["oid"]:
                raise AutomationError("Existing LFS object failed its integrity check")
            if not target.exists():
                shutil.copyfile(source, target)
            shutil.copyfile(source, ensure_project_file(root, item["path"]))
    return manifest


def apply(args) -> None:
    manifest = apply_bundle(Path(args.root).resolve(), Path(args.bundle_dir).resolve(), args.expected_base)
    outputs({"candidate_sha": manifest["patch_sha256"], "risk": manifest["risk"],
             "changed_files": len(manifest["files"])})


def classify(args) -> None:
    manifest, _ = load_bundle(Path(args.bundle_dir).resolve())
    if args.output:
        write_json(Path(args.output), manifest)
    outputs({"risk": manifest["risk"], "candidate_sha": manifest["patch_sha256"],
             "requires_manual_review": manifest["requires_manual_review"]})


def seal_validation(args) -> None:
    manifest, _ = load_bundle(Path(args.bundle_dir).resolve())
    report = read_json(Path(args.project_report))
    if report.get("base_sha") != manifest["base_sha"] or type(report.get("passed")) is not bool:
        raise AutomationError("Project report has the wrong baseline or no explicit verdict")
    if report.get("status") not in {"passed", "failed"} or (report["status"] == "passed") != report["passed"]:
        raise AutomationError("Project report verdict is inconsistent")
    checks = report.get("checks")
    if not isinstance(checks, list) or not checks:
        raise AutomationError("Project report has no executed checks")
    if any(not isinstance(check, dict) for check in checks):
        raise AutomationError("Project report contains an invalid check")
    native = report.get("native_unity_run", False)
    if type(native) is not bool:
        raise AutomationError("Project report native-verification field is invalid")
    sealed = {"schema_version": 1, "passed": report["passed"] and not args.generation_failed,
              "patch_sha256": manifest["patch_sha256"], "base_sha": manifest["base_sha"],
              "native_unity_tested": native, "generation_failed": args.generation_failed,
              "checks": checks, "risk": manifest["risk"],
              "requires_manual_review": manifest["requires_manual_review"]}
    write_json(Path(args.output), sealed)
    outputs({"validation_passed": sealed["passed"], "risk": manifest["risk"],
             "candidate_sha": manifest["patch_sha256"]})


def git_auth_environment(repository: str | None) -> dict:
    env = dict(os.environ)
    token = env.get("GITHUB_TOKEN") or env.get("GH_TOKEN")
    if token:
        if not repository:
            raise AutomationError("Repository identity is required for authenticated publishing")
        repository_name(repository)
        header = "AUTHORIZATION: basic " + base64.b64encode(("x-access-token:" + token).encode()).decode()
        # Scope authorization to the GitHub repository rather than every remote.
        env["GIT_CONFIG_COUNT"] = "2"
        env["GIT_CONFIG_KEY_0"] = "http.https://github.com/" + repository + ".git.extraheader"
        env["GIT_CONFIG_VALUE_0"] = header
        env["GIT_CONFIG_KEY_1"] = "credential.helper"
        env["GIT_CONFIG_VALUE_1"] = ""
    return env


def publish(args) -> None:
    root = Path(args.root).resolve()
    directory = Path(args.bundle_dir).resolve()
    manifest, _ = load_bundle(directory)
    report = read_json(Path(args.validation_file))
    if (report.get("schema_version") != 1 or report.get("patch_sha256") != manifest["patch_sha256"]
            or report.get("base_sha") != manifest["base_sha"] or type(report.get("passed")) is not bool
            or type(report.get("native_unity_tested")) is not bool):
        raise AutomationError("Publish report does not identify this exact validated candidate")
    if not report["passed"] and not args.preserve_failed:
        raise AutomationError("Failed validation cannot be published")
    branch = check_branch(args.branch)
    repository = args.repository or os.environ.get("GITHUB_REPOSITORY")
    if repository:
        repository_name(repository)
        expected_remote = "https://github.com/" + repository + ".git"
        if git(root, "remote", "get-url", "origin").decode().strip().rstrip("/") != expected_remote:
            raise AutomationError("Origin does not match the authorized GitHub repository")
    auth = git_auth_environment(repository)
    remote_result = git(root, "ls-remote", "origin", "refs/heads/" + branch, env=auth).decode().split()
    if not remote_result:
        raise AutomationError("Target remote branch is missing")
    remote_sha = check_sha(remote_result[0])
    can_publish = (report["passed"] and not manifest["requires_manual_review"]
                   and (manifest["risk"] == "low" or report["native_unity_tested"])
                   and remote_sha == manifest["base_sha"])
    applied = apply_bundle(root, directory, manifest["base_sha"])
    run_id = os.environ.get("GITHUB_RUN_ID", "local")
    if not re.fullmatch(r"[A-Za-z0-9_-]+", run_id):
        raise AutomationError("Invalid run identity")
    destination = branch if can_publish else f"rpg/iteration-{run_id}-{manifest['patch_sha256'][:12]}"
    if not applied["files"]:
        commit_sha = manifest["base_sha"]
        published = False
        destination = branch
    else:
        git(root, "-c", "user.name=github-actions[bot]", "-c",
            "user.email=41898282+github-actions[bot]@users.noreply.github.com", "commit", "-m",
            f"Improve RPG iteration {run_id} ({manifest['risk']} risk)")
        commit_sha = check_sha(git(root, "rev-parse", "HEAD").decode().strip())
        if manifest.get("lfs_objects"):
            # Native Git LFS uploads exact, previously hash-checked objects; no
            # project hooks or generated scripts execute in this write-token job.
            git(root, "lfs", "push", "origin", "HEAD", env=auth)
        try:
            git(root, "push", "origin", "HEAD:refs/heads/" + destination, env=auth)
        except AutomationError:
            # A user's push can win after the ls-remote check. Do not rebase or
            # force-push automatically; preserve the already validated commit.
            if not can_publish:
                raise
            destination = f"rpg/iteration-{run_id}-{manifest['patch_sha256'][:12]}"
            git(root, "push", "origin", "HEAD:refs/heads/" + destination, env=auth)
            can_publish = False
        published = can_publish
    state_updated = False
    if repository and (os.environ.get("GITHUB_TOKEN") or os.environ.get("GH_TOKEN")):
        try:
            api = GitHub(repository)
            state, file_sha = api.read_state(check_branch(args.state_branch))
            if state and state.get("run_id") == run_id:
                evaluate_state(state, utc_now())
                state["status"] = "finished"
                state["last_result"] = {"branch": destination, "commit_sha": commit_sha,
                                        "published_to_master": published, "validation_passed": report["passed"],
                                        "finished_at": timestamp(utc_now())}
                api.write_state(args.state_branch, state, file_sha)
                state_updated = True
        except AutomationError:
            # The code is already durable; report state failure without pretending
            # that the successful push was rolled back.
            state_updated = False
    outputs({"commit_sha": commit_sha, "branch": destination, "published_to_master": published,
             "validation_passed": report["passed"], "risk": manifest["risk"], "state_updated": state_updated})


def parser() -> argparse.ArgumentParser:
    result = argparse.ArgumentParser(description=__doc__)
    commands = result.add_subparsers(dest="command", required=True)
    command = commands.add_parser("gate")
    command.add_argument("--repository", required=True)
    command.add_argument("--branch", default="master")
    command.add_argument("--state-branch", default="rpg-automation-state")
    command.add_argument("--force", action="store_true")
    command.add_argument("--dry-run", action="store_true")
    command.set_defaults(function=gate)
    command = commands.add_parser("export")
    command.add_argument("--root", default=".")
    command.add_argument("--base-sha", required=True)
    command.add_argument("--output-dir", required=True)
    command.set_defaults(function=export)
    for name, function in (("apply", apply), ("classify", classify), ("seal-validation", seal_validation), ("publish", publish)):
        command = commands.add_parser(name)
        command.add_argument("--bundle-dir", required=True)
        if name in {"apply", "publish"}:
            command.add_argument("--root", default=".")
        if name == "apply":
            command.add_argument("--expected-base", required=True)
        if name == "classify":
            command.add_argument("--output")
        if name == "seal-validation":
            command.add_argument("--project-report", required=True)
            command.add_argument("--output", required=True)
            command.add_argument("--generation-failed", action="store_true")
        if name == "publish":
            command.add_argument("--validation-file", required=True)
            command.add_argument("--branch", default="master")
            command.add_argument("--repository")
            command.add_argument("--state-branch", default="rpg-automation-state")
            command.add_argument("--preserve-failed", action="store_true")
        command.set_defaults(function=function)
    return result


def main() -> int:
    try:
        arguments = parser().parse_args()
        arguments.function(arguments)
        return 0
    except AutomationError as exc:
        print("Automation stopped: " + str(exc))
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
