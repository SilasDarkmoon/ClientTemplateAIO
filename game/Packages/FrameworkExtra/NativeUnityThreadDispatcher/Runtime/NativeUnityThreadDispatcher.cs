using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices;

namespace UnityEngineEx.Native
{
    public static class NativeUnityThreadDispatcher
    {
        public static bool Ready
        {
            get
            {
#if (UNITY_IOS || UNITY_ANDROID && ENABLE_IL2CPP) && !UNITY_EDITOR && ENABLE_NATIVEUNITYTHREADDISPATCHER
                if (!NativeImported._Linked)
                {
                    NativeImported._Linked = true;
                    if (NativeImported.IsDispatcherReady())
                    {
                        NativeImported.RegDispatcherEventHandler(NativeImported.Func_HandleEvents);
                        NativeImported._Ready = true;
                        return true;
                    }
                }
                return NativeImported._Ready;
#elif UNITY_ANDROID && !UNITY_EDITOR && ENABLE_NATIVEUNITYTHREADDISPATCHER
                if (!NativeImported._Linked)
                {
                    NativeImported._Linked = true;
                    try
                    {
                        System.Runtime.InteropServices.Marshal.PrelinkAll(typeof(NativeImported));
                        if (NativeImported.IsDispatcherReady())
                        {
                            NativeImported.RegDispatcherEventHandler(NativeImported.Func_HandleEvents);
                            NativeImported._Ready = true;
                            return true;
                        }
                    }
                    catch { }
                }
                return NativeImported._Ready;
#else
                return false;
#endif
            }
        }

#pragma warning disable 0067
        public static event Action HandleEventsInUnityThread = () => { };
#pragma warning restore

#if UNITY_IOS && !UNITY_EDITOR && ENABLE_NATIVEUNITYTHREADDISPATCHER
        private static class NativeImported
        {
            internal static bool _Linked = false;
            internal static bool _Ready = false;

            internal delegate void Del_HandleEvents();
            [AOT.MonoPInvokeCallback(typeof(Del_HandleEvents))]
            internal static void HandleEvents()
            {
                HandleEventsInUnityThread();
            }
            internal static readonly Del_HandleEvents Func_HandleEvents = new Del_HandleEvents(HandleEvents);

            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            internal static extern void RegDispatcherEventHandler(Del_HandleEvents handler);
            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            internal static extern void TrigDispatcherEvent();
            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            internal static extern bool IsDispatcherReady();
        }

        public static void TrigEventInUnityThread()
        {
            NativeImported.TrigDispatcherEvent();
        }
#elif UNITY_ANDROID && !UNITY_EDITOR && ENABLE_NATIVEUNITYTHREADDISPATCHER
        private static class NativeImported
        {
            internal static bool _Linked = false;
            internal static bool _Ready = false;

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            internal delegate void Del_HandleEvents();
            [AOT.MonoPInvokeCallback(typeof(Del_HandleEvents))]
            internal static void HandleEvents()
            {
                HandleEventsInUnityThread();
            }
            internal static readonly Del_HandleEvents Func_HandleEvents = new Del_HandleEvents(HandleEvents);

            [DllImport("UnityThreadDispatcher", CallingConvention = CallingConvention.Cdecl)]
            internal static extern void RegDispatcherEventHandler(Del_HandleEvents handler);
            [DllImport("UnityThreadDispatcher", CallingConvention = CallingConvention.Cdecl)]
            internal static extern void TrigDispatcherEvent();
            [DllImport("UnityThreadDispatcher", CallingConvention = CallingConvention.Cdecl)]
            internal static extern bool IsDispatcherReady();
        }

        public static void TrigEventInUnityThread()
        {
            NativeImported.TrigDispatcherEvent();
        }
#else
        public static void TrigEventInUnityThread() { }
#endif

        private class NativeUnityThreadDispatcherWrapper : UnityThreadDispatcher.INativeUnityThreadDispatcher
        {
            public bool Ready { get { return NativeUnityThreadDispatcher.Ready; } }

            public event Action HandleEventsInUnityThread
            {
                add { NativeUnityThreadDispatcher.HandleEventsInUnityThread += value; }
                remove { NativeUnityThreadDispatcher.HandleEventsInUnityThread -= value; }
            }

            public void TrigEventInUnityThread()
            {
                NativeUnityThreadDispatcher.TrigEventInUnityThread();
            }
        }
        [UnityEngine.RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnUnityStart()
        {
            UnityThreadDispatcher.NativeUnityThreadDispatcherWrapper = new NativeUnityThreadDispatcherWrapper();
        }
    }
}
