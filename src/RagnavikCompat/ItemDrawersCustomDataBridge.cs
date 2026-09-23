using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace RagnavikCompat;

internal static class ItemDrawersCustomDataBridge
{
    private const string StorageKey = "Ragnavik_CustomItems";
    private const string AddRpc = "Ragnavik_AddCustomItem";
    private const string ReturnRpc = "Ragnavik_ReturnCustomItems";
    private const int StorageVersion = 1;
    private const int MaxStoredRecords = 100_000;
    private const int MaxCustomDataEntries = 1_000;

    private static DrawerContract? _contract;
    private static ManualLogSource? _log;
    private static bool _autoPickupActive;

    internal static bool HasRequiredApi()
    {
        var drawerType = AccessTools.TypeByName("kg_ItemDrawers.DrawerComponent");
        var destroyPatchType = AccessTools.TypeByName("kg_ItemDrawers.Piece_OnDestroy_Patch");
        return drawerType != null && destroyPatchType != null &&
               AccessTools.Method(drawerType, "Awake", Type.EmptyTypes) != null &&
               AccessTools.Method(drawerType, "UseItem", new[] { typeof(Humanoid), typeof(ItemDrop.ItemData) })?.ReturnType == typeof(bool) &&
               AccessTools.Method(drawerType, "RPC_WithdrawItem_Request", new[] { typeof(long), typeof(int) })?.ReturnType == typeof(void) &&
               AccessTools.Method(drawerType, "Repeat", Type.EmptyTypes) != null &&
               AccessTools.Method(typeof(ItemDrop), nameof(ItemDrop.CanPickup), new[] { typeof(bool) })?.ReturnType == typeof(bool) &&
               AccessTools.Method(destroyPatchType, "Postfix", new[] { typeof(Piece) })?.ReturnType == typeof(void) &&
               DrawerContract.TryCreate(drawerType, out _);
    }

    internal static bool TryEnable(Harmony harmony, ManualLogSource log)
    {
        var drawerType = AccessTools.TypeByName("kg_ItemDrawers.DrawerComponent");
        if (drawerType == null || !DrawerContract.TryCreate(drawerType, out _contract))
            return false;

        var awake = AccessTools.Method(drawerType, "Awake", Type.EmptyTypes);
        var useItem = AccessTools.Method(drawerType, "UseItem", new[] { typeof(Humanoid), typeof(ItemDrop.ItemData) });
        var withdraw = AccessTools.Method(drawerType, "RPC_WithdrawItem_Request", new[] { typeof(long), typeof(int) });
        var repeat = AccessTools.Method(drawerType, "Repeat", Type.EmptyTypes);
        var canPickup = AccessTools.Method(typeof(ItemDrop), nameof(ItemDrop.CanPickup), new[] { typeof(bool) });
        var destroyPatchType = AccessTools.TypeByName("kg_ItemDrawers.Piece_OnDestroy_Patch");
        var destroyPostfix = destroyPatchType == null ? null : AccessTools.Method(destroyPatchType, "Postfix", new[] { typeof(Piece) });
        if (awake == null || useItem?.ReturnType != typeof(bool) || withdraw?.ReturnType != typeof(void) ||
            repeat == null || canPickup?.ReturnType != typeof(bool) || destroyPostfix?.ReturnType != typeof(void))
            return false;

        _log = log;
        harmony.Patch(awake, postfix: new HarmonyMethod(typeof(ItemDrawersCustomDataBridge), nameof(AfterDrawerAwake)));
        harmony.Patch(useItem, prefix: new HarmonyMethod(typeof(ItemDrawersCustomDataBridge), nameof(BeforeUseItem)));
        harmony.Patch(withdraw, prefix: new HarmonyMethod(typeof(ItemDrawersCustomDataBridge), nameof(BeforeWithdraw)));
        harmony.Patch(repeat,
            prefix: new HarmonyMethod(typeof(ItemDrawersCustomDataBridge), nameof(BeforeAutoPickup)),
            finalizer: new HarmonyMethod(typeof(ItemDrawersCustomDataBridge), nameof(AfterAutoPickup)));
        harmony.Patch(canPickup, prefix: new HarmonyMethod(typeof(ItemDrawersCustomDataBridge), nameof(BeforeCanPickup)));
        harmony.Patch(destroyPostfix, prefix: new HarmonyMethod(typeof(ItemDrawersCustomDataBridge), nameof(BeforeDrawerDestroyed)));
        return true;
    }

