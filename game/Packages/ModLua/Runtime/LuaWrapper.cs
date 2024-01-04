using System;
using System.Collections;
using System.Collections.Generic;
using LuaLib;

using lua = LuaLib.LuaCoreLib;
using lual = LuaLib.LuaAuxLib;
using luae = LuaLib.LuaLibEx;
using static LuaLib.LuaPack;

namespace LuaLib
{
    public class BaseLua : ScriptDynamic, IDisposable
    {
        protected internal IntPtr _L;
        protected internal int _R;

        protected internal LuaRef Ref
        {
            get
            {
                return _Binding as LuaRef;
            }
            set
            {
                _Binding = value;
            }
        }
        public IntPtr L
        {
            get { return _L; }
            internal protected set
            {
                var old = _L;
                _L = value;
                if (Ref == null)
                {
                    if (value != IntPtr.Zero && _R != 0)
                    {
                        var man = value.GetOrCreateRefMan();
                        Ref = man.GetLuaRef();
                        Ref.L = value;
                        Ref.Refid = _R;
                    }
                }
                else
                {
                    if (value == IntPtr.Zero)
                    {
                        //_R = 0;
                        Ref.Dispose();
                        Ref = null;
                    }
                    else
                    {
                        if (old != value)
                        {
                            DynamicHelper.LogError("Try to set LuaRef's L to another value.");
                        }
                    }
                }
            }
        }
        public override int Refid
        {
            get { return _R; }
            protected internal set
            {
                var old = _R;
                _R = value;
                if (Ref == null)
                {
                    if (_L != IntPtr.Zero && value != 0)
                    {
                        var man = _L.GetOrCreateRefMan();
                        Ref = man.GetLuaRef();
                        Ref.L = _L;
                        Ref.Refid = value;
                    }
                }
                else
                {
                    if (value == 0)
                    {
                        //_L = IntPtr.Zero;
                        Ref.Dispose();
                        Ref = null;
                    }
                    else
                    {
                        if (old != value)
                        {
                            DynamicHelper.LogError("Try to set LuaRef's Refid to another value.");
                        }
                    }
                }
            }
        }
        public virtual bool IsClosed { get { return object.ReferenceEquals(Ref, null) || Ref.IsClosed; } }

        public override string ToString()
        {
            return "LuaRef:" + Refid.ToString();
        }
        public override bool Equals(object obj)
        {
            if (obj is BaseLua)
            {
                return Refid == ((BaseLua)obj).Refid && L == ((BaseLua)obj).L;
            }
            return false;
        }
        public override int GetHashCode()
        {
            return Refid ^ L.GetHashCode();
        }

        public BaseLua() { }
        public BaseLua(IntPtr l, int refid)
        {
            L = l;
            Refid = refid;
        }
        public virtual void Dispose()
        {
            if (Ref != null)
            {
                Ref.Dispose();
                Ref = null;
            }
        }

        protected internal override object ConvertBinding(Type type)
        {
            if (type.IsSubclassOf(typeof(Delegate)))
            {
                return LuaLib.LuaDelegateGenerator.CreateDelegate(type, this);
            }
            else if (type == typeof(bool))
            {
                if (L != IntPtr.Zero && Refid != 0)
                {
                    L.getref(Refid);
                    bool rv = !L.isnoneornil(-1) && !(L.isboolean(-1) && !L.toboolean(-1));
                    L.pop(1);
                    return rv;
                }
                return false;
            }
            else if (typeof(ILuaWrapper).IsAssignableFrom(type))
            {
                try
                {
                    var val = Activator.CreateInstance(type);
                    var wrapper = (ILuaWrapper)val;
                    wrapper.Binding = this;
                    return wrapper;
                }
                catch (Exception e)
                { // we can not create instance of wrapper?
                    DynamicHelper.LogError(e);
                    return null;
                }
            }
            DynamicHelper.LogInfo("__convert(" + type.ToString() + ") meta-method Not Implemented.");
            return null;
        }
        public virtual void PushToLua(IntPtr l)
        {
            l.getref(Refid);
        }

        public void Call<TIn>(string func, TIn args)
            where TIn : struct, ILuaPack
        { // this is fix for: Call("func", Pack(XXX)) will select Call<P0, P1>(P0 p0, P1 p1)
            var l = L;
            PushToLua(l);
            l.GetField(-1, func);
            l.remove(-2);
            LuaFuncExHelper.PushArgsAndCall(l, args);
        }
        public void Call()
        {
            var l = L;
            PushToLua(l);
            LuaFuncHelper.PushArgsAndCall(l);
        }
        public void Call<P0>(P0 p0)
        {
            var l = L;
            PushToLua(l);
            LuaFuncHelper.PushArgsAndCall(l, p0);
        }
        public void Call<P0, P1>(P0 p0, P1 p1)
        {
            var l = L;
            PushToLua(l);
            LuaFuncHelper.PushArgsAndCall(l, p0, p1);
        }
        public void Call<P0, P1, P2>(P0 p0, P1 p1, P2 p2)
        {
            var l = L;
            PushToLua(l);
            LuaFuncHelper.PushArgsAndCall(l, p0, p1, p2);
        }
        public void Call<P0, P1, P2, P3>(P0 p0, P1 p1, P2 p2, P3 p3)
        {
            var l = L;
            PushToLua(l);
            LuaFuncHelper.PushArgsAndCall(l, p0, p1, p2, p3);
        }
        public void Call<P0, P1, P2, P3, P4>(P0 p0, P1 p1, P2 p2, P3 p3, P4 p4)
        {
            var l = L;
            PushToLua(l);
            LuaFuncHelper.PushArgsAndCall(l, p0, p1, p2, p3, p4);
        }
        public void Call<P0, P1, P2, P3, P4, P5>(P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5)
        {
            var l = L;
            PushToLua(l);
            LuaFuncHelper.PushArgsAndCall(l, p0, p1, p2, p3, p4, p5);
        }
        public void Call<P0, P1, P2, P3, P4, P5, P6>(P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6)
        {
            var l = L;
            PushToLua(l);
            LuaFuncHelper.PushArgsAndCall(l, p0, p1, p2, p3, p4, p5, p6);
        }
        public void Call<P0, P1, P2, P3, P4, P5, P6, P7>(P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7)
        {
            var l = L;
            PushToLua(l);
            LuaFuncHelper.PushArgsAndCall(l, p0, p1, p2, p3, p4, p5, p6, p7);
        }
        public void Call<P0, P1, P2, P3, P4, P5, P6, P7, P8>(P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7, P8 p8)
        {
            var l = L;
            PushToLua(l);
            LuaFuncHelper.PushArgsAndCall(l, p0, p1, p2, p3, p4, p5, p6, p7, p8);
        }
        public void Call<P0, P1, P2, P3, P4, P5, P6, P7, P8, P9>(P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7, P8 p8, P9 p9)
        {
            var l = L;
            PushToLua(l);
            LuaFuncHelper.PushArgsAndCall(l, p0, p1, p2, p3, p4, p5, p6, p7, p8, p9);
        }
        public bool Call<R>(out R r)
        {
            var l = L;
            PushToLua(l);
            return LuaFuncHelper.PushArgsAndCall(l, out r);
        }
        public bool Call<R, P0>(out R r, P0 p0)
        {
            var l = L;
            PushToLua(l);
            return LuaFuncHelper.PushArgsAndCall(l, out r, p0);
        }
        public bool Call<R, P0, P1>(out R r, P0 p0, P1 p1)
        {
            var l = L;
            PushToLua(l);
            return LuaFuncHelper.PushArgsAndCall(l, out r, p0, p1);
        }
        public bool Call<R, P0, P1, P2>(out R r, P0 p0, P1 p1, P2 p2)
        {
            var l = L;
            PushToLua(l);
            return LuaFuncHelper.PushArgsAndCall(l, out r, p0, p1, p2);
        }
        public bool Call<R, P0, P1, P2, P3>(out R r, P0 p0, P1 p1, P2 p2, P3 p3)
        {
            var l = L;
            PushToLua(l);
            return LuaFuncHelper.PushArgsAndCall(l, out r, p0, p1, p2, p3);
        }
        public bool Call<R, P0, P1, P2, P3, P4>(out R r, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4)
        {
            var l = L;
            PushToLua(l);
            return LuaFuncHelper.PushArgsAndCall(l, out r, p0, p1, p2, p3, p4);
        }
        public bool Call<R, P0, P1, P2, P3, P4, P5>(out R r, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5)
        {
            var l = L;
            PushToLua(l);
            return LuaFuncHelper.PushArgsAndCall(l, out r, p0, p1, p2, p3, p4, p5);
        }
        public bool Call<R, P0, P1, P2, P3, P4, P5, P6>(out R r, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6)
        {
            var l = L;
            PushToLua(l);
            return LuaFuncHelper.PushArgsAndCall(l, out r, p0, p1, p2, p3, p4, p5, p6);
        }
        public bool Call<R, P0, P1, P2, P3, P4, P5, P6, P7>(out R r, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7)
        {
            var l = L;
            PushToLua(l);
            return LuaFuncHelper.PushArgsAndCall(l, out r, p0, p1, p2, p3, p4, p5, p6, p7);
        }
        public bool Call<R, P0, P1, P2, P3, P4, P5, P6, P7, P8>(out R r, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7, P8 p8)
        {
            var l = L;
            PushToLua(l);
            return LuaFuncHelper.PushArgsAndCall(l, out r, p0, p1, p2, p3, p4, p5, p6, p7, p8);
        }
        public bool Call<R, P0, P1, P2, P3, P4, P5, P6, P7, P8, P9>(out R r, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7, P8 p8, P9 p9)
        {
            var l = L;
            PushToLua(l);
            return LuaFuncHelper.PushArgsAndCall(l, out r, p0, p1, p2, p3, p4, p5, p6, p7, p8, p9);
        }
        public R Call<R>()
        {
            R r;
            Call(out r);
            return r;
        }
        public R Call<R, P0>(P0 p0)
        {
            R r;
            Call(out r, p0);
            return r;
        }
        public R Call<R, P0, P1>(P0 p0, P1 p1)
        {
            R r;
            Call(out r, p0, p1);
            return r;
        }
        public R Call<R, P0, P1, P2>(P0 p0, P1 p1, P2 p2)
        {
            R r;
            Call(out r, p0, p1, p2);
            return r;
        }
        public R Call<R, P0, P1, P2, P3>(P0 p0, P1 p1, P2 p2, P3 p3)
        {
            R r;
            Call(out r, p0, p1, p2, p3);
            return r;
        }
        public R Call<R, P0, P1, P2, P3, P4>(P0 p0, P1 p1, P2 p2, P3 p3, P4 p4)
        {
            R r;
            Call(out r, p0, p1, p2, p3, p4);
            return r;
        }
        public R Call<R, P0, P1, P2, P3, P4, P5>(P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5)
        {
            R r;
            Call(out r, p0, p1, p2, p3, p4, p5);
            return r;
        }
        public R Call<R, P0, P1, P2, P3, P4, P5, P6>(P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6)
        {
            R r;
            Call(out r, p0, p1, p2, p3, p4, p5, p6);
            return r;
        }
        public R Call<R, P0, P1, P2, P3, P4, P5, P6, P7>(P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7)
        {
            R r;
            Call(out r, p0, p1, p2, p3, p4, p5, p6, p7);
            return r;
        }
        public R Call<R, P0, P1, P2, P3, P4, P5, P6, P7, P8>(P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7, P8 p8)
        {
            R r;
            Call(out r, p0, p1, p2, p3, p4, p5, p6, p7, p8);
            return r;
        }
        public R Call<R, P0, P1, P2, P3, P4, P5, P6, P7, P8, P9>(P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7, P8 p8, P9 p9)
        {
            R r;
            Call(out r, p0, p1, p2, p3, p4, p5, p6, p7, p8, p9);
            return r;
        }
    }

