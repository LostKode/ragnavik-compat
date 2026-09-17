# Ragnavik Compatibility

One package for independently guarded Valheim compatibility modules. The preloader assembly handles API bridges required before plugins load. A normal BepInEx plugin can hold runtime interoperability fixes when needed.

The first module restores the removed four parameter `Inventory.AddItem(ItemData, int, int, int)` overload required by the `ItemDataManager` library bundled with MagicRevamp 1.5.0. It forwards to Valheim 1.0's current five parameter method and includes the `ZLog.Log` placeholder expected by the legacy Harmony transpiler.

