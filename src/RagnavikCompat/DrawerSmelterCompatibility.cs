using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace RagnavikCompat;

// Extend the verified LazyVikings refill tick after its normal chest handling.
// Drawers are not Containers and therefore never enter its inventory search.
internal static class DrawerSmelterCompatibility
{
    internal const string LazyGuid = "blacks7ar.LazyVikings";
    private static Harmony? patches;
    private static ManualLogSource? log;
    private static MethodInfo allDrawers = null!, stopwatch = null!, getFuel = null!, getQueue = null!;
    private static readonly Dictionary<string, ConfigEntryBase> settings = new();

    internal static void Enable(ManualLogSource logger)
    {
        log = logger;
        if (!Chainloader.PluginInfos.TryGetValue(LazyGuid, out var lazy) ||
            !Chainloader.PluginInfos.TryGetValue("kg.ItemDrawers", out var drawers) ||
            lazy.Metadata.Version != new Version(1, 2, 4) || drawers.Metadata.Version != new Version(1, 4, 0))
        {
            logger.LogInfo("Drawer smelter bridge skipped: requires LazyVikings 1.2.4 and KG ItemDrawers 1.4.0.");
            return;
        }
        try
        {
            var plugin = lazy.Instance.GetType();
            var assembly = plugin.Assembly;
            var hook = assembly.GetType("LazyVikings.Patches.SmelterPatch");
            var helper = assembly.GetType("LazyVikings.Utils.Helper");
            var api = drawers.Instance.GetType().Assembly.GetType("API.ClientSideV2");
            var target = hook == null ? null : AccessTools.DeclaredMethod(hook, "UpdateSmelter_Prefix", new[] { typeof(Smelter) });
            stopwatch = helper == null ? null! : AccessTools.DeclaredMethod(helper, "GetGameObjectStopwatch", new[] { typeof(GameObject) });
            allDrawers = api == null ? null! : AccessTools.DeclaredMethod(api, "AllDrawers", Type.EmptyTypes);
            if (target?.IsStatic != true || target.ReturnType != typeof(void) ||
                stopwatch?.IsStatic != true || stopwatch.ReturnType != typeof(Stopwatch) ||
                allDrawers?.IsStatic != true || allDrawers.ReturnType != typeof(List<ZNetView>))
                throw new InvalidOperationException("Upstream refill or drawer API changed.");
            getFuel = AccessTools.DeclaredMethod(typeof(Smelter), "GetFuel", Type.EmptyTypes);
            getQueue = AccessTools.DeclaredMethod(typeof(Smelter), "GetQueueSize", Type.EmptyTypes);
            if (getFuel?.ReturnType != typeof(float) || getQueue?.ReturnType != typeof(int) ||
                AccessTools.DeclaredMethod(typeof(Smelter), "RPC_AddFuel", new[] { typeof(long) })?.ReturnType != typeof(void) ||
                AccessTools.DeclaredMethod(typeof(Smelter), "RPC_AddOre", new[] { typeof(long), typeof(string), typeof(bool) })?.ReturnType != typeof(void))
                throw new InvalidOperationException("Valheim smelter API changed.");
            foreach (var key in new[] { "_enableSmelter", "_smelterAutomation", "_smelterRadius", "_smelterIgnorePrivateAreaCheck",
                         "_enableBlastFurnace", "_blastfurnaceAutomation", "_blastfurnaceRadius", "_blastfurnaceIgnorePrivateAreaCheck", "_leaveOne" })
            {
                if (AccessTools.Field(plugin, key)?.GetValue(null) is not ConfigEntryBase entry ||
                    (key.EndsWith("Radius") ? entry.SettingType != typeof(float) : !entry.SettingType.IsEnum))
                    throw new InvalidOperationException($"Upstream setting {key} changed.");
                settings[key] = entry;
            }
            patches = new Harmony(RagnavikCompatPlugin.PluginGuid + ".drawersmelter");
            patches.Patch(target,
                prefix: new HarmonyMethod(typeof(DrawerSmelterCompatibility), nameof(BeforeRefill)),
                postfix: new HarmonyMethod(typeof(DrawerSmelterCompatibility), nameof(AfterRefill)));
            logger.LogInfo("Drawer smelter bridge active: owned nearby drawers supply smelters and blast furnaces after chest refill.");
        }
        catch (Exception error)
        {
            Disable();
            logger.LogWarning($"Drawer smelter bridge disabled: {error.GetBaseException().Message}");
        }
    }

    internal static void Disable() { patches?.UnpatchSelf(); patches = null; settings.Clear(); }

    private static float Fuel(Smelter machine) => (float)getFuel.Invoke(machine, null)!;
    private static int Queue(Smelter machine) => (int)getQueue.Invoke(machine, null)!;

