using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
using UnityEngine;

using Object = UnityEngine.Object;
#endif

namespace UnityEngineEx
{
    public static class IsolatedPrefs
    {
        private static void LogError(object message)
        {
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
            Debug.LogError(message);
#else
            Console.WriteLine(message);
#endif
        }

#if UNITY_STANDALONE && !UNITY_EDITOR || !UNITY_ENGINE && !UNITY_5_3_OR_NEWER
#if NET_4_6 || !UNITY_ENGINE && !UNITY_5_3_OR_NEWER
        private class IsolatedIDFileHolder
        {
            private int _InstanceID = 0;
            public int InstanceID { get { return _InstanceID; } }

            private System.Threading.Mutex _IsolatedIDMutex = new System.Threading.Mutex(false, "IsolatedIDMutex");
            private System.IO.FileStream _IsolatedIDHolder;

            public IsolatedIDFileHolder()
            {
                _IsolatedIDMutex.WaitOne();
                try
                {
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
                    var file = Application.persistentDataPath + "/iid.txt";
                    var fileh = Application.persistentDataPath + "/iidh.txt";
#else
#if USE_CURRENT_FOLDER_AS_DATAPATH
                    var file = "./runtime/iid.txt";
                    var fileh = "./runtime/iidh.txt";
#else
                    var file = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "runtime/iid.txt");
                    var fileh = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "runtime/iidh.txt");
#endif
                    if (ResManager.IsInUnityFolder)
                    {
                        file = ResManager.UnityRoot + "/EditorOutput/MosLuaStandalone/runtime/iid.txt";
                        fileh = ResManager.UnityRoot + "/EditorOutput/MosLuaStandalone/runtime/iidh.txt";
                    }
#endif
                    if (PlatDependant.IsFileExist(fileh))
                    {
                        bool shouldDeleteFile = true;
                        try
                        {
                            var hstream = System.IO.File.Open(fileh, System.IO.FileMode.Open, System.IO.FileAccess.Write, System.IO.FileShare.Read);
                            if (hstream == null)
                            {
                                shouldDeleteFile = false;
                            }
                            else
                            {
                                hstream.Dispose();
                            }
                        }
                        catch (Exception)
                        {
                            shouldDeleteFile = false;
                        }
                        if (shouldDeleteFile)
                        {
                            PlatDependant.DeleteFile(fileh);
                            PlatDependant.DeleteFile(file);
                        }
                    }
                    if (!PlatDependant.IsFileExist(fileh))
                    {
                        using (var sw = PlatDependant.OpenWriteText(fileh))
                        {
                            sw.Write(" ");
                        }
                    }
                    _IsolatedIDHolder = System.IO.File.Open(fileh, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read);
                    if (PlatDependant.IsFileExist(file))
                    {
                        try
                        {
                            using (var sr = PlatDependant.OpenReadText(file))
                            {
                                var index = sr.ReadLine();
                                int.TryParse(index, out _InstanceID);
                            }
                        }
                        catch (Exception e)
                        {
                            LogError(e);
                        }
                    }
                    using (var sw = PlatDependant.OpenWriteText(file))
                    {
                        sw.Write(_InstanceID + 1);
                    }
                }
                catch (Exception e)
                {
                    LogError(e);
                }
                finally
                {
                    _IsolatedIDMutex.ReleaseMutex();
                }

                PlatDependant.PreQuitting += Close;
            }

