using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
using UnityEngine;

using Object = UnityEngine.Object;
#endif

namespace UnityEngineEx
{
    public static partial class ResManager
    {
#if !UNITY_ENGINE && !UNITY_5_3_OR_NEWER
        private static bool? _RunInExeDir;
        public static bool RunInExeDir
        {
            get
            {
                if (_RunInExeDir == null)
                {
                    var curpath = System.IO.Directory.GetCurrentDirectory();
                    var exepath = System.AppDomain.CurrentDomain.BaseDirectory;
                    _RunInExeDir = PlatDependant.IsFileSameName(curpath, exepath);
                }
                return (bool)_RunInExeDir;
            }
        }
        private static bool? _IsInUnityFolder;
        private static bool? _IsInUnityStreamingFolder;
        private static string _UnityRoot;
        public static bool IsInUnityFolder
        {
            get
            {
                if (_IsInUnityFolder == null)
                {
                    if (PlatDependant.IsFileExist("./ProjectSettings/ProjectSettings.asset"))
                    {
                        _IsInUnityFolder = true;
                        _IsInUnityStreamingFolder = false;
                        _UnityRoot = ".";
                        return true;
                    }
                    else
                    {
                        int index;
                        var full = System.IO.Path.GetFullPath(".");
                        index = full.IndexOf("/Assets");
                        if (index >= 0 && (full.Length == index + "/Assets".Length || full[index + "/Assets".Length] == '/' || full[index + "/Assets".Length] == '\\'))
                        {
                            _IsInUnityFolder = true;
                            _UnityRoot = full.Substring(0, index);
                            return true;
                        }
                        index = full.IndexOf("\\Assets");
                        if (index >= 0 && (full.Length == index + "/Assets".Length || full[index + "/Assets".Length] == '/' || full[index + "/Assets".Length] == '\\'))
                        {
                            _IsInUnityFolder = true;
                            _UnityRoot = full.Substring(0, index);
                            return true;
                        }
                        index = full.IndexOf("/Packages");
                        if (index >= 0 && (full.Length == index + "/Packages".Length || full[index + "/Packages".Length] == '/' || full[index + "/Packages".Length] == '\\'))
                        {
                            _IsInUnityFolder = true;
                            _UnityRoot = full.Substring(0, index);
                            return true;
                        }
                        index = full.IndexOf("\\Packages");
                        if (index >= 0 && (full.Length == index + "/Packages".Length || full[index + "/Packages".Length] == '/' || full[index + "/Packages".Length] == '\\'))
                        {
                            _IsInUnityFolder = true;
                            _UnityRoot = full.Substring(0, index);
                            return true;
                        }
                        _IsInUnityFolder = false;
                        return false;
                    }
                }
                return (bool)_IsInUnityFolder;
            }
        }
        public static bool IsInUnityStreamingFolder
        {
            get
            {
                if (_IsInUnityStreamingFolder == null)
                {
                    if (IsInUnityFolder)
                    {
                        int index;
                        var full = System.IO.Path.GetFullPath(".");
                        index = full.IndexOf("/Assets");
                        if (index < 0)
                        {
                            index = full.IndexOf("\\Assets");
                        }
                        if (index < 0)
                        {
                            _IsInUnityStreamingFolder = false;
                        }
                        else
                        {
                            var sub = full.Substring(index + "/Assets".Length);
                            if (sub.StartsWith("/StreamingAssets") || sub.StartsWith("\\StreamingAssets"))
                            {
                                if (sub.Length == "/StreamingAssets".Length || sub.Length == "/StreamingAssets".Length + 1 && (sub["/StreamingAssets".Length] == '/' || sub["/StreamingAssets".Length] == '\\'))
                                {
                                    _IsInUnityStreamingFolder = true;
                                }
                                else
                                {
                                    _IsInUnityStreamingFolder = false;
                                }
                            }
                            else
                            {
                                _IsInUnityStreamingFolder = false;
                            }
                        }
                    }
                    else
                    {
                        _IsInUnityStreamingFolder = false;
                    }
                }
                return (bool)_IsInUnityStreamingFolder;
            }
        }
        public static string UnityRoot
        {
            get { return _UnityRoot; }
        }

