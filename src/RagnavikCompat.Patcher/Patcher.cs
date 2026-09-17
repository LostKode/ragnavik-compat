using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace RagnavikCompat.Patcher
{
    public static class Patcher
    {
        private static readonly ManualLogSource Log = Logger.CreateLogSource("RagnavikCompat.Patcher");
        public static IEnumerable<string> TargetDLLs => new[] { "assembly_valheim.dll" };
        public static void Initialize() { }

        public static void Patch(AssemblyDefinition assembly)
        {
            if (assembly?.Name?.Name != "assembly_valheim") return;
            BridgeResult result = ApplyMagicRevampItemDataManagerBridge(assembly);
            Log.LogInfo($"MagicRevamp ItemDataManager bridge: {result}");
        }

        public static BridgeResult ApplyMagicRevampItemDataManagerBridge(AssemblyDefinition assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            ModuleDefinition module = assembly.MainModule;
            TypeDefinition inventory = module.GetType("Inventory");
            if (inventory == null) return BridgeResult.SkippedInventoryTypeMissing;
            if (inventory.Methods.Any(IsLegacyAddItem)) return BridgeResult.SkippedAlreadyPresent;
            MethodDefinition current = inventory.Methods.SingleOrDefault(IsCurrentAddItem);
            if (current == null) return BridgeResult.SkippedCurrentTargetMissing;
            MethodReference zlog = FindZLog(module);
            if (zlog == null) return BridgeResult.SkippedZLogTargetMissing;

            var bridge = new MethodDefinition("AddItem", MethodAttributes.Public | MethodAttributes.HideBySig, current.ReturnType);
            for (int index = 0; index < 4; index++)
            {
                ParameterDefinition parameter = current.Parameters[index];
                bridge.Parameters.Add(new ParameterDefinition(parameter.Name, parameter.Attributes, module.ImportReference(parameter.ParameterType)));
            }

            ILProcessor il = bridge.Body.GetILProcessor();
            il.Emit(OpCodes.Ldstr, "Ragnavik ItemDataManager AddItem compatibility bridge");
            il.Emit(OpCodes.Call, zlog);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldarg_3);
            il.Emit(OpCodes.Ldarg, bridge.Parameters[3]);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Callvirt, current);
            il.Emit(OpCodes.Ret);
            inventory.Methods.Add(bridge);
            return BridgeResult.Applied;
        }

        private static bool IsLegacyAddItem(MethodDefinition method) => method.Name == "AddItem" && method.Parameters.Count == 4 && method.Parameters[0].ParameterType.Name == "ItemData" && method.Parameters.Skip(1).All(parameter => parameter.ParameterType.MetadataType == MetadataType.Int32);
        private static bool IsCurrentAddItem(MethodDefinition method) => method.Name == "AddItem" && method.Parameters.Count == 5 && method.Parameters[0].ParameterType.Name == "ItemData" && method.Parameters.Skip(1).Take(3).All(parameter => parameter.ParameterType.MetadataType == MetadataType.Int32) && method.Parameters[4].ParameterType.MetadataType == MetadataType.Boolean;

        private static MethodReference FindZLog(ModuleDefinition module)
        {
            MethodReference existing = module.GetMemberReferences().OfType<MethodReference>().FirstOrDefault(method => method.Name == "Log" && method.DeclaringType.Name == "ZLog" && method.Parameters.Count == 1);
            if (existing != null) return module.ImportReference(existing);
            MethodDefinition definition = module.GetType("ZLog")?.Methods.FirstOrDefault(method => method.Name == "Log" && method.IsStatic && method.Parameters.Count == 1);
            return definition == null ? null : module.ImportReference(definition);
        }
    }

    public enum BridgeResult { Applied, SkippedAlreadyPresent, SkippedInventoryTypeMissing, SkippedCurrentTargetMissing, SkippedZLogTargetMissing }
}

