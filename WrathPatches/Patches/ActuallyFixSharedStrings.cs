﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

using Kingmaker.Blueprints.JsonSystem.Converters;
using Kingmaker.Localization;

using HarmonyLib;

using System.Reflection;

namespace WrathPatches
{
    [WrathPatch("Fix Shared Strings")]
    [HarmonyPatch]
    public static class ActuallyFixSharedStrings
    {
        static MethodInfo? CreateSharedStringInstance => typeof(ScriptableObject)
            .GetMembers(BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy)
            .OfType<MethodInfo>()
            .Where(m => m.Name == nameof(ScriptableObject.CreateInstance) && m.IsGenericMethodDefinition)
            .FirstOrDefault()
            ?.MakeGenericMethod(typeof(SharedStringAsset));

        [HarmonyPatch(typeof(SharedStringConverter), nameof(SharedStringConverter.ReadJson))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> SharedStringConverter_ReadJson_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            //Main.Logger.Log($"{nameof(SharedStringConverter_ReadJson_Transpiler)}");

            var ssCons = typeof(SharedStringAsset).GetConstructor([]);
            
            var createSs = CreateSharedStringInstance ?? throw new Exception($"Unable to find or create constructor");

            var count = 0;

            foreach (var i in instructions)
            {
                if (i.Is(OpCodes.Newobj, ssCons))
                    yield return new CodeInstruction(OpCodes.Call, createSs);

                else yield return i;

                count++;
            }

            if (count == 0)
                throw new Exception("Could not find instructions to patch");
        }
    }
}
