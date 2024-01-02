using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;

namespace UnityEngineEx
{
    using CoroutineTasks;

    public static partial class ResManager
    {
        public interface IResLoader : ILifetime
        {
            //void OnEnable();
            void BeforeLoadFirstScene();
            void AfterLoadFirstScene();

            object Preload(string asset);
            Object LoadRes(string asset, Type type);
            void LoadScene(string name, bool additive);

            CoroutineWork LoadResAsync(string asset, Type type);
            CoroutineWork LoadSceneAsync(string name, bool additive);

            void UnloadUnusedRes();
            void UnloadAllRes(bool unloadPermanentBundle);
            void MarkPermanent(string assetname);

            List<string> ParseRunningResKeys();
            List<string> GetLoadedBundleFileNames();
            void AfterResFilesDeployed();
        }
        private static IResLoader _ResLoader;
        public static IResLoader ResLoader
        {
            get { return _ResLoader; }
            set
            {
                if (_ResLoader != null)
                {
                    RemoveInitItem(_ResLoader);
                }
                _ResLoader = value;
                AddInitItem(value);
            }
        }
        public interface ISceneDestroyHandler
        {
            void PreDestroy(IList<GameObject> objs);
            void PostDestroy();
        }
        private static List<ISceneDestroyHandler> _DestroyHandlers = new List<ISceneDestroyHandler>();
        public static List<ISceneDestroyHandler> DestroyHandlers { get { return _DestroyHandlers; } }

        public static Object LoadFromResource(string name, Type type)
        {
            if (!string.IsNullOrEmpty(name))
            {
                var ext = System.IO.Path.GetExtension(name);
                name = name.Substring(0, name.Length - ext.Length);

                string[] distributeFlags = GetValidDistributeFlags();
                if (distributeFlags != null)
                {
                    for (int i = distributeFlags.Length - 1; i >= 0; --i)
                    {
                        var flag = distributeFlags[i];
                        var real = "dist/" + flag + "/" + name;
                        Object loaded;
                        if (type == null)
                        {
                            loaded = Resources.Load(real);
                            if (loaded is Texture2D)
                            {
                                var sprite = Resources.Load<Sprite>(real);
                                if (sprite)
                                {
                                    loaded = sprite;
                                }
                            }
                        }
                        else
                        {
                            loaded = Resources.Load(real, type);
                        }
                        if (loaded)
                        {
                            return loaded;
                        }
                    }
                }
                if (type == null)
                {
                    var loaded = Resources.Load(name);
                    if (loaded is Texture2D)
                    {
                        var sprite = Resources.Load<Sprite>(name);
                        if (sprite)
                        {
                            loaded = sprite;
                        }
                    }
                    return loaded;
                }
                else
                {
                    return Resources.Load(name, type);
                }
            }
            return null;
        }
        public static Object LoadFromResource(string name)
        {
            return LoadFromResource(name, null);
        }
        public static object Preload(string asset)
        {
#if PROFILER_EX_FRAME_TIMER
            using (var timercon = _FrameTimerContext.Restart("PreloadRes: {0}", asset))
#endif
#if ENABLE_PROFILER
            using (var pcon = ProfilerContext.Create("PreloadRes"))
            using (var pcon2 = ProfilerContext.Create(asset))
#endif
            return ResLoader.Preload(asset);
        }
        public static Object LoadRes(string asset, Type type)
        {
#if PROFILER_EX_FRAME_TIMER
            using (var timercon = _FrameTimerContext.Restart("LoadRes: {0}", asset))
#endif
#if ENABLE_PROFILER
            using (var pcon = ProfilerContext.Create("LoadRes"))
            using (var pcon2 = ProfilerContext.Create(asset))
#endif
            return ResLoader.LoadRes(asset, type);
        }
        public static Object LoadRes(string asset)
        {
            return LoadRes(asset, null);
        }
        public static T LoadRes<T>(string asset) where T : Object
        {
            return LoadRes(asset, typeof(T)) as T;
        }
        public static Object LoadResDeep(string asset, Type type)
        {
            var obj = LoadRes(asset, type);
            if (obj)
            {
                return obj;
            }
            var res = LoadFromResource(asset, type);
            if (!res)
            {
                Debug.LogError("Cannot LoadResDeep: " + asset);
            }
            return res;
        }
        public static Object LoadResDeep(string asset)
        {
            return LoadResDeep(asset, null);
        }

