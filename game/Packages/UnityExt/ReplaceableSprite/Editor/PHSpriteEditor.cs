﻿using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngineEx;

namespace UnityEditorEx
{
    [InitializeOnLoad]
    public static class PHSpriteEditor
    {
        static PHSpriteEditor()
        {
            if (LoadCachedSpriteReplacement())
            {
                CheckUpdatedPHSpriteSource();
            }
            else
            {
                CacheAllSpriteReplacement();
            }
            ModEditor.ShouldAlreadyInit();
            PackageEditor.OnPackagesChanged += CheckDistributeFlagsAndSpriteReplacement;
            DistributeEditor.OnDistributeFlagsChanged += CheckDistributeFlagsAndSpriteReplacement;
        }

        private static void AddGitIgnore(string assetpath)
        {
            var gitpath = System.IO.Path.GetDirectoryName(assetpath) + "/.gitignore";
            ModEditorUtils.AddGitIgnore(gitpath, System.IO.Path.GetFileName(assetpath), System.IO.Path.GetFileName(assetpath) + ".meta");
        }
        private static void RemoveGitIgnore(string assetpath)
        {
            var gitpath = System.IO.Path.GetDirectoryName(assetpath) + "/.gitignore";
            ModEditorUtils.RemoveGitIgnore(gitpath, System.IO.Path.GetFileName(assetpath), System.IO.Path.GetFileName(assetpath) + ".meta");
        }
        public static void CreateReplaceableSprite(string assetpath)
        {
            var source = System.IO.Path.GetDirectoryName(assetpath) + "/." + System.IO.Path.GetFileName(assetpath);
            if (System.IO.File.Exists(source))
            {
                Debug.LogWarning("Already created replaceable sprite for " + assetpath);
            }
            else
            {
                string type, mod, dist;
                var norm = ResManager.GetAssetNormPath(assetpath, out type, out mod, out dist);
                if (type != "res")
                {
                    Debug.LogError("Can only create replaceable sprite in ModRes folder. Current: " + assetpath);
                }
                else
                {
                    if (!string.IsNullOrEmpty(mod) && ModEditor.IsModOptional(mod) || !string.IsNullOrEmpty(dist))
                    {
                        Debug.LogError("Can only create replaceable sprite in non-mod & non-dist. Current: " + assetpath);
                    }
                    else
                    {
                        //var norm = ResInfoEditor.GetAssetNormPath(assetpath);
                        _CachedSpritePlaceHolder[norm] = assetpath;
                        _CachedSpriteReplacement[assetpath] = assetpath;

                        var desc = ScriptableObject.CreateInstance<PHSpriteDesc>();
                        desc.PHAssetMD5 = ModEditorUtils.GetFileMD5(assetpath) + "-" + ModEditorUtils.GetFileLength(assetpath);
                        AssetDatabaseUtils.CreateAssetSafe(desc, assetpath + ".phs.asset");
                        PlatDependant.CopyFile(assetpath, source);
                        var meta = assetpath + ".meta";
                        if (PlatDependant.IsFileExist(meta))
                        {
                            PlatDependant.CopyFile(meta, meta + ".~");
                        }

                        AddGitIgnore(assetpath);

                        CheckSpriteReplacement(norm);
                        SaveCachedSpriteReplacement();
                    }
                }
            }
        }
        [MenuItem("Assets/Create/Replaceable Sprite", priority = 2020)]
        public static void CreateReplaceableSprite()
        {
            bool found = false;
            var guids = Selection.assetGUIDs;
            if (guids != null)
            {
                for (int i = 0; i < guids.Length; ++i)
                {
                    var guid = guids[i];
                    var assetpath = AssetDatabase.GUIDToAssetPath(guid);
                    if (!string.IsNullOrEmpty(assetpath))
                    {
                        var ai = AssetImporter.GetAtPath(assetpath);
                        if (ai is TextureImporter)
                        {
                            found = true;
                            CreateReplaceableSprite(assetpath);
                        }
                    }
                }
            }
            if (!found)
            {
                Debug.Log("Cannot create replaceable sprite. No Texture2D or Sprite selected.");
            }
        }
        [MenuItem("Assets/Create/Replaceable Asset (Experimental)", priority = 2021)]
        public static void CreateReplaceableAsset()
        {
            bool found = false;
            var guids = Selection.assetGUIDs;
            if (guids != null)
            {
                for (int i = 0; i < guids.Length; ++i)
                {
                    var guid = guids[i];
                    var assetpath = AssetDatabase.GUIDToAssetPath(guid);
                    if (!string.IsNullOrEmpty(assetpath))
                    {
                        if (PlatDependant.IsFileExist(assetpath))
                        {
                            found = true;
                            CreateReplaceableSprite(assetpath);
                        }
                    }
                }
            }
            if (!found)
            {
                Debug.Log("Cannot create replaceable asset. No asset selected.");
            }
        }

