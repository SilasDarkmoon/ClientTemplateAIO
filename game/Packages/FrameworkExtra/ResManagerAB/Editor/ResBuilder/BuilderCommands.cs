﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngineEx;

namespace UnityEditorEx
{
    public static class ResBuilderCommands
    {
        [MenuItem("Res/Check Build", priority = 200105)]
        public static void CheckBuildCommand()
        {
            ResBuilderChecker.CheckRes("EditorOutput/Intermediate/ResBuildCheckResult.txt");
        }

        [MenuItem("Res/Build Res (No Update)", priority = 200110)]
        public static void BuildResCommand()
        {
            ResBuilder.BuildingParams = ResBuilder.ResBuilderParams.Create();
            ResBuilder.BuildingParams.makezip = false;
            var work = ResBuilder.BuildResAsync(null, null);
            while (work.MoveNext()) ;
            ResBuilder.BuildingParams = null;
        }

        private static bool CheckAssetInDistributeFlags(string fileName, HashSet<string> flagsSet)
        {
            string mod, dist;
            ResBuilderChecker.GetBundleModAndDist(fileName, out mod, out dist);
            if (!string.IsNullOrEmpty(mod) && !flagsSet.Contains(mod))
            {
                return false;
            }
            if (!string.IsNullOrEmpty(dist) && !flagsSet.Contains(dist))
            {
                return false;
            }
            return true;
        }

        private static bool CheckSptInDistributeFlags(string relativePath, HashSet<string> flagsSet)
        {
            var alldflags = DistributeEditor.GetAllDistributesCached();
            string mod, dist;
            ResBuilderChecker.GetSptModAndDist(relativePath, out mod, out dist);
            mod = mod?.ToLower();
            dist = dist?.ToLower();
            if (!string.IsNullOrEmpty(mod) && !flagsSet.Contains(mod) && alldflags.Contains(mod))
            {
                return false;
            }
            if (!string.IsNullOrEmpty(dist) && !flagsSet.Contains(dist))
            {
                return false;
            }
            return true;
        }

        [MenuItem("Res/Delete Asset not in DistributeFlags", priority = 200180)]
        public static void DelAssetNotInDistributeFlags()
        {
            var flags = ResManager.GetDistributeFlags();
            HashSet<string> flagsSet = new HashSet<string>();
            if (flags != null)
            {
                for (int i = 0; i < flags.Length; ++i)
                {
                    flagsSet.Add(flags[i].ToLower());
                }
            }

            StringBuilder sblog = new StringBuilder();
            Debug.LogFormat("Start to check res!");
            if (Directory.Exists("Assets/StreamingAssets/res/"))
            {
                var allexistfiles = PlatDependant.GetAllFiles("Assets/StreamingAssets/res/");
                foreach (var item in allexistfiles)
                {
                    string fileName = Path.GetFileName(item);
                    bool isselected = CheckAssetInDistributeFlags(fileName, flagsSet);
                    if (!isselected)
                    {
                        PlatDependant.DeleteFile(item);
                        sblog.AppendLine(item);
                    }
                }
            }

            string sptroot = "Assets/StreamingAssets/spt";
            if (Directory.Exists("Assets/StreamingAssets/spt/@64"))
            {
                sptroot = "Assets/StreamingAssets/spt/@64";
                if (Directory.Exists("Assets/StreamingAssets/spt/@32"))
                {
                    Directory.Delete("Assets/StreamingAssets/spt/@32", true);
                    sblog.AppendLine("Assets/StreamingAssets/spt/@32/");
                }
            }

            if (Directory.Exists(sptroot))
            {
                var allexistFolders = PlatDependant.GetAllFolders(sptroot);
                foreach (var item in allexistFolders)
                {
                    var relativePath = item.Substring(sptroot.Length);
                    bool isselected = CheckSptInDistributeFlags(relativePath, flagsSet);
                    if (!isselected)
                    {
                        if (Directory.Exists(item))
                        {
                            Directory.Delete(item, true);
                            sblog.Append(item);
                            sblog.AppendLine("/");
                        }
                    }
                }
            }

            Debug.LogFormat("DelAssetNotInDistributeFlags Done!\n{0}", sblog.ToString());
        }

        #region Test
        [MenuItem("Test/Res/Create Icon", priority = 500010)]
        public static void TestCreateIcon()
        {
            IconMaker.WriteTextToImage(UnityEngine.Random.Range(0, 1000).ToString(), "EditorOutput/temp.png");
            System.IO.Directory.CreateDirectory("EditorOutput/testfolder");
            if (IconMaker.ChangeImageToIco("EditorOutput/temp.png", null))
            {
                IconMaker.SetFolderIcon("EditorOutput/testfolder", "EditorOutput/temp.ico");
            }
            else
            {
                IconMaker.SetFolderIcon("EditorOutput/testfolder", "EditorOutput/temp.png");
            }
        }
        #endregion
    }
}