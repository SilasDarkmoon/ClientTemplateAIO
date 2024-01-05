//#define LUA_USE_SYSTEM_ENCODING_ON_EDITOR_WIN

using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace LuaLib
{
    public static class LuaCoreLib
    {
        #region LuaTypes
        public const int LUA_TNONE = -1;
        public const int LUA_TNIL = 0;
        public const int LUA_TBOOLEAN = 1;
        public const int LUA_TLIGHTUSERDATA = 2;
        public const int LUA_TNUMBER = 3;
        public const int LUA_TSTRING = 4;
        public const int LUA_TTABLE = 5;
        public const int LUA_TFUNCTION = 6;
        public const int LUA_TUSERDATA = 7;
        public const int LUA_TTHREAD = 8;
        #endregion

        private static readonly string[] _TypeNames = new[]
        {
            "nil", "boolean", "userdata", "number", "string", "table", "function", "userdata", "thread",
        };
        private static readonly string _TypeName_None = "no value";
        private static readonly string _TypeName_Undefined = null;
        public static string typename(int typecode)
        {
            if (typecode == -1)
            {
                return _TypeName_None;
            }
            else if (typecode >= 0 && typecode < _TypeNames.Length)
            {
                return _TypeNames[typecode];
            }
            return _TypeName_Undefined;
        }

        #region LuaGCOptions
        public const int LUA_GCSTOP = 0;
        public const int LUA_GCRESTART = 1;
        public const int LUA_GCCOLLEC = 2;
        public const int LUA_GCCOUNT = 3;
        public const int LUA_GCCOUNTB = 4;
        public const int LUA_GCSTEP = 5;
        public const int LUA_GCSETPAUSE = 6;
        public const int LUA_GCSETSTEPMUL = 7;
        #endregion

        #region LuaThreadStatus
        public const int LUA_YIELD = 1;
        public const int LUA_ERRRUN = 2;
        public const int LUA_ERRSYNTAX = 3;
        public const int LUA_ERRMEM = 4;
        public const int LUA_ERRERR = 5;
        #endregion

        #region LuaIndexes
        public const int LUA_REGISTRYINDEX = -10000;
        public const int LUA_ENVIRONINDEX = -10001;
        public const int LUA_GLOBALSINDEX = -10002;
        #endregion

        #region Other Lua Macros
        public const int LUA_MULTRET = -1;
        public const int LUA_NOREF = -2;
        public const int LUA_REFNIL = -1;
        #endregion

        #region Lua Core Func
#if UNITY_EDITOR || !UNITY_ENGINE && !UNITY_5_3_OR_NEWER
#if UNITY_EDITOR_OSX
        public const string LUADLL = "lua";
#else
#if LUADLL_NAME_LUA51
#if DLLIMPORT_NAME_FULL
        public const string LUADLL = "liblua51.so";
#else
        public const string LUADLL = "lua51";
#endif
#elif LUADLL_NAME_LUAJIT
#if DLLIMPORT_NAME_FULL
        public const string LUADLL = "libluajit.so";
#else
        public const string LUADLL = "luajit";
#endif
#else
#if DLLIMPORT_NAME_FULL
        public const string LUADLL = "liblua.so";
#else
        public const string LUADLL = "lua";
#endif
#endif
#endif
#elif UNITY_IPHONE
        public const string LUADLL = "__Internal";
#else
        public const string LUADLL = "lua";
#endif
#if UNITY_EDITOR || !UNITY_ENGINE && !UNITY_5_3_OR_NEWER
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_atpanic")]
        public static extern CFunction atpanic(this IntPtr luaState, CFunction panicf);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_call")]
        public static extern void call(this IntPtr luaState, int nArgs, int nResults);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_checkstack")]
        public static extern bool checkstack(this IntPtr luaState, int extra);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_close")]
        public static extern void close(this IntPtr luaState);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_concat")]
        public static extern void concat(this IntPtr luaState, int n);
        // lua_cpcall
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_createtable")]
        public static extern void createtable(this IntPtr luaState, int narr, int nrec);
        // lua_dump
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_equal")]
        public static extern bool equal(this IntPtr luaState, int index1, int index2);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_error")]
        public static extern int error(this IntPtr luaState);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_gc")]
        public static extern int gc(this IntPtr luaState, int what, int data);
        // lua_getallocf
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_getfenv")]
        public static extern void getfenv(this IntPtr luaState, int stackPos);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_getfield")]
        public static extern void getfield(this IntPtr luaState, int stackPos, byte[] meta);
        public static void getfield(this IntPtr luaState, int stackPos, string meta)
        {
            var bytes = meta.DefaultEncode();
            getfield(luaState, stackPos, bytes);
        }
        // lua_getglobal -> below
        // lua_gethook
        // lua_gethookcount
        // lua_gethookmask
        // lua_getinfo
        // lua_getlocal
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_getmetatable")]
        public static extern bool getmetatable(this IntPtr luaState, int objIndex);
        // lua_getstack
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_gettable")]
        public static extern void gettable(this IntPtr luaState, int index);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_gettop")]
        public static extern int gettop(this IntPtr luaState);
        // lua_getupvalue
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_insert")]
        public static extern void insert(this IntPtr luaState, int newTop);
        // lua_isboolean -> below
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_iscfunction")]
        public static extern bool iscfunction(this IntPtr luaState, int index);
        // lua_isfunction -> below
        // lua_islightuserdata -> below
        // lua_isnil -> below
        // lua_isnone -> below
        // lua_isnoneornil -> below
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_isnumber")]
        public static extern bool isnumber(this IntPtr luaState, int index);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_isstring")]
        public static extern bool isstring(this IntPtr luaState, int index);
        // lua_istable -> below
        // lua_isthread -> below
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_isuserdata")]
        public static extern bool isuserdata(this IntPtr luaState, int stackPos);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_lessthan")]
        public static extern bool lessthan(this IntPtr luaState, int stackPos1, int stackPos2);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_load")]
        public static extern int load(this IntPtr luaState, Reader reader, IntPtr ud, byte[] chunkname);
        public static int load(this IntPtr luaState, Reader reader, IntPtr ud, string chunkname)
        {
            var bytes = chunkname.DefaultEncode();
            return load(luaState, reader, ud, bytes);
        }
        // lua_newstate
        // lua_newtable -> below
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_newthread")]
        public static extern IntPtr newthread(this IntPtr L);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_newuserdata")]
        public static extern IntPtr newuserdata(this IntPtr luaState, IntPtr size);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_next")]
        public static extern bool next(this IntPtr luaState, int index);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_objlen")]
        public static extern IntPtr objlen(this IntPtr luaState, int stackPos);
#if ENABLE_PROFILER && ENABLE_PROFILER_LUA && ENABLE_PROFILER_LUA_DEEP && !DISABLE_PROFILER_LUA_PCALL
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_pcall")]
        public static extern int lua_pcall(IntPtr luaState, int nArgs, int nResults, int errfunc);
        public static int pcall(this IntPtr luaState, int nArgs, int nResults, int errfunc)
        {
            System.Text.StringBuilder sbName = new System.Text.StringBuilder();
            if (luaState.iscfunction(-nArgs - 1))
            {
                sbName.Append(luaState.GetSimpleStackInfo(8));
                sbName.Append(">[C]");
            }
            else
            {
                string funcName, fileName;
                int lineStart, lineCur;
                luaState.GetFuncInfo(-nArgs - 1, out funcName, out fileName, out lineStart, out lineCur);
                sbName.Append(fileName);
                sbName.Append(":");
                sbName.Append(lineStart);
                //sbName.Append(" ");
                //sbName.Append(funcName); // NOTICE: we cannot get the name directly from the func.
            }
            using (var pcon = UnityEngineEx.ProfilerContext.Create("LuaFunc pcall"))
            using (var pconi = UnityEngineEx.ProfilerContext.Create(sbName))
                return lua_pcall(luaState, nArgs, nResults, errfunc);
        }
#else
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_pcall")]
        public static extern int pcall(this IntPtr luaState, int nArgs, int nResults, int errfunc);
#endif
        // lua_pop -> below
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_pushboolean")]
        public static extern void pushboolean(this IntPtr luaState, bool value);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_pushcclosure")]
        public static extern void pushcclosure(this IntPtr luaState, CFunction fn, int n);
        // lua_pushcfunction -> below
        // lua_pushfstring
        // lua_pushinteger
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_pushlightuserdata")]
        public static extern void pushlightuserdata(this IntPtr luaState, IntPtr udata);
        // lua_pushliteral
        public static void pushlstring(this IntPtr luaState, string str, IntPtr size)
        {
            var bytes = str.DefaultEncode();
            pushlstring(luaState, bytes, size);
        }
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_pushlstring")]
        public static extern void pushlstring(this IntPtr luaState, byte[] str, IntPtr size); // lua_pushlstring+
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_pushnil")]
        public static extern void pushnil(this IntPtr luaState);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_pushnumber")]
        public static extern void pushnumber(this IntPtr luaState, double number);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_pushstring")]
        public static extern void pushstring(this IntPtr luaState, byte[] str);
        public static void pushstring(this IntPtr luaState, string str)
        {
            var bytes = str.DefaultEncode();
            pushstring(luaState, bytes);
        }
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_pushthread")]
        public static extern bool pushthread(this IntPtr L);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_pushvalue")]
        public static extern void pushvalue(this IntPtr luaState, int index);
        // lua_pushvfstring
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_rawequal")]
        public static extern bool rawequal(this IntPtr luaState, int stackPos1, int stackPos2);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_rawget")]
        public static extern void rawget(this IntPtr luaState, int index);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_rawgeti")]
        public static extern void rawget(this IntPtr luaState, int tableIndex, int index);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_rawset")]
        public static extern void rawset(this IntPtr luaState, int index);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_rawseti")]
        public static extern void rawset(this IntPtr luaState, int tableIndex, int index);
        // lua_register
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_remove")]
        public static extern void remove(this IntPtr luaState, int index);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_replace")]
        public static extern void replace(this IntPtr luaState, int index);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_resume")]
        public static extern int resume(this IntPtr L, int narg);
        // lua_setallocf
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_setfenv")]
        public static extern bool setfenv(this IntPtr luaState, int stackPos);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_setfield")]
        public static extern void setfield(this IntPtr luaState, int stackPos, byte[] name);
        public static void setfield(this IntPtr luaState, int stackPos, string name)
        {
            var bytes = name.DefaultEncode();
            setfield(luaState, stackPos, bytes);
        }
        // lua_setglobal -> below
        // lua_sethook
        // lua_setlocal
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_setmetatable")]
        public static extern bool setmetatable(this IntPtr luaState, int objIndex);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_settable")]
        public static extern void settable(this IntPtr luaState, int index);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_settop")]
        public static extern void settop(this IntPtr luaState, int newTop);
        // lua_setupvalue
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_status")]
        public static extern int status(this IntPtr L);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_toboolean")]
        public static extern bool toboolean(this IntPtr luaState, int index);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_tocfunction")]
        public static extern CFunction tocfunction(this IntPtr luaState, int index);
        // lua_tointeger
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_tolstring")]
        public static extern IntPtr tolstring(this IntPtr luaState, int index, out IntPtr strLen);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_tonumber")]
        public static extern double tonumber(this IntPtr luaState, int index);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_topointer")]
        public static extern IntPtr topointer(this IntPtr luaState, int index);
        // lua_tostring -> below
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_tothread")]
        public static extern IntPtr tothread(this IntPtr L, int index);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_touserdata")]
        public static extern IntPtr touserdata(this IntPtr luaState, int index);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_type")]
        public static extern int type(this IntPtr luaState, int index);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_typename")]
        public static extern IntPtr typename(IntPtr luaState, int type);
        // lua_upvalueindex -> below
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_xmove")]
        public static extern void xmove(this IntPtr from, IntPtr to, int n);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_yield")]
        public static extern int yield(this IntPtr L, int nresults);
#elif UNITY_WP8 || UNITY_METRO
        public delegate CFunction del_lua_atpanic(IntPtr luaState, CFunction panicf);
        public static del_lua_atpanic lua_atpanic;
        public static CFunction atpanic(this IntPtr luaState, CFunction panicf) { return lua_atpanic(luaState, panicf); }

        public delegate void del_lua_call(IntPtr luaState, int nArgs, int nResults);
        public static del_lua_call lua_call;
        public static void call(this IntPtr luaState, int nArgs, int nResults) { lua_call(luaState, nArgs, nResults); }

        public delegate bool del_lua_checkstack(IntPtr luaState, int extra);
        public static del_lua_checkstack lua_checkstack;
        public static bool checkstack(this IntPtr luaState, int extra) { return lua_checkstack(luaState, extra); }

        public delegate void del_lua_close(IntPtr luaState);
        public static del_lua_close lua_close;
        public static void close(this IntPtr luaState) { lua_close(luaState); }

        public delegate void del_lua_concat(IntPtr luaState, int n);
        public static del_lua_concat lua_concat;
        public static void concat(this IntPtr luaState, int n) { lua_concat(luaState, n); }

        // lua_cpcall

        public delegate void del_lua_createtable(IntPtr luaState, int narr, int nrec);
        public static del_lua_createtable lua_createtable;
        public static void createtable(this IntPtr luaState, int narr, int nrec) { lua_createtable(luaState, narr, nrec); }

        // lua_dump

        public delegate bool del_lua_equal(IntPtr luaState, int index1, int index2);
        public static del_lua_equal lua_equal;
        public static bool equal(this IntPtr luaState, int index1, int index2) { return lua_equal(luaState, index1, index2); }

        public delegate int del_lua_error(IntPtr luaState);
        public static del_lua_error lua_error;
        public static int error(this IntPtr luaState) { return lua_error(luaState); }

        public delegate int del_lua_gc(IntPtr luaState, int what, int data);
        public static del_lua_gc lua_gc;
        public static int gc(this IntPtr luaState, int what, int data) { return lua_gc(luaState, what, data); }

        // lua_getallocf

        public delegate void del_lua_getfenv(IntPtr luaState, int stackPos);
        public static del_lua_getfenv lua_getfenv;
        public static void getfenv(this IntPtr luaState, int stackPos) { lua_getfenv(luaState, stackPos); }

        public delegate void del_lua_getfield(IntPtr luaState, int stackPos, string meta);
        public static del_lua_getfield lua_getfield;
        public static void getfield(this IntPtr luaState, int stackPos, string meta) { lua_getfield(luaState, stackPos, meta); }

        // lua_getglobal -> below
        // lua_gethook
        // lua_gethookcount
        // lua_gethookmask
        // lua_getinfo
        // lua_getlocal

        public delegate bool del_lua_getmetatable(IntPtr luaState, int objIndex);
        public static del_lua_getmetatable lua_getmetatable;
        public static bool getmetatable(this IntPtr luaState, int objIndex) { return lua_getmetatable(luaState, objIndex); }

        // lua_getstack

        public delegate void del_lua_gettable(IntPtr luaState, int index);
        public static del_lua_gettable lua_gettable;
        public static void gettable(this IntPtr luaState, int index) { lua_gettable(luaState, index); }

        public delegate int del_lua_gettop(IntPtr luaState);
        public static del_lua_gettop lua_gettop;
        public static int gettop(this IntPtr luaState) { return lua_gettop(luaState); }

        // lua_getupvalue

        public delegate void del_lua_insert(IntPtr luaState, int newTop);
        public static del_lua_insert lua_insert;
        public static void insert(this IntPtr luaState, int newTop) { lua_insert(luaState, newTop); }

        // lua_isboolean -> below

        public delegate bool del_lua_iscfunction(IntPtr luaState, int index);
        public static del_lua_iscfunction lua_iscfunction;
        public static bool iscfunction(this IntPtr luaState, int index) { return lua_iscfunction(luaState, index); }

        // lua_isfunction -> below
        // lua_islightuserdata -> below
        // lua_isnil -> below
        // lua_isnone -> below
        // lua_isnoneornil -> below

        public delegate bool del_lua_isnumber(IntPtr luaState, int index);
        public static del_lua_isnumber lua_isnumber;
        public static bool isnumber(this IntPtr luaState, int index) { return lua_isnumber(luaState, index); }

        public delegate bool del_lua_isstring(IntPtr luaState, int index);
        public static del_lua_isstring lua_isstring;
        public static bool isstring(this IntPtr luaState, int index) { return lua_isstring(luaState, index); }

        // lua_istable -> below
        // lua_isthread -> below

        public delegate bool del_lua_isuserdata(IntPtr luaState, int stackPos);
        public static del_lua_isuserdata lua_isuserdata;
        public static bool isuserdata(this IntPtr luaState, int stackPos) { return lua_isuserdata(luaState, stackPos); }

        public delegate bool del_lua_lessthan(IntPtr luaState, int stackPos1, int stackPos2);
        public static del_lua_lessthan lua_lessthan;
        public static bool lessthan(this IntPtr luaState, int stackPos1, int stackPos2) { return lua_lessthan(luaState, stackPos1, stackPos2); }

        public delegate int del_lua_load(IntPtr luaState, Reader reader, IntPtr ud, string chunkname);
        public static del_lua_load lua_load;
        public static int load(this IntPtr luaState, Reader reader, IntPtr ud, string chunkname) { return lua_load(luaState, reader, ud, chunkname); }

        // lua_newstate
        // lua_newtable -> below

        public delegate IntPtr del_lua_newthread(IntPtr L);
        public static del_lua_newthread lua_newthread;
        public static IntPtr newthread(this IntPtr L) { return lua_newthread(L); }

        public delegate IntPtr del_lua_newuserdata(IntPtr luaState, IntPtr size);
        public static del_lua_newuserdata lua_newuserdata;
        public static IntPtr newuserdata(this IntPtr luaState, IntPtr size) { return lua_newuserdata(luaState, size); }

        public delegate bool del_lua_next(IntPtr luaState, int index);
        public static del_lua_next lua_next;
        public static bool next(this IntPtr luaState, int index) { return lua_next(luaState, index); }

        public delegate IntPtr del_lua_objlen(IntPtr luaState, int stackPos);
        public static del_lua_objlen lua_objlen;
        public static IntPtr objlen(this IntPtr luaState, int stackPos) { return lua_objlen(luaState, stackPos); }

        public delegate int del_lua_pcall(IntPtr luaState, int nArgs, int nResults, int errfunc);
        public static del_lua_pcall lua_pcall;
#if ENABLE_PROFILER && ENABLE_PROFILER_LUA && ENABLE_PROFILER_LUA_DEEP && !DISABLE_PROFILER_LUA_PCALL
        public static int pcall(this IntPtr luaState, int nArgs, int nResults, int errfunc)
        {
            System.Text.StringBuilder sbName = new System.Text.StringBuilder();
            if (luaState.iscfunction(-nArgs - 1))
            {
                sbName.Append(luaState.GetSimpleStackInfo(8));
                sbName.Append(">[C]");
            }
            else
            {
                string funcName, fileName;
                int lineStart, lineCur;
                luaState.GetFuncInfo(-nArgs - 1, out funcName, out fileName, out lineStart, out lineCur);
                sbName.Append(fileName);
                sbName.Append(":");
                sbName.Append(lineStart);
                //sbName.Append(" ");
                //sbName.Append(funcName); // NOTICE: we cannot get the name directly from the func.
            }
            using (var pcon = UnityEngineEx.ProfilerContext.Create("LuaFunc pcall"))
            using (var pconi = UnityEngineEx.ProfilerContext.Create(sbName))
                return lua_pcall(luaState, nArgs, nResults, errfunc);
        }
#else
        public static int pcall(this IntPtr luaState, int nArgs, int nResults, int errfunc) { return lua_pcall(luaState, nArgs, nResults, errfunc); }
#endif

        // lua_pop -> below

        public delegate void del_lua_pushboolean(IntPtr luaState, bool value);
        public static del_lua_pushboolean lua_pushboolean;
        public static void pushboolean(this IntPtr luaState, bool value) { lua_pushboolean(luaState, value); }

        public delegate void del_lua_pushcclosure(IntPtr luaState, CFunction fn, int n);
        public static del_lua_pushcclosure lua_pushcclosure;
        public static void pushcclosure(this IntPtr luaState, CFunction fn, int n) { lua_pushcclosure(luaState, fn, n); }

        // lua_pushcfunction -> below
        // lua_pushfstring
        // lua_pushinteger

        public delegate void del_lua_pushlightuserdata(IntPtr luaState, IntPtr udata);
        public static del_lua_pushlightuserdata lua_pushlightuserdata;
        public static void pushlightuserdata(this IntPtr luaState, IntPtr udata) { lua_pushlightuserdata(luaState, udata); }

        // lua_pushliteral

        public delegate void del_lua_pushlstring(IntPtr luaState, string str, IntPtr size);
        public static del_lua_pushlstring lua_pushlstring;
        public static void pushlstring(this IntPtr luaState, string str, IntPtr size) { lua_pushlstring(luaState, str, size); }

        public delegate void del_lua_pushbuffer(IntPtr luaState, IntPtr buffer, IntPtr size);
        public static del_lua_pushbuffer lua_pushbuffer;
        public static void pushlstring(this IntPtr luaState, byte[] str, IntPtr size) // lua_pushlstring+
        {
            GCHandle h = GCHandle.Alloc(str, GCHandleType.Pinned);
            lua_pushbuffer(luaState, h.AddrOfPinnedObject(), size);
            h.Free();
        }

        public delegate void del_lua_pushnil(IntPtr luaState);
        public static del_lua_pushnil lua_pushnil;
        public static void pushnil(this IntPtr luaState) { lua_pushnil(luaState); }

        public delegate void del_lua_pushnumber(IntPtr luaState, double number);
        public static del_lua_pushnumber lua_pushnumber;
        public static void pushnumber(this IntPtr luaState, double number) { lua_pushnumber(luaState, number); }

        public delegate void del_lua_pushstring(IntPtr luaState, string str);
        public static del_lua_pushstring lua_pushstring;
        public static void pushstring(this IntPtr luaState, string str) { lua_pushstring(luaState, str); }

        public delegate bool del_lua_pushthread(IntPtr L);
        public static del_lua_pushthread lua_pushthread;
        public static bool pushthread(this IntPtr L) { return lua_pushthread(L); }

        public delegate void del_lua_pushvalue(IntPtr luaState, int index);
        public static del_lua_pushvalue lua_pushvalue;
        public static void pushvalue(this IntPtr luaState, int index) { lua_pushvalue(luaState, index); }

        // lua_pushvfstring

        public delegate bool del_lua_rawequal(IntPtr luaState, int stackPos1, int stackPos2);
        public static del_lua_rawequal lua_rawequal;
        public static bool rawequal(this IntPtr luaState, int stackPos1, int stackPos2) { return lua_rawequal(luaState, stackPos1, stackPos2); }

        public delegate void del_lua_rawget(IntPtr luaState, int index);
        public static del_lua_rawget lua_rawget;
        public static void rawget(this IntPtr luaState, int index) { lua_rawget(luaState, index); }

        public delegate void del_lua_rawgeti(IntPtr luaState, int tableIndex, int index);
        public static del_lua_rawgeti lua_rawgeti;
        public static void rawget(this IntPtr luaState, int tableIndex, int index) { lua_rawgeti(luaState, tableIndex, index); }

        public delegate void del_lua_rawset(IntPtr luaState, int index);
        public static del_lua_rawset lua_rawset;
        public static void rawset(this IntPtr luaState, int index) { lua_rawset(luaState, index); }

        public delegate void del_lua_rawseti(IntPtr luaState, int tableIndex, int index);
        public static del_lua_rawseti lua_rawseti;
        public static void rawset(this IntPtr luaState, int tableIndex, int index) { lua_rawseti(luaState, tableIndex, index); }

        // lua_register

        public delegate void del_lua_remove(IntPtr luaState, int index);
        public static del_lua_remove lua_remove;
        public static void remove(this IntPtr luaState, int index) { lua_remove(luaState, index); }

        public delegate void del_lua_replace(IntPtr luaState, int index);
        public static del_lua_replace lua_replace;
        public static void replace(this IntPtr luaState, int index) { lua_replace(luaState, index); }

        public delegate int del_lua_resume(IntPtr L, int narg);
        public static del_lua_resume lua_resume;
        public static int resume(this IntPtr L, int narg) { return lua_resume(L, narg); }

        // lua_setallocf

        public delegate bool del_lua_setfenv(IntPtr luaState, int stackPos);
        public static del_lua_setfenv lua_setfenv;
        public static bool setfenv(this IntPtr luaState, int stackPos) { return lua_setfenv(luaState, stackPos); }

        public delegate void del_lua_setfield(IntPtr luaState, int stackPos, string name);
        public static del_lua_setfield lua_setfield;
        public static void setfield(this IntPtr luaState, int stackPos, string name) { lua_setfield(luaState, stackPos, name); }

        // lua_setglobal -> below
        // lua_sethook
        // lua_setlocal

        public delegate bool del_lua_setmetatable(IntPtr luaState, int objIndex);
        public static del_lua_setmetatable lua_setmetatable;
        public static bool setmetatable(this IntPtr luaState, int objIndex) { return lua_setmetatable(luaState, objIndex); }

        public delegate void del_lua_settable(IntPtr luaState, int index);
        public static del_lua_settable lua_settable;
        public static void settable(this IntPtr luaState, int index) { lua_settable(luaState, index); }

        public delegate void del_lua_settop(IntPtr luaState, int newTop);
        public static del_lua_settop lua_settop;
        public static void settop(this IntPtr luaState, int newTop) { lua_settop(luaState, newTop); }

        // lua_setupvalue

        public delegate int del_lua_status(IntPtr L);
        public static del_lua_status lua_status;
        public static int status(this IntPtr L) { return lua_status(L); }

        public delegate bool del_lua_toboolean(IntPtr luaState, int index);
        public static del_lua_toboolean lua_toboolean;
        public static bool toboolean(this IntPtr luaState, int index) { return lua_toboolean(luaState, index); }

        public delegate CFunction del_lua_tocfunction(IntPtr luaState, int index);
        public static del_lua_tocfunction lua_tocfunction;
        public static CFunction tocfunction(this IntPtr luaState, int index) { return lua_tocfunction(luaState, index); }

        // lua_tointeger

        public delegate IntPtr del_lua_tolstring(IntPtr luaState, int index, IntPtr out_strLen);
        public static del_lua_tolstring lua_tolstring;
        public static IntPtr tolstring(this IntPtr luaState, int index, out IntPtr strLen)
        {
            IntPtr p = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(IntPtr)));
            var rv = lua_tolstring(luaState, index, p);
            strLen = Marshal.ReadIntPtr(p);
            Marshal.FreeCoTaskMem(p);
            return rv;
        }

        public delegate double del_lua_tonumber(IntPtr luaState, int index);
        public static del_lua_tonumber lua_tonumber;
        public static double tonumber(this IntPtr luaState, int index) { return lua_tonumber(luaState, index); }

        public delegate IntPtr del_lua_topointer(IntPtr luaState, int index);
        public static del_lua_topointer lua_topointer;
        public static IntPtr topointer(this IntPtr luaState, int index) { return lua_topointer(luaState, index); }

        // lua_tostring -> below

        public delegate IntPtr del_lua_tothread(IntPtr L, int index);
        public static del_lua_tothread lua_tothread;
        public static IntPtr tothread(this IntPtr L, int index) { return lua_tothread(L, index); }

        public delegate IntPtr del_lua_touserdata(IntPtr luaState, int index);
        public static del_lua_touserdata lua_touserdata;
        public static IntPtr touserdata(this IntPtr luaState, int index) { return lua_touserdata(luaState, index); }

        public delegate int del_lua_type(IntPtr luaState, int index);
        public static del_lua_type lua_type;
        public static int type(this IntPtr luaState, int index) { return lua_type(luaState, index); }

        public delegate IntPtr del_lua_typename(IntPtr luaState, int type);
        public static del_lua_typename lua_typename;
        public static IntPtr typename(IntPtr luaState, int type) { return lua_typename(luaState, type); }

        // lua_upvalueindex -> below

        public delegate void del_lua_xmove(IntPtr from, IntPtr to, int n);
        public static del_lua_xmove lua_xmove;
        public static void xmove(this IntPtr from, IntPtr to, int n) { lua_xmove(from, to, n); }

        public delegate int del_lua_yield(IntPtr L, int nresults);
        public static del_lua_yield lua_yield;
        public static int yield(this IntPtr L, int nresults) { return lua_yield(L, nresults); }