        internal readonly static List<string> _CachedDistributeFlags = new List<string>();
        // place holder -> replacement
        internal readonly static Dictionary<string, string> _CachedSpriteReplacement = new Dictionary<string, string>();
        // norm -> place holder full path
        internal readonly static Dictionary<string, string> _CachedSpritePlaceHolder = new Dictionary<string, string>();
        // place holder -> md5
        private readonly static Dictionary<string, string> _CachedPlaceHolderMD5 = new Dictionary<string, string>();

        private static bool LoadCachedSpriteReplacement()
        {
            // _CachedPlaceHolderMD5 is optional
            _CachedPlaceHolderMD5.Clear();
            if (PlatDependant.IsFileExist("EditorOutput/Runtime/phspritemd5.txt"))
            {
                string json = "";
                using (var sr = PlatDependant.OpenReadText("EditorOutput/Runtime/phspritemd5.txt"))
                {
                    json = sr.ReadToEnd();
                }
                try
                {
                    var jo = new JSONObject(json);
                    for (int i = 0; i < jo.list.Count; ++i)
                    {
                        var key = jo.keys[i];
                        var val = jo.list[i].str;
                        _CachedPlaceHolderMD5[key] = val;
                    }
                }
                catch { }
            }

            _CachedSpriteReplacement.Clear();
            _CachedSpritePlaceHolder.Clear();
            _CachedDistributeFlags.Clear();
            if (PlatDependant.IsFileExist("EditorOutput/Runtime/phsprite.txt"))
            {
                string json = "";
                using (var sr = PlatDependant.OpenReadText("EditorOutput/Runtime/phsprite.txt"))
                {
                    json = sr.ReadToEnd();
                }
                try
                {
                    var jo = new JSONObject(json);
                    try
                    {
                        var phr = jo["phsprites"] as JSONObject;
                        if (phr != null && phr.type == JSONObject.Type.OBJECT)
                        {
                            for (int i = 0; i < phr.list.Count; ++i)
                            {
                                var key = phr.keys[i];
                                var val = phr.list[i].str;
                                _CachedSpriteReplacement[key] = val;
                                var norm = ResInfoEditor.GetAssetNormPath(key);
                                _CachedSpritePlaceHolder[norm] = key;
                            }
                        }
                        var dists = jo["dflags"] as JSONObject;
                        if (dists != null && dists.type == JSONObject.Type.ARRAY)
                        {
                            for (int i = 0; i < dists.list.Count; ++i)
                            {
                                var val = dists.list[i].str;
                                _CachedDistributeFlags.Add(val);
                            }
                        }
                    }
                    catch { }
                }
                catch { }
                return true;
            }
            return false;
        }
        private static void SaveCachedSpriteReplacement()
        {
            SaveCachedPlaceHolderMD5();
            var jo = new JSONObject(JSONObject.Type.OBJECT);
            var phs = new JSONObject(JSONObject.Type.OBJECT);
            jo["phsprites"] = phs;
            foreach (var kvp in _CachedSpriteReplacement)
            {
                phs[kvp.Key] = JSONObject.CreateStringObject(kvp.Value);
            }
            var dflags = new JSONObject(JSONObject.Type.ARRAY);
            jo["dflags"] = dflags;
            for (int i = 0; i < _CachedDistributeFlags.Count; ++i)
            {
                dflags.Add(_CachedDistributeFlags[i]);
            }
            using (var sw = PlatDependant.OpenWriteText("EditorOutput/Runtime/phsprite.txt"))
            {
                sw.Write(jo.ToString(true));
            }
        }
        private static void SaveCachedPlaceHolderMD5()
        {
            if (_CachedPlaceHolderMD5.Count > 0)
            {
                var jo = new JSONObject(JSONObject.Type.OBJECT);
                foreach (var kvp in _CachedPlaceHolderMD5)
                {
                    jo[kvp.Key] = JSONObject.CreateStringObject(kvp.Value);
                }
                using (var sw = PlatDependant.OpenWriteText("EditorOutput/Runtime/phspritemd5.txt"))
                {
                    sw.Write(jo.ToString(true));
                }
            }
            else
            {
                PlatDependant.DeleteFile("EditorOutput/Runtime/phspritemd5.txt");
            }
        }
        private static void CacheAllSpriteReplacement()
        {
            var assets = AssetDatabase.GetAllAssetPaths();
            for (int i = 0; i < assets.Length; ++i)
            {
                var asset = assets[i];
                if (asset.EndsWith(".phs.asset"))
                {
                    string type, mod, dist;
                    ResManager.GetAssetNormPath(asset, out type, out mod, out dist);
                    if (type == "res" && (string.IsNullOrEmpty(mod) || !ModEditor.IsModOptional(mod)) && string.IsNullOrEmpty(dist))
                    {
                        AddPHSprite(asset);
                    }
                }
            }
        }

