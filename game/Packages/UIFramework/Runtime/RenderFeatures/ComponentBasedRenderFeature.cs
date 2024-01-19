using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteInEditMode]
public abstract class ComponentBasedRenderFeature : MonoBehaviour
{
    public UnityEngine.CameraType TargetCameraType;

    protected Camera _Camera;
    public Camera Camera { get { return _Camera; } }

    private void OnValidate()
    {
#if UNITY_EDITOR
        UniversalRenderFeature.RemoveRenderFeature(this);
        if (enabled && gameObject.activeInHierarchy)
        {
            //if (Application.isPlaying || GetType().GetCustomAttributes(typeof(ExecuteInEditMode), true).Length > 0)
            {
                Awake();
                OnEnable();
            }
        }
#endif
    }
    protected virtual void Awake()
    {
        _Camera = GetComponent<Camera>();
    }
    private void OnEnable()
    {
        UniversalRenderFeature.RegRenderFeature(this);
    }
    private void OnDisable()
    {
        UniversalRenderFeature.UnregRenderFeature(this);
    }

    public abstract void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData);
}