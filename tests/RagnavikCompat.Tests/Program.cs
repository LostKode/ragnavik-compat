using RagnavikCompat;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

if (args.Length != 3)
    throw new ArgumentException("Pass the installed EpicMMOSystem.dll, Afterdeath.dll and Starvation.dll for signature verification.");

AssertTrue(EpicMmoReloadGuardCompatibility.Supports(new Version(1, 9, 67)), "supported EpicMMO version");
AssertFalse(EpicMmoReloadGuardCompatibility.Supports(new Version(1, 9, 66)), "older EpicMMO version");
AssertFalse(EpicMmoReloadGuardCompatibility.Supports(new Version(1, 9, 68)), "newer EpicMMO version");
AssertFalse(EpicMmoReloadGuardCompatibility.Supports(null), "missing EpicMMO version");
Console.WriteLine("PASS: reload guard is restricted to WackyEpicMMOSystem 1.9.67");

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
    Console.WriteLine("PASS: EpicMMO 1.9.67 ReadJsonValues(sender, e) patch point verified");
}

VerifyMethod(
    args[1],
    "Afterdeath",
    "Utils",
    "IsGhost",
    new[] { "player" },
    "Afterdeath 1.0.10 Utils.IsGhost(Player) patch contract verified");
VerifyMethod(
    args[2],
    "",
    "DamagePlayer",
    "Prefix",
    new[] { "__instance", "dt", "forceUpdate" },
    "Starvation 1.0.5 DamagePlayer.Prefix(Player, float, bool) patch point verified");

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
