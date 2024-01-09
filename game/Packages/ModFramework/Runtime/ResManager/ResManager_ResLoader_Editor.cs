using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;

namespace UnityEngineEx
{
    public static partial class ResManager
    {
#if UNITY_EDITOR
        public class EditorResLoader : IResLoader
        {
            public interface IEditorResLoaderEx
            {
                string FindFile(string path);
            }
            public static List<IEditorResLoaderEx> ExLoaders = new List<IEditorResLoaderEx>();

            public EditorResLoader()
            {
                bool useclientloader = false;
#if FORCE_USE_CLIENT_RESLOADER
                useclientloader = true;
#endif
                if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || !useclientloader)
                {
                    ResLoader = this;
                }
                else
                {
                    UnityEditor.EditorApplication.playModeStateChanged += e =>
                    {
                        if (e == UnityEditor.PlayModeStateChange.ExitingPlayMode)
                        {
                            ResLoader = this;
                        }
                    };
                }
                OnRebuildRuntimeResCache += RebuildRuntimeResCache;
            }
            //public void OnEnable() { }
            public void BeforeLoadFirstScene() { }
            public void AfterLoadFirstScene() { }

            public class RuntimeCache
            {
                public Dictionary<string, string> Mapping = new Dictionary<string, string>();
                public Dictionary<string, string> ModToPackage = new Dictionary<string, string>();
                public List<string> CriticalMods = new List<string>();
                public List<string> DFlags = new List<string>();

