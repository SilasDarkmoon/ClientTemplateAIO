using System;
using System.Collections.Generic;
using UnityEngineEx;

using lua = LuaLib.LuaCoreLib;
using lual = LuaLib.LuaAuxLib;
using luae = LuaLib.LuaLibEx;

namespace LuaLib
{
    public static class LuaExtend
    {
        public class LuaTransExtend : SelfHandled, ILuaTrans, ILuaTransMulti
        {
            public static readonly LuaTransExtend Instance = new LuaTransExtend();

            protected internal LuaTransExtend()
            {
            }

            #region ILuaTrans
            public void SetData(IntPtr l, int index, object val)
            {
                l.checkstack(3);
                l.pushvalue(index); // otab
                l.pushlightuserdata(LuaConst.LRKEY_TARGET); // otab #tar
                l.rawget(-2); // otab obj
                if (l.istable(-1))
                {
                    l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS); // otab obj #trans
                    l.gettable(-2); // otab obj trans
                    var trans = l.GetLuaLightObject(-1) as ILuaTrans;
                    l.pop(1); // otab obj
                    if (trans != null)
                    {
                        trans.SetData(l, -1, val);
                    }
                }
                l.pop(2);
            }

            public object GetLua(IntPtr l, int index)
            {
                l.checkstack(3);
                l.pushvalue(index); // otab
                l.pushlightuserdata(LuaConst.LRKEY_TARGET); // otab #tar
                l.rawget(-2); // otab obj
                object rv = null;
                if (l.istable(-1))
                {
                    l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS); // otab obj #trans
                    l.gettable(-2); // otab obj trans
                    var trans = l.GetLuaLightObject(-1) as ILuaTrans;
                    l.pop(1); // otab obj
                    if (trans != null)
                    {
                        rv = trans.GetLua(l, -1);
                    }
                }
                l.pop(2);
                return rv;
            }

            public Type GetType(IntPtr l, int index)
            {
                l.checkstack(3);
                l.pushvalue(index); // otab
                l.pushlightuserdata(LuaConst.LRKEY_TARGET); // otab #tar
                l.rawget(-2); // otab obj
                Type rv = null;
                if (l.istable(-1))
                {
                    l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS); // otab obj #trans
                    l.gettable(-2); // otab obj trans
                    var trans = l.GetLuaLightObject(-1) as ILuaTrans;
                    l.pop(1); // otab obj
                    if (trans != null)
                    {
                        rv = trans.GetType(l, -1);
                    }
                }
                l.pop(2);
                return rv;
            }

            public bool Nonexclusive { get { return false; } }
            #endregion

            #region ILuaTransMulti
            public void SetData<T>(IntPtr l, int index, T val)
            {
                l.checkstack(3);
                l.pushvalue(index); // otab
                l.pushlightuserdata(LuaConst.LRKEY_TARGET); // otab #tar
                l.rawget(-2); // otab obj
                if (l.istable(-1))
                {
                    l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS); // otab obj #trans
                    l.gettable(-2); // otab obj trans
                    var trans = l.GetLuaLightObject(-1) as ILuaTrans;
                    l.pop(1); // otab obj
                    if (trans != null)
                    {
                        var ttrans = trans as ILuaTrans<T>;
                        if (ttrans != null)
                        {
                            ttrans.SetData(l, -1, val);
                        }
                        else
                        {
                            trans.SetData(l, -1, val);
                        }
                    }
                }
                l.pop(2);
            }

            public T GetLua<T>(IntPtr l, int index)
            {
                l.checkstack(3);
                l.pushvalue(index); // otab
                l.pushlightuserdata(LuaConst.LRKEY_TARGET); // otab #tar
                l.rawget(-2); // otab obj
                T rv = default(T);
                if (l.istable(-1))
                {
                    l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS); // otab obj #trans
                    l.gettable(-2); // otab obj trans
                    var trans = l.GetLuaLightObject(-1) as ILuaTrans;
                    l.pop(1); // otab obj
                    if (trans != null)
                    {
                        var ttrans = trans as ILuaTrans<T>;
                        if (ttrans != null)
                        {
                            rv = ttrans.GetLua(l, -1);
                        }
                        else
                        {
                            if (trans.Nonexclusive && !typeof(T).IsAssignableFrom(trans.GetType(l, -1)))
                            {
                                var extrans = LuaTypeHub.GetTypeHub(typeof(T));
                                var gtrans = extrans as ILuaTrans<T>;
                                if (gtrans != null)
                                {
                                    rv = gtrans.GetLua(l, -1);
                                }
                                else
                                {
                                    var raw = extrans.GetLua(l, -1);
                                    if (raw is T)
                                    {
                                        rv = (T)raw;
                                    }
                                }
                            }
                            else
                            {
                                var raw = trans.GetLua(l, -1);
                                if (raw is T)
                                {
                                    rv = (T)raw;
                                }
                            }
                        }
                    }
                }
                l.pop(2);
                return rv;
            }
            #endregion
        }

        private static void MakeExtendObj(IntPtr l, int index)
        {
            l.checkstack(4);
            index = l.NormalizeIndex(index);
            l.newtable(); // ext
            l.pushlightuserdata(LuaConst.LRKEY_TARGET); // ext #tar
            l.pushvalue(index); // ext #tar tar
            l.rawset(-3); // ext
            l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS); // ext #trans
            l.pushlightuserdata(LuaTransExtend.Instance.r); // ext #trans trans
            l.rawset(-3); // ext
            l.pushlightuserdata(LuaConst.LRKEY_EXTENDED); // ext #ext
            l.pushnumber(1); // ext #ext 1
            l.rawset(-3); // ext

            l.pushlightuserdata(LuaConst.LRKEY_EXTENDED); // ext #ext
            l.gettable(lua.LUA_REGISTRYINDEX); // ext meta
            if (!l.istable(-1))
            {
                l.pop(1); // ext
                CreateExtendMeta(l); // ext meta
                l.pushlightuserdata(LuaConst.LRKEY_EXTENDED); // ext meta #ext
                l.pushvalue(-2); // ext meta #ext meta
                l.settable(lua.LUA_REGISTRYINDEX); // ext meta
            }
            l.setmetatable(-2); // ext
            l.replace(index);
        }
        private static void MakeExtendType(IntPtr l, int index)
        {
            l.checkstack(4);
            index = l.NormalizeIndex(index);
            l.newtable(); // ext
            l.pushlightuserdata(LuaConst.LRKEY_TARGET); // ext #tar
            l.pushvalue(index); // ext #tar tar
            l.rawset(-3); // ext
            l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS); // ext #trans
            l.pushlightuserdata(LuaTransExtend.Instance.r); // ext #trans trans
            l.rawset(-3); // ext
            l.pushlightuserdata(LuaConst.LRKEY_EXTENDED); // ext #ext
            l.pushnumber(2); // ext #ext 2
            l.rawset(-3); // ext

            l.pushlightuserdata(LuaConst.LRKEY_EXTENDED); // ext #ext
            l.gettable(lua.LUA_REGISTRYINDEX); // ext meta
            if (!l.istable(-1))
            {
                l.pop(1); // ext
                CreateExtendMeta(l); // ext meta
                l.pushlightuserdata(LuaConst.LRKEY_EXTENDED); // ext meta #ext
                l.pushvalue(-2); // ext meta #ext meta
                l.settable(lua.LUA_REGISTRYINDEX); // ext meta
            }
            l.setmetatable(-2); // ext
            l.replace(index);
        }
        private static void MakeExtendCallable(IntPtr l, int index, int targetIndex)
        {
            l.checkstack(4);
            index = l.NormalizeIndex(index);
            if (targetIndex != 0)
            {
                targetIndex = l.NormalizeIndex(targetIndex);
            }
            l.newtable(); // ext
            l.pushlightuserdata(LuaConst.LRKEY_TARGET); // ext #tar
            l.pushvalue(index); // ext #tar tar
            l.rawset(-3); // ext
            l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS); // ext #trans
            l.pushlightuserdata(LuaTransExtend.Instance.r); // ext #trans trans
            l.rawset(-3); // ext
            l.pushlightuserdata(LuaConst.LRKEY_EXTENDED); // ext #ext
            l.pushnumber(3); // ext #ext 3
            l.rawset(-3); // ext
            if (targetIndex != 0)
            {
                l.pushlightuserdata(LuaConst.LRKEY_EXT_CALLEE);
                l.pushvalue(targetIndex);
                l.rawset(-3);
            }

            l.pushlightuserdata(LuaConst.LRKEY_EXTENDED); // ext #ext
            l.gettable(lua.LUA_REGISTRYINDEX); // ext meta
            if (!l.istable(-1))
            {
                l.pop(1); // ext
                CreateExtendMeta(l); // ext meta
                l.pushlightuserdata(LuaConst.LRKEY_EXTENDED); // ext meta #ext
                l.pushvalue(-2); // ext meta #ext meta
                l.settable(lua.LUA_REGISTRYINDEX); // ext meta
            }
            l.setmetatable(-2); // ext
            l.replace(index);
        }

        public static void MakeLuaExtend(IntPtr l, int index)
        {
#if !DISABLE_LUA_HUB_C
            if (LuaHub.LuaHubC.Ready)
            {
                LuaHub.LuaHubC.MakeExtend(l, index);
            }
            else
#endif
            {
                MakeExtend(l, index, 0);
            }
        }
        public static void MakeLuaUnextend(IntPtr l, int index)
        {
#if !DISABLE_LUA_HUB_C
            if (LuaHub.LuaHubC.Ready)
            {
                LuaHub.LuaHubC.MakeUnextend(l, index);
            }
            else
#endif
            {
                MakeUnextend(l, index);
            }
        }

        private static void MakeExtend(IntPtr l, int index)
        {
            MakeExtend(l, index, 0);
        }
        private static void MakeExtend(IntPtr l, int index, int targetIndex)
        {
            if (l.IsUserData(index))
            {
                MakeExtendObj(l, index);
            }
            else if (l.istable(index))
            {
                int exttype = 0;
                if (l.getmetatable(index)) // meta
                {
                    l.pushlightuserdata(LuaConst.LRKEY_EXTENDED); // meta #ext
                    l.rawget(-2); // meta exttype
                    exttype = (int)l.tonumber(-1);
                    l.pop(2); // X
                }

                switch (exttype)
                {
                    case 1:
                        MakeExtendObj(l, index);
                        break;
                    case 2:
                        MakeExtendType(l, index);
                        break;
                    case 3:
                        MakeExtendCallable(l, index, targetIndex);
                        break;
                }
            }
        }

        private static void MakeUnextend(IntPtr l, int index)
        {
            if (l.istable(index) && IsExtended(l, index))
            {
                index = l.NormalizeIndex(index);
                l.pushlightuserdata(LuaConst.LRKEY_TARGET); // #tar
                l.gettable(index); // tar
                l.replace(index);
            }
        }

        public static void CreateExtendMeta(IntPtr l)
        {
            l.checkstack(3);
            l.newtable();
            l.PushString(LuaConst.LS_META_KEY_CALL);
            l.pushcfunction(LuaFuncExtCall);
            l.rawset(-3);
            l.PushString(LuaConst.LS_META_KEY_INDEX);
            l.pushcfunction(LuaFuncExtIndex);
            l.rawset(-3);
            l.PushString(LuaConst.LS_META_KEY_NINDEX);
            l.pushcfunction(LuaFuncExtNewIndex);
            l.rawset(-3);
            l.PushString(LuaConst.LS_META_KEY_EQ);
            l.PushCommonEqFunc();
            l.rawset(-3);

            // bin-op
            l.PushString(LuaConst.LS_META_KEY_ADD);
            l.PushString(LuaConst.LS_SP_KEY_ADD);
            l.pushcclosure(LuaFuncExtBin, 1);
            l.rawset(-3);
            l.PushString(LuaConst.LS_META_KEY_SUB);
            l.PushString(LuaConst.LS_SP_KEY_SUB);
            l.pushcclosure(LuaFuncExtBin, 1);
            l.rawset(-3);
            l.PushString(LuaConst.LS_META_KEY_MUL);
            l.PushString(LuaConst.LS_SP_KEY_MUL);
            l.pushcclosure(LuaFuncExtBin, 1);
            l.rawset(-3);
            l.PushString(LuaConst.LS_META_KEY_DIV);
            l.PushString(LuaConst.LS_SP_KEY_DIV);
            l.pushcclosure(LuaFuncExtBin, 1);
            l.rawset(-3);
            l.PushString(LuaConst.LS_META_KEY_MOD);
            l.PushString(LuaConst.LS_SP_KEY_MOD);
            l.pushcclosure(LuaFuncExtBin, 1);
            l.rawset(-3);
            l.PushString(LuaConst.LS_META_KEY_LT);
            l.PushString(LuaConst.LS_SP_KEY_LT);
            l.pushcclosure(LuaFuncExtBin, 1);
            l.rawset(-3);
            l.PushString(LuaConst.LS_META_KEY_LE);
            l.PushString(LuaConst.LS_SP_KEY_LE);
            l.pushcclosure(LuaFuncExtBin, 1);
            l.rawset(-3);

            // unary-op
            l.PushString(LuaConst.LS_META_KEY_UNM);
            l.pushvalue(-1);
            l.pushcclosure(LuaFuncExtUnary, 1);
            l.rawset(-3);
            l.PushString(LuaConst.LS_META_KEY_TOSTRING);
            l.pushvalue(-1);
            l.pushcclosure(LuaFuncExtUnary, 1);
            l.rawset(-3);
        }

        public static bool IsExtended(this IntPtr l, int index)
        {
            bool extended = false;
            if (l.istable(index))
            {
                index = l.NormalizeIndex(index);
                l.pushlightuserdata(LuaConst.LRKEY_EXTENDED); // #ext
                l.gettable(index); // ext
                extended = l.toboolean(-1);
                l.pop(1);
            }
            return extended;
        }
        public static bool IsExtended(this IntPtr l)
        {
            l.GetGlobal(LuaConst.LS_SP_KEY_EXT);
            var rv = l.toboolean(-1);
            l.pop(1);
            return rv;
        }

        private static readonly lua.CFunction LuaFuncExtCall = new lua.CFunction(LuaMetaExtCall);
        private static readonly lua.CFunction LuaFuncExtIndex = new lua.CFunction(LuaMetaExtIndex);
        private static readonly lua.CFunction LuaFuncExtNewIndex = new lua.CFunction(LuaMetaExtNewIndex);
        private static readonly lua.CFunction LuaFuncExtBin = new lua.CFunction(LuaMetaExtBinaryOp);
        private static readonly lua.CFunction LuaFuncExtUnary = new lua.CFunction(LuaMetaExtUnaryOp);

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        private static int LuaMetaExtCall(IntPtr l)
        {
            l.pushcfunction(LuaHub.LuaFuncOnError); // err
            var oldtop = l.gettop();
            l.pushvalue(1); // func
            MakeUnextend(l, -1);
            l.pushlightuserdata(LuaConst.LRKEY_EXT_CALLEE);
            l.gettable(1); // func tar
            if (l.isnoneornil(-1))
            {
                l.pop(1); // func
            }
            else
            {
                MakeUnextend(l, -1);
            }
            for (int i = 2; i <= oldtop - 1; ++i)
            {
                l.pushvalue(i);
                MakeUnextend(l, -1);
            }
            var code = l.pcall(l.gettop() - oldtop - 1, lua.LUA_MULTRET, oldtop);
            if (code != 0)
            {
                l.settop(oldtop - 1);
                return 0;
            }
            var rvcnt = l.gettop() - oldtop;
            for (int i = 1; i <= rvcnt; ++i)
            {
                MakeExtend(l, i + oldtop);
            }
            l.remove(oldtop);
            return rvcnt;
        }
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        private static int LuaMetaExtIndex(IntPtr l)
        {
            l.pushvalue(1); // obj
            MakeUnextend(l, -1);
            l.pushvalue(2); // obj key
            MakeUnextend(l, -1);
            l.gettable(-2); // obj val

            int exttype = 0;
            l.pushlightuserdata(LuaConst.LRKEY_EXTENDED); // obj val #ext
            l.gettable(1); // obj val exttype
            exttype = (int)l.tonumber(-1);
            l.pop(1); // obj val
            switch (exttype)
            {
                case 2:
                    MakeExtend(l, -1);
                    break;
                case 1:
                    {
                        if (l.isfunction(-1))
                        {
                            l.pushvalue(-1); // obj val val
                            l.gettable(-3); // obj val info
                            if (l.toboolean(-1))
                            {
                                MakeExtendCallable(l, -2, 1);
                            }
                            l.pop(1); // obj val
                        }
                        else
                        {
                            MakeExtend(l, -1, 1);
                        }
                    }
                    break;
                case 3:
                    {
                        l.pushlightuserdata(LuaConst.LRKEY_EXT_CALLEE);
                        l.gettable(1);
                        if (l.isnoneornil(-1))
                        {
                            l.pop(1);
                            MakeExtendCallable(l, -1, 0);
                        }
                        else
                        {
                            // obj val tar
                            MakeExtendCallable(l, -2, -1);
                            l.pop(1); // obj val
                        }
                    }
                    break;
            }
            l.remove(-2); // val

            return 1;
        }
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        private static int LuaMetaExtNewIndex(IntPtr l)
        {
            l.pushvalue(1); // obj
            MakeUnextend(l, -1);
            l.pushvalue(2); // obj key
            MakeUnextend(l, -1);
            l.pushvalue(3); // obj key val
            MakeUnextend(l, -1);
            l.settable(-3); // obj
            l.pop(1); // X
            return 0;
        }
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        private static int LuaMetaExtBinaryOp(IntPtr l)
        {
            if (l.IsUserData(1) || l.istable(1))
            {
                l.pushcfunction(LuaHub.LuaFuncOnError); // err
                l.pushvalue(lua.upvalueindex(1)); // err "op"
                l.gettable(1); // err func1
                if (l.isfunction(-1))
                {
                    l.pushvalue(1); // err func1 op1
                    MakeUnextend(l, -1);
                    l.pushvalue(2); // err func1 op1 op2
                    MakeUnextend(l, -1);
                    var code = l.pcall(2, 2, -4); // err rv failed
                    if (code != 0)
                    { // err failmessage
                        l.pop(2); // X
                    }
                    else if (!l.toboolean(-1))
                    { // err rv failed(false)
                        l.pop(1); // err rv
                        l.remove(-2); // rv
                        MakeExtend(l, -1);
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
                    MakeUnextend(l, -1);
                    l.pushvalue(2); // err func2 op1 op2
                    MakeUnextend(l, -1);
                    var code = l.pcall(2, 2, -4); // err rv failed
                    if (code != 0)
                    { // err failmessage
                        l.pop(2); // X
                    }
                    else if (!l.toboolean(-1))
                    { // err rv failed(false)
                        l.pop(1); // err rv
                        l.remove(-2); // rv
                        MakeExtend(l, -1);
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
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        private static int LuaMetaExtUnaryOp(IntPtr l)
        {
            l.pushvalue(1); // obj
            MakeUnextend(l, -1);
            if (l.getmetatable(-1)) // obj meta
            {
                l.pushvalue(lua.upvalueindex(1)); // obj meta $op
                l.rawget(-2); // obj meta op
                if (l.isnoneornil(-1))
                {
                    l.pop(3); // X
                    return 0;
                }
                l.remove(-2); // obj op
                l.insert(-2); // op obj
                l.pushcfunction(LuaHub.LuaFuncOnError); // op obj err
                l.insert(-3); // err op obj
                var code = l.pcall(1, 1, -3); // err rv
                if (code != 0)
                {
                    l.pop(2); // X
                    return 0;
                }
                else
                {
                    l.remove(-2); // rv
                    MakeExtend(l, -1);
                    return 1;
                }
            }
            else // obj
            {
                l.pop(1); // X
                return 0;
            }
        }
    }
}