# Coin pouch trader compatibility

Targets inspected: CurrencyPocket 1.0.15, EpicLoot 0.14.13, and the locally installed Valheim assembly. The module checks exact optional-mod versions and method signatures before installing its patches. The vanilla trader and EpicLoot bridges have separate Harmony owners so an unsupported EpicLoot version does not disable vanilla payouts.

EpicLoot's `EpicLoot_UnityLib.InventoryManagement.CountItem(string)` supplies adventure affordability. Its `RemoveItem(string, int)` measures inventory removals and charges any shortfall to registered providers. The bridge spends pouch coins and reduces that method's amount before its normal logic runs, avoiding a second provider charge. Secret stash and gambling share the merchant purchase path; successful treasure map callbacks use the same removal API. Bounties currently award currencies rather than charge an acceptance fee. Their `GiveItem("Coins", amount)` rewards enter the pouch; forest, iron, and gold tokens keep their original behavior.

Vanilla purchases use a scoped prefix/finalizer around `StoreGui.BuySelectedItem`. Only within that scope is CurrencyPocket's original debit postfix suppressed, and the requested inventory removal reduced by the pouch payment. The scope ends even if the purchase throws. Other inventory removal behavior remains unchanged. A guarded transpiler replaces the single payout `Inventory.AddItem` call in `StoreGui.SellItem`, retaining vanilla sale effects and notifications. Pouch overflow retains the normal inventory payout path.

## Automated validation

`dotnet run --project tests/CurrencyTrader.Tests -c Release` runs 18 transaction tests against the production bridge with actual Harmony patches and minimal game hosts. The test-only Lib.Harmony package supports CoreCLR; the shipped plugin builds against Valheim's bundled HarmonyX. An owner-scoped cleanup adapter bridges the test library's naming difference. Tests cover pouch-only and mixed balances, provider shortfalls, vanilla double-charge prevention, exception scope cleanup, full-inventory sales and bounty rewards, token isolation, overflow, UI and write failures, unloading, and unsupported EpicLoot versions.

The existing compatibility/signature suite also passed using installed mod DLLs and the built plugin. The release build succeeded with two existing framework assembly-version conflict warnings. Package validation passed; comparison with Compatibility 1.0.19 confirmed identical archive entries, dependencies, creature configuration, and icon.

## Remaining in-game validation

On a user-requested local Test environment, exercise secret stash, gambling, treasure maps, bounty claims, ordinary purchases, and sales with pouch-only and mixed gold balances. Verify total funds change by exactly the displayed price or reward, including with a full inventory. No in-game transaction validation or production deployment was performed for this patch. Do not create a Gale profile unless explicitly requested.