#elif UNITY_IPHONE
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CFunction lua_atpanic(IntPtr luaState, CFunction panicf);
        public static CFunction atpanic(this IntPtr luaState, CFunction panicf) { return lua_atpanic(luaState, panicf); }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_call(IntPtr luaState, int nArgs, int nResults);
        public static void call(this IntPtr luaState, int nArgs, int nResults) { lua_call(luaState, nArgs, nResults); }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_checkstack(IntPtr luaState, int extra);
        public static bool checkstack(this IntPtr luaState, int extra) { return lua_checkstack(luaState, extra); }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_close(IntPtr luaState);
        public static void close(this IntPtr luaState) { lua_close(luaState); }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_concat(IntPtr luaState, int n);
        public static void concat(this IntPtr luaState, int n) { lua_concat(luaState, n); }

        // lua_cpcall

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_createtable(IntPtr luaState, int narr, int nrec);
        public static void createtable(this IntPtr luaState, int narr, int nrec) { lua_createtable(luaState, narr, nrec); }

        // lua_dump

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_equal(IntPtr luaState, int index1, int index2);
        public static bool equal(this IntPtr luaState, int index1, int index2) { return lua_equal(luaState, index1, index2); }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_error(IntPtr luaState);
        public static int error(this IntPtr luaState) { return lua_error(luaState); }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_gc(IntPtr luaState, int what, int data);
        public static int gc(this IntPtr luaState, int what, int data) { return lua_gc(luaState, what, data); }

        // lua_getallocf

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_getfenv(IntPtr luaState, int stackPos);
        public static void getfenv(this IntPtr luaState, int stackPos) { lua_getfenv(luaState, stackPos); }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_getfield(IntPtr luaState, int stackPos, string meta);
        public static void getfield(this IntPtr luaState, int stackPos, string meta) { lua_getfield(luaState, stackPos, meta); }

        // lua_getglobal -> below
        // lua_gethook
        // lua_gethookcount
        // lua_gethookmask
        // lua_getinfo
        // lua_getlocal

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_getmetatable(IntPtr luaState, int objIndex);
        public static bool getmetatable(this IntPtr luaState, int objIndex) { return lua_getmetatable(luaState, objIndex); }

        // lua_getstack

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_gettable(IntPtr luaState, int index);
        public static void gettable(this IntPtr luaState, int index) { lua_gettable(luaState, index); }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_gettop(IntPtr luaState);
        public static int gettop(this IntPtr luaState) { return lua_gettop(luaState); }

        // lua_getupvalue

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_insert(IntPtr luaState, int newTop);
        public static void insert(this IntPtr luaState, int newTop) { lua_insert(luaState, newTop); }

        // lua_isboolean -> below

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_iscfunction(IntPtr luaState, int index);
        public static bool iscfunction(this IntPtr luaState, int index) { return lua_iscfunction(luaState, index); }

        // lua_isfunction -> below
        // lua_islightuserdata -> below
        // lua_isnil -> below
        // lua_isnone -> below
        // lua_isnoneornil -> below

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_isnumber(IntPtr luaState, int index);
        public static bool isnumber(this IntPtr luaState, int index) { return lua_isnumber(luaState, index); }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_isstring(IntPtr luaState, int index);
        public static bool isstring(this IntPtr luaState, int index) { return lua_isstring(luaState, index); }

        // lua_istable -> below
        // lua_isthread -> below

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_isuserdata(IntPtr luaState, int stackPos);
        public static bool isuserdata(this IntPtr luaState, int stackPos) { return lua_isuserdata(luaState, stackPos); }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_lessthan(IntPtr luaState, int stackPos1, int stackPos2);
        public static bool lessthan(this IntPtr luaState, int stackPos1, int stackPos2) { return lua_lessthan(luaState, stackPos1, stackPos2); }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_load(IntPtr luaState, Reader reader, IntPtr ud, string chunkname);
        public static int load(this IntPtr luaState, Reader reader, IntPtr ud, string chunkname) { return lua_load(luaState, reader, ud, chunkname); }

        // lua_newstate
        // lua_newtable -> below

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr lua_newthread(IntPtr L);
        public static IntPtr newthread(this IntPtr L) { return lua_newthread(L); }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr lua_newuserdata(IntPtr luaState, IntPtr size);
        public static IntPtr newuserdata(this IntPtr luaState, IntPtr size) { return lua_newuserdata(luaState, size); }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_next(IntPtr luaState, int index);
        public static bool next(this IntPtr luaState, int index) { return lua_next(luaState, index); }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr lua_objlen(IntPtr luaState, int stackPos);
        public static IntPtr objlen(this IntPtr luaState, int stackPos) { return lua_objlen(luaState, stackPos); }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_pcall(IntPtr luaState, int nArgs, int nResults, int errfunc);
