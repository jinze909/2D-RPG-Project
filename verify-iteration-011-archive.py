#!/usr/bin/env python3
"""Read-only iteration 11 source/archive/publication verification.

This program writes evidence and optional HTTPS copies under an explicitly chosen
temporary directory. It does not mutate either Git checkout or an existing ZIP.
Archive membership is taken from the reviewed repository packager. Native Unity
validation is never inferred from these source/distribution checks.
"""
from __future__ import annotations
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path, PurePosixPath
import re
import subprocess
import urllib.request
import zipfile

PREFIX = '2D-RPG-Project/'
SOURCE_ROOT = Path('/workspace/2D-RPG-Project')
DELIVERY_ROOT = Path('/workspace/rpg-deliveries-iteration-008')
LFS_PREFIX = b'version https://git-lfs.github.com/spec/v1\n'

def require(condition, message):
    if not condition:
        raise ValueError(message)

def git(root, *args, binary=False):
    return subprocess.check_output(['git', '-C', str(root), *args], text=not binary)

def sha(data):
    return hashlib.sha256(data).hexdigest()

def tree(root, ref):
    records = {}
    for record in git(root, 'ls-tree', '-r', '-z', ref, binary=True).split(b'\0'):
        if record:
            header, name = record.split(b'\t', 1)
            mode, kind, oid = header.decode().split(' ')
            records[name.decode()] = {'mode': mode, 'kind': kind, 'oid': oid}
    return records

def full_commit(root, value):
    require(re.fullmatch(r'[0-9a-f]{40}', value) is not None, 'A full immutable Git SHA is required')
    require(git(root, 'rev-parse', value + '^{commit}').strip() == value, 'Commit identity mismatch')
    return value

def archive_paths(records):
    return {name: item['oid'] for name, item in records.items()
            if name.endswith('.zip') or name.endswith('.zip.sha256')}

def snapshot(root):
    require(not git(root, 'status', '--porcelain').strip(), 'Delivery checkout must be clean before snapshot')
    head = git(root, 'rev-parse', 'HEAD').strip()
    blobs = archive_paths(tree(root, head))
    require(blobs, 'No historical archive artifacts found')
    for name, oid in blobs.items():
        require(git(root, 'hash-object', '--', name).strip() == oid, 'Historical local artifact differs: ' + name)
    return {'delivery_baseline_commit': head,
            'archive_artifact_count': len(blobs),
            'zip_count': sum(name.endswith('.zip') for name in blobs),
            'checksum_count': sum(name.endswith('.zip.sha256') for name in blobs),
            'archive_blobs': blobs,
            'native_unity_validation': False}

def preservation(root, baseline, ref):
    records = tree(root, ref)
    for name, oid in baseline['archive_blobs'].items():
        require(name in records and records[name]['oid'] == oid, 'Historical Git blob changed or missing: ' + name)
        require((root / name).is_file(), 'Historical local artifact missing: ' + name)
        require(git(root, 'hash-object', '--', name).strip() == oid, 'Historical local bytes changed: ' + name)
    return {'previous_archive_blobs_preserved_count': len(baseline['archive_blobs']),
            'previous_archive_blobs_preserved': baseline['archive_blobs'],
            'preservation_ref': git(root, 'rev-parse', ref).strip()}

def packager(root):
    spec = importlib.util.spec_from_file_location('trusted_project_packager', root / 'tools/package_unity_project.py')
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module