    public class BaseLuaOnStack : BaseLua
    {
        public override string ToString()
        {
            return "LuaOnStack:" + StackPos.ToString();
        }
        public override bool Equals(object obj)
        {
            if (obj is BaseLuaOnStack)
            {
                return StackPos == ((BaseLuaOnStack)obj).StackPos && L == ((BaseLuaOnStack)obj).L;
            }
            return false;
        }
        public override int GetHashCode()
        {
            return StackPos ^ L.GetHashCode();
        }
        public override int Refid
        {
            get
            {
                return 0;
            }
            protected internal set
            {
            }
        }
        public override bool IsClosed { get { return false; } }

        public virtual int StackPos
        {
            //get
            //{
            //    if (_Binding == null)
            //        return 0;
            //    if (_Binding is int)
            //        return (int)_Binding;
            //    return 0;
            //}
            //protected internal set
            //{
            //    _Binding = value;
            //}
            get; set;
        }

        protected internal override object ConvertBinding(Type type)
        {
            if (type == typeof(bool))
            {
                if (L != IntPtr.Zero && StackPos != 0)
                {
                    bool rv = !L.isnoneornil(StackPos) && !(L.isboolean(StackPos) && !L.toboolean(StackPos));
                    return rv;
                }
                return false;
            }
            else if (typeof(ILuaWrapper).IsAssignableFrom(type))
            {
                try
                {
                    var val = Activator.CreateInstance(type);
                    var wrapper = (ILuaWrapper)val;
                    wrapper.Binding = this;
                    return wrapper;
                }
                catch (Exception e)
                { // we can not create instance of wrapper?
                    DynamicHelper.LogError(e);
                    return null;
                }
            }
            DynamicHelper.LogInfo("__convert(" + type.ToString() + ") meta-method Not Implemented.");
            return null;
        }
        public override void PushToLua(IntPtr l)
        {
            l.pushvalue(StackPos);
        }
    }
}

namespace LuaLib
{
    public static partial class LuaHub
    {
        public static void PushLua(this IntPtr l, BaseLuaOnStack val)
        {
            PushLua(l, (BaseLua)val);
        }
        public static void PushLua(this IntPtr l, BaseLua val)
        {
            if (ReferenceEquals(val, null) || val.IsClosed)
            {
                l.pushnil();
            }
            else
            {
                val.PushToLua(l);
            }
        }
        public static void GetLua(this IntPtr l, int index, out BaseLua val)
        {
            l.pushvalue(index);
            val = new BaseLua(l, l.refer());
        }
        public static void GetLua(this IntPtr l, int index, out BaseLuaOnStack val)
        {
            val = new BaseLuaOnStack() { L = l, StackPos = l.NormalizeIndex(index) };
        }

        private class LuaPushNative_BaseLuaOnStack : LuaPushNativeBase<LuaLib.BaseLuaOnStack>
        {
            public override BaseLuaOnStack GetLua(IntPtr l, int index)
            {
                return new BaseLuaOnStack() { L = l, StackPos = index };
            }
            public override IntPtr PushLua(IntPtr l, LuaLib.BaseLuaOnStack val)
            {
                l.pushvalue(val.StackPos);
                return IntPtr.Zero;
            }
        }
        private static LuaPushNative_BaseLuaOnStack ___tpn_BaseLuaOnStack = new LuaPushNative_BaseLuaOnStack();
        private class LuaPushNative_BaseLua : LuaPushNativeBase<LuaLib.BaseLua>
        {
            public override BaseLua GetLua(IntPtr l, int index)
            {
                l.pushvalue(index);
                int refid = l.refer();
                return new BaseLua(l, refid);
            }
            public override IntPtr PushLua(IntPtr l, LuaLib.BaseLua val)
            {
                l.getref(val.Refid);
                return IntPtr.Zero;
            }
        }
        private static LuaPushNative_BaseLua ___tpn_BaseLua = new LuaPushNative_BaseLua();
    }
}

// Lua Wrapper. Its data and functions are in lua but we can use it in C# by this wrapper.
// When we push this wrapper to lua, we actually push its binding LuaTable
namespace LuaLib
{
    public interface ILuaWrapper
    {
        BaseLua Binding { get; set; }
        string LuaFile { get; }
    }
    public class BaseLuaWrapper : ILuaWrapper, IDisposable, IExpando
    {
        protected BaseLua _Binding;
        public virtual BaseLua Binding
        {
            get { return _Binding; }
            set
            {
                _Binding = value;
                if (!ReferenceEquals(value, null))
                {
                    var l = L;
                    using (var lr = l.CreateStackRecover())
                    {
                        l.PushLua(value);
                        if (l.IsUserDataTable(-1))
                        {
                            l.PushUserDataTableRaw(-1);
                            l.remove(-2);
                            l.pushvalue(-1);
                            _Binding = new BaseLua(l, l.refer());
                        }
                        if (_CachedFields != null)
                        {
                            foreach (var kvp in _CachedFields)
                            {
                                l.PushString(kvp.Key);
                                l.PushLua(kvp.Value);
                                l.settable(-3);
                            }
                            _CachedFields = null;
                        }
                    }
                }
            }
        }
        public virtual string LuaFile { get; protected set; }
        public IntPtr L { get { return ReferenceEquals(Binding, null) ? IntPtr.Zero : Binding.L; } }
        protected virtual bool ShouldCheckLuaHubSub { get { return true; } }

        public BaseLuaWrapper()
        {
            if (ShouldCheckLuaHubSub)
            {
                CheckLuaHubSub(this);
            }
        }
        public BaseLuaWrapper(IntPtr l) : this()
        {
            this.BindLua(l);
        }
        protected internal static LuaTypeHub.TypeHubValueType CheckLuaHubSub(object thiz)
        {
            var type = thiz.GetType();
            LuaTypeHub.TypeHubValueType hub;
            if (!LuaHubSubs.TryGetValue(type, out hub))
            {
                // Notice: the thiz is the instance of type, we donot need create another instance of type.
                //try
                //{
                //    LuaLib.ILuaWrapper wrapper = (LuaLib.ILuaWrapper)Activator.CreateInstance(type);
                //    if (LuaHubSubs.TryGetValue(type, out hub))
                //    {
                //        return hub;
                //    }
                //}
                //catch (Exception e)
                //{
                //    DynamicHelper.LogError(e);
                //    //return null;
                //}

                try
                {
                    hub = (LuaTypeHub.TypeHubValueType)Activator.CreateInstance(typeof(LuaHub.BaseLuaWrapperHub<>).MakeGenericType(type));
                    LuaHubSubs[type] = hub;
                }
                catch (Exception e)
                {
                    DynamicHelper.LogError(e);
                    return null;
                }
            }
            return hub;
        }
        protected internal static LuaTypeHub.TypeHubValueType CheckLuaHubSub(Type type)
        {
            LuaTypeHub.TypeHubValueType hub;
            if (!LuaHubSubs.TryGetValue(type, out hub))
            {
                if (!type.IsAbstract && !type.IsInterface)
                {
                    try
                    {
                        LuaLib.ILuaWrapper wrapper = (LuaLib.ILuaWrapper)Activator.CreateInstance(type);
                        if (LuaHubSubs.TryGetValue(type, out hub))
                        {
                            return hub;
                        }
                    }
                    catch (Exception e)
                    {
                        DynamicHelper.LogError(e);
                        //return null;
                    }
                }

                try
                {
                    hub = (LuaTypeHub.TypeHubValueType)Activator.CreateInstance(typeof(LuaHub.BaseLuaWrapperHub<>).MakeGenericType(type));
                    LuaHubSubs[type] = hub;
                }
                catch (Exception e)
                {
                    DynamicHelper.LogError(e);
                    return null;
                }
            }
            return hub;
        }
        protected internal static readonly Dictionary<Type, LuaTypeHub.TypeHubValueType> LuaHubSubs = new Dictionary<Type, LuaTypeHub.TypeHubValueType>();
        private static LuaHub.BaseLuaWrapperHub<BaseLuaWrapper> LuaHubSub = new LuaHub.BaseLuaWrapperHub<BaseLuaWrapper>();

        protected Dictionary<string, object> _CachedFields;
        public T Get<T>(string field)
        {
            if (ReferenceEquals(Binding, null))
            {
                object val;
                if (_CachedFields == null)
                {
                    return default(T);
                }
                else
                {
                    _CachedFields.TryGetValue(field, out val);
                    return val is T ? (T)val : default(T);
                }
            }
            else
            {
                var l = Binding.L;
                using (var lr = l.CreateStackRecover())
                {
                    l.PushLua(Binding);
                    T val;
                    l.GetTable(out val, -1, field);
                    return val;
                }
            }
        }
        public void Set<T>(string field, T val)
        {
            if (ReferenceEquals(Binding, null))
            {
                if (_CachedFields == null)
                {
                    _CachedFields = new Dictionary<string, object>();
                }
                _CachedFields[field] = val;
            }
            else
            {
                var l = Binding.L;
                using (var lr = l.CreateStackRecover())
                {
                    l.PushLua(Binding);
                    l.SetTable(-1, Pack(val), field);
                }
            }
        }
        public T GetOrCreate<T>(string field)
            where T : ILuaWrapper, new()
        {
            if (ReferenceEquals(Binding, null))
            {
                if (_CachedFields == null)
                {
                    _CachedFields = new Dictionary<string, object>();
                }
                object val;
                if (!_CachedFields.TryGetValue(field, out val) || !(val is T))
                {
                    val = new T();
                    _CachedFields[field] = val;
                }
                return (T)val;
            }
            else
            {
                var l = Binding.L;
                using (var lr = l.CreateStackRecover())
                {
                    l.PushLua(Binding);
                    T val;
                    l.GetTable(out val, -1, field);
                    if (ReferenceEquals(val, null))
                    {
                        val = new T();
                        val.BindLua(l);
                        l.SetTable(-1, Pack(val), field);
                    }
                    return val;
                }
            }
        }

