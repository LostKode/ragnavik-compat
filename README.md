# Ragnavik Compatibility

Ragnavik Compatibility contains independently guarded fixes between mods in the Ragnavik Valheim modpack and narrow temporary fixes for individual mods until upstream releases resolve them.

## Modules

### Epic Loot and MagicPlugin Take All bridge

When Epic Loot and MagicPlugin are both installed, the runtime plugin moves all container contents through Valheim's original-item transfer path instead of its coordinate-preserving bulk clone path. This preserves their item data instead of cloning the item into its old container coordinate. Mod version changes do not disable the bridge; it remains guarded by Valheim's expected `Inventory.MoveAll(Inventory)` signature.

The module remains inactive when either mod is absent or when Valheim's expected `Inventory.MoveAll(Inventory)` signature is missing.

### CurrencyPocket Take All bridge

When CurrencyPocket 1.0.15 is installed on the client, coins transferred with Take All go directly into the local player's coin pouch. Other valuables and ordinary items continue through the normal Take All transfer path.

The module remains inactive when CurrencyPocket is absent, when its version differs, or when its expected balance and UI methods change.

### EpicMMO added creature coverage

The package installs an additional EpicMMO creature database for the creature prefabs currently supplied by SeaAnimals 0.3.8, OdinBear 1.4.9, OdinHorse 1.7.2, OdinMounts 0.1.5, BottledNeck 0.3.3, BeeQueen 1.1.2, and GoodestBoy 0.3.7. Levels follow each creature's configured biome and combat strength. Wild creatures award experience, while companion creatures and offspring use zero experience values to prevent farming pets for progression.

The database uses exact prefab names from the installed assemblies and remains harmless when an owning mod is absent. Review and update the mappings whenever one of those creature mods changes version.

### Afterdeath starvation guard

When Afterdeath 1.0.10 and Starvation 1.0.5 are both installed, the runtime plugin prevents Starvation's health drain routine from running while a player is an Afterdeath spirit. Normal starvation resumes after resurrection.

The module remains inactive when either mod is absent, when either version differs, or when Starvation's expected `DamagePlayer.Prefix(Player, float, bool)` signature is missing.

### Afterdeath spirit travel bridge

When Afterdeath 1.0.10 is installed, spirits can use Valheim teleport transitions. This allows corpse recovery through dungeon entrances and permits ordinary portal travel while in spirit form. Afterdeath continues to control resurrection, spirit movement, exploration, combat, and other interactions.

The module remains inactive when Afterdeath is absent, when its version differs, or when its expected `DisableTeleport.Prefix()` signature changes.

### Afterdeath nearest-bed spawn

When Afterdeath 1.0.10 is installed and a player has a valid bed spawn, the runtime plugin compares that bed with Afterdeath's nearest Skathi using horizontal distance from the death point. The player respawns at the bed only when it is strictly closer; equal distances continue to use Skathi.

The module remains inactive when Afterdeath is absent, when its version differs, or when its expected `Utils.GetClosestLocation(Vector3)` signature changes. Invalid or destroyed bed spawns continue through Valheim's normal validation and do not replace Skathi.

### EpicMMO reload guard

The runtime plugin includes the JSON reload guard migrated from `ragnavik-epicmmo-reload-guard`. On a dedicated server with WackyEpicMMOSystem 1.9.68 present, it coalesces noisy watcher events, ignores metadata only changes, and permits one reload when JSON content or paths actually change. It watches the existing `BepInEx/config/EpicMMOSystem` JSON tree and does not introduce a separate configuration file.

The module remains inactive on clients, when EpicMMO is absent, when its version is not exactly 1.9.68, or when the expected `ReadJsonValues(sender, e)` watcher signature is missing.

## Installation

Install the package on both clients and servers as part of the Ragnavik modpack. The archive places:

* `RagnavikCompat.dll` in `BepInEx/plugins/RagnavikCompat`

The affected mods are optional integrations and are therefore not declared as hard Thunderstore dependencies.

The package has one shared plugin identity, `lostkode.ragnavik.compat`. Version check that GUID on clients and servers. The retired standalone server-only GUID, `LostKode.RagnavikEpicMMOReloadGuard`, must not remain in the effective server manifest or `CatosAntiCheat_ServerOnly.txt` after pack migration.

## Upstream update policy

Before updating a mod covered by this package, review its release notes and changelog for an upstream fix or relevant API change. Revalidate the guarded signature and behavior against the exact candidate DLL. Remove or disable the local module when the upstream correction is verified so both fixes never run together.

See [release instructions](docs/RELEASING.md) before producing or publishing a package.
