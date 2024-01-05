using System;
using System.Collections.Generic;
using UnityEngineEx;

using lua = LuaLib.LuaCoreLib;
using lual = LuaLib.LuaAuxLib;
using luae = LuaLib.LuaLibEx;

namespace LuaLib
{
    public static partial class LuaHub
    {
        public static bool IsString(this IntPtr l, int index)
        {
            return l.type(index) == LuaCoreLib.LUA_TSTRING;
        }

        public static bool IsNumber(this IntPtr l, int index)
        {
            return l.type(index) == LuaCoreLib.LUA_TNUMBER;
        }

        public static bool IsUserData(this IntPtr l, int index)
        {
            return l.type(index) == LuaCoreLib.LUA_TUSERDATA;
        }
        private static bool IsUserDataTableRaw(this IntPtr l, int index)
        {
            using (var lr = l.CreateStackRecover())
            {
                if (l.getmetatable(index))
                {
                    l.GetField(-1, "__udtabletype");
                    if (!l.isnoneornil(-1))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public static bool IsUserDataTable(this IntPtr l, int index)
        {
            if (l.type(index) == LuaCoreLib.LUA_TUSERDATA)
            {
                return IsUserDataTableRaw(l, index);
            }
            return false;
        }
        public static void PushUserDataTableRaw(this IntPtr l, int index)
        {
            l.pushvalue(index);
            while (l.IsUserDataTable(-1))
            { // udt
                if (!l.getmetatable(-1))
                {
                    break;
                }
                // udt meta
                l.GetField(-1, "__raw"); // udt meta raw
                l.insert(-3);
                l.pop(2); // raw
            }
        }
        public static bool IsObject(this IntPtr l, int index)
        {
            if (l.IsUserData(index))
            {
                return !IsUserDataTableRaw(l, index);
            }
            else if (l.istable(index))
            {
                index = l.NormalizeIndex(index);
                l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS); // #trans
                l.gettable(index); // trans
                bool isobj = l.islightuserdata(-1);
                l.pop(1);
                return isobj;
            }
            return false;
        }

        public static bool IsTable(this IntPtr l, int index)
        {
            if (!l.istable(index))
            { // NOTICE: currently not compatible with UserDataTable
                return false;
            }
            index = l.NormalizeIndex(index);
            l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS); // #trans
            l.gettable(index); // trans
            bool isobj = l.islightuserdata(-1);
            l.pop(1);
            return !isobj;
        }
        public static bool IsArray(this IntPtr l, int index)
        {
            if (!l.istable(index))
            { // NOTICE: currently not compatible with UserDataTable
                return false;
            }
            index = l.NormalizeIndex(index);

            if (l.getn(index) > 0)
            {
                return true;
            }

            //l.pushnumber(1);
            //l.rawget(index);
            //if (!l.isnoneornil(-1))
            //{
            //    l.pop(1);
            //    return true;
            //}
            //l.pop(1);

            l.pushnil();
            if (l.next(index))
            {
                var isnumerkey = l.IsNumber(-2);
                l.pop(2);
                return isnumerkey;
            }
            return true;
        }

        public static void UpdateData(this IntPtr l, int index, object val)
        {
            if (val == null)
            {
                if (l.istable(index))
                {
                    LuaLib.LuaCommonMeta.LuaTransCommon.Instance.SetData(l, index, val);
                }
            }
            else
            {
                var hub = LuaTypeHub.GetTypeHub(val.GetType());
                hub.SetData(l, index, val);
            }
        }

        private static void PushLuaTypeRaw(this IntPtr l, ILuaTypeHub hub)
        {
            hub.PushLuaTypeRaw(l);
        }
        public static void PushLuaType(this IntPtr l, ILuaTypeHub hub)
        {
            LuaObjCache.PushObjFromCache(l, hub.r);
            if (l.isnoneornil(-1))
            {
                l.pop(1);
                PushLuaTypeRaw(l, hub);
            }
        }
        public static void PushLuaType(this IntPtr l, Type t)
        {
            if (LuaObjCache.PushObjFromCache(l, t)) return;

            ILuaTypeHub hub = LuaTypeHub.GetTypeHub(t);
            if (hub != null)
            {
                PushLuaTypeRaw(l, hub);
            }
            else
            {
                PushLuaObject(l, t);
            }
        }

        public static void PushLuaObject(this IntPtr l, object val)
        {
            if (l != IntPtr.Zero)
            {
                if (val == null)
                {
                    l.checkstack(6);
                    LuaObjCache.PushOrCreateObjCacheReg(l); // reg
                    l.pushlightuserdata(IntPtr.Zero); // reg 0
                    l.gettable(-2); // reg ud
                    if (!l.isnoneornil(-1))
                    {
                        l.remove(-2); // ud
                        return;
                    }
                    l.pop(1); // reg
                    l.getmetatable(-1); // reg meta
                    l.GetField(-1, LuaConst.LS_META_KEY_INDEX); // reg meta index
                    l.pushlightuserdata(IntPtr.Zero); // reg meta index 0
                    LuaTypeHub.EmptyTypeHub.PushLua(l, null); // reg meta index 0 ud
                    l.pushvalue(-1); // reg meta index 0 ud ud
                    l.insert(-6); // ud reg meta index 0 ud
                    l.settable(-3); // ud reg meta index
                    l.pop(3); // ud
                }
                else
                {
                    if (LuaObjCache.PushObjFromCache(l, val)) return;
                    var type = val.GetType();
                    ILuaTypeHub sub = LuaTypeHub.GetTypeHub(type);
                    if (sub != null)
                    {
                        var h = sub.PushLua(l, val);
                        if (sub.ShouldCache && h != IntPtr.Zero)
                        {
                            LuaObjCache.RegObj(l, val, -1, h);
                        }
                    }
                    else
                    {
                        PushLuaRawObject(l, val);
                        l.PushCommonMetaTable();
                        l.setmetatable(-2);
                    }
                }
            }
        }
        public static void PushLuaObject(this IntPtr l, object val, Type type)
        {
            if (l != IntPtr.Zero)
            {
                if (type == null)
                {
                    PushLuaObject(l, val);
                }
                else
                {
                    if (val != null && val.GetType() == type)
                    {
                        PushLuaObject(l, val);
                    }
                    else
                    {
                        ILuaTypeHub sub = LuaTypeHub.GetTypeHub(type);
                        if (sub != null)
                        {
                            sub.PushLua(l, val);
                        }
                        else
                        {
                            PushLuaRawObject(l, val);
                            l.PushCommonMetaTable();
                            l.setmetatable(-2);
                        }
                    }
                }
            }
        }

        public static void PushLua(this IntPtr l, object val)
        {
#if ENABLE_PROFILER_LUA_DEEP
            using (var pcon = ProfilerContext.Create("PushLua (typeless): {0}", (val == null ? "null" : val.GetType().Name)))
#endif
            if (l != IntPtr.Zero)
            {
                if (val is LuaLib.LuaState)
                {
                    var state = (LuaLib.LuaState)val;
                    if (state.L.Indicator() == l.Indicator())
                    {
                        state.L.pushthread();
                        if (state.L != l)
                        {
                            state.L.xmove(l, 1);
                        }
                    }
                    else
                    {
                        l.PushLuaObject(state);
                    }
                }
                else if (val is LuaLib.BaseLuaOnStack)
                {
                    l.pushvalue(((LuaLib.BaseLuaOnStack)val).StackPos);
                }
                else if (val is LuaLib.BaseLua)
                {
                    l.getref(((LuaLib.BaseLua)val).Refid);
                }
                //else if (raw is lua.CFunction)
                //{

                //}
                else if (val is ILuaTypeHub)
                {
                    l.PushLuaType(val as ILuaTypeHub);
                }
                else if (val is Type)
                {
                    l.PushLuaType(val as Type);
                }
                else if (val == null)
                {
                    l.pushnil();
                }
                else if (val is bool)
                {
                    l.pushboolean((bool)val);
                }
                else if (val is string)
                {
                    //l.pushstring((string)val);
                    l.PushString((string)val);
                }
                else if (val is byte[])
                {
                    l.pushbuffer((byte[])val);
                }
                else if (val is IntPtr)
                {
                    l.pushlightuserdata((IntPtr)val);
                }
                else if (val is char)
                {
                    l.pushnumber((char)val);
                }
                else if (IsNumeric(val))
                {
                    if (val is decimal)
                    {
                        LuaPushNativeLongNumberCache.DecimalCache = (decimal)val;
                        if (LuaPushNativeLongNumberCache.SafeMode)
                        {
                            var hub = LuaTypeHub.GetTypeHub(typeof(decimal));
                            hub.PushLuaCommon(l, (decimal)val);
                        }
                        else
                        {
                            l.pushnumber(Convert.ToDouble(val));
                        }
                    }
                    else if (val is long)
                    {
                        LuaPushNativeLongNumberCache.Int64Cache = (ulong)(long)val;
                        if (LuaPushNativeLongNumberCache.SafeMode)
                        {
                            var hub = LuaTypeHub.GetTypeHub(typeof(long));
                            hub.PushLuaCommon(l, (long)val);
                        }
                        else
                        {
                            l.pushnumber(Convert.ToDouble(val));
                        }
                    }
                    else if (val is ulong)
                    {
                        LuaPushNativeLongNumberCache.Int64Cache = (ulong)val;
                        if (LuaPushNativeLongNumberCache.SafeMode)
                        {
                            var hub = LuaTypeHub.GetTypeHub(typeof(ulong));
                            hub.PushLuaCommon(l, (ulong)val);
                        }
                        else
                        {
                            l.pushnumber(Convert.ToDouble(val));
                        }
                    }
                    else
                    {
                        l.pushnumber(Convert.ToDouble(val));
                    }
                }
                else if (val is Enum)
                {
                    l.pushnumber(Convert.ToDouble(val));
                }
                else
                {
                    // Try native push.
                    object func;
                    LuaPushNative._NativePushLuaFuncs.TryGetValue(val.GetType(), out func);
                    if (func != null)
                    {
                        ILuaPush pfunc = func as ILuaPush;
                        if (pfunc != null)
                        {
                            pfunc.PushLua(l, val);
                            return;
                        }
                    }
                    // Common push.
                    l.PushLuaObject(val);
                }
            }
        }
        private static void PushLuaNonNative(this IntPtr l, object val)
        {
            // These should be handled in LuaPushNative
            //if (val is LuaLib.BaseLua)
            //{
            //    l.getref(((LuaLib.BaseLua)val).Refid);
            //}
            //else if (val is LuaLib.BaseLuaOnStack)
            //{
            //    l.pushvalue(((LuaLib.BaseLuaOnStack)val).StackPos);
            //}
            //else if (val is LuaLib.LuaState)
            //{
            //    ((LuaLib.LuaState)val).L.pushthread();
            //}
            ////else if (raw is lua.CFunction)
            ////{

            ////}
            //else
            if (val is ILuaTypeHub)
            {
                l.PushLuaType(val as ILuaTypeHub);
            }
            else if (val is Type)
            {
                l.PushLuaType(val as Type);
            }
            else
            {
                l.PushLuaObject(val);
            }
        }
        public static void PushLuaExplicit<T>(this IntPtr l, T val)
        {
            var type = typeof(T);
            ILuaTypeHub sub = LuaTypeHub.GetTypeHub(type);
            if (sub != null)
            {
                if (sub.ShouldCache)
                {
                    if (sub.PushFromCache(l, val)) return;
                }
                var sub2 = sub as ILuaPush<T>;
                IntPtr h;
                if (sub2 != null)
                {
                    h = sub2.PushLua(l, val);
                }
                else
                {
#if ENABLE_PROFILER && ENABLE_PROFILER_LUA && ENABLE_PROFILER_LUA_DEEP && !DISABLE_PROFILER_LUA_GC_ALLOC
                    using (var pcon = ProfilerContext.Create("box val of {0}", typeof(T)))
#endif
                    h = sub.PushLua(l, val);
                }
                if (sub.ShouldCache && h != IntPtr.Zero)
                {
                    LuaObjCache.RegObj(l, val, -1, h);
                }
            }
            else
            {
                PushLuaRawObject(l, val);
                l.PushCommonMetaTable();
                l.setmetatable(-2);
            }
        }
        public static void PushLua<T>(this IntPtr l, T val)
        {
            Type valtype = typeof(T);
            bool isvalue = valtype.IsValueType;
            if (!isvalue && object.ReferenceEquals(val, null))
            {
                l.pushnil();
                return;
            }
            if (!isvalue && !valtype.IsSealed)
            {
                valtype = val.GetType();
            }
            object func;
            LuaPushNative._NativePushLuaFuncs.TryGetValue(valtype, out func);
            if (func != null)
            {
                ILuaPush<T> gfunc = func as ILuaPush<T>;
                if (gfunc != null)
                {
                    gfunc.PushLua(l, val);
                    return;
                }
                else if (func is ILuaPush)
                {
                    ((ILuaPush)func).PushLua(l, (object)val);
                    return;
                }
            }
            if (valtype.IsEnum)
            {
                l.pushnumber(Convert.ToDouble(val));
            }
            else if (isvalue)
            {
                var uutype = Nullable.GetUnderlyingType(valtype);
                if (uutype != null)
                {
                    ILuaTypeHub sub = LuaTypeHub.GetTypeHub(uutype);
                    if (sub != null)
                    {
                        var sub2 = sub as ILuaPush<T>;
                        if (sub2 != null)
                        {
                            sub2.PushLua(l, val);
                        }
                        else
                        {
#if ENABLE_PROFILER && ENABLE_PROFILER_LUA && ENABLE_PROFILER_LUA_DEEP && !DISABLE_PROFILER_LUA_GC_ALLOC
                            using (var pcon = ProfilerContext.Create("box val of {0}", typeof(T)))
#endif
                            sub.PushLua(l, val);
                        }
                    }
                    else
                    {
                        PushLuaRawObject(l, val);
                        l.PushCommonMetaTable();
                        l.setmetatable(-2);
                    }
                }
                else
                {
                    PushLuaExplicit<T>(l, val);
                }
            }
            else if (typeof(T).IsSealed)
            {
                PushLuaExplicit<T>(l, val);
            }
            else
            {
                PushLuaNonNative(l, (object)val);
            }
        }

        public static IntPtr PushLuaRawObject(this IntPtr l, object val)
        {
            l.checkstack(2);
            var buffer = l.newuserdata(new IntPtr(System.Runtime.InteropServices.Marshal.SizeOf(typeof(IntPtr)))); // ud
            IntPtr data = IntPtr.Zero;
            if (val != null)
            {
                var handle = System.Runtime.InteropServices.GCHandle.Alloc(val);
                data = (IntPtr)handle;
            }
            System.Runtime.InteropServices.Marshal.WriteIntPtr(buffer, data);

            l.PushRawMetaTable(); // ud meta
            l.setmetatable(-2); // ud
            return data;
        }
        public static void PushLuaRaw(this IntPtr l, object val)
        {
            if (l != IntPtr.Zero)
            {
                if (val == null)
                {
                    l.pushnil();
                }
                else
                {
                    PushLuaRawObject(l, val);
                }
            }
        }

#region Push Methods for CLR Type
        public static void PushLua(this IntPtr l, Type val)
        {
            if (val == null)
            {
                l.pushnil();
            }
            else
            {
                l.PushLuaType(val);
            }
        }
        public static void PushLua(this IntPtr l, ILuaTypeHub val)
        {
            if (val == null)
            {
                l.pushnil();
            }
            else
            {
                l.PushLuaType(val);
            }
        }
#endregion

#region Push Methods for Native Lua Types
        public static void PushLua(this IntPtr l, bool val)
        {
            l.pushboolean((bool)val);
        }
        public static void PushLua(this IntPtr l, bool? val)
        {
            if (val == null)
            {
                l.pushnil();
            }
            else
            {
                PushLua(l, val.Value);
            }
        }
        public static void PushLua(this IntPtr l, string val)
        {
            if (val == null)
                l.pushnil();
            else
                l.PushString(val);
        }
        public static void PushLua(this IntPtr l, byte[] val)
        {
            l.pushbuffer(val);
        }
        public static void PushLua(this IntPtr l, IntPtr val)
        {
            l.pushlightuserdata(val);
        }
        public static void PushLua(this IntPtr l, IntPtr? val)
        {
            if (val == null)
            {
                l.pushnil();
            }
            else
            {
                PushLua(l, val.Value);
            }
        }
        public static void PushLua(this IntPtr l, UIntPtr val)
        {
            l.pushlightuserdata((IntPtr)(ulong)val);
        }
        public static void PushLua(this IntPtr l, UIntPtr? val)
        {
            if (val == null)
            {
                l.pushnil();
            }
            else
            {
                PushLua(l, val.Value);
            }
        }
        public static void PushLua(this IntPtr l, byte val)
        {
            l.pushnumber(val);
        }
        public static void PushLua(this IntPtr l, byte? val)
        {
            if (val == null)
            {
                l.pushnil();
            }
            else
            {
                PushLua(l, val.Value);
            }
        }
        public static void PushLua(this IntPtr l, char val)
        {
            l.pushnumber(val);
        }
        public static void PushLua(this IntPtr l, char? val)
        {
            if (val == null)
            {
                l.pushnil();
            }
            else
            {
                PushLua(l, val.Value);
            }
        }
        public static void PushLua(this IntPtr l, decimal val)
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
        }
        public static void PushLua(this IntPtr l, decimal? val)
        {
            if (val == null)
            {
                l.pushnil();
            }
            else
            {
                PushLua(l, val.Value);
            }
        }
        public static void PushLua(this IntPtr l, double val)
        {
            l.pushnumber(val);
        }
        public static void PushLua(this IntPtr l, double? val)
        {
            if (val == null)
            {
                l.pushnil();
            }
            else
            {
                PushLua(l, val.Value);
            }
        }
        public static void PushLua(this IntPtr l, short val)
        {
            l.pushnumber(val);
        }
        public static void PushLua(this IntPtr l, short? val)
        {
            if (val == null)
            {
                l.pushnil();
            }
            else
            {
                PushLua(l, val.Value);
            }
        }
        public static void PushLua(this IntPtr l, int val)
        {
            l.pushnumber(val);
        }
        public static void PushLua(this IntPtr l, int? val)
        {
            if (val == null)
            {
                l.pushnil();
            }
            else
            {
                PushLua(l, val.Value);
            }
        }
        public static void PushLua(this IntPtr l, long val)
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
        }
        public static void PushLua(this IntPtr l, long? val)
        {
            if (val == null)
            {
                l.pushnil();
            }
            else
            {
                PushLua(l, val.Value);
            }
        }
        public static void PushLua(this IntPtr l, sbyte val)
        {
            l.pushnumber(val);
        }
        public static void PushLua(this IntPtr l, sbyte? val)
        {
            if (val == null)
            {
                l.pushnil();
            }
            else
            {
                PushLua(l, val.Value);
            }
        }
        public static void PushLua(this IntPtr l, float val)
        {
            l.pushnumber(val);
        }
        public static void PushLua(this IntPtr l, float? val)
        {
            if (val == null)
            {
                l.pushnil();
            }
            else
            {
                PushLua(l, val.Value);
            }
        }
        public static void PushLua(this IntPtr l, ushort val)
        {
            l.pushnumber(val);
        }
        public static void PushLua(this IntPtr l, ushort? val)
        {
            if (val == null)
            {
                l.pushnil();
            }
            else
            {
                PushLua(l, val.Value);
            }
        }
        public static void PushLua(this IntPtr l, uint val)
        {
            l.pushnumber(val);
        }
        public static void PushLua(this IntPtr l, uint? val)
        {
            if (val == null)
            {
                l.pushnil();
            }
            else
            {
                PushLua(l, val.Value);
            }
        }
        public static void PushLua(this IntPtr l, ulong val)
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
        }
        public static void PushLua(this IntPtr l, ulong? val)
        {
            if (val == null)
            {
                l.pushnil();
            }
            else
            {
                PushLua(l, val.Value);
            }
        }
