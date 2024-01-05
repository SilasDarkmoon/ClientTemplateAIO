using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngineEx;

using lua = LuaLib.LuaCoreLib;
using lual = LuaLib.LuaAuxLib;
using luae = LuaLib.LuaLibEx;

namespace LuaLib
{
    public static class PropertyMetaHelper
    {
        public static ILuaMetaCall CreatePropertyGetter(PropertyInfo pi)
        {
            bool isStatic = false;
            MethodBase getter = pi.GetGetMethod();
            if (getter != null)
            {
                isStatic = getter.IsStatic;
            }
            if (isStatic)
            {
                return new StaticPropertyGetter() { _pi = pi, _Getter = getter };
            }
            else
            {
                return new InstancePropertyGetter() { _pi = pi, _Getter = getter };
            }
        }
        public static ILuaMetaCall CreatePropertySetter(PropertyInfo pi, bool updateDataAfterCall)
        {
            bool isStatic = false;
            MethodBase setter = pi.GetSetMethod();
            if (setter != null)
            {
                isStatic = setter.IsStatic;
            }
            if (isStatic)
            {
                return new StaticPropertySetter() { _pi = pi, _Setter = setter };
            }
            else
            {
                if (updateDataAfterCall)
                {
                    return new ValueTypePropertySetter() { _pi = pi, _Setter = setter };
                }
                else
                {
                    return new InstancePropertySetter() { _pi = pi, _Setter = setter };
                }
            }
        }
        public static ILuaMetaCall CreateFieldGetter(FieldInfo fi)
        {
            bool isStatic = fi.IsStatic;
            if (isStatic)
            {
                return new StaticFieldGetter(fi);
            }
            else
            {
                return new InstanceFieldGetter(fi);
            }
        }
        public static ILuaMetaCall CreateFieldSetter(FieldInfo fi, bool updateDataAfterCall)
        {
            bool isStatic = fi.IsStatic;
            if (isStatic)
            {
                return new StaticFieldSetter(fi);
            }
            else
            {
                if (updateDataAfterCall)
                {
                    return new ValueTypeFieldSetter(fi);
                }
                else
                {
                    return new InstanceFieldSetter(fi);
                }
            }
        }

