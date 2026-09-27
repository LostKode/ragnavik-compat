using System.Reflection;
using RagnavikCompat;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;

var log = new ManualLogSource();
var owner = new Harmony("test");
var local = new Player();
Player.m_localPlayer = local;
var bag = new Inventory();
local.Inventory.Backpacks.Add(bag);
var door = new Door { m_keyItem = new ItemDrop() };
var hook = typeof(BackpackKeyCompatibility).GetMethod("AfterHaveKey", BindingFlags.NonPublic | BindingFlags.Static)!;
int passed = 0;
bool Check(bool original = false, bool world = true, Humanoid? actor = null)
{
    object[] args = { door, actor ?? local, world, original };
    hook.Invoke(null, args);
    return (bool)args[3];
}
void Assert(bool ok, string scenario)
{
    if (!ok) throw new Exception(scenario);
    passed++;
    Console.WriteLine("PASS: " + scenario);
}
void Enable(string version = "1.3.10", string? mines = null)
{
    BackpackKeyCompatibility.Disable();
    Chainloader.PluginInfos.Clear();
    Chainloader.PluginInfos[BackpackKeyCompatibility.BackpackGuid] = new PluginInfo(new Backpacks.Plugin(), version);
    if (mines != null) Chainloader.PluginInfos["blacks7ar.OreMines"] = new PluginInfo(new OreMines.Plugin(), mines);
    BackpackKeyCompatibility.Enable(owner, log);
}

BackpackKeyCompatibility.Enable(owner, log);
Assert(!Check(), "missing backpack mod leaves original result");
Enable("1.3.11");
Assert(!Check(), "unverified backpack version stays inactive");
Enable();
Assert(Harmony.Target?.Name == "HaveKey", "only Door.HaveKey is patched");
Assert(Check(true), "existing inventory key retains original success");
Assert(!Check(), "empty backpack does not unlock");
bag.Items.Add(("wrong", 0));
Assert(!Check(), "wrong key does not unlock");
foreach (var key in new[] { "$item_cryptkey", "$bom_eikthyrkey", "$bom_bonemasskey", "$bom_crystalkey", "$bom_lavakey" })
{
    door.m_keyItem.m_itemData.m_shared.m_name = key;
    bag.Items.Clear(); bag.Items.Add((key, 0));
    Assert(Check(), "carried backpack unlocks with " + key);
}
Assert(!Check(actor: new Player()), "another player cannot use local backpack");
local.Inventory.Backpacks.Clear();
Assert(!Check(), "dropped backpack no longer counts");
local.Inventory.Backpacks.Add(new Inventory()); local.Inventory.Backpacks.Add(bag);
Assert(Check(), "search includes later carried backpacks");
Inventory.WorldLevel = 1;
Assert(!Check(), "lower-world-level key stays rejected");
Assert(Check(world: false), "relaxed lookup supports vanilla low-level message");
Inventory.WorldLevel = 0;
door.m_consumeKey = true;
Assert(!Check(), "consumable vanilla door cannot unlock for free");
door.m_consumeKey = false;
Enable(mines: "1.2.1");
door.m_name = "$bom_tinminegate";
OreMines.Plugin._consumableKeys.BoxedValue = "Off";
Assert(Check(), "reusable OreMines key works");
OreMines.Plugin._consumableKeys.BoxedValue = "On";
Assert(!Check(), "live consumable toggle requires main inventory");
Assert(Check(true), "consumable setting preserves original key success");
OreMines.Plugin._consumableKeys.BoxedValue = "Off";
Assert(Check(), "live toggle back to reusable restores backpack support");
Enable(mines: "1.2.2");
Assert(!Check(), "unknown OreMines version fails closed for mine gates");
door.m_name = "$piece_cryptgate";
Assert(Check(), "unknown OreMines does not disable vanilla crypt keys");
Backpacks.API.Throw = true;
Assert(!Check(), "backpack API exception preserves original denial");
Backpacks.API.Throw = false;
Assert(!Check(), "failed module stays disabled instead of throwing repeatedly");
Assert(Check(true), "API failure never revokes existing key success");
Assert(log.Warnings.Count == 1, "API failure logs once");
Enable();
BackpackKeyCompatibility.Disable();
Assert(!Check(), "plugin teardown disables lookups");
Console.WriteLine($"{passed} backpack-key regression checks passed.");
