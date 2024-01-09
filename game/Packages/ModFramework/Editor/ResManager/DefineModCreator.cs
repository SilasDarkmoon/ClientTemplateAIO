using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngineEx;

namespace UnityEditorEx
{
    public class DefineModCreator : EditorWindow
    {
        [MenuItem("Mods/Make Mod for Precompiler Define Symbol", priority = 200010)]
        static void Init()
        {
            var win = GetWindow<DefineModCreator>();
            win.titleContent = new GUIContent("Type Symbol");
        }

        protected string _Symbol = "DEBUG_";
        protected string _Desc = "";
        protected bool _ExportToLua = false;
        void OnGUI()
        {
            GUILayout.BeginVertical();
            GUILayout.Label("Which precompiler symbol would you like to define?");
            _Symbol = GUILayout.TextField(_Symbol);
            GUILayout.Label("You can write some description below:");
            _Desc = GUILayout.TextField(_Desc);
            _ExportToLua = EditorGUILayout.ToggleLeft("Export to Lua", _ExportToLua);
            if (GUILayout.Button("OK"))
            {
                if (string.IsNullOrEmpty(_Symbol))
                {
                    EditorUtility.DisplayDialog("Error", "Empty Symbol!", "OK");
                }
                else if (_Symbol.EndsWith("_"))
                {
                    EditorUtility.DisplayDialog("Error", "Symbol should not end with _", "OK");
                }
                else
                {
                    if (System.IO.Directory.Exists("Assets/Mods/" + _Symbol))
                    {
                        EditorUtility.DisplayDialog("Warning", "It seems that the mod has been already created.", "OK");
                    }
                    else
                    {
                        var descdir = "Assets/Mods/" + _Symbol + "/Resources";
                        System.IO.Directory.CreateDirectory(descdir);
                        AssetDatabase.ImportAsset(descdir);
                        var desc = ScriptableObject.CreateInstance<ModDesc>();
                        desc.Mod = _Symbol;
                        AssetDatabase.CreateAsset(desc, "Assets/Mods/" + _Symbol + "/Resources/resdesc.asset");
                        var sympath = "Assets/Mods/" + _Symbol + "/Link/mcs.rsp";
                        using (var sw = PlatDependant.OpenWriteText(sympath))
                        {
                            sw.Write("-define:");
                            sw.WriteLine(_Symbol);
                        }
                        if (_ExportToLua)
                        {
                            var luapath = "Assets/Mods/" + _Symbol + "/ModSpt/init.lua";
                            using (var sw = PlatDependant.OpenWriteText(luapath))
                            {
                                sw.Write(_Symbol);
                                sw.Write(" = true");
                            }
                        }
                        var descpath = "Assets/Mods/" + _Symbol + "/.desc.txt";
                        using (var sw = PlatDependant.OpenWriteText(descpath))
                        {
                            sw.WriteLine("{");
                            sw.WriteLine("    \"Title\" : \"C# define" + (string.IsNullOrEmpty(_Desc) ? "" : ", " + _Desc) + "\",");
                            sw.WriteLine("    \"Order\" : 1000000,");
                            sw.WriteLine("    \"Color\" : \"black\"");
                            sw.WriteLine("}");
                        }

                        ModEditor.CheckModsVisibility();
                        DistributeSelectWindow.Init();
                    }
                    Close();
                }
            }
            GUILayout.EndVertical();
        }
    }
}
