using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngineEx;

namespace LuaLib
{
    public class BaseDynamic // TODO: IDynamic and DynamicStruct
    {
        protected internal static BaseDynamic _Empty = new BaseDynamic();
        public static BaseDynamic Empty
        {
            get { return _Empty; }
        }

        internal protected object _Binding = null;
        public virtual object Binding
        {
            get
            {
                return _Binding;
            }
            internal set
            {
                _Binding = value;
            }
        }

        public object this[object key]
        {
            get
            {
                return GetFieldImp(key);
            }
            set
            {
                SetFieldImp(key, value);
            }
        }

        internal protected virtual object GetFieldImp(object key)
        {
            throw new NotImplementedException("__index meta-method Not Implemented.");
        }

        internal protected virtual bool SetFieldImp(object key, object val)
        {
            throw new NotImplementedException("__newindex meta-method Not Implemented.");
        }

        public virtual object[] Call(params object[] args)
        {
            throw new NotImplementedException("__call meta-method Not Implemented.");
        }

        public virtual object UnaryOp(string op)
        {
            switch (op)
            {
                case "+":
                    return this;
                default:
                    DynamicHelper.LogInfo("__unary-op(" + op + ") meta-method Not Implemented.");
                    return null;
            }
        }

        public virtual object BinaryOp(string op, object other)
        {
            switch (op)
            {
                case "==":
                    return Equals(other);
                default:
                    DynamicHelper.LogInfo("__binary-op(" + op + ") meta-method Not Implemented.");
                    return null;
            }
        }

        internal protected virtual object ConvertBinding(Type type)
        {
            if (type == typeof(bool))
            {
                return _Binding.ToBoolean();
            }
            if (type != null && type.IsInstanceOfType(_Binding))
            {
                return _Binding;
            }
            //DynamicHelper.LogInfo((_Binding == null ? "null" : _Binding.ToString() + "(" + _Binding.GetType().ToString() + ") ") + "__convert(" + type.ToString() + ") meta-method Not Implemented.");
            return null;
        }
        public object Convert(Type type)
        {
            if (type == null)
                return null;
            
            var rv = this.ConvertTypeRaw(type);
            if (rv != null)
                return rv;
            rv = ConvertBinding(type);
            if (rv != null)
                return rv;
            return _Binding.ConvertTypeRaw(type);
        }

        public override string ToString()
        {
            if (_Binding == null)
                return "null";
            else if (object.ReferenceEquals(_Binding, this))
                return base.ToString();
            else
                return _Binding.ToString();
        }
        public override int GetHashCode()
        {
            if (_Binding == null)
                return 0;
            else
            {
                if (object.ReferenceEquals(_Binding, this))
                {
                    return base.GetHashCode();
                }
                else
                {
                    return _Binding.GetHashCode();
                }
            }
        }
        public override bool Equals(object obj)
        {
            var raw = this;//.UnwrapDynamic();
            var raw2 = obj;//.UnwrapDynamic();
            if (raw is BaseDynamic || raw2 is BaseDynamic)
            {
                return object.ReferenceEquals(raw, raw2);
            }
            if (object.ReferenceEquals(raw, null) || object.ReferenceEquals(raw2, null))
            {
                if (!object.ReferenceEquals(raw, null))
                {
                    return raw.Equals(null);
                }
                else if (!object.ReferenceEquals(raw2, null))
                {
                    return raw2.Equals(null);
                }
                else
                {
                    return object.ReferenceEquals(raw, raw2);
                }
            }
            return raw.Equals(raw2);
        }

        public static bool EitherEquals(object o1, object o2)
        {
            if (o1 == null)
            {
                if (o2 == null)
                {
                    return true;
                }
                else
                {
                    return o2.Equals(o1);
                }
            }
            else
            {
                if (o2 == null)
                {
                    return o1.Equals(o2);
                }
                else
                {
                    return o1.Equals(o2) || o2.Equals(o1);
                }
            }
        }
        public static bool operator ==(BaseDynamic source, BaseDynamic other)
        {
            return EitherEquals(source, other);
        }
        public static bool operator !=(BaseDynamic source, BaseDynamic other)
        {
            return !EitherEquals(source, other);
        }

        public static implicit operator bool(BaseDynamic obj)
        {
            return !object.ReferenceEquals(obj, null) && obj.ToBoolean();
        }

        public virtual void ReturnToPool()
        {
            ObjectPool.ReturnToPool(this);
        }
    }

    public abstract class ScriptDynamic : BaseDynamic
    {
        public virtual int Refid
        {
            get
            {
                if (_Binding == null)
                    return 0;
                if (_Binding is int)
                    return (int)_Binding;
                return 0;
            }
            protected internal set
            {
                _Binding = value;
            }
        }

