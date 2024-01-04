using System;
using System.Collections.Generic;
using System.Reflection;

using lua = LuaLib.LuaCoreLib;
using lual = LuaLib.LuaAuxLib;
using luae = LuaLib.LuaLibEx;

namespace LuaLib
{
    public interface ILuaNative
    {
        void Wrap(IntPtr l, int index);
        void Unwrap(IntPtr l, int index);
        int LuaType { get; }
    }

    public static partial class LuaHub
    {
        public static class LuaPushNativeLongNumberCache
        {
            [ThreadStatic] internal static decimal DecimalCache;
            [ThreadStatic] internal static ulong Int64Cache;
            [ThreadStatic] internal static bool SafeMode;
        }
        public abstract class LuaPushNative
        {
            protected internal static Dictionary<Type, object> _NativePushLuaFuncs = new Dictionary<Type, object>();
        }
        public abstract class LuaPushNativeBase<T> : LuaPushNative, ILuaPush<T>, ILuaTrans<T>, ILuaPush
        {
            public LuaPushNativeBase()
            {
                _NativePushLuaFuncs[typeof(T)] = this;
            }

            public void SetData(IntPtr l, int index, T val)
            {
                // no meaning.
            }
            public abstract T GetLua(IntPtr l, int index);
            public abstract IntPtr PushLua(IntPtr l, T val);

            public bool ShouldCache { get { return false; } }
            public IntPtr PushLua(IntPtr l, object val)
            {
                if (val is T)
                {
                    return PushLua(l, (T)val);
                }
                else
                {
                    l.pushnil();
                    return IntPtr.Zero;
                }
            }
            public bool PushFromCache(IntPtr l, object val)
            {
                return false;
            }
        }
        public abstract class LuaPushNativeValueType<T> : LuaPushNativeBase<T>, ILuaPush<T?>, ILuaTrans<T?> where T : struct
        {
            public LuaPushNativeValueType()
            {
                _NativePushLuaFuncs[typeof(T?)] = this;
            }