        public virtual void Dispose()
        {
            if (!ReferenceEquals(Binding, null))
            {
                Binding.Dispose();
                Binding = null;
            }
        }

        BaseDynamic IExpando.Core { get { return _Binding; } }
        IFieldsProvider IExpando.Extra { get { return new DictionaryFieldsProvider(_CachedFields); } }
    }
    public class BaseLuaWrapper<T> : BaseLuaWrapper where T : BaseLuaWrapper, new()
    {
        protected override bool ShouldCheckLuaHubSub { get { return false; } }
        public BaseLuaWrapper() : base() { }
        public BaseLuaWrapper(IntPtr l) : base(l) { }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            BaseLua b1 = Binding;
            BaseLua b2 = null;
            var wrapper = obj as ILuaWrapper;
            if (!ReferenceEquals(wrapper, null))
            {
                b2 = wrapper.Binding;
            }
            else
            {
                b2 = obj as BaseLua;
                if (ReferenceEquals(b2, null))
                {
                    return false;
                }
            }
            if (ReferenceEquals(b1, null))
            {
                return ReferenceEquals(b2, null);
            }
            else
            {
                return b1.Equals(b2);
            }
        }
        public override int GetHashCode()
        {
            if (ReferenceEquals(Binding, null))
            {
                return 0;
            }
            else
            {
                return Binding.GetHashCode();
            }
        }

        public static bool operator ==(BaseLuaWrapper<T> w1, object w2)
        {
            bool w1null = ReferenceEquals(w1, null);
            bool w2null = ReferenceEquals(w2, null);
            if (w1null || w2null)
            {
                return w1null == w2null;
            }
            return w1.Equals(w2);
        }
        public static bool operator !=(BaseLuaWrapper<T> w1, object w2)
        {
            bool w1null = ReferenceEquals(w1, null);
            bool w2null = ReferenceEquals(w2, null);
            if (w1null || w2null)
            {
                return w1null != w2null;
            }
            return !w1.Equals(w2);
        }

        protected static LuaHub.BaseLuaWrapperHub<T> LuaHubSub = new LuaHub.BaseLuaWrapperHub<T>(); // TODO: we must create one instance of the wrapper in order to make this LuaHubSub available. Should we improve this? (By adding RuntimeInitializeOnLoad)
    }

    public static class LuaWrapperExtensions
    {
        private static IntPtr GetNativeTrans(Type t)
        {
            object ntrans;
            if (LuaHub.LuaPushNative._NativePushLuaFuncs.TryGetValue(t, out ntrans))
            {
                ILuaHandle hntrans = ntrans as ILuaHandle;
                if (hntrans != null)
                {
                    return hntrans.r;
                }
            }
            return IntPtr.Zero;
        }

        public static bool BindLua<TIn>(this ILuaWrapper thiz, IntPtr l, TIn args)
            where TIn : struct, ILuaPack
        {
            if (thiz == null)
            {
                return false;
            }
            if (ReferenceEquals(thiz.Binding, null))
            {
                if (string.IsNullOrEmpty(thiz.LuaFile))
                {
                    l.newtable();
                }
                else if (!l.NewTable(thiz.LuaFile, args)) // ud
                {
                    l.pop(1); // X
                    if (args.Length == 0)
                    {
                        l.Require(thiz.LuaFile); // file
                        if (!l.istable(-1) && !l.IsUserDataTable(-1))
                        {
                            l.pop(1); // X
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                if (!l.getmetatable(-1)) // ud meta
                { // ud
                    l.newtable(); // ud meta
                    l.pushvalue(-1); // ud meta meta
                    l.SetField(-2, LuaConst.LS_META_KEY_INDEX); // ud meta
                    l.pushvalue(-1); // ud meta meta
                    l.setmetatable(-3); // ud meta
                }
                else
                { // we have already made metatable. that means we attached to lua class. this metatable is shared. so we'd better not record data to the metatable.
                    l.pop(1);
                    l.pushvalue(-1); // ud ud
                }
                var ntrans = GetNativeTrans(thiz.GetType());
                if (ntrans == IntPtr.Zero)
                {
                    ntrans = LuaHub._LuaWrapperNativeCommon.r;
                    l.PushString(LuaConst.LS_SP_KEY_TYPE);
                    l.PushLua(thiz.GetType());
                    l.rawset(-3);
                }
                l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS); // ud meta #trans
                l.pushlightuserdata(ntrans); // ud meta #trans trans
                l.rawset(-3); // ud meta
                l.pushlightuserdata(LuaConst.LRKEY_TARGET);
                l.PushLuaRawObject(new WeakReference(thiz));
                l.rawset(-3); // ud meta
                thiz.Binding = new LuaTable(l, -2);
                l.pop(2); // X
                return true;
            }
            else
            {
                return true;
            }
        }

        public static bool BindLua(this ILuaWrapper thiz, IntPtr l)
        {
            return thiz.BindLua(l, Pack());
        }

        public static bool BindType(this ILuaWrapper thiz)
        {
            if (thiz == null)
            {
                return false;
            }
            if (ReferenceEquals(thiz.Binding, null))
            {
                return false;
            }
            var l = thiz.Binding.L;
            //using (l.CreateStackRecover())
            {
                l.PushLua(thiz.Binding); // ud
                if (!l.getmetatable(-1)) // ud meta
                { // ud
                    l.newtable(); // ud meta
                    l.pushvalue(-1); // ud meta meta
                    l.SetField(-2, LuaConst.LS_META_KEY_INDEX); // ud meta
                    l.pushvalue(-1); // ud meta meta
                    l.setmetatable(-3); // ud meta
                }
                else
                { // we have already made metatable. check whether it is a lua-class
                    // ud meta
                    l.GetField(-1, "__ctype"); // ud meta ctype
                    if (!l.isnoneornil(-1))
                    { // this is a lua-class
                        l.pop(2); // ud
                        l.pushvalue(-1); // ud ud
                    }
                    else
                    { // check the trans is stored in the ud
                        l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS); // ud meta ctype #trans
                        l.rawget(-4); // ud meta ctype trans
                        if (!l.isnoneornil(-1))
                        { // the trans is stored in the ud, it is a lua-class
                            l.pop(3); // ud
                            l.pushvalue(-1); // ud ud
                        }
                        else
                        { // normal lua table
                            l.pop(2); // ud meta
                        }
                    }
                }
                // ud meta
                var ntrans = GetNativeTrans(thiz.GetType());
                if (ntrans == IntPtr.Zero)
                {
                    ntrans = LuaHub._LuaWrapperNativeCommon.r;
                    l.PushString(LuaConst.LS_SP_KEY_TYPE);
                    l.PushLua(thiz.GetType());
                    l.rawset(-3);
                }
                l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS); // ud meta #trans
                l.pushlightuserdata(ntrans); // ud meta #trans trans
                l.rawset(-3); // ud meta
                l.pushlightuserdata(LuaConst.LRKEY_TARGET);
                l.PushLuaRawObject(new WeakReference(thiz));
                l.rawset(-3); // ud meta
                l.pop(2); // X
                return true;
            }
        }

        public static T GetWrapper<T>(this ILuaWrapper thiz) where T : ILuaWrapper, new()
        {
            if (thiz == null)
            {
                return default(T);
            }
            else
            {
                var copy = new T();
                copy.Binding = thiz.Binding;
                return copy;
            }
        }
        public static void GetWrapper<T>(this ILuaWrapper thiz, out T copy) where T : ILuaWrapper, new()
        {
            copy = thiz.GetWrapper<T>();
        }

        public static T GetWrapper<T>(this BaseLua lua) where T : ILuaWrapper, new()
        {
            if (ReferenceEquals(lua, null))
            {
                return default(T);
            }
            else
            {
                var result = new T();
                result.Binding = lua;
                return result;
            }
        }
        public static void GetWrapper<T>(this BaseLua lua, out T result) where T : ILuaWrapper, new()
        {
            result = lua.GetWrapper<T>();
        }

        public static T GetWrapper<T>(this object o) where T : ILuaWrapper, new()
        {
            if (o is T)
            {
                return (T)o;
            }
            else if (o is BaseLua)
            {
                var result = new T();
                result.Binding = (BaseLua)o;
                return result;
            }
            else if (o is ILuaWrapper)
            {
                var result = new T();
                result.Binding = ((ILuaWrapper)o).Binding;
                return result;
            }
            return default(T);
        }
        public static void GetWrapper<T>(this object o, out T result) where T : ILuaWrapper, new()
        {
            result = o.GetWrapper<T>();
        }

        public static string GetLuaTypeName(this BaseLua lua)
        {
            if (ReferenceEquals(lua, null) || lua.IsClosed || lua.L == IntPtr.Zero)
            {
                return null;
            }
            var l = lua.L;
            using (var lr = l.CreateStackRecover())
            {
                lua.PushToLua(l);
                var cname = l.GetTable<string>(-1, "__cname");
                return cname;
            }
        }
        public static string GetLuaTypeName(this ILuaWrapper thiz)
        {
            if (thiz == null)
            {
                return null;
            }
            else
            {
                return GetLuaTypeName(thiz.Binding);
            }
        }

        public static T Clone<T>(this T lua) where T : BaseLua
        {
            if (ReferenceEquals(lua, null) || lua.IsClosed || lua.L == IntPtr.Zero)
            {
                return default(T);
            }
            var l = lua.L;
            using (var lr = l.CreateStackRecover())
            {
                T cloned;
                lua.PushToLua(l);
                l.CallGlobal(out cloned, "clone", Pack(l.OnStackTop()));
                return cloned;
            }
        }
    }
}