        private static List<Pack<string, bool, bool>> LoadingScenes = new List<Pack<string, bool, bool>>();
        private static int LoadingSceneQueueIndex = -1;
        private static CoroutineMonitorConcurrent LoadingSceneWork;
        public static void LoadScene(string name, bool additive)
        {
            //if (!additive)
            //{
            //    string loadingScene = System.IO.Path.GetFileNameWithoutExtension(name);
            //    if (loadingScene != "IntermediateScene")
            //    {
            //        if (LoadingScenes.Count > 0)
            //        {
            //            string curScene = System.IO.Path.GetFileNameWithoutExtension(LoadingScenes[LoadingScenes.Count - 1].t1);
            //            if (curScene == loadingScene)
            //            {
            //                LoadScene("Common/IntermediateScene.unity", false);
            //            }
            //        }
            //        else
            //        {
            //            var sceneCnt = UnityEngine.SceneManagement.SceneManager.sceneCount;
            //            for (int i = 0; i < sceneCnt; ++i)
            //            {
            //                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).name == loadingScene)
            //                {
            //                    LoadScene("Common/IntermediateScene.unity", false);
            //                    break;
            //                }
            //            }
            //        }
            //    }
            //}

            LoadingScenes.Add(new Pack<string, bool, bool>(name, false, additive));
            if (LoadingScenes.Count == 1)
            {
                LoadingSceneQueueIndex = 0;
                UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoadedClearLoadingQueue;
#if PROFILER_EX_FRAME_TIMER
                using (var timercon = _FrameTimerContext.Restart("LoadScene: {0}", name))
#endif
#if ENABLE_PROFILER
                using (var pcon = ProfilerContext.Create("LoadScene"))
                using (var pcon2 = ProfilerContext.Create(name))
#endif
                ResLoader.LoadScene(name, additive);
            }
        }
        private static void OnSceneLoadedClearLoadingQueue(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            ++LoadingSceneQueueIndex;
            if (LoadingSceneQueueIndex < LoadingScenes.Count)
            {
                var info = LoadingScenes[LoadingSceneQueueIndex];
                if (info.t2)
                {
                    if (LoadingSceneWork == null)
                    {
                        CreateLoadingSceneWork();
                    }
#if PROFILER_EX_FRAME_TIMER
                    using (var timercon = _FrameTimerContext.Restart("Begin LoadSceneAsync: {0}", info.t1))
#endif
#if ENABLE_PROFILER
                    using (var pcon = ProfilerContext.Create("Begin LoadSceneAsync"))
                    using (var pcon2 = ProfilerContext.Create(info.t1))
#endif
                    {
                        var work = ResLoader.LoadSceneAsync(info.t1, info.t3);
                        LoadingSceneWork.InsertWork(work, LoadingSceneWork.WorkCount - 1);
                    }
                }
                else
                {
#if PROFILER_EX_FRAME_TIMER
                    using (var timercon = _FrameTimerContext.Restart("LoadScene: {0}", info.t1))
#endif
#if ENABLE_PROFILER
                    using (var pcon = ProfilerContext.Create("LoadScene"))
                    using (var pcon2 = ProfilerContext.Create(info.t1))
#endif
                    ResLoader.LoadScene(info.t1, info.t3);
                }
            }
            else
            {
                LoadingScenes.Clear();
                LoadingSceneQueueIndex = -1;
                LoadingSceneWork = null;
                UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoadedClearLoadingQueue;
            }
        }
        public static void LoadScene(string name)
        {
            LoadScene(name, false);
        }
        public static void RebuildRuntimeResCache()
        {
            OnRebuildRuntimeResCache();
        }
        public static event Action OnRebuildRuntimeResCache = () => { };