#if ENABLE_PROFILER && ENABLE_PROFILER_LUA && ENABLE_PROFILER_LUA_DEEP && !DISABLE_PROFILER_LUA_PCALL
        public static int pcall(this IntPtr luaState, int nArgs, int nResults, int errfunc)
        {
            System.Text.StringBuilder sbName = new System.Text.StringBuilder();
            if (luaState.iscfunction(-nArgs - 1))
            {
                sbName.Append(luaState.GetSimpleStackInfo(8));
                sbName.Append(">[C]");
            }
            else
            {
                string funcName, fileName;
                int lineStart, lineCur;
                luaState.GetFuncInfo(-nArgs - 1, out funcName, out fileName, out lineStart, out lineCur);
                sbName.Append(fileName);
                sbName.Append(":");
                sbName.Append(lineStart);
                //sbName.Append(" ");
                //sbName.Append(funcName); // NOTICE: we cannot get the name directly from the func.
            }
            using (var pcon = UnityEngineEx.ProfilerContext.Create("LuaFunc pcall"))
            using (var pconi = UnityEngineEx.ProfilerContext.Create(sbName))
                return lua_pcall(luaState, nArgs, nResults, errfunc);
        }
#else
        public static int pcall(this IntPtr luaState, int nArgs, int nResults, int errfunc) { return lua_pcall(luaState, nArgs, nResults, errfunc); }