            private void Close()
            {
                _IsolatedIDMutex.WaitOne();
                try
                {
                    if (_IsolatedIDHolder != null)
                    {
                        _IsolatedIDHolder.Dispose();
                        _IsolatedIDHolder = null;
                    }
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
                    var file = Application.persistentDataPath + "/iid.txt";
#else
#if USE_CURRENT_FOLDER_AS_DATAPATH
                    var file = "./runtime/iid.txt";
#else
                    var file = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "runtime/iid.txt");
#endif
                    if (ResManager.IsInUnityFolder)
                    {
                        file = ResManager.UnityRoot + "/EditorOutput/MosLuaStandalone/runtime/iid.txt";
                    }
#endif
                    int instanceid = 0;
                    if (PlatDependant.IsFileExist(file))
                    {
                        try
                        {
                            using (var sr = PlatDependant.OpenReadText(file))
                            {
                                var index = sr.ReadLine();
                                int.TryParse(index, out instanceid);
                            }
                        }
                        catch (Exception e)
                        {
                            LogError(e);
                        }
                    }
                    if (instanceid <= 1)
                    {
                        PlatDependant.DeleteFile(file);
                    }
                    else
                    {
                        using (var sw = PlatDependant.OpenWriteText(file))
                        {
                            sw.Write(instanceid - 1);
                        }
                    }
                }
                catch (Exception e)
                {
                    LogError(e);
                }
                finally
                {
                    _IsolatedIDMutex.ReleaseMutex();
                }
            }
        }
        private static IsolatedIDFileHolder _InstanceHolder = new IsolatedIDFileHolder();
#else
        private class IsolatedIDFileHolder
        {
            private int _InstanceID = 0;
            public int InstanceID { get { return _InstanceID; } }

            private System.IO.FileStream _IsolatedIDHolder;

            public IsolatedIDFileHolder()
            {
                var file = Application.persistentDataPath + "/iid.data";
                System.IO.FileStream sfile = null;
                while (sfile == null)
                {
                    try
                    {
                        if (!System.IO.Directory.Exists(Application.persistentDataPath))
                        {
                            System.IO.Directory.CreateDirectory(Application.persistentDataPath);
                        }
                        sfile = System.IO.File.Open(file, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite, System.IO.FileShare.None);
                    }
                    catch { }
                    if (sfile == null)
                    {
                        System.Threading.Thread.Sleep(1);
                    }
                }
                try
                {
                    var fileh = Application.persistentDataPath + "/iidh.txt";
                    if (PlatDependant.IsFileExist(fileh))
                    {
                        bool shouldDeleteFile = true;
                        try
                        {
                            var hstream = System.IO.File.Open(fileh, System.IO.FileMode.Open, System.IO.FileAccess.Write, System.IO.FileShare.Read);
                            if (hstream == null)
                            {
                                shouldDeleteFile = false;
                            }
                            else
                            {
                                hstream.Dispose();
                            }
                        }
                        catch (Exception)
                        {
                            shouldDeleteFile = false;
                        }
                        if (shouldDeleteFile)
                        {
                            PlatDependant.DeleteFile(fileh);
                            sfile.Seek(0, System.IO.SeekOrigin.Begin);
                            sfile.SetLength(0);
                        }
                    }
                    if (!PlatDependant.IsFileExist(fileh))
                    {
                        using (var sw = PlatDependant.OpenWriteText(fileh))
                        {
                            sw.Write(" ");
                        }
                    }
                    _IsolatedIDHolder = System.IO.File.Open(fileh, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read);
                    if (sfile.Length >= 4)
                    {
                        sfile.Seek(0, System.IO.SeekOrigin.Begin);
                        using (var br = new System.IO.BinaryReader(sfile, System.Text.Encoding.UTF8, true))
                        {
                            _InstanceID = br.ReadInt32();
                        }
                    }
                    sfile.Seek(0, System.IO.SeekOrigin.Begin);
                    sfile.SetLength(0);
                    using (var bw = new System.IO.BinaryWriter(sfile, System.Text.Encoding.UTF8, true))
                    {
                        bw.Write(_InstanceID + 1);
                    }
                }
                catch (Exception e)
                {
                    LogError(e);
                }
                finally
                {
                    sfile.Dispose();
                }

                PlatDependant.PreQuitting += Close;
            }

            private void Close()
            {
                var file = Application.persistentDataPath + "/iid.data";
                System.IO.FileStream sfile = null;
                while (sfile == null)
                {
                    try
                    {
                        if (!System.IO.Directory.Exists(Application.persistentDataPath))
                        {
                            System.IO.Directory.CreateDirectory(Application.persistentDataPath);
                        }
                        sfile = System.IO.File.Open(file, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite, System.IO.FileShare.None);
                    }
                    catch { }
                    if (sfile == null)
                    {
                        System.Threading.Thread.Sleep(1);
                    }
                }
                try
                {
                    if (_IsolatedIDHolder != null)
                    {
                        _IsolatedIDHolder.Dispose();
                        _IsolatedIDHolder = null;
                    }
                    int instanceid = 0;
                    if (sfile.Length >= 4)
                    {
                        sfile.Seek(0, System.IO.SeekOrigin.Begin);
                        using (var br = new System.IO.BinaryReader(sfile, System.Text.Encoding.UTF8, true))
                        {
                            instanceid = br.ReadInt32();
                        }
                    }
                    sfile.Seek(0, System.IO.SeekOrigin.Begin);
                    sfile.SetLength(0);
                    if (instanceid > 1)
                    {
                        using (var bw = new System.IO.BinaryWriter(sfile, System.Text.Encoding.UTF8, true))
                        {
                            bw.Write(instanceid - 1);
                        }
                    }
                }
                catch (Exception e)
                {
                    LogError(e);
                }
                finally
                {
                    sfile.Dispose();
                }
            }
        }
        private static IsolatedIDFileHolder _InstanceHolder = new IsolatedIDFileHolder();
