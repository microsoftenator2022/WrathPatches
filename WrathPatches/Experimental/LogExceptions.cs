using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker;
using Kingmaker.Logging.Configuration.Platforms;

using Owlcat.Runtime.Core.Logging;

namespace WrathPatches.Experimental
{
    [HarmonyPatch]
    internal static class Log_Exceptions_Patch
    {
        class ShortLogWithoutCallstacks : UberLoggerFile, IDisposableLogSink, ILogSink
        {
            public ShortLogWithoutCallstacks(string filename, string path = null!, bool includeCallStacks = true, bool extendedLog = false)
                : base(filename, path, includeCallStacks, extendedLog)
            {
#if DEBUG
                Main.Logger.Log(string.Join("new", typeof(ShortLogWithoutCallstacks)));

                if (this.LogFileWriter.BaseStream is not FileStream fs)
                    Main.Logger.Error($"{nameof(LogFileWriter)} is not a FileStream");
                else
                    Main.Logger.Log($"File: {fs.Name}");
#endif
            }

            void ILogSink.Log(LogInfo logInfo)
            {
#if DEBUG
                Main.Logger.Log(string.Join(typeof(ShortLogWithoutCallstacks).ToString(), ".", nameof(Log)));
                Main.Logger.Log($"IsException? {logInfo.IsException}");
                Main.Logger.Log(logInfo.Message);
#endif

                if (!logInfo.IsException)
                {
                    logInfo = new LogInfo(
                        logInfo.Source,
                        logInfo.Channel,
                        logInfo.TimeStamp,
                        logInfo.Severity,
                        null,
                        logInfo.IsException,
                        logInfo.Message);
                }

                Log(logInfo);
            }
        }

        [HarmonyPatch(typeof(LogSinkFactory), nameof(LogSinkFactory.CreateShort))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> LogSinkFactory_CreateShort_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            Main.Logger.Log(nameof(LogSinkFactory_CreateShort_Transpiler));

            foreach (var i in instructions)
            {
                if (i.opcode == OpCodes.Newobj &&
                    typeof(UberLoggerFile).GetConstructors(AccessTools.all)
                    .Any(i.OperandIs))
                    i.operand = typeof(ShortLogWithoutCallstacks).GetConstructors().First();

                yield return i;
            }
        }

        [HarmonyPatch(typeof(Logger), nameof(Logger.Log))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Logger_Log_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGen)
        {
            var exParamIndex = 4;

            var lb = ilGen.DeclareLocal(typeof(Exception));
            lb.SetLocalSymInfo("ex2");
            var localIndex = lb.LocalIndex;

            var iList = instructions.ToList();

            iList.InsertRange(0, new[]
            {
                new CodeInstruction(OpCodes.Ldarg_S, exParamIndex), // Exception ex
                new CodeInstruction(OpCodes.Stloc_S, localIndex),
            });

            var logInfoCtorIndex = iList
                .Indexed()
                .Where(i => i.item.opcode == OpCodes.Newobj &&
                    typeof(LogInfo).GetConstructors()
                        .Any(i.item.OperandIs))
                .Select(i => i.index)
                .First();

            var getEx = iList[logInfoCtorIndex - 5];

            if (!getEx.IsLdarg(exParamIndex))
                throw new InvalidOperationException($"{getEx} should be ldarg");

            getEx.opcode = OpCodes.Ldloc_S;
            getEx.operand = localIndex;

            return iList;
        }
    }
}
