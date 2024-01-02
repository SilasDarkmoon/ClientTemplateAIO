using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngineEx;

namespace UnityEditorEx
{
    public static class DistributeEditor
    {
        internal static string GetDistributeFlagsFilePath()
        {
            //var mod = ModEditorUtils.__MOD__;
            //string path = ModEditor.GetPackageOrModRoot(mod);
            //if (!string.IsNullOrEmpty(path))
            //{
            //    path += "/Resources";
            //    System.IO.Directory.CreateDirectory(path);
            //    path += "/DistributeFlags.txt";
            //}
            //else
            //{
            //    path = "Assets/Resources/DistributeFlags.txt";
            //}
            //return path;
            return "Assets/Resources/DistributeFlags.txt";
        }

        public static void CheckDefaultSelectedDistributeFlags()
        {
            var path = GetDistributeFlagsFilePath();
            if (!System.IO.File.Exists(path))
            {
                var src = ModEditor.FindAssetInMods("DefaultDistributeFlags.txt", true);
                if (src != null && System.IO.File.Exists(src))
                {
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                    System.IO.File.Copy(src, path);
                    AssetDatabase.ImportAsset(path);
                    return;
                }
            }
        }

        public static string[] GetDistributesInMod(string mod)
        {
            List<string> dflags = new List<string>();
            HashSet<string> setflags = new HashSet<string>();

            Action<string> checkDir = pathc =>
            {
                if (System.IO.Directory.Exists(pathc))
                {
                    var dirs = System.IO.Directory.GetDirectories(pathc);
                    foreach (var dir in dirs)
                    {
                        var path = System.IO.Path.GetFileName(dir);
                        if (setflags.Add(path))
                        {
                            dflags.Add(path);
                        }
                    }
                }
            };
            Action<string> checkSub = sub =>
            {
                if (string.IsNullOrEmpty(mod))
                {
                    checkDir("Assets" + sub);
                }
                else
                {
                    var proot = ModEditor.GetModRootInPackage(mod);
                    if (!string.IsNullOrEmpty(proot))
                    {
                        checkDir(proot + sub);
                    }
                    checkDir("Assets/Mods/" + mod + sub);
                }
            };

            checkSub("/ModRes/dist/");
            checkSub("/ModSpt/dist/");
            checkSub("/Resources/dist/");

            return dflags.ToArray();
        }
        public static string[] GetAllDistributes()
        {
            List<string> dflags = new List<string>();
            HashSet<string> setflags = new HashSet<string>();

            var subflags = GetDistributesInMod("");
            for (int j = 0; j < subflags.Length; ++j)
            {
                var subflag = subflags[j];
                if (setflags.Add(subflag))
                {
                    dflags.Add(subflag);
                }
            }

            var mods = ModEditor.GetAllModsOrPackages();
            for (int i = 0; i < mods.Length; ++i)
            {
                subflags = GetDistributesInMod(mods[i]);
                for (int j = 0; j < subflags.Length; ++j)
                {
                    var subflag = subflags[j];
                    if (setflags.Add(subflag))
                    {
                        dflags.Add(subflag);
                    }
                }
            }

            return dflags.ToArray();
        }
        public static string[] GetOptionalDistributes()
        {
            HashSet<string> flags = new HashSet<string>(GetAllDistributes());
            flags.UnionWith(ModEditor.GetOptionalMods());
            var arrflags = flags.ToArray();
            Array.Sort(arrflags);
            return arrflags;
        }

        public static OrderedEvent<Action> OnDistributeFlagsChanged = (Action)(() => { });
        internal static void FireOnDistributeFlagsChanged()
        {
            OnDistributeFlagsChanged.Handler();
        }