#endif
#endif

        private static string _InstallID;
        private static string LoadInstallID()
        {
#if UNITY_EDITOR || !UNITY_ENGINE && !UNITY_5_3_OR_NEWER
            string mosid = null;
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
            string mosidfile = "EditorOutput/Runtime/mosid.txt";
#else
#if USE_CURRENT_FOLDER_AS_DATAPATH
            string mosidfile = "./runtime/mosid.txt";
#else
            string mosidfile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "runtime/mosid.txt");
#endif
            if (ResManager.IsInUnityFolder)
            {
                mosidfile = ResManager.UnityRoot + "/EditorOutput/Runtime/mosid.txt";
            }
#endif
            if (PlatDependant.IsFileExist(mosidfile))
            {
                try
                {
                    using (var sr = PlatDependant.OpenReadText(mosidfile))
                    {
                        mosid = sr.ReadLine().Trim();
                    }
                }
                catch (Exception e)
                {
                    LogError(e);
                }
            }
            if (string.IsNullOrEmpty(mosid))
            {
                mosid = Guid.NewGuid().ToString("N");
                try
                {
                    using (var sw = PlatDependant.OpenWriteText(mosidfile))
                    {
                        sw.WriteLine(mosid);
                    }
                }
                catch (Exception e)
                {
                    LogError(e);
                }
            }
            return mosid;
#else
            string mosid = null;
            if (PlayerPrefs.HasKey("___Pref__MosID"))
            {
                mosid = PlayerPrefs.GetString("___Pref__MosID");
            }
            if (string.IsNullOrEmpty(mosid))
            {
                mosid = Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString("___Pref__MosID", mosid);
                PlayerPrefs.Save();
            }
            return mosid;
