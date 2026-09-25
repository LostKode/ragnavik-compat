using System;
using System.Reflection;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace RagnavikCompat;

internal static class FarmingXpCompatibility
{
    private const string Ack = "Ragnavik.HarvestXp.v1";
    private static readonly HarvestXpLedger Ledger = new();
    private static MethodInfo? contains, getExp, addExp;
    private static ConfigEntry<bool>? disabled, pickupDisabled, pieceDisabled;
    private static bool registered;
    private static ManualLogSource? log;
    private static readonly FieldInfo Picked = AccessTools.Field(typeof(Pickable), "m_picked");
    private static readonly FieldInfo PickedLocal = AccessTools.Field(typeof(Pickable), "m_pickedLocal");

    private static Harmony? xpHarmony, previewHarmony;
    internal static void Enable(Harmony harmony, ManualLogSource logger)
    {
        log = logger;
        previewHarmony = new Harmony(harmony.Id + ".farmingpreview");
        try { EnablePreviewGuard(previewHarmony, logger); }
        catch (Exception error)
        {
            previewHarmony.UnpatchSelf();
            logger.LogWarning($"Farming preview guard disabled: {error.Message}");
        }
        xpHarmony = new Harmony(harmony.Id + ".farmingxp");
        try { EnableXp(xpHarmony, logger); }
        catch (Exception error)
        {
            xpHarmony.UnpatchSelf();
            logger.LogWarning($"Farming XP bridge disabled: {error.Message}");
        }
    }
    internal static void Disable()
    {
        xpHarmony?.UnpatchSelf();
        previewHarmony?.UnpatchSelf();
        Clear();
    }
    private static void EnableXp(Harmony harmony, ManualLogSource logger)
    {
        if (!Chainloader.PluginInfos.TryGetValue("WackyMole.EpicMMOSystem", out var mmo) ||
            mmo.Metadata.Version != new System.Version(1, 9, 68))
        {
            logger.LogInfo("Farming XP bridge skipped: requires EpicMMO 1.9.68.");
            return;
        }
        var assembly = mmo.Instance.GetType().Assembly;
        var main = assembly.GetType("EpicMMOSystem.EpicMMOSystem");
        var data = assembly.GetType("EpicMMOSystem.DataMonsters");
        var api = assembly.GetType("API.EMMOS_API");
        var harvest = assembly.GetType("EpicMMOSystem.LevelSystem_noncombat+PickablePickMMOWacky");
        var placement = assembly.GetType("EpicMMOSystem.LevelSystem_noncombat+Player_placepiece_patch_epicmmoA");
        contains = data == null ? null : AccessTools.Method(data, "contains", new[] { typeof(string) });
        getExp = data == null ? null : AccessTools.Method(data, "getExp", new[] { typeof(string) });
        addExp = api == null ? null : AccessTools.Method(api, "AddExp", new[] { typeof(int) });
        disabled = main == null ? null : AccessTools.Field(main, "disableNonCombatObjects")?.GetValue(null) as ConfigEntry<bool>;
        pickupDisabled = main == null ? null : AccessTools.Field(main, "disablePickableXP")?.GetValue(null) as ConfigEntry<bool>;
        pieceDisabled = main == null ? null : AccessTools.Field(main, "disablePieceXP")?.GetValue(null) as ConfigEntry<bool>;
        var oldHarvest = harvest == null ? null : AccessTools.Method(harvest, "Postfix", new[] { typeof(Pickable) });
        var oldPlacement = placement == null ? null : AccessTools.Method(placement, "Postfix", new[] { typeof(Player), typeof(Piece) });
        var pick = AccessTools.Method(typeof(Pickable), "RPC_Pick", new[] { typeof(long), typeof(int) });
        var interact = AccessTools.Method(typeof(Pickable), "Interact", new[] { typeof(Humanoid), typeof(bool), typeof(bool) });
        var place = AccessTools.Method(typeof(Player), "TryPlacePiece", new[] { typeof(Piece) });
        var sceneAwake = AccessTools.Method(typeof(ZNetScene), "Awake");
        var sceneDestroy = AccessTools.Method(typeof(ZNetScene), "OnDestroy");
        if (sceneAwake == null || sceneDestroy == null || contains?.ReturnType != typeof(bool) || getExp?.ReturnType != typeof(int) || addExp?.ReturnType != typeof(void) ||
            disabled == null || pickupDisabled == null || pieceDisabled == null || oldHarvest == null || oldPlacement == null ||
            pick == null || interact == null || place?.ReturnType != typeof(bool) || Picked == null || PickedLocal == null)
        {
            logger.LogWarning("Farming XP bridge skipped: expected XP or game signatures changed.");
            return;
        }
        // Suppress only the defective upstream harvest reward and cultivated-crop reward.
        // Other noncombat actions retain their original implementation.
        harmony.Patch(oldHarvest, prefix: Patch(nameof(SkipOldHarvest)));
        harmony.Patch(oldPlacement, prefix: Patch(nameof(KeepNonCropPlacement)));
        harmony.Patch(sceneAwake, postfix: Patch(nameof(Register)));
        harmony.Patch(sceneDestroy, prefix: Patch(nameof(Clear)));
        harmony.Patch(interact, prefix: Patch(nameof(BeforeInteract)), finalizer: Patch(nameof(AfterInteract)));
        harmony.Patch(pick, prefix: Patch(nameof(BeforePick)), postfix: Patch(nameof(AfterPick)));
        harmony.Patch(place, postfix: Patch(nameof(AfterPlacement)));
        try { EnableBulkPlacement(harmony, logger); }
        catch (Exception error) { logger.LogWarning($"Bulk planting XP skipped: {error.Message}"); }
        logger.LogInfo("Farming XP bridge active: owner-confirmed per-harvest XP and successful crop planting XP.");
    }

