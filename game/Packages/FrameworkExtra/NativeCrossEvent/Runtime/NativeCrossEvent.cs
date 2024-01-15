using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices;

namespace UnityEngineEx.Native
{
    public static class NativeCrossEvents
    {
#if UNITY_IOS && !UNITY_EDITOR && !DUMMY_NATIVE_EVENTS
        private static class NativeImported
        {
            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            internal static extern void Init_CommonNativeEvents();
        }

        private static void Init()
        {
            NativeImported.Init_CommonNativeEvents();
        }

#elif UNITY_ANDROID && !UNITY_EDITOR && !DUMMY_NATIVE_EVENTS
        private static class NativeImported
        {
            [DllImport("CommonNativeEvents", CallingConvention = CallingConvention.Cdecl)]
            internal static extern void Init_CommonNativeEvents();
        }

        private static void Init()
        {
            NativeImported.Init_CommonNativeEvents();
            using (AndroidJavaClass jc = new AndroidJavaClass("host.silas.anative.android.commonnativeevents.CommonNativeEvents"))
            {
                jc.CallStatic("Init");
            }
        }

#else
        private static void Init() { }
#endif

        [UnityEngine.RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnUnityStart()
        {
            ResManager.AddInitItem(new ResManager.ActionInitItem(ResManager.LifetimeOrders.CrossEvent + 5, Init, null));
        }
    }
}