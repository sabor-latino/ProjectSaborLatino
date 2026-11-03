"""Verify reviewed migration sources before publishing; never connect to a database."""
import argparse
import hashlib
from pathlib import Path
import re
import sys


def fingerprint(root: Path) -> str:
    """Hash migration names and normalized UTF-8 sources, including the snapshot."""
    paths = sorted(
        (path for path in (root / 'backendSL' / 'Migrations').rglob('*')
         if path.is_file() and path.suffix.lower() == '.cs'),
        key=lambda path: path.relative_to(root).as_posix(),
    )
    if not paths:
        raise ValueError('No migration source files found in backendSL/Migrations.')
    digest = hashlib.sha256()
    for path in paths:
        name = path.relative_to(root).as_posix()
        content = path.read_text(encoding='utf-8-sig').replace('\r\n', '\n').replace('\r', '\n')
        digest.update(name.encode('utf-8') + bytes([0]))
        digest.update(content.encode('utf-8') + bytes([0]))
    return digest.hexdigest()


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument('--baseline', type=Path)
    args = parser.parse_args()
    reviewed = args.baseline or args.root / '.github' / 'azure-sql-baseline.sha256'
    try:
        expected = reviewed.read_text(encoding='utf-8-sig').strip()
        if not re.fullmatch('[0-9a-f]{64}', expected):
            raise ValueError('Reviewed baseline must contain exactly 64 lowercase hexadecimal characters.')
        actual = fingerprint(args.root)
        if actual != expected:
            raise ValueError(
                'Azure SQL migration baseline changed. Apply/review the production migration separately '
                'and update this baseline before deploying. Never use personal LocalDB. '
                f'Expected {expected}; actual {actual}.'
            )
    except (OSError, UnicodeError, ValueError) as error:
        print(error, file=sys.stderr)
        return 1
    print(f'Azure SQL source fingerprint {actual} matches the reviewed baseline.')
    return 0


if __name__ == '__main__':
    sys.exit(main())
