# Ragnavik Compatibility

Ragnavik Compatibility contains independently guarded fixes between mods in the Ragnavik Valheim modpack and narrow temporary fixes for individual mods until upstream releases resolve them.

## Modules

### Flora Collector Foraging XP

For FloraCollector 1.1.4 with Foraging 1.0.11, successful extraction grants one normal Foraging award per item, including partial collectors. Rewards go to the player who collected the items after the owner confirms extraction. Empty, failed, and repeated extraction attempts do not grant XP. This replaces the upstream full-collector interaction reward.

Awards use Foraging's normal skill path and XP setting. With Ragnavik Gameplay 1.0.5, selected professions receive 100% and unselected professions receive 50%. This bridge does not apply an additional multiplier. Install the matched compatibility and gameplay versions on all clients and the server during an authorized release.

### Cart bumper signs

With CraftyCarts 3.2.3 and BoardersBumperBlurbs 1.0.3 installed, cart signs use their owning cart network view regardless of patch order. Converted CraftyCarts sign text uses the vanilla sign font before activation. This module disables itself for other mod versions or changed APIs.


### ItemDrawers custom-data bridge

When kg.ItemDrawers is installed and exposes the expected drawer API, cooked food, magic reagents, upgrades, and other items with custom data can be deposited and withdrawn without losing their per-item metadata. Custom-data records are persisted on the drawer ZDO and restored exactly when withdrawn. Automatic drawer pickup leaves custom-data drops on the ground so it cannot route them through ItemDrawers original lossy bulk path.

The module follows the package GUID and runtime API contract instead of an exact version. It remains active across compatible ItemDrawers updates and disables only itself when the required methods or properties change.


### Epic Loot and MagicPlugin Take All bridge

When Epic Loot and MagicPlugin are both installed, the runtime plugin moves all container contents through Valheim's original-item transfer path instead of its coordinate-preserving bulk clone path. This preserves their item data instead of cloning the item into its old container coordinate. Mod version changes do not disable the bridge; it remains guarded by Valheim's expected `Inventory.MoveAll(Inventory)` signature.

The module remains inactive when either mod is absent or when Valheim's expected `Inventory.MoveAll(Inventory)` signature is missing.

### CurrencyPocket Take All bridge

When CurrencyPocket 1.0.15 is installed on the client, coins transferred with Take All go directly into the local player's coin pouch. Other valuables and ordinary items continue through the normal Take All transfer path.

The module remains inactive when CurrencyPocket is absent, when its version differs, or when its expected balance and UI methods change.

### EpicMMO added creature coverage

The package installs an additional EpicMMO creature database for the creature prefabs currently supplied by SeaAnimals 0.3.8, OdinBear 1.4.9, OdinHorse 1.7.2, OdinMounts 0.1.5, BottledNeck 0.3.3, BeeQueen 1.1.2, and GoodestBoy 0.3.7. Levels follow each creature's configured biome and combat strength. Wild creatures award experience, while companion creatures and offspring use zero experience values to prevent farming pets for progression.

The supplement also covers Valheim 1.0 biome skeletons, wild roots, sleeping variants, frost creatures, boss phases and summons. See [the creature audit](docs/CREATURE_LEVELS.md) for levels, rewards and exclusions.

The database uses exact prefab names from the installed assemblies and remains harmless when an owning mod is absent. Review and update the mappings whenever one of those creature mods changes version.

### Afterdeath starvation guard

When Afterdeath 1.0.11 and Starvation 1.0.5 are both installed, the runtime plugin prevents Starvation's health drain routine from running while a player is an Afterdeath spirit. Normal starvation resumes after resurrection.

The module remains inactive when either mod is absent, when either version differs, or when Starvation's expected `DamagePlayer.Prefix(Player, float, bool)` signature is missing.

### Afterdeath spirit travel bridge

When Afterdeath 1.0.11 is installed, spirits can use Valheim teleport transitions. This allows corpse recovery through dungeon entrances and permits ordinary portal travel while in spirit form. Afterdeath continues to control resurrection, spirit movement, exploration, combat, and other interactions.

The module remains inactive when Afterdeath is absent, when its version differs, or when its expected `DisableTeleport.Prefix()` signature changes.

### Afterdeath spirit home access

When Afterdeath 1.0.11 is installed, spirits can operate doors and interact with their own assigned bed. Doors still run through Valheim's normal interaction path, so private ward allow lists remain authoritative. Interacting with the player's assigned bed resurrects that spirit. Other players' beds, containers, crafting stations, pickups, and all other interactions remain blocked by Afterdeath.

The module remains inactive when Afterdeath is absent, when its version differs, or when Afterdeath's interaction blocker, ghost status field, or Valheim's expected bed interaction signature changes.

### AzuEPI grave quick-slot recovery

When AzuExtendedPlayerInventory 2.5.1 is installed, items recovered through a successful grave Take All return to the same quick slots they occupied at death, including the default Alt+Z, Alt+X, and Alt+C cells. If another item occupies a destination while the player is a spirit, it is safely swapped into the recovered item's temporary inventory cell rather than overwritten. AzuEPI continues to handle equipment re-equipping.

The module remains inactive when AzuEPI is absent, when its version differs, or when its quick-slot snapshot API or Valheim's grave transfer signatures change.

### Afterdeath nearest-bed spawn

When Afterdeath 1.0.11 is installed and a player has a valid bed spawn, the runtime plugin compares that bed with Afterdeath's nearest Skathi using horizontal distance from the death point. The player respawns at the bed only when it is strictly closer; equal distances continue to use Skathi.

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
