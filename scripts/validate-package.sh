#!/usr/bin/env sh
set -eu

if [ "$#" -ne 1 ]; then
  echo "usage: $0 PACKAGE_ZIP" >&2
  exit 2
fi

python3 - "$1" "$(dirname "$0")/.." <<'PY'
from pathlib import Path
import sys
import zipfile

archive = Path(sys.argv[1])
required = {
    "manifest.json",
    "icon.png",
    "README.md",
    "CHANGELOG.md",
    "plugins/RagnavikCompat/RagnavikCompat.dll",
    "config/EpicMMOSystem/Ragnavik_AddedCreatures.json",
}
if not archive.is_file():
    raise SystemExit(f"missing package archive: {archive}")
with zipfile.ZipFile(archive) as package:
    corrupt = package.testzip()
    if corrupt:
        raise SystemExit(f"corrupt package entry: {corrupt}")
    names = set(package.namelist())
    import json
    creatures = json.loads(package.read("config/EpicMMOSystem/Ragnavik_AddedCreatures.json"))
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
sys.path.insert(0, str(Path(sys.argv[2]).resolve() / "scripts"))
from validate_creatures import validate, SNAPSHOT
validate(creatures, json.loads(SNAPSHOT.read_text()))
print(f"package layout valid: {archive}")
PY