        /// <summary>
        /// we moved the place holder XXX.png to .XXX.png, which will not monitored by unity editor.
        /// So we need store the md5 of .XXX.png to desc file.
        /// when we push the changed desc file to git repository, other developers could know the .XXX.png was changed in unity editor
        /// </summary>
        [MenuItem("Res/Check Replaceable Sprite Updated", priority = 202000)]
        public static void CheckUpdatedPHSpriteSource()
        {
            foreach (var kvp in _CachedSpriteReplacement)
            {
                CheckUpdatedPHSpriteSource(kvp.Key);
            }
        }
        /// <summary>
        /// we moved the place holder XXX.png to .XXX.png, which will not monitored by unity editor.
        /// So we need store the md5 of .XXX.png to desc file.
        /// when we push the changed desc file to git repository, other developers could know the .XXX.png was changed in unity editor
        /// </summary>
        private static void CheckUpdatedPHSpriteSource(string phasset)
        {
            var source = System.IO.Path.GetDirectoryName(phasset) + "/." + System.IO.Path.GetFileName(phasset);
            if (PlatDependant.IsFileExist(source))
            {
                var md5 = ModEditorUtils.GetFileMD5(source) + "-" + ModEditorUtils.GetFileLength(source);
                var descpath = phasset + ".phs.asset";
                RecordPHSpriteSourceMD5(descpath, md5);

                string rep;
                if (_CachedSpriteReplacement.TryGetValue(phasset, out rep) && rep == phasset)
                {
                    var oldmd5 = ModEditorUtils.GetFileMD5(phasset) + "-" + ModEditorUtils.GetFileLength(phasset);
                    if (oldmd5 != md5)
                    {
                        PlatDependant.CopyFile(source, phasset);
                        AssetDatabaseUtils.ForceImportAssetSafe(phasset);
                    }
                }
            }
        }
        private static void RecordPHSpriteSourceMD5(string descpath, string md5)
        {
            PHSpriteDesc desc = null;
            if (!PlatDependant.IsFileExist(descpath) || (desc = AssetDatabase.LoadAssetAtPath<PHSpriteDesc>(descpath)) == null)
            {
                desc = ScriptableObject.CreateInstance<PHSpriteDesc>();
                desc.PHAssetMD5 = md5;
                AssetDatabaseUtils.CreateAssetSafe(desc, descpath);
            }
            else
            {
                if (desc.PHAssetMD5 != md5)
                {
                    desc.PHAssetMD5 = md5;
                    EditorUtility.SetDirty(desc);
                    AssetDatabaseUtils.SaveChangedAssetsSafe();
                }
            }
        }
        private static void CheckAndRecordPHSpriteSourceMD5(string phasset)
        {
            var source = System.IO.Path.GetDirectoryName(phasset) + "/." + System.IO.Path.GetFileName(phasset);
            if (PlatDependant.IsFileExist(source))
            {
                var md5 = ModEditorUtils.GetFileMD5(source) + "-" + ModEditorUtils.GetFileLength(source);
                var descpath = phasset + ".phs.asset";
                RecordPHSpriteSourceMD5(descpath, md5);
            }
        }