        public static string[] FindAssetsInModsAndDists(string prefix, string file)
        {
            if (prefix == null)
            {
                prefix = "";
            }
            else if (prefix.Length > 0 && !prefix.EndsWith("/") && !prefix.EndsWith("\\"))
            {
                prefix += "/";
            }
            List<string> results = new List<string>();
            var alldflags = GetAllDistributes();
            for (int i = 0; i < alldflags.Length; ++i)
            {
                var dflag = alldflags[i];
                var path = prefix + "dist/" + dflag + "/" + file;
                results.AddRange(ModEditor.FindAssetsInMods(path));
            }
            results.AddRange(ModEditor.FindAssetsInMods(prefix + file));
            return results.ToArray();
        }

        private static string[] _CachedAllDFlags = null;
        public static string[] GetAllDistributesCached()
        {
            if (_CachedAllDFlags == null)
            {
                _CachedAllDFlags = GetAllDistributes();
            }
            return _CachedAllDFlags;
        }

        public static string FindDistributeDescFile(string dflag)
        {
            if (!string.IsNullOrEmpty(dflag))
            {
                string descfile = null;
                var distpart = "/dist/" + dflag + "/.desc.txt";
                var descpart = "ModRes" + distpart;
                descfile = ModEditor.FindAssetInMods(descpart);
                if (descfile != null)
                {
                    return descfile;
                }
                descpart = "ModSpt" + distpart;
                descfile = ModEditor.FindAssetInMods(descpart);
                if (descfile != null)
                {
                    return descfile;
                }

                descfile = "Assets/ModRes" + distpart;
                if (System.IO.File.Exists(descfile))
                {
                    return descfile;
                }
                descfile = "Assets/ModSpt" + distpart;
                if (System.IO.File.Exists(descfile))
                {
                    return descfile;
                }

                descfile = "Assets/Mods/" + dflag + "/.desc.txt";
                if (System.IO.File.Exists(descfile))
                {
                    return descfile;
                }

                var pname = ModEditor.GetPackageName(dflag);
                if (pname != null)
                {
                    descfile = "Packages/" + pname + "/.desc.txt";
                    if (System.IO.File.Exists(descfile))
                    {
                        return descfile;
                    }
                }
            }
            return null;
        }