        public struct ArrayIndexerPair
        {
            public ILuaMetaCall Getter;
            public ILuaMetaCall Setter;
        }
        public static readonly Dictionary<Type, ArrayIndexerPair> ArrayIndexers = new Dictionary<Type, ArrayIndexerPair>();
        static PropertyMetaHelper()
        {
            ILuaMetaCall getter, setter;
            getter = new ArrayGetter<bool>();
            setter = new ArraySetter<bool>();
            ArrayIndexers[typeof(bool[])] = new ArrayIndexerPair() { Getter = getter, Setter = setter };
            ArrayIndexers[typeof(List<bool>)] = new ArrayIndexerPair() { Getter = getter, Setter = setter };

            getter = new ArrayGetter<byte>();
            setter = new ArraySetter<byte>();
            ArrayIndexers[typeof(byte[])] = new ArrayIndexerPair() { Getter = getter, Setter = setter };
            ArrayIndexers[typeof(List<byte>)] = new ArrayIndexerPair() { Getter = getter, Setter = setter };

            getter = new ArrayGetter<char>();
            setter = new ArraySetter<char>();
            ArrayIndexers[typeof(char[])] = new ArrayIndexerPair() { Getter = getter, Setter = setter };
            ArrayIndexers[typeof(List<char>)] = new ArrayIndexerPair() { Getter = getter, Setter = setter };

            getter = new ArrayGetter<decimal>();
            setter = new ArraySetter<decimal>();
            ArrayIndexers[typeof(decimal[])] = new ArrayIndexerPair() { Getter = getter, Setter = setter };
            ArrayIndexers[typeof(List<decimal>)] = new ArrayIndexerPair() { Getter = getter, Setter = setter };

            getter = new ArrayGetter<double>();
            setter = new ArraySetter<double>();
            ArrayIndexers[typeof(double[])] = new ArrayIndexerPair() { Getter = getter, Setter = setter };
            ArrayIndexers[typeof(List<double>)] = new ArrayIndexerPair() { Getter = getter, Setter = setter };

            getter = new ArrayGetter<float>();
            setter = new ArraySetter<float>();
            ArrayIndexers[typeof(float[])] = new ArrayIndexerPair() { Getter = getter, Setter = setter };
            ArrayIndexers[typeof(List<float>)] = new ArrayIndexerPair() { Getter = getter, Setter = setter };

            getter = new ArrayGetter<int>();
            setter = new ArraySetter<int>();
            ArrayIndexers[typeof(int[])] = new ArrayIndexerPair() { Getter = getter, Setter = setter };
            ArrayIndexers[typeof(List<int>)] = new ArrayIndexerPair() { Getter = getter, Setter = setter };

            getter = new ArrayGetter<IntPtr>();
            setter = new ArraySetter<IntPtr>();
            ArrayIndexers[typeof(IntPtr[])] = new ArrayIndexerPair() { Getter = getter, Setter = setter };
            ArrayIndexers[typeof(List<IntPtr>)] = new ArrayIndexerPair() { Getter = getter, Setter = setter };

            getter = new ArrayGetter<long>();
            setter = new ArraySetter<long>();
            ArrayIndexers[typeof(long[])] = new ArrayIndexerPair() { Getter = getter, Setter = setter };
            ArrayIndexers[typeof(List<long>)] = new ArrayIndexerPair() { Getter = getter, Setter = setter };

            getter = new ArrayGetter<sbyte>();
            setter = new ArraySetter<sbyte>();
            ArrayIndexers[typeof(sbyte[])] = new ArrayIndexerPair() { Getter = getter, Setter = setter };
            ArrayIndexers[typeof(List<sbyte>)] = new ArrayIndexerPair() { Getter = getter, Setter = setter };

            getter = new ArrayGetter<short>();
            setter = new ArraySetter<short>();
            ArrayIndexers[typeof(short[])] = new ArrayIndexerPair() { Getter = getter, Setter = setter };
            ArrayIndexers[typeof(List<short>)] = new ArrayIndexerPair() { Getter = getter, Setter = setter };

            getter = new ArrayGetter<uint>();
            setter = new ArraySetter<uint>();
            ArrayIndexers[typeof(uint[])] = new ArrayIndexerPair() { Getter = getter, Setter = setter };
            ArrayIndexers[typeof(List<uint>)] = new ArrayIndexerPair() { Getter = getter, Setter = setter };

            getter = new ArrayGetter<ulong>();
            setter = new ArraySetter<ulong>();
            ArrayIndexers[typeof(ulong[])] = new ArrayIndexerPair() { Getter = getter, Setter = setter };
            ArrayIndexers[typeof(List<ulong>)] = new ArrayIndexerPair() { Getter = getter, Setter = setter };

            getter = new ArrayGetter<ushort>();
            setter = new ArraySetter<ushort>();
            ArrayIndexers[typeof(ushort[])] = new ArrayIndexerPair() { Getter = getter, Setter = setter };
            ArrayIndexers[typeof(List<ushort>)] = new ArrayIndexerPair() { Getter = getter, Setter = setter };

#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
            getter = new ArrayGetter<UnityEngine.Vector3>();
            setter = new ArraySetter<UnityEngine.Vector3>();
            ArrayIndexers[typeof(UnityEngine.Vector3[])] = new ArrayIndexerPair() { Getter = getter, Setter = setter };
            ArrayIndexers[typeof(List<UnityEngine.Vector3>)] = new ArrayIndexerPair() { Getter = getter, Setter = setter };

            getter = new ArrayGetter<UnityEngine.Bounds>();
            setter = new ArraySetter<UnityEngine.Bounds>();
            ArrayIndexers[typeof(UnityEngine.Bounds[])] = new ArrayIndexerPair() { Getter = getter, Setter = setter };
            ArrayIndexers[typeof(List<UnityEngine.Bounds>)] = new ArrayIndexerPair() { Getter = getter, Setter = setter };

            getter = new ArrayGetter<UnityEngine.LayerMask>();
            setter = new ArraySetter<UnityEngine.LayerMask>();
            ArrayIndexers[typeof(UnityEngine.LayerMask[])] = new ArrayIndexerPair() { Getter = getter, Setter = setter };
            ArrayIndexers[typeof(List<UnityEngine.LayerMask>)] = new ArrayIndexerPair() { Getter = getter, Setter = setter };

            getter = new ArrayGetter<UnityEngine.Plane>();
            setter = new ArraySetter<UnityEngine.Plane>();
            ArrayIndexers[typeof(UnityEngine.Plane[])] = new ArrayIndexerPair() { Getter = getter, Setter = setter };
            ArrayIndexers[typeof(List<UnityEngine.Plane>)] = new ArrayIndexerPair() { Getter = getter, Setter = setter };

            getter = new ArrayGetter<UnityEngine.Quaternion>();
            setter = new ArraySetter<UnityEngine.Quaternion>();
            ArrayIndexers[typeof(UnityEngine.Quaternion[])] = new ArrayIndexerPair() { Getter = getter, Setter = setter };
            ArrayIndexers[typeof(List<UnityEngine.Quaternion>)] = new ArrayIndexerPair() { Getter = getter, Setter = setter };

            getter = new ArrayGetter<UnityEngine.Color>();
            setter = new ArraySetter<UnityEngine.Color>();
            ArrayIndexers[typeof(UnityEngine.Color[])] = new ArrayIndexerPair() { Getter = getter, Setter = setter };
            ArrayIndexers[typeof(List<UnityEngine.Color>)] = new ArrayIndexerPair() { Getter = getter, Setter = setter };

            getter = new ArrayGetter<UnityEngine.Ray>();
            setter = new ArraySetter<UnityEngine.Ray>();
            ArrayIndexers[typeof(UnityEngine.Ray[])] = new ArrayIndexerPair() { Getter = getter, Setter = setter };
            ArrayIndexers[typeof(List<UnityEngine.Ray>)] = new ArrayIndexerPair() { Getter = getter, Setter = setter };

            getter = new ArrayGetter<UnityEngine.Rect>();
            setter = new ArraySetter<UnityEngine.Rect>();
            ArrayIndexers[typeof(UnityEngine.Rect[])] = new ArrayIndexerPair() { Getter = getter, Setter = setter };
            ArrayIndexers[typeof(List<UnityEngine.Rect>)] = new ArrayIndexerPair() { Getter = getter, Setter = setter };

            getter = new ArrayGetter<UnityEngine.Vector2>();
            setter = new ArraySetter<UnityEngine.Vector2>();
            ArrayIndexers[typeof(UnityEngine.Vector2[])] = new ArrayIndexerPair() { Getter = getter, Setter = setter };
            ArrayIndexers[typeof(List<UnityEngine.Vector2>)] = new ArrayIndexerPair() { Getter = getter, Setter = setter };

            getter = new ArrayGetter<UnityEngine.Vector4>();
            setter = new ArraySetter<UnityEngine.Vector4>();
            ArrayIndexers[typeof(UnityEngine.Vector4[])] = new ArrayIndexerPair() { Getter = getter, Setter = setter };
            ArrayIndexers[typeof(List<UnityEngine.Vector4>)] = new ArrayIndexerPair() { Getter = getter, Setter = setter };
#endif
        }
    }

