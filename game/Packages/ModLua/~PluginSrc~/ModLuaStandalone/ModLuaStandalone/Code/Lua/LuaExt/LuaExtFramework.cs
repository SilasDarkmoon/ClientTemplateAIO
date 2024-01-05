using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using LuaLib;
using UnityEngineEx;
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
using UnityEngine;
#endif

using lua = LuaLib.LuaCoreLib;
using lual = LuaLib.LuaAuxLib;
using luae = LuaLib.LuaLibEx;

namespace LuaLib
{
    public static partial class LuaExLibs
    {
        private static LuaExLibItem _LuaExLib_Framework_Instance = new LuaExLibItem(LuaFramework.Init, 0);
#if UNITY_EDITOR
        private static LuaExLibItem _LuaExLib_LuaInit_Instance = new LuaExLibItem(LuaFramework.InitLua, 1000);
#endif
    }

    public static class LuaFramework
    {
        public static void Init(IntPtr L)
        {
            if (L != IntPtr.Zero)
            {
                L.atpanic(ClrDelPanic);

                //using (var lr = new LuaStateRecover(L))
                {
                    L.GetGlobal("clr"); // clr
                    if (L.istable(-1))
                    {
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
                        L.pushcfunction(ClrDelCoroutine); // clr func
                        L.SetField(-2, "coroutine"); // clr
                        L.pushcfunction(ClrDelBehavCoroutine); // clr func
                        L.SetField(-2, "bcoroutine"); // clr
                        L.pushcfunction(ClrDelGetUnityCoroutine); // clr func
                        L.SetField(-2, "getucoroutine"); // clr
                        L.pushcfunction(ClrDelGetLuaCoroutine); // clr func
                        L.SetField(-2, "getlcoroutine"); // clr
#endif
                        L.pushcfunction(ClrDelRunningCoroutine); // clr func
                        L.SetField(-2, "runningco"); // clr
                        L.pushcfunction(ClrDelCoroutineFinally); // clr func
                        L.SetField(-2, "cofinally"); // clr
                        L.pushcfunction(ClrDelCoroutineContinue); // clr func
                        L.SetField(-2, "cocontinue"); // clr
                        L.pushcfunction(ClrDelReset); // clr func
                        L.SetField(-2, "reset"); // clr
                        L.PushString(ThreadSafeValues.AppPlatform); // clr plat
                        L.SetField(-2, "plat"); // clr
                        L.pushcfunction(ClrDelGetMeID);
                        L.SetField(-2, "meid");
                        L.pushcfunction(ClrDelSplitStr);
                        L.SetField(-2, "splitstr");
                        L.pushcfunction(ClrDelFormatDataString);
                        L.SetField(-2, "datastr");
                        L.pushcfunction(ClrDelFormatJsonString);
                        L.SetField(-2, "jsonstr");
                        L.pushcfunction(ClrDelFormatLuaString);
                        L.SetField(-2, "luastr");
                        L.pushcfunction(ClrDelCurrentLua);
                        L.SetField(-2, "thislua");
                        L.pushcfunction(ClrDelGetLuaRegistry);
                        L.SetField(-2, "luareg");
                        L.pushcfunction(ClrDelToPointer);
                        L.SetField(-2, "topointer");
                        L.pushcfunction(ClrDelNewUserdata);
                        L.SetField(-2, "newud");
                        L.pushcfunction(ClrDelUserdataInfo);
                        L.SetField(-2, "udinfo");
                        L.pushcfunction(ClrDelRandomState);
                        L.SetField(-2, "randomstate");
                        L.PushString(ThreadSafeValues.UpdatePath);
                        L.SetField(-2, "updatepath");
                        L.PushString(ThreadSafeValues.LogPath);
                        L.SetField(-2, "logpath");
                        L.pushcfunction(ClrDelFormat);
                        L.SetField(-2, "format");
                        L.pushcfunction(ClrDelGetLangValueOfUserDataType);
                        L.SetField(-2, "trans");
                        L.pushcfunction(ClrDelGetLangValueOfStringType);
                        L.SetField(-2, "transstr");
                        L.pushcfunction(ClrDelUpdateLanguageConverter);
                        L.SetField(-2, "updatetrans");
                        L.pushcfunction(ClrDelGetExtendedHash);
                        L.SetField(-2, "exhash");
                        L.pushcfunction(ClrDelGetLastDecimal);
                        L.SetField(-2, "lastdecimal");
                        L.pushcfunction(ClrDelGetLastUInt64);
                        L.SetField(-2, "lastulong");
                        L.pushcfunction(ClrDelGetLastInt64);
                        L.SetField(-2, "lastlong");

                        // profiler
                        L.pushcfunction(LuaProfileHelper.Del_ProfilerBeginSample);
                        L.SetField(-2, "beginsample");
                        L.pushcfunction(LuaProfileHelper.Del_ProfilerEndSample);
                        L.SetField(-2, "endsample");
                        L.pushcfunction(LuaProfileHelper.Del_AppendProfilerMessage);
                        L.SetField(-2, "profilermess");
                    }
                    L.pop(1); // (empty)

                    // UnityEngine.Debug.Log
                    L.pushcfunction(LuaHub.LuaFuncOnInfo); // func
                    L.SetGlobal("print"); // (empty)
                    L.pushcfunction(LuaHub.LuaFuncOnInfo); // func
                    L.SetGlobal("printi"); // (empty)
                    L.pushcfunction(LuaHub.LuaFuncOnWarning); // func
                    L.SetGlobal("printw"); // (empty)
                    L.pushcfunction(LuaHub.LuaFuncOnError); // func
                    L.SetGlobal("printe"); // (empty)
                }

                ClrFuncReset(L);
            }
        }

        public delegate void InitDelegate(IntPtr l);
        public static readonly List<InitDelegate> FurtherInitFuncs = new List<InitDelegate>();
        public class FurtherInit
        {
            public FurtherInit(InitDelegate init)
            {
                if (init != null)
                {
                    FurtherInitFuncs.Add(init);
                }
            }
        }

