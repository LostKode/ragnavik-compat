using System.Diagnostics;
using System.Runtime.CompilerServices;
namespace UnityEngine {
 public class Vector3 { public float x; public static float Distance(Vector3 a, Vector3 b) => Math.Abs(a.x-b.x); }
 public class Transform { public Vector3 position = new(); }
 public class GameObject { public string name=""; public Transform transform = new(); public ZNetView view=null!; }
 public class Component { public GameObject gameObject=new(); public Transform transform=>gameObject.transform; public T GetComponent<T>() where T:class => gameObject.view as T ?? null!; }
}
public class ZDO {
 public Dictionary<string,object> Data=new();
 public string GetString(string key,string fallback="") => Data.TryGetValue(key,out var value)?(string)value:fallback;
 public int GetInt(string key,int fallback=0) => Data.TryGetValue(key,out var value)?(int)value:fallback;
 public void Set(string key,int value)=>Data[key]=value;
}
public class ZNetView : UnityEngine.Component {
 public const long Everybody=0;
 public bool Owner=true,Valid=true; public Smelter? Machine; public ZDO Zdo=new();
 public bool IsOwner()=>Owner; public bool IsValid()=>Valid; public ZDO GetZDO()=>Zdo;
 public void InvokeRPC(long peer,string method,params object[] args) { }
 public void InvokeRPC(string method,params object[] args) {
  if(Machine==null)return;
  if(Machine.ThrowBefore)throw new Exception("before acceptance");
  if(!Machine.Reject){if(method=="RPC_AddFuel")Machine.Fuel++;else Machine.Queue++;}
  if(Machine.ThrowAfter)throw new Exception("after acceptance");
 }
}
public class ZPackage {
 private readonly BinaryReader reader;
 public ZPackage(byte[] bytes)=>reader=new(new MemoryStream(bytes));
 public int ReadInt()=>reader.ReadInt32();
}
public class Player { public static Player? m_localPlayer=new(); }
public static class PrivateArea { public static bool Allowed=true; public static bool CheckAccess(UnityEngine.Vector3 p,float r,bool flash,bool ward)=>Allowed; }
public class ItemDrop : UnityEngine.Component { }
public class Smelter : UnityEngine.Component {
 public string m_name="$piece_smelter"; public int m_maxOre=10,m_maxFuel=10,Queue; public float Fuel;
 public bool Reject,ThrowBefore,ThrowAfter; public ItemDrop m_fuelItem=new();
 public class ItemConversion { public ItemDrop m_from=new(); }
 public List<ItemConversion> m_conversion=new();
 public Smelter(){gameObject.view=new(){Machine=this};m_fuelItem.gameObject.name="Coal";var c=new ItemConversion();c.m_from.gameObject.name="CopperOre";m_conversion.Add(c);}
 private float GetFuel()=>Fuel; private int GetQueueSize()=>Queue;
 private void RPC_AddFuel(long sender){} private void RPC_AddOre(long sender,string ore,bool cheated){}
}
namespace BepInEx.Configuration {
 public class ConfigEntryBase { public object BoxedValue; public Type SettingType=>BoxedValue.GetType();public ConfigEntryBase(object value)=>BoxedValue=value; }
}
namespace BepInEx.Logging { public class ManualLogSource { public void LogInfo(object s)=>Console.WriteLine(s);public void LogWarning(object s)=>Console.WriteLine(s); } }
namespace BepInEx.Bootstrap {
 public class Metadata { public Version Version=new(1,2,4); }
 public class Info { public Metadata Metadata=new();public object Instance=new LazyVikings.Plugin(); }
 public static class Chainloader { public static Dictionary<string,Info> PluginInfos=new(); }
}
namespace RagnavikCompat { public static class RagnavikCompatPlugin { public const string PluginGuid="tests"; } }
namespace LazyVikings {
 using BepInEx.Configuration;
 public enum Toggle { Off,On } public enum Automation { Deposit,Fuel,Both }
 public class Plugin {
  public static ConfigEntryBase _enableSmelter=new(Toggle.On),_smelterAutomation=new(Automation.Both),_smelterRadius=new(5f),_smelterIgnorePrivateAreaCheck=new(Toggle.On),
  _enableBlastFurnace=new(Toggle.On),_blastfurnaceAutomation=new(Automation.Both),_blastfurnaceRadius=new(5f),_blastfurnaceIgnorePrivateAreaCheck=new(Toggle.On),_leaveOne=new(Toggle.Off);
 }
}
namespace LazyVikings.Utils { public static class Helper { public static Stopwatch Timer=new(); public static Stopwatch GetGameObjectStopwatch(UnityEngine.GameObject go)=>Timer; } }
namespace LazyVikings.Patches {
 public static class SmelterPatch {
  public static bool ChestFill;
  [MethodImpl(MethodImplOptions.NoInlining)]
  public static void UpdateSmelter_Prefix(Smelter machine){if(Utils.Helper.Timer.IsRunning && Utils.Helper.Timer.ElapsedMilliseconds<1000)return;Utils.Helper.Timer.Restart();if(ChestFill){machine.Queue=machine.m_maxOre;machine.Fuel=machine.m_maxFuel;}}
 }
}
namespace API { public static class ClientSideV2 { public static List<ZNetView> Views=new();public static List<ZNetView> AllDrawers()=>Views; } }
namespace HarmonyLib { public static class HarmonyXAdapter { public static void UnpatchSelf(this Harmony harmony)=>harmony.UnpatchAll(harmony.Id); } }
