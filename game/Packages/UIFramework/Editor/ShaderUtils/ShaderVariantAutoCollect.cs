using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngineEx;

using Object = UnityEngine.Object;

namespace UnityEditorEx
{
    [InitializeOnLoad]
    public static class ShaderVariantAutoCollect
    {
        static ShaderVariantAutoCollect()
        {
#if AUTO_COLLECT_SHADER_VARIANTS
            EditorBridge.AfterPlayModeChange += () =>
            {
                if (!Application.isPlaying)
                {
                    CollectShaderVariant();
                    SplitAutoCollectedShaderVariants();
                }
            };
#endif
        }

        private static Action<string> Func_SaveEditorAutoCollectedShaderVariantTo;
        public static void SaveEditorAutoCollectedShaderVariantTo(string path)
        {
            if (Func_SaveEditorAutoCollectedShaderVariantTo == null)
            {
                var minfo = typeof(UnityEditor.ShaderUtil).GetMethod("SaveCurrentShaderVariantCollection", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                Func_SaveEditorAutoCollectedShaderVariantTo = (Action<string>)Delegate.CreateDelegate(typeof(Action<string>), minfo);
            }
            Func_SaveEditorAutoCollectedShaderVariantTo(path);
        }

        public static bool HaveSameElements<T>(this IList<T> l1, IList<T> l2)
        {
            if (l1 == null || l2 == null)
            {
                return l1 == l2;
            }
            if (l1.Count != l2.Count)
            {
                return false;
            }
            var set = new HashSet<T>(l1);
            set.ExceptWith(l2);
            return set.Count == 0;
        }

        public static bool ShaderVariantEquals(this ShaderVariantCollection.ShaderVariant sv1, ShaderVariantCollection.ShaderVariant sv2)
        {
            if (sv1.shader != sv2.shader)
            {
                return false;
            }
            if (sv1.passType != sv2.passType)
            {
                return false;
            }
            return sv1.keywords.HaveSameElements(sv2.keywords);
        }

        public static bool IsAssetInWriteableDirectory(string path)
        {
            if (path.StartsWith("Assets/"))
            {
                return true;
            }
            else
            {
                var pinfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(path);
                if (pinfo != null)
                {
                    if (pinfo.source == UnityEditor.PackageManager.PackageSource.Local
                        || pinfo.source == UnityEditor.PackageManager.PackageSource.Embedded
                        )
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static char[] KeyWordsSplitChars = new[] { ' ', '\t', '\r', '\n' };
        public static ShaderVariantCollection.ShaderVariant[] GetAllShaderVariantInShaderVariantCollection(string path)
        {
            if (!System.IO.File.Exists(path))
            {
                return null;
            }
            var ys = new YamlDotNet.RepresentationModel.YamlStream();
            using (var sr = System.IO.File.OpenText(path))
            {
                ys.Load(sr);
            }
            if (ys.Documents.Count <= 0)
            {
                return null;
            }

            List<ShaderVariantCollection.ShaderVariant> list = new List<ShaderVariantCollection.ShaderVariant>();
            try
            {
                var rootNode = ys.Documents[0].RootNode;
                var listNode = (YamlDotNet.RepresentationModel.YamlSequenceNode)rootNode["ShaderVariantCollection"]["m_Shaders"];
                foreach (YamlDotNet.RepresentationModel.YamlMappingNode itemNode in listNode)
                {
                    var guidNode = (YamlDotNet.RepresentationModel.YamlScalarNode)itemNode["first"]["guid"];
                    var guid = guidNode.Value;
                    var shaderpath = AssetDatabase.GUIDToAssetPath(guid);
                    if (AssetDatabase.GetMainAssetTypeAtPath(shaderpath) == typeof(Shader))
                    {
                        var shader = (Shader)AssetDatabase.LoadMainAssetAtPath(shaderpath);
                        var varsNode = (YamlDotNet.RepresentationModel.YamlSequenceNode)itemNode["second"]["variants"];
                        foreach (YamlDotNet.RepresentationModel.YamlMappingNode varNode in varsNode)
                        {
                            var passTypeStr = ((YamlDotNet.RepresentationModel.YamlScalarNode)varNode["passType"]).Value;
                            int passTypeN;
                            int.TryParse(passTypeStr, out passTypeN);
                            var passType = (UnityEngine.Rendering.PassType)passTypeN;

                            var kwFullStr = ((YamlDotNet.RepresentationModel.YamlScalarNode)varNode["keywords"]).Value;
                            List<string> kws = new List<string>();
                            if (!string.IsNullOrEmpty(kwFullStr))
                            {
                                var parts = kwFullStr.Split(KeyWordsSplitChars, StringSplitOptions.RemoveEmptyEntries);
                                if (parts != null && parts.Length > 0)
                                {
                                    kws.AddRange(parts);
                                }
                            }

                            var sv = new ShaderVariantCollection.ShaderVariant();
                            sv.shader = shader;
                            sv.passType = passType;
                            sv.keywords = kws.ToArray();
                            list.Add(sv);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            return list.ToArray();
        }

        public static void SplitShaderVariantCollection(string svcpath)
        {
            var svs = GetAllShaderVariantInShaderVariantCollection(svcpath);
            if (svs != null)
            {
                Dictionary<Shader, List<ShaderVariantCollection.ShaderVariant>> map = new Dictionary<Shader, List<ShaderVariantCollection.ShaderVariant>>();
                foreach (var sv in svs)
                {
                    var shader = sv.shader;
                    if (!map.ContainsKey(shader))
                    {
                        map[shader] = new List<ShaderVariantCollection.ShaderVariant>();
                    }
                    var list = map[shader];
                    list.Add(sv);
                }
                foreach (var kvp in map)
                {
                    var shader = kvp.Key;
                    var path = AssetDatabase.GetAssetPath(shader);
                    if (IsAssetInWriteableDirectory(path))
                    {
                        var subsvcpath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), System.IO.Path.GetFileNameWithoutExtension(path) + ".shadervariants");
                        if (!System.IO.File.Exists(subsvcpath))
                        {
                            AssetDatabase.CreateAsset(new ShaderVariantCollection(), subsvcpath);
                        }
                        var subsvc = (ShaderVariantCollection)AssetDatabase.LoadMainAssetAtPath(subsvcpath);
                        foreach (var sv in kvp.Value)
                        {
                            subsvc.Add(sv);
                        }
                    }
                }
                AssetDatabase.SaveAssets();
            }
        }

        public static void SplitAutoCollectedShaderVariants()
        {
            var resmanagerasmdefpath = UnityEditor.Compilation.CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(typeof(ModEditor).Assembly.GetName().Name);
            var mod = ModEditor.GetAssetModName(resmanagerasmdefpath);
            string pre = "Assets";
            if (!string.IsNullOrEmpty(mod))
            {
                pre = "Assets/Mods/" + mod;
            }
            var persistsvcpath = pre + "/Build/AutoCollectedSVC.shadervariants";
            SplitShaderVariantCollection(persistsvcpath);
        }

        public static void CollectShaderVariant()
        {
            var resmanagerasmdefpath = UnityEditor.Compilation.CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(typeof(ModEditor).Assembly.GetName().Name);
            var mod = ModEditor.GetAssetModName(resmanagerasmdefpath);
            string pre = "Assets";
            if (!string.IsNullOrEmpty(mod))
            {
                pre = "Assets/Mods/" + mod;
            }
            var tmpsvcpath = pre + "/Build/AutoCollectedSVCTemp.shadervariants";
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(tmpsvcpath));
            SaveEditorAutoCollectedShaderVariantTo(tmpsvcpath);
            var persistsvcpath = pre + "/Build/AutoCollectedSVC.shadervariants";
            if (!System.IO.File.Exists(persistsvcpath))
            {
                AssetDatabase.Refresh();
                AssetDatabase.MoveAsset(tmpsvcpath, persistsvcpath);
            }
            else
            {
                AssetDatabase.Refresh();
                var svs = GetAllShaderVariantInShaderVariantCollection(tmpsvcpath);
                var existing = (ShaderVariantCollection)AssetDatabase.LoadMainAssetAtPath(persistsvcpath);
                foreach (var sv in svs)
                {
                    existing.Add(sv);
                }
                AssetDatabase.SaveAssets();
                AssetDatabase.DeleteAsset(tmpsvcpath);
            }
        }

#if UNITY_INCLUDE_TESTS
#region TESTS
        [MenuItem("Test/Res/Show Selected Asset Main Type", priority = 600010)]
        private static void ShowSelectedAssetMainType()
        {
            if (Selection.assetGUIDs != null && Selection.assetGUIDs.Length > 0)
            {
                var guid = Selection.assetGUIDs[0];
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var type = AssetDatabase.GetMainAssetTypeAtPath(path);
                Debug.Log(type);
            }
        }
        [MenuItem("Test/Res/Collect Shader Variant", priority = 600020)]
        private static void CollectShaderVariantCommand()
        {
            CollectShaderVariant();
        }
        [MenuItem("Test/Res/Split Collected Shader Variant", priority = 600030)]
        private static void SplitAutoCollectedShaderVariantsCommand()
        {
            SplitAutoCollectedShaderVariants();
        }
#endregion
#endif
    }
}