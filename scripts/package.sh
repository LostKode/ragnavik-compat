#!/usr/bin/env sh
set -eu

project_dir=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
version=$(python3 -c 'import json,sys; print(json.load(open(sys.argv[1]))["version_number"])' "$project_dir/package/manifest.json")
runtime_dll="$project_dir/src/RagnavikCompat/bin/Release/netstandard2.1/RagnavikCompat.dll"
stage="$project_dir/artifacts/package"
archive="$project_dir/artifacts/LostKode-Ragnavik_Compatibility-$version.zip"

for dll in "$runtime_dll"; do
  if [ ! -f "$dll" ]; then
    echo "missing release DLL: $dll; run scripts/build.sh first" >&2
    exit 1
  fi
done

rm -rf "$stage"
mkdir -p "$stage/plugins/RagnavikCompat"
mkdir -p "$stage/config/EpicMMOSystem"
cp "$project_dir/package/manifest.json" "$stage/manifest.json"
cp "$project_dir/package/icon.png" "$stage/icon.png"
cp "$project_dir/README.md" "$stage/README.md"
cp "$project_dir/CHANGELOG.md" "$stage/CHANGELOG.md"
cp "$runtime_dll" "$stage/plugins/RagnavikCompat/RagnavikCompat.dll"
cp "$project_dir/package/config/EpicMMOSystem/Ragnavik_AddedCreatures.json" "$stage/config/EpicMMOSystem/Ragnavik_AddedCreatures.json"

rm -f "$archive"
python3 - "$stage" "$archive" <<'PY'
from pathlib import Path
import sys
import zipfile

stage = Path(sys.argv[1])
archive = Path(sys.argv[2])
with zipfile.ZipFile(archive, "w", compression=zipfile.ZIP_DEFLATED) as output:
    for path in sorted(item for item in stage.rglob("*") if item.is_file()):
        output.write(path, path.relative_to(stage).as_posix())
PY
echo "$archive"
