using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngineEx;
using LuaLib;

namespace UnityEditorEx
{
    public static class SptBuilder
    {
        public enum BuildScriptResult
        {
            Unknown = 0,
            Fail = 1,
            Success = 2,
            UpToDate = 3,
        }
        public static BuildScriptResult BuildScript(string file, string dest, int arch)
        {
            var srcFileHash = ModEditorUtils.GetFileMD5(file) + "-" + ModEditorUtils.GetFileLength(file);
            var infofile = dest + ".srcinfo";
            if (PlatDependant.IsFileExist(infofile) && PlatDependant.IsFileExist(dest))
            {
                string dstFileHash = "";
                using (var sr = PlatDependant.OpenReadText(infofile))
                {
                    dstFileHash = sr.ReadLine();
                }
                if (!string.IsNullOrEmpty(dstFileHash))
                {
                    if (dstFileHash == srcFileHash + "-" + ModEditorUtils.GetFileMD5(dest) + "-" + ModEditorUtils.GetFileLength(dest))
                    {
                        return BuildScriptResult.UpToDate;
                    }
                }
            }

            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dest));
            System.IO.File.Delete(dest);
            System.IO.File.Delete(infofile);

            bool success = false;
            if (arch == 32)
            {
                success = BuildScriptSub_32(file, dest);
            }
            else
            {
                success = BuildScriptSub_64(file, dest);
            }

            if (success && PlatDependant.IsFileExist(dest))
            {
                using (var sw = PlatDependant.OpenWriteText(infofile))
                {
                    sw.Write(srcFileHash + "-" + ModEditorUtils.GetFileMD5(dest) + "-" + ModEditorUtils.GetFileLength(dest));
                }
                return BuildScriptResult.Success;
            }

            BuildScriptSub_RawCopy(file, dest);
            return BuildScriptResult.Fail;
        }
        private static void BuildScriptSub_RawCopy(string src, string dest)
        {
            // make a as-it-is copy.
            System.IO.File.Copy(src, dest, true);
        }
        private static string _ThisMod;
        private static string ThisMod
        {
            get
            {
                if (_ThisMod == null)
                {
                    _ThisMod = ModEditorUtils.__MOD__;
                }
                return _ThisMod;
            }
        }
        private static string _ToolsDir;
        private static string ToolsDir
        {
            get
            {
                if (_ToolsDir == null)
                {
                    _ToolsDir = System.IO.Path.GetFullPath(ModEditor.GetPackageOrModRoot(ThisMod)) + "/~Tools~/";
                }
                return _ToolsDir;
            }
        }

#if UNITY_EDITOR_OSX
        [ThreadStatic] public static System.Diagnostics.Process BuildSptProc;
#endif
        private static bool BuildScriptSub_64(string src, string dest)
        {
            var filefull = System.IO.Path.GetFullPath(src);
            var destfull = System.IO.Path.GetFullPath(dest);

            string luajitPath = "";
            string workingDirectory = "";
#if UNITY_EDITOR_WIN
            workingDirectory = ToolsDir + "luajit-2.1.0-beta3/x64";
            luajitPath = workingDirectory + "/luajit.exe";
#elif UNITY_EDITOR_OSX
            workingDirectory = ToolsDir + "luajit-2.1.0-beta3/x64";
            luajitPath = workingDirectory + "/luajit";
#endif
            if (!string.IsNullOrEmpty(luajitPath))
            {
                System.Diagnostics.ProcessStartInfo si = new System.Diagnostics.ProcessStartInfo(luajitPath, "-b -s \"" + filefull + "\" \"" + destfull + "\"");
                si.WorkingDirectory = workingDirectory;
#if UNITY_EDITOR_OSX
                if (BuildSptProc != null)
                    return ModEditorUtils.ExecuteProcessInShell(BuildSptProc, si) == 0;
                else
#endif
                return ModEditorUtils.ExecuteProcess(si);
            }
            return false;
        }
        private static bool BuildScriptSub_32(string src, string dest)
        {
            var filefull = System.IO.Path.GetFullPath(src);
            var destfull = System.IO.Path.GetFullPath(dest);

            string luajitPath = "";
            string workingDirectory = "";
#if UNITY_EDITOR_WIN
            workingDirectory = ToolsDir + "luajit-2.1.0-beta3/x86";
            luajitPath = workingDirectory + "/luajit.exe";
#elif UNITY_EDITOR_OSX
            workingDirectory = ToolsDir + "luajit-2.1.0-beta3/x86";
            luajitPath = workingDirectory + "/luajit";
#endif
            if (!string.IsNullOrEmpty(luajitPath))
            {
                System.Diagnostics.ProcessStartInfo si = new System.Diagnostics.ProcessStartInfo(luajitPath, "-b -s \"" + filefull + "\" \"" + destfull + "\"");
                si.WorkingDirectory = workingDirectory;
#if UNITY_EDITOR_OSX
                if (BuildSptProc != null)
                    return ModEditorUtils.ExecuteProcessInShell(BuildSptProc, si) == 0;
                else
#endif
                return ModEditorUtils.ExecuteProcess(si);
            }
            return false;
        }

        public class SptBuildWork
        {
            public struct SptBuildItem
            {
                public string Norm;
                public string Mod;
                public string ModRoot;
                public string PackageName;
                public string Dist;
                public string Dest; // NOTICE: if you use Dest, the Norm will not be modified by Mod&Dist, Norm will be used as Src.

                public string GetSource()
                {
                    if (string.IsNullOrEmpty(Dest))
                    {
                        var name = Norm ?? "";
                        if (!string.IsNullOrEmpty(Dist))
                        {
                            name = "dist/" + Dist + "/" + name;
                        }
                        if (string.IsNullOrEmpty(Mod))
                        {
                            return "Assets/ModSpt/" + name;
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(ModRoot))
                            {
                                return ModRoot + "/ModSpt/" + name;
                            }
                            else
                            {
                                return "Assets/Mods/" + Mod + "/ModSpt/" + name;
                            }
                        }
                    }
                    else
                    {
                        return Norm;
                    }
                }
                public string GetSourceAsset()
                {
                    if (string.IsNullOrEmpty(Dest))
                    {
                        var name = Norm ?? "";
                        if (!string.IsNullOrEmpty(Dist))
                        {
                            name = "dist/" + Dist + "/" + name;
                        }
                        if (string.IsNullOrEmpty(Mod))
                        {
                            return "Assets/ModSpt/" + name;
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(PackageName))
                            {
                                return "Packages/" + PackageName + "/ModSpt/" + name;
                            }
                            else
                            {
                                return "Assets/Mods/" + Mod + "/ModSpt/" + name;
                            }
                        }
                    }
                    else
                    {
                        return Norm;
                    }
                }
                public string GetDest()
                {
                    if (string.IsNullOrEmpty(Dest))
                    {
                        var name = Norm ?? "";
                        if (!string.IsNullOrEmpty(Dist))
                        {
                            name = "dist/" + Dist + "/" + name;
                        }
                        if (string.IsNullOrEmpty(Mod))
                        {
                            return name;
                        }
                        else
                        {
                            return "mod/" + Mod + "/" + name;
                        }
                    }
                    else
                    {
                        return Dest;
                    }
                }
                public string GetDest(int arch)
                {
                    if (string.IsNullOrEmpty(Dest))
                    {
                        var name = Norm ?? "";
                        if (!string.IsNullOrEmpty(Dist))
                        {
                            name = "dist/" + Dist + "/" + name;
                        }
                        if (string.IsNullOrEmpty(Mod))
                        {
                            return "@" + arch + "/" + name;
                        }
                        else
                        {
                            return "@" + arch + "/" + "mod/" + Mod + "/" + name;
                        }
                    }
                    else
                    {
                        return Dest;
                    }
                }
            }

            public readonly List<SptBuildItem> Files = new List<SptBuildItem>();
            public string OutputDir;
            public string OutputExt = ".lua";
            public bool RawCopy = false;

            public Action OnDone = null;

            private int NextFileIndex = 0;
            private int DoneCount = 0;
            private BuildScriptResult[] Results;
            private System.Threading.AutoResetEvent BuildDone = new System.Threading.AutoResetEvent(false);

            public bool IsMultiArchBuild
            {
                get
                {
#if UNITY_ANDROID
                    return true;
#else
                    return false;
#endif
                }
            }