def verify(root, archive_path, source, delivery_root, baseline, preservation_ref='HEAD'):
    full_commit(root, source)
    require(git(root, 'rev-parse', 'HEAD').strip() == source, 'Checkout HEAD does not match exact source SHA')
    require(not git(root, 'diff', 'HEAD', '--name-only').strip(), 'Tracked source checkout is dirty')
    source_tree = tree(root, source)
    files = packager(root).files_for_archive(root)
    paths = {path.relative_to(root).as_posix(): path for path in files}
    untracked = set(git(root, 'ls-files', '--others', '--exclude-standard').splitlines())
    require(not (untracked & set(paths)), 'Included source files remain untracked')
    require(set(paths) <= set(source_tree), 'Packager includes files absent from the source commit')
    expected = {PREFIX + name for name in paths} | {PREFIX + 'ARCHIVE-INFO.json'}
    require(archive_path.is_file(), 'Archive not found')
    digest = sha(archive_path.read_bytes())
    checksum_path = archive_path.with_suffix(archive_path.suffix + '.sha256')
    require(checksum_path.is_file(), 'Archive SHA-256 sidecar is missing')
    require(checksum_path.read_bytes() == (digest + '  ' + archive_path.name + '\n').encode(), 'SHA-256 sidecar mismatch')
    manifest, source_bytes, lfs_count, guids = [], 0, 0, {}
    with zipfile.ZipFile(archive_path) as archive:
        names = archive.namelist()
        require(len(names) == len(set(names)), 'Duplicate ZIP member')
        require(set(names) == expected, 'ZIP membership differs from trusted packager')
        require(archive.testzip() is None, 'ZIP CRC validation failed')
        info = json.loads(archive.read(PREFIX + 'ARCHIVE-INFO.json'))
        require(info == {'project': '2D-RPG-Project', 'unity': '2022.3.53f1', 'commit': source,
                         'working_tree_has_local_changes': False, 'native_unity_validation': False,
                         'file_count': len(files)}, 'ARCHIVE-INFO is not the exact clean source identity')
        for required in ('ProjectSettings/EditorBuildSettings.asset', 'Packages/manifest.json',
                         'ProjectSettings/ProjectVersion.txt'):
            require(PREFIX + required in expected, 'Required Unity project file missing: ' + required)
        require('m_EditorVersion: 2022.3.53f1' in archive.read(PREFIX + 'ProjectSettings/ProjectVersion.txt').decode(), 'Wrong Unity version')
        for name, path in sorted(paths.items()):
            entry = archive.getinfo(PREFIX + name)
            require(not entry.is_dir(), 'Unexpected directory member')
            require((entry.external_attr >> 16) & 0o170000 != 0o120000, 'ZIP symlink refused: ' + name)
            data = archive.read(PREFIX + name)
            require(data == path.read_bytes(), 'Source bytes differ from archive: ' + name)
            require(not data.startswith(LFS_PREFIX), 'Unmaterialized LFS pointer: ' + name)
            committed = git(root, 'show', source + ':' + name, binary=True)
            if committed.startswith(LFS_PREFIX):
                pointer = committed.decode()
                oid = re.search(r'^oid sha256:([0-9a-f]{64})$', pointer, re.MULTILINE)
                size = re.search(r'^size ([0-9]+)$', pointer, re.MULTILINE)
                require(oid is not None and size is not None, 'Malformed LFS pointer in commit: ' + name)
                require(sha(data) == oid.group(1) and len(data) == int(size.group(1)), 'Materialized LFS bytes differ from committed OID/size: ' + name)
                lfs_count += 1
            else:
                require(committed == data, 'Archive differs from committed Git source: ' + name)
            if name.startswith('Assets/'):
                if name.endswith('.meta'):
                    require(name[:-5] in paths or any(n.startswith(name[:-5] + '/') for n in paths), 'Orphan asset .meta: ' + name)
                    guid = re.search(rb'^guid: ([0-9a-f]{32})$', data, re.MULTILINE)
                    require(guid is not None, 'Asset .meta lacks GUID: ' + name)
                    require(guid.group(1) not in guids, 'Duplicate asset GUID: ' + name)
                    guids[guid.group(1)] = name
                else:
                    require(name + '.meta' in paths, 'Asset lacks .meta: ' + name)
                    for ancestor in PurePosixPath(name).parents:
                        if str(ancestor) not in ('.', 'Assets'):
                            require(str(ancestor) + '.meta' in paths, 'Asset folder lacks .meta: ' + str(ancestor))
            manifest.append({'path': name, 'bytes': len(data), 'sha256': sha(data)})
            source_bytes += len(data)
    result = {'verified': True, 'commit': source, 'unity': '2022.3.53f1',
              'zip': str(archive_path), 'zip_bytes': archive_path.stat().st_size,
              'zip_sha256': digest, 'source_file_count': len(files),
              'zip_member_count': len(expected), 'asset_meta_count': len(guids),
              'compared_source_bytes': source_bytes,
              'file_manifest_sha256': sha(json.dumps(manifest, sort_keys=True, separators=(',', ':')).encode()),
              'crc': 'passed', 'exact_file_set': 'passed',
              'source_bytes_and_member_sha256': 'passed', 'commit_bytes_or_lfs_oid': 'passed',
              'lfs_objects_verified': lfs_count, 'lfs_pointers': 'none',
              'cache_or_credential_members': 'none', 'native_unity_validation': False,
              'actual_player_playtest': False}
    result.update(preservation(delivery_root, baseline, preservation_ref))
    return result