#endregion

        public static Type GetLuaRawObjectType(this IntPtr l, int index)
        {
            var obj = GetLuaObject(l, index);
            if (obj != null)
            {
                return obj.GetType();
            }
            return null;
        }
        private static Type GetLuaTableObjectType(this IntPtr l, int index, out bool isUserData)
        {
            isUserData = false;
            l.checkstack(2);
            l.pushvalue(index); // ud
            l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS); // ud #trans
            l.gettable(-2); // ud trans
            ILuaTrans trans = null;
            if (l.isuserdata(-1))
            {
                trans = l.GetLuaLightObject(-1) as ILuaTrans;
            }
            l.pop(2);

            if (trans != null)
            {
                isUserData = true;
                return trans.GetType(l, index);
            }
            return null;
        }
        public static Type GetLuaObjectType(this IntPtr l, int index)
        {
            if (l.istable(index))
            {
                bool isUserData;
                return GetLuaTableObjectType(l, index, out isUserData);
            }
            else if (l.IsUserData(index))
            {
                return GetLuaRawObjectType(l, index);
            }
            return null;
        }
        public static Type GetType(this IntPtr l, int index, out int typecode, out bool isobj)
        {
            typecode = l.type(index);
            switch (typecode)
            {
                case lua.LUA_TUSERDATA:
                    if (IsUserDataTableRaw(l, index))
                    {
                        isobj = false;
                        return typeof(LuaLib.LuaTable);
                    }
                    else
                    {
                        var t = GetLuaRawObjectType(l, index);
                        isobj = t != null;
                        return t;
                    }
                case lua.LUA_TSTRING:
                    isobj = false;
                    return typeof(string);
                case lua.LUA_TTABLE:
                    {
                        var type = GetLuaTableObjectType(l, index, out isobj);
                        return isobj ? type : typeof(LuaLib.LuaTable);
                    }
                case lua.LUA_TNUMBER:
                    isobj = false;
                    return typeof(double);
                case lua.LUA_TBOOLEAN:
                    isobj = false;
                    return typeof(bool);
                case lua.LUA_TNIL:
                    isobj = false;
                    return null;
                case lua.LUA_TNONE:
                    isobj = false;
                    return null;
                case lua.LUA_TLIGHTUSERDATA:
                    isobj = false;
                    return typeof(IntPtr);
                case lua.LUA_TFUNCTION:
                    isobj = false;
                    return typeof(LuaLib.LuaFunc);
                case lua.LUA_TTHREAD:
                    isobj = false;
                    return typeof(LuaLib.LuaOnStackThread);
            }
            isobj = false;
            return null;
        }
        public static Type GetType(this IntPtr l, int index, out bool isobj)
        {
            int typecode;
            return GetType(l, index, out typecode, out isobj);
        }
        public static Type GetType(this IntPtr l, int index, out int typecode)
        {
            bool isobj;
            return GetType(l, index, out typecode, out isobj);
        }
        public static Type GetType(this IntPtr l, int index)
        {
            int typecode;
            return GetType(l, index, out typecode);
        }
        public static object GetLuaLightObject(this IntPtr l, int index)
        {
            IntPtr pud = l.touserdata(index);
            try
            {
                System.Runtime.InteropServices.GCHandle handle = (System.Runtime.InteropServices.GCHandle)pud;
                return handle.Target;
            }
            catch { }
            return null;
        }

        public static object GetLuaTableObjectDirect(this IntPtr l, int index)
        {
            var pos = l.NormalizeIndex(index);
            object rv;
            if (LuaObjCacheSlim.TryGet(l, pos, out rv))
            {
                return rv;
            }
            l.checkstack(2);
            l.pushlightuserdata(LuaConst.LRKEY_TARGET); // #tar
            l.rawget(pos); // obj
            if (l.IsUserData(-1))
            {
                rv = l.GetLuaRawObject(-1);
            }
            l.pop(1);
            if (rv != null)
            {
                LuaObjCacheSlim.Record(l, rv, pos);
            }
            return rv;
        }
        public static object GetLuaTableObjectChecked(this IntPtr l, int index)
        {
            if (l.istable(index))
            {
                return GetLuaTableObjectDirect(l, index);
            }
            return null;
        }

        private static object GetLuaTableObject(this IntPtr l, int index, out bool isUserData)
        {
            var pos = l.NormalizeIndex(index);
            object rv;
            if (LuaObjCacheSlim.TryGet(l, pos, out rv))
            {
                isUserData = true;
                return rv;
            }

            isUserData = false;
            l.checkstack(2);
            l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS); // #trans
            l.gettable(pos); // trans
            ILuaTrans trans = null;
            if (l.isuserdata(-1))
            {
                trans = l.GetLuaLightObject(-1) as ILuaTrans;
            }
            l.pop(1);

            if (trans != null)
            {
                isUserData = true;
                rv = trans.GetLua(l, index);
                //// Notice the LuaObjCacheSlim.Record should happen when access #tar from a table
                //if (rv != null && rv.GetType().IsClass)
                //{
                //    LuaObjCacheSlim.Record(rv, l.topointer(pos), pos);
                //}
                return rv;
            }
            return null;
        }
        public static object GetLuaRawObject(this IntPtr l, int index)
        {
            IntPtr pud = l.touserdata(index);
            IntPtr hval = System.Runtime.InteropServices.Marshal.ReadIntPtr(pud);
            try
            {
                System.Runtime.InteropServices.GCHandle handle = (System.Runtime.InteropServices.GCHandle)hval;
                return handle.Target;
            }
            catch { }
            return null;
        }
        public static object GetLuaObject(this IntPtr l, int index)
        {
            if (l != IntPtr.Zero)
            {
                if (l.IsUserData(index))
                {
                    return GetLuaRawObject(l, index);
                }
                else if (l.istable(index))
                {
                    bool isUserData;
                    return GetLuaTableObject(l, index, out isUserData);
                }
            }
            return null;
        }

        public static object GetLua(this IntPtr l, int index)
        {
            if (l != IntPtr.Zero)
            {
                int typecode = l.type(index);
                switch (typecode)
                {
                    case lua.LUA_TUSERDATA:
                        if (IsUserDataTableRaw(l, index))
                        {
                            return new LuaLib.LuaTable(l, index);
                        }
                        else
                        {
                            return GetLuaRawObject(l, index);
                        }
                    case lua.LUA_TSTRING:
                        return l.GetString(index);
                    case lua.LUA_TTABLE:
                        {
                            bool isUserData;
                            var obj = GetLuaTableObject(l, index, out isUserData);
                            return isUserData ? obj : new LuaLib.LuaTable(l, index);
                        }
                    case lua.LUA_TNUMBER:
                        return l.tonumber(index);
                    case lua.LUA_TBOOLEAN:
                        return l.toboolean(index);
                    case lua.LUA_TNIL:
                        return null;
                    case lua.LUA_TNONE:
                        return null;
                    case lua.LUA_TLIGHTUSERDATA:
                        return l.touserdata(index);
                    case lua.LUA_TFUNCTION:
                        return new LuaLib.LuaFunc(l, index);
                    case lua.LUA_TTHREAD:
                        IntPtr lthd = l.tothread(index);
                        l.pushvalue(index);
                        int refid = l.refer();
                        return new LuaLib.LuaOnStackThread(refid, lthd);
                }
            }
            return null;
        }

        public static object GetLuaOnStack(this IntPtr l, int index)
        {
            if (l != IntPtr.Zero)
            {
                index = l.NormalizeIndex(index);
                int typecode = l.type(index);
                switch (typecode)
                {
                    case lua.LUA_TUSERDATA:
                        if (IsUserDataTableRaw(l, index))
                        {
                            return new LuaLib.LuaOnStackTable(l, index);
                        }
                        else
                        {
                            return GetLuaRawObject(l, index);
                        }
                    case lua.LUA_TSTRING:
                        return l.GetString(index);
                    case lua.LUA_TTABLE:
                        {
                            bool isUserData;
                            var obj = GetLuaTableObject(l, index, out isUserData);
                            return isUserData ? obj : new LuaLib.LuaOnStackTable(l, index);
                        }
                    case lua.LUA_TNUMBER:
                        return l.tonumber(index);
                    case lua.LUA_TBOOLEAN:
                        return l.toboolean(index);
                    case lua.LUA_TNIL:
                        return null;
                    case lua.LUA_TNONE:
                        return null;
                    case lua.LUA_TLIGHTUSERDATA:
                        return l.touserdata(index);
                    case lua.LUA_TFUNCTION:
                        return new LuaLib.LuaOnStackFunc(l, index);
                    case lua.LUA_TTHREAD:
                        IntPtr lthd = l.tothread(index);
                        l.pushvalue(index);
                        int refid = l.refer();
                        return new LuaLib.LuaOnStackThread(refid, lthd);
                }
            }
            return null;
        }

        public static object GetLuaOrOnStack(this IntPtr l, int index)
        {
            if (l != IntPtr.Zero)
            {
                int typecode = l.type(index);
                switch (typecode)
                {
                    case lua.LUA_TUSERDATA:
                        if (IsUserDataTableRaw(l, index))
                        {
                            l.pushvalue(index);
                            l.GetField(-1, "__luastackonly");
                            bool isluastackonly = l.toboolean(-1);
                            l.pop(2);
                            if (isluastackonly)
                            {
                                return new LuaLib.LuaOnStackTable(l, index);
                            }
                            else
                            {
                                return new LuaLib.LuaTable(l, index);
                            }
                        }
                        else
                        {
                            return GetLuaRawObject(l, index);
                        }
                    case lua.LUA_TSTRING:
                        return l.GetString(index);
                    case lua.LUA_TTABLE:
                        {
                            bool isUserData;
                            var obj = GetLuaTableObject(l, index, out isUserData);
                            if (isUserData)
                            {
                                return obj;
                            }
                            else
                            {
                                l.pushvalue(index);
                                l.GetField(-1, "__luastackonly");
                                bool isluastackonly = l.toboolean(-1);
                                l.pop(2);
                                if (isluastackonly)
                                {
                                    return new LuaLib.LuaOnStackTable(l, index);
                                }
                                else
                                {
                                    return new LuaLib.LuaTable(l, index);
                                }
                            }
                        }
                    case lua.LUA_TNUMBER:
                        return l.tonumber(index);
                    case lua.LUA_TBOOLEAN:
                        return l.toboolean(index);
                    case lua.LUA_TNIL:
                        return null;
                    case lua.LUA_TNONE:
                        return null;
                    case lua.LUA_TLIGHTUSERDATA:
                        return l.touserdata(index);
                    case lua.LUA_TFUNCTION:
                        return new LuaLib.LuaFunc(l, index);
                    case lua.LUA_TTHREAD:
                        IntPtr lthd = l.tothread(index);
                        l.pushvalue(index);
                        int refid = l.refer();
                        return new LuaLib.LuaOnStackThread(refid, lthd);
                }
            }
            return null;
        }

        public static void GetLuaExplicit<T>(this IntPtr l, int index, out T val)
        {
            if (l != IntPtr.Zero)
            {
                if (typeof(T) == typeof(byte[]))
                {
                    if (l.IsString(index))
                    {
                        val = (T)(object)l.tolstring(index);
                        return;
                    }
                }
                else if (typeof(T) == typeof(LuaLib.LuaTable))
                {
                    if (l.istable(index) || IsUserDataTable(l, index))
                    {
                        val = (T)(object)new LuaLib.LuaTable(l, index);
                        return;
                    }
                }
                else if (typeof(T) == typeof(LuaLib.LuaOnStackTable))
                {
                    if (l.istable(index) || IsUserDataTable(l, index))
                    {
                        val = (T)(object)new LuaLib.LuaOnStackTable(l, index);
                        return;
                    }
                }
                else if (typeof(T) == typeof(LuaLib.LuaRawTable))
                {
                    if (l.istable(index)) // Raw table is not compatible with UserDataTable
                    {
                        val = (T)(object)new LuaLib.LuaRawTable(l, index);
                        return;
                    }
                }
                else if (typeof(T) == typeof(LuaLib.LuaOnStackRawTable))
                {
                    if (l.istable(index)) // Raw table is not compatible with UserDataTable
                    {
                        val = (T)(object)new LuaLib.LuaOnStackRawTable(l, index);
                        return;
                    }
                }
                else if (typeof(T) == typeof(LuaLib.LuaFunc))
                {
                    if (l.isfunction(index))
                    {
                        val = (T)(object)new LuaLib.LuaFunc(l, index);
                        return;
                    }
                }
                else if (typeof(T) == typeof(LuaLib.LuaOnStackFunc))
                {
                    if (l.isfunction(index))
                    {
                        val = (T)(object)new LuaLib.LuaOnStackFunc(l, index);
                        return;
                    }
                }
                else if (typeof(T) == typeof(IntPtr))
                {
                    if (l.isuserdata(index))
                    {
                        val = ConvertUtils.FakeConvert<IntPtr, T>(l.touserdata(index));
                        return;
                    }
                    else if (l.isthread(index))
                    {
                        val = ConvertUtils.FakeConvert<IntPtr, T>(l.tothread(index));
                        return;
                    }
                }

                GetLua<T>(l, index, out val);
                return;
            }
            val = default(T);
            return;
        }
        public static void GetLua<T>(this IntPtr l, int index, out T val)
        {
            // 1. trans stored in table
            var luatype = l.type(index);
            bool istable = luatype == lua.LUA_TTABLE;
            if (istable)
            {
                l.checkstack(2);
                l.pushvalue(index); // ud
                l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS); // ud #trans
                l.gettable(-2); // ud trans
                ILuaTrans trans = null;
                if (l.isuserdata(-1))
                {
                    trans = l.GetLuaLightObject(-1) as ILuaTrans;
                }
                l.pop(2);

                if (trans != null)
                {
                    var ttrans = trans as ILuaTrans<T>;
                    if (ttrans != null)
                    {
                        val = ttrans.GetLua(l, index);
                        return;
                    }
                    var mtrans = trans as ILuaTransMulti;
                    if (mtrans != null)
                    {
                        val = mtrans.GetLua<T>(l, index);
                        return;
                    }
                    if (!trans.Nonexclusive || typeof(T).IsAssignableFrom(trans.GetType(l, index)))
                    {
                        var raw = trans.GetLua(l, index);
                        //if (raw is T)
                        {
                            val = (T)raw;
                        }
                        //else
                        //{
                        //    val = default(T);
                        //}
                        return;
                    }
                }
            }
            // 2. check lua-native hub
            if (luatype > 0) // !l.isnoneornil
            {
                object func;
                {
                    LuaPushNative._NativePushLuaFuncs.TryGetValue(typeof(T), out func);
                    ILuaTrans<T> gfunc = func as ILuaTrans<T>;
                    if (gfunc != null)
                    {
                        val = gfunc.GetLua(l, index);
                        return;
                    }
                }
                if (typeof(T).IsEnum())
                {
                    if (luatype == lua.LUA_TNUMBER || luatype == lua.LUA_TSTRING)
                    {
                        //var hub = LuaTypeHub.GetTypeHub(typeof(T)) as LuaTypeHub.TypeHubClonedValuePrecompiled<T>;
                        //if (hub != null)
                        //{ // this should not happen...
                        //    val = hub.GetLuaChecked(l, index);
                        //    return;
                        //}
                        if (luatype == lua.LUA_TNUMBER)
                        {
                            var num = l.tonumber(index);
#if CONVERT_ENUM_SAFELY
                            val = (T)Enum.ToObject(typeof(T), (ulong)num);
#else
                            val = EnumUtils.ConvertToEnumForcibly<T>((ulong)num);
#endif
                            return;
                        }
                        else
                        {
                            var str = l.GetString(index);
                            val = EnumUtils.ConvertStrToEnum<T>(str);
                            return;
                        }
                    }
                }
                else if ((luatype == lua.LUA_TTABLE || luatype == lua.LUA_TUSERDATA && IsUserDataTableRaw(l, index)) && typeof(LuaLib.ILuaWrapper).IsAssignableFrom(typeof(T)))
                { // the BaseLuaWrapper is not initialized?
                    try
                    {
                        val = Activator.CreateInstance<T>();
                        var wrapper = (LuaLib.ILuaWrapper)(object)val;
                        wrapper.Binding = new LuaLib.LuaTable(l, index);
                        return;
                    }
                    catch (Exception e)
                    { // we can not create instance of wrapper?
                        PlatDependant.LogError(e);
                    }
                }
                else if (!istable)
                {
                    var uutype = Nullable.GetUnderlyingType(typeof(T));
                    if (uutype != null && uutype.IsEnum())
                    { // Nullable<Enum>
                        if (luatype == lua.LUA_TNUMBER)
                        {
                            var num = l.tonumber(index);
                            var eval = Enum.ToObject(uutype, (ulong)num);
                            val = (T)eval;
                            return;
                        }
                        else
                        {
                            var str = l.GetString(index);
                            var eval = Enum.Parse(uutype, str);
                            val = (T)eval;
                            return;
                        }
                    }
                }
            }
            // 3. for sealed CLR type and lua-table. we can get LuaTypeHub from CLR-type. this is commonly for Protocols.
            if (istable && typeof(T).IsSealed)
            {
                var uutype = Nullable.GetUnderlyingType(typeof(T));
                var trans = LuaTypeHub.GetTypeHub(uutype ?? typeof(T));
                var ttrans = trans as ILuaTrans<T>;
                if (ttrans != null)
                {
                    val = ttrans.GetLua(l, index);
                    return;
                }
                var mtrans = trans as ILuaTransMulti;
                if (mtrans != null)
                {
                    val = mtrans.GetLua<T>(l, index);
                    return;
                }
                var raw = trans.GetLua(l, index);
                if (raw is T)
                {
                    val = (T)raw;
                    return;
                }
                //else
                //{
                //    val = default(T);
                //}
                //return;
            }
            // 4. use non-generic GetLua
            if (typeof(T) == typeof(object))
            {
                var raw = GetLuaOrOnStack(l, index);
                val = (T)raw;
                return;
            }
            else
            {
                var raw = GetLua(l, index);
                if (raw == null)
                {
                    val = default(T);
                    return;
                }
                else
                {
                    if (raw is T)
                    {
                        val = (T)raw;
                        return;
                    }
                    else if (raw is double)
                    {
                        if (IsNumeric(typeof(T)))
                        {
                            val = (T)Convert.ChangeType(raw, typeof(T));
                            return;
                        }
                    }
                    else if (raw is LuaLib.BaseLua)
                    {
                        if (typeof(T).IsSubclassOf(typeof(Delegate)))
                        {
                            val = (T)(object)LuaDelegateGenerator.CreateDelegate(typeof(T), raw as LuaLib.BaseLua);
                            return;
                        }
                        //else if (typeof(LuaLib.ILuaWrapper).IsAssignableFrom(typeof(T)))
                        //{ // the BaseLuaWrapper is not initialized?
                        //    try
                        //    {
                        //        val = Activator.CreateInstance<T>();
                        //        var wrapper = (LuaLib.ILuaWrapper)(object)val;
                        //        wrapper.Binding = raw as LuaLib.BaseLua;
                        //        return;
                        //    }
                        //    catch (Exception e)
                        //    { // we can not create instance of wrapper?
                        //        PlatDependant.LogError(e);
                        //    }
                        //}
                    }
                }

                val = default(T);
                return;
            }
        }
        public static T GetLua<T>(this IntPtr l, int index)
        {
            T val;
            GetLua<T>(l, index, out val);
            return val;
        }

