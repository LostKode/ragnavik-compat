using System;
using System.Reflection;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace RagnavikCompat;

internal static class CollectorForagingCompatibility
{
    private static Harmony? harmony;
    private static MethodInfo? getLevel;
    private static ManualLogSource? log;

    internal static void Enable(ManualLogSource logger)
    {
        log = logger;
        if (!Chainloader.PluginInfos.TryGetValue("blacks7ar.FloraCollector", out var flora) ||
            flora.Metadata.Version != new System.Version(1, 1, 4) ||
            !Chainloader.PluginInfos.TryGetValue("org.bepinex.plugins.foraging", out var foraging) ||
            foraging.Metadata.Version != new System.Version(1, 0, 11))
        {
            logger.LogInfo("Collector XP bridge skipped: requires FloraCollector 1.1.4 and Foraging 1.0.11.");
            return;
        }
        harmony = new Harmony(RagnavikCompatPlugin.PluginGuid + ".collectorforaging");
        try
        {
            var assembly = flora.Instance.GetType().Assembly;
            var collector = assembly.GetType("FloraCollector.Function.Collector");
            var patches = assembly.GetType("FloraCollector.Function.Patches");
            if (collector == null || patches == null || !typeof(Component).IsAssignableFrom(collector))
                throw new MissingMemberException("Collector types changed");
            getLevel = AccessTools.Method(collector, "GetLevel", Type.EmptyTypes);
            var extract = AccessTools.Method(collector, "RPC_Extract", new[] { typeof(long) });
            var oldReward = AccessTools.Method(patches, "Interact_Prefix", new[] { collector.MakeByRefType(), typeof(Humanoid) });
            var registration = foraging.Instance.GetType().Assembly.GetType("Foraging.Foraging+PlayerAwake");
            var register = registration == null ? null : AccessTools.Method(registration, "Postfix", new[] { typeof(Player) });
            if (getLevel?.ReturnType != typeof(int) || extract?.ReturnType != typeof(void) ||
                oldReward?.ReturnType != typeof(void) || !oldReward.IsStatic || register?.ReturnType != typeof(void))
                throw new MissingMethodException("Collector or Foraging XP signatures changed");
            harmony.Patch(oldReward, prefix: new HarmonyMethod(typeof(CollectorForagingCompatibility), nameof(SkipOldReward)));
            harmony.Patch(extract,
                prefix: new HarmonyMethod(typeof(CollectorForagingCompatibility), nameof(BeforeExtract)),
                postfix: new HarmonyMethod(typeof(CollectorForagingCompatibility), nameof(AfterExtract)));
            logger.LogInfo("Collector XP bridge active: one normal Foraging award per successfully extracted item.");
        }
        catch (Exception error)
        {
            harmony.UnpatchSelf();
            logger.LogWarning($"Collector XP bridge disabled: {error.GetBaseException().Message}");
        }
    }

    internal static void Disable() => harmony?.UnpatchSelf();
    private static bool SkipOldReward() => false;

    private static void BeforeExtract(Component __instance, long __0, ref Extraction? __state)
    {
        try
        {
            var view = __instance.GetComponent<ZNetView>();
            if (view == null || !view.IsValid() || !view.IsOwner()) return;
            var count = (int)getLevel!.Invoke(__instance, null);
            if (count <= 0) return;
            // The native Foraging harvest hook identifies the requesting player by ZDO creator.
            foreach (var player in Player.GetAllPlayers())
            {
                var playerView = player.GetComponent<ZNetView>();
                if (playerView != null && playerView.IsValid() && playerView.GetZDO().m_uid.UserID == __0)
                {
                    __state = new Extraction(count, playerView);
                    return;
                }
            }
        }
        catch (Exception error) { log?.LogWarning($"Collector XP capture failed: {error.GetBaseException().Message}"); }
    }

    private static void AfterExtract(Component __instance, Extraction? __state)
    {
        if (__state == null) return;
        try
        {
            var after = (int)getLevel!.Invoke(__instance, null);
            var count = CollectorXpPolicy.ConfirmedUnits(__state.Count, after, true);
            if (__state.PlayerView == null || !__state.PlayerView.IsValid()) return;
            // Send individual vanilla-sized skill gains to the player's owner. This preserves
            // level-up behavior, Foraging's XP setting, and Gameplay's 100%/50% profession rule.
            for (var i = 0; i < count; i++)
                __state.PlayerView.InvokeRPC("Foraging IncreaseSkill", new object[] { 1 });
        }
        catch (Exception error) { log?.LogWarning($"Collector XP award failed: {error.GetBaseException().Message}"); }
    }

    private sealed class Extraction
    {
        internal readonly int Count;
        internal readonly ZNetView PlayerView;
        internal Extraction(int count, ZNetView playerView) { Count = count; PlayerView = playerView; }
    }
}
