using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngineEx;

using Object = UnityEngine.Object;

namespace UnityEditorEx
{
    [InitializeOnLoad]
    public static class FontCharInfoAutoCollect
    {
        static FontCharInfoAutoCollect()
        {
#if AUTO_COLLECT_PH_FONT_CHAR_INFO
            EditorApplication.playModeStateChanged += reason =>
            {
                if (reason == PlayModeStateChange.ExitingPlayMode)
                {
                    CollectFontCharInfoForPlaceHolderFonts();
                }
            };
#endif
        }

        public static void CollectFontCharInfoForPlaceHolderFonts()
        {
            var fontinfo = PHFontEditor.GetFontReplacementPaths();
            foreach (var kvp in fontinfo)
            {
                CollectFontCharInfo(kvp.Key, kvp.Value);
            }
        }

        public static void CollectFontCharInfo(string phfont, string rfont)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(phfont);
            if (font)
            {
                var cinfo = font.characterInfo;
                if (cinfo != null && cinfo.Length > 0)
                {
                    var filename = System.IO.Path.GetFileNameWithoutExtension(phfont);
                    string type, mod, dist;
                    ResManager.GetAssetNormPath(rfont, out type, out mod, out dist);
                    string cinfopath = "/Common/Fonts/" + filename + ".json";
                    if (!string.IsNullOrEmpty(dist))
                    {
                        cinfopath = "/dist/" + dist + cinfopath;
                    }
                    cinfopath = "/ModRes" + cinfopath;
                    if (!string.IsNullOrEmpty(mod))
                    {
                        cinfopath = "/Mods/" + mod + cinfopath;
                    }
                    cinfopath = "Assets" + cinfopath;

                    Dictionary<Pack<int, FontStyle>, Pack<HashSet<int>, string>> cinfosave = new Dictionary<Pack<int, FontStyle>, Pack<HashSet<int>, string>>();
                    if (System.IO.File.Exists(cinfopath))
                    {
                        string json = "";
                        using (var sr = PlatDependant.OpenReadText(cinfopath))
                        {
                            json = sr.ReadToEnd();
                        }
                        try
                        {
                            var jo = new JSONObject(json);
                            try
                            {
                                var root = jo["cinfo"] as JSONObject;
                                if (root != null && root.type == JSONObject.Type.ARRAY)
                                {
                                    for (int i = 0; i < root.list.Count; ++i)
                                    {
                                        var oldcinfoitem = root.list[i];
                                        if (oldcinfoitem != null && oldcinfoitem.type == JSONObject.Type.OBJECT)
                                        {
                                            int size;
                                            FontStyle style;
                                            string combined;
                                            HashSet<int> codes = new HashSet<int>();

                                            size = (int)(oldcinfoitem["size"]?.n ?? 0);
                                            style = (FontStyle)(int)(oldcinfoitem["style"]?.n ?? 0);
                                            combined = oldcinfoitem["combined"]?.str ?? "";
                                            var codesnode = oldcinfoitem["codes"];
                                            if (codesnode != null && codesnode.type == JSONObject.Type.ARRAY)
                                            {
                                                for (int j = 0; j < codesnode.list.Count; ++j)
                                                {
                                                    var codeitem = codesnode.list[j];
                                                    var code = codeitem?.n;
                                                    if (code != null)
                                                    {
                                                        codes.Add((int)code.GetValueOrDefault());
                                                    }
                                                }
                                            }

                                            cinfosave[new Pack<int, FontStyle>(size, style)] = new Pack<HashSet<int>, string>(codes, combined);
                                        }
                                    }
                                }
                            }
                            catch { }
                        }
                        catch { }
                    }

                    bool cinfosavechanged = false;
                    for (int i = 0; i < cinfo.Length; ++i)
                    {
                        var cinfoitem = cinfo[i];
                        int size = cinfoitem.size;
                        FontStyle style = cinfoitem.style;
                        int code = cinfoitem.index;

                        var cinfokey = new Pack<int, FontStyle>(size, style);
                        Pack<HashSet<int>, string> cinfovalue;
                        if (!cinfosave.TryGetValue(cinfokey, out cinfovalue))
                        {
                            cinfovalue = new Pack<HashSet<int>, string>(new HashSet<int>(), "");
                        }
                        if (cinfovalue.t1.Add(code))
                        {
                            cinfosavechanged = true;
                            string ch = "";
                            Span<byte> bytes = stackalloc byte[4];
                            if (BitConverter.TryWriteBytes(bytes, code))
                            {
                                try
                                {
                                    ch = System.Text.Encoding.UTF32.GetString(bytes);
                                }
                                catch { }
                            }
                            cinfovalue.t2 += ch;
                            cinfosave[cinfokey] = cinfovalue;
                        }
                    }

                    if (cinfosavechanged)
                    {
                        var jsonsave = new JSONObject(JSONObject.Type.OBJECT);
                        var jsonroot = new JSONObject(JSONObject.Type.ARRAY);
                        jsonsave.AddField("cinfo", jsonroot);
                        foreach (var kvp in cinfosave)
                        {
                            var size = kvp.Key.t1;
                            var style = kvp.Key.t2;
                            var combined = kvp.Value.t2;
                            var codes = kvp.Value.t1;

                            var jsonitem = new JSONObject(JSONObject.Type.OBJECT);
                            jsonitem.AddField("size", size);
                            jsonitem.AddField("style", (int)style);
                            jsonitem.AddField("combined", combined);
                            var jsoncodes = new JSONObject(JSONObject.Type.ARRAY);
                            foreach (var code in codes)
                            {
                                jsoncodes.Add(code);
                            }
                            jsonitem.AddField("codes", jsoncodes);

                            jsonroot.Add(jsonitem);
                        }

                        using (var sw = PlatDependant.OpenWriteText(cinfopath))
                        {
                            sw.Write(jsonsave.ToString(false));
                        }
                    }
                }
            }
        }
    }
}