                public void Init()
                {
                    DFlags.AddRange(GetValidDistributeFlags());
                    CriticalMods.AddRange(EditorToClientUtils.GetCriticalMods());
                    for (int i = 0; i < DFlags.Count; ++i)
                    {
                        var flag = DFlags[i];
                        ModToPackage[flag] = EditorToClientUtils.GetPackageNameFromModName(flag);
                    }
                    for (int i = 0; i < CriticalMods.Count; ++i)
                    {
                        var flag = CriticalMods[i];
                        ModToPackage[flag] = EditorToClientUtils.GetPackageNameFromModName(flag);
                    }
                }
            }
            private class RuntimeCachePostprocessor : UnityEditor.AssetPostprocessor
            {
                public override int GetPostprocessOrder()
                {
                    return int.MinValue;
                }
                private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
                {
                    RebuildRuntimeResCache();
                }
            }
            private static RuntimeCache _RuntimeCache = new RuntimeCache();
            public static RuntimeCache ResRuntimeCache { get { return _RuntimeCache; } }
            public static void RebuildRuntimeResCache()
            {
                _RuntimeCache = new RuntimeCache();
                _RuntimeCache.Init();
            }
            public static string CheckModPath(string path)
            {
                return CheckModPath(path, false);
            }
            public static string CheckModPath(string path, bool ignoreExFinder)
            {
                string found = null;
                Func<string, bool> checkFile = file =>
                {
                    bool exist = PlatDependant.IsFileExist(file);
                    if (exist)
                    {
#if EDITOR_LOADER_NO_CHECK
                        found = file;
                        return true;
#endif
                        if (found == null)
                        {
                            found = file;
                        }
                        else
                        {
                            Debug.LogWarning("Duplicated item: " + found + "\nReplaces: " + file);
                        }
                        return true;
                    }

                    string exfound = null;
                    for (int i = 0; i < ExLoaders.Count; ++i)
                    {
                        var exloader = ExLoaders[i];
                        exfound = exloader.FindFile(file);
                        if (exfound != null && PlatDependant.IsFileExist(exfound))
                        {
#if EDITOR_LOADER_NO_CHECK
                            found = exfound;
                            return true;
#endif
                            if (found == null)
                            {
                                found = exfound;
                            }
                            else
                            {
                                Debug.LogWarning("Duplicated item: " + found + "\nReplaces: " + exfound);
                            }
                            return true;
                        }
                    }
                    return false;
                };

                var dflags = _RuntimeCache.DFlags;
                for (int i = dflags.Count - 1; i >= 0; --i)
                {
                    var dflag = dflags[i];
                    string package;
                    _RuntimeCache.ModToPackage.TryGetValue(dflag, out package);
                    if (!string.IsNullOrEmpty(package))
                    {
#if EDITOR_LOAD_RAW_RES
                        {
                            var realpath = "Packages/" + package + "/Raw/" + path;
#if EDITOR_LOADER_NO_CHECK
                            if (checkFile(realpath))
                            {
                                return found;
                            }
#else
                            checkFile(realpath);
#endif
                        }
#endif
                        {
                            var realpath = "Packages/" + package + "/" + path;
#if EDITOR_LOADER_NO_CHECK
                            if (checkFile(realpath))
                            {
                                return found;
                            }
#else
                            checkFile(realpath);
#endif
                        }
                    }
#if EDITOR_LOAD_RAW_RES
                    {
                        var realpath = "Assets/Mods/" + dflag + "/Raw/" + path;
#if EDITOR_LOADER_NO_CHECK
                        if (checkFile(realpath))
                        {
                            return found;
                        }
#else
                        checkFile(realpath);
#endif
                    }
#endif
                    {
                        var realpath = "Assets/Mods/" + dflag + "/" + path;
#if EDITOR_LOADER_NO_CHECK
                        if (checkFile(realpath))
                        {
                            return found;
                        }
#else
                        checkFile(realpath);
#endif
                    }
                }
                var cflags = _RuntimeCache.CriticalMods;
                for (int i = cflags.Count - 1; i >= 0; --i)
                {
                    var dflag = cflags[i];
                    string package;
                    _RuntimeCache.ModToPackage.TryGetValue(dflag, out package);
                    if (!string.IsNullOrEmpty(package))
                    {
#if EDITOR_LOAD_RAW_RES
                        {
                            var realpath = "Packages/" + package + "/Raw/" + path;
#if EDITOR_LOADER_NO_CHECK
                            if (checkFile(realpath))
                            {
                                return found;
                            }
#else
                            checkFile(realpath);
#endif
                        }
#endif
                        {
                            var realpath = "Packages/" + package + "/" + path;
#if EDITOR_LOADER_NO_CHECK
                            if (checkFile(realpath))
                            {
                                return found;
                            }
#else
                            checkFile(realpath);
#endif
                        }
                    }
#if EDITOR_LOAD_RAW_RES
                    {
                        var realpath = "Assets/Mods/" + dflag + "/Raw/" + path;
#if EDITOR_LOADER_NO_CHECK
                        if (checkFile(realpath))
                        {
                            return found;
                        }
#else
                        checkFile(realpath);
#endif
                    }
#endif
                    {
                        var realpath = "Assets/Mods/" + dflag + "/" + path;
#if EDITOR_LOADER_NO_CHECK
                        if (checkFile(realpath))
                        {
                            return found;
                        }
#else
                        checkFile(realpath);
#endif
                    }
                }
#if EDITOR_LOAD_RAW_RES
                {
                    var realpath = "Assets/Raw/" + path;
#if EDITOR_LOADER_NO_CHECK
                    if (checkFile(realpath))
                    {
                        return found;
                    }
#else
                    checkFile(realpath);
#endif
                }
#endif
                {
                    var realpath = "Assets/" + path;
#if EDITOR_LOADER_NO_CHECK
                    if (checkFile(realpath))
                    {
                        return found;
                    }
#else
                    checkFile(realpath);
#endif
                }
                return found;
            }
            private static string[] _DistributeFolderNames = new[] { "ModRes/", "ModSpt/", "Resources/" };
            public static string CheckDistributePath(string path)
            {
                return CheckDistributePath(path, false);
            }
            public static string CheckDistributePath(string path, bool noWarningWhenNotFound)
            {
                return CheckDistributePath(path, noWarningWhenNotFound, false);
            }
            public static string CheckDistributePath(string path, bool noWarningWhenNotFound, bool ignoreExFinder)
            {
                string found = null;
                if (_RuntimeCache.Mapping.TryGetValue(path, out found))
                {
                    return found;
                }
                string distFolderName = null;
                for (int i = 0; i < _DistributeFolderNames.Length; ++i)
                {
                    var folder = _DistributeFolderNames[i];
                    if (path.StartsWith(folder))
                    {
                        distFolderName = folder;
                        break;
                    }
                }

                if (distFolderName != null)
                {
                    var dflags = _RuntimeCache.DFlags;
                    for (int i = dflags.Count - 1; i >= 0; --i)
                    {
                        var dflag = dflags[i];
                        var realpath = distFolderName + "dist/" + dflag + path.Substring(distFolderName.Length - 1);
                        var dfound = CheckModPath(realpath, ignoreExFinder);
                        if (dfound != null)
                        {
#if EDITOR_LOADER_NO_CHECK
                            _RuntimeCache.Mapping[path] = dfound;
                            return dfound;
#endif
                            if (found == null)
                            {
                                found = dfound;
                            }
                            else
                            {
                                Debug.LogWarning("Duplicated item: " + found + "\nReplaces: " + dfound);
                            }
                        }
                    }
                }
                {
                    var dfound = CheckModPath(path, ignoreExFinder);
                    if (dfound != null)
                    {
#if EDITOR_LOADER_NO_CHECK
                        _RuntimeCache.Mapping[path] = dfound;
                        return dfound;
#endif
                        if (found == null)
                        {
                            found = dfound;
                        }
                        else
                        {
                            Debug.LogWarning("Duplicated item: " + found + "\nReplaces: " + dfound);
                        }
                    }
                }
#if EDITOR_LOADER_NO_CHECK
                _RuntimeCache.Mapping[path] = null;
                return null;
#endif
                if (found == null)
                {
                    if (!noWarningWhenNotFound)
                    {
                        Debug.LogWarning("Not found: " + path);
                    }
                }
                else
                {
                    try
                    {
                        var guid = UnityEditor.AssetDatabase.AssetPathToGUID(found);
                        if (string.IsNullOrEmpty(guid))
                        {
                            Debug.LogError("Unable to find asset (case error?): " + found);
                        }
                        var ondisk = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                        if (found != ondisk)
                        {
                            Debug.LogError("File name case error. Loading: " + found + "\nOnDisk: " + (ondisk ?? "??"));
                        }
                    }
                    catch { } // If it is called inside ctor of MonoBehaviour, AssetPathToGUID will fail.
                }
                _RuntimeCache.Mapping[path] = found;
                OnResFound(path, found);
                return found;
            }
            public static string CheckModPathSafe(string path)
            {
                string found = null;
                Func<string, bool> checkFile = file =>
                {
                    bool exist = PlatDependant.IsFileExist(file);
                    if (exist)
                    {
                        found = file;
                        return true;
                    }
                    return false;
                };

                var dflags = _RuntimeCache.DFlags;
                for (int i = dflags.Count - 1; i >= 0; --i)
                {
                    var dflag = dflags[i];
                    string package;
                    _RuntimeCache.ModToPackage.TryGetValue(dflag, out package);
                    if (!string.IsNullOrEmpty(package))
                    {
#if EDITOR_LOAD_RAW_RES
                        {
                            var realpath = "Packages/" + package + "/Raw/" + path;
                            if (checkFile(realpath))
                            {
                                return found;
                            }
                        }
#endif
                        {
                            var realpath = "Packages/" + package + "/" + path;
                            if (checkFile(realpath))
                            {
                                return found;
                            }
                        }
                    }
#if EDITOR_LOAD_RAW_RES
                    {
                        var realpath = "Assets/Mods/" + dflag + "/Raw/" + path;
                        if (checkFile(realpath))
                        {
                            return found;
                        }
                    }
#endif
                    {
                        var realpath = "Assets/Mods/" + dflag + "/" + path;
                        if (checkFile(realpath))
                        {
                            return found;
                        }
                    }
                }
                var cflags = _RuntimeCache.CriticalMods;
                for (int i = cflags.Count - 1; i >= 0; --i)
                {
                    var dflag = cflags[i];
                    string package;
                    _RuntimeCache.ModToPackage.TryGetValue(dflag, out package);
                    if (!string.IsNullOrEmpty(package))
                    {
#if EDITOR_LOAD_RAW_RES
                        {
                            var realpath = "Packages/" + package + "/Raw/" + path;
                            if (checkFile(realpath))
                            {
                                return found;
                            }
                        }
#endif
                        {
                            var realpath = "Packages/" + package + "/" + path;
                            if (checkFile(realpath))
                            {
                                return found;
                            }
                        }
                    }
#if EDITOR_LOAD_RAW_RES
                    {
                        var realpath = "Assets/Mods/" + dflag + "/Raw/" + path;
                        if (checkFile(realpath))
                        {
                            return found;
                        }
                    }
#endif
                    {
                        var realpath = "Assets/Mods/" + dflag + "/" + path;
                        if (checkFile(realpath))
                        {
                            return found;
                        }
                    }
                }
#if EDITOR_LOAD_RAW_RES
                {
                    var realpath = "Assets/Raw/" + path;
                    if (checkFile(realpath))
                    {
                        return found;
                    }
                }
#endif
                {
                    var realpath = "Assets/" + path;
                    if (checkFile(realpath))
                    {
                        return found;
                    }
                }
                return found;
            }
            public static string CheckDistributePathSafe(string path)
            {
                return CheckDistributePathSafe(null, path);
            }
            public static string CheckDistributePathSafe(string prefix, string file)
            {
                string distFolderName = prefix;
                string normal = file;
                if (distFolderName == null)
                {
                    for (int i = 0; i < _DistributeFolderNames.Length; ++i)
                    {
                        var folder = _DistributeFolderNames[i];
                        if (file.StartsWith(folder))
                        {
                            distFolderName = folder;
                            normal = file.Substring(distFolderName.Length);
                            break;
                        }
                    }
                }
                else if (distFolderName.Length > 0 && !distFolderName.EndsWith("/") && !distFolderName.EndsWith("/"))
                {
                    distFolderName += "/";
                }

                if (distFolderName != null)
                {
                    var dflags = _RuntimeCache.DFlags;
                    for (int i = dflags.Count - 1; i >= 0; --i)
                    {
                        var dflag = dflags[i];
                        var realpath = distFolderName + "dist/" + dflag + "/" + normal;
                        var dfound = CheckModPathSafe(realpath);
                        if (dfound != null)
                        {
                            return dfound;
                        }
                    }
                    {
                        var dfound = CheckModPathSafe(distFolderName + normal);
                        if (dfound != null)
                        {
                            return dfound;
                        }
                    }
                }
                else
                {
                    var dfound = CheckModPathSafe(normal);
                    if (dfound != null)
                    {
                        return dfound;
                    }
                }
                return null;
            }

