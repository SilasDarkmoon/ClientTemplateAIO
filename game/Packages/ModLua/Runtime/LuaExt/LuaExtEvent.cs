using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using LuaLib;
using UnityEngineEx;

using lua = LuaLib.LuaCoreLib;
using lual = LuaLib.LuaAuxLib;
using luae = LuaLib.LuaLibEx;

namespace LuaLib
{
    public static partial class LuaExLibs
    {
        private static LuaExLibItem _LuaExLib_Event_Instance = new LuaExLibItem(LuaEvent.Init, 100);
    }

    public static class LuaEvent
    {
        public static void Init(IntPtr l)
        {
            l.newtable(); // luaevt
            l.pushvalue(-1); // luaevt luaevt
            l.SetGlobal("luaevt"); // luaevt
            l.pushcfunction(Func_TrigLuaEvent); // luaevt func
            l.SetField(-2, "trig"); // luaevt
            l.pushcfunction(Func_SetEventParamNames); // luaevt func
            l.SetField(-2, "names"); // luaevt
            l.pushcfunction(Func_RegLuaEventHandler); // luaevt func
            l.SetField(-2, "reg"); // luaevt
            l.pushcfunction(Func_UnregLuaEventHandler); // luaevt func
            l.SetField(-2, "unreg"); // luaevt
            l.pushcfunction(Func_SetHandlerOrder); // luaevt func
            l.SetField(-2, "order"); // luaevt
            l.pushcfunction(Func_ResetLuaEventReg); // luaevt func
            l.SetField(-2, "reset"); // luaevt
            l.pushcfunction(Func_LuaDelayedEvents); // luaevt func
            l.SetField(-2, "delayed"); // luaevt
            l.pushcfunction(Func_HandlerCount); // luaevt func
            l.SetField(-2, "count"); // luaevt
            l.pushcfunction(Func_MarkRaw); // luaevt func
            l.SetField(-2, "raw"); // luaevt
            l.pop(1);
        }