#region Get Methods for Native Lua Types
        public static void GetLua(this IntPtr l, int index, out byte[] val)
        {
            if (l.IsString(index))
            {
                val = l.tolstring(index);
                return;
            }
            else
            {
                var raw = GetLuaObject(l, index);
                val = raw as byte[];
                return;
            }
        }
        public static void GetLua(this IntPtr l, int index, out string val)
        {
            if (l.IsString(index))
            {
                val = l.GetString(index);
                return;
            }
            else
            {
                var raw = GetLuaObject(l, index);
                val = raw as string;
                return;
            }
        }
        public static void GetLua(this IntPtr l, int index, out IntPtr val)
        {
            if (l.isuserdata(index))
            {
                val = l.touserdata(index);
                return;
            }
            else if (l.isthread(index))
            {
                val = l.tothread(index);
                return;
            }
            else if (l.IsObject(index))
            {
                var obj = GetLuaObject(l, index);
                if (obj is IntPtr)
                {
                    val = (IntPtr)obj;
                }
            }
            val = IntPtr.Zero;
            return;
        }
        public static void GetLua(this IntPtr l, int index, out IntPtr? val)
        {
            if (l.isnoneornil(index))
            {
                val = null;
            }
            else
            {
                IntPtr raw;
                GetLua(l, index, out raw);
                val = raw;
            }
            return;
        }
        public static void GetLua(this IntPtr l, int index, out UIntPtr val)
        {
            IntPtr raw;
            GetLua(l, index, out raw);
            val = (UIntPtr)(ulong)raw;
            return;
        }
        public static void GetLua(this IntPtr l, int index, out UIntPtr? val)
        {
            if (l.isnoneornil(index))
            {
                val = null;
            }
            else
            {
                UIntPtr raw;
                GetLua(l, index, out raw);
                val = raw;
            }
            return;
        }
        public static void GetLua(this IntPtr l, int index, out bool val)
        {
            if (l.isboolean(index))
            {
                val = l.toboolean(index);
            }
            else
            {
                val = l.GetLua<bool>(index);
            }
        }
        public static void GetLua(this IntPtr l, int index, out bool? val)
        {
            if (l.isnoneornil(index))
            {
                val = null;
            }
            else
            {
                bool raw;
                GetLua(l, index, out raw);
                val = raw;
            }
            return;
        }
        public static void GetLua(this IntPtr l, int index, out byte val)
        {
            if (l.IsNumber(index))
            {
                val = (byte)l.tonumber(index);
            }
            else
            {
                val = l.GetLua<byte>(index);
            }
        }
        public static void GetLua(this IntPtr l, int index, out byte? val)
        {
            if (l.isnoneornil(index))
            {
                val = null;
            }
            else
            {
                byte raw;
                GetLua(l, index, out raw);
                val = raw;
            }
            return;
        }
        public static void GetLua(this IntPtr l, int index, out char val)
        {
            if (l.IsNumber(index))
            {
                val = (char)l.tonumber(index);
            }
            else
            {
                val = l.GetLua<char>(index);
            }
        }
        public static void GetLua(this IntPtr l, int index, out char? val)
        {
            if (l.isnoneornil(index))
            {
                val = null;
            }
            else
            {
                char raw;
                GetLua(l, index, out raw);
                val = raw;
            }
            return;
        }
        public static void GetLua(this IntPtr l, int index, out decimal val)
        {
            if (l.IsNumber(index))
            {
                val = (decimal)l.tonumber(index);
            }
            else if (l.IsUserData(index))
            {
                val = l.GetLuaRawObject(index).As<decimal>();
            }
            else
            {
                val = l.GetLua<decimal>(index);
            }
        }
        public static void GetLua(this IntPtr l, int index, out decimal? val)
        {
            if (l.isnoneornil(index))
            {
                val = null;
            }
            else
            {
                decimal raw;
                GetLua(l, index, out raw);
                val = raw;
            }
            return;
        }
        public static void GetLua(this IntPtr l, int index, out double val)
        {
            if (l.IsNumber(index))
            {
                val = l.tonumber(index);
            }
            else
            {
                val = l.GetLua<double>(index);
            }
        }
        public static void GetLua(this IntPtr l, int index, out double? val)
        {
            if (l.isnoneornil(index))
            {
                val = null;
            }
            else
            {
                double raw;
                GetLua(l, index, out raw);
                val = raw;
            }
            return;
        }
        public static void GetLua(this IntPtr l, int index, out short val)
        {
            if (l.IsNumber(index))
            {
                val = (short)l.tonumber(index);
            }
            else
            {
                val = l.GetLua<short>(index);
            }
        }
        public static void GetLua(this IntPtr l, int index, out short? val)
        {
            if (l.isnoneornil(index))
            {
                val = null;
            }
            else
            {
                short raw;
                GetLua(l, index, out raw);
                val = raw;
            }
            return;
        }
        public static void GetLua(this IntPtr l, int index, out int val)
        {
            if (l.IsNumber(index))
            {
                val = (int)l.tonumber(index);
            }
            else
            {
                val = l.GetLua<int>(index);
            }
        }
        public static void GetLua(this IntPtr l, int index, out int? val)
        {
            if (l.isnoneornil(index))
            {
                val = null;
            }
            else
            {
                int raw;
                GetLua(l, index, out raw);
                val = raw;
            }
            return;
        }
        public static void GetLua(this IntPtr l, int index, out long val)
        {
            if (l.IsNumber(index))
            {
                val = (long)l.tonumber(index);
            }
            else if (l.IsUserData(index))
            {
                val = l.GetLuaRawObject(index).As<long>();
            }
            else
            {
                val = l.GetLua<long>(index);
            }
        }
        public static void GetLua(this IntPtr l, int index, out long? val)
        {
            if (l.isnoneornil(index))
            {
                val = null;
            }
            else
            {
                long raw;
                GetLua(l, index, out raw);
                val = raw;
            }
            return;
        }
        public static void GetLua(this IntPtr l, int index, out sbyte val)
        {
            if (l.IsNumber(index))
            {
                val = (sbyte)l.tonumber(index);
            }
            else
            {
                val = l.GetLua<sbyte>(index);
            }
        }
        public static void GetLua(this IntPtr l, int index, out sbyte? val)
        {
            if (l.isnoneornil(index))
            {
                val = null;
            }
            else
            {
                sbyte raw;
                GetLua(l, index, out raw);
                val = raw;
            }
            return;
        }
        public static void GetLua(this IntPtr l, int index, out float val)
        {
            if (l.IsNumber(index))
            {
                val = (float)l.tonumber(index);
            }
            else
            {
                val = l.GetLua<float>(index);
            }
        }
        public static void GetLua(this IntPtr l, int index, out float? val)
        {
            if (l.isnoneornil(index))
            {
                val = null;
            }
            else
            {
                float raw;
                GetLua(l, index, out raw);
                val = raw;
            }
            return;
        }
        public static void GetLua(this IntPtr l, int index, out ushort val)
        {
            if (l.IsNumber(index))
            {
                val = (ushort)l.tonumber(index);
            }
            else
            {
                val = l.GetLua<ushort>(index);
            }
        }
        public static void GetLua(this IntPtr l, int index, out ushort? val)
        {
            if (l.isnoneornil(index))
            {
                val = null;
            }
            else
            {
                ushort raw;
                GetLua(l, index, out raw);
                val = raw;
            }
            return;
        }
        public static void GetLua(this IntPtr l, int index, out uint val)
        {
            if (l.IsNumber(index))
            {
                val = (uint)l.tonumber(index);
            }
            else
            {
                val = l.GetLua<uint>(index);
            }
        }
        public static void GetLua(this IntPtr l, int index, out uint? val)
        {
            if (l.isnoneornil(index))
            {
                val = null;
            }
            else
            {
                uint raw;
                GetLua(l, index, out raw);
                val = raw;
            }
            return;
        }
        public static void GetLua(this IntPtr l, int index, out ulong val)
        {
            if (l.IsNumber(index))
            {
                val = (ulong)l.tonumber(index);
            }
            else if (l.IsUserData(index))
            {
                val = l.GetLuaRawObject(index).As<ulong>();
            }
            else
            {
                val = l.GetLua<ulong>(index);
            }
        }
        public static void GetLua(this IntPtr l, int index, out ulong? val)
        {
            if (l.isnoneornil(index))
            {
                val = null;
            }
            else
            {
                ulong raw;
                GetLua(l, index, out raw);
                val = raw;
            }
            return;
        }
#endregion

#region Nullables
#endregion
    }
}