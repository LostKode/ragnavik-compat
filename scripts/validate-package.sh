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
if len(creatures) != 37:
    raise SystemExit(f"expected 37 Ragnavik creature mappings, found {len(creatures)}")
creature_names = [entry.get("name") for entry in creatures]
if len(set(creature_names)) != len(creature_names):
    raise SystemExit("Ragnavik creature mappings contain duplicate prefab names")
for entry in creatures:
    if set(entry) != {"name", "minExp", "maxExp", "level"}:
        raise SystemExit(f"invalid creature mapping fields: {entry}")
    if not isinstance(entry["name"], str) or not entry["name"]:
        raise SystemExit(f"invalid creature prefab name: {entry}")
    if not all(isinstance(entry[key], int) for key in ("minExp", "maxExp", "level")):
        raise SystemExit(f"non-integer creature mapping: {entry}")
    if entry["minExp"] < 0 or entry["maxExp"] < entry["minExp"] or entry["level"] < 0:
        raise SystemExit(f"invalid creature progression values: {entry}")
print(f"package layout valid: {archive}")
PY
