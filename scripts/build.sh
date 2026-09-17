#!/usr/bin/env sh
set -eu

if [ "$#" -ne 2 ]; then
  echo "usage: $0 VALHEIM_MANAGED_DIR BEPINEX_CORE_DIR" >&2
  exit 2
fi

project_dir=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
mkdir -p "$project_dir/lib"
cp "$2/BepInEx.dll" "$project_dir/lib/BepInEx.dll"
cp "$2/Mono.Cecil.dll" "$project_dir/lib/Mono.Cecil.dll"

dotnet build "$project_dir/src/RagnavikCompat.Patcher/RagnavikCompat.Patcher.csproj" \
  --configuration Release

dotnet build "$project_dir/src/RagnavikCompat/RagnavikCompat.csproj" \
  --configuration Release \
  -p:ValheimManagedDir="$1" \
  -p:BepInExCoreDir="$2"
