using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UnityEditorEx
{
    [InitializeOnLoad]
    public static partial class ModEditorInitializer
    {
        public static void ShouldAlreadyInit() { }

        private class ModEditorInitializer_ResManager
        {
            public ModEditorInitializer_ResManager()
            {
                ResManagerEditorEntry.ShouldAlreadyInit();
                ModEditor.ShouldAlreadyInit();

                PackageEditor.OnPackagesChanged += OnPackagesChanged;
                DistributeEditor.OnDistributeFlagsChanged += OnDistributeFlagsChanged;
                //DistributeEditor.OnDistributeFlagsChanged += ModEditor.CheckModsVisibility;
                //DistributeEditor.OnDistributeFlagsChanged += UnityEngineEx.ResManager.RebuildRuntimeResCache;
            }

            private static void OnPackagesChanged()
            {
                DistributeEditor.CheckDefaultSelectedDistributeFlags();
                ModEditor.CheckModsAndMakeLink();
                UnityEngineEx.ResManager.RebuildRuntimeResCache();
            }
            [UnityEngineEx.EventOrder(-100)]
            private static void OnDistributeFlagsChanged()
            {
                ModEditor.CheckModsVisibility();
                UnityEngineEx.ResManager.RebuildRuntimeResCache();
            }
        }
#pragma warning disable 0414
        private static ModEditorInitializer_ResManager i_ModEditorInitializer_ResManager = new ModEditorInitializer_ResManager();
#pragma warning restore
    }
}