        internal static readonly lua.CFunction Func_TrigLuaEvent = new lua.CFunction(TrigLuaEvent);
        internal static readonly lua.CFunction Func_SetEventParamNames = new lua.CFunction(SetEventParamNames);
        internal static readonly lua.CFunction Func_RegLuaEventHandler = new lua.CFunction(RegLuaEventHandler);
        internal static readonly lua.CFunction Func_UnregLuaEventHandler = new lua.CFunction(UnregLuaEventHandler);
        internal static readonly lua.CFunction Func_SetHandlerOrder = new lua.CFunction(SetHandlerOrder);
        internal static readonly lua.CFunction Func_ResetLuaEventReg = new lua.CFunction(ResetLuaEventReg);
        internal static readonly lua.CFunction Func_LuaDelayedEvents = new lua.CFunction(LuaDelayedEvents);
        internal static readonly lua.CFunction Func_HandlerCount = new lua.CFunction(LuaFuncHandlerCount);
        internal static readonly lua.CFunction Func_MarkRaw = new lua.CFunction(LuaFuncMarkTableAsRaw);

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int TrigLuaEvent(IntPtr l)
        {
            if (l != IntPtr.Zero)
            {
                var cate = l.GetLua<string>(1);
                if (cate == null)
                {
                    return 0;
                }
                int token = CrossEvent.TOKEN_CALL;
                var top = l.gettop();
                CrossEvent.SetParamCount(token, Math.Max(0, top - 1));
                for (int i = 2; i <= top; ++i)
                {
                    if (l.IsCrossEventDataTable(i))
                    {
                        CrossEvent.ContextExchangeObj = GetParams(l, i);
                    }
                    else
                    {
                        CrossEvent.ContextExchangeObj = l.GetLua(i);
                    }
                    CrossEvent.SetParam(token, i - 2);
                }
                CrossEvent.TrigEvent(cate);
                int rvcnt = CrossEvent.GetParamCount(token);
                for (int i = 0; i < rvcnt; ++i)
                {
                    CrossEvent.GetParam(token, i);
                    if (CrossEvent.ContextExchangeObj is List<CrossEvent.EventParam>)
                    {
                        PushParams(l, CrossEvent.ContextExchangeObj as List<CrossEvent.EventParam>);
                    }
                    else
                    {
                        l.PushLua(CrossEvent.ContextExchangeObj);
                    }
                }
                return l.gettop() - top;
            }
            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int SetEventParamNames(IntPtr l)
        {
            if (l != IntPtr.Zero)
            {
                int token = CrossEvent.TOKEN_CALL;
                var top = l.gettop();
                CrossEvent.SetParamCount(token, top);
                for (int i = 1; i <= top; ++i)
                {
                    var name = l.GetLua<string>(i);
                    CrossEvent.SetValStr(name);
                    CrossEvent.SetParamName(token, i - 1);
                }
            }
            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int RegLuaEventHandler(IntPtr l)
        {
            if (l != IntPtr.Zero)
            {
                int order = 0;
                if (l.gettop() >= 3)
                {
                    order = l.GetLua<int>(3);
                }
                var cate = l.GetLua<string>(1);
                if (cate != null)
                {
                    var refid = CrossEvent.RegHandler(cate, (CrossEvent.ICEventHandler)null);
                    if (order != 0)
                    {
                        CrossEvent.SetHandlerOrder(cate, refid, order);
                    }
                    using (var lr = new LuaStateRecover(l))
                    {
                        l.GetField(lua.LUA_REGISTRYINDEX, "___levt");
                        if (!l.istable(-1))
                        {
                            l.pop(1);
                            l.newtable();
                            l.pushvalue(-1);
                            l.SetField(lua.LUA_REGISTRYINDEX, "___levt");
                        }

                        l.pushvalue(1); // levt cate
                        l.gettable(-2); // levt cate list
                        if (!l.istable(-1))
                        {
                            l.pop(1); // levt cate
                            l.newtable(); // levt cate list
                            l.pushvalue(1); // levt cate list cate
                            l.pushvalue(-2); // levt cate list cate list
                            l.settable(-4); // levt cate list
                        }
                        l.pushnumber(refid); // levt cate list refid
                        l.pushvalue(2); // levt cate list refid handler
                        l.settable(-3); //  levt cate list
                    }
                    l.pushnumber(refid);
                    return 1;
                }
            }
            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int UnregLuaEventHandler(IntPtr l)
        {
            if (l != IntPtr.Zero)
            {
                var cate = l.GetLua<string>(1);
                if (cate != null)
                {
                    var refid = l.GetLua<int>(2);
                    CrossEvent.UnregHandler(cate, refid);
                }
            }
            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int SetHandlerOrder(IntPtr l)
        {
            if (l != IntPtr.Zero)
            {
                var cate = l.GetLua<string>(1);
                if (cate != null)
                {
                    int refid = l.GetLua<int>(2);
                    int order = l.GetLua<int>(3);
                    CrossEvent.SetHandlerOrder(cate, refid, order);
                }
            }
            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ResetLuaEventReg(IntPtr l)
        {
            CrossEvent.ResetCrossEvent();
            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int LuaDelayedEvents(IntPtr l)
        {
            // this is no need now...
            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int LuaFuncHandlerCount(IntPtr l)
        {
            if (l.IsString(1))
            {
                var cate = l.GetString(1);
                l.pushnumber(CrossEvent.GetHandlerCount(cate));
            }
            else if (l.isnoneornil(1))
            {
                l.pushnumber(CrossEvent.GetHandlerCount(null));
            }
            else
            {
                l.pushnumber(0);
            }
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int LuaFuncMarkTableAsRaw(IntPtr l)
        {
            MarkTableAsRaw(l, 1);
            l.pushvalue(1);
            return 1;
        }

        internal static void MarkTableAsRaw(this IntPtr l, int index)
        {
            if (l.IsTable(index))
            {
                if (l.getmetatable(index))
                {
                    MarkTableAsRaw(l, -1);
                    l.pop(1);
                }
                else
                {
                    var absindex = l.NormalizeIndex(index); // (tab ...)
                    l.newtable(); // (tab ...) meta
                    l.pushboolean(true); // (tab ...) meta true
                    l.SetField(-2, "__luaevt_luaonly"); // (tab ...) meta
                    l.setmetatable(absindex); // (tab ...)
                }
            }
        }

        internal static bool IsCrossEventDataTable(this IntPtr l, int index)
        {
            if (!l.IsTable(index))
            {
                return false;
            }
            if (l.getmetatable(index))
            { // meta
                l.GetField(-1, "__isobject"); // meta __isobject
                if (!l.isnoneornil(-1) && (!l.isboolean(-1) || l.toboolean(-1)))
                {
                    l.pop(2);
                    return false;
                }
                l.pop(1); // meta
                l.GetField(-1, "__luaevt_luaonly");
                if (!l.isnoneornil(-1) && (!l.isboolean(-1) || l.toboolean(-1)))
                {
                    l.pop(2);
                    return false;
                }
                l.pop(1);
                var isMetaCrossEventDataTable = IsCrossEventDataTable(l, -1);
                l.pop(1);
                if (!isMetaCrossEventDataTable)
                {
                    return isMetaCrossEventDataTable;
                }
            }
            // TODO: get a field on the table itself?
            return true;
        }

        public static List<CrossEvent.EventParam> GetParams(IntPtr l, int index)
        {
            if (l.IsTable(index))
            {
                List<CrossEvent.EventParam> rvs = new List<CrossEvent.EventParam>();
                l.pushvalue(index); // tab
                int cnt = l.getn(-1);
                if (cnt > 0)
                {
                    // this is an array
                    for (int i = 1; i <= cnt; ++i)
                    {
                        l.pushnumber(i); // tab key
                        l.gettable(-2); // tab val
                        object val;
                        if (l.IsCrossEventDataTable(-1))
                        {
                            val = GetParams(l, -1);
                        }
                        else
                        {
                            val = l.GetLua(-1);
                        }
                        rvs.Add(new CrossEvent.EventParam() { Value = val });
                        l.pop(1); // tab;
                    }
                }
                else
                {
                    // this is a dictionary
                    l.pushnil();
                    while (l.next(-2))
                    {
                        string key = l.GetLua<string>(-2);
                        object val;
                        if (l.IsCrossEventDataTable(-1))
                        {
                            val = GetParams(l, -1);
                        }
                        else
                        {
                            val = l.GetLua(-1);
                        }
                        rvs.Add(new CrossEvent.EventParam() { Value = val, Name = key });
                        l.pop(1);
                    }
                }
                l.pop(1);
                return rvs;
            }
            return null;
        }
        public static void PushParams(IntPtr l, List<CrossEvent.EventParam> values)
        {
            if (values == null)
            {
                l.pushnil();
            }
            else
            {
                l.newtable(); // tab
                for (int i = 0; i < values.Count; ++i)
                {
                    var par = values[i];
                    if (par.Name == null)
                    {
                        l.pushnumber(i + 1);
                    }
                    else
                    {
                        l.PushString(par.Name);
                    }

                    var val = par.Value;
                    if (val is List<CrossEvent.EventParam>)
                    {
                        PushParams(l, val as List<CrossEvent.EventParam>);
                    }
                    else
                    {
                        l.PushLua(val);
                    }

                    l.settable(-3);
                }
            }
        }

        private class CrossEventEx_Lua : CrossEvent.ICrossEventEx
        {
            public object GetGlobal(string name)
            {
                if (ThreadSafeValues.IsMainThread)
                {
                    var l = GlobalLua.L.L;
                    if (l.GetHierarchicalRaw(lua.LUA_GLOBALSINDEX, name))
                    {
                        object rv = null;
                        if (l.IsCrossEventDataTable(-1))
                        {
                            rv = GetParams(l, -1);
                        }
                        else
                        {
                            rv = l.GetLua(-1);
                        }
                        l.pop(1);
                        return rv;
                    }
                }
                return null;
            }

            public void HandleEvent(string cate, int refid)
            {
                if (ThreadSafeValues.IsMainThread)
                {
                    var pars = CrossEvent.CurrentContext._P[CrossEvent.TOKEN_ARGS];
                    var rvs = CrossEvent.CurrentContext._P[CrossEvent.TOKEN_RETS];
                    var l = GlobalLua.L.L;
                    using (var lr = new LuaStateRecover(l))
                    {
                        l.GetField(lua.LUA_REGISTRYINDEX, "___levt");
                        if (l.istable(-1))
                        {
                            l.PushString(cate);
                            l.gettable(-2);
                            if (l.istable(-1))
                            {
                                l.pushnumber(refid);
                                l.gettable(-2);
                                if (l.isfunction(-1))
                                {
                                    var oldtop = l.gettop();
                                    l.PushString(cate);
                                    for (int i = 0; i < pars.Count; ++i)
                                    {
                                        var val = pars[i].Value;
                                        if (val is List<CrossEvent.EventParam>)
                                        {
                                            PushParams(l, val as List<CrossEvent.EventParam>);
                                        }
                                        else
                                        {
                                            l.PushLua(val);
                                        }
                                    }
                                    var code = LuaFuncHelper.CallInternal(l, oldtop);
                                    if (code == 0)
                                    {
                                        var newtop = l.gettop();
                                        int rvsi = 0;
                                        for (int i = oldtop; i <= newtop; ++i)
                                        {
                                            var param = new CrossEvent.EventParam();
                                            if (l.IsCrossEventDataTable(i))
                                            {
                                                param.Value = GetParams(l, i);
                                            }
                                            else
                                            {
                                                param.Value = l.GetLua(i);
                                            }
                                            if (rvsi == rvs.Count)
                                            {
                                                rvs.Add(param);
                                            }
                                            else
                                            {
                                                rvs[rvsi] = param;
                                            }
                                            ++rvsi;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    var pars = CrossEvent.CurrentContext._P[CrossEvent.TOKEN_ARGS];
                    UnityThreadDispatcher.RunInUnityThread(() =>
                    {
                        var l = GlobalLua.L.L;
                        using (var lr = new LuaStateRecover(l))
                        {
                            l.GetField(lua.LUA_REGISTRYINDEX, "___levt");
                            if (l.istable(-1))
                            {
                                l.PushString(cate);
                                l.gettable(-2);
                                if (l.istable(-1))
                                {
                                    l.pushnumber(refid);
                                    l.gettable(-2);
                                    if (l.isfunction(-1))
                                    {
                                        var oldtop = l.gettop();
                                        l.PushString(cate);
                                        for (int i = 0; i < pars.Count; ++i)
                                        {
                                            var val = pars[i].Value;
                                            if (val is List<CrossEvent.EventParam>)
                                            {
                                                PushParams(l, val as List<CrossEvent.EventParam>);
                                            }
                                            else
                                            {
                                                l.PushLua(val);
                                            }
                                        }
                                        LuaFuncHelper.CallInternal(l, oldtop);
                                    }
                                }
                            }
                        }
                    });
                }
            }

            public void Reset()
            {
                if (ThreadSafeValues.IsMainThread)
                {
                    ResetRaw();
                }
                else
                {
                    UnityThreadDispatcher.RunInUnityThread(ResetRaw);
                }
            }
            private static void ResetRaw()
            {
                var l = GlobalLua.L.L;
                using (var lr = new LuaStateRecover(l))
                {
                    l.newtable();
                    l.SetField(lua.LUA_REGISTRYINDEX, "___levt");
                }
            }

            public void SetGlobal(string name, object val)
            {
                if (ThreadSafeValues.IsMainThread)
                {
                    SetGlobalRaw(name, val);
                }
                else
                {
                    UnityThreadDispatcher.RunInUnityThread(() =>
                    {
                        SetGlobalRaw(name, val);
                    });
                }
            }
            private static void SetGlobalRaw(string name, object val)
            {
                var l = GlobalLua.L.L;
                if (val is List<CrossEvent.EventParam>)
                {
                    PushParams(l, val as List<CrossEvent.EventParam>);
                }
                else
                {
                    l.PushLua(val);
                }
                GlobalLua.L.L.SetHierarchicalRaw(lua.LUA_GLOBALSINDEX, name, -1);
                l.pop(1);
            }

            public void UnregHandler(string cate, int refid)
            {
                if (cate != null)
                {
                    if (ThreadSafeValues.IsMainThread)
                    {
                        UnregHandlerRaw(cate, refid);
                    }
                    else
                    {
                        UnityThreadDispatcher.RunInUnityThread(() =>
                        {
                            UnregHandlerRaw(cate, refid);
                        });
                    }
                }
            }
            private static void UnregHandlerRaw(string cate, int refid)
            {
                var l = GlobalLua.L.L;
                using (var lr = new LuaStateRecover(l))
                {
                    l.GetField(lua.LUA_REGISTRYINDEX, "___levt");
                    if (l.istable(-1))
                    {
                        if (refid <= 0)
                        {
                            l.pushnil();
                            l.SetField(-2, cate);
                        }
                        else
                        {
                            l.GetField(-1, cate);
                            if (l.istable(-1))
                            {
                                int cnt = 0;
                                l.pushnil();
                                while (l.next(-2))
                                {
                                    if (l.IsNumber(-2))
                                    {
                                        var key = (int)l.tonumber(-2);
                                        if (key > cnt)
                                        {
                                            cnt = key;
                                        }
                                    }
                                    l.pop(1);
                                }

                                l.pushnumber(refid);
                                l.pushnil();
                                l.settable(-3);
                                for (int i = refid; i <= cnt; ++i)
                                {
                                    l.pushnumber(i);
                                    l.pushnumber(i + 1);
                                    l.gettable(-3);
                                    l.settable(-3);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static CrossEventEx_Lua _CrossEventEx_Lua_Instance;
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnUnityStart()
        {
            if (_CrossEventEx_Lua_Instance == null)
            {
                _CrossEventEx_Lua_Instance = new CrossEventEx_Lua();
                CrossEvent.CrossEventEx.Add(_CrossEventEx_Lua_Instance);
            }
        }
    }
}