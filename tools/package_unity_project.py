#!/usr/bin/env python3
"""Package a usable Unity source project with real LFS assets and no caches/secrets."""
from __future__ import annotations
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import zipfile

PROJECT_DIRS = ('Assets', 'Packages', 'ProjectSettings', 'docs', 'tools', 'tests', '.agents', '.github')
ROOT_FILES = ('README.md', 'AGENTS.md', 'DEVELOPMENT_PROGRESS.md', 'KNOWN_ISSUES.md',
              'NEXT_ITERATION.md', 'SKILLS_USAGE.md', 'LICENSE', 'LICENSE.md',
              '.gitignore', '.gitattributes', '.vsconfig', 'ignore.conf')
EXCLUDED = {'.git', '.ssh', 'Library', 'Temp', 'Logs', 'Obj', 'obj', 'Build', 'Builds',
            '__pycache__', 'node_modules', '.venv', '.pytest_cache', '.vs'}

def files_for_archive(root: Path):
    files = []
    for name in PROJECT_DIRS:
        folder = root / name
        if name in PROJECT_DIRS[:3] and not folder.is_dir():
            raise ValueError('Missing required Unity directory: ' + name)
        if not folder.exists():
            continue
        for path in sorted(folder.rglob('*')):
            relative = path.relative_to(root)
            if any(part in EXCLUDED for part in relative.parts):
                continue
            if path.is_symlink():
                raise ValueError('Symlinks are not accepted in project archives: ' + str(relative))
            if not path.is_file():
                continue
            lower = path.name.lower()
            if lower.startswith('.env') or lower in {'id_rsa', 'id_ed25519', 'unity_lic.ulf'} or path.suffix.lower() in {'.key', '.pem', '.p12', '.ulf', '.pyc'}:
                raise ValueError('Credential/cache file refused: ' + str(relative))
            with path.open('rb') as handle:
                if handle.read(43).startswith(b'version https://git-lfs.github.com/spec/v1'):
                    raise ValueError('Download real LFS objects before packaging: ' + str(relative))
            files.append(path)
    files.extend(root / name for name in ROOT_FILES if (root / name).is_file())
    return sorted(set(files))

def package(root: Path, output: Path):
    files = files_for_archive(root)
    try:
        head = subprocess.check_output(['git', '-C', str(root), 'rev-parse', 'HEAD'], text=True).strip()
        included = {path.relative_to(root).as_posix() for path in files}
        dirty = bool(subprocess.check_output(['git', '-C', str(root), 'diff', 'HEAD', '--name-only'], text=True).strip())
        untracked = subprocess.check_output(['git', '-C', str(root), 'ls-files', '--others', '--exclude-standard'], text=True).splitlines()
        dirty = dirty or any(name in included for name in untracked)
    except subprocess.CalledProcessError:
        head, dirty = None, None
    output.parent.mkdir(parents=True, exist_ok=True)
    metadata = {'project': '2D-RPG-Project', 'unity': '2022.3.53f1', 'commit': head,
                'working_tree_has_local_changes': dirty, 'native_unity_validation': False,
                'file_count': len(files)}
    with zipfile.ZipFile(output, 'w', zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
        for path in files:
            archive.write(path, '2D-RPG-Project/' + path.relative_to(root).as_posix())
        archive.writestr('2D-RPG-Project/ARCHIVE-INFO.json', json.dumps(metadata, indent=2) + '\n')
    with zipfile.ZipFile(output) as archive:
        corrupt = archive.testzip()
        if corrupt:
            raise ValueError('Archive checksum failed: ' + corrupt)
        for required in ('ProjectSettings/EditorBuildSettings.asset', 'Packages/manifest.json', 'ProjectSettings/ProjectVersion.txt'):
            if '2D-RPG-Project/' + required not in archive.namelist():
                raise ValueError('Required project entry missing: ' + required)
    digest = hashlib.sha256(output.read_bytes()).hexdigest()
    output.with_suffix(output.suffix + '.sha256').write_text(digest + '  ' + output.name + '\n')
    print(json.dumps({**metadata, 'zip': str(output), 'bytes': output.stat().st_size,
                      'sha256': digest}, indent=2))
    return metadata

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    package(args.root.resolve(), args.output.resolve())

if __name__ == '__main__':
    main()
