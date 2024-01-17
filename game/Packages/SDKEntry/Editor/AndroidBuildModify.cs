using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngineEx;

namespace UnityEditorEx
{ 
    public static class AndroidBuildModify
    {
        public static string FindAndroidProjectRoot(string path)
        {
            var seaching = path;
            while (!string.IsNullOrEmpty(seaching))
            {
                bool found = false;
                var file = System.IO.Path.Combine(seaching, "build.gradle");
                if (System.IO.File.Exists(file))
                {
                    foreach (var line in System.IO.File.ReadLines(file))
                    {
                        if (line.TrimStart().StartsWith("classpath 'com.android.tools.build:gradle:"))
                        {
                            found = true;
                            break;
                        }
                    }
                }
                if (found)
                {
                    break;
                }
                seaching = System.IO.Path.GetDirectoryName(seaching);
            }
            return string.IsNullOrEmpty(seaching) ? path : seaching;
        }
        public static string FindAndroidProjectAppFolder(string root)
        {
            var targetname = System.IO.Path.Combine(root, "launcher");
            if (System.IO.File.Exists(targetname + "/build.gradle"))
            {
                return targetname;
            }
            var subs = System.IO.Directory.GetDirectories(root);
            foreach (var sub in subs)
            {
                var file = System.IO.Path.Combine(sub, "build.gradle");
                if (System.IO.File.Exists(file))
                {
                    foreach (var line in System.IO.File.ReadLines(file))
                    {
                        if (line.TrimStart().StartsWith("applicationId '"))
                        {
                            return sub;
                        }
                    }
                }
            }
            return root;
        }
        public static string FindAndroidProjectUnityLibFolder(string root)
        {
            var targetname = System.IO.Path.Combine(root, "unityLibrary");
            if (System.IO.File.Exists(targetname + "/build.gradle"))
            {
                return targetname;
            }
            var subs = System.IO.Directory.GetDirectories(root);
            foreach (var sub in subs)
            {
                var file = System.IO.Path.Combine(sub, "build.gradle");
                if (System.IO.File.Exists(file))
                {
                    bool islib = false;
                    foreach (var line in System.IO.File.ReadLines(file))
                    {
                        if (!islib)
                        {
                            var linet = line.Trim();
                            if (linet.StartsWith("apply plugin") && linet.EndsWith("'com.android.library'"))
                            {
                                islib = true;
                            }
                        }
                        else
                        {
#if UNITY_2020_2_OR_NEWER || NETCOREAPP3_0 || NETCOREAPP3_1 || NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1 || NETSTANDARD2_1_OR_GREATER
                            if (line.Contains("unity", StringComparison.InvariantCultureIgnoreCase))
#else
                            if (line.ToLower().Contains("unity"))
#endif
                            {
                                return sub;
                            }
                        }
                    }
                }
            }
            return root;
        }


#if UNITY_2018_2_OR_NEWER
        private class AndroidPostBuild : UnityEditor.Android.IPostGenerateGradleAndroidProject
        {
            public int callbackOrder { get { return 0; } }

            public void OnPostGenerateGradleAndroidProject(string path)
            {
                var proot = FindAndroidProjectRoot(path);
                var ulibroot = FindAndroidProjectUnityLibFolder(proot);
                string amani = System.IO.Path.Combine(ulibroot, "src/main/AndroidManifest.xml");
                var doc = System.Xml.Linq.XDocument.Load(amani);
                var appnode = doc.Element("manifest").Element("application").Element("activity");
                appnode.Attribute(System.Xml.Linq.XName.Get("name", "http://schemas.android.com/apk/res/android")).SetValue("host.silas.anative.android.EntryActivity");
                doc.Save(amani);
            }
        }
#else
        private class AndroidPreBuild : UnityEditor.Build.IPreprocessBuild, UnityEditor.Build.IPostprocessBuild
        {
            public int callbackOrder { get { return 0; } }

            public void OnPreprocessBuild(BuildTarget target, string path)
            {
                if (target == BuildTarget.Android)
                {
                    string amani = "EditorOutput/Intermediate/AndroidManifest.xml";
                    //if (!PlatDependant.IsFileExist(amani))
                    {
                        var omani = EditorApplication.applicationContentsPath + "/PlaybackEngines/AndroidPlayer/Apk/AndroidManifest.xml";
                        if (!PlatDependant.IsFileExist(omani))
                        {
                            omani = EditorApplication.applicationContentsPath + "/PlaybackEngines/AndroidPlayer/Apk/UnityManifest.xml";
                            if (!PlatDependant.IsFileExist(omani))
                            {
                                Debug.LogError("Cannot find original AndroidManifest.xml");
                                return;
                            }
                        }

                        PlatDependant.CopyFile(omani, amani);
                    }

                    try
                    {
                        var doc = System.Xml.Linq.XDocument.Load(amani);
                        var appnode = doc.Element("manifest").Element("application").Element("activity");
                        appnode.Attribute(System.Xml.Linq.XName.Get("name", "http://schemas.android.com/apk/res/android")).SetValue("host.silas.anative.android.EntryActivity");
                        doc.Save(amani);
                        PlatDependant.CopyFile(amani, "Assets/Plugins/Android/AndroidManifest.xml");
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }

            public void OnPostprocessBuild(BuildTarget target, string path)
            {
                PlatDependant.DeleteFile("EditorOutput/Intermediate/AndroidManifest.xml");
            }
        }
#endif
    }
}
