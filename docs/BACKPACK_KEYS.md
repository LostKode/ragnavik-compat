# Backpack key bridge verification

The bridge targets Smoothbrain Backpacks 1.3.10 and the shared `Door.HaveKey(Humanoid, bool)` check used by vanilla crypts and OreMines 1.2.1. Inspection of the packaged mod assemblies confirmed `Backpacks.API.GetAllBackpackInventories(Inventory)` returns the inventories of backpacks carried in the supplied inventory. It does not use nearby containers or the open inventory UI.

OreMines has a separate `Interact_Postfix` which removes keys from the main inventory when Consumable Keys is enabled. The bridge deliberately leaves those gates unchanged in that mode. Supporting consumption from backpacks requires a separate removal-and-save integration. Vanilla consumable doors are likewise excluded. The optional `Door.m_consumeKey` field is reflected because it is absent from the pinned build reference and present in the newer installed game.

## Automated checks

Run `dotnet run --project tests/RagnavikCompat.BackpackKeys.Tests --configuration Release`. On this workspace, use the Windows SDK specified in the suite AGENTS.md with a Windows project path. The test executable rolls forward to an installed newer runtime.

The harness executes the actual bridge against minimal game/mod doubles. It checks supported and unsupported versions, correct and wrong keys, ordinary-inventory priority, carried versus dropped backpacks, multiple backpacks, local-player isolation, world-level argument forwarding, live consumable-mode changes, optional-mod isolation, API failure containment, and teardown. It does not simulate Unity networking or prove in-game door operation.

## In-game smoke test still required

Use the Test environment and an existing authorized test setup. Do not create or modify Gale profiles as part of this check without an explicit request.

1. With no key in the main inventory, carry a backpack containing CryptKey. Interact with a locked swamp crypt; verify it opens and the key stays in the backpack.
2. Repeat for copper/tin with BOM_EikthyrKey and the other mine keys with OreMines Consumable Keys off.
3. Remove or drop the backpack, or replace the key with the wrong key. Verify locked gates remain locked.
4. Verify lower-world-level keys retain the normal rejection/message and ordinary-inventory keys still work.
5. Turn Consumable Keys on. A key only in a backpack must not unlock the mine. Move the key into the main inventory and verify the existing mod consumes it as usual.
6. Verify ward restrictions and Afterdeath door behavior remain intact; the bridge changes only key possession and leaves those other checks in place.

No production deployment or game-profile installation is part of this patch.
