using System;
using UnityEngine;
using HarmonyLib;
using Kingmaker.BundlesLoading;
using System.Linq;
using Kingmaker.Modding;
using System.Collections.Generic;
using Kingmaker;
using System.Reflection;
using System.IO;

namespace ShaderWrapper
{
    public class ScriptableShaderWrapper : ScriptableObject
    {
        const string AssetName = "scriptableshaderwrapperinstance";
        const string BundleName = "scriptableshaderwrapper";

        static AssetBundle Bundle;

        static ScriptableShaderWrapper m_Instance;

        public static ScriptableShaderWrapper Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    if (BundlesLoadService.Instance is null)
                    {
                        throw new Exception("Attempt to get ScriptableShaderWrapper Instance before BundlesLoadService Instance is set.");
                    }
                    try
                    {
                        //var Bundle = BundlesLoadService.Instance.RequestBundleForAsset(AssetName);

                        if (Bundle == null)
                            Bundle = AssetBundle.LoadFromFile(
                                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), BundleName));

                        if (Bundle == null)
                        {
                            throw new Exception("Acquired null bundle when trying to set ScriptableShaderWrapper Instance");
                        }

                        m_Instance = Bundle.LoadAsset<ScriptableShaderWrapper>(AssetName);
                    }
                    catch(Exception Ex)
                    {
                        Kingmaker.PFLog.Bundles.Exception(Ex);
                        return null;
                    }
                    if (m_Instance == null)
                    {
                        Kingmaker.PFLog.Bundles.Warning("ScriptableShaderWrapper null Instance after trying to get it!");
                    }
                }
                return m_Instance;
            }
        }

        public Shader[] shaders;
    }

    [HarmonyPatch(typeof(OwlcatModification), "PatchMaterialShaders")]
    static public class PatchForShaderFind
    {
        static bool Prefix(IEnumerable<Material> materials)
        {
            PFLog.Mods.Log("PatchForShaderFind enter");
           foreach(var material in materials)
           {
               string shaderName = material?.shader?.name;
                PFLog.Mods.Log($"PatchForShaderFind material is {material?.name ?? "NULL"}, shader is {shaderName ?? "Null"}");

                if (shaderName == null)
                {
                    PFLog.Mods.Log($"PatchForShaderFind continue");
                    continue;
                }
                var shaderNew = Shader.Find(shaderName);
                PFLog.Mods.Log($"PatchForShaderFind shader after Find is null? {shaderNew == null}.");

                if (shaderNew == null)
                {
                    if (BundlesLoadService.Instance == null)
                    {
                        PFLog.Mods.Log($"PatchForShaderFind BundlesLoadService is null. Continue");
                        continue;
                    }
                    else if (ScriptableShaderWrapper.Instance is ScriptableShaderWrapper shaderWrapper)
                    {
                        foreach(var shader in shaderWrapper.shaders)
                        {
                            bool equals = shader?.name == shaderName;
                            PFLog.Mods.Log($"PatchForShaderFind current shader is {shader?.name ?? "null"}. Equals? {equals}");
                            if (equals)
                            {
                                shaderNew = shader;
                                break;
                            }

                        }
                        //shaderNew = shaderWrapper.shaders.FirstOrDefault(x => x != null && x.name == shaderName);
                        PFLog.Mods.Log($"PatchForShaderFind shaderNew after wrapper is null? {shaderNew == null}");

                    }

                }
                if (shaderNew != null)
                   material.shader= shaderNew;
           }
            return false;
        }
    }
}