        private static IEnumerator LoadFromResourceWork(CoroutineWork req, string name, Type type)
        {
            if (req.Result != null)
            {
                yield break;
            }
            while (AsyncWorkTimer.Check()) yield return null;
            if (!string.IsNullOrEmpty(name))
            {
                var ext = System.IO.Path.GetExtension(name);
                name = name.Substring(0, name.Length - ext.Length);

                string[] distributeFlags = GetValidDistributeFlags();
                if (distributeFlags != null)
                {
                    for (int i = distributeFlags.Length - 1; i >= 0; --i)
                    {
                        while (AsyncWorkTimer.Check()) yield return null;

                        var flag = distributeFlags[i];
                        ResourceRequest rawreq = null;
                        if (type == null)
                        {
                            try
                            {
                                rawreq = Resources.LoadAsync("dist/" + flag + "/" + name);
                            }
                            catch (Exception e)
                            {
                                PlatDependant.LogError(e);
                            }
                        }
                        else
                        {
                            try
                            {
                                rawreq = Resources.LoadAsync("dist/" + flag + "/" + name, type);
                            }
                            catch (Exception e)
                            {
                                PlatDependant.LogError(e);
                            }
                        }
                        if (rawreq != null)
                        {
                            yield return rawreq;
                            if (rawreq.asset != null)
                            {
                                req.Result = rawreq.asset;
                                if (type == null && rawreq.asset is Texture2D)
                                {
                                    rawreq = null;
                                    try
                                    {
                                        rawreq = Resources.LoadAsync("dist/" + flag + "/" + name, typeof(Sprite));
                                    }
                                    catch (Exception e)
                                    {
                                        PlatDependant.LogError(e);
                                    }
                                    if (rawreq != null)
                                    {
                                        yield return rawreq;
                                        if (rawreq.asset != null)
                                        {
                                            req.Result = rawreq.asset;
                                        }
                                    }
                                }
                                yield break;
                            }
                        }
                    }
                }
                {
                    while (AsyncWorkTimer.Check()) yield return null;
                    ResourceRequest rawreq = null;
                    if (type == null)
                    {
                        try
                        {
                            rawreq = Resources.LoadAsync(name);
                        }
                        catch (Exception e)
                        {
                            PlatDependant.LogError(e);
                        }
                    }
                    else
                    {
                        try
                        {
                            rawreq = Resources.LoadAsync(name, type);
                        }
                        catch (Exception e)
                        {
                            PlatDependant.LogError(e);
                        }
                    }
                    if (rawreq != null)
                    {
                        yield return rawreq;
                        if (rawreq.asset != null)
                        {
                            req.Result = rawreq.asset;
                            if (type == null && rawreq.asset is Texture2D)
                            {
                                rawreq = null;
                                try
                                {
                                    rawreq = Resources.LoadAsync(name, typeof(Sprite));
                                }
                                catch (Exception e)
                                {
                                    PlatDependant.LogError(e);
                                }
                                if (rawreq != null)
                                {
                                    yield return rawreq;
                                    if (rawreq.asset != null)
                                    {
                                        req.Result = rawreq.asset;
                                    }
                                }
                            }
                            yield break;
                        }
                    }
                }
            }
        }
        public static CoroutineWork LoadFromResourceAsync(string name, Type type)
        {
            var work = new CoroutineWorkSingle();
            work.SetWork(LoadFromResourceWork(work, name, type));
            return work;
        }
        public static CoroutineWork LoadFromResourceAsync(string name)
        {
            return LoadFromResourceAsync(name, null);
        }
        public static CoroutineWork LoadResAsync(string name, Type type)
        {
#if PROFILER_EX_FRAME_TIMER
            using (var timercon = _FrameTimerContext.Restart("Begin LoadResAsync: {0}", name))
#endif
#if ENABLE_PROFILER
            using (var pcon = ProfilerContext.Create("Begin LoadResAsync"))
            using (var pcon2 = ProfilerContext.Create(name))
#endif
            return ResLoader.LoadResAsync(name, type);
        }
        public static CoroutineWork LoadResAsync(string name)
        {
            return LoadResAsync(name, null);
        }
        public static CoroutineWork LoadResDeepAsync(string name, Type type)
        {
            var queue = new CoroutineWorkQueue();
            queue.AddWork(LoadResAsync(name, type));

            var work = new CoroutineWorkSingle();
            work.SetWork(LoadFromResourceWork(work, name, type));
            queue.AddWork(work);
            return queue;
        }
        public static CoroutineWork LoadResDeepAsync(string name)
        {
            return LoadResDeepAsync(name, null);
        }
        public static CoroutineWork LoadSceneAsync(string name, bool additive)
        {
            //if (!additive)
            //{
            //    string loadingScene = System.IO.Path.GetFileNameWithoutExtension(name);
            //    if (loadingScene != "IntermediateScene")
            //    {
            //        if (LoadingScenes.Count > 0)
            //        {
            //            string curScene = System.IO.Path.GetFileNameWithoutExtension(LoadingScenes[LoadingScenes.Count - 1].t1);
            //            if (curScene == loadingScene)
            //            {
            //                LoadScene("Common/IntermediateScene.unity", false);
            //            }
            //        }
            //        else
            //        {
            //            var sceneCnt = UnityEngine.SceneManagement.SceneManager.sceneCount;
            //            for (int i = 0; i < sceneCnt; ++i)
            //            {
            //                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).name == loadingScene)
            //                {
            //                    LoadScene("Common/IntermediateScene.unity", false);
            //                    break;
            //                }
            //            }
            //        }
            //    }
            //}

            LoadingScenes.Add(new Pack<string, bool, bool>(name, true, additive));
            if (LoadingScenes.Count == 1)
            {
                LoadingSceneQueueIndex = 0;
                CreateLoadingSceneWork();
                UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoadedClearLoadingQueue;
#if PROFILER_EX_FRAME_TIMER
                using (var timercon = _FrameTimerContext.Restart("Begin LoadSceneAsync: {0}", name))
#endif
#if ENABLE_PROFILER
                using (var pcon = ProfilerContext.Create("Begin LoadSceneAsync"))
                using (var pcon2 = ProfilerContext.Create(name))
#endif
                {
                    var work = ResLoader.LoadSceneAsync(name, additive);
                    LoadingSceneWork.InsertWork(work, LoadingSceneWork.WorkCount - 1);
                }
                return LoadingSceneWork;
            }
            else
            {
                if (LoadingSceneWork == null)
                { // The previous loading scene is sync-loading
                    CreateLoadingSceneWork();
                }
                return LoadingSceneWork;
            }
        }
        public static CoroutineWork LoadSceneAsync(string name)
        {
            return LoadSceneAsync(name, false);
        }
        private class LoadSceneAsyncYieldable : UnityEngine.CustomYieldInstruction
        {
            public override bool keepWaiting
            {
                get
                {
                    return LoadingScenes.Count > 0;
                }
            }
        }
        private static void CreateLoadingSceneWork()
        {
            LoadingSceneWork = new CoroutineMonitorConcurrent();
            LoadingSceneWork.AddWork(new CoroutineWorkSingle(new LoadSceneAsyncYieldable()));
            LoadingSceneWork.TryStart();
        }

