# Ragnavik Compatibility

One package for independently guarded Valheim compatibility modules. It owns fixes between mods in the Ragnavik modpack and narrow temporary fixes for individual mods until upstream releases resolve them.

The preloader module restores the removed four parameter `Inventory.AddItem(ItemData, int, int, int)` overload required by the `ItemDataManager` library bundled with MagicRevamp 1.5.0. It forwards to Valheim 1.0's current five parameter method and includes the `ZLog.Log` placeholder expected by the legacy Harmony transpiler.

The runtime plugin includes the EpicMMO JSON reload guard migrated from `ragnavik-epicmmo-reload-guard`. On a dedicated server with EpicMMO present, it coalesces noisy watcher events, ignores metadata only changes, and permits one reload when JSON content or paths actually change. It remains inactive on clients, when EpicMMO is absent, or when the expected watcher method is missing.

## Upstream update policy

Before updating a mod covered by this package, review its release notes and changelog for an upstream fix or relevant API change. Revalidate the guarded signature and behavior against the exact candidate DLL. Remove or disable the local module when the upstream correction is verified so both fixes never run together.