def https_verify(args, baseline):
    commit = full_commit(args.delivery_root, args.delivery_commit)
    require(args.download_dir.resolve() != args.archive.parent.resolve(), 'HTTPS download must not overwrite published archives')
    args.download_dir.mkdir(parents=True, exist_ok=True)
    downloads = []
    for local_path in (args.archive, args.archive.with_suffix(args.archive.suffix + '.sha256')):
        name = local_path.name
        remote_git_data = git(args.delivery_root, 'show', commit + ':' + name, binary=True)
        require(remote_git_data == local_path.read_bytes(), 'Publication commit differs from local artifact: ' + name)
        url = 'https://raw.githubusercontent.com/jinze909/2D-RPG-Project/' + commit + '/' + name
        request = urllib.request.Request(url, headers={'User-Agent': 'RPG-Iteration-011-Delivery-Verification'})
        with urllib.request.urlopen(request, timeout=60) as response:
            require(response.status == 200, 'HTTPS response not successful')
            data = response.read()
            require(response.url.startswith('https://raw.githubusercontent.com/'), 'Unexpected HTTPS redirect')
        require(data == local_path.read_bytes(), 'Downloaded publication bytes differ: ' + name)
        download_path = args.download_dir / name
        require(not download_path.exists(), 'Refusing to overwrite existing download evidence: ' + str(download_path))
        download_path.write_bytes(data)
        downloads.append({'url': url, 'http_status': 200, 'bytes': len(data),
                          'sha256': sha(data), 'matches_published_local_bytes': True})
    result = verify(args.root, args.download_dir / args.archive.name, args.source,
                    args.delivery_root, baseline, commit)
    return {'https_download': downloads, 'https_redownload_verification': result,
            'delivery_binary_commit': commit, 'native_unity_validation': False}

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('operation', choices=('snapshot', 'verify', 'https'))
    parser.add_argument('--root', type=Path, default=SOURCE_ROOT)
    parser.add_argument('--delivery-root', type=Path, default=DELIVERY_ROOT)
    parser.add_argument('--source')
    parser.add_argument('--archive', type=Path)
    parser.add_argument('--baseline', type=Path)
    parser.add_argument('--preservation-ref', default='HEAD')
    parser.add_argument('--delivery-commit')
    parser.add_argument('--download-dir', type=Path)
    parser.add_argument('--output', required=True, type=Path)
    args = parser.parse_args()
    if args.operation == 'snapshot':
        result = snapshot(args.delivery_root)
    else:
        require(args.source and args.archive and args.baseline, 'source/archive/baseline required')
        baseline = json.loads(args.baseline.read_text())
        if args.operation == 'verify':
            result = verify(args.root, args.archive, args.source, args.delivery_root, baseline, args.preservation_ref)
        else:
            require(args.delivery_commit and args.download_dir, 'delivery-commit/download-dir required')
            result = https_verify(args, baseline)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps(result, indent=2))

if __name__ == '__main__':
    main()