    private static void AfterDrawerAwake(object __instance)
    {
        var view = _contract?.GetView(__instance);
        if (view == null)
            return;

        view.Register<string>(AddRpc, (sender, payload) => ReceiveDeposit(__instance, sender, payload));
        view.Register<string>(ReturnRpc, (_, payload) => RestoreItems(payload));
    }

    private static bool BeforeUseItem(object __instance, Humanoid user, ItemDrop.ItemData item, ref bool __result)
    {
        if (item?.m_customData == null || item.m_customData.Count == 0 ||
            _contract?.GetView(__instance) is not { } view)
            return true;

        var prefab = item.m_dropPrefab == null ? "" : item.m_dropPrefab.name;
        var amount = _contract.GetAmount(__instance);
        if (string.IsNullOrEmpty(prefab) || item.m_stack <= 0 || _contract.IsExcluded(prefab) ||
            (amount > 0 && (!string.Equals(_contract.GetPrefab(__instance), prefab, StringComparison.Ordinal) ||
                            _contract.GetQuality(__instance) != item.m_quality)))
            return true;

        var payload = Encode(new[] { StoredItem.From(item, prefab) });
        user.GetInventory().RemoveItem(item);
        view.InvokeRPC(AddRpc, new object[] { payload });
        __result = true;
        return false;
    }

    private static void ReceiveDeposit(object drawer, long sender, string payload)
    {
        if (_contract?.GetView(drawer) is not { } view || !view.IsOwner())
            return;

        var incoming = Decode(payload);
        if (incoming.Count != 1)
            return;

        var item = incoming[0];
        var amount = _contract.GetAmount(drawer);
        if (_contract.IsExcluded(item.Prefab) ||
            (amount > 0 && (!string.Equals(_contract.GetPrefab(drawer), item.Prefab, StringComparison.Ordinal) ||
                            _contract.GetQuality(drawer) != item.Quality)))
        {
            view.InvokeRPC(sender, ReturnRpc, new object[] { payload });
            return;
        }

        var stored = ReadStored(view);
        if (stored.Count >= MaxStoredRecords || amount > int.MaxValue - item.Stack)
        {
            _log?.LogWarning("ItemDrawers custom-data deposit was returned because the drawer storage limit was reached.");
            view.InvokeRPC(sender, ReturnRpc, new object[] { payload });
            return;
        }
        stored.Add(item);
        WriteStored(view, stored);
        _contract.SetPrefab(drawer, item.Prefab);
        _contract.SetQuality(drawer, item.Quality);
        _contract.SetAmount(drawer, checked(amount + item.Stack));
        _contract.UpdateIcon(drawer);
    }

    private static bool BeforeWithdraw(object __instance, long sender, ref int amount)
    {
        if (amount <= 0 || _contract?.GetView(__instance) is not { } view || !view.IsOwner())
            return true;

        var stored = ReadStored(view);
        if (stored.Count == 0)
            return true;

        var remainingRequest = amount;
        var returned = new List<StoredItem>();
        for (var index = stored.Count - 1; index >= 0 && remainingRequest > 0; index--)
        {
            var take = Math.Min(remainingRequest, stored[index].Stack);
            returned.Add(stored[index].WithStack(take));
            remainingRequest -= take;
            if (take == stored[index].Stack)
                stored.RemoveAt(index);
            else
                stored[index] = stored[index].WithStack(stored[index].Stack - take);
        }

        var removed = returned.Sum(item => item.Stack);
        WriteStored(view, stored);
        _contract.SetAmount(__instance, Math.Max(0, _contract.GetAmount(__instance) - removed));
        view.InvokeRPC(sender, ReturnRpc, new object[] { Encode(returned) });
        amount -= removed;

        if (amount > 0)
            return true;

        if (_contract.GetAmount(__instance) == 0)
        {
            _contract.SetPrefab(__instance, "");
            _contract.SetQuality(__instance, 1);
        }
        _contract.UpdateIcon(__instance);
        return false;
    }

