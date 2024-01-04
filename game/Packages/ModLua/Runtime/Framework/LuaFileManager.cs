using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngineEx;

using lua = LuaLib.LuaCoreLib;
using lual = LuaLib.LuaAuxLib;
using luae = LuaLib.LuaLibEx;

#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
#if !NET_4_6 && !NET_STANDARD_2_0
using Unity.IO.Compression;
#else
using System.IO.Compression;
#endif
using UnityEngine;

using uobj = UnityEngine.Object;
#else
using System.IO.Compression;
#endif

namespace LuaLib
{
    public static class LuaFileManager
    {
        public class LuaStreamReader : IDisposable
        {
            public static readonly lua.Reader ReaderDel = new lua.Reader(ReaderFunc);
            [AOT.MonoPInvokeCallback(typeof(lua.Reader))]
            private static IntPtr ReaderFunc(IntPtr l, IntPtr ud, IntPtr size)
            {
                if (ud != IntPtr.Zero)
                {
                    LuaStreamReader reader = null;
                    try
                    {
                        System.Runtime.InteropServices.GCHandle handle = (System.Runtime.InteropServices.GCHandle)ud;
                        reader = handle.Target as LuaStreamReader;
                    }
                    catch (Exception e)
                    {
                        l.LogError(e);
                    }

                    if (reader != null && reader._Stream != null)
                    {
                        if (reader._Buffer == null)
                        {
                            reader._Buffer = new byte[64 * 1024];
                            reader._BufferHandle = System.Runtime.InteropServices.GCHandle.Alloc(reader._Buffer, System.Runtime.InteropServices.GCHandleType.Pinned);
                        }

                        int cnt = 0;
                        try
                        {
                            cnt = reader._Stream.Read(reader._Buffer, 0, reader._Buffer.Length);
                        }
                        catch (Exception e)
                        {
                            l.LogError(e);
                        }
                        if (cnt <= 0)
                        {
                            reader.Dispose();
                            System.Runtime.InteropServices.Marshal.WriteIntPtr(size, IntPtr.Zero);
                            return IntPtr.Zero;
                        }
                        else
                        {
                            System.Runtime.InteropServices.Marshal.WriteIntPtr(size, new IntPtr(cnt));
                            return reader._BufferHandle.AddrOfPinnedObject();
                        }
                    }
                }
                return IntPtr.Zero;
            }

            private byte[] _Buffer;
            private System.Runtime.InteropServices.GCHandle _BufferHandle;
            private System.IO.Stream _Stream;

            public LuaStreamReader(System.IO.Stream stream)
            {
                _Stream = stream;
            }
            public LuaStreamReader(System.IO.Stream stream, byte[] buffer)
                : this (stream)
            {
                if (buffer != null)
                {
                    _Buffer = buffer;
                    _BufferHandle = System.Runtime.InteropServices.GCHandle.Alloc(_Buffer, System.Runtime.InteropServices.GCHandleType.Pinned);
                }
            }
            public void Reuse(System.IO.Stream stream, byte[] buffer)
            {
                Dispose();
                _Stream = stream;
                if (buffer != null)
                {
                    _Buffer = buffer;
                    _BufferHandle = System.Runtime.InteropServices.GCHandle.Alloc(_Buffer, System.Runtime.InteropServices.GCHandleType.Pinned);
                }
                GC.ReRegisterForFinalize(this);
            }

#region IDisposable Support
            protected virtual void DisposeRaw()
            {
                if (_Stream != null)
                {
                    _Stream.Dispose();
                    _Stream = null;
                }
                if (_Buffer != null)
                {
                    _BufferHandle.Free();
                    _Buffer = null;
                }
            }
            ~LuaStreamReader()
            {
                DisposeRaw();
            }
            public void Dispose()
            {
                DisposeRaw();
                GC.SuppressFinalize(this);
            }
#endregion
        }

#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
        public static void SaveManifest(ResManifest mani, string file)
        {
            var tmpfile = file + ".tmp";
            using (var sw = PlatDependant.OpenWriteText(tmpfile))
            {
                if (mani != null && mani.Root != null)
                {
                    Stack<Pack<int, ResManifestNode>> candis = new Stack<Pack<int, ResManifestNode>>();
                    candis.Push(new Pack<int, ResManifestNode>(0, mani.Root));
                    while (candis.Count > 0)
                    {
                        var ppair = candis.Pop();
                        var plvl = ppair.t1;
                        var parent = ppair.t2;

                        for (int i = 0; i < plvl; ++i)
                        {
                            sw.Write("*");
                        }
                        sw.WriteLine(parent.PPath ?? "");

                        var children = parent.Children;
                        if (children != null)
                        {
                            var clvl = plvl + 1;
                            for (int i = children.Count - 1; i >= 0; --i)
                            {
                                var child = children.Values[i];
                                candis.Push(new Pack<int, ResManifestNode>(clvl, child));
                            }
                        }
                    }
                }
            }
            PlatDependant.MoveFile(tmpfile, file);
        }
        public static ResManifest LoadManifest(string file)
        {
            ResManifest mani = new ResManifest();
            if (PlatDependant.IsFileExist(file))
            {
                using (var sr = PlatDependant.OpenReadText(file))
                {
                    if (sr != null)
                    {
                        List<ResManifestNode> nodeStack = new List<ResManifestNode>();
                        var root = new ResManifestNode(mani);
                        mani.Root = root;
                        nodeStack.Add(root);

                        int nxtChar = -1;
                        while ((nxtChar = sr.Peek()) > 0)
                        {
                            int lvl = 0;
                            while (nxtChar == '*')
                            {
                                sr.Read();
                                ++lvl;
                                nxtChar = sr.Peek();
                            }
                            string ppath = sr.ReadLine();
                            if (string.IsNullOrEmpty(ppath))
                            {
                                continue;
                            }

                            if (nodeStack.Count > lvl)
                            {
                                var last = nodeStack[nodeStack.Count - 1];
                                if (last.Children == null || last.Children.Count <= 0)
                                {
                                    ResManifestItem item;
                                    item = new ResManifestItem(last);
                                    last.Item = item;
                                }

                                nodeStack.RemoveRange(lvl, nodeStack.Count - lvl);
                            }

                            {
                                var last = nodeStack[nodeStack.Count - 1];
                                if (last.Children == null)
                                {
                                    last.Children = new SortedList<string, ResManifestNode>();
                                }
                                var child = new ResManifestNode(last, ppath);
                                last.Children[ppath] = child;
                                nodeStack.Add(child);
                            }
                        }

                        if (nodeStack.Count > 1)
                        {
                            var last = nodeStack[nodeStack.Count - 1];
                            ResManifestItem item;
                            item = new ResManifestItem(last);
                            last.Item = item;
                        }

                        mani.TrimExcess();
                    }
                }
            }
            return mani;
        }