        public override string ToString()
        {
            return "ScriptRef:" + Refid.ToString();
        }
        public override bool Equals(object obj)
        {
            if (obj is ScriptDynamic)
            {
                return Refid == ((ScriptDynamic)obj).Refid;
            }
            return false;
        }
        public override int GetHashCode()
        {
            return Refid;
        }
    }

    public abstract class NoBindingDynamic : BaseDynamic
    {
        protected NoBindingDynamic()
        {
            _Binding = this;
        }
    }

    public interface IFieldsProvider : IDictionary<object, object>, IDictionary, IDisposable
    {
    }

    public class BaseFieldsProvider : IFieldsProvider
    {
        internal protected virtual IEnumerator<KeyValuePair<object, object>> GetEnumeratorImp()
        {
            DynamicHelper.LogInfo("no enum on fields provider");
            yield break;
        }
        internal protected virtual int GetCountImp()
        {
            var etor = GetEnumerator();
            if (etor != null)
            {
                int count = 0;
                while(etor.MoveNext())
                {
                    ++count;
                }
                return count;
            }
            return 0;
        }
        internal protected virtual void CopyToImp(Array array, int index)
        {
            int curi = 0;
            if (array is KeyValuePair<object, object>[])
            {
                foreach (var val in this)
                {
                    if (curi >= index && curi < array.Length)
                    {
                        array.SetValue(val, curi);
                    }
                    ++curi;
                }
            }
            else
            {
                foreach (var val in this)
                {
                    if (curi >= index && curi < array.Length)
                    {
                        array.SetValue(new DictionaryEntry(val.Key, val.Value), curi);
                    }
                    ++curi;
                }
            }
        }
        internal protected virtual bool IsSynchronizedImp()
        {
            return false;
        }
        internal protected virtual object GetSyncRootImp()
        {
            return this;
        }
        internal protected virtual object SetValueImp(object key, object val)
        {
            DynamicHelper.LogInfo("no setter on fields provider");
            return null;
        }
        internal protected virtual object GetValueImp(object key)
        {
            DynamicHelper.LogInfo("no getter on fields provider");
            return null;
        }
        internal protected virtual void ClearImp()
        {
            LinkedList<object> keys = new LinkedList<object>();
            foreach (var kvp in this)
            {
                keys.AddLast(kvp.Key);
            }
            foreach (var key in keys)
            {
                SetValueImp(key, null);
            }
        }
        internal protected virtual bool IsReadOnlyImp()
        {
            return false;
        }
        internal protected virtual object[] GetKeysImp()
        {
            List<object> keys = new List<object>();
            foreach (var kvp in this)
            {
                keys.Add(kvp.Key);
            }
            return keys.ToArray();
        }
        internal protected virtual object[] GetValuesImp()
        {
            List<object> values = new List<object>();
            foreach (var kvp in this)
            {
                values.Add(kvp.Value);
            }
            return values.ToArray();
        }

        public virtual void CopyTo(Array array, int index)
        {
            CopyToImp(array, index);
        }

        public int Count
        {
            get { return GetCountImp(); }
        }

        public bool IsSynchronized
        {
            get { return IsSynchronizedImp(); }
        }

        public object SyncRoot
        {
            get { return GetSyncRootImp(); }
        }

        public void Add(KeyValuePair<object, object> item)
        {
            SetValueImp(item.Key, item.Value);
        }

        public void Clear()
        {
            ClearImp();
        }

        public bool Contains(KeyValuePair<object, object> item)
        {
            var val = GetValueImp(item.Key);
            if (val == item.Value)
                return true;
            return false;
        }

        public virtual void CopyTo(KeyValuePair<object, object>[] array, int arrayIndex)
        {
            CopyToImp(array, arrayIndex);
        }

        public bool IsReadOnly
        {
            get { return IsReadOnlyImp(); }
        }

        public bool Remove(KeyValuePair<object, object> item)
        {
            return Remove(item.Key);
        }

        public void Add(object key, object value)
        {
            SetValueImp(key, value);
        }

        public bool ContainsKey(object key)
        {
            var val = GetValueImp(key);
            return val != null;
        }

        public ICollection<object> Keys
        {
            get { return GetKeysImp(); }
        }

        public bool Remove(object key)
        {
            var val = SetValueImp(key, null);
            return val != null;
        }

        public bool TryGetValue(object key, out object value)
        {
            var val = GetValueImp(key);
            value = val as BaseDynamic;
            return val != null;
        }

        public ICollection<object> Values
        {
            get { return GetValuesImp(); }
        }

        public object this[object key]
        {
            get
            {
                return GetValueImp(key);
            }
            set
            {
                SetValueImp(key, value);
            }
        }

        public bool Contains(object key)
        {
            return ContainsKey(key);
        }