    public class StaticFieldGetter : SelfHandled, ILuaMetaCall
    {
        private FieldInfo _fi;
        public StaticFieldGetter(FieldInfo fi)
        {
            _fi = fi;
        }

#region ILuaMetaCall
        public void call(IntPtr l, object tar)
        {
#if UNITY_EDITOR
            if (_fi != null)
            {
                BaseMethodMeta.TrigOnReflectInvokeMember(_fi.ReflectedType, _fi.Name);
            }
#endif
            object rv = null;
            try
            {
                rv = _fi.GetValue(null);
            }
            catch(Exception e)
            {
                l.LogError(e);
            }
            l.PushLua(rv);
        }
#endregion
    }
    public class StaticFieldSetter : SelfHandled, ILuaMetaCall
    {
        private FieldInfo _fi;
        public StaticFieldSetter(FieldInfo fi)
        {
            _fi = fi;
        }

#region ILuaMetaCall
        public void call(IntPtr l, object tar)
        {
#if UNITY_EDITOR
            if (_fi != null)
            {
                BaseMethodMeta.TrigOnReflectInvokeMember(_fi.ReflectedType, _fi.Name);
            }
#endif
            object val = l.GetLua(1);
            try
            {
                val = val.ConvertTypeRaw(_fi.FieldType);
                _fi.SetValue(null, val);
            }
            catch(Exception e)
            {
                l.LogError(e);
            }
        }
#endregion
    }
    public class StaticPropertyGetter : SelfHandled, ILuaMetaCall
    {
        internal PropertyInfo _pi;
        internal MethodBase _Getter;

