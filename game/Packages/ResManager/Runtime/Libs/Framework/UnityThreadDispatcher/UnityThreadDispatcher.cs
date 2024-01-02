using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using uobj = UnityEngine.Object;

namespace UnityEngineEx
{
    public static class UnityThreadDispatcher
    {
        public interface INativeUnityThreadDispatcher
        {
            bool Ready { get; }
            event Action HandleEventsInUnityThread;
            void TrigEventInUnityThread();
        }
        public static INativeUnityThreadDispatcher NativeUnityThreadDispatcherWrapper;

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Init()
        {
            CheckAndInit();
        }
        public static void RunInUnityThread(Action act)
        {
            AddEvent(act);
        }
        public static void RunInUnityThreadAndWait(Action act)
        {
            if (act != null)
            {
                if (ThreadSafeValues.IsMainThread)
                {
                    act();
                }
                else
                {
                    System.Threading.ManualResetEvent waithandle = new System.Threading.ManualResetEvent(false);
                    AddEvent(() =>
                    {
                        act();
                        waithandle.Set();
                    });
                    waithandle.WaitOne();
                    waithandle.Close();
                }
            }
        }
        public static T RunInUnityThreadAndWait<T>(Func<T> func)
        {
            if (func != null)
            {
                if (ThreadSafeValues.IsMainThread)
                {
                    return func();
                }
                else
                {
                    T rv = default(T);
                    System.Threading.ManualResetEvent waithandle = new System.Threading.ManualResetEvent(false);
                    AddEvent(() =>
                    {
                        rv = func();
                        waithandle.Set();
                    });
                    waithandle.WaitOne();
                    waithandle.Close();
                    return rv;
                }
            }
            return default(T);
        }

#pragma warning disable 0414
#if !NET_4_6 && !NET_STANDARD_2_0
        private static Unity.Collections.Concurrent.ConcurrentQueue<Action> ActionQueue = new Unity.Collections.Concurrent.ConcurrentQueue<Action>();
#else
        private static System.Collections.Concurrent.ConcurrentQueue<Action> ActionQueue = new System.Collections.Concurrent.ConcurrentQueue<Action>();
#endif
        private static bool _Inited = false;
        private static bool _UsingObjRunner = false;
        internal static GameObject _RunningObj = null;
        private static System.Threading.SynchronizationContext _MainThreadSyncContext;
#pragma warning restore

        private static void CheckAndInit()
        {
#if UNITY_2017_1_OR_NEWER
            try
            {
                _MainThreadSyncContext = System.Threading.SynchronizationContext.Current;
            }
            catch (Exception e)
            {
                PlatDependant.LogError(e);
            }
            if (_MainThreadSyncContext != null)
            {
                return;
            }
#endif
#if UNITY_EDITOR
            if (!_Inited)
            {
                _Inited = true;
                EditorBridge.WeakUpdate += HandleEvents;
            }
            return;
#else
#if MOD_NATIVEUNITYTHREADDISPATCHER
            if (NativeUnityThreadDispatcherWrapper != null && NativeUnityThreadDispatcherWrapper.Ready)
            {
                if (!_Inited)
                {
                    _Inited = true;
                    NativeUnityThreadDispatcherWrapper.HandleEventsInUnityThread += HandleEvents;
                }
                return;
            }
#endif
            _UsingObjRunner = true;
            if (!_RunningObj)
            {
                _RunningObj = new GameObject();
                _RunningObj.AddComponent<UnityThreadDispatcherBehav>();
                GameObject.DontDestroyOnLoad(_RunningObj);
                _RunningObj.hideFlags = HideFlags.HideAndDontSave;
            }
#endif
        }
        private static void AddEvent(Action act)
        {
            if (_MainThreadSyncContext != null)
            {
                if (act != null)
                {
                    if (ThreadSafeValues.IsMainThread)
                    {
                        try
                        {
                            act();
                        }
                        catch (Exception e)
                        {
                            PlatDependant.LogError(e);
                        }
                    }
                    else
                    {
                        _MainThreadSyncContext.Post(state =>
                        {
                            try
                            {
                                act();
                            }
                            catch (Exception e)
                            {
                                PlatDependant.LogError(e);
                            }
                        }, null);
                    }
                }
                return;
            }
            ActionQueue.Enqueue(act);
            if (ThreadSafeValues.IsMainThread)
            {
                HandleEvents();
                return;
            }
#if !UNITY_EDITOR
#if MOD_NATIVEUNITYTHREADDISPATCHER
            if (_Inited && !_UsingObjRunner)
            {
                NativeUnityThreadDispatcherWrapper.TrigEventInUnityThread();
            }
#endif
#endif
        }
        internal static void HandleEvents()
        {
            Action act = null;
            while (ActionQueue.TryDequeue(out act))
            {
                if (act != null)
                {
                    try
                    {
                        act();
                    }
                    catch (Exception e)
                    {
                        PlatDependant.LogError(e);
                    }
                }
            }
        }