        public static void UnloadUnusedRes()
        {
            Resources.UnloadUnusedAssets();
            ResLoader.UnloadUnusedRes();
        }
        public static IEnumerator UnloadUnusedResDeepAsync()
        {
            yield return Resources.UnloadUnusedAssets();
            ResLoader.UnloadUnusedRes();
        }
        public static IEnumerator UnloadUnusedResDeep()
        {
            for (int i = 0; i < 3; ++i)
            {
                yield return UnloadUnusedResDeepStep();
            }
        }
        public static void CollectGarbageLite()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        public static IEnumerator UnloadUnusedResAsync()
        {
            yield return Resources.UnloadUnusedAssets();
        }
        public static void UnloadUnusedResLite()
        {
            ResLoader.UnloadUnusedRes();
        }

        public static IEnumerator UnloadUnusedResDeepStep()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            yield return UnloadUnusedResDeepAsync();
        }
        public static void UnloadAllRes()
        {
            ResLoader.UnloadAllRes(false);
        }
        public static void UnloadAllRes(bool unloadPermanentBundle)
        {
            ResLoader.UnloadAllRes(unloadPermanentBundle);
        }
        public static void MarkPermanent(string assetname)
        {
            ResLoader.MarkPermanent(assetname);
        }
        public static List<string> ParseRunningResKeys()
        {
            return ResLoader.ParseRunningResKeys();
        }
        public static List<string> GetLoadedBundleFileNames()
        {
            return ResLoader.GetLoadedBundleFileNames();
        }
        public static void AfterResFilesDeployed()
        {
            ResLoader.AfterResFilesDeployed();
        }

        public static bool IsClientResLoader
        {
            get
            {
#if UNITY_EDITOR
                return !(_ResLoader is EditorResLoader);
#else
                return true;
#endif
            }
        }

