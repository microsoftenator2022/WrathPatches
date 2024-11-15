using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.Modding;

namespace WrathPatches.Patches;

[WrathPatch("@Kurufinve ShaderWrapper fix for owlmod material shaders")]
[HarmonyPatch(typeof(OwlcatModificationsManager), nameof(OwlcatModificationsManager.Start))]
static class ShaderWrapper
{
    static void Prefix()
    {
        var shaderWrapperAssembly =
            Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ShaderWrapper.dll"));

        Main.HarmonyInstance.PatchAll(shaderWrapperAssembly);
    }
}
