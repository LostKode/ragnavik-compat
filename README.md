# Ragnavik Compatibility

Ragnavik Compatibility contains independently guarded fixes between mods in the Ragnavik Valheim modpack and narrow temporary fixes for individual mods until upstream releases resolve them.

## Modules

### Epic Loot and MagicPlugin Take All bridge

When Epic Loot 0.14.10 and MagicPlugin 2.2.0 are both installed, the runtime plugin moves all container contents through Valheim's original-item transfer path instead of its coordinate-preserving bulk clone path. This preserves their item data instead of cloning the item into its old container coordinate.

The module remains inactive when either mod is absent, when either version differs, or when Valheim's expected `Inventory.MoveAll(Inventory)` signature is missing.

### Afterdeath starvation guard

When Afterdeath 1.0.10 and Starvation 1.0.5 are both installed, the runtime plugin prevents Starvation's health drain routine from running while a player is an Afterdeath spirit. Normal starvation resumes after resurrection.

The module remains inactive when either mod is absent, when either version differs, or when Starvation's expected `DamagePlayer.Prefix(Player, float, bool)` signature is missing.

### EpicMMO reload guard

The runtime plugin includes the JSON reload guard migrated from `ragnavik-epicmmo-reload-guard`. On a dedicated server with WackyEpicMMOSystem 1.9.67 present, it coalesces noisy watcher events, ignores metadata only changes, and permits one reload when JSON content or paths actually change. It watches the existing `BepInEx/config/EpicMMOSystem` JSON tree and does not introduce a separate configuration file.

The module remains inactive on clients, when EpicMMO is absent, when its version is not exactly 1.9.67, or when the expected `ReadJsonValues(sender, e)` watcher signature is missing.

## Installation

Install the package on both clients and servers as part of the Ragnavik modpack. The archive places:

* `RagnavikCompat.dll` in `BepInEx/plugins/RagnavikCompat`

The affected mods are optional integrations and are therefore not declared as hard Thunderstore dependencies.

The package has one shared plugin identity, `lostkode.ragnavik.compat`. Version check that GUID on clients and servers. The retired standalone server-only GUID, `LostKode.RagnavikEpicMMOReloadGuard`, must not remain in the effective server manifest or `CatosAntiCheat_ServerOnly.txt` after pack migration.

## Upstream update policy

Before updating a mod covered by this package, review its release notes and changelog for an upstream fix or relevant API change. Revalidate the guarded signature and behavior against the exact candidate DLL. Remove or disable the local module when the upstream correction is verified so both fixes never run together.

See [release instructions](docs/RELEASING.md) before producing or publishing a package.