        public class DictionaryEnumerator : IDictionaryEnumerator, IEnumerator, IEnumerator<KeyValuePair<object, object>>
        {
            public IEnumerator<KeyValuePair<object, object>> _RawEnumerator = null;
            public DictionaryEntry Entry
            {
                get
                {
                    var val = _RawEnumerator.Current;
                    return new DictionaryEntry(val.Key, val.Value);
                }
            }

            public object Key
            {
                get { return _RawEnumerator.Current.Key; }
            }

            public object Value
            {
                get { return _RawEnumerator.Current.Value; }
            }

            object IEnumerator.Current
            {
                get
                {
                    return Entry;
                    //return _RawEnumerator.Current;
                }
            }

            public bool MoveNext()
            {
                return _RawEnumerator.MoveNext();
            }

            public void Reset()
            {
                _RawEnumerator.Reset();
            }

            public KeyValuePair<object, object> Current
            {
                get { return _RawEnumerator.Current; }
            }

            public void Dispose()
            {
                _RawEnumerator.Dispose();
            }
        }

        public bool IsFixedSize
        {
            get { return IsReadOnlyImp(); }
        }

        ICollection IDictionary.Keys
        {
            get { return GetKeysImp(); }
        }

        void IDictionary.Remove(object key)
        {
            Remove(key);
        }

        ICollection IDictionary.Values
        {
            get { return GetValuesImp(); }
        }

        object IDictionary.this[object key]
        {
            get
            {
                return this[key];
            }
            set
            {
                this[key] = value as BaseDynamic;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator() as DictionaryEnumerator;
        }

        public IEnumerator<KeyValuePair<object, object>> GetEnumerator()
        {
            return new DictionaryEnumerator() { _RawEnumerator = GetEnumeratorImp() };
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return GetEnumerator() as DictionaryEnumerator;
        }

        public virtual void Dispose()
        { }
    }

    public class DictionaryFieldsProvider : BaseFieldsProvider
    {
        protected IDictionary _Dict;
        public DictionaryFieldsProvider(IDictionary dict)
        {
            _Dict = dict;
        }

        protected internal override IEnumerator<KeyValuePair<object, object>> GetEnumeratorImp()
        {
            foreach (DictionaryEntry entry in _Dict)
            {
                yield return new KeyValuePair<object, object>(entry.Key, entry.Value);
            }
        }
        protected internal override int GetCountImp()
        {
            return _Dict.Count;
        }
        protected internal override bool IsSynchronizedImp()
        {
            return _Dict.IsSynchronized;
        }
        protected internal override object GetSyncRootImp()
        {
            return _Dict.SyncRoot;
        }
        protected internal override object GetValueImp(object key)
        {
            if (_Dict.Contains(key))
            {
                return _Dict[key];
            }
            else
            {
                return null;
            }
        }
        protected internal override object SetValueImp(object key, object val)
        {
            var old = GetValueImp(key);
            if (val == null)
            {
                _Dict.Remove(key);
            }
            else
            {
                _Dict[key] = val;
            }
            return old;
        }
        protected internal override void ClearImp()
        {
            _Dict.Clear();
        }
        protected internal override bool IsReadOnlyImp()
        {
            return _Dict.IsReadOnly;
        }
    }

    public interface IExpando
    {
        BaseDynamic Core { get; }
        IFieldsProvider Extra { get; }
    }

    public static class DynamicHelper
    {
        //public static List<KeyValuePair<Predicate<object>, Func<object, BaseDynamic>>> DynamicWrapperFuncs = new List<KeyValuePair<Predicate<object>, Func<object, BaseDynamic>>>()
        //{
        //    new KeyValuePair<Predicate<object>, Func<object, BaseDynamic>>(obj =>(obj is Type), obj => ClrTypeWrapper.GetFromPool(obj as Type)),
        //    new KeyValuePair<Predicate<object>, Func<object, BaseDynamic>>(obj =>(obj is Delegate), obj => ((Delegate)obj).WrapDelegate()),
        //    new KeyValuePair<Predicate<object>, Func<object, BaseDynamic>>(obj => true, obj => ClrObjectWrapper.GetFromPool(obj, null)),
        //};
        //public static BaseDynamic WrapDynamic(this object binding)
        //{
        //    if (binding == null)
        //        return null;
        //    if (binding is BaseDynamic)
        //        return binding as BaseDynamic;
        //    foreach (var kvp in DynamicWrapperFuncs)
        //    {
        //        if (kvp.Key(binding))
        //        {
        //            return kvp.Value(binding);
        //        }
        //    }
        //    return null;
        //}

        //public static object UnwrapDynamic(this object dobj)
        //{
        //    if (object.ReferenceEquals(dobj, null))
        //        return null;
        //    if (dobj is IExpando)
        //        return UnwrapDynamic(((IExpando)dobj).Core);
        //    if (dobj is ScriptDynamic)
        //        return dobj;
        //    if (dobj is BaseDynamic)
        //        return ((BaseDynamic)dobj).Binding;
        //    return dobj;
        //}

