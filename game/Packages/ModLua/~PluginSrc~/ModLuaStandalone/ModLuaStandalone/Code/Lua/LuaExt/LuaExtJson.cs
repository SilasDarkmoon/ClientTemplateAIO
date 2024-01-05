using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using LuaLib;

using lua = LuaLib.LuaCoreLib;
using lual = LuaLib.LuaAuxLib;
using luae = LuaLib.LuaLibEx;

namespace LuaLib
{
    public static partial class LuaExLibs
    {
        private static LuaExLibItem _LuaExLib_Json_Instance = new LuaExLibItem(Json2Lua.Init, 100);
    }

    public static class Json2LuaNative
    {
        public static readonly Action<IntPtr> InitFunc;

#if UNITY_EDITOR || UNITY_STANDALONE || !UNITY_ENGINE && !UNITY_5_3_OR_NEWER
#if UNITY_EDITOR_OSX
        public const string LIB_PATH = "LuaJsonNative";
#else
#if DLLIMPORT_NAME_FULL
        public const string LIB_PATH = "libLuaJsonNative.so";
#else
        public const string LIB_PATH = "LuaJsonNative";
#endif
#endif
        static Json2LuaNative()
        {
            InitFunc = null;
#if !UNITY_ENGINE && !UNITY_5_3_OR_NEWER
            UnityEngineEx.PluginManager.LoadLib(LIB_PATH);
#endif
            try
            {
                if (IsReady())
                {
                    InitFunc = InitLuaJsonPlugin;
                }
                else
                {
                    UnityEngineEx.PlatDependant.LogError("Can not initialize Json2LuaNative");
                }
            }
            catch (Exception e)
            {
                UnityEngineEx.PlatDependant.LogError(e);
            }
        }

        [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void InitLuaJsonPlugin(IntPtr l);

        [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool IsReady();
#elif UNITY_IPHONE
        public const string LIB_PATH = "__Internal";

        static Json2LuaNative()
        {
            InitFunc = null;
            try
            {
                InitFunc = InitLuaJsonPlugin;
            }
            catch { }
        }

        [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void InitLuaJsonPlugin(IntPtr l);
#elif UNITY_ANDROID
        public const string LIB_PATH = "LuaJsonNative";

        static Json2LuaNative()
        {
            InitFunc = null;
            try
            {
                if (IsReady())
                {
                    InitFunc = InitLuaJsonPlugin;
                }
            }
            catch { }
        }

        [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void InitLuaJsonPlugin(IntPtr l);

        [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool IsReady();
#endif
    }

    public static class Json2Lua
    {
        internal static readonly lua.CFunction funcEncode = new lua.CFunction(Encode);
        internal static readonly lua.CFunction funcDecode = new lua.CFunction(Decode);

        public static void Init(IntPtr L)
        {
            if (Json2LuaNative.InitFunc != null)
            {
                Json2LuaNative.InitFunc(L);
            }
            else
            {
                L.newtable();
                L.pushvalue(-1);
                L.SetGlobal("json");
                L.PushString("encode");
                L.pushcfunction(funcEncode);
                L.settable(-3);
                L.PushString("decode");
                L.pushcfunction(funcDecode);
                L.settable(-3);
                L.pop(1);
            }
        }

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int Encode(IntPtr L)
        {
            JSONObject jobj = Encode(L, 1);
            L.pushstring(jobj.ToString());

            return 1;
        }

        public static JSONObject Encode(IntPtr L, int index)
        {
            JSONObject jobj;
            int typecode = L.type(index);
            switch (typecode)
            {
                case lua.LUA_TBOOLEAN:
                    {
                        jobj = new JSONObject(L.toboolean(index));
                    }
                    break;
                case lua.LUA_TNIL:
                case lua.LUA_TNONE:
                    {
                        jobj = new JSONObject(JSONObject.nullJO);
                    }
                    break;
                case lua.LUA_TNUMBER:
                    {
                        jobj = new JSONObject(L.tonumber(index));
                    }
                    break;
                case lua.LUA_TSTRING:
                    {
                        jobj = new JSONObject(JSONObject.Type.STRING);
                        jobj.str = L.tostring(index);
                    }
                    break;
                case lua.LUA_TTABLE:
                    {
                        var len = LuaArrayLength(L, index);
                        if (len > 0)
                        {
                            var array = new JSONObject[len];
                            for (int i = 0; i < len; ++i)
                            {
                                L.pushnumber(i + 1);
                                L.gettable(index);

                                JSONObject childjo = Encode(L, L.gettop());
                                L.pop(1);
                                array[i] = childjo;
                            }
                            jobj = new JSONObject(array);
                        }
                        else
                        {
                            var dict = new System.Collections.Generic.Dictionary<string, JSONObject>();
                            L.pushnil();
                            while (L.next(index))
                            {
                                L.pushvalue(-2);
                                string key = L.tostring(-1);
                                L.pop(1);
                                if (key != null)
                                {
                                    JSONObject childjo = Encode(L, L.gettop());
                                    dict[key] = childjo;
                                }
                                L.pop(1);
                            }
                            jobj = new JSONObject(dict);
                        }
                    }
                    break;
                default:
                    {
                        jobj = new JSONObject();
                    }
                    break;
            }

            return jobj;
        }

        public static int LuaArrayLength(IntPtr L, int index)
        {
            int max = 0;
            int items = 0;
            L.pushnil();
            while (L.next(index))
            {
                if (L.type(-2) == lua.LUA_TNUMBER)
                {
                    double key = L.tonumber(-2);
                    if (Math.Floor(key) == key && key >= 1)
                    {
                        if (key > max)
                        {
                            max = (int)key;
                        }
                        ++items;
                        L.pop(1);
                        continue;
                    }

                }
                L.pop(2);
                return -1;
            }

            if (max == L.getn(index))
            {
                return max;
            }
            else
            {
                return -1;
            }
        }

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int Decode(IntPtr L)
        {
            JSONObject jobj = new JSONObject(L.tostring(-1));

            Decode(L, jobj);

            return 1;
        }

        public static void Decode(IntPtr L, JSONObject jobj)
        {
            if (jobj.type == JSONObject.Type.BOOL)
            {
                L.pushboolean(jobj.b);
            }
            else if (jobj.type == JSONObject.Type.NULL)
            {
                L.pushnil();
            }
            else if (jobj.type == JSONObject.Type.NUMBER)
            {
                L.pushnumber(jobj.n);
            }
            else if (jobj.type == JSONObject.Type.STRING)
            {
                L.pushstring(jobj.str);
            }
            else if (jobj.type == JSONObject.Type.ARRAY)
            {
                L.newtable();
                for (int i = 0; i < jobj.list.Count; ++i)
                {
                    L.pushnumber(i + 1);
                    Decode(L, jobj.list[i]);
                    L.settable(-3);
                }
            }
            else if (jobj.type == JSONObject.Type.OBJECT)
            {
                L.newtable();
                for (int i = 0; i < jobj.list.Count; ++i)
                {
                    L.pushstring(jobj.keys[i]);
                    Decode(L, jobj.list[i]);
                    L.settable(-3);
                }
            }
            else
            {
                L.pushnil();
            }
        }
    }
}