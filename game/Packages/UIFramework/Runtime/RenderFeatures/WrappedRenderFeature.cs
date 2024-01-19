using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class WrappedRenderFeature : ComponentBasedRenderFeature
{
    public ScriptableRendererFeature Wrapped;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (Wrapped)
        {
#if UNITY_EDITOR
            if (Wrapped is UniversalRenderFeature)
            {
                Debug.LogError("Should not wrap UniversalRenderFeature! Please remove it.");
                return;
            }
#endif
            Wrapped.AddRenderPasses(renderer, ref renderingData);
        }
    }
}