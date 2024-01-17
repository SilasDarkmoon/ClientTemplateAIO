using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices;

namespace UnityEngineEx.Native
{
    public static class SDKEntry
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        private static void Init()
        {
            PlatDependant.LogError("Android SDK Entry Init!!!!");
            using (AndroidJavaClass jc = new AndroidJavaClass("host.silas.anative.android.sdkplugin.SDKPlugin"))
            {
                jc.CallStatic("Init");
            }
        }
#elif UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
        private static extern void InitEventsForSubSDKs();
        private static void Init()
        {
            InitEventsForSubSDKs();
        }
#else
        private static void Init() { }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnUnityStart()
        {
            ResManager.AddInitItem(new ResManager.ActionInitItem(ResManager.LifetimeOrders.CrossEvent + 7, Init, null));
        }
    }
}
