using System;

using LuaLib;
using lua = LuaLib.LuaCoreLib;
using lual = LuaLib.LuaAuxLib;
using luae = LuaLib.LuaLibEx;

namespace LuaLib
{
    public struct LuaStackPos
    {
        //private IntPtr _L;
        //public IntPtr L
        //{
        //    get { return _L; }
        //    internal set { _L = value; }
        //}
        private int _Pos;
        public int Pos
        {
            get { return _Pos; }
            internal set { _Pos = value; }
        }
        public bool IsValid { get { return _Pos != 0; } }
        public bool IsAbsolute { get { return _Pos > 0; } }

        public static implicit operator int(LuaStackPos stackpos)
        {
            return stackpos.Pos;
        }
        public static readonly LuaStackPos Top = new LuaStackPos { Pos = -1 };
    }

    public static partial class LuaHub
    {
        public static LuaStackPos OnStack(this IntPtr l, int pos)
        {
            return new LuaStackPos() { Pos = l.NormalizeIndex(pos) };
        }
        public static LuaStackPos OnStackTop(this IntPtr l)
        {
            return l.OnStack(-1);
        }

        public static void PushLua(this IntPtr l, LuaStackPos val)
        {
            l.pushvalue(val.Pos);
        }
        public static void GetLua(this IntPtr l, int pos, out LuaStackPos val)
        {
            val = l.OnStack(pos);
        }
        public static void PushLua(this IntPtr l, LuaStackPos? val)
        {
            if (val == null)
            {
                l.pushnil();
            }
            else
            {
                l.pushvalue(val.Value.Pos);
            }
        }
        public static void GetLua(this IntPtr l, int pos, out LuaStackPos? val)
        {
            if (l.isnoneornil(pos))
            {
                val = null;
            }
            else
            {
                val = l.OnStack(pos);
            }
        }

        private class LuaPushNative_LuaStackPos : LuaPushNativeValueType<LuaStackPos>
        {
            public override LuaStackPos GetLua(IntPtr l, int index)
            {
                return l.OnStack(index);
            }
            public override IntPtr PushLua(IntPtr l, LuaStackPos val)
            {
                l.pushvalue(val.Pos);
                return IntPtr.Zero;
            }
        }
        private static LuaPushNative_LuaStackPos ___tpn_LuaStackPos = new LuaPushNative_LuaStackPos();
    }
}