using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;

namespace UnityEngineEx
{
    public static class DontDestroyOnLoadManager
    {
        private static GameObject Finder;
        
        public static GameObject[] GetAllDontDestroyOnLoadObjs()
        {
            if (!Finder)
            {
                Finder = new GameObject("DontDestroyOnLoadFinder");
                Object.DontDestroyOnLoad(Finder);
                Finder.hideFlags = HideFlags.HideAndDontSave; // Notice: it is dangerous to make a GameObject HideAndDontSave but not DontDestroyOnLoad.
            }
            return Finder.scene.GetRootGameObjects();
        }
    }
}