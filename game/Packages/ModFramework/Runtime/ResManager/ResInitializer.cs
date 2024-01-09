using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResInitializer : ScriptableObject
{
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    private static class Initializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void CheckInit()
        {
        }
        static Initializer()
        {
            var assets = Resources.LoadAll<ResInitializer>("ResInitializer");
            if (assets != null)
            {
                for (int i = 0; i < assets.Length; ++i)
                {
                    var asset = assets[i];
                    if (asset)
                    {
                        asset.Init();
                    }
                }
            }
        }
    }

    public ResInitializer[] SubInitializers;

    public virtual void Init()
    {
        if (SubInitializers != null)
        {
            for (int i = 0; i < SubInitializers.Length; ++i)
            {
                var initializer = SubInitializers[i];
                if (initializer != null)
                {
                    initializer.Init();
                }
            }
        }
    }

    public static void CheckInit()
    {
        UnityEngineEx.ResManager.AfterLoadFirstScene();
        Initializer.CheckInit();
    }
}
