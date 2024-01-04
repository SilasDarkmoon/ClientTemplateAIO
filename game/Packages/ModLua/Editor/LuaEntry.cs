using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngineEx;

namespace UnityEditorEx
{
    [InitializeOnLoad]
    public static class LuaEntry
    {
        static LuaEntry()
        {
            SafeInitializerUtils.DoDelayedInitialize();
            ModEditorInitializer.ShouldAlreadyInit();

            PackageEditor.OnPackagesChanged += ReinitGlobalLua;
            DistributeEditor.OnDistributeFlagsChanged += ReinitGlobalLua;
        }
        private static void ReinitGlobalLua()
        {
            GlobalLua.Reinit();
        }

        [MenuItem("Res/Build Scripts (No Update, Raw Copy)", priority = 200120)]
        public static void BuildSptCommand()
        {
            ResBuilder.BuildingParams = ResBuilder.ResBuilderParams.Create();
            ResBuilder.BuildingParams.makezip = false;
            var work = SptBuilder.BuildSptAsync(null, null, new[] { new SptBuilder.SptBuilderEx_RawCopy() });
            while (work.MoveNext()) ;
            ResBuilder.BuildingParams = null;
        }

        [MenuItem("Lua/Reinit Global Lua", priority = 300010)]
        public static void ReinitGlobalLuaInEditor()
        {
            if (!Application.isPlaying)
            {
                ReinitGlobalLua();
            }
            else
            {
                PlatDependant.LogError("Cannot reinit global lua while playing.");
            }
        }
    }
}