        #region Action && Func Wrapper
        public static Action DropFuncReturn<R>(this Func<R> raw)
        {
            return () => raw();
        }
        public static Action<T> DropFuncReturn<T, R>(this Func<T, R> raw)
        {
            return (t) => raw(t);
        }
        public static Action<T1, T2> DropFuncReturn<T1, T2, R>(this Func<T1, T2, R> raw)
        {
            return (t1, t2) => raw(t1, t2);
        }
        public static Action<T1, T2, T3> DropFuncReturn<T1, T2, T3, R>(this Func<T1, T2, T3, R> raw)
        {
            return (t1, t2, t3) => raw(t1, t2, t3);
        }
        public static Action<T1, T2, T3, T4> DropFuncReturn<T1, T2, T3, T4, R>(this Func<T1, T2, T3, T4, R> raw)
        {
            return (t1, t2, t3, t4) => raw(t1, t2, t3, t4);
        }
        public static Action<T1, T2, T3, T4, T5> DropFuncReturn<T1, T2, T3, T4, T5, R>(this Func<T1, T2, T3, T4, T5, R> raw)
        {
            return (t1, t2, t3, t4, t5) => raw(t1, t2, t3, t4, t5);
        }
        public static Action<T1, T2, T3, T4, T5, T6> DropFuncReturn<T1, T2, T3, T4, T5, T6, R>(this Func<T1, T2, T3, T4, T5, T6, R> raw)
        {
            return (t1, t2, t3, t4, t5, t6) => raw(t1, t2, t3, t4, t5, t6);
        }
        public static Action<T1, T2, T3, T4, T5, T6, T7> DropFuncReturn<T1, T2, T3, T4, T5, T6, T7, R>(this Func<T1, T2, T3, T4, T5, T6, T7, R> raw)
        {
            return (t1, t2, t3, t4, t5, t6, t7) => raw(t1, t2, t3, t4, t5, t6, t7);
        }
        public static Action<T1, T2, T3, T4, T5, T6, T7, T8> DropFuncReturn<T1, T2, T3, T4, T5, T6, T7, T8, R>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, R> raw)
        {
            return (t1, t2, t3, t4, t5, t6, t7, t8) => raw(t1, t2, t3, t4, t5, t6, t7, t8);
        }
        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> DropFuncReturn<T1, T2, T3, T4, T5, T6, T7, T8, T9, R>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, R> raw)
        {
            return (t1, t2, t3, t4, t5, t6, t7, t8, t9) => raw(t1, t2, t3, t4, t5, t6, t7, t8, t9);
        }
        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> DropFuncReturn<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, R>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, R> raw)
        {
            return (t1, t2, t3, t4, t5, t6, t7, t8, t9, t10) => raw(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);
        }
        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> DropFuncReturn<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, R>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, R> raw)
        {
            return (t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11) => raw(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11);
        }
        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> DropFuncReturn<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, R>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, R> raw)
        {
            return (t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12) => raw(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12);
        }
        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> DropFuncReturn<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, R>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, R> raw)
        {
            return (t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13) => raw(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13);
        }
        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> DropFuncReturn<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, R>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, R> raw)
        {
            return (t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14) => raw(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14);
        }
        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> DropFuncReturn<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, R>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, R> raw)
        {
            return (t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15) => raw(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15);
        }
        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> DropFuncReturn<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, R>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, R> raw)
        {
            return (t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16) => raw(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16);
        }

        public static OutDel ConvertDel<InDel, OutDel>(this InDel in_del) where InDel : Delegate where OutDel : Delegate
        {
            return (OutDel)Delegate.CreateDelegate(typeof(OutDel), in_del.Target, in_del.Method);
        }

        public static Action UnityThreadAction(this Action raw)
        {
            return () => RunInUnityThread(raw);
        }
        public static Action<T> UnityThreadAction<T>(this Action<T> raw)
        {
            return (t) => RunInUnityThread(() => raw(t));
        }
        public static Action<T1, T2> UnityThreadAction<T1, T2>(this Action<T1, T2> raw)
        {
            return (t1, t2) => RunInUnityThread(() => raw(t1, t2));
        }
        public static Action<T1, T2, T3> UnityThreadAction<T1, T2, T3>(this Action<T1, T2, T3> raw)
        {
            return (t1, t2, t3) => RunInUnityThread(() => raw(t1, t2, t3));
        }
        public static Action<T1, T2, T3, T4> UnityThreadAction<T1, T2, T3, T4>(this Action<T1, T2, T3, T4> raw)
        {
            return (t1, t2, t3, t4) => RunInUnityThread(() => raw(t1, t2, t3, t4));
        }
        public static Action<T1, T2, T3, T4, T5> UnityThreadAction<T1, T2, T3, T4, T5>(this Action<T1, T2, T3, T4, T5> raw)
        {
            return (t1, t2, t3, t4, t5) => RunInUnityThread(() => raw(t1, t2, t3, t4, t5));
        }
        public static Action<T1, T2, T3, T4, T5, T6> UnityThreadAction<T1, T2, T3, T4, T5, T6>(this Action<T1, T2, T3, T4, T5, T6> raw)
        {
            return (t1, t2, t3, t4, t5, t6) => RunInUnityThread(() => raw(t1, t2, t3, t4, t5, t6));
        }
        public static Action<T1, T2, T3, T4, T5, T6, T7> UnityThreadAction<T1, T2, T3, T4, T5, T6, T7>(this Action<T1, T2, T3, T4, T5, T6, T7> raw)
        {
            return (t1, t2, t3, t4, t5, t6, t7) => RunInUnityThread(() => raw(t1, t2, t3, t4, t5, t6, t7));
        }
        public static Action<T1, T2, T3, T4, T5, T6, T7, T8> UnityThreadAction<T1, T2, T3, T4, T5, T6, T7, T8>(this Action<T1, T2, T3, T4, T5, T6, T7, T8> raw)
        {
            return (t1, t2, t3, t4, t5, t6, t7, t8) => RunInUnityThread(() => raw(t1, t2, t3, t4, t5, t6, t7, t8));
        }
        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> UnityThreadAction<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> raw)
        {
            return (t1, t2, t3, t4, t5, t6, t7, t8, t9) => RunInUnityThread(() => raw(t1, t2, t3, t4, t5, t6, t7, t8, t9));
        }
        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> UnityThreadAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> raw)
        {
            return (t1, t2, t3, t4, t5, t6, t7, t8, t9, t10) => RunInUnityThread(() => raw(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10));
        }
        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> UnityThreadAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> raw)
        {
            return (t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11) => RunInUnityThread(() => raw(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11));
        }
        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> UnityThreadAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> raw)
        {
            return (t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12) => RunInUnityThread(() => raw(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12));
        }
        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> UnityThreadAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> raw)
        {
            return (t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13) => RunInUnityThread(() => raw(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13));
        }
        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> UnityThreadAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> raw)
        {
            return (t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14) => RunInUnityThread(() => raw(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14));
        }
        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> UnityThreadAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> raw)
        {
            return (t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15) => RunInUnityThread(() => raw(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15));
        }
        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> UnityThreadAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> raw)
        {
            return (t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16) => RunInUnityThread(() => raw(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16));
        }

        public static Action UnityThreadWaitAction(this Action raw)
        {
            return () => RunInUnityThreadAndWait(raw);
        }
        public static Action<T> UnityThreadWaitAction<T>(this Action<T> raw)
        {
            return (t) => RunInUnityThreadAndWait(() => raw(t));
        }
        public static Action<T1, T2> UnityThreadWaitAction<T1, T2>(this Action<T1, T2> raw)
        {
            return (t1, t2) => RunInUnityThreadAndWait(() => raw(t1, t2));
        }
        public static Action<T1, T2, T3> UnityThreadWaitAction<T1, T2, T3>(this Action<T1, T2, T3> raw)
        {
            return (t1, t2, t3) => RunInUnityThreadAndWait(() => raw(t1, t2, t3));
        }
        public static Action<T1, T2, T3, T4> UnityThreadWaitAction<T1, T2, T3, T4>(this Action<T1, T2, T3, T4> raw)
        {
            return (t1, t2, t3, t4) => RunInUnityThreadAndWait(() => raw(t1, t2, t3, t4));
        }
        public static Action<T1, T2, T3, T4, T5> UnityThreadWaitAction<T1, T2, T3, T4, T5>(this Action<T1, T2, T3, T4, T5> raw)
        {
            return (t1, t2, t3, t4, t5) => RunInUnityThreadAndWait(() => raw(t1, t2, t3, t4, t5));
        }
        public static Action<T1, T2, T3, T4, T5, T6> UnityThreadWaitAction<T1, T2, T3, T4, T5, T6>(this Action<T1, T2, T3, T4, T5, T6> raw)
        {
            return (t1, t2, t3, t4, t5, t6) => RunInUnityThreadAndWait(() => raw(t1, t2, t3, t4, t5, t6));
        }
        public static Action<T1, T2, T3, T4, T5, T6, T7> UnityThreadWaitAction<T1, T2, T3, T4, T5, T6, T7>(this Action<T1, T2, T3, T4, T5, T6, T7> raw)
        {
            return (t1, t2, t3, t4, t5, t6, t7) => RunInUnityThreadAndWait(() => raw(t1, t2, t3, t4, t5, t6, t7));
        }
        public static Action<T1, T2, T3, T4, T5, T6, T7, T8> UnityThreadWaitAction<T1, T2, T3, T4, T5, T6, T7, T8>(this Action<T1, T2, T3, T4, T5, T6, T7, T8> raw)
        {
            return (t1, t2, t3, t4, t5, t6, t7, t8) => RunInUnityThreadAndWait(() => raw(t1, t2, t3, t4, t5, t6, t7, t8));
        }
        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> UnityThreadWaitAction<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> raw)
        {
            return (t1, t2, t3, t4, t5, t6, t7, t8, t9) => RunInUnityThreadAndWait(() => raw(t1, t2, t3, t4, t5, t6, t7, t8, t9));
        }
        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> UnityThreadWaitAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> raw)
        {
            return (t1, t2, t3, t4, t5, t6, t7, t8, t9, t10) => RunInUnityThreadAndWait(() => raw(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10));
        }
        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> UnityThreadWaitAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> raw)
        {
            return (t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11) => RunInUnityThreadAndWait(() => raw(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11));
        }
        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> UnityThreadWaitAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> raw)
        {
            return (t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12) => RunInUnityThreadAndWait(() => raw(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12));
        }
        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> UnityThreadWaitAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> raw)
        {
            return (t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13) => RunInUnityThreadAndWait(() => raw(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13));
        }
        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> UnityThreadWaitAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> raw)
        {
            return (t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14) => RunInUnityThreadAndWait(() => raw(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14));
        }
        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> UnityThreadWaitAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> raw)
        {
            return (t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15) => RunInUnityThreadAndWait(() => raw(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15));
        }
        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> UnityThreadWaitAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> raw)
        {
            return (t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16) => RunInUnityThreadAndWait(() => raw(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16));
        }

        public static Func<R> UnityThreadWaitFunc<R>(this Func<R> raw)
        {
            return () => RunInUnityThreadAndWait<R>(raw);
        }
        public static Func<T, R> UnityThreadWaitFunc<T, R>(this Func<T, R> raw)
        {
            return (t) => RunInUnityThreadAndWait<R>(() => raw(t));
        }
        public static Func<T1, T2, R> UnityThreadWaitFunc<T1, T2, R>(this Func<T1, T2, R> raw)
        {
            return (t1, t2) => RunInUnityThreadAndWait<R>(() => raw(t1, t2));
        }
        public static Func<T1, T2, T3, R> UnityThreadWaitFunc<T1, T2, T3, R>(this Func<T1, T2, T3, R> raw)
        {
            return (t1, t2, t3) => RunInUnityThreadAndWait<R>(() => raw(t1, t2, t3));
        }
        public static Func<T1, T2, T3, T4, R> UnityThreadWaitFunc<T1, T2, T3, T4, R>(this Func<T1, T2, T3, T4, R> raw)
        {
            return (t1, t2, t3, t4) => RunInUnityThreadAndWait<R>(() => raw(t1, t2, t3, t4));
        }
        public static Func<T1, T2, T3, T4, T5, R> UnityThreadWaitFunc<T1, T2, T3, T4, T5, R>(this Func<T1, T2, T3, T4, T5, R> raw)
        {
            return (t1, t2, t3, t4, t5) => RunInUnityThreadAndWait<R>(() => raw(t1, t2, t3, t4, t5));
        }
        public static Func<T1, T2, T3, T4, T5, T6, R> UnityThreadWaitFunc<T1, T2, T3, T4, T5, T6, R>(this Func<T1, T2, T3, T4, T5, T6, R> raw)
        {
            return (t1, t2, t3, t4, t5, t6) => RunInUnityThreadAndWait<R>(() => raw(t1, t2, t3, t4, t5, t6));
        }
        public static Func<T1, T2, T3, T4, T5, T6, T7, R> UnityThreadWaitFunc<T1, T2, T3, T4, T5, T6, T7, R>(this Func<T1, T2, T3, T4, T5, T6, T7, R> raw)
        {
            return (t1, t2, t3, t4, t5, t6, t7) => RunInUnityThreadAndWait<R>(() => raw(t1, t2, t3, t4, t5, t6, t7));
        }
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, R> UnityThreadWaitFunc<T1, T2, T3, T4, T5, T6, T7, T8, R>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, R> raw)
        {
            return (t1, t2, t3, t4, t5, t6, t7, t8) => RunInUnityThreadAndWait<R>(() => raw(t1, t2, t3, t4, t5, t6, t7, t8));
        }
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, R> UnityThreadWaitFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, R>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, R> raw)
        {
            return (t1, t2, t3, t4, t5, t6, t7, t8, t9) => RunInUnityThreadAndWait<R>(() => raw(t1, t2, t3, t4, t5, t6, t7, t8, t9));
        }
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, R> UnityThreadWaitFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, R>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, R> raw)
        {
            return (t1, t2, t3, t4, t5, t6, t7, t8, t9, t10) => RunInUnityThreadAndWait<R>(() => raw(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10));
        }
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, R> UnityThreadWaitFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, R>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, R> raw)
        {
            return (t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11) => RunInUnityThreadAndWait<R>(() => raw(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11));
        }
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, R> UnityThreadWaitFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, R>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, R> raw)
        {
            return (t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12) => RunInUnityThreadAndWait<R>(() => raw(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12));
        }
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, R> UnityThreadWaitFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, R>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, R> raw)
        {
            return (t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13) => RunInUnityThreadAndWait<R>(() => raw(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13));
        }
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, R> UnityThreadWaitFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, R>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, R> raw)
        {
            return (t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14) => RunInUnityThreadAndWait<R>(() => raw(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14));
        }
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, R> UnityThreadWaitFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, R>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, R> raw)
        {
            return (t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15) => RunInUnityThreadAndWait<R>(() => raw(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15));
        }
        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, R> UnityThreadWaitFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, R>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, R> raw)
        {
            return (t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16) => RunInUnityThreadAndWait<R>(() => raw(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16));
        }
        #endregion
    }
}