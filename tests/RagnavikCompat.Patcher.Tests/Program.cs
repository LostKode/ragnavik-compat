using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using RagnavikCompat.Patcher;

if (args.Length != 1) { Console.Error.WriteLine("Usage: test <assembly_valheim.dll>"); return 2; }
using AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(Path.GetFullPath(args[0]));
BridgeResult first = Patcher.ApplyMagicRevampItemDataManagerBridge(assembly);
if (first != BridgeResult.Applied) { Console.Error.WriteLine($"Expected Applied, received {first}."); return 1; }
MethodDefinition bridge = assembly.MainModule.GetType("Inventory").Methods.Single(method => method.Name == "AddItem" && method.Parameters.Count == 4 && method.Parameters[0].ParameterType.Name == "ItemData");
if (!bridge.Body.Instructions.Any(instruction => instruction.Operand is MethodReference target && target.Name == "AddItem" && target.Parameters.Count == 5)) { Console.Error.WriteLine("Bridge does not forward to current AddItem."); return 1; }
BridgeResult second = Patcher.ApplyMagicRevampItemDataManagerBridge(assembly);
if (second != BridgeResult.SkippedAlreadyPresent) { Console.Error.WriteLine($"Expected idempotent skip, received {second}."); return 1; }
Console.WriteLine("PASS: exact live assembly accepted the bridge and the second application skipped cleanly.");
return 0;

