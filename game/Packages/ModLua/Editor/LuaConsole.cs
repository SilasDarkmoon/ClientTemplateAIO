using System;
using UnityEngine;
using UnityEditor;
using UnityEngineEx;
using LuaLib;

using lua = LuaLib.LuaCoreLib;
using lual = LuaLib.LuaAuxLib;
using luae = LuaLib.LuaLibEx;
using static LuaLib.LuaPack;

namespace UnityEditorEx
{
    public class LuaConsole : EditorWindow
    {
        [MenuItem("Lua/Lua Console", priority = 300030)]
        static void Init()
        {
            GetWindow(typeof(LuaConsole)).titleContent = new GUIContent("Lua Console");
        }

        public string Command;
        public string Result;
        //public Vector2 CommandScroll;
        public Vector2 ResultScroll;

        void OnGUI()
        {
            //CommandScroll = EditorGUILayout.BeginScrollView(CommandScroll, GUILayout.MaxHeight(500));
            Command = EditorGUILayout.TextArea(Command, GUILayout.MaxHeight(500));
            //EditorGUILayout.EndScrollView();
            if (GUILayout.Button("Run!"))
            {
                var l = GlobalLua.L.L;
                using (var lr = l.CreateStackRecover())
                {
                    l.DoString(Command);
                    var ntop = l.gettop();
                    if (ntop <= lr.Top)
                    {
                        Result = "<No Result>";
                    }
                    else
                    {
                        var cnt = ntop - lr.Top;
                        System.Text.StringBuilder sb = new System.Text.StringBuilder();
                        l.pushcfunction(LuaHub.LuaFuncOnError);
                        for (int i = 0; i < cnt; ++i)
                        {
                            if (i > 0)
                            {
                                sb.AppendLine(",");
                            }
                            var index = lr.Top + i + 1;
                            l.GetHierarchicalRaw(lua.LUA_GLOBALSINDEX, "table.concat");
                            l.GetGlobal("vardump");
                            l.pushvalue(index);
                            if (cnt == 1)
                            {
                                l.PushString("result");
                            }
                            else
                            {
                                l.pushnumber(i + 1);
                            }
                            
                            l.pcall(2, 1, ntop + 1);
                            l.PushString(Environment.NewLine);
                            l.pcall(2, 1, ntop + 1);
                            string tabstr;
                            l.GetLua(-1, out tabstr);
                            sb.Append(tabstr);
                            l.pop(1);
                        }
                        Result = sb.ToString();
                    }
                }
            }
            ResultScroll = EditorGUILayout.BeginScrollView(ResultScroll);
            EditorGUILayout.TextArea(Result);
            EditorGUILayout.EndScrollView();
        }
    }

#if UNITY_INCLUDE_TESTS
    #region TESTS
    public static class LuaEditorTestCommands
    {
#if UNITY_EDITOR
        [UnityEditor.MenuItem("Test/Lua/Test Lua", priority = 300010)]
        public static void TestLua()
        {
            UnityEditor.AssetDatabase.OpenAsset(UnityEditor.AssetDatabase.LoadMainAssetAtPath(ResManager.__ASSET__), ResManager.__LINE__);

            var l = GlobalLua.L.L;
            using (var lr = l.CreateStackRecover())
            {
                l.pushnumber(250);
                string str;
                l.CallGlobal(out str, "dump", LuaPack.Pack(l.OnStack(-1)));
                PlatDependant.LogError(str);
            }
        }
#endif
    }
    #endregion
#endif
}