        internal StaticPropertyGetter() { }
        public StaticPropertyGetter(PropertyInfo pi)
        {
            _pi = pi;
            MethodBase getter = pi.GetGetMethod();
            if (getter != null)
            {
                _Getter = getter;
            }
        }

#region ILuaMetaCall
        public void call(IntPtr l, object tar)
        {
#if UNITY_EDITOR
            if (_pi != null)
            {
                BaseMethodMeta.TrigOnReflectInvokeMember(_pi.ReflectedType, _pi.Name);
            }
#endif
            if (_Getter == null)
            {
                l.LogError("This property does not have a getter.");
                return;
            }
            object rv = null;
            try
            {
                rv = _Getter.Invoke(null, ObjectPool.GetReturnValueFromPool(0));
            }
            catch (Exception e)
            {
                l.LogError(e);
            }
            l.PushLua(rv);
        }
#endregion
    }
    public class StaticPropertySetter : SelfHandled, ILuaMetaCall
    {
        internal PropertyInfo _pi;
        internal MethodBase _Setter;

        internal StaticPropertySetter() { }
        public StaticPropertySetter(PropertyInfo pi)
        {
            _pi = pi;
            MethodBase setter = pi.GetSetMethod();
            if (setter != null)
            {
                _Setter = setter;
            }
        }

#region ILuaMetaCall
        public void call(IntPtr l, object tar)
        {
#if UNITY_EDITOR
            if (_pi != null)
            {
                BaseMethodMeta.TrigOnReflectInvokeMember(_pi.ReflectedType, _pi.Name);
            }
#endif
            if (_Setter == null)
            {
                l.LogError("This property does not have a setter.");
                return;
            }
            object val = l.GetLua(1);
            try
            {
                val = val.ConvertTypeRaw(_pi.PropertyType);
                var args = ObjectPool.GetReturnValueFromPool(1);
                args[0] = val;
                _Setter.Invoke(null, args);
            }
            catch (Exception e)
            {
                l.LogError(e);
            }
        }
#endregion
    }

    public class InstanceFieldGetter : SelfHandled, ILuaMetaCall
    {
        private FieldInfo _fi;
        public InstanceFieldGetter(FieldInfo fi)
        {
            _fi = fi;
        }

#region ILuaMetaCall
        public void call(IntPtr l, object tar)
        {
#if UNITY_EDITOR
            if (_fi != null)
            {
                BaseMethodMeta.TrigOnReflectInvokeMember(_fi.ReflectedType, _fi.Name);
            }
#endif
            tar = l.GetLuaObject(1);
            object rv = null;
            try
            {
                rv = _fi.GetValue(tar);
            }
            catch (Exception e)
            {
                l.LogError(e);
            }
            l.PushLua(rv);
        }
#endregion
    }
    public class InstanceFieldSetter : SelfHandled, ILuaMetaCall
    {
        private FieldInfo _fi;
        public InstanceFieldSetter(FieldInfo fi)
        {
            _fi = fi;
        }

#region ILuaMetaCall
        public void call(IntPtr l, object tar)
        {
#if UNITY_EDITOR
            if (_fi != null)
            {
                BaseMethodMeta.TrigOnReflectInvokeMember(_fi.ReflectedType, _fi.Name);
            }
#endif
            tar = l.GetLuaObject(1);
            var val = l.GetLua(2);
            try
            {
                val = val.ConvertTypeRaw(_fi.FieldType);
                _fi.SetValue(tar, val);
            }
            catch (Exception e)
            {
                l.LogError(e);
            }
        }
#endregion
    }
    public class InstancePropertyGetter : SelfHandled, ILuaMetaCall
    {
        internal PropertyInfo _pi;
        internal MethodBase _Getter;

