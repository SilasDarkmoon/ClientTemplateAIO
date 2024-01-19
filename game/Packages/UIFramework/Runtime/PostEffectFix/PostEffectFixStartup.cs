using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

using Object = UnityEngine.Object;

namespace UnityEngineEx
{
    static class PostEffectFixStartup
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnUnityStart()
        {
            var peprefab = Resources.Load<GameObject>("PostEffectSetupCamera");
            if (peprefab)
            {
                var layer = peprefab.GetComponent<PostProcessLayer>();
                if (layer)
                {
                    try
                    {
                        var resources = typeof(PostProcessLayer).GetField("m_Resources", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(layer) as PostProcessResources;
                        if (resources)
                        {
                            typeof(RuntimeUtilities).GetField("s_Resources", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).SetValue(null, resources);
                            var copyStdMaterial = RuntimeUtilities.copyStdMaterial;
                            var copyStdFromDoubleWideMaterial = RuntimeUtilities.copyStdFromDoubleWideMaterial;
                            var copyMaterial = RuntimeUtilities.copyMaterial;
                            var copyFromTexArrayMaterial = RuntimeUtilities.copyFromTexArrayMaterial;
                        }
                    }
                    catch (Exception e)
                    {
                        PlatDependant.LogError(e);
                    }
                }
            }
        }
    }
}