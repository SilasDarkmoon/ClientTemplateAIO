using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using uobj = UnityEngine.Object;

#if UNITY_EDITOR
namespace UnityEngineEx
{
    public static partial class ResManager
    {
        public static int __LINE__
        {
            get
            {
                return new System.Diagnostics.StackTrace(1, true).GetFrame(0).GetFileLineNumber();
            }
        }
        public static string __FILE__
        {
            get
            {
                return new System.Diagnostics.StackTrace(1, true).GetFrame(0).GetFileName();
            }
        }
        public static string __ASSET__
        {
            get
            {
                var file = new System.Diagnostics.StackTrace(1, true).GetFrame(0).GetFileName();

                return GetAssetNameFromPath(file) ?? file;
            }
        }
        public static string __MOD__
        {
            get
            {
                var file = new System.Diagnostics.StackTrace(1, true).GetFrame(0).GetFileName();
                var asset = GetAssetNameFromPath(file);

                string assetType, mod, dist;
                GetAssetNormPath(asset, out assetType, out mod, out dist);
                return mod;
            }
        }

        public static string[] GetAllMods()
        {
            return EditorToClientUtils.GetAllMods();
        }
        public static bool IsModOptional(string mod)
        {
            return EditorToClientUtils.IsModOptional(mod);
        }
        public static string[] GetCriticalMods()
        {
            return EditorToClientUtils.GetCriticalMods();
        }
        public static string GetModNameFromPackageName(string package)
        {
            return EditorToClientUtils.GetModNameFromPackageName(package);
        }
        public static string GetPackageNameFromModName(string mod)
        {
            return EditorToClientUtils.GetPackageNameFromModName(mod);
        }
        public static string GetAssetNameFromPath(string path)
        {
            return EditorToClientUtils.GetAssetNameFromPath(path);
        }
        public static string GetPathFromAssetName(string asset)
        {
            return EditorToClientUtils.GetPathFromAssetName(asset);
        }
    }
}
#endif