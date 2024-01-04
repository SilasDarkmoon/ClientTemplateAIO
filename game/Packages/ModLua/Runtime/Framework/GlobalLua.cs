#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
using UnityEngine;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using LuaLib;

using lua = LuaLib.LuaCoreLib;
using lual = LuaLib.LuaAuxLib;
using luae = LuaLib.LuaLibEx;

namespace LuaLib
{
    public static partial class LuaExLibs
    {
        public delegate void LuaInitFunc(IntPtr L);
        public class LuaExLibItem
        {
            public LuaInitFunc InitFunc;
            public int Order;

            protected LuaExLibItem()
            {
                InitFuncs.Add(this);
            }
            public LuaExLibItem(LuaInitFunc initFunc, int order) : this()
            {
                InitFunc = initFunc;
                Order = order;
            }
        }

        private static List<LuaExLibItem> _InitFuncs;
        public static List<LuaExLibItem> InitFuncs
        {
            get
            {
                if (_InitFuncs == null)
                {
                    _InitFuncs = new List<LuaExLibItem>();
                }
                return _InitFuncs;
            }
        }
    }
}

namespace UnityEngineEx
{
    public static class GlobalLua
    {
        public static LuaState L;

        static GlobalLua()
        {
#if UNITY_EDITOR
            if (SafeInitializerUtils.CheckShouldDelay()) return;
#endif
            LuaExLibs.InitFuncs.Sort((a, b) => a.Order - b.Order);
            LuaExLibs.InitFuncs.TrimExcess();
#if UNITY_EDITOR || !UNITY_ENGINE && !UNITY_5_3_OR_NEWER
            Init();
#endif
#if UNITY_EDITOR || ENABLE_EX_STACK_TRACE_LUA
            PlatDependant.OnExStackTrace = ExStackTraceLua;
#endif
        }

        public static void Init()
        {
#if UNITY_EDITOR
            try
            { 
#endif
            if (object.ReferenceEquals(L, null))
            {
                L = new LuaState();
                for (int i = 0; i < LuaExLibs.InitFuncs.Count; ++i)
                {
                    var func = LuaExLibs.InitFuncs[i].InitFunc;
                    if (func != null)
                    {
                        try
                        {
                            func(L);
                        }
                        catch (Exception e)
                        {
                            PlatDependant.LogError(e);
                        }
                    }
                }
            }
            else
            {
                LuaFramework.ClrFuncReset(L.L);
            }
#if UNITY_EDITOR
            }
            catch (DllNotFoundException e)
            {
                Debug.LogException(e);
                LuaDllPostprocessor.LuaDllMissing = true;
            }
#endif
        }
        public static void Reinit()
        {
            if (!object.ReferenceEquals(L, null))
            {
                L.Dispose();
                L = null;
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
            }
            Init();
        }

#if !UNITY_ENGINE && !UNITY_5_3_OR_NEWER
        public static int luaopen_ModLua(IntPtr l)
        {
            if (!object.ReferenceEquals(L, null))
            {
                L.Dispose();
                L = null;
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
            }
            L = new LuaState(l);
            for (int i = 0; i < LuaExLibs.InitFuncs.Count; ++i)
            {
                var func = LuaExLibs.InitFuncs[i].InitFunc;
                if (func != null)
                {
                    try
                    {
                        func(L);
                    }
                    catch (Exception e)
                    {
                        PlatDependant.LogError(e);
                    }
                }
            }
            if (ResManager.IsInUnityFolder)
            {
                //LuaLib.LuaFramework.TryRequireLua(L, "clrstruct.init");
                LuaLib.LuaFramework.TryRequireLua(L, "libs.init");
                LuaLib.LuaFramework.TryRequireLua(L, "core.init");
            }
            return 0;
        }
#endif

#if UNITY_EDITOR
        private static bool IsEditorRunning = false;
        public static void EditorCheckRunningState()
        {
            if (!IsEditorRunning && Application.isPlaying)
            {
                IsEditorRunning = true;

                Action onPlayModeChanged = null;
                onPlayModeChanged = () =>
                {
                    if (!Application.isPlaying)
                    {
                        Reinit();

                        EditorBridge.OnPlayModeChanged -= onPlayModeChanged;
                        IsEditorRunning = false;
                    }
                };
                EditorBridge.OnPlayModeChanged += onPlayModeChanged;
            }
        }
#endif

#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
        public static Coroutine StartLuaCoroutine(LuaFunc lfunc)
        {
            return StartLuaCoroutineForBehav(null, lfunc);
        }
        public static Coroutine StartLuaCoroutineForBehav(this MonoBehaviour behav, LuaFunc lfunc)
        {
            var work = EnumLuaCoroutine(lfunc);
            if (behav != null)
            {
                return CoroutineRunner.StartSafeCoroutine(behav, work);
            }
            else
            {
                return CoroutineRunner.StartCoroutine(work);
            }
        }
        public static IEnumerator EnumLuaCoroutine(LuaFunc lfunc)
        {
            if (lfunc != null)
            {
                LuaThread lthd = new LuaThread(lfunc);
                lfunc.Dispose();
                return EnumLuaCoroutine(lthd);
            }
            else
            {
                return CoroutineRunner.GetEmptyEnumerator();
            }
        }

