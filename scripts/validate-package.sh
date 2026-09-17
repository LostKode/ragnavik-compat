#!/usr/bin/env sh
set -eu

if [ "$#" -ne 1 ]; then
  echo "usage: $0 PACKAGE_ZIP" >&2
  exit 2
fi

python3 - "$1" <<'PY'
from pathlib import Path
import sys
import zipfile

archive = Path(sys.argv[1])
required = {
    "manifest.json",
    "icon.png",
    "README.md",
    "CHANGELOG.md",
    "patchers/RagnavikCompat.Patcher.dll",
    "plugins/RagnavikCompat/RagnavikCompat.dll",
}
if not archive.is_file():
    raise SystemExit(f"missing package archive: {archive}")
with zipfile.ZipFile(archive) as package:
    corrupt = package.testzip()
    if corrupt:
        raise SystemExit(f"corrupt package entry: {corrupt}")
    names = set(package.namelist())
missing = required - names
if missing:
    raise SystemExit(f"missing package entries: {sorted(missing)}")
forbidden = [
    name for name in names
    if any(part in {"bin", "obj", "references", "node_modules"} for part in Path(name).parts)
    or name.endswith((".pdb", ".zip"))
]
if forbidden:
    raise SystemExit(f"forbidden package entries: {sorted(forbidden)}")
print(f"package layout valid: {archive}")
PY
