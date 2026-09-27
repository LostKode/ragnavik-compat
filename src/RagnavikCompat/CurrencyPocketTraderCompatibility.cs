using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;

namespace RagnavikCompat;

// Separate Harmony owners let a changed optional API disable only its own bridge.
internal static class CurrencyPocketTraderCompatibility
{
    private static readonly Harmony CurrencyHarmony = new(RagnavikCompatPlugin.PluginGuid + ".currency-trader");
    private static readonly Harmony EpicHarmony = new(RagnavikCompatPlugin.PluginGuid + ".currency-epicloot");
    [ThreadStatic] private static int _storePurchaseDepth;
    private static Func<int>? _getBalance;
    private static Action<int, Player>? _setBalance;
    private static Action? _refresh;
    private static ManualLogSource? _log;
    private static readonly Type[] AddSignature = { typeof(string), typeof(int), typeof(int), typeof(int), typeof(long), typeof(string), typeof(bool) };

    internal static void Enable(ManualLogSource log)
    {
        _log = log;
        if (!Chainloader.PluginInfos.TryGetValue("Azumatt.CurrencyPocket", out var pocket) ||
            !CurrencyPocketTakeAllCompatibility.Supports(pocket.Metadata.Version))
            return;

        try
        {
            var misc = AccessTools.TypeByName("CurrencyPocket.MiscFunctions");
            _getBalance = (Func<int>)Required(misc, "GetPlayerCoinsFromCustomData", typeof(int), Type.EmptyTypes)
                .CreateDelegate(typeof(Func<int>));
            _setBalance = (Action<int, Player>)Required(misc, "UpdatePlayerCustomData", typeof(void), typeof(int), typeof(Player))
                .CreateDelegate(typeof(Action<int, Player>));
            _refresh = (Action)Required(AccessTools.TypeByName("CurrencyPocket.CurrencyPocket"), "UpdatePocketUI", typeof(void), Type.EmptyTypes)
                .CreateDelegate(typeof(Action));
            var oldDebit = Required(AccessTools.TypeByName("CurrencyPocket.CurrencyPocket+Inventory_RemoveItem_Patch"),
                "Postfix", typeof(void), typeof(Inventory), typeof(string), typeof(int), typeof(int), typeof(bool));
            var remove = Required(typeof(Inventory), "RemoveItem", typeof(void), typeof(string), typeof(int), typeof(int), typeof(bool));
            var buy = Required(typeof(StoreGui), "BuySelectedItem", typeof(void), Type.EmptyTypes);
            var sell = Required(typeof(StoreGui), "SellItem", typeof(void), Type.EmptyTypes);
            Required(typeof(Inventory), "AddItem", typeof(ItemDrop.ItemData), AddSignature);
            CurrencyHarmony.Patch(oldDebit, prefix: Hook(nameof(SkipOriginalPocketDebit)));
            CurrencyHarmony.Patch(buy, prefix: Hook(nameof(BeginStorePurchase)), finalizer: Hook(nameof(EndStorePurchase)));
            CurrencyHarmony.Patch(remove, prefix: Hook(nameof(SpendPocketBeforeInventory)));
            CurrencyHarmony.Patch(sell, transpiler: Hook(nameof(RouteSalePayout)));
            log.LogInfo("CurrencyPocket trader bridge active: spend pouch coins once and deposit sale proceeds directly.");
        }
        catch (Exception error)
        {
            CurrencyHarmony.UnpatchSelf();
            log.LogWarning($"CurrencyPocket trader bridge disabled: {error.GetBaseException().Message}");
            return;
        }

        if (!Chainloader.PluginInfos.TryGetValue("randyknapp.mods.epicloot", out var epic) || epic.Metadata.Version != new System.Version(0, 14, 13))
        {
            log.LogInfo("CurrencyPocket EpicLoot bridge skipped: requires verified EpicLoot 0.14.13.");
            return;
        }
        try
        {
            var inventory = AccessTools.TypeByName("EpicLoot_UnityLib.InventoryManagement");
            var count = Required(inventory, "CountItem", typeof(int), typeof(string));
            var remove = Required(inventory, "RemoveItem", typeof(void), typeof(string), typeof(int));
            var give = Required(inventory, "GiveItem", typeof(void), typeof(string), typeof(int));
            EpicHarmony.Patch(count, postfix: Hook(nameof(IncludePocketBalance)));
            EpicHarmony.Patch(remove, prefix: Hook(nameof(SpendEpicPocket)));
            EpicHarmony.Patch(give, prefix: Hook(nameof(DepositEpicReward)));
            log.LogInfo("CurrencyPocket EpicLoot bridge active for adventure affordability, payments, and coin rewards.");
        }
        catch (Exception error)
        {
            EpicHarmony.UnpatchSelf();
            log.LogWarning($"CurrencyPocket EpicLoot bridge disabled: {error.GetBaseException().Message}");
        }
    }