        public static string FindFileRelative(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }
            if (path[0] != '\\' && path[0] != '/')
            {
                path = "/" + path;
            }
            var spath = ThreadSafeValues.UpdatePath + path;
            if (PlatDependant.IsFileExist(spath))
            {
                return spath;
            }
            spath = ThreadSafeValues.AppStreamingAssetsPath + path;
            if (PlatDependant.IsFileExist(spath))
            {
                return spath;
            }
            spath = "." + path;
            if (PlatDependant.IsFileExist(spath))
            {
                return spath;
            }
            if (!RunInExeDir)
            {
                spath = System.AppDomain.CurrentDomain.BaseDirectory + path;
                if (PlatDependant.IsFileExist(spath))
                {
                    return spath;
                }
            }
            return null;
        }
        private static string FindFileInMod(string path, string mod)
        {
            return FindFileInMod("", path, mod);
        }
        private static string FindFileInMod(string prefix, string path, string mod)
        {
            var realpath = path;
            if (!IsInUnityFolder || IsInUnityStreamingFolder)
            {
                if (!string.IsNullOrEmpty(mod))
                {
                    realpath = prefix + "mod/" + mod + "/" + path;
                }
                else
                {
                    realpath = prefix + path;
                }
                return FindFileRelative(realpath);
            }
            else
            {
                if (!string.IsNullOrEmpty(mod))
                {
                    realpath = UnityRoot + "/Packages/" + mod + "/" + prefix + path;
                    if (System.IO.File.Exists(realpath))
                    {
                        return realpath;
                    }
                    realpath = UnityRoot + "/Library/PackageCache/" + mod + "/" + prefix + path;
                    if (System.IO.File.Exists(realpath))
                    {
                        return realpath;
                    }
                    realpath = UnityRoot + "/Assets/Mods/" + mod + "/" + prefix + path;
                    if (System.IO.File.Exists(realpath))
                    {
                        return realpath;
                    }
                    return null;
                }
                else
                {
                    realpath = UnityRoot + "/Assets/" + prefix + path;
                    if (System.IO.File.Exists(realpath))
                    {
                        return realpath;
                    }
                    realpath = UnityRoot + "/" + prefix + path;
                    if (System.IO.File.Exists(realpath))
                    {
                        return realpath;
                    }
                    realpath = prefix + path;
                    return FindFileRelative(realpath);
                }
            }
        }
        private static string FindFileInMods(string path, out string foundmod)
        {
            return FindFileInMods("", path, out foundmod);
        }
        private static string FindFileInMods(string prefix, string path, out string foundmod)
        {
            if (!IsInUnityFolder)
            {
                var flags = ResManager.GetValidDistributeFlags();
                for (int j = flags.Length - 1; j >= 0; --j)
                {
                    var mod = flags[j];
                    var found = FindFileInMod(path, mod);
                    if (found != null)
                    {
                        foundmod = mod;
                        return found;
                    }
                }
                {
                    var found = FindFileInMod(path, null);
                    if (found != null)
                    {
                        foundmod = null;
                        return found;
                    }
                }
                foundmod = null;
                return null;
            }
            else
            {
                if (IsInUnityStreamingFolder)
                {
                    var modsroot = UnityRoot + "/Assets/StreamingAssets/" + prefix + "mod";
                    if (System.IO.Directory.Exists(modsroot))
                    {
                        var subs = System.IO.Directory.GetDirectories(modsroot);
                        if (subs != null)
                        {
                            for (int i = 0; i < subs.Length; ++i)
                            {
                                var modroot = subs[i];
                                var file = modroot + "/" + path;
                                if (System.IO.File.Exists(file))
                                {
                                    foundmod = modroot.Substring(modsroot.Length + 1);
                                    return file;
                                }
                            }
                        }
                    }
                    {
                        var file = UnityRoot + "/Assets/StreamingAssets/" + prefix + path;
                        if (System.IO.File.Exists(file))
                        {
                            foundmod = null;
                            return file;
                        }
                    }
                    foundmod = null;
                    return null;
                }
                else
                {
                    {
                        var modsroot = UnityRoot + "/Packages";
                        if (System.IO.Directory.Exists(modsroot))
                        {
                            var subs = System.IO.Directory.GetDirectories(modsroot);
                            if (subs != null)
                            {
                                for (int i = 0; i < subs.Length; ++i)
                                {
                                    var modroot = subs[i];
                                    var file = modroot + "/" + prefix + path;
                                    if (System.IO.File.Exists(file))
                                    {
                                        foundmod = modroot.Substring(modsroot.Length + 1);
                                        return file;
                                    }
                                }
                            }
                        }
                    }
                    {
                        var modsroot = UnityRoot + "/Library/PackageCache";
                        if (System.IO.Directory.Exists(modsroot))
                        {
                            var subs = System.IO.Directory.GetDirectories(modsroot);
                            if (subs != null)
                            {
                                for (int i = 0; i < subs.Length; ++i)
                                {
                                    var modroot = subs[i];
                                    var file = modroot + "/" + prefix + path;
                                    if (System.IO.File.Exists(file))
                                    {
                                        foundmod = modroot.Substring(modsroot.Length + 1);
                                        return file;
                                    }
                                }
                            }
                        }
                    }
                    {
                        var modsroot = UnityRoot + "/Assets/Mods";
                        if (System.IO.Directory.Exists(modsroot))
                        {
                            var subs = System.IO.Directory.GetDirectories(modsroot);
                            if (subs != null)
                            {
                                for (int i = 0; i < subs.Length; ++i)
                                {
                                    var modroot = subs[i];
                                    var file = modroot + "/" + prefix + path;
                                    if (System.IO.File.Exists(file))
                                    {
                                        foundmod = modroot.Substring(modsroot.Length + 1);
                                        return file;
                                    }
                                }
                            }
                        }
                    }
                    {
                        var file = UnityRoot + "/Assets/" + prefix + path;
                        if (System.IO.File.Exists(file))
                        {
                            foundmod = null;
                            return file;
                        }
                    }
                    {
                        var file = UnityRoot + "/" + prefix + path;
                        if (System.IO.File.Exists(file))
                        {
                            foundmod = null;
                            return file;
                        }
                    }
                    foundmod = null;
                    return null;
                }
            }
        }
        private static string FindFileInDist(string path, string dist, out string mod)
        {
            return FindFileInDist("", path, dist, out mod);
        }
        private static string FindFileInDist(string prefix, string path, string dist, out string mod)
        {
            var realpath = path;
            if (!string.IsNullOrEmpty(dist))
            {
                realpath = "dist/" + dist + "/" + path;
            }
            return FindFileInMods(prefix, realpath, out mod);
        }
        private static string FindFileInDists(string path, out string mod, out string founddist)
        {
            return FindFileInDists("", path, out mod, out founddist);
        }
        private static string FindFileInDists(string prefix, string path, out string mod, out string founddist)
        {
            var flags = ResManager.GetValidDistributeFlags();
            for (int i = flags.Length - 1; i >= 0; --i)
            {
                var dist = flags[i];
                var found = FindFileInDist(prefix, path, dist, out mod);
                if (found != null)
                {
                    founddist = dist;
                    return found;
                }
            }
            {
                var found = FindFileInDist(prefix, path, null, out mod);
                if (found != null)
                {
                    founddist = null;
                    return found;
                }
            }
            mod = null;
            founddist = null;
            return null;
        }
        public static string FindFile(string path, out string mod, out string dist)
        {
            return FindFile("", path, out mod, out dist);
        }
        public static string FindFile(string prefix, string path, out string mod, out string dist)
        {
            if (string.IsNullOrEmpty(path))
            {
                mod = null;
                dist = null;
                return null;
            }
            if (path[0] != '\\' && path[0] != '/')
            {
                path = "/" + path;
            }
            return FindFileInDists(prefix, path, out mod, out dist);
        }
        public static string FindFile(string path)
        {
            return FindFile("", path);
        }
        public static string FindFile(string prefix, string path)
        {
            string mod, dist;
            return FindFile(prefix, path, out mod, out dist);
        }

