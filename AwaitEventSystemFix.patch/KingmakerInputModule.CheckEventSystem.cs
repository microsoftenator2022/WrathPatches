using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.UI.Selection;

using Owlcat.Runtime.Core.Logging;

using WrathPatches.TranspilerUtil;

namespace WrathPatches.Experimental
{
    [HarmonyPatch]
    internal static class KingmakerInputModule_CheckEventSystem
    {
        static MethodBase? TargetMethod() =>
            typeof(KingmakerInputModule)
                .GetNestedTypes(AccessTools.all)
                .Where(t => t.GetCustomAttributes<CompilerGeneratedAttribute>().Any())
                .Select(t => t.GetMethod("MoveNext", AccessTools.all))
                .FirstOrDefault();

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            Main.Logger.Log($"{nameof(KingmakerInputModule_CheckEventSystem)}.{nameof(Transpiler)}");

            var match = new Func<CodeInstruction, bool>[]
            {
                ci => ci.LoadsField(typeof(LogChannel).GetField(nameof(LogChannel.System), AccessTools.all)),
                ci => ci.opcode == OpCodes.Ldstr && "Await event system".Equals(ci.operand),
                ci => ci.opcode == OpCodes.Call,
                ci => ci.opcode == OpCodes.Callvirt
            };

            var iMatch = instructions.FindInstructionsIndexed(match);

            //foreach ((var index, var instruction) in iMatch)
            //{
            //    Main.Logger.Log($"{index}: {instruction}");
            //}

            if (!iMatch.Any())
            {
                Main.Logger.Log("No match found");
                return instructions;
            }

            var iList = instructions.ToList();
            
            foreach ((var index, var _) in iMatch)
            {
                iList[index] = new CodeInstruction(OpCodes.Nop);
            }

            return iList;
        }
    }
}
