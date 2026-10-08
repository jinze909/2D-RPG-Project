"""Exercise source delivery boundaries using real project assets."""
import importlib.util
from pathlib import Path
import shutil
import tempfile
import unittest
import zipfile

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('package_unity_project', ROOT / 'tools/package_unity_project.py')
packager = importlib.util.module_from_spec(spec)
spec.loader.exec_module(packager)

class DistributionChecks(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='rpg-distribution-check-')
        self.root = Path(self.temp.name) / 'project'
        self.root.mkdir()
        for name in ('Assets', 'Packages', 'ProjectSettings'):
            shutil.copytree(ROOT / name, self.root / name)

    def tearDown(self):
        self.temp.cleanup()

    def test_complete_archive_contains_real_png_meta_and_settings(self):
        target = Path(self.temp.name) / 'project.zip'
        packager.package(self.root, target)
        with zipfile.ZipFile(target) as archive:
            prefix = '2D-RPG-Project/'
            self.assertEqual(archive.read(prefix + 'Assets/Actions/Acts/ViolaMoves.png'),
                             (ROOT / 'Assets/Actions/Acts/ViolaMoves.png').read_bytes())
            self.assertIn(prefix + 'Assets/Actions/Acts/ViolaMoves.png.meta', archive.namelist())
            self.assertIn(prefix + 'Packages/packages-lock.json', archive.namelist())
            self.assertIn(prefix + 'ProjectSettings/EditorBuildSettings.asset', archive.namelist())
            self.assertIsNone(archive.testzip())
        self.assertTrue(target.with_suffix('.zip.sha256').is_file())

    def test_caches_and_home_credentials_do_not_enter_archive(self):
        for name in ('Library', 'Temp', 'Logs', '.git', '.ssh'):
            path = self.root / name
            path.mkdir()
            (path / 'never-package.txt').write_text('fixture only')
        cache = self.root / 'Assets/__pycache__'
        cache.mkdir()
        (cache / 'generated.pyc').write_bytes(b'fixture')
        names = [path.relative_to(self.root).as_posix() for path in packager.files_for_archive(self.root)]
        self.assertFalse(any('never-package' in name or '__pycache__' in name for name in names))

    def test_secret_file_inside_required_tree_is_refused(self):
        (self.root / 'Assets/.env').write_text('fixture only; not a credential')
        with self.assertRaisesRegex(ValueError, 'Credential'):
            packager.files_for_archive(self.root)

    def test_lfs_pointer_cannot_be_delivered_as_image(self):
        (self.root / 'Assets/Actions/Acts/ViolaMoves.png').write_text(
            'version https://git-lfs.github.com/spec/v1\noid sha256:' + '0' * 64 + '\nsize 100\n')
        with self.assertRaisesRegex(ValueError, 'LFS'):
            packager.files_for_archive(self.root)

    def test_symlink_cannot_read_files_outside_project(self):
        outside = Path(self.temp.name) / 'outside.txt'
        outside.write_text('fixture only')
        (self.root / 'Assets/linked.txt').symlink_to(outside)
        with self.assertRaisesRegex(ValueError, 'Symlink'):
            packager.files_for_archive(self.root)

    def test_required_unity_directory_must_exist(self):
        shutil.rmtree(self.root / 'Packages')
        with self.assertRaisesRegex(ValueError, 'required Unity'):
            packager.files_for_archive(self.root)

if __name__ == '__main__':
    unittest.main()