#endif

        // lua_pop -> below

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushboolean(IntPtr luaState, bool value);
        public static void pushboolean(this IntPtr luaState, bool value) { lua_pushboolean(luaState, value); }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushcclosure(IntPtr luaState, CFunction fn, int n);
        public static void pushcclosure(this IntPtr luaState, CFunction fn, int n) { lua_pushcclosure(luaState, fn, n); }

        // lua_pushcfunction -> below
        // lua_pushfstring
        // lua_pushinteger

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushlightuserdata(IntPtr luaState, IntPtr udata);
        public static void pushlightuserdata(this IntPtr luaState, IntPtr udata) { lua_pushlightuserdata(luaState, udata); }

        // lua_pushliteral

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushlstring(IntPtr luaState, IntPtr str, IntPtr size);
        public static void pushlstring(this IntPtr luaState, string str, IntPtr size)
        {
            var ptr = Marshal.StringToCoTaskMemAnsi(str);
            lua_pushlstring(luaState, ptr, size);
            Marshal.FreeCoTaskMem(ptr);
        }
        public static void pushlstring(this IntPtr luaState, byte[] str, IntPtr size) // lua_pushlstring+
        {
            GCHandle gh = GCHandle.Alloc(str, GCHandleType.Pinned);
            lua_pushlstring(luaState, gh.AddrOfPinnedObject(), size);
            gh.Free();
        }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushnil(IntPtr luaState);
        public static void pushnil(this IntPtr luaState) { lua_pushnil(luaState); }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushnumber(IntPtr luaState, double number);
        public static void pushnumber(this IntPtr luaState, double number) { lua_pushnumber(luaState, number); }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushstring(IntPtr luaState, string str);
        public static void pushstring(this IntPtr luaState, string str) { lua_pushstring(luaState, str); }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_pushthread(IntPtr L);
        public static bool pushthread(this IntPtr L) { return lua_pushthread(L); }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushvalue(IntPtr luaState, int index);
        public static void pushvalue(this IntPtr luaState, int index) { lua_pushvalue(luaState, index); }

        // lua_pushvfstring

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_rawequal(IntPtr luaState, int stackPos1, int stackPos2);
        public static bool rawequal(this IntPtr luaState, int stackPos1, int stackPos2) { return lua_rawequal(luaState, stackPos1, stackPos2); }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_rawget(IntPtr luaState, int index);
        public static void rawget(this IntPtr luaState, int index) { lua_rawget(luaState, index); }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_rawgeti(IntPtr luaState, int tableIndex, int index);
        public static void rawget(this IntPtr luaState, int tableIndex, int index) { lua_rawgeti(luaState, tableIndex, index); }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_rawset(IntPtr luaState, int index);
        public static void rawset(this IntPtr luaState, int index) { lua_rawset(luaState, index); }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_rawseti(IntPtr luaState, int tableIndex, int index);
        public static void rawset(this IntPtr luaState, int tableIndex, int index) { lua_rawseti(luaState, tableIndex, index); }

        // lua_register

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_remove(IntPtr luaState, int index);
        public static void remove(this IntPtr luaState, int index) { lua_remove(luaState, index); }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_replace(IntPtr luaState, int index);
        public static void replace(this IntPtr luaState, int index) { lua_replace(luaState, index); }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_resume(IntPtr L, int narg);
        public static int resume(this IntPtr L, int narg) { return lua_resume(L, narg); }

        // lua_setallocf

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_setfenv(IntPtr luaState, int stackPos);
        public static bool setfenv(this IntPtr luaState, int stackPos) { return lua_setfenv(luaState, stackPos); }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_setfield(IntPtr luaState, int stackPos, string name);
        public static void setfield(this IntPtr luaState, int stackPos, string name) { lua_setfield(luaState, stackPos, name); }

        // lua_setglobal -> below
        // lua_sethook
        // lua_setlocal

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_setmetatable(IntPtr luaState, int objIndex);
        public static bool setmetatable(this IntPtr luaState, int objIndex) { return lua_setmetatable(luaState, objIndex); }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_settable(IntPtr luaState, int index);
        public static void settable(this IntPtr luaState, int index) { lua_settable(luaState, index); }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_settop(IntPtr luaState, int newTop);
        public static void settop(this IntPtr luaState, int newTop) { lua_settop(luaState, newTop); }

        // lua_setupvalue

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_status(IntPtr L);
        public static int status(this IntPtr L) { return lua_status(L); }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool lua_toboolean(IntPtr luaState, int index);
        public static bool toboolean(this IntPtr luaState, int index) { return lua_toboolean(luaState, index); }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CFunction lua_tocfunction(IntPtr luaState, int index);
        public static CFunction tocfunction(this IntPtr luaState, int index) { return lua_tocfunction(luaState, index); }

        // lua_tointeger

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr lua_tolstring(IntPtr luaState, int index, out IntPtr strLen);
        public static IntPtr tolstring(this IntPtr luaState, int index, out IntPtr strLen) { return lua_tolstring(luaState, index, out strLen); }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern double lua_tonumber(IntPtr luaState, int index);
        public static double tonumber(this IntPtr luaState, int index) { return lua_tonumber(luaState, index); }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr lua_topointer(IntPtr luaState, int index);
        public static IntPtr topointer(this IntPtr luaState, int index) { return lua_topointer(luaState, index); }

        // lua_tostring -> below

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr lua_tothread(IntPtr L, int index);
        public static IntPtr tothread(this IntPtr L, int index) { return lua_tothread(L, index); }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr lua_touserdata(IntPtr luaState, int index);
        public static IntPtr touserdata(this IntPtr luaState, int index) { return lua_touserdata(luaState, index); }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_type(IntPtr luaState, int index);
        public static int type(this IntPtr luaState, int index) { return lua_type(luaState, index); }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr lua_typename(IntPtr luaState, int type);
        public static IntPtr typename(IntPtr luaState, int type) { return lua_typename(luaState, type); }

        // lua_upvalueindex -> below

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_xmove(IntPtr from, IntPtr to, int n);
        public static void xmove(this IntPtr from, IntPtr to, int n) { lua_xmove(from, to, n); }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_yield(IntPtr L, int nresults);
        public static int yield(this IntPtr L, int nresults) { return lua_yield(L, nresults); }