        //public static T UnwrapDynamic<T>(this object dobj)
        //{
        //    var rv = UnwrapDynamic(dobj);
        //    return rv is T ? (T)rv : default(T);
        //}

        public static object UnwrapReturnValues(this object[] vals)
        {
            if (vals == null || vals.Length < 1)
                return null;
            return vals[0];//.UnwrapDynamic();
        }
        public static T UnwrapReturnValues<T>(this object[] vals)
        {
            return UnwrapReturnValues(vals).ConvertType<T>();
        }

        public static T GetWeakReference<T>(this System.WeakReference wr)
        {
            if (wr != null)
            {
                try
                {
                    if (wr.IsAlive)
                    {
                        var obj = wr.Target;
                        if (obj is T)
                        {
                            return (T)obj;
                        }
                    }
                }
                catch { }
            }
            return default(T);
        }

        //internal static Dictionary<ParamTypes, System.Reflection.MethodInfo> _ConvertMethods = new Dictionary<ParamTypes, System.Reflection.MethodInfo>();
        // I decide not to use cache for converters. Instead, use the method below when you want to convert obj by implicit or explicit operators.
        public static object ConvertTypeEx(this object obj, Type type)
        {
            if (type == null)
                return null;
            if (obj == null)
                return null;
            var rv = ConvertType(obj, type);
            if (rv != null)
                return rv;

            System.Reflection.MethodInfo mi = null;
            LinkedList<KeyValuePair<Type, object>> types = new LinkedList<KeyValuePair<Type, object>>();
            types.AddLast(new KeyValuePair<Type, object>(type, obj));
            types.AddLast(new KeyValuePair<Type, object>(obj.GetType(), obj));
            while (obj is IExpando)
            {
                obj = ((IExpando)obj).Core;
                if (obj != null)
                {
                    types.AddLast(new KeyValuePair<Type, object>(obj.GetType(), obj));
                }
            }
            if (obj is BaseDynamic)
            {
                obj = ((BaseDynamic)obj).Binding;
                if (obj != null)
                {
                    types.AddLast(new KeyValuePair<Type, object>(obj.GetType(), obj));
                }
            }

            foreach(var kvp in types)
            {
                mi = null;
                try
                {
                    mi = kvp.Key.GetMethods().First(mic =>
                    {
                        if (mic.IsStatic && mic.ReturnType == type && mic.Name == "op_Implicit")
                        {
                            var pars = mic.GetParameters();
                            if (pars != null && pars.Length == 1 && pars[0].ParameterType == kvp.Value.GetType())
                            {
                                return true;
                            }
                        }
                        return false;
                    });
                }
                catch { }
                if (mi != null)
                {
                    try
                    {
                        var args = ObjectPool.GetParamsFromPool(1);
                        args[0] = kvp.Value;
                        var rv2 = mi.Invoke(null, args);
                        ObjectPool.ReturnParamsToPool(args);
                        return rv2;
                    }
                    catch { }
                    return null;
                }
                mi = null;
                try
                {
                    mi = kvp.Key.GetMethods().First(mic =>
                    {
                        if (mic.IsStatic && mic.ReturnType == type && mic.Name == "op_Explicit")
                        {
                            var pars = mic.GetParameters();
                            if (pars != null && pars.Length == 1 && pars[0].ParameterType == kvp.Value.GetType())
                            {
                                return true;
                            }
                        }
                        return false;
                    });
                }
                catch { }
                if (mi != null)
                {
                    try
                    {
                        var args = ObjectPool.GetParamsFromPool(1);
                        args[0] = kvp.Value;
                        var rv2 = mi.Invoke(null, args);
                        ObjectPool.ReturnParamsToPool(args);
                        return rv2;
                    }
                    catch { }
                    return null;
                }
            }
            return null;
        }

//        public static object ConvertTypeRaw(this object obj, Type type)
//        {
//            if (type == null)
//                return null;
//            if (obj == null)
//                return null;
//            if (obj is LuaLib.BaseLua && type.IsSubclassOf(typeof(Delegate)))
//            {
//                return LuaLib.LuaDelegateGenerator.CreateDelegate(type, obj as LuaLib.BaseLua);
//            }
//            if (type.IsAssignableFrom(obj.GetType()))
//                return obj;
//            bool oconv = obj.IsObjIConvertible();
//            bool tconv = type.IsTypeIConvertible();
//            if (oconv && tconv)
//            {
//                if (type.IsEnum())
//                {
//                    if (obj is string)
//                    {
//                        return Enum.Parse(type, obj as string);
//                    }
//                    else
//                    {
//                        return Enum.ToObject(type, (object)Convert.ToUInt64(obj));
//                    }
//                }
//                else if (obj is Enum)
//                {
//                    if (type == typeof(string))
//                    {
//                        return obj.ToString();
//                    }
//                    else
//                    {
//                        return Convert.ChangeType(Convert.ToUInt64(obj), type);
//                    }
//                }
//                try
//                {
//                    return Convert.ChangeType(obj, type);
//                }
//                catch { }
//            }
//#if NETFX_CORE
//            if (oconv && type == typeof(IntPtr))
//            {
//                try
//                {
//                    long l = Convert.ToInt64(obj);
//                    IntPtr p = (IntPtr)l;
//                    return p;
//                }
//                catch
//                {
//                    return null;
//                }
//            }
//            else if (obj.GetType() == typeof(IntPtr) && tconv)
//            {
//                IntPtr p = (IntPtr)obj;
//                long l = (long)p;
//                try
//                {
//                    return Convert.ChangeType(l, type);
//                }
//                catch
//                {
//                    return null;
//                }
//            }
//#endif
//            return null;
//        }

//        public static bool CanConvertRaw(this Type curtype, Type totype)
//        {
//            if (totype == null)
//            {
//                if (curtype == null)
//                {
//                    return true;
//                }
//                else
//                {
//                    return false;
//                }
//            }
//            if (curtype == null)
//            {
//                if (totype.IsValueType())
//                {
//                    return false;
//                }
//                else
//                {
//                    return true;
//                }
//            }
//            if (totype.IsAssignableFrom(curtype))
//                return true;
//            if (curtype.IsTypeIConvertible() && totype.IsTypeIConvertible())
//            {
//                return true;
//            }
//            return false;
//        }

