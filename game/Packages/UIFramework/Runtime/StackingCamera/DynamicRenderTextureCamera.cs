using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngineEx;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(UniversalAdditionalCameraData))]
public class DynamicRenderTextureCamera : StackingCamera
{
    public string RenderTexturePath;

    private Camera _Camera;
    private UniversalAdditionalCameraData _CameraEx;

    private void Awake()
    {
        _Camera = GetComponent<Camera>();
        _CameraEx = GetComponent<UniversalAdditionalCameraData>();
        if (_Camera && !_CameraEx)
        {
            _CameraEx = gameObject.AddComponent<UniversalAdditionalCameraData>();
        }

        if (_Camera && !string.IsNullOrEmpty(RenderTexturePath))
        {
            var rt = ResManager.LoadRes<RenderTexture>(RenderTexturePath);
            if (rt)
            {
                _Camera.targetTexture = rt;
            }
        }
    }
}