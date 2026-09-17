# Ragnavik Compatibility

Ragnavik Compatibility contains independently guarded fixes between mods in the Ragnavik Valheim modpack and narrow temporary fixes for individual mods until upstream releases resolve them.

## Modules

### MagicRevamp ItemDataManager bridge

The preloader restores the removed four parameter `Inventory.AddItem(ItemData, int, int, int)` overload required by the `ItemDataManager` library bundled with MagicRevamp 1.5.0. It forwards to Valheim 1.0's current five parameter method and supplies the `ZLog.Log` call expected by the legacy Harmony transpiler.

The bridge skips itself if the legacy overload already exists or if the current target API cannot be verified.

### EpicMMO reload guard

The runtime plugin includes the JSON reload guard migrated from `ragnavik-epicmmo-reload-guard`. On a dedicated server with EpicMMO present, it coalesces noisy watcher events, ignores metadata only changes, and permits one reload when JSON content or paths actually change.

The module remains inactive on clients, when EpicMMO is absent, or when the expected `ReadJsonValues(sender, e)` watcher signature is missing.

## Installation

Install the package on both clients and servers as part of the Ragnavik modpack. The archive places:

* `RagnavikCompat.Patcher.dll` in `BepInEx/patchers`
* `RagnavikCompat.dll` in `BepInEx/plugins/RagnavikCompat`

The affected mods are optional integrations and are therefore not declared as hard Thunderstore dependencies.

## Upstream update policy

Before updating a mod covered by this package, review its release notes and changelog for an upstream fix or relevant API change. Revalidate the guarded signature and behavior against the exact candidate DLL. Remove or disable the local module when the upstream correction is verified so both fixes never run together.

See [release instructions](docs/RELEASING.md) before producing or publishing a package.