        //public static bool CanConvertRawObj(this object obj, Type totype)
        //{
        //    Type vtype = null;
        //    if (obj != null)
        //    {
        //        vtype = obj.GetType();
        //    }
        //    return CanConvertRaw(vtype, totype);
        //}

        public static object ConvertType(this object obj, Type type)
        {
            if (obj is IExpando)
            {
                return ConvertType(((IExpando)obj).Core, type);
            }
            if (obj is BaseDynamic)
            {
                return ((BaseDynamic)obj).Convert(type);
            }
            if (type == typeof(BaseDynamic))
            {
                return obj;
            }
            return obj.ConvertTypeRaw(type);
        }

        public static T ConvertType<T>(this object obj)
        {
            var val = ConvertType(obj, typeof(T));
            return val == null ? default(T) : (T)val;
        }

        public static bool ToBoolean(this object obj)
        {
            object val = ConvertType(obj, typeof(bool));
            if (val == null)
            {
                if (obj is IExpando)
                {
                    return ((IExpando)obj).Core != null && ((IExpando)obj).Core._Binding != null;
                }
                if (obj is BaseDynamic)
                {
                    return ((BaseDynamic)obj)._Binding != null;
                }
                else
                {
                    return obj != null;
                }
            }
            return (bool)val;
        }

        public static void LogInfo(object obj)
        {
            PlatDependant.LogInfo(obj);
        }

        public static void LogError(object obj)
        {
            PlatDependant.LogError(obj);
        }

        public static void LogWarning(object obj)
        {
            PlatDependant.LogWarning(obj);
        }

        public static int GetPrimitiveTypeSize(this Type type)
        {
            switch (type.GetTypeCode())
            {
                case TypeCode.Boolean:
                    return sizeof(bool);
                case TypeCode.Byte:
                    return sizeof(byte);
                case TypeCode.Char:
                    return sizeof(char);
                //case TypeCode.DateTime:
                //    return sizeof(DateTime);
                case TypeCode.Decimal:
                    return sizeof(decimal);
                case TypeCode.Double:
                    return sizeof(double);
                case TypeCode.Int16:
                    return sizeof(short);
                case TypeCode.Int32:
                    return sizeof(int);
                case TypeCode.Int64:
                    return sizeof(long);
                case TypeCode.SByte:
                    return sizeof(sbyte);
                case TypeCode.Single:
                    return sizeof(float);
                case TypeCode.UInt16:
                    return sizeof(ushort);
                case TypeCode.UInt32:
                    return sizeof(uint);
                case TypeCode.UInt64:
                    return sizeof(ulong);
            }
            return 0;
        }

        public static int GetPrimitiveTypeWeight(this Type type)
        {
            if (type == null)
            {
                return 0;
            }
            if (type.IsEnum())
            {
                return 11;
            }
            switch (type.GetTypeCode())
            {
                case TypeCode.Boolean:
                    return 0;
                case TypeCode.Byte:
                    return 1;
                case TypeCode.Char:
                    return 1;
                case TypeCode.DateTime:
                    return 9;
                case TypeCode.Decimal:
                    return 16;
                case TypeCode.Double:
                    return 10;
                case TypeCode.Int16:
                    return 2;
                case TypeCode.Int32:
                    return 4;
                case TypeCode.Int64:
                    return 8;
                case TypeCode.SByte:
                    return 1;
                case TypeCode.Single:
                    return 5;
                case TypeCode.UInt16:
                    return 2;
                case TypeCode.UInt32:
                    return 4;
                case TypeCode.UInt64:
                    return 8;
            }
            return 17;
        }
    }