namespace LuaLib
{
    public static partial class LuaHub
    {
        public class BaseLuaWrapperHub<T> : LuaTypeHub.TypeHubValueType, ILuaTrans<T>, ILuaPush<T>, IInstanceCreator<T>, ILuaNative, ILuaConvert
            where T : ILuaWrapper, new()
        {
            public T NewInstance() { return new T(); }
            object IInstanceCreator.NewInstance() { return NewInstance(); }

            public override bool Nonexclusive { get { return true; } }
            public void SetDataRaw(IntPtr l, int index, T val)
            {
                if (_ExposeToLua)
                {
                    l.checkstack(3);
                    l.pushvalue(index); // otab
                    l.pushlightuserdata(LuaConst.LRKEY_TARGET); // otab #tar
                    LuaHubNative.PushLua(l, val); // otab #tar obj
                    l.rawset(-3); // otab
                    l.pop(1);
                }
                else
                {
                    val.BindType();
                    LuaHubNative.SetDataRaw(l, index, val);
                }
            }
            public T GetLuaRaw(IntPtr l, int index)
            {
                if (_ExposeToLua)
                {
                    l.checkstack(2);
                    l.pushvalue(index); // otab
                    l.pushlightuserdata(LuaConst.LRKEY_TARGET); // otab #tar
                    l.rawget(-2); // otab ud
                    var result = LuaHubNative.GetLua(l, -1);
                    l.pop(2); // X
                    return result;
                }
                else
                {
                    return LuaHubNative.GetLua(l, index);
                }
            }

            public class LuaWrapperNative : LuaPushNativeBase<T>, ILuaTypeRef, ILuaTrans
            {
                public Type t { get { return typeof(T); } }
                protected IntPtr _r;
                public IntPtr r
                {
                    get { return _r; }
                }

                public LuaWrapperNative()
                {
                    _r = (IntPtr)System.Runtime.InteropServices.GCHandle.Alloc(this);
                }

                public override T GetLua(IntPtr l, int index)
                {
                    if (l.istable(index) || l.IsUserDataTable(index))
                    {
                        l.checkstack(3);
                        l.pushvalue(index); // ud
                        l.pushlightuserdata(LuaConst.LRKEY_TARGET); // ud #tar
                        l.gettable(-2); // ud tar
                        var current = l.isuserdata(-1) ? (l.GetLuaRawObject(-1) as WeakReference).GetWeakReference<T>() : default(T);
                        l.pop(2); // X
                        if (current != null && !ReferenceEquals(current.Binding, null)) // TODO: when T is ValueType, current != null may cause box of current.
                        {
                            // Notice: the current may point to diffent lua table because it is created by clone() in lua.
                            var pthis = l.topointer(index);
                            l.PushLua(current.Binding);
                            var pcur = l.topointer(-1);
                            l.pop(1);
                            if (pthis == pcur)
                            {
                                return current;
                            }
                        }
                        {
                            var val = new T();
                            l.pushvalue(index); // ud
                            val.Binding = new BaseLua(l, l.refer()); // X
                            l.pushvalue(index); // ud
                            if (!l.getmetatable(-1)) // ud meta
                            { // ud
                                l.newtable(); // ud meta
                                l.pushvalue(-1); // ud meta meta
                                l.SetField(-2, LuaConst.LS_META_KEY_INDEX); // ud meta
                                l.pushvalue(-1); // ud meta meta
                                l.setmetatable(-3); // ud meta
                            }
                            else
                            {
                                l.pushlightuserdata(LuaConst.LRKEY_TARGET); // ud meta #tar
                                l.rawget(-2); // ud meta tar
                                if (l.isnoneornil(-1))
                                { // the tar is not stored in metatable
                                    l.pop(2); // ud
                                    l.pushvalue(-1); // ud ud
                                }
                                else
                                {
                                    l.pop(1); // ud meta
                                }
                            }
                            l.pushlightuserdata(LuaConst.LRKEY_TARGET); // ud meta #tar
                            l.PushLuaRawObject(new WeakReference(val)); // ud meta #tar tar
                            l.rawset(-3); // ud meta
                            l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS);
                            l.pushlightuserdata(r);
                            l.rawset(-3);
                            l.pop(2); // X
                            return val;
                        }
                    }
                    return default(T);
                }
                public override IntPtr PushLua(IntPtr l, T val)
                {
                    if (!typeof(T).IsValueType)
                    {
                        if (ReferenceEquals(val, null))
                        {
                            l.pushnil();
                            return IntPtr.Zero;
                        }
                    }
                    if (ReferenceEquals(val.Binding, null))
                    {
                        if (!val.BindLua(l))
                        {
                            l.pushnil();
                            return IntPtr.Zero;
                        }
                    }
                    l.PushLua(val.Binding);
                    return IntPtr.Zero;
                }
                public void SetDataRaw(IntPtr l, int index, T val)
                {
                    if (!typeof(T).IsValueType)
                    {
                        if (ReferenceEquals(val, null))
                        {
                            return;
                        }
                    }
                    if (ReferenceEquals(val.Binding, null))
                    {
                        val.BindLua(l);
                    }
                    else if (!(val.Binding is BaseLuaOnStack))
                    {
                        l.checkstack(3);
                        l.PushLua(val.Binding); // ud
                        l.pushlightuserdata(LuaConst.LRKEY_TARGET); // ud #tar
                        l.gettable(-2); // ud tar
                        var current = l.isuserdata(-1) ? (l.GetLuaRawObject(-1) as WeakReference).GetWeakReference<T>() : default(T);
                        if (ReferenceEquals(current, val))
                        {
                            l.pop(2); // X
                        }
                        else
                        {
                            l.pop(1); // ud
                            if (!l.getmetatable(-1)) // ud meta
                            { // ud
                                l.newtable(); // ud meta
                                l.pushvalue(-1); // ud meta meta
                                l.SetField(-2, LuaConst.LS_META_KEY_INDEX); // ud meta
                                l.pushvalue(-1); // ud meta meta
                                l.setmetatable(-3); // ud meta
                            }
                            else
                            {
                                l.pushlightuserdata(LuaConst.LRKEY_TARGET); // ud meta #tar
                                l.rawget(-2); // ud meta tar
                                if (l.isnoneornil(-1))
                                { // the tar is not stored in metatable
                                    l.pop(2); // ud
                                    l.pushvalue(-1); // ud ud
                                }
                                else
                                {
                                    l.pop(1); // ud meta
                                }
                            }
                            l.pushlightuserdata(LuaConst.LRKEY_TARGET); // ud meta #tar
                            l.PushLuaRawObject(new WeakReference(val)); // ud meta #tar tar
                            l.rawset(-3); // ud meta
                            l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS);
                            l.pushlightuserdata(r);
                            l.rawset(-3);
                            l.pop(2); // X
                        }
                    }
                }

                public bool Nonexclusive { get { return true; } }
                public void SetData(IntPtr l, int index, object val)
                {
                    SetDataRaw(l, index, (T)val);
                }
                object ILuaTrans.GetLua(IntPtr l, int index)
                {
                    return GetLua(l, index);
                }
                public Type GetType(IntPtr l, int index)
                {
                    return typeof(T);
                }
            }
            public static readonly LuaWrapperNative LuaHubNative = new LuaWrapperNative();

            private bool _ExposeToLua;
            public BaseLuaWrapperHub(bool exposeToLua) : base(exposeToLua ? typeof(T) : null)
            {
                _ExposeToLua = exposeToLua;
                if (!exposeToLua)
                {
                    t = typeof(T);
                }
                PutIntoCache();
                BaseLuaWrapper.LuaHubSubs[t] = this;

                _ConvertFromFuncs = new[]
                {
                    new KeyValuePair<Type, LuaConvertFunc>(typeof(BaseLua), ConvertFromBaseLua),
                    new KeyValuePair<Type, LuaConvertFunc>(typeof(ILuaWrapper), ConvertFromLuaWrapper),
                };
                _ConvertFuncs[typeof(LuaTable)] = ConvertToLuaTable;
                _ConvertFuncs[typeof(LuaRawTable)] = ConvertToLuaRawTable;
            }
            public BaseLuaWrapperHub() : this(false) { }
            protected override bool UpdateDataAfterCall
            {
                get { return true; }
            }

