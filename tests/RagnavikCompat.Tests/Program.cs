using RagnavikCompat;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

if (args.Length != 7)
    throw new ArgumentException("Pass the installed EpicMMOSystem.dll, Afterdeath.dll, Starvation.dll, CurrencyPocket.dll, AzuExtendedPlayerInventory.dll, kg_ItemDrawers.dll and the built RagnavikCompat.dll for signature verification.");

AssertTrue(EpicMmoReloadGuardCompatibility.Supports(new Version(1, 9, 68)), "supported EpicMMO version");
AssertFalse(EpicMmoReloadGuardCompatibility.Supports(new Version(1, 9, 67)), "older EpicMMO version");
AssertFalse(EpicMmoReloadGuardCompatibility.Supports(new Version(1, 9, 69)), "newer EpicMMO version");
AssertFalse(EpicMmoReloadGuardCompatibility.Supports(null), "missing EpicMMO version");
Console.WriteLine("PASS: reload guard is restricted to WackyEpicMMOSystem 1.9.68");

AssertTrue(AfterdeathStarvationCompatibility.Supports(new Version(1, 0, 10), new Version(1, 0, 5)), "supported Afterdeath and Starvation versions");
AssertFalse(AfterdeathStarvationCompatibility.Supports(new Version(1, 0, 9), new Version(1, 0, 5)), "older Afterdeath version");
AssertFalse(AfterdeathStarvationCompatibility.Supports(new Version(1, 0, 10), new Version(1, 0, 4)), "older Starvation version");
AssertFalse(AfterdeathStarvationCompatibility.Supports(null, new Version(1, 0, 5)), "missing Afterdeath version");
AssertFalse(AfterdeathStarvationCompatibility.Supports(new Version(1, 0, 10), null), "missing Starvation version");
Console.WriteLine("PASS: starvation guard is restricted to Afterdeath 1.0.10 and Starvation 1.0.5");
AssertFalse(AfterdeathStarvationCompatibility.ShouldRunStarvation(true, false), "living Afterdeath spirit");
AssertTrue(AfterdeathStarvationCompatibility.ShouldRunStarvation(false, false), "ordinary living player");
AssertTrue(AfterdeathStarvationCompatibility.ShouldRunStarvation(true, true), "dead player");
Console.WriteLine("PASS: starvation runs normally except while the player is an Afterdeath spirit");

AssertTrue(AfterdeathTeleportCompatibility.Supports(new Version(1, 0, 10)), "supported Afterdeath teleport version");
AssertFalse(AfterdeathTeleportCompatibility.Supports(new Version(1, 0, 9)), "older Afterdeath teleport version");
AssertFalse(AfterdeathTeleportCompatibility.Supports(new Version(1, 0, 11)), "newer Afterdeath teleport version");
AssertFalse(AfterdeathTeleportCompatibility.Supports(null), "missing Afterdeath teleport version");
AssertTrue(AfterdeathTeleportCompatibility.ShouldAllowTeleport(true, false), "living Afterdeath spirit teleport");
AssertFalse(AfterdeathTeleportCompatibility.ShouldAllowTeleport(false, false), "ordinary living player teleport override");
AssertFalse(AfterdeathTeleportCompatibility.ShouldAllowTeleport(true, true), "dead player teleport override");
Console.WriteLine("PASS: teleport override is restricted to living Afterdeath spirits on Afterdeath 1.0.10");

AssertTrue(AfterdeathDoorCompatibility.Supports(new Version(1, 0, 10)), "supported Afterdeath door version");
AssertFalse(AfterdeathDoorCompatibility.Supports(new Version(1, 0, 9)), "older Afterdeath door version");
AssertFalse(AfterdeathDoorCompatibility.Supports(new Version(1, 0, 11)), "newer Afterdeath door version");
AssertFalse(AfterdeathDoorCompatibility.Supports(null), "missing Afterdeath door version");
AssertTrue(AfterdeathDoorCompatibility.ShouldAllowInteraction(true, false, true, false), "living Afterdeath spirit door");
AssertFalse(AfterdeathDoorCompatibility.ShouldAllowInteraction(true, false, false, false), "living Afterdeath spirit non-door");
AssertFalse(AfterdeathDoorCompatibility.ShouldAllowInteraction(false, false, true, false), "ordinary living player door");
AssertFalse(AfterdeathDoorCompatibility.ShouldAllowInteraction(true, true, true, false), "dead player door");
AssertTrue(AfterdeathDoorCompatibility.ShouldAllowInteraction(true, false, false, true), "living Afterdeath spirit assigned bed");
AssertFalse(AfterdeathDoorCompatibility.ShouldAllowInteraction(true, false, false, false), "living Afterdeath spirit unassigned bed");
Console.WriteLine("PASS: home access override permits only doors and the assigned bed for living Afterdeath spirits on Afterdeath 1.0.10");

