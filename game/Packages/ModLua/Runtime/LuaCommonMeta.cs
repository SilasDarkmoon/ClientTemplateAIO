using System;
using System.Collections.Generic;

using lua = LuaLib.LuaCoreLib;
using lual = LuaLib.LuaAuxLib;
using luae = LuaLib.LuaLibEx;

namespace LuaLib
{
    public static class LuaCommonMeta
    {
        public static void PushRawMetaTable(this IntPtr l)
        {
            l.checkstack(3);
            l.pushlightuserdata(LuaConst.LRKEY_OBJ_META_RAW); // mkey
            l.gettable(lua.LUA_REGISTRYINDEX); // meta
            if (!l.istable(-1))
            {
                l.pop(1); // X
                l.CreateRawMetaTable(); // meta
                l.pushlightuserdata(LuaConst.LRKEY_OBJ_META_RAW); // meta mkey
                l.pushvalue(-2); // meta mkey meta
                l.settable(lua.LUA_REGISTRYINDEX); // meta
            }
        }
        private static void CreateRawMetaTable(this IntPtr l)
        {
            l.checkstack(2);
            l.newtable(); // meta
            l.pushcfunction(LuaFuncRawGC); // meta func
            l.SetField(-2, LuaConst.LS_META_KEY_GC); // meta
            l.pushcfunction(LuaFuncRawToStr);
            l.SetField(-2, LuaConst.LS_META_KEY_TOSTRING);
        }

        public static void PushCachedMetaTable(this IntPtr l)
        {
            l.checkstack(3);
            l.pushlightuserdata(LuaConst.LRKEY_OBJ_META_CACHED); // mkey
            l.gettable(lua.LUA_REGISTRYINDEX); // meta
            if (!l.istable(-1))
            {
                l.pop(1); // X
                l.CreateCachedMetaTable(); // meta
                l.pushlightuserdata(LuaConst.LRKEY_OBJ_META_CACHED); // meta mkey
                l.pushvalue(-2); // meta mkey meta
                l.settable(lua.LUA_REGISTRYINDEX); // meta
            }
        }
        private static void CreateCachedMetaTable(this IntPtr l)
        {
            l.checkstack(2);
            l.newtable(); // meta
            l.pushcfunction(LuaFuncCachedGC); // meta func
            l.SetField(-2, LuaConst.LS_META_KEY_GC); // meta
            l.pushcfunction(LuaFuncRawToStr);
            l.SetField(-2, LuaConst.LS_META_KEY_TOSTRING);
        }

        public static void PushCommonMetaTable(this IntPtr l)
        {
            l.checkstack(3);
            l.pushlightuserdata(LuaConst.LRKEY_OBJ_META); // mkey
            l.gettable(lua.LUA_REGISTRYINDEX); // meta
            if (!l.istable(-1))
            {
                l.pop(1); // X
                l.CreateCommonMetaTable(); // meta
                l.pushlightuserdata(LuaConst.LRKEY_OBJ_META); // meta mkey
                l.pushvalue(-2); // meta mkey meta
                l.settable(lua.LUA_REGISTRYINDEX); // meta
            }
        }
        private static void CreateCommonMetaTable(this IntPtr l)
        {
            l.checkstack(2);
            l.newtable(); // meta
            l.pushcfunction(LuaFuncCommonGC); // meta func
            l.SetField(-2, LuaConst.LS_META_KEY_GC); // meta
            l.pushcfunction(LuaFuncRawToStr); // meta func
            l.SetField(-2, LuaConst.LS_META_KEY_TOSTRING); // meta
        }

        public static void PushCommonEqFunc(this IntPtr l)
        {
            l.checkstack(3);
            l.pushlightuserdata(LuaConst.LRKEY_OBJ_META_EQ); // mkey
            l.gettable(lua.LUA_REGISTRYINDEX); // meta
            if (!l.isfunction(-1))
            {
                l.pop(1); // X
                l.PushBinaryOp(LuaConst.LS_SP_KEY_EQ);
#if !DISABLE_LUA_HUB_C
                if (LuaHub.LuaHubC.Ready)
                {
                    LuaHub.LuaHubC.lua_pushCommonEq(l, 1);
                }
                else
#endif
                {

                    l.pushcclosure(LuaFuncEq, 1); // meta
                }
                l.pushlightuserdata(LuaConst.LRKEY_OBJ_META_EQ); // meta mkey
                l.pushvalue(-2); // meta mkey meta
                l.settable(lua.LUA_REGISTRYINDEX); // meta
            }
        }