#endif
        }
        public static void ReloadInstallID()
        {
            _InstallID = LoadInstallID();
        }
        public static string InstallID
        {
            get
            {
                if (_InstallID == null)
                {
                    ReloadInstallID();
                }
                return _InstallID;
            }
        }
        private static string _IsolatedID;
        private static string LoadIsolatedID()
        {
#if UNITY_EDITOR
            return InstallID;
#elif UNITY_STANDALONE || !UNITY_ENGINE && !UNITY_5_3_OR_NEWER
            if (_InstanceHolder.InstanceID == 0)
            {
                return InstallID;
            }
            else
            {
                return string.Format("{0}-{1}-", InstallID, _InstanceHolder.InstanceID);
            }
#else
            return InstallID;
#endif
        }
        public static void ReloadIsolatedID()
        {
            ReloadInstallID();
            _IsolatedID = LoadIsolatedID();
        }
        public static string IsolatedID
        {
            get
            {
                if (_InstallID == null || _IsolatedID == null)
                {
                    ReloadIsolatedID();
                }
                return _IsolatedID;
            }
        }

        public static int InstanceID
        {
            get
            {
#if UNITY_STANDALONE && !UNITY_EDITOR || !UNITY_ENGINE && !UNITY_5_3_OR_NEWER
                return _InstanceHolder.InstanceID;
#else
                return 0;
#endif
            }
        }

        public static string GetIsolatedPath()
        {
#if UNITY_EDITOR
            return "EditorOutput/Runtime";
#elif UNITY_STANDALONE
#if UNITY_STANDALONE_WIN
            if (_InstanceHolder.InstanceID == 0)
            {
                return UnityEngine.Application.dataPath + "/runtime";
            }
            else
            {
                return UnityEngine.Application.dataPath + "/runtime/instance" + _InstanceHolder.InstanceID.ToString();
            }
#else
            if (_InstanceHolder.InstanceID == 0)
            {
                return UnityEngine.Application.temporaryCachePath;
            }
            else
            {
                return UnityEngine.Application.temporaryCachePath + "/instance" + _InstanceHolder.InstanceID.ToString();
            }
#endif
#elif UNITY_ANDROID
            return UnityEngine.Application.persistentDataPath;
#elif UNITY_ENGINE || UNITY_5_3_OR_NEWER
            return UnityEngine.Application.temporaryCachePath;
#else
            if (_InstanceHolder.InstanceID == 0)
            {
                if (ResManager.IsInUnityFolder)
                {
                    return ResManager.UnityRoot + "/EditorOutput/MosLuaStandalone/instance0";
                }
#if USE_CURRENT_FOLDER_AS_DATAPATH
                return "./cache";
#else
                return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cache");
#endif
            }
            else
            {
                if (ResManager.IsInUnityFolder)
                {
                    return ResManager.UnityRoot + "/EditorOutput/MosLuaStandalone/instance" + _InstanceHolder.InstanceID.ToString();
                }
#if USE_CURRENT_FOLDER_AS_DATAPATH
                return "./cache/instance" + _InstanceHolder.InstanceID.ToString();
#else
                return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cache/instance" + _InstanceHolder.InstanceID.ToString());
#endif
            }
#endif
        }

        public static bool CONFIG__UPDATE_TO_DOC_FOLDER = true;
        public static string GetUpdatePath()
        {
#if UNITY_EDITOR
            return "EditorOutput/Runtime";
#elif UNITY_STANDALONE
            return UnityEngine.Application.temporaryCachePath;
#elif UNITY_ANDROID
            return UnityEngine.Application.persistentDataPath;
#elif UNITY_IOS
            if (PlayerPrefs.GetInt("___CONFIG__UPDATE_TO_DOC_FOLDER", 0) != 0 || CONFIG__UPDATE_TO_DOC_FOLDER)
            {
                return UnityEngine.Application.persistentDataPath;
            }
            else
            {
                return UnityEngine.Application.temporaryCachePath;
            }
#elif UNITY_ENGINE || UNITY_5_3_OR_NEWER
            return UnityEngine.Application.temporaryCachePath;
#else
            if (ResManager.IsInUnityFolder)
            {
                return ResManager.UnityRoot + "/EditorOutput/Runtime";
            }
#if USE_CURRENT_FOLDER_AS_DATAPATH
            return "./cache";
#else
            return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cache");
#endif
#endif
        }

