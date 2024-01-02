using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
using UnityEngine;

using uobj = UnityEngine.Object;
#else
using PlayerPrefs = UnityEngineEx.IsolatedPrefs;
#endif

namespace UnityEngineEx
{
    public static partial class ResManager
    {
        private class ResManagerInit_DistributeFlags
        {
            public ResManagerInit_DistributeFlags()
            {
                DistributeFlags.OnDistributeFlagsChangedRuntime += RebuildRuntimeResCache;
            }
        }
        private static ResManagerInit_DistributeFlags _ResManagerInit_DistributeFlags = new ResManagerInit_DistributeFlags();

        public static List<string> PreRuntimeDFlags
        {
            get
            {
                return DistributeFlags.PreRuntimeDFlags;
            }
            set
            {
                DistributeFlags.PreRuntimeDFlags = value;
            }
        }

        public static HashSet<string> RuntimeForbiddenDFlags
        {
            get
            {
                return DistributeFlags.RuntimeForbiddenDFlags;
            }
        }

        public static List<string> RuntimeExDFlags
        {
            get
            {
                return DistributeFlags.RuntimeExDFlags;
            }
        }

        public static string[] GetDistributeFlags()
        {
            return DistributeFlags.GetDistributeFlags();
        }
        public static HashSet<string> GetDistributeFlagsSet()
        {
            return DistributeFlags.GetDistributeFlagsSet();
        }
        public static string[] GetValidDistributeFlags()
        {
            return DistributeFlags.GetValidDistributeFlags();
        }
        public static HashSet<string> GetValidDistributeFlagsSet()
        {
            return DistributeFlags.GetValidDistributeFlagsSet();
        }

        public static bool HasDistributeFlag(string flag)
        {
            return DistributeFlags.HasDistributeFlag(flag);
        }
        public static bool IsDistributeFlagValid(string flag)
        {
            return DistributeFlags.IsDistributeFlagValid(flag);
        }
        public static void RemoveDistributeFlag(string flag)
        {
            DistributeFlags.RemoveDistributeFlag(flag);
        }
        public static void AddDistributeFlag(string flag)
        {
            DistributeFlags.AddDistributeFlag(flag);
        }
        public static void SetDistributeFlags(IEnumerable<string> toRemove, IEnumerable<string> toAdd)
        {
            DistributeFlags.SetDistributeFlags(toRemove, toAdd);
        }
        public static void ReloadDistributeFlags()
        {
            DistributeFlags.ReloadDistributeFlags();
        }

#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
        public static void ClearLoadedDistributeDescs()
        {
            DistributeFlags.ClearLoadedDistributeDescs();
        }
        public static ModDesc GetDistributeDesc(string flag)
        {
            return DistributeFlags.GetDistributeDesc(flag);
        }
#endif
        public static bool CheckDistributeDep(string flag)
        {
            return DistributeFlags.CheckDistributeDep(flag);
        }

        public static string GetAssetNormPath(string rawpath, out string type, out string mod, out string dist)
        {
            return DistributeFlags.GetAssetNormPath(rawpath, out type, out mod, out dist);
        }
    }
}