            public override IntPtr PushLua(IntPtr l, object val)
            {
                return PushLua(l, (T)val);
            }
            public override void SetData(IntPtr l, int index, object val)
            {
                SetDataRaw(l, index, (T)val);
            }
            public override object GetLua(IntPtr l, int index)
            {
                return GetLuaRaw(l, index);
            }
            public IntPtr PushLua(IntPtr l, T val)
            {
                if (_ExposeToLua)
                {
                    l.checkstack(3);
                    l.newtable(); // ud
                    SetDataRaw(l, -1, val);
                    l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS); // ud #trans
                    l.pushlightuserdata(r); // ud #trans trans
                    l.rawset(-3); // ud

                    PushToLuaCached(l); // ud type
                    l.pushlightuserdata(LuaConst.LRKEY_OBJ_META); // ud type #meta
                    l.rawget(-2); // ud type meta
                    l.setmetatable(-3); // ud type
                    l.pop(1); // ud
                }
                else
                {
                    val.BindType();
                    LuaHubNative.PushLua(l, val);
                }
                return IntPtr.Zero;
            }
            public void SetData(IntPtr l, int index, T val)
            {
                SetDataRaw(l, index, val);
            }
            T ILuaTrans<T>.GetLua(IntPtr l, int index)
            {
                return GetLuaRaw(l, index);
            }

            public void Wrap(IntPtr l, int index)
            {
                T val;
                l.GetLua(index, out val);
                PushLua(l, val);
            }
            public void Unwrap(IntPtr l, int index)
            {
                T val;
                l.GetLua(index, out val);
                LuaHubNative.PushLua(l, val);
            }
            public int LuaType { get { return LuaCoreLib.LUA_TTABLE; } }

            public int ConvertFromBaseLua(IntPtr l, int index)
            {
                BaseLua binding;
                l.GetLua(index, out binding);
                if (ReferenceEquals(binding, null))
                {
                    return 0;
                }
                var result = new T();
                result.Binding = binding;
                PushLua(l, result);
                return 1;
            }
            public int ConvertFromLuaWrapper(IntPtr l, int index)
            {
                var wrapper = l.GetLua(index);
                if (wrapper != null)
                {
                    if (wrapper is BaseLua)
                    {
                        var result = new T();
                        result.Binding = (BaseLua)wrapper;
                        PushLua(l, result);
                        return 1;
                    }
                    else if (wrapper is ILuaWrapper)
                    {
                        var result = new T();
                        result.Binding = ((ILuaWrapper)wrapper).Binding;
                        PushLua(l, result);
                        return 1;
                    }
                }
                return 0;
            }
            public int ConvertToLuaTable(IntPtr l, int index)
            {
                Unwrap(l, index);
                var inst = new LuaTable(l, -1);
                l.PushLuaObject(inst);
                return 1;
            }
            public int ConvertToLuaRawTable(IntPtr l, int index)
            {
                Unwrap(l, index);
                var inst = new LuaRawTable(l, -1);
                l.PushLuaObject(inst);
                return 1;
            }
        }

        private class BaseLuaWrapperHubPrepareFuncs
        {
            private static LuaTypeHub.TypeHubBase PrepareTypeHub(Type type)
            {
                return BaseLuaWrapper.CheckLuaHubSub(type);
            }

            public BaseLuaWrapperHubPrepareFuncs()
            {
                LuaTypeHub.RegTypeHubPrepareFunc(typeof(ILuaWrapper), PrepareTypeHub);
            }
        }
        private static BaseLuaWrapperHubPrepareFuncs _BaseLuaWrapperHubPrepareFuncs = new BaseLuaWrapperHubPrepareFuncs();

        public class LuaWrapperNativeCommon : ILuaHandle, ILuaTrans, ILuaPush
        {
            protected IntPtr _r;
            public IntPtr r
            {
                get { return _r; }
            }
            public LuaWrapperNativeCommon()
            {
                _r = (IntPtr)System.Runtime.InteropServices.GCHandle.Alloc(this);
            }

            public object GetLua(IntPtr l, int index)
            {
                if (l.istable(index) || l.IsUserDataTable(index))
                {
                    l.checkstack(3);
                    l.pushvalue(index); // ud
                    l.pushlightuserdata(LuaConst.LRKEY_TARGET); // ud #tar
                    l.gettable(-2); // ud tar
                    var current = l.isuserdata(-1) ? (l.GetLuaRawObject(-1) as WeakReference).GetWeakReference<ILuaWrapper>() : null;
                    l.pop(2); // X
                    if (current != null && !ReferenceEquals(current.Binding, null))
                    {
                        // Notice: the current may point to diffent lua table because it is created by clone() in lua.
                        var pthis = l.topointer(index);
                        l.PushLua(current.Binding);
                        var pcur = l.topointer(-1);
                        l.pop(1);
                        if (pthis == pcur)
                        {
                            return current;
                        }
                    }
                    Type wrapperType;
                    l.pushvalue(index); // ud
                    l.PushString(LuaConst.LS_SP_KEY_TYPE); // ud @type
                    l.gettable(-2); // ud type
                    l.GetLua(-1, out wrapperType);
                    l.pop(2); // X
                    if (wrapperType == null || !typeof(ILuaWrapper).IsAssignableFrom(wrapperType))
                    {
                        wrapperType = typeof(BaseLuaWrapper);
                    }

                    {
                        ILuaWrapper val = null;
                        try
                        {
                            val = Activator.CreateInstance(wrapperType) as ILuaWrapper;
                        }
                        catch (Exception e)
                        {
                            DynamicHelper.LogError(e);
                        }
                        if (val == null)
                        {
                            val = new BaseLuaWrapper();
                        }

                        {
                            l.pushvalue(index); // ud
                            val.Binding = new BaseLua(l, l.refer()); // X
                            l.pushvalue(index); // ud
                            if (!l.getmetatable(-1)) // ud meta
                            { // ud
                                l.newtable(); // ud meta
                                l.pushvalue(-1); // ud meta meta
                                l.SetField(-2, LuaConst.LS_META_KEY_INDEX); // ud meta
                                l.pushvalue(-1); // ud meta meta
                                l.setmetatable(-3); // ud meta
                            }
                            else
                            {
                                l.pushlightuserdata(LuaConst.LRKEY_TARGET); // ud meta #tar
                                l.rawget(-2); // ud meta tar
                                if (l.isnoneornil(-1))
                                { // the tar is not stored in metatable
                                    l.pop(2); // ud
                                    l.pushvalue(-1); // ud ud
                                }
                                else
                                {
                                    l.pop(1); // ud meta
                                }
                            }
                            l.pushlightuserdata(LuaConst.LRKEY_TARGET); // ud meta #tar
                            l.PushLuaRawObject(new WeakReference(val)); // ud meta #tar tar
                            l.rawset(-3); // ud meta
                            l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS);
                            l.pushlightuserdata(r);
                            l.rawset(-3); // ud meta
                            l.pop(2); // X
                            return val;
                        }
                    }
                }
                return null;
            }
            public IntPtr PushLua(IntPtr l, object val)
            {
                var wrapper = val as ILuaWrapper;
                if (ReferenceEquals(wrapper, null))
                {
                    l.pushnil();
                    return IntPtr.Zero;
                }
                if (ReferenceEquals(wrapper.Binding, null))
                {
                    if (!wrapper.BindLua(l))
                    {
                        l.pushnil();
                        return IntPtr.Zero;
                    }
                }
                l.PushLua(wrapper.Binding);
                return IntPtr.Zero;
            }
            public void SetData(IntPtr l, int index, object val)
            {
                var wrapper = val as ILuaWrapper;
                if (ReferenceEquals(wrapper, null))
                {
                    return;
                }
                if (ReferenceEquals(wrapper.Binding, null))
                {
                    wrapper.BindLua(l);
                }
                else if (!(wrapper.Binding is BaseLuaOnStack))
                {
                    l.checkstack(3);
                    l.PushLua(wrapper.Binding); // ud
                    l.pushlightuserdata(LuaConst.LRKEY_TARGET); // ud #tar
                    l.gettable(-2); // ud tar
                    var current = l.isuserdata(-1) ? (l.GetLuaRawObject(-1) as WeakReference).GetWeakReference<ILuaWrapper>() : null;
                    if (ReferenceEquals(current, val))
                    {
                        l.pop(2); // X
                    }
                    else
                    {
                        l.pop(1); // ud
                        if (!l.getmetatable(-1)) // ud meta
                        { // ud
                            l.newtable(); // ud meta
                            l.pushvalue(-1); // ud meta meta
                            l.SetField(-2, LuaConst.LS_META_KEY_INDEX); // ud meta
                            l.pushvalue(-1); // ud meta meta
                            l.setmetatable(-3); // ud meta
                        }
                        else
                        {
                            l.pushlightuserdata(LuaConst.LRKEY_TARGET); // ud meta #tar
                            l.rawget(-2); // ud meta tar
                            if (l.isnoneornil(-1))
                            { // the tar is not stored in metatable
                                l.pop(2); // ud
                                l.pushvalue(-1); // ud ud
                            }
                            else
                            {
                                l.pop(1); // ud meta
                            }
                        }
                        l.pushlightuserdata(LuaConst.LRKEY_TARGET); // ud meta #tar
                        l.PushLuaRawObject(new WeakReference(val)); // ud meta #tar tar
                        l.rawset(-3); // ud meta
                        l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS);
                        l.pushlightuserdata(r);
                        l.rawset(-3);
                        l.PushString(LuaConst.LS_SP_KEY_TYPE);
                        l.PushLua(val.GetType());
                        l.rawset(-3);
                        l.pop(2); // X
                    }
                }
            }

            public bool Nonexclusive { get { return true; } }
            public bool ShouldCache { get { return false; } }
            public Type GetType(IntPtr l, int index)
            {
                Type t;
                l.checkstack(3);
                l.pushvalue(index); // ud
                l.PushString(LuaConst.LS_SP_KEY_TYPE); // ud @type
                l.gettable(-2); // ud type
                l.GetLua(-1, out t);
                l.pop(2);
                if (t == null || !typeof(ILuaWrapper).IsAssignableFrom(t))
                {
                    t = typeof(BaseLuaWrapper);
                }
                return t;
            }
            public bool PushFromCache(IntPtr l, object val)
            {
                return false;
            }
        }
        internal static LuaWrapperNativeCommon _LuaWrapperNativeCommon = new LuaWrapperNativeCommon();
    }
}

// Some lua collections
namespace LuaLib
{
    public class LuaList<T> : BaseLuaWrapper<LuaList<T>>, ICollection<T>, IEnumerable<T>, IEnumerable, IList<T>, IReadOnlyCollection<T>, IReadOnlyList<T>, ICollection, IList
    {
        public LuaList() : base() { }
        public LuaList(IntPtr l) : base(l) { }

        protected static IEqualityComparer<T> _Comparer = EqualityComparer<T>.Default;
        protected bool Equals(T v1, T v2)
        {
            return _Comparer.Equals(v1, v2);
        }

        public bool IsSynchronized { get { return false; } }
        public object SyncRoot { get { return LuaHubSub; } }

        public void CopyTo(Array array, int index)
        {
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                l.PushLua(Binding);
                var cnt = l.getn(-1);
                for (int i = 0; i < cnt; ++i)
                {
                    l.pushnumber(i + 1);
                    l.rawget(-2);
                    var val = l.GetLua<T>(-1);
                    array.SetValue(val, index + i);
                    l.pop(1);
                }
            }
        }

        public T this[int index]
        {
            get
            {
                var l = L;
                using (var lr = l.CreateStackRecover())
                {
                    l.PushLua(Binding);
                    l.pushnumber(index + 1);
                    l.rawget(-2);
                    return l.GetLua<T>(-1);
                }
            }
            set
            {
                var l = L;
                using (var lr = l.CreateStackRecover())
                {
                    l.PushLua(Binding);
                    l.pushnumber(index + 1);
                    l.PushLua(value);
                    l.rawset(-3);
                }
            }
        }

        public int Count
        {
            get
            {
                var l = L;
                using (var lr = l.CreateStackRecover())
                {
                    l.PushLua(Binding);
                    return l.getn(-1);
                }
            }
        }

        public int Capacity { get { return int.MaxValue; } }
        public bool IsReadOnly { get { return false; } }
        public bool IsFixedSize { get { return false; } }

        object IList.this[int index]
        {
            get { return this[index]; }
            set { this[index] = (T)value; }
        }

