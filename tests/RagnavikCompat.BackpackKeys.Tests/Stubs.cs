// Minimal doubles execute the actual bridge without loading Unity. These
// tests cover bridge decisions; actual assembly signatures are checked by builds
// and decompilation, and gameplay still requires an in-game smoke test.
using System.Reflection;

public class Inventory
{
    public static int WorldLevel;
    public List<(string Name, int Level)> Items = new();
    public List<Inventory> Backpacks = new();
    public bool HaveItem(string name, bool matchWorldLevel) => Items.Any(i => i.Name == name && (!matchWorldLevel || i.Level >= WorldLevel));
}
public class Humanoid { public Inventory Inventory = new(); public Inventory GetInventory() => Inventory; }
public class Player : Humanoid { public static Player? m_localPlayer; }
public class ItemDrop
{
    public ItemData m_itemData = new();
    public class ItemData { public SharedData m_shared = new(); }
    public class SharedData { public string m_name = "$item_cryptkey"; }
}
public class Door
{
    public ItemDrop? m_keyItem;
    public string m_name = "$piece_cryptgate";
    public bool m_consumeKey;
    private bool HaveKey(Humanoid player, bool matchWorldLevel = true) => true;
}
namespace Backpacks
{
    public class Plugin { }
    public static class API
    {
        public static bool Throw;
        public static List<Inventory> GetAllBackpackInventories(Inventory inventory) => Throw ? throw new Exception("test failure") : inventory.Backpacks;
    }
}
namespace OreMines
{
    public class Plugin { public static BepInEx.Configuration.ConfigEntryBase _consumableKeys = new(); }
}
namespace BepInEx.Configuration { public class ConfigEntryBase { public object BoxedValue = "Off"; } }
namespace BepInEx.Bootstrap
{
    public static class Chainloader { public static Dictionary<string, PluginInfo> PluginInfos = new(); }
    public class PluginInfo
    {
        public object Instance;
        public PluginMetadata Metadata;
        public PluginInfo(object instance, string version) { Instance = instance; Metadata = new() { Version = new Version(version) }; }
    }
    public class PluginMetadata { public Version Version = new(0, 0); }
}
namespace BepInEx.Logging
{
    public class ManualLogSource
    {
        public List<string> Warnings = new();
        public void LogInfo(object value) { }
        public void LogWarning(object value) => Warnings.Add(value.ToString()!);
    }
}
namespace HarmonyLib
{
    public class Harmony
    {
        public string Id;
        public static MethodInfo? Target;
        public Harmony(string id) => Id = id;
        public void Patch(MethodInfo method, HarmonyMethod postfix) => Target = method;
        public void UnpatchSelf() => Target = null;
    }
    public class HarmonyMethod { public HarmonyMethod(Type type, string name) { } }
    public static class AccessTools
    {
        public static MethodInfo? DeclaredMethod(Type type, string name, Type[] args) => type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, args, null);
        public static FieldInfo? Field(Type type, string name) => type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
    }
}
