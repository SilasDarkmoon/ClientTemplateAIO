using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices;

namespace UnityEngineEx.Native
{
    public static class NativeThreadLocal
    {
        public static bool Ready
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR || ENABLE_IL2CPP
                return true;
#elif UNITY_ANDROID && !UNITY_EDITOR
                if (!NativeImported._Linked)
                {
                    NativeImported._Linked = true;
                    try
                    {
                        System.Runtime.InteropServices.Marshal.PrelinkAll(typeof(NativeImported));
                        NativeImported._Ready = true;
                        return true;
                    }
                    catch { }
                }
                return NativeImported._Ready;
#else
                return false;
#endif
            }
        }

#if UNITY_IOS && !UNITY_EDITOR
        private static class NativeImported
        {
            internal delegate void Del_DisposeObj(IntPtr handle);
            [AOT.MonoPInvokeCallback(typeof(Del_DisposeObj))]
            internal static void DisposeObj(IntPtr handle)
            {
                if (handle != IntPtr.Zero)
                {
                    try
                    {
                        var gchandle = (GCHandle)handle;
                        gchandle.Free();
                    }
                    catch (Exception e)
                    {
                        PlatDependant.LogError(e);
                    }
                }
            }
            internal static readonly Del_DisposeObj Func_DisposeObj = new Del_DisposeObj(DisposeObj);

            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            internal static extern void RegThreadLocalContainer(IntPtr obj, Del_DisposeObj funcDispose);
            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr GetThreadLocalContainer();
            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr GetThreadID();
        }

        public static void SetContainer(object obj)
        {
            if (obj != null)
            {
                var handle = GCHandle.Alloc(obj);
                NativeImported.RegThreadLocalContainer((IntPtr)handle, NativeImported.Func_DisposeObj);
            }
        }
        public static T GetContainer<T>() where T : class
        {
            var handle = NativeImported.GetThreadLocalContainer();
            if (handle != IntPtr.Zero)
            {
                object obj = null;
                try
                {
                    obj = ((GCHandle)handle).Target;
                }
                catch (Exception e)
                {
                    PlatDependant.LogError(e);
                }
                return obj as T;
            }
            return null;
        }
        public static ulong GetThreadID()
        {
            return (ulong)NativeImported.GetThreadID();
        }
#elif UNITY_ANDROID && !UNITY_EDITOR
        private static class NativeImported
        {
            internal static bool _Linked = false;
            internal static bool _Ready = false;

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            internal delegate void Del_DisposeObj(IntPtr handle);
            [AOT.MonoPInvokeCallback(typeof(Del_DisposeObj))]
            internal static void DisposeObj(IntPtr handle)
            {
                if (handle != IntPtr.Zero)
                {
                    try
                    {
                        var gchandle = (GCHandle)handle;
                        gchandle.Free();
                    }
                    catch (Exception e)
                    {
                        PlatDependant.LogError(e);
                    }
                }
            }
            internal static readonly Del_DisposeObj Func_DisposeObj = new Del_DisposeObj(DisposeObj);

            [DllImport("NativeThreadLocal", CallingConvention = CallingConvention.Cdecl)]
            internal static extern void RegThreadLocalContainer(IntPtr obj, Del_DisposeObj funcDispose);
            [DllImport("NativeThreadLocal", CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr GetThreadLocalContainer();
            [DllImport("NativeThreadLocal", CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr GetThreadID();
        }

        public static void SetContainer(object obj)
        {
            if (obj != null)
            {
                var handle = GCHandle.Alloc(obj);
                NativeImported.RegThreadLocalContainer((IntPtr)handle, NativeImported.Func_DisposeObj);
            }
        }
        public static T GetContainer<T>() where T : class
        {
            var handle = NativeImported.GetThreadLocalContainer();
            if (handle != IntPtr.Zero)
            {
                object obj = null;
                try
                {
                    obj = ((GCHandle)handle).Target;
                }
                catch (Exception e)
                {
                    PlatDependant.LogError(e);
                }
                return obj as T;
            }
            return null;
        }
        public static ulong GetThreadID()
        {
            return (ulong)NativeImported.GetThreadID();
        }
#else
        public static void SetContainer(object obj) { }
        public static T GetContainer<T>() where T : class { return null; }
        public static ulong GetThreadID() { return 0; }
#endif

        private class NativeThreadLocalWrapper : ThreadLocalObj.INativeThreadLocal
        {
            public bool Ready { get { return NativeThreadLocal.Ready; } }

            public T GetContainer<T>() where T : class
            {
                return NativeThreadLocal.GetContainer<T>();
            }

            public ulong GetThreadID()
            {
                return NativeThreadLocal.GetThreadID();
            }

            public void SetContainer(object obj)
            {
                NativeThreadLocal.SetContainer(obj);
            }
        }
        [UnityEngine.RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnUnityStart()
        {
            ThreadLocalObj.NativeThreadLocalWrapper = new NativeThreadLocalWrapper();
        }
    }
}