            IntPtr ILuaPush<T?>.PushLua(IntPtr l, T? val)
            {
                if (val == null)
                {
                    l.pushnil();
                }
                else
                {
                    PushLua(l, (T)val);
                }
                return IntPtr.Zero;
            }
            void ILuaTrans<T?>.SetData(IntPtr l, int index, T? val)
            {
                // no meaning.
            }
            T? ILuaTrans<T?>.GetLua(IntPtr l, int index)
            {
                if (l.isnoneornil(index))
                {
                    return null;
                }
                else
                {
                    return GetLua(l, index);
                }
            }
        }
        private class LuaPushNative_bool : LuaPushNativeValueType<bool>
        {
            public override bool GetLua(IntPtr l, int index)
            {
                if (l.isboolean(index))
                {
                    return l.toboolean(index);
                }
                else if (l.IsNumber(index))
                {
                    return l.tonumber(index) != 0.0;
                }
                return false;
            }
            public override IntPtr PushLua(IntPtr l, bool val)
            {
                l.pushboolean(val);
                return IntPtr.Zero;
            }
        }
        private static LuaPushNative_bool ___tpn_bool = new LuaPushNative_bool();
        private class LuaPushNative_byte : LuaPushNativeValueType<byte>
        {
            public override byte GetLua(IntPtr l, int index)
            {
                if (l.IsNumber(index))
                {
                    return (byte)l.tonumber(index);
                }
                else
                {
                    return 0;
                }
            }
            public override IntPtr PushLua(IntPtr l, byte val)
            {
                l.pushnumber(val);
                return IntPtr.Zero;
            }
        }
        private static LuaPushNative_byte ___tpn_byte = new LuaPushNative_byte();
        private class LuaPushNative_bytes : LuaPushNativeBase<byte[]>
        {
            public override byte[] GetLua(IntPtr l, int index)
            {
                if (l.IsString(index))
                {
                    return l.tolstring(index);
                }
                else
                {
                    return null;
                }
            }
            public override IntPtr PushLua(IntPtr l, byte[] val)
            {
                //if (val == null)
                //    l.pushnil();
                //else
                    l.pushbuffer(val);
                return IntPtr.Zero;
            }
        }
        private static LuaPushNative_bytes ___tpn_bytes = new LuaPushNative_bytes();
        private class LuaPushNative_char : LuaPushNativeValueType<char>
        {
            public override char GetLua(IntPtr l, int index)
            {
                if (l.IsNumber(index))
                {
                    return (char)l.tonumber(index);
                }
                else if (l.IsString(index))
                {
                    var str = l.GetString(index);
                    if (!string.IsNullOrEmpty(str))
                    {
                        return str[0];
                    }
                }
                return '\0';
            }
            public override IntPtr PushLua(IntPtr l, char val)
            {
                l.pushnumber(val);
                return IntPtr.Zero;
            }
        }
        private static LuaPushNative_char ___tpn_char = new LuaPushNative_char();
        private class LuaPushNative_decimal : LuaPushNativeValueType<decimal>
        {
            public override decimal GetLua(IntPtr l, int index)
            {
                if (l.IsNumber(index))
                {
                    return (decimal)l.tonumber(index);
                }
                else
                {
                    return 0;
                }
            }
            public override IntPtr PushLua(IntPtr l, decimal val)
            {
                LuaPushNativeLongNumberCache.DecimalCache = val;
                if (LuaPushNativeLongNumberCache.SafeMode)
                {
                    var hub = LuaTypeHub.GetTypeHub(typeof(decimal));
                    hub.PushLuaCommon(l, val);
                }
                else
                {
                    l.pushnumber((double)val);
                }
                return IntPtr.Zero;
            }
        }
        private static LuaPushNative_decimal ___tpn_decimal = new LuaPushNative_decimal();
        private class LuaPushNative_double : LuaPushNativeValueType<double>
        {
            public override double GetLua(IntPtr l, int index)
            {
                if (l.IsNumber(index))
                {
                    return l.tonumber(index);
                }
                else
                {
                    return 0;
                }
            }
            public override IntPtr PushLua(IntPtr l, double val)
            {
                l.pushnumber(val);
                return IntPtr.Zero;
            }
        }
        private static LuaPushNative_double ___tpn_double = new LuaPushNative_double();
        private class LuaPushNative_float : LuaPushNativeValueType<float>
        {
            public override float GetLua(IntPtr l, int index)
            {
                if (l.IsNumber(index))
                {
                    return (float)l.tonumber(index);
                }
                else
                {
                    return 0;
                }
            }
            public override IntPtr PushLua(IntPtr l, float val)
            {
                l.pushnumber(val);
                return IntPtr.Zero;
            }
        }
        private static LuaPushNative_float ___tpn_float = new LuaPushNative_float();
        private class LuaPushNative_int : LuaPushNativeValueType<int>
        {
            public override int GetLua(IntPtr l, int index)
            {
                if (l.IsNumber(index))
                {
                    return (int)l.tonumber(index);
                }
                else
                {
                    return 0;
                }
            }
            public override IntPtr PushLua(IntPtr l, int val)
            {
                l.pushnumber(val);
                return IntPtr.Zero;
            }
        }
        private static LuaPushNative_int ___tpn_int = new LuaPushNative_int();
        private class LuaPushNative_IntPtr : LuaPushNativeValueType<IntPtr>
        {
            public override IntPtr GetLua(IntPtr l, int index)
            {
                if (l.isuserdata(index))
                {
                    return l.touserdata(index);
                }
                else if (l.isnumber(index))
                {
                    return new IntPtr((long)l.tonumber(index));
                }
                else
                {
                    return l.topointer(index);
                }
            }
            public override IntPtr PushLua(IntPtr l, IntPtr val)
            {
                l.pushlightuserdata(val);
                return IntPtr.Zero;
            }
        }
        private static LuaPushNative_IntPtr ___tpn_IntPtr = new LuaPushNative_IntPtr();
        private class LuaPushNative_UIntPtr : LuaPushNativeValueType<UIntPtr>
        {
            public override UIntPtr GetLua(IntPtr l, int index)
            {
                if (l.isuserdata(index))
                {
                    return (UIntPtr)(ulong)l.touserdata(index);
                }
                else if (l.isnumber(index))
                {
                    return new UIntPtr((ulong)l.tonumber(index));
                }
                else
                {
                    return (UIntPtr)(ulong)l.topointer(index);
                }
            }
            public override IntPtr PushLua(IntPtr l, UIntPtr val)
            {
                l.pushlightuserdata((IntPtr)(ulong)val);
                return IntPtr.Zero;
            }
        }
        private static LuaPushNative_UIntPtr ___tpn_UIntPtr = new LuaPushNative_UIntPtr();
        private class LuaPushNative_long : LuaPushNativeValueType<long>
        {
            public override long GetLua(IntPtr l, int index)
            {
                if (l.IsNumber(index))
                {
                    return (long)l.tonumber(index);
                }
                else
                {
                    return 0;
                }
            }
            public override IntPtr PushLua(IntPtr l, long val)
            {
                LuaPushNativeLongNumberCache.Int64Cache = (ulong)val;
                if (LuaPushNativeLongNumberCache.SafeMode)
                {
                    var hub = LuaTypeHub.GetTypeHub(typeof(long));
                    hub.PushLuaCommon(l, val);
                }
                else
                {
                    l.pushnumber((double)val);
                }
                return IntPtr.Zero;
            }
        }
        private static LuaPushNative_long ___tpn_long = new LuaPushNative_long();
        private class LuaPushNative_sbyte : LuaPushNativeValueType<sbyte>
        {
            public override sbyte GetLua(IntPtr l, int index)
            {
                if (l.IsNumber(index))
                {
                    return (sbyte)l.tonumber(index);
                }
                else
                {
                    return 0;
                }
            }
            public override IntPtr PushLua(IntPtr l, sbyte val)
            {
                l.pushnumber(val);
                return IntPtr.Zero;
            }
        }
        private static LuaPushNative_sbyte ___tpn_sbyte = new LuaPushNative_sbyte();
        private class LuaPushNative_short : LuaPushNativeValueType<short>
        {
            public override short GetLua(IntPtr l, int index)
            {
                if (l.IsNumber(index))
                {
                    return (short)l.tonumber(index);
                }
                else
                {
                    return 0;
                }
            }
            public override IntPtr PushLua(IntPtr l, short val)
            {
                l.pushnumber(val);
                return IntPtr.Zero;
            }
        }
        private static LuaPushNative_short ___tpn_short = new LuaPushNative_short();
        private class LuaPushNative_string : LuaPushNativeBase<string>
        {
            public override string GetLua(IntPtr l, int index)
            {
                return l.GetString(index);
            }
            public override IntPtr PushLua(IntPtr l, string val)
            {
                //if (val == null)
                //    l.pushnil();
                //else
                    l.PushString(val);
                return IntPtr.Zero;
            }
        }
        private static LuaPushNative_string ___tpn_string = new LuaPushNative_string();
        private class LuaPushNative_uint : LuaPushNativeValueType<uint>
        {
            public override uint GetLua(IntPtr l, int index)
            {
                if (l.IsNumber(index))
                {
                    return (uint)l.tonumber(index);
                }
                else
                {
                    return 0;
                }
            }
            public override IntPtr PushLua(IntPtr l, uint val)
            {
                l.pushnumber(val);
                return IntPtr.Zero;
            }
        }
        private static LuaPushNative_uint ___tpn_uint = new LuaPushNative_uint();
        private class LuaPushNative_ulong : LuaPushNativeValueType<ulong>
        {
            public override ulong GetLua(IntPtr l, int index)
            {
                if (l.IsNumber(index))
                {
                    return (ulong)l.tonumber(index);
                }
                else
                {
                    return 0;
                }
            }
            public override IntPtr PushLua(IntPtr l, ulong val)
            {
                LuaPushNativeLongNumberCache.Int64Cache = val;
                if (LuaPushNativeLongNumberCache.SafeMode)
                {
                    var hub = LuaTypeHub.GetTypeHub(typeof(ulong));
                    hub.PushLuaCommon(l, val);
                }
                else
                {
                    l.pushnumber((double)val);
                }
                return IntPtr.Zero;
            }
        }
        private static LuaPushNative_ulong ___tpn_ulong = new LuaPushNative_ulong();
        private class LuaPushNative_ushort : LuaPushNativeValueType<ushort>
        {
            public override ushort GetLua(IntPtr l, int index)
            {
                if (l.IsNumber(index))
                {
                    return (ushort)l.tonumber(index);
                }
                else
                {
                    return 0;
                }
            }
            public override IntPtr PushLua(IntPtr l, ushort val)
            {
                l.pushnumber(val);
                return IntPtr.Zero;
            }
        }
        private static LuaPushNative_ushort ___tpn_ushort = new LuaPushNative_ushort();
        private class LuaPushNative_Type : LuaPushNativeBase<Type>
        {
            public override Type GetLua(IntPtr l, int index)
            {
                return l.GetLua(index) as Type;
            }
            public override IntPtr PushLua(IntPtr l, Type val)
            {
                //if (val == null)
                //    l.pushnil();
                //else
                    l.PushLuaType(val);
                return IntPtr.Zero;
            }
        }
        private static LuaPushNative_Type ___tpn_Type = new LuaPushNative_Type();
    }
}