using UnityEngineEx;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UnityEditorEx
{
    [InitializeOnLoad]
    public class AnimOnlyModelResBuilder : ResBuilder.BaseResBuilderEx<AnimOnlyModelResBuilder>
    {
        private static HierarchicalInitializer _Initializer = new HierarchicalInitializer(0);

        public override string FormatBundleName(string asset, string mod, string dist, string norm)
        {
            if (asset.EndsWith(".animonly.txt"))
            {
                if (System.IO.Path.GetFileName(asset) == "builder.animonly.txt")
                {
                    var dir = System.IO.Path.GetDirectoryName(asset);
                    var files = System.IO.Directory.GetFiles(dir);
                    for (int i = 0; i < files.Length; ++i)
                    {
                        var file = files[i].Replace('\\', '/');
                        if (System.IO.File.Exists(file))
                        {
                            if (AssetImporter.GetAtPath(file) is ModelImporter)
                            {
                                DeleteAllSubAssetsExceptAnim(file);
                            }
                        }
                    }
                }
                else if (asset.EndsWith(".builder.animonly.txt"))
                {
                    var file = asset.Substring(0, asset.Length - ".builder.animonly.txt".Length);
                    if (System.IO.File.Exists(file))
                    {
                        DeleteAllSubAssetsExceptAnim(file);
                    }
                }
            }
            return null;
        }

        public static void DeleteAllSubAssetsExceptAnim(string assetpath)
        {
            var assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetpath);
            for (int i = 0; i < assets.Length; ++i)
            {
                var asset = assets[i];
                if (!(asset is AnimationClip))
                {
                    GameObject.DestroyImmediate(asset, true);
                }
            }
        }
    }
}