    public static class ExpandoHelper
    {
        public static object GetField(this IExpando exp, object key) // caller to this func must restore the lua-stack's top after calling this func.
        {
            if (exp != null && key != null)
            {
                using (var ex = exp.Extra)
                {
                    if (ex != null)
                    {
                        object obj;
                        if (ex.TryGetValue(key, out obj))
                        {
                            return obj;
                        }
                    }
                    if (exp.Core != null)
                    {
                        return exp.Core[key];
                    }
                }
            }
            return null;
        }
        public static bool SetField(this IExpando exp, object key, BaseDynamic val) // caller to this func must restore the lua-stack's top after calling this func.
        {
            if (exp != null && key != null)
            {
                using (var ex = exp.Extra)
                {
                    if (ex != null)
                    {
                        if (ex.ContainsKey(key))
                        {
                            ex[key] = val;
                            return true;
                        }
                    }
                    if (exp.Core != null)
                    {
                        if (exp.Core.SetFieldImp(key, val))
                        {
                            return true;
                        }
                    }
                    if (ex != null)
                    {
                        ex[key] = val;
                        return true;
                    }
                }
            }
            return false;
        }
    }

    public static class ObjectPool
    {
        [ThreadStatic] internal static Dictionary<Type, IInstancePool> _Pool;
        public static object TryGetFromPool(this Type type)
        {
            if (type != null)
            {
                if (_Pool == null)
                {
                    _Pool = new Dictionary<Type, IInstancePool>();
                }
                IInstancePool pool;
                if (!_Pool.TryGetValue(type, out pool))
                {
                    pool = new CommonInstancePool();
                    _Pool[type] = pool;
                }
                var list = pool.Pool;
                if (list.Count > 0)
                {
                    var rv = list[list.Count - 1];
                    list.RemoveAt(list.Count - 1);
                    return rv;
                }
            }
            return null;
        }
        public static object GetFromPool(this Type type, Func<object> funcCreate, Action<object> funcInit)
        {
            if (type != null)
            {
                if (_Pool == null)
                {
                    _Pool = new Dictionary<Type, IInstancePool>();
                }
                IInstancePool pool;
                if (!_Pool.TryGetValue(type, out pool))
                {
                    pool = new CommonInstancePool();
                    _Pool[type] = pool;
                }
                var list = pool.Pool;
                if (list.Count > 0)
                {
                    var rv = list[list.Count - 1];
                    list.RemoveAt(list.Count - 1);
                    if (funcInit != null)
                    {
                        funcInit(rv);
                    }
                    return rv;
                }
                else
                {
                    if (funcCreate != null)
                    {
                        return funcCreate();
                    }
                    else
                    {
                        return Activator.CreateInstance(type);
                    }
                }
            }
            return null;
        }
        public static object GetFromPool(this Type type, Func<object> funcCreate)
        {
            return GetFromPool(type, funcCreate, null);
        }
        public static object GetFromPool(this Type type)
        {
            return GetFromPool(type, null);
        }
        public static T GetFromPool<T>(this Func<T> funcCreate, Action<T> funcInit)
        {
            Func<object> funcCreateReal = null;
            if (funcCreate != null)
            {
                funcCreateReal = () => funcCreate();
            }
            Action<object> funcInitReal = null;
            if (funcInit != null)
            {
                funcInitReal = obj => funcInit((T)obj);
            }
            var rv = GetFromPool(typeof(T), funcCreateReal, funcInitReal);
            if (rv == null)
            {
                return default(T);
            }
            else
            {
                return (T)rv;
            }
        }
        public static T GetFromPool<T>(this Func<T> funcCreate)
        {
            return GetFromPool<T>(funcCreate, null);
        }
        public static T GetFromPool<T>()
        {
            Func<T> funcCreate = null;
            return GetFromPool<T>(funcCreate);
        }
        public static void ReturnToPool(this object obj)
        {
            if (obj != null)
            {
                if (!(obj is ValueType))
                {
                    var type = obj.GetType();
                    if (_Pool == null)
                    {
                        _Pool = new Dictionary<Type, IInstancePool>();
                    }
                    IInstancePool pool;
                    if (!_Pool.TryGetValue(type, out pool))
                    {
                        pool = new CommonInstancePool();
                        _Pool[type] = pool;
                    }
                    pool.Pool.Add(obj);
                }
            }
        }

