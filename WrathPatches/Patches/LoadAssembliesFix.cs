using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.Modding;

namespace WrathPatches.Patches
{
    [WrathPatch("Only load .dll files from owlmod Assemblies directories")]
    [HarmonyPatch(typeof(OwlcatModification), nameof(OwlcatModification.LoadAssemblies))]
    static class LoadAssembliesFix
    {
        static IEnumerable<string> GetAssemblies(IEnumerable<string> files)
        {
            //return files.Where(f => Path.GetExtension(f) == ".dll");

            foreach (var f in files)
            {
                if (Path.GetExtension(f) == ".dll")
                {
#if DEBUG
                    Main.Logger.Log($"Loading assembly: {f}");
#endif

                    yield return f;
                }
#if DEBUG
                else
                {
                    Main.Logger.Log($"Skipping non-dll file: {f}");
                }
#endif
            }
        }
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var i in instructions)
            {
                yield return i;

                if (i.Calls(AccessTools.Method(typeof(OwlcatModification), nameof(OwlcatModification.GetFilesFromDirectory))))
                {
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(LoadAssembliesFix), nameof(GetAssemblies)));
                }
            }
        }
    }
}
