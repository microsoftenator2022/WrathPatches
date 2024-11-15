using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingmaker.Modding;
using HarmonyLib;
using System.Reflection;

namespace ShaderWrapper
{
    internal class Main
    {
        static Harmony harmony;

        [OwlcatModificationEnterPoint]
        public static void EntryPoint(OwlcatModification owlcatModification)
        {
            if (harmony == null)
            {
                harmony = new Harmony(owlcatModification.Manifest.UniqueName);
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
        }
    }
}
