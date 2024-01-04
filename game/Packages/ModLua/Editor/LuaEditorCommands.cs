using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Collections.Generic;
using UnityEngineEx;
using LuaLib;

using lua = LuaLib.LuaCoreLib;
using lual = LuaLib.LuaAuxLib;
using luae = LuaLib.LuaLibEx;
using static LuaLib.LuaPack;

namespace UnityEditorEx
{
    public static class LuaEditorCommands
    {
        [MenuItem("GameObject/Push to Lua", priority = 12)]
        public static void PushSelectedToLua()
        {
            var go = Selection.activeGameObject;
            if (go != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(go)))
            {
                GlobalLua.L.L.SetGlobal("___EDITOR_TEMP_PUSHED", go);
                Debug.Log("Pushed to Lua. Use ___EDITOR_TEMP_PUSHED to access this GameObject.");
            }
            else
            {
                Debug.Log("Nothing is pushed. Check your selection.");
            }
        }

        [MenuItem("CONTEXT/Component/Push to Lua", priority = 10001)]
        public static void PushSelectedCompToLua(MenuCommand command)
        {
            var comp = command.context as Component;
            if (comp != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(comp)))
            {
                GlobalLua.L.L.SetGlobal("___EDITOR_TEMP_PUSHED", comp);
                Debug.Log("Pushed to Lua. Use ___EDITOR_TEMP_PUSHED to access this Component.");
            }
            else
            {
                Debug.Log("Nothing is pushed. Check your selection.");
            }
        }

        [MenuItem("Assets/Push to Lua", priority = 2029)]
        public static void PushSelectedAssetToLua()
        {
            var selection = Selection.activeObject;
            if (selection != null && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(selection)))
            {
                GlobalLua.L.L.SetGlobal("___EDITOR_TEMP_PUSHED", selection);
                Debug.Log("Pushed to Lua. Use ___EDITOR_TEMP_PUSHED to access this Asset.");
            }
            else
            {
                Debug.Log("Nothing is pushed. Check your selection.");
            }
        }
    }
}