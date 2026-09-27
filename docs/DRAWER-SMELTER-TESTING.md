# Drawer smelter bridge validation

## Verified source

The reviewed LazyVikings 1.2.4 `SmelterPatch.UpdateSmelter_Prefix` searches only `Container` inventories. KG ItemDrawers 1.4.0 instead exposes `API.ClientSideV2.AllDrawers()` and stores `Prefab`, `Amount`, and `Quality` in its ZDO. The module patches the LazyVikings method itself and observes its existing stopwatch before the refill. It runs after normal chest processing and rereads capacity.

The installed Valheim assembly confirms owner-directed RPCs dispatch synchronously through `ZRoutedRpc.InvokeRoutedRPC`. The bridge debits one drawer item and calls the normal smelter RPC; a rejected addition restores the item. If a later effect throws after acceptance, the item stays debited. Drawer icon updates use the existing `UpdateIcon` broadcast. No network ownership is claimed.

## Automated checks

Run `dotnet run --project tests/DrawerSmelter.Tests -c Release` with .NET 9. The suite compiles the production module against game stubs and uses real Harmony patches. It checks ore/fuel balance, chest precedence, throttle, capacity, range, ward settings, leave-one, ownership, metadata protection, rejected transfers, exceptions, unloading, and version guards. These tests do not replace a Unity multiplayer session.

## Pending game validation

Use an explicitly requested Gale Test profile, the local `galetest1` world, LazyVikings 1.2.4 and KG ItemDrawers 1.4.0. Do not use production for this test.

1. Confirm the log reports `Drawer smelter bridge active` without patch errors.
2. Place a smelter and blast furnace near separate coal and compatible ore drawers, without supply chests. Verify each machine gains resources and drawer counts fall by matching amounts. Verify output still collects normally.
3. Test full machines, empty drawers, a drawer outside the configured radius, and leave-one enabled/disabled. Try deposit-only and disabled machine automation.
4. Add a supply chest; confirm chest feeding continues and neither input nor fuel overfills.
5. Test ward denial with private-area checks enabled. Drawers storing upgraded/custom-data items must remain untouched.
6. With two test clients, verify only the owner transfers items, remote-owned drawers are skipped, and ordinary game ownership migration resumes feeding without duplication. Rejoin and confirm persisted balances and drawer labels.

Built artifact is a development candidate. No shared/client/server dependency manifests, anti-cheat policy, Gale profiles, published packages, or live services are changed by this patch task. A release must coordinate those package versions through the suite's normal release process.
