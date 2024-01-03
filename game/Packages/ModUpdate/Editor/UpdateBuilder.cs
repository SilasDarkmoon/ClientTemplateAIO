﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
#if !NET_4_6 && !NET_STANDARD_2_0
using Unity.IO.Compression;
#else
using System.IO.Compression;
#endif
using UnityEditor;
using UnityEngineEx;

using Object = UnityEngine.Object;

namespace UnityEditorEx
{
    public static class UpdateBuilder
    {
        public static bool BuildResUpdate(string oldz, string newz, string diff)
        {
            LinkedList<IDisposable> lstToDispose = new LinkedList<IDisposable>();
            try
            {
                ZipArchive olda = null, newa = null, diffa = null;

                try
                {
                    var olds = File.OpenRead(oldz);
                    lstToDispose.AddFirst(olds);
                    olda = new ZipArchive(olds, ZipArchiveMode.Read);
                    lstToDispose.AddFirst(olda);
                }
                catch { }
                try
                {
                    var news = File.OpenRead(newz);
                    lstToDispose.AddFirst(news);
                    newa = new ZipArchive(news, ZipArchiveMode.Read);
                    lstToDispose.AddFirst(newa);
                }
                catch { }

                HashSet<string> diffb = new HashSet<string>();
                if (newa == null)
                {
                    return false;
                }
                else if (Path.GetFileNameWithoutExtension(oldz) != Path.GetFileNameWithoutExtension(newz))
                {
                    PlatDependant.LogError("Build update diff error - the old zip and new zip have different names.");
                    return false;
                }
                else
                {
                    var reskey = Path.GetFileNameWithoutExtension(newz).ToLower();
                    string mentry = "res/mani/" + reskey + ".m.ab";
                    string dverentry = "res/version/" + reskey + ".txt";
                    ResManifest mold = null;
                    ResManifest mnew = null;

                    // get mani of old
                    try
                    {
                        if (olda != null)
                        {
                            var oldme = olda.GetEntry(mentry);
                            if (oldme != null)
                            {
                                using (var stream = oldme.Open())
                                {
                                    using (var mems = new MemoryStream())
                                    {
                                        stream.CopyTo(mems);
                                        var mab = UnityEngine.AssetBundle.LoadFromMemory(mems.ToArray());
                                        if (mab)
                                        {
                                            var allassets = mab.LoadAllAssets<ResOnDiskManifest>();
                                            if (allassets != null && allassets.Length > 0)
                                            {
                                                mold = ResManifest.Load(allassets[0]);
                                            }
                                            mab.Unload(true);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                    // get mani of new
                    try
                    {
                        var newme = newa.GetEntry(mentry);
                        if (newme != null)
                        {
                            using (var stream = newme.Open())
                            {
                                using (var mems = new MemoryStream())
                                {
                                    stream.CopyTo(mems);
                                    var mab = UnityEngine.AssetBundle.LoadFromMemory(mems.ToArray());
                                    if (mab)
                                    {
                                        var allassets = mab.LoadAllAssets<ResOnDiskManifest>();
                                        if (allassets != null && allassets.Length > 0)
                                        {
                                            mnew = ResManifest.Load(allassets[0]);
                                        }
                                        mab.Unload(true);
                                    }
                                }
                            }
                        }
                    }
                    catch { }

                    string abrootold = "res/";
                    string umpathold = "res/res";
                    if (mold != null && !string.IsNullOrEmpty(mold.MFlag))
                    {
                        abrootold += "mod/" + mold.MFlag + "/";
                        umpathold = abrootold + mold.MFlag;
                    }
                    string abrootnew = "res/";
                    string umpathnew = "res/res";
                    if (mnew != null && !string.IsNullOrEmpty(mnew.MFlag))
                    {
                        abrootnew += "mod/" + mnew.MFlag + "/";
                        umpathnew = abrootnew + mnew.MFlag;
                    }

                    // parse old manifest
                    UnityEngine.AssetBundleManifest maniold = null;
                    try
                    {
                        if (olda != null)
                        {
                            var emani = olda.GetEntry(umpathold);
                            if (emani != null)
                            {
                                using (var smani = emani.Open())
                                {
                                    using (var mems = new MemoryStream())
                                    {
                                        smani.CopyTo(mems);
                                        var resab = UnityEngine.AssetBundle.LoadFromMemory(mems.ToArray());
                                        if (resab)
                                        {
                                            var allassets = resab.LoadAllAssets<UnityEngine.AssetBundleManifest>();
                                            if (allassets != null && allassets.Length > 0)
                                            {
                                                maniold = allassets[0];
                                                if (maniold)
                                                {
                                                    maniold = Object.Instantiate(maniold);
                                                }
                                            }
                                            resab.Unload(true);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                    // parse new manifest
                    UnityEngine.AssetBundleManifest maninew = null;
                    try
                    {
                        var emani = newa.GetEntry(umpathnew);
                        if (emani != null)
                        {
                            using (var smani = emani.Open())
                            {
                                using (var mems = new MemoryStream())
                                {
                                    smani.CopyTo(mems);
                                    var resab = UnityEngine.AssetBundle.LoadFromMemory(mems.ToArray());
                                    if (resab)
                                    {
                                        var allassets = resab.LoadAllAssets<UnityEngine.AssetBundleManifest>();
                                        if (allassets != null && allassets.Length > 0)
                                        {
                                            maninew = allassets[0];
                                            if (maninew != null)
                                            {
                                                maninew = Object.Instantiate(maninew);
                                            }
                                        }
                                        resab.Unload(true);
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                    // both manifest found?
                    if (maninew == null)
                    {
                        File.Copy(newz, diff, true);
                        return true;
                    }

                    // parse diff assets and bundles
                    if (maninew != null)
                    {
                        var allbundles = maninew.GetAllAssetBundles();
                        foreach (var bundle in allbundles)
                        {
                            var newe = newa.GetEntry(abrootnew + bundle);
                            if (newe == null)
                            {
                                continue;
                            }
                            if (maniold != null) // && maninew.GetAssetBundleHash(bundle) == maniold.GetAssetBundleHash(bundle)) // Donot use GetAssetBundleHash, it is not stable.
                            {
                                if (olda != null)
                                {
                                    var olde = olda.GetEntry(abrootold + bundle);
                                    if (olde != null)
                                    {
                                        if (olde.Length == newe.Length)
                                        {
                                            string md5old = "";
                                            string md5new = "";
                                            try
                                            {
                                                using (var sold = olde.Open())
                                                {
                                                    md5old = ModEditorUtils.GetStreamMD5(sold);
                                                }
                                            }
                                            catch { }
                                            try
                                            {
                                                using (var snew = newe.Open())
                                                {
                                                    md5new = ModEditorUtils.GetStreamMD5(snew);
                                                }
                                            }
                                            catch { }
                                            if (md5new == md5old)
                                            {
                                                continue;
                                            }
                                        }
                                    }
                                }
                            }
                            diffb.Add(bundle);
                        }
                    }

                    // create update zip
                    if (diffb.Count > 0)
                    {
                        try
                        {
                            var streama = PlatDependant.OpenWrite(diff);
                            if (streama != null)
                            {
                                lstToDispose.AddFirst(streama);
                                diffa = new ZipArchive(streama, ZipArchiveMode.Create);
                                if (diffa != null)
                                {
                                    lstToDispose.AddFirst(diffa);

                                    // each bundle
                                    foreach (var bundle in diffb)
                                    {
                                        try
                                        {
                                            var ename = abrootnew + bundle;
                                            var entryn = newa.GetEntry(ename);
                                            if (entryn != null)
                                            {
                                                var entryd = diffa.CreateEntry(ename, CompressionLevel.Optimal);
                                                if (entryd != null)
                                                {
                                                    using (var streamn = entryn.Open())
                                                    {
                                                        using (var streamd = entryd.Open())
                                                        {
                                                            streamn.CopyTo(streamd);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        catch { }
                                    }

                                    // mani / unity manifest / version.txt
                                    string[] rawcopyentries = new[] { mentry, umpathnew, "res/version.txt", dverentry };
                                    for (int i = 0; i < rawcopyentries.Length; ++i)
                                    {
                                        var ename = rawcopyentries[i];
                                        try
                                        {
                                            var entrys = newa.GetEntry(ename);
                                            if (entrys != null)
                                            {
                                                var entryd = diffa.CreateEntry(ename, CompressionLevel.Optimal);
                                                if (entryd != null)
                                                {
                                                    using (var streams = entrys.Open())
                                                    {
                                                        using (var streamd = entryd.Open())
                                                        {
                                                            streams.CopyTo(streamd);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        catch { }
                                    }
                                }
                            }
                        }
                        catch { }
                        return true;
                    }
                }
            }
            catch { }
            finally
            {
                foreach (var dis in lstToDispose)
                {
                    if (dis != null)
                    {
                        dis.Dispose();
                    }
                }
            }
            return false;
        }
        public static bool BuildSptUpdate(string oldz, string newz, string diff)
        {
            LinkedList<IDisposable> lstToDispose = new LinkedList<IDisposable>();
            try
            {
                ZipArchive olda = null, newa = null, diffa = null;

                try
                {
                    var olds = File.OpenRead(oldz);
                    lstToDispose.AddFirst(olds);
                    olda = new ZipArchive(olds, ZipArchiveMode.Read);
                    lstToDispose.AddFirst(olda);
                }
                catch { }
                try
                {
                    var news = File.OpenRead(newz);
                    lstToDispose.AddFirst(news);
                    newa = new ZipArchive(news, ZipArchiveMode.Read);
                    lstToDispose.AddFirst(newa);
                }
                catch { }

                if (newa == null)
                {
                    return false;
                }
                else if (olda == null)
                {
                    File.Copy(newz, diff, true);
                    return true;
                }
                else
                {
                    var entries = newa.Entries;
                    List<string> diffEntries = new List<string>();
                    for (int i = 0; i < entries.Count; ++i)
                    {
                        var entry = entries[i];
                        try
                        {
                            if (entry.Name.EndsWith(".lua"))
                            {
                                var oentry = olda.GetEntry(entry.FullName);
                                if (oentry != null && oentry.Length == entry.Length)
                                {
                                    string md5old = "";
                                    string md5new = "";
                                    try
                                    {
                                        using (var sold = oentry.Open())
                                        {
                                            md5old = ModEditorUtils.GetStreamMD5(sold);
                                        }
                                    }
                                    catch { }
                                    try
                                    {
                                        using (var snew = entry.Open())
                                        {
                                            md5new = ModEditorUtils.GetStreamMD5(snew);
                                        }
                                    }
                                    catch { }
                                    if (md5new == md5old)
                                    {
                                        continue;
                                    }
                                }
                                diffEntries.Add(entry.FullName);
                            }
                        }
                        catch { }
                    }

                    if (diffEntries.Count > 0)
                    {
                        try
                        {
                            var streama = PlatDependant.OpenWrite(diff);
                            if (streama != null)
                            {
                                lstToDispose.AddFirst(streama);
                                diffa = new ZipArchive(streama, ZipArchiveMode.Create);
                                if (diffa != null)
                                {
                                    lstToDispose.AddFirst(diffa);
                                    // lua files
                                    for (int i = 0; i < diffEntries.Count; ++i)
                                    {
                                        var ename = diffEntries[i];
                                        try
                                        {
                                            var entrys = newa.GetEntry(ename);
                                            if (entrys != null)
                                            {
                                                var entryd = diffa.CreateEntry(ename, CompressionLevel.Optimal);
                                                if (entryd != null)
                                                {
                                                    using (var streams = entrys.Open())
                                                    {
                                                        using (var streamd = entryd.Open())
                                                        {
                                                            streams.CopyTo(streamd);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        catch { }
                                    }
                                    // version.txt
                                    var sptkey = Path.GetFileNameWithoutExtension(newz).ToLower();
                                    var archindex = sptkey.LastIndexOf('.');
                                    if (archindex > 0)
                                    {
                                        sptkey = sptkey.Substring(0, archindex);
                                    }
                                    var dverentry = "spt/version/" + sptkey + ".txt";
                                    string[] rawcopyentries = new[] { "spt/version.txt", dverentry };
                                    for (int i = 0; i < rawcopyentries.Length; ++i)
                                    {
                                        var ename = rawcopyentries[i];
                                        try
                                        {
                                            var entrys = newa.GetEntry(ename);
                                            if (entrys != null)
                                            {
                                                var entryd = diffa.CreateEntry(ename, CompressionLevel.Optimal);
                                                if (entryd != null)
                                                {
                                                    using (var streams = entrys.Open())
                                                    {
                                                        using (var streamd = entryd.Open())
                                                        {
                                                            streams.CopyTo(streamd);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        catch { }
                                    }
                                }
                            }
                        }
                        catch { }
                        return true;
                    }
                }
            }
            catch { }
            finally
            {
                foreach (var dis in lstToDispose)
                {
                    if (dis != null)
                    {
                        dis.Dispose();
                    }
                }
            }
            return false;
        }
        /// <remarks>not recommend</remarks>
        public static bool BuildResRawUpdate(string oldz, string newz, string diff)
        {
            LinkedList<IDisposable> lstToDispose = new LinkedList<IDisposable>();
            try
            {
                ZipArchive olda = null, newa = null, diffa = null;

                try
                {
                    var olds = File.OpenRead(oldz);
                    lstToDispose.AddFirst(olds);
                    olda = new ZipArchive(olds, ZipArchiveMode.Read);
                    lstToDispose.AddFirst(olda);
                }
                catch { }
                try
                {
                    var news = File.OpenRead(newz);
                    lstToDispose.AddFirst(news);
                    newa = new ZipArchive(news, ZipArchiveMode.Read);
                    lstToDispose.AddFirst(newa);
                }
                catch { }

                if (newa == null)
                {
                    return false;
                }
                else if (olda == null)
                {
                    File.Copy(newz, diff, true);
                    return true;
                }
                else
                {
                    var entries = newa.Entries;
                    List<string> diffEntries = new List<string>();
                    for (int i = 0; i < entries.Count; ++i)
                    {
                        var entry = entries[i];
                        try
                        {
                            if (!entry.Name.EndsWith(".manifest"))
                            {
                                var oentry = olda.GetEntry(entry.FullName);
                                if (oentry != null && oentry.Length == entry.Length)
                                {
                                    string md5old = "";
                                    string md5new = "";
                                    try
                                    {
                                        using (var sold = oentry.Open())
                                        {
                                            md5old = ModEditorUtils.GetStreamMD5(sold);
                                        }
                                    }
                                    catch { }
                                    try
                                    {
                                        using (var snew = entry.Open())
                                        {
                                            md5new = ModEditorUtils.GetStreamMD5(snew);
                                        }
                                    }
                                    catch { }
                                    if (md5new == md5old)
                                    {
                                        continue;
                                    }
                                }
                                diffEntries.Add(entry.FullName);
                            }
                        }
                        catch { }
                    }

                    if (diffEntries.Count > 0)
                    {
                        try
                        {
                            var streama = PlatDependant.OpenWrite(diff);
                            if (streama != null)
                            {
                                lstToDispose.AddFirst(streama);
                                diffa = new ZipArchive(streama, ZipArchiveMode.Create);
                                if (diffa != null)
                                {
                                    lstToDispose.AddFirst(diffa);
                                    // lua files
                                    for (int i = 0; i < diffEntries.Count; ++i)
                                    {
                                        var ename = diffEntries[i];
                                        try
                                        {
                                            var entrys = newa.GetEntry(ename);
                                            if (entrys != null)
                                            {
                                                var entryd = diffa.CreateEntry(ename, CompressionLevel.Optimal);
                                                if (entryd != null)
                                                {
                                                    using (var streams = entrys.Open())
                                                    {
                                                        using (var streamd = entryd.Open())
                                                        {
                                                            streams.CopyTo(streamd);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        catch { }
                                    }
                                }
                            }
                        }
                        catch { }
                        return true;
                    }
                }
            }
            catch { }
            finally
            {
                foreach (var dis in lstToDispose)
                {
                    if (dis != null)
                    {
                        dis.Dispose();
                    }
                }
            }
            return false;
        }
        /// <remarks>not recommend</remarks>
        public static bool BuildRawUpdate(string oldz, string newz, string diff)
        {
            LinkedList<IDisposable> lstToDispose = new LinkedList<IDisposable>();
            try
            {
                ZipArchive olda = null, newa = null, diffa = null;

                try
                {
                    var olds = File.OpenRead(oldz);
                    lstToDispose.AddFirst(olds);
                    olda = new ZipArchive(olds, ZipArchiveMode.Read);
                    lstToDispose.AddFirst(olda);
                }
                catch { }
                try
                {
                    var news = File.OpenRead(newz);
                    lstToDispose.AddFirst(news);
                    newa = new ZipArchive(news, ZipArchiveMode.Read);
                    lstToDispose.AddFirst(newa);
                }
                catch { }

                if (newa == null)
                {
                    return false;
                }
                else if (olda == null)
                {
                    File.Copy(newz, diff, true);
                    return true;
                }
                else
                {
                    var entries = newa.Entries;
                    List<string> diffEntries = new List<string>();
                    for (int i = 0; i < entries.Count; ++i)
                    {
                        var entry = entries[i];
                        try
                        {
                            var oentry = olda.GetEntry(entry.FullName);
                            if (oentry != null && oentry.Length == entry.Length)
                            {
                                string md5old = "";
                                string md5new = "";
                                try
                                {
                                    using (var sold = oentry.Open())
                                    {
                                        md5old = ModEditorUtils.GetStreamMD5(sold);
                                    }
                                }
                                catch { }
                                try
                                {
                                    using (var snew = entry.Open())
                                    {
                                        md5new = ModEditorUtils.GetStreamMD5(snew);
                                    }
                                }
                                catch { }
                                if (md5new == md5old)
                                {
                                    continue;
                                }
                            }
                            diffEntries.Add(entry.FullName);
                        }
                        catch { }
                    }

                    if (diffEntries.Count > 0)
                    {
                        try
                        {
                            var streama = PlatDependant.OpenWrite(diff);
                            if (streama != null)
                            {
                                lstToDispose.AddFirst(streama);
                                diffa = new ZipArchive(streama, ZipArchiveMode.Create);
                                if (diffa != null)
                                {
                                    lstToDispose.AddFirst(diffa);
                                    for (int i = 0; i < diffEntries.Count; ++i)
                                    {
                                        var ename = diffEntries[i];
                                        try
                                        {
                                            var entrys = newa.GetEntry(ename);
                                            if (entrys != null)
                                            {
                                                var entryd = diffa.CreateEntry(ename, CompressionLevel.Optimal);
                                                if (entryd != null)
                                                {
                                                    using (var streams = entrys.Open())
                                                    {
                                                        using (var streamd = entryd.Open())
                                                        {
                                                            streams.CopyTo(streamd);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        catch { }
                                    }
                                }
                            }
                        }
                        catch { }
                        return true;
                    }
                }
            }
            catch { }
            finally
            {
                foreach (var dis in lstToDispose)
                {
                    if (dis != null)
                    {
                        dis.Dispose();
                    }
                }
            }
            return false;
        }

        public static IEnumerator BuildAllUpdateWork(string olddir, string newdir, string diffdir, IEditorWorkProgressShower winprog)
        {
            olddir = (olddir ?? ".").TrimEnd('/', '\\');
            newdir = (newdir ?? ".").TrimEnd('/', '\\');
            diffdir = (diffdir ?? ".").TrimEnd('/', '\\');

            var logger = new EditorWorkProgressLogger() { Shower = winprog };
            System.IO.StreamWriter swlog = null;
            try
            {
                System.IO.Directory.CreateDirectory(diffdir);
                swlog = new System.IO.StreamWriter(diffdir + "/UpdateBuildLog.txt", false, System.Text.Encoding.UTF8);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log(e);
            }

            System.Collections.Concurrent.ConcurrentQueue<string> threadedLogs = new System.Collections.Concurrent.ConcurrentQueue<string>();
            int mainThreadLogScheduled = 0;
            UnityEngine.Application.LogCallback LogToFile = (message, stack, logtype) =>
            {
                if (ThreadSafeValues.IsMainThread)
                {
                    swlog.WriteLine(message);
                    swlog.Flush();
                    string mess;
                    while (threadedLogs.TryDequeue(out mess))
                    {
                        swlog.WriteLine(mess);
                        swlog.Flush();
                    }
                }
                else
                {
                    threadedLogs.Enqueue(message);
                    if (System.Threading.Interlocked.Increment(ref mainThreadLogScheduled) == 1)
                    {
                        UnityThreadDispatcher.RunInUnityThread(() =>
                        {
                            string mess;
                            while (threadedLogs.TryDequeue(out mess))
                            {
                                swlog.WriteLine(mess);
                                swlog.Flush();
                            }
                            System.Threading.Interlocked.Decrement(ref mainThreadLogScheduled);
                        });
                    }
                    else
                    {
                        System.Threading.Interlocked.Decrement(ref mainThreadLogScheduled);
                    }
                }
            };
            if (swlog != null)
            {
                UnityEngine.Application.logMessageReceivedThreaded += LogToFile;
            }
            bool cleanupDone = false;
            Action BuilderCleanup = () =>
            {
                if (!cleanupDone)
                {
                    logger.Log("(Phase) Build Update Cleaup.");
                    cleanupDone = true;
                    logger.Log("(Done) Build Update Cleaup.");
                    if (swlog != null)
                    {
                        UnityEngine.Application.logMessageReceivedThreaded -= LogToFile;
                        swlog.Flush();
                        swlog.Dispose();
                    }
                }
            };
            if (winprog != null) winprog.OnQuit += BuilderCleanup;

            try
            {
                logger.Log("Build Res Update");
                {
                    var subdir = "/res/";
                    var oroot = olddir + subdir;
                    var nroot = newdir + subdir;
                    var droot = diffdir + subdir;
                    var nfiles = PlatDependant.GetAllFiles(nroot);
                    for (int i = 0; i < nfiles.Length; ++i)
                    {
                        var nfile = nfiles[i];
                        if (nfile.EndsWith(".zip"))
                        {
                            nfile = nfile.Substring(nroot.Length);
                            logger.Log(nfile);
                            if (BuildResUpdate(oroot + nfile, nroot + nfile, droot + nfile))
                            {
                                logger.Log("Done: " + nfile);
                            }
                            else
                            {
                                logger.Log("No diff: " + nfile);
                            }
                            if (winprog != null && AsyncWorkTimer.Check()) yield return null;
                        }
                    }
                }
                logger.Log("Build Spt Update");
                {
                    var subdir = "/spt/";
                    var oroot = olddir + subdir;
                    var nroot = newdir + subdir;
                    var droot = diffdir + subdir;
                    var nfiles = PlatDependant.GetAllFiles(nroot);
                    for (int i = 0; i < nfiles.Length; ++i)
                    {
                        var nfile = nfiles[i];
                        if (nfile.EndsWith(".zip"))
                        {
                            nfile = nfile.Substring(nroot.Length);
                            logger.Log(nfile);
                            if (BuildSptUpdate(oroot + nfile, nroot + nfile, droot + nfile))
                            {
                                logger.Log("Done: " + nfile);
                            }
                            else
                            {
                                logger.Log("No diff: " + nfile);
                            }
                            if (winprog != null && AsyncWorkTimer.Check()) yield return null;
                        }
                    }
                }
            }
            finally
            {
                BuilderCleanup();
                logger.Log("(Done) Build Update.");
            }
        }
        public static IEnumerator BuildNearestUpdate(IEditorWorkProgressShower winprog)
        {
            var outputDir = "EditorOutput/Build/";
            var seldir = outputDir;
            string dirUpdateRoot = "";
            string dirUpdateLast = "";
            string dirUpdateThis = "";
            var regex = new System.Text.RegularExpressions.Regex(@"\d{6}_\d{6}");
            var subdirs = Directory.GetDirectories(seldir, "*", SearchOption.AllDirectories);
            var subdirs2 =
                from subdir in subdirs
                where regex.IsMatch(Path.GetFileName(subdir))
                orderby subdir descending
                select subdir;
            if (subdirs2.Count() > 1)
            {
                dirUpdateRoot = Path.GetDirectoryName(subdirs2.First());
                dirUpdateThis = subdirs2.First();
                dirUpdateLast = subdirs2.Skip(1).First();

                var work = BuildAllUpdateWork(dirUpdateLast + "/whole", dirUpdateThis + "/whole", dirUpdateThis + "/update", winprog);
                if (work != null)
                {
                    while (work.MoveNext())
                    {
                        if (winprog != null)
                        {
                            yield return work.Current;
                        }
                    }
                }
            }
        }

        [MenuItem("Res/Build Nearest Update", priority = 200130)]
        public static void BuildNearestUpdate()
        {
            var work = BuildNearestUpdate(null);
            while (work.MoveNext()) ;
        }
        [MenuItem("Res/Fix Nearest Update Archive", priority = 200126)]
        public static void FixNearestUpdateArchive()
        {
            var outputDir = "EditorOutput/Build/";
            var seldir = outputDir;
            string dirUpdateRoot = "";
            string dirUpdateThis = "";
            var regex = new System.Text.RegularExpressions.Regex(@"\d{6}_\d{6}");
            var subdirs = Directory.GetDirectories(seldir, "*", SearchOption.AllDirectories);
            var subdirs2 =
                from subdir in subdirs
                where regex.IsMatch(Path.GetFileName(subdir))
                orderby subdir descending
                select subdir;
            if (subdirs2.Count() > 0)
            {
                dirUpdateRoot = Path.GetDirectoryName(subdirs2.First());
                dirUpdateThis = subdirs2.First();

                ResBuilder.CopyMissingBuiltFilesToArchiveFolder(dirUpdateThis + "/whole", null);
            }
        }
        [MenuItem("Res/Restore From Nearest Update Archive", priority = 200127)]
        public static void RestoreFromNearestUpdateArchive()
        {
            var outputDir = "EditorOutput/Build/";
            var seldir = outputDir;
            string dirUpdateRoot = "";
            string dirUpdateThis = "";
            var regex = new System.Text.RegularExpressions.Regex(@"\d{6}_\d{6}");
            var subdirs = Directory.GetDirectories(seldir, "*", SearchOption.AllDirectories);
            var subdirs2 =
                from subdir in subdirs
                where regex.IsMatch(Path.GetFileName(subdir))
                orderby subdir descending
                select subdir;
            if (subdirs2.Count() > 0)
            {
                dirUpdateRoot = Path.GetDirectoryName(subdirs2.First());
                dirUpdateThis = subdirs2.First();

                ResBuilder.RestoreFromArchiveFolder(dirUpdateThis + "/whole", null);
            }
        }
    }

    [InitializeOnLoad]
    public class UpdateBuilderEx : ResBuilder.BaseResBuilderEx<UpdateBuilderEx>
    {
        private static HierarchicalInitializer _Initializer = new HierarchicalInitializer(0);

        public override void Cleanup()
        {
            if (System.IO.Directory.Exists("Assets/StreamingAssets/res/version/"))
            {
                System.IO.Directory.Delete("Assets/StreamingAssets/res/version/", true);
            }
            if (System.IO.Directory.Exists("Assets/StreamingAssets/spt/version/"))
            {
                System.IO.Directory.Delete("Assets/StreamingAssets/spt/version/", true);
            }
        }
    }
}