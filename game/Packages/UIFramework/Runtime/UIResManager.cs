using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;

namespace UnityEngineEx
{
    public static class UIResManager
    {
        private static Camera _UICamera;
        private static Camera _DialogCamera;
        private static bool _UICameraLoading;
        private const int SceneAndDialogCacheLayer = 17;
        private const string UICameraName = "UICameraAndEventSystem(Clone)";

        private static AudioListener _UIAudioListener = null;
        private static DualKawaseBlurRenderPassFeature _blurRPF;

        public static Camera FindUICamera()
        {
            if (!_UICameraLoading)
            {
                //if (_UICamera != null && !_UICamera.isActiveAndEnabled) _UICamera = null;
                if (_UICamera == null)
                {
                    _UICameraLoading = true;
                    CreateCameraAndEventSystem();
                    _UICameraLoading = false;
                }
            }
            return _UICamera;
        }

        public static Camera GetUIDialogCamera()
        {
            //if (_DialogCamera != null && !_DialogCamera.isActiveAndEnabled) _DialogCamera = null;
            if (_DialogCamera == null)
            {
                _UICameraLoading = true;
                CreateCameraAndEventSystem();
                _UICameraLoading = false;
            }
            return _DialogCamera;
        }

        private static GameObject _UICameraAndEventSystemTemplate = null;
        private static GameObject _UICameraAndEventSystemGo = null;
        public static Camera CreateCameraAndEventSystem()
        {
            if (_UICameraAndEventSystemTemplate == null) _UICameraAndEventSystemTemplate = ResManager.LoadResDeep("UICameraAndEventSystem.prefab") as GameObject;
            if (_UICameraAndEventSystemGo != null && _UICamera != null) return _UICamera;
            _UICameraAndEventSystemGo = GameObject.Instantiate(_UICameraAndEventSystemTemplate);
            _UICamera = GameObject.Find("UICamera").GetComponentInChildren<Camera>();
            _DialogCamera = GameObject.Find("DialogCamera").GetComponentInChildren<Camera>();
            _UIAudioListener = _UICameraAndEventSystemGo.GetComponentInChildren<AudioListener>();
            _blurRPF = _UICamera.gameObject.GetComponent<DualKawaseBlurRenderPassFeature>();
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying)
            {
                UnityEditor.EditorApplication.playModeStateChanged += UnloadCameraAndEventSystemTemplateOnPlayModeStateChanged;
            }
            else
            {
                UnloadCameraAndEventSystemTemplate();
            }

            if (UnityEditor.EditorApplication.isPlaying)
#endif
            Object.DontDestroyOnLoad(_UICameraAndEventSystemGo);
            return _UICamera;
        }

#if UNITY_EDITOR
        private static void UnloadCameraAndEventSystemTemplateOnPlayModeStateChanged(UnityEditor.PlayModeStateChange reason)
        {
            //if (reason == UnityEditor.PlayModeStateChange.EnteredEditMode)
            {
                UnloadCameraAndEventSystemTemplate();
                UnityEditor.EditorApplication.playModeStateChanged -= UnloadCameraAndEventSystemTemplateOnPlayModeStateChanged;
            }
        }
        public static void UnloadCameraAndEventSystemTemplate()
        {
            if (_UICameraAndEventSystemTemplate)
            {
                _UICameraAndEventSystemTemplate = null;
                UnityEditor.EditorUtility.UnloadUnusedAssetsImmediate();
            }
        }
#endif

        public static bool TryChangeToUIScene()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "UIScene")
            {
                ResManager.LoadScene("Common/UIScene.unity");
                return true;
            }
            return false;
        }

        public class PackedSceneObjs
        {
            public readonly List<GameObject> SceneObjs = new List<GameObject>();
            public readonly List<GameObject> DialogObjs = new List<GameObject>();
            public readonly List<bool> InitialSceneObjsActive = new List<bool>();
            public readonly List<bool> InitialDialogObjsActive = new List<bool>();
        }

        public static PackedSceneObjs PackSceneAndDialogs(GameObject[] dialogObjs)
        {
            var oldObjs = ResManager.FindAllGameObject();

            var ret = new PackedSceneObjs();
            foreach (var obj in oldObjs)
            {
                if ((obj.layer == SceneAndDialogCacheLayer) ||
                    (UICameraName.Equals(obj.name)))
                {
                    continue;
                }
                var canvas = obj.GetComponent<Canvas>();
                if (canvas != null && canvas.sortingLayerName == "Dialog")
                {
                    bool find = false;
                    if (dialogObjs != null)
                    {
                        foreach (var dialogObj in dialogObjs)
                        {
                            if (dialogObj != obj) continue;
                            find = true;
                            break;
                        }
                    }
                    if (find)
                    {
                        ret.DialogObjs.Add(obj);
                        ret.InitialDialogObjsActive.Add(obj.activeSelf);
                        continue;
                    }
                    ret.SceneObjs.Add(obj);
                    ret.InitialSceneObjsActive.Add(obj.activeSelf);
                    continue;
                }
                var canvasGroup = obj.GetComponent<CanvasGroup>();
                if (canvasGroup != null && canvasGroup.blocksRaycasts == false)
                {
                    GameObject.Destroy(obj);
                    continue;
                }
                ret.SceneObjs.Add(obj);
                ret.InitialSceneObjsActive.Add(obj.activeSelf);
            }
            // 按order从小到大排序，这样才能和lua中的记录对应上
            ret.DialogObjs.Sort((x, y) =>
            {
                var orderX = x.transform.GetComponent<Canvas>().sortingOrder;
                var orderY = y.transform.GetComponent<Canvas>().sortingOrder;
                return orderX - orderY;
            });
            for (int i = 0; i < ret.DialogObjs.Count; ++i)
            {
                ret.InitialDialogObjsActive.Add(ret.DialogObjs[i].activeSelf);
            }
            return ret;
        }

        public static List<GameObject> PackSceneObj()
        {
            return ResManager.FindAllGameObject();
        }

        public static void ChangeGameObjectLayer(Object o, int layer)
        {
            if (o == null) return;
            Component com = o as Component;
            GameObject go;
            if (com == null)
            {
                go = o as GameObject;
            }
            else
            {
                go = com.gameObject;
            }
            //遍历当前物体及其所有子物体
            foreach (Transform tran in go.GetComponentsInChildren<Transform>(true))
            {
                tran.gameObject.layer = layer;//更改物体的Layer层
            }
        }

        /// <summary>
        /// 如果进入的场景是.unity场景，就使用场景自带的audiolistener，
        /// 如果进入的是prefab的scene，就使用UICameraAndEventSystem里的
        /// </summary>
        /// <param name="path"></param>
        public static void SetUIAudioListener(string path)
        {
            int index = path.IndexOf("unity");
            bool flag = index == -1 ? true : false;
            if (_UIAudioListener && _UIAudioListener.enabled != flag)
            {
                _UIAudioListener.enabled = flag;
            }
        }

        public static void SetUIAudioListenerEnable(bool flag)
        {
            if (_UIAudioListener && _UIAudioListener.enabled != flag)
            {
                _UIAudioListener.enabled = flag;
            }
        }

        public static void SetCameraBlur(bool enabled, float blurRadius = 0, int iteration = 1, float rtDownScaling = 1, float time = 0, float rate = 0.3f)
        {
            if (_blurRPF)
            {
                _blurRPF.enabled = enabled;

                if (enabled)
                {
                    _blurRPF.DoCameraBlur(blurRadius, iteration, rtDownScaling, time, rate);
                }
                else
                {
                    _blurRPF.StopCameraBlur();
                }
            }
        }
    }
}