AssertTrue(AfterdeathNearestBedCompatibility.Supports(new Version(1, 0, 10)), "supported Afterdeath nearest-bed version");
AssertFalse(AfterdeathNearestBedCompatibility.Supports(new Version(1, 0, 9)), "older Afterdeath nearest-bed version");
AssertFalse(AfterdeathNearestBedCompatibility.Supports(new Version(1, 0, 11)), "newer Afterdeath nearest-bed version");
AssertFalse(AfterdeathNearestBedCompatibility.Supports(null), "missing Afterdeath nearest-bed version");
AssertTrue(AfterdeathNearestBedCompatibility.ShouldUseBed(true, 0, 0, 100, 0, 25, 0), "closer bed");
AssertFalse(AfterdeathNearestBedCompatibility.ShouldUseBed(true, 0, 0, 25, 0, 100, 0), "closer Skathi");
AssertFalse(AfterdeathNearestBedCompatibility.ShouldUseBed(true, 0, 0, 50, 0, 0, 50), "equal distance keeps Skathi");
AssertFalse(AfterdeathNearestBedCompatibility.ShouldUseBed(false, 0, 0, 100, 0, 25, 0), "missing valid bed");
AssertTrue(AfterdeathNearestBedCompatibility.ShouldUseBed(true, 100, 100, 0, 0, 90, 90), "XZ distance from non-origin death");
Console.WriteLine("PASS: Afterdeath selects a valid bed only when it is strictly closer than Skathi");

AssertTrue(AzuEpiQuickSlotRecoveryCompatibility.Supports(new Version(2, 5, 1)), "supported AzuEPI version");
AssertFalse(AzuEpiQuickSlotRecoveryCompatibility.Supports(new Version(2, 5, 0)), "older AzuEPI version");
AssertFalse(AzuEpiQuickSlotRecoveryCompatibility.Supports(new Version(2, 5, 2)), "newer AzuEPI version");
AssertFalse(AzuEpiQuickSlotRecoveryCompatibility.Supports(null), "missing AzuEPI version");
Console.WriteLine("PASS: grave quick-slot recovery is restricted to AzuExtendedPlayerInventory 2.5.1");

AssertTrue(EpicLootMagicPluginTakeAllCompatibility.Supports(new Version(0, 14, 11), new Version(2, 2, 1)), "supported Epic Loot and MagicPlugin versions");
AssertTrue(EpicLootMagicPluginTakeAllCompatibility.Supports(new Version(0, 14, 10), new Version(2, 2, 1)), "older Epic Loot version");
AssertTrue(EpicLootMagicPluginTakeAllCompatibility.Supports(new Version(0, 15, 0), new Version(2, 3, 0)), "future mod versions");
AssertFalse(EpicLootMagicPluginTakeAllCompatibility.Supports(null, new Version(2, 2, 1)), "missing Epic Loot version");
AssertFalse(EpicLootMagicPluginTakeAllCompatibility.Supports(new Version(0, 14, 11), null), "missing MagicPlugin version");
Console.WriteLine("PASS: Take All bridge accepts any installed Epic Loot and MagicPlugin versions");

AssertTrue(CurrencyPocketTakeAllCompatibility.Supports(new Version(1, 0, 15)), "supported CurrencyPocket version");
AssertFalse(CurrencyPocketTakeAllCompatibility.Supports(new Version(1, 0, 14)), "older CurrencyPocket version");
AssertFalse(CurrencyPocketTakeAllCompatibility.Supports(new Version(1, 0, 16)), "newer CurrencyPocket version");
AssertFalse(CurrencyPocketTakeAllCompatibility.Supports(null), "missing CurrencyPocket version");
AssertTrue(CurrencyPocketTakeAllCompatibility.IsCoin("$item_coins", 1), "positive coin stack");
AssertFalse(CurrencyPocketTakeAllCompatibility.IsCoin("$item_coins", 0), "empty coin stack");
AssertFalse(CurrencyPocketTakeAllCompatibility.IsCoin("$item_amber", 1), "non-coin valuable");
AssertTrue(CurrencyPocketTakeAllCompatibility.TryAddBalance(25, 10, out var updatedBalance) && updatedBalance == 35, "coin balance addition");
AssertFalse(CurrencyPocketTakeAllCompatibility.TryAddBalance(int.MaxValue, 1, out _), "coin balance overflow");
Console.WriteLine("PASS: CurrencyPocket bridge is restricted to 1.0.15 and accepts only valid coin stacks");

AssertTrue(ItemDrawersCustomDataCompatibility.Supports(new Version(1, 4, 0), true), "current ItemDrawers package API");
AssertTrue(ItemDrawersCustomDataCompatibility.Supports(new Version(9, 9, 9), true), "future ItemDrawers package API");
AssertFalse(ItemDrawersCustomDataCompatibility.Supports(new Version(1, 4, 0), false), "incompatible ItemDrawers API");
AssertFalse(ItemDrawersCustomDataCompatibility.Supports(null, true), "missing ItemDrawers package");
Console.WriteLine("PASS: custom-data bridge follows the ItemDrawers package API instead of pinning a version");

