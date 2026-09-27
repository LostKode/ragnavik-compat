# Compatibility 1.0.22: drawers can supply smelters and blast furnaces

Nearby KG ItemDrawers can now automatically supply coal and accepted ores to smelters and blast furnaces through LazyVikings. Existing chest supplies are processed first. Drawer output collection continues as before.

The bridge follows each machine's automation, range and ward settings, plus the global leave-one option. It checks available capacity before transferring items. Upgraded items and drawers with preserved custom item data are excluded. As with chest automation, the player running the refill must own the machine and source drawer.

This release supports LazyVikings 1.2.4 and KG ItemDrawers 1.4.0. Other versions disable the bridge until reviewed. It also retains the reusable backpack-key support introduced in Compatibility 1.0.21.

Players should update through the coordinated Ragnavik pack release once it is available. Publishing this Compatibility package alone does not update an existing client profile or the server. Clients and server must use the same Compatibility version.
