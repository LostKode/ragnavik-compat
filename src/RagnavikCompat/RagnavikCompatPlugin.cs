using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace RagnavikCompat;

[BepInPlugin(PluginGuid, "Ragnavik Compatibility", "1.0.5")]
[BepInDependency("WackyMole.EpicMMOSystem", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("org.bepinex.plugins.afterdeath", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("org.bepinex.plugins.starvation", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("randyknapp.mods.epicloot", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("blacks7ar.MagicPlugin", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("Azumatt.CurrencyPocket", BepInDependency.DependencyFlags.SoftDependency)]
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
    private static bool _takeAllBridgeEnabled;
    private static bool _takeAllPatchInstalled;
    private static bool _currencyPocketTakeAllEnabled;
    private static MethodInfo? _getPocketBalance;
    private static MethodInfo? _updatePocketBalance;
    private static MethodInfo? _updatePocketUi;

    private Harmony? _harmony;
    private MethodInfo? _readJsonValues;
    private string? _folder;
    private string? _lastFingerprint;

    private void Awake()
    {
        _instance = this;
        _harmony = new Harmony(PluginGuid);
        EnableAfterdeathStarvationCompatibility();
        EnableEpicLootMagicPluginTakeAllCompatibility();
        EnableCurrencyPocketTakeAllCompatibility();

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
        _harmony.Patch(_readJsonValues,
            prefix: new HarmonyMethod(typeof(RagnavikCompatPlugin), nameof(BeforeJsonReload)));
        Logger.LogInfo("EpicMMO JSON reload guard is active on the dedicated server.");
    }

    private void EnableEpicLootMagicPluginTakeAllCompatibility()
    {
        Chainloader.PluginInfos.TryGetValue("randyknapp.mods.epicloot", out var epicLootPlugin);
        Chainloader.PluginInfos.TryGetValue("blacks7ar.MagicPlugin", out var magicPlugin);
        var epicLootVersion = epicLootPlugin?.Metadata.Version;
        var magicPluginVersion = magicPlugin?.Metadata.Version;

        if (!EpicLootMagicPluginTakeAllCompatibility.Supports(epicLootVersion, magicPluginVersion))
        {
            Logger.LogInfo($"Epic Loot and MagicPlugin Take All bridge skipped because the installed versions are Epic Loot {epicLootVersion?.ToString() ?? "not installed"} and MagicPlugin {magicPluginVersion?.ToString() ?? "not installed"}; expected {EpicLootMagicPluginTakeAllCompatibility.SupportedEpicLootVersion} and {EpicLootMagicPluginTakeAllCompatibility.SupportedMagicPluginVersion}.");
            return;
        }

        var moveAll = AccessTools.Method(typeof(Inventory), nameof(Inventory.MoveAll), new[] { typeof(Inventory) });
        if (moveAll == null || moveAll.ReturnType != typeof(void))
        {
            Logger.LogInfo("Epic Loot and MagicPlugin Take All bridge skipped because Valheim's expected Inventory.MoveAll(Inventory) signature changed.");
            return;
        }

        _takeAllBridgeEnabled = true;
        EnsureTakeAllPatch(moveAll);
        Logger.LogInfo("Epic Loot and MagicPlugin Take All bridge is active for all container contents.");
    }

    private void EnableCurrencyPocketTakeAllCompatibility()
    {
        if (!Chainloader.PluginInfos.TryGetValue("Azumatt.CurrencyPocket", out var currencyPocketPlugin) ||
            !CurrencyPocketTakeAllCompatibility.Supports(currencyPocketPlugin.Metadata.Version))
        {
            var installedVersion = currencyPocketPlugin?.Metadata.Version?.ToString() ?? "not installed";
            Logger.LogInfo($"CurrencyPocket Take All bridge skipped because CurrencyPocket {installedVersion} is not the supported {CurrencyPocketTakeAllCompatibility.SupportedVersion} version.");
            return;
        }

        var miscFunctions = AccessTools.TypeByName("CurrencyPocket.MiscFunctions");
        var currencyPocket = AccessTools.TypeByName("CurrencyPocket.CurrencyPocket");
        _getPocketBalance = miscFunctions == null
            ? null
            : AccessTools.Method(miscFunctions, "GetPlayerCoinsFromCustomData", Type.EmptyTypes);
        _updatePocketBalance = miscFunctions == null
            ? null
            : AccessTools.Method(miscFunctions, "UpdatePlayerCustomData", new[] { typeof(int), typeof(Player) });
        _updatePocketUi = currencyPocket == null
            ? null
            : AccessTools.Method(currencyPocket, "UpdatePocketUI", Type.EmptyTypes);

        if (_getPocketBalance?.ReturnType != typeof(int) ||
            _updatePocketBalance?.ReturnType != typeof(void) ||
            _updatePocketUi?.ReturnType != typeof(void))
        {
            Logger.LogInfo("CurrencyPocket Take All bridge skipped because the expected balance or UI methods changed. Review its changelog before adapting this module.");
            _getPocketBalance = null;
            _updatePocketBalance = null;
            _updatePocketUi = null;
            return;
        }

        _currencyPocketTakeAllEnabled = true;
        var moveAll = AccessTools.Method(typeof(Inventory), nameof(Inventory.MoveAll), new[] { typeof(Inventory) });
        if (moveAll == null || moveAll.ReturnType != typeof(void))
        {
            _currencyPocketTakeAllEnabled = false;
            Logger.LogInfo("CurrencyPocket Take All bridge skipped because Valheim's expected Inventory.MoveAll(Inventory) signature changed.");
            return;
        }

        EnsureTakeAllPatch(moveAll);
        Logger.LogInfo("CurrencyPocket Take All bridge is active. Container coins move directly into the pouch.");
    }

    private void EnsureTakeAllPatch(MethodInfo moveAll)
    {
        if (_takeAllPatchInstalled)
            return;

        _harmony!.Patch(moveAll,
            prefix: new HarmonyMethod(typeof(RagnavikCompatPlugin), nameof(BeforeMoveAll)));
        _takeAllPatchInstalled = true;
    }

    private static bool BeforeMoveAll(Inventory __instance, Inventory fromInventory)
    {
        if ((!_takeAllBridgeEnabled && !_currencyPocketTakeAllEnabled) || ReferenceEquals(__instance, fromInventory))
            return true;

        var moved = 0;
        foreach (var item in fromInventory.GetAllItems().ToArray())
        {
            if (TryMoveCoinsToCurrencyPocket(__instance, fromInventory, item))
            {
                moved++;
                continue;
            }

            if (!_takeAllBridgeEnabled)
                continue;

            // Vanilla MoveAll first clones each stack into its old source-grid coordinate.
            // The normal AddItem path moves the original object and safely leaves any
            // remainder in the source inventory when the destination cannot hold it all.
            if (__instance.AddItem(item) && fromInventory.RemoveItem(item))
                moved++;
        }

        if (moved > 0)
            _instance?.Logger.LogInfo($"Moved {moved} container stack(s) through compatible Take All paths.");
        return !_takeAllBridgeEnabled;
    }

    private static bool TryMoveCoinsToCurrencyPocket(Inventory destination, Inventory source, ItemDrop.ItemData item)
    {
        var localPlayer = Player.m_localPlayer;
        if (!_currencyPocketTakeAllEnabled || localPlayer == null ||
            !ReferenceEquals(destination, localPlayer.GetInventory()) ||
            !CurrencyPocketTakeAllCompatibility.IsCoin(item.m_shared?.m_name, item.m_stack) ||
            _getPocketBalance == null || _updatePocketBalance == null)
            return false;

        try
        {
            var currentBalance = (int)_getPocketBalance.Invoke(null, null);
            if (!CurrencyPocketTakeAllCompatibility.TryAddBalance(currentBalance, item.m_stack, out var updatedBalance))
                return false;

            _updatePocketBalance.Invoke(null, new object?[] { updatedBalance, localPlayer });
            bool removed;
            try
            {
                removed = source.RemoveItem(item);
            }
            catch (Exception error)
            {
                _updatePocketBalance.Invoke(null, new object?[] { currentBalance, localPlayer });
                _instance?.Logger.LogError($"CurrencyPocket Take All could not remove the deposited coin stack; restored the previous pouch balance: {error.GetBaseException().Message}");
                return false;
            }

            if (!removed)
            {
                _updatePocketBalance.Invoke(null, new object?[] { currentBalance, localPlayer });
                return false;
            }

            try
            {
                _updatePocketUi?.Invoke(null, null);
            }
            catch (Exception error)
            {
                _instance?.Logger.LogWarning($"Coins entered CurrencyPocket, but its UI refresh failed: {error.GetBaseException().Message}");
            }

            return true;
        }
        catch (Exception error)
        {
            _instance?.Logger.LogError($"CurrencyPocket Take All transfer failed; leaving the coin stack in the container: {error.GetBaseException().Message}");
            return false;
        }
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
        _takeAllBridgeEnabled = false;
        _takeAllPatchInstalled = false;
        _currencyPocketTakeAllEnabled = false;
        _getPocketBalance = null;
        _updatePocketBalance = null;
        _updatePocketUi = null;
    }
}