            public static event Action<string, string> OnResFound = (norm, real) => { };

            public static Object LoadMainAsset(string name)
            {
                Object rv = null;
                try
                {
                    rv = UnityEditor.AssetDatabase.LoadMainAssetAtPath(name);
                }
                catch { }
                if (rv == null || rv is GameObject || rv is Font)
                {
                    return rv;
                }
                if (rv is Texture2D)
                {
                    var assets = UnityEditor.AssetDatabase.LoadAllAssetRepresentationsAtPath(name);
                    if (assets != null && assets.Length > 0)
                    {
                        return assets[0];
                    }
                }
                return rv;
            }
            public static Object LoadAsset(string asset, Type type)
            {
#if COMPATIBLE_RESMANAGER_V1
                asset = CompatibleAssetName(asset);
#endif
                var found = CheckDistributePath("ModRes/" + asset);
                if (found != null)
                {
                    if (type == null)
                    {
                        return LoadMainAsset(found);
                    }
                    else
                    {
                        return UnityEditor.AssetDatabase.LoadAssetAtPath(found, type);
                    }
                }
                return null;
            }
            public static void LoadSceneImmediate(string name, bool additive)
            {
#if COMPATIBLE_RESMANAGER_V1
                name = CompatibleAssetName(name);
#endif
                var found = CheckDistributePath("ModRes/" + name);
                if (found != null)
                {
                    if (additive)
                    {
                        UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(found, new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Additive));
                    }
                    else
                    {
                        UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(found, new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
                    }
                }
                else
                {
                    if (System.IO.File.Exists(name))
                    {
                        if (additive)
                        {
                            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(name, new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Additive));
                        }
                        else
                        {
                            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(name, new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
                        }
                    }
                }
            }