#else
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_atpanic")]
        public static extern CFunction atpanic(this IntPtr luaState, CFunction panicf);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_call")]
        public static extern void call(this IntPtr luaState, int nArgs, int nResults);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_checkstack")]
        public static extern bool checkstack(this IntPtr luaState, int extra);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_close")]
        public static extern void close(this IntPtr luaState);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_concat")]
        public static extern void concat(this IntPtr luaState, int n);
        // lua_cpcall
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_createtable")]
        public static extern void createtable(this IntPtr luaState, int narr, int nrec);
        // lua_dump
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_equal")]
        public static extern bool equal(this IntPtr luaState, int index1, int index2);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_error")]
        public static extern int error(this IntPtr luaState);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_gc")]
        public static extern int gc(this IntPtr luaState, int what, int data);
        // lua_getallocf
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_getfenv")]
        public static extern void getfenv(this IntPtr luaState, int stackPos);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_getfield")]
        public static extern void getfield(this IntPtr luaState, int stackPos, string meta);
        // lua_getglobal -> below
        // lua_gethook
        // lua_gethookcount
        // lua_gethookmask
        // lua_getinfo
        // lua_getlocal
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_getmetatable")]
        public static extern bool getmetatable(this IntPtr luaState, int objIndex);
        // lua_getstack
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_gettable")]
        public static extern void gettable(this IntPtr luaState, int index);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_gettop")]
        public static extern int gettop(this IntPtr luaState);
        // lua_getupvalue
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_insert")]
        public static extern void insert(this IntPtr luaState, int newTop);
        // lua_isboolean -> below
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_iscfunction")]
        public static extern bool iscfunction(this IntPtr luaState, int index);
        // lua_isfunction -> below
        // lua_islightuserdata -> below
        // lua_isnil -> below
        // lua_isnone -> below
        // lua_isnoneornil -> below
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_isnumber")]
        public static extern bool isnumber(this IntPtr luaState, int index);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_isstring")]
        public static extern bool isstring(this IntPtr luaState, int index);
        // lua_istable -> below
        // lua_isthread -> below
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_isuserdata")]
        public static extern bool isuserdata(this IntPtr luaState, int stackPos);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_lessthan")]
        public static extern bool lessthan(this IntPtr luaState, int stackPos1, int stackPos2);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_load")]
        public static extern int load(this IntPtr luaState, Reader reader, IntPtr ud, string chunkname);
        // lua_newstate
        // lua_newtable -> below
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_newthread")]
        public static extern IntPtr newthread(this IntPtr L);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_newuserdata")]
        public static extern IntPtr newuserdata(this IntPtr luaState, IntPtr size);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_next")]
        public static extern bool next(this IntPtr luaState, int index);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_objlen")]
        public static extern IntPtr objlen(this IntPtr luaState, int stackPos);
#if ENABLE_PROFILER && ENABLE_PROFILER_LUA && ENABLE_PROFILER_LUA_DEEP && !DISABLE_PROFILER_LUA_PCALL
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_pcall")]
        public static extern int lua_pcall(IntPtr luaState, int nArgs, int nResults, int errfunc);
        public static int pcall(this IntPtr luaState, int nArgs, int nResults, int errfunc)
        {
            System.Text.StringBuilder sbName = new System.Text.StringBuilder();
            if (luaState.iscfunction(-nArgs - 1))
            {
                sbName.Append(luaState.GetSimpleStackInfo(8));
                sbName.Append(">[C]");
            }
            else
            {
                string funcName, fileName;
                int lineStart, lineCur;
                luaState.GetFuncInfo(-nArgs - 1, out funcName, out fileName, out lineStart, out lineCur);
                sbName.Append(fileName);
                sbName.Append(":");
                sbName.Append(lineStart);
                //sbName.Append(" ");
                //sbName.Append(funcName); // NOTICE: we cannot get the name directly from the func.
            }
            using (var pcon = UnityEngineEx.ProfilerContext.Create("LuaFunc pcall"))
            using (var pconi = UnityEngineEx.ProfilerContext.Create(sbName))
                return lua_pcall(luaState, nArgs, nResults, errfunc);
        }
#else
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_pcall")]
        public static extern int pcall(this IntPtr luaState, int nArgs, int nResults, int errfunc);