    private static void RestoreItems(string payload)
    {
        var player = Player.m_localPlayer;
        if (player == null)
            return;

        foreach (var stored in Decode(payload))
            DropItem(stored, player.transform.position + Vector3.up);
    }

    private static void DropItem(StoredItem stored, Vector3 position)
    {
        var prefab = ObjectDB.instance?.GetItemPrefab(stored.Prefab);
        var template = prefab == null ? null : prefab.GetComponent<ItemDrop>();
        if (template == null)
        {
            _log?.LogError($"Cannot restore ItemDrawers item '{stored.Prefab}' because its prefab is unavailable.");
            return;
        }
        var item = template.m_itemData.Clone();
        item.m_stack = stored.Stack;
        item.m_quality = stored.Quality;
        item.m_customData = new Dictionary<string, string>(stored.CustomData, StringComparer.Ordinal);
        ItemDrop.DropItem(item, item.m_stack, position, Quaternion.identity);
    }

    private static void BeforeDrawerDestroyed(Piece __0)
    {
        if (_contract == null)
            return;
        var drawer = __0.GetComponent(_contract.DrawerType);
        if (drawer == null || _contract.GetView(drawer) is not { } view || !view.IsOwner())
            return;
        var stored = ReadStored(view);
        if (stored.Count == 0)
            return;
        var removed = stored.Sum(item => item.Stack);
        foreach (var item in stored)
            DropItem(item, __0.transform.position + Vector3.up);
        WriteStored(view, new List<StoredItem>());
        _contract.SetAmount(drawer, Math.Max(0, _contract.GetAmount(drawer) - removed));
    }

    private static void BeforeAutoPickup() => _autoPickupActive = true;

    private static Exception? AfterAutoPickup(Exception? __exception)
    {
        _autoPickupActive = false;
        return __exception;
    }

    private static bool BeforeCanPickup(ItemDrop __instance, ref bool __result)
    {
        if (!_autoPickupActive || __instance.m_itemData?.m_customData == null ||
            __instance.m_itemData.m_customData.Count == 0)
            return true;

        __result = false;
        return false;
    }

    private static List<StoredItem> ReadStored(ZNetView view) =>
        Decode(view.GetZDO()?.GetString(StorageKey, "") ?? "");

    private static void WriteStored(ZNetView view, List<StoredItem> values) =>
        view.GetZDO()?.Set(StorageKey, Encode(values));

    private static string Encode(IEnumerable<StoredItem> source)
    {
        var values = source.ToList();
        var package = new ZPackage();
        package.Write(StorageVersion);
        package.Write(values.Count);
        foreach (var item in values)
        {
            package.Write(item.Prefab);
            package.Write(item.Quality);
            package.Write(item.Stack);
            package.Write(item.CustomData.Count);
            foreach (var pair in item.CustomData.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                package.Write(pair.Key);
                package.Write(pair.Value);
            }
        }
        return Convert.ToBase64String(package.GetArray());
    }

    private static List<StoredItem> Decode(string payload)
    {
        if (string.IsNullOrEmpty(payload))
            return new List<StoredItem>();

        try
        {
            var package = new ZPackage(Convert.FromBase64String(payload));
            if (package.ReadInt() != StorageVersion)
                return new List<StoredItem>();

            var count = package.ReadInt();
            if (count < 0 || count > MaxStoredRecords)
                return new List<StoredItem>();

            var result = new List<StoredItem>(count);
            for (var index = 0; index < count; index++)
            {
                var prefab = package.ReadString();
                var quality = package.ReadInt();
                var stack = package.ReadInt();
                var customCount = package.ReadInt();
                if (string.IsNullOrEmpty(prefab) || quality < 1 || stack < 1 ||
                    customCount < 1 || customCount > MaxCustomDataEntries)
                    return new List<StoredItem>();

                var customData = new Dictionary<string, string>(customCount, StringComparer.Ordinal);
                for (var customIndex = 0; customIndex < customCount; customIndex++)
                    customData[package.ReadString()] = package.ReadString();
                result.Add(new StoredItem(prefab, quality, stack, customData));
            }
            return result;
        }
        catch (Exception error)
        {
            _log?.LogError($"Cannot read preserved ItemDrawers custom data: {error.GetBaseException().Message}");
            return new List<StoredItem>();
        }
    }