        public static List<GameObject> FindAllGameObject()
        {
            var count = UnityEngine.SceneManagement.SceneManager.sceneCount;
            UnityEngine.SceneManagement.Scene sceneItem;
            List<GameObject> list = new List<GameObject>();
            for (int i = 0; i < count; i++)
            {
                sceneItem = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                list.AddRange(sceneItem.GetRootGameObjects());
            }
            return list;
        }
        public static void DestroyAllHard()
        {
            var oldObjs = FindAllGameObject();
            for (int i = 0; i < _DestroyHandlers.Count; ++i)
            {
                _DestroyHandlers[i].PreDestroy(oldObjs);
            }
            for (int i = 0; i < oldObjs.Count; ++i)
            {
                Object.Destroy(oldObjs[i]);
            }

            var ddols = DontDestroyOnLoadManager.GetAllDontDestroyOnLoadObjs();
            for (int i = 0; i < _DestroyHandlers.Count; ++i)
            {
                _DestroyHandlers[i].PreDestroy(ddols);
            }
            for (int i = 0; i < ddols.Length; ++i)
            {
                Object.Destroy(ddols[i]);
            }

            for (int i = 0; i < _DestroyHandlers.Count; ++i)
            {
                _DestroyHandlers[i].PostDestroy();
            }

            UnloadAllRes(true);
        }
        public static void DestroyAllHardSafe()
        {
            var oldObjs = FindAllGameObject();
            for (int i = 0; i < _DestroyHandlers.Count; ++i)
            {
                _DestroyHandlers[i].PreDestroy(oldObjs);
            }
            for (int i = 0; i < oldObjs.Count; ++i)
            {
                Object.Destroy(oldObjs[i]);
            }

            var ddols = DontDestroyOnLoadManager.GetAllDontDestroyOnLoadObjs();
            for (int i = 0; i < _DestroyHandlers.Count; ++i)
            {
                _DestroyHandlers[i].PreDestroy(ddols);
            }
            for (int i = 0; i < ddols.Length; ++i)
            {
                Object.Destroy(ddols[i]);
            }

            for (int i = 0; i < _DestroyHandlers.Count; ++i)
            {
                _DestroyHandlers[i].PostDestroy();
            }

            UnloadAllRes();
        }
        public static void DestroyAll()
        {
            var oldObjs = FindAllGameObject();
            for (int i = 0; i < _DestroyHandlers.Count; ++i)
            {
                _DestroyHandlers[i].PreDestroy(oldObjs);
            }
            for (int i = 0; i < oldObjs.Count; ++i)
            {
                Object.Destroy(oldObjs[i]);
            }
            for (int i = 0; i < _DestroyHandlers.Count; ++i)
            {
                _DestroyHandlers[i].PostDestroy();
            }

            UnloadAllRes();
        }
        private static void OnLowMemory()
        {
#if PROFILER_EX_FRAME_TIMER
            ProfilerEx.AppendFrameTimerMessageLine("LowMemory");
#endif
#if ENABLE_PROFILER
            using (var pcon = ProfilerContext.Create("LowMemory"))
#endif
            StartGarbageCollectNorm();
        }

#if PROFILER_EX_FRAME_TIMER
        private static FrameTimerContext _FrameTimerContext = new FrameTimerContext("ResLoaderTimer");
#endif

#if COMPATIBLE_RESMANAGER_V1
        public static string CompatibleAssetName(string asset)
        {
            if (asset != null && asset.StartsWith("Assets/MosaicRes/"))
            {
                return asset.Substring("Assets/MosaicRes/".Length);
            }
            return asset;
        }
#endif

        private static int _InitStatus = 0;
        private static void DoResManagerInitPre()
        {
            ResLoader.BeforeLoadFirstScene();
            GarbageCollector.GarbageCollectorEvents[0] += CollectGarbageLite;
            GarbageCollector.GarbageCollectorEvents[1] += UnloadUnusedResAsync;
            GarbageCollector.GarbageCollectorEvents[2] += UnloadUnusedResLite;
#if !UNITY_EDITOR
            Application.lowMemory += OnLowMemory;
#endif
        }
        private static void DoResManagerInitPost()
        {
            if (ModUnityMainBehav.MainBehavInstance == null)
            {
                var inititems = GetInitItems(int.MinValue, int.MaxValue);
                for (int i = 0; i < inititems.Length; ++i)
                {
                    inititems[i].Prepare();
                }
                for (int i = 0; i < inititems.Length; ++i)
                {
                    inititems[i].Init();
                }
            }
            ResLoader.AfterLoadFirstScene();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void BeforeLoadFirstScene()
        {
            if (_InitStatus == 0)
            {
                _InitStatus = 1;
                DoResManagerInitPre();
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void AfterLoadFirstScene()
        {
            BeforeLoadFirstScene();
            if (_InitStatus == 1)
            {
                _InitStatus = 2;
                DoResManagerInitPost();
            }
        }
    }
}