#endif
        // lua_pop -> below
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_pushboolean")]
        public static extern void pushboolean(this IntPtr luaState, bool value);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_pushcclosure")]
        public static extern void pushcclosure(this IntPtr luaState, CFunction fn, int n);
        // lua_pushcfunction -> below
        // lua_pushfstring
        // lua_pushinteger
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_pushlightuserdata")]
        public static extern void pushlightuserdata(this IntPtr luaState, IntPtr udata);
        // lua_pushliteral
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_pushlstring")]
        public static extern void pushlstring(this IntPtr luaState, string str, IntPtr size);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_pushlstring")]
        public static extern void pushlstring(this IntPtr luaState, byte[] str, IntPtr size); // lua_pushlstring+
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_pushnil")]
        public static extern void pushnil(this IntPtr luaState);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_pushnumber")]
        public static extern void pushnumber(this IntPtr luaState, double number);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_pushstring")]
        public static extern void pushstring(this IntPtr luaState, string str);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_pushthread")]
        public static extern bool pushthread(this IntPtr L);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_pushvalue")]
        public static extern void pushvalue(this IntPtr luaState, int index);
        // lua_pushvfstring
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_rawequal")]
        public static extern bool rawequal(this IntPtr luaState, int stackPos1, int stackPos2);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_rawget")]
        public static extern void rawget(this IntPtr luaState, int index);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_rawgeti")]
        public static extern void rawget(this IntPtr luaState, int tableIndex, int index);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_rawset")]
        public static extern void rawset(this IntPtr luaState, int index);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_rawseti")]
        public static extern void rawset(this IntPtr luaState, int tableIndex, int index);
        // lua_register
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_remove")]
        public static extern void remove(this IntPtr luaState, int index);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_replace")]
        public static extern void replace(this IntPtr luaState, int index);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_resume")]
        public static extern int resume(this IntPtr L, int narg);
        // lua_setallocf
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_setfenv")]
        public static extern bool setfenv(this IntPtr luaState, int stackPos);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_setfield")]
        public static extern void setfield(this IntPtr luaState, int stackPos, string name);
        // lua_setglobal -> below
        // lua_sethook
        // lua_setlocal
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_setmetatable")]
        public static extern bool setmetatable(this IntPtr luaState, int objIndex);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_settable")]
        public static extern void settable(this IntPtr luaState, int index);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_settop")]
        public static extern void settop(this IntPtr luaState, int newTop);
        // lua_setupvalue
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_status")]
        public static extern int status(this IntPtr L);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_toboolean")]
        public static extern bool toboolean(this IntPtr luaState, int index);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_tocfunction")]
        public static extern CFunction tocfunction(this IntPtr luaState, int index);
        // lua_tointeger
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_tolstring")]
        public static extern IntPtr tolstring(this IntPtr luaState, int index, out IntPtr strLen);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_tonumber")]
        public static extern double tonumber(this IntPtr luaState, int index);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_topointer")]
        public static extern IntPtr topointer(this IntPtr luaState, int index);
        // lua_tostring -> below
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_tothread")]
        public static extern IntPtr tothread(this IntPtr L, int index);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_touserdata")]
        public static extern IntPtr touserdata(this IntPtr luaState, int index);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_type")]
        public static extern int type(this IntPtr luaState, int index);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_typename")]
        public static extern IntPtr typename(IntPtr luaState, int type);
        // lua_upvalueindex -> below
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_xmove")]
        public static extern void xmove(this IntPtr from, IntPtr to, int n);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lua_yield")]
        public static extern int yield(this IntPtr L, int nresults);
#endif
        #endregion

#if (UNITY_WP8 || UNITY_METRO) && !UNITY_EDITOR
        public delegate IntPtr Alloc(IntPtr ud, IntPtr ptr, IntPtr osize, IntPtr nsize);
        public delegate int CFunction(IntPtr l);
        public delegate IntPtr Reader(IntPtr l, IntPtr ud, IntPtr size);
#elif UNITY_IPHONE && !UNITY_EDITOR
        public delegate IntPtr Alloc(IntPtr ud, IntPtr ptr, IntPtr osize, IntPtr nsize);
        public delegate int CFunction(IntPtr l);
        public delegate IntPtr Reader(IntPtr l, IntPtr ud, IntPtr size);
#else
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr Alloc(IntPtr ud, IntPtr ptr, IntPtr osize, IntPtr nsize);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int CFunction(IntPtr l);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr Reader(IntPtr l, IntPtr ud, IntPtr size);
#endif

        #region platform-independent
        // lua_Debug
        // lua_Hook
        // lua_Integer
        // lua_Number
        // lua_State
        // lua_Writer

        public static void getglobal(this IntPtr luaState, string name) // lua_getglobal
        {
            getfield(luaState, LUA_GLOBALSINDEX, name);
        }
        public static bool isboolean(this IntPtr luaState, int index) // lua_isboolean
        {
            return type(luaState, index) == LUA_TBOOLEAN;
        }
        public static bool isfunction(this IntPtr luaState, int stackPos) // lua_isfunction
        {
            return type(luaState, stackPos) == LUA_TFUNCTION;
        }
        public static bool islightuserdata(this IntPtr luaState, int stackPos) // lua_islightuserdata
        {
            return type(luaState, stackPos) == LUA_TLIGHTUSERDATA;
        }
        public static bool isnil(this IntPtr luaState, int index) // lua_isnil
        {
            return type(luaState, index) == LUA_TNIL;
        }
        public static bool isnone(this IntPtr luaState, int index) // lua_isnone
        {
            return type(luaState, index) == LUA_TNONE;
        }
        public static bool isnoneornil(this IntPtr luaState, int index) // lua_isnoneornil
        {
            return type(luaState, index) <= 0;
        }
        public static bool istable(this IntPtr luaState, int stackPos) // lua_istable
        {
            return type(luaState, stackPos) == LUA_TTABLE;
        }
        public static bool isthread(this IntPtr luaState, int stackPos) // lua_isthread
        {
            return type(luaState, stackPos) == LUA_TTHREAD;
        }
        public static void newtable(this IntPtr luaState) // lua_newtable
        {
            createtable(luaState, 0, 0);
        }
        public static void pop(this IntPtr luaState, int amount) // lua_pop
        {
            settop(luaState, -(amount) - 1);
        }
        public static void pushcfunction(this IntPtr luaState, CFunction fn) // lua_pushcfunction
        {
            pushcclosure(luaState, fn, 0);
        }
        public static void pushlstring(this IntPtr luaState, string str, int size) // lua_pushlstring++
        {
            pushlstring(luaState, str, new IntPtr(size));
        }
        public static void pushlstring(this IntPtr luaState, byte[] buffer, int size) // lua_pushlstring+++
        {
            pushlstring(luaState, buffer, new IntPtr(size));
        }
        public static void setglobal(this IntPtr luaState, string name) // lua_setglobal
        {
            setfield(luaState, LUA_GLOBALSINDEX, name);
        }
        public static byte[] tolstring(this IntPtr luaState, int index) // lua_tolstring+
        {
            IntPtr strlen;
            IntPtr str = tolstring(luaState, index, out strlen);
            if (str != IntPtr.Zero)
            {
                byte[] buffer = new byte[strlen.ToInt32()];
                Marshal.Copy(str, buffer, 0, strlen.ToInt32());
                return buffer;
            }
            else
            {
                return null;
            }
        }
        public static string tostring(this IntPtr luaState, int index) // lua_tostring
        {
            var buffer = tolstring(luaState, index);
            if (buffer == null)
            {
                return null;
            }
#if UNITY_EDITOR_WIN && LUA_USE_SYSTEM_ENCODING_ON_EDITOR_WIN
            return System.Text.Encoding.Default.GetString(buffer);
#elif UNITY_EDITOR
            return System.Text.Encoding.UTF8.GetString(buffer);
#elif UNITY_WP8 || UNITY_METRO
            return System.Text.Encoding.UTF8.GetString(buffer, 0, buffer.Length);
#else
            return System.Text.Encoding.UTF8.GetString(buffer);
#endif
        }
        public static string tostring_system_encoding(this IntPtr luaState, int index) // lua_tostring
        {
            var buffer = tolstring(luaState, index);
            if (buffer == null)
            {
                return null;
            }
            return System.Text.Encoding.Default.GetString(buffer);
        }
        public static void pushstring_system_encoding(this IntPtr luaState, string str)
        {
            var bytes = str.SystemEncode();
            luaState.pushbuffer(bytes);
        }
        public static int upvalueindex(int index) // lua_upvalueindex
        {
            return LUA_GLOBALSINDEX - index;
        }
        #endregion

        public static void PrepareLib()
        {
#if !(UNITY_IOS && !UNITY_EDITOR) && !ENABLE_IL2CPP
            try
            {
                Marshal.PrelinkAll(typeof(LuaCoreLib));
            }
            catch (Exception e)
            {
                UnityEngineEx.PlatDependant.LogError(e);
            }
#endif
        }

#if !UNITY_ENGINE && !UNITY_5_3_OR_NEWER
        static LuaCoreLib()
        {
            UnityEngineEx.PluginManager.LoadLib(LUADLL);
        }
