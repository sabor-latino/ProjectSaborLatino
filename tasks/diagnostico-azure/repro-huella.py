"""Diagnóstico de solo lectura: huellas de blobs Git, sin modificar el workflow/SQL."""
import hashlib
import json
from pathlib import PurePosixPath, PureWindowsPath
import re
import subprocess


def git(*arguments):
    return subprocess.check_output(['git', *arguments])


def inspect(ref):
    files = [name for name in git('ls-tree', '-r', '--name-only', ref, 'backendSL/Migrations')
             .decode().splitlines() if name.endswith('.cs')]
    contents = {name: git('show', f'{ref}:{name}').decode('utf-8-sig')
                .replace('\r\n', '\n').replace('\r', '\n') for name in files}
    workflow = git('show', f'{ref}:.github/workflows/deploy-azure.yml').decode('utf-8-sig')
    expected = re.search(r"expected = '([0-9a-f]{64})'", workflow).group(1)
    variants = []
    for flavour, path_class in [('posix', PurePosixPath), ('windows', PureWindowsPath)]:
        ordered = sorted(files, key=path_class)
        for separator_name, separator in [('NUL', bytes([0])), ('literal_backslash_zero', bytes([92, 48]))]:
            digest = hashlib.sha256()
            for name in ordered:
                digest.update(name.encode() + separator)
                digest.update(contents[name].encode() + separator)
            value = digest.hexdigest()
            variants.append({'ordering': flavour, 'separator': separator_name,
                             'hash': value, 'matches_expected': value == expected})
    return {'ref': ref, 'migration_tree': git('rev-parse', f'{ref}:backendSL/Migrations').decode().strip(),
            'files': len(files), 'migrations': sum(not name.endswith(('.Designer.cs', 'ModelSnapshot.cs')) for name in files),
            'expected': expected, 'variants': variants,
            'first_windows': sorted(files, key=PureWindowsPath)[:2],
            'first_posix': sorted(files, key=PurePosixPath)[:2]}


if __name__ == '__main__':
    print(json.dumps([inspect('5428206e2008e2c02cd000deb4f3910bf361d860'),
                      inspect('14a50422725a9254db0f89bf27fa8e902e07b547')], indent=2))