        public void Add(T item)
        {
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                l.PushLua(Binding);
                var cnt = l.getn(-1);
                l.pushnumber(cnt + 1);
                l.PushLua(item);
                l.rawset(-3);
            }
        }
        public void AddRange(IEnumerable<T> collection)
        {
            var index = 0;
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                l.PushLua(Binding);
                var cnt = l.getn(-1);
                foreach (var item in collection)
                {
                    l.pushnumber(cnt + (++index));
                    l.PushLua(item);
                    l.rawset(-3);
                }
            }
        }
        public void Clear()
        {
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                l.PushLua(Binding);
                var cnt = l.getn(-1);
                for (int i = 0; i < cnt; ++i)
                {
                    l.pushnumber(i + 1);
                    l.pushnil();
                    l.rawset(-3);
                }
            }
        }
        public bool Contains(T item)
        {
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                l.PushLua(Binding);
                var cnt = l.getn(-1);
                for (int i = 0; i < cnt; ++i)
                {
                    l.pushnumber(i + 1);
                    l.rawget(-2);
                    var val = l.GetLua<T>(-1);
                    if (Equals(val, item))
                    {
                        return true;
                    }
                    l.pop(1);
                }
            }
            return false;
        }
        public void CopyTo(int index, T[] array, int arrayIndex, int count)
        {
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                l.PushLua(Binding);
                for (int i = 0; i < count; ++i)
                {
                    l.pushnumber(index + i + 1);
                    l.rawget(-2);
                    var val = l.GetLua<T>(-1);
                    array[arrayIndex + i] = val;
                    l.pop(1);
                }
            }
        }
        public void CopyTo(T[] array, int arrayIndex)
        {
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                l.PushLua(Binding);
                var cnt = l.getn(-1);
                for (int i = 0; i < cnt; ++i)
                {
                    l.pushnumber(i + 1);
                    l.rawget(-2);
                    var val = l.GetLua<T>(-1);
                    array[arrayIndex + i] = val;
                    l.pop(1);
                }
            }
        }
        public void CopyTo(T[] array)
        {
            CopyTo(array, 0);
        }

        public bool Exists(Predicate<T> match)
        {
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                l.PushLua(Binding);
                var cnt = l.getn(-1);
                for (int i = 0; i < cnt; ++i)
                {
                    l.pushnumber(i + 1);
                    l.rawget(-2);
                    var val = l.GetLua<T>(-1);
                    if (match(val))
                    {
                        return true;
                    }
                    l.pop(1);
                }
            }
            return false;
        }
        public T Find(Predicate<T> match)
        {
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                l.PushLua(Binding);
                var cnt = l.getn(-1);
                for (int i = 0; i < cnt; ++i)
                {
                    l.pushnumber(i + 1);
                    l.rawget(-2);
                    var val = l.GetLua<T>(-1);
                    if (match(val))
                    {
                        return val;
                    }
                    l.pop(1);
                }
            }
            return default(T);
        }
        public List<T> FindAll(Predicate<T> match)
        {
            List<T> results = new List<T>();
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                l.PushLua(Binding);
                var cnt = l.getn(-1);
                for (int i = 0; i < cnt; ++i)
                {
                    l.pushnumber(i + 1);
                    l.rawget(-2);
                    var val = l.GetLua<T>(-1);
                    if (match(val))
                    {
                        results.Add(val);
                    }
                    l.pop(1);
                }
            }
            return results;
        }
        public int FindIndex(int startIndex, int count, Predicate<T> match)
        {
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                l.PushLua(Binding);
                for (int i = 0; i < count; ++i)
                {
                    l.pushnumber(startIndex + i + 1);
                    l.rawget(-2);
                    var val = l.GetLua<T>(-1);
                    if (match(val))
                    {
                        return startIndex + i;
                    }
                    l.pop(1);
                }
            }
            return -1;
        }
        public int FindIndex(int startIndex, Predicate<T> match)
        {
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                l.PushLua(Binding);
                var count = l.getn(-1) - startIndex;
                for (int i = 0; i < count; ++i)
                {
                    l.pushnumber(startIndex + i + 1);
                    l.rawget(-2);
                    var val = l.GetLua<T>(-1);
                    if (match(val))
                    {
                        return startIndex + i;
                    }
                    l.pop(1);
                }
            }
            return -1;
        }
        public int FindIndex(Predicate<T> match)
        {
            return FindIndex(0, match);
        }
        public T FindLast(Predicate<T> match)
        {
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                l.PushLua(Binding);
                var cnt = l.getn(-1);
                for (int i = cnt; i > 0; --i)
                {
                    l.pushnumber(i);
                    l.rawget(-2);
                    var val = l.GetLua<T>(-1);
                    if (match(val))
                    {
                        return val;
                    }
                    l.pop(1);
                }
            }
            return default(T);
        }
        public int FindLastIndex(int startIndex, int count, Predicate<T> match)
        {
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                l.PushLua(Binding);
                for (int i = 0; i < count; ++i)
                {
                    l.pushnumber(startIndex + 1 - i);
                    l.rawget(-2);
                    var val = l.GetLua<T>(-1);
                    if (match(val))
                    {
                        return startIndex - i;
                    }
                    l.pop(1);
                }
            }
            return -1;
        }
        public int FindLastIndex(int startIndex, Predicate<T> match)
        {
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                l.PushLua(Binding);
                var count = l.getn(-1);
                for (int i = 0; i < count; ++i)
                {
                    l.pushnumber(startIndex + 1 - i);
                    l.rawget(-2);
                    var val = l.GetLua<T>(-1);
                    if (match(val))
                    {
                        return startIndex - i;
                    }
                    l.pop(1);
                }
            }
            return -1;
        }
        public int FindLastIndex(Predicate<T> match)
        {
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                l.PushLua(Binding);
                var cnt = l.getn(-1);
                for (int i = cnt; i > 0; --i)
                {
                    l.pushnumber(i);
                    l.rawget(-2);
                    var val = l.GetLua<T>(-1);
                    if (match(val))
                    {
                        return i - 1;
                    }
                    l.pop(1);
                }
            }
            return -1;
        }
        public void ForEach(Action<T> action)
        {
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                l.PushLua(Binding);
                var cnt = l.getn(-1);
                for (int i = 0; i < cnt; ++i)
                {
                    l.pushnumber(i + 1);
                    l.rawget(-2);
                    var val = l.GetLua<T>(-1);
                    l.pop(1);
                    action(val);
                }
            }
        }

        public struct Enumerator : IEnumerator<T>
        {
            private BaseLua Binding;
            private int Index;

            public T Current
            {
                get
                {
                    var l = Binding.L;
                    using (var lr = l.CreateStackRecover())
                    {
                        l.PushLua(Binding);
                        l.pushnumber(Index);
                        l.rawget(-2);
                        return l.GetLua<T>(-1);
                    }
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            public Enumerator(LuaList<T> list)
            {
                Index = 0;
                Binding = list.Binding;
            }

            public void Dispose()
            {
                Index = 0;
                Binding = null;
            }

            public bool MoveNext()
            {
                var index = ++Index;
                if (ReferenceEquals(Binding, null))
                {
                    return false;
                }
                var l = Binding.L;
                using (var lr = l.CreateStackRecover())
                {
                    l.PushLua(Binding);
                    var cnt = l.getn(-1);
                    return index <= cnt;
                }
            }

            public void Reset()
            {
                Index = 0;
            }
        }
        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(this);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public List<T> GetRange(int index, int count)
        {
            List<T> results = new List<T>();
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                l.PushLua(Binding);
                for (int i = 0; i < count; ++i)
                {
                    l.pushnumber(index + i + 1);
                    l.rawget(-2);
                    var val = l.GetLua<T>(-1);
                    results.Add(val);
                    l.pop(1);
                }
            }
            return results;
        }
        public int IndexOf(T item, int index, int count)
        {
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                l.PushLua(Binding);
                for (int i = 0; i < count; ++i)
                {
                    l.pushnumber(index + i + 1);
                    l.rawget(-2);
                    var val = l.GetLua<T>(-1);
                    if (Equals(val, item))
                    {
                        return index + i;
                    }
                    l.pop(1);
                }
            }
            return -1;
        }
        public int IndexOf(T item, int index)
        {
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                l.PushLua(Binding);
                var count = l.getn(-1) - index;
                for (int i = 0; i < count; ++i)
                {
                    l.pushnumber(index + i + 1);
                    l.rawget(-2);
                    var val = l.GetLua<T>(-1);
                    if (Equals(val, item))
                    {
                        return index + i;
                    }
                    l.pop(1);
                }
            }
            return -1;
        }
        public int IndexOf(T item)
        {
            return IndexOf(item, 0);
        }
        public void Insert(int index, T item)
        {
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                l.PushLua(Binding);
                var cnt = l.getn(-1);
                for (int i = cnt - 1; i >= 0; --i)
                {
                    if (i > index)
                    {
                        l.pushnumber(i + 2);
                        l.pushnumber(i + 1);
                        l.rawget(-3);
                        l.rawset(-3);
                    }
                    else if (i == index)
                    {
                        l.pushnumber(i + 1);
                        l.PushLua(item);
                        l.rawset(-3);
                        break;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
        public void InsertRange(int index, IEnumerable<T> collection)
        {
            var list = new List<T>();
            foreach (var item in collection)
            {
                list.Add(item);
            }
            var rangecnt = list.Count;
            if (rangecnt > 0)
            {
                var l = L;
                using (var lr = l.CreateStackRecover())
                {
                    l.PushLua(Binding);
                    var cnt = l.getn(-1);
                    for (int i = cnt - 1; i >= 0; --i)
                    {
                        if (i > index)
                        {
                            l.pushnumber(i + 1 + rangecnt);
                            l.pushnumber(i + 1);
                            l.rawget(-3);
                            l.rawset(-3);
                        }
                        else
                        {
                            break;
                        }
                    }
                    for (int i = 0; i < rangecnt; ++i)
                    {
                        l.pushnumber(index + 1 + i);
                        l.PushLua(list[i]);
                        l.rawset(-3);
                    }
                }
            }
        }
        public int LastIndexOf(T item)
        {
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                l.PushLua(Binding);
                var cnt = l.getn(-1);
                for (int i = cnt; i > 0; --i)
                {
                    l.pushnumber(i);
                    l.rawget(-2);
                    var val = l.GetLua<T>(-1);
                    if (Equals(val, item))
                    {
                        return i - 1;
                    }
                    l.pop(1);
                }
            }
            return -1;
        }
        public int LastIndexOf(T item, int index)
        {
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                l.PushLua(Binding);
                var count = l.getn(-1);
                for (int i = 0; i < count; ++i)
                {
                    l.pushnumber(index + 1 - i);
                    l.rawget(-2);
                    var val = l.GetLua<T>(-1);
                    if (Equals(val, item))
                    {
                        return index - i;
                    }
                    l.pop(1);
                }
            }
            return -1;
        }
        public int LastIndexOf(T item, int index, int count)
        {
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                l.PushLua(Binding);
                for (int i = 0; i < count; ++i)
                {
                    l.pushnumber(index + 1 - i);
                    l.rawget(-2);
                    var val = l.GetLua<T>(-1);
                    if (Equals(val, item))
                    {
                        return index - i;
                    }
                    l.pop(1);
                }
            }
            return -1;
        }
        public bool Remove(T item)
        {
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                l.PushLua(Binding);
                var cnt = l.getn(-1);
                for (int i = 0; i < cnt; ++i)
                {
                    l.pushnumber(i + 1);
                    l.rawget(-2);
                    var val = l.GetLua<T>(-1);
                    l.pop(1);
                    if (Equals(val, item))
                    {
                        for (i = i + 1; i < cnt; ++i)
                        {
                            l.pushnumber(i);
                            l.pushnumber(i + 1);
                            l.rawget(-3);
                            l.rawset(-3);
                        }
                        l.pushnumber(cnt);
                        l.pushnil();
                        l.rawset(-3);
                        return true;
                    }
                }
            }
            return false;
        }
        public int RemoveAll(Predicate<T> match)
        {
            int removed = 0;
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                l.PushLua(Binding);
                var cnt = l.getn(-1);
                for (int i = 0; i < cnt; ++i)
                {
                    l.pushnumber(i + 1);
                    l.rawget(-2);
                    var val = l.GetLua<T>(-1);
                    if (match(val))
                    {
                        ++removed;
                        l.pop(1);
                    }
                    else if (removed > 0)
                    {
                        l.pushnumber(i + 1 - removed);
                        l.insert(-2);
                        l.rawset(-3);
                        l.pushnumber(i + 1);
                        l.pushnil();
                        l.rawset(-3);
                    }
                    else
                    {
                        l.pop(1);
                    }
                }
            }
            return removed;
        }
        public void RemoveAt(int index)
        {
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                l.PushLua(Binding);
                var cnt = l.getn(-1);
                if (index < cnt)
                {
                    for (int i = index + 1; i < cnt; ++i)
                    {
                        l.pushnumber(i);
                        l.pushnumber(i + 1);
                        l.rawget(-3);
                        l.rawset(-3);
                    }
                    l.pushnumber(cnt);
                    l.pushnil();
                    l.rawset(-3);
                }
            }
        }
        public void RemoveRange(int index, int count)
        {
            if (count > 0)
            {
                var l = L;
                using (var lr = l.CreateStackRecover())
                {
                    l.PushLua(Binding);
                    var cnt = l.getn(-1);
                    for (int i = index; i < cnt; ++i)
                    {
                        l.pushnumber(i + 1);
                        l.pushnumber(i + count + 1);
                        l.rawget(-3);
                        l.rawset(-3);
                    }
                }
            }
        }
        public T[] ToArray()
        {
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                l.PushLua(Binding);
                var cnt = l.getn(-1);
                T[] result = new T[cnt];
                for (int i = 0; i < cnt; ++i)
                {
                    l.pushnumber(i + 1);
                    l.rawget(-2);
                    var val = l.GetLua<T>(-1);
                    result[i] = val;
                    l.pop(1);
                }
                return result;
            }
        }
        public void TrimExcess()
        {
        }
        public bool TrueForAll(Predicate<T> match)
        {
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                l.PushLua(Binding);
                var cnt = l.getn(-1);
                for (int i = 0; i < cnt; ++i)
                {
                    l.pushnumber(i + 1);
                    l.rawget(-2);
                    var val = l.GetLua<T>(-1);
                    l.pop(1);
                    if (!match(val))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        int IList.Add(object value)
        {
            Add((T)value);
            return Count - 1;
        }
        bool IList.Contains(object value)
        {
            if (value is T)
            {
                return Contains((T)value);
            }
            if (value == null && !typeof(T).IsValueType)
            {
                return Contains((T)value);
            }
            return false;
        }
        int IList.IndexOf(object value)
        {
            if (value is T)
            {
                return IndexOf((T)value);
            }
            if (value == null && !typeof(T).IsValueType)
            {
                return IndexOf((T)value);
            }
            return -1;
        }
        void IList.Insert(int index, object value)
        {
            Insert(index, (T)value);
        }
        void IList.Remove(object value)
        {
            if (value is T)
            {
                Remove((T)value);
            }
            if (value == null && !typeof(T).IsValueType)
            {
                Remove((T)value);
            }
        }
    }

    public class LuaQueue<T> : LuaList<T>
    {
        private static new LuaHub.BaseLuaWrapperHub<LuaQueue<T>> LuaHubSub = new LuaHub.BaseLuaWrapperHub<LuaQueue<T>>();

        public LuaQueue() : base() { }
        public LuaQueue(IntPtr l) : base(l) { }

        public T Dequeue()
        {
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                l.PushLua(Binding);
                T item;
                l.pushnumber(1);
                l.rawget(-2);
                l.GetLua(-1, out item);
                l.pop(1);
                var cnt = l.getn(-1);
                for (int i = 1; i < cnt; ++i)
                {
                    l.pushnumber(i);
                    l.pushnumber(i + 1);
                    l.rawget(-3);
                    l.rawset(-3);
                }
                l.pushnumber(cnt);
                l.pushnil();
                l.rawset(-3);
                return item;
            }
        }
        public void Enqueue(T item)
        {
            Add(item);
        }
        public T Peek()
        {
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                l.PushLua(Binding);
                T item;
                l.pushnumber(1);
                l.rawget(-2);
                l.GetLua(-1, out item);
                return item;
            }
        }
    }

    public class LuaStack<T> : LuaList<T>
    {
        private static new LuaHub.BaseLuaWrapperHub<LuaStack<T>> LuaHubSub = new LuaHub.BaseLuaWrapperHub<LuaStack<T>>();

        public LuaStack() : base() { }
        public LuaStack(IntPtr l) : base(l) { }

        public T Peek()
        {
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                l.PushLua(Binding);
                var cnt = l.getn(-1);
                if (cnt > 0)
                {
                    l.pushnumber(cnt);
                    l.rawget(-2);
                    return l.GetLua<T>(-1);
                }
                else
                {
                    return default(T);
                }
            }
        }
        public T Pop()
        {
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                l.PushLua(Binding);
                var cnt = l.getn(-1);
                if (cnt > 0)
                {
                    l.pushnumber(cnt);
                    l.rawget(-2);
                    var item = l.GetLua<T>(-1);
                    l.pop(1);
                    l.pushnumber(cnt);
                    l.pushnil();
                    l.rawset(-3);
                    return item;
                }
                else
                {
                    return default(T);
                }
            }
        }
        public void Push(T item)
        {
            Add(item);
        }
    }

    public class LuaDictionary<TK, TV> : BaseLuaWrapper<LuaDictionary<TK, TV>>, ICollection<KeyValuePair<TK, TV>>, IEnumerable<KeyValuePair<TK, TV>>, IEnumerable, IDictionary<TK, TV>, IReadOnlyCollection<KeyValuePair<TK, TV>>, IReadOnlyDictionary<TK, TV>, ICollection, IDictionary,
        UnityEngineEx.IConvertibleDictionary
    {
        public LuaDictionary() : base() { }
        public LuaDictionary(IntPtr l) : base(l) { }
        public virtual bool IsRaw { get; set; }

        protected static IEqualityComparer<TK> _KeyComparer = EqualityComparer<TK>.Default;
        protected static bool Equals(TK v1, TK v2)
        {
            return _KeyComparer.Equals(v1, v2);
        }
        protected static IEqualityComparer<TV> _ValueComparer = EqualityComparer<TV>.Default;
        protected static bool EqualsValue(TV v1, TV v2)
        {
            return _ValueComparer.Equals(v1, v2);
        }

        protected void GetTable(IntPtr l, int index)
        {
            if (IsRaw)
            {
                l.rawget(index);
            }
            else
            {
                l.gettable(index);
            }
        }
        protected void SetTable(IntPtr l, int index)
        {
            if (IsRaw)
            {
                l.rawset(index);
            }
            else
            {
                l.settable(index);
            }
        }
        protected void GetTable(int index)
        {
            GetTable(L, index);
        }
        protected void SetTable(int index)
        {
            SetTable(L, index);
        }

        public TV this[TK key]
        {
            get
            {
                var l = L;
                using (var lr = l.CreateStackRecover())
                {
                    l.PushLua(Binding);
                    TV item;
                    l.PushLua(key);
                    GetTable(l, -2);
                    l.GetLua(-1, out item);
                    return item;
                }
            }
            set
            {
                var l = L;
                using (var lr = l.CreateStackRecover())
                {
                    l.PushLua(Binding);
                    l.PushLua(key);
                    l.PushLua(value);
                    SetTable(l, -3);
                }
            }
        }
        public int Count
        {
            get
            {
                int count = 0;
                var l = L;
                using (var lr = l.CreateStackRecover())
                {
                    l.PushLua(Binding);
                    l.pushnil();
                    while (l.next(-2))
                    {
                        ++count;
                        l.pop(1);
                    }
                }
                return count;
            }
        }
        public void Add(TK key, TV value)
        {
            this[key] = value;
        }
        public void Clear()
        {
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                l.PushLua(Binding);
                l.pushnil();
                while (l.next(-2))
                {
                    l.pop(1);
                    l.pushvalue(-1);
                    l.pushnil();
                    l.rawset(-4);
                }
            }
        }
        public bool ContainsKey(TK key)
        {
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                l.PushLua(Binding);
                l.PushLua(key);
                GetTable(l, -2);
                return !l.isnoneornil(-1);
            }
        }
        public bool Remove(TK key)
        {
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                l.PushLua(Binding);
                l.PushLua(key);
                l.pushvalue(-1);
                GetTable(l, -3);
                var exist = !l.isnoneornil(-1);
                l.pop(1);
                l.pushnil();
                SetTable(l, -3);
                return exist;
            }
        }
        public bool TryGetValue(TK key, out TV value)
        {
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                l.PushLua(Binding);
                l.PushLua(key);
                GetTable(l, -2);
                var exist = !l.isnoneornil(-1);
                if (exist)
                {
                    l.GetLua(-1, out value);
                }
                else
                {
                    value = default(TV);
                }
                return exist;
            }
        }
        public bool ContainsValue(TV value)
        {
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                l.PushLua(Binding);
                l.pushnil();
                while (l.next(-2))
                {
                    var item = l.GetLua<TV>(-1);
                    if (EqualsValue(item, value))
                    {
                        return true;
                    }
                    l.pop(1);
                }
            }
            return false;
        }

        public void CopyTo(Array array, int arrayIndex)
        {
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                int count = 0;
                l.PushLua(Binding);
                l.pushnil();
                while (l.next(-2))
                {
                    var val = l.GetLua<TV>(-1);
                    l.pushvalue(-2);
                    var key = l.GetLua<TK>(-1);
                    array.SetValue(new KeyValuePair<TK, TV>(key, val), arrayIndex + count);
                    l.pop(2);
                    ++count;
                }
            }
        }
        public void CopyTo(KeyValuePair<TK, TV>[] array, int arrayIndex)
        {
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                int count = 0;
                l.PushLua(Binding);
                l.pushnil();
                while (l.next(-2))
                {
                    var val = l.GetLua<TV>(-1);
                    l.pushvalue(-2);
                    var key = l.GetLua<TK>(-1);
                    array[arrayIndex + count] = new KeyValuePair<TK, TV>(key, val);
                    l.pop(2);
                    ++count;
                }
            }
        }
        public void Add(KeyValuePair<TK, TV> item)
        {
            Add(item.Key, item.Value);
        }
        public bool Contains(KeyValuePair<TK, TV> item)
        {
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                l.PushLua(Binding);
                l.PushLua(item.Key);
                GetTable(l, -2);
                var existing = l.GetLua<TV>(-1);
                if (EqualsValue(existing, item.Value))
                {
                    return true;
                }
                return false;
            }
        }
        public bool Remove(KeyValuePair<TK, TV> item)
        {
            var l = L;
            using (var lr = l.CreateStackRecover())
            {
                l.PushLua(Binding);
                l.PushLua(item.Key);
                l.pushvalue(-1);
                GetTable(l, -3);
                var existing = l.GetLua<TV>(-1);
                if (EqualsValue(existing, item.Value))
                {
                    l.pop(1);
                    l.pushnil();
                    SetTable(l, -3);
                    return true;
                }
                return false;
            }
        }

        public struct Enumerator : IEnumerator<KeyValuePair<TK, TV>>, IEnumerator, IDisposable, IDictionaryEnumerator
        {
            private BaseLua Binding;
            private object CurrentKey;

            public Enumerator(LuaDictionary<TK, TV> thiz)
            {
                Current = default(KeyValuePair<TK, TV>);
                CurrentKey = null;
                Binding = thiz.Binding;
            }

            public KeyValuePair<TK, TV> Current { get; private set; }

            object IEnumerator.Current { get { return Current; } }

            DictionaryEntry IDictionaryEnumerator.Entry { get { return new DictionaryEntry(Current.Key, Current.Value); } }

            object IDictionaryEnumerator.Key { get { return Current.Key; } }

            object IDictionaryEnumerator.Value { get { return Current.Value; } }

            public void Dispose()
            {
                Current = default(KeyValuePair<TK, TV>);
                CurrentKey = null;
                Binding = null;
            }
            public bool MoveNext()
            {
                var l = Binding.L;
                using (var lr = l.CreateStackRecover())
                {
                    l.PushLua(Binding);
                    l.PushLua(CurrentKey);
                    var success = l.next(-2);
                    if (success)
                    {
                        var val = l.GetLua<TV>(-1);
                        l.pushvalue(-2);
                        var key = l.GetLua<TK>(-1);
                        Current = new KeyValuePair<TK, TV>(key, val);
                        CurrentKey = l.GetLua(-3);
                    }
                    else
                    {
                        Current = default(KeyValuePair<TK, TV>);
                        //CurrentKey = null;
                    }
                    return success;
                }
            }
            public void Reset()
            {
                Current = default(KeyValuePair<TK, TV>);
                CurrentKey = null;
            }
        }
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }
        IEnumerator<KeyValuePair<TK, TV>> IEnumerable<KeyValuePair<TK, TV>>.GetEnumerator()
        {
            return GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return GetEnumerator();
        }

        public KeyCollection Keys { get { return new KeyCollection(this); } }
        public ValueCollection Values { get { return new ValueCollection(this); } }
        ICollection<TK> IDictionary<TK, TV>.Keys { get { return Keys; } }
        ICollection<TV> IDictionary<TK, TV>.Values { get { return Values; } }
        IEnumerable<TK> IReadOnlyDictionary<TK, TV>.Keys { get { return Keys; } }
        IEnumerable<TV> IReadOnlyDictionary<TK, TV>.Values { get { return Values; } }
        ICollection IDictionary.Keys { get { return Keys; } }
        ICollection IDictionary.Values { get { return Values; } }
        public struct KeyCollection : ICollection<TK>, IEnumerable<TK>, IEnumerable, IReadOnlyCollection<TK>, ICollection
        {
            private LuaDictionary<TK, TV> Parent;
            public KeyCollection(LuaDictionary<TK, TV> thiz)
            {
                Parent = thiz;
            }
            public int Count { get { return Parent.Count; } }
            public void CopyTo(TK[] array, int index)
            {
                var l = Parent.L;
                using (var lr = l.CreateStackRecover())
                {
                    int count = 0;
                    l.PushLua(Parent.Binding);
                    l.pushnil();
                    while (l.next(-2))
                    {
                        l.pushvalue(-2);
                        var key = l.GetLua<TK>(-1);
                        array[index + count] = key;
                        l.pop(2);
                        ++count;
                    }
                }
            }
            public bool Contains(TK key)
            {
                return Parent.ContainsKey(key);
            }
            public bool IsReadOnly { get { return true; } }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(Parent.GetEnumerator());
            }
            IEnumerator<TK> IEnumerable<TK>.GetEnumerator()
            {
                return GetEnumerator();
            }
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public struct Enumerator : IEnumerator<TK>, IEnumerator, IDisposable
            {
                private LuaDictionary<TK, TV>.Enumerator Parent;
                public Enumerator(LuaDictionary<TK, TV>.Enumerator parent)
                {
                    Parent = parent;
                }
                public TK Current { get { return Parent.Current.Key; } }
                object IEnumerator.Current { get { return Current; } }

                public void Dispose()
                {
                    Parent.Dispose();
                }
                public bool MoveNext()
                {
                    return Parent.MoveNext();
                }
                public void Reset()
                {
                    Parent.Reset();
                }
            }

            void ICollection<TK>.Add(TK item)
            {
                throw new NotSupportedException();
            }
            void ICollection<TK>.Clear()
            {
                throw new NotSupportedException();
            }
            bool ICollection<TK>.Remove(TK item)
            {
                throw new NotSupportedException();
            }
            public bool IsSynchronized { get { return false; } }
            public object SyncRoot { get { return LuaHubSub; } }
            public void CopyTo(Array array, int index)
            {
                var l = Parent.L;
                using (var lr = l.CreateStackRecover())
                {
                    int count = 0;
                    l.PushLua(Parent.Binding);
                    l.pushnil();
                    while (l.next(-2))
                    {
                        l.pushvalue(-2);
                        var key = l.GetLua<TK>(-1);
                        array.SetValue(key, index + count);
                        l.pop(2);
                        ++count;
                    }
                }
            }
        }
        public sealed class ValueCollection : ICollection<TV>, IEnumerable<TV>, IEnumerable, IReadOnlyCollection<TV>, ICollection
        {
            private LuaDictionary<TK, TV> Parent;
            public ValueCollection(LuaDictionary<TK, TV> thiz)
            {
                Parent = thiz;
            }
            public int Count { get { return Parent.Count; } }
            public void CopyTo(TV[] array, int index)
            {
                var l = Parent.L;
                using (var lr = l.CreateStackRecover())
                {
                    int count = 0;
                    l.PushLua(Parent.Binding);
                    l.pushnil();
                    while (l.next(-2))
                    {
                        var val = l.GetLua<TV>(-1);
                        array[index + count] = val;
                        l.pop(1);
                        ++count;
                    }
                }
            }
            public bool Contains(TV val)
            {
                return Parent.ContainsValue(val);
            }
            public bool IsReadOnly { get { return true; } }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(Parent.GetEnumerator());
            }
            IEnumerator<TV> IEnumerable<TV>.GetEnumerator()
            {
                return GetEnumerator();
            }
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public struct Enumerator : IEnumerator<TV>, IEnumerator, IDisposable
            {
                private LuaDictionary<TK, TV>.Enumerator Parent;
                public Enumerator(LuaDictionary<TK, TV>.Enumerator parent)
                {
                    Parent = parent;
                }
                public TV Current { get { return Parent.Current.Value; } }
                object IEnumerator.Current { get { return Current; } }

                public void Dispose()
                {
                    Parent.Dispose();
                }
                public bool MoveNext()
                {
                    return Parent.MoveNext();
                }
                public void Reset()
                {
                    Parent.Reset();
                }
            }

            void ICollection<TV>.Add(TV item)
            {
                throw new NotSupportedException();
            }
            void ICollection<TV>.Clear()
            {
                throw new NotSupportedException();
            }
            bool ICollection<TV>.Remove(TV item)
            {
                throw new NotSupportedException();
            }
            public bool IsSynchronized { get { return false; } }
            public object SyncRoot { get { return LuaHubSub; } }
            public void CopyTo(Array array, int index)
            {
                var l = Parent.L;
                using (var lr = l.CreateStackRecover())
                {
                    int count = 0;
                    l.PushLua(Parent.Binding);
                    l.pushnil();
                    while (l.next(-2))
                    {
                        var val = l.GetLua<TV>(-1);
                        array.SetValue(val, index + count);
                        l.pop(1);
                        ++count;
                    }
                }
            }
        }

        public bool IsSynchronized { get { return false; } }
        public object SyncRoot { get { return LuaHubSub; } }
        public bool IsReadOnly { get { return false; } }
        public bool IsFixedSize { get { return false; } }

        void IDictionary.Add(object key, object value)
        {
            Add((TK)key, (TV)value);
        }
        bool IDictionary.Contains(object key)
        {
            if (key is TK)
            {
                return ContainsKey((TK)key);
            }
            if (key == null && !typeof(TK).IsValueType)
            {
                return ContainsKey((TK)key);
            }
            return false;
        }
        void IDictionary.Remove(object key)
        {
            if (key is TK)
            {
                Remove((TK)key);
            }
        }
        object IDictionary.this[object key]
        {
            get { return this[(TK)key]; }
            set { this[(TK)key] = (TV)value; }
        }
    }
}

