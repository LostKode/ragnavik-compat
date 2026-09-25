using Mono.Cecil;
using Mono.Cecil.Cil;

if (args.Length != 3) throw new ArgumentException("Pass CraftyCarts DLL, BoardersBumperBlurbs DLL, built RagnavikCompat DLL.");
using var carts = AssemblyDefinition.ReadAssembly(args[0]);
using var bumpers = AssemblyDefinition.ReadAssembly(args[1]);
using var compat = AssemblyDefinition.ReadAssembly(args[2]);
MethodDefinition Require(AssemblyDefinition a, string type, string name, string result, string argument)
{
    var m = a.MainModule.GetType(type)?.Methods.SingleOrDefault(m => m.Name == name);
    if (m == null || !m.IsStatic || m.ReturnType.FullName != result || m.Parameters.Count != 1 || m.Parameters[0].ParameterType.FullName != argument)
        throw new Exception("Unsupported upstream signature: " + type + "." + name);
    return m;
}
var cr = Require(carts, "CraftyCartsRemake.Sign_Awake_Transpiler", "GetComponentFromSelfOrParent", "ZNetView", "UnityEngine.Component");
var br = Require(bumpers, "BoardersBumperBlurbs.Game.Sign_Awake_Patch", "ResolveZNetView", "ZNetView", "UnityEngine.Component");
Require(carts, "CraftyCartsRemake.SignAwakePatch2", "Prefix", "System.Void", "Sign");
foreach (var (assembly, type) in new[] { (carts, "CraftyCartsRemake.CraftyCart"), (bumpers, "BoardersBumperBlurbs.Game.BumperSticker") })
    if (assembly.MainModule.GetType(type)?.BaseType?.FullName != "UnityEngine.MonoBehaviour") throw new Exception("Marker component changed");
foreach (var r in new[] { cr, br })
{
    var calls = r.Body.Instructions.Select(i => i.Operand).OfType<MethodReference>().ToList();
    if (!calls.Any(m => m.Name == "GetComponent") || !calls.Any(m => m.Name == "GetComponentInParent"))
        throw new Exception("Resolver behavior changed; re-review " + r.FullName);
}
Console.WriteLine("PASS: both installed resolver signatures, marker components, and font conversion target match the bridge.");
var module = compat.MainModule.GetType("RagnavikCompat.CartSignCompatibility") ?? throw new Exception("Bridge missing");
var enable = module.Methods.Single(m => m.Name == "Enable");
var strings = enable.Body.Instructions.Where(i => i.OpCode == OpCodes.Ldstr).Select(i => (string)i.Operand).ToHashSet();
foreach (var r in new[] {cr, br})
    if (!strings.Contains(r.DeclaringType.FullName) || !strings.Contains(r.Name)) throw new Exception("Uncovered resolver: " + r.FullName);
Console.WriteLine("PASS: bridge covers both resolver winners without relying on transpiler order.");
var convert = module.Methods.Single(m => m.Name == "ConvertCartText");
var il = convert.Body.Instructions;
int Index(string name) => il.ToList().FindIndex(i => i.Operand is MethodReference m && m.Name == name);
if (!(Index("SetActive") < Index("AddComponent") && Index("AddComponent") < Index("set_font")))
    throw new Exception("Text must be inactive before TMP creation and font assignment");
var lastActivation = il.Last(i => i.Operand is MethodReference m && m.Name == "SetActive");
if (il.IndexOf(lastActivation) <= Index("set_font")) throw new Exception("Text activated before font assignment");
Console.WriteLine("PASS: built text conversion assigns its font before activation.");
