using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;

namespace UnityEngineEx
{
#if UNITY_EDITOR
    public static partial class EditorToClientUtils
    {
        public static bool Ready = false;

        public static Func<string[]> GetAllModsFunc { set; private get; }
        public static Func<string, bool> CheckModOptionalFunc { set; private get; }
        public static Func<string, string> ModNameToPackageName { set; private get; }
        public static Func<string, string> PackageNameToModName { set; private get; }
        public static Func<string, string> AssetNameToPath { set; private get; }
        public static Func<string, string> PathToAssetName { set; private get; }

        public static string[] GetAllMods()
        {
            if (GetAllModsFunc != null)
            {
                return GetAllModsFunc();
            }
            return null;
        }
        public static bool IsModOptional(string mod)
        {
            if (CheckModOptionalFunc != null)
            {
                return CheckModOptionalFunc(mod);
            }
            return false;
        }

        public static string[] GetCriticalMods()
        {
            List<string> mods = new List<string>();
            if (GetAllModsFunc != null && CheckModOptionalFunc != null)
            {
                var allmods = GetAllModsFunc();
                for (int i = 0; i < allmods.Length; ++i)
                {
                    var mod = allmods[i];
                    if (!CheckModOptionalFunc(mod))
                    {
                        mods.Add(mod);
                    }
                }
                mods.Sort();
            }
            return mods.ToArray();
        }

        public static string GetModNameFromPackageName(string package)
        {
            if (PackageNameToModName != null)
            {
                return PackageNameToModName(package);
            }
            return null;
        }
        public static string GetPackageNameFromModName(string mod)
        {
            if (ModNameToPackageName != null)
            {
                return ModNameToPackageName(mod);
            }
            return null;
        }
        public static string GetAssetNameFromPath(string path)
        {
            if (PathToAssetName != null)
            {
                return PathToAssetName(path);
            }
            return path;
        }
        public static string GetPathFromAssetName(string asset)
        {
            if (AssetNameToPath != null)
            {
                return AssetNameToPath(asset);
            }
            return asset;
        }
    }
#endif
}