        private static System.Threading.ManualResetEvent _RuntimeManifestReady = new System.Threading.ManualResetEvent(true);

        private static ResManifest _RuntimeRawManifest;
        private static ResManifest _RuntimeManifest;
        private static void LoadRuntimeManifest(TaskProgress progress)
        {
            try
            {
                var maniPath = ThreadSafeValues.UpdatePath + "/spt/manifest.m.txt";
                if (PlatDependant.IsFileExist(maniPath))
                {
                    _RuntimeRawManifest = LoadManifest(maniPath);
                }
                else
                {
                    ResManifest mani = new ResManifest();
                    // load from update path
                    var sptfolder = ThreadSafeValues.UpdatePath + "/spt/";
                    try
                    {
                        var files = PlatDependant.GetAllFiles(sptfolder);
                        if (files != null && files.Length > 0)
                        {
                            for (int i = 0; i < files.Length; ++i)
                            {
                                var file = files[i];
                                var part = file.Substring(sptfolder.Length).Replace('\\', '/');
                                var node = mani.AddOrGetItem(part);
                                if (node.Item == null)
                                {
                                    ResManifestItem item;
                                    item = new ResManifestItem(node);
                                    node.Item = item;
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        PlatDependant.LogError(e);
                    }
                    // load from package
                    if (ThreadSafeValues.AppStreamingAssetsPath.Contains("://"))
                    {
                        if (ThreadSafeValues.AppPlatform == RuntimePlatform.Android.ToString() && ResManager.LoadAssetsFromApk)
                        {
                            // Obb
                            if (ResManager.LoadAssetsFromObb)
                            {
                                var allobbs = ResManager.AllObbZipArchives;
                                if (allobbs != null)
                                {
                                    sptfolder = "spt/";

                                    for (int z = 0; z < allobbs.Length; ++z)
                                    {
                                        if (!PlatDependant.IsFileExist(ResManager.AllObbPaths[z]))
                                        { // means the obb is to be downloaded.
                                            continue;
                                        }

                                        var zip = allobbs[z];
                                        var prefix = sptfolder;
                                        if (ResManager.AllNonRawExObbs[z] != null)
                                        {
                                            var obbpre = ResManager.AllNonRawExObbs[z].GetEntryPrefix();
                                            if (obbpre != null)
                                            {
                                                prefix = obbpre + prefix;
                                            }
                                        }
                                        int retryTimes = 10;
                                        int entryindex = 0;
                                        for (int i = 0; i < retryTimes; ++i)
                                        {
                                            Exception error = null;
                                            do
                                            {
                                                ZipArchive za = zip;
                                                if (za == null)
                                                {
                                                    PlatDependant.LogError("Obb Archive Cannot be read.");
                                                    break;
                                                }
                                                try
                                                {
                                                    var entries = za.Entries;
                                                    while (entryindex < entries.Count)
                                                    {
                                                        var entry = entries[entryindex];
                                                        var fullname = entry.FullName;
                                                        if (fullname.StartsWith(prefix))
                                                        {
                                                            var part = fullname.Substring(prefix.Length);
                                                            var node = mani.AddOrGetItem(part);
                                                            if (node.Item == null)
                                                            {
                                                                ResManifestItem item;
                                                                item = new ResManifestItem(node);
                                                                node.Item = item;
                                                            }
                                                        }
                                                        ++entryindex;
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
                                    }
                                }
                            }
                            // Apk
                            //if (true)
                            {
                                sptfolder = "assets/spt/";
                                int retryTimes = 10;
                                int entryindex = 0;
                                for (int i = 0; i < retryTimes; ++i)
                                {
                                    Exception error = null;
                                    do
                                    {
                                        ZipArchive za = ResManager.AndroidApkZipArchive;
                                        if (za == null)
                                        {
                                            PlatDependant.LogError("Apk Archive Cannot be read.");
                                            break;
                                        }
                                        try
                                        {
                                            var entries = za.Entries;
                                            while (entryindex < entries.Count)
                                            {
                                                var entry = entries[entryindex];
                                                var fullname = entry.FullName;
                                                if (fullname.StartsWith(sptfolder))
                                                {
                                                    var part = fullname.Substring(sptfolder.Length);
                                                    var node = mani.AddOrGetItem(part);
                                                    if (node.Item == null)
                                                    {
                                                        ResManifestItem item;
                                                        item = new ResManifestItem(node);
                                                        node.Item = item;
                                                    }
                                                }
                                                ++entryindex;
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
                            }
                        }
                    }
                    else
                    {
                        sptfolder = ThreadSafeValues.AppStreamingAssetsPath + "/spt/";
                        try
                        {
                            var files = PlatDependant.GetAllFiles(sptfolder);
                            if (files != null && files.Length > 0)
                            {
                                for (int i = 0; i < files.Length; ++i)
                                {
                                    var file = files[i];
                                    var part = file.Substring(sptfolder.Length).Replace('\\', '/');
                                    var node = mani.AddOrGetItem(part);
                                    if (node.Item == null)
                                    {
                                        ResManifestItem item;
                                        item = new ResManifestItem(node);
                                        node.Item = item;
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            PlatDependant.LogError(e);
                        }
                    }

                    mani.TrimExcess();
                    _RuntimeRawManifest = mani;
                    _RuntimeManifestReady.Set();
                    SaveManifest(mani, maniPath);
                }
            }
            finally
            {
                ResManager.UnloadAllObbs(); // Unload Thread Static Stream
                _RuntimeManifestReady.Set();
            }
        }
        // This will judge whether a mod is optional, so this should be called in UnityMain thread.
        private static ResManifest MergeAndCollapseRuntimeManifest(ResManifest rawmani)
        {
            var root = rawmani.Root;
            var rmani = new ResManifest();
            if (root != null)
            {
                rmani.Root = new ResManifestNode(rmani);
                ResManifestNode tmpNode = new ResManifestNode(rawmani);
                ResManifestNode archNode = null;
                if (root.Children != null)
                {
                    tmpNode.Children = new SortedList<string, ResManifestNode>();
                    for (int i = 0; i < root.Children.Count; ++i)
                    {
                        var child = root.Children.Values[i];
                        if (child.PPath == "mod")
                        {
                            continue;
                        }
                        else if (child.PPath == "@64")
                        {
                            if (Environment.Is64BitProcess)
                            {
                                archNode = child;
                            }
                            continue;
                        }
                        else if (child.PPath == "@32")
                        {
                            if (!Environment.Is64BitProcess)
                            {
                                archNode = child;
                            }
                            continue;
                        }
                        tmpNode.Children[child.PPath] = child;
                    }
                }
                // merge - no mod
                ResManifest.MergeManifestNode(rmani.Root, tmpNode, true);
                // merge - no mod on arch
                if (archNode != null)
                {
                    tmpNode = new ResManifestNode(rawmani);
                    if (archNode.Children != null)
                    {
                        tmpNode.Children = new SortedList<string, ResManifestNode>();
                        for (int i = 0; i < archNode.Children.Count; ++i)
                        {
                            var child = archNode.Children.Values[i];
                            if (child.PPath == "mod")
                            {
                                continue;
                            }
                            tmpNode.Children[child.PPath] = child;
                        }
                    }
                    ResManifest.MergeManifestNode(rmani.Root, tmpNode, true);
                }
                // merge - mod
                MergeRuntimeManifestInMod(rmani, root);
                // merge - mod on arch
                if (archNode != null)
                {
                    MergeRuntimeManifestInMod(rmani, archNode);
                }
                // Collapse
                var flags = ResManager.GetValidDistributeFlags();
                rmani.CollapseManifest(flags);
                rmani.TrimExcess();
            }
            return rmani;
        }
        private static void MergeRuntimeManifestInMod(ResManifest target, ResManifestNode root)
        {
            var flags = ResManager.GetValidDistributeFlags();
            if (root.Children != null)
            {
                ResManifestNode modNode;
                if (root.Children.TryGetValue("mod", out modNode))
                {
                    if (modNode != null && modNode.Children != null)
                    {
                        var modChildren = modNode.Children;
                        // merge - critical mod
                        for (int i = 0; i < modChildren.Count; ++i)
                        {
                            var modChild = modChildren.Values[i];
                            var mod = modChild.PPath;
                            var moddesc = ResManager.GetDistributeDesc(mod);
                            if (moddesc == null || (moddesc.InMain && !moddesc.IsOptional))
                            {
                                ResManifest.MergeManifestNode(target.Root, modChild, true);
                            }
                        }
                        // merge - opt mod
                        for (int i = 0; i < flags.Length; ++i)
                        {
                            var flag = flags[i];
                            ResManifestNode modChild;
                            if (modChildren.TryGetValue(flag, out modChild))
                            {
                                if (modChild != null)
                                {
                                    ResManifest.MergeManifestNode(target.Root, modChild, true);
                                }
                            }
                        }
                    }
                }
            }
        }
        public static void StartLoadRuntimeManifest()
        {
            _RuntimeManifestReady.WaitOne();
            _RuntimeRawManifest = null;
            _RuntimeManifest = null;
            _RuntimeManifestReady.Reset();
            PlatDependant.RunBackground(LoadRuntimeManifest);
        }
        public static void ResetRuntimeManifest()
        {
            _RuntimeManifestReady.WaitOne();
            var filePath = ThreadSafeValues.UpdatePath + "/spt/manifest.m.txt";
            PlatDependant.DeleteFile(filePath);
        }
        public static void CheckRuntimeManifest()
        {
            _RuntimeManifestReady.WaitOne();
            if (_RuntimeRawManifest == null)
            {
                StartLoadRuntimeManifest();
                _RuntimeManifestReady.WaitOne();
            }
            _RuntimeManifest = MergeAndCollapseRuntimeManifest(_RuntimeRawManifest);
        }
#endif

#if !UNITY_ENGINE && !UNITY_5_3_OR_NEWER
        private static string[] _CriticalLuaMods;
#endif
        public static string[] GetCriticalLuaMods()
        {
#if UNITY_EDITOR
            return EditorToClientUtils.GetCriticalMods();
#elif UNITY_ENGINE || UNITY_5_3_OR_NEWER
            if (_RuntimeRawManifest != null)
            {
                var root = _RuntimeRawManifest.Root;
                if (root != null && root.Children != null)
                {
                    var archnodepath = Environment.Is64BitProcess ? "@64" : "@32";
                    ResManifestNode archnode;
                    if (root.Children.TryGetValue(archnodepath, out archnode))
                    {
                        List<string> cmods = new List<string>();
                        GetCriticalLuaMods(archnode, cmods);
                        GetCriticalLuaMods(root, cmods);
                        return cmods.ToArray();
                    }
                    else
                    {
                        ResManifestNode modnode;
                        if (root.Children.TryGetValue("mod", out modnode))
                        {
                            var modChildren = modnode.Children;
                            if (modChildren != null)
                            {
                                List<string> cmods = new List<string>(modnode.Children.Count);
                                for (int i = 0; i < modChildren.Count; ++i)
                                {
                                    var modChild = modChildren.Values[i];
                                    var mod = modChild.PPath;
                                    var moddesc = ResManager.GetDistributeDesc(mod);
                                    if (moddesc == null || (moddesc.InMain && !moddesc.IsOptional))
                                    {
                                        cmods.Add(mod);
                                    }
                                }
                                return cmods.ToArray();
                            }
                        }
                    }
                }
            }
            return new string[0];
#else
            if (ResManager.IsInUnityFolder)
            {
                if (_CriticalLuaMods == null)
                {
                    if (ResManager.IsInUnityStreamingFolder)
                    {
                        List<string> mods = new List<string>();
                        var modsroot = UnityStreamingSptRoot + "/mod/";
                        var subs = System.IO.Directory.GetDirectories(modsroot);
                        if (subs != null)
                        {
                            for (int i = 0; i < subs.Length; ++i)
                            {
                                var modroot = subs[i];
                                var mod = modroot.Substring(modsroot.Length);
                                mods.Add(mod);
                            }
                        }
                        _CriticalLuaMods = mods.ToArray();
                    }
                    else
                    {
                        List<string> mods = new List<string>();
                        HashSet<string> set = new HashSet<string>();
                        {
                            var modsroot = ResManager.UnityRoot + "/Packages/";
                            if (System.IO.Directory.Exists(modsroot))
                            {
                                var subs = System.IO.Directory.GetDirectories(modsroot);
                                if (subs != null)
                                {
                                    for (int i = 0; i < subs.Length; ++i)
                                    {
                                        var modroot = subs[i];
                                        var mod = modroot.Substring(modsroot.Length);
                                        if (set.Add(mod))
                                        {
                                            mods.Add(mod);
                                        }
                                    }
                                }
                            }
                        }
                        {
                            var modsroot = ResManager.UnityRoot + "/Library/PackageCache/";
                            if (System.IO.Directory.Exists(modsroot))
                            {
                                var subs = System.IO.Directory.GetDirectories(modsroot);
                                if (subs != null)
                                {
                                    for (int i = 0; i < subs.Length; ++i)
                                    {
                                        var modroot = subs[i];
                                        var mod = modroot.Substring(modsroot.Length);
                                        if (set.Add(mod))
                                        {
                                            mods.Add(mod);
                                        }
                                    }
                                }
                            }
                        }
                        {
                            var modsroot = ResManager.UnityRoot + "/Assets/Mods/";
                            if (System.IO.Directory.Exists(modsroot))
                            {
                                var subs = System.IO.Directory.GetDirectories(modsroot);
                                if (subs != null)
                                {
                                    for (int i = 0; i < subs.Length; ++i)
                                    {
                                        var modroot = subs[i];
                                        var mod = modroot.Substring(modsroot.Length);
                                        if (set.Add(mod))
                                        {
                                            mods.Add(mod);
                                        }
                                    }
                                }
                            }
                        }
                        _CriticalLuaMods = mods.ToArray();
                    }
                }
                return _CriticalLuaMods;
            }
            else
            {
                return ResManager.GetValidDistributeFlags();
            }
#endif
        }

#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
        private static void GetCriticalLuaMods(ResManifestNode root, List<string> cmods)
        {
            ResManifestNode modnode;
            if (root.Children.TryGetValue("mod", out modnode))
            {
                var modChildren = modnode.Children;
                if (modChildren != null)
                {
                    for (int i = 0; i < modChildren.Count; ++i)
                    {
                        var modChild = modChildren.Values[i];
                        var mod = modChild.PPath;
                        var moddesc = ResManager.GetDistributeDesc(mod);
                        if (moddesc == null || (moddesc.InMain && !moddesc.IsOptional))
                        {
                            cmods.Add(mod);
                        }
                    }
                }
            }
        }
#endif

        private static readonly char[] _LuaRequireSeperateChars = new[] { '.', '/', '\\' };
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
        private static System.IO.Stream GetLuaStream(ResManifestItem item, out string location)
        {
            try
            {
                var rnode = item.Node;
                System.Text.StringBuilder sbpath = new System.Text.StringBuilder();
                while (rnode.Parent != null)
                {
                    if (sbpath.Length > 0)
                    {
                        sbpath.Insert(0, '/');
                    }
                    sbpath.Insert(0, rnode.PPath);
                    rnode = rnode.Parent;
                }
                var path = sbpath.ToString();

                // load from update path
                var sptpath = ThreadSafeValues.UpdatePath + "/spt/" + path;
                if (PlatDependant.IsFileExist(sptpath))
                {
                    location = sptpath;
                    return PlatDependant.OpenRead(sptpath);
                }
                // load from package
                if (ThreadSafeValues.AppStreamingAssetsPath.Contains("://"))
                {
                    if (ThreadSafeValues.AppPlatform == RuntimePlatform.Android.ToString() && ResManager.LoadAssetsFromApk)
                    {
                        // Obb
                        if (ResManager.LoadAssetsFromObb)
                        {
                            var allobbs = ResManager.AllObbZipArchives;
                            if (allobbs != null)
                            {
                                sptpath = "spt/" + path;

                                for (int z = allobbs.Length - 1; z >= 0; --z)
                                {
                                    if (!PlatDependant.IsFileExist(ResManager.AllObbPaths[z]))
                                    { // means the obb is to be downloaded.
                                        continue;
                                    }
                                    
                                    var zip = allobbs[z];
                                    var entryname = sptpath;
                                    if (ResManager.AllNonRawExObbs[z] != null)
                                    {
                                        var obbpre = ResManager.AllNonRawExObbs[z].GetEntryPrefix();
                                        if (obbpre != null)
                                        {
                                            entryname = obbpre + entryname;
                                        }
                                    }

                                    int retryTimes = 10;
                                    for (int i = 0; i < retryTimes; ++i)
                                    {
                                        Exception error = null;
                                        do
                                        {
                                            ZipArchive za = zip;
                                            if (za == null)
                                            {
                                                PlatDependant.LogError("Obb Archive Cannot be read.");
                                                break;
                                            }
                                            try
                                            {
                                                var entry = za.GetEntry(entryname);
                                                if (entry != null)
                                                {
                                                    location = sptpath;
                                                    return entry.Open();
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
                                }
                            }
                        }
                        // Apk
                        //if (true)
                        {
                            sptpath = "assets/spt/" + path;
                            int retryTimes = 10;
                            for (int i = 0; i < retryTimes; ++i)
                            {
                                Exception error = null;
                                do
                                {
                                    ZipArchive za = ResManager.AndroidApkZipArchive;
                                    if (za == null)
                                    {
                                        PlatDependant.LogError("Apk Archive Cannot be read.");
                                        break;
                                    }
                                    try
                                    {
                                        var entry = za.GetEntry(sptpath);
                                        if (entry != null)
                                        {
                                            location = sptpath;
                                            return entry.Open();
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
                        }
                    }
                }
                else
                {
                    sptpath = ThreadSafeValues.AppStreamingAssetsPath + "/spt/" + path;
                    if (PlatDependant.IsFileExist(sptpath))
                    {
                        location = sptpath;
                        return PlatDependant.OpenRead(sptpath);
                    }
                }
            }
            catch (Exception e)
            {
                PlatDependant.LogError(e);
            }
            location = "";
            return null;
        }
#else
        private static bool _UnityStreamingSptRootParsed;
        private static string _UnityStreamingSptRoot;
        public static string UnityStreamingSptRoot
        {
            get
            {
                if (!_UnityStreamingSptRootParsed)
                {
                    _UnityStreamingSptRootParsed = true;
                    if (ResManager.IsInUnityFolder)
                    {
                        if (ResManager.IsInUnityStreamingFolder)
                        {
                            var sptroot = ResManager.UnityRoot + "/Assets/StreamingAssets/spt";
                            var archroot = sptroot + "/@" + (System.Environment.Is64BitProcess ? "64" : "32");
                            if (System.IO.Directory.Exists(archroot))
                            {
                                sptroot = archroot;
                            }
                            _UnityStreamingSptRoot = sptroot;
                        }
                    }
                }
                return _UnityStreamingSptRoot;
            }
        }

        public static System.IO.Stream GetLuaStreamInMod(string mod, string file)
        {
            var realpath = file;
            if (!ResManager.IsInUnityFolder)
            {
                if (!string.IsNullOrEmpty(mod))
                {
                    realpath = "mod/" + mod + "/" + file;
                }
                var stream = ResManager.LoadFileRelative(realpath);
                if (stream == null)
                {
                    stream = ResManager.LoadFileRelative("spt/" + realpath);
                }
                return stream;
            }
            else
            {
                if (ResManager.IsInUnityStreamingFolder)
                {
                    if (!string.IsNullOrEmpty(mod))
                    {
                        realpath = "mod/" + mod + "/" + file;
                    }
                    var tar = UnityStreamingSptRoot + "/" + realpath;
                    if (System.IO.File.Exists(tar))
                    {
                        return PlatDependant.OpenRead(tar);
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(mod))
                    {
                        realpath = ResManager.UnityRoot + "/Packages/" + mod + "/ModSpt/" + file;
                        if (System.IO.File.Exists(realpath))
                        {
                            return PlatDependant.OpenRead(realpath);
                        }
                        realpath = ResManager.UnityRoot + "/Library/PackageCache/" + mod + "/ModSpt/" + file;
                        if (System.IO.File.Exists(realpath))
                        {
                            return PlatDependant.OpenRead(realpath);
                        }
                        realpath = ResManager.UnityRoot + "/Assets/Mods/" + mod + "/ModSpt/" + file;
                        if (System.IO.File.Exists(realpath))
                        {
                            return PlatDependant.OpenRead(realpath);
                        }
                        return null;
                    }
                    else
                    {
                        realpath = ResManager.UnityRoot + "/Assets/ModSpt/" + file;
                        if (System.IO.File.Exists(realpath))
                        {
                            return PlatDependant.OpenRead(realpath);
                        }
                        //realpath = ResManager.UnityRoot + "/" + file;
                        //if (System.IO.File.Exists(realpath))
                        //{
                        //    return PlatDependant.OpenRead(realpath);
                        //}
                        //realpath = file;
                        //return ResManager.LoadFileRelative(realpath);
                        return null;
                    }
                }
            }
        }
#endif
        public static System.IO.Stream GetLuaStream(string name, out string location)
        {
#if UNITY_EDITOR
            try
            {
                if (!string.IsNullOrEmpty(name))
                {
                    if (name.Length > 0 && name[0] == '?')
                    {
                        var real = name.Substring("?raw.".Length);
                        string mod = null;
                        string norm = real;
                        if (real.StartsWith("mod."))
                        {
                            if (real.StartsWith("mod.\""))
                            {
                                var mindex = real.IndexOf('\"', "mod.\"".Length);
                                if (mindex > 0)
                                {
                                    mod = real.Substring("mod.\"".Length, mindex - "mod.\"".Length);
                                    norm = real.Substring(mindex + 2);
                                }
                            }
                            else
                            {
                                var mindex = real.IndexOf('.', "mod.".Length);
                                if (mindex > 0)
                                {
                                    mod = real.Substring("mod.".Length, mindex - "mod.".Length);
                                    norm = real.Substring(mindex + 1);
                                }
                            }
                        }
                        norm = norm.Replace('.', '/');
                        bool isFileExist = false;
                        if (mod == null)
                        {
                            real = "Assets/ModSpt/" + norm + ".lua";
                        }
                        else
                        {
                            string package;
                            ResManager.EditorResLoader.ResRuntimeCache.ModToPackage.TryGetValue(mod, out package);
                            if (!string.IsNullOrEmpty(package))
                            {
                                real = "Packages/" + package + "/ModSpt/" + norm + ".lua";
                                //real = EditorToClientUtils.GetPathFromAssetName(real);
                                isFileExist = !string.IsNullOrEmpty(real) && PlatDependant.IsFileExist(real);
                            }
                            if (!isFileExist)
                            {
                                real = "Assets/Mods/" + mod + "/ModSpt/" + norm + ".lua";
                            }
                        }
                        if (isFileExist || PlatDependant.IsFileExist(real))
                        {
                            location = real;
                            return PlatDependant.OpenRead(real);
                        }
                    }
                    else
                    {
                        var file = "ModSpt/" + name.Replace('.', '/') + ".lua";
                        string found;
                        if (ThreadSafeValues.IsMainThread)
                        {
                            found = ResManager.EditorResLoader.CheckDistributePath(file, true, true);
                        }
                        else
                        {
                            found = ResManager.EditorResLoader.CheckDistributePathSafe(file);
                        }
                        //if (found != null)
                        //{
                        //    if (found.StartsWith("Packages/"))
                        //    {
                        //        found = EditorToClientUtils.GetPathFromAssetName(found);
                        //    }
                        //}
                        if (found != null)
                        {
                            location = found;
                            return PlatDependant.OpenRead(found);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                PlatDependant.LogError(e);
            }
#elif UNITY_ENGINE || UNITY_5_3_OR_NEWER
            try
            {
                if (name.Length > 0 && name[0] == '?')
                {
                    if (name.StartsWith("?raw."))
                    {
                        if (_RuntimeRawManifest != null)
                        {
                            var real = name.Substring("?raw.".Length);
                            real = real.Replace("\"", "");
                            string archreal = Environment.Is64BitProcess ? "@64." + real : "@32." + real;
                            ResManifestNode node;
                            if (_RuntimeRawManifest.TryGetItemIgnoreExt(archreal, out node, _LuaRequireSeperateChars) || _RuntimeRawManifest.TryGetItemIgnoreExt(real, out node, _LuaRequireSeperateChars))
                            {
                                if (node != null && node.Item != null)
                                {
                                    var item = node.Item;
                                    while (item.Ref != null)
                                    {
                                        item = item.Ref;
                                    }
                                    return GetLuaStream(item, out location);
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (_RuntimeManifest != null)
                    {
                        var node = _RuntimeManifest.GetItem(name, _LuaRequireSeperateChars);
                        if (node != null && node.Item != null)
                        {
                            var item = node.Item;
                            while (item.Ref != null)
                            {
                                item = item.Ref;
                            }
                            return GetLuaStream(item, out location);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                PlatDependant.LogError(e);
            }
#else
            try
            {
                if (!string.IsNullOrEmpty(name))
                {
                    if (name.Length > 0 && name[0] == '?')
                    {
                        var real = name.Substring("?raw.".Length);
                        string mod = null;
                        string norm = real;
                        if (real.StartsWith("mod."))
                        {
                            if (real.StartsWith("mod.\""))
                            {
                                var mindex = real.IndexOf('\"', "mod.\"".Length);
                                if (mindex > 0)
                                {
                                    mod = real.Substring("mod.\"".Length, mindex - "mod.\"".Length);
                                    norm = real.Substring(mindex + 2);
                                }
                            }
                            else
                            {
                                var mindex = real.IndexOf('.', "mod.".Length);
                                if (mindex > 0)
                                {
                                    mod = real.Substring("mod.".Length, mindex - "mod.".Length);
                                    norm = real.Substring(mindex + 1);
                                }
                            }
                        }
                        norm = norm.Replace('.', '/');
                        if (mod == null)
                        {
                            real = "/spt/" + norm + ".lua";
                        }
                        else
                        {
                            real = "/mod/" + mod + "/spt/" + norm + ".lua";
                        }
                        var stream = GetLuaStreamInMod(mod, norm + ".lua");
                        if (stream != null)
                        {
                            location = real;
                            return stream;
                        }
                    }
                    else
                    {
                        var file = name.Replace('.', '/') + ".lua";
                        string prefix = "spt/";
                        if (ResManager.IsInUnityStreamingFolder)
                        {
                            if (!UnityStreamingSptRoot.EndsWith("/spt"))
                            {
                                prefix += "@" + (System.Environment.Is64BitProcess ? "64/" : "32/");
                            }
                        }
                        else if (ResManager.IsInUnityFolder)
                        {
                            prefix = "ModSpt/";
                        }
                        string real, mod, dist;
                        real = ResManager.FindFile(prefix, file, out mod, out dist);
                        if (real != null)
                        {
                            try
                            {
                                var stream = PlatDependant.OpenRead(real);
                                if (stream != null)
                                {
                                    location = file;
                                    if (!string.IsNullOrEmpty(dist))
                                    {
                                        location = "dist/" + dist + "/" + location;
                                    }
                                    if (!string.IsNullOrEmpty(mod))
                                    {
                                        location = "mod/" + mod + "/" + location;
                                    }
                                    return stream;
                                }
                            }
                            catch (Exception e)
                            {
                                PlatDependant.LogError(e);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                PlatDependant.LogError(e);
            }
#endif
            location = "";
            return null;
        }
        public static System.IO.Stream GetLuaStream(string name)
        {
            string location;
            return GetLuaStream(name, out location);
        }

#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
        public static bool RuntimeManifestOld = true;
        private static void PrepareRuntimeManifest()
        {
            if (RuntimeManifestOld)
            {
                RuntimeManifestOld = false;
                StartLoadRuntimeManifest();
            }
        }
        public static class LifetimeOrders
        {
            public const int SptLoader = 600;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnUnityStart()
        {
#if !UNITY_EDITOR
#if MOD_UPDATE
            ResManager.AddInitItem(new ResManager.ActionInitItem(ResManager.LifetimeOrders.CrossEvent + 10, RegCrossEvents, null));
#else
            ResManager.AddInitItem(new ResManager.ActionInitItem(ResManager.LifetimeOrders.ABLoader + 5, PrepareRuntimeManifest, null));
#endif
            ResManager.AddInitItem(LifetimeOrders.SptLoader, CheckRuntimeManifest);
#endif
        }
#endif

#if MOD_UPDATE
        private static Dictionary<string, int> _RecordedResVersions_Res;
        private static Dictionary<string, int> _RecordedResVersions_Spt;
        private static Dictionary<string, int> _RecordedResVersionsCache;
        public static Dictionary<string, int> RecordedResVersions
        {
            get
            {
                if (_RecordedResVersionsCache == null)
                {
                    _RecordedResVersionsCache = new Dictionary<string, int>();
                    _RecordedResVersionsCache.Merge(_RecordedResVersions_Res);
                    _RecordedResVersionsCache.Merge(_RecordedResVersions_Spt);
                }
                return _RecordedResVersionsCache;
            }
        }
        private static void RegCrossEvents()
        {
            CrossEvent.RegHandler("SptManifestReady", cate =>
            {
                CrossEvent.GetParam(CrossEvent.TOKEN_ARGS, 0);
                var vers = CrossEvent.ContextExchangeObj as CrossEvent.RawEventData<Dictionary<string, int>>;
                _RecordedResVersionsCache = null;
                _RecordedResVersions_Spt = null;
                if (vers != null)
                {
                    _RecordedResVersions_Spt = vers.Data;
                }

                StartLoadRuntimeManifest();
            });
            CrossEvent.RegHandler("ReportResVersion", cate =>
            {
                CrossEvent.GetParam(CrossEvent.TOKEN_ARGS, 0);
                var vers = CrossEvent.ContextExchangeObj as CrossEvent.RawEventData<Dictionary<string, int>>;
                _RecordedResVersionsCache = null;
                _RecordedResVersions_Res = null;
                if (vers != null)
                {
                    _RecordedResVersions_Res = vers.Data;
                }
            });
            CrossEvent.RegHandler("ResetSptRuntimeManifest", cate =>
            {
                ResetRuntimeManifest();
            });
        }

        public static void PushVersionToLua(IntPtr l)
        {
            l.newtable();
#if UNITY_EDITOR
            l.pushnumber(int.MaxValue);
            l.SetField(-2, "editor");
#else
            var resvers = RecordedResVersions;
            if (resvers != null)
            {
                foreach (var kvp in resvers)
                {
                    l.pushnumber(kvp.Value);
                    l.SetField(-2, kvp.Key);
                }
            }
#endif
            l.SetGlobal("___resver");
        }

        //private static LuaLib.LuaExLibs.LuaExLibItem _LuaExLib_UpdateVersion_Instance = new LuaLib.LuaExLibs.LuaExLibItem(PushVersionToLua, 500);
#endif
    }
}

#if MOD_UPDATE
namespace LuaLib
{
    public static partial class LuaExLibs
    {
        private static LuaLib.LuaFramework.FurtherInit _InitLuaPushResVersion = new LuaLib.LuaFramework.FurtherInit(LuaLib.LuaFileManager.PushVersionToLua);
    }
}
#endif