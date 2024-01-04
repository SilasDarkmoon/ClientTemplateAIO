using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngineEx;
using LuaLib;

using lua = LuaLib.LuaCoreLib;
using lual = LuaLib.LuaAuxLib;
using luae = LuaLib.LuaLibEx;

namespace LuaLib
{
    public static class HotFixCaller
    {
        public class HotFixCallerContext
        {
            public HashSet<IntPtr> ReadyStates;
            public LuaState ThreadedLuaState;
            public IntPtr CallerLuaState;
            public HashSet<long> NonExistTokens;
        }
        [ThreadStatic] private static HotFixCallerContext _Context;
#if !NET_4_6 && !NET_STANDARD_2_0
        private static Unity.Collections.Concurrent.ConcurrentQueue<HotFixCallerContext> _CallerContexts = new Unity.Collections.Concurrent.ConcurrentQueue<HotFixCallerContext>();
#else
        private static System.Collections.Concurrent.ConcurrentQueue<HotFixCallerContext> _CallerContexts = new System.Collections.Concurrent.ConcurrentQueue<HotFixCallerContext>();
#endif
        private static HotFixCallerContext GetOrCreateContext()
        {
            HotFixCallerContext context;
            if ((context = _Context) != null)
            {
                return context;
            }
            context = new HotFixCallerContext();
            _CallerContexts.Enqueue(context);
            _Context = context;
            return context;
        }

        public static void ResetAllCallerContexts()
        {
            var contexts = _CallerContexts.ToArray();
            for (int i = 0; i < contexts.Length; ++i)
            {
                var context = contexts[i];
                System.Threading.Volatile.Write(ref context.ReadyStates, null);
                System.Threading.Volatile.Write(ref context.ThreadedLuaState, null);
                System.Threading.Volatile.Write(ref context.CallerLuaState, IntPtr.Zero);
                System.Threading.Volatile.Write(ref context.NonExistTokens, null);
            }
        }

        private static volatile int _PackageVer = -1;

        public static IntPtr GetLuaStateForHotFix()
        {
#if UNITY_EDITOR
            if ((_Context == null || _Context.ReadyStates == null) && SafeInitializerUtils.IsInitializingInUnityCtor)
            {
                return IntPtr.Zero;
            }
#endif
            var context = GetOrCreateContext();
            if (context.ReadyStates == null)
            {
                context.ReadyStates = new HashSet<IntPtr>();
                if (ThreadSafeValues.IsMainThread)
                {
#if UNITY_EDITOR
#if DEBUG_LUA_HOTFIX_IN_EDITOR
                    _PackageVer = 0;
#else
#endif
#else
                    int packageVer;
                    GlobalLua.L.L.GetGlobalTable(out packageVer, "___resver", "package");
                    _PackageVer = packageVer;
#endif
                }
            }
            var running = LuaHub.RunningLuaState;
            if (running == IntPtr.Zero)
            {
                if (ReferenceEquals(context.ThreadedLuaState, null))
                {
                    if (ThreadSafeValues.IsMainThread)
                    {
                        running = context.ThreadedLuaState = GlobalLua.L;
                    }
                    else
                    {
                        running = context.ThreadedLuaState = new LuaState();
                        IntPtr l = running;
                        Assembly2Lua.Init(l);
                        Json2Lua.Init(l);
                        LuaFramework.Init(l);
                        // should we init other libs (maybe in other package)? for example: lua-protobuf?
                        // currently these are enough.
                        // and calling a func with hotfix in non-main thread rarely happens.
                    }
                    running.SetGlobal("hotfixver", _PackageVer);
                    InitHotFixRoot(running);
                }
                else
                {
                    running = context.ThreadedLuaState;
                }
            }
            else
            {
                if (context.CallerLuaState != running)
                {
                    context.CallerLuaState = running;
                    if (context.ReadyStates.Add(running.Indicator()))
                    {
                        running.SetGlobal("hotfixver", _PackageVer);
                        InitHotFixRoot(running);
                    }
                }
            }
            return running;
        }
        public static void InitHotFixRoot(IntPtr l)
        {
            if (l.TryRequire("hotfix").IsValid)
            { // hotfix
                l.pushlightuserdata(LuaConst.LRKEY_HOTFIX_ROOT); // hotfix #hotfix
                l.insert(-2); // #hotfix hotfix
                l.settable(lua.LUA_REGISTRYINDEX); // X
            }
        }
        public static void GetHotFixRoot(IntPtr l)
        {
            l.pushlightuserdata(LuaConst.LRKEY_HOTFIX_ROOT); // #hotfix
            l.gettable(lua.LUA_REGISTRYINDEX); // hotfix
        }