    private sealed class StoredItem
    {
        internal string Prefab { get; }
        internal int Quality { get; }
        internal int Stack { get; }
        internal Dictionary<string, string> CustomData { get; }

        internal StoredItem(string prefab, int quality, int stack, Dictionary<string, string> customData)
        {
            Prefab = prefab;
            Quality = quality;
            Stack = stack;
            CustomData = customData;
        }

        internal static StoredItem From(ItemDrop.ItemData item, string prefab) =>
            new(prefab, Math.Max(1, item.m_quality), item.m_stack,
                new Dictionary<string, string>(item.m_customData, StringComparer.Ordinal));

        internal StoredItem WithStack(int stack) => new(Prefab, Quality, stack, CustomData);
    }

    private sealed class DrawerContract
    {
        internal Type DrawerType { get; }
        private readonly PropertyInfo _view;
        private readonly PropertyInfo _prefab;
        private readonly PropertyInfo _amount;
        private readonly PropertyInfo _quality;
        private readonly MethodInfo _updateIcon;
        private readonly FieldInfo? _excluded;

        private DrawerContract(PropertyInfo view, PropertyInfo prefab, PropertyInfo amount, PropertyInfo quality,
            MethodInfo updateIcon, FieldInfo? excluded)
        {
            DrawerType = view.DeclaringType!;
            _view = view;
            _prefab = prefab;
            _amount = amount;
            _quality = quality;
            _updateIcon = updateIcon;
            _excluded = excluded;
        }

        internal static bool TryCreate(Type drawerType, out DrawerContract? contract)
        {
            contract = null;
            var view = AccessTools.Property(drawerType, "_znv");
            var prefab = AccessTools.Property(drawerType, "CurrentPrefab");
            var amount = AccessTools.Property(drawerType, "CurrentAmount");
            var quality = AccessTools.Property(drawerType, "Quality");
            var updateIcon = AccessTools.Method(drawerType, "RPC_UpdateIcon",
                new[] { typeof(long), typeof(string), typeof(int), typeof(int) });
            var pluginType = AccessTools.TypeByName("kg_ItemDrawers.ItemDrawers");
            var excluded = pluginType == null ? null : AccessTools.Field(pluginType, "ExcludeSet");

            if (view?.PropertyType != typeof(ZNetView) || prefab?.PropertyType != typeof(string) ||
                amount?.PropertyType != typeof(int) || quality?.PropertyType != typeof(int) ||
                prefab.SetMethod == null || amount.SetMethod == null || quality.SetMethod == null ||
                updateIcon?.ReturnType != typeof(void))
                return false;

            contract = new DrawerContract(view, prefab, amount, quality, updateIcon,
                excluded);
            return true;
        }

        internal ZNetView? GetView(object drawer) => _view.GetValue(drawer) as ZNetView;
        internal string GetPrefab(object drawer) => (string?)_prefab.GetValue(drawer) ?? "";
        internal void SetPrefab(object drawer, string value) => _prefab.SetValue(drawer, value);
        internal int GetAmount(object drawer) => (int)(_amount.GetValue(drawer) ?? 0);
        internal void SetAmount(object drawer, int value) => _amount.SetValue(drawer, value);
        internal int GetQuality(object drawer) => (int)(_quality.GetValue(drawer) ?? 1);
        internal void SetQuality(object drawer, int value) => _quality.SetValue(drawer, value);
        internal bool IsExcluded(string prefab) =>
            _excluded?.GetValue(null) is HashSet<string> excluded && excluded.Contains(prefab);
        internal void UpdateIcon(object drawer) =>
            _updateIcon.Invoke(drawer, new object[] { 0L, GetPrefab(drawer), GetAmount(drawer), GetQuality(drawer) });
    }
}