        internal InstancePropertyGetter() { }
        public InstancePropertyGetter(PropertyInfo pi)
        {
            _pi = pi;
            MethodBase getter = pi.GetGetMethod();
            if (getter != null)
            {
                _Getter = getter;
            }
        }

#region ILuaMetaCall
        public void call(IntPtr l, object tar)
        {
#if UNITY_EDITOR
            if (_pi != null)
            {
                BaseMethodMeta.TrigOnReflectInvokeMember(_pi.ReflectedType, _pi.Name);
            }
#endif
            if (_Getter == null)
            {
                l.LogError("This property does not have a getter.");
                return;
            }
            tar = l.GetLuaObject(1);
            object rv = null;
            try
            {
                rv = _Getter.Invoke(tar, ObjectPool.GetReturnValueFromPool(0));
            }
            catch (Exception e)
            {
                l.LogError(e);
            }
            l.PushLua(rv);
        }
#endregion
    }
    public class InstancePropertySetter : SelfHandled, ILuaMetaCall
    {
        internal PropertyInfo _pi;
        internal MethodBase _Setter;

        internal InstancePropertySetter() { }
        public InstancePropertySetter(PropertyInfo pi)
        {
            _pi = pi;
            MethodBase setter = pi.GetSetMethod();
            if (setter != null)
            {
                _Setter = setter;
            }
        }

#region ILuaMetaCall
        public void call(IntPtr l, object tar)
        {
#if UNITY_EDITOR
            if (_pi != null)
            {
                BaseMethodMeta.TrigOnReflectInvokeMember(_pi.ReflectedType, _pi.Name);
            }
#endif
            if (_Setter == null)
            {
                l.LogError("This property does not have a setter.");
                return;
            }
            tar = l.GetLuaObject(1);
            var val = l.GetLua(2);
            try
            {
                val = val.ConvertTypeRaw(_pi.PropertyType);
                var args = ObjectPool.GetReturnValueFromPool(1);
                args[0] = val;
                _Setter.Invoke(tar, args);
            }
            catch (Exception e)
            {
                l.LogError(e);
            }
        }
#endregion
    }

    public class ValueTypeFieldSetter : SelfHandled, ILuaMetaCall
    {
        private FieldInfo _fi;
        public ValueTypeFieldSetter(FieldInfo fi)
        {
            _fi = fi;
        }

#region ILuaMetaCall
        public void call(IntPtr l, object tar)
        {
#if UNITY_EDITOR
            if (_fi != null)
            {
                BaseMethodMeta.TrigOnReflectInvokeMember(_fi.ReflectedType, _fi.Name);
            }
#endif
            tar = l.GetLuaObject(1);
            var val = l.GetLua(2);
            try
            {
                val = val.ConvertTypeRaw(_fi.FieldType);
                _fi.SetValue(tar, val);
            }
            catch (Exception e)
            {
                l.LogError(e);
            }
            l.UpdateData(1, tar);
        }
#endregion
    }
    public class ValueTypePropertySetter : SelfHandled, ILuaMetaCall
    {
        internal PropertyInfo _pi;
        internal MethodBase _Setter;

