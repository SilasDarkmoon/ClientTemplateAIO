using System;
using System.Collections;
using System.Collections.Generic;
#if !NET_4_6 && !NET_STANDARD_2_0
using Unity.IO.Compression;
#else
using System.IO.Compression;
#endif
using UnityEngine;

using Object = UnityEngine.Object;

namespace UnityEngineEx
{
    public static partial class ResManager
    {
        public interface IObbEx
        {
            string HostedObbName { get; }
            bool IsRaw { get; } // raw: this is an obb zip file. not-raw: this is contained in some other file.
            bool IsReady { get; }
            string Error { get; }
            void GetProgress(out long progress, out long total);
            string GetContainingFile(); // raw: get the obb zip file. not-raw: get the containing file. check this file exists to determine whether we can load assets from the obb.
            System.IO.Stream OpenWholeObb(System.IO.Stream containingStream); // open the obb zip stream for both raw or not-raw. for raw, return null, means we should open file at GetContainingFile(). for not-raw, the stream is a span of GetContainingFile()。
            string GetEntryPrefix(); // get the entry prefix, null for no prefix
            string FindEntryUrl(string entryname); // for not-raw obb, maybe an asset can be loaded but can not find url of it.
            void Reset();
        }

        private class ResManager_ObbLoader : ILifetime
        {
            public ResManager_ObbLoader()
            {
                AddInitItem(this);
#if !FORCE_DECOMPRESS_ASSETS_ON_ANDROID
                if (Application.platform == RuntimePlatform.Android)
                {
                    _LoadAssetsFromApk = true;
#if !FORCE_DECOMPRESS_ASSETS_FROM_OBB
                    _LoadAssetsFromObb = true;
#endif
                }
#endif
            }
            public int Order { get { return LifetimeOrders.ABLoader - 1; } }
            public void Prepare()
            {
                if (Application.platform == RuntimePlatform.Android)
                {
#if DEBUG_OBB_IN_DOWNLOAD_PATH
#if UNITY_ANDROID
                    if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.ExternalStorageRead))
                    {
                        UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.ExternalStorageRead);
                    }
                    if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.ExternalStorageWrite))
                    {
                        UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.ExternalStorageWrite);
                    }
#endif
                    _ObbPath = "/storage/emulated/0/Download/default.obb";
                    _MainObbEx = null;
                    var obb2path = "/storage/emulated/0/Download/obb2.obb";
                    _AllObbPaths = new[] { _ObbPath, obb2path };
                    _AllObbNames = new[] { "testobb", "testobb2" };
                    _AllNonRawExObbs = new IObbEx[_AllObbNames.Length];