        public static bool TryRequireLua(IntPtr l, string lib)
        {
            l.GetGlobal("require"); // require
            l.PushString(lib); // require "lib"
            if (l.pcall(1, 0, 0) == 0)
            {
                return true;
            }
            else
            {
                l.pop(1);
                return false;
            }
        }
        public static void InitLua_Critical(IntPtr l)
        {
            // mods
            var mods = LuaFileManager.GetCriticalLuaMods();
            for (int i = 0; i < mods.Length; ++i)
            {
                var mod = mods[i];
                TryRequireLua(l, "?raw.mod.\"" + mod + "\".init");
            }

            TryRequireLua(l, "?raw.init");
        }
        public static void InitLua_Mods(IntPtr l)
        {
            var flags = ResManager.GetValidDistributeFlags();
            for (int i = 0; i < flags.Length; ++i)
            {
                var flag = flags[i];
                TryRequireLua(l, "?raw.mod.\"" + flag + "\".init");
            }
        }
        public static void InitLua_Dist(IntPtr l)
        {
            // distribute
            var flags = ResManager.GetValidDistributeFlags();
            for (int i = 0; i < flags.Length; ++i)
            {
                var flag = flags[i];
                TryRequireLua(l, "distribute." + flag);
            }
        }
        public static void InitLua_PreInit(IntPtr l)
        {
            TryRequireLua(l, "preinit");
        }
        public static void InitLua_PostInit(IntPtr l)
        {
            TryRequireLua(l, "postinit");
        }
        public static void InitLua(IntPtr l)
        {
            InitLua_PreInit(l);
            InitLua_Critical(l);
            InitLua_Mods(l);
            InitLua_Dist(l);
            InitLua_PostInit(l);
        }

#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        private static void OnUnityStart()
        {
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
            ResManager.AddInitItem(ResManager.LifetimeOrders.PreEntrySceneDone, GlobalLuaInitLua);
#endif
        }
#if UNITY_EDITOR || !UNITY_ENGINE && !UNITY_5_3_OR_NEWER
        private static bool _Awaken = false;
        private static void GlobalLuaInitLua()
        {
            if (_Awaken)
            {
                GlobalLua.Init();
                InitLua(GlobalLua.L.L);
            }
            else
            {
                _Awaken = true;
            }
        }
#else
        private static void GlobalLuaInitLua()
        {
            GlobalLua.Init();
            InitLua(GlobalLua.L.L);
        }
#endif

#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
        public static readonly lua.CFunction ClrDelCoroutine = new lua.CFunction(ClrFuncCoroutine);
        public static readonly lua.CFunction ClrDelBehavCoroutine = new lua.CFunction(ClrFuncBehavCoroutine);
        public static readonly lua.CFunction ClrDelGetUnityCoroutine = new lua.CFunction(ClrFuncGetUnityCoroutine);
        public static readonly lua.CFunction ClrDelGetLuaCoroutine = new lua.CFunction(ClrFuncGetLuaCoroutine);
#endif
        public static readonly lua.CFunction ClrDelRunningCoroutine = new lua.CFunction(ClrFuncRunningCoroutine);
        public static readonly lua.CFunction ClrDelCoroutineFinally = new lua.CFunction(ClrFuncCoroutineFinally);
        public static readonly lua.CFunction ClrDelCoroutineContinue = new lua.CFunction(ClrFuncCoroutineContinue);
        public static readonly lua.CFunction ClrDelPanic = new lua.CFunction(ClrFuncPanic);
        public static readonly lua.CFunction ClrDelReset = new lua.CFunction(ClrFuncReset);
        public static readonly lua.CFunction ClrDelApkLoader = new lua.CFunction(ClrFuncApkLoader);
        public static readonly lua.CFunction ClrDelGetMeID = new lua.CFunction(ClrFuncGetMeID);
        public static readonly lua.CFunction ClrDelSplitStr = new lua.CFunction(ClrFuncSplitStr);
        public static readonly lua.CFunction ClrDelFormatDataString = new lua.CFunction(ClrFuncFormatDataString);
        public static readonly lua.CFunction ClrDelFormatLuaString = new lua.CFunction(ClrFuncFormatLuaString);
        public static readonly lua.CFunction ClrDelFormatJsonString = new lua.CFunction(ClrFuncFormatJsonString);
        public static readonly lua.CFunction ClrDelCurrentLua = new lua.CFunction(ClrFuncCurrentLua);
        public static readonly lua.CFunction ClrDelGetLuaRegistry = new lua.CFunction(ClrFuncGetLuaRegistry);
        public static readonly lua.CFunction ClrDelToPointer = new lua.CFunction(ClrFuncToPointer);
        public static readonly lua.CFunction ClrDelNewUserdata = new lua.CFunction(ClrFuncNewUserdata);
        public static readonly lua.CFunction ClrDelUserdataInfo = new lua.CFunction(ClrFuncUserdataInfo);
        public static readonly lua.CFunction ClrDelRandomState = new lua.CFunction(ClrFuncRandomState);
        public static readonly lua.CFunction ClrDelFormat = new lua.CFunction(ClrFuncFormat);
        public static readonly lua.CFunction ClrDelGetLangValueOfUserDataType = new lua.CFunction(ClrFuncGetLangValueOfUserDataType);
        public static readonly lua.CFunction ClrDelGetLangValueOfStringType = new lua.CFunction(ClrFuncGetLangValueOfStringType);
        public static readonly lua.CFunction ClrDelUpdateLanguageConverter = new lua.CFunction(UpdateLanguageConverter);
        public static readonly lua.CFunction ClrDelGetExtendedHash = new lua.CFunction(ClrFuncGetExtendedHash);
        public static readonly lua.CFunction ClrDelGetLastDecimal = new lua.CFunction(ClrFuncGetLastDecimal);
        public static readonly lua.CFunction ClrDelGetLastUInt64 = new lua.CFunction(ClrFuncGetLastUInt64);
        public static readonly lua.CFunction ClrDelGetLastInt64 = new lua.CFunction(ClrFuncGetLastInt64);

#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncCoroutine(IntPtr l)
        {
            var oldtop = l.gettop();

            if (l.isfunction(1))
            {
                var lfunc = new LuaOnStackFunc(l, 1);
                var co = GlobalLua.StartLuaCoroutine(lfunc);
                l.settop(oldtop);
                l.PushLua(co);
            }
            else if (l.isthread(1))
            {
                var lthd = new LuaOnStackThread(l, 1);
                var co = GlobalLua.StartLuaCoroutine(lthd);
                l.settop(oldtop);
                l.PushLua(co);
            }

            return l.gettop() - oldtop;
        }
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncBehavCoroutine(IntPtr l)
        {
            var oldtop = l.gettop();

            if (l.IsObject(1))
            {
                var behav = l.GetLua<UnityEngine.MonoBehaviour>(1);
                if (l.isfunction(2))
                {
                    var lfunc = new LuaOnStackFunc(l, 2);
                    var co = GlobalLua.StartLuaCoroutineForBehav(behav, lfunc);
                    l.settop(oldtop);
                    l.PushLua(co);
                }
                else if (l.isthread(2))
                {
                    var lthd = new LuaOnStackThread(l, 2);
                    var co = GlobalLua.StartLuaCoroutineForBehav(behav, lthd);
                    l.settop(oldtop);
                    l.PushLua(co);
                }
            }

            return l.gettop() - oldtop;
        }
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncGetUnityCoroutine(IntPtr l)
        {
            if (l.gettop() <= 0)
            {
                var coinfo = CoroutineRunner.CurrentCoroutineInfo;
                var co = CoroutineRunner.CurrentCoroutine;
                l.PushLua(coinfo);
                l.PushLua(co);
                return 2;
            }
            else if (l.isthread(1))
            {
                var lthd = l.tothread(1);
                var co = LuaStateHelper.GetUnityCoroutine(lthd);
                l.PushLua(co);
                l.PushLua(co == null ? null : co.coroutine);
                return 2;
            }
            else
            {
                var raw = l.GetLua(1);
                Coroutine co = raw as Coroutine;
                if (co != null)
                {
                    var coinfo = CoroutineRunner.GetCoroutineInfo(co);
                    if (coinfo != null)
                    {
                        l.PushLua(coinfo);
                        l.PushLua(co);
                        return 2;
                    }
                }
                else
                {
                    var coinfo = raw as CoroutineRunner.CoroutineInfo;
                    if (coinfo != null)
                    {
                        if (CoroutineRunner.RunningCoroutines.Contains(coinfo))
                        {
                            l.PushLua(coinfo);
                            l.PushLua(coinfo.coroutine);
                            return 2;
                        }
                    }
                }
            }
            return 0;
        }
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncGetLuaCoroutine(IntPtr l)
        {
            if (l.gettop() <= 0)
            {
                var lthd = LuaStateHelper.RunningLuaThread;
                if (lthd != IntPtr.Zero)
                {
                    if (lthd.Indicator() == l.Indicator())
                    {
                        lthd.pushthread();
                        if (lthd != l)
                        {
                            lthd.xmove(l, 1);
                        }
                    }
                    else
                    {
                        l.PushLuaObject(new LuaOnStackThread(lthd));
                    }
                    return 1;
                }
            }
            else
            {
                if (l.isthread(1))
                {
                    var lthd = l.tothread(1);
                    var co = LuaStateHelper.GetUnityCoroutine(lthd);
                    if (co != null)
                    {
                        l.pushvalue(1);
                        return 1;
                    }
                }
                else
                {
                    var raw = l.GetLua(1);
                    CoroutineRunner.CoroutineInfo coinfo = raw as CoroutineRunner.CoroutineInfo;
                    if (coinfo == null)
                    {
                        Coroutine co = raw as Coroutine;
                        if (co != null)
                        {
                            coinfo = CoroutineRunner.GetCoroutineInfo(co);
                        }
                    }
                    if (coinfo != null)
                    {
                        var lthd = LuaStateHelper.GetLuaCoroutine(coinfo);
                        if (lthd != IntPtr.Zero)
                        {
                            if (lthd.Indicator() == l.Indicator())
                            {
                                lthd.pushthread();
                                if (lthd != l)
                                {
                                    lthd.xmove(l, 1);
                                }
                            }
                            else
                            {
                                l.PushLuaObject(new LuaOnStackThread(lthd));
                            }
                            return 1;
                        }
                    }
                }
            }
            return 0;
        }
#endif

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncRunningCoroutine(IntPtr l)
        {
            if (l == LuaStateHelper.RunningLuaThread)
            {
                l.pushthread();
                return 1;
            }
            else
            {
                return 0;
            }
        }

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncCoroutineFinally(IntPtr l)
        {
            var argcnt = l.gettop();
            if (argcnt <= 0)
            { // return clr.cofinally() -- get current co's finally
                l.pushlightuserdata(LuaConst.LRKEY_CO_FINALLY); // #fin
                l.gettable(lua.LUA_REGISTRYINDEX); // fin
                if (l.istable(-1))
                {
                    l.pushthread(); // fin thd
                    l.gettable(-2); // fin func
                    l.remove(-2); // func
                    return 1;
                }
                l.pop(1); // X
            }
            else if (argcnt == 1 && l.isnil(1) || argcnt >= 2 && l.isnil(1) && l.isnil(2))
            { // clr.cofinally(nil) -- clear current co's finally
                l.pushlightuserdata(LuaConst.LRKEY_CO_FINALLY); // #fin
                l.gettable(lua.LUA_REGISTRYINDEX); // fin
                if (l.istable(-1))
                {
                    l.pushthread(); // fin thd
                    l.pushnil(); // fin thd nil
                    l.settable(-3); // fin
                }
                l.pop(1); // X
            }
            else if (argcnt == 1 && l.isfunction(1))
            { // clr.cofinally(function()end) -- set current co's finally
                l.pushlightuserdata(LuaConst.LRKEY_CO_FINALLY); // #fin
                l.gettable(lua.LUA_REGISTRYINDEX); // fin
                if (!l.istable(-1))
                {
                    l.pop(1); // X
                    l.newtable(); // fin
                    l.pushlightuserdata(LuaConst.LRKEY_CO_FINALLY); // fin #fin
                    l.pushvalue(-2); // fin #fin fin
                    l.settable(lua.LUA_REGISTRYINDEX); // fin
                    l.newtable(); // fin meta
                    l.PushString(LuaConst.LS_COMMON_K); // fin meta "k"
                    l.SetField(-2, LuaConst.LS_META_KEY_MODE); // fin meta
                    l.setmetatable(-2); // fin
                }
                l.pushthread(); // fin thd
                l.pushvalue(1); // fin thd func
                l.settable(-3); // fin
                l.pop(1); // X
            }
            else if (argcnt == 1)
            { // return clr.cofinally(co) -- get target co's finally
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
                var irv = ClrFuncGetLuaCoroutine(l);
                if (irv > 0)
#else
                l.pushvalue(1);
#endif
                {
                    l.settop(2); // lco
                    l.pushlightuserdata(LuaConst.LRKEY_CO_FINALLY); // lco #fin
                    l.gettable(lua.LUA_REGISTRYINDEX); // lco fin
                    if (l.istable(-1))
                    {
                        l.pushvalue(-2); // lco fin lco
                        l.gettable(-2); // lco fin func
                        l.insert(-3); // func lco fin
                        l.pop(2); // func
                        return 1;
                    }
                    l.pop(2); // X
                }
            }
            else if (l.isnil(2))
            { // clr.cofinally(co, nil) -- clear target co's finally
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
                var irv = ClrFuncGetLuaCoroutine(l);
                if (irv <= 0)
                {
                    return 0;
                }
                else
                {
                    l.settop(argcnt + 1); // lco
                }
#else
                l.pushvalue(1); // lco
#endif
                l.pushlightuserdata(LuaConst.LRKEY_CO_FINALLY); // lco #fin
                l.gettable(lua.LUA_REGISTRYINDEX); // lco fin
                if (l.istable(-1))
                {
                    l.pushvalue(-2); // lco fin lco
                    l.pushnil(); // lco fin lco nil
                    l.settable(-3); // lco fin
                }
                l.pop(2); // X
            }
            else if (l.isfunction(2))
            { // clr.cofinally(co, function()end) -- set target co's finally
                if (l.isnil(1))
                {
                    l.pushthread(); // current thread
                }
                else
                {
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
                    var irv = ClrFuncGetLuaCoroutine(l);
                    if (irv <= 0)
                    {
                        return 0;
                    }
                    else
                    {
                        l.settop(argcnt + 1); // lco
                    }
#else
                    l.pushvalue(1); // lco
#endif
                }
                l.pushlightuserdata(LuaConst.LRKEY_CO_FINALLY); // lco #fin
                l.gettable(lua.LUA_REGISTRYINDEX); // lco fin
                if (!l.istable(-1))
                {
                    l.pop(1); // lco
                    l.newtable(); // lco fin
                    l.pushlightuserdata(LuaConst.LRKEY_CO_FINALLY); // lco fin #fin
                    l.pushvalue(-2); // lco fin #fin fin
                    l.settable(lua.LUA_REGISTRYINDEX); // lco fin
                    l.newtable(); // lco fin meta
                    l.PushString(LuaConst.LS_COMMON_K); // lco fin meta "k"
                    l.SetField(-2, LuaConst.LS_META_KEY_MODE); // lco fin meta
                    l.setmetatable(-2); // lco fin
                }
                l.pushvalue(-2); // lco fin lco
                l.pushvalue(2); // lco fin lco func
                l.settable(-3); // lco fin
                l.pop(2); // X
            }
            return 0;
        }
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncCoroutineContinue(IntPtr l)
        {
            var argcnt = l.gettop();
            if (argcnt <= 0)
            { // return clr.cocontinue() -- get current co's continue
                return l.GetContinuationFunc();
            }
            else if (argcnt == 1 && l.isnil(1) || argcnt >= 2 && l.isnil(1) && l.isnil(2))
            { // clr.cocontinue(nil) -- clear current co's continue
                l.pushnil();
                l.SetContinuationFunc(-1);
                l.pop(1);
                return 0;
            }
            else if (argcnt == 1 && l.isfunction(1))
            { // clr.cocontinue(function()end) -- set current co's continue
                l.SetContinuationFunc(1);
                return 0;
            }
            else if (argcnt == 1)
            { // return clr.cocontinue(co) -- get target co's continue
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
                var irv = ClrFuncGetLuaCoroutine(l);
                if (irv > 0)
#endif
                {
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
                    var lthd = l.tothread(2);
                    l.settop(1); // X
#else
                    var lthd = l;
#endif
                    var rv = lthd.GetContinuationFunc();
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
                    if (rv > 0)
                    {
                        if (lthd != l)
                        {
                            lthd.xmove(l, rv);
                        }
                    }
#endif
                    return rv;
                }
            }
            else if (l.isnil(2))
            { // clr.cocontinue(co, nil) -- clear target co's continue
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
                var irv = ClrFuncGetLuaCoroutine(l);
                if (irv <= 0)
                {
                    return 0;
                }
                var lthd = l.tothread(argcnt + 1);
                l.settop(argcnt); // X
#else
                var lthd = l;
#endif
                lthd.pushnil();
                lthd.SetContinuationFunc(-1);
                lthd.pop(1);
            }
            else if (l.isfunction(2))
            { // clr.cocontinue(co, function()end) -- set target co's continue
                if (l.isnil(1))
                {
                    // current thread
                }
                else
                {
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
                    var irv = ClrFuncGetLuaCoroutine(l);
                    if (irv <= 0)
                    {
                        return 0;
                    }
                    var lthd = l.tothread(argcnt + 1);
                    l.settop(argcnt); // X
                    if (lthd != l)
                    {
                        l.pushvalue(2);
                        l.xmove(lthd, 1);
                        lthd.SetContinuationFunc(-1);
                        lthd.pop(1);
                        return 0;
                    }
#endif
                }
                l.SetContinuationFunc(2);
            }
            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncPanic(IntPtr l)
        {
            var top = l.gettop();
            var error = l.GetLua(-1);
            string message = error == null ? "" : error.ToString();
            message = "Lua error at " + top + ": " + message;
            LuaHub.LogError(l, message);
            throw new LuaAtPanicException(message);
        }

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncReset(IntPtr l)
        {
            l.GetGlobal("package"); // package
            if (l.istable(-1))
            {
                {
                    l.GetField(-1, "loaders"); // package loaders
                    if (l.istable(-1))
                    {
                        l.GetField(-1, "apkloader"); // package loaders apkloader
                        if (l.isnoneornil(-1))
                        {
                            l.pop(1); // package loaders
                            l.pushcfunction(ClrDelApkLoader); // package loaders apkloader
                            l.pushvalue(-1); // package loaders apkloader apkloader
                            l.SetField(-3, "apkloader"); // package loaders apkloader 
                        }
                        l.pushnumber(1); // package loaders apkloader 1
                        l.gettable(-3); // package loaders apkloader 1stloader
                        if (l.equal(-1, -2))
                        {
                            l.pop(2); // package loaders
                        }
                        else
                        {
                            l.pop(1); // package loaders apkloader
                            var cnt = l.getn(-2);
                            for (int i = cnt; i >= 1; --i)
                            {
                                l.pushnumber(i + 1); // package loaders apkloader i+1
                                l.pushnumber(i); // package loaders apkloader i+1 i
                                l.gettable(-4); // package loaders apkloader i+1 func
                                l.settable(-4); // package loaders apkloader
                            }
                            l.pushnumber(1); // package loaders apkloader 1
                            l.insert(-2); // package loaders 1 apkloader
                            l.settable(-3); // package loaders
                        }
                    }
                    l.pop(1); // package
                }
            }
            l.pop(1); // X

            // reset updatepath
            l.GetGlobal("clr"); // clr
            if (l.istable(-1))
            {
                l.PushString(ThreadSafeValues.UpdatePath);
                l.SetField(-2, "updatepath");
                l.PushString(ThreadSafeValues.LogPath);
                l.SetField(-2, "logpath");
            }
            l.pop(1); // (empty)

            // res version
            l.pushnil();
            l.SetGlobal("___resver");

            // ex init
            for (int i = 0; i < FurtherInitFuncs.Count; ++i)
            {
                var init = FurtherInitFuncs[i];
                if (init != null)
                {
                    init(l);
                }
            }

            return 0;
        }

        [ThreadStatic] private static LuaFileManager.LuaStreamReader _LuaStreamReader;
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncApkLoader(IntPtr l)
        {
            string mname = l.GetString(1);
            if (!string.IsNullOrEmpty(mname))
            {
                string location;
                System.IO.Stream stream = null;
                GCHandle? handle = null;
                try
                {
                    stream = LuaFileManager.GetLuaStream(mname, out location);
                    if (stream != null)
                    {
                        if (_LuaStreamReader == null)
                        {
                            _LuaStreamReader = new LuaFileManager.LuaStreamReader(null);
                        }
                        _LuaStreamReader.Reuse(stream, PlatDependant.CopyStreamBuffer);
                        handle = GCHandle.Alloc(_LuaStreamReader);
                        //location = string.Format("@{0}:{1}", mname, location);
                        location = "@" + location;
                        if (l.load(LuaFileManager.LuaStreamReader.ReaderDel, (IntPtr)handle.Value, location) == 0)
                        {
                            return 1;
                        }
                        else
                        {
                            DynamicHelper.LogError(l.GetLua(-1));
                            l.pop(1);
                            return 0;
                        }
                    }
                }
                catch (Exception e)
                {
                    l.LogError(e);
                }
                finally
                {
                    if (stream != null)
                    {
                        stream.Dispose();
                    }
                    if (handle != null)
                    {
                        handle.Value.Free();
                    }
                }
            }
            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncGetMeID(IntPtr l)
        {
            var meID = ThreadSafeValues.Meid;
            l.PushString(meID);
            return 1;
        }
        private static void SplitStr(IntPtr l, string str)
        {
            l.newtable();
            int index = 0;
            for (int i = 0; i < str.Length; ++i)
            {
                l.pushnumber(++index);
                System.Text.StringBuilder pstr = new System.Text.StringBuilder();
                var ch = str[i];
                pstr.Append(ch);
                if (ch >= 0xD800 && ch <= 0xDFFF)
                {
                    if (++i < str.Length)
                    {
                        pstr.Append(str[i]);
                    }
                }
                l.PushString(pstr.ToString());
                l.settable(-3);
            }
        }
        private static void SplitStr(IntPtr l, System.Text.StringBuilder str)
        {
            l.newtable();
            int index = 0;
            for (int i = 0; i < str.Length; ++i)
            {
                l.pushnumber(++index);
                System.Text.StringBuilder pstr = new System.Text.StringBuilder();
                var ch = str[i];
                pstr.Append(ch);
                if (ch >= 0xD800 && ch <= 0xDFFF)
                {
                    if (++i < str.Length)
                    {
                        pstr.Append(str[i]);
                    }
                }
                l.PushString(pstr.ToString());
                l.settable(-3);
            }
        }
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncSplitStr(IntPtr l)
        {
            if (l.isstring(1))
            {
                SplitStr(l, l.GetString(1));
            }
            else if (l.IsObject(1))
            {
                var inputStr = l.GetLua(1);
                if (inputStr is string)
                {
                    SplitStr(l, inputStr.ToString());
                }
                else if (inputStr is System.Text.StringBuilder)
                {
                    SplitStr(l, (System.Text.StringBuilder)inputStr);
                }
                else
                {
                    return 0;
                }
            }
            return 1;
        }
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncFormatDataString(IntPtr l)
        {
            if (l.isstring(1))
            {
                var buffer = l.tolstring(1);
                l.PushString(PlatDependant.FormatDataString(buffer));
                return 1;
            }
            else if (l.IsObject(1))
            {
                var obj = l.GetLua(1);
                if (obj is byte[])
                {
                    var buffer = (byte[])obj;
                    l.PushString(PlatDependant.FormatDataString(buffer));
                    return 1;
                }
                else if (obj is IList<byte>)
                {
                    var buffer = (IList<byte>)obj;
                    l.PushString(PlatDependant.FormatDataString(buffer));
                    return 1;
                }
            }
            return 0;
        }
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncFormatLuaString(IntPtr l)
        {
            byte[] data;
            l.GetLua(1, out data);
            if (data == null)
            {
                return 0;
            }
            l.PushLua(LuaString.FormatLuaString(data));
            return 1;
        }
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncFormatJsonString(IntPtr l)
        {
            byte[] data;
            l.GetLua(1, out data);
            if (data == null)
            {
                return 0;
            }
            l.PushLua(PlatDependant.FormatJsonString(data));
            return 1;
        }
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncCurrentLua(IntPtr l)
        {
            l.pushthread();
            return 1;
        }
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncGetLuaRegistry(IntPtr l)
        {
            l.pushvalue(lua.LUA_REGISTRYINDEX);
            return 1;
        }
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncToPointer(IntPtr l)
        {
            if (l.gettop() == 0)
            {
                l.pushlightuserdata(l);
                return 1;
            }
            else
            {
                if (l.isnumber(1))
                {
                    var p = new IntPtr((long)l.tonumber(1));
                    l.pushlightuserdata(p);
                    return 1;
                }
                else
                {
                    var p = l.topointer(1);
                    l.pushlightuserdata(p);
                    return 1;
                }
            }
        }
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncNewUserdata(IntPtr l)
        {
            int size = 0;
            var argcnt = l.gettop();
            bool firstArgIsSize;
            if (firstArgIsSize = argcnt > 0 && l.isnumber(1))
            {
                size = (int)l.tonumber(1);
            }
            if (size < 0)
            {
                size = 0;
            }
            l.newuserdata((IntPtr)size);

            if (firstArgIsSize)
            {
                if (argcnt > 1 && l.istable(2))
                {
                    l.pushvalue(2);
                    l.setmetatable(-2);
                }
                if (argcnt > 2 && l.istable(3))
                {
                    l.pushvalue(3);
                    l.setfenv(-2);
                }
            }
            else
            {
                if (argcnt > 0 && l.istable(1))
                {
                    l.pushvalue(1);
                    l.setmetatable(-2);
                }
                if (argcnt > 1 && l.istable(2))
                {
                    l.pushvalue(2);
                    l.setfenv(-2);
                }
            }
            return 1;
        }
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncUserdataInfo(IntPtr l)
        {
            if (l.type(1) != lua.LUA_TUSERDATA)
            {
                return 0;
            }
            l.pushnumber(l.getn(1));
            if (!l.getmetatable(1))
            {
                l.pushnil();
            }
            l.getfenv(1);
            return 3;
        }
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncRandomState(IntPtr l)
        {
            int type;
            IntPtr ps = IntPtr.Zero;
            using (var lr = l.CreateStackRecover())
            {
                l.pushcfunction(LuaHub.LuaFuncOnError); // err
                l.GetGlobal("debug"); // TODO: import lua_getupvalue
                l.GetField(-1, "getupvalue");
                l.remove(-2); // err getupvalue
                l.GetGlobal("math");
                l.GetField(-1, "random");
                l.GetField(-2, "randomex");
                if (l.isfunction(-1))
                {
                    type = 2;
                }
                else
                {
                    type = 1;
                }
                l.pop(1);
                l.remove(-2); // err getupvalue random
                l.pushnumber(1); // err getupvalue ramdom 1
                l.pcall(2, 2, -4);
                if (!l.isuserdata(-1))
                {
                    type = 0;
                    return 0;
                }
                else
                {
                    ps = l.touserdata(-1);
                }
            }

            if (l.gettop() == 0)
            { // get random state
                l.newtable();
                if (type == 1)
                {
                    var v0l = (uint)Marshal.ReadInt32(ps);
                    l.pushnumber(v0l);
                    l.SetField(-2, "v0l");
                    var v0h = (uint)Marshal.ReadInt32(ps, 4);
                    l.pushnumber(v0h);
                    l.SetField(-2, "v0h");
                    var v1l = (uint)Marshal.ReadInt32(ps, 8);
                    l.pushnumber(v1l);
                    l.SetField(-2, "v1l");
                    var v1h = (uint)Marshal.ReadInt32(ps, 12);
                    l.pushnumber(v1h);
                    l.SetField(-2, "v1h");
                    var v2l = (uint)Marshal.ReadInt32(ps, 16);
                    l.pushnumber(v2l);
                    l.SetField(-2, "v2l");
                    var v2h = (uint)Marshal.ReadInt32(ps, 20);
                    l.pushnumber(v2h);
                    l.SetField(-2, "v2h");
                    var v3l = (uint)Marshal.ReadInt32(ps, 24);
                    l.pushnumber(v3l);
                    l.SetField(-2, "v3l");
                    var v3h = (uint)Marshal.ReadInt32(ps, 28);
                    l.pushnumber(v3h);
                    l.SetField(-2, "v3h");
                    return 1;
                }
                else
                {
                    var x0 = (ushort)(uint)Marshal.ReadInt32(ps);
                    l.pushnumber(x0);
                    l.SetField(-2, "x0");
                    var x1 = (ushort)(uint)Marshal.ReadInt32(ps, 4);
                    l.pushnumber(x1);
                    l.SetField(-2, "x1");
                    var x2 = (ushort)(uint)Marshal.ReadInt32(ps, 8);
                    l.pushnumber(x2);
                    l.SetField(-2, "x2");
                    var a0 = (uint)Marshal.ReadInt32(ps, 12);
                    l.pushnumber(a0);
                    l.SetField(-2, "a0");
                    var a1 = (uint)Marshal.ReadInt32(ps, 16);
                    l.pushnumber(a1);
                    l.SetField(-2, "a1");
                    var a2 = (uint)Marshal.ReadInt32(ps, 20);
                    l.pushnumber(a2);
                    l.SetField(-2, "a2");
                    var c = (uint)Marshal.ReadInt32(ps, 24);
                    l.pushnumber(c);
                    l.SetField(-2, "c");

                    long unite = x2;
                    unite <<= 16;
                    unite += x1;
                    unite <<= 16;
                    unite += x0;

                    l.pushnumber(unite);
                    return 2;
                }
            }
            else if (l.istable(1))
            { // set random state
                if (type == 1)
                {
                    l.GetField(1, "v0l");
                    var v0l = (uint)l.tonumber(-1);
                    Marshal.WriteInt32(ps, (int)v0l);
                    l.pop(1);
                    l.GetField(1, "v0h");
                    var v0h = (uint)l.tonumber(-1);
                    Marshal.WriteInt32(ps, 4, (int)v0h);
                    l.pop(1);
                    l.GetField(1, "v1l");
                    var v1l = (uint)l.tonumber(-1);
                    Marshal.WriteInt32(ps, 8, (int)v1l);
                    l.pop(1);
                    l.GetField(1, "v1h");
                    var v1h = (uint)l.tonumber(-1);
                    Marshal.WriteInt32(ps, 12, (int)v1h);
                    l.pop(1);
                    l.GetField(1, "v2l");
                    var v2l = (uint)l.tonumber(-1);
                    Marshal.WriteInt32(ps, 16, (int)v2l);
                    l.pop(1);
                    l.GetField(1, "v2h");
                    var v2h = (uint)l.tonumber(-1);
                    Marshal.WriteInt32(ps, 20, (int)v2h);
                    l.pop(1);
                    l.GetField(1, "v3l");
                    var v3l = (uint)l.tonumber(-1);
                    Marshal.WriteInt32(ps, 24, (int)v3l);
                    l.pop(1);
                    l.GetField(1, "v3h");
                    var v3h = (uint)l.tonumber(-1);
                    Marshal.WriteInt32(ps, 28, (int)v3h);
                    l.pop(1);
                }
                else
                {
                    l.GetField(1, "x0");
                    var x0 = (uint)l.tonumber(-1);
                    Marshal.WriteInt32(ps, (int)x0);
                    l.pop(1);
                    l.GetField(1, "x1");
                    var x1 = (uint)l.tonumber(-1);
                    Marshal.WriteInt32(ps, 4, (int)x1);
                    l.pop(1);
                    l.GetField(1, "x2");
                    var x2 = (uint)l.tonumber(-1);
                    Marshal.WriteInt32(ps, 8, (int)x2);
                    l.pop(1);
                    l.GetField(1, "a0");
                    var a0 = (uint)l.tonumber(-1);
                    Marshal.WriteInt32(ps, 12, (int)a0);
                    l.pop(1);
                    l.GetField(1, "a1");
                    var a1 = (uint)l.tonumber(-1);
                    Marshal.WriteInt32(ps, 16, (int)a1);
                    l.pop(1);
                    l.GetField(1, "a2");
                    var a2 = (uint)l.tonumber(-1);
                    Marshal.WriteInt32(ps, 20, (int)a2);
                    l.pop(1);
                    l.GetField(1, "c");
                    var c = (uint)l.tonumber(-1);
                    Marshal.WriteInt32(ps, 24, (int)c);
                    l.pop(1);
                }
                return 0;
            }
            else if (l.isnumber(1) && type == 2)
            {
                long unite = (long)l.tonumber(1);
                uint x0 = (uint)(unite & 0xFFFFL);
                Marshal.WriteInt32(ps, (int)x0);
                uint x1 = (uint)((unite & (0xFFFFL << 16)) >> 16);
                Marshal.WriteInt32(ps, 4, (int)x1);
                uint x2 = (uint)((unite & (0xFFFFL << 32)) >> 32);
                Marshal.WriteInt32(ps, 8, (int)x2);
                return 0;
            }
            else
            { // don't know what to do.
                return 0;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct LuaRandomState
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct LuaRandomStateEmpty
            {
            }
            [StructLayout(LayoutKind.Sequential)]
            public struct LuaRandomStateLuaJit
            {
                public ulong v0;
                public ulong v1;
                public ulong v2;
                public ulong v3;
            }
            [StructLayout(LayoutKind.Sequential)]
            public struct LuaRandomStateLuaJitSplit
            {
                public uint v0l;
                public uint v0h;
                public uint v1l;
                public uint v1h;
                public uint v2l;
                public uint v2h;
                public uint v3l;
                public uint v3h;
            }
            [StructLayout(LayoutKind.Sequential)]
            public struct LuaRandomStateEx
            {
                public ushort x0;
                public ushort x1;
                public ushort x2;
                private ushort xreserved;
                public uint a0;
                public uint a1;
                public uint a2;
                public uint c;
            }

            [FieldOffset(0)]
            public LuaRandomStateEmpty Empty;
            [FieldOffset(0)]
            public LuaRandomStateLuaJit LuaJit;
            [FieldOffset(0)]
            public LuaRandomStateLuaJitSplit LuaJitSplit;
            [FieldOffset(0)]
            public LuaRandomStateEx Ex;
            [FieldOffset(0)]
            public long ExUnite;

            [FieldOffset(32)]
            public byte Type; // 0 - Empty; 1 - LuaJit; 2 - Ex;
        }
        public static LuaRandomState GetRandomState(IntPtr l)
        {
            LuaRandomState state = new LuaRandomState();
            using (var lr = l.CreateStackRecover())
            {
                l.pushcfunction(LuaHub.LuaFuncOnError); // err
                l.GetGlobal("debug"); // TODO: import lua_getupvalue
                l.GetField(-1, "getupvalue");
                l.remove(-2); // err getupvalue
                l.GetGlobal("math");
                l.GetField(-1, "random");
                l.GetField(-2, "randomex");
                if (l.isfunction(-1))
                {
                    state.Type = 2;
                }
                else
                {
                    state.Type = 1;
                }
                l.pop(1);
                l.remove(-2); // err getupvalue random
                l.pushnumber(1); // err getupvalue random 1
                l.pcall(2, 2, -4);
                if (!l.isuserdata(-1))
                {
                    state.Type = 0;
                }
                else
                {
                    IntPtr ps = l.touserdata(-1);
                    if (state.Type == 1)
                    {
                        state.LuaJit.v0 = (ulong)Marshal.ReadInt64(ps);
                        state.LuaJit.v1 = (ulong)Marshal.ReadInt64(ps, 8);
                        state.LuaJit.v2 = (ulong)Marshal.ReadInt64(ps, 16);
                        state.LuaJit.v3 = (ulong)Marshal.ReadInt64(ps, 24);
                    }
                    else
                    {
                        state.Ex.x0 = (ushort)(uint)Marshal.ReadInt32(ps);
                        state.Ex.x1 = (ushort)(uint)Marshal.ReadInt32(ps, 4);
                        state.Ex.x2 = (ushort)(uint)Marshal.ReadInt32(ps, 8);
                        state.Ex.a0 = (uint)Marshal.ReadInt32(ps, 12);
                        state.Ex.a1 = (uint)Marshal.ReadInt32(ps, 16);
                        state.Ex.a2 = (uint)Marshal.ReadInt32(ps, 20);
                        state.Ex.c = (uint)Marshal.ReadInt32(ps, 24);
                    }
                }
            }
            return state;
        }
        public static void SetRandomState(IntPtr l, LuaRandomState state)
        {
            if (state.Type == 0)
            {
                return;
            }
            using (var lr = l.CreateStackRecover())
            {
                l.pushcfunction(LuaHub.LuaFuncOnError); // err
                l.GetGlobal("debug"); // TODO: import lua_getupvalue
                l.GetField(-1, "getupvalue");
                l.remove(-2); // err getupvalue
                l.GetGlobal("math");
                l.GetField(-1, "random");
                l.remove(-2); // err getupvalue random
                l.pushnumber(1); // err getupvalue random 1
                l.pcall(2, 2, -4);
                if (!l.isuserdata(-1))
                {
                    return;
                }
                else
                {
                    IntPtr ps = l.touserdata(-1);
                    if (state.Type == 1)
                    {
                        Marshal.WriteInt64(ps, (long)state.LuaJit.v0);
                        Marshal.WriteInt64(ps, 8, (long)state.LuaJit.v1);
                        Marshal.WriteInt64(ps, 16, (long)state.LuaJit.v2);
                        Marshal.WriteInt64(ps, 24, (long)state.LuaJit.v3);
                    }
                    else if (state.Type == 2)
                    {
                        Marshal.WriteInt32(ps, (int)(uint)state.Ex.x0);
                        Marshal.WriteInt32(ps, 4, (int)(uint)state.Ex.x1);
                        Marshal.WriteInt32(ps, 8, (int)(uint)state.Ex.x2);
                        Marshal.WriteInt32(ps, 12, (int)state.Ex.a0);
                        Marshal.WriteInt32(ps, 16, (int)state.Ex.a1);
                        Marshal.WriteInt32(ps, 20, (int)state.Ex.a2);
                        Marshal.WriteInt32(ps, 24, (int)state.Ex.c);
                    }
                }
            }
        }

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncFormat(IntPtr l)
        {
            var top = l.gettop();
            if (top <= 0)
            {
                return 0;
            }
            if (top == 1)
            {
                l.pushvalue(1);
                return 1;
            }
            else
            {
                var args = new object[top - 1];
                for (int i = 0; i < args.Length; ++i)
                {
                    args[i] = l.GetLua(2 + i);
                }
                var format = l.GetString(1);
                string result = null;
                try
                {
                    result = string.Format(format, args);
                }
                catch (Exception e)
                {
                    l.LogError(e);
                    l.pushvalue(1);
                    return 1;
                }
                l.PushString(result);
                return 1;
            }
        }

        private static int ClrFuncGetLangValue(IntPtr l, bool isStringType)
        {
            var oldtop = l.gettop();
            if (l.istable(1))
            {
                l.pushnumber(1); // 1
                l.gettable(1); // key
                if (l.IsString(-1))
                {
                    string key = l.GetString(-1);
                    l.pop(1); // X
                    var len = l.getn(1);
                    object[] args = new object[len - 1];
                    for (int i = 2; i <= len; ++i)
                    {
                        l.pushnumber(i);
                        l.gettable(1);
                        args[i - 2] = l.GetLua(-1);
                        l.pop(1);
                    }

                    string val = LanguageConverter.GetLangValue(key, args);

                    if (isStringType)
                    {
                        l.pushstring(val);
                    }
                    else
                    {
                        l.PushLuaObject(val);
                    }
                    return 1;
                }
                else
                {
                    l.settop(oldtop);
                    return 0;
                }
            }
            else
            {
                if (l.IsString(1))
                {
                    string key = l.GetString(1);
                    object[] args = new object[oldtop - 1];
                    for (int i = 2; i <= oldtop; ++i)
                    {
                        args[i - 2] = l.GetLua(i);
                    }

                    string val = LanguageConverter.GetLangValue(key, args);

                    if (isStringType)
                    {
                        l.pushstring(val);
                    }
                    else
                    {
                        l.PushLuaObject(val);
                    }
                    return 1;
                }
                else
                {
                    l.settop(oldtop);
                    return 0;
                }
            }
        }
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncGetLangValueOfUserDataType(IntPtr l)
        {
            return ClrFuncGetLangValue(l, false);
        }
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncGetLangValueOfStringType(IntPtr l)
        {
            return ClrFuncGetLangValue(l, true);
        }
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int UpdateLanguageConverter(IntPtr l)
        {
            if (l.istable(1))
            {
                Dictionary<string, string> updatedMap = new Dictionary<string, string>();
                l.pushnil();
                while (l.next(1))
                {
                    string key, val;
                    l.GetLua(-2, out key);
                    l.GetLua(-1, out val);
                    if (key != null)
                    {
                        updatedMap[key] = val;
                    }
                    l.pop(1);
                }
                LanguageConverter.UpdateDict(updatedMap);
            }
            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncGetExtendedHash(IntPtr l)
        {
            if (l.IsString(1))
            {
                var extab = l.TryRequire("data.exhash");
                if (extab.IsValid)
                { // extab
                    l.pushvalue(1); // extab str
                    l.gettable(-2); // extab hash
                    if (l.IsNumber(-1))
                    {
                        l.remove(-2); // hash
                        return 1;
                    }
                    l.pop(2); // X
                }
                var argcnt = l.gettop();
                var str = l.GetString(1);
                ushort headOffset = 0;
                ushort tailOffset = 0;
                int criticalPos = 0;
                byte exflag = 0;
                if (argcnt >= 2)
                {
                    l.GetLua(2, out headOffset);
                    if (argcnt >= 3)
                    {
                        l.GetLua(3, out tailOffset);
                        if (argcnt >= 4)
                        {
                            l.GetLua(4, out criticalPos);
                            if (argcnt >= 5)
                            {
                                l.GetLua(5, out exflag);
                            }
                        }
                    }
                }
                var hash = ExtendedStringHash.GetHashCodeEx(str, headOffset, tailOffset, criticalPos, exflag);
                l.PushLua(hash);
                return 1;
            }
            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncGetLastDecimal(IntPtr l)
        {
            var hub = LuaTypeHub.GetTypeHub(typeof(decimal));
            hub.PushLuaCommon(l, LuaHub.LuaPushNativeLongNumberCache.DecimalCache);
            return 1;
        }
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncGetLastUInt64(IntPtr l)
        {
            var hub = LuaTypeHub.GetTypeHub(typeof(ulong));
            hub.PushLuaCommon(l, LuaHub.LuaPushNativeLongNumberCache.Int64Cache);
            return 1;
        }
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncGetLastInt64(IntPtr l)
        {
            var hub = LuaTypeHub.GetTypeHub(typeof(long));
            hub.PushLuaCommon(l, (long)LuaHub.LuaPushNativeLongNumberCache.Int64Cache);
            return 1;
        }
    }

    public class LuaAtPanicException : Exception
    {
        public LuaAtPanicException(string message)
            : base(message)
        {
        }
    }
}