        internal ValueTypePropertySetter() { }
        public ValueTypePropertySetter(PropertyInfo pi)
        {
            _pi = pi;
            MethodBase setter = pi.GetSetMethod();
            if (setter != null)
            {
                _Setter = setter;
            }
        }

#region ILuaMetaCall
        public void call(IntPtr l, object tar)
        {
#if UNITY_EDITOR
            if (_pi != null)
            {
                BaseMethodMeta.TrigOnReflectInvokeMember(_pi.ReflectedType, _pi.Name);
            }
#endif
            if (_Setter == null)
            {
                l.LogError("This property does not have a setter.");
                return;
            }
            tar = l.GetLuaObject(1);
            var val = l.GetLua(2);
            try
            {
                val = val.ConvertTypeRaw(_pi.PropertyType);
                var args = ObjectPool.GetReturnValueFromPool(1);
                args[0] = val;
                _Setter.Invoke(tar, args);
            }
            catch (Exception e)
            {
                l.LogError(e);
            }
            l.UpdateData(1, tar);
        }
#endregion
    }

    public class IndexGetter : SelfHandled, ILuaMetaCall
    {
        private BaseMethodMeta _RawIndexGetter;

        public IndexGetter(BaseMethodMeta raw)
        {
            _RawIndexGetter = raw;
        }

        public void call(IntPtr l, object tar)
        {
            var types = BaseMethodMeta.GetArgTypes(l);
            if (!_RawIndexGetter.CanCall(types))
            {
                return;
            }
            _RawIndexGetter.call(l, tar);
        }
    }
    public class IndexSetter : SelfHandled, ILuaMetaCall
    {
        private BaseMethodMeta _RawIndexSetter;

        public IndexSetter(BaseMethodMeta raw)
        {
            _RawIndexSetter = raw;
        }

        public void call(IntPtr l, object tar)
        {
            var types = BaseMethodMeta.GetArgTypes(l);
            if (!_RawIndexSetter.CanCall(types))
            {
                l.pushboolean(true); // bool failed;
                return;
            }
            _RawIndexSetter.call(l, tar);
        }
}

    public class ListGetter : SelfHandled, ILuaMetaCall
    {
        public void call(IntPtr l, object tar)
        {
            if (l.IsNumber(2))
            {
                var list = l.GetLuaObject(1) as System.Collections.IList;
                if (list != null)
                {
                    var index = (int)l.tonumber(2);
                    if (index >= 0 && index < list.Count)
                    {
                        try
                        {
                            var rv = list[index];
                            l.PushLua(rv);
                        }
                        catch { }
                    }
                }
            }
        }
    }
    public class ListSetter : SelfHandled, ILuaMetaCall
    {
        public void call(IntPtr l, object tar)
        {
            if (l.IsNumber(2))
            {
                var list = l.GetLuaObject(1) as System.Collections.IList;
                if (list != null)
                {
                    var index = (int)l.tonumber(2);
                    if (index >= 0 && index < list.Count)
                    {
                        var val = l.GetLua(3);
                        if (list.GetType().HasElementType)
                        {
                            val = val.ConvertTypeRaw(list.GetType().GetElementType());
                        }
                        try
                        {
                            list[index] = val;
                            return;
                        }
                        catch
                        {
                        }
                    }
                }
            }
            l.pushboolean(true); // bool failed;
        }
    }

    public class ArrayGetter<T> : SelfHandled, ILuaMetaCall
    {
        public void call(IntPtr l, object tar)
        {
            if (l.IsNumber(2))
            {
                var list = l.GetLuaObject(1) as System.Collections.Generic.IList<T>;
                if (list != null)
                {
                    var index = (int)l.tonumber(2);
                    if (index >= 0 && index < list.Count)
                    {
                        try
                        {
                            var rv = list[index];
                            l.PushLua(rv);
                        }
                        catch { }
                    }
                }
            }
        }
    }
    public class ArraySetter<T> : SelfHandled, ILuaMetaCall
    {
        public void call(IntPtr l, object tar)
        {
            if (l.IsNumber(2))
            {
                var list = l.GetLuaObject(1) as System.Collections.Generic.IList<T>;
                if (list != null)
                {
                    var index = (int)l.tonumber(2);
                    if (index >= 0 && index < list.Count)
                    {
                        T val;
                        l.GetLua(3, out val);
                        try
                        {
                            list[index] = val;
                            return;
                        }
                        catch { }
                    }
                }
            }
            l.pushboolean(true); // bool failed;
        }
    }
}