    private static HarmonyMethod Patch(string name) => new(typeof(FarmingXpCompatibility), name);
    private static bool SkipOldHarvest() => false;
    private static bool KeepNonCropPlacement(Piece piece) => piece == null || piece.GetComponent<Plant>() == null;
    private static void Register()
    {
        if (registered || ZRoutedRpc.instance == null) return;
        ZRoutedRpc.instance.Register<string>(Ack, Receive);
        registered = true;
    }
    private static void Clear() { registered = false; Ledger.Clear(); }
    private static void Receive(long sender, string id)
    {
        var xp = Ledger.Confirm(id, sender, Time.time);
        if (xp > 0 && Player.m_localPlayer != null && disabled?.Value == false && pickupDisabled?.Value == false)
            Award(xp);
    }
    private static int XpFor(string name, int fallback = 0)
    {
        var cloneName = name.EndsWith("(Clone)", StringComparison.Ordinal) ? name : name + "(Clone)";
        return contains!.Invoke(null, new object[] { cloneName }) is true
            ? Math.Max(0, (int)getExp!.Invoke(null, new object[] { cloneName })) : fallback;
    }
    private static void Award(int xp)
    {
        if (xp <= 0) return;
        try { addExp!.Invoke(null, new object[] { xp }); }
        catch (Exception e) { log?.LogWarning($"Farming XP award failed: {e.GetBaseException().Message}"); }
    }
    private static void BeforeInteract(Pickable __instance, Humanoid character, ref string? __state)
    {
        if (character != Player.m_localPlayer || disabled?.Value != false || pickupDisabled?.Value != false ||
            __instance.GetComponent<ZNetView>() == null || !__instance.GetComponent<ZNetView>().IsValid() || (bool)Picked.GetValue(__instance) ||
            (bool)PickedLocal.GetValue(__instance)) return;
        Register();
        var zdo = __instance.GetComponent<ZNetView>().GetZDO();
        __state = zdo.m_uid.ToString();
        Ledger.Begin(__state, zdo.GetOwner(), Time.time, XpFor(__instance.name));
    }
    private static Exception? AfterInteract(Pickable __instance, string? __state, Exception? __exception)
    {
        if (__state != null && (__exception != null || !(bool)PickedLocal.GetValue(__instance)))
            Ledger.Cancel(__state);
        return __exception;
    }
    private static void BeforePick(Pickable __instance, long sender, ref HarvestState? __state)
    {
        if (__instance.GetComponent<ZNetView>() == null || !__instance.GetComponent<ZNetView>().IsValid() || !__instance.GetComponent<ZNetView>().IsOwner() ||
            (bool)Picked.GetValue(__instance)) return;
        __state = new HarvestState(__instance.GetComponent<ZNetView>().GetZDO().m_uid.ToString(), sender);
    }
    private static void AfterPick(Pickable __instance, HarvestState? __state)
    {
        if (__state != null && (bool)Picked.GetValue(__instance) && ZRoutedRpc.instance != null)
            ZRoutedRpc.instance.InvokeRoutedRPC(__state.Sender, Ack, __state.Id);
    }
    private sealed class HarvestState
    {
        internal readonly string Id;
        internal readonly long Sender;
        internal HarvestState(string id, long sender) { Id = id; Sender = sender; }
    }
    private static void AfterPlacement(Player __instance, Piece piece, bool __result)
    {
        if (__result && __instance == Player.m_localPlayer && piece != null)
            AwardPlant(piece.GetComponent<Plant>());
    }
    private static void AwardPlant(Plant? plant)
    {
        if (plant == null || disabled?.Value != false || pieceDisabled?.Value != false) return;
        // Fruit/vegetable plants only: exclude trees and decorative plants.
        foreach (var grown in plant.m_grownPrefabs)
            if (grown != null && grown.GetComponent<Pickable>() != null)
            {
                Award(XpFor(plant.name, 1));
                return;
            }
    }
    private static void EnableBulkPlacement(Harmony harmony, ManualLogSource logger)
    {
        if (!Chainloader.PluginInfos.TryGetValue("advize.PlantEasily", out var plugin) ||
            plugin.Metadata.Version != new System.Version(2, 2, 2)) return;
        var type = plugin.Instance.GetType().Assembly.GetType("Advize_PlantEasily.PlacementController");
        var method = type == null ? null : AccessTools.Method(type, "PlacePiece", new[] { typeof(Player), typeof(GameObject), typeof(GameObject) });
        if (method?.ReturnType != typeof(void))
        {
            logger.LogWarning("Bulk planting XP bridge skipped: PlantEasily placement signature changed.");
            return;
        }
        harmony.Patch(method, postfix: Patch(nameof(AfterBulkPlacement)));
    }
    private static void AfterBulkPlacement(Player player, GameObject piecePrefab)
    {
        if (player == Player.m_localPlayer && piecePrefab != null) AwardPlant(piecePrefab.GetComponent<Plant>());
    }
    private static void EnablePreviewGuard(Harmony harmony, ManualLogSource logger)
    {
        if (!Chainloader.PluginInfos.TryGetValue("org.bepinex.plugins.farming", out var farming) ||
            farming.Metadata.Version != new System.Version(2, 2, 3)) return;
        var type = farming.Instance.GetType().Assembly.GetType("Farming.Farming+SaveSkillLevel");
        var method = type == null ? null : AccessTools.Method(type, "Postfix", new[] { typeof(Plant) });
        if (method?.ReturnType != typeof(void)) return;
        harmony.Patch(method, prefix: Patch(nameof(ValidPlantOnly)));
        logger.LogInfo("Farming preview guard active for Farming 2.2.3.");
    }
    // When patching a patch, __instance would mean its C# receiver, not its Plant argument.
    private static bool ValidPlantOnly(Plant __0) => __0 != null && Player.m_localPlayer != null &&
        __0.GetComponent<ZNetView>() != null && __0.GetComponent<ZNetView>().IsValid() && __0.GetComponent<ZNetView>().GetZDO() != null;
}