#else
                    bool hasobb = false;
                    string mainobbpath = null;
                    IObbEx mainobbex = null;
                    List<Pack<string, string>> obbs = new List<Pack<string, string>>();

                    using (var stream = LoadFileInStreaming("hasobb.flag.txt"))
                    {
                        if (stream != null)
                        {
                            hasobb = true;

                            string appid = Application.identifier;
                            string obbroot = Application.persistentDataPath;
                            int obbrootindex = obbroot.IndexOf(appid);
                            if (obbrootindex > 0)
                            {
                                obbroot = obbroot.Substring(0, obbrootindex);
                            }
                            obbrootindex = obbroot.LastIndexOf("/Android");
                            if (obbrootindex > 0)
                            {
                                obbroot = obbroot.Substring(0, obbrootindex);
                            }
                            if (!obbroot.EndsWith("/") && !obbroot.EndsWith("\\"))
                            {
                                obbroot += "/";
                            }
                            obbroot += "Android/obb/" + appid + "/";

                            using (var sr = new System.IO.StreamReader(stream))
                            {
                                string line;
                                while ((line = sr.ReadLine()) != null)
                                {
                                    var parts = line.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                                    if (parts != null && parts.Length > 0)
                                    {
                                        var obbname = parts[0];
                                        string obbpath = null;
                                        if (AllExObbs.ContainsKey(obbname))
                                        {
                                            var oex = AllExObbs[obbname];
                                            obbpath = oex.GetContainingFile();
                                        }
                                        else
                                        {
                                            int obbver = 0;
                                            if (parts.Length > 1)
                                            {
                                                var val = parts[1];
                                                if (!int.TryParse(val, out obbver))
                                                {
                                                    obbpath = val;
                                                }
                                            }
                                            if (obbpath == null)
                                            {
                                                if (obbver <= 0)
                                                {
                                                    obbver = AppVer;
                                                }
                                                obbpath = obbname + "." + obbver + "." + appid + ".obb";
                                            }
                                            if (!obbpath.Contains("/") && !obbpath.Contains("\\"))
                                            {
                                                obbpath = obbroot + obbpath;
                                            }

                                            if (!PlatDependant.IsFileExist(obbpath))
                                            { // use updatepath as obb path
                                                obbpath = ThreadSafeValues.UpdatePath + "/obb/" + obbname + "." + obbver + ".obb";
                                            }
                                        }

                                        obbs.Add(new Pack<string, string>(obbname, obbpath));
                                        if (obbname == "main")
                                        {
                                            mainobbpath = obbpath;
                                            if (AllExObbs.ContainsKey(obbname))
                                            {
                                                var oex = AllExObbs[obbname];
                                                if (!oex.IsRaw)
                                                {
                                                    mainobbex = oex;
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            if (mainobbpath == null)
                            {
                                mainobbpath = obbroot + "main." + AppVer + "." + appid + ".obb";

                                if (!PlatDependant.IsFileExist(mainobbpath))
                                { // use updatepath as obb path
                                    mainobbpath = ThreadSafeValues.UpdatePath + "/obb/main." + AppVer + ".obb";
                                }
                                
                                obbs.Insert(0, new Pack<string, string>("main", mainobbpath));
                            }
                        }
                    }

                    if (hasobb)
                    {
                        _ObbPath = mainobbpath;
                        _MainObbEx = mainobbex;
                        _AllObbPaths = new string[obbs.Count];
                        _AllObbNames = new string[obbs.Count];
                        _AllNonRawExObbs = new IObbEx[obbs.Count];
                        for (int i = 0; i < obbs.Count; ++i)
                        {
                            _AllObbPaths[i] = obbs[i].t2;
                            string obbname = _AllObbNames[i] = obbs[i].t1;
                            if (AllExObbs.ContainsKey(obbname))
                            {
                                var oex = AllExObbs[obbname];
                                if (!oex.IsRaw)
                                {
                                    _AllNonRawExObbs[i] = oex;
                                }
                            }
                        }
                    }
                    else
                    {
                        _ObbPath = null;
                        _MainObbEx = null;
                        _AllObbPaths = null;
                        _AllObbNames = null;
                        _AllNonRawExObbs = null;
                    }
#endif
                }
            }
            public void Init() { }
            public void Cleanup()
            {
                UnloadAllObbs();
            }
        }
#pragma warning disable 0414
        private static ResManager_ObbLoader i_ResManager_ObbLoader = new ResManager_ObbLoader();
#pragma warning restore

        private static bool _LoadAssetsFromApk;
        public static bool LoadAssetsFromApk
        {
            get { return _LoadAssetsFromApk; }
        }
        private static bool _LoadAssetsFromObb;
        public static bool LoadAssetsFromObb
        {
            get { return _LoadAssetsFromObb; }
        }

        public static bool SkipPending = true;
        public static bool SkipUpdate = false;
        public static bool SkipObb = false;
        public static bool SkipPackage = false;

        // TODO: in server?
        public static System.IO.Stream LoadFileInStreaming(string file)
        {
            return LoadFileInStreaming("", file, false, false);
        }
        public static System.IO.Stream LoadFileInStreaming(string prefix, string file, bool variantModAndDist, bool ignoreHotUpdate)
        {
            List<string> allflags;
            if (variantModAndDist)
            {
                var flags = ResManager.GetValidDistributeFlags();
                allflags = new List<string>(flags.Length + 1);
                allflags.Add(null);
                allflags.AddRange(flags);
            }
            else
            {
                allflags = new List<string>(1) { null };
            }

            if (!SkipPending && !ignoreHotUpdate)
            {
                string root = ThreadSafeValues.UpdatePath + "/pending/";
                for (int n = allflags.Count - 1; n >= 0; --n)
                {
                    var dist = allflags[n];
                    for (int m = allflags.Count - 1; m >= 0; --m)
                    {
                        var mod = allflags[m];
                        var moddir = "";
                        if (mod != null)
                        {
                            moddir = "mod/" + mod + "/";
                        }
                        if (dist != null)
                        {
                            moddir += "dist/" + dist + "/";
                        }
                        var path = root + prefix + moddir + file;
                        if (PlatDependant.IsFileExist(path))
                        {
                            return PlatDependant.OpenRead(path);
                        }
                    }
                }
            }
            if (!SkipUpdate && !ignoreHotUpdate)
            {
                string root = ThreadSafeValues.UpdatePath + "/";
                for (int n = allflags.Count - 1; n >= 0; --n)
                {
                    var dist = allflags[n];
                    for (int m = allflags.Count - 1; m >= 0; --m)
                    {
                        var mod = allflags[m];
                        var moddir = "";
                        if (mod != null)
                        {
                            moddir = "mod/" + mod + "/";
                        }
                        if (dist != null)
                        {
                            moddir += "dist/" + dist + "/";
                        }
                        var path = root + prefix + moddir + file;
                        if (PlatDependant.IsFileExist(path))
                        {
                            return PlatDependant.OpenRead(path);
                        }
                    }
                }
            }
            if (ThreadSafeValues.AppStreamingAssetsPath.Contains("://"))
            {
                if (ThreadSafeValues.AppPlatform == RuntimePlatform.Android.ToString() && _LoadAssetsFromApk)
                {
                    var allobbs = AllObbZipArchives;
                    if (!SkipObb && _LoadAssetsFromObb && allobbs != null)
                    {
                        for (int n = allflags.Count - 1; n >= 0; --n)
                        {
                            var dist = allflags[n];
                            for (int m = allflags.Count - 1; m >= 0; --m)
                            {
                                var mod = allflags[m];
                                var moddir = "";
                                if (mod != null)
                                {
                                    moddir = "mod/" + mod + "/";
                                }
                                if (dist != null)
                                {
                                    moddir += "dist/" + dist + "/";
                                }
                                var entryname = prefix + moddir + file;

                                for (int z = allobbs.Length - 1; z >= 0; --z)
                                {
                                    if (!PlatDependant.IsFileExist(ResManager.AllObbPaths[z]))
                                    { // means the obb is to be downloaded.
                                        continue;
                                    }
                                    
                                    var zip = allobbs[z];
                                    string fullentryname = entryname;
                                    if (ResManager.AllNonRawExObbs[z] != null)
                                    {
                                        var obbpre = ResManager.AllNonRawExObbs[z].GetEntryPrefix();
                                        if (obbpre != null)
                                        {
                                            fullentryname = obbpre + fullentryname;
                                        }
                                    }
                                    int retryTimes = 3;
                                    for (int i = 0; i < retryTimes; ++i)
                                    {
                                        ZipArchive za = zip;
                                        if (za == null)
                                        {
                                            PlatDependant.LogError("Obb Archive Cannot be read.");
                                            if (i != retryTimes - 1)
                                            {
                                                PlatDependant.LogInfo("Need Retry " + i);
                                            }
                                            continue;
                                        }

                                        try
                                        {
                                            var entry = za.GetEntry(fullentryname);
                                            if (entry != null)
                                            {
                                                return entry.Open();
                                            }
                                            break;
                                        }
                                        catch (Exception e)
                                        {
                                            PlatDependant.LogError(e);
                                            if (i != retryTimes - 1)
                                            {
                                                PlatDependant.LogInfo("Need Retry " + i);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (!SkipPackage)
                    {
                        for (int n = allflags.Count - 1; n >= 0; --n)
                        {
                            var dist = allflags[n];
                            for (int m = allflags.Count - 1; m >= 0; --m)
                            {
                                var mod = allflags[m];
                                var moddir = "";
                                if (mod != null)
                                {
                                    moddir = "mod/" + mod + "/";
                                }
                                if (dist != null)
                                {
                                    moddir += "dist/" + dist + "/";
                                }
                                var entryname = prefix + moddir + file;

                                int retryTimes = 3;
                                for (int i = 0; i < retryTimes; ++i)
                                {
                                    ZipArchive za = AndroidApkZipArchive;
                                    if (za == null)
                                    {
                                        PlatDependant.LogError("Apk Archive Cannot be read.");
                                        if (i != retryTimes - 1)
                                        {
                                            PlatDependant.LogInfo("Need Retry " + i);
                                        }
                                        continue;
                                    }

                                    try
                                    {
                                        var entry = za.GetEntry("assets/" + entryname);
                                        if (entry != null)
                                        {
                                            return entry.Open();
                                        }
                                        break;
                                    }
                                    catch (Exception e)
                                    {
                                        PlatDependant.LogError(e);
                                        if (i != retryTimes - 1)
                                        {
                                            PlatDependant.LogInfo("Need Retry " + i);
                                        }
                                    }
                                }

                            }
                        }
                    }
                }
            }
            else
            {
                if (!SkipPackage)
                {
                    string root = ThreadSafeValues.AppStreamingAssetsPath + "/";
                    for (int n = allflags.Count - 1; n >= 0; --n)
                    {
                        var dist = allflags[n];
                        for (int m = allflags.Count - 1; m >= 0; --m)
                        {
                            var mod = allflags[m];
                            var moddir = "";
                            if (mod != null)
                            {
                                moddir = "mod/" + mod + "/";
                            }
                            if (dist != null)
                            {
                                moddir += "dist/" + dist + "/";
                            }
                            var path = root + prefix + moddir + file;
                            if (PlatDependant.IsFileExist(path))
                            {
                                return PlatDependant.OpenRead(path);
                            }
                        }
                    }
                }
            }
            return null;
        }
        public static string FindUrlInStreaming(string file)
        {
            return FindUrlInStreaming("", file, false, false);
        }
        public static string FindUrlInStreaming(string prefix, string file, bool variantModAndDist, bool ignoreHotUpdate)
        {
            List<string> allflags;
            if (variantModAndDist)
            {
                var flags = ResManager.GetValidDistributeFlags();
                allflags = new List<string>(flags.Length + 1);
                allflags.Add(null);
                allflags.AddRange(flags);
            }
            else
            {
                allflags = new List<string>(1) { null };
            }

            if (!SkipPending && !ignoreHotUpdate)
            {
                string root = ThreadSafeValues.UpdatePath + "/pending/";
                for (int n = allflags.Count - 1; n >= 0; --n)
                {
                    var dist = allflags[n];
                    for (int m = allflags.Count - 1; m >= 0; --m)
                    {
                        var mod = allflags[m];
                        var moddir = "";
                        if (mod != null)
                        {
                            moddir = "mod/" + mod + "/";
                        }
                        if (dist != null)
                        {
                            moddir += "dist/" + dist + "/";
                        }
                        var path = root + prefix + moddir + file;
                        if (PlatDependant.IsFileExist(path))
                        {
                            return path;
                        }
                    }
                }
            }
            if (!SkipUpdate && !ignoreHotUpdate)
            {
                string root = ThreadSafeValues.UpdatePath + "/";
                for (int n = allflags.Count - 1; n >= 0; --n)
                {
                    var dist = allflags[n];
                    for (int m = allflags.Count - 1; m >= 0; --m)
                    {
                        var mod = allflags[m];
                        var moddir = "";
                        if (mod != null)
                        {
                            moddir = "mod/" + mod + "/";
                        }
                        if (dist != null)
                        {
                            moddir += "dist/" + dist + "/";
                        }
                        var path = root + prefix + moddir + file;
                        if (PlatDependant.IsFileExist(path))
                        {
                            return path;
                        }
                    }
                }
            }
            if (ThreadSafeValues.AppStreamingAssetsPath.Contains("://"))
            {
                if (ThreadSafeValues.AppPlatform == RuntimePlatform.Android.ToString() && _LoadAssetsFromApk)
                {
                    var allobbs = AllObbZipArchives;
                    if (!SkipObb && _LoadAssetsFromObb && allobbs != null)
                    {
                        for (int n = allflags.Count - 1; n >= 0; --n)
                        {
                            var dist = allflags[n];
                            for (int m = allflags.Count - 1; m >= 0; --m)
                            {
                                var mod = allflags[m];
                                var moddir = "";
                                if (mod != null)
                                {
                                    moddir = "mod/" + mod + "/";
                                }
                                if (dist != null)
                                {
                                    moddir += "dist/" + dist + "/";
                                }
                                var entryname = prefix + moddir + file;

                                for (int z = allobbs.Length - 1; z >= 0; --z)
                                {
                                    if (!PlatDependant.IsFileExist(ResManager.AllObbPaths[z]))
                                    { // means the obb is to be downloaded.
                                        continue;
                                    }

                                    if (ResManager.AllNonRawExObbs[z] != null)
                                    {
                                        var result = ResManager.AllNonRawExObbs[z].FindEntryUrl(entryname);
                                        if (result != null)
                                        {
                                            return result;
                                        }
                                    }
                                    if (ResManager.AllNonRawExObbs[z] == null || ResManager.AllNonRawExObbs[z].GetEntryPrefix() != null)
                                    {
                                        var zip = allobbs[z];
                                        var fullentryname = entryname;
                                        if (ResManager.AllNonRawExObbs[z] != null)
                                        {
                                            fullentryname = ResManager.AllNonRawExObbs[z].GetEntryPrefix() + fullentryname;
                                        }
                                        int retryTimes = 3;
                                        for (int i = 0; i < retryTimes; ++i)
                                        {
                                            ZipArchive za = zip;
                                            if (za == null)
                                            {
                                                PlatDependant.LogError("Obb Archive Cannot be read.");
                                                if (i != retryTimes - 1)
                                                {
                                                    PlatDependant.LogInfo("Need Retry " + i);
                                                }
                                                continue;
                                            }

                                            try
                                            {
                                                var entry = za.GetEntry(fullentryname);
                                                if (entry != null)
                                                {
                                                    return "jar:file://" + AllObbPaths[z] + "!/" + fullentryname;
                                                }
                                                break;
                                            }
                                            catch (Exception e)
                                            {
                                                PlatDependant.LogError(e);
                                                if (i != retryTimes - 1)
                                                {
                                                    PlatDependant.LogInfo("Need Retry " + i);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (!SkipPackage)
                    {
                        for (int n = allflags.Count - 1; n >= 0; --n)
                        {
                            var dist = allflags[n];
                            for (int m = allflags.Count - 1; m >= 0; --m)
                            {
                                var mod = allflags[m];
                                var moddir = "";
                                if (mod != null)
                                {
                                    moddir = "mod/" + mod + "/";
                                }
                                if (dist != null)
                                {
                                    moddir += "dist/" + dist + "/";
                                }
                                var entryname = prefix + moddir + file;

                                int retryTimes = 3;
                                for (int i = 0; i < retryTimes; ++i)
                                {
                                    ZipArchive za = AndroidApkZipArchive;
                                    if (za == null)
                                    {
                                        PlatDependant.LogError("Apk Archive Cannot be read.");
                                        if (i != retryTimes - 1)
                                        {
                                            PlatDependant.LogInfo("Need Retry " + i);
                                        }
                                        continue;
                                    }

                                    try
                                    {
                                        var entry = za.GetEntry("assets/" + entryname);
                                        if (entry != null)
                                        {
                                            return ThreadSafeValues.AppStreamingAssetsPath + "/" + entryname;
                                        }
                                        break;
                                    }
                                    catch (Exception e)
                                    {
                                        PlatDependant.LogError(e);
                                        if (i != retryTimes - 1)
                                        {
                                            PlatDependant.LogInfo("Need Retry " + i);
                                        }
                                    }
                                }

                            }
                        }
                    }
                }
            }
            else
            {
                if (!SkipPackage)
                {
                    string root = ThreadSafeValues.AppStreamingAssetsPath + "/";
                    for (int n = allflags.Count - 1; n >= 0; --n)
                    {
                        var dist = allflags[n];
                        for (int m = allflags.Count - 1; m >= 0; --m)
                        {
                            var mod = allflags[m];
                            var moddir = "";
                            if (mod != null)
                            {
                                moddir = "mod/" + mod + "/";
                            }
                            if (dist != null)
                            {
                                moddir += "dist/" + dist + "/";
                            }
                            var path = root + prefix + moddir + file;
                            if (PlatDependant.IsFileExist(path))
                            {
                                return path;
                            }
                        }
                    }
                }
            }
            return null;
        }

        public static int GetAppVer()
        {
            int versionCode = CrossEvent.TrigClrEvent<int>("SDK_GetAppVerCode");
            if (versionCode <= 0)
            { // the cross call failed. we parse it from the string like "1.0.0.25"
                var vername = ThreadSafeValues.AppVerName;
                if (!int.TryParse(vername, out versionCode))
                {
                    int split = vername.LastIndexOf(".");
                    if (split > 0)
                    {
                        var verlastpart = vername.Substring(split + 1);
                        int.TryParse(verlastpart, out versionCode);
                    }
                }
            }
            return versionCode;
        }
        private static int? _cached_AppVer;
        public static int AppVer
        {
            get
            {
                if (_cached_AppVer == null)
                {
                    _cached_AppVer = GetAppVer();
                }
                return (int)_cached_AppVer;
            }
        }

        #region Zip Archive on Android APK
        [ThreadStatic] private static System.IO.Stream _AndroidApkFileStream;
        [ThreadStatic] private static ZipArchive _AndroidApkZipArchive;
        public static System.IO.Stream AndroidApkFileStream
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                try
                {
                    bool disposed = false;
                    try
                    {
                        if (_AndroidApkFileStream == null)
                        {
                            disposed = true;
                        }
                        else if (!_AndroidApkFileStream.CanSeek)
                        {
                            disposed = true;
                        }
                    }
                    catch
                    {
                        disposed = true;
                    }
                    if (disposed)
                    {
                        _AndroidApkFileStream = null;
                        _AndroidApkFileStream = PlatDependant.OpenRead(ThreadSafeValues.AppDataPath);
                    }
                }
                catch (Exception e)
                {
                    PlatDependant.LogError(e);
                }
#endif
                return _AndroidApkFileStream;
            }
        }
        public static ZipArchive AndroidApkZipArchive
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                try
                {
                    bool disposed = false;
                    try
                    {
                        if (_AndroidApkZipArchive == null)
                        {
                            disposed = true;
                        }
                        else
                        {
#if !NET_4_6 && !NET_STANDARD_2_0
                            _AndroidApkZipArchive.ThrowIfDisposed();
#else
                            { var entries = _AndroidApkZipArchive.Entries; }
#endif
                            if (_AndroidApkZipArchive.Mode == ZipArchiveMode.Create)
                            {
                                disposed = true;
                            }
                        }
                    }
                    catch
                    {
                        disposed = true;
                    }
                    if (disposed)
                    {
                        _AndroidApkZipArchive = null;
                        _AndroidApkZipArchive = new ZipArchive(AndroidApkFileStream);
                    }
                }
                catch (Exception e)
                {
                    PlatDependant.LogError(e);
                }
#endif
                return _AndroidApkZipArchive;
            }
        }

        private static string _ObbPath;
        public static string ObbPath
        {
            get { return _ObbPath; }
        }
        private static IObbEx _MainObbEx;
        public static IObbEx MainObbEx
        {
            get { return _MainObbEx; }
        }
        private static string[] _AllObbPaths;
        public static string[] AllObbPaths
        {
            get { return _AllObbPaths; }
        }
        private static string[] _AllObbNames;
        public static string[] AllObbNames
        {
            get { return _AllObbNames; }
        }
        public static readonly Dictionary<string, IObbEx> AllExObbs = new Dictionary<string, IObbEx>();
        private static IObbEx[] _AllNonRawExObbs;
        public static IObbEx[] AllNonRawExObbs
        {
            get { return _AllNonRawExObbs; }
        }

        [ThreadStatic] private static System.IO.Stream _ObbFileStream;
        [ThreadStatic] private static ZipArchive _ObbZipArchive;
        public static System.IO.Stream ObbFileStream
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                if (_ObbPath != null)
                {
                    try
                    {
                        bool disposed = false;
                        try
                        {
                            if (_ObbFileStream == null)
                            {
                                disposed = true;
                            }
                            else if (!_ObbFileStream.CanSeek)
                            {
                                disposed = true;
                            }
                        }
                        catch
                        {
                            disposed = true;
                        }
                        if (disposed)
                        {
                            _ObbFileStream = null;
                            _ObbFileStream = PlatDependant.OpenRead(_ObbPath);
                        }
                    }
                    catch (Exception e)
                    {
                        PlatDependant.LogError(e);
                    }
                }
                else
                {
                    _ObbFileStream = null;
                }
#endif
                return _ObbFileStream;
            }
        }
        public static ZipArchive ObbZipArchive
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                if (_ObbPath != null && ObbFileStream != null)
                {
                    try
                    {
                        bool disposed = false;
                        try
                        {
                            if (_ObbZipArchive == null)
                            {
                                disposed = true;
                            }
                            else
                            {
#if !NET_4_6 && !NET_STANDARD_2_0
                                _ObbZipArchive.ThrowIfDisposed();
#else
                                { var entries = _ObbZipArchive.Entries; }
#endif
                                if (_ObbZipArchive.Mode == ZipArchiveMode.Create)
                                {
                                    disposed = true;
                                }
                            }
                        }
                        catch
                        {
                            disposed = true;
                        }
                        if (disposed)
                        {
                            _ObbZipArchive = null;
                            if (_MainObbEx != null)
                            {
                                _ObbZipArchive = new ZipArchive(_MainObbEx.OpenWholeObb(ObbFileStream) ?? ObbFileStream);
                            }
                            else
                            {
                                _ObbZipArchive = new ZipArchive(ObbFileStream);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        PlatDependant.LogError(e);
                    }
                }
                else
                {
                    _ObbZipArchive = null;
                }
#endif
                return _ObbZipArchive;
            }
        }
        [ThreadStatic] private static System.IO.Stream[] _AllObbFileStreams;
        [ThreadStatic] private static ZipArchive[] _AllObbZipArchives;
        public static System.IO.Stream[] AllObbFileStreams
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                if (_AllObbPaths != null)
                {
                    if (_AllObbFileStreams == null)
                    {
                        _AllObbFileStreams = new System.IO.Stream[_AllObbPaths.Length];
                    }
                    for (int i = 0; i < _AllObbFileStreams.Length; ++i)
                    {
                        try
                        {
                            bool disposed = false;
                            try
                            {
                                if (_AllObbFileStreams[i] == null)
                                {
                                    disposed = true;
                                }
                                else if (!_AllObbFileStreams[i].CanSeek)
                                {
                                    disposed = true;
                                }
                            }
                            catch
                            {
                                disposed = true;
                            }
                            if (disposed)
                            {
                                _AllObbFileStreams[i] = null;
                                _AllObbFileStreams[i] = PlatDependant.OpenRead(_AllObbPaths[i]);
                            }
                        }
                        catch (Exception e)
                        {
                            PlatDependant.LogError(e);
                        }
                    }
                }
                else
                {
                    _AllObbFileStreams = null;
                }
#endif
                return _AllObbFileStreams;
            }
        }
        public static ZipArchive[] AllObbZipArchives
        {
            get
            {
                var filestreams = AllObbFileStreams;
#if UNITY_ANDROID && !UNITY_EDITOR
                if (_AllObbPaths != null && filestreams != null)
                {
                    if (_AllObbZipArchives == null)
                    {
                        _AllObbZipArchives = new ZipArchive[filestreams.Length];
                    }
                    for (int i = 0; i < _AllObbZipArchives.Length; ++i)
                    {
                        try
                        {
                            bool disposed = false;
                            try
                            {
                                if (_AllObbZipArchives[i] == null)
                                {
                                    disposed = true;
                                }
                                else
                                {
#if !NET_4_6 && !NET_STANDARD_2_0
                                    _AllObbZipArchives[i].ThrowIfDisposed();
#else
                                    { var entries = _AllObbZipArchives[i].Entries; }
#endif
                                    if (_AllObbZipArchives[i].Mode == ZipArchiveMode.Create)
                                    {
                                        disposed = true;
                                    }
                                }
                            }
                            catch
                            {
                                disposed = true;
                            }
                            if (disposed)
                            {
                                _AllObbZipArchives[i] = null;
                                if (filestreams[i] != null)
                                {
                                    if (_AllNonRawExObbs[i] != null)
                                    {
                                        _AllObbZipArchives[i] = new ZipArchive(_AllNonRawExObbs[i].OpenWholeObb(filestreams[i]) ?? filestreams[i]);
                                    }
                                    else
                                    {
                                        _AllObbZipArchives[i] = new ZipArchive(filestreams[i]);
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            PlatDependant.LogError(e);
                        }
                    }
                }
                else
                {
                    _AllObbZipArchives = null;
                }
#endif
                return _AllObbZipArchives;
            }
        }

        public enum ZipEntryType
        {
            NonExist = 0,
            Compressed = 1,
            Uncompressed = 2,
        }
        public static ZipEntryType ObbEntryType(string file)
        {
            ZipEntryType result = ZipEntryType.NonExist;
            var allarchives = AllObbZipArchives;
            if (allarchives != null)
            {
                for (int n = allarchives.Length - 1; n >= 0; --n)
                {
                    if (!PlatDependant.IsFileExist(ResManager.AllObbPaths[n]))
                    { // means the obb is to be downloaded.
                        continue;
                    }

                    var archive = allarchives[n];
                    int retryTimes = 10;
                    for (int i = 0; i < retryTimes; ++i)
                    {
                        Exception error = null;
                        do
                        {
                            ZipArchive za = archive;
                            if (za == null)
                            {
                                error = new Exception("Obb Archive Cannot be read.");
                                break;
                            }

                            try
                            {
                                var entry = za.GetEntry(file);
                                if (entry != null)
                                {
                                    result = ZipEntryType.Compressed;
                                    if (entry.CompressedLength == entry.Length)
                                    {
                                        result = ZipEntryType.Uncompressed;
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                error = e;
                                break;
                            }
                        } while (false);
                        if (error != null)
                        {
                            if (i == retryTimes - 1)
                            {
                                PlatDependant.LogError(error);
                                throw error;
                            }
                            else
                            {
                                PlatDependant.LogError(error);
                                PlatDependant.LogInfo("Need Retry " + i);
                            }
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (result != ZipEntryType.NonExist)
                    {
                        break;
                    }
                }
            }
            return result;
        }
        public static bool IsFileInObb(string file)
        {
            return ObbEntryType(file) != ZipEntryType.NonExist;
        }

        public static void UnloadAllObbs()
        {
            if (_ObbZipArchive != null)
            {
                try
                {
                    _ObbZipArchive.Dispose();
                }
                catch (Exception e)
                {
                    PlatDependant.LogError(e);
                }
                _ObbZipArchive = null;
            }
            if (_ObbFileStream != null)
            {
                try
                {
                    _ObbFileStream.Dispose();
                }
                catch (Exception e)
                {
                    PlatDependant.LogError(e);
                }
                _ObbFileStream = null;
            }
            if (_AllObbZipArchives != null)
            {
                for (int i = 0; i < _AllObbZipArchives.Length; ++i)
                {
                    if (_AllObbZipArchives[i] != null)
                    {
                        try
                        {
                            _AllObbZipArchives[i].Dispose();
                        }
                        catch (Exception e)
                        {
                            PlatDependant.LogError(e);
                        }
                    }
                }
                _AllObbZipArchives = null;
            }
            if (_AllObbFileStreams != null)
            {
                for (int i = 0; i < _AllObbFileStreams.Length; ++i)
                {
                    if (_AllObbFileStreams[i] != null)
                    {
                        try
                        {
                            _AllObbFileStreams[i].Dispose();
                        }
                        catch (Exception e)
                        {
                            PlatDependant.LogError(e);
                        }
                    }
                }
                _AllObbFileStreams = null;
            }
        }
#endregion
    }
}
