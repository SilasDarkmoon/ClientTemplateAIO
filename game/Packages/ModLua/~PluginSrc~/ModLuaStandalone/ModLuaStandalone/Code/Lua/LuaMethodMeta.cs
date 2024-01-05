#if UNITY_2020_2_OR_NEWER || NETCOREAPP3_0 || NETCOREAPP3_1 || NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1 || NETSTANDARD2_1_OR_GREATER
#define RUNTIME_HAS_READONLY_REF
#endif
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngineEx;

using lua = LuaLib.LuaCoreLib;
using lual = LuaLib.LuaAuxLib;
using luae = LuaLib.LuaLibEx;

namespace LuaLib
{
    public struct Types : IList<Type>, IEquatable<Types>
    {
        private Type t0;
        private Type t1;
        private Type t2;
        private Type t3;
        private Type t4;
        private Type t5;
        private Type t6;
        private Type t7;
        private Type t8;
        private Type t9;
        private List<Type> tx;

        private int _cnt;

        #region static funcs for set and get
        private delegate Type GetTypeDel(ref Types types);
        private delegate void SetTypeDel(ref Types types, Type type);

        private static Type GetType0(ref Types types) { return types.t0; }
        private static Type GetType1(ref Types types) { return types.t1; }
        private static Type GetType2(ref Types types) { return types.t2; }
        private static Type GetType3(ref Types types) { return types.t3; }
        private static Type GetType4(ref Types types) { return types.t4; }
        private static Type GetType5(ref Types types) { return types.t5; }
        private static Type GetType6(ref Types types) { return types.t6; }
        private static Type GetType7(ref Types types) { return types.t7; }
        private static Type GetType8(ref Types types) { return types.t8; }
        private static Type GetType9(ref Types types) { return types.t9; }

        private static void SetType0(ref Types types, Type type) { types.t0 = type; }
        private static void SetType1(ref Types types, Type type) { types.t1 = type; }
        private static void SetType2(ref Types types, Type type) { types.t2 = type; }
        private static void SetType3(ref Types types, Type type) { types.t3 = type; }
        private static void SetType4(ref Types types, Type type) { types.t4 = type; }
        private static void SetType5(ref Types types, Type type) { types.t5 = type; }
        private static void SetType6(ref Types types, Type type) { types.t6 = type; }
        private static void SetType7(ref Types types, Type type) { types.t7 = type; }
        private static void SetType8(ref Types types, Type type) { types.t8 = type; }
        private static void SetType9(ref Types types, Type type) { types.t9 = type; }

        private static GetTypeDel[] GetTypeFuncs = new GetTypeDel[]
        {
            GetType0,
            GetType1,
            GetType2,
            GetType3,
            GetType4,
            GetType5,
            GetType6,
            GetType7,
            GetType8,
            GetType9,
        };
        private static SetTypeDel[] SetTypeFuncs = new SetTypeDel[]
        {
            SetType0,
            SetType1,
            SetType2,
            SetType3,
            SetType4,
            SetType5,
            SetType6,
            SetType7,
            SetType8,
            SetType9,
        };
        #endregion

        #region IList<Type>
        public int IndexOf(Type item)
        {
            for (int i = 0; i < _cnt; ++i)
            {
                if (this[i] == item)
                {
                    return i;
                }
            }
            return -1;
        }

        public void Insert(int index, Type item)
        {
            if (index >= 0 && index <= _cnt)
            {
                this.Add(null);
                for (int i = _cnt - 1; i > index; --i)
                {
                    this[i] = this[i - 1];
                }
                this[index] = item;
            }
        }

        public void RemoveAt(int index)
        {
            if (index >= 0 && index < _cnt)
            {
                for (int i = index + 1; i < _cnt; ++i)
                {
                    this[i - 1] = this[i];
                }
                this[_cnt - 1] = null;
                --_cnt;
            }
        }