#if UNITY_EDITOR
#if UNITY_INCLUDE_TESTS
#region TESTS
namespace Mods.Test
{
    using UnityEngineEx;

    public static class TestLuaCollections
    {
        [UnityEditor.MenuItem("Test/Lua/Test Collections", priority = 300030)]
        public static void Test()
        {
            var l = GlobalLua.L.L;
            using (var lr = l.CreateStackRecover())
            {
                var ll = new LuaList<int>(l);
                for (int i = 0; i < 10; ++i)
                {
                    ll.Add(i);
                }

                ll.RemoveRange(3, 2);
                l.CallGlobal("dumpraw", Pack(ll));

                l.newtable();
                LuaQueue<int> lq;
                l.GetLua(-1, out lq);
                for (int i = 0; i < 10; ++i)
                {
                    lq.Enqueue(i);
                }
                PlatDependant.LogInfo(lq.Dequeue());
                PlatDependant.LogInfo(lq.Dequeue());
                l.CallGlobal("dumpraw", Pack(lq));
                l.pop(1);

                l.newtable();
                LuaStack<int> ls = l.GetLuaOnStack(-1).GetWrapper<LuaStack<int>>();
                for (int i = 0; i < 10; ++i)
                {
                    ls.Push(i);
                }
                PlatDependant.LogInfo(ls.Pop());
                PlatDependant.LogInfo(ls.Pop());
                l.CallGlobal("dumpraw", Pack(ls));
                l.pop(1);

                var ld = new LuaDictionary<string, int>(l);
                for (int i = 0; i < 10; ++i)
                {
                    ld["s" + i] = i;
                }
                PlatDependant.LogInfo(ld["s3"]);
                PlatDependant.LogInfo(ld.Remove("s3"));
                int s4;
                PlatDependant.LogInfo(ld.TryGetValue("s4", out s4));
                PlatDependant.LogInfo(s4);

                PlatDependant.LogInfo(ld.Get<GCCollectionMode>("s0"));
                l.CallGlobal("dumpraw", Pack(ld));
            }
        }
    }
}
#endregion
#endif
#endif
