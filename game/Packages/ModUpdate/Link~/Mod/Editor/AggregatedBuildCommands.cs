#if MOD_RESMANAGER
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngineEx;

namespace UnityEditorEx
{
    public static class AggregatedBuildCommands
    {
        [MenuItem("Res/Build All (Raw)", priority = 200140)]
        public static void BuildAllRaw()
        {
            ResBuilder.BuildingParams = ResBuilder.ResBuilderParams.Create();
            ResBuilder.BuildingParams.makezip = false;
            IEnumerator work;
#if MOD_LUA
            work = SptBuilder.BuildSptAsync(null, null, new[] { new SptBuilder.SptBuilderEx_RawCopy() });
            while (work.MoveNext()) ;
#endif
            work = ResBuilder.BuildResAsync(null, null);
            while (work.MoveNext()) ;
            ResBuilder.BuildingParams = null;
        }
        [MenuItem("Res/Build All (Quick)", priority = 200150)]
        public static void BuildAllQuick()
        {
            ResBuilder.BuildingParams = ResBuilder.ResBuilderParams.Create();
            ResBuilder.BuildingParams.makezip = false;
            IEnumerator work;
#if MOD_LUA
            work = SptBuilder.BuildSptAsync(null, null);
            while (work.MoveNext()) ;
#endif
            work = ResBuilder.BuildResAsync(null, null);
            while (work.MoveNext()) ;
            ResBuilder.BuildingParams = null;
        }
        [MenuItem("Res/Build All (Full)", priority = 200160)]
        public static void BuildAllFull()
        {
            ResBuilder.BuildingParams = ResBuilder.ResBuilderParams.Create();
            IEnumerator work;
#if MOD_LUA
            work = SptBuilder.BuildSptAsync(null, null);
            while (work.MoveNext()) ;
#endif
            work = ResBuilder.BuildResAsync(null, null);
            while (work.MoveNext()) ;
            ResBuilder.BuildingParams = null;
            UpdateBuilder.BuildNearestUpdate();
        }
        [MenuItem("Res/Build All (Full, With Progress Window)", priority = 200170)]
        public static void BuildAllFullWithProg()
        {
            ResBuilder.BuildingParams = ResBuilder.ResBuilderParams.Create();
            var winprog = new EditorWorkProgressShowerInConsole();
#if MOD_LUA
            winprog.Works.Add(SptBuilder.BuildSptAsync(null, winprog));
#endif
            winprog.Works.Add(ResBuilder.BuildResAsync(null, winprog));
            winprog.Works.Add(UpdateBuilder.BuildNearestUpdate(winprog));
            winprog.OnQuit += () => { ResBuilder.BuildingParams = null; };
            winprog.StartWork();
        }

        [MenuItem("Res/Archive Built Res", priority = 200125)]
        public static void ArchiveBuiltRes()
        {
            var timetoken = ResBuilder.ResBuilderParams.Create().timetoken;
            IEnumerator work;
#if MOD_LUA
            work = SptBuilder.ZipBuiltSptAsync(null, timetoken);
            while (work.MoveNext()) ;
#endif
            work = ResBuilder.ZipBuiltResAsync(null, timetoken);
            while (work.MoveNext()) ;
        }

        [MenuItem("Res/Restore Streaming Assets From Latest Build", priority = 200128)]
        public static void RestoreStreamingAssetsFromLatestBuild()
        {
#if MOD_LUA
            SptBuilder.RestoreStreamingAssetsFromLatestBuild();
#endif
            ResBuilder.RestoreStreamingAssetsFromLatestBuild();
        }
    }
}
#endif