        public static Coroutine StartLuaCoroutine(LuaOnStackFunc lfunc)
        {
            return StartLuaCoroutineForBehav(null, lfunc);
        }
        public static Coroutine StartLuaCoroutineForBehav(this MonoBehaviour behav, LuaOnStackFunc lfunc)
        {
            var work = EnumLuaCoroutine(lfunc);
            if (behav != null)
            {
                return CoroutineRunner.StartSafeCoroutine(behav, work);
            }
            else
            {
                return CoroutineRunner.StartCoroutine(work);
            }
        }
        public static Coroutine StartLuaCoroutine(LuaOnStackThread lthd)
        {
            return StartLuaCoroutineForBehav(null, lthd);
        }
        public static Coroutine StartLuaCoroutineForBehav(this MonoBehaviour behav, LuaOnStackThread lthd)
        {
            var work = EnumLuaCoroutine(lthd);
            if (behav != null)
            {
                return CoroutineRunner.StartSafeCoroutine(behav, work);
            }
            else
            {
                return CoroutineRunner.StartCoroutine(work);
            }
        }
        public static IEnumerator EnumLuaCoroutine(LuaOnStackFunc lfunc)
        {
            if (lfunc != null)
            {
                LuaThread lthd = new LuaThread(lfunc);
                return EnumLuaCoroutine(lthd);
            }
            else
            {
                return CoroutineRunner.GetEmptyEnumerator();
            }
        }

        public static IEnumerator EnumLuaCoroutine(LuaOnStackThread lthd)
        {
            if (lthd != null)
            {
                using (lthd)
                {
                    while (true)
                    {
#if UNITY_EDITOR
                        if (lthd.IsClosed)
                        {
                            yield break;
                        }
#endif
                        lthd.DoResume();
                        var l = lthd.L;
                        var rvc = l.gettop();
                        if (lthd.IsDone)
                        {
                            l.settop(0);
                            yield break;
                        }
                        else if (rvc > 0)
                        {
                            var result = l.GetLua(1);
                            if (result is UnityEngineEx.CoroutineRunner.CoroutineAbortedYieldable)
                            {
                                l.LogError("Current lua coroutine aborted!");
                                yield break;
                            }
                            if (rvc >= 2 && l.toboolean(2))
                            {
                                l.settop(0);
                                if (result is IEnumerator)
                                {
                                    var etor = result as IEnumerator;
                                    while (etor.MoveNext())
                                    {
                                        yield return etor.Current;
                                    }
                                }
                                else if (result is IEnumerable)
                                {
                                    var enumerable = result as IEnumerable;
                                    foreach (var obj in enumerable)
                                    {
                                        yield return obj;
                                    }
                                }
                                else
                                {
                                    yield return result;
                                }
                            }
                            else
                            {
                                l.settop(0);
                                yield return result;
                            }
                        }
                        else
                        {
                            yield return null;
                        }
                    }
                }
            }
        }
#endif

        public static string ExStackTraceLua(string existing_stack)
        {
            if (LuaHub.ForbidLuaStackTrace || !ThreadSafeValues.IsMainThread && LuaHub.RunningLuaState == IntPtr.Zero)
            {
                return null;
            }
            else if (existing_stack == null
                || existing_stack.Contains(".LuaCoreLib:")
                || existing_stack.Contains(".LuaAuxLib:")
                )
            {
                var l = LuaHub.RunningLuaState;
                if (l == IntPtr.Zero)
                {
                    l = GlobalLua.L.L;
                }
                LuaHub.PushLuaStackTrace(l);
                var lstack = l.tostring(-1);
                l.pop(1);
                return lstack;
            }
            else
            {
                return null;
            }
        }

        public static IEnumerator DeepGC()
        {
            for (int i = 0; i < 3; ++i)
            {
                if (!object.ReferenceEquals(L, null))
                {
                    L.L.gc(2, 0);
                    yield return null;
                }
            }
        }
        public static void DeepGCStep()
        {
            if (!object.ReferenceEquals(L, null))
            {
                L.L.gc(2, 0);
                L.L.GetOrCreateRefMan().DoPendingRecycle();
            }
        }

#if UNITY_EDITOR
        private class LuaDllPostprocessor : UnityEditor.AssetPostprocessor
        {
            public static bool LuaDllMissing = false;

            private static bool IsLuaDll(string path)
            {
                if (path != null)
                {
                    if (UnityEditor.AssetImporter.GetAtPath(path) is UnityEditor.PluginImporter)
                    {
                        var file = System.IO.Path.GetFileNameWithoutExtension(path);
                        if (file == "lua" || file == "liblua")
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            private static bool CheckLuaDll(string path)
            {
                if (IsLuaDll(path))
                {
                    if (LuaDllMissing)
                    {
                        UnityEditor.AssetDatabase.ImportAsset(ResManager.__ASSET__, UnityEditor.ImportAssetOptions.ForceUpdate);
                    }
                    return true;
                }
                return false;
            }
            private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                if (LuaDllMissing)
                {
                    if (importedAssets != null)
                    {
                        for (int i = 0; i < importedAssets.Length; ++i)
                        {
                            if (CheckLuaDll(importedAssets[i]))
                            {
                                return;
                            }
                        }
                    }
                    if (movedAssets != null)
                    {
                        for (int i = 0; i < movedAssets.Length; ++i)
                        {
                            if (CheckLuaDll(importedAssets[i]))
                            {
                                return;
                            }
                        }
                    }
                }
            }
        }
#endif
    }

#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
    public static class GlobalLuaEntry
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnUnityStart()
        {
            ResManager.GarbageCollector.GarbageCollectorEvents[0].Insert(0, GlobalLua.DeepGCStep);
#if UNITY_EDITOR
            ResManager.AddInitItem(ResManager.LifetimeOrders.Zero, GlobalLuaEditorCheck);
#endif
        }
#if UNITY_EDITOR
        private static void GlobalLuaEditorCheck()
        {
            GlobalLua.EditorCheckRunningState();
        }
#endif
    }
#endif
}