using (var input = File.OpenRead(args[0]))
using (var pe = new PEReader(input))
{
    var metadata = pe.GetMetadataReader();
    var methods = metadata.TypeDefinitions
        .Select(typeHandle => metadata.GetTypeDefinition(typeHandle))
        .Where(type => metadata.GetString(type.Name) == "EpicMMOSystem" &&
                       metadata.GetString(type.Namespace) == "EpicMMOSystem")
        .SelectMany(type => type.GetMethods())
        .Select(handle => metadata.GetMethodDefinition(handle))
        .Where(method => metadata.GetString(method.Name) == "ReadJsonValues")
        .ToArray();
    if (methods.Length != 1)
        throw new Exception($"Expected one EpicMMO ReadJsonValues method, found {methods.Length}.");
    var parameters = methods[0].GetParameters()
        .Select(handle => metadata.GetString(metadata.GetParameter(handle).Name))
        .Where(name => name != "")
        .ToArray();
    if (!parameters.SequenceEqual(new[] { "sender", "e" }))
        throw new Exception("Unexpected EpicMMO watcher parameters: " + string.Join(", ", parameters));
    Console.WriteLine("PASS: EpicMMO 1.9.68 ReadJsonValues(sender, e) patch point verified");
}

VerifyMethod(
    args[1],
    "Afterdeath",
    "Utils",
    "IsGhost",
    new[] { "player" },
    "Afterdeath 1.0.10 Utils.IsGhost(Player) patch contract verified");
VerifyMethod(
    args[1],
    "",
    "DisableTeleport",
    "Prefix",
    Array.Empty<string>(),
    "Afterdeath 1.0.10 DisableTeleport.Prefix() patch contract verified");
VerifyMethod(
    args[1],
    "",
    "DisableInteractText",
    "Postfix",
    new[] { "__instance", "hover" },
    "Afterdeath 1.0.10 DisableInteractText.Postfix(Player, ref GameObject) patch contract verified");
VerifyField(
    args[1],
    "Afterdeath",
    "Afterdeath",
    "ghostStatus",
    "Afterdeath 1.0.10 ghostStatus field contract verified");

VerifyMethod(
    args[1],
    "Afterdeath",
    "Utils",
    "GetClosestLocation",
    new[] { "position" },
    "Afterdeath 1.0.10 Utils.GetClosestLocation(Vector3) patch contract verified");

VerifyMethod(
    args[2],
    "",
    "DamagePlayer",
    "Prefix",
    new[] { "__instance", "dt", "forceUpdate" },
    "Starvation 1.0.5 DamagePlayer.Prefix(Player, float, bool) patch point verified");
VerifyMethod(
    args[3],
    "CurrencyPocket",
    "MiscFunctions",
    "GetPlayerCoinsFromCustomData",
    Array.Empty<string>(),
    "CurrencyPocket 1.0.15 GetPlayerCoinsFromCustomData() contract verified");
VerifyMethod(
    args[3],
    "CurrencyPocket",
    "MiscFunctions",
    "UpdatePlayerCustomData",
    new[] { "coinCount", "player" },
    "CurrencyPocket 1.0.15 UpdatePlayerCustomData(int, Player) contract verified");
VerifyMethod(
    args[3],
    "CurrencyPocket",
    "CurrencyPocket",
    "UpdatePocketUI",
    Array.Empty<string>(),
    "CurrencyPocket 1.0.15 UpdatePocketUI() contract verified");
VerifyMethod(
    args[4],
    "AzuEPI",
    "API",
    "GetQuickSlotSnapshots",
    new[] { "inv" },
    "AzuExtendedPlayerInventory 2.5.1 GetQuickSlotSnapshots(Inventory) contract verified");
VerifyMethod(
    args[4],
    "AzuEPI",
    "SlotSnapshot",
    "get_GridPos",
    Array.Empty<string>(),
    "AzuExtendedPlayerInventory 2.5.1 SlotSnapshot.GridPos contract verified");

VerifyMethod(
    args[5],
    "kg_ItemDrawers",
    "DrawerComponent",
    "UseItem",
    new[] { "user", "item" },
    "ItemDrawers DrawerComponent.UseItem(Humanoid, ItemData) contract verified");
VerifyMethod(
    args[5],
    "kg_ItemDrawers",
    "DrawerComponent",
    "RPC_WithdrawItem_Request",
    new[] { "sender", "amount" },
    "ItemDrawers withdrawal RPC contract verified");