#endif

    }

    public static class LuaAuxLib
    {
#if UNITY_EDITOR || !UNITY_ENGINE && !UNITY_5_3_OR_NEWER
        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "luaL_callmeta")]
        public static extern bool callmeta(this IntPtr luaState, int stackPos, byte[] name);
        public static bool callmeta(this IntPtr luaState, int stackPos, string name)
        {
            var bytes = name.DefaultEncode();
            return callmeta(luaState, stackPos, bytes);
        }
        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "luaL_checkudata")]
        public static extern IntPtr checkudata(this IntPtr luaState, int stackPos, byte[] meta);
        public static IntPtr checkudata(this IntPtr luaState, int stackPos, string meta)
        {
            var bytes = meta.DefaultEncode();
            return checkudata(luaState, stackPos, bytes);
        }
        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "luaL_error")]
        public static extern int error(this IntPtr luaState, byte[] message); // Notice the message should not contain '%' mark or convert it to "%%"
        public static int error(this IntPtr luaState, string message) // Notice the message should not contain '%' mark or convert it to "%%"
        {
            var bytes = message.DefaultEncode();
            return error(luaState, bytes);
        }
        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "luaL_getmetafield")]
        public static extern bool getmetafield(this IntPtr luaState, int stackPos, byte[] field);
        public static bool getmetafield(this IntPtr luaState, int stackPos, string field)
        {
            var bytes = field.DefaultEncode();
            return getmetafield(luaState, stackPos, bytes);
        }
        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "luaL_gsub")]
        public static extern byte[] gsub(this IntPtr luaState, byte[] str, byte[] pattern, byte[] replacement);
        public static string gsub(this IntPtr luaState, string str, string pattern, string replacement)
        {
            var buffer = gsub(luaState, str.DefaultEncode(), pattern.DefaultEncode(), replacement.DefaultEncode());
#if UNITY_EDITOR_WIN && LUA_USE_SYSTEM_ENCODING_ON_EDITOR_WIN
            return System.Text.Encoding.Default.GetString(buffer);
#else
            return System.Text.Encoding.UTF8.GetString(buffer);
#endif
        }
        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "luaL_loadbuffer")]
        public static extern int loadbuffer(this IntPtr luaState, byte[] buff, IntPtr size, byte[] name);
        public static int loadbuffer(this IntPtr luaState, string buff, IntPtr size, string name)
        {
            return loadbuffer(luaState, buff.DefaultEncode(), size, name.DefaultEncode());
        }
        public static int loadbuffer(this IntPtr luaState, byte[] buff, IntPtr size, string name) // luaL_loadbuffer+
        {
            return loadbuffer(luaState, buff, size, name.DefaultEncode());
        }
        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "luaL_loadfile")]
        public static extern int loadfile(this IntPtr luaState, byte[] filename);
        public static int loadfile(this IntPtr luaState, string filename)
        {
            var bytes = filename.SystemEncode();
            return loadfile(luaState, bytes);
        }
        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "luaL_loadstring")]
        public static extern int loadstring(this IntPtr luaState, byte[] chunk);
        public static int loadstring(this IntPtr luaState, string chunk)
        {
            var bytes = chunk.DefaultEncode();
            return loadstring(luaState, bytes);
        }
        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "luaL_newmetatable")]
        public static extern bool newmetatable(this IntPtr luaState, byte[] meta);
        public static bool newmetatable(this IntPtr luaState, string meta)
        {
            var bytes = meta.DefaultEncode();
            return newmetatable(luaState, bytes);
        }
        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "luaL_newstate")]
        public static extern IntPtr newstate();
        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "luaL_openlibs")]
        public static extern void openlibs(this IntPtr luaState);
        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "luaL_ref")]
        public static extern int refer(this IntPtr luaState, int registryIndex);
        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "luaL_unref")]
        public static extern void unref(this IntPtr luaState, int registryIndex, int reference);
        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "luaL_where")]
        public static extern void where(this IntPtr luaState, int level);
#elif UNITY_WP8 || UNITY_METRO
        public delegate bool del_luaL_callmeta(IntPtr luaState, int stackPos, string name);
        public static del_luaL_callmeta luaL_callmeta;
        public static bool callmeta(this IntPtr luaState, int stackPos, string name) { return luaL_callmeta(luaState, stackPos, name); }

        public delegate IntPtr del_luaL_checkudata(IntPtr luaState, int stackPos, string meta);
        public static del_luaL_checkudata luaL_checkudata;
        public static IntPtr checkudata(this IntPtr luaState, int stackPos, string meta) { return luaL_checkudata(luaState, stackPos, meta); }

        public delegate int del_luaL_error(IntPtr luaState, string message);
        public static del_luaL_error luaL_error;
        public static int error(this IntPtr luaState, string message) // Notice the message should not contain '%' mark or convert it to "%%"
        {
            return luaL_error(luaState, message);
        }

        public delegate bool del_luaL_getmetafield(IntPtr luaState, int stackPos, string field);
        public static del_luaL_getmetafield luaL_getmetafield;
        public static bool getmetafield(this IntPtr luaState, int stackPos, string field) { return luaL_getmetafield(luaState, stackPos, field); }

        public delegate string del_luaL_gsub(IntPtr luaState, string str, string pattern, string replacement);
        public static del_luaL_gsub luaL_gsub;
        public static string gsub(this IntPtr luaState, string str, string pattern, string replacement) { return luaL_gsub(luaState, str, pattern, replacement); }

        public delegate int del_luaL_loadbuffer(IntPtr luaState, string buff, IntPtr size, string name);
        public static del_luaL_loadbuffer luaL_loadbuffer;
        public static int loadbuffer(this IntPtr luaState, string buff, IntPtr size, string name) { return luaL_loadbuffer(luaState, buff, size, name); }

        public delegate int del_luaL_loadbufferRaw(IntPtr luaState, IntPtr buff, IntPtr size, string name);
        public static del_luaL_loadbufferRaw luaL_loadbufferRaw;
        public static int loadbuffer(this IntPtr luaState, byte[] buff, IntPtr size, string name) // luaL_loadbuffer+
        {
            GCHandle h = GCHandle.Alloc(buff, GCHandleType.Pinned);
            var rv = luaL_loadbufferRaw(luaState, h.AddrOfPinnedObject(), size, name);
            h.Free();
            return rv;
        }

        public delegate int del_luaL_loadfile(IntPtr luaState, string filename);
        public static del_luaL_loadfile luaL_loadfile;
        public static int loadfile(this IntPtr luaState, string filename) { return luaL_loadfile(luaState, filename); }

        public delegate int del_luaL_loadstring(IntPtr luaState, string chunk);
        public static del_luaL_loadstring luaL_loadstring;
        public static int loadstring(this IntPtr luaState, string chunk) { return luaL_loadstring(luaState, chunk); }

        public delegate bool del_luaL_newmetatable(IntPtr luaState, string meta);
        public static del_luaL_newmetatable luaL_newmetatable;
        public static bool newmetatable(this IntPtr luaState, string meta) { return luaL_newmetatable(luaState, meta); }

        public delegate IntPtr del_luaL_newstate();
        public static del_luaL_newstate luaL_newstate;
        public static IntPtr newstate() { return luaL_newstate(); }

        public delegate void del_luaL_openlibs(IntPtr luaState);
        public static del_luaL_openlibs luaL_openlibs;
        public static void openlibs(this IntPtr luaState) { luaL_openlibs(luaState); }

        public delegate int del_luaL_ref(IntPtr luaState, int registryIndex);
        public static del_luaL_ref luaL_ref;
        public static int refer(this IntPtr luaState, int registryIndex) { return luaL_ref(luaState, registryIndex); }

        public delegate void del_luaL_unref(IntPtr luaState, int registryIndex, int reference);
        public static del_luaL_unref luaL_unref;
        public static void unref(this IntPtr luaState, int registryIndex, int reference) { luaL_unref(luaState, registryIndex, reference); }

        public delegate void del_luaL_where(IntPtr luaState, int level);
        public static del_luaL_where luaL_where;
        public static void where(this IntPtr luaState, int level) { luaL_where(luaState, level); }