        private static System.IO.Stream LoadFileRaw(string path)
        {
            if (path == null)
            {
                return null;
            }
            else
            {
                return PlatDependant.OpenRead(path);
            }
        }
        public static System.IO.Stream LoadFileRelative(string path)
        {
            return LoadFileRaw(FindFileRelative(path));
        }
        private static System.IO.Stream LoadFileInMod(string path, string mod)
        {
            return LoadFileRaw(FindFileInMod(path, mod));
        }
        private static System.IO.Stream LoadFileInMod(string prefix, string path, string mod)
        {
            return LoadFileRaw(FindFileInMod(prefix, path, mod));
        }
        private static System.IO.Stream LoadFileInMods(string path)
        {
            string mod;
            return LoadFileRaw(FindFileInMods(path, out mod));
        }
        private static System.IO.Stream LoadFileInMods(string prefix, string path)
        {
            string mod;
            return LoadFileRaw(FindFileInMods(prefix, path, out mod));
        }
        private static System.IO.Stream LoadFileInDist(string path, string dist)
        {
            string mod;
            return LoadFileRaw(FindFileInDist(path, dist, out mod));
        }
        private static System.IO.Stream LoadFileInDist(string prefix, string path, string dist)
        {
            string mod;
            return LoadFileRaw(FindFileInDist(prefix, path, dist, out mod));
        }
        private static System.IO.Stream LoadFileInDists(string path)
        {
            string mod, dist;
            return LoadFileRaw(FindFileInDists(path, out mod, out dist));
        }
        private static System.IO.Stream LoadFileInDists(string prefix, string path)
        {
            string mod, dist;
            return LoadFileRaw(FindFileInDists(prefix, path, out mod, out dist));
        }
        public static System.IO.Stream LoadFile(string path)
        {
            return LoadFileRaw(FindFile(path));
        }
        public static System.IO.Stream LoadFile(string prefix, string path)
        {
            return LoadFileRaw(FindFile(prefix, path));
        }
        public static string LoadText(string path)
        {
            using (var stream = LoadFile(path))
            {
                if (stream != null)
                {
                    try
                    {
                        var sr = new System.IO.StreamReader(stream);
                        return sr.ReadToEnd();
                    }
                    catch (Exception e)
                    {
                        PlatDependant.LogError(e);
                    }
                }
            }
            return null;
        }
#endif
        public static string LoadConfig(string file, out Dictionary<string, string> config)
        {
            config = null;
            if (string.IsNullOrEmpty(file))
            {
                return "LoadConfig - filename is empty";
            }
            else
            {
                try
                {
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
                    TextAsset txt = ResManager.LoadResDeep(file, typeof(TextAsset)) as TextAsset;
                    if (txt == null)
                    {
                        return "LoadConfig - cannot load file: " + file;
                    }
                    else
                    {
                        JSONObject json = new JSONObject(txt.text);
                        config = json.ToDictionary();
                        return null;
                    }
#else
                    var text = LoadText(file);
                    if (string.IsNullOrEmpty(text))
                    {
                        return "LoadConfig - cannot load file: " + file;
                    }
                    else
                    {
                        JSONObject json = new JSONObject(text);
                        config = json.ToDictionary();
                        return null;
                    }
#endif
                }
                catch (Exception e)
                {
                    return e.ToString();
                }
            }
        }
        public static Dictionary<string, string> LoadConfig(string file)
        {
            Dictionary<string, string> config;
            var error = LoadConfig(file, out config);
            if (error != null)
            {
                PlatDependant.LogError(error);
            }
            return config;
        }
        public static Dictionary<string, string> TryLoadConfig(string file)
        {
            Dictionary<string, string> config;
            LoadConfig(file, out config);
            return config;
        }

