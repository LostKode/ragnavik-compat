using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace RagnavikCompat;

// Only extend door checks: general inventory checks also drive crafting and
// consumption and must not report items they cannot remove.
internal static class BackpackKeyCompatibility
{
    internal const string BackpackGuid = "org.bepinex.plugins.backpacks";
    private const string OreMinesGuid = "blacks7ar.OreMines";
    private static Harmony? patches;
    private static FieldInfo? consumeKey;
    private static Func<Inventory, List<Inventory>>? getInventories;
    private static ConfigEntryBase? consumableMineKeys;
    private static bool unknownOreMines;
    private static ManualLogSource? logger;

    internal static void Enable(Harmony owner, ManualLogSource log)
    {
        if (!Chainloader.PluginInfos.TryGetValue(BackpackGuid, out var backpack) ||
            backpack.Metadata.Version != new System.Version(1, 3, 10))
        {
            log.LogInfo("Backpack key bridge skipped: requires Smoothbrain Backpacks 1.3.10.");
            return;
        }
        logger = log;
        patches = new Harmony(owner.Id + ".backpackkeys");
        try
        {
            var api = backpack.Instance.GetType().Assembly.GetType("Backpacks.API");
            var getter = api == null ? null : AccessTools.DeclaredMethod(api,
                "GetAllBackpackInventories", new[] { typeof(Inventory) });
            var haveKey = AccessTools.DeclaredMethod(typeof(Door), "HaveKey",
                new[] { typeof(Humanoid), typeof(bool) });
            if (getter?.IsStatic != true || getter.ReturnType != typeof(List<Inventory>) ||
                haveKey?.ReturnType != typeof(bool) || haveKey.IsStatic)
                throw new InvalidOperationException("Backpack or door API changed; review this module.");
            // Older Valheim builds have no consumable-door field.
            consumeKey = AccessTools.Field(typeof(Door), "m_consumeKey");
            if (consumeKey != null && (consumeKey.FieldType != typeof(bool) || consumeKey.IsStatic))
                throw new InvalidOperationException("Door consumption API changed.");
            getInventories = (Func<Inventory, List<Inventory>>)Delegate.CreateDelegate(
                typeof(Func<Inventory, List<Inventory>>), getter);
            unknownOreMines = false;
            if (Chainloader.PluginInfos.TryGetValue(OreMinesGuid, out var mines))
            {
                var field = AccessTools.Field(mines.Instance.GetType(), "_consumableKeys");
                consumableMineKeys = field?.IsStatic == true ? field.GetValue(null) as ConfigEntryBase : null;
                unknownOreMines = mines.Metadata.Version != new System.Version(1, 2, 1) ||
                    consumableMineKeys?.BoxedValue?.ToString() is not ("On" or "Off");
            }
            patches.Patch(haveKey, postfix: new HarmonyMethod(typeof(BackpackKeyCompatibility), nameof(AfterHaveKey)));
            log.LogInfo("Backpack key bridge active for reusable door keys, including swamp crypt and OreMines keys. Consumable keys still require the main inventory.");
        }
        catch (Exception e)
        {
            Disable();
            log.LogWarning($"Backpack key bridge disabled: {e.Message}");
        }
    }

    private static void AfterHaveKey(Door __instance, Humanoid __0, bool __1, ref bool __result)
    {
        if (__result || __0 == null || __0 != Player.m_localPlayer ||
            __instance.m_keyItem == null || getInventories == null)
            return;
        // OreMines consumes keys in its own Interact postfix. Fail closed when
        // that mode is on or cannot be verified, including future mod versions.
        if (__instance.m_name?.StartsWith("$bom_", StringComparison.Ordinal) == true &&
            (unknownOreMines || (consumableMineKeys != null && consumableMineKeys.BoxedValue?.ToString() != "Off")))
            return;
        try
        {
            if (consumeKey?.GetValue(__instance) is true) return;
            var keyName = __instance.m_keyItem!.m_itemData.m_shared.m_name;
            foreach (var inventory in getInventories!(__0!.GetInventory()))
            {
                // Preserve Valheim's world-level matching and its relaxed
                // second lookup for the "key is too low" message.
                if (inventory != null && inventory.HaveItem(keyName, __1))
                {
                    __result = true;
                    return;
                }
            }
        }
        catch (Exception e)
        {
            getInventories = null;
            logger?.LogWarning($"Backpack key lookup disabled after an API failure: {e.Message}");
        }
    }

    internal static void Disable()
    {
        patches?.UnpatchSelf();
        patches = null;
        consumeKey = null;
        getInventories = null;
        consumableMineKeys = null;
        unknownOreMines = false;
    }
}
