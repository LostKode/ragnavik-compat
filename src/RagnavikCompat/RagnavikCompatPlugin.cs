using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace RagnavikCompat;

[BepInPlugin(PluginGuid, "Ragnavik Compatibility", "1.0.2")]
[BepInDependency("WackyMole.EpicMMOSystem", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("org.bepinex.plugins.afterdeath", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("org.bepinex.plugins.starvation", BepInDependency.DependencyFlags.SoftDependency)]
public sealed class RagnavikCompatPlugin : BaseUnityPlugin
{
    public const string PluginGuid = "lostkode.ragnavik.compat";
    private const float QuietSeconds = 1.5f;

    private static RagnavikCompatPlugin? _instance;
    private static bool _allowOriginal;
    private static object? _epicMmoInstance;
    private static bool _pending;
    private static int _pendingEvents;
    private static float _lastEventAt;

    private Harmony? _harmony;
    private MethodInfo? _readJsonValues;
    private string? _folder;
    private string? _lastFingerprint;

    private void Awake()
    {
        _harmony = new Harmony(PluginGuid);
        EnableAfterdeathStarvationCompatibility();

        // The server pack is also installed on clients. Never alter their EpicMMO watcher.
        if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
            return;

        if (!Chainloader.PluginInfos.TryGetValue("WackyMole.EpicMMOSystem", out var epicMmoPlugin) ||
            !EpicMmoReloadGuardCompatibility.Supports(epicMmoPlugin.Metadata.Version))
        {
            var installedVersion = epicMmoPlugin?.Metadata.Version?.ToString() ?? "not installed";
            Logger.LogInfo($"EpicMMO reload guard skipped because WackyEpicMMOSystem {installedVersion} is not the supported {EpicMmoReloadGuardCompatibility.SupportedVersion} version.");
            return;
        }

        var epicMmo = AccessTools.TypeByName("EpicMMOSystem.EpicMMOSystem");
        _readJsonValues = epicMmo == null ? null : AccessTools.Method(epicMmo, "ReadJsonValues");
        var watcherParameters = _readJsonValues?.GetParameters();
        if (_readJsonValues == null || watcherParameters == null || watcherParameters.Length != 2 ||
            watcherParameters[0].Name != "sender" || watcherParameters[1].Name != "e" ||
            !typeof(FileSystemEventArgs).IsAssignableFrom(watcherParameters[1].ParameterType))
        {
            Logger.LogInfo("EpicMMO reload guard skipped because EpicMMO is absent or the expected ReadJsonValues(sender, e) signature changed. Review its changelog before adapting this module.");
            return;
        }

        _folder = Path.Combine(Paths.ConfigPath, "EpicMMOSystem");
        _lastFingerprint = JsonFolderFingerprint.Compute(_folder);
        _instance = this;
        _harmony.Patch(_readJsonValues,
            prefix: new HarmonyMethod(typeof(RagnavikCompatPlugin), nameof(BeforeJsonReload)));
        Logger.LogInfo("EpicMMO JSON reload guard is active on the dedicated server.");
    }

    private void EnableAfterdeathStarvationCompatibility()
    {
        Chainloader.PluginInfos.TryGetValue("org.bepinex.plugins.afterdeath", out var afterdeathPlugin);
        Chainloader.PluginInfos.TryGetValue("org.bepinex.plugins.starvation", out var starvationPlugin);
        var afterdeathVersion = afterdeathPlugin?.Metadata.Version;
        var starvationVersion = starvationPlugin?.Metadata.Version;

        if (!AfterdeathStarvationCompatibility.Supports(afterdeathVersion, starvationVersion))
        {
            Logger.LogInfo($"Afterdeath starvation guard skipped because the installed versions are Afterdeath {afterdeathVersion?.ToString() ?? "not installed"} and Starvation {starvationVersion?.ToString() ?? "not installed"}; expected {AfterdeathStarvationCompatibility.SupportedAfterdeathVersion} and {AfterdeathStarvationCompatibility.SupportedStarvationVersion}.");
            return;
        }

        var damagePlayer = AccessTools.TypeByName("Starvation.Starvation+DamagePlayer");
        var starvationPrefix = damagePlayer == null
            ? null
            : AccessTools.Method(damagePlayer, "Prefix", new[] { typeof(Player), typeof(float), typeof(bool) });
        if (starvationPrefix == null || starvationPrefix.ReturnType != typeof(void))
        {
            Logger.LogInfo("Afterdeath starvation guard skipped because Starvation's expected DamagePlayer.Prefix(Player, float, bool) signature changed. Review its changelog before adapting this module.");
            return;
        }

        _harmony!.Patch(starvationPrefix,
            prefix: new HarmonyMethod(typeof(RagnavikCompatPlugin), nameof(BeforeStarvationDamage)));
        Logger.LogInfo("Afterdeath starvation guard is active. Spirits no longer take starvation damage.");
    }

    private static bool BeforeStarvationDamage(Player __0) => AfterdeathStarvationCompatibility.ShouldRunStarvation(
        __0.m_customData.ContainsKey("Afterdeath Ghost"),
        __0.IsDead());

    private static bool BeforeJsonReload(object __instance, FileSystemEventArgs __1)
    {
        if (_allowOriginal || _instance == null)
            return true;

        if (__1 == null)
            return false;

        _epicMmoInstance = __instance;
        _lastEventAt = Time.realtimeSinceStartup;
        _pendingEvents++;
        _pending = true;
        return false;
    }

    private void Update()
    {
        if (!_pending || _folder == null || _readJsonValues == null ||
            Time.realtimeSinceStartup - _lastEventAt < QuietSeconds)
            return;

        _pending = false;
        var eventCount = _pendingEvents;
        _pendingEvents = 0;

        string currentFingerprint;
        try
        {
            currentFingerprint = JsonFolderFingerprint.Compute(_folder);
        }
        catch (Exception error)
        {
            Logger.LogError($"Cannot fingerprint EpicMMO JSON files; retrying: {error}");
            _pending = true;
            _lastEventAt = Time.realtimeSinceStartup;
            return;
        }

        if (string.Equals(currentFingerprint, _lastFingerprint, StringComparison.Ordinal))
        {
            Logger.LogInfo($"Ignored {eventCount} EpicMMO JSON metadata-only event(s).");
            return;
        }

        if (_epicMmoInstance == null)
            return;

        try
        {
            _allowOriginal = true;
            _readJsonValues.Invoke(_epicMmoInstance, new object?[] { null, null });
            _lastFingerprint = currentFingerprint;
            Logger.LogInfo($"Coalesced {eventCount} EpicMMO JSON event(s) into one content reload.");
        }
        catch (Exception error)
        {
            Logger.LogError($"EpicMMO JSON reload failed; retrying: {error}");
            _pending = true;
            _lastEventAt = Time.realtimeSinceStartup;
        }
        finally
        {
            _allowOriginal = false;
        }
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
        if (ReferenceEquals(_instance, this))
            _instance = null;
        _pending = false;
        _pendingEvents = 0;
    }
}
