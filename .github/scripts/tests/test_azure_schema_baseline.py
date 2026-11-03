"""Regressions for the reviewed Azure SQL source fingerprint; no database access."""
import hashlib
import importlib.util
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest


SCRIPT = Path(__file__).resolve().parents[1] / 'azure_schema_baseline.py'
SPEC = importlib.util.spec_from_file_location('azure_schema_baseline', SCRIPT)
baseline = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(baseline)


class AzureSchemaBaselineTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.migrations = self.root / 'backendSL' / 'Migrations'
        self.migrations.mkdir(parents=True)
        self.write('InitialCreate.cs', b'public class InitialCreate {}\n')
        self.write('InitialCreate.Designer.cs', b'// designer\n')
        self.write('AppModelSnapshot.cs', b'// snapshot\n')

    def write(self, name, content):
        (self.migrations / name).write_bytes(content)

    def test_uses_posix_string_order_and_single_nul_separators(self):
        # Explicit byte stream is independent of the production sorting function.
        expected = hashlib.sha256(
            b'backendSL/Migrations/AppModelSnapshot.cs\x00// snapshot\n\x00'
            b'backendSL/Migrations/InitialCreate.Designer.cs\x00// designer\n\x00'
            b'backendSL/Migrations/InitialCreate.cs\x00public class InitialCreate {}\n\x00'
        ).hexdigest()
        self.assertEqual(expected, baseline.fingerprint(self.root))

    def test_bom_and_line_endings_are_equivalent(self):
        self.write('InitialCreate.cs', b'first\nsecond\n')
        expected = baseline.fingerprint(self.root)
        for content in [b'\xef\xbb\xbffirst\nsecond\n', b'first\r\nsecond\r\n', b'first\rsecond\r']:
            with self.subTest(content=content):
                self.write('InitialCreate.cs', content)
                self.assertEqual(expected, baseline.fingerprint(self.root))

    def test_added_migration_changes_fingerprint(self):
        original = baseline.fingerprint(self.root)
        self.write('SecondMigration.cs', b'// new migration\n')
        self.assertNotEqual(original, baseline.fingerprint(self.root))

    def test_nested_migration_changes_fingerprint(self):
        original = baseline.fingerprint(self.root)
        nested = self.migrations / 'Nested'
        nested.mkdir()
        (nested / 'SecondMigration.cs').write_bytes(b'// nested migration\n')
        self.assertNotEqual(original, baseline.fingerprint(self.root))

    def test_uppercase_source_extension_is_included(self):
        # Include the name exactly as stored, regardless of host glob case rules.
        expected = hashlib.sha256(
            b'backendSL/Migrations/AppModelSnapshot.cs\x00// snapshot\n\x00'
            b'backendSL/Migrations/InitialCreate.Designer.cs\x00// designer\n\x00'
            b'backendSL/Migrations/InitialCreate.cs\x00public class InitialCreate {}\n\x00'
            b'backendSL/Migrations/SecondMigration.CS\x00// uppercase extension\n\x00'
        ).hexdigest()
        self.write('SecondMigration.CS', b'// uppercase extension\n')
        self.assertEqual(expected, baseline.fingerprint(self.root))

    def test_renamed_migration_changes_fingerprint(self):
        original = baseline.fingerprint(self.root)
        (self.migrations / 'InitialCreate.cs').rename(self.migrations / 'RenamedMigration.cs')
        self.assertNotEqual(original, baseline.fingerprint(self.root))

    def test_deleted_migration_changes_fingerprint(self):
        original = baseline.fingerprint(self.root)
        (self.migrations / 'InitialCreate.cs').unlink()
        self.assertNotEqual(original, baseline.fingerprint(self.root))

    def test_changed_source_designer_and_snapshot_change_fingerprint(self):
        for name in ['InitialCreate.cs', 'InitialCreate.Designer.cs', 'AppModelSnapshot.cs']:
            with self.subTest(name=name):
                original_content = (self.migrations / name).read_bytes()
                original = baseline.fingerprint(self.root)
                self.write(name, original_content + b'// schema change\n')
                self.assertNotEqual(original, baseline.fingerprint(self.root))
                self.write(name, original_content)

    def test_missing_or_empty_migrations_are_rejected(self):
        for path in self.migrations.glob('*.cs'):
            path.unlink()
        with self.assertRaisesRegex(ValueError, 'No migration source files'):
            baseline.fingerprint(self.root)
        self.migrations.rmdir()
        with self.assertRaisesRegex(ValueError, 'No migration source files'):
            baseline.fingerprint(self.root)

    def run_cli(self, expected):
        reviewed = self.root / 'reviewed.sha256'
        reviewed.write_text(expected + '\n', encoding='utf-8')
        return subprocess.run(
            [sys.executable, str(SCRIPT), '--root', str(self.root), '--baseline', str(reviewed)],
            capture_output=True, text=True, check=False,
        )

    def test_cli_accepts_reviewed_fingerprint(self):
        result = self.run_cli(baseline.fingerprint(self.root))
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertIn('matches the reviewed baseline', result.stdout)

    def test_cli_rejects_schema_drift(self):
        result = self.run_cli('0' * 64)
        self.assertEqual(1, result.returncode)
        self.assertIn('Apply/review the production migration separately', result.stderr)

    def test_cli_rejects_invalid_baseline(self):
        result = self.run_cli('not-a-sha256')
        self.assertEqual(1, result.returncode)
        self.assertIn('64 lowercase hexadecimal characters', result.stderr)


if __name__ == '__main__':
    unittest.main()