        private static bool AddPHSprite(string descpath)
        {
            if (!string.IsNullOrEmpty(descpath) && descpath.EndsWith(".phs.asset"))
            {
                var asset = descpath.Substring(0, descpath.Length - ".phs.asset".Length);
                if (!_CachedSpriteReplacement.ContainsKey(asset))
                {
                    var norm = ResInfoEditor.GetAssetNormPath(asset);
                    _CachedSpritePlaceHolder[norm] = asset;
                    _CachedSpriteReplacement[asset] = "";
                    CheckSpriteReplacement(norm);
                    return true;
                }
                else
                {
                    CheckUpdatedPHSpriteSource(asset);
                }
            }
            return false;
        }
        private static void RemovePHSprite(string descpath)
        {
            if (!string.IsNullOrEmpty(descpath) && descpath.EndsWith(".phs.asset"))
            {
                var asset = descpath.Substring(0, descpath.Length - ".phs.asset".Length);
                var norm = ResInfoEditor.GetAssetNormPath(asset);
                _CachedSpritePlaceHolder.Remove(norm);
                _CachedSpriteReplacement.Remove(asset);
                _CachedPlaceHolderMD5.Remove(asset);
            }
        }
        private static void DeletePHSprite(string descpath)
        {
            RemovePHSprite(descpath);
            if (!string.IsNullOrEmpty(descpath) && descpath.EndsWith(".phs.asset"))
            {
                var asset = descpath.Substring(0, descpath.Length - ".phs.asset".Length);
                var source = System.IO.Path.GetDirectoryName(asset) + "/." + System.IO.Path.GetFileName(asset);
                if (System.IO.File.Exists(source))
                {
                    System.IO.File.Delete(asset);
                    System.IO.File.Move(source, asset);
                    var phmetasrc = asset + ".meta.~";
                    if (PlatDependant.IsFileExist(phmetasrc))
                    {
                        PlatDependant.MoveFile(phmetasrc, asset + ".meta");
                    }

                    RemoveGitIgnore(asset);
                    AssetDatabaseUtils.ForceImportAssetSafe(asset);
                }
            }
        }

        private static bool CheckDistributeFlags()
        {
            var flags = ResManager.GetValidDistributeFlags();
            if (flags.Length != _CachedDistributeFlags.Count)
            {
                return true;
            }
            for (int i = 0; i < flags.Length; ++i)
            {
                if (flags[i] != _CachedDistributeFlags[i])
                {
                    return true;
                }
            }
            return false;
        }
        private static void CheckDistributeFlagsAndSpriteReplacement()
        {
            if (CheckDistributeFlags())
            {
                foreach (var kvp in _CachedSpritePlaceHolder)
                {
                    CheckSpriteReplacement(kvp.Key);
                }
                _CachedDistributeFlags.Clear();
                _CachedDistributeFlags.AddRange(ResManager.GetValidDistributeFlags());
                SaveCachedSpriteReplacement();
            }
        }