        public static Dictionary<string, object> LoadFullConfig(string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                PlatDependant.LogError("LoadConfig - filename is empty");
            }
            else
            {
                try
                {
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
                    TextAsset txt = ResManager.LoadResDeep(file, typeof(TextAsset)) as TextAsset;
                    if (txt == null)
                    {
                        PlatDependant.LogError("LoadConfig - cannot load file: " + file);
                    }
                    else
                    {
                        JSONObject json = new JSONObject(txt.text);
                        if (json.IsObject)
                        {
                            return json.ToObject() as Dictionary<string, object>;
                        }
                    }
#else
                    var text = LoadText(file);
                    if (string.IsNullOrEmpty(text))
                    {
                        PlatDependant.LogError("LoadConfig - cannot load file: " + file);
                    }
                    else
                    {
                        JSONObject json = new JSONObject(text);
                        if (json.IsObject)
                        {
                            return json.ToObject() as Dictionary<string, object>;
                        }
                    }
#endif
                }
                catch (Exception e)
                {
                    PlatDependant.LogError(e);
                }
            }
            return null;
        }
    }

    public static class ConfigManager
    {
        public static Dictionary<string, object> LoadConfig(string file)
        {
            return ResManager.LoadFullConfig(file);
        }
        public static IDictionary<string, object> Merge(this IDictionary<string, object> dict, IDictionary<string, object> dict2)
        {
            if (dict == null)
            {
                return dict2;
            }
            if (dict2 != null)
            {
                foreach (var kvp in dict2)
                {
                    dict[kvp.Key] = kvp.Value;
                }
            }
            return dict;
        }
        public static IDictionary<TKey, TValue> Merge<TKey, TValue>(this IDictionary<TKey, TValue> dict, IDictionary<TKey, TValue> dict2)
        {
            if (dict == null)
            {
                return dict2;
            }
            if (dict2 != null)
            {
                foreach (var kvp in dict2)
                {
                    dict[kvp.Key] = kvp.Value;
                }
            }
            return dict;
        }

        public static T Get<T>(this IDictionary<string, object> dict, string key)
        {
            if (dict == null)
            {
                return default(T);
            }
            else if (dict is IConvertibleDictionary)
            {
                return ((IConvertibleDictionary)dict).Get<T>(key);
            }
            else
            {
                object val;
                if (dict.TryGetValue(key, out val))
                {
                    return val.Convert<T>();
                }
                return default(T);
            }
        }
        public static void Set<T>(this IDictionary<string, object> dict, string key, T val)
        {
            if (dict == null)
            {
                return;
            }
            else if (key == null)
            {
                return;
            }
            else if (val == null)
            {
                dict.Remove(key);
            }
            else if (dict is IConvertibleDictionary)
            {
                ((IConvertibleDictionary)dict).Set<T>(key, val);
            }
            else
            {
                dict[key] = val;
            }
        }
    }
}