        [ThreadStatic] internal static Dictionary<int, object[]> _ReturnValuePool;
        [ThreadStatic] internal static object[][] _ReturnValuePoolLow;
        public static object[] GetReturnValueFromPool(int len)
        {
            if (len >= 0)
            {
                object[] val;
                if (len < 5)
                {
                    if (_ReturnValuePoolLow == null)
                    {
                        _ReturnValuePoolLow = new[] { new object[0], new object[1], new object[2], new object[3], new object[4], };
                    }
                    val = _ReturnValuePoolLow[len];
                }
                else
                {
                    if (_ReturnValuePool == null)
                    {
                        _ReturnValuePool = new Dictionary<int, object[]>();
                    }
                    if (!_ReturnValuePool.TryGetValue(len, out val))
                    {
                        val = new object[len];
                        _ReturnValuePool[len] = val;
                    }
                }
                for (int i = 0; i < val.Length; ++i)
                {
                    val[i] = null;
                }
                return val;
            }
            return null;
        }

        [ThreadStatic] internal static Dictionary<int, List<object[]>> _ParamsPool;
        [ThreadStatic] internal static List<object[]>[] _ParamsPoolLow;
        public static object[] GetParamsFromPool(int len)
        {
            if (len >= 0)
            {
                if (len < 5)
                {
                    if (_ParamsPoolLow == null)
                    {
                        _ParamsPoolLow = new[] { new List<object[]>(), new List<object[]>(), new List<object[]>(), new List<object[]>(), new List<object[]>(), };
                    }
                    List<object[]> list = _ParamsPoolLow[len];
                    if (list.Count > 0)
                    {
                        var rv = list[list.Count - 1];
                        list.RemoveAt(list.Count - 1);
                        for (int i = 0; i < rv.Length; ++i)
                        {
                            rv[i] = null;
                        }
                        return rv;
                    }
                    else
                    {
                        return new object[len];
                    }
                }
                else
                {
                    if (_ParamsPool == null)
                    {
                        _ParamsPool = new Dictionary<int, List<object[]>>();
                    }
                    List<object[]> list;
                    if (!_ParamsPool.TryGetValue(len, out list))
                    {
                        list = new List<object[]>();
                        _ParamsPool[len] = list;
                    }
                    if (list.Count > 0)
                    {
                        var rv = list[list.Count - 1];
                        list.RemoveAt(list.Count - 1);
                        for (int i = 0; i < rv.Length; ++i)
                        {
                            rv[i] = null;
                        }
                        return rv;
                    }
                    else
                    {
                        return new object[len];
                    }
                }
            }
            return null;
        }
        public static void ReturnParamsToPool(this object[] args)
        {
            if (args != null)
            {
                int len = args.Length;
                if (len < 5)
                {
                    if (_ParamsPoolLow == null)
                    {
                        _ParamsPoolLow = new[] { new List<object[]>(), new List<object[]>(), new List<object[]>(), new List<object[]>(), new List<object[]>(), };
                    }
                    List<object[]> list = _ParamsPoolLow[len];
                    list.Add(args);
                }
                else
                {
                    if (_ParamsPool == null)
                    {
                        _ParamsPool = new Dictionary<int, List<object[]>>();
                    }
                    List<object[]> list;
                    if (!_ParamsPool.TryGetValue(len, out list))
                    {
                        list = new List<object[]>();
                        _ParamsPool[len] = list;
                    }
                    list.Add(args);
                }
            }
        }

        [ThreadStatic] internal static Dictionary<int, List<Type[]>> _ParamTypesPool;
        [ThreadStatic] internal static List<Type[]>[] _ParamTypesPoolLow;
        public static Type[] GetParamTypesFromPool(int len)
        {
            if (len >= 0)
            {
                if (len < 5)
                {
                    if (_ParamTypesPoolLow == null)
                    {
                        _ParamTypesPoolLow = new[] { new List<Type[]>(), new List<Type[]>(), new List<Type[]>(), new List<Type[]>(), new List<Type[]>(), };
                    }
                    List<Type[]> list = _ParamTypesPoolLow[len];
                    if (list.Count > 0)
                    {
                        var rv = list[list.Count - 1];
                        list.RemoveAt(list.Count - 1);
                        for (int i = 0; i < rv.Length; ++i)
                        {
                            rv[i] = null;
                        }
                        return rv;
                    }
                    else
                    {
                        return new Type[len];
                    }
                }
                else
                {
                    if (_ParamTypesPool == null)
                    {
                        _ParamTypesPool = new Dictionary<int, List<Type[]>>();
                    }
                    List<Type[]> list;
                    if (!_ParamTypesPool.TryGetValue(len, out list))
                    {
                        list = new List<Type[]>();
                        _ParamTypesPool[len] = list;
                    }
                    if (list.Count > 0)
                    {
                        var rv = list[list.Count - 1];
                        list.RemoveAt(list.Count - 1);
                        for (int i = 0; i < rv.Length; ++i)
                        {
                            rv[i] = null;
                        }
                        return rv;
                    }
                    else
                    {
                        return new Type[len];
                    }
                }
            }
            return null;
        }
        public static void ReturnParamTypesToPool(this Type[] args)
        {
            if (args != null)
            {
                int len = args.Length;
                if (len < 5)
                {
                    if (_ParamTypesPoolLow == null)
                    {
                        _ParamTypesPoolLow = new[] { new List<Type[]>(), new List<Type[]>(), new List<Type[]>(), new List<Type[]>(), new List<Type[]>(), };
                    }
                    List<Type[]> list = _ParamTypesPoolLow[len];
                    list.Add(args);
                }
                else
                {
                    if (_ParamTypesPool == null)
                    {
                        _ParamTypesPool = new Dictionary<int, List<Type[]>>();
                    }
                    List<Type[]> list;
                    if (!_ParamTypesPool.TryGetValue(len, out list))
                    {
                        list = new List<Type[]>();
                        _ParamTypesPool[len] = list;
                    }
                    list.Add(args);
                }
            }
        }