        private static bool CheckSpriteReplacement(string phnorm)
        {
            string phasset;
            if (_CachedSpritePlaceHolder.TryGetValue(phnorm, out phasset))
            {
                var real = ResManager.EditorResLoader.CheckDistributePath("ModRes/" + phnorm, true);
                if (string.IsNullOrEmpty(real))
                {
                    real = phasset;
                }
                bool phassetexist;
                if (!(phassetexist = PlatDependant.IsFileExist(phasset)) || _CachedSpriteReplacement[phasset] != real)
                {
                    if (!phassetexist)
                    {
                        CheckAndRecordPHSpriteSourceMD5(phasset);
                    }
                    _CachedSpriteReplacement[phasset] = real;
                    var phmeta = phasset + ".meta";
                    if (!PlatDependant.IsFileExist(phmeta))
                    {
                        var phmetasrc = phmeta + ".~";
                        if (PlatDependant.IsFileExist(phmetasrc))
                        {
                            PlatDependant.CopyFile(phmetasrc, phmeta);
                        }
                    }
                    if (real == phasset)
                    {
                        var source = System.IO.Path.GetDirectoryName(phasset) + "/." + System.IO.Path.GetFileName(phasset);
                        PlatDependant.CopyFile(source, phasset);
                    }
                    else
                    {
                        PlatDependant.CopyFile(real, phasset);
                    }
                    AssetDatabaseUtils.ForceImportAssetSafe(phasset);
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// the placeholder.png can be reimported in 3 conditions:
        /// 1) AssetDatabase.ImportAsset(...., ForceUpdate) or user do an reimport in asset context menu. this is filtered by _CachedPlaceHolderMD5. (the content of the asset have not actually changed).
        /// 2) We replaced the placeholder with some other image in selected mod / dist. this is filtered by comparing placeholder's md5 with target's md5.
        /// 3) We edit the placeholder.png outside unity editor or we copy new file to overwrite it in Explorer/Finder. This mean we want to change the original placeholder.png's content. So we need to copy it to .placeholder.png
        /// </summary>
        private static void CheckPHSpriteChangedOutsideEditor(string phasset)
        {
            if (_CachedSpriteReplacement.ContainsKey(phasset))
            {
                var phmd5 = ModEditorUtils.GetFileMD5(phasset) + "-" + ModEditorUtils.GetFileLength(phasset);
                if (!_CachedPlaceHolderMD5.ContainsKey(phasset) || _CachedPlaceHolderMD5[phasset] != phmd5)
                {
                    _CachedPlaceHolderMD5[phasset] = phmd5;
                    SaveCachedPlaceHolderMD5();
                    var source = System.IO.Path.GetDirectoryName(phasset) + "/." + System.IO.Path.GetFileName(phasset);
                    if (PlatDependant.IsFileExist(source))
                    {
                        var srcmd5 = ModEditorUtils.GetFileMD5(source) + "-" + ModEditorUtils.GetFileLength(source);
                        if (phmd5 == srcmd5)
                        {
                            return;
                        }
                    }
                    var target = _CachedSpriteReplacement[phasset];
                    if (target != phasset && !string.IsNullOrEmpty(target) && PlatDependant.IsFileExist(target))
                    {
                        var tarmd5 = ModEditorUtils.GetFileMD5(target) + "-" + ModEditorUtils.GetFileLength(target);
                        if (phmd5 == tarmd5)
                        {
                            return;
                        }
                    }

                    PlatDependant.CopyFile(phasset, source);
                    RecordPHSpriteSourceMD5(phasset + ".phs.asset", phmd5);
                    _CachedSpriteReplacement[phasset] = phasset;
                    CheckSpriteReplacement(ResInfoEditor.GetAssetNormPath(phasset));
                }
            }
        }

        internal static void RestoreAllReplacement()
        {
            foreach (var kvp in _CachedSpriteReplacement)
            {
                var phasset = kvp.Key;
                var source = System.IO.Path.GetDirectoryName(phasset) + "/." + System.IO.Path.GetFileName(phasset);
                if (PlatDependant.IsFileExist(source))
                {
                    PlatDependant.CopyFile(source, phasset);
                    AssetDatabaseUtils.ForceImportAssetSafe(phasset);
                }
            }
        }
        internal static void RemakeAllReplacement()
        {
            foreach (var kvp in _CachedSpriteReplacement)
            {
                var phasset = kvp.Key;
                var source = kvp.Value;
                if (PlatDependant.IsFileExist(source))
                {
                    PlatDependant.CopyFile(source, phasset);
                    AssetDatabaseUtils.ForceImportAssetSafe(phasset);
                }
            }
        }

        private class PHSpritePostprocessor : AssetPostprocessor
        {
            private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                HashSet<string> added = new HashSet<string>();
                HashSet<string> deleted = new HashSet<string>();
                if (importedAssets != null)
                {
                    added.UnionWith(importedAssets);
                }
                if (deletedAssets != null)
                {
                    deleted.UnionWith(deletedAssets);
                }
                if (movedAssets != null)
                {
                    added.UnionWith(movedAssets);
                }
                if (movedFromAssetPaths != null)
                {
                    deleted.UnionWith(movedFromAssetPaths);
                }

                bool dirty = false;
                foreach (var asset in added)
                {
                    if (asset.EndsWith(".phs.asset"))
                    {
                        dirty |= AddPHSprite(asset);
                        var phasset = asset.Substring(0, asset.Length - ".phs.asset".Length);
                        AddGitIgnore(phasset);
                    }
                    else //if (AssetImporter.GetAtPath(asset) is TextureImporter)
                    {
                        var norm = ResInfoEditor.GetAssetNormPath(asset);
                        if (_CachedSpritePlaceHolder.ContainsKey(norm))
                        {
                            if (!_CachedSpriteReplacement.ContainsKey(asset))
                            { // the dist image.
                                dirty |= CheckSpriteReplacement(norm);
                            }
                            else
                            { // the ph image.
                                var phdesc = asset + ".phs.asset";
                                if (!deleted.Contains(phdesc)) // from git. the ph-sprite is changed to normal sprite.
                                { // we want to change to content of the ph-sprite, we need to sync it to the backup.
                                    CheckPHSpriteChangedOutsideEditor(asset);
                                }
                            }
                        }
                    }
                }
                foreach (var asset in deleted)
                {
                    if (asset.EndsWith(".phs.asset"))
                    {
                        dirty = true;
                        DeletePHSprite(asset); // restore ph sprite to normal sprite.
                        var phasset = asset.Substring(0, asset.Length - ".phs.asset".Length);
                        if (deleted.Contains(phasset))
                        { // we deleted ph-desc and ph-image, in Editor. the backup of the ph-image is still there. we need to delete all.
                            PlatDependant.DeleteFile(phasset);
                        }
                        RemoveGitIgnore(phasset);
                    }
                    else
                    {
                        var norm = ResInfoEditor.GetAssetNormPath(asset);
                        if (_CachedSpritePlaceHolder.ContainsKey(norm))
                        {
                            var phasset = _CachedSpritePlaceHolder[norm];
                            var phdesc = phasset + ".phs.asset";
                            if (!deleted.Contains(phdesc)) // which means we made a full delete.
                            { // check if the dist sprite is changed and will we make a sync
                                dirty |= CheckSpriteReplacement(norm);
                            }
                        }
                    }
                }
                if (dirty)
                {
                    SaveCachedSpriteReplacement();
                }
            }
        }
    }
}