#if UNITY_EDITOR || UNITY_STANDALONE
        private static DataDictionary _Dict = new DataDictionary();
        static IsolatedPrefs()
        {
            string json = null;
            string file = GetIsolatedPath() + "/iprefs.txt";
            if (PlatDependant.IsFileExist(file))
            {
                try
                {
                    using (var sr = PlatDependant.OpenReadText(file))
                    {
                        json = sr.ReadToEnd();
                    }
                    if (!string.IsNullOrEmpty(json))
                    {
                        JsonUtility.FromJsonOverwrite(json, _Dict);
                    }
                }
                catch (Exception e)
                {
                    LogError(e);
                }
            }
        }

        public static void DeleteAll()
        {
            _Dict.Clear();
        }
        public static void DeleteKey(string key)
        {
            _Dict.Remove(key);
        }
        public static double GetNumber(string key)
        {
            object val;
            if (_Dict.TryGetValue(key, out val))
            {
                if (val is double)
                {
                    return (double)val;
                }
            }
            return 0;
        }
        public static int GetInt(string key)
        {
            object val;
            if (_Dict.TryGetValue(key, out val))
            {
                if (val is int)
                {
                    return (int)val;
                }
            }
            return 0;
        }
        public static string GetString(string key)
        {
            object val;
            if (_Dict.TryGetValue(key, out val))
            {
                if (val is string)
                {
                    return (string)val;
                }
            }
            return null;
        }
        public static bool HasKey(string key)
        {
            return _Dict.ContainsKey(key);
        }
        public static void Save()
        {
            try
            {
                string json = JsonUtility.ToJson(_Dict);
                string file = GetIsolatedPath() + "/iprefs.txt";
                if (string.IsNullOrEmpty(json))
                {
                    PlatDependant.DeleteFile(file);
                }
                else
                {
                    using (var sw = PlatDependant.OpenWriteText(file))
                    {
                        sw.Write(json);
                    }
                }
            }
            catch (Exception e)
            {
                LogError(e);
            }
        }
        public static void SetNumber(string key, double value)
        {
            _Dict[key] = value;
        }
        public static void SetInt(string key, int value)
        {
            _Dict[key] = value;
        }
        public static void SetString(string key, string value)
        {
            _Dict[key] = value;
        }
#elif UNITY_ENGINE || UNITY_5_3_OR_NEWER
        public static void DeleteAll()
        {
            PlayerPrefs.DeleteAll();
        }
        public static void DeleteKey(string key)
        {
            PlayerPrefs.DeleteKey(key);
        }
        public static double GetNumber(string key)
        {
            return PlayerPrefs.GetFloat(key);
        }
        public static int GetInt(string key)
        {
            return PlayerPrefs.GetInt(key);
        }
        public static string GetString(string key)
        {
            return PlayerPrefs.GetString(key);
        }
        public static bool HasKey(string key)
        {
            return PlayerPrefs.HasKey(key);
        }
        public static void Save()
        {
            PlayerPrefs.Save();
        }
        public static void SetNumber(string key, double value)
        {
            PlayerPrefs.SetFloat(key, (float)value);
        }
        public static void SetInt(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
        }
        public static void SetString(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
        }
#else
        private static JSONObject _Dict = new JSONObject();
        static IsolatedPrefs()
        {
            string json = null;
            string file = GetIsolatedPath() + "/iprefs.txt";
            if (PlatDependant.IsFileExist(file))
            {
                try
                {
                    using (var sr = PlatDependant.OpenReadText(file))
                    {
                        json = sr.ReadToEnd();
                    }
                    if (!string.IsNullOrEmpty(json))
                    {
                        try
                        {
                            _Dict = new JSONObject(json);
                        }
                        catch (Exception e)
                        {
                            LogError(e);
                        }
                    }
                }
                catch (Exception e)
                {
                    LogError(e);
                }
            }
        }

        public static void DeleteAll()
        {
            _Dict.Clear();
        }
        public static void DeleteKey(string key)
        {
            _Dict.RemoveField(key);
        }
        public static double GetNumber(string key)
        {
            double val = 0.0;
            _Dict.GetField(ref val, key);
            return val;
        }
        public static int GetInt(string key)
        {
            int val = 0;
            _Dict.GetField(ref val, key);
            return val;
        }
        public static string GetString(string key)
        {
            string val = null;
            _Dict.GetField(ref val, key);
            return val;
        }
        public static bool HasKey(string key)
        {
            return _Dict.HasField(key);
        }
        public static void Save()
        {
            try
            {
                string json = _Dict.ToString(true);
                string file = GetIsolatedPath() + "/iprefs.txt";
                if (string.IsNullOrEmpty(json))
                {
                    PlatDependant.DeleteFile(file);
                }
                else
                {
                    using (var sw = PlatDependant.OpenWriteText(file))
                    {
                        sw.Write(json);
                    }
                }
            }
            catch (Exception e)
            {
                LogError(e);
            }
        }
        public static void SetNumber(string key, double value)
        {
            _Dict.SetField(key, value);
        }
        public static void SetInt(string key, int value)
        {
            _Dict.SetField(key, value);
        }
        public static void SetString(string key, string value)
        {
            _Dict.SetField(key, value);
        }
#endif
    }
}
