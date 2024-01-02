using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngineEx;

using Object = UnityEngine.Object;

namespace UnityEditorEx
{
    public static class AssetDatabaseUtils
    {
        private static bool _Importing;
        private static readonly List<Action> _DelayedOpsInImporting = new List<Action>();

        private class AssetDatabaseUtils_Postprocessor_Pre : AssetPostprocessor
        {
            public override int GetPostprocessOrder()
            {
                return int.MinValue;
            }
            private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                _Importing = true;
            }
        }
        private class AssetDatabaseUtils_Postprocessor_Post : AssetPostprocessor
        {
            public override int GetPostprocessOrder()
            {
                return int.MaxValue;
            }
            private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                _Importing = false;
                for (int i = 0; i < _DelayedOpsInImporting.Count; ++i)
                {
                    var act = _DelayedOpsInImporting[i];
                    if (act != null)
                    {
                        act();
                    }
                }
                _DelayedOpsInImporting.Clear();
            }
        }

        public static void RunAfterImporting(Action act)
        {
            if (act != null)
            {
                if (_Importing)
                {
                    _DelayedOpsInImporting.Add(act);
                }
                else
                {
                    act();
                }
            }
        }
        public static void ForceImportAssetSafe(string asset)
        {
            RunAfterImporting(() => AssetDatabase.ImportAsset(asset, ImportAssetOptions.ForceUpdate));
        }
        public static void CreateAssetSafe(Object obj, string path)
        {
            RunAfterImporting(() => AssetDatabase.CreateAsset(obj, path));
        }
        public static void SaveChangedAssetsSafe()
        {
            RunAfterImporting(AssetDatabase.SaveAssets);
        }
    }
}