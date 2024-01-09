using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngineEx;
#if !NET_4_6 && !NET_STANDARD_2_0
using Unity.IO.Compression;
#else
using System.IO.Compression;
#endif

namespace UnityEditorEx
{
    public static class ResBuilder
    {
        #region ExBuilder Interface
        public class ResBuilderParams
        {
            public string timetoken;
            public int version = 0;
            public bool makezip = true;

            private ResBuilderParams() { }
            public static ResBuilderParams Create()
            {
                return new ResBuilderParams()
                {
                    timetoken = DateTime.Now.ToString("yyMMdd_HHmmss"),
                };
            }
        }
        public static ResBuilderParams BuildingParams = null;

        public interface IBundleBuildInfo
        {
            string BundleName { get; set; }
            IList<string> Assets { get; set; }
            string FileName { get; }
            object RealInfo { get; set; }
        }
        public interface IResBuildWork
        {
            string Phase { get; set; }
            string OutputDir { get; set; }

            IBundleBuildInfo[] BundleBuildInfos { get; set; }
            ResManifest[] Manifests { get; set; }
            HashSet<int> ForceRefreshBundles { get; set; }
            Dictionary<string, object> Attached { get; set; }
        }

        public interface IResBuilderEx
        {
            //IEnumerator CustomBuild();
            void Prepare(string output);
            bool IgnoreAsset(string asset, string mod, string dist, string norm);
            string FormatBundleName(string asset, string mod, string dist, string norm);
            bool CreateItem(ResManifestNode node);
            void ModifyItem(ResManifestItem item);
            void GenerateBuildWork(string bundleName, IList<string> assets, IBundleBuildInfo bundlework, IResBuildWork modwork, int bindex);
            void PostBuildWork(string mod, IResBuildWork work, string dest);
            void Cleanup();
            void OnSuccess();
        }
        public static readonly List<IResBuilderEx> ResBuilderEx = new List<IResBuilderEx>();

        public abstract class BaseResBuilderEx : IResBuilderEx
        {
            public virtual void Cleanup()
            {
            }
            public virtual bool CreateItem(ResManifestNode node)
            {
                return false;
            }
            public virtual string FormatBundleName(string asset, string mod, string dist, string norm)
            {
                return null;
            }
            public virtual void ModifyItem(ResManifestItem item)
            {
            }
            public virtual void OnSuccess()
            {
            }
            public virtual void Prepare(string output)
            {
            }
            public virtual bool IgnoreAsset(string asset, string mod, string dist, string norm)
            {
                return false;
            }

            void IResBuilderEx.GenerateBuildWork(string bundleName, IList<string> assets, IBundleBuildInfo bundlework, IResBuildWork modwork, int bindex)
            {
            }

            void IResBuilderEx.PostBuildWork(string mod, IResBuildWork work, string dest)
            {
            }
        }
        public abstract class BaseResBuilderEx<T> : BaseResBuilderEx, IResBuilderEx where T : BaseResBuilderEx<T>, new()
        {
            public virtual void GenerateBuildWork(string bundleName, IList<string> assets, IBundleBuildInfo bundlework, IResBuildWork modwork, int bindex)
            {
            }

            public virtual void PostBuildWork(string mod, IResBuildWork work, string dest)
            {
            }

            void IResBuilderEx.GenerateBuildWork(string bundleName, IList<string> assets, IBundleBuildInfo bundlework, IResBuildWork modwork, int bindex)
            {
                GenerateBuildWork(bundleName, assets, bundlework, modwork, bindex);
            }

            void IResBuilderEx.PostBuildWork(string mod, IResBuildWork work, string dest)
            {
                PostBuildWork(mod, work, dest);
            }

            protected static T _BuilderEx = new T();
            protected struct HierarchicalInitializer
            {
                public HierarchicalInitializer(int preserved)
                {
                    ResBuilderEx.Add(_BuilderEx);
                }
            }
        }
        #endregion

        #region Ignore Assets
        private static readonly HashSet<string> _IgnoreFiles = new HashSet<string>()
        {
            ".cginc",
            ".hlsl",
        };
        public static void AddIgnoreFileExt(string ext)
        {
            if (ext != null && ext.StartsWith("."))
            {
                _IgnoreFiles.Add(ext);
            }
        }
        public static bool IgnoreByExt(string asset)
        {
            var ext = System.IO.Path.GetExtension(asset);
            return _IgnoreFiles.Contains(ext);
        }
        private static readonly HashSet<Type> _IgnoreScriptableAssets = new HashSet<Type>()
        {
            typeof(UnityEditor.LightingDataAsset),
        };
        public static void AddIgnoreScriptableAsset(Type type)
        {
            if (type != null)
            {
                _IgnoreScriptableAssets.Add(type);
            }
        }
        public static bool IgnoreByScriptableAsset(string asset)
        {
            return asset.EndsWith(".asset") && _IgnoreScriptableAssets.Contains(AssetDatabase.GetMainAssetTypeAtPath(asset));
        }
        private static readonly List<Func<string, bool>> _AssetFilters = new List<Func<string, bool>>()
        {
            //asset => asset.EndsWith(".asset") && _IgnoreScriptableAssets.Contains(AssetDatabase.GetMainAssetTypeAtPath(asset)),
        };
        public static void AddIgnoreFilter(Func<string, bool> filter)
        {
            if (filter != null)
            {
                if (!_AssetFilters.Contains(filter))
                {
                    _AssetFilters.Add(filter);
                }
            }
        }
        public static bool IgnoreByFilter(string asset)
        {
            for (int i = 0; i < _AssetFilters.Count; ++i)
            {
                var filter = _AssetFilters[i];
                if (filter(asset))
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region Make Zip
        public static TaskProgress MakeZipBackground(string zipFile, string srcDir, IList<string> entries, System.Threading.EventWaitHandle waithandle)
        {
            return PlatDependant.RunBackground(progress =>
            {
                try
                {
                    if (string.IsNullOrEmpty(zipFile) || entries == null || entries.Count == 0 || !System.IO.Directory.Exists(srcDir))
                    {
                        return;
                    }
                    progress.Total = entries.Count;
                    using (var stream = PlatDependant.OpenWrite(zipFile))
                    {
                        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create))
                        {
                            if (!srcDir.EndsWith("/") && !srcDir.EndsWith("\\"))
                            {
                                srcDir += "/";
                            }
                            for (int i = 0; i < entries.Count; ++i)
                            {
                                progress.Length = i;
                                var entry = entries[i];
                                if (string.IsNullOrEmpty(entry))
                                {
                                    continue;
                                }

                                var src = srcDir + entry;
                                if (PlatDependant.IsFileExist(src))
                                {
                                    try
                                    {
                                        using (var srcstream = PlatDependant.OpenRead(src))
                                        {
                                            var zentry = zip.CreateEntry(entry.Replace('\\', '/'));
                                            using (var dststream = zentry.Open())
                                            {
                                                srcstream.CopyTo(dststream);
                                            }
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        PlatDependant.LogError("zip entry FAIL! " + entry);
                                        PlatDependant.LogError(e);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    PlatDependant.LogError("Build zip FAIL! " + zipFile);
                    PlatDependant.LogError(e);
                }
                finally
                {
                    if (waithandle != null)
                    {
                        waithandle.Set();
                    }
                }
            });
        }
        public static IEnumerator MakeZipsBackground(IList<Pack<string, string, IList<string>>> zips, IEditorWorkProgressShower winprog)
        {
            var logger = new EditorWorkProgressLogger() { Shower = winprog };
            if (zips != null)
            {
                System.Threading.EventWaitHandle waithandle = null;
                if (winprog == null)
                {
                    waithandle = new System.Threading.ManualResetEvent(true);
                }
                int next = 0;
                int done = 0;
                int cpucnt = System.Environment.ProcessorCount;
                Pack<string, TaskProgress>[] working = new Pack<string, TaskProgress>[cpucnt];
                while (done < zips.Count)
                {
                    for (int i = 0; i < cpucnt; ++i)
                    {
                        var info = working[i];
                        if (info.t2 == null)
                        {
                            if (next < zips.Count)
                            {
                                var zip = zips[next++];
                                if (winprog == null)
                                {
                                    waithandle.Reset();
                                }
                                working[i] = new Pack<string, TaskProgress>(zip.t1, MakeZipBackground(zip.t1, zip.t2, zip.t3, waithandle));
                            }
                        }
                        else
                        {
                            if (info.t2.Done)
                            {
                                ++done;
                                logger.Log("Zip file DONE! " + info.t1);
                                working[i].t2 = null;
                            }
                        }
                    }
                    if (done >= zips.Count)
                    {
                        break;
                    }
                    if (winprog == null)
                    {
                        waithandle.WaitOne();
                    }
                    else
                    {
                        yield return null;
                    }
                }
                logger.Log("Zip ALL DONE!");
            }
            else
            {
                logger.Log("Zip - No file to zip.");
            }
        }
        public static IEnumerator MakeZipAsync(string zipFile, string srcDir, IList<string> entries, IEditorWorkProgressShower winprog)
        {
            var logger = new EditorWorkProgressLogger() { Shower = winprog };
            logger.Log("Zipping: " + zipFile);
            if (string.IsNullOrEmpty(zipFile) || entries == null || entries.Count == 0 || !System.IO.Directory.Exists(srcDir))
            {
                logger.Log("Nothing to zip");
                yield break;
            }

            var stream = PlatDependant.OpenWrite(zipFile);
            if (stream == null)
            {
                logger.Log("Cannot create zip file.");
                yield break;
            }

            var zip = new ZipArchive(stream, ZipArchiveMode.Create);

            try
            {
                if (!srcDir.EndsWith("/") && !srcDir.EndsWith("\\"))
                {
                    srcDir += "/";
                }
                for (int i = 0; i < entries.Count; ++i)
                {
                    var entry = entries[i];
                    if (winprog != null && AsyncWorkTimer.Check()) yield return null;
                    logger.Log(entry);
                    if (string.IsNullOrEmpty(entry))
                    {
                        continue;
                    }

                    var src = srcDir + entry;
                    if (PlatDependant.IsFileExist(src))
                    {
                        try
                        {
                            using (var srcstream = PlatDependant.OpenRead(src))
                            {
                                var zentry = zip.CreateEntry(entry.Replace('\\', '/'));
                                using (var dststream = zentry.Open())
                                {
                                    srcstream.CopyTo(dststream);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            logger.Log("(Error)(Not Critical)");
                            logger.Log(e.ToString());
                        }
                    }
                }
            }
            finally
            {
                zip.Dispose();
                stream.Dispose();
            }
        }
        #endregion

        #region Abstract Build
        public interface IResBuilderImp
        {
            string BundleExtName { get; }
            IResBuildWork CreateWork();
            IBundleBuildInfo CreateBundleBuildInfo();
            TaskProgress Prepare(string outputdir);
            void Cleanup(IResBuildWork work);
            void Build(IResBuildWork work);
            Dictionary<string, IList<string>> GroupBuiltFiles(string outputdir);
        }
        public static IResBuilderImp BuilderImp;

        public static IEnumerator GenerateBuildWorkAsync(Dictionary<string, IResBuildWork> result, IList<string> assets, IEditorWorkProgressShower winprog)
        {
            return GenerateBuildWorkAsync(result, assets, winprog, null);
        }
        public static IEnumerator GenerateBuildWorkAsync(Dictionary<string, IResBuildWork> result, IList<string> assets, IEditorWorkProgressShower winprog, IList<IResBuilderEx> runOnceExBuilder)
        {
            var logger = new EditorWorkProgressLogger() { Shower = winprog };
            logger.Log("(Start) Generate Build Work.");
            if (winprog != null && AsyncWorkTimer.Check()) yield return null;

            if (result == null)
            {
                logger.Log("(Error) You have to provide container to retrive the result.");
                yield break;
            }
            result.Clear();

            if (assets == null)
            {
                logger.Log("(Option) Get All Assets.");
                assets = AssetDatabase.GetAllAssetPaths();
                if (winprog != null && AsyncWorkTimer.Check()) yield return null;
            }

            if (assets != null)
            {
                List<IResBuilderEx> allExBuilders = new List<IResBuilderEx>(ResBuilderEx);
                if (runOnceExBuilder != null)
                {
                    allExBuilders.AddRange(runOnceExBuilder);
                }

                var allDistDescs = DistributeEditor.GetAllDistributeDescs();
                Dictionary<string, Dictionary<string, List<string>>> mod2build = new Dictionary<string, Dictionary<string, List<string>>>();
                Dictionary<string, Dictionary<string, ResManifest>> mod2mani = new Dictionary<string, Dictionary<string, ResManifest>>();
                for (int i = 0; i < assets.Count; ++i)
                {
                    if (winprog != null && AsyncWorkTimer.Check()) yield return null;
                    var asset = assets[i];
                    logger.Log(asset);

                    if (string.IsNullOrEmpty(asset))
                    {
                        logger.Log("Empty Path.");
                        continue;
                    }
                    if (System.IO.Directory.Exists(asset))
                    {
                        logger.Log("Folder.");
                        continue;
                    }
                    if (ResInfoEditor.IsAssetScript(asset))
                    {
                        logger.Log("Script.");
                        continue;
                    }
                    if (IgnoreByExt(asset))
                    {
                        logger.Log("Ignored By Ext.");
                        continue;
                    }
                    if (IgnoreByScriptableAsset(asset))
                    {
                        logger.Log("Ignored By Scriptable Asset.");
                        continue;
                    }
                    if (IgnoreByFilter(asset))
                    {
                        logger.Log("Ignored By Filter.");
                        continue;
                    }

                    string mod = null;
                    string opmod = null;
                    string dist = null;
                    string norm = asset;
                    bool inPackage = false;
                    DistributeEditor.DistDesc distdesc;
                    if (asset.StartsWith("Assets/Mods/") || (inPackage = asset.StartsWith("Packages/")))
                    {
                        string sub;
                        if (inPackage)
                        {
                            sub = asset.Substring("Packages/".Length);
                        }
                        else
                        {
                            sub = asset.Substring("Assets/Mods/".Length);
                        }
                        var index = sub.IndexOf('/');
                        if (index < 0)
                        {
                            logger.Log("Cannot Parse Module.");
                            continue;
                        }
                        mod = sub.Substring(0, index);
                        if (inPackage)
                        {
                            mod = ModEditor.GetPackageModName(mod);
                        }
                        if (string.IsNullOrEmpty(mod))
                        {
                            logger.Log("Empty Module.");
                            continue;
                        }
                        if (allDistDescs.TryGetValue(mod, out distdesc) && distdesc.NoSelectNoBuild && !ResManager.GetDistributeFlagsSet().Contains(mod))
                        {
                            logger.Log("Mod NoSelectNoBuild.");
                            continue;
                        }
                        sub = sub.Substring(index + 1);
                        if (!sub.StartsWith("ModRes/"))
                        {
                            logger.Log("Should Ignore This Asset.");
                            continue;
                        }
                        var moddesc = ResManager.GetDistributeDesc(mod);
                        bool isMainPackage = inPackage && !ModEditor.ShouldTreatPackageAsMod(ModEditor.GetPackageName(mod));
                        if (moddesc == null || moddesc.InMain || isMainPackage)
                        {
                            mod = "";
                            if (moddesc != null && moddesc.IsOptional && !isMainPackage)
                            {
                                opmod = moddesc.Mod;
                            }
                        }

                        sub = sub.Substring("ModRes/".Length);
                        norm = sub;
                        if (sub.StartsWith("dist/"))
                        {
                            sub = sub.Substring("dist/".Length);
                            index = sub.IndexOf('/');
                            if (index > 0)
                            {
                                dist = sub.Substring(0, index);
                                norm = sub.Substring(index + 1);
                            }
                        }
                    }
                    else
                    {
                        if (asset.StartsWith("Assets/ModRes/"))
                        {
                            mod = "";
                            var sub = asset.Substring("Assets/ModRes/".Length);
                            norm = sub;
                            if (sub.StartsWith("dist/"))
                            {
                                sub = sub.Substring("dist/".Length);
                                var index = sub.IndexOf('/');
                                if (index > 0)
                                {
                                    dist = sub.Substring(0, index);
                                    norm = sub.Substring(index + 1);
                                }
                            }
                        }
                        else
                        {
                            logger.Log("Should Ignore This Asset.");
                            continue;
                        }
                    }

                    if (string.IsNullOrEmpty(norm))
                    {
                        logger.Log("Normallized Path Empty.");
                        continue;
                    }
                    if (!string.IsNullOrEmpty(dist) && allDistDescs.TryGetValue(dist, out distdesc) && distdesc.NoSelectNoBuild && !ResManager.GetDistributeFlagsSet().Contains(dist))
                    {
                        logger.Log("Dist NoSelectNoBuild.");
                        continue;
                    }

                    bool ignored = false;
                    for (int j = 0; j < allExBuilders.Count; ++j)
                    {
                        if (allExBuilders[j].IgnoreAsset(asset, mod, dist, norm))
                        {
                            ignored = true;
                            break;
                        }
                    }
                    if (ignored)
                    {
                        logger.Log("Ignored by BuilderEx.");
                        continue;
                    }

                    mod = mod ?? "";
                    dist = dist ?? "";
                    logger.Log("Mod " + mod + "; Dist " + dist + "; Norm " + norm);

                    Dictionary<string, List<string>> builds;
                    if (!mod2build.TryGetValue(mod, out builds))
                    {
                        builds = new Dictionary<string, List<string>>();
                        mod2build[mod] = builds;
                    }

                    Dictionary<string, ResManifest> manis;
                    if (!mod2mani.TryGetValue(opmod ?? mod, out manis))
                    {
                        manis = new Dictionary<string, ResManifest>();
                        mod2mani[opmod ?? mod] = manis;
                    }
                    ResManifest mani;
                    if (!manis.TryGetValue(dist, out mani))
                    {
                        mani = new ResManifest();
                        mani.MFlag = opmod ?? mod;
                        mani.DFlag = dist;
                        if (opmod != null)
                        {
                            mani.InMain = true;
                        }
                        manis[dist] = mani;
                    }

                    string bundle = null;
                    bool shouldWriteBRef = false;
                    for (int j = 0; j < allExBuilders.Count; ++j)
                    {
                        bundle = allExBuilders[j].FormatBundleName(asset, opmod ?? mod, dist, norm);
                        if (bundle != null)
                        {
                            break;
                        }
                    }
                    if (bundle == null)
                    {
                        bundle = FormatBundleName(asset, opmod ?? mod, dist, norm);
                    }
                    else
                    {
                        shouldWriteBRef = true;
                    }

                    List<string> build;
                    if (!builds.TryGetValue(bundle, out build))
                    {
                        build = new List<string>();
                        builds[bundle] = build;
                    }
                    build.Add(asset);

                    var node = mani.AddOrGetItem(asset);
                    for (int j = 0; j < allExBuilders.Count; ++j)
                    {
                        if (allExBuilders[j].CreateItem(node))
                        {
                            break;
                        }
                    }
                    if (node.Item == null)
                    {
                        var item = new ResManifestItem(node);
                        if (asset.EndsWith(".prefab"))
                        {
                            item.Type = (int)ResManifestItemType.Prefab;
                        }
                        else if (asset.EndsWith(".unity"))
                        {
                            item.Type = (int)ResManifestItemType.Scene;
                        }
                        else
                        {
                            item.Type = (int)ResManifestItemType.Normal;
                        }
                        if (shouldWriteBRef)
                        {
                            item.BRef = bundle;
                        }
                        node.Item = item;
                    }
                    for (int j = 0; j < allExBuilders.Count; ++j)
                    {
                        allExBuilders[j].ModifyItem(node.Item);
                    }
                }

                if (winprog != null && AsyncWorkTimer.Check()) yield return null;
                logger.Log("(Phase) Combine the final result.");

                foreach (var kvpbuild in mod2build)
                {
                    var mod = kvpbuild.Key;
                    var builds = kvpbuild.Value;
                    IResBuildWork work = BuilderImp.CreateWork();
                    if (mod == "")
                    {
                        List<ResManifest> manis = new List<ResManifest>(mod2mani[mod].Values);
                        foreach (var kvpmm in mod2mani)
                        {
                            if (!mod2build.ContainsKey(kvpmm.Key))
                            {
                                manis.AddRange(kvpmm.Value.Values);
                            }
                        }
                        work.Manifests = manis.ToArray();
                    }
                    else
                    {
                        work.Manifests = mod2mani[mod].Values.ToArray();
                    }

                    IBundleBuildInfo[] bundleBuildInfos = new IBundleBuildInfo[builds.Count];
                    int index = 0;
                    foreach (var kvpbundle in builds)
                    {
                        var bundleName = kvpbundle.Key;
                        var bundleAssets = kvpbundle.Value;
                        IBundleBuildInfo bundleBuildInfo = BuilderImp.CreateBundleBuildInfo();
                        bundleBuildInfo.BundleName = bundleName;
                        bundleBuildInfo.Assets = bundleAssets;
                        for (int j = 0; j < allExBuilders.Count; ++j)
                        {
                            allExBuilders[j].GenerateBuildWork(bundleName, bundleAssets, bundleBuildInfo, work, index);
                        }
                        bundleBuildInfos[index++] = bundleBuildInfo;
                    }
                    work.BundleBuildInfos = bundleBuildInfos;

                    result[mod] = work;
                }
            }

            logger.Log("(Done) Generate Build Work.");
        }

        public static IEnumerator BuildResAsync(IList<string> assets, IEditorWorkProgressShower winprog)
        {
            return BuildResAsync(assets, winprog, null);
        }
        public static IEnumerator BuildResAsync(IList<string> assets, IEditorWorkProgressShower winprog, IList<IResBuilderEx> runOnceExBuilder)
        {
            bool isDefaultBuild = assets == null;
            var logger = new EditorWorkProgressLogger() { Shower = winprog };
            bool shouldCreateBuildingParams = BuildingParams == null;
            BuildingParams = BuildingParams ?? ResBuilderParams.Create();
            var timetoken = BuildingParams.timetoken;
            var makezip = BuildingParams.makezip;
            int version = 0;
            if (isDefaultBuild)
            {
                if (BuildingParams != null && BuildingParams.version > 0)
                {
                    version = BuildingParams.version;
                }
                else
                {
                    version = GetResVersion();
                    BuildingParams.version = version;
                }
            }
            string outputDir = "Latest";
            if (!isDefaultBuild)
            {
                outputDir = timetoken + (version > 0 ? ("_" + version) : "") + "/build";
            }
            outputDir = "EditorOutput/Build/" + outputDir;

            System.IO.StreamWriter swlog = null;
            try
            {
                System.IO.Directory.CreateDirectory(outputDir + "/log/");
                swlog = new System.IO.StreamWriter(outputDir + "/log/ResBuildLog.txt", false, System.Text.Encoding.UTF8);
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }

            EditorApplication.LockReloadAssemblies();
            List<IResBuilderEx> allExBuilders = new List<IResBuilderEx>(ResBuilderEx);
            if (runOnceExBuilder != null)
            {
                allExBuilders.AddRange(runOnceExBuilder);
            }
            System.Collections.Concurrent.ConcurrentQueue<string> threadedLogs = new System.Collections.Concurrent.ConcurrentQueue<string>();
            int mainThreadLogScheduled = 0;
            Application.LogCallback LogToFile = (message, stack, logtype) =>
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
                Application.logMessageReceivedThreaded += LogToFile;
            }
            var progPrepare = BuilderImp.Prepare(outputDir);
            for (int i = 0; i < allExBuilders.Count; ++i)
            {
                allExBuilders[i].Prepare(outputDir);
            }
            bool cleanupDone = false;
            Action BuilderCleanup = () =>
            {
                if (!cleanupDone)
                {
                    logger.Log("(Phase) Build Res Cleaup.");
                    cleanupDone = true;
                    for (int i = 0; i < allExBuilders.Count; ++i)
                    {
                        allExBuilders[i].Cleanup();
                    }
                    logger.Log("(Done) Build Res Cleaup.");
                    if (swlog != null)
                    {
                        Application.logMessageReceivedThreaded -= LogToFile;
                        swlog.Flush();
                        swlog.Dispose();

                        if (isDefaultBuild)
                        {
                            var logdir = "EditorOutput/Build/" + timetoken + (version > 0 ? ("_" + version) : "") + "/log/";
                            System.IO.Directory.CreateDirectory(logdir);
                            System.IO.File.Copy(outputDir + "/log/ResBuildLog.txt", logdir + "ResBuildLog.txt", true);
                        }
                    }
                    if (shouldCreateBuildingParams)
                    {
                        BuildingParams = null;
                    }
                    EditorApplication.UnlockReloadAssemblies();
                }
            };
            if (winprog != null) winprog.OnQuit += BuilderCleanup;

            try
            {
                logger.Log("(Start) Build Res.");
                if (winprog != null && AsyncWorkTimer.Check()) yield return null;

                //logger.Log("(Phase) Ex Full Build System.");
                //for (int i = 0; i < allExBuilders.Count; ++i)
                //{
                //    IEnumerator exwork = allExBuilders[i].CustomBuild();
                //    if (exwork != null)
                //    {
                //        while (exwork.MoveNext())
                //        {
                //            if (winprog != null)
                //            {
                //                yield return exwork.Current;
                //            }
                //        }
                //    }
                //    if (winprog != null && AsyncWorkTimer.Check()) yield return null;
                //}

                // Generate Build Work
                Dictionary<string, IResBuildWork> works = new Dictionary<string, IResBuildWork>();
                var work = GenerateBuildWorkAsync(works, assets, winprog, runOnceExBuilder);
                while (work.MoveNext())
                {
                    if (winprog != null)
                    {
                        yield return work.Current;
                    }
                }

                logger.Log("(Phase) Write Manifest.");
                var managermod = ModEditorUtils.__MOD__;
                var manidir = "Assets/Mods/" + managermod + "/Build/";
                System.IO.Directory.CreateDirectory(manidir);
                List<IBundleBuildInfo> listManiBuilds = new List<IBundleBuildInfo>();
                foreach (var kvp in works)
                {
                    foreach (var mani in kvp.Value.Manifests)
                    {
                        var mod = mani.MFlag;
                        var dist = mani.DFlag;
                        if (winprog != null && AsyncWorkTimer.Check()) yield return null;
                        logger.Log("Mod " + mod + "; Dist " + dist);

                        var dmani = ResManifest.Save(mani);
                        var filename = "m-" + mod + "-d-" + dist;
                        var manipath = manidir + filename + ".m.asset";
                        AssetDatabase.CreateAsset(dmani, manipath);

                        var bundleInfo = BuilderImp.CreateBundleBuildInfo();
                        bundleInfo.BundleName = filename + ".m" + BuilderImp.BundleExtName;
                        bundleInfo.Assets = new[] { manipath };
                        listManiBuilds.Add(bundleInfo);
                    }
                }
                IResBuildWork maniBuildWork = BuilderImp.CreateWork();
                maniBuildWork.BundleBuildInfos = listManiBuilds.ToArray();
                maniBuildWork.Phase = "manifest";

                logger.Log("(Phase) Wait For Prepare.");
                if (winprog != null)
                {
                    while (!progPrepare.Done)
                    {
                        yield return null;
                    }
                }
                else
                {
                    while (!progPrepare.Done)
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                }


                logger.Log("(Phase) Build Manifest.");
                if (winprog != null && AsyncWorkTimer.Check()) yield return null;
                var outmanidir = outputDir + "/res/mani";
                System.IO.Directory.CreateDirectory(outmanidir);
                maniBuildWork.OutputDir = outmanidir;
                BuilderImp.Build(maniBuildWork);

                logger.Log("(Phase) Delete Unused Manifest.");
                if (winprog != null && AsyncWorkTimer.Check()) yield return null;
                BuilderImp.Cleanup(maniBuildWork);

                logger.Log("(Phase) Real Build.");
                foreach (var kvp in works)
                {
                    var mod = kvp.Key;
                    var buildwork = kvp.Value;
                    logger.Log("Mod " + mod);
                    if (winprog != null && AsyncWorkTimer.Check()) yield return null;

                    var dest = outputDir + "/res";
                    if (!string.IsNullOrEmpty(mod))
                    {
                        dest += "/mod/" + mod;
                    }
                    buildwork.OutputDir = dest;
                    System.IO.Directory.CreateDirectory(dest);

                    // delete old force-refresh bundles
                    buildwork.Phase = "force";
                    BuilderImp.Cleanup(buildwork);

                    // Fire Build!
                    buildwork.Phase = "res";
                    BuilderImp.Build(buildwork);
                    for (int i = 0; i < allExBuilders.Count; ++i)
                    {
                        allExBuilders[i].PostBuildWork(mod, kvp.Value, dest);
                    }
                }

                logger.Log("(Phase) Delete Mod Folder Not Built.");
                var outmoddir = outputDir + "/res/mod/";
                if (System.IO.Directory.Exists(outmoddir))
                {
                    var builtMods = new HashSet<string>(works.Keys);
                    var allModFolders = System.IO.Directory.GetDirectories(outmoddir);
                    int deletedModFolderCnt = 0;
                    for (int i = 0; i < allModFolders.Length; ++i)
                    {
                        if (winprog != null && AsyncWorkTimer.Check()) yield return null;
                        var modfolder = allModFolders[i];
                        logger.Log(modfolder);
                        var mod = modfolder.Substring(outmoddir.Length);
                        if (!builtMods.Contains(mod))
                        {
                            System.IO.Directory.Delete(modfolder, true);
                            ++deletedModFolderCnt;
                        }
                    }
                    if (deletedModFolderCnt == allModFolders.Length)
                    {
                        System.IO.Directory.Delete(outmoddir, true);
                    }
                }

                if (isDefaultBuild)
                {
                    logger.Log("(Phase) Write Version.");
                    var outverdir = outputDir + "/res/version.txt";
                    System.IO.File.WriteAllText(outverdir, version.ToString());
                    // Make icon
                    IconMaker.SetFolderIconToText(outputDir, version.ToString());
                    IconMaker.SetFolderIconToText(outputDir + "/res", version.ToString());
                }

                logger.Log("(Phase) Copy.");
                var outresdir = outputDir + "/res/";
                var allbuildfiles = PlatDependant.GetAllFiles(outresdir);
                if (System.IO.Directory.Exists("Assets/StreamingAssets/res/"))
                {
                    logger.Log("Delete old.");
                    var allexistfiles = PlatDependant.GetAllFiles("Assets/StreamingAssets/res/");
                    for (int i = 0; i < allexistfiles.Length; ++i)
                    {
                        if (winprog != null && AsyncWorkTimer.Check()) yield return null;
                        PlatDependant.DeleteFile(allexistfiles[i]);
                    }
                }
                HashSet<string> nocopyfiles = new HashSet<string>()
                {
                    "icon.png",
                    "icon.ico",
                    "desktop.ini",
                    "Icon\r",
                };
                for (int i = 0; i < allbuildfiles.Length; ++i)
                {
                    if (winprog != null && AsyncWorkTimer.Check()) yield return null;
                    var srcfile = allbuildfiles[i];
                    if (srcfile.EndsWith(".DS_Store"))
                    {
                        continue;
                    }
                    var part = srcfile.Substring(outresdir.Length);
                    if (nocopyfiles.Contains(part))
                    {
                        continue;
                    }
                    logger.Log(part);
                    var destfile = "Assets/StreamingAssets/res/" + part;
                    PlatDependant.CreateFolder(System.IO.Path.GetDirectoryName(destfile));
                    System.IO.File.Copy(srcfile, destfile);
                }

                if (System.IO.Directory.Exists("Assets/StreamingAssets/res/mod/"))
                {
                    logger.Log("(Phase) Delete StreamingAssets Mod Folder Not Built.");
                    var builtMods = new HashSet<string>(works.Keys);
                    var allModFolders = System.IO.Directory.GetDirectories("Assets/StreamingAssets/res/mod/");
                    int deletedModFolderCnt = 0;
                    for (int i = 0; i < allModFolders.Length; ++i)
                    {
                        if (winprog != null && AsyncWorkTimer.Check()) yield return null;
                        var modfolder = allModFolders[i];
                        logger.Log(modfolder);
                        var mod = modfolder.Substring("Assets/StreamingAssets/res/mod/".Length);
                        if (!builtMods.Contains(mod))
                        {
                            System.IO.Directory.Delete(modfolder, true);
                            ++deletedModFolderCnt;
                        }
                    }
                    if (deletedModFolderCnt == allModFolders.Length)
                    {
                        System.IO.Directory.Delete("Assets/StreamingAssets/res/mod/", true);
                    }
                }

                // Clearup useless files.
                var cleanupwork = BuilderImp.CreateWork();
                cleanupwork.Phase = "res";
                cleanupwork.OutputDir = "Assets/StreamingAssets/res";
                BuilderImp.Cleanup(cleanupwork);

                if (isDefaultBuild && makezip)
                {
                    work = ZipBuiltResAsync(winprog, timetoken);
                    while (work.MoveNext())
                    {
                        if (winprog != null)
                        {
                            yield return work.Current;
                        }
                    }
                }

                for (int i = 0; i < allExBuilders.Count; ++i)
                {
                    allExBuilders[i].OnSuccess();
                }
            }
            finally
            {
                BuilderCleanup();
                logger.Log("(Done) Build Res.");
            }
        }

        #region Build Infos And Params
        public static string FormatBundleName(string asset, string mod, string dist, string norm)
        {
            System.Text.StringBuilder sbbundle = new System.Text.StringBuilder();
            sbbundle.Append("m-");
            sbbundle.Append(mod ?? "");
            sbbundle.Append("-d-");
            sbbundle.Append(dist ?? "");
            sbbundle.Append("-");
            sbbundle.Append(System.IO.Path.GetDirectoryName(norm));
            sbbundle.Replace('\\', '-');
            sbbundle.Replace('/', '-');
            if (norm.EndsWith(".unity"))
            {
                sbbundle.Append("-");
                sbbundle.Append(System.IO.Path.GetFileNameWithoutExtension(norm));
                sbbundle.Append(".s");
            }
            else if (norm.EndsWith(".prefab"))
            {
                sbbundle.Append(".o");
            }
            sbbundle.Append(BuilderImp.BundleExtName);
            return sbbundle.ToString();
        }

        public static bool IsBundleInModAndDist(string bundle, string mod, string dist)
        {
            var mdstr = "m-" + (mod ?? "") + "-d-" + (dist ?? "");
            var keypre = mdstr + "-";
            var keypost = BuilderImp.BundleExtName + "." + mdstr;
            bundle = bundle ?? "";
            return bundle.StartsWith(keypre, StringComparison.InvariantCultureIgnoreCase) || bundle.EndsWith(keypost, StringComparison.InvariantCultureIgnoreCase);
        }

        public static int GetResVersion()
        {
            int version = 0;
            if (BuildingParams != null)
            {
                version = BuildingParams.version;
            }
            if (version <= 0)
            {
                int lastBuildVersion = 0;
                int streamingVersion = 0;
                var outverdir = "EditorOutput/Build/Latest/res/version.txt";

                if (System.IO.File.Exists("Assets/StreamingAssets/res/version.txt"))
                {
                    var lines = System.IO.File.ReadAllLines("Assets/StreamingAssets/res/version.txt");
                    if (lines != null && lines.Length > 0)
                    {
                        int.TryParse(lines[0], out streamingVersion);
                    }
                }
                if (System.IO.File.Exists(outverdir))
                {
                    var lines = System.IO.File.ReadAllLines(outverdir);
                    if (lines != null && lines.Length > 0)
                    {
                        int.TryParse(lines[0], out lastBuildVersion);
                    }
                }
                if (streamingVersion > 0 || lastBuildVersion <= 0)
                {
                    version = Math.Max(lastBuildVersion, streamingVersion) + 10;
                }
                else
                {
                    version = lastBuildVersion;
                }

                if (BuildingParams != null)
                {
                    BuildingParams.version = version;
                }
            }
            return version;
        }
        #endregion
        #endregion

        #region Utils
        public static string[] GetFilesRelative(string root)
        {
            if (!System.IO.Directory.Exists(root))
            {
                return null;
            }
            if (!root.EndsWith("/") && !root.EndsWith("\\"))
            {
                root += "/";
            }
            List<string> results = new List<string>();
            var files = PlatDependant.GetAllFiles(root);
            if (files != null)
            {
                foreach (var file in files)
                {
                    var item = file.Substring(root.Length);
                    results.Add(item);
                }
            }
            return results.ToArray();
        }
        private static char[] DirSplitChars = new[] { '\\', '/' };
        public static string[] GetFilesIncludingZipEntries(string root, int ignoreZipEntryDirLevel)
        {
            if (!System.IO.Directory.Exists(root))
            {
                return null;
            }
            if (!root.EndsWith("/") && !root.EndsWith("\\"))
            {
                root += "/";
            }
            List<string> results = new List<string>();
            var files = PlatDependant.GetAllFiles(root);
            if (files != null)
            {
                foreach (var file in files)
                {
                    if (file.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var dir = System.IO.Path.GetDirectoryName(file);
                        dir = dir.Substring(root.Length);
                        dir = dir.Replace("\\", "/");
                        if (!dir.EndsWith("/"))
                        {
                            dir += "/";
                        }
                        try
                        {
                            using (var stream = PlatDependant.OpenRead(file))
                            {
                                using (var zip = new ZipArchive(stream, ZipArchiveMode.Read))
                                {
                                    var entries = zip.Entries;
                                    foreach (var entry in entries)
                                    {
                                        var ename = entry.FullName;
                                        var rname = ename;
                                        for (int i = 0; i < ignoreZipEntryDirLevel; ++i)
                                        {
                                            var index = rname.IndexOfAny(DirSplitChars);
                                            if (index >= 0)
                                            {
                                                rname = rname.Substring(index + 1);
                                            }
                                            else
                                            {
                                                rname = null;
                                                break;
                                            }
                                        }
                                        if (!string.IsNullOrEmpty(rname))
                                        {
                                            var item = dir + rname;
                                            results.Add(item);
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                    else
                    {
                        var item = file.Substring(root.Length);
                        results.Add(item);
                    }
                }
            }
            return results.ToArray();
        }
        public static void CopyMissing(string src, string dest, int ignoreZipEntryDirLevel, string[] ignoredItems)
        {
            if (!System.IO.Directory.Exists(src))
            {
                return;
            }
            if (!src.EndsWith("/") && !src.EndsWith("\\"))
            {
                src += "/";
            }
            System.IO.Directory.CreateDirectory(dest);
            if (!System.IO.Directory.Exists(dest))
            {
                return;
            }
            if (!dest.EndsWith("/") && !dest.EndsWith("\\"))
            {
                dest += "/";
            }
            var existing = GetFilesIncludingZipEntries(dest, ignoreZipEntryDirLevel);
            HashSet<string> existingset = new HashSet<string>();
            if (existing != null)
            {
                foreach (var item in existing)
                {
                    existingset.Add(item.ToLower());
                }
            }
            HashSet<string> blackset = new HashSet<string>();
            if (ignoredItems != null)
            {
                foreach (var item in ignoredItems)
                {
                    blackset.Add(item.ToLower());
                }
            }
            var srcitems = GetFilesRelative(src);
            if (srcitems != null)
            {
                foreach (var item in srcitems)
                {
                    var litem = item.ToLower();
                    if (!existingset.Contains(litem))
                    {
                        PlatDependant.CopyFile(src + item, dest + item);
                    }
                }
            }
        }

        public static void CopyOrUnzip(string src, string dest, int ignoreZipEntryDirLevel, string[] ignoredItems)
        {
            if (!System.IO.Directory.Exists(src))
            {
                return;
            }
            if (!src.EndsWith("/") && !src.EndsWith("\\"))
            {
                src += "/";
            }
            System.IO.Directory.CreateDirectory(dest);
            if (!System.IO.Directory.Exists(dest))
            {
                return;
            }
            if (!dest.EndsWith("/") && !dest.EndsWith("\\"))
            {
                dest += "/";
            }
            var files = PlatDependant.GetAllFiles(src);
            if (files != null)
            {
                foreach (var file in files)
                {
                    if (file.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var dir = System.IO.Path.GetDirectoryName(file);
                        dir = dir.Substring(src.Length);
                        dir = dir.Replace("\\", "/");
                        if (!dir.EndsWith("/"))
                        {
                            dir += "/";
                        }
                        try
                        {
                            using (var stream = PlatDependant.OpenRead(file))
                            {
                                using (var zip = new ZipArchive(stream, ZipArchiveMode.Read))
                                {
                                    var entries = zip.Entries;
                                    foreach (var entry in entries)
                                    {
                                        var ename = entry.FullName;
                                        var rname = ename;
                                        for (int i = 0; i < ignoreZipEntryDirLevel; ++i)
                                        {
                                            var index = rname.IndexOfAny(DirSplitChars);
                                            if (index >= 0)
                                            {
                                                rname = rname.Substring(index + 1);
                                            }
                                            else
                                            {
                                                rname = null;
                                                break;
                                            }
                                        }
                                        if (!string.IsNullOrEmpty(rname))
                                        {
                                            try
                                            {
                                                var item = dir + rname;
                                                var destfile = dest + item;
                                                using (var estream = entry.Open())
                                                {
                                                    using (var dstream = PlatDependant.OpenWrite(destfile))
                                                    {
                                                        estream.CopyTo(dstream);
                                                    }
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                Debug.LogException(e);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                    else
                    {
                        var item = file.Substring(src.Length);
                        var destfile = dest + item;
                        PlatDependant.CopyFile(file, destfile);
                    }
                }
            }
        }

        public static void CopyMissingBuiltFilesToArchiveFolder(string dest, string[] ignoredItems)
        {
            if (!System.IO.Directory.Exists(dest))
            {
                return;
            }
            CopyMissing("EditorOutput/Build/Latest/", dest, 1, ignoredItems);
        }
        public static void RestoreFromArchiveFolder(string src, string[] ignoredItems)
        {
            CopyOrUnzip(src, "EditorOutput/Build/Latest/", 1, ignoredItems);
            var dirs = PlatDependant.GetAllFolders("EditorOutput/Build/Latest/");
            foreach (var dir in dirs)
            {
                IconMaker.FixIcon(dir);
            }
            IconMaker.FixIcon("EditorOutput/Build/Latest/");
        }
        public static IEnumerator ZipBuiltResAsync(IEditorWorkProgressShower winprog, string timetoken)
        {
            if (string.IsNullOrEmpty(timetoken))
            {
                timetoken = ResBuilderParams.Create().timetoken;
            }
            var outputDir = "EditorOutput/Build/Latest";
            var logger = new EditorWorkProgressLogger() { Shower = winprog };
            logger.Log("(Phase) Zip.");
            // parse version
            int version = 0;
            if (System.IO.File.Exists(outputDir + "/res/version.txt"))
            {
                foreach (var line in System.IO.File.ReadLines(outputDir + "/res/version.txt"))
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        if (int.TryParse(line, out version))
                        {
                            break;
                        }
                    }
                }
            }
            if (version > 0)
            {
                timetoken = timetoken + "_" + version;
            }
            var outzipdir = "EditorOutput/Build/" + timetoken + "/whole/res/";
            System.IO.Directory.CreateDirectory(outzipdir);
            List<Pack<string, string, IList<string>>> zips = new List<Pack<string, string, IList<string>>>();
            var builtGroups = BuilderImp.GroupBuiltFiles(outputDir);
            foreach (var kvp in builtGroups)
            {
                var reskey = kvp.Key;
                logger.Log(reskey);
                var entries = kvp.Value;
                var zipfile = outzipdir + reskey + ".zip";
                zips.Add(new Pack<string, string, IList<string>>(zipfile, outputDir, entries));
            }
            if (zips.Count > 0)
            {
                var workz = ResBuilder.MakeZipsBackground(zips, winprog);
                while (workz.MoveNext())
                {
                    if (winprog != null)
                    {
                        yield return workz.Current;
                    }
                }
            }

            CopyMissingBuiltFilesToArchiveFolder("EditorOutput/Build/" + timetoken + "/whole/", null);

            // Make icon
            IconMaker.SetFolderIconToFileContent("EditorOutput/Build/" + timetoken, outputDir + "/res/version.txt");
        }
        public static void RestoreStreamingAssetsFromLatestBuild()
        {
            var srcroot = "EditorOutput/Build/Latest/res/";
            var dstroot = "Assets/StreamingAssets/res/";

            if (System.IO.Directory.Exists(srcroot))
            {
                if (System.IO.Directory.Exists(dstroot))
                {
                    System.IO.Directory.Delete(dstroot, true);
                }
                System.IO.Directory.CreateDirectory(dstroot);

                HashSet<string> nocopyfiles = new HashSet<string>()
                {
                    "icon.png",
                    "icon.ico",
                    "desktop.ini",
                    "Icon\r",
                };
                var allbuildfiles = PlatDependant.GetAllFiles(srcroot);
                for (int i = 0; i < allbuildfiles.Length; ++i)
                {
                    var srcfile = allbuildfiles[i];
                    if (srcfile.EndsWith(".DS_Store"))
                    {
                        continue;
                    }
                    var part = srcfile.Substring(srcroot.Length);
                    if (nocopyfiles.Contains(part))
                    {
                        continue;
                    }
                    var destfile = dstroot + part;
                    PlatDependant.CreateFolder(System.IO.Path.GetDirectoryName(destfile));
                    System.IO.File.Copy(srcfile, destfile);
                }

                // Clearup useless files.
                var cleanupwork = BuilderImp.CreateWork();
                cleanupwork.Phase = "res";
                cleanupwork.OutputDir = "Assets/StreamingAssets/res";
                BuilderImp.Cleanup(cleanupwork);

                List<IResBuilderEx> allExBuilders = ResBuilderEx;
                for (int i = 0; i < allExBuilders.Count; ++i)
                {
                    allExBuilders[i].Cleanup();
                }
            }
        }
        #endregion
    }
}