#if UNITY_ANDROID
            private static readonly int[] _MultiBuildArchs = new[] { 32, 64 };
#else
            private static readonly int[] _MultiBuildArchs = new int[0];
#endif
            public int[] MultiBuildArchs
            {
                get { return _MultiBuildArchs; }
            }

            public void StartWork()
            {
                PlatDependant.LogInfo("Start Build Work");
                Results = new BuildScriptResult[Files.Count];
                int cpucnt = System.Environment.ProcessorCount;
                for (int i = 0; i < cpucnt; ++i)
                {
                    PlatDependant.RunBackground(BuildWork);
                }
            }
            public IEnumerator WaitForWorkDone(IEditorWorkProgressShower win)
            {
                if (win == null)
                {
                    int doneindex = 0;
                    int donecnt = 0;
                    while (doneindex < Results.Length)
                    {
                        BuildDone.WaitOne(1000);
                        while (doneindex < Results.Length && Results[doneindex] != BuildScriptResult.Unknown)
                        {
                            Debug.LogFormat("Build Spt Monitor Checking {0}", doneindex);
                            var result = Results[doneindex];
                            var file = Files[doneindex];
                            var mess = file.GetDest() + " : " + result.ToString();
                            if (result == BuildScriptResult.Fail)
                            {
                                Debug.LogError(mess);
                            }
                            else
                            {
                                Debug.Log(mess);
                            }
                            ++doneindex;
                        }
                        donecnt = doneindex;
                        Debug.LogFormat("Build Spt Monotor Done Count {0}", donecnt);
                    }
                }
                else
                {
                    int doneindex = 0;
                    int donecnt = 0;
                    while (doneindex < Results.Length)
                    {
                        while (doneindex < Results.Length && Results[doneindex] != BuildScriptResult.Unknown)
                        {
                            var result = Results[doneindex];
                            var file = Files[doneindex];
                            var mess = file.GetDest() + " : " + result.ToString();
                            if (result == BuildScriptResult.Fail)
                            {
                                win.Message = mess;
                                Debug.LogError(mess);
                            }
                            else
                            {
                                win.Message = mess;
                                Debug.Log(mess);
                            }
                            ++doneindex;
                            if (AsyncWorkTimer.Check()) yield return null;
                        }
                        donecnt = doneindex;
                        yield return null;
                    }
                }
            }

            private void BuildWork(TaskProgress prog)
            {
#if UNITY_EDITOR_OSX
                BuildSptProc = ModEditorUtils.StartShell();
                try
                { 
#endif
                while (true)
                {
                    var index = System.Threading.Interlocked.Increment(ref NextFileIndex) - 1;
                    if (index >= Files.Count)
                    {
                        return;
                    }
                    var item = Files[index];
                    Debug.LogFormat("Start Building {0}/{1}", index, Files.Count);
                    BuildScriptResult result = BuildScriptResult.Fail;
                    try
                    {
                        if (string.IsNullOrEmpty(item.Norm))
                        {
                            result = BuildScriptResult.UpToDate;
                        }
                        else
                        {
                            var src = item.GetSource();
                            if (System.IO.File.Exists(src))
                            {
                                var dst = item.GetDest() ?? "";
                                if (dst.EndsWith(".lua"))
                                {
                                    dst = dst.Substring(0, dst.Length - ".lua".Length);
                                }
                                dst += OutputExt;
                                if (!string.IsNullOrEmpty(OutputDir))
                                {
                                    dst = OutputDir + dst;
                                }
                                else if (string.IsNullOrEmpty(item.Dest))
                                {
                                    dst = "Assets/StreamingAssets/spt/" + dst;
                                }

                                if (RawCopy)
                                {
                                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dst));
                                    System.IO.File.Delete(dst);
                                    System.IO.File.Delete(dst + ".srcinfo");
                                    BuildScriptSub_RawCopy(src, dst);
                                    result = BuildScriptResult.Success;
                                }
                                else
                                {
                                    if (IsMultiArchBuild)
                                    {
                                        var archs = MultiBuildArchs;
                                        int mresult = int.MaxValue;
                                        for (int i = 0; i < archs.Length; ++i)
                                        {
                                            var arch = archs[i];
                                            dst = item.GetDest(arch) ?? "";
                                            if (dst.EndsWith(".lua"))
                                            {
                                                dst = dst.Substring(0, dst.Length - ".lua".Length);
                                            }
                                            dst += OutputExt;
                                            if (!string.IsNullOrEmpty(OutputDir))
                                            {
                                                dst = OutputDir + dst;
                                            }
                                            else if (string.IsNullOrEmpty(item.Dest))
                                            {
                                                dst = "Assets/StreamingAssets/spt/" + dst;
                                            }

                                            var presult = (int)BuildScript(src, dst, arch);
                                            mresult = Math.Min(mresult, presult);
                                        }
                                        result = (BuildScriptResult)mresult;
                                    }
                                    else
                                    {
                                        result = BuildScript(src, dst, 64);
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        Results[index] = result;
                        System.Threading.Interlocked.Increment(ref DoneCount);
                        Debug.Log("Work done count " + DoneCount);
                        BuildDone.Set();
                    }
                }
#if UNITY_EDITOR_OSX
                }
                finally
                { 
                    BuildSptProc.Kill();
                    BuildSptProc = null;
                }
#endif
            }

            public void DeleteNonBuildOldFiles()
            {
                HashSet<string> buildFiles = new HashSet<string>();
                for (int i = 0; i < Files.Count; ++i)
                {
                    var item = Files[i];
                    var dst = item.GetDest() ?? "";
                    if (dst.EndsWith(".lua"))
                    {
                        dst = dst.Substring(0, dst.Length - ".lua".Length);
                    }
                    dst += OutputExt;
                    if (!string.IsNullOrEmpty(OutputDir))
                    {
                        dst = OutputDir + dst;
                    }
                    else if (string.IsNullOrEmpty(item.Dest))
                    {
                        dst = "Assets/StreamingAssets/spt/" + dst;
                    }

                    if (RawCopy || !IsMultiArchBuild)
                    {
                        buildFiles.Add(dst.Replace('\\', '/'));
                    }
                    else
                    {
                        var archs = MultiBuildArchs;
                        int mresult = int.MaxValue;
                        for (int j = 0; j < archs.Length; ++j)
                        {
                            var arch = archs[j];
                            dst = item.GetDest(arch) ?? "";
                            if (dst.EndsWith(".lua"))
                            {
                                dst = dst.Substring(0, dst.Length - ".lua".Length);
                            }
                            dst += OutputExt;
                            if (!string.IsNullOrEmpty(OutputDir))
                            {
                                dst = OutputDir + dst;
                            }
                            else if (string.IsNullOrEmpty(item.Dest))
                            {
                                dst = "Assets/StreamingAssets/spt/" + dst;
                            }

                            buildFiles.Add(dst.Replace('\\', '/'));
                        }
                    }
                }

                string outputdir = OutputDir;
                if (string.IsNullOrEmpty(outputdir))
                {
                    outputdir = "Assets/StreamingAssets/spt/";
                }
                if (System.IO.Directory.Exists(outputdir))
                {
                    var files = PlatDependant.GetAllFiles(outputdir);
                    for (int i = 0; i < files.Length; ++i)
                    {
                        var file = files[i].Replace('\\', '/');
                        if (file.EndsWith(OutputExt) && !buildFiles.Contains(file))
                        {
                            PlatDependant.DeleteFile(file);
                            PlatDependant.DeleteFile(file + ".srcinfo");
                        }
                    }
                }
            }

            public ResManifest[] GenerateManifests()
            {
                Dictionary<string, Dictionary<string, ResManifest>> mod2mani = new Dictionary<string, Dictionary<string, ResManifest>>();
                for (int i = 0; i < Files.Count; ++i)
                {
                    var item = Files[i];
                    if (!string.IsNullOrEmpty(item.Norm))
                    {
                        var mod = item.Mod ?? "";
                        var dist = item.Dist ?? "";

                        Dictionary<string, ResManifest> manis;
                        if (!mod2mani.TryGetValue(mod, out manis))
                        {
                            manis = new Dictionary<string, ResManifest>();
                            mod2mani[mod] = manis;
                        }
                        ResManifest mani;
                        if (!manis.TryGetValue(dist, out mani))
                        {
                            mani = new ResManifest();
                            mani.MFlag = mod;
                            mani.DFlag = dist;
                            manis[dist] = mani;
                        }
                        mani.AddOrGetItem(item.GetSourceAsset());
                    }
                }
                List<ResManifest> result = new List<ResManifest>();
                foreach (var mmani in mod2mani)
                {
                    result.AddRange(mmani.Value.Values);
                }
                return result.ToArray();
            }
        }
        public interface ISptBuilderEx
        {
            void PreGenerateBuildWork(SptBuildWork buildwork);
            void ModifyBuildWork(SptBuildWork buildwork);
        }
        public static readonly List<ISptBuilderEx> SptBuilderEx = new List<ISptBuilderEx>();
        public class SptBuilderEx_RawCopy : ISptBuilderEx
        {
            public void PreGenerateBuildWork(SptBuildWork buildwork)
            {
                buildwork.RawCopy = true;
            }
            public void ModifyBuildWork(SptBuildWork buildwork)
            {
            }
        }

        public static IEnumerator GenerateBuildWorkAsync(SptBuildWork result, IList<string> scripts, IEditorWorkProgressShower winprog)
        {
            return GenerateBuildWorkAsync(result, scripts, winprog, null);
        }
        public static IEnumerator GenerateBuildWorkAsync(SptBuildWork result, IList<string> scripts, IEditorWorkProgressShower winprog, IList<ISptBuilderEx> runOnceBuilderEx)
        {
            List<ISptBuilderEx> allBuilderEx = new List<ISptBuilderEx>(SptBuilderEx);
            if (runOnceBuilderEx != null)
            {
                allBuilderEx.AddRange(runOnceBuilderEx);
            }

            var logger = new EditorWorkProgressLogger() { Shower = winprog };
            logger.Log("(Start) Generate Spt Build Work.");
            if (winprog != null && AsyncWorkTimer.Check()) yield return null;

            if (result == null)
            {
                logger.Log("(Error) You have to provide container to retrive the result.");
                yield break;
            }

            for (int j = 0; j < allBuilderEx.Count; ++j)
            {
                allBuilderEx[j].PreGenerateBuildWork(result);
            }

            result.Files.Clear();

            if (scripts == null)
            {
                logger.Log("(Option) Get All Scripts.");
                scripts = AssetDatabase.GetAllAssetPaths();
                if (winprog != null && AsyncWorkTimer.Check()) yield return null;
            }

            if (scripts != null)
            {
                Dictionary<string, SptBuildWork.SptBuildItem> workitems = new Dictionary<string, SptBuildWork.SptBuildItem>();
                for (int i = 0; i < scripts.Count; ++i)
                {
                    if (winprog != null && AsyncWorkTimer.Check()) yield return null;
                    var script = scripts[i];
                    if (!script.EndsWith(".lua"))
                    {
                        continue;
                    }
                    logger.Log(script);

                    if (!System.IO.File.Exists(script))
                    {
                        logger.Log("Not Exist.");
                        continue;
                    }

                    string mod = null;
                    string dist = null;
                    string norm = script;
                    string package = null;
                    if (script.StartsWith("Assets/Mods/"))
                    {
                        var sub = script.Substring("Assets/Mods/".Length);
                        var index = sub.IndexOf('/');
                        if (index < 0)
                        {
                            logger.Log("Cannot Parse Module.");
                            continue;
                        }
                        mod = sub.Substring(0, index);
                        if (string.IsNullOrEmpty(mod))
                        {
                            logger.Log("Empty Module.");
                            continue;
                        }
                        sub = sub.Substring(index + 1);
                        if (!sub.StartsWith("ModSpt/"))
                        {
                            logger.Log("Should Ignore This Script.");
                            continue;
                        }

                        sub = sub.Substring("ModSpt/".Length);
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
                    else if (script.StartsWith("Packages/"))
                    {
                        var sub = script.Substring("Packages/".Length);
                        var index = sub.IndexOf('/');
                        if (index < 0)
                        {
                            logger.Log("Cannot Parse Package.");
                            continue;
                        }
                        package = sub.Substring(0, index);
                        if (string.IsNullOrEmpty(package))
                        {
                            logger.Log("Empty Package Name.");
                            continue;
                        }
                        sub = sub.Substring(index + 1);
                        if (!sub.StartsWith("ModSpt/"))
                        {
                            logger.Log("Should Ignore This Script.");
                            continue;
                        }

                        sub = sub.Substring("ModSpt/".Length);
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
                        if (script.StartsWith("Assets/ModSpt/"))
                        {
                            mod = "";
                            var sub = script.Substring("Assets/ModSpt/".Length);
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
                            logger.Log("Should Ignore This Script.");
                            continue;
                        }
                    }

                    if (string.IsNullOrEmpty(norm))
                    {
                        logger.Log("Normallized Path Empty.");
                        continue;
                    }
                    mod = mod ?? "";
                    dist = dist ?? "";
                    if (package == null)
                    {
                        logger.Log("Mod " + mod + "; Dist " + dist + "; Norm " + norm);
                    }
                    else
                    {
                        mod = ModEditor.GetPackageModName(package) ?? "";
                        logger.Log("Package " + package + "; Mod " + mod + "; Dist " + dist + "; Norm " + norm);
                    }

                    SptBuildWork.SptBuildItem item = new SptBuildWork.SptBuildItem()
                    {
                        Norm = norm,
                        Mod = mod,
                        Dist = dist,
                    };
                    if (package != null)
                    {
                        item.PackageName = package;
                        item.ModRoot = ModEditor.GetPackageRoot(package);
                    }

                    var dst = item.GetDest();
                    if (!workitems.ContainsKey(dst) || !string.IsNullOrEmpty(item.Mod) && string.IsNullOrEmpty(workitems[dst].PackageName))
                    {
                        workitems[dst] = item;
                    }
                }

                result.Files.AddRange(workitems.Values);
            }

            for (int j = 0; j < allBuilderEx.Count; ++j)
            {
                allBuilderEx[j].ModifyBuildWork(result);
            }
            logger.Log("(Done) Generate Spt Build Work.");
        }

        public static IEnumerator BuildSptAsync(IList<string> scripts, IEditorWorkProgressShower winprog)
        {
            return BuildSptAsync(scripts, winprog, null);
        }
        public static IEnumerator BuildSptAsync(IList<string> scripts, IEditorWorkProgressShower winprog, IList<ISptBuilderEx> runOnceBuilderEx)
        {
            bool isDefaultBuild = scripts == null;
            var logger = new EditorWorkProgressLogger() { Shower = winprog };
            bool shouldCreateBuildingParams = ResBuilder.BuildingParams == null;
            ResBuilder.BuildingParams = ResBuilder.BuildingParams ?? ResBuilder.ResBuilderParams.Create();
            var timetoken = ResBuilder.BuildingParams.timetoken;
            var makezip = ResBuilder.BuildingParams.makezip;
            int version = 0;
            if (isDefaultBuild)
            { // parse version
                var outverdir = "EditorOutput/Build/Latest/spt/version.txt";
                if (ResBuilder.BuildingParams != null && ResBuilder.BuildingParams.version > 0)
                {
                    version = ResBuilder.BuildingParams.version;
                }
                else
                {
                    version = ResBuilder.GetResVersion();

                    int lastBuildVersion = 0;
                    int streamingVersion = 0;

                    if (System.IO.File.Exists("Assets/StreamingAssets/spt/version.txt"))
                    {
                        var lines = System.IO.File.ReadAllLines("Assets/StreamingAssets/spt/version.txt");
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
                        int maxver = Math.Max(lastBuildVersion, streamingVersion);
                        if (maxver >= version)
                        {
                            version = maxver + 10;
                        }
                    }
                    else
                    {
                        version = lastBuildVersion;
                    }
                    ResBuilder.BuildingParams.version = version;
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
                swlog = new System.IO.StreamWriter(outputDir + "/log/SptBuildLog.txt", false, System.Text.Encoding.UTF8);
            }
            catch (Exception e)
            {
                Debug.Log(e);
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
            bool cleanupDone = false;
            Action BuilderCleanup = () =>
            {
                if (!cleanupDone)
                {
                    logger.Log("(Phase) Build Spt Cleaup.");
                    cleanupDone = true;
                    logger.Log("(Done) Build Spt Cleaup.");
                    if (swlog != null)
                    {
                        Application.logMessageReceivedThreaded -= LogToFile;
                        swlog.Flush();
                        swlog.Dispose();

                        if (isDefaultBuild)
                        {
                            var logdir = "EditorOutput/Build/" + timetoken + (version > 0 ? ("_" + version) : "") + "/log/";
                            System.IO.Directory.CreateDirectory(logdir);
                            System.IO.File.Copy(outputDir + "/log/SptBuildLog.txt", logdir + "SptBuildLog.txt", true);
                        }
                    }
                    if (shouldCreateBuildingParams)
                    {
                        ResBuilder.BuildingParams = null;
                    }
                }
            };
            if (winprog != null) winprog.OnQuit += BuilderCleanup;

            try
            {
                logger.Log("(Start) Build Spt.");
                if (winprog != null && AsyncWorkTimer.Check()) yield return null;

                // Generate Build Work
                SptBuildWork buildwork = new SptBuildWork();
                buildwork.OutputDir = outputDir + "/spt/";
                var work = GenerateBuildWorkAsync(buildwork, scripts, winprog, runOnceBuilderEx);
                while (work.MoveNext())
                {
                    if (winprog != null)
                    {
                        yield return work.Current;
                    }
                }
                // Fire Build Work
                logger.Log("(Start) Run Build Spt (On ThreadPool)!");
                buildwork.StartWork();

                //// Maybe we donot need the manifest, we can build one on first startup.
                //// We can save the runtime manifest in json file, but we should test the speed of loading the manifest from json file.
                //// If it is too slow to load the manifest from json file, maybe we need this again.
                //// Or we should find the file at runtime (as we do before).
                //logger.Log("(Phase) Write Spt Manifest.");
                //var managermod = ModEditorUtils.__MOD__;
                //var manidir = "Assets/Mods/" + managermod + "/Build/";
                //System.IO.Directory.CreateDirectory(manidir);
                //List<AssetBundleBuild> listManiBuilds = new List<AssetBundleBuild>();
                //HashSet<string> maniFileNames = new HashSet<string>();
                //foreach (var kvp in works)
                //{
                //    var mod = kvp.Key;
                //    foreach (var mani in kvp.Value.Manifests)
                //    {
                //        var dist = mani.DFlag;
                //        if (winprog != null && AsyncWorkTimer.Check()) yield return null;
                //        logger.Log("Mod " + mod + "; Dist " + dist);

                //        var dmani = ResManifest.Save(mani);
                //        var filename = "m-" + mod + "-d-" + dist;
                //        var manipath = manidir + filename + ".m.asset";
                //        AssetDatabase.CreateAsset(dmani, manipath);

                //        maniFileNames.Add(filename.ToLower());
                //        listManiBuilds.Add(new AssetBundleBuild() { assetBundleName = filename + ".m.ab", assetNames = new[] { manipath } });
                //    }
                //}

                //logger.Log("(Phase) Build Manifest.");
                //if (winprog != null && AsyncWorkTimer.Check()) yield return null;
                //var buildopt = BuildAssetBundleOptions.DeterministicAssetBundle | BuildAssetBundleOptions.ChunkBasedCompression;
                //BuildTarget buildtar = EditorUserBuildSettings.activeBuildTarget;
                //var outmanidir = outputDir + "/res/mani";
                //System.IO.Directory.CreateDirectory(outmanidir);
                //BuildPipeline.BuildAssetBundles(outmanidir, listManiBuilds.ToArray(), buildopt, buildtar);

                //logger.Log("(Phase) Delete Unused Manifest.");
                //if (winprog != null && AsyncWorkTimer.Check()) yield return null;
                //var manifiles = PlatDependant.GetAllFiles(outmanidir);
                //for (int i = 0; i < manifiles.Length; ++i)
                //{
                //    var file = manifiles[i];
                //    if (file.EndsWith(".m.ab"))
                //    {
                //        var filename = file.Substring(outmanidir.Length + 1, file.Length - outmanidir.Length - 1 - ".m.ab".Length);
                //        if (!maniFileNames.Contains(filename))
                //        {
                //            PlatDependant.DeleteFile(file);
                //            PlatDependant.DeleteFile(file + ".manifest");
                //        }
                //    }
                //}

                logger.Log("(Phase) Delete Old Build Whose Source File Has Been Deleted.");
                buildwork.DeleteNonBuildOldFiles();

                logger.Log("(Phase) Delete Mod Folder Not Built.");
                var builtMods = new HashSet<string>();
                for (int i = 0; i < buildwork.Files.Count; ++i)
                {
                    var mod = buildwork.Files[i].Mod;
                    builtMods.Add(mod ?? "");
                }
                List<string> dstsptRoots = new List<string>();
                if (buildwork.RawCopy || !buildwork.IsMultiArchBuild)
                {
                    dstsptRoots.Add("/spt/");
                    if (System.IO.Directory.Exists(outputDir + "/spt/"))
                    {
                        var subsptfolders = System.IO.Directory.GetDirectories(outputDir + "/spt/");
                        if (subsptfolders != null)
                        {
                            for (int i = 0; i < subsptfolders.Length; ++i)
                            {
                                var subfolder = subsptfolders[i];
                                if (subfolder.StartsWith(outputDir + "/spt/@", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    var sub = subfolder.Substring(outputDir.Length + "/spt/@".Length);
                                    var index = sub.IndexOfAny(new[] { '/', '\\' });
                                    if (index > 0)
                                    {
                                        sub = sub.Substring(0, index);
                                    }
                                    int arch;
                                    if (int.TryParse(sub, out arch))
                                    {
                                        System.IO.Directory.Delete(subfolder, true);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    var archs = buildwork.MultiBuildArchs;
                    for (int i = 0; i < archs.Length; ++i)
                    {
                        var arch = archs[i];
                        dstsptRoots.Add("/spt/@" + arch + "/");
                    }
                    if (System.IO.Directory.Exists(outputDir + "/spt/mod/"))
                    {
                        System.IO.Directory.Delete(outputDir + "/spt/mod/", true);
                    }
                }
                for (int j = 0; j < dstsptRoots.Count; ++j)
                {
                    var outmoddir = outputDir + dstsptRoots[j] + "mod/";
                    if (System.IO.Directory.Exists(outmoddir))
                    {
                        if (builtMods.Count == 0 || builtMods.Count == 1 && builtMods.Contains(""))
                        {
                            System.IO.Directory.Delete(outmoddir, true);
                        }
                        else
                        {
                            var allModFolders = System.IO.Directory.GetDirectories(outmoddir);
                            for (int i = 0; i < allModFolders.Length; ++i)
                            {
                                if (winprog != null && AsyncWorkTimer.Check()) yield return null;
                                var modfolder = allModFolders[i];
                                logger.Log(modfolder);
                                var mod = modfolder.Substring(outmoddir.Length);
                                if (!builtMods.Contains(mod))
                                {
                                    System.IO.Directory.Delete(modfolder, true);
                                }
                            }
                        }
                    }

                    //var samoddir = "Assets/StreamingAssets" + dstsptRoots[j] + "mod/";
                    //if (System.IO.Directory.Exists(samoddir))
                    //{
                    //    logger.Log("(Phase) Delete StreamingAssets Mod Folder Not Built.");
                    //    if (builtMods.Count == 0 || builtMods.Count == 1 && builtMods.Contains(""))
                    //    {
                    //        System.IO.Directory.Delete(samoddir, true);
                    //    }
                    //    else
                    //    {
                    //        var allModFolders = System.IO.Directory.GetDirectories(samoddir);
                    //        for (int i = 0; i < allModFolders.Length; ++i)
                    //        {
                    //            if (winprog != null && AsyncWorkTimer.Check()) yield return null;
                    //            var modfolder = allModFolders[i];
                    //            logger.Log(modfolder);
                    //            var mod = modfolder.Substring(samoddir.Length);
                    //            if (!builtMods.Contains(mod))
                    //            {
                    //                System.IO.Directory.Delete(modfolder, true);
                    //            }
                    //        }
                    //    }
                    //}
                }

                if (isDefaultBuild)
                {
                    logger.Log("(Phase) Write Version.");
                    var outverdir = "EditorOutput/Build/Latest/spt/version.txt";
                    if (System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(outverdir)))
                    {
                        System.IO.File.WriteAllText(outverdir, version.ToString());
                    }
                    // Make icon
                    IconMaker.SetFolderIconToText(outputDir, version.ToString());
                    IconMaker.SetFolderIconToText(outputDir + "/spt", version.ToString());
                }

                logger.Log("(Phase) Delete old scripts in Streaming Assets.");
                if (System.IO.Directory.Exists("Assets/StreamingAssets/spt/"))
                {
                    if (System.IO.Directory.Exists("Assets/StreamingAssets/spt_temp/"))
                    { // the editor crashed when doing last build
                        System.IO.Directory.Delete("Assets/StreamingAssets/spt_temp/", true);
                    }
                    System.IO.Directory.Move("Assets/StreamingAssets/spt", "Assets/StreamingAssets/spt_temp");
                    System.IO.Directory.Delete("Assets/StreamingAssets/spt_temp/", true);
                    PlatDependant.CreateFolder("Assets/StreamingAssets/spt/");
                }

                logger.Log("(Phase) Wait For Build.");
                work = buildwork.WaitForWorkDone(winprog);
                while (work.MoveNext())
                {
                    if (winprog != null)
                    {
                        yield return work.Current;
                    }
                }


                logger.Log("(Phase) Copy.");
                var outsptdir = outputDir + "/spt/";
                if (System.IO.Directory.Exists(outsptdir))
                {
                    HashSet<string> nocopyfiles = new HashSet<string>()
                    {
                        "icon.png",
                        "icon.ico",
                        "desktop.ini",
                        "Icon\r",
                    };
                    var allbuildfiles = PlatDependant.GetAllFiles(outsptdir);
                    for (int i = 0; i < allbuildfiles.Length; ++i)
                    {
                        if (winprog != null && AsyncWorkTimer.Check()) yield return null;
                        var srcfile = allbuildfiles[i];
                        if (srcfile.EndsWith(".DS_Store"))
                        {
                            continue;
                        }
                        if (srcfile.EndsWith(".srcinfo"))
                        {
                            continue;
                        }
                        var part = srcfile.Substring(outsptdir.Length);
                        if (nocopyfiles.Contains(part))
                        {
                            continue;
                        }
                        logger.Log(part);
                        var destfile = "Assets/StreamingAssets/spt/" + part;
                        PlatDependant.CreateFolder(System.IO.Path.GetDirectoryName(destfile));
                        System.IO.File.Copy(srcfile, destfile);
                    }
                }

                if (isDefaultBuild && makezip)
                {
                    work = ZipBuiltSptAsync(winprog, timetoken);
                    while (work.MoveNext())
                    {
                        if (winprog != null)
                        {
                            yield return work.Current;
                        }
                    }
                }
            }
            finally
            {
                BuilderCleanup();
                logger.Log("(Done) Build Spt.");
            }
        }

        public static IEnumerator ZipBuiltSptAsync(IEditorWorkProgressShower winprog, string timetoken)
        {
            if (string.IsNullOrEmpty(timetoken))
            {
                timetoken = ResBuilder.ResBuilderParams.Create().timetoken;
            }
            var outputDir = "EditorOutput/Build/Latest";
            var logger = new EditorWorkProgressLogger() { Shower = winprog };
            logger.Log("(Phase) Zip.");
            int version = 0;
            if (System.IO.File.Exists(outputDir + "/spt/version.txt"))
            {
                foreach (var line in System.IO.File.ReadLines(outputDir + "/spt/version.txt"))
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
            List<string> dstsptRoots = new List<string>();
            bool multiArch = false;
            if (System.IO.Directory.Exists(outputDir + "/spt/"))
            {
                var rootsubs = System.IO.Directory.GetDirectories(outputDir + "/spt/");
                if (rootsubs != null)
                {
                    for (int i = 0; i < rootsubs.Length; ++i)
                    {
                        var rootsub = rootsubs[i];
                        var dirname = rootsub.Substring(outputDir.Length + "/spt/".Length);
                        if (dirname.StartsWith("@"))
                        {
                            var archname = dirname.Substring("@".Length);
                            int arch;
                            if (int.TryParse(archname, out arch))
                            {
                                multiArch = true;
                                dstsptRoots.Add("/spt/" + dirname + "/");
                            }
                        }
                    }
                }
            }
            if (!multiArch)
            {
                dstsptRoots.Add("/spt/");
            }

            Dictionary<string, Pack<string, string, IList<string>>> zips = new Dictionary<string, Pack<string, string, IList<string>>>();
            var outzipdir = "EditorOutput/Build/" + timetoken + (version > 0 ? ("_" + version) : "") + "/whole/spt/";
            System.IO.Directory.CreateDirectory(outzipdir);
            Dictionary<string, HashSet<string>> builtModsAndDists = new Dictionary<string, HashSet<string>>();
            builtModsAndDists[""] = new HashSet<string>();
            for (int j = 0; j < dstsptRoots.Count; ++j)
            {
                var sptFolder = outputDir + dstsptRoots[j];
                var modRoot = sptFolder + "mod/";
                if (System.IO.Directory.Exists(modRoot))
                {
                    var moddirs = System.IO.Directory.GetDirectories(modRoot);
                    if (moddirs != null)
                    {
                        for (int i = 0; i < moddirs.Length; ++i)
                        {
                            var mod = moddirs[i].Substring(modRoot.Length);
                            if (!builtModsAndDists.ContainsKey(mod))
                            {
                                builtModsAndDists[mod] = new HashSet<string>();
                            }
                        }
                    }
                }

                foreach (var kvp in builtModsAndDists)
                {
                    var mod = kvp.Key;
                    var hashset = kvp.Value;
                    hashset.Add("");

                    var distroot = sptFolder;
                    if (string.IsNullOrEmpty(mod))
                    {
                        distroot = sptFolder + "dist/";
                    }
                    else
                    {
                        distroot = sptFolder + "mod/" + mod + "/dist/";
                    }
                    if (System.IO.Directory.Exists(distroot))
                    {
                        var distdirs = System.IO.Directory.GetDirectories(distroot);
                        if (distdirs != null)
                        {
                            for (int i = 0; i < distdirs.Length; ++i)
                            {
                                var dist = distdirs[i].Substring(distroot.Length);
                                hashset.Add(dist);
                            }
                        }
                    }
                }
            }

            HashSet<string> InMainNonOptMods = new HashSet<string>();
            foreach (var kvpModsAndDists in builtModsAndDists)
            {
                var mod = kvpModsAndDists.Key;
                if (mod != "")
                {
                    var moddesc = ResManager.GetDistributeDesc(mod);
                    if (moddesc == null || (moddesc.InMain && !moddesc.IsOptional))
                    {
                        InMainNonOptMods.Add(mod);
                    }
                }
            }
            if (InMainNonOptMods.Count > 0)
            {
                if (!builtModsAndDists.ContainsKey(""))
                {
                    builtModsAndDists[""] = new HashSet<string>();
                }
            }

            foreach (var kvpModsAndDists in builtModsAndDists)
            {
                var mod = kvpModsAndDists.Key;
                var dists = kvpModsAndDists.Value;
                if (InMainNonOptMods.Contains(mod))
                {
                    continue;
                }
                List<Pack<string, string>> lstModAndDist = new List<Pack<string, string>>();
                foreach (var dist in dists)
                {
                    lstModAndDist.Add(new Pack<string, string>(mod, dist));
                }
                if (mod == "")
                {
                    foreach (var exmod in InMainNonOptMods)
                    {
                        foreach (var exdist in builtModsAndDists[exmod])
                        {
                            lstModAndDist.Add(new Pack<string, string>(exmod, exdist));
                        }
                    }
                }
                for (int i = 0; i < lstModAndDist.Count; ++i)
                {
                    var exmod = lstModAndDist[i].t1;
                    var dist = lstModAndDist[i].t2;

                    for (int j = 0; j < dstsptRoots.Count; ++j)
                    {
                        var sptFolder = outputDir + dstsptRoots[j];
                        if (exmod != "")
                        {
                            sptFolder += "mod/";
                            sptFolder += exmod;
                            sptFolder += "/";
                        }
                        if (dist != "")
                        {
                            sptFolder += "dist/";
                            sptFolder += dist;
                            sptFolder += "/";
                        }

                        List<string> entries = new List<string>();
                        if (System.IO.Directory.Exists(sptFolder))
                        {
                            try
                            {
                                var files = PlatDependant.GetAllFiles(sptFolder);
                                for (int k = 0; k < files.Length; ++k)
                                {
                                    var file = files[k];
                                    if (file.EndsWith(".lua"))
                                    {
                                        var raw = file.Substring(sptFolder.Length).Replace('\\', '/');
                                        if (!(exmod == "" && raw.StartsWith("mod/") || dist == "" && raw.StartsWith("dist/")))
                                        {
                                            var entry = file.Substring(outputDir.Length + 1);
                                            entries.Add(entry);
                                            entries.Add(entry + ".srcinfo");
                                        }
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                logger.Log("(Error)(Not Critical)");
                                logger.Log(e.ToString());
                            }
                        }
                        if (entries.Count > 0)
                        {
                            var sptkey = "m-" + mod.ToLower() + "-d-" + dist.ToLower();
                            string filename;
                            if (dstsptRoots[j] == "/spt/")
                            {
                                filename = sptkey;
                            }
                            else
                            {
                                var sub = dstsptRoots[j].Substring("/spt/".Length, dstsptRoots[j].Length - "/spt/".Length - 1);
                                filename = sptkey + "." + sub;
                            }
                            string zipfile = outzipdir + filename + ".zip";

                            if (zips.ContainsKey(filename))
                            {
                                entries.AddRange(zips[filename].t3);
                                zips[filename] = new Pack<string, string, IList<string>>(zipfile, outputDir, entries);
                            }
                            else
                            {
                                //// mani
                                //var mani = "m-" + mod.ToLower() + "-d-" + dist.ToLower() + ".m.ab";
                                //mani = "res/mani/" + mani;
                                //entries.Add(mani);
                                //entries.Add(mani + ".manifest");
                                //entries.Add("res/mani/mani");
                                //entries.Add("res/mani/mani.manifest");
                                // version
                                entries.Add("spt/version.txt");
                                var dversion = "spt/version/" + sptkey + ".txt";
                                PlatDependant.CopyFile(outputDir + "/spt/version.txt", outputDir + "/" + dversion);
                                entries.Add(dversion);

                                zips[filename] = new Pack<string, string, IList<string>>(zipfile, outputDir, entries);
                            }
                            //var workz = ResBuilder.MakeZipAsync(zipfile, outputDir, entries, winprog);
                            //while (workz.MoveNext())
                            //{
                            //    if (winprog != null)
                            //    {
                            //        yield return workz.Current;
                            //    }
                            //}
                        }
                    }
                }
            }
            if (zips.Count > 0)
            {
                var workz = ResBuilder.MakeZipsBackground(zips.Values.ToArray(), winprog);
                while (workz.MoveNext())
                {
                    if (winprog != null)
                    {
                        yield return workz.Current;
                    }
                }
            }

            // Make icon
            IconMaker.SetFolderIconToFileContent("EditorOutput/Build/" + timetoken + (version > 0 ? ("_" + version) : ""), outputDir + "/spt/version.txt");
        }

        public static void RestoreStreamingAssetsFromLatestBuild()
        {
            var srcroot = "EditorOutput/Build/Latest/spt/";
            var dstroot = "Assets/StreamingAssets/spt/";

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
                    if (srcfile.EndsWith(".srcinfo"))
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
            }
        }

        private class SptBuilderPreExport : UnityEditor.Build.IPreprocessBuild
        {
            public int callbackOrder { get { return 0; } }
            private static HashSet<string> NonSptFiles = new HashSet<string>()
            {
                "manifest.m.txt",
                "index.txt",
                "version.txt",
                "ver.txt",
            };

            public void OnPreprocessBuild(BuildTarget target, string path)
            {
                ResManifest sptmani = new ResManifest();
                HashSet<string> keys = new HashSet<string>();
                HashSet<int> archs = new HashSet<int>();
                string sptroot = "Assets/StreamingAssets/spt/";
                if (PlatDependant.IsFileExist("Assets/StreamingAssets/hasobb.flag.txt"))
                {
                    sptroot = "EditorOutput/Build/Latest/spt/";
                }
                using (var sw = PlatDependant.OpenWriteText("Assets/StreamingAssets/spt/index.txt"))
                {
                    var files = PlatDependant.GetAllFiles(sptroot);
                    if (files != null)
                    {
                        for (int i = 0; i < files.Length; ++i)
                        {
                            var file = files[i];
                            if (file.EndsWith(".meta"))
                            {
                                continue;
                            }
                            if (file.EndsWith(".srcinfo"))
                            {
                                continue;
                            }
                            var part = file.Substring(sptroot.Length);
                            if (NonSptFiles.Contains(part))
                            {
                                continue;
                            }
                            sptmani.AddOrGetItem(part);
                            if (part.StartsWith("@"))
                            {
                                var index = part.IndexOfAny(new[] { '/', '\\' });
                                if (index > 0)
                                {
                                    var dir0 = part.Substring(1, index - 1);
                                    int arch;
                                    if (int.TryParse(dir0, out arch))
                                    {
                                        archs.Add(arch);
                                        part = part.Substring(index + 1);
                                    }
                                }
                            }
                            string mod = "";
                            string dist = "";
                            if (part.StartsWith("mod/"))
                            {
                                var iend = part.IndexOf('/', "mod/".Length);
                                if (iend > 0)
                                {
                                    mod = part.Substring("mod/".Length, iend - "mod/".Length);
                                    part = part.Substring(iend + 1);
                                }
                            }
                            if (part.StartsWith("dist/"))
                            {
                                var iend = part.IndexOf('/', "dist/".Length);
                                if (iend > 0)
                                {
                                    dist = part.Substring("dist/".Length, iend - "dist/".Length);
                                }
                            }

                            if (mod != "")
                            {
                                var moddesc = ResManager.GetDistributeDesc(mod);
                                if (moddesc == null || (moddesc.InMain && !moddesc.IsOptional))
                                {
                                    mod = "";
                                }
                            }

                            var key = "m-" + (mod ?? "").ToLower() + "-d-" + (dist ?? "").ToLower();
                            if (keys.Add(key))
                            {
                                sw.WriteLine(key);
                            }
                        }
                    }
                }
                if (archs.Count > 0)
                {
                    foreach (var arch in archs)
                    {
                        sptmani.AddOrGetItem("@arch/@" + arch);
                    }
                }
                LuaFileManager.SaveManifest(sptmani, "Assets/StreamingAssets/spt/manifest.m.txt");
            }
        }
    }
}