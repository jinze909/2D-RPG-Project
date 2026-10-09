#!/usr/bin/env python3
"""Validate real Unity project data and C# behavior without claiming native play."""
from __future__ import annotations
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys

TRUSTED_ROOT = Path(__file__).resolve().parents[1]

def validate(root: Path) -> dict:
    checks = []
    def check(name, operation):
        try:
            detail = operation()
            checks.append({'name': name, 'status': 'passed', **(detail or {})})
        except Exception as error:
            checks.append({'name': name, 'status': 'failed', 'error': str(error)})

    def version():
        first = (root / 'ProjectSettings/ProjectVersion.txt').read_text().splitlines()[0]
        if first != 'm_EditorVersion: 2022.3.53f1':
            raise ValueError('Required editor is international Unity 2022.3.53f1')
        return {'editor': '2022.3.53f1'}

    def assets():
        from PIL import Image
        guids, images, meta_count, references = {}, 0, 0, 0
        entries = sorted((root / 'Assets').rglob('*'))
        for path in entries:
            if path.is_symlink():
                raise ValueError('Asset symlink is not accepted: ' + str(path.relative_to(root)))
            if not path.name.endswith('.meta'):
                if not Path(str(path) + '.meta').is_file():
                    raise ValueError('Missing .meta: ' + str(path.relative_to(root)))
                if path.is_file():
                    with path.open('rb') as handle:
                        if handle.read(43).startswith(b'version https://git-lfs.github.com/spec/v1'):
                            raise ValueError('Unmaterialized LFS asset: ' + str(path.relative_to(root)))
            else:
                if not Path(str(path)[:-5]).exists():
                    raise ValueError('Orphan .meta: ' + str(path.relative_to(root)))
                match = re.search(r'^guid: ([0-9a-f]{32})$', path.read_text(), re.MULTILINE)
                if not match:
                    raise ValueError('Missing/invalid Unity GUID: ' + str(path.relative_to(root)))
                if match[1] in guids:
                    raise ValueError('Duplicate Unity GUID: ' + match[1])
                guids[match[1]] = str(path.relative_to(root))
                meta_count += 1
            if path.is_file() and path.suffix.lower() == '.png':
                with Image.open(path) as image:
                    image.verify()
                images += 1
        if not images or not meta_count:
            raise ValueError('No real image/meta assets were checked')
        for path in entries:
            if path.is_file() and path.suffix in {'.unity', '.asset', '.anim', '.controller', '.prefab'}:
                for guid in re.findall(r'guid: ([0-9a-f]{32})', path.read_text()):
                    if guid.startswith('0000000000000000'):
                        continue  # Unity built-in resources, not project GUIDs.
                    if guid not in guids:
                        raise ValueError('Unresolved project GUID in ' + str(path.relative_to(root)) + ': ' + guid)
                    references += 1
        return {'pngs_decoded': images, 'unique_meta_guids': meta_count,
                'project_guid_references': references}

    def scenes():
        settings = (root / 'ProjectSettings/EditorBuildSettings.asset').read_text()
        enabled = re.findall(r'- enabled: 1\s+path: ([^\n]+)\s+guid: ([0-9a-f]{32})', settings)
        if not enabled:
            raise ValueError('No enabled build scene')
        for name, guid in enabled:
            path = root / name.strip()
            if not path.is_file() or not Path(str(path) + '.meta').is_file():
                raise ValueError('Missing enabled build scene: ' + name)
            actual = re.search(r'^guid: ([0-9a-f]{32})$', Path(str(path) + '.meta').read_text(), re.MULTILINE)
            if not actual or actual[1] != guid:
                raise ValueError('Build scene GUID differs from .meta: ' + name)
        json.loads((root / 'Packages/manifest.json').read_text())
        json.loads((root / 'Packages/packages-lock.json').read_text())
        for path in (root / 'Assets').rglob('*.inputactions'):
            json.loads(path.read_text())
        return {'enabled_build_scenes': len(enabled)}

    def lfs():
        result = subprocess.run(['git', '-C', str(root), 'lfs', 'ls-files', '--json'],
                                text=True, capture_output=True, check=True)
        records = json.loads(result.stdout)
        records = records['files'] if isinstance(records, dict) else records
        for record in records:
            digest = hashlib.sha256((root / record['name']).read_bytes()).hexdigest()
            if digest != record['oid']:
                raise ValueError('LFS OID mismatch: ' + record['name'])
        return {'lfs_objects_verified': len(records)}

    def skills():
        manifest = json.loads((root / 'docs/skills-source-manifest.json').read_text())
        count = 0
        for skill in manifest['skills']:
            entry = root / skill['path']
            if not entry.is_file():
                raise ValueError('Missing skill entry: ' + skill['name'])
            for name, digest in skill['files_sha256'].items():
                path = entry.parent / name
                if path.is_symlink() or hashlib.sha256(path.read_bytes()).hexdigest() != digest:
                    raise ValueError('Vendored skill hash differs: ' + skill['name'] + '/' + name)
                count += 1
        if len(manifest['skills']) != 8:
            raise ValueError('The eight reviewed RPG skills must remain available')
        return {'skill_bundles': 8, 'skill_files_verified': count}

    def behavior(script, kind='real C# with Unity boundary doubles; not native Unity'):
        # Always execute the validator's own trusted baseline runner/fixtures.
        process = subprocess.run([sys.executable, str(TRUSTED_ROOT / 'tools' / script),
                                  '--project-root', str(root)], text=True, capture_output=True)
        output = process.stdout + process.stderr
        print(output, end='', flush=True)
        passed = len(re.findall(r'^PASS\b', output, re.MULTILINE))
        failed = len(re.findall(r'^FAIL\b', output, re.MULTILINE))
        if process.returncode or failed or not passed:
            raise ValueError(f'{script}: exit={process.returncode}, passed={passed}, failed={failed}')
        return {'passed_tests': passed, 'failed_tests': failed,
                'validation_kind': kind}

    def contracts(script):
        process = subprocess.run([sys.executable, str(TRUSTED_ROOT / 'tools' / script),
                                  '--project-root', str(root)], text=True, capture_output=True)
        output = process.stdout + process.stderr
        print(output, end='', flush=True)
        count = re.search(r'Ran (\d+) tests?', output)
        if process.returncode or not count or int(count[1]) == 0:
            raise ValueError(f'{script}: exit={process.returncode}, no verified positive test count')
        return {'passed_tests': int(count[1]), 'failed_tests': 0,
                'validation_kind': 'serialized data / geometry checks; not native Unity'}

    check('editor_version', version)
    check('asset_and_guid_integrity', assets)
    check('build_scenes_and_json', scenes)
    check('lfs_sha256', lfs)
    check('portable_skills_integrity', skills)
    check('player_movement_behavior', lambda: behavior('run_player_checks.py'))
    check('player_resource_behavior', lambda: behavior('run_resource_checks.py'))
    check('input_and_animation_contracts', lambda: contracts('run_input_animation_checks.py'))
    check('clearing_combat_rules', lambda: behavior('run_gameplay_checks.py', 'actual pure C# rules; no Unity engine'))
    check('clearing_progression_and_persistence', lambda: behavior('run_progression_checks.py',
          'actual pure C# progression/codec plus real Mono temporary-filesystem IO; no Unity engine'))
    check('clearing_scene_contracts', lambda: contracts('run_scene_checks.py'))
    check('clearing_presentation_behavior', lambda: behavior('run_presentation_checks.py',
          'actual C# with recording component/audio boundaries; no native physics, UI layout or audible output'))
    check('clearing_pixel_art', lambda: behavior('run_art_checks.py',
          'actual managed C# pixel arrays and original asset hashes; no native rendering'))
    try:
        base_sha = subprocess.check_output(['git', '-C', str(root), 'rev-parse', 'HEAD'], text=True).strip()
    except subprocess.CalledProcessError:
        base_sha = None
    passed = all(item['status'] == 'passed' for item in checks)
    return {'status': 'passed' if passed else 'failed', 'passed': passed,
            'base_sha': base_sha, 'native_unity_run': False, 'checks': checks,
            'not_run': ['Unity import/compile', 'native physics', 'Animator rendering',
                        'EditMode/PlayMode', 'audio/playability', 'platform build']}

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, default=TRUSTED_ROOT)
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    report = validate(args.root.resolve())
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n')
    print(json.dumps(report, ensure_ascii=False, indent=2))
    return 0 if report['passed'] else 1

if __name__ == '__main__':
    raise SystemExit(main())