        public static ILuaMeta GetLuaMeta(this IntPtr l, int index)
        {
            l.getfenv(index); // env
            ILuaMeta mex = null;
            if (l.istable(-1))
            {
                l.pushlightuserdata(LuaConst.LRKEY_OBJ_META_EX); // env mkey
                l.gettable(-2); // env mex
                if (l.islightuserdata(-1))
                {
                    mex = l.GetLuaLightObject(-1) as ILuaMeta;
                }
                l.pop(2); // X
            }
            else
            {
                l.pop(1); // X
            }
            return mex;
        }

        public class LuaTransCommon : SelfHandled, ILuaTrans
        {
            public static readonly LuaTransCommon Instance = new LuaTransCommon();

            protected internal LuaTransCommon()
            {
            }

            #region ILuaTrans
            public void SetData(IntPtr l, int index, object val)
            {
                l.checkstack(3);
                l.pushvalue(index); // otab
                l.pushlightuserdata(LuaConst.LRKEY_TARGET); // otab #tar
                if (val == null)
                {
                    l.pushnil();
                }
                else
                {
                    l.PushLuaRawObject(val); // otab #tar obj
                }
                l.rawset(-3); // otab
                l.pop(1);
            }

            public object GetLua(IntPtr l, int index)
            {
                return l.GetLuaTableObjectDirect(index);
            }

            public Type GetType(IntPtr l, int index)
            {
                l.checkstack(2);
                l.pushvalue(index); // otab
                l.pushlightuserdata(LuaConst.LRKEY_TARGET); // otab #tar
                l.rawget(-2); // otab obj
                object rv = null;
                if (l.IsUserData(-1))
                {
                    rv = l.GetLuaRawObject(-1);
                }
                l.pop(2);
                if (rv != null)
                {
                    return rv.GetType();
                }
                return null;
            }
            public bool Nonexclusive { get { return false; } }
            #endregion
        }

        public class LuaTransType : SelfHandled, ILuaTrans
        {
            public static readonly LuaTransType Instance = new LuaTransType();

            protected internal LuaTransType()
            {
            }

            #region ILuaTrans
            public void SetData(IntPtr l, int index, object val)
            {
                // can not change
                l.LogError("Type wrapper's binded-obj cannot be changed.");
            }

            public object GetLua(IntPtr l, int index)
            {
                l.checkstack(2);
                l.pushvalue(index); // otab
                l.pushlightuserdata(LuaConst.LRKEY_TARGET); // otab #tar
                l.rawget(-2); // otab obj
                var typeref = l.GetLuaLightObject(-1) as ILuaTypeRef;
                l.pop(2);
                if (typeref != null)
                {
                    return typeref.t;
                }
                else
                {
                    return null;
                }
            }

            public Type GetType(IntPtr l, int index)
            {
                return typeof(Type);
            }
            public bool Nonexclusive { get { return false; } }
            #endregion
        }

        public static void PushFunction(this IntPtr l, ILuaMetaCall meta)
        {
            l.checkstack(1);
            l.pushlightuserdata(meta.r);
            l.pushcclosure(LuaFuncCall, 1);
        }

        public static void PushBinaryOp(this IntPtr l, LuaString op)
        {
            l.checkstack(5);
            l.PushString(op); // str
            l.pushlightuserdata(LuaConst.LRKEY_OBJ_META_BIN); // str #bin
            l.gettable(lua.LUA_REGISTRYINDEX); // str reg
            if (!l.istable(-1))
            {
                l.pop(1); // str
                l.newtable(); // str reg
                l.pushlightuserdata(LuaConst.LRKEY_OBJ_META_BIN); // str reg #bin
                l.pushvalue(-2); // str reg #bin reg
                l.settable(lua.LUA_REGISTRYINDEX); // str reg
            }
            l.pushvalue(-2); // str reg str
            l.rawget(-2); // str reg func
            if (l.isfunction(-1))
            {
                l.insert(-3); // func str reg
                l.pop(2); // func
                return;
            }
            l.pop(1); // str reg
            l.pushvalue(-2); // str reg str
#if !DISABLE_LUA_HUB_C
            if (LuaHub.LuaHubC.Ready)
            {
                LuaHub.LuaHubC.lua_pushCommonBinaryOp(l, 1);
            }
            else
#endif
            {
                l.pushcclosure(LuaFuncBinary, 1);
            }
            // str reg func
            l.pushvalue(-3); // str reg func str
            l.pushvalue(-2); // str reg func str func
            l.rawset(-4); // str reg func
            l.insert(-3); // func str reg
            l.pop(2); // func
        }

        public static readonly lua.CFunction LuaFuncRawGC = new lua.CFunction(LuaMetaRawGC);
        public static readonly lua.CFunction LuaFuncCachedGC = new lua.CFunction(LuaMetaCachedGC);
        public static readonly lua.CFunction LuaFuncCommonGC = new lua.CFunction(LuaMetaCommonGC);
        public static readonly lua.CFunction LuaFuncCall = new lua.CFunction(LuaMetaCall);
        public static readonly lua.CFunction LuaFuncEq = new lua.CFunction(LuaMetaEq);
        public static readonly lua.CFunction LuaFuncRawEq = new lua.CFunction(LuaMetaRawEq);
        public static readonly lua.CFunction LuaFuncRawToStr = new lua.CFunction(LuaMetaRawToStr);
        public static readonly lua.CFunction LuaFuncCommonToStr = new lua.CFunction(LuaMetaCommonToStr);
        public static readonly lua.CFunction LuaFuncBinary = new lua.CFunction(LuaMetaCommonBinaryOp);

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        private static int LuaMetaRawGC(IntPtr l)
        {
            IntPtr pud = l.touserdata(1);
            IntPtr hval = System.Runtime.InteropServices.Marshal.ReadIntPtr(pud);
            System.Runtime.InteropServices.GCHandle handle;
            try
            {
                handle = (System.Runtime.InteropServices.GCHandle)hval;
                handle.Free();
            }
            catch { }
            return 0;
        }
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        private static int LuaMetaCachedGC(IntPtr l)
        {
            IntPtr pud = l.touserdata(1);
            IntPtr hval = System.Runtime.InteropServices.Marshal.ReadIntPtr(pud);
            System.Runtime.InteropServices.GCHandle handle = new System.Runtime.InteropServices.GCHandle();
            object obj = null;
            try
            {
                handle = (System.Runtime.InteropServices.GCHandle)hval;
                obj = handle.Target;
            }
            catch { }

            if (obj != null)
            {
                var cache = LuaObjCache.GetObjCache(l);
                if (cache != null)
                {
                    cache.Remove(obj);
                }
            }

            try
            {
                handle.Free();
            }
            catch { }

            return 0;
        }
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        private static int LuaMetaCommonGC(IntPtr l)
        {
            IntPtr pud = l.touserdata(1);
            IntPtr hval = System.Runtime.InteropServices.Marshal.ReadIntPtr(pud);
            System.Runtime.InteropServices.GCHandle handle = new System.Runtime.InteropServices.GCHandle();
            object obj = null;
            try
            {
                handle = (System.Runtime.InteropServices.GCHandle)hval;
                obj = handle.Target;
            }
            catch { }

            if (obj != null)
            {
                var cache = LuaObjCache.GetObjCache(l);
                if (cache != null)
                {
                    cache.Remove(obj);
                }

                ILuaMeta mex = GetLuaMeta(l, 1);
                if (mex != null)
                {
                    mex.gc(l, obj); // note: the gc should not throw any exception!
                }
            }

            try
            {
                handle.Free();
            }
            catch { }

            return 0;
        }
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        private static int LuaMetaCall(IntPtr l)
        {
            var oldtop = l.gettop();
            var meta = l.GetLuaLightObject(lua.upvalueindex(1)) as ILuaMetaCall;
            try
            {
                meta.call(l, null);
            }
            catch (Exception e)
            {
                l.LogError(e.ToString());
                l.settop(oldtop);
            }
            return l.gettop() - oldtop;
        }
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        private static int LuaMetaEq(IntPtr l)
        {
            l.pushcfunction(LuaHub.LuaFuncOnError); // err
            var oldtop = l.gettop();
            l.pushvalue(lua.upvalueindex(1)); // binop
            l.pushvalue(1);
            l.pushvalue(2);
            var code = l.pcall(2, lua.LUA_MULTRET, -4);
            if (code != 0)
            { // err failmessage
                l.pop(2); // X
            }
            else
            { // err results
                var rv = l.gettop() - oldtop;
                l.remove(oldtop);
                if (rv > 0)
                {
                    return rv;
                }
            }
            return LuaMetaRawEq(l);
        }
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        private static int LuaMetaRawEq(IntPtr l)
        {
            var o1 = l.GetLua(1);
            var o2 = l.GetLua(2);
            l.pushboolean(BaseDynamic.EitherEquals(o1, o2));
            return 1;
        }
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        private static int LuaMetaRawToStr(IntPtr l)
        {
            IntPtr pud = l.touserdata(1);
            IntPtr hval = System.Runtime.InteropServices.Marshal.ReadIntPtr(pud);
            System.Runtime.InteropServices.GCHandle handle;
            object obj = null;
            try
            {
                handle = (System.Runtime.InteropServices.GCHandle)hval;
                obj = handle.Target;
            }
            catch { }

            if (obj == null)
            {
                l.PushString("nullptr");
            }
            else
            {
                l.PushString(obj.ToString());
            }
            return 1;
        }
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        private static int LuaMetaCommonToStr(IntPtr l)
        {
            var o1 = l.GetLua(1);
            if (o1 == null)
            {
                l.PushString("nullptr");
            }
            else
            {
                l.PushString(o1.ToString());
            }
            return 1;
        }
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        private static int LuaMetaCommonBinaryOp(IntPtr l)
        {
            if (l.IsUserData(1) || l.istable(1))
            {
                l.pushcfunction(LuaHub.LuaFuncOnError); // err
                l.pushvalue(lua.upvalueindex(1)); // err "op"
                l.gettable(1); // err func1
                if (l.isfunction(-1))
                {
                    l.pushvalue(1); // err func1 op1
                    l.pushvalue(2); // err func1 op1 op2
                    var code = l.pcall(2, 2, -4); // err rv failed
                    if (code != 0)
                    { // err failmessage
                        l.pop(2); // X
                    }
                    else if (!l.toboolean(-1))
                    { // err rv failed(false)
                        l.pop(1); // err rv
                        l.remove(-2); // rv
                        return 1;
                    }
                    else
                    { // err rv failed(true)
                        l.pop(3); // X
                    }
                }
                else
                {
                    l.pop(2); // X
                }
            }
            if (l.IsUserData(2) || l.istable(2))
            {
                l.pushcfunction(LuaHub.LuaFuncOnError); // err
                l.pushvalue(lua.upvalueindex(1)); // err "op"
                l.gettable(2); // err func2
                if (l.isfunction(-1))
                {
                    l.pushvalue(1); // err func2 op1
                    l.pushvalue(2); // err func2 op1 op2
                    var code = l.pcall(2, 2, -4); // err rv failed
                    if (code != 0)
                    { // err failmessage
                        l.pop(2); // X
                    }
                    else if (!l.toboolean(-1))
                    { // err rv failed(false)
                        l.pop(1); // err rv
                        l.remove(-2); // rv
                        return 1;
                    }
                    else
                    { // err rv failed(true)
                        l.pop(3); // X
                    }
                }
                else
                {
                    l.pop(2); // X
                }
            }
            return 0;
        }
    }
}