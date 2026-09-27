using HarmonyLib;
using RagnavikCompat;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using LazyVikings;
using LazyVikings.Patches;
using LazyVikings.Utils;
using API;

int passed=0;
void Check(bool value,string text){if(!value)throw new Exception(text);Console.WriteLine("PASS "+text);passed++;}
void Enable(){DrawerSmelterCompatibility.Enable(new ManualLogSource());}
ZNetView Drawer(string prefab,int amount,float distance=1){var d=new ZNetView();d.Zdo.Data["Prefab"]=prefab;d.Zdo.Set("Amount",amount);d.transform.position.x=distance;ClientSideV2.Views.Add(d);return d;}
Smelter Reset(){Helper.Timer.Reset();ClientSideV2.Views.Clear();Player.m_localPlayer=new();PrivateArea.Allowed=true;SmelterPatch.ChestFill=false;Plugin._enableSmelter.BoxedValue=Toggle.On;Plugin._smelterAutomation.BoxedValue=Automation.Both;Plugin._leaveOne.BoxedValue=Toggle.Off;Plugin._smelterIgnorePrivateAreaCheck.BoxedValue=Toggle.On;return new();}
Chainloader.PluginInfos[DrawerSmelterCompatibility.LazyGuid]=new();
Chainloader.PluginInfos["kg.ItemDrawers"]=new(){Metadata=new(){Version=new(1,4,0)}};
var upstream = new HarmonyLib.Harmony("tests.lazy.upstream");
upstream.Patch(HarmonyLib.AccessTools.Method(typeof(Smelter), "UpdateSmelter"), prefix: new HarmonyLib.HarmonyMethod(typeof(SmelterPatch), "UpdateSmelter_Prefix"));
Enable();
var actual=Reset();Drawer("Coal",2);Drawer("CopperOre",2);actual.UpdateSmelter();
Check(actual.Fuel==1&&actual.Queue==1,"bridge runs through an already installed upstream Harmony prefix");
var m=Reset();var coal=Drawer("Coal",4);var ore=Drawer("CopperOre",4);SmelterPatch.UpdateSmelter_Prefix(m);
Check(m.Fuel==1&&m.Queue==1&&coal.Zdo.GetInt("Amount")==3&&ore.Zdo.GetInt("Amount")==3,"fuel and ore debited exactly once");
SmelterPatch.UpdateSmelter_Prefix(m);Check(m.Fuel==1&&m.Queue==1,"upstream one second throttle respected");
m=Reset();m.m_name="$piece_blastfurnace";ore=Drawer("CopperOre",2);SmelterPatch.UpdateSmelter_Prefix(m);Check(m.Queue==1,"blast furnace supported");
m=Reset();m.m_name="$piece_charcoalkiln";Drawer("CopperOre",2);SmelterPatch.UpdateSmelter_Prefix(m);Check(m.Queue==0,"other machine types untouched");
m=Reset();m.Queue=m.m_maxOre;m.Fuel=9.5f;coal=Drawer("Coal",2);ore=Drawer("CopperOre",2);SmelterPatch.UpdateSmelter_Prefix(m);Check(coal.Zdo.GetInt("Amount")==2&&ore.Zdo.GetInt("Amount")==2,"full queue and fractional fuel cannot overfill");
m=Reset();SmelterPatch.ChestFill=true;ore=Drawer("CopperOre",2);SmelterPatch.UpdateSmelter_Prefix(m);Check(ore.Zdo.GetInt("Amount")==2,"chest refill takes precedence and capacity is reread");
m=Reset();ore=Drawer("CopperOre",1);coal=Drawer("Coal",1);Plugin._leaveOne.BoxedValue=Toggle.On;SmelterPatch.UpdateSmelter_Prefix(m);Check(m.Queue==0&&m.Fuel==0,"leave one preserves final ore and fuel");
m=Reset();ore=Drawer("CopperOre",1);SmelterPatch.UpdateSmelter_Prefix(m);Check(m.Queue==1&&ore.Zdo.GetInt("Amount")==0,"last item consumed when leave one is off");
m=Reset();Drawer("CopperOre",2,6);SmelterPatch.UpdateSmelter_Prefix(m);Check(m.Queue==0,"out of range drawers excluded");
m=Reset();Drawer("CopperOre",2,5);SmelterPatch.UpdateSmelter_Prefix(m);Check(m.Queue==1,"range boundary included");
m=Reset();ore=Drawer("CopperOre",2);ore.Owner=false;SmelterPatch.UpdateSmelter_Prefix(m);Check(m.Queue==0&&ore.Zdo.GetInt("Amount")==2,"remote drawer never claimed or debited");
m=Reset();m.gameObject.view.Owner=false;Drawer("CopperOre",2);SmelterPatch.UpdateSmelter_Prefix(m);Check(m.Queue==0,"nonowner machine does not feed");
m=Reset();Player.m_localPlayer=null;Drawer("CopperOre",2);SmelterPatch.UpdateSmelter_Prefix(m);Check(m.Queue==0,"dedicated server does not compete with client automation");
m=Reset();PrivateArea.Allowed=false;Plugin._smelterIgnorePrivateAreaCheck.BoxedValue=Toggle.Off;Drawer("CopperOre",2);SmelterPatch.UpdateSmelter_Prefix(m);Check(m.Queue==0,"ward exclusion honored");
m=Reset();Plugin._smelterAutomation.BoxedValue=Automation.Deposit;Drawer("CopperOre",2);SmelterPatch.UpdateSmelter_Prefix(m);Check(m.Queue==0,"deposit-only mode does not feed");
m=Reset();Plugin._enableSmelter.BoxedValue=Toggle.Off;Drawer("CopperOre",2);SmelterPatch.UpdateSmelter_Prefix(m);Check(m.Queue==0,"disabled smelter does not feed");
m=Reset();ore=Drawer("CopperOre",2);ore.Zdo.Set("Quality",2);SmelterPatch.UpdateSmelter_Prefix(m);Check(m.Queue==0,"upgraded items protected");
m=Reset();ore=Drawer("CopperOre",2);ore.Zdo.Data["Ragnavik_CustomItems"]="corrupt";SmelterPatch.UpdateSmelter_Prefix(m);Check(m.Queue==0,"malformed custom data fails closed");
Check(!DrawerSmelterCompatibility.HasProtectedItems(Convert.ToBase64String(new byte[]{1,0,0,0,0,0,0,0})),"empty metadata ledger remains usable");
Check(DrawerSmelterCompatibility.HasProtectedItems(Convert.ToBase64String(new byte[]{1,0,0,0,1,0,0,0})),"nonempty metadata ledger protected");
m=Reset();Drawer("Iron",2);SmelterPatch.UpdateSmelter_Prefix(m);Check(m.Queue==0,"unsupported material not consumed");
m=Reset();m.Reject=true;ore=Drawer("CopperOre",2);SmelterPatch.UpdateSmelter_Prefix(m);Check(m.Queue==0&&ore.Zdo.GetInt("Amount")==2,"rejected RPC restores drawer item");
m=Reset();m.ThrowBefore=true;ore=Drawer("CopperOre",2);SmelterPatch.UpdateSmelter_Prefix(m);Check(m.Queue==0&&ore.Zdo.GetInt("Amount")==2,"exception before acceptance restores item and disables bridge");
Enable();m=Reset();m.ThrowAfter=true;ore=Drawer("CopperOre",2);SmelterPatch.UpdateSmelter_Prefix(m);Check(m.Queue==1&&ore.Zdo.GetInt("Amount")==1,"exception after acceptance does not duplicate item");
Enable();DrawerSmelterCompatibility.Disable();m=Reset();Drawer("CopperOre",2);SmelterPatch.UpdateSmelter_Prefix(m);Check(m.Queue==0,"unload removes patches");
Chainloader.PluginInfos[DrawerSmelterCompatibility.LazyGuid].Metadata.Version=new(1,2,5);Enable();m=Reset();Drawer("CopperOre",2);SmelterPatch.UpdateSmelter_Prefix(m);Check(m.Queue==0,"unknown upstream version skips bridge");
upstream.UnpatchSelf();
Console.WriteLine($"{passed} drawer feeding tests passed.");