        public static bool CallHotFix<TIn, TOut>(string token, TIn args, out TOut result)
            where TIn : struct, ILuaPack
            where TOut : struct, ILuaPack
        {
#if UNITY_EDITOR && !DEBUG_LUA_HOTFIX_IN_EDITOR
            result = default(TOut);
            return false;
#else
            result = default(TOut);
            var l = GetLuaStateForHotFix();
#if UNITY_EDITOR
            if (l == IntPtr.Zero)
            {
                result = default(TOut);
                return false;
            }
#endif
            using (var lr = l.CreateStackRecover())
            {
                //if (l.TryRequire("hotfix").IsValid)
                GetHotFixRoot(l);
                {
                    if (l.istable(-1))
                    {
                        l.GetField(-1, token); // hotfix func
                        if (l.isfunction(-1))
                        {
                            var oldtop = l.gettop();
                            l.pushcfunction(LuaHub.LuaFuncOnError); // hotfix func error
                            l.insert(-2); // hotfix error func
                            var argc = args.Length;
                            args.PushToLua(l); // hotfix error func args
                            var code = l.pcall(argc, result.Length + 1, oldtop); // hotfix error success results
                            if (code == 0)
                            {
                                if (l.toboolean(oldtop + 1))
                                {
                                    result.GetFromLua(l);
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
#endif
        }

        public static bool CallHotFixN<TIn, TOut>(long token, TIn args, out TOut result)
            where TIn : struct, ILuaPack
            where TOut : struct, ILuaPack
        {
#if UNITY_EDITOR && !DEBUG_LUA_HOTFIX_IN_EDITOR
            result = default(TOut);
            return false;
#else
            result = default(TOut);
            if (_Context != null && _Context.NonExistTokens != null && _Context.NonExistTokens.Contains(token))
            {
                return false;
            }
            var l = GetLuaStateForHotFix();
#if UNITY_EDITOR
            if (l == IntPtr.Zero)
            {
                result = default(TOut);
                return false;
            }
#endif
            using (var lr = l.CreateStackRecover())
            {
                //if (l.TryRequire("hotfix").IsValid)
                GetHotFixRoot(l);
                {
                    if (l.istable(-1))
                    {
                        l.pushnumber(token); // hotfix token
                        l.gettable(-2); // hotfix func
                        if (l.isfunction(-1))
                        {
                            var oldtop = l.gettop();
                            l.pushcfunction(LuaHub.LuaFuncOnError); // hotfix func error
                            l.insert(-2); // hotfix error func
                            var argc = args.Length;
                            args.PushToLua(l); // hotfix error func args
                            var code = l.pcall(argc, result.Length + 1, oldtop); // hotfix error success results
                            if (code == 0)
                            {
                                if (l.toboolean(oldtop + 1))
                                {
                                    result.GetFromLua(l);
                                    return true;
                                }
                            }
                        }
                        else
                        {
                            var context = GetOrCreateContext();
                            if (context.NonExistTokens == null)
                            {
                                context.NonExistTokens = new HashSet<long> { token };
                            }
                            else
                            {
                                context.NonExistTokens.Add(token);
                            }
                        }
                    }
                }
            }
            return false;
#endif
        }

        #region Method Hash
        private static readonly Dictionary<string, long> _DesignatedHash = new Dictionary<string, long>();
        public static long GetTokenHash(string token)
        {
            if (token == null) return 0;
            string mainpart = token;
            bool istail = false;
            if (token.EndsWith(" tail"))
            {
                istail = true;
                mainpart = token.Substring(0, token.Length - " tail".Length);
            }
            else if (token.EndsWith(" head"))
            {
                mainpart = token.Substring(0, token.Length - " head".Length);
            }
            long hash = 0;
            if (!_DesignatedHash.TryGetValue(mainpart, out hash))
            {
                int criticalindex = mainpart.IndexOf(' ');
                if (criticalindex >= 0 && criticalindex + 1 < mainpart.Length)
                {
                    ++criticalindex;
                }
                else
                {
                    criticalindex = -1;
                }
                hash = ExtendedStringHash.GetHashCodeEx(mainpart, 0, 1, criticalindex, 0);
            }
            if (istail)
            {
                hash = -hash;
            }
            return hash;
        }
        public static void LoadDesignatedHash(string lib)
        {
            if (!ThreadSafeValues.IsMainThread)
            {
                return;
            }
            _DesignatedHash.Clear();
            var l = GlobalLua.L.L;
            if (l.TryRequire(lib, true).IsValid)
            {
                l.ForEach<string, long>(-1, (str, hash) => _DesignatedHash.Add(str, hash));
                l.pop(1);
            }
        }
        #endregion
    }
}