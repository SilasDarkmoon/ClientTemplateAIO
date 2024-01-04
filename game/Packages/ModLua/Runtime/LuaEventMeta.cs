using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngineEx;

using lua = LuaLib.LuaCoreLib;
using lual = LuaLib.LuaAuxLib;
using luae = LuaLib.LuaLibEx;

namespace LuaLib
{
    // TODO: in method remove_XXX, we need better to Dispose the LuaFunc.
    // TODO: topointer(func) to get the IntPtr of the lua func and use this to AddRef / Release between multiple LuaFuncs
#if UNITY_INCLUDE_TESTS
    #region TESTS
    public static class LuaEventMetaTest
    {
        public static event Action OnCommand = () => { };

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Test/Lua/Test Event", priority = 300020)]
        public static void TestEvent()
        {
            OnCommand();
        }
#endif
    }
    #endregion
#endif
}