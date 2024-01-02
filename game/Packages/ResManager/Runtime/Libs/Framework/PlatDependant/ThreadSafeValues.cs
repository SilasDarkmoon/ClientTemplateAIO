using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
using UnityEngine;

using uobj = UnityEngine.Object;
#endif

namespace UnityEngineEx
{
    public static class ThreadSafeValues
    {
        static ThreadSafeValues()
        {
#if UNITY_EDITOR
            if (SafeInitializerUtils.CheckShouldDelay()) return;
#endif
            Init();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        public static void Init()
        {
            _UpdatePath = IsolatedPrefs.GetUpdatePath();
            _IsolatedPath = IsolatedPrefs.GetIsolatedPath();
#if UNITY_IOS && !UNITY_EDITOR && (LOG_TO_DOCUMENT_FOLDER || DEVELOPMENT_BUILD || ALWAYS_SHOW_LOG || DEBUG)
            _LogPath = Application.persistentDataPath;
#else
            _LogPath = _IsolatedPath;
#endif
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
            _cached_Application_platform = Application.platform.ToString();
            _cached_Application_streamingAssetsPath = Application.streamingAssetsPath;
            _cached_Application_temporaryCachePath = Application.temporaryCachePath;
            _cached_Application_persistentDataPath = Application.persistentDataPath;
            _cached_Application_dataPath = Application.dataPath;
            _cached_AppVerName = Application.version;
            _cached_Mosid = IsolatedPrefs.IsolatedID;
            _UnityThreadID = ThreadLocalObj.GetThreadId();
#else
#if NETCOREAPP
            _cached_Application_platform = "DotNetCore";
#else
            _cached_Application_platform = "DotNet";
#endif
#if USE_CURRENT_FOLDER_AS_DATAPATH
            _cached_Application_streamingAssetsPath = "./streaming";
            _cached_Application_temporaryCachePath = "./cache";
            _cached_Application_persistentDataPath = "./runtime";
            _cached_Application_dataPath = ".";
#else
            _cached_Application_streamingAssetsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "streaming");
            _cached_Application_temporaryCachePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cache");
            _cached_Application_persistentDataPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "runtime");
            _cached_Application_dataPath = AppDomain.CurrentDomain.BaseDirectory;
#endif
            if (ResManager.IsInUnityFolder)
            {
                _cached_Application_streamingAssetsPath = ResManager.UnityRoot + "/Assets/StreamingAssets";
                _cached_Application_temporaryCachePath = ResManager.UnityRoot + "/EditorOutput/MosLuaStandalone/cache";
                _cached_Application_persistentDataPath = ResManager.UnityRoot + "/EditorOutput/MosLuaStandalone/runtime";
                _cached_Application_dataPath = ResManager.UnityRoot;
            }

            _cached_AppVerName = typeof(ThreadSafeValues).Assembly.GetName().Version?.ToString() ?? "0.0.0.0";
            _cached_Mosid = IsolatedPrefs.IsolatedID;
            _UnityThreadID = (ulong)System.Threading.Thread.CurrentThread.ManagedThreadId;
#endif
            _IsMainThread = true;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR && (DEVELOPMENT_BUILD || DEBUG || DEBUG_SHOW_PROCESS_ID_IN_WIN_TITLE)
            SetWindowTitle(System.Diagnostics.Process.GetCurrentProcess().Id.ToString() + "-" + _cached_Mosid);
#endif
        }

        private static string _UpdatePath;
        private static string _LogPath;
        private static string _IsolatedPath;
        private static string _cached_Application_platform;
        private static string _cached_Application_streamingAssetsPath;
        private static string _cached_Application_temporaryCachePath;
        private static string _cached_Application_persistentDataPath;
        private static string _cached_Application_dataPath;
        private static string _cached_AppVerName;
        private static string _cached_Mosid;
        private static ulong _UnityThreadID;
        [ThreadStatic] private static bool _IsMainThread;

        public static string UpdatePath { get { return _UpdatePath; } }
        public static string LogPath { get { return _LogPath; } }
        public static string IsolatedPath { get { return _IsolatedPath; } }
        public static string AppPlatform { get { return _cached_Application_platform; } }
        public static string AppStreamingAssetsPath { get { return _cached_Application_streamingAssetsPath; } }
        public static string AppTemporaryCachePath { get { return _cached_Application_temporaryCachePath; } }
        public static string AppPersistentDataPath { get { return _cached_Application_persistentDataPath; } }
        public static string AppDataPath { get { return _cached_Application_dataPath; } }
        public static string AppVerName { get { return _cached_AppVerName; } }
        public static string Mosid { get { return _cached_Mosid; } }
        public static ulong UnityThreadID { get { return _UnityThreadID; } }
        public static bool IsMainThread { get { return _IsMainThread; } }
        public static ulong ThreadId
        {
            get
            {
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
                return ThreadLocalObj.GetThreadId();
#else
                return (ulong)System.Threading.Thread.CurrentThread.ManagedThreadId;
#endif
            }
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
#region WIN32API
        delegate bool EnumWindowsCallBack(IntPtr hwnd, IntPtr lParam);
        [System.Runtime.InteropServices.DllImport("user32", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        static extern bool SetWindowTextW(IntPtr hwnd, string title);
        [System.Runtime.InteropServices.DllImport("user32")]
        static extern int EnumWindows(EnumWindowsCallBack lpEnumFunc, IntPtr lParam);
        [System.Runtime.InteropServices.DllImport("user32")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, ref IntPtr lpdwProcessId);
#endregion
        static IntPtr myWindowHandle;
        public static void SetWindowTitle(string title)
        {
            IntPtr handle = (IntPtr)System.Diagnostics.Process.GetCurrentProcess().Id;  //获取进程ID
            EnumWindows(new EnumWindowsCallBack(EnumWindCallback), handle);     //枚举查找本窗口
            SetWindowTextW(myWindowHandle, title); //设置窗口标题
        }
        [AOT.MonoPInvokeCallback(typeof(EnumWindowsCallBack))]
        static bool EnumWindCallback(IntPtr hwnd, IntPtr lParam)
        {
            IntPtr pid = IntPtr.Zero;
            GetWindowThreadProcessId(hwnd, ref pid);
            if (pid == lParam)  //判断当前窗口是否属于本进程
            {
                myWindowHandle = hwnd;
                return false;
            }
            return true;
        }
#endif
    }
}