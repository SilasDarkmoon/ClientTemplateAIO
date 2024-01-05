using System;
using System.Collections.Generic;
using UnityEngineEx;

using lua = LuaLib.LuaCoreLib;
using lual = LuaLib.LuaAuxLib;
using luae = LuaLib.LuaLibEx;

namespace LuaLib
{
    public interface ILuaHandle
    {
        IntPtr r { get; }
    }
    public interface ILuaTypeRef : ILuaHandle
    {
        Type t { get; }
    }

    public interface ILuaTrans
    {
        void SetData(IntPtr l, int index, object val);
        object GetLua(IntPtr l, int index);
        Type GetType(IntPtr l, int index);
        bool Nonexclusive { get; } // for GetLua<T>. Can the lua-table be converted to C# objects of different types?
    }
    public interface ILuaTrans<T>
    {
        void SetData(IntPtr l, int index, T val);
        T GetLua(IntPtr l, int index);
    }
    public interface ILuaTransMulti
    {
        void SetData<T>(IntPtr l, int index, T val);
        T GetLua<T>(IntPtr l, int index);
    }
    public interface ILuaPush
    {
        IntPtr PushLua(IntPtr l, object val);
        bool ShouldCache { get; }
        bool PushFromCache(IntPtr l, object val);
    }
    public interface ILuaPush<T>
    {
        IntPtr PushLua(IntPtr l, T val);
    }
    public interface IInstanceCreator
    {
        object NewInstance();
    }
    public interface IInstanceCreator<T> : IInstanceCreator
    {
        new T NewInstance();
    }
    public interface ILuaTypeHub : ILuaHandle, ILuaTypeRef, ILuaTrans, ILuaPush
    {
        void PushLuaTypeRaw(IntPtr l);
    }

    public interface ILuaMetaCall : ILuaHandle
    {
        void call(IntPtr l, object tar);
    }
    public interface ILuaMetaGC : ILuaHandle
    {
        void gc(IntPtr l, object obj);
    }
    public interface ILuaMetaIndex : ILuaHandle
    {
        void index(IntPtr l, object tar, int kindex);
    }
    public interface ILuaMetaNewIndex : ILuaHandle
    {
        void newindex(IntPtr l, object tar, int kindex, int valindex);
    }
    public interface ILuaMeta : ILuaMetaCall, ILuaMetaGC, ILuaMetaIndex, ILuaMetaNewIndex
    {
    }

    public delegate int LuaConvertFunc(IntPtr l, int index);
    public interface ILuaConvert
    {
        LuaConvertFunc GetConverter(Type totype);
        LuaConvertFunc GetConverterFrom(Type fromtype);
    }

    public abstract class SelfHandled : ILuaHandle
    {
        protected IntPtr _r;
        public IntPtr r
        {
            get { return _r; }
        }

        protected internal SelfHandled()
        {
            _r = (IntPtr)System.Runtime.InteropServices.GCHandle.Alloc(this);
        }
    }

    public struct LuaStateRecover : IDisposable
    {
        private IntPtr _l;
        private IntPtr _oldRunningState;
        private int _top;

        public int Top
        {
            get { return _top; }
        }

        public LuaStateRecover(IntPtr l)
        {
            _l = l;
            _top = l.gettop(); // this is dangerous, but the user should call it with available "l".
            _oldRunningState = LuaHub.RunningLuaState;
            LuaHub.RunningLuaState = l;
        }

        public void Dispose()
        {
            LuaHub.RunningLuaState = _oldRunningState;
            int top = _l.gettop();
            if (top < _top)
            {
                _l.LogWarning("lua stack top is lower than the prev top, there may be some mistake!");
            }
            else
            {
                _l.settop(_top);
            }
        }
    }
    public struct LuaRunningStateRecorder : IDisposable
    {
        private IntPtr _oldRunningState;

        public LuaRunningStateRecorder(IntPtr l)
        {
            _oldRunningState = LuaHub.RunningLuaState;
            LuaHub.RunningLuaState = l;
        }
        public void Dispose()
        {
            LuaHub.RunningLuaState = _oldRunningState;
        }
    }