    private static MethodInfo Required(Type? type, string name, Type result, params Type[] args)
    {
        var method = type == null ? null : AccessTools.DeclaredMethod(type, name, args);
        if (method == null || method.ReturnType != result)
            throw new MissingMethodException(type?.FullName, name);
        return method;
    }

    private static HarmonyMethod Hook(string name) => new(typeof(CurrencyPocketTraderCompatibility), name);
    private static bool IsCoin(string name) => name == CurrencyPocketTakeAllCompatibility.CoinSharedName;
    private static bool IsLocal(Inventory inventory) => Player.m_localPlayer != null && ReferenceEquals(inventory, Player.m_localPlayer.GetInventory());

    private static int Balance()
    {
        int balance = _getBalance!();
        if (balance < 0) throw new InvalidOperationException("CurrencyPocket balance is negative.");
        return balance;
    }

    private static void WriteBalance(int value)
    {
        _setBalance!(value, Player.m_localPlayer);
        // A visual refresh failure must never retry an already committed payment.
        try { _refresh!(); }
        catch (Exception error) { _log?.LogWarning($"CurrencyPocket UI refresh failed: {error.GetBaseException().Message}"); }
    }

    private static void Spend(ref int amount)
    {
        if (amount <= 0 || Player.m_localPlayer == null) return;
        int balance = Balance();
        int fromPocket = Math.Min(balance, amount);
        if (fromPocket == 0) return;
        WriteBalance(balance - fromPocket);
        amount -= fromPocket;
    }

    private static void BeginStorePurchase() => _storePurchaseDepth++;
    private static void EndStorePurchase() => _storePurchaseDepth--;

    private static bool SkipOriginalPocketDebit(Inventory __0, string __1) => _storePurchaseDepth == 0 || !IsLocal(__0) || !IsCoin(__1);

    private static void SpendPocketBeforeInventory(Inventory __instance, string name, ref int amount)
    {
        if (_storePurchaseDepth > 0 && IsLocal(__instance) && IsCoin(name)) Spend(ref amount);
    }

    private static void IncludePocketBalance(string __0, ref int __result)
    {
        if (IsCoin(__0) && Player.m_localPlayer != null)
            __result = (int)Math.Min(int.MaxValue, (long)__result + Balance());
    }

    private static bool SpendEpicPocket(string __0, ref int __1)
    {
        if (!IsCoin(__0) || Player.m_localPlayer == null) return true;
        // Reduce EpicLoot's requested amount before it calculates the provider shortfall.
        // Its normal path then handles loose coins and external inventories exactly once.
        Spend(ref __1);
        return __1 > 0;
    }

    private static bool TryDeposit(int amount)
    {
        if (Player.m_localPlayer == null || !CurrencyPocketTakeAllCompatibility.TryAddBalance(Balance(), amount, out int updated))
            return false;
        WriteBalance(updated);
        return true;
    }

    private static bool DepositEpicReward(string __0, int __1) => __0 != "Coins" || !TryDeposit(__1);

    private static IEnumerable<CodeInstruction> RouteSalePayout(IEnumerable<CodeInstruction> instructions)
    {
        var code = instructions.ToList();
        var add = Required(typeof(Inventory), "AddItem", typeof(ItemDrop.ItemData), AddSignature);
        var matches = code.Where(instruction => instruction.Calls(add)).ToList();
        if (matches.Count != 1) throw new InvalidOperationException("StoreGui.SellItem payout signature changed.");
        matches[0].opcode = OpCodes.Call;
        matches[0].operand = AccessTools.Method(typeof(CurrencyPocketTraderCompatibility), nameof(AddSaleCoins));
        return code;
    }

    private static ItemDrop.ItemData AddSaleCoins(Inventory inventory, string name, int stack, int quality, int variant, long crafterId, string crafterName, bool cheated)
    {
        if (IsLocal(inventory) && name == "Coins")
        {
            var coin = ObjectDB.instance.GetItemPrefab(name).GetComponent<ItemDrop>().m_itemData.Clone();
            coin.m_stack = stack;
            if (TryDeposit(stack)) return coin;
        }
        return inventory.AddItem(name, stack, quality, variant, crafterId, crafterName, cheated);
    }

    internal static void Disable()
    {
        EpicHarmony.UnpatchSelf();
        CurrencyHarmony.UnpatchSelf();
        _getBalance = null;
        _setBalance = null;
        _refresh = null;
        _log = null;
    }
}