        public interface IInstancePool
        {
            IList Pool { get; }
        }
        public interface IInstancePool<T> : IInstancePool where T : class
        {
            new List<T> Pool { get; }
        }
        public static T TryGetFromPool<T>(this IInstancePool<T> pool) where T : class
        {
            if (pool != null)
            {
                var list = pool.Pool;
                if (list.Count > 0)
                {
                    var rv = list[list.Count - 1];
                    list.RemoveAt(list.Count - 1);
                    return rv;
                }
            }
            return default(T);
        }
        public static T GetFromPool<T>(this IInstancePool<T> pool, Func<T> funcCreate, Action<T> funcInit) where T : class
        {
            if (pool != null)
            {
                var list = pool.Pool;
                if (list.Count > 0)
                {
                    var rv = list[list.Count - 1];
                    list.RemoveAt(list.Count - 1);
                    if (funcInit != null)
                    {
                        funcInit(rv);
                    }
                    return rv;
                }
                else
                {
                    if (funcCreate != null)
                    {
                        return funcCreate();
                    }
                    else
                    {
                        return (T)Activator.CreateInstance(typeof(T));
                    }
                }
            }
            return default(T);
        }
        public static T GetFromPool<T>(this IInstancePool<T> pool, Func<T> funcCreate) where T : class
        {
            return GetFromPool<T>(pool, funcCreate, null);
        }
        public static T GetFromPool<T>(this IInstancePool<T> pool) where T : class
        {
            return GetFromPool<T>(pool, null);
        }
        public static void ReturnToPool<T>(this IInstancePool<T> pool, T obj) where T : class
        {
            if (obj != null)
            {
                if (pool != null)
                {
                    var list = pool.Pool;
                    list.Add(obj);
                }
            }
        }

        public class CommonInstancePool : IInstancePool<object>
        {
            protected internal List<object> _Pool = new List<object>();

            public List<object> Pool
            {
                get { return _Pool; }
            }

            IList IInstancePool.Pool
            {
                get { return _Pool; }
            }
        }
        public class GenericInstancePool<T> : IInstancePool<T> where T : class
        {
            protected internal List<T> _List = new List<T>();
            public GenericInstancePool()
            {
                if (_Pool == null)
                {
                    _Pool = new Dictionary<Type, IInstancePool>();
                }
                IInstancePool oldpool;
                if (_Pool.TryGetValue(typeof(T), out oldpool))
                {
                    foreach(var obj in oldpool.Pool)
                    {
                        _List.Add((T)obj);
                    }
                }
                _Pool[typeof(T)] = this;
            }

            public List<T> Pool
            {
                get { return _List; }
            }

            IList IInstancePool.Pool
            {
                get { return _List; }
            }
        }

        public class WeakReferencePoolClass : GenericInstancePool<WeakReference>
        {
            public WeakReference GetFromPool(object target)
            {
                var rv = this.TryGetFromPool();
                if (rv == null)
                {
                    rv = new WeakReference(target);
                }
                else
                {
                    rv.Target = target;
                }
                return rv;
            }
        }
        [ThreadStatic] internal static WeakReferencePoolClass _WeakReferencePool;
        public static WeakReferencePoolClass WeakReferencePool
        {
            get
            {
                if (_WeakReferencePool == null)
                {
                    _WeakReferencePool = new WeakReferencePoolClass();
                }
                return _WeakReferencePool;
            }
        }

        public static void DropAllPool()
        {
            _ReturnValuePool = null;
            _ReturnValuePoolLow = null;
            _ParamsPool = null;
            _ParamsPoolLow = null;
            _ParamTypesPool = null;
            _ParamTypesPoolLow = null;
            if (_Pool != null)
            {
                foreach(var kvpool in _Pool)
                {
                    kvpool.Value.Pool.Clear();
                }
            }
        }
    }
}