    private static bool On(string name) => settings[name].BoxedValue.ToString() == "On";
    private static bool Owns(Smelter machine) => machine != null && Player.m_localPlayer != null &&
        machine.GetComponent<ZNetView>() != null && machine.GetComponent<ZNetView>().IsValid() && machine.GetComponent<ZNetView>().IsOwner();

    private static void BeforeRefill(Smelter __0, out bool __state)
    {
        __state = false;
        if (!Owns(__0) || (__0.m_name != "$piece_smelter" && __0.m_name != "$piece_blastfurnace")) return;
        try
        {
            var timer = (Stopwatch)stopwatch.Invoke(null, new object[] { __0.gameObject })!;
            __state = !timer.IsRunning || timer.ElapsedMilliseconds >= 1000;
        }
        catch (Exception error) { Fail(error); }
    }

    private static void AfterRefill(Smelter __0, bool __state)
    {
        if (!__state || !Owns(__0) || patches == null) return;
        try
        {
            bool furnace = __0.m_name == "$piece_blastfurnace";
            string prefix = furnace ? "_blastfurnace" : "_smelter";
            if (!On(furnace ? "_enableBlastFurnace" : "_enableSmelter") ||
                settings[prefix + "Automation"].BoxedValue.ToString() is not ("Fuel" or "Both")) return;
            float radius = Math.Min(50f, Math.Max(1f, (float)settings[prefix + "Radius"].BoxedValue));
            bool checkWard = !On(prefix + "IgnorePrivateAreaCheck");
            int reserve = On("_leaveOne") ? 1 : 0;
            var nearby = ((List<ZNetView>)allDrawers.Invoke(null, null)!)
                .Where(view => view != null && view.IsValid() && view.IsOwner() &&
                    Vector3.Distance(view.transform.position, __0.transform.position) <= radius &&
                    (!checkWard || PrivateArea.CheckAccess(view.transform.position, 0f, false, true)))
                .OrderBy(view => Vector3.Distance(view.transform.position, __0.transform.position)).ToArray();
            bool fueled = false, fed = false;
            foreach (var view in nearby)
            {
                if (!Owns(__0) || !view.IsValid() || !view.IsOwner()) return;
                var zdo = view.GetZDO();
                if (HasProtectedItems(zdo.GetString("Ragnavik_CustomItems", "")) || zdo.GetInt("Quality", 1) != 1) continue;
                string prefab = zdo.GetString("Prefab", "");
                if (!fueled && __0.m_fuelItem != null && prefab == __0.m_fuelItem.gameObject.name &&
                    Math.Ceiling(Fuel(__0)) < __0.m_maxFuel)
                    fueled = Transfer(__0, view, prefab, reserve, true);
                if (fed || Queue(__0) >= __0.m_maxOre) continue;
                if (__0.m_conversion.Any(conversion => conversion.m_from != null && conversion.m_from.gameObject.name == prefab))
                    fed = Transfer(__0, view, prefab, reserve, false);
                if (fueled && fed) break;
            }
        }
        catch (Exception error) { Fail(error); }
    }

    // No custom-data item may pass through the drawer's lossy bulk API. An empty
    // versioned record is safe; malformed data remains protected, not discarded.
    internal static bool HasProtectedItems(string payload)
    {
        if (string.IsNullOrEmpty(payload)) return false;
        try
        {
            var package = new ZPackage(Convert.FromBase64String(payload));
            return package.ReadInt() != 1 || package.ReadInt() != 0;
        }
        catch { return true; }
    }

    private static bool Transfer(Smelter machine, ZNetView drawer, string prefab, int reserve, bool fuel)
    {
        var zdo = drawer.GetZDO();
        int amount = zdo.GetInt("Amount", 0);
        if (amount <= reserve) return false;
        float before = fuel ? Fuel(machine) : Queue(machine);
        zdo.Set("Amount", amount - 1);
        bool accepted = false;
        try
        {
            // Both objects are locally owned, so the normal game RPC executes
            // synchronously. Never claim a remote drawer or queue a blind debit.
            if (fuel) machine.GetComponent<ZNetView>().InvokeRPC("RPC_AddFuel", Array.Empty<object>());
            else machine.GetComponent<ZNetView>().InvokeRPC("RPC_AddOre", new object[] { prefab, false });
        }
        finally
        {
            accepted = (fuel ? Fuel(machine) : Queue(machine)) > before;
            if (!accepted) zdo.Set("Amount", amount);
            drawer.InvokeRPC(ZNetView.Everybody, "UpdateIcon", new object[] { prefab, zdo.GetInt("Amount", 0), 1 });
        }
        return accepted;
    }

    private static void Fail(Exception error)
    {
        Disable();
        log?.LogWarning($"Drawer smelter bridge disabled after a runtime error: {error.GetBaseException().Message}");
    }
}