        public struct DistDesc
        {
            public bool NoSelectNoBuild;
            public bool IsCritical;
            public bool ShouldIgnore;
            public string Title;
            public string Desc;
            public string Color;
            public int Order;
        }
        public static DistDesc GetDistributeDesc(string dflag)
        {
            DistDesc distdesc = new DistDesc();
            var descfile = FindDistributeDescFile(dflag);
            if (descfile != null)
            {
                if (System.IO.File.Exists(descfile))
                {
                    var content = System.IO.File.ReadAllText(descfile);
                    try
                    {
                        distdesc = JsonUtility.FromJson<DistDesc>(content);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
            return distdesc;
        }

        public static Dictionary<string, DistDesc> GetAllDistributeDescs()
        {
            var results = new Dictionary<string, DistDesc>();
            var dflags = GetAllDistributesCached();
            for (int i = 0; i < dflags.Length; ++i)
            {
                var dflag = dflags[i];
                var descfile = FindDistributeDescFile(dflag);
                if (descfile != null)
                {
                    if (System.IO.File.Exists(descfile))
                    {
                        var content = System.IO.File.ReadAllText(descfile);
                        try
                        {
                            var distdesc = JsonUtility.FromJson<DistDesc>(content);
                            results[dflag] = distdesc;
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                }
            }
            return results;
        }

        private class RefreshCachePostProcessor : AssetPostprocessor
        {
            private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                _CachedAllDFlags = null;
            }
        }
    }

    public class DistributeSelectWindow : EditorWindow, ISerializationCallbackReceiver
    {
        [MenuItem("Res/Select Distribute Flags", priority = 200000)]
        public static void Init()
        {
            var win = GetWindow<DistributeSelectWindow>();
            win.titleContent = new GUIContent("Distribute Flags");
            win.DistributeFlags = new Dictionary<string, bool>();
            win.DistributeFlagOrder = new LinkedList<string>();
            var allflags = DistributeEditor.GetOptionalDistributes();
            for (int i = 0; i < allflags.Length; ++i)
            {
                win.DistributeFlags[allflags[i]] = false;
            }
            var selflags = UnityEngineEx.DistributeFlags.PreRuntimeDFlags;
            for (int i = 0; i < selflags.Count; ++i)
            {
                win.SelectDistributeFlag(selflags[i], true);
            }
            win.SaveDistributeFlags();
            win.DistributeDescs = null;
            win.TryLoadDistributeDescs();
        }

        private void TryLoadDistributeDescs()
        {
            if (DistributeDescs == null)
            {
                SortedAllDistributeFlags.Clear();
                DistributeDescs = new Dictionary<string, DistributeEditor.DistDesc>();
                foreach (var kvp in DistributeFlags)
                {
                    var dflag = kvp.Key;
                    DistributeDescs[dflag] = DistributeEditor.GetDistributeDesc(dflag);
                    SortedAllDistributeFlags.Add(dflag);
                }
                SortedAllDistributeFlags.Sort((flaga, flagb) =>
                {
                    DistributeEditor.DistDesc desca = DistributeDescs[flaga];
                    DistributeEditor.DistDesc descb = DistributeDescs[flagb];
                    if (desca.ShouldIgnore != descb.ShouldIgnore)
                    {
                        return descb.ShouldIgnore ? -1 : 1;
                    }

                    var ordera = desca.Order;
                    var orderb = descb.Order;
                    if (ordera != orderb)
                    {
                        return ordera - orderb;
                    }
                    var ca = desca.Color;
                    var cb = descb.Color;
                    var cc = string.Compare(ca, cb);
                    if (cc != 0)
                    {
                        return -cc;
                    }
                    if (desca.IsCritical != descb.IsCritical)
                    {
                        return desca.IsCritical ? -1 : 1;
                    }

                    return string.Compare(flaga, flagb);
                });
            }
        }

        public Dictionary<string, bool> DistributeFlags = new Dictionary<string, bool>();
        public Dictionary<string, DistributeEditor.DistDesc> DistributeDescs = null;
        private List<string> SortedAllDistributeFlags = new List<string>();
        public LinkedList<string> DistributeFlagOrder = new LinkedList<string>();

        string[] OptionConfigs = new string[] { "0", "1", "2", "3" };
        int OptionConfigIndex = 0;

        public Vector2 offset1, offset2;

        private void RefreshConfig(string config, int configIndex)
        {
            DistributeFlags = new Dictionary<string, bool>();
            DistributeFlagOrder = new LinkedList<string>();

            var allflags = DistributeEditor.GetOptionalDistributes();
            for (int i = 0; i < allflags.Length; ++i)
            {
                DistributeFlags[allflags[i]] = false;
            }

            var selflags = string.IsNullOrEmpty(config) ? UnityEngineEx.DistributeFlags.PreRuntimeDFlags.ToArray() : config.Split('<');
            for (int i = 0; i < selflags.Length; ++i)
            {
                if (string.IsNullOrEmpty(selflags[i]))
                {
                    continue;
                }
                SelectDistributeFlag(selflags[i], true);
            }
        }

        void OnGUI()
        {
            TryLoadDistributeDescs();
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            if (GUILayout.Button("Reset"))
            {
                Init();
            }
            offset1 = GUILayout.BeginScrollView(offset1);
            for (int i = 0; i < SortedAllDistributeFlags.Count; ++i)
            {
                var dflag = SortedAllDistributeFlags[i];
                var selected = DistributeFlags[dflag];
                DistributeEditor.DistDesc desc;
                var togglesize = GUI.skin.toggle.CalcSize(GUIContent.none);
                var dflagcontent = new GUIContent(dflag);
                GUIStyle style = EditorStyles.label;
                if (DistributeDescs.TryGetValue(dflag, out desc))
                {
                    if (!string.IsNullOrEmpty(desc.Desc))
                    {
                        dflagcontent.tooltip = desc.Desc;
                    }
                    if (!string.IsNullOrEmpty(desc.Title))
                    {
                        dflagcontent.text += " \t(" + desc.Title + ")";
                    }
                    if (desc.IsCritical)
                    {
                        style = EditorStyles.boldLabel;
                    }
                    if (!string.IsNullOrEmpty(desc.Color))
                    {
                        Color color;
                        if (ColorUtility.TryParseHtmlString(desc.Color, out color))
                        {
                            var rect = GUILayoutUtility.GetRect(0, 0);
                            rect.width = togglesize.x + GUI.skin.toggle.margin.left;
                            rect.height = GUI.skin.toggle.border.top + GUI.skin.toggle.margin.top + GUI.skin.toggle.margin.top - GUI.skin.toggle.overflow.top;
                            EditorGUI.DrawRect(rect, color);
                        }
                    }
                }
                var option = GUILayout.Width(style.CalcSize(dflagcontent).x + togglesize.x);
                SelectDistributeFlag(dflag, EditorGUILayout.ToggleLeft(dflagcontent, selected, style, option));
                if (desc.ShouldIgnore)
                {
                    var rect = GUILayoutUtility.GetLastRect();
                    rect.yMin += Mathf.Ceil(rect.height / 2f);
                    rect.height = 1f;
                    EditorGUI.DrawRect(rect, GUI.color);
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            if (GUILayout.Button("Apply"))
            {
                SaveDistributeFlags(OptionConfigIndex);
                Close();
            }

            //abcd配置
            GUILayout.BeginHorizontal();
            var _index = GUILayout.SelectionGrid(OptionConfigIndex, OptionConfigs, Math.Min(10, OptionConfigs.Length), GUILayout.MinWidth(GUI.skin.button.CalcSize(new GUIContent("3")).x * Math.Min(10, OptionConfigs.Length)));
            if (_index != OptionConfigIndex)
            {
                OptionConfigIndex = _index;
                if (PlayerPrefs.HasKey("DistributeFlags_" + OptionConfigIndex))
                {
                    RefreshConfig(PlayerPrefs.GetString("DistributeFlags_" + OptionConfigIndex), OptionConfigIndex);
                }
            }
            GUILayout.EndHorizontal();

            offset2 = GUILayout.BeginScrollView(offset2);
            var node = DistributeFlagOrder.First;
            LinkedListNode<string> nodeup = null, nodedown = null, noderemove = null;
            while (node != null)
            {
                var curnode = node;
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("X", new GUIStyle(GUI.skin.button) { stretchWidth = false, fixedWidth = 25 })) noderemove = curnode;
                if (GUILayout.Button("▲", new GUIStyle(GUI.skin.button) { stretchWidth = false, fixedWidth = 25 })) nodeup = curnode;
                if (GUILayout.Button("▼", new GUIStyle(GUI.skin.button) { stretchWidth = false, fixedWidth = 25 })) nodedown = curnode;
                EditorGUILayout.LabelField(node.Value, GUILayout.Width(GUI.skin.label.CalcSize(new GUIContent(node.Value)).x));
                GUILayout.EndHorizontal();
                node = node.Next;
            }
            if (nodeup != null)
            {
                var nodep = nodeup.Previous;
                if (nodep != null)
                {
                    DistributeFlagOrder.Remove(nodeup);
                    DistributeFlagOrder.AddBefore(nodep, nodeup);
                }
            }
            if (nodedown != null)
            {
                var noden = nodedown.Next;
                if (noden != null)
                {
                    DistributeFlagOrder.Remove(nodedown);
                    DistributeFlagOrder.AddAfter(noden, nodedown);
                }
            }
            if (noderemove != null)
            {
                SelectDistributeFlag(noderemove.Value, false);
            }
            GUILayout.EndScrollView();

            #region copy past
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Copy"))
            {
                System.Text.StringBuilder sbflags = new System.Text.StringBuilder();
                foreach (var oflag in DistributeFlagOrder)
                {
                    sbflags.Append("<");
                    sbflags.Append(oflag);
                }
                GUIUtility.systemCopyBuffer = sbflags.ToString();
            }
            GUILayout.Space(20);
            if (GUILayout.Button("Paste"))
            {
                var pasteConfig = GUIUtility.systemCopyBuffer;
                if (!string.IsNullOrEmpty(pasteConfig))
                {
                    Debug.LogError(pasteConfig);
                    Debug.LogError(OptionConfigIndex);
                    RefreshConfig(pasteConfig, OptionConfigIndex);
                }
            }
            GUILayout.EndHorizontal();
            #endregion copy past

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        public void SelectDistributeFlag(string flag, bool sel)
        {
            var old = false;
            if (DistributeFlags.TryGetValue(flag, out old))
            {
                DistributeFlags[flag] = sel;
                if (old && !sel)
                {
                    DistributeFlagOrder.Remove(flag);
                }
                else if (!old && sel)
                {
                    DistributeFlagOrder.AddLast(flag);
                }
            }
        }

        public void SaveDistributeFlags(int configIndex = 0)
        {
            var old = UnityEngineEx.DistributeFlags.PreRuntimeDFlags;
            bool changed = old.Count != DistributeFlagOrder.Count;
            if (!changed)
            {
                int index = 0;
                foreach (var flag in DistributeFlagOrder)
                {
                    if (old[index++] != flag)
                    {
                        changed = true;
                        break;
                    }
                }
            }
            if (changed)
            {
                AssetDatabase.SaveAssets();
                System.Text.StringBuilder sbflags = new System.Text.StringBuilder();
                foreach (var oflag in DistributeFlagOrder)
                {
                    sbflags.Append("<");
                    sbflags.Append(oflag);
                }
                var path = DistributeEditor.GetDistributeFlagsFilePath();
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                System.IO.File.WriteAllText(path, sbflags.ToString());
                AssetDatabase.ImportAsset(ModEditor.GetAssetNameFromPath(path));

                PlayerPrefs.SetString("DistributeFlags_" + configIndex, sbflags.ToString());
                for (int i = 1; i <= 4; i++)
                {
                    if (!PlayerPrefs.HasKey("DistributeFlags_" + i))
                    {
                        PlayerPrefs.SetString("DistributeFlags_" + i, sbflags.ToString());
                    }
                }

                UnityEngineEx.DistributeFlags.PreRuntimeDFlags = new List<string>(DistributeFlagOrder);
                DistributeEditor.FireOnDistributeFlagsChanged();
            }
            else if (configIndex > 0)
            {
                System.Text.StringBuilder sbflags = new System.Text.StringBuilder();
                foreach (var oflag in DistributeFlagOrder)
                {
                    sbflags.Append("<");
                    sbflags.Append(oflag);
                }
                PlayerPrefs.SetString("DistributeFlags_" + configIndex, sbflags.ToString());
            }
        }

        [SerializeField] protected string[] SerializeDistributeFlags;
        [SerializeField] protected bool[] SerializeDistributeFlagStates;
        [SerializeField] string[] SerializeDistributeFlagOrder;
        public void OnBeforeSerialize()
        {
            SerializeDistributeFlags = DistributeFlags.Keys.ToArray();
            SerializeDistributeFlagStates = DistributeFlags.Values.ToArray();
            SerializeDistributeFlagOrder = DistributeFlagOrder.ToArray();
        }

        public void OnAfterDeserialize()
        {
            DistributeFlags.Clear();
            DistributeFlagOrder.Clear();
            DistributeDescs = null;
            if (SerializeDistributeFlags != null && SerializeDistributeFlagStates != null)
            {
                for (int i = 0; i < SerializeDistributeFlags.Length && i < SerializeDistributeFlagStates.Length; ++i)
                {
                    DistributeFlags[SerializeDistributeFlags[i]] = SerializeDistributeFlagStates[i];
                }
            }
            if (SerializeDistributeFlagOrder != null)
            {
                for (int i = 0; i < SerializeDistributeFlagOrder.Length; ++i)
                {
                    DistributeFlagOrder.AddLast(SerializeDistributeFlagOrder[i]);
                }
            }
        }
    }
}