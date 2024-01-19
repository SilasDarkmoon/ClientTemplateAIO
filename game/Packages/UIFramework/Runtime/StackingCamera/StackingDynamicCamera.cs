using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(UniversalAdditionalCameraData))]
public class StackingDynamicCamera : StackingCamera
{
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
    }

    private void OnEnable()
    {
        StackingMainCamera.RegSceneCamera(_Camera);
    }
    private void OnDisable()
    {
        StackingMainCamera.UnregSceneCamera(_Camera);
    }
}