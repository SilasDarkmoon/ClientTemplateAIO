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
    public sealed class LuaOnStackFunc : BaseLuaOnStack
    {
        public LuaOnStackFunc(IntPtr l, int index)
        {
            L = l;
            StackPos = index;
        }

        public override object[] Call(params object[] args)
        {
            if (L != IntPtr.Zero)
            {
                if (L.isfunction(StackPos))
                {
                    L.pushvalue(StackPos);
                    return L.PushArgsAndCall(args);
                }
                else
                {
                    DynamicHelper.LogInfo("luafunc : the index is not a func.");
                }
            }
            else
            {
                DynamicHelper.LogInfo("luafunc : null state.");
            }
            return null;
        }
        public override string ToString()
        {
            return "LuaFuncOnStack:" + StackPos.ToString();
        }

        public static explicit operator lua.CFunction(LuaOnStackFunc val)
        {
            if (val != null && val.L != IntPtr.Zero)
            {
                if (val.L.iscfunction(val.StackPos))
                {
                    return val.L.tocfunction(val.StackPos);
                }
            }
            return null;
        }
    }

    public class LuaFunc : BaseLua
    {
        public LuaFunc(IntPtr l, int stackpos)
        {
            L = l;
            if (l != IntPtr.Zero)
            {
                if (l.isfunction(stackpos))
                {
                    l.pushvalue(stackpos);
                    Refid = l.refer();
                }
            }
        }
        protected internal LuaFunc()
        { }
        public override object[] Call(params object[] args)
        {
            if (L != IntPtr.Zero)
            {
                if (Refid != 0)
                {
                    L.getref(Refid);
                    return L.PushArgsAndCall(args);
                }
                else
                {
                    DynamicHelper.LogInfo("luafunc : null ref");
                }
            }
            else
            {
                DynamicHelper.LogInfo("luafunc : null state.");
            }
            return null;
        }
        public override string ToString()
        {
            return "LuaFunc:" + Refid.ToString();
        }
        public static implicit operator LuaFunc(LuaOnStackFunc val)
        {
            if (val != null && val.L != IntPtr.Zero)
                return new LuaFunc(val.L, val.StackPos);
            return null;
        }
        public static implicit operator LuaOnStackFunc(LuaFunc val)
        {
            if (val != null && val.L != IntPtr.Zero && val.Refid != 0)
            {
                val.L.getref(val.Refid);
                return new LuaOnStackFunc(val.L, val.L.gettop());
            }
            return null;
        }
        public static explicit operator lua.CFunction(LuaFunc val)
        {
            if (val != null && val.L != IntPtr.Zero && val.Refid != 0)
            {
                val.L.getref(val.Refid);
                lua.CFunction cfunc = null;
                if (val.L.iscfunction(-1))
                {
                    cfunc = val.L.tocfunction(-1);
                }
                val.L.pop(1);
                return cfunc;
            }
            return null;
        }
    }

    public static class LuaFuncHelper
    {
        public static int PushArgsAndCallRaw(this IntPtr l, params object[] args)
        {
            if (l != IntPtr.Zero)
            {
                var oldtop = l.gettop();
                l.pushcfunction(LuaHub.LuaFuncOnError);
                l.insert(oldtop);
                if (args != null)
                {
                    foreach (var arg in args)
                    {
                        l.PushLua(arg);
                    }
                }
                var argc = args == null ? 0 : args.Length;
                var lrr = new LuaRunningStateRecorder(l);
                var code = l.pcall(argc, lua.LUA_MULTRET, oldtop);
                lrr.Dispose();
                if (l.gettop() >= oldtop)
                {
                    l.remove(oldtop);
                }
                return code;
            }
            return lua.LUA_ERRERR;
        }
        public static object[] PushArgsAndCall(this IntPtr l, params object[] args)
        {
            if (l != IntPtr.Zero)
            {
                var oldtop = l.gettop() - 1;
                var code = PushArgsAndCallRaw(l, args);
                var newtop = l.gettop();
                object[] rv = null;
                if (code == 0 && newtop >= oldtop)
                {
                    rv = ObjectPool.GetReturnValueFromPool(newtop - oldtop);
                    for (int i = 0; i < (newtop - oldtop); ++i)
                    {
                        rv[i] = l.GetLua(i + oldtop + 1);
                    }
                }
                if (code != 0)
                {
                    DynamicHelper.LogError(l.GetLua(-1));
                }
                if (newtop >= oldtop)
                {
                    l.pop(newtop - oldtop);
                }
                return rv;
            }
            return null;
        }

        internal static int CallInternal(IntPtr l, int oldtop)
        {
            int pcnt = l.gettop() - oldtop; // func, args(*pcnt)
            l.pushcfunction(LuaHub.LuaFuncOnError); // func, args(*pcnt), err
            l.insert(oldtop); // err, func, args(*pcnt)
            var lrr = new LuaRunningStateRecorder(l);
            var code = l.pcall(pcnt, lua.LUA_MULTRET, oldtop); // err, rv(*x)
            lrr.Dispose();
            l.remove(oldtop); // rv(*x)
            if (code != 0)
            {
                DynamicHelper.LogError(l.GetLua(-1));
            }
            return code;
        }
        internal static int CallInternalSingleReturn(IntPtr l, int oldtop)
        {
            int pcnt = l.gettop() - oldtop; // func, args(*pcnt)
            l.pushcfunction(LuaHub.LuaFuncOnError); // func, args(*pcnt), err
            l.insert(oldtop); // err, func, args(*pcnt)
            var lrr = new LuaRunningStateRecorder(l);
            var code = l.pcall(pcnt, 1, oldtop); // err, rv
            lrr.Dispose();
            l.remove(oldtop); // rv
            if (code != 0)
            {
                DynamicHelper.LogError(l.GetLua(-1));
            }
            return code;
        }
        public static int PushArgsAndCallRawSingleReturn(this IntPtr l)
        {
            var oldtop = l.gettop();
            return CallInternalSingleReturn(l, oldtop);
        }
        public static int PushArgsAndCallRawSingleReturn<P0>(this IntPtr l, P0 p0)
        {
            var oldtop = l.gettop();
            l.PushLua(p0);
            return CallInternalSingleReturn(l, oldtop);
        }
        public static int PushArgsAndCallRawSingleReturn<P0, P1>(this IntPtr l, P0 p0, P1 p1)
        {
            var oldtop = l.gettop();
            l.PushLua(p0);
            l.PushLua(p1);
            return CallInternalSingleReturn(l, oldtop);
        }
        public static int PushArgsAndCallRawSingleReturn<P0, P1, P2>(this IntPtr l, P0 p0, P1 p1, P2 p2)
        {
            var oldtop = l.gettop();
            l.PushLua(p0);
            l.PushLua(p1);
            l.PushLua(p2);
            return CallInternalSingleReturn(l, oldtop);
        }
        public static int PushArgsAndCallRawSingleReturn<P0, P1, P2, P3>(this IntPtr l, P0 p0, P1 p1, P2 p2, P3 p3)
        {
            var oldtop = l.gettop();
            l.PushLua(p0);
            l.PushLua(p1);
            l.PushLua(p2);
            l.PushLua(p3);
            return CallInternalSingleReturn(l, oldtop);
        }
        public static int PushArgsAndCallRawSingleReturn<P0, P1, P2, P3, P4>(this IntPtr l, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4)
        {
            var oldtop = l.gettop();
            l.PushLua(p0);
            l.PushLua(p1);
            l.PushLua(p2);
            l.PushLua(p3);
            l.PushLua(p4);
            return CallInternalSingleReturn(l, oldtop);
        }
        public static int PushArgsAndCallRawSingleReturn<P0, P1, P2, P3, P4, P5>(this IntPtr l, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5)
        {
            var oldtop = l.gettop();
            l.PushLua(p0);
            l.PushLua(p1);
            l.PushLua(p2);
            l.PushLua(p3);
            l.PushLua(p4);
            l.PushLua(p5);
            return CallInternalSingleReturn(l, oldtop);
        }
        public static int PushArgsAndCallRawSingleReturn<P0, P1, P2, P3, P4, P5, P6>(this IntPtr l, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6)
        {
            var oldtop = l.gettop();
            l.PushLua(p0);
            l.PushLua(p1);
            l.PushLua(p2);
            l.PushLua(p3);
            l.PushLua(p4);
            l.PushLua(p5);
            l.PushLua(p6);
            return CallInternalSingleReturn(l, oldtop);
        }
        public static int PushArgsAndCallRawSingleReturn<P0, P1, P2, P3, P4, P5, P6, P7>(this IntPtr l, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7)
        {
            var oldtop = l.gettop();
            l.PushLua(p0);
            l.PushLua(p1);
            l.PushLua(p2);
            l.PushLua(p3);
            l.PushLua(p4);
            l.PushLua(p5);
            l.PushLua(p6);
            l.PushLua(p7);
            return CallInternalSingleReturn(l, oldtop);
        }
        public static int PushArgsAndCallRawSingleReturn<P0, P1, P2, P3, P4, P5, P6, P7, P8>(this IntPtr l, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7, P8 p8)
        {
            var oldtop = l.gettop();
            l.PushLua(p0);
            l.PushLua(p1);
            l.PushLua(p2);
            l.PushLua(p3);
            l.PushLua(p4);
            l.PushLua(p5);
            l.PushLua(p6);
            l.PushLua(p7);
            l.PushLua(p8);
            return CallInternalSingleReturn(l, oldtop);
        }
        public static int PushArgsAndCallRawSingleReturn<P0, P1, P2, P3, P4, P5, P6, P7, P8, P9>(this IntPtr l, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7, P8 p8, P9 p9)
        {
            var oldtop = l.gettop();
            l.PushLua(p0);
            l.PushLua(p1);
            l.PushLua(p2);
            l.PushLua(p3);
            l.PushLua(p4);
            l.PushLua(p5);
            l.PushLua(p6);
            l.PushLua(p7);
            l.PushLua(p8);
            l.PushLua(p9);
            return CallInternalSingleReturn(l, oldtop);
        }
        public static int CallRawSingleReturn(this IntPtr l, int index, string func)
        {
            l.GetField(index, func);
            return l.PushArgsAndCallRawSingleReturn();
        }
        public static int CallRawSingleReturn<P0>(this IntPtr l, int index, string func, P0 p0)
        {
            l.GetField(index, func);
            return l.PushArgsAndCallRawSingleReturn(p0);
        }
        public static int CallRawSingleReturn<P0, P1>(this IntPtr l, int index, string func, P0 p0, P1 p1)
        {
            l.GetField(index, func);
            return l.PushArgsAndCallRawSingleReturn(p0, p1);
        }
        public static int CallRawSingleReturn<P0, P1, P2>(this IntPtr l, int index, string func, P0 p0, P1 p1, P2 p2)
        {
            l.GetField(index, func);
            return l.PushArgsAndCallRawSingleReturn(p0, p1, p2);
        }
        public static int CallRawSingleReturn<P0, P1, P2, P3>(this IntPtr l, int index, string func, P0 p0, P1 p1, P2 p2, P3 p3)
        {
            l.GetField(index, func);
            return l.PushArgsAndCallRawSingleReturn(p0, p1, p2, p3);
        }
        public static int CallRawSingleReturn<P0, P1, P2, P3, P4>(this IntPtr l, int index, string func, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4)
        {
            l.GetField(index, func);
            return l.PushArgsAndCallRawSingleReturn(p0, p1, p2, p3, p4);
        }
        public static int CallRawSingleReturn<P0, P1, P2, P3, P4, P5>(this IntPtr l, int index, string func, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5)
        {
            l.GetField(index, func);
            return l.PushArgsAndCallRawSingleReturn(p0, p1, p2, p3, p4, p5);
        }
        public static int CallRawSingleReturn<P0, P1, P2, P3, P4, P5, P6>(this IntPtr l, int index, string func, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6)
        {
            l.GetField(index, func);
            return l.PushArgsAndCallRawSingleReturn(p0, p1, p2, p3, p4, p5, p6);
        }
        public static int CallRawSingleReturn<P0, P1, P2, P3, P4, P5, P6, P7>(this IntPtr l, int index, string func, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7)
        {
            l.GetField(index, func);
            return l.PushArgsAndCallRawSingleReturn(p0, p1, p2, p3, p4, p5, p6, p7);
        }
        public static int CallRawSingleReturn<P0, P1, P2, P3, P4, P5, P6, P7, P8>(this IntPtr l, int index, string func, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7, P8 p8)
        {
            l.GetField(index, func);
            return l.PushArgsAndCallRawSingleReturn(p0, p1, p2, p3, p4, p5, p6, p7, p8);
        }
        public static int CallRawSingleReturn<P0, P1, P2, P3, P4, P5, P6, P7, P8, P9>(this IntPtr l, int index, string func, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7, P8 p8, P9 p9)
        {
            l.GetField(index, func);
            return l.PushArgsAndCallRawSingleReturn(p0, p1, p2, p3, p4, p5, p6, p7, p8, p9);
        }
        public static int PushArgsAndCallRaw(this IntPtr l)
        {
            var oldtop = l.gettop();
            return CallInternal(l, oldtop);
        }
        public static int PushArgsAndCallRaw<P0>(this IntPtr l, P0 p0)
        {
            var oldtop = l.gettop();
            l.PushLua(p0);
            return CallInternal(l, oldtop);
        }
        public static int PushArgsAndCallRaw<P0, P1>(this IntPtr l, P0 p0, P1 p1)
        {
            var oldtop = l.gettop();
            l.PushLua(p0);
            l.PushLua(p1);
            return CallInternal(l, oldtop);
        }
        public static int PushArgsAndCallRaw<P0, P1, P2>(this IntPtr l, P0 p0, P1 p1, P2 p2)
        {
            var oldtop = l.gettop();
            l.PushLua(p0);
            l.PushLua(p1);
            l.PushLua(p2);
            return CallInternal(l, oldtop);
        }
        public static int PushArgsAndCallRaw<P0, P1, P2, P3>(this IntPtr l, P0 p0, P1 p1, P2 p2, P3 p3)
        {
            var oldtop = l.gettop();
            l.PushLua(p0);
            l.PushLua(p1);
            l.PushLua(p2);
            l.PushLua(p3);
            return CallInternal(l, oldtop);
        }
        public static int PushArgsAndCallRaw<P0, P1, P2, P3, P4>(this IntPtr l, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4)
        {
            var oldtop = l.gettop();
            l.PushLua(p0);
            l.PushLua(p1);
            l.PushLua(p2);
            l.PushLua(p3);
            l.PushLua(p4);
            return CallInternal(l, oldtop);
        }
        public static int PushArgsAndCallRaw<P0, P1, P2, P3, P4, P5>(this IntPtr l, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5)
        {
            var oldtop = l.gettop();
            l.PushLua(p0);
            l.PushLua(p1);
            l.PushLua(p2);
            l.PushLua(p3);
            l.PushLua(p4);
            l.PushLua(p5);
            return CallInternal(l, oldtop);
        }
        public static int PushArgsAndCallRaw<P0, P1, P2, P3, P4, P5, P6>(this IntPtr l, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6)
        {
            var oldtop = l.gettop();
            l.PushLua(p0);
            l.PushLua(p1);
            l.PushLua(p2);
            l.PushLua(p3);
            l.PushLua(p4);
            l.PushLua(p5);
            l.PushLua(p6);
            return CallInternal(l, oldtop);
        }
        public static int PushArgsAndCallRaw<P0, P1, P2, P3, P4, P5, P6, P7>(this IntPtr l, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7)
        {
            var oldtop = l.gettop();
            l.PushLua(p0);
            l.PushLua(p1);
            l.PushLua(p2);
            l.PushLua(p3);
            l.PushLua(p4);
            l.PushLua(p5);
            l.PushLua(p6);
            l.PushLua(p7);
            return CallInternal(l, oldtop);
        }
        public static int PushArgsAndCallRaw<P0, P1, P2, P3, P4, P5, P6, P7, P8>(this IntPtr l, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7, P8 p8)
        {
            var oldtop = l.gettop();
            l.PushLua(p0);
            l.PushLua(p1);
            l.PushLua(p2);
            l.PushLua(p3);
            l.PushLua(p4);
            l.PushLua(p5);
            l.PushLua(p6);
            l.PushLua(p7);
            l.PushLua(p8);
            return CallInternal(l, oldtop);
        }
        public static int PushArgsAndCallRaw<P0, P1, P2, P3, P4, P5, P6, P7, P8, P9>(this IntPtr l, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7, P8 p8, P9 p9)
        {
            var oldtop = l.gettop();
            l.PushLua(p0);
            l.PushLua(p1);
            l.PushLua(p2);
            l.PushLua(p3);
            l.PushLua(p4);
            l.PushLua(p5);
            l.PushLua(p6);
            l.PushLua(p7);
            l.PushLua(p8);
            l.PushLua(p9);
            return CallInternal(l, oldtop);
        }
        public static int CallRaw(this IntPtr l, int index, string func)
        {
            var oldtop = l.gettop();
            l.GetField(index, func);
            var code = CallInternal(l, oldtop + 1);
            if (code != 0)
            {
                l.pop(1);
            }
            return l.gettop() - oldtop;
        }
        public static int CallRaw<P0>(this IntPtr l, int index, string func, P0 p0)
        {
            var oldtop = l.gettop();
            l.GetField(index, func);
            l.PushLua(p0);
            var code = CallInternal(l, oldtop + 1);
            if (code != 0)
            {
                l.pop(1);
            }
            return l.gettop() - oldtop;
        }
        public static int CallRaw<P0, P1>(this IntPtr l, int index, string func, P0 p0, P1 p1)
        {
            var oldtop = l.gettop();
            l.GetField(index, func);
            l.PushLua(p0);
            l.PushLua(p1);
            var code = CallInternal(l, oldtop + 1);
            if (code != 0)
            {
                l.pop(1);
            }
            return l.gettop() - oldtop;
        }
        public static int CallRaw<P0, P1, P2>(this IntPtr l, int index, string func, P0 p0, P1 p1, P2 p2)
        {
            var oldtop = l.gettop();
            l.GetField(index, func);
            l.PushLua(p0);
            l.PushLua(p1);
            l.PushLua(p2);
            var code = CallInternal(l, oldtop + 1);
            if (code != 0)
            {
                l.pop(1);
            }
            return l.gettop() - oldtop;
        }
        public static int CallRaw<P0, P1, P2, P3>(this IntPtr l, int index, string func, P0 p0, P1 p1, P2 p2, P3 p3)
        {
            var oldtop = l.gettop();
            l.GetField(index, func);
            l.PushLua(p0);
            l.PushLua(p1);
            l.PushLua(p2);
            l.PushLua(p3);
            var code = CallInternal(l, oldtop + 1);
            if (code != 0)
            {
                l.pop(1);
            }
            return l.gettop() - oldtop;
        }
        public static int CallRaw<P0, P1, P2, P3, P4>(this IntPtr l, int index, string func, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4)
        {
            var oldtop = l.gettop();
            l.GetField(index, func);
            l.PushLua(p0);
            l.PushLua(p1);
            l.PushLua(p2);
            l.PushLua(p3);
            l.PushLua(p4);
            var code = CallInternal(l, oldtop + 1);
            if (code != 0)
            {
                l.pop(1);
            }
            return l.gettop() - oldtop;
        }
        public static int CallRaw<P0, P1, P2, P3, P4, P5>(this IntPtr l, int index, string func, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5)
        {
            var oldtop = l.gettop();
            l.GetField(index, func);
            l.PushLua(p0);
            l.PushLua(p1);
            l.PushLua(p2);
            l.PushLua(p3);
            l.PushLua(p4);
            l.PushLua(p5);
            var code = CallInternal(l, oldtop + 1);
            if (code != 0)
            {
                l.pop(1);
            }
            return l.gettop() - oldtop;
        }
        public static int CallRaw<P0, P1, P2, P3, P4, P5, P6>(this IntPtr l, int index, string func, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6)
        {
            var oldtop = l.gettop();
            l.GetField(index, func);
            l.PushLua(p0);
            l.PushLua(p1);
            l.PushLua(p2);
            l.PushLua(p3);
            l.PushLua(p4);
            l.PushLua(p5);
            l.PushLua(p6);
            var code = CallInternal(l, oldtop + 1);
            if (code != 0)
            {
                l.pop(1);
            }
            return l.gettop() - oldtop;
        }
        public static int CallRaw<P0, P1, P2, P3, P4, P5, P6, P7>(this IntPtr l, int index, string func, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7)
        {
            var oldtop = l.gettop();
            l.GetField(index, func);
            l.PushLua(p0);
            l.PushLua(p1);
            l.PushLua(p2);
            l.PushLua(p3);
            l.PushLua(p4);
            l.PushLua(p5);
            l.PushLua(p6);
            l.PushLua(p7);
            var code = CallInternal(l, oldtop + 1);
            if (code != 0)
            {
                l.pop(1);
            }
            return l.gettop() - oldtop;
        }
        public static int CallRaw<P0, P1, P2, P3, P4, P5, P6, P7, P8>(this IntPtr l, int index, string func, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7, P8 p8)
        {
            var oldtop = l.gettop();
            l.GetField(index, func);
            l.PushLua(p0);
            l.PushLua(p1);
            l.PushLua(p2);
            l.PushLua(p3);
            l.PushLua(p4);
            l.PushLua(p5);
            l.PushLua(p6);
            l.PushLua(p7);
            l.PushLua(p8);
            var code = CallInternal(l, oldtop + 1);
            if (code != 0)
            {
                l.pop(1);
            }
            return l.gettop() - oldtop;
        }
        public static int CallRaw<P0, P1, P2, P3, P4, P5, P6, P7, P8, P9>(this IntPtr l, int index, string func, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7, P8 p8, P9 p9)
        {
            var oldtop = l.gettop();
            l.GetField(index, func);
            l.PushLua(p0);
            l.PushLua(p1);
            l.PushLua(p2);
            l.PushLua(p3);
            l.PushLua(p4);
            l.PushLua(p5);
            l.PushLua(p6);
            l.PushLua(p7);
            l.PushLua(p8);
            l.PushLua(p9);
            var code = CallInternal(l, oldtop + 1);
            if (code != 0)
            {
                l.pop(1);
            }
            return l.gettop() - oldtop;
        }
        public static void PushArgsAndCall(this IntPtr l)
        {
            var oldtop = l.gettop();
            CallInternal(l, oldtop);
            l.settop(oldtop - 1);
        }
        public static void PushArgsAndCall<P0>(this IntPtr l, P0 p0)
        {
            var oldtop = l.gettop();
            l.PushLua(p0);
            CallInternal(l, oldtop);
            l.settop(oldtop - 1);
        }
        public static void PushArgsAndCall<P0, P1>(this IntPtr l, P0 p0, P1 p1)
        {
            var oldtop = l.gettop();
            l.PushLua(p0);
            l.PushLua(p1);
            CallInternal(l, oldtop);
            l.settop(oldtop - 1);
        }
        public static void PushArgsAndCall<P0, P1, P2>(this IntPtr l, P0 p0, P1 p1, P2 p2)
        {
            var oldtop = l.gettop();
            l.PushLua(p0);
            l.PushLua(p1);
            l.PushLua(p2);
            CallInternal(l, oldtop);
            l.settop(oldtop - 1);
        }
        public static void PushArgsAndCall<P0, P1, P2, P3>(this IntPtr l, P0 p0, P1 p1, P2 p2, P3 p3)
        {
            var oldtop = l.gettop();
            l.PushLua(p0);
            l.PushLua(p1);
            l.PushLua(p2);
            l.PushLua(p3);
            CallInternal(l, oldtop);
            l.settop(oldtop - 1);
        }
        public static void PushArgsAndCall<P0, P1, P2, P3, P4>(this IntPtr l, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4)
        {
            var oldtop = l.gettop();
            l.PushLua(p0);
            l.PushLua(p1);
            l.PushLua(p2);
            l.PushLua(p3);
            l.PushLua(p4);
            CallInternal(l, oldtop);
            l.settop(oldtop - 1);
        }
        public static void PushArgsAndCall<P0, P1, P2, P3, P4, P5>(this IntPtr l, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5)
        {
            var oldtop = l.gettop();
            l.PushLua(p0);
            l.PushLua(p1);
            l.PushLua(p2);
            l.PushLua(p3);
            l.PushLua(p4);
            l.PushLua(p5);
            CallInternal(l, oldtop);
            l.settop(oldtop - 1);
        }
        public static void PushArgsAndCall<P0, P1, P2, P3, P4, P5, P6>(this IntPtr l, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6)
        {
            var oldtop = l.gettop();
            l.PushLua(p0);
            l.PushLua(p1);
            l.PushLua(p2);
            l.PushLua(p3);
            l.PushLua(p4);
            l.PushLua(p5);
            l.PushLua(p6);
            CallInternal(l, oldtop);
            l.settop(oldtop - 1);
        }
        public static void PushArgsAndCall<P0, P1, P2, P3, P4, P5, P6, P7>(this IntPtr l, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7)
        {
            var oldtop = l.gettop();
            l.PushLua(p0);
            l.PushLua(p1);
            l.PushLua(p2);
            l.PushLua(p3);
            l.PushLua(p4);
            l.PushLua(p5);
            l.PushLua(p6);
            l.PushLua(p7);
            CallInternal(l, oldtop);
            l.settop(oldtop - 1);
        }
        public static void PushArgsAndCall<P0, P1, P2, P3, P4, P5, P6, P7, P8>(this IntPtr l, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7, P8 p8)
        {
            var oldtop = l.gettop();
            l.PushLua(p0);
            l.PushLua(p1);
            l.PushLua(p2);
            l.PushLua(p3);
            l.PushLua(p4);
            l.PushLua(p5);
            l.PushLua(p6);
            l.PushLua(p7);
            l.PushLua(p8);
            CallInternal(l, oldtop);
            l.settop(oldtop - 1);
        }
        public static void PushArgsAndCall<P0, P1, P2, P3, P4, P5, P6, P7, P8, P9>(this IntPtr l, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7, P8 p8, P9 p9)
        {
            var oldtop = l.gettop();
            l.PushLua(p0);
            l.PushLua(p1);
            l.PushLua(p2);
            l.PushLua(p3);
            l.PushLua(p4);
            l.PushLua(p5);
            l.PushLua(p6);
            l.PushLua(p7);
            l.PushLua(p8);
            l.PushLua(p9);
            CallInternal(l, oldtop);
            l.settop(oldtop - 1);
        }
        public static bool PushArgsAndCall<R>(this IntPtr l, out R r)
        {
            var oldtop = l.gettop() - 1;
            var code = CallInternal(l, oldtop + 1);
            if (code == 0 && l.gettop() > oldtop)
            {
                l.GetLua(oldtop + 1, out r);
                if (typeof(R).IsOnStack())
                {
                    l.settop(oldtop + 1);
                }
                else
                {
                    l.settop(oldtop);
                }
                return true;
            }
            r = default(R);
            l.settop(oldtop);
            return false;
        }
        public static bool PushArgsAndCall<R, P0>(this IntPtr l, out R r, P0 p0)
        {
            var oldtop = l.gettop() - 1;
            l.PushLua(p0);
            var code = CallInternal(l, oldtop + 1);
            if (code == 0 && l.gettop() > oldtop)
            {
                l.GetLua(oldtop + 1, out r);
                if (typeof(R).IsOnStack())
                {
                    l.settop(oldtop + 1);
                }
                else
                {
                    l.settop(oldtop);
                }
                return true;
            }
            r = default(R);
            l.settop(oldtop);
            return false;
        }
        public static bool PushArgsAndCall<R, P0, P1>(this IntPtr l, out R r, P0 p0, P1 p1)
        {
            var oldtop = l.gettop() - 1;
            l.PushLua(p0);
            l.PushLua(p1);
            var code = CallInternal(l, oldtop + 1);
            if (code == 0 && l.gettop() > oldtop)
            {
                l.GetLua(oldtop + 1, out r);
                if (typeof(R).IsOnStack())
                {
                    l.settop(oldtop + 1);
                }
                else
                {
                    l.settop(oldtop);
                }
                return true;
            }
            r = default(R);
            l.settop(oldtop);
            return false;
        }
        public static bool PushArgsAndCall<R, P0, P1, P2>(this IntPtr l, out R r, P0 p0, P1 p1, P2 p2)
        {
            var oldtop = l.gettop() - 1;
            l.PushLua(p0);
            l.PushLua(p1);
            l.PushLua(p2);
            var code = CallInternal(l, oldtop + 1);
            if (code == 0 && l.gettop() > oldtop)
            {
                l.GetLua(oldtop + 1, out r);
                if (typeof(R).IsOnStack())
                {
                    l.settop(oldtop + 1);
                }
                else
                {
                    l.settop(oldtop);
                }
                return true;
            }
            r = default(R);
            l.settop(oldtop);
            return false;
        }
        public static bool PushArgsAndCall<R, P0, P1, P2, P3>(this IntPtr l, out R r, P0 p0, P1 p1, P2 p2, P3 p3)
        {
            var oldtop = l.gettop() - 1;
            l.PushLua(p0);
            l.PushLua(p1);
            l.PushLua(p2);
            l.PushLua(p3);
            var code = CallInternal(l, oldtop + 1);
            if (code == 0 && l.gettop() > oldtop)
            {
                l.GetLua(oldtop + 1, out r);
                if (typeof(R).IsOnStack())
                {
                    l.settop(oldtop + 1);
                }
                else
                {
                    l.settop(oldtop);
                }
                return true;
            }
            r = default(R);
            l.settop(oldtop);
            return false;
        }
        public static bool PushArgsAndCall<R, P0, P1, P2, P3, P4>(this IntPtr l, out R r, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4)
        {
            var oldtop = l.gettop() - 1;
            l.PushLua(p0);
            l.PushLua(p1);
            l.PushLua(p2);
            l.PushLua(p3);
            l.PushLua(p4);
            var code = CallInternal(l, oldtop + 1);
            if (code == 0 && l.gettop() > oldtop)
            {
                l.GetLua(oldtop + 1, out r);
                if (typeof(R).IsOnStack())
                {
                    l.settop(oldtop + 1);
                }
                else
                {
                    l.settop(oldtop);
                }
                return true;
            }
            r = default(R);
            l.settop(oldtop);
            return false;
        }
        public static bool PushArgsAndCall<R, P0, P1, P2, P3, P4, P5>(this IntPtr l, out R r, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5)
        {
            var oldtop = l.gettop() - 1;
            l.PushLua(p0);
            l.PushLua(p1);
            l.PushLua(p2);
            l.PushLua(p3);
            l.PushLua(p4);
            l.PushLua(p5);
            var code = CallInternal(l, oldtop + 1);
            if (code == 0 && l.gettop() > oldtop)
            {
                l.GetLua(oldtop + 1, out r);
                if (typeof(R).IsOnStack())
                {
                    l.settop(oldtop + 1);
                }
                else
                {
                    l.settop(oldtop);
                }
                return true;
            }
            r = default(R);
            l.settop(oldtop);
            return false;
        }
        public static bool PushArgsAndCall<R, P0, P1, P2, P3, P4, P5, P6>(this IntPtr l, out R r, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6)
        {
            var oldtop = l.gettop() - 1;
            l.PushLua(p0);
            l.PushLua(p1);
            l.PushLua(p2);
            l.PushLua(p3);
            l.PushLua(p4);
            l.PushLua(p5);
            l.PushLua(p6);
            var code = CallInternal(l, oldtop + 1);
            if (code == 0 && l.gettop() > oldtop)
            {
                l.GetLua(oldtop + 1, out r);
                if (typeof(R).IsOnStack())
                {
                    l.settop(oldtop + 1);
                }
                else
                {
                    l.settop(oldtop);
                }
                return true;
            }
            r = default(R);
            l.settop(oldtop);
            return false;
        }
        public static bool PushArgsAndCall<R, P0, P1, P2, P3, P4, P5, P6, P7>(this IntPtr l, out R r, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7)
        {
            var oldtop = l.gettop() - 1;
            l.PushLua(p0);
            l.PushLua(p1);
            l.PushLua(p2);
            l.PushLua(p3);
            l.PushLua(p4);
            l.PushLua(p5);
            l.PushLua(p6);
            l.PushLua(p7);
            var code = CallInternal(l, oldtop + 1);
            if (code == 0 && l.gettop() > oldtop)
            {
                l.GetLua(oldtop + 1, out r);
                if (typeof(R).IsOnStack())
                {
                    l.settop(oldtop + 1);
                }
                else
                {
                    l.settop(oldtop);
                }
                return true;
            }
            r = default(R);
            l.settop(oldtop);
            return false;
        }
        public static bool PushArgsAndCall<R, P0, P1, P2, P3, P4, P5, P6, P7, P8>(this IntPtr l, out R r, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7, P8 p8)
        {
            var oldtop = l.gettop() - 1;
            l.PushLua(p0);
            l.PushLua(p1);
            l.PushLua(p2);
            l.PushLua(p3);
            l.PushLua(p4);
            l.PushLua(p5);
            l.PushLua(p6);
            l.PushLua(p7);
            l.PushLua(p8);
            var code = CallInternal(l, oldtop + 1);
            if (code == 0 && l.gettop() > oldtop)
            {
                l.GetLua(oldtop + 1, out r);
                if (typeof(R).IsOnStack())
                {
                    l.settop(oldtop + 1);
                }
                else
                {
                    l.settop(oldtop);
                }
                return true;
            }
            r = default(R);
            l.settop(oldtop);
            return false;
        }
        public static bool PushArgsAndCall<R, P0, P1, P2, P3, P4, P5, P6, P7, P8, P9>(this IntPtr l, out R r, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7, P8 p8, P9 p9)
        {
            var oldtop = l.gettop() - 1;
            l.PushLua(p0);
            l.PushLua(p1);
            l.PushLua(p2);
            l.PushLua(p3);
            l.PushLua(p4);
            l.PushLua(p5);
            l.PushLua(p6);
            l.PushLua(p7);
            l.PushLua(p8);
            l.PushLua(p9);
            var code = CallInternal(l, oldtop + 1);
            if (code == 0 && l.gettop() > oldtop)
            {
                l.GetLua(oldtop + 1, out r);
                if (typeof(R).IsOnStack())
                {
                    l.settop(oldtop + 1);
                }
                else
                {
                    l.settop(oldtop);
                }
                return true;
            }
            r = default(R);
            l.settop(oldtop);
            return false;
        }
        public static R PushArgsAndCall<R>(this IntPtr l)
        {
            R r;
            PushArgsAndCall(l, out r);
            return r;
        }
        public static R PushArgsAndCall<R, P0>(this IntPtr l, P0 p0)
        {
            R r;
            PushArgsAndCall(l, out r, p0);
            return r;
        }
        public static R PushArgsAndCall<R, P0, P1>(this IntPtr l, P0 p0, P1 p1)
        {
            R r;
            PushArgsAndCall(l, out r, p0, p1);
            return r;
        }
        public static R PushArgsAndCall<R, P0, P1, P2>(this IntPtr l, P0 p0, P1 p1, P2 p2)
        {
            R r;
            PushArgsAndCall(l, out r, p0, p1, p2);
            return r;
        }
        public static R PushArgsAndCall<R, P0, P1, P2, P3>(this IntPtr l, P0 p0, P1 p1, P2 p2, P3 p3)
        {
            R r;
            PushArgsAndCall(l, out r, p0, p1, p2, p3);
            return r;
        }
        public static R PushArgsAndCall<R, P0, P1, P2, P3, P4>(this IntPtr l, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4)
        {
            R r;
            PushArgsAndCall(l, out r, p0, p1, p2, p3, p4);
            return r;
        }
        public static R PushArgsAndCall<R, P0, P1, P2, P3, P4, P5>(this IntPtr l, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5)
        {
            R r;
            PushArgsAndCall(l, out r, p0, p1, p2, p3, p4, p5);
            return r;
        }
        public static R PushArgsAndCall<R, P0, P1, P2, P3, P4, P5, P6>(this IntPtr l, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6)
        {
            R r;
            PushArgsAndCall(l, out r, p0, p1, p2, p3, p4, p5, p6);
            return r;
        }
        public static R PushArgsAndCall<R, P0, P1, P2, P3, P4, P5, P6, P7>(this IntPtr l, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7)
        {
            R r;
            PushArgsAndCall(l, out r, p0, p1, p2, p3, p4, p5, p6, p7);
            return r;
        }
        public static R PushArgsAndCall<R, P0, P1, P2, P3, P4, P5, P6, P7, P8>(this IntPtr l, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7, P8 p8)
        {
            R r;
            PushArgsAndCall(l, out r, p0, p1, p2, p3, p4, p5, p6, p7, p8);
            return r;
        }
        public static R PushArgsAndCall<R, P0, P1, P2, P3, P4, P5, P6, P7, P8, P9>(this IntPtr l, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7, P8 p8, P9 p9)
        {
            R r;
            PushArgsAndCall(l, out r, p0, p1, p2, p3, p4, p5, p6, p7, p8, p9);
            return r;
        }
    }
}

namespace LuaLib
{
    public static partial class LuaHub
    {
        public static void PushLua(this IntPtr l, LuaLib.LuaOnStackFunc val)
        {
            l.pushvalue(val.StackPos);
        }
        public static void PushLua(this IntPtr l, LuaLib.LuaFunc val)
        {
            l.getref(val.Refid);
        }
        public static void GetLua(this IntPtr l, int index, out LuaLib.LuaFunc val)
        {
            if (l.isfunction(index))
            {
                val = new LuaLib.LuaFunc(l, index);
                return;
            }
            val = null;
            return;
        }
        public static void GetLua(this IntPtr l, int index, out LuaLib.LuaOnStackFunc val)
        {
            if (l.isfunction(index))
            {
                val = new LuaLib.LuaOnStackFunc(l, index);
                return;
            }
            val = null;
            return;
        }

        private class LuaPushNative_LuaOnStackFunc : LuaPushNativeBase<LuaLib.LuaOnStackFunc>
        {
            public override LuaOnStackFunc GetLua(IntPtr l, int index)
            {
                return new LuaLib.LuaOnStackFunc(l, index);
            }
            public override IntPtr PushLua(IntPtr l, LuaLib.LuaOnStackFunc val)
            {
                l.pushvalue(val.StackPos);
                return IntPtr.Zero;
            }
        }
        private static LuaPushNative_LuaOnStackFunc ___tpn_LuaOnStackFunc = new LuaPushNative_LuaOnStackFunc();
        private class LuaPushNative_LuaFunc : LuaPushNativeBase<LuaLib.LuaFunc>
        {
            public override LuaFunc GetLua(IntPtr l, int index)
            {
                return new LuaLib.LuaFunc(l, index);
            }
            public override IntPtr PushLua(IntPtr l, LuaLib.LuaFunc val)
            {
                l.getref(val.Refid);
                return IntPtr.Zero;
            }
        }
        private static LuaPushNative_LuaFunc ___tpn_LuaFunc = new LuaPushNative_LuaFunc();
    }
}