using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace RagnavikCompat;

[BepInPlugin(PluginGuid, "Ragnavik Compatibility", "1.0.17")]
[BepInDependency("WackyMole.EpicMMOSystem", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("org.bepinex.plugins.farming", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("blacks7ar.FloraCollector", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("org.bepinex.plugins.foraging", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("advize.PlantEasily", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("org.bepinex.plugins.afterdeath", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("org.bepinex.plugins.starvation", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("randyknapp.mods.epicloot", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("blacks7ar.MagicPlugin", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("Azumatt.CurrencyPocket", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("Azumatt.AzuExtendedPlayerInventory", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(ItemDrawersCustomDataCompatibility.PluginGuid, BepInDependency.DependencyFlags.SoftDependency)]
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
    private static StatusEffect? _afterdeathGhostStatus;
    private static MethodInfo? _getQuickSlotSnapshots;
    private static PropertyInfo? _quickSlotGridPos;
    private static FieldInfo? _quickSlotGridPosX;
    private static FieldInfo? _quickSlotGridPosY;
    private static readonly Dictionary<int, List<QuickSlotRestore>> PendingQuickSlotRestores = new();

    private Harmony? _harmony;
    private MethodInfo? _readJsonValues;
    private string? _folder;
    private string? _lastFingerprint;

    private void Awake()
    {
        _instance = this;
        _harmony = new Harmony(PluginGuid);
        FarmingXpCompatibility.Enable(_harmony, Logger);
        CollectorForagingCompatibility.Enable(Logger);
        EnableAfterdeathStarvationCompatibility();
        EnableAfterdeathTeleportCompatibility();
        EnableAfterdeathDoorCompatibility();
        EnableAfterdeathNearestBedCompatibility();
        EnableAzuEpiQuickSlotRecoveryCompatibility();
        EnableEpicLootMagicPluginTakeAllCompatibility();
        EnableCurrencyPocketTakeAllCompatibility();
        EnableItemDrawersCustomDataCompatibility();

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
            Logger.LogInfo($"Epic Loot and MagicPlugin Take All bridge skipped because both mods are required; installed versions are Epic Loot {epicLootVersion?.ToString() ?? "not installed"} and MagicPlugin {magicPluginVersion?.ToString() ?? "not installed"}.");
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

    private void EnableItemDrawersCustomDataCompatibility()
    {
        if (!Chainloader.PluginInfos.TryGetValue(ItemDrawersCustomDataCompatibility.PluginGuid, out var itemDrawersPlugin))
        {
            Logger.LogInfo("ItemDrawers custom-data bridge skipped because kg.ItemDrawers is not installed.");
            return;
        }

        var hasRequiredApi = ItemDrawersCustomDataBridge.HasRequiredApi();
        if (!ItemDrawersCustomDataCompatibility.Supports(itemDrawersPlugin.Metadata.Version, hasRequiredApi))
        {
            Logger.LogInfo($"ItemDrawers custom-data bridge skipped because kg.ItemDrawers {itemDrawersPlugin.Metadata.Version} does not expose the required drawer API. Review its changelog before adapting this module.");
            return;
        }

        if (ItemDrawersCustomDataBridge.TryEnable(_harmony!, Logger))
            Logger.LogInfo($"ItemDrawers custom-data bridge is active for kg.ItemDrawers {itemDrawersPlugin.Metadata.Version}. Cooked food, magic reagents, upgrades, and other custom-data items retain their data in drawers.");
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

    private void EnableAfterdeathTeleportCompatibility()
    {
        if (!Chainloader.PluginInfos.TryGetValue("org.bepinex.plugins.afterdeath", out var afterdeathPlugin) ||
            !AfterdeathTeleportCompatibility.Supports(afterdeathPlugin.Metadata.Version))
        {
            var installedVersion = afterdeathPlugin?.Metadata.Version?.ToString() ?? "not installed";
            Logger.LogInfo($"Afterdeath spirit teleport bridge skipped because Afterdeath {installedVersion} is not the supported {AfterdeathTeleportCompatibility.SupportedAfterdeathVersion} version.");
            return;
        }

        var disableTeleport = AccessTools.TypeByName("Afterdeath.BlockStuff+DisableTeleport");
        var afterdeathPrefix = disableTeleport == null
            ? null
            : AccessTools.Method(disableTeleport, "Prefix", Type.EmptyTypes);
        if (afterdeathPrefix == null || afterdeathPrefix.ReturnType != typeof(bool))
        {
            Logger.LogInfo("Afterdeath spirit teleport bridge skipped because Afterdeath's expected DisableTeleport.Prefix() signature changed. Review its changelog before adapting this module.");
            return;
        }

        _harmony!.Patch(afterdeathPrefix,
            prefix: new HarmonyMethod(typeof(RagnavikCompatPlugin), nameof(BeforeAfterdeathTeleportBlock)));
        Logger.LogInfo("Afterdeath spirit teleport bridge is active. Spirits can use portals and dungeon transitions.");
    }

    private static bool BeforeAfterdeathTeleportBlock(ref bool __result)
    {
        var player = Player.m_localPlayer;
        if (player == null || !AfterdeathTeleportCompatibility.ShouldAllowTeleport(
                player.m_customData.ContainsKey("Afterdeath Ghost"),
                player.IsDead()))
            return true;

        __result = true;
        return false;
    }

    private void EnableAfterdeathDoorCompatibility()
    {
        if (!Chainloader.PluginInfos.TryGetValue("org.bepinex.plugins.afterdeath", out var afterdeathPlugin) ||
            !AfterdeathDoorCompatibility.Supports(afterdeathPlugin.Metadata.Version))
        {
            var installedVersion = afterdeathPlugin?.Metadata.Version?.ToString() ?? "not installed";
            Logger.LogInfo($"Afterdeath spirit door bridge skipped because Afterdeath {installedVersion} is not the supported {AfterdeathDoorCompatibility.SupportedAfterdeathVersion} version.");
            return;
        }
        var disableInteractText = AccessTools.TypeByName("Afterdeath.BlockStuff+DisableInteractText");
        var afterdeathPostfix = disableInteractText == null
            ? null
            : AccessTools.Method(disableInteractText, "Postfix", new[] { typeof(Player), typeof(GameObject).MakeByRefType() });
        var afterdeathType = AccessTools.TypeByName("Afterdeath.Afterdeath");
        var ghostStatusField = afterdeathType == null ? null : AccessTools.Field(afterdeathType, "ghostStatus");
        var bedInteract = AccessTools.Method(typeof(Bed), nameof(Bed.Interact), new[] { typeof(Humanoid), typeof(bool), typeof(bool) });
        if (afterdeathPostfix == null || afterdeathPostfix.ReturnType != typeof(void) ||
            ghostStatusField == null || !typeof(StatusEffect).IsAssignableFrom(ghostStatusField.FieldType) ||
            bedInteract == null || bedInteract.ReturnType != typeof(bool))
        {
            Logger.LogInfo("Afterdeath spirit home access skipped because its interaction blocker, ghost status, or Valheim's Bed.Interact signature changed. Review the relevant changelog before adapting this module.");
            return;
        }

        _afterdeathGhostStatus = ghostStatusField.GetValue(null) as StatusEffect;
        if (_afterdeathGhostStatus == null)
        {
            Logger.LogInfo("Afterdeath spirit home access skipped because Afterdeath's ghost status is unavailable.");
            return;
        }

        _harmony!.Patch(afterdeathPostfix,
            prefix: new HarmonyMethod(typeof(RagnavikCompatPlugin), nameof(BeforeAfterdeathInteractionBlock)));
        _harmony.Patch(bedInteract,
            prefix: new HarmonyMethod(typeof(RagnavikCompatPlugin), nameof(BeforeAssignedBedInteraction)));
        Logger.LogInfo("Afterdeath spirit home access is active. Spirits can use permitted doors and resurrect at their assigned bed.");
    }

    private static bool BeforeAfterdeathInteractionBlock(Player __0, GameObject? hover)
    {
        var isDoor = hover != null && hover.GetComponentInParent<Door>() != null;
        var isAssignedBed = hover != null && IsAssignedBed(hover);
        return !AfterdeathDoorCompatibility.ShouldAllowInteraction(
            __0.m_customData.ContainsKey("Afterdeath Ghost"),
            __0.IsDead(),
            isDoor,
            isAssignedBed);
    }

    private static bool BeforeAssignedBedInteraction(Bed __instance, Humanoid __0, bool __1, ref bool __result)
    {
        if (__1 || __0 is not Player player || _afterdeathGhostStatus == null ||
            !AfterdeathDoorCompatibility.ShouldAllowInteraction(
                player.m_customData.ContainsKey("Afterdeath Ghost"),
                player.IsDead(),
                false,
                IsAssignedBed(__instance.gameObject)))
            return true;

        player.GetSEMan().RemoveStatusEffect(_afterdeathGhostStatus);
        __result = true;
        return false;
    }

    private static bool IsAssignedBed(GameObject candidate)
    {
        var profile = Game.instance?.GetPlayerProfile();
        var bed = candidate.GetComponentInParent<Bed>();
        return profile != null && profile.HaveCustomSpawnPoint() && bed != null &&
               Vector3.Distance(bed.transform.position, profile.GetCustomSpawnPoint()) <= 3f;
    }

    private void EnableAzuEpiQuickSlotRecoveryCompatibility()
    {
        if (!Chainloader.PluginInfos.TryGetValue("Azumatt.AzuExtendedPlayerInventory", out var azuEpiPlugin) ||
            !AzuEpiQuickSlotRecoveryCompatibility.Supports(azuEpiPlugin.Metadata.Version))
        {
            var installedVersion = azuEpiPlugin?.Metadata.Version?.ToString() ?? "not installed";
            Logger.LogInfo($"AzuEPI grave quick-slot recovery skipped because AzuExtendedPlayerInventory {installedVersion} is not the supported {AzuEpiQuickSlotRecoveryCompatibility.SupportedVersion} version.");
            return;
        }

        var api = AccessTools.TypeByName("AzuEPI.API");
        var slotSnapshot = AccessTools.TypeByName("AzuEPI.SlotSnapshot");
        _getQuickSlotSnapshots = api == null
            ? null
            : AccessTools.Method(api, "GetQuickSlotSnapshots", new[] { typeof(Inventory) });
        _quickSlotGridPos = slotSnapshot == null ? null : AccessTools.Property(slotSnapshot, "GridPos");
        _quickSlotGridPosX = _quickSlotGridPos == null ? null : AccessTools.Field(_quickSlotGridPos.PropertyType, "x");
        _quickSlotGridPosY = _quickSlotGridPos == null ? null : AccessTools.Field(_quickSlotGridPos.PropertyType, "y");
        var takeAll = AccessTools.Method(typeof(Container), nameof(Container.TakeAll), new[] { typeof(Humanoid) });
        var takeAllSuccess = AccessTools.Method(typeof(TombStone), "OnTakeAllSuccess");

        if (_getQuickSlotSnapshots == null ||
            !typeof(IEnumerable).IsAssignableFrom(_getQuickSlotSnapshots.ReturnType) ||
            _quickSlotGridPos == null || _quickSlotGridPosX == null || _quickSlotGridPosY == null ||
            takeAll == null || takeAll.ReturnType != typeof(void) ||
            takeAllSuccess == null || takeAllSuccess.ReturnType != typeof(void))
        {
            Logger.LogInfo("AzuEPI grave quick-slot recovery skipped because its slot snapshot API or Valheim's grave transfer signatures changed. Review the relevant changelog before adapting this module.");
            return;
        }

        _harmony!.Patch(takeAll,
            prefix: new HarmonyMethod(typeof(RagnavikCompatPlugin), nameof(CaptureGraveQuickSlots)));
        var restorePostfix = new HarmonyMethod(typeof(RagnavikCompatPlugin), nameof(RestoreGraveQuickSlots))
        {
            priority = Priority.Last
        };
        _harmony.Patch(takeAllSuccess, postfix: restorePostfix);
        Logger.LogInfo("AzuEPI grave quick-slot recovery is active. Recovered quick-slot items return to their original hotkey cells.");
    }

    private static void CaptureGraveQuickSlots(Container __instance, Humanoid __0)
    {
        var tombstone = __instance.GetComponent<TombStone>();
        var graveInventory = __instance.GetInventory();
        if (tombstone == null || __0 is not Player player || player != Player.m_localPlayer ||
            graveInventory == null || _getQuickSlotSnapshots == null || _quickSlotGridPos == null ||
            _quickSlotGridPosX == null || _quickSlotGridPosY == null)
            return;

        var restores = new List<QuickSlotRestore>();
        if (_getQuickSlotSnapshots.Invoke(null, new object[] { graveInventory }) is IEnumerable snapshots)
        {
            foreach (var snapshot in snapshots)
            {
                if (snapshot == null || _quickSlotGridPos.GetValue(snapshot) is not object gridPos ||
                    _quickSlotGridPosX.GetValue(gridPos) is not int x ||
                    _quickSlotGridPosY.GetValue(gridPos) is not int y)
                    continue;

                var item = graveInventory.GetItemAt(x, y);
                if (item != null)
                    restores.Add(new QuickSlotRestore(item, x, y));
            }
        }

        PendingQuickSlotRestores[tombstone.GetInstanceID()] = restores;
    }

    private static void RestoreGraveQuickSlots(TombStone __instance)
    {
        var key = __instance.GetInstanceID();
        if (!PendingQuickSlotRestores.TryGetValue(key, out var restores))
            return;

        PendingQuickSlotRestores.Remove(key);
        var inventory = Player.m_localPlayer?.GetInventory();
        if (inventory == null || restores.Count == 0)
            return;

        var recoveredItems = inventory.GetAllItems();
        var changed = false;
        foreach (var restore in restores)
        {
            if (!recoveredItems.Any(item => ReferenceEquals(item, restore.Item)))
                continue;

            var current = restore.Item.m_gridPos;
            if (current.x == restore.X && current.y == restore.Y)
                continue;

            var displaced = inventory.GetItemAt(restore.X, restore.Y);
            if (displaced != null && !ReferenceEquals(displaced, restore.Item))
                displaced.m_gridPos = current;

            var target = restore.Item.m_gridPos;
            target.x = restore.X;
            target.y = restore.Y;
            restore.Item.m_gridPos = target;
            changed = true;
        }

        if (changed)
            inventory.m_onChanged?.Invoke();
    }

    private sealed class QuickSlotRestore
    {
        public ItemDrop.ItemData Item { get; }
        public int X { get; }
        public int Y { get; }

        public QuickSlotRestore(ItemDrop.ItemData item, int x, int y)
        {
            Item = item;
            X = x;
            Y = y;
        }
    }

    private void EnableAfterdeathNearestBedCompatibility()
    {
        if (!Chainloader.PluginInfos.TryGetValue("org.bepinex.plugins.afterdeath", out var afterdeathPlugin) ||
            !AfterdeathNearestBedCompatibility.Supports(afterdeathPlugin.Metadata.Version))
        {
            var installedVersion = afterdeathPlugin?.Metadata.Version?.ToString() ?? "not installed";
            Logger.LogInfo($"Afterdeath nearest-bed spawn skipped because Afterdeath {installedVersion} is not the supported {AfterdeathNearestBedCompatibility.SupportedAfterdeathVersion} version.");
            return;
        }

        var afterdeathUtils = AccessTools.TypeByName("Afterdeath.Utils");
        var getClosestLocation = afterdeathUtils == null
            ? null
            : AccessTools.Method(afterdeathUtils, "GetClosestLocation", new[] { typeof(Vector3) });
        if (getClosestLocation == null || getClosestLocation.ReturnType != typeof(Vector3))
        {
            Logger.LogInfo("Afterdeath nearest-bed spawn skipped because Afterdeath's expected Utils.GetClosestLocation(Vector3) signature changed. Review its changelog before adapting this module.");
            return;
        }

        _harmony!.Patch(getClosestLocation,
            postfix: new HarmonyMethod(typeof(RagnavikCompatPlugin), nameof(ChooseNearestAfterdeathSpawn)));
        Logger.LogInfo("Afterdeath nearest-bed spawn is active. A valid bed wins when it is closer to the death point than Skathi.");
    }

    private static void ChooseNearestAfterdeathSpawn(Vector3 position, ref Vector3 __result)
    {
        var profile = Game.instance?.GetPlayerProfile();
        if (profile == null || !profile.HaveCustomSpawnPoint())
            return;

        var bedPoint = profile.GetCustomSpawnPoint();
        if (AfterdeathNearestBedCompatibility.ShouldUseBed(
                true, position.x, position.z, __result.x, __result.z, bedPoint.x, bedPoint.z))
            __result = bedPoint;
    }

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
        FarmingXpCompatibility.Disable();
        CollectorForagingCompatibility.Disable();
        _harmony?.UnpatchSelf();
        if (ReferenceEquals(_instance, this))
            _instance = null;
        _pending = false;
        _pendingEvents = 0;
        _takeAllBridgeEnabled = false;
        _takeAllPatchInstalled = false;
        _currencyPocketTakeAllEnabled = false;
        _afterdeathGhostStatus = null;
        _getPocketBalance = null;
        _updatePocketBalance = null;
        _updatePocketUi = null;
    }
}