        public Type this[int index]
        {
            get
            {
                if (index >= 0 && index < _cnt)
                {
                    if (index < GetTypeFuncs.Length)
                    {
                        return GetTypeFuncs[index](ref this);
                    }
                    else
                    {
                        if (tx != null)
                        {
                            var pindex = index - GetTypeFuncs.Length;
                            if (pindex < tx.Count)
                            {
                                return tx[pindex];
                            }
                        }
                    }
                }
                return null;
            }
            set
            {
                if (index >= 0 && index < _cnt)
                {
                    if (index < SetTypeFuncs.Length)
                    {
                        SetTypeFuncs[index](ref this, value);
                    }
                    else
                    {
                        if (tx != null)
                        {
                            var pindex = index - SetTypeFuncs.Length;
                            if (pindex < tx.Count)
                            {
                                tx[pindex] = value;
                            }
                        }
                    }
                }
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(Type item)
        {
            if (_cnt < SetTypeFuncs.Length)
            {
                this[_cnt++] = item;
            }
            else
            {
                ++_cnt;
                if (tx == null)
                {
                    tx = new List<Type>(8);
                }
                tx.Add(item);
            }
        }

        public void Clear()
        {
            _cnt = 0;
            t0 = null;
            t1 = null;
            t2 = null;
            t3 = null;
            t4 = null;
            t5 = null;
            t6 = null;
            t7 = null;
            t8 = null;
            t9 = null;
            tx = null;
        }

        public bool Contains(Type item)
        {
            return IndexOf(item) >= 0;
        }

        public void CopyTo(Type[] array, int arrayIndex)
        {
            if (arrayIndex >= 0)
            {
                for (int i = 0; i < _cnt && i + arrayIndex < array.Length; ++i)
                {
                    array[arrayIndex + i] = this[i];
                }
            }
        }

        public int Count
        {
            get { return _cnt; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(Type item)
        {
            var index = IndexOf(item);
            if (index >= 0 && index < _cnt)
            {
                RemoveAt(index);
                return true;
            }
            return false;
        }

        public IEnumerator<Type> GetEnumerator()
        {
            for (int i = 0; i < _cnt; ++i)
            {
                yield return this[i];
            }
        }
        #endregion

        public override bool Equals(object obj)
        {
            if (obj is Types)
            {
                Types types2 = (Types)obj;
                if (types2._cnt == _cnt)
                {
                    for (int i = 0; i < _cnt; ++i)
                    {
                        if (this[i] != types2[i])
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
            return false;
        }
        public bool Equals(Types types2)
        {
            if (types2._cnt == _cnt)
            {
                for (int i = 0; i < _cnt; ++i)
                {
                    if (this[i] != types2[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
        internal static bool OpEquals(Types source, Types other)
        {
            return source.Equals(other);
        }
        public static bool operator ==(Types source, Types other)
        {
            return OpEquals(source, other);
        }
        public static bool operator !=(Types source, Types other)
        {
            return !OpEquals(source, other);
        }

        public static bool IsLuaWrapper(Type t)
        {
            return typeof(Delegate).IsAssignableFrom(t) || typeof(LuaLib.ILuaWrapper).IsAssignableFrom(t) || typeof(LuaLib.BaseLua).IsAssignableFrom(t);
        }
        public enum TypeNullableCate
        {
            Object,
            Nullable,
            ValueType,
        }
        public static TypeNullableCate GetNullableCate(Type t)
        {
            Type nntype;
            return GetNullableCate(t, out nntype);
        }
        public static TypeNullableCate GetNullableCate(Type t, out Type nntype)
        {
            if (t.IsValueType)
            {
                nntype = Nullable.GetUnderlyingType(t);
                if (nntype == null)
                {

                    return TypeNullableCate.ValueType;
                }
                else
                {
                    return TypeNullableCate.Nullable;
                }
            }
            else
            {
                nntype = null;
                return TypeNullableCate.Object;
            }
        }
        // the greater weight means more detail and explicit
        public static int Compare(Types ta, Types tb)
        {
            // TODO : IComparable IComparable<T>
            if (ta.Count != tb.Count)
            {
                return ta.Count - tb.Count;
            }
            for (int i = 0; i < ta.Count; ++i)
            {
                var tya = ta[i];
                var tyb = tb[i];
                if (tya != tyb)
                {
                    if (tya == null)
                    {
                        return -1;
                    }
                    if (tyb == null)
                    {
                        return 1;
                    }
                    if (tya.IsAssignableFrom(tyb))
                    {
                        return -1;
                    }
                    if (tyb.IsAssignableFrom(tya))
                    {
                        return 1;
                    }
                    var a2b = LuaHub.CanConvertRaw(tya, tyb);
                    var b2a = LuaHub.CanConvertRaw(tyb, tya);
                    if (a2b && !b2a)
                    {
                        return 1;
                    }
                    if (!a2b && b2a)
                    {
                        return -1;
                    }
                    if (!a2b && !b2a)
                    {
                        // Delegates、ILuaWrapper、BaseLua
                        bool iswa = IsLuaWrapper(tya);
                        bool iswb = IsLuaWrapper(tyb);
                        if (iswa != iswb)
                        {
                            if (iswa)
                            {
                                return -1;
                            }
                            else
                            {
                                return 1;
                            }
                        }
                        // Nullable?
                        var nca = GetNullableCate(tya);
                        var ncb = GetNullableCate(tyb);
                        if (nca != ncb)
                        {
                            return ((int)nca) - ((int)ncb);
                        }

                        // (string) (params string[]) - this can do with comparing HasElementType.
                        // notice: but maybe we should move this to GroupMethodMeta? and we should check (string[]) and (params string[][])
                        var hasea = tya.HasElementType;
                        var haseb = tyb.HasElementType;
                        if (hasea != haseb)
                        {
                            return hasea ? 1 : -1;
                        }
                        // (short) (double) call with float - this is hard to judge, maybe we should check implicit operator?
                        return tya.FullName.GetHashCode() - tyb.FullName.GetHashCode();
                    }
                    else // (a2b && b2a)
                    {
                        // Nullable?
                        Type nnta, nntb;
                        var nca = GetNullableCate(tya, out nnta);
                        var ncb = GetNullableCate(tyb, out nntb);
                        if (nca != ncb)
                        {
                            return ((int)nca) - ((int)ncb);
                        }

                        var rv = LuaHub.GetTypeWeight(nnta ?? tya) - LuaHub.GetTypeWeight(nntb ?? tyb);
                        if (rv != 0)
                        {
                            return rv;
                        }
                    }
                }
            }
            return 0;
        }

        public override int GetHashCode()
        {
            int code = 0;
            for (int i = 0; i < Count; ++i)
            {
                code <<= 1;
                var type = this[i];
                if (type != null)
                {
                    code += type.GetHashCode();
                }
            }
            return code;
        }

        public Types Clone()
        {
            var newtypes = new Types();
            for (int i = 0; i < _cnt; ++i)
            {
                newtypes.Add(this[i]);
            }
            return newtypes;
        }
    }

    public abstract class BaseMethodMeta : SelfHandled, ILuaMetaCall, ILuaTrans//, ILuaPush
    {
        public static Types GetArgTypes(IntPtr l)
        {
            var index = 1;
            var top = l.gettop();
            Types types = new Types();
            for (; index <= top; ++index)
            {
                types.Add(l.GetType(index));
            }
            return types;
        }

        public abstract int CanCallByArgsOfType(Types pt);
        public bool CanCall(Types pt)
        {
            return CanCallByArgsOfType(pt) >= 0;
        }

        #region ILuaMetaCall
        public abstract void call(IntPtr l, object tar);
        #endregion

        public virtual void WrapFunctionByTable(IntPtr l)
        {
#if !DISABLE_LUA_HUB_C
            if (LuaHub.LuaHubC.Ready)
            {
                LuaHub.LuaHubC.lua_wrapFunctionByTable(l, r);
            }
            else
#endif
            {
                // rawfunc
                l.checkstack(5);
                l.newtable(); // rawfunc ftab
                l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS); // rawfunc ftab #trans
                l.pushlightuserdata(r); // rawfunc ftab #trans trans
                l.rawset(-3); // rawfunc ftab
                l.pushlightuserdata(LuaConst.LRKEY_TARGET); // rawfunc ftab #tar
                l.pushvalue(-3); // rawfunc ftab #tar rawfunc
                l.rawset(-3); // rawfunc ftab
                l.newtable(); // rawfunc ftab meta
                l.PushString(LuaConst.LS_META_KEY_CALL); // rawfunc ftab meta __call
                l.pushvalue(-4); // rawfunc ftab meta __call rawfunc
                l.pushcclosure(LuaFuncWrapCall, 1); // rawfunc ftab meta __call func
                l.rawset(-3); // rawfunc ftab meta
                l.pushlightuserdata(LuaConst.LRKEY_EXTENDED);
                l.pushnumber(3);
                l.rawset(-3);
                l.setmetatable(-2); // rawfunc ftab
                l.remove(-2); // ftab
            }
        }

        private static readonly lua.CFunction LuaFuncWrapCall = new lua.CFunction(LuaMetaWrapCall);

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        private static int LuaMetaWrapCall(IntPtr l)
        {
            var oldtop = l.gettop();
            l.checkstack(oldtop + 2);
            l.pushcfunction(LuaHub.LuaFuncOnError); // err
            l.pushvalue(lua.upvalueindex(1)); // err realfunc
            int argc = 0;
            for (int i = 2; i <= oldtop; ++i)
            {
                ++argc;
                l.pushvalue(i);
            }
            // err realfunc args(*argc)
            var code = l.pcall(argc, lua.LUA_MULTRET, oldtop + 1); // err rv(*n)
            if (code != 0)
            { // err failmessage
                l.pop(2); // X
                return 0;
            }
            l.remove(oldtop + 1); // rv(*n)
            return l.gettop() - oldtop;
        }

        #region ILuaTrans
        public void SetData(IntPtr l, int index, object val)
        {
            // can not change
            l.LogError("ILuaMetaCall wrapper's binded-obj cannot be changed.");
        }

        public object GetLua(IntPtr l, int index)
        {
            return this;
        }

        public Type GetType(IntPtr l, int index)
        {
            return GetType();
        }
        public bool Nonexclusive { get { return false; } }
        #endregion

        //#region ILuaPush
        //public IntPtr PushLuaRaw(IntPtr l, object val)
        //{
        //    l.PushFunction(this);
        //    return IntPtr.Zero;
        //}

        //public void BindMetaTable(IntPtr l, object val, IntPtr h)
        //{
        //    WrapFunctionByTable(l);
        //}
        //#endregion

#if UNITY_EDITOR
        public static Action<Type, string> OnReflectInvokeMember = null;
        internal static void TrigOnReflectInvokeMember(Type type, string member)
        {
            if (OnReflectInvokeMember != null)
                OnReflectInvokeMember(type, member);
        }
        internal static void TrigOnReflectInvokeMember(Type type, System.Reflection.MemberInfo member)
        {
            var attrs = member.GetCustomAttributes(typeof(ObsoleteAttribute));
            if (attrs != null)
            {
                foreach (var attr in attrs)
                {
                    var oattr = attr as ObsoleteAttribute;
                    if (oattr != null)
                    {
                        if (oattr.IsError)
                        {
                            throw new NotSupportedException(member.ToString() + " is Obsoleted.");
                        }
                        else
                        {
                            PlatDependant.LogWarning(member.ToString() + " is Obsoleted.");
                        }
                    }
                }
            }

            if (OnReflectInvokeMember != null)
                OnReflectInvokeMember(type, member.Name);
        }
#endif
    }

    public class CtorMethodMeta : SelfHandled, ILuaMetaCall
    {
        private Type _t;
        private BaseMethodMeta _NormalCtor;
        public BaseMethodMeta NormalCtor { get { return _NormalCtor; } }

        public CtorMethodMeta(Type t)
        {
            _t = t;
            var ctors = t.GetConstructors();
            _NormalCtor = PackedMethodMeta.CreateMethodMeta(ctors, null, false);
        }

        public void call(IntPtr l, object tar)
        {
#if UNITY_EDITOR
            BaseMethodMeta.TrigOnReflectInvokeMember(_t, ".ctor");
#endif
            var oldtop = l.gettop();
            if (oldtop <= 1 && _t.IsValueType())
            {
                try
                {
                    var rv = Activator.CreateInstance(_t);
                    l.PushLua(rv);
                }
                catch (Exception e)
                {
                    l.settop(oldtop);
                    l.LogError(e);
                }
            }
            else if (_NormalCtor != null)
            {
                _NormalCtor.call(l, tar);
            }
        }
    }

    public abstract class BaseUniqueMethodMeta : BaseMethodMeta
    {
        protected internal Types _ParamTypes;
        protected internal IList<int> _OutParamIndices;
        protected internal int _LastIsParams = -1;
        public abstract MethodBase Method { get; }

        public static BaseUniqueMethodMeta CreateMethodMeta(MethodBase minfo, bool updateDataAfterCall)
        {
            if (minfo is ConstructorInfo)
            {
                return new CtorInfoMethodMeta(minfo as ConstructorInfo);
            }
            else
            {
                if (minfo.IsStatic)
                {
                    return new StaticMethodMeta(minfo);
                }
                else
                {
                    if (updateDataAfterCall)
                    {
                        return new ValueTypeMethodMeta(minfo);
                    }
                    else
                    {
                        return new InstanceMethodMeta(minfo);
                    }
                }
            }
        }

    }

    internal class CtorInfoMethodMeta : BaseUniqueMethodMeta
    {
        private ConstructorInfo _Method;
        public override MethodBase Method { get { return _Method; } }

        public CtorInfoMethodMeta(ConstructorInfo minfo)
        {
            _Method = minfo;
            var pars = minfo.GetParameters();
            if (pars != null)
            {
                List<int> oindices = new List<int>(4);
                for (int i = 0; i < pars.Length; ++i)
                {
                    var type = pars[i].ParameterType;
                    if (type.IsByRef)
                    {
                        _ParamTypes.Add(type.GetElementType());
#if RUNTIME_HAS_READONLY_REF
                        if (!pars[i].IsIn)
#endif
                        {
                            oindices.Add(i);
                        }
                    }
                    else
                    {
                        _ParamTypes.Add(type);
                    }
                    if (i == pars.Length - 1 && type.IsArray)
                    {
                        var attrs = pars[i].GetCustomAttributes(typeof(ParamArrayAttribute), true);
#if NETFX_CORE
                        if (attrs != null && attrs.Count() > 0)
#else
                        if (attrs != null && attrs.Length > 0)
#endif
                        {
                            _LastIsParams = i;
                        }
                    }
                }
                _OutParamIndices = oindices;
            }
        }

        public override int CanCallByArgsOfType(Types pt)
        {
            // TODO: check the target.
            int rv = 0;
            var ptcnt = pt.Count - 1;
            for (int i = 0; i < _ParamTypes.Count; ++i)
            {
                var ptindex = i + 1;
                Type mtype = _ParamTypes[i];
                if (i == _LastIsParams)
                {
                    int ex = 0;
                    if (ptcnt == _ParamTypes.Count && mtype.IsAssignableFrom(pt[ptindex]))
                    {
                    }
                    else
                    {
                        var etype = mtype.GetElementType();
                        for (int j = ptindex; j < pt.Count; ++j)
                        {
                            var ctype = pt[j];
                            if (!LuaHub.CanConvertRaw(ctype, etype))
                            {
                                return -1;
                            }
                            if (ctype == null || !etype.IsAssignableFrom(ctype))
                            {
                                ex = 1;
                            }
                        }
                    }
                    rv += ex << i;
                    return rv < 0 ? int.MaxValue : rv;
                }
                Type curtype = null;
                if (ptindex < pt.Count)
                {
                    curtype = pt[ptindex];
                }
                if (!LuaHub.CanConvertRaw(curtype, mtype))
                { // can not call
                    return -1;
                }
                if (curtype == null || !mtype.IsAssignableFrom(curtype))
                { // this is numeric and the type do not match.
                    rv += 1 << i;
                }
            }
            for (int i = _ParamTypes.Count; i < ptcnt; ++i)
            {
                rv += 1 << i;
            }
            return rv < 0 ? int.MaxValue : rv;
        }

        public override void call(IntPtr l, object tar)
        {
            try
            {
                object[] rargs;
                var largc = l.gettop() - 1;
                if (_LastIsParams >= 0)
                {
                    rargs = ObjectPool.GetReturnValueFromPool(_LastIsParams + 1);
                    for (int i = 0; i < _LastIsParams; ++i)
                    {
                        object arg = null;
                        if (i < largc)
                        {
                            var index = i + 2;
                            arg = l.GetLua(index);
                        }
                        rargs[i] = arg.ConvertTypeRaw(_ParamTypes[i]);
                    }
                    Array arr = null;
                    if (largc == _LastIsParams + 1)
                    {
                        var raw = l.GetLua(-1);
                        if (_ParamTypes[_LastIsParams].IsInstanceOfType(raw))
                        {
                            arr = raw as Array;
                            rargs[_LastIsParams] = arr;
                        }
                    }
                    if (arr == null)
                    {
                        int arrLen = 0;
                        if (largc > _LastIsParams)
                        {
                            arrLen = largc - _LastIsParams;
                        }
                        var etype = _ParamTypes[_LastIsParams].GetElementType();
                        arr = Array.CreateInstance(etype, arrLen);
                        rargs[_LastIsParams] = arr;
                        for (int i = 0; i < arr.Length; ++i)
                        {
                            var index = _LastIsParams + i + 2;
                            arr.SetValue(l.GetLua(index).ConvertTypeRaw(etype), i);
                        }
                    }
                }
                else
                {
                    int len = _ParamTypes.Count;
                    rargs = ObjectPool.GetReturnValueFromPool(len);
                    for (int i = 0; i < len; ++i)
                    {
                        object arg = null;
                        if (i < largc)
                        {
                            var index = i + 2;
                            arg = l.GetLua(index);
                        }
                        rargs[i] = arg.ConvertTypeRaw(_ParamTypes[i]);
                    }
                }
                object result = _Method.Invoke(rargs);
                l.PushLua(result);
                if (_OutParamIndices != null && rargs != null)
                {
                    for (int ii = 0; ii < _OutParamIndices.Count; ++ii)
                    {
                        var index = _OutParamIndices[ii];
                        if (index >= 0 && index < rargs.Length)
                        {
                            l.PushLua(rargs[index]);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // perhaps we should make a Call and a TryCall. perhaps we should show which lua-state is doing the log.
                l.LogError("Unable To Call: " + _Method.Name + "@" + _Method.DeclaringType.Name + " \n" + e.ToString());
                throw;
            }
        }
    }

    public class StaticMethodMeta : BaseUniqueMethodMeta
    {
        private MethodBase _Method;
        public override MethodBase Method { get { return _Method; } }

        public StaticMethodMeta(MethodBase minfo)
        {
            _Method = minfo;
            var pars = minfo.GetParameters();
            if (pars != null)
            {
                List<int> oindices = new List<int>(4);
                for (int i = 0; i < pars.Length; ++i)
                {
                    var type = pars[i].ParameterType;
                    if (type.IsByRef)
                    {
                        _ParamTypes.Add(type.GetElementType());
#if RUNTIME_HAS_READONLY_REF
                        if (!pars[i].IsIn)
#endif
                        {
                            oindices.Add(i);
                        }
                    }
                    else
                    {
                        _ParamTypes.Add(type);
                    }
                    if (i == pars.Length - 1 && type.IsArray)
                    {
                        var attrs = pars[i].GetCustomAttributes(typeof(ParamArrayAttribute), true);
#if NETFX_CORE
                        if (attrs != null && attrs.Count() > 0)
#else
                        if (attrs != null && attrs.Length > 0)
#endif
                        {
                            _LastIsParams = i;
                        }
                    }
                }
                _OutParamIndices = oindices;
            }
        }

        public override int CanCallByArgsOfType(Types pt)
        {
            // TODO: check the target.
            int rv = 0;
            var ptcnt = pt.Count;
            for (int i = 0; i < _ParamTypes.Count; ++i)
            {
                var ptindex = i;
                Type mtype = _ParamTypes[i];
                if (i == _LastIsParams)
                {
                    int ex = 0;
                    if (ptcnt == _ParamTypes.Count && mtype.IsAssignableFrom(pt[ptindex]))
                    {
                    }
                    else
                    {
                        var etype = mtype.GetElementType();
                        for (int j = ptindex; j < pt.Count; ++j)
                        {
                            var ctype = pt[j];
                            if (!LuaHub.CanConvertRaw(ctype, etype))
                            {
                                return -1;
                            }
                            if (ctype == null || !etype.IsAssignableFrom(ctype))
                            {
                                ex = 1;
                            }
                        }
                    }
                    rv += ex << i;
                    return rv < 0 ? int.MaxValue : rv;
                }
                Type curtype = null;
                if (ptindex < pt.Count)
                {
                    curtype = pt[ptindex];
                }
                if (!LuaHub.CanConvertRaw(curtype, mtype))
                { // can not call
                    return -1;
                }
                if (curtype == null || !mtype.IsAssignableFrom(curtype))
                { // this is numeric and the type do not match.
                    rv += 1 << i;
                }
            }
            for (int i = _ParamTypes.Count; i < ptcnt; ++i)
            {
                rv += 1 << i;
            }
            return rv < 0 ? int.MaxValue : rv;
        }

        public override void call(IntPtr l, object tar)
        {
#if UNITY_EDITOR
            if (_Method != null)
            {
                TrigOnReflectInvokeMember(_Method.ReflectedType, _Method.Name);
            }
#endif
            try
            {
                object[] rargs;
                var largc = l.gettop();
                if (_LastIsParams >= 0)
                {
                    rargs = ObjectPool.GetReturnValueFromPool(_LastIsParams + 1);
                    for (int i = 0; i < _LastIsParams; ++i)
                    {
                        object arg = null;
                        if (i < largc)
                        {
                            var index = i + 1;
                            arg = l.GetLua(index);
                        }
                        rargs[i] = arg.ConvertTypeRaw(_ParamTypes[i]);
                    }
                    Array arr = null;
                    if (largc == _LastIsParams + 1)
                    {
                        var raw = l.GetLua(-1);
                        if (_ParamTypes[_LastIsParams].IsInstanceOfType(raw))
                        {
                            arr = raw as Array;
                            rargs[_LastIsParams] = arr;
                        }
                    }
                    if (arr == null)
                    {
                        int arrLen = 0;
                        if (largc > _LastIsParams)
                        {
                            arrLen = largc - _LastIsParams;
                        }
                        var etype = _ParamTypes[_LastIsParams].GetElementType();
                        arr = Array.CreateInstance(etype, arrLen);
                        rargs[_LastIsParams] = arr;
                        for (int i = 0; i < arr.Length; ++i)
                        {
                            var index = _LastIsParams + i + 1;
                            arr.SetValue(l.GetLua(index).ConvertTypeRaw(etype), i);
                        }
                    }
                }
                else
                {
                    int len = _ParamTypes.Count;
                    rargs = ObjectPool.GetReturnValueFromPool(len);
                    for (int i = 0; i < len; ++i)
                    {
                        object arg = null;
                        if (i < largc)
                        {
                            var index = i + 1;
                            arg = l.GetLua(index);
                        }
                        rargs[i] = arg.ConvertTypeRaw(_ParamTypes[i]);
                    }
                }
                object result = null;
                // ideally, we should not call the overridden method and call exactly the method provided by the MethodInfo.
                // but the MethodInfo will always call the finally overridden method provided by the target object.
                // there is a solution that creates delegate with RuntimeMethodHandle using Activator class.
                // but the delegate itself is to be declared. so this is not the common solution.
                // see http://stackoverflow.com/questions/4357729/use-reflection-to-invoke-an-overridden-base-method
                // the temporary solution is we should declare public non-virtual method in the derived class and call base.XXX in this method and we can call this method.
                result = _Method.Invoke(tar, rargs);
#if !UNITY_ENGINE && !UNITY_5_3_OR_NEWER || NET_4_6 || NET_STANDARD_2_0
                if (_Method.IsSpecialName)
                {
                    l.PushLua(result);
                }
                else
                {
                    LuaLib.LuaTupleUtils.PushValueOrTuple(l, result);
                }
#else
                l.PushLua(result);
#endif
                if (_OutParamIndices != null && rargs != null)
                {
                    for (int ii = 0; ii < _OutParamIndices.Count; ++ii)
                    {
                        var index = _OutParamIndices[ii];
                        if (index >= 0 && index < rargs.Length)
                        {
                            l.PushLua(rargs[index]);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // perhaps we should make a Call and a TryCall. perhaps we should show which lua-state is doing the log.
                l.LogError("Unable To Call: " + _Method.Name + "@" + _Method.DeclaringType.Name + " \n" + e.ToString());
                throw;
            }
        }
    }

    internal class InstanceMethodMeta : BaseUniqueMethodMeta
    {
        private MethodBase _Method;
        public override MethodBase Method { get { return _Method; } }

        public InstanceMethodMeta(MethodBase minfo)
        {
            _Method = minfo;
            var pars = minfo.GetParameters();
            if (pars != null)
            {
                List<int> oindices = new List<int>(4);
                for (int i = 0; i < pars.Length; ++i)
                {
                    var type = pars[i].ParameterType;
                    if (type.IsByRef)
                    {
                        _ParamTypes.Add(type.GetElementType());
#if RUNTIME_HAS_READONLY_REF
                        if (!pars[i].IsIn)
#endif
                        {
                            oindices.Add(i);
                        }
                    }
                    else
                    {
                        _ParamTypes.Add(type);
                    }
                    if (i == pars.Length - 1 && type.IsArray)
                    {
                        var attrs = pars[i].GetCustomAttributes(typeof(ParamArrayAttribute), true);
#if NETFX_CORE
                        if (attrs != null && attrs.Count() > 0)
#else
                        if (attrs != null && attrs.Length > 0)
#endif
                        {
                            _LastIsParams = i;
                        }
                    }
                }
                _OutParamIndices = oindices;
            }
        }

        public override int CanCallByArgsOfType(Types pt)
        {
            // TODO: check the target.
            int rv = 0;
            var ptcnt = pt.Count - 1;
            for (int i = 0; i < _ParamTypes.Count; ++i)
            {
                var ptindex = i + 1;
                Type mtype = _ParamTypes[i];
                if (i == _LastIsParams)
                {
                    int ex = 0;
                    if (ptcnt == _ParamTypes.Count && mtype.IsAssignableFrom(pt[ptindex]))
                    {
                    }
                    else
                    {
                        var etype = mtype.GetElementType();
                        for (int j = ptindex; j < pt.Count; ++j)
                        {
                            var ctype = pt[j];
                            if (!LuaHub.CanConvertRaw(ctype, etype))
                            {
                                return -1;
                            }
                            if (ctype == null || !etype.IsAssignableFrom(ctype))
                            {
                                ex = 1;
                            }
                        }
                    }
                    rv += ex << i;
                    return rv < 0 ? int.MaxValue : rv;
                }
                Type curtype = null;
                if (ptindex < pt.Count)
                {
                    curtype = pt[ptindex];
                }
                if (!LuaHub.CanConvertRaw(curtype, mtype))
                { // can not call
                    return -1;
                }
                if (curtype == null || !mtype.IsAssignableFrom(curtype))
                { // this is numeric and the type do not match.
                    rv += 1 << i;
                    // TODO: if (rv < 0) - already overflow, should return. - we should also check other metas, they have same problem.
                    // TODO: make a common function CanCall
                }
            }
            for (int i = _ParamTypes.Count; i < ptcnt; ++i)
            {
                rv += 1 << i;
            }
            return rv < 0 ? int.MaxValue : rv;
        }

        public override void call(IntPtr l, object tar)
        {
#if UNITY_EDITOR
            if (_Method != null)
            {
                TrigOnReflectInvokeMember(_Method.ReflectedType, _Method.Name);
            }
#endif
            try
            {
                object[] rargs;
                var largc = l.gettop() - 1;
                if (_LastIsParams >= 0)
                {
                    rargs = ObjectPool.GetReturnValueFromPool(_LastIsParams + 1);
                    for (int i = 0; i < _LastIsParams; ++i)
                    {
                        object arg = null;
                        if (i < largc)
                        {
                            var index = i + 2;
                            arg = l.GetLua(index);
                        }
                        rargs[i] = arg.ConvertTypeRaw(_ParamTypes[i]);
                    }
                    Array arr = null;
                    if (largc == _LastIsParams + 1)
                    {
                        var raw = l.GetLua(-1);
                        if (_ParamTypes[_LastIsParams].IsInstanceOfType(raw))
                        {
                            arr = raw as Array;
                            rargs[_LastIsParams] = arr;
                        }
                    }
                    if (arr == null)
                    {
                        int arrLen = 0;
                        if (largc > _LastIsParams)
                        {
                            arrLen = largc - _LastIsParams;
                        }
                        var etype = _ParamTypes[_LastIsParams].GetElementType();
                        arr = Array.CreateInstance(etype, arrLen);
                        rargs[_LastIsParams] = arr;
                        for (int i = 0; i < arr.Length; ++i)
                        {
                            var index = _LastIsParams + i + 2;
                            arr.SetValue(l.GetLua(index).ConvertTypeRaw(etype), i);
                        }
                    }
                }
                else
                {
                    int len = _ParamTypes.Count;
                    rargs = ObjectPool.GetReturnValueFromPool(len);
                    for (int i = 0; i < len; ++i)
                    {
                        object arg = null;
                        if (i < largc)
                        {
                            var index = i + 2;
                            arg = l.GetLua(index);
                        }
                        rargs[i] = arg.ConvertTypeRaw(_ParamTypes[i]);
                    }
                }
                object result = null;
                tar = l.GetLuaObject(1);
                // ideally, we should not call the overridden method and call exactly the method provided by the MethodInfo.
                // but the MethodInfo will always call the finally overridden method provided by the target object.
                // there is a solution that creates delegate with RuntimeMethodHandle using Activator class.
                // but the delegate itself is to be declared. so this is not the common solution.
                // see http://stackoverflow.com/questions/4357729/use-reflection-to-invoke-an-overridden-base-method
                // the temporary solution is we should declare public non-virtual method in the derived class and call base.XXX in this method and we can call this method.
                result = _Method.Invoke(tar, rargs);
#if !UNITY_ENGINE && !UNITY_5_3_OR_NEWER || NET_4_6 || NET_STANDARD_2_0
                if (_Method.IsSpecialName)
                {
                    l.PushLua(result);
                }
                else
                {
                    LuaLib.LuaTupleUtils.PushValueOrTuple(l, result);
                }
#else
                l.PushLua(result);
#endif
                if (_OutParamIndices != null && rargs != null)
                {
                    for (int ii = 0; ii < _OutParamIndices.Count; ++ii)
                    {
                        var index = _OutParamIndices[ii];
                        if (index >= 0 && index < rargs.Length)
                        {
                            l.PushLua(rargs[index]);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // perhaps we should make a Call and a TryCall. perhaps we should show which lua-state is doing the log.
                l.LogError("Unable To Call: " + _Method.Name + "@" + _Method.DeclaringType.Name + " \n" + e.ToString());
                throw;
            }
        }
    }

    internal class ValueTypeMethodMeta : BaseUniqueMethodMeta
    {
        private MethodBase _Method;
        public override MethodBase Method { get { return _Method; } }

        public ValueTypeMethodMeta(MethodBase minfo)
        {
            _Method = minfo;
            var pars = minfo.GetParameters();
            if (pars != null)
            {
                List<int> oindices = new List<int>(4);
                for (int i = 0; i < pars.Length; ++i)
                {
                    var type = pars[i].ParameterType;
                    if (type.IsByRef)
                    {
                        _ParamTypes.Add(type.GetElementType());
#if RUNTIME_HAS_READONLY_REF
                        if (!pars[i].IsIn)
#endif
                        {
                            oindices.Add(i);
                        }
                    }
                    else
                    {
                        _ParamTypes.Add(type);
                    }
                    if (i == pars.Length - 1 && type.IsArray)
                    {
                        var attrs = pars[i].GetCustomAttributes(typeof(ParamArrayAttribute), true);
#if NETFX_CORE
                        if (attrs != null && attrs.Count() > 0)
#else
                        if (attrs != null && attrs.Length > 0)
#endif
                        {
                            _LastIsParams = i;
                        }
                    }
                }
                _OutParamIndices = oindices;
            }
        }

        public override int CanCallByArgsOfType(Types pt)
        {
            // TODO: check the target.
            int rv = 0;
            var ptcnt = pt.Count - 1;
            for (int i = 0; i < _ParamTypes.Count; ++i)
            {
                var ptindex = i + 1;
                Type mtype = _ParamTypes[i];
                if (i == _LastIsParams)
                {
                    int ex = 0;
                    if (ptcnt == _ParamTypes.Count && mtype.IsAssignableFrom(pt[ptindex]))
                    {
                    }
                    else
                    {
                        var etype = mtype.GetElementType();
                        for (int j = ptindex; j < pt.Count; ++j)
                        {
                            var ctype = pt[j];
                            if (!LuaHub.CanConvertRaw(ctype, etype))
                            {
                                return -1;
                            }
                            if (ctype == null || !etype.IsAssignableFrom(ctype))
                            {
                                ex = 1;
                            }
                        }
                    }
                    rv += ex << i;
                    return rv < 0 ? int.MaxValue : rv;
                }
                Type curtype = null;
                if (ptindex < pt.Count)
                {
                    curtype = pt[ptindex];
                }
                if (!LuaHub.CanConvertRaw(curtype, mtype))
                { // can not call
                    return -1;
                }
                if (curtype == null || !mtype.IsAssignableFrom(curtype))
                { // this is numeric and the type do not match.
                    rv += 1 << i;
                }
            }
            for (int i = _ParamTypes.Count; i < ptcnt; ++i)
            {
                rv += 1 << i;
            }
            return rv < 0 ? int.MaxValue : rv;
        }

        public override void call(IntPtr l, object tar)
        {
#if UNITY_EDITOR
            if (_Method != null)
            {
                TrigOnReflectInvokeMember(_Method.ReflectedType, _Method.Name);
            }
#endif
            try
            {
                object[] rargs;
                var largc = l.gettop() - 1;
                if (_LastIsParams >= 0)
                {
                    rargs = ObjectPool.GetReturnValueFromPool(_LastIsParams + 1);
                    for (int i = 0; i < _LastIsParams; ++i)
                    {
                        object arg = null;
                        if (i < largc)
                        {
                            var index = i + 2;
                            arg = l.GetLua(index);
                        }
                        rargs[i] = arg.ConvertTypeRaw(_ParamTypes[i]);
                    }
                    Array arr = null;
                    if (largc == _LastIsParams + 1)
                    {
                        var raw = l.GetLua(-1);
                        if (_ParamTypes[_LastIsParams].IsInstanceOfType(raw))
                        {
                            arr = raw as Array;
                            rargs[_LastIsParams] = arr;
                        }
                    }
                    if (arr == null)
                    {
                        int arrLen = 0;
                        if (largc > _LastIsParams)
                        {
                            arrLen = largc - _LastIsParams;
                        }
                        var etype = _ParamTypes[_LastIsParams].GetElementType();
                        arr = Array.CreateInstance(etype, arrLen);
                        rargs[_LastIsParams] = arr;
                        for (int i = 0; i < arr.Length; ++i)
                        {
                            var index = _LastIsParams + i + 2;
                            arr.SetValue(l.GetLua(index).ConvertTypeRaw(etype), i);
                        }
                    }
                }
                else
                {
                    int len = _ParamTypes.Count;
                    rargs = ObjectPool.GetReturnValueFromPool(len);
                    for (int i = 0; i < len; ++i)
                    {
                        object arg = null;
                        if (i < largc)
                        {
                            var index = i + 2;
                            arg = l.GetLua(index);
                        }
                        rargs[i] = arg.ConvertTypeRaw(_ParamTypes[i]);
                    }
                }
                object result = null;
                tar = l.GetLuaObject(1);
                // ideally, we should not call the overridden method and call exactly the method provided by the MethodInfo.
                // but the MethodInfo will always call the finally overridden method provided by the target object.
                // there is a solution that creates delegate with RuntimeMethodHandle using Activator class.
                // but the delegate itself is to be declared. so this is not the common solution.
                // see http://stackoverflow.com/questions/4357729/use-reflection-to-invoke-an-overridden-base-method
                // the temporary solution is we should declare public non-virtual method in the derived class and call base.XXX in this method and we can call this method.
                result = _Method.Invoke(tar, rargs);
#if !UNITY_ENGINE && !UNITY_5_3_OR_NEWER || NET_4_6 || NET_STANDARD_2_0
                if (_Method.IsSpecialName)
                {
                    l.PushLua(result);
                }
                else
                {
                    LuaLib.LuaTupleUtils.PushValueOrTuple(l, result);
                }
#else
                l.PushLua(result);
#endif
                l.UpdateData(1, tar);
                if (_OutParamIndices != null && rargs != null)
                {
                    for (int ii = 0; ii < _OutParamIndices.Count; ++ii)
                    {
                        var index = _OutParamIndices[ii];
                        if (index >= 0 && index < rargs.Length)
                        {
                            l.PushLua(rargs[index]);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // perhaps we should make a Call and a TryCall. perhaps we should show which lua-state is doing the log.
                l.LogError("Unable To Call: " + _Method.Name + "@" + _Method.DeclaringType.Name + " \n" + e.ToString());
                throw;
            }
        }
    }

    public abstract class BaseOverloadedMethodMeta : BaseMethodMeta
    {
        public abstract BaseUniqueMethodMeta FindAppropriate(Types pt);
    }
    public class GroupMethodMeta : BaseOverloadedMethodMeta
    {
        private BaseUniqueMethodMeta[] _SeqCache;
        private Dictionary<Types, BaseUniqueMethodMeta> _TypedCache;

        public GroupMethodMeta(IList<MethodBase> minfos, Dictionary<Types, BaseUniqueMethodMeta> tcache, bool updateDataAfterCall)
        {
            if (minfos != null)
            {
                BaseUniqueMethodMeta[] callables = new BaseUniqueMethodMeta[minfos.Count];
                for (int i = 0; i < minfos.Count; ++i)
                {
                    callables[i] = BaseUniqueMethodMeta.CreateMethodMeta(minfos[i], updateDataAfterCall);
                }
                Array.Sort(callables, (ca, cb) =>
                {
                    return Types.Compare(ca._ParamTypes, cb._ParamTypes);
                }); // weight ↑
                _SeqCache = callables;
            }
            else
            {
                _SeqCache = new BaseUniqueMethodMeta[0];
            }
            if (tcache == null)
            {
                tcache = new Dictionary<Types, BaseUniqueMethodMeta>();
            }
            _TypedCache = tcache;
        }

        public override BaseUniqueMethodMeta FindAppropriate(Types pt)
        {
            BaseUniqueMethodMeta ucore = null;
            if (_TypedCache.TryGetValue(pt, out ucore))
            {
                if (ucore == null)
                {
                    return null;
                }
                else
                {
                    return ucore;
                }
            }
            int foundw = int.MaxValue;
            for (int i = 0; i < _SeqCache.Length; ++i)
            {
                var meta = _SeqCache[i];
                var cancall = meta.CanCallByArgsOfType(pt);
                if (cancall == 0)
                {
                    ucore = meta;
                    foundw = 0;
                    break;
                }
                if (cancall > 0 && cancall <= foundw)
                {
                    ucore = meta;
                    foundw = cancall;
                }
            }
            if (ucore != null)
            {
                _TypedCache[pt] = ucore;
                return ucore;
            }
            else
            {
                _TypedCache[pt] = null;
                return null;
            }
        }

        public override void call(IntPtr l, object tar)
        {
            var types = GetArgTypes(l);
            var meta = FindAppropriate(types);
            if (meta == null)
            {
                l.LogError("Cann't find method with appropriate params.");
                throw new ArgumentException();
            }
            else
            {
                meta.call(l, tar);
            }
        }

        public override int CanCallByArgsOfType(Types pt)
        {
            var meta = FindAppropriate(pt);
            if (meta != null)
            {
                return 0;
            }
            else
            {
                return -1;
            }
        }
    }
    public class PackedMethodMeta : BaseOverloadedMethodMeta
    {
        internal Dictionary<int, BaseMethodMeta> _Groups = new Dictionary<int, BaseMethodMeta>();
        internal int _MaxNullableCode = 0;
        internal BaseMethodMeta _NullableMethodMeta = null;

        public PackedMethodMeta(IList<MethodBase> minfos, Dictionary<Types, BaseUniqueMethodMeta> tcache, bool updateDataAfterCall)
        {
            if (minfos != null)
            {
                Dictionary<int, List<MethodBase>> pmethods = new Dictionary<int, List<MethodBase>>();
                int maxCode = 0;
                List<MethodBase> nmethods = null;
                for (int i = 0; i < minfos.Count; ++i)
                {
                    var minfo = minfos[i];
                    var pars = minfo.GetParameters();
                    bool hasNullable = false;
                    Types types = new Types();
                    if (minfo is ConstructorInfo)
                    {
                        types.Add(typeof(Type));
                    }
                    else if (!minfo.IsStatic)
                    {
                        types.Add(minfo.DeclaringType);
                    }
                    if (pars != null)
                    {
                        for (int j = 0; j < pars.Length; ++j)
                        {
                            var ptype = pars[j].ParameterType;
                            if (ptype.IsByRef)
                            {
                                ptype = ptype.GetElementType();
                            }
                            types.Add(ptype);
                            if (!hasNullable && Nullable.GetUnderlyingType(ptype) != null)
                            {
                                hasNullable = true;
                            }
                        }
                    }
                    var code = LuaHub.GetParamsCode(types);
                    List<MethodBase> arr = null;
                    if (!pmethods.TryGetValue(code, out arr))
                    {
                        arr = new List<MethodBase>();
                        pmethods[code] = arr;
                    }
                    arr.Add(minfo);
                    if (hasNullable)
                    {
                        if (code > maxCode)
                        {
                            maxCode = code;
                        }
                        if (nmethods == null)
                        {
                            nmethods = new List<MethodBase>();
                        }
                        nmethods.Add(minfo);
                    }
                }
                foreach (var kvp in pmethods)
                {
                    if (kvp.Value.Count > 1)
                    {
                        _Groups[kvp.Key] = new GroupMethodMeta(kvp.Value, tcache, updateDataAfterCall);
                    }
                    else
                    {
                        _Groups[kvp.Key] = BaseUniqueMethodMeta.CreateMethodMeta(kvp.Value[0], updateDataAfterCall);
                    }
                }
                if (nmethods != null)
                {
                    _MaxNullableCode = maxCode;
                    if (nmethods.Count > 1)
                    {
                        _NullableMethodMeta = new GroupMethodMeta(nmethods, tcache, updateDataAfterCall);
                    }
                    else
                    {
                        _NullableMethodMeta = BaseUniqueMethodMeta.CreateMethodMeta(nmethods[0], updateDataAfterCall);
                    }
                }
            }
        }
        public PackedMethodMeta(IList<MethodBase> minfos, bool updateDataAfterCall)
            : this(minfos, null, updateDataAfterCall)
        {
        }
        protected PackedMethodMeta()
        {
        }

        public override BaseUniqueMethodMeta FindAppropriate(Types pt)
        {
            var code = LuaHub.GetParamsCode(pt);
            BaseMethodMeta rcore = null;
            _Groups.TryGetValue(code, out rcore);
            if (rcore != null)
            {
                if (rcore is BaseUniqueMethodMeta)
                {
                    return (BaseUniqueMethodMeta)rcore;
                }
                else
                {
                    var meta = ((GroupMethodMeta)rcore).FindAppropriate(pt);
                    if (meta != null)
                    {
                        return meta;
                    }
                }
            }
            if (_NullableMethodMeta != null && code < _MaxNullableCode && _NullableMethodMeta.CanCallByArgsOfType(pt) >= 0)
            {
                if (_NullableMethodMeta is BaseUniqueMethodMeta)
                {
                    return (BaseUniqueMethodMeta)_NullableMethodMeta;
                }
                else if (_NullableMethodMeta is BaseOverloadedMethodMeta)
                {
                    return ((BaseOverloadedMethodMeta)_NullableMethodMeta).FindAppropriate(pt);
                }
            }
            return null;
        }

        public override void call(IntPtr l, object tar)
        {
            var types = GetArgTypes(l);
            var code = LuaHub.GetParamsCode(types);
            BaseMethodMeta rcore = null;
            _Groups.TryGetValue(code, out rcore);
            if (rcore != null)
            {
                if (rcore is BaseUniqueMethodMeta)
                {
                    rcore.call(l, tar);
                    return;
                }
                else
                {
                    var meta = ((GroupMethodMeta)rcore).FindAppropriate(types);
                    if (meta != null)
                    {
                        meta.call(l, tar);
                        return;
                    }
                }
            }
            if (_NullableMethodMeta != null && code < _MaxNullableCode && _NullableMethodMeta.CanCallByArgsOfType(types) >= 0)
            {
                _NullableMethodMeta.call(l, tar);
                return;
            }
            else
            {
                l.LogError("Cann't find method with appropriate params.");
                throw new ArgumentException("Cann't find method with appropriate params.");
            }
        }

        public override int CanCallByArgsOfType(Types pt)
        {
            var code = LuaHub.GetParamsCode(pt);
            BaseMethodMeta rcore = null;
            _Groups.TryGetValue(code, out rcore);
            if (rcore != null)
            {
                if (rcore is BaseUniqueMethodMeta)
                {
                    return 0;
                }
                else
                {
                    var meta = ((GroupMethodMeta)rcore).FindAppropriate(pt);
                    if (meta != null)
                    {
                        return 1;
                    }
                }
            }
            if (_NullableMethodMeta != null)
            {
                if (code < _MaxNullableCode)
                {
                    return _NullableMethodMeta.CanCallByArgsOfType(pt);
                }
            }
            return -1;
        }

        public static BaseMethodMeta CreateMethodMeta(IList<MethodBase> minfos, Dictionary<Types, BaseUniqueMethodMeta> tcache, bool updateDataAfterCall)
        {
            if (minfos != null && minfos.Count > 0)
            {
                var packed = new PackedMethodMeta(minfos, tcache, updateDataAfterCall);
                if (packed._Groups.Count > 1 || packed._NullableMethodMeta != null)
                {
                    return packed;
                }
                else
                {
                    foreach (var kvp in packed._Groups)
                    {
                        return kvp.Value;
                    }
                }
            }
            return null;
        }
    }

    public class GenericMethodMeta : BaseMethodMeta
    {
        protected BaseMethodMeta _NormalMethod;
        protected Dictionary<int, IList<MethodInfo>> _GenericMethods = new Dictionary<int, IList<MethodInfo>>();
        public readonly Dictionary<Types, lua.CFunction> _GenericMethodsCache = new Dictionary<Types, lua.CFunction>();
        protected bool _UpdateDataAfterCall;

        public override int CanCallByArgsOfType(Types pt)
        {
            if (_NormalMethod != null)
            {
                return _NormalMethod.CanCallByArgsOfType(pt);
            }
            return -1;
        }

        public override void call(IntPtr l, object tar)
        {
            if (_NormalMethod != null)
            {
                _NormalMethod.call(l, tar);
            }
            else
            {
                // should we determine the generic-parameters by calling-args? Perhaps not... for performance.
                l.LogError("Cannot call. This is pure generic func.");
            }
        }

        public GenericMethodMeta(IList<MethodBase> minfos, IList<MethodBase> gminfos, bool updateDataAfterCall)
        {
            _NormalMethod = PackedMethodMeta.CreateMethodMeta(minfos, null, updateDataAfterCall);
            _UpdateDataAfterCall = updateDataAfterCall;

            if (gminfos != null && gminfos.Count > 0)
            {
                Dictionary<int, List<MethodInfo>> dict = new Dictionary<int, List<MethodInfo>>();
                for (int i = 0; i < gminfos.Count; ++i)
                {
                    var gminfo = gminfos[i] as MethodInfo;
                    if (gminfo != null)
                    {
                        var gtypes = gminfo.GetGenericArguments();
                        if (gtypes != null && gtypes.Length > 0)
                        {
                            var gcnt = gtypes.Length;
                            List<MethodInfo> lst;
                            if (!dict.TryGetValue(gcnt, out lst))
                            {
                                lst = new List<MethodInfo>();
                                dict[gcnt] = lst;
                            }
                            lst.Add(gminfo);
                        }
                    }
                }

                foreach (var kvp in dict)
                {
                    _GenericMethods[kvp.Key] = kvp.Value;
                }
            }
        }

        public override void WrapFunctionByTable(IntPtr l)
        {
            base.WrapFunctionByTable(l); // ftab
            l.getmetatable(-1); // ftab meta
            l.PushString(LuaConst.LS_META_KEY_INDEX); // ftab meta __index
            l.pushlightuserdata(r); // ftab meta __index this
            l.pushcclosure(LuaFuncGenericIndex, 1); // ftab meta __index func
            l.rawset(-3); // ftab meta
            l.PushString(LuaConst.LS_META_KEY_NINDEX); // ftab meta __newindex
            l.pushlightuserdata(r); // ftab meta __newindex this
            l.pushcclosure(LuaFuncGenericNewIndex, 1); // ftab meta __newindex func
            l.rawset(-3); // ftab meta
            l.pop(1); // ftab
        }

        public static BaseMethodMeta CreateMethodMeta(IList<MethodBase> minfos, IList<MethodBase> gminfos, bool updateDataAfterCall)
        {
            if (gminfos != null && gminfos.Count > 0)
            {
                return new GenericMethodMeta(minfos, gminfos, updateDataAfterCall);
            }
            else
            {
                return PackedMethodMeta.CreateMethodMeta(minfos, null, updateDataAfterCall);
            }
        }

        private static readonly lua.CFunction LuaFuncGenericIndex = new lua.CFunction(LuaMetaGenericIndex);
        private static readonly lua.CFunction LuaFuncGenericNewIndex = new lua.CFunction(LuaMetaGenericNewIndex);

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        private static int LuaMetaGenericIndex(IntPtr l)
        {
            if (l.istable(2))
            {
                GenericMethodMeta meta = l.GetLuaLightObject(lua.upvalueindex(1)) as GenericMethodMeta;
                if (meta != null && meta._GenericMethods.Count > 0)
                {
                    l.pushnumber(1); // 1
                    l.gettable(2); // type[1]
                    var type1 = l.GetLuaObjectType(-1);
                    l.pop(1); // X
                    Types gtypes = new Types();
                    if (type1 == typeof(Type))
                    {
                        int index = 1;
                        while (true)
                        {
                            l.pushnumber(index);
                            l.gettable(2);
                            Type t = l.GetLuaObject(-1) as Type;
                            l.pop(1);
                            if (t == null)
                            {
                                break;
                            }
                            gtypes.Add(t);
                            ++index;
                        }
                    }
                    else
                    {
                        if (l.GetLuaObjectType(2) == typeof(Type))
                        {
                            Type t = l.GetLuaObject(2) as Type;
                            gtypes.Add(t);
                        }
                    }

                    if (gtypes.Count > 0)
                    {
                        if (meta._GenericMethods.ContainsKey(gtypes.Count))
                        {
                            l.checkstack(4);
                            l.pushlightuserdata(LuaConst.LRKEY_GENERIC_CACHE); // #gcache
                            l.rawget(1); // gcache
                            if (!l.istable(-1))
                            {
                                l.pop(1); // X
                                l.newtable(); // gcache
                                l.pushlightuserdata(LuaConst.LRKEY_GENERIC_CACHE); // gcache #gcache
                                l.pushvalue(-2); // gcache #gcache gcache
                                l.rawset(1); // gcache
                            }
                            for (int i = 0; i < gtypes.Count; ++i)
                            {
                                l.PushLuaType(gtypes[i]); // pretab type
                                l.pushvalue(-1); // pretab type type
                                l.rawget(-3); // pretab type nxttab
                                if (l.istable(-1))
                                {
                                    l.insert(-3); // nxttab pretab type
                                    l.pop(2); // nxttab
                                }
                                else
                                {
                                    l.pop(1); // pretab type
                                    l.newtable(); // pretab type nxttab
                                    l.pushvalue(-1); // pretab type nxttab nxttab
                                    l.insert(-4); // nxttab pretab type nxttab
                                    l.rawset(-3); // nxttab pretab
                                    l.pop(1); // nxttab
                                }
                            }
                            l.pushlightuserdata(LuaConst.LRKEY_GENERIC_CACHE); // gcache #gcache
                            l.rawget(-2); // gcache func
                            if (l.isnoneornil(-1))
                            {
                                l.pop(1); // gcache

                                lua.CFunction precompiled = null;
                                if (meta._GenericMethodsCache.TryGetValue(gtypes, out precompiled) && precompiled != null)
                                {
                                    l.pushcfunction(precompiled); // gcache func
                                    var lazymeta = new LazyParameterizedMethodMeta(meta, gtypes);
                                    lazymeta.WrapFunctionByTable(l);
                                }
                                else
                                {
                                    var methods = meta._GenericMethods[gtypes.Count];
                                    var tarray = ObjectPool.GetParamTypesFromPool(gtypes.Count);
                                    for (int i = 0; i < tarray.Length; ++i)
                                    {
                                        tarray[i] = gtypes[i];
                                    }
                                    List<MethodBase> gmethods = new List<MethodBase>(methods.Count);
                                    for (int i = 0; i < methods.Count; ++i)
                                    {
                                        var method = methods[i];
                                        try
                                        {
                                            var gmethod = method.MakeGenericMethod(tarray);
                                            gmethods.Add(gmethod);
                                        }
                                        catch (Exception e)
                                        {
                                            l.LogError(e);
                                        }
                                    }

                                    if (gmethods.Count <= 0)
                                    {
                                        l.pushcfunction(LuaHub.LuaFuncOnError); // gcache func
                                    }
                                    else
                                    {
                                        var gmeta = PackedMethodMeta.CreateMethodMeta(gmethods, null, meta._UpdateDataAfterCall);
                                        l.PushFunction(gmeta); // gcache func
                                                               // should we cache it back to _GenericMethodsCache? need lock... we can cache it in c# code, instead of in runtime.
                                        gmeta.WrapFunctionByTable(l);
                                    }
                                }
                                l.pushlightuserdata(LuaConst.LRKEY_GENERIC_CACHE); // gcache func #gcache
                                l.pushvalue(-2); // gcache func #gcache func
                                l.rawset(-4); // gcache func
                                l.remove(-2); // func
                                return 1;
                            }
                            else
                            {
                                l.remove(-2); // func
                                return 1;
                            }
                        }
                        l.pushnil();
                        return 1;
                    }
                }
            }

            return 0;
        }
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        private static int LuaMetaGenericNewIndex(IntPtr l)
        {
            if (l.istable(2) && (l.isnil(3) || l.isfunction(3)))
            {
                GenericMethodMeta meta = l.GetLuaLightObject(lua.upvalueindex(1)) as GenericMethodMeta;
                if (meta != null && meta._GenericMethods.Count > 0)
                {
                    l.pushnumber(1); // 1
                    l.gettable(2); // type[1]
                    var type1 = l.GetLuaObjectType(-1);
                    l.pop(1); // X
                    Types gtypes = new Types();
                    if (type1 == typeof(Type))
                    {
                        int index = 1;
                        while (true)
                        {
                            l.pushnumber(index);
                            l.gettable(2);
                            Type t = l.GetLuaObject(-1) as Type;
                            l.pop(1);
                            if (t == null)
                            {
                                break;
                            }
                            gtypes.Add(t);
                            ++index;
                        }
                    }
                    else
                    {
                        if (l.GetLuaObjectType(2) == typeof(Type))
                        {
                            Type t = l.GetLuaObject(2) as Type;
                            gtypes.Add(t);
                        }
                    }

                    if (gtypes.Count > 0)
                    {
                        if (meta._GenericMethods.ContainsKey(gtypes.Count))
                        {
                            l.checkstack(4);
                            l.pushlightuserdata(LuaConst.LRKEY_GENERIC_CACHE); // #gcache
                            l.rawget(1); // gcache
                            if (!l.istable(-1))
                            {
                                l.pop(1); // X
                                l.newtable(); // gcache
                                l.pushlightuserdata(LuaConst.LRKEY_GENERIC_CACHE); // gcache #gcache
                                l.pushvalue(-2); // gcache #gcache gcache
                                l.rawset(1); // gcache
                            }
                            for (int i = 0; i < gtypes.Count; ++i)
                            {
                                l.PushLuaType(gtypes[i]); // pretab type
                                l.pushvalue(-1); // pretab type type
                                l.rawget(-3); // pretab type nxttab
                                if (l.istable(-1))
                                {
                                    l.insert(-3); // nxttab pretab type
                                    l.pop(2); // nxttab
                                }
                                else
                                {
                                    l.pop(1); // pretab type
                                    l.newtable(); // pretab type nxttab
                                    l.pushvalue(-1); // pretab type nxttab nxttab
                                    l.insert(-4); // nxttab pretab type nxttab
                                    l.rawset(-3); // nxttab pretab
                                    l.pop(1); // nxttab
                                }
                            }
                            BaseMethodMeta gmeta = null;
                            l.pushlightuserdata(LuaConst.LRKEY_GENERIC_CACHE); // gcache #gcache
                            l.rawget(-2); // gcache oldfunc
                            if (l.istable(-1))
                            {
                                l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS); // gcache oldfunc #trans
                                l.rawget(-2); // gcache oldfunc trans
                                if (l.islightuserdata(-1))
                                {
                                    var trans = l.GetLuaLightObject(-1);
                                    gmeta = trans as BaseMethodMeta;
                                }
                                l.pop(2); // gcache
                            }
                            else
                            {
                                l.pop(1); // gcache
                            }
                            if (gmeta == null)
                            {
                                gmeta = new LazyParameterizedMethodMeta(meta, gtypes);
                            }

                            l.pushlightuserdata(LuaConst.LRKEY_GENERIC_CACHE); // gcache #gcache
                            l.pushvalue(3); // gcache #gcache func
                            gmeta.WrapFunctionByTable(l);
                            l.rawset(-3); // gcache
                            l.pop(1); // X
                            return 0;
                        }
                        return 0;
                    }
                }
            }
            return 0;
        }

        private class LazyParameterizedMethodMeta : BaseOverloadedMethodMeta
        {
            public GenericMethodMeta DefinitionMethodMeta;
            public Types ParameterizedTypes;
            private BaseMethodMeta ParameterizedMethodMeta;
            private bool Parameterized;

            public LazyParameterizedMethodMeta(GenericMethodMeta dmeta, Types types)
            {
                DefinitionMethodMeta = dmeta;
                ParameterizedTypes = types;
            }

            public void DoParameterize()
            {
                if (!Parameterized)
                {
                    Parameterized = true;

                    var dmeta = DefinitionMethodMeta;
                    var gtypes = ParameterizedTypes;
                    var methods = dmeta._GenericMethods[gtypes.Count];
                    var tarray = ObjectPool.GetParamTypesFromPool(gtypes.Count);
                    for (int i = 0; i < tarray.Length; ++i)
                    {
                        tarray[i] = gtypes[i];
                    }
                    List<MethodBase> gmethods = new List<MethodBase>(methods.Count);
                    for (int i = 0; i < methods.Count; ++i)
                    {
                        var method = methods[i];
                        try
                        {
                            var gmethod = method.MakeGenericMethod(tarray);
                            gmethods.Add(gmethod);
                        }
                        catch (Exception e)
                        {
                            PlatDependant.LogError(e);
                        }
                    }

                    if (gmethods.Count > 0)
                    {
                        var gmeta = PackedMethodMeta.CreateMethodMeta(gmethods, null, dmeta._UpdateDataAfterCall);
                        ParameterizedMethodMeta = gmeta;
                    }
                }
            }

            public override void call(IntPtr l, object tar)
            {
                DoParameterize();
                if (ParameterizedMethodMeta == null)
                {
                    l.LogError("Cannot parameterize generic method.");
                }
                else
                {
                    ParameterizedMethodMeta.call(l, tar);
                }
            }

            public override int CanCallByArgsOfType(Types pt)
            {
                DoParameterize();
                if (ParameterizedMethodMeta == null)
                {
                    return -1;
                }
                else
                {
                    return ParameterizedMethodMeta.CanCallByArgsOfType(pt);
                }
            }

            public override BaseUniqueMethodMeta FindAppropriate(Types pt)
            {
                DoParameterize();
                if (ParameterizedMethodMeta == null)
                {
                    return null;
                }
                else
                {
                    BaseUniqueMethodMeta uniquemeta = ParameterizedMethodMeta as BaseUniqueMethodMeta;
                    if (uniquemeta != null)
                    {
                        return uniquemeta;
                    }
                    else
                    {
                        BaseOverloadedMethodMeta overloaded = ParameterizedMethodMeta as BaseOverloadedMethodMeta;
                        if (overloaded != null)
                        {
                            return overloaded.FindAppropriate(pt);
                        }
                    }
                }
                return null;
            }
        }
    }

    public class BinaryOpMeta : SelfHandled, ILuaMetaCall
    {
        private BaseMethodMeta _RawOp;

        public BinaryOpMeta(BaseMethodMeta raw)
        {
            _RawOp = raw;
        }

        public void call(IntPtr l, object tar)
        {
            var types = BaseMethodMeta.GetArgTypes(l);
            if (!_RawOp.CanCall(types))
            {
                l.pushnil();
                l.pushboolean(true);
                return;
            }
            _RawOp.call(l, tar);
        }
    }
}