VerifyMethod(
    args[5],
    "kg_ItemDrawers",
    "Piece_OnDestroy_Patch",
    "Postfix",
    new[] { "__instance" },
    "ItemDrawers destruction postfix contract verified");

VerifyMethod(
    args[6],
    "RagnavikCompat",
    "RagnavikCompatPlugin",
    "BeforeAfterdeathInteractionBlock",
    new[] { "__0", "hover" },
    "Ragnavik interaction bridge binds the static Afterdeath player argument positionally");


var folder = Path.Combine(Path.GetTempPath(), "ragnavik-epicmmo-guard-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(folder);
try
{
    var firstFile = Path.Combine(folder, "Monsters.json");
    File.WriteAllText(firstFile, "[{\"name\":\"Boar\"}]");
    var original = JsonFolderFingerprint.Compute(folder);

    File.SetLastWriteTimeUtc(firstFile, DateTime.UtcNow.AddMinutes(1));
    AssertEqual(original, JsonFolderFingerprint.Compute(folder), "metadata-only edit");

    File.WriteAllText(firstFile, "[{\"name\":\"Deer\"}]");
    AssertDifferent(original, JsonFolderFingerprint.Compute(folder), "JSON content edit");

    var contentFingerprint = JsonFolderFingerprint.Compute(folder);
    File.Move(firstFile, Path.Combine(folder, "Creatures.json"));
    AssertDifferent(contentFingerprint, JsonFolderFingerprint.Compute(folder), "JSON rename");

    var subfolder = Path.Combine(folder, "Nested");
    Directory.CreateDirectory(subfolder);
    File.WriteAllText(Path.Combine(subfolder, "Bosses.json"), "[{\"name\":\"Troll\"}]");
    var nestedFingerprint = JsonFolderFingerprint.Compute(folder);
    Directory.Move(subfolder, Path.Combine(folder, "Moved"));
    AssertDifferent(nestedFingerprint, JsonFolderFingerprint.Compute(folder), "directory rename");

    Console.WriteLine("PASS: metadata ignored, content, file rename and directory rename detected");
}
finally
{
    Directory.Delete(folder, recursive: true);
}

static void AssertEqual(string expected, string actual, string label)
{
    if (expected != actual) throw new Exception($"{label} changed fingerprint unexpectedly");
}

static void AssertDifferent(string expected, string actual, string label)
{
    if (expected == actual) throw new Exception($"{label} did not change fingerprint");
}

static void AssertTrue(bool actual, string label)
{
    if (!actual) throw new Exception($"{label} was not accepted");
}

static void AssertFalse(bool actual, string label)
{
    if (actual) throw new Exception($"{label} was accepted unexpectedly");
}

static void VerifyField(string assemblyPath, string typeNamespace, string typeName, string fieldName, string successMessage)
{
    using var input = File.OpenRead(assemblyPath);
    using var pe = new PEReader(input);
    var metadata = pe.GetMetadataReader();
    var fields = metadata.TypeDefinitions
        .Select(typeHandle => metadata.GetTypeDefinition(typeHandle))
        .Where(type => metadata.GetString(type.Name) == typeName && metadata.GetString(type.Namespace) == typeNamespace)
        .SelectMany(type => type.GetFields())
        .Select(handle => metadata.GetFieldDefinition(handle))
        .Where(field => metadata.GetString(field.Name) == fieldName)
        .ToArray();
    if (fields.Length != 1)
        throw new Exception($"Expected one {typeNamespace}.{typeName}.{fieldName} field, found {fields.Length}.");
    Console.WriteLine("PASS: " + successMessage);
}

static void VerifyMethod(string assemblyPath, string typeNamespace, string typeName, string methodName, string[] expectedParameters, string successMessage)
{
    using var input = File.OpenRead(assemblyPath);
    using var pe = new PEReader(input);
    var metadata = pe.GetMetadataReader();
    var methods = metadata.TypeDefinitions
        .Select(typeHandle => metadata.GetTypeDefinition(typeHandle))
        .Where(type => metadata.GetString(type.Name) == typeName && metadata.GetString(type.Namespace) == typeNamespace)
        .SelectMany(type => type.GetMethods())
        .Select(handle => metadata.GetMethodDefinition(handle))
        .Where(method => metadata.GetString(method.Name) == methodName)
        .ToArray();
    if (methods.Length != 1)
        throw new Exception($"Expected one {typeNamespace}.{typeName}.{methodName} method, found {methods.Length}.");
    var parameters = methods[0].GetParameters()
        .Select(handle => metadata.GetString(metadata.GetParameter(handle).Name))
        .Where(name => name != "")
        .ToArray();
    if (!parameters.SequenceEqual(expectedParameters))
        throw new Exception($"Unexpected {typeNamespace}.{typeName}.{methodName} parameters: " + string.Join(", ", parameters));
    Console.WriteLine("PASS: " + successMessage);
}