    public static partial class LuaHub
    {
        private static HashSet<Type> NumericTypes = new HashSet<Type>()
        {
            typeof(bool),
            typeof(byte),
            typeof(decimal),
            typeof(double),
            typeof(short),
            typeof(int),
            typeof(long),
            typeof(sbyte),
            typeof(float),
            typeof(ushort),
            typeof(uint),
            typeof(ulong),
        };
        private static HashSet<Type> ConvertibleTypes = new HashSet<Type>()
        {
            typeof(bool),
            typeof(byte),
            typeof(decimal),
            typeof(double),
            typeof(short),
            typeof(int),
            typeof(long),
            typeof(sbyte),
            typeof(float),
            typeof(ushort),
            typeof(uint),
            typeof(ulong),

            typeof(char),
            typeof(string),
            typeof(IntPtr),
        };

        public static bool IsNumeric(Type t)
        {
            return NumericTypes.Contains(t);
        }
        public static bool IsNumeric(object val)
        {
            return val != null && IsNumeric(val.GetType());
        }
        public static bool IsConvertible(Type t)
        {
            return ConvertibleTypes.Contains(t) || t.IsEnum();
        }
        public static bool IsConvertible(object val)
        {
            return val != null && IsConvertible(val.GetType());
        }
        public static bool CanConvertRaw(Type curtype, Type totype)
        {
            if (totype == null)
            {
                if (curtype == null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            if (curtype == null)
            {
                if (totype.IsValueType() && Nullable.GetUnderlyingType(totype) == null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            if (totype.IsAssignableFrom(curtype))
            {
                return true;
            }
            if (IsConvertible(curtype) && IsConvertible(totype))
            {
                return true;
            }
            if (curtype.IsSubclassOf(typeof(BaseDynamic)))
            {
                if (totype.IsSubclassOf(typeof(Delegate)))
                {
                    return true;
                }
                else if (typeof(LuaLib.ILuaWrapper).IsAssignableFrom(totype))
                {
                    return true;
                }
            }
            else
            {
                bool hasNullableType = false;
                if (Nullable.GetUnderlyingType(curtype) != null)
                {
                    hasNullableType = true;
                    curtype = curtype.GetGenericArguments()[0];
                }
                if (Nullable.GetUnderlyingType(totype) != null)
                {
                    hasNullableType = true;
                    totype = totype.GetGenericArguments()[0];
                }
                if (hasNullableType)
                {
                    return CanConvertRaw(curtype, totype);
                }
            }
            return false;
        }
        public static object ConvertTypeRaw(this object obj, Type type)
        {
            if (type == null)
                return null;
            if (obj == null)
                return null;
            if (obj is LuaLib.BaseLua)
            {
                if (type.IsSubclassOf(typeof(Delegate)))
                {
                    return LuaLib.LuaDelegateGenerator.CreateDelegate(type, (LuaLib.BaseLua)obj);
                }
                else if (typeof(LuaLib.ILuaWrapper).IsAssignableFrom(type))
                {
                    try
                    {
                        LuaLib.ILuaWrapper wrapper = (LuaLib.ILuaWrapper)Activator.CreateInstance(type);
                        wrapper.Binding = (LuaLib.BaseLua)obj;
                        return wrapper;
                    }
                    catch (Exception e)
                    {
                        PlatDependant.LogError(e);
                        return null;
                    }
                }
            }
            if (type.IsAssignableFrom(obj.GetType()))
                return obj;
            if (type.IsEnum())
            {
                if (obj is string)
                {
                    try
                    {
                        return Enum.Parse(type, obj as string);
                    }
                    catch
                    {
                        return null;
                    }
                }
                else if (IsNumeric(obj))
                {
                    return Enum.ToObject(type, (object)Convert.ToUInt64(obj));
                }
                else
                {
                    return Enum.ToObject(type, 0UL);
                }
            }
            else if (obj is Enum)
            {
                if (type == typeof(string))
                {
                    return obj.ToString();
                }
                else if (IsNumeric(type))
                {
                    return Convert.ChangeType(Convert.ToUInt64(obj), type);
                }
                else
                {
                    return Convert.ChangeType(0, type);
                }
            }
            else if (IsNumeric(type) && IsNumeric(obj))
            {
                try
                {
                    return Convert.ChangeType(obj, type);
                }
                catch
                {
                    return null;
                }
            }
            else if (type == typeof(IntPtr) && IsNumeric(obj))
            {
                try
                {
                    long l = Convert.ToInt64(obj);
                    IntPtr p = (IntPtr)l;
                    return p;
                }
                catch
                {
                    return null;
                }
            }
            else if (obj is IntPtr && IsNumeric(type))
            {
                IntPtr p = (IntPtr)obj;
                long l = (long)p;
                try
                {
                    return Convert.ChangeType(l, type);
                }
                catch
                {
                    return null;
                }
            }
            else if (type == typeof(bool))
            {
                return ConvertUtils.ToBoolean(obj);
            }
            else if (IsConvertible(type) && IsConvertible(obj))
            {
                try
                {
                    return Convert.ChangeType(obj, type);
                }
                catch
                {
                    return null;
                }
            }
            else if (Nullable.GetUnderlyingType(type) != null)
            {
                var gtype = type.GetGenericArguments()[0];
                return ConvertTypeRaw(obj, gtype);
            }
            return null;
        }
        public static int GetTypeWeight(Type type)
        {
            if (type == null)
            {
                return 0;
            }
            if (type.IsEnum())
            {
                return 14;
            }
            switch (type.GetTypeCode())
            {
                case TypeCode.Boolean:
                    return 10;
                case TypeCode.Byte:
                    return 34;
                case TypeCode.Char:
                    return 20;
                case TypeCode.DateTime:
                    return 24;
                case TypeCode.Decimal:
                    return 70;
                case TypeCode.Double:
                    return 68;
                case TypeCode.Int16:
                    return 40;
                case TypeCode.Int32:
                    return 50;
                case TypeCode.Int64:
                    return 60;
                case TypeCode.SByte:
                    return 30;
                case TypeCode.Single:
                    return 58;
                case TypeCode.UInt16:
                    return 44;
                case TypeCode.UInt32:
                    return 54;
                case TypeCode.UInt64:
                    return 64;
            }
            return 80;
        }
        public static int GetParamsCode(IList<Type> types)
        {
            int code = 0;
            if (types != null)
            {
                for (int i = 0; i < types.Count; ++i)
                {
                    int codepart = 0;
                    if (types[i] != null && types[i].IsValueType())
                    {
                        codepart = 1 << i;
                    }
                    code += codepart;
                }
            }
            return code;
        }
        //public static int[] GetParamsCodesWithNullable(IList<Type> types)
        //{ // this may cause mass amount of codes: think of Func(int? * 32), this will return 2^32 codes.
        //    List<int> codes = new List<int>() { 0 };
        //    if (types != null)
        //    {
        //        for (int i = 0; i < types.Count; ++i)
        //        {
        //            var type = types[i];
        //            if (type != null && type.IsValueType())
        //            {
        //                if (Nullable.GetUnderlyingType(type) == null)
        //                {
        //                    for (int j = 0; j < codes.Count; ++j)
        //                    {
        //                        int codepart = 1 << i;
        //                        codes[j] += codepart;
        //                    }
        //                }
        //                else
        //                { // Nullable?
        //                    int curcnt = codes.Count;
        //                    for (int j = 0; j < curcnt; ++j)
        //                    {
        //                        int codepart = 1 << i;
        //                        codes.Add(codes[j] + codepart);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    if (codes.Count <= 1)
        //    {
        //        return null;
        //    }
        //    else
        //    {
        //        return codes.ToArray();
        //    }
        //}

        public static int NormalizeIndex(this IntPtr l, int index)
        {
            if (index < 0)
            {
                var top = l.gettop();
                if (-index <= top)
                {
                    index = top + 1 + index;
                }
            }
            return index;
        }
        public static LuaStateRecover CreateStackRecover(this IntPtr l)
        {
            return new LuaStateRecover(l);
        }
        public static IntPtr Indicator(this IntPtr l)
        {
            return l.topointer(lua.LUA_REGISTRYINDEX);
        }
        public static bool IsSameThread(this IntPtr l, IntPtr l2)
        { // usually, we can use l == l2. this function is almost useless.
            l.pushvalue(lua.LUA_REGISTRYINDEX);
            bool same = l2.istable(-1) && l2.topointer(-1) == l.topointer(-1);
            l.pop(1);
            return same;
        }

        public static string GetLuaStackTrace(this IntPtr l)
        {
            l.PushLuaStackTrace();
            var st = l.tostring(-1);
            l.pop(1);
            return st;
        }
        internal static void PushLuaStackTrace(this IntPtr l)
        {
            l.checkstack(4);
            l.GetGlobal(LuaConst.LS_LIB_DEBUG); // debug
            l.GetField(-1, LuaConst.LS_LIB_TRACEBACK); // debug traceback
            l.PushString(LuaConst.LS_COMMON_EMPTY); // debug tb ""
            l.pushnumber(2); // debug tb "" 2
            l.pcall(2, 1, 0); // debug "stack"
            l.remove(-2); // "stack"
            if (l.pushthread())
            {
                l.pop(1);
                return;
            }
            l.pop(1); // "stack"
            if (!l.getmetatable(lua.LUA_GLOBALSINDEX))
            {
                return;
            }
            // "stack" envmeta
            l.GetField(-1, "__master"); // "stack" envmeta thd
            if (!l.isthread(-1))
            {
                l.pop(2);
                return;
            }
            l.remove(-2); // "stack" thd
            l.GetGlobal(LuaConst.LS_LIB_DEBUG); // "stack" thd debug
            l.GetField(-1, LuaConst.LS_LIB_TRACEBACK); // "stack" thd debug traceback
            l.remove(-2); // "stack" thd traceback
            l.insert(-2); // "stack" tb thd
            l.PushString("\nmain thread:"); // "stack" tb thd "main"
            l.pushnumber(2); // "stack" tb thd "main" 2
            l.pcall(3, 1, 0); // "stack" "stack2"
            l.concat(2); // "stackfull"
        }
        public static void LogInfo(this IntPtr l, object message)
        {
            PushLuaStackTrace(l);
            var lstack = l.tostring(-1);
            l.pop(1);
            var m = (message ?? "nullptr").ToString() + "\n" + lstack;
            ForbidLuaStackTrace = true;
            PlatDependant.LogInfo(m);
            ForbidLuaStackTrace = false;
        }
        public static void LogWarning(this IntPtr l, object message)
        {
            PushLuaStackTrace(l);
            var lstack = l.tostring(-1);
            l.pop(1);
            var m = (message ?? "nullptr").ToString() + "\n" + lstack;
            ForbidLuaStackTrace = true;
            PlatDependant.LogWarning(m);
            ForbidLuaStackTrace = false;
        }
        public static void LogError(this IntPtr l, object message)
        {
            PushLuaStackTrace(l);
            var lstack = l.tostring(-1);
            l.pop(1);
            var m = (message ?? "nullptr").ToString() + "\n" + lstack;
            ForbidLuaStackTrace = true;
            PlatDependant.LogError(m);
            ForbidLuaStackTrace = false;
        }
        [ThreadStatic] internal static bool ForbidLuaStackTrace;

        public static readonly lua.CFunction LuaFuncOnInfo = new lua.CFunction(LuaOnInfo);
        public static readonly lua.CFunction LuaFuncOnWarning = new lua.CFunction(LuaOnWarning);
        public static readonly lua.CFunction LuaFuncOnError = new lua.CFunction(LuaOnError);
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        private static int LuaOnInfo(IntPtr l)
        {
            l.checkstack(1);
            l.pushvalue(1);
            var message = l.GetLua(-1);
            LogInfo(l, message);
            return 1;
        }
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        private static int LuaOnWarning(IntPtr l)
        {
            l.checkstack(1);
            l.pushvalue(1);
            var message = l.GetLua(-1);
            LogWarning(l, message);
            return 1;
        }
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        private static int LuaOnError(IntPtr l)
        {
            l.checkstack(1);
            l.pushvalue(1);
            var message = l.GetLua(-1);
            LogError(l, message);
            return 1;
        }

        [ThreadStatic] internal static IntPtr RunningLuaState;
    }
}