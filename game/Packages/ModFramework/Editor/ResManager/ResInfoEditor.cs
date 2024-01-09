using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngineEx;

namespace UnityEditorEx
{
    public static class ResInfoEditor
    {
        [MenuItem("Assets/Get Selected Asset Path (Raw)", priority = 2027)]
        public static void GetSelectedAssetPathRaw()
        {
            if (Selection.assetGUIDs != null)
            {
                var guid = Selection.assetGUIDs.First();
                if (guid != null)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path != null)
                    {
                        GUIUtility.systemCopyBuffer = path;
                        Debug.Log(path);
                    }
                }
            }
        }

        [MenuItem("Assets/Get Selected Asset Path", priority = 2028)]
        public static void GetSelectedAssetPath()
        {
            if (Selection.assetGUIDs != null)
            {
                var guid = Selection.assetGUIDs.First();
                if (guid != null)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path != null)
                    {
                        string norm = GetAssetNormPath(path);
                        GUIUtility.systemCopyBuffer = norm;
                        Debug.Log(norm);
                    }
                }
            }
        }

        [MenuItem("Res/Ping Asset in Clipboard &_c", priority = 200200)]
        public static void PingAssetInClipboard()
        {
            string path = GUIUtility.systemCopyBuffer;
            if (string.IsNullOrEmpty(path))
            {
                Debug.Log("Clipboard is empty.");
                return;
            }
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset)
            {
                EditorUtility.FocusProjectWindow();
                EditorGUIUtility.PingObject(asset);
                return;
            }
            var real = path;
#if COMPATIBLE_RESMANAGER_V1
            real = ResManager.CompatibleAssetName(path);
            if (real != path)
            {
                path = real;
                asset = AssetDatabase.LoadMainAssetAtPath(real);
                if (asset)
                {
                    EditorUtility.FocusProjectWindow();
                    EditorGUIUtility.PingObject(asset);
                    return;
                }
            }
#endif
            real = FindDistributeAsset(path);
            if (!string.IsNullOrEmpty(real))
            {
                asset = AssetDatabase.LoadMainAssetAtPath(real);
                if (asset)
                {
                    EditorUtility.FocusProjectWindow();
                    EditorGUIUtility.PingObject(asset);
                    return;
                }
            }
            real = path.Replace('.', '/');
            real = "ModSpt/" + real + ".lua";
            real = ResManager.EditorResLoader.CheckDistributePath(real);
            if (!string.IsNullOrEmpty(real))
            {
                asset = AssetDatabase.LoadMainAssetAtPath(real);
                if (asset)
                {
                    EditorUtility.FocusProjectWindow();
                    EditorGUIUtility.PingObject(asset);
                    return;
                }
            }
            Debug.Log("Can not find asset: " + path);
        }

        [MenuItem("Res/Find Asset", priority = 200210)]
        public static void FindAssetInProject()
        {
            var file = EditorUtility.OpenFilePanel("Select File to Find", null, null);
            if (!string.IsNullOrEmpty(file) && PlatDependant.IsFileExist(file))
            {
                var md5 = ModEditorUtils.GetFileMD5(file);

                string found = null;
                foreach (var asset in AssetDatabase.GetAllAssetPaths())
                {
                    if (ModEditorUtils.GetFileMD5(asset) == md5)
                    {
                        found = asset;
                        break;
                    }
                }

                if (found == null)
                {
                    EditorUtility.DisplayDialog("Result", "Not Found.", "OK");
                }
                else
                {
                    var asset = AssetDatabase.LoadMainAssetAtPath(found);
                    EditorUtility.FocusProjectWindow();
                    EditorGUIUtility.PingObject(asset);
                    Selection.activeObject = asset;
                }
            }
        }

        [MenuItem("Res/Delete Empty Folders", priority = 200220)]
        public static void DeleteEmptyAssetFolders()
        {
            var assets = AssetDatabase.GetAllAssetPaths();
            Array.Sort(assets, (a, b) => -Comparer<string>.Default.Compare(a, b));
            HashSet<string> nonEmptyFolders = new HashSet<string>();
            bool dirty = false;
            for (int i = 0; i < assets.Length; ++i)
            {
                var path = assets[i];
                if (!ModEditorUtils.IsDirLink(path) && System.IO.Directory.Exists(path) && !nonEmptyFolders.Contains(path))
                {
                    try
                    {
                        System.IO.Directory.Delete(path, true);
                        dirty = true;
                        Debug.LogError("Delete: " + path);
                        continue;
                    }
                    catch { }
                }
                {
                    var dir = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
                    nonEmptyFolders.Add(dir);
                }
            }
            if (dirty)
            {
                AssetDatabase.Refresh();
            }
        }

        [MenuItem("Assets/Create/ScriptableObject Asset", priority = 30)]
        public static void CreateScriptableObjectAsset()
        {
            var guids = Selection.assetGUIDs;
            if (guids != null)
            {
                var scripts = from guid in guids
                              let path = AssetDatabase.GUIDToAssetPath(guid)
                              let asset = AssetDatabase.LoadAssetAtPath<MonoScript>(path)
                              where asset
                              let stype = asset.GetClass()
                              where stype != null && stype.IsSubclassOf(typeof(ScriptableObject))
                              select new { Type = stype, Path = path };

                var script = scripts.FirstOrDefault();
                if (script != null)
                {
                    var asset = ScriptableObject.CreateInstance(script.Type);
                    var fileWithoutExt = script.Path.Substring(0, script.Path.Length - System.IO.Path.GetExtension(script.Path).Length);
                    var file = fileWithoutExt + ".asset";
                    if (System.IO.File.Exists(file))
                    {
                        int i = 0;
                        while (true)
                        {
                            file = fileWithoutExt + ++i + ".asset";
                            if (!System.IO.File.Exists(file))
                            {
                                break;
                            }
                        }
                    }
                    AssetDatabase.CreateAsset(asset, file);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }
        }

        public static string GetAssetNormPath(string path)
        {
            if (path != null)
            {
                string type, mod, dist;
                string norm = ResManager.GetAssetNormPath(path, out type, out mod, out dist);
                if (string.IsNullOrEmpty(norm))
                {
                    norm = path;
                }
                if (type == "spt")
                {
                    if (norm.EndsWith(".lua"))
                    {
                        norm = norm.Substring(0, norm.Length - ".lua".Length);
                    }
                    norm = norm.Replace('/', '.');
                }
                return norm;
            }
            return null;
        }

        public static string FindDistributeAsset(string norm)
        {
            var strval = norm;
            if (strval.StartsWith("Assets/"))
            {
                if (PlatDependant.IsFileExist(strval))
                {
                    return strval;
                }
            }
            string real = strval;
            if (!real.StartsWith("ModSpt/") && !real.StartsWith("ModRes/"))
            {
                real = "ModRes/" + real;
            }
            real = ResManager.EditorResLoader.CheckDistributePath(real);
            return real;
        }

        public static readonly HashSet<string> ScriptAssetExts = new HashSet<string>() { ".cs", ".js", ".boo" };
        public static bool IsAssetScript(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            //if (path.Contains("/ModSpt/")) return true;
            var ext = System.IO.Path.GetExtension(path);
            return ScriptAssetExts.Contains(ext);
        }
    }
}