#elif UNITY_IPHONE
        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool luaL_callmeta(IntPtr luaState, int stackPos, string name);
        public static bool callmeta(this IntPtr luaState, int stackPos, string name) { return luaL_callmeta(luaState, stackPos, name); }

        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr luaL_checkudata(IntPtr luaState, int stackPos, string meta);
        public static IntPtr checkudata(this IntPtr luaState, int stackPos, string meta) { return luaL_checkudata(luaState, stackPos, meta); }

        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int luaL_error(IntPtr luaState, string message); // Notice the message should not contain '%' mark or convert it to "%%"
        public static int error(this IntPtr luaState, string message) { return luaL_error(luaState, message); } // Notice the message should not contain '%' mark or convert it to "%%"

        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool luaL_getmetafield(IntPtr luaState, int stackPos, string field);
        public static bool getmetafield(this IntPtr luaState, int stackPos, string field) { return luaL_getmetafield(luaState, stackPos, field); }

        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern string luaL_gsub(IntPtr luaState, string str, string pattern, string replacement);
        public static string gsub(this IntPtr luaState, string str, string pattern, string replacement) { return luaL_gsub(luaState, str, pattern, replacement); }

        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int luaL_loadbuffer(IntPtr luaState, IntPtr buff, IntPtr size, string name);
        public static int loadbuffer(this IntPtr luaState, string buff, IntPtr size, string name)
        {
            var ptr = Marshal.StringToCoTaskMemAnsi(buff);
            var rv = luaL_loadbuffer(luaState, ptr, size, name);
            Marshal.FreeCoTaskMem(ptr);
            return rv;
        }
        public static int loadbuffer(this IntPtr luaState, byte[] buff, IntPtr size, string name) // luaL_loadbuffer+
        {
            var gh = GCHandle.Alloc(buff, GCHandleType.Pinned);
            var rv = luaL_loadbuffer(luaState, gh.AddrOfPinnedObject(), size, name);
            gh.Free();
            return rv;
        }

        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int luaL_loadfile(IntPtr luaState, string filename);
        public static int loadfile(this IntPtr luaState, string filename) { return luaL_loadfile(luaState, filename); }

        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int luaL_loadstring(IntPtr luaState, string chunk);
        public static int loadstring(this IntPtr luaState, string chunk) { return luaL_loadstring(luaState, chunk); }

        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool luaL_newmetatable(IntPtr luaState, string meta);
        public static bool newmetatable(this IntPtr luaState, string meta) { return luaL_newmetatable(luaState, meta); }

        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr luaL_newstate();
        public static IntPtr newstate() { return luaL_newstate(); }

        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void luaL_openlibs(IntPtr luaState);
        public static void openlibs(this IntPtr luaState) { luaL_openlibs(luaState); }

        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int luaL_ref(IntPtr luaState, int registryIndex);
        public static int refer(this IntPtr luaState, int registryIndex) { return luaL_ref(luaState, registryIndex); }

        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void luaL_unref(IntPtr luaState, int registryIndex, int reference);
        public static void unref(this IntPtr luaState, int registryIndex, int reference) { luaL_unref(luaState, registryIndex, reference); }

        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void luaL_where(IntPtr luaState, int level);
        public static void where(this IntPtr luaState, int level) { luaL_where(luaState, level); }
#else
        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "luaL_callmeta")]
        public static extern bool callmeta(this IntPtr luaState, int stackPos, string name);
        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "luaL_checkudata")]
        public static extern IntPtr checkudata(this IntPtr luaState, int stackPos, string meta);
        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "luaL_error")]
        public static extern int error(this IntPtr luaState, string message); // Notice the message should not contain '%' mark or convert it to "%%"
        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "luaL_getmetafield")]
        public static extern bool getmetafield(this IntPtr luaState, int stackPos, string field);
        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "luaL_gsub")]
        public static extern string gsub(this IntPtr luaState, string str, string pattern, string replacement);
        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "luaL_loadbuffer")]
        public static extern int loadbuffer(this IntPtr luaState, string buff, IntPtr size, string name);
        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "luaL_loadbuffer")]
        public static extern int loadbuffer(this IntPtr luaState, byte[] buff, IntPtr size, string name); // luaL_loadbuffer+
        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "luaL_loadfile")]
        public static extern int loadfile(this IntPtr luaState, string filename);
        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "luaL_loadstring")]
        public static extern int loadstring(this IntPtr luaState, string chunk);
        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "luaL_newmetatable")]
        public static extern bool newmetatable(this IntPtr luaState, string meta);
        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "luaL_newstate")]
        public static extern IntPtr newstate();
        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "luaL_openlibs")]
        public static extern void openlibs(this IntPtr luaState);
        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "luaL_ref")]
        public static extern int refer(this IntPtr luaState, int registryIndex);
        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "luaL_unref")]
        public static extern void unref(this IntPtr luaState, int registryIndex, int reference);
        [DllImport(LuaCoreLib.LUADLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "luaL_where")]
        public static extern void where(this IntPtr luaState, int level);
#endif

        // luaL_Buffer
        // luaL_Reg

        // luaL_addchar
        // luaL_addlstring
        // luaL_addsize
        // luaL_addstring
        // luaL_addvalue
        // luaL_argcheck
        // luaL_argerror
        // luaL_buffinit

        // luaL_callmeta -> above

        // luaL_checkany
        // luaL_checkint
        // luaL_checkinteger
        // luaL_checklong
        // luaL_checklstring
        // luaL_checknumber
        // luaL_checkoption
        // luaL_checkstack
        // luaL_checkstring
        // luaL_checktype

        // luaL_checkudata -> above

        public static int dofile(this IntPtr luaState, string fileName) // luaL_dofile
        {
            int result = loadfile(luaState, fileName);
            if (result != 0)
                return result;

            return LuaCoreLib.pcall(luaState, 0, LuaCoreLib.LUA_MULTRET, 0);
        }
        public static int dostring(this IntPtr luaState, string chunk) // luaL_dostring
        {
            int result = loadstring(luaState, chunk);
            if (result != 0)
                return result;

            return LuaCoreLib.pcall(luaState, 0, LuaCoreLib.LUA_MULTRET, 0);
        }

        // luaL_error -> above

        // luaL_getmetafield -> above

        public static void getmetatable(this IntPtr luaState, string meta) // luaL_getmetatable
        {
            LuaCoreLib.getfield(luaState, LuaCoreLib.LUA_REGISTRYINDEX, meta);
        }

        // luaL_gsub -> above

        // luaL_loadbuffer -> above
        public static int loadbuffer(this IntPtr l, string buff, int size, string name) // luaL_loadbuffer++
        {
            return loadbuffer(l, buff, new IntPtr(size), name);
        }
        public static int loadbuffer(this IntPtr l, byte[] buff, int size, string name) // luaL_loadbuffer+++
        {
            return loadbuffer(l, buff, new IntPtr(size), name);
        }

        // luaL_loadfile -> above

        // luaL_loadstring -> above

        // luaL_newmetatable -> above

        // luaL_newstate -> above

        // luaL_openlibs -> above

        // luaL_optint
        // luaL_optinteger
        // luaL_optlong
        // luaL_optlstring
        // luaL_optnumber
        // luaL_optstring
        // luaL_prepbuffer
        // luaL_pushresult

        // luaL_ref -> above

        // luaL_register
        public static string typename(this IntPtr luaState, int stackPos) // luaL_typename
        {
            //var pstr = LuaCoreLib.typename(luaState, LuaCoreLib.type(luaState, stackPos));
            //if (pstr == IntPtr.Zero)
            //{
            //    return null;
            //}
            //else
            //{
            //    return System.Runtime.InteropServices.Marshal.PtrToStringAnsi(pstr);
            //}
            return LuaCoreLib.typename(LuaCoreLib.type(luaState, stackPos));
        }
        // luaL_typerror

        // luaL_unref -> above

        // luaL_where -> above
    }

    public static class LuaLibEx
    {
        public static int refer(this IntPtr l)
        {
            return LuaAuxLib.refer(l, LuaCoreLib.LUA_REGISTRYINDEX);
        }
        public static void unref(this IntPtr l, int refid)
        {
            LuaAuxLib.unref(l, LuaCoreLib.LUA_REGISTRYINDEX, refid);
        }
        public static void getref(this IntPtr l, int refid)
        {
            LuaCoreLib.rawget(l, LuaCoreLib.LUA_REGISTRYINDEX, refid);
        }
        public static int dobuffer(this IntPtr luaState, byte[] chunk)
        {
            int result = LuaAuxLib.loadbuffer(luaState, chunk, chunk.Length, "unnamed-chunk");
            if (result != 0)
                return result;

            return LuaCoreLib.pcall(luaState, 0, -1, 0);
        }
        public static int dofile(this IntPtr luaState, string fileName, int errfunc)
        {
            int result = LuaAuxLib.loadfile(luaState, fileName);
            if (result != 0)
                return result;

            return LuaCoreLib.pcall(luaState, 0, LuaCoreLib.LUA_MULTRET, errfunc);
        }
        public static void pushbuffer(this IntPtr luaState, byte[] buffer)
        {
            LuaCoreLib.pushlstring(luaState, buffer, new IntPtr(buffer.Length));
        }
        public static int getn(this IntPtr l, int index)
        {
            return LuaCoreLib.objlen(l, index).ToInt32();
        }
        public static byte[] DefaultEncode(this string str)
        {
#if UNITY_EDITOR_WIN && LUA_USE_SYSTEM_ENCODING_ON_EDITOR_WIN
            var encoding = System.Text.Encoding.Default;
#else
            var encoding = System.Text.Encoding.UTF8;
#endif
            var size = encoding.GetMaxByteCount(str.Length + 1);
            var rv = new byte[size];
            encoding.GetBytes(str, 0, str.Length, rv, 0);
            return rv;
        }
        public static byte[] SystemEncode(this string str)
        {
            var encoding = System.Text.Encoding.Default;
            var size = encoding.GetMaxByteCount(str.Length + 1);
            var rv = new byte[size];
            encoding.GetBytes(str, 0, str.Length, rv, 0);
            return rv;
        }
        public static byte[] ASCIIEncode(this string str)
        {
            var encoding = System.Text.Encoding.ASCII;
            var size = encoding.GetMaxByteCount(str.Length);
            var rv = new byte[size];
            encoding.GetBytes(str, 0, str.Length, rv, 0);
            return rv;
        }
    }
}

#if !UNITY_ENGINE && !UNITY_5_3_OR_NEWER
namespace AOT
{
    public class MonoPInvokeCallbackAttribute : System.Attribute
    {
        public MonoPInvokeCallbackAttribute(System.Type t) { }
    }
}
#endif