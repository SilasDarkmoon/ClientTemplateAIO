using System;
using System.Collections.Generic;
using System.Reflection;
using LuaLib;
using UnityEngineEx;

using lua = LuaLib.LuaCoreLib;
using lual = LuaLib.LuaAuxLib;
using luae = LuaLib.LuaLibEx;

namespace LuaLib
{
    public static class LuaDelegateGenerator
    {
        public interface IDelegateLuaWrapper
        {
            BaseLua Target { set; get; }
            Delegate MakeDelegate(Type deltype);
            void SetDelegate(Delegate del);
            void CallByLua(IntPtr l);
        }
        public abstract class BaseDelegateLuaWrapper : IDelegateLuaWrapper
        {
            protected BaseLua _Target;

            public BaseLua Target
            {
                get
                {
                    return _Target;
                }
                set
                {
                    _Target = value;
                }
            }
            public bool IsClosed
            {
                get
                {
                    return ReferenceEquals(_Target, null) || _Target.IsClosed;
                }
            }
            public Delegate MakeDelegate(Type deltype)
            {
#if NETFX_CORE
                return this.GetType().GetMethod("Call").CreateDelegate(deltype, this);
#else
                return Delegate.CreateDelegate(deltype, this, "Call");
#endif
            }

            protected Delegate _DelWrapped;
            public abstract Type DelType { get; }
            public void SetDelegate(Delegate del)
            {
                if (del == null)
                {
                    _DelWrapped = null;
                }
                else
                {
#if NETFX_CORE
                    _DelWrapped = del.GetMethodInfo().CreateDelegate(DelType, del.Target);
#else
                    _DelWrapped = del.Method.CreateDelegate(DelType, del.Target);
#endif
                }
            }
            [InheritablePreserve]
            public abstract void CallByLua(IntPtr l);
        }

        [UnityPreserve]
        public class ActionLuaWrapper : BaseDelegateLuaWrapper
        {
            [UnityPreserve]
            public void Call()
            {
                if (IsClosed) return;
                Target.Call();
            }
            public delegate void WrappedDelegate();
            public override Type DelType { get { return typeof(WrappedDelegate); } }
            public override void CallByLua(IntPtr l)
            {
                if (_DelWrapped == null) return;
                ((WrappedDelegate)_DelWrapped)();
            }
        }
        [UnityPreserve]
        public class ActionLuaWrapper<T> : BaseDelegateLuaWrapper
        {
            [UnityPreserve]
            public void Call(T arg)
            {
                if (IsClosed) return;
                Target.Call(arg);
            }
            public delegate void WrappedDelegate(T p0);
            public override Type DelType { get { return typeof(WrappedDelegate); } }
            public override void CallByLua(IntPtr l)
            {
                if (_DelWrapped == null) return;
                var p0 = l.GetLua<T>(2);
                ((WrappedDelegate)_DelWrapped)(p0);
            }
        }
        [UnityPreserve]
        public class ActionLuaWrapper<T1, T2> : BaseDelegateLuaWrapper
        {
            [UnityPreserve]
            public void Call(T1 arg1, T2 arg2)
            {
                if (IsClosed) return;
                Target.Call(arg1, arg2);
            }
            public delegate void WrappedDelegate(T1 p0, T2 p1);
            public override Type DelType { get { return typeof(WrappedDelegate); } }
            public override void CallByLua(IntPtr l)
            {
                if (_DelWrapped == null) return;
                var p0 = l.GetLua<T1>(2);
                var p1 = l.GetLua<T2>(3);
                ((WrappedDelegate)_DelWrapped)(p0, p1);
            }
        }
        [UnityPreserve]
        public class ActionLuaWrapper<T1, T2, T3> : BaseDelegateLuaWrapper
        {
            [UnityPreserve]
            public void Call(T1 arg1, T2 arg2, T3 arg3)
            {
                if (IsClosed) return;
                Target.Call(arg1, arg2, arg3);
            }
            public delegate void WrappedDelegate(T1 p0, T2 p1, T3 p2);
            public override Type DelType { get { return typeof(WrappedDelegate); } }
            public override void CallByLua(IntPtr l)
            {
                if (_DelWrapped == null) return;
                var p0 = l.GetLua<T1>(2);
                var p1 = l.GetLua<T2>(3);
                var p2 = l.GetLua<T3>(4);
                ((WrappedDelegate)_DelWrapped)(p0, p1, p2);
            }
        }
        [UnityPreserve]
        public class ActionLuaWrapper<T1, T2, T3, T4> : BaseDelegateLuaWrapper
        {
            [UnityPreserve]
            public void Call(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            {
                if (IsClosed) return;
                Target.Call(arg1, arg2, arg3, arg4);
            }
            public delegate void WrappedDelegate(T1 p0, T2 p1, T3 p2, T4 p3);
            public override Type DelType { get { return typeof(WrappedDelegate); } }
            public override void CallByLua(IntPtr l)
            {
                if (_DelWrapped == null) return;
                var p0 = l.GetLua<T1>(2);
                var p1 = l.GetLua<T2>(3);
                var p2 = l.GetLua<T3>(4);
                var p3 = l.GetLua<T4>(5);
                ((WrappedDelegate)_DelWrapped)(p0, p1, p2, p3);
            }
        }
        [UnityPreserve]
        public class ActionLuaWrapper<T1, T2, T3, T4, T5> : BaseDelegateLuaWrapper
        {
            [UnityPreserve]
            public void Call(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            {
                if (IsClosed) return;
                Target.Call(arg1, arg2, arg3, arg4, arg5);
            }
            public delegate void WrappedDelegate(T1 p0, T2 p1, T3 p2, T4 p3, T5 p4);
            public override Type DelType { get { return typeof(WrappedDelegate); } }
            public override void CallByLua(IntPtr l)
            {
                if (_DelWrapped == null) return;
                var p0 = l.GetLua<T1>(2);
                var p1 = l.GetLua<T2>(3);
                var p2 = l.GetLua<T3>(4);
                var p3 = l.GetLua<T4>(5);
                var p4 = l.GetLua<T5>(6);
                ((WrappedDelegate)_DelWrapped)(p0, p1, p2, p3, p4);
            }
        }
        [UnityPreserve]
        public class ActionLuaWrapper<T1, T2, T3, T4, T5, T6> : BaseDelegateLuaWrapper
        {
            [UnityPreserve]
            public void Call(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            {
                if (IsClosed) return;
                Target.Call(arg1, arg2, arg3, arg4, arg5, arg6);
            }
            public delegate void WrappedDelegate(T1 p0, T2 p1, T3 p2, T4 p3, T5 p4, T6 p5);
            public override Type DelType { get { return typeof(WrappedDelegate); } }
            public override void CallByLua(IntPtr l)
            {
                if (_DelWrapped == null) return;
                var p0 = l.GetLua<T1>(2);
                var p1 = l.GetLua<T2>(3);
                var p2 = l.GetLua<T3>(4);
                var p3 = l.GetLua<T4>(5);
                var p4 = l.GetLua<T5>(6);
                var p5 = l.GetLua<T6>(7);
                ((WrappedDelegate)_DelWrapped)(p0, p1, p2, p3, p4, p5);
            }
        }
        [UnityPreserve]
        public class ActionLuaWrapper<T1, T2, T3, T4, T5, T6, T7> : BaseDelegateLuaWrapper
        {
            [UnityPreserve]
            public void Call(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
            {
                if (IsClosed) return;
                Target.Call(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            }
            public delegate void WrappedDelegate(T1 p0, T2 p1, T3 p2, T4 p3, T5 p4, T6 p5, T7 p6);
            public override Type DelType { get { return typeof(WrappedDelegate); } }
            public override void CallByLua(IntPtr l)
            {
                if (_DelWrapped == null) return;
                var p0 = l.GetLua<T1>(2);
                var p1 = l.GetLua<T2>(3);
                var p2 = l.GetLua<T3>(4);
                var p3 = l.GetLua<T4>(5);
                var p4 = l.GetLua<T5>(6);
                var p5 = l.GetLua<T6>(7);
                var p6 = l.GetLua<T7>(8);
                ((WrappedDelegate)_DelWrapped)(p0, p1, p2, p3, p4, p5, p6);
            }
        }
        [UnityPreserve]
        public class ActionLuaWrapper<T1, T2, T3, T4, T5, T6, T7, T8> : BaseDelegateLuaWrapper
        {
            [UnityPreserve]
            public void Call(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
            {
                if (IsClosed) return;
                Target.Call(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
            }
            public delegate void WrappedDelegate(T1 p0, T2 p1, T3 p2, T4 p3, T5 p4, T6 p5, T7 p6, T8 p7);
            public override Type DelType { get { return typeof(WrappedDelegate); } }
            public override void CallByLua(IntPtr l)
            {
                if (_DelWrapped == null) return;
                var p0 = l.GetLua<T1>(2);
                var p1 = l.GetLua<T2>(3);
                var p2 = l.GetLua<T3>(4);
                var p3 = l.GetLua<T4>(5);
                var p4 = l.GetLua<T5>(6);
                var p5 = l.GetLua<T6>(7);
                var p6 = l.GetLua<T7>(8);
                var p7 = l.GetLua<T8>(9);
                ((WrappedDelegate)_DelWrapped)(p0, p1, p2, p3, p4, p5, p6, p7);
            }
        }
        [UnityPreserve]
        public class ActionLuaWrapper<T1, T2, T3, T4, T5, T6, T7, T8, T9> : BaseDelegateLuaWrapper
        {
            [UnityPreserve]
            public void Call(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
            {
                if (IsClosed) return;
                Target.Call(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
            }
            public delegate void WrappedDelegate(T1 p0, T2 p1, T3 p2, T4 p3, T5 p4, T6 p5, T7 p6, T8 p7, T9 p8);
            public override Type DelType { get { return typeof(WrappedDelegate); } }
            public override void CallByLua(IntPtr l)
            {
                if (_DelWrapped == null) return;
                var p0 = l.GetLua<T1>(2);
                var p1 = l.GetLua<T2>(3);
                var p2 = l.GetLua<T3>(4);
                var p3 = l.GetLua<T4>(5);
                var p4 = l.GetLua<T5>(6);
                var p5 = l.GetLua<T6>(7);
                var p6 = l.GetLua<T7>(8);
                var p7 = l.GetLua<T8>(9);
                var p8 = l.GetLua<T9>(10);
                ((WrappedDelegate)_DelWrapped)(p0, p1, p2, p3, p4, p5, p6, p7, p8);
            }
        }

        [UnityPreserve]
        public class FuncLuaWrapper<R> : BaseDelegateLuaWrapper
        {
            [UnityPreserve]
            public R Call()
            {
                R r = default(R);
                if (IsClosed) return r;
                Target.Call(out r);
                return r;
            }
            public delegate R WrappedDelegate();
            public override Type DelType { get { return typeof(WrappedDelegate); } }
            public override void CallByLua(IntPtr l)
            {
                if (_DelWrapped == null) return;
                var rv = ((WrappedDelegate)_DelWrapped)();
                l.PushLua(rv);
            }
        }
        [UnityPreserve]
        public class FuncLuaWrapper<R, T> : BaseDelegateLuaWrapper
        {
            [UnityPreserve]
            public R Call(T arg)
            {
                R r = default(R);
                if (IsClosed) return r;
                Target.Call(out r, arg);
                return r;
            }
            public delegate R WrappedDelegate(T p0);
            public override Type DelType { get { return typeof(WrappedDelegate); } }
            public override void CallByLua(IntPtr l)
            {
                if (_DelWrapped == null) return;
                var p0 = l.GetLua<T>(2);
                var rv = ((WrappedDelegate)_DelWrapped)(p0);
                l.PushLua(rv);
            }
        }
        [UnityPreserve]
        public class FuncLuaWrapper<R, T1, T2> : BaseDelegateLuaWrapper
        {
            [UnityPreserve]
            public R Call(T1 arg1, T2 arg2)
            {
                R r = default(R);
                if (IsClosed) return r;
                Target.Call(out r, arg1, arg2);
                return r;
            }
            public delegate R WrappedDelegate(T1 p0, T2 p1);
            public override Type DelType { get { return typeof(WrappedDelegate); } }
            public override void CallByLua(IntPtr l)
            {
                if (_DelWrapped == null) return;
                var p0 = l.GetLua<T1>(2);
                var p1 = l.GetLua<T2>(3);
                var rv = ((WrappedDelegate)_DelWrapped)(p0, p1);
                l.PushLua(rv);
            }
        }
        [UnityPreserve]
        public class FuncLuaWrapper<R, T1, T2, T3> : BaseDelegateLuaWrapper
        {
            [UnityPreserve]
            public R Call(T1 arg1, T2 arg2, T3 arg3)
            {
                R r = default(R);
                if (IsClosed) return r;
                Target.Call(out r, arg1, arg2, arg3);
                return r;
            }
            public delegate R WrappedDelegate(T1 p0, T2 p1, T3 p2);
            public override Type DelType { get { return typeof(WrappedDelegate); } }
            public override void CallByLua(IntPtr l)
            {
                if (_DelWrapped == null) return;
                var p0 = l.GetLua<T1>(2);
                var p1 = l.GetLua<T2>(3);
                var p2 = l.GetLua<T3>(4);
                var rv = ((WrappedDelegate)_DelWrapped)(p0, p1, p2);
                l.PushLua(rv);
            }
        }
        [UnityPreserve]
        public class FuncLuaWrapper<R, T1, T2, T3, T4> : BaseDelegateLuaWrapper
        {
            [UnityPreserve]
            public R Call(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            {
                R r = default(R);
                if (IsClosed) return r;
                Target.Call(out r, arg1, arg2, arg3, arg4);
                return r;
            }
            public delegate R WrappedDelegate(T1 p0, T2 p1, T3 p2, T4 p3);
            public override Type DelType { get { return typeof(WrappedDelegate); } }
            public override void CallByLua(IntPtr l)
            {
                if (_DelWrapped == null) return;
                var p0 = l.GetLua<T1>(2);
                var p1 = l.GetLua<T2>(3);
                var p2 = l.GetLua<T3>(4);
                var p3 = l.GetLua<T4>(5);
                var rv = ((WrappedDelegate)_DelWrapped)(p0, p1, p2, p3);
                l.PushLua(rv);
            }
        }
        [UnityPreserve]
        public class FuncLuaWrapper<R, T1, T2, T3, T4, T5> : BaseDelegateLuaWrapper
        {
            [UnityPreserve]
            public R Call(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            {
                R r = default(R);
                if (IsClosed) return r;
                Target.Call(out r, arg1, arg2, arg3, arg4, arg5);
                return r;
            }
            public delegate R WrappedDelegate(T1 p0, T2 p1, T3 p2, T4 p3, T5 p4);
            public override Type DelType { get { return typeof(WrappedDelegate); } }
            public override void CallByLua(IntPtr l)
            {
                if (_DelWrapped == null) return;
                var p0 = l.GetLua<T1>(2);
                var p1 = l.GetLua<T2>(3);
                var p2 = l.GetLua<T3>(4);
                var p3 = l.GetLua<T4>(5);
                var p4 = l.GetLua<T5>(6);
                var rv = ((WrappedDelegate)_DelWrapped)(p0, p1, p2, p3, p4);
                l.PushLua(rv);
            }
        }
        [UnityPreserve]
        public class FuncLuaWrapper<R, T1, T2, T3, T4, T5, T6> : BaseDelegateLuaWrapper
        {
            [UnityPreserve]
            public R Call(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            {
                R r = default(R);
                if (IsClosed) return r;
                Target.Call(out r, arg1, arg2, arg3, arg4, arg5, arg6);
                return r;
            }
            public delegate R WrappedDelegate(T1 p0, T2 p1, T3 p2, T4 p3, T5 p4, T6 p5);
            public override Type DelType { get { return typeof(WrappedDelegate); } }
            public override void CallByLua(IntPtr l)
            {
                if (_DelWrapped == null) return;
                var p0 = l.GetLua<T1>(2);
                var p1 = l.GetLua<T2>(3);
                var p2 = l.GetLua<T3>(4);
                var p3 = l.GetLua<T4>(5);
                var p4 = l.GetLua<T5>(6);
                var p5 = l.GetLua<T6>(7);
                var rv = ((WrappedDelegate)_DelWrapped)(p0, p1, p2, p3, p4, p5);
                l.PushLua(rv);
            }
        }
        [UnityPreserve]
        public class FuncLuaWrapper<R, T1, T2, T3, T4, T5, T6, T7> : BaseDelegateLuaWrapper
        {
            [UnityPreserve]
            public R Call(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
            {
                R r = default(R);
                if (IsClosed) return r;
                Target.Call(out r, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
                return r;
            }
            public delegate R WrappedDelegate(T1 p0, T2 p1, T3 p2, T4 p3, T5 p4, T6 p5, T7 p6);
            public override Type DelType { get { return typeof(WrappedDelegate); } }
            public override void CallByLua(IntPtr l)
            {
                if (_DelWrapped == null) return;
                var p0 = l.GetLua<T1>(2);
                var p1 = l.GetLua<T2>(3);
                var p2 = l.GetLua<T3>(4);
                var p3 = l.GetLua<T4>(5);
                var p4 = l.GetLua<T5>(6);
                var p5 = l.GetLua<T6>(7);
                var p6 = l.GetLua<T7>(8);
                var rv = ((WrappedDelegate)_DelWrapped)(p0, p1, p2, p3, p4, p5, p6);
                l.PushLua(rv);
            }
        }
        [UnityPreserve]
        public class FuncLuaWrapper<R, T1, T2, T3, T4, T5, T6, T7, T8> : BaseDelegateLuaWrapper
        {
            [UnityPreserve]
            public R Call(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
            {
                R r = default(R);
                if (IsClosed) return r;
                Target.Call(out r, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
                return r;
            }
            public delegate R WrappedDelegate(T1 p0, T2 p1, T3 p2, T4 p3, T5 p4, T6 p5, T7 p6, T8 p7);
            public override Type DelType { get { return typeof(WrappedDelegate); } }
            public override void CallByLua(IntPtr l)
            {
                if (_DelWrapped == null) return;
                var p0 = l.GetLua<T1>(2);
                var p1 = l.GetLua<T2>(3);
                var p2 = l.GetLua<T3>(4);
                var p3 = l.GetLua<T4>(5);
                var p4 = l.GetLua<T5>(6);
                var p5 = l.GetLua<T6>(7);
                var p6 = l.GetLua<T7>(8);
                var p7 = l.GetLua<T8>(9);
                var rv = ((WrappedDelegate)_DelWrapped)(p0, p1, p2, p3, p4, p5, p6, p7);
                l.PushLua(rv);
            }
        }
        [UnityPreserve]
        public class FuncLuaWrapper<R, T1, T2, T3, T4, T5, T6, T7, T8, T9> : BaseDelegateLuaWrapper
        {
            [UnityPreserve]
            public R Call(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
            {
                R r = default(R);
                if (IsClosed) return r;
                Target.Call(out r, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
                return r;
            }
            public delegate R WrappedDelegate(T1 p0, T2 p1, T3 p2, T4 p3, T5 p4, T6 p5, T7 p6, T8 p7, T9 p8);
            public override Type DelType { get { return typeof(WrappedDelegate); } }
            public override void CallByLua(IntPtr l)
            {
                if (_DelWrapped == null) return;
                var p0 = l.GetLua<T1>(2);
                var p1 = l.GetLua<T2>(3);
                var p2 = l.GetLua<T3>(4);
                var p3 = l.GetLua<T4>(5);
                var p4 = l.GetLua<T5>(6);
                var p5 = l.GetLua<T6>(7);
                var p6 = l.GetLua<T7>(8);
                var p7 = l.GetLua<T8>(9);
                var p8 = l.GetLua<T9>(10);
                var rv = ((WrappedDelegate)_DelWrapped)(p0, p1, p2, p3, p4, p5, p6, p7, p8);
                l.PushLua(rv);
            }
        }

        private static readonly Dictionary<Type, Type> DelType2WrapperType = new Dictionary<Type, Type>();

        private static Type[] ActionWrapperTypes = new[]
        {
            typeof(ActionLuaWrapper),
            typeof(ActionLuaWrapper<>),
            typeof(ActionLuaWrapper<,>),
            typeof(ActionLuaWrapper<,,>),
            typeof(ActionLuaWrapper<,,,>),
            typeof(ActionLuaWrapper<,,,,>),
            typeof(ActionLuaWrapper<,,,,,>),
            typeof(ActionLuaWrapper<,,,,,,>),
            typeof(ActionLuaWrapper<,,,,,,,>),
            typeof(ActionLuaWrapper<,,,,,,,,>),
        };
        private static Type[] FuncWrapperTypes = new[]
        {
            typeof(FuncLuaWrapper<>),
            typeof(FuncLuaWrapper<,>),
            typeof(FuncLuaWrapper<,,>),
            typeof(FuncLuaWrapper<,,,>),
            typeof(FuncLuaWrapper<,,,,>),
            typeof(FuncLuaWrapper<,,,,,>),
            typeof(FuncLuaWrapper<,,,,,,>),
            typeof(FuncLuaWrapper<,,,,,,,>),
            typeof(FuncLuaWrapper<,,,,,,,,>),
            typeof(FuncLuaWrapper<,,,,,,,,,>),
        };
        public static Type GetWrapperType(Type returnType, int argc)
        {
            if (returnType == typeof(void))
            {
                if (argc >= 0 && argc < ActionWrapperTypes.Length)
                {
                    return ActionWrapperTypes[argc];
                }
            }
            else
            {
                if (argc >= 0 && argc < FuncWrapperTypes.Length)
                {
                    return FuncWrapperTypes[argc];
                }
            }
            return null;
        }

        public static IDelegateLuaWrapper CreateDelegateImp(Type t, BaseLua dyn)
        {
            if (t != null && t.IsSubclassOf(typeof(Delegate)))
            {
                bool cached = false;
                Type wrapperType = null;
                lock (DelType2WrapperType)
                {
                    cached = DelType2WrapperType.TryGetValue(t, out wrapperType);
                }
                if (!cached)
                {
                    MethodInfo invoke = t.GetMethod("Invoke");
                    ParameterInfo[] pars = invoke.GetParameters();
                    if (invoke.ReturnType == typeof(void))
                    {
                        if (pars != null && pars.Length > 0)
                        {
                            if (pars.Length < ActionWrapperTypes.Length)
                            {
                                wrapperType = ActionWrapperTypes[pars.Length];
                                Type[] genericTypes = new Type[pars.Length];
                                for (int i = 0; i < pars.Length; ++i)
                                {
                                    genericTypes[i] = pars[i].ParameterType;
                                }
                                wrapperType = wrapperType.MakeGenericType(genericTypes);
                            }
                        }
                        else
                        {
                            wrapperType = typeof(ActionLuaWrapper);
                        }
                    }
                    else
                    {
                        var gcnt = 0;
                        if (pars != null && pars.Length > 0)
                        {
                            gcnt = pars.Length;
                        }
                        Type[] genericTypes = new Type[gcnt + 1];
                        genericTypes[0] = invoke.ReturnType;
                        if (gcnt < FuncWrapperTypes.Length)
                        {
                            wrapperType = FuncWrapperTypes[gcnt];
                            for (int i = 0; i < gcnt; ++i)
                            {
                                genericTypes[i + 1] = pars[i].ParameterType;
                            }
                            wrapperType = wrapperType.MakeGenericType(genericTypes);
                        }
                    }
                    if (wrapperType != null)
                    {
                        lock (DelType2WrapperType)
                        {
                            DelType2WrapperType[t] = wrapperType;
                        }
                    }
                }
                if (wrapperType != null)
                {
                    var wrapper = Activator.CreateInstance(wrapperType) as IDelegateLuaWrapper;
                    wrapper.Target = dyn;
                    return wrapper;
                }
            }
            return null;
        }
        private static Delegate CreateDelegateFromWrapper(Type deltype, IDelegateLuaWrapper delwrapper)
        {
            return delwrapper.MakeDelegate(deltype);
        }
        public static Delegate CreateDelegate(Type t, BaseLua dyn)
        {
            {
                var dynlua = dyn;
                var l = dynlua.L;
                var refid = dynlua.Refid;
                if (l != IntPtr.Zero && refid != 0)
                {
                    using (var lr = new LuaStateRecover(l))
                    {
                        l.checkstack(5);
                        l.pushlightuserdata(LuaConst.LRKEY_DEL_CACHE); // #reg
                        l.gettable(lua.LUA_REGISTRYINDEX); // reg
                        if (!l.istable(-1))
                        {
                            l.pop(1); // X

                            l.newtable(); // reg
                            l.newtable(); // reg meta
                            l.PushString(LuaConst.LS_COMMON_K); // reg meta 'k'
                            l.SetField(-2, LuaConst.LS_META_KEY_MODE); // reg meta
                            l.setmetatable(-2); // reg
                            l.pushlightuserdata(LuaConst.LRKEY_DEL_CACHE); // reg #reg
                            l.pushvalue(-2); // reg #reg reg
                            l.settable(lua.LUA_REGISTRYINDEX); // reg
                        }

                        l.getref(refid); // reg func
                        l.gettable(-2); // reg finfo
                        if (!l.istable(-1))
                        {
                            l.pop(1); // reg
                            l.newtable(); // reg finfo
                            l.pushvalue(-2); // reg finfo reg
                            l.getref(refid); // reg finfo reg func
                            l.pushvalue(-3); // reg finfo reg func finfo
                            l.settable(-3); // reg info reg
                            l.pop(1); //reg info
                        }

                        l.PushLua(t); // reg finfo dtype
                        l.gettable(-2); // reg finfo del
                        IDelegateLuaWrapper delwrapper = null;
                        if (l.isuserdata(-1))
                        {
                            var wr = l.GetLua<WeakReference>(-1);
                            if (wr != null)
                            {
                                delwrapper = wr.GetWeakReference<IDelegateLuaWrapper>();
                                if (delwrapper == null)
                                {
                                    l.pop(1); // reg finfo
                                    l.PushLua(t); // reg finfo dtype
                                    l.pushnil(); // reg finfo dtype nil
                                    l.settable(-3); // reg finfo
                                    wr.ReturnToPool();
                                }
                            }
                        }
                        if (delwrapper == null)
                        {
                            delwrapper = CreateDelegateImp(t, dyn);
                            if (delwrapper != null)
                            {
                                var wr = ObjectPool.WeakReferencePool.GetFromPool(delwrapper);
                                l.pop(1); // reg finfo
                                l.PushLua(t); // reg finfo dtype
                                l.PushLuaRawObject(wr); // reg finfo dtype del
                                l.settable(-3); // reg finfo
                            }
                        }
                        else
                        {
                            delwrapper.Target = dyn;
                        }
                        if (delwrapper != null)
                        {
                            return CreateDelegateFromWrapper(t, delwrapper);
                        }
                    }
                }
            }
            var wrapper = CreateDelegateImp(t, dyn);
            if (wrapper != null)
            {
                return CreateDelegateFromWrapper(t, wrapper);
            }
            return null;
        }
        public static T CreateDelegate<T>(BaseLua dyn) where T : class
        {
            return CreateDelegate(typeof(T), dyn) as T;
        }
        public static void DisposeDelegate(Delegate del)
        {
            var tar = del.Target as BaseDelegateLuaWrapper;
            if (tar != null && !ReferenceEquals(tar.Target, null))
            {
                tar.Target.Dispose();
            }
        }
        public static void DisposeDelegate(BaseLua func)
        {
            var dynlua = func;
            var l = dynlua.L;
            var refid = dynlua.Refid;
            if (l != IntPtr.Zero && refid != 0)
            {
                using (var lr = new LuaStateRecover(l))
                {
                    l.checkstack(5);
                    l.pushlightuserdata(LuaConst.LRKEY_DEL_CACHE); // #reg
                    l.gettable(lua.LUA_REGISTRYINDEX); // reg
                    if (!l.istable(-1))
                    {
                        return;
                    }

                    l.getref(refid); // reg func
                    l.gettable(-2); // reg finfo
                    if (!l.istable(-1))
                    {
                        return;
                    }

                    l.pushnil();
                    while (l.next(-2))
                    {
                        WeakReference wr;
                        l.GetLua(-1, out wr);
                        if (wr != null)
                        {
                            var delwrapper = wr.GetWeakReference<IDelegateLuaWrapper>();
                            if (delwrapper != null)
                            {
                                if (!ReferenceEquals(delwrapper.Target, null))
                                {
                                    delwrapper.Target.Dispose();
                                }
                            }
                            wr.ReturnToPool();
                        }
                        l.pop(1);

                        //l.pushvalue(-1);
                        //l.pushnil();
                        //l.settable(-4);
                    }

                    l.pop(1); // reg
                    l.getref(refid); // reg func
                    l.pushnil(); // reg func nil
                    l.settable(-3); // reg

                    func.Dispose();
                }
            }
        }

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        private static int LuaMetaDelegateInvoke(IntPtr l)
        {
            var oldtop = l.gettop();
            try
            {
                IDelegateLuaWrapper wrap = null;
                l.pushlightuserdata(LuaConst.LRKEY_DEL_WRAP); // #wrap
                l.rawget(1); // wrap
                if (l.IsUserData(-1))
                {
                    wrap = l.GetLuaRawObject(-1) as IDelegateLuaWrapper;
                    l.pop(1); // X
                }
                else
                {
                    l.pop(1); // X
                    var del = l.GetLua(1) as Delegate;
                    wrap = CreateDelegateImp(del.GetType(), null);
                    wrap.SetDelegate(del);
                    l.pushlightuserdata(LuaConst.LRKEY_DEL_WRAP); // #wrap
                    l.PushLuaRawObject(wrap); // #wrap wrap
                    l.rawset(1); // X
                }
                if (wrap != null)
                {
                    wrap.CallByLua(l);
                }
                return l.gettop() - oldtop;
            }
            catch (Exception e)
            {
                l.LogError(e.ToString());
                l.settop(oldtop);
                return 0;
            }
        }
        public static readonly lua.CFunction LuaFuncDelegateInvoke = new lua.CFunction(LuaMetaDelegateInvoke);
        public static void PushDelegateInvokeMeta(IntPtr l)
        {
            l.pushlightuserdata(LuaConst.LRKEY_DEL_WRAP); // #wrap
            l.gettable(lua.LUA_REGISTRYINDEX); // wrap
            if (!l.isfunction(-1))
            {
                l.pop(1); // X
                l.pushcfunction(LuaFuncDelegateInvoke); // wrap
                l.pushlightuserdata(LuaConst.LRKEY_DEL_WRAP); // wrap #wrap
                l.pushvalue(-2); // wrap #wrap wrap
                l.settable(lua.LUA_REGISTRYINDEX); // wrap
            }
        }
    }
}