            public static AsyncOperation LoadSceneAsyncInPlayMode(string name, bool additive)
            {
#if COMPATIBLE_RESMANAGER_V1
                name = CompatibleAssetName(name);
#endif
                var found = CheckDistributePath("ModRes/" + name);
                AsyncOperation op = null;
                if (found != null)
                {
                    if (additive)
                    {
                        op = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(found, new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Additive));
                    }
                    else
                    {
                        op = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(found, new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
                    }
                }
                else
                {
                    if (System.IO.File.Exists(name))
                    {
                        if (additive)
                        {
                            op = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(name, new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Additive));
                        }
                        else
                        {
                            op = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(name, new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
                        }
                    }
                }

                return op;
            }

            public static void EditorStartupPrepare()
            {
                // Currently, we need to do nothing.
            }

            public object Preload(string asset)
            {
                return null;
            }
            public Object LoadRes(string asset, Type type)
            {
                return LoadAsset(asset, type);
            }
            public void LoadScene(string name, bool additive)
            {
                LoadSceneImmediate(name, additive);
            }

            public CoroutineTasks.CoroutineWork LoadResAsync(string asset, Type type)
            {
                var work = new CoroutineTasks.CoroutineMonitorRaw();
                work.Result = LoadRes(asset, type);
                work.TryStart();
                return work;
            }
            public CoroutineTasks.CoroutineWork LoadSceneAsync(string name, bool additive)
            {
                AsyncOperation op = LoadSceneAsyncInPlayMode(name, additive);
                return new CoroutineTasks.CoroutineWorkAsyncOp(op);
            }

