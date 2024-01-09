using UnityEngineEx;
using System.Collections.Generic;
using UnityEditor;

namespace UnityEditorEx
{
    [InitializeOnLoad]
    public class ResRefBuilder : ResBuilder.BaseResBuilderEx<ResRefBuilder>
    {
        private static HierarchicalInitializer _Initializer = new HierarchicalInitializer(0);

        public override void GenerateBuildWork(string bundleName, IList<string> assets, ResBuilder.IBundleBuildInfo bwork, ResBuilder.IResBuildWork modwork, int bindex)
        {
            List<string> RefTargets = new List<string>();
            HashSet<string> RefTargetsMap = new HashSet<string>();

            for (int i = 0; i < assets.Count; ++i)
            {
                var path = assets[i];
                var assetType = AssetDatabase.GetMainAssetTypeAtPath(path);
                if (assetType == typeof(ResRef))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<ResRef>(path);
                    if (asset)
                    {
                        if (asset.Refs != null)
                        {
                            for (int j = 0; j < asset.Refs.Length; ++j)
                            {
                                var aref = asset.Refs[j];
                                var refpath = AssetDatabase.GetAssetPath(aref);
                                if (refpath != null)
                                {
                                    if (!assets.Contains(refpath))
                                    {
                                        if (RefTargetsMap.Add(refpath))
                                        {
                                            RefTargets.Add(refpath);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (RefTargets.Count > 0)
            {
                var listfull = new List<string>(bwork.Assets);
                listfull.AddRange(RefTargets);
                bwork.Assets = listfull;
            }
        }
    }
}