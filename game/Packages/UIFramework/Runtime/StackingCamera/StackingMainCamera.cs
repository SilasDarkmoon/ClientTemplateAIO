using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngineEx;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(UniversalAdditionalCameraData))]
public class StackingMainCamera : StackingCamera
{
    private Camera _Camera;
    private UniversalAdditionalCameraData _CameraEx;
    private List<Camera> _CameraStack;
    private Camera _FirstCameraInStack;

    public Camera Camera { get { return _Camera; } }
    public UniversalAdditionalCameraData CameraEx { get { return _CameraEx; } }

    private void Awake()
    {
        _Camera = GetComponent<Camera>();
        _CameraEx = GetComponent<UniversalAdditionalCameraData>();
        if (_Camera && !_CameraEx)
        {
            _CameraEx = gameObject.AddComponent<UniversalAdditionalCameraData>();
        }
        _CameraEx.renderType = CameraRenderType.Base;
        _Instance = this;
        ManageCameraStackRaw();
    }
    //private void Update()
    //{
    //    if (_Camera)
    //    {
    //        GetSceneCameras();
    //        ManageCameraStackRaw();
    //    }
    //}
    private void OnDestroy()
    {
        if (_Instance == this)
        {
            _Instance = null;
        }
    }

    private void ManageCameraStackRaw()
    {
        if (_Camera)
        {
            _SceneCameras.RemoveWhere(scenecam => !scenecam);
            var cameras = _SceneCameras;
            if (_CameraStack == null)
            {
                _CameraStack = _CameraEx.cameraStack;
            }
            var stack = _CameraStack;
            for (int i = stack.Count - 1; i >= 0; --i)
            {
                var old = stack[i];
                if (!old || !cameras.Contains(old))
                {
                    stack.RemoveAt(i);
                }
            }

            var oldcameras = new HashSet<Camera>(stack);
            bool newcam = false;
            foreach (var cam in cameras)
            {
                if (cam != _Camera && !oldcameras.Contains(cam))
                {
                    if (cam.targetTexture == null)
                    {
                        var camex = cam.gameObject.GetComponent<UniversalAdditionalCameraData>();
                        if (!camex)
                        {
                            camex = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
                        }
                        camex.renderType = CameraRenderType.Overlay;
                        cam.allowDynamicResolution = _Camera.allowDynamicResolution;
                        stack.Add(cam);
                        newcam = true;
                    }
                }
            }

            if (newcam)
            {
                stack.Sort((cam1, cam2) =>
                {
                    if (cam1.depth == cam2.depth) return 0;
                    else if (cam1.depth > cam2.depth) return 1;
                    else return -1;
                });
            }

            Camera first = null;
            if (stack.Count > 0)
            {
                first = stack[0];
            }
            if (first != _FirstCameraInStack)
            {
                _FirstCameraInStack = first;
                if (first != null)
                {
                    if (first.clearFlags == CameraClearFlags.Skybox)
                    {
                        _Camera.clearFlags = CameraClearFlags.Skybox;
                    }
                    else
                    {
                        _Camera.clearFlags = CameraClearFlags.SolidColor;
                        _Camera.backgroundColor = first.backgroundColor;
                    }
                }
                else
                {
                    _Camera.clearFlags = CameraClearFlags.SolidColor;
                    _Camera.backgroundColor = new Color();
                }
            }
            bool captureOpaque = false;
            bool captureDepth = false;
            
            for (int i = 0; i < stack.Count; ++i)
            {
                var cam = stack[i];
                if (cam)
                {
                    var camex = cam.GetComponent<UniversalAdditionalCameraData>();
                    if (camex)
                    {
                        if (camex.requiresColorOption == CameraOverrideOption.On)
                        {
                            captureOpaque = true;
                        }
                        if (camex.requiresDepthOption == CameraOverrideOption.On)
                        {
                            captureDepth = true;
                        }
                        if (captureOpaque && captureDepth)
                        {
                            break;
                        }
                    }
                }
            }
            _CameraEx.requiresColorOption = captureOpaque ? CameraOverrideOption.On : CameraOverrideOption.UsePipelineSettings;
            _CameraEx.requiresDepthOption = captureDepth ? CameraOverrideOption.On : CameraOverrideOption.UsePipelineSettings;
        }
    }
    public static void ManageCameraStack()
    {
        var instance = Instance;
        if (instance)
        {
            instance.ManageCameraStackRaw();
        }
    }

    private static StackingMainCamera _Instance;
    private static bool _IsInstanceCreating;
    public static StackingMainCamera Instance
    {
        get
        {
            if (!_Instance && !_IsInstanceCreating)
            {
                _IsInstanceCreating = true;
                CreateStackingMainCamera();
                _IsInstanceCreating = false;
            }
            return _Instance;
        }
    }
    public static bool HasInstance
    {
        get { return _Instance; }
    }

    public static int MainCameraRendererIndex = -1;
    private static void CreateStackingMainCamera()
    {
        var go = new GameObject("StackingMainCamera");
        DontDestroyOnLoad(go);
        var cam = go.AddComponent<Camera>();
        var camex = go.AddComponent<UniversalAdditionalCameraData>();
        var maincam = go.AddComponent<StackingMainCamera>();
        //go.AddComponent<FixHDRRenderTargetAlphaRenderFeature>();

        cam.cullingMask = 0;
        cam.depth = -100;
        cam.useOcclusionCulling = false;
        cam.allowDynamicResolution = true;

        camex.SetRenderer(MainCameraRendererIndex);
    }

    private static HashSet<Camera> _SceneCameras = new HashSet<Camera>();
    public static HashSet<Camera> GetSceneCameras()
    {
        var cams = _SceneCameras;
        cams.Clear();
        cams.UnionWith(Object.FindObjectsOfType<Camera>());
        return cams;
    }
    public static void RegSceneCamera(Camera cam)
    {
        if (cam)
        {
            _SceneCameras.Add(cam);
            ManageCameraStack();
        }
    }
    public static void UnregSceneCamera(Camera cam)
    {
        _SceneCameras.Remove(cam);
        if (_Instance)
        {
            ManageCameraStack();
        }
    }
}