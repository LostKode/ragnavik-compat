using System.Runtime.CompilerServices;

// Minimal hosts for executing the production Harmony module without a Unity process.
namespace RagnavikCompat { public class RagnavikCompatPlugin { public const string PluginGuid = "test.currency-trader"; } }
namespace BepInEx.Logging { public class ManualLogSource { public void LogInfo(object s) => Console.WriteLine(s); public void LogWarning(object s) => Console.WriteLine(s); } }
namespace BepInEx.Bootstrap
{
    public static class Chainloader { public static Dictionary<string, PluginInfo> PluginInfos = new(); }
    public class PluginInfo { public Metadata Metadata = new(); }
    public class Metadata { public Version Version = new(1, 0, 15); }
}
public class ItemDrop
{
    public ItemData m_itemData = new();
    public class ItemData { public int m_stack; public ItemData Clone() => new() { m_stack = m_stack }; }
}
public class Prefab { public T GetComponent<T>() where T : new() => new(); }
public class ObjectDB { public static ObjectDB instance = new(); public Prefab GetItemPrefab(string name) => new(); }
public class Player { public static Player m_localPlayer = new(); public Inventory inventory = new(); public Inventory GetInventory() => inventory; }
public class Inventory
{
    public int Coins;
    public int Other;
    public bool Full;
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void RemoveItem(string name, int amount, int itemQuality = -1, bool worldLevelBased = true)
    { if (name == "$item_coins") Coins -= Math.Min(Coins, amount); else Other -= Math.Min(Other, amount); }
    [MethodImpl(MethodImplOptions.NoInlining)]
    public ItemDrop.ItemData AddItem(string name, int stack, int quality, int variant, long crafterId, string crafterName, bool cheated)
    { if (Full) return null!; if (name == "Coins") Coins += stack; return new() { m_stack = stack }; }
}
public class StoreGui
{
    public int Price;
    public int Sale;
    public bool Throw;
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void BuySelectedItem()
    { if (Throw) throw new InvalidOperationException("cancelled"); Player.m_localPlayer.GetInventory().RemoveItem("$item_coins", Price); }
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void SellItem() => Player.m_localPlayer.GetInventory().AddItem("Coins", Sale, 1, 0, 0L, "", false);
}
namespace CurrencyPocket
{
    public static class MiscFunctions
    {
        public static int Balance;
        public static bool FailWrite;
        public static int GetPlayerCoinsFromCustomData() => Balance;
        public static void UpdatePlayerCustomData(int value, Player player = null!) { if (FailWrite) throw new InvalidOperationException("write failed"); Balance = value; }
    }
    public static class CurrencyPocket
    {
        public static bool FailUi;
        public static void UpdatePocketUI() { if (FailUi) throw new InvalidOperationException("UI unavailable"); }
        public static class Inventory_RemoveItem_Patch
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void Postfix(Inventory __instance, string name, int amount, int itemQuality, bool worldLevelBased)
            { if (__instance == Player.m_localPlayer?.GetInventory() && name == "$item_coins" && MiscFunctions.Balance >= amount) MiscFunctions.Balance -= amount; }
        }
    }
}
namespace EpicLoot_UnityLib
{
    public class InventoryManagement
    {
        public int ProviderCoins;
        public int Tokens;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public int CountItem(string item) => item == "$item_coins" ? Player.m_localPlayer.GetInventory().Coins + ProviderCoins : Tokens;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void RemoveItem(string item, int amount)
        {
            if (item != "$item_coins") { Tokens -= amount; return; }
            var inventory = Player.m_localPlayer.GetInventory();
            int before = inventory.Coins;
            inventory.RemoveItem(item, amount);
            ProviderCoins -= Math.Min(ProviderCoins, amount - (before - inventory.Coins));
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void GiveItem(string item, int amount)
        { if (item == "Coins") Player.m_localPlayer.GetInventory().AddItem(item, amount, 1, 0, 0, "", false); else Tokens += amount; }
    }
}
namespace HarmonyLib
{
    // HarmonyX exposes this convenience method; CoreCLR Harmony uses owner-scoped UnpatchAll.
    public static class HarmonyXAdapter { public static void UnpatchSelf(this Harmony harmony) => harmony.UnpatchAll(harmony.Id); }
}