            public int Order { get { return LifetimeOrders.ResLoader; } }
            public void Prepare() { }
            public void Init()
            {
                EditorStartupPrepare();
            }
            public void Cleanup()
            {
            }
            public void UnloadUnusedRes()
            {
                UnityEditor.EditorUtility.UnloadUnusedAssetsImmediate();
            }
            public void UnloadAllRes(bool unloadPermanentBundle)
            {
                UnloadUnusedRes();
            }
            public void MarkPermanent(string assetname)
            {
            }
            public List<string> ParseRunningResKeys()
            {
                return null;
            }
            public List<string> GetLoadedBundleFileNames()
            {
                return null;
            }
            public void AfterResFilesDeployed()
            {
            }

            public static void UnloadAssets(Object[] objs)
            {
                foreach (var obj in objs)
                {
                    if (obj)
                    {
                        var path = UnityEditor.AssetDatabase.GetAssetPath(obj);
                        if (string.IsNullOrEmpty(path))
                        {
                            if (obj is Component)
                            {
                                Object.DestroyImmediate(((Component)obj).transform.root.gameObject);
                            }
                            else
                            {
                                Object.DestroyImmediate(obj);
                            }
                        }
                        else
                        {
                            // TODO: unload prefab parts by prefab root
                            Resources.UnloadAsset(obj);
                        }
                    }
                }
            }
        }
        public static EditorResLoader EditorResLoaderInstance = new EditorResLoader();
#endif
    }
}