using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngineEx;

using lua = LuaLib.LuaCoreLib;
using lual = LuaLib.LuaAuxLib;
using luae = LuaLib.LuaLibEx;

namespace LuaLib
{
    public static class LuaTypeHub
    {
        public static readonly TypeHubBase EmptyTypeHub = new TypeHubBase(null);
#if !UNITY_ENGINE && !UNITY_5_3_OR_NEWER || NET_4_6 || NET_STANDARD_2_0
        private static System.Collections.Concurrent.ConcurrentDictionary<Type, TypeHubBase> _TypeHubCache = new System.Collections.Concurrent.ConcurrentDictionary<Type, TypeHubBase>();
#else
        private static Dictionary<Type, TypeHubBase> _TypeHubCache = new Dictionary<Type, TypeHubBase>();
#endif
        private static Dictionary<Type, Func<Type, TypeHubBase>> _TypeHubCreators = new Dictionary<Type, Func<Type, TypeHubBase>>(); // Notice: this field should be filled only once, on starting.
        private static LinkedList<KeyValuePair<Type, Func<Type, TypeHubBase>>> _TypeHubPrepareFuncs = new LinkedList<KeyValuePair<Type, Func<Type, TypeHubBase>>>();

        public static void RegTypeHubCreator(Type type, Func<Type, TypeHubBase> creator)
        {
            if (type != null && creator != null)
            {
                _TypeHubCreators[type] = creator;
            }
        }
        public static void RegTypeHubPrepareFunc(Type type, Func<Type, TypeHubBase> func)
        {
            if (type != null && func != null)
            {
                _TypeHubPrepareFuncs.AddLast(new KeyValuePair<Type, Func<Type, TypeHubBase>>(type, func));
            }
        }

        private static void PutIntoCache(TypeHubBase hub)
        {
#if (UNITY_ENGINE || UNITY_5_3_OR_NEWER) && !NET_4_6 && !NET_STANDARD_2_0
            lock (_TypeHubCache)
#endif
            {
                _TypeHubCache[hub.t] = hub;
            }
        }
        public static TypeHubBase GetTypeHub(Type type)
        {
#if ENABLE_PROFILER_LUA_DEEP
            using (var pcon = ProfilerContext.Create("GetTypeHub: {0}", (type == null ? "null" : type.Name)))
#endif
            if (type != null)
            {
#if NETFX_CORE
                LuaLib.Assembly2Lua._SearchAssemblies.Add(type.GetTypeInfo().Assembly);
#endif
                TypeHubBase hub = null;
#if (UNITY_ENGINE || UNITY_5_3_OR_NEWER) && !NET_4_6 && !NET_STANDARD_2_0
                lock (_TypeHubCache)
#endif
                {
                    if (_TypeHubCache.TryGetValue(type, out hub))
                    {
                        return hub;
                    }
                }
                Func<Type, TypeHubBase> creator;
                if (_TypeHubCreators.TryGetValue(type, out creator))
                {
                    try
                    {
                        hub = creator(type);
                    }
                    catch (Exception e)
                    {
                        PlatDependant.LogError(e);
                    }
                }
                if (hub == null)
                {
                    var node = _TypeHubPrepareFuncs.First;
                    while (node != null)
                    {
                        var kvp = node.Value;
                        if (kvp.Key.IsAssignableFrom(type))
                        {
                            try
                            {
                                hub = kvp.Value(type);
                                if (hub != null)
                                {
                                    break;
                                }
                            }
                            catch (Exception e)
                            {
                                PlatDependant.LogError(e);
                            }
                        }
                        node = node.Next;
                    }
                }
                if (hub == null)
                {
                    // TODO: delegates / structs
                    if (type.IsSubclassOf(typeof(ValueType)))
                    {
                        hub = new TypeHubValueType(type);
                    }
                    else if (type.IsSubclassOf(typeof(Delegate)))
                    {
                        hub = new TypeHubDelegate(type);
                    }
                    else
                    {
                        hub = new TypeHubCommon(type);
                    }
                }
                PutIntoCache(hub);
                return hub;
            }
            return new TypeHubBase(null);
        }
        public static Type GetCachedTypeHubType(Type type)
        {
            if (type != null)
            {
#if NETFX_CORE
                LuaLib.Assembly2Lua._SearchAssemblies.Add(type.GetTypeInfo().Assembly);
#endif
                TypeHubBase hub = null;
#if (UNITY_ENGINE || UNITY_5_3_OR_NEWER) && !NET_4_6 && !NET_STANDARD_2_0
                lock (_TypeHubCache)
#endif
                {
                    if (_TypeHubCache.TryGetValue(type, out hub))
                    {
                        return hub.GetType();
                    }
                }
            }
            return null;
        }

        public class TypeHubBase : SelfHandled, ILuaTypeHub
        {
            public struct LuaMetaCallWithPrecompiled
            {
                public ILuaMetaCall _Method;
                public lua.CFunction _Precompiled;
            }

            protected LuaMetaCallWithPrecompiled _Ctor;
            internal LuaMetaCallWithPrecompiled Ctor { get { return _Ctor; } }
            protected SafeDict<string, LuaMetaCallWithPrecompiled> _StaticMethods = new SafeDict<string, LuaMetaCallWithPrecompiled>();
            protected SafeDict<string, LuaMetaCallWithPrecompiled> _StaticFieldsIndex = new SafeDict<string, LuaMetaCallWithPrecompiled>();
            protected SafeDict<string, LuaMetaCallWithPrecompiled> _StaticFieldsNewIndex = new SafeDict<string, LuaMetaCallWithPrecompiled>();

            protected SafeDict<string, LuaMetaCallWithPrecompiled> _InstanceMethods = new SafeDict<string, LuaMetaCallWithPrecompiled>();
            protected SafeDict<string, LuaMetaCallWithPrecompiled> _InstanceFieldsIndex = new SafeDict<string, LuaMetaCallWithPrecompiled>();
            protected SafeDict<string, LuaMetaCallWithPrecompiled> _InstanceFieldsNewIndex = new SafeDict<string, LuaMetaCallWithPrecompiled>();
#if UNITY_EDITOR
            protected HashSet<string> _InstanceMethods_DirectFromBase = new HashSet<string>();
#endif

            protected SafeDict<string, LuaMetaCallWithPrecompiled> _IndexAccessor = new SafeDict<string, LuaMetaCallWithPrecompiled>(); // this[XXX], "get" / "set"
            protected SafeDict<string, LuaMetaCallWithPrecompiled> _Ops = new SafeDict<string, LuaMetaCallWithPrecompiled>();

            protected SafeDict<int, Type> _GenericTypes = new SafeDict<int, Type>();
            public readonly SafeDict<Types, TypeHubBase> _GenericTypesCache = new SafeDict<Types, TypeHubBase>();

            protected SafeDict<string, TypeHubBase> _NestedTypes = new SafeDict<string, TypeHubBase>();

            protected SafeDict<Type, LuaConvertFunc> _ConvertFuncs = new SafeDict<Type, LuaConvertFunc>();
            protected IList<KeyValuePair<Type, LuaConvertFunc>> _ConvertFromFuncs;

            public static void PushCallableRaw(IntPtr l, LuaMetaCallWithPrecompiled info)
            {
                if (info._Precompiled != null)
                {
                    l.pushcfunction(info._Precompiled);
                }
                else if (info._Method != null)
                {
                    l.PushFunction(info._Method);
                }
                else
                {
                    l.pushnil();
                }
            }
            public static void PushCallable(IntPtr l, LuaMetaCallWithPrecompiled info)
            {
                PushCallableRaw(l, info);
                var methodmeta = info._Method as BaseMethodMeta;
                if (methodmeta != null)
                {
                    methodmeta.WrapFunctionByTable(l);
                }
            }

            public void PushToLuaCached(IntPtr l)
            {
                LuaObjCache.PushObjFromCache(l, r);
                if (l.isnoneornil(-1))
                {
                    l.pop(1);
                    PushLuaTypeRaw(l);
                }
            }

            protected internal TypeHubBase(Type type)
            {
                t = type;

                if (type != null)
                {
                    // Parse Type Info!
                    // ctor
                    _Ctor = new LuaMetaCallWithPrecompiled() { _Method = new CtorMethodMeta(type) };
                    // methods
                    Dictionary<string, List<MethodBase>> smethods = new Dictionary<string, List<MethodBase>>();
                    Dictionary<string, List<MethodBase>> imethods = new Dictionary<string, List<MethodBase>>();
                    foreach (var minfo in type.GetMethods()) // FlattenHierarchy?
                    {
                        string name = minfo.Name;
                        if (minfo.IsStatic)
                        {
                            List<MethodBase> list;
                            smethods.TryGetValue(name, out list);
                            if (list == null)
                            {
                                list = new List<MethodBase>();
                                smethods[name] = list;
                            }
                            list.Add(minfo);
                        }
                        else
                        {
                            List<MethodBase> list;
                            imethods.TryGetValue(name, out list);
                            if (list == null)
                            {
                                list = new List<MethodBase>();
                                imethods[name] = list;
                            }
                            list.Add(minfo);
                        }
                    }
                    // static methods
                    foreach (var kvp in smethods)
                    {
                        List<MethodBase> fmethods = new List<MethodBase>();
                        List<MethodBase> gmethods = new List<MethodBase>();
                        for (int i = 0; i < kvp.Value.Count; ++i)
                        {
                            var method = kvp.Value[i];
                            if (method.ContainsGenericParameters)
                            {
                                gmethods.Add(method);
                            }
                            else
                            {
                                fmethods.Add(method);
                            }
                        }
                        var meta = GenericMethodMeta.CreateMethodMeta(fmethods.ToArray(), gmethods.ToArray(), UpdateDataAfterCall);
                        if (meta != null)
                        {
                            _StaticMethods[kvp.Key] = new LuaMetaCallWithPrecompiled() { _Method = meta };
                        }
                    }
                    // instance methods
                    foreach (var kvp in imethods)
                    {
                        List<MethodBase> fmethods = new List<MethodBase>();
                        List<MethodBase> gmethods = new List<MethodBase>();
                        for (int i = 0; i < kvp.Value.Count; ++i)
                        {
                            var method = kvp.Value[i];
                            if (method.ContainsGenericParameters)
                            {
                                gmethods.Add(method);
                            }
                            else
                            {
                                fmethods.Add(method);
                            }
                        }
                        var meta = GenericMethodMeta.CreateMethodMeta(fmethods.ToArray(), gmethods.ToArray(), UpdateDataAfterCall);
                        if (meta != null)
                        {
                            _InstanceMethods[kvp.Key] = new LuaMetaCallWithPrecompiled() { _Method = meta };
                        }
                    }
                    // fields
                    foreach (var fi in type.GetFields())
                    {
                        var name = fi.Name;
                        var getter = PropertyMetaHelper.CreateFieldGetter(fi);
                        var setter = PropertyMetaHelper.CreateFieldSetter(fi, UpdateDataAfterCall);
                        var isStatic = getter is StaticFieldGetter;
                        if (isStatic)
                        {
                            _StaticFieldsIndex[name] = new LuaMetaCallWithPrecompiled() { _Method = getter };
                            _StaticFieldsNewIndex[name] = new LuaMetaCallWithPrecompiled() { _Method = setter };
                        }
                        else
                        {
                            _InstanceFieldsIndex[name] = new LuaMetaCallWithPrecompiled() { _Method = getter };
                            _InstanceFieldsNewIndex[name] = new LuaMetaCallWithPrecompiled() { _Method = setter };
                        }
                    }
                    // properties
                    foreach (var pi in type.GetProperties())
                    {
                        var name = pi.Name;
                        var getter = PropertyMetaHelper.CreatePropertyGetter(pi);
                        var setter = PropertyMetaHelper.CreatePropertySetter(pi, UpdateDataAfterCall);
                        var isStatic = getter is StaticPropertyGetter || setter is StaticPropertySetter;
                        if (isStatic)
                        {
                            _StaticFieldsIndex[name] = new LuaMetaCallWithPrecompiled() { _Method = getter };
                            _StaticFieldsNewIndex[name] = new LuaMetaCallWithPrecompiled() { _Method = setter };
                        }
                        else
                        {
                            _InstanceFieldsIndex[name] = new LuaMetaCallWithPrecompiled() { _Method = getter };
                            _InstanceFieldsNewIndex[name] = new LuaMetaCallWithPrecompiled() { _Method = setter };
                        }
                    }
                    // this[XXX]
                    LuaMetaCallWithPrecompiled indexGetter;
                    LuaMetaCallWithPrecompiled indexSetter;
                    PropertyMetaHelper.ArrayIndexerPair indexerpair;
                    if (PropertyMetaHelper.ArrayIndexers.TryGetValue(type, out indexerpair))
                    {
                        indexGetter = new LuaMetaCallWithPrecompiled() { _Method = indexerpair.Getter };
                        _IndexAccessor["get"] = indexGetter;
                        indexSetter = new LuaMetaCallWithPrecompiled() { _Method = indexerpair.Setter };
                        _IndexAccessor["set"] = indexSetter;
                    }
                    else if (typeof(System.Collections.IList).IsAssignableFrom(type))
                    {
#if UNITY_EDITOR || !ENABLE_IL2CPP
                        if (type.IsArray)
                        {
                            indexGetter = new LuaMetaCallWithPrecompiled() { _Method = Activator.CreateInstance(typeof(ArrayGetter<>).MakeGenericType(type.GetElementType())) as ILuaMetaCall };
                            _IndexAccessor["get"] = indexGetter;
                            indexSetter = new LuaMetaCallWithPrecompiled() { _Method = Activator.CreateInstance(typeof(ArraySetter<>).MakeGenericType(type.GetElementType())) as ILuaMetaCall };
                            _IndexAccessor["set"] = indexSetter;
                        }
                        else
#endif
                        {
                            indexGetter = new LuaMetaCallWithPrecompiled() { _Method = new ListGetter() };
                            _IndexAccessor["get"] = indexGetter;
                            indexSetter = new LuaMetaCallWithPrecompiled() { _Method = new ListSetter() };
                            _IndexAccessor["set"] = indexSetter;
                        }
                    }
                    else
                    {
                        if (_InstanceMethods.TryGetValue("get_Item", out indexGetter) && indexGetter._Method is BaseMethodMeta)
                        {
                            var rawmeta = indexGetter._Method as BaseMethodMeta;
                            indexGetter = new LuaMetaCallWithPrecompiled() { _Method = new IndexGetter(rawmeta) };
                            _IndexAccessor["get"] = indexGetter;
                        }
                        if (_InstanceMethods.TryGetValue("set_Item", out indexSetter) && indexSetter._Method is BaseMethodMeta)
                        {
                            var rawmeta = indexSetter._Method as BaseMethodMeta;
                            indexSetter = new LuaMetaCallWithPrecompiled() { _Method = new IndexSetter(rawmeta) };
                            _IndexAccessor["set"] = indexSetter;
                        }
                    }
                    // ops
                    LuaMetaCallWithPrecompiled binOp;
                    if (_StaticMethods.TryGetValue("op_Addition", out binOp) && binOp._Method is BaseMethodMeta)
                    {
                        var rawmeta = binOp._Method as BaseMethodMeta;
                        binOp = new LuaMetaCallWithPrecompiled() { _Method = new BinaryOpMeta(rawmeta) };
                        _Ops["__add"] = binOp;
                    }
                    if (_StaticMethods.TryGetValue("op_Multiply", out binOp) && binOp._Method is BaseMethodMeta)
                    {
                        var rawmeta = binOp._Method as BaseMethodMeta;
                        binOp = new LuaMetaCallWithPrecompiled() { _Method = new BinaryOpMeta(rawmeta) };
                        _Ops["__mul"] = binOp;
                    }
                    if (_StaticMethods.TryGetValue("op_Subtraction", out binOp) && binOp._Method is BaseMethodMeta)
                    {
                        var rawmeta = binOp._Method as BaseMethodMeta;
                        binOp = new LuaMetaCallWithPrecompiled() { _Method = new BinaryOpMeta(rawmeta) };
                        _Ops["__sub"] = binOp;
                    }
                    if (_StaticMethods.TryGetValue("op_Division", out binOp) && binOp._Method is BaseMethodMeta)
                    {
                        var rawmeta = binOp._Method as BaseMethodMeta;
                        binOp = new LuaMetaCallWithPrecompiled() { _Method = new BinaryOpMeta(rawmeta) };
                        _Ops["__div"] = binOp;
                    }
                    if (_StaticMethods.TryGetValue("op_Modulus", out binOp) && binOp._Method is BaseMethodMeta)
                    {
                        var rawmeta = binOp._Method as BaseMethodMeta;
                        binOp = new LuaMetaCallWithPrecompiled() { _Method = new BinaryOpMeta(rawmeta) };
                        _Ops["__mod"] = binOp;
                    }
                    if (_StaticMethods.TryGetValue("op_LessThan", out binOp) && binOp._Method is BaseMethodMeta)
                    {
                        var rawmeta = binOp._Method as BaseMethodMeta;
                        binOp = new LuaMetaCallWithPrecompiled() { _Method = new BinaryOpMeta(rawmeta) };
                        _Ops["__lt"] = binOp;
                    }
                    if (_StaticMethods.TryGetValue("op_LessThanOrEqual", out binOp) && binOp._Method is BaseMethodMeta)
                    {
                        var rawmeta = binOp._Method as BaseMethodMeta;
                        binOp = new LuaMetaCallWithPrecompiled() { _Method = new BinaryOpMeta(rawmeta) };
                        _Ops["__le"] = binOp;
                    }
                    // nested types
                    Dictionary<string, List<Type>> ntypes = new Dictionary<string, List<Type>>();
                    foreach (var ntype in type.GetAllNestedTypes())
                    {
                        var nname = ntype.Name;
                        var ig = nname.IndexOf('`');
                        if (ig > 0 && ig < nname.Length)
                        {
                            nname = nname.Substring(0, ig);
                        }
                        List<Type> types;
                        ntypes.TryGetValue(nname, out types);
                        if (types == null)
                        {
                            types = new List<Type>();
                            ntypes[nname] = types;
                        }
                        if (ntype.ContainsGenericParameters)
                        {
                            types.Add(ntype);
                        }
                        else
                        {
                            types.Insert(0, ntype);
                        }
                    }

                    foreach (var kvp in ntypes)
                    {
                        var gtypes = kvp.Value;
                        Type ftype = gtypes[0];
                        int index = 1;
                        if (ftype.ContainsGenericParameters)
                        {
                            ftype = null;
                            index = 0;
                        }
                        var hub = LuaTypeHub.GetTypeHub(ftype);
                        for (; index < gtypes.Count; ++index)
                        {
                            hub.RegGenericTypeDefinition(gtypes[index]);
                        }
                        _NestedTypes[kvp.Key] = hub;
                    }
                }
            }

            public void RegGenericTypeDefinition(Type t)
            {
                _GenericTypes[t.GetGenericArguments().Length] = t;
            }

            protected virtual bool UpdateDataAfterCall
            {
                get { return false; }
            }
            public virtual bool Nonexclusive { get { return false; } }

            public Type t { get; protected set; }

            public virtual IntPtr PushLua(IntPtr l, object val)
            {
                return PushLuaCommon(l, val);
            }
            public IntPtr PushLuaCommon(IntPtr l, object val)
            {
#if !DISABLE_LUA_HUB_C
                if (LuaHub.LuaHubC.Ready)
                {
                    IntPtr h = IntPtr.Zero;
                    if (val != null)
                    {
                        h = (IntPtr)System.Runtime.InteropServices.GCHandle.Alloc(val);
                    }
                    LuaHub.LuaHubC.lua_pushObject(l, h, r, ShouldCache);
                    return h;
                }
                else
#endif
                {
                    var h = LuaHub.PushLuaRawObject(l, val); // ud
                    if (ShouldCache)
                    {
                        l.PushCachedMetaTable();
                        l.setmetatable(-2);
                    }

                    // ud
                    l.checkstack(4);
                    l.newtable(); // ud otab
                    l.pushlightuserdata(LuaConst.LRKEY_TARGET); // ud otab #tar
                    l.pushvalue(-3); // ud otab #tar ud
                    l.rawset(-3); // ud otab
                    l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS); // ud otab #trans
                    l.pushlightuserdata(r); // ud otab #trans trans
                    l.rawset(-3); // ud otab
                    PushToLuaCached(l); // // ud otab type
                    l.pushlightuserdata(LuaConst.LRKEY_OBJ_META); // ud otab type #meta
                    l.rawget(-2); // ud otab type meta
                    l.setmetatable(-3); // ud otab type
                    l.pop(1); // ud otab
                    l.remove(-2); // otab

                    //// ud
                    //PushToLuaCached(l); // ud type
                    //l.pushlightuserdata(LuaConst.LRKEY_OBJ_META); // ud type #meta
                    //l.rawget(-2); // ud type meta
                    //l.setmetatable(-3); // ud type
                    //l.pop(1); // ud

                    return h;
                }
            }

            public virtual void SetData(IntPtr l, int index, object val)
            {
#if !DISABLE_LUA_HUB_C
                if (LuaHub.LuaHubC.Ready)
                {
                    IntPtr h = IntPtr.Zero;
                    if (val != null)
                    {
                        h = (IntPtr)System.Runtime.InteropServices.GCHandle.Alloc(val);
                    }
                    LuaHub.LuaHubC.lua_setObject(l, index, h);
                }
                else
#endif
                {
                    LuaCommonMeta.LuaTransCommon.Instance.SetData(l, index, val);
                }
            }

            public virtual object GetLua(IntPtr l, int index)
            {
#if !DISABLE_LUA_HUB_C
                if (LuaHub.LuaHubC.Ready)
                {
                    IntPtr h;
                    LuaHub.LuaHubC.lua_getObject(l, index, out h);
                    try
                    {
                        return ((System.Runtime.InteropServices.GCHandle)h).Target;
                    }
                    catch
                    {
                        return null;
                    }
                }
                else
#endif
                {
                    return LuaHub.GetLuaTableObjectDirect(l, index);
                }
            }

            public void PushLuaObject(IntPtr l, object val)
            {
                if (l != IntPtr.Zero)
                {
                    if (val == null)
                    {
                        PushLua(l, null); // should not cache...
                    }
                    else
                    {
                        if (ShouldCache)
                        {
                            if (LuaObjCache.PushObjFromCache(l, val)) return;
                        }
                        var h = PushLua(l, val);
                        if (ShouldCache && h != IntPtr.Zero)
                        {
                            LuaObjCache.RegObj(l, val, -1, h);
                        }
                    }
                }
            }

            public Type GetType(IntPtr l, int index)
            {
                return t;
            }

            public void PushLuaTypeRaw(IntPtr l)
            {
                l.checkstack(11);
                l.newtable(); // ttab
                LuaObjCache.RegObjStrong(l, this, -1, r);
                if (t != null)
                {
                    LuaObjCache.RegObjStrong(l, t, -1, r);
                }
                // ttab[#trans] = trans
                l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS); // ttab #trans
                l.pushlightuserdata(LuaCommonMeta.LuaTransType.Instance.r); // ttab #trans trans
                l.rawset(-3); // ttab
                // ttab[#tar] = hub
                l.pushlightuserdata(LuaConst.LRKEY_TARGET); // ttab #tar
                l.pushlightuserdata(r); // ttab #tar tar
                l.rawset(-3); // ttab
                // ttab[#objmeta] = ometa
                l.pushlightuserdata(LuaConst.LRKEY_OBJ_META); // ttab #objmeta
                l.newtable(); // ttab #objmeta ometa
                l.rawset(-3); // ttab
                // ttab[@type] = typeof(Type)
                l.PushString(LuaConst.LS_SP_KEY_TYPE); // ttab @type
                var typehub = LuaTypeHub.GetTypeHub(typeof(Type));
                l.PushLuaType(typehub); // ttab @type typeof(Type)
                l.rawset(-3); // ttab
                // ttab[#typeobj] = type
                l.pushlightuserdata(LuaConst.LRKEY_TYPE_OBJ); // ttab #tobj
                typehub.PushLuaCommon(l, t); // ttab #tobj tobj
                l.pushvalue(-1); // ttab #tobj tobj tobj
                l.insert(-4); // tobj ttab #tobj tobj
                l.rawset(-3); // tobj ttab
                // static methods
                foreach (var kvp in _StaticMethods)
                {
                    l.PushString(kvp.Key);
                    PushCallable(l, kvp.Value);
                    l.rawset(-3);
                }
                // nested types
                foreach (var kvp in _NestedTypes)
                {
                    l.PushString(kvp.Key);
                    l.PushLuaType(kvp.Value);
                    l.rawset(-3);
                }
                // readonly fields
                l.newtable(); // tobj ttab consts
                // getter
                l.PushString(LuaConst.LS_SP_KEY_GETTER); // tobj ttab consts @getter
                l.newtable(); // tobj ttab consts @getter getter
                foreach (var kvp in _StaticFieldsIndex)
                {
                    l.PushString(kvp.Key);
                    PushCallableRaw(l, kvp.Value);
                    l.rawset(-3);
                }
                l.PushString(LuaConst.LS_SP_KEY_NONPUBLIC);
                l.pushvalue(-5);
                l.pushcclosure(LuaTypeNonPublicReflector.LuaFuncCreateStaticReflector, 1);
                l.rawset(-3);
                l.PushString(LuaConst.LS_SP_KEY_REFLECTOR);
                l.pushvalue(-5);
                l.pushcclosure(LuaTypeNonPublicReflector.LuaFuncCreateReflector, 1);
                l.rawset(-3);
                l.pushlightuserdata(LuaConst.LRKEY_GETTER); // tobj ttab consts @getter getter #getter
                l.pushvalue(-2); // tobj ttab consts @getter getter #getter getter
                l.rawset(-6); // tobj ttab consts @getter getter
                l.pushvalue(-1); // tobj ttab consts @getter getter getter
                l.insert(-5); // tobj getter ttab consts @getter getter
                l.rawset(-3); // tobj getter ttab consts
                // setter
                l.PushString(LuaConst.LS_SP_KEY_SETTER); // tobj getter ttab consts @setter
                l.newtable(); // tobj getter ttab consts @setter setter
                foreach (var kvp in _StaticFieldsNewIndex)
                {
                    l.PushString(kvp.Key);
                    PushCallableRaw(l, kvp.Value);
                    l.rawset(-3);
                }
                l.pushlightuserdata(LuaConst.LRKEY_SETTER); // tobj getter ttab consts @setter setter #setter
                l.pushvalue(-2); // tobj getter ttab consts @setter setter #setter setter
                l.rawset(-6); // tobj getter ttab consts @setter setter
                l.pushvalue(-1); // tobj getter ttab consts @setter setter setter
                l.insert(-5); // tobj getter setter ttab consts @setter setter
                l.rawset(-3); // tobj getter setter ttab consts
                // typemeta
                l.newtable(); // tobj getter setter ttab consts tmeta
                // __call
                if (t != null)
                {
                    l.PushString(LuaConst.LS_META_KEY_CALL); // tobj getter setter ttab consts tmeta __call
                    PushCallableRaw(l, _Ctor); // tobj getter setter ttab consts tmeta __call ctor
#if ENABLE_PROFILER && ENABLE_PROFILER_LUA && ENABLE_PROFILER_LUA_DEEP && !DISABLE_PROFILER_LUA_GC_ALLOC
                    if (!t.IsValueType)
                    {
                        l.PushString("ctor of " + t.ToString());
                        l.pushcclosure(LuaProfileHelper.Del_CallFuncWithProfilerTag, 2);
                    }
#endif
                    l.rawset(-3); // tobj getter setter ttab consts tmeta
                }
                // __eq
                l.PushString(LuaConst.LS_META_KEY_EQ);
                l.PushCommonEqFunc();
                l.rawset(-3);
                // __tostring
                l.PushString(LuaConst.LS_META_KEY_TOSTRING);
                l.pushcfunction(LuaCommonMeta.LuaFuncCommonToStr);
                l.rawset(-3);
                // __index
                l.PushString(LuaConst.LS_META_KEY_INDEX); // tobj getter setter ttab consts tmeta __index
                l.pushvalue(-6); // tobj getter setter ttab consts tmeta __index getter
                l.pushvalue(-8); // tobj getter setter ttab consts tmeta __index getter tobj
                l.pushlightuserdata(r); // tobj getter setter ttab consts tmeta __index getter tobj this
                l.pushvalue(-6); // tobj getter setter ttab consts tmeta __index getter tobj this consts
#if !DISABLE_LUA_HUB_C
                if (LuaHub.LuaHubC.Ready)
                {
                    LuaHub.LuaHubC.lua_pushTypeIndex(l, 4);
                }
                else
#endif
                {
                    l.pushcclosure(LuaFuncTypeIndex, 4); // tobj getter setter ttab consts tmeta __index func
                }
                l.rawset(-3); // tobj getter setter ttab consts tmeta
                // __newindex
                l.PushString(LuaConst.LS_META_KEY_NINDEX); // tobj getter setter ttab consts tmeta __newindex
                l.pushvalue(-5); // tobj getter setter ttab consts tmeta __newindex setter
                l.pushvalue(-8); // tobj getter setter ttab consts tmeta __newindex setter tobj
#if !DISABLE_LUA_HUB_C
                if (LuaHub.LuaHubC.Ready)
                {
                    LuaHub.LuaHubC.lua_pushTypeNewIndex(l, 2);
                }
                else
#endif
                {
                    l.pushcclosure(LuaFuncTypeNewIndex, 2); // tobj getter setter ttab consts tmeta __newindex func
                }
                l.rawset(-3); // tobj getter setter ttab consts tmeta
                // ext-type
                l.pushlightuserdata(LuaConst.LRKEY_EXTENDED);
                l.pushnumber(2);
                l.rawset(-3);
                // clean
                l.remove(-4); // tobj getter ttab consts tmeta
                l.remove(-4); // tobj ttab consts tmeta
                l.remove(-4); // ttab consts tmeta
                l.setmetatable(-3); // ttab consts
                // #objmeta
                l.pushlightuserdata(LuaConst.LRKEY_OBJ_META); // ttab consts #objmeta
                l.rawget(-3); // ttab consts ometa
                l.PushString(LuaConst.LS_SP_KEY_OBJMETA); // ttab consts ometa @objmeta
                l.pushvalue(-2); // ttab consts ometa @objmeta ometa
                l.rawset(-4); // ttab consts ometa
                // objmethods
#if UNITY_EDITOR
                if (t != null && t.BaseType != null)
                {
                    var baseclass = GetTypeHub(t.BaseType);
                    if (baseclass != null)
                    {
                        foreach (var kvp in _InstanceMethods)
                        {
                            if (kvp.Value._Precompiled != null)
                            {
                                if (baseclass._InstanceMethods.ContainsKey(kvp.Key))
                                {
                                    var basemethod = baseclass._InstanceMethods[kvp.Key];
                                    if (kvp.Value._Precompiled == basemethod._Precompiled)
                                    {
                                        if (!_InstanceMethods_DirectFromBase.Contains(kvp.Key))
                                        {
                                            BaseMethodMeta.TrigOnReflectInvokeMember(t, kvp.Key);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
#endif
                l.newtable(); // ttab consts ometa omethods
                foreach (var kvp in _InstanceMethods)
                {
                    l.PushString(kvp.Key);
                    PushCallable(l, kvp.Value);
                    l.rawset(-3);
                }
                l.pushlightuserdata(LuaConst.LRKEY_METHODS); // ttab consts ometa omethods #methods
                l.pushvalue(-2); // ttab consts ometa omethods #methods omethods
                l.rawset(-4); // ttab consts ometa omethods
                l.PushString(LuaConst.LS_SP_KEY_OBJMETHODS); // ttab consts ometa omethods @objmethods
                l.pushvalue(-2); // ttab consts ometa omethods @objmethods omethods
                l.rawset(-5); // ttab consts ometa omethods
                // @type
                l.PushString(LuaConst.LS_SP_KEY_TYPE); // ttab consts ometa omethods @type
                l.pushvalue(-5); // ttab consts ometa omethods @type ttab
                l.rawset(-3); // ttab consts ometa omethods
                // bin-ops
                if (_Ops.ContainsKey("__add"))
                {
                    l.PushString(LuaConst.LS_SP_KEY_ADD);
                    PushCallableRaw(l, _Ops["__add"]);
                    l.rawset(-3);
                }
                if (_Ops.ContainsKey("__sub"))
                {
                    l.PushString(LuaConst.LS_SP_KEY_SUB);
                    PushCallableRaw(l, _Ops["__sub"]);
                    l.rawset(-3);
                }
                if (_Ops.ContainsKey("__mul"))
                {
                    l.PushString(LuaConst.LS_SP_KEY_MUL);
                    PushCallableRaw(l, _Ops["__mul"]);
                    l.rawset(-3);
                }
                if (_Ops.ContainsKey("__div"))
                {
                    l.PushString(LuaConst.LS_SP_KEY_DIV);
                    PushCallableRaw(l, _Ops["__div"]);
                    l.rawset(-3);
                }
                if (_Ops.ContainsKey("__mod"))
                {
                    l.PushString(LuaConst.LS_SP_KEY_MOD);
                    PushCallableRaw(l, _Ops["__mod"]);
                    l.rawset(-3);
                }
                if (_Ops.ContainsKey("__lt"))
                {
                    l.PushString(LuaConst.LS_SP_KEY_LT);
                    PushCallableRaw(l, _Ops["__lt"]);
                    l.rawset(-3);
                }
                if (_Ops.ContainsKey("__le"))
                {
                    l.PushString(LuaConst.LS_SP_KEY_LE);
                    PushCallableRaw(l, _Ops["__le"]);
                    l.rawset(-3);
                }
                // TODO: == and !=
                {
                    l.PushString(LuaConst.LS_SP_KEY_EQ);
                    l.pushvalue(-1);
                    l.rawset(-3);
                }
                // #trans
                l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS); // ttab consts ometa omethods #trans
                l.pushlightuserdata(r); // ttab consts ometa omethods #trans trans
                l.rawset(-3); // ttab consts ometa omethods
                // getter
                l.newtable(); // ttab consts ometa omethods getter
                foreach (var kvp in _InstanceFieldsIndex)
                {
                    l.PushString(kvp.Key);
                    PushCallableRaw(l, kvp.Value);
                    l.rawset(-3);
                }
                l.PushString(LuaConst.LS_SP_KEY_NONPUBLIC);
                l.pushcfunction(LuaTypeNonPublicReflector.LuaFuncCreateInstanceReflector);
                l.rawset(-3);
                l.pushlightuserdata(LuaConst.LRKEY_GETTER); // ttab consts ometa omethods getter #getter
                l.pushvalue(-2); // ttab consts ometa omethods getter #getter getter
                l.rawset(-5); // ttab consts ometa omethods getter
                l.PushString(LuaConst.LS_SP_KEY_OBJGETTER); // ttab consts ometa omethods getter @objgetter
                l.pushvalue(-2); // ttab consts ometa omethods getter @objgetter getter
                l.rawset(-6); // ttab consts ometa omethods getter
                // setter
                l.newtable(); // ttab consts ometa omethods getter setter
                foreach (var kvp in _InstanceFieldsNewIndex)
                {
                    l.PushString(kvp.Key);
                    PushCallableRaw(l, kvp.Value);
                    l.rawset(-3);
                }
                l.pushlightuserdata(LuaConst.LRKEY_SETTER); // ttab consts ometa omethods getter setter #setter
                l.pushvalue(-2); // ttab consts ometa omethods getter setter #setter setter
                l.rawset(-6); // ttab consts ometa omethods getter setter
                l.PushString(LuaConst.LS_SP_KEY_OBJSETTER); // ttab consts ometa omethods getter setter @objsetter
                l.pushvalue(-2); // ttab consts ometa omethods getter setter @objsetter setter
                l.rawset(-7); // ttab consts ometa omethods getter setter
                l.remove(-5); // ttab ometa omethods getter setter
                // __newindex
                l.PushString(LuaConst.LS_META_KEY_NINDEX); // ttab ometa omethods getter setter __newindex
                l.pushvalue(-4); // ttab ometa omethods getter setter __newindex omethods
                l.pushvalue(-3); // ttab ometa omethods getter setter __newindex omethods setter
                LuaMetaCallWithPrecompiled indexSetter;
                if (_IndexAccessor.TryGetValue("set", out indexSetter))
                {
                    l.PushString(LuaConst.LS_SP_KEY_INDEX);
                    l.pushvalue(-1);
                    PushCallableRaw(l, indexSetter);
                    l.rawset(-4); // ttab ometa omethods getter setter __newindex omethods setter indexerkey
                }
                else
                {
                    l.pushnil(); // ttab ometa omethods getter setter __newindex omethods setter indexerkey
                }
#if !DISABLE_LUA_HUB_C
                if (LuaHub.LuaHubC.Ready)
                {
                    LuaHub.LuaHubC.lua_pushObjNewIndex(l, 3);
                }
                else
#endif
                {
                    l.pushcclosure(LuaFuncObjNewIndex, 3); // ttab ometa omethods getter setter __newindex func
                }
                l.rawset(-6); // ttab ometa omethods getter setter
                l.pop(1); // ttab ometa omethods getter
                // __index
                l.PushString(LuaConst.LS_META_KEY_INDEX); // ttab ometa omethods getter __index
                l.insert(-3); // ttab ometa __index omethods getter
                LuaMetaCallWithPrecompiled indexGetter;
                if (_IndexAccessor.TryGetValue("get", out indexGetter))
                {
                    l.PushString(LuaConst.LS_SP_KEY_INDEX);
                    l.pushvalue(-1);
                    PushCallableRaw(l, indexGetter);
                    l.rawset(-4); // ttab ometa __index omethods getter indexerkey
                }
                else
                {
                    l.pushnil(); // ttab ometa __index omethods getter indexerkey
                }
#if !DISABLE_LUA_HUB_C
                if (LuaHub.LuaHubC.Ready)
                {
                    LuaHub.LuaHubC.lua_pushObjIndex(l, 3);
                }
                else
#endif
                {
                    l.pushcclosure(LuaFuncObjIndex, 3); // ttab ometa __index func
                }
                l.rawset(-3); // ttab ometa
                //l.PushString(LuaConst.LS_META_KEY_GC); // ttab ometa __gc
                //l.pushcfunction(LuaCommonMeta.LuaFuncRawGC);  // ttab ometa __gc func
                //l.rawset(-3); // ttab ometa
                // __eq
                l.PushString(LuaConst.LS_META_KEY_EQ);
                l.PushCommonEqFunc();
                l.rawset(-3);
                // bin-op
                if (_Ops.ContainsKey("__add"))
                {
                    l.PushString(LuaConst.LS_META_KEY_ADD);
                    l.PushBinaryOp(LuaConst.LS_SP_KEY_ADD);
                    l.rawset(-3);
                }
                if (_Ops.ContainsKey("__sub"))
                {
                    l.PushString(LuaConst.LS_META_KEY_SUB);
                    l.PushBinaryOp(LuaConst.LS_SP_KEY_SUB);
                    l.rawset(-3);
                }
                if (_Ops.ContainsKey("__mul"))
                {
                    l.PushString(LuaConst.LS_META_KEY_MUL);
                    l.PushBinaryOp(LuaConst.LS_SP_KEY_MUL);
                    l.rawset(-3);
                }
                if (_Ops.ContainsKey("__div"))
                {
                    l.PushString(LuaConst.LS_META_KEY_DIV);
                    l.PushBinaryOp(LuaConst.LS_SP_KEY_DIV);
                    l.rawset(-3);
                }
                if (_Ops.ContainsKey("__mod"))
                {
                    l.PushString(LuaConst.LS_META_KEY_MOD);
                    l.PushBinaryOp(LuaConst.LS_SP_KEY_MOD);
                    l.rawset(-3);
                }
                if (_Ops.ContainsKey("__lt"))
                {
                    l.PushString(LuaConst.LS_META_KEY_LT);
                    l.PushBinaryOp(LuaConst.LS_SP_KEY_LT);
                    l.rawset(-3);
                }
                if (_Ops.ContainsKey("__le"))
                {
                    l.PushString(LuaConst.LS_META_KEY_LE);
                    l.PushBinaryOp(LuaConst.LS_SP_KEY_LE);
                    l.rawset(-3);
                }
                // unary-op
                if (_StaticMethods.ContainsKey("op_UnaryNegation"))
                {
                    l.PushString(LuaConst.LS_META_KEY_UNM);
                    PushCallableRaw(l, _StaticMethods["op_UnaryNegation"]);
                    l.rawset(-3);
                }
                // __tostring
                if (_InstanceMethods.ContainsKey("ToString"))
                {
                    l.PushString(LuaConst.LS_META_KEY_TOSTRING);
                    PushCallableRaw(l, _InstanceMethods["ToString"]);
                    l.rawset(-3);
                }
                // __call
                if (t != null && t.IsSubclassOf(typeof(Delegate)))
                {
                    if (_InstanceMethods.ContainsKey("Invoke"))
                    {
                        l.PushString(LuaConst.LS_META_KEY_CALL);
                        LuaDelegateGenerator.PushDelegateInvokeMeta(l);
                        l.rawset(-3);
                    }
                }
                // ext-type
                l.pushlightuserdata(LuaConst.LRKEY_EXTENDED);
                l.pushnumber(1);
                l.rawset(-3);
                l.pop(1); // ttab
            }

            public LuaConvertFunc GetConverter(Type totype)
            {
                LuaConvertFunc rv;
                _ConvertFuncs.TryGetValue(totype, out rv);
                return rv;
            }
            public LuaConvertFunc GetConverterFrom(Type fromtype)
            {
                if (_ConvertFromFuncs != null)
                {
                    for (int i = 0; i < _ConvertFromFuncs.Count; ++i)
                    {
                        var kvp = _ConvertFromFuncs[i];
                        var stype = kvp.Key;
                        if (stype.IsAssignableFrom(fromtype))
                        {
                            return kvp.Value;
                        }
                    }
                }
                return null;
            }

            public virtual bool ShouldCache
            {
                get { return true; }
            }
            public virtual bool PushFromCache(IntPtr l, object val)
            {
                if (val == null)
                {
                    l.pushnil();
                    return true;
                }
                else
                {
                    return LuaObjCache.PushObjFromCache(l, val);
                }
            }

            private static readonly lua.CFunction LuaFuncTypeIndex = new lua.CFunction(LuaMetaTypeIndex);
            private static readonly lua.CFunction LuaFuncTypeNewIndex = new lua.CFunction(LuaMetaTypeNewIndex);
            private static readonly lua.CFunction LuaFuncObjIndex = new lua.CFunction(LuaMetaObjIndex);
            private static readonly lua.CFunction LuaFuncObjNewIndex = new lua.CFunction(LuaMetaObjNewIndex);
            public static readonly lua.CFunction LuaFuncGenericIndex = new lua.CFunction(LuaMetaGenericIndex);
            public static readonly lua.CFunction LuaFuncGenericNewIndex = new lua.CFunction(LuaMetaGenericNewIndex);

            [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
            private static int LuaMetaTypeIndex(IntPtr l)
            {
                //var oldtop = l.gettop();
                l.pushcfunction(LuaHub.LuaFuncOnError); // err
                l.pushvalue(2); // err key
                l.gettable(lua.upvalueindex(1)); // err getter
                if (!l.isnoneornil(-1))
                {
                    var code = l.pcall(0, 1, -2); // err rv
                    if (code != 0)
                    {
                        l.pop(2);
                        return 0;
                    }
                    l.remove(-2); // rv
                    return 1;
                }
                l.pop(2); // X
                l.pushvalue(2); // key
                l.gettable(lua.upvalueindex(2)); // rv
                if (!l.isnoneornil(-1))
                {
                    return 1;
                }
                l.pop(1); // X
                if (l.istable(2))
                {
                    var rv = LuaMetaGenericIndex(l);
                    if (rv > 0)
                    {
                        return rv;
                    }
                }
                l.pushvalue(2); // key
                l.gettable(lua.upvalueindex(4)); // rv;
                return 1;

                //return l.gettop() - oldtop;
            }
            [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
            private static int LuaMetaTypeNewIndex(IntPtr l)
            {
                //var oldtop = l.gettop();
                if (l.istable(2))
                {
                    var rv = LuaMetaGenericNewIndex(l);
                    if (rv > 0)
                    {
                        l.pop(rv);
                        return 0;
                    }
                }
                l.pushcfunction(LuaHub.LuaFuncOnError); // err
                l.pushvalue(2); // err key
                l.gettable(lua.upvalueindex(1)); // err setter
                if (!l.isnoneornil(-1))
                {
                    l.pushvalue(3); // err setter v
                    var code = l.pcall(1, 0, -3); // err
                    if (code != 0)
                    { // err failmessage
                        l.pop(2);
                        return 0;
                    }
                    l.pop(1); // X
                    return 0;
                }
                l.pop(2); // X

                // try set properties on type-obj.
                if (l.isnil(3))
                {
                    l.pushvalue(2); // k
                    l.pushvalue(3); // k v
                    l.settable(lua.upvalueindex(2)); // X
                    return 0;
                }
                else
                {
                    l.pushvalue(2); // k
                    l.pushvalue(3); // k v
                    l.settable(lua.upvalueindex(2)); // X
                    l.pushvalue(2); // k
                    l.rawget(lua.upvalueindex(2)); // v'
                    if (l.isnoneornil(-1))
                    {
                        // set C# property
                        l.pop(1); // X
                        return 0;
                    }
                    else
                    {
                        // set type-obj's ex-fields
                        l.pop(1);
                        l.pushvalue(2); // k
                        l.pushnil(); // k nil
                        l.rawset(lua.upvalueindex(2)); // X
                        l.pushvalue(2); // k
                        l.pushvalue(3); // k v
                        l.rawset(1); // X
                        return 0;
                    }
                }

                //return l.gettop() - oldtop;
            }
            [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
            private static int LuaMetaObjIndex(IntPtr l)
            {
                //var oldtop = l.gettop();
                l.pushvalue(2); // k
                l.gettable(lua.upvalueindex(1)); // rv
                if (!l.isnoneornil(-1))
                {
                    return 1;
                }
                l.pop(1); // X
                l.pushcfunction(LuaHub.LuaFuncOnError); // err
                l.pushvalue(2); // err k
                l.gettable(lua.upvalueindex(2)); // err getter
                if (!l.isnoneornil(-1))
                {
                    l.pushvalue(1); // err getter tar
                    var code = l.pcall(1, 1, -3); // err rv
                    if (code != 0)
                    {
                        l.pop(2);
                        return 0;
                    }
                    l.remove(-2); // rv
                    return 1;
                }
                l.pop(2); // X
                if (!l.isnoneornil(lua.upvalueindex(3)))
                {
                    l.pushcfunction(LuaHub.LuaFuncOnError); // err
                    l.pushvalue(lua.upvalueindex(3)); // err indexerkey
                    l.rawget(lua.upvalueindex(2)); // err indexer
                    l.pushvalue(1); // err indexer tar
                    l.pushvalue(2); // err indexer tar key
                    var code = l.pcall(2, 1, -4); // err rv
                    if (code != 0)
                    {
                        l.pop(2);
                        return 0;
                    }
                    l.remove(-2); // rv
                    return 1;
                }
                return 0;

                //return l.gettop() - oldtop;
            }
            [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
            private static int LuaMetaObjNewIndex(IntPtr l)
            {
                //var oldtop = l.gettop();
                l.pushcfunction(LuaHub.LuaFuncOnError); // err
                l.pushvalue(2); // err k
                l.gettable(lua.upvalueindex(2)); // err setter
                if (!l.isnoneornil(-1))
                {
                    l.pushvalue(1); // err setter tar
                    l.pushvalue(3); // err setter tar val
                    var code = l.pcall(2, 0, -4); // err
                    if (code != 0)
                    {
                        l.pop(2);
                        return 0;
                    }
                    l.pop(1); // X
                    return 0;
                }
                l.pop(2); // X
                if (!l.isnoneornil(lua.upvalueindex(3)))
                {
                    l.pushcfunction(LuaHub.LuaFuncOnError); // err
                    l.pushvalue(lua.upvalueindex(3)); // err indexerkey
                    l.rawget(lua.upvalueindex(2)); // err indexer
                    l.pushvalue(1); // err indexer tar
                    l.pushvalue(2); // err indexer tar key
                    l.pushvalue(3); // err indexer tar key val
                    var code = l.pcall(3, 1, -5); // err failed
                    bool failed = code != 0 || l.toboolean(-1);
                    l.pop(2); // X
                    if (!failed)
                    {
                        return 0;
                    }
                }
                // raw set
                l.pushvalue(1);
                l.pushvalue(2);
                l.pushvalue(3);
                l.rawset(-3);
                l.pop(1);
                return 0;
                //return l.gettop() - oldtop;
            }

            [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
            private static int LuaMetaGenericIndex(IntPtr l)
            {
                TypeHubBase hub = l.GetLuaLightObject(lua.upvalueindex(3)) as TypeHubBase;
                if (hub != null && hub._GenericTypes.Count > 0)
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
                        if (hub._GenericTypes.ContainsKey(gtypes.Count))
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
                            l.rawget(-2); // gcache type
                            if (l.isnoneornil(-1))
                            {
                                l.pop(1); // gcache

                                TypeHubBase precompiled = null;
                                if (hub._GenericTypesCache.TryGetValue(gtypes, out precompiled) && precompiled != null)
                                {
                                    l.PushLuaType(precompiled); // gcache type
                                }
                                else
                                {
                                    var gtype = hub._GenericTypes[gtypes.Count];
                                    var tarray = ObjectPool.GetParamTypesFromPool(gtypes.Count);
                                    for (int i = 0; i < tarray.Length; ++i)
                                    {
                                        tarray[i] = gtypes[i];
                                    }
                                    Type rvType = null;
                                    try
                                    {
                                        rvType = gtype.MakeGenericType(tarray);
                                    }
                                    catch (Exception e)
                                    {
                                        l.LogError(e);
                                    }

                                    if (rvType == null)
                                    {
                                        l.PushLuaType(LuaTypeHub.EmptyTypeHub); // gcache type
                                    }
                                    else
                                    {
                                        l.PushLuaType(rvType); // gcache type
                                        // should we cache it back to _GenericTypesCache? need lock... we can cache it in c# code, instead of in runtime.
                                    }
                                }
                                l.pushlightuserdata(LuaConst.LRKEY_GENERIC_CACHE); // gcache type #gcache
                                l.pushvalue(-2); // gcache type #gcache type
                                l.rawset(-4); // gcache type
                                l.remove(-2); // type
                                return 1;
                            }
                            else
                            {
                                l.remove(-2); // type
                                return 1;
                            }
                        }
                        l.pushnil();
                        return 1;
                    }
                }

                return 0;
            }
            [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
            private static int LuaMetaGenericNewIndex(IntPtr l)
            {
                if (l.isnil(3) || l.GetLuaObjectType(3) == typeof(Type))
                {
                    l.pushlightuserdata(LuaConst.LRKEY_TARGET); // #tar
                    l.rawget(1); // tar
                    TypeHubBase hub = l.GetLuaLightObject(-1) as TypeHubBase;
                    l.pop(1); // X
                    if (hub != null && hub._GenericTypes.Count > 0)
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
                            if (hub._GenericTypes.ContainsKey(gtypes.Count))
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
                                l.pushvalue(3); // gcache #gcache type
                                l.rawset(-3); // gcache
                                l.pop(1); // X
                                l.pushvalue(3); // val
                                return 1;
                            }
                            l.pushvalue(3); // val
                            return 1;
                        }
                    }
                }
                return 0;
            }
        }

        public class TypeHubCommon : TypeHubBase
        {
            public TypeHubCommon(Type type) : base(type) { }
            protected void PutIntoCache()
            {
                LuaTypeHub.PutIntoCache(this);
            }

            // TODO: System.Object members should be here.
        }

        public class TypeHubValueType : TypeHubCommon
        {
            public TypeHubValueType(Type type) : base(type) { }

            public override bool ShouldCache
            {
                get { return false; }
            }
        }

        public class TypeHubDelegate : TypeHubCommon
        {
            public TypeHubDelegate(Type type) : base(type) { }
        }

        public abstract class TypeHubCommonPrecompiled : TypeHubCommon
        {
            public TypeHubCommonPrecompiled(Type type) : base(type)
            {
                PutIntoCache();
                RegPrecompiledStatic();
            }

            public virtual void RegPrecompiledStatic() { }
        }
        public abstract class TypeHubCommonPrecompiled<T> : TypeHubCommonPrecompiled
        {
            public TypeHubCommonPrecompiled() : base(typeof(T)) { }
        }

        public abstract class TypeHubClonedValuePrecompiled<T> : TypeHubValueType, ILuaTrans, ILuaTrans<T>, ILuaPush<T>
        {
            public TypeHubClonedValuePrecompiled() : base(typeof(T))
            {
                PutIntoCache();
                RegPrecompiledStatic();
            }

            protected override bool UpdateDataAfterCall
            {
                get { return true; }
            }

            public virtual void RegPrecompiledStatic() { }

            public virtual void SetData(IntPtr l, int index, T val)
            {
                SetData(l, index, (object)val);
            }

            public virtual object GetLuaObject(IntPtr l, int index)
            {
                return base.GetLua(l, index);
            }

            object ILuaTrans.GetLua(IntPtr l, int index)
            {
                return GetLuaObject(l, index);
            }

            public new virtual T GetLua(IntPtr l, int index)
            {
                var raw = GetLuaObject(l, index);
                if (raw is T)
                {
                    return (T)raw;
                }
                else
                {
                    return default(T);
                }
            }
            public virtual T GetLuaChecked(IntPtr l, int index)
            {
                if (l.istable(index))
                {
                    return GetLua(l, index);
                }
                return default(T);
            }

            public virtual IntPtr PushLua(IntPtr l, T val)
            {
                return PushLua(l, (object)val);
            }
        }
        public abstract class TypeHubValueTypePrecompiled<T> : TypeHubClonedValuePrecompiled<T>, ILuaTrans<T?>, ILuaPush<T?> where T : struct
        {
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
                SetData(l, index, (object)val);
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

        public abstract class TypeHubEnumPrecompiled<T> : TypeHubValueTypePrecompiled<T>, ILuaNative where T : struct
        {
            public TypeHubEnumPrecompiled()
            {
                LuaHubNative = new TypeHubEnumNative(this);
            }

            protected override bool UpdateDataAfterCall
            {
                get { return false; }
            }

            public override IntPtr PushLua(IntPtr l, object val)
            {
                PushLua(l, (T)val);
                return IntPtr.Zero;
            }
            public override void SetData(IntPtr l, int index, object val)
            {
                SetDataRaw(l, index, (T)val);
            }
            public override object GetLuaObject(IntPtr l, int index)
            {
                return GetLuaRaw(l, index);
            }

            public override IntPtr PushLua(IntPtr l, T val)
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
                return IntPtr.Zero;
            }
            public override void SetData(IntPtr l, int index, T val)
            {
                SetDataRaw(l, index, val);
            }
            public override T GetLua(IntPtr l, int index)
            {
                return GetLuaRaw(l, index);
            }

            public virtual T ConvertFromNum(double val)
            {
                try
                {
#if CONVERT_ENUM_SAFELY
                    return (T)Enum.ToObject(typeof(T), (ulong)val);
#else
                    return EnumUtils.ConvertToEnumForcibly<T>((ulong)val);
#endif
                }
                catch (Exception e)
                {
                    PlatDependant.LogError(e);
                }
                return default(T);
            }
            public virtual double ConvertToNum(T val)
            {
#if CONVERT_ENUM_SAFELY
                return Convert.ToDouble(val);
#else
                return EnumUtils.ConvertFromEnumForcibly<T>(val);
#endif
            }
            public void SetDataRaw(IntPtr l, int index, T val)
            {
                l.checkstack(3);
                l.pushvalue(index); // otab
                l.pushlightuserdata(LuaConst.LRKEY_TARGET); // otab #tar
                l.pushnumber(ConvertToNum(val)); // otab #tar val
                l.rawset(-3); // otab
                l.pop(1);
            }
            public T GetLuaRaw(IntPtr l, int index)
            {
                T rv;
                l.checkstack(2);
                l.pushvalue(index); // otab
                l.pushlightuserdata(LuaConst.LRKEY_TARGET); // otab #tar
                l.rawget(-2); // otab val
                rv = ConvertFromNum(l.tonumber(-1));
                l.pop(2); // X
                return rv;
            }
            public override T GetLuaChecked(IntPtr l, int index)
            {
                if (l.istable(index))
                {
                    return GetLuaRaw(l, index);
                }
                else if (l.IsNumber(index))
                {
                    return ConvertFromNum(l.tonumber(index));
                }
                else if (l.IsString(index))
                {
                    return EnumUtils.ConvertStrToEnum<T>(l.GetString(index));
                }
                return default(T);
            }

            public class TypeHubEnumNative : LuaHub.LuaPushNativeValueType<T>
            {
                protected TypeHubEnumPrecompiled<T> _Hub;
                public TypeHubEnumNative(TypeHubEnumPrecompiled<T> hub)
                {
                    _Hub = hub;
                }
                public override T GetLua(IntPtr l, int index)
                {
                    if (l.IsNumber(index))
                    {
                        return _Hub.ConvertFromNum(l.tonumber(index));
                    }
                    else if (l.IsString(index))
                    {
                        return EnumUtils.ConvertStrToEnum<T>(l.GetString(index));
                    }
                    return default(T);
                }
                public override IntPtr PushLua(IntPtr l, T val)
                {
                    l.pushnumber(_Hub.ConvertToNum(val));
                    return IntPtr.Zero;
                }
            }
            public readonly TypeHubEnumNative LuaHubNative;

            public void Wrap(IntPtr l, int index)
            {
                var val = GetLuaChecked(l, index);
                PushLua(l, val);
            }

            public void Unwrap(IntPtr l, int index)
            {
                var val = GetLuaChecked(l, index);
                l.pushnumber(ConvertToNum(val));
            }

            public int LuaType { get { return lua.LUA_TNUMBER; } }
        }

        public class TypeHubCreator<THubSub> where THubSub : TypeHubBase, new()
        {
            public TypeHubCreator(Type tOrigin)
            {
                RegTypeHubCreator(tOrigin, CreateTypeHubSub);
            }

            protected THubSub _TypeHubSub;
            protected object _Locker = new object();
            public THubSub TypeHubSub
            {
                get
                {
                    if (_TypeHubSub == null)
                    {
                        lock (_Locker)
                        {
                            if (_TypeHubSub == null)
                            {
                                _TypeHubSub = new THubSub();
                            }
                        }
                    }
                    return _TypeHubSub;
                }
            }
            protected TypeHubBase CreateTypeHubSub(Type type)
            {
                return TypeHubSub;
            }

            //public void PushLuaObject(IntPtr l, object val)
            //{
            //    TypeHubSub.PushLuaObject(l, val);
            //}
        }

        //public class TypeHubValueTypeCreator<TOrigin, THubSub>
        //    : TypeHubCreator<TOrigin, THubSub> where THubSub : TypeHubValueTypePrecompiled<TOrigin>, new()
        //{
        //    public void PushLua(IntPtr l, TOrigin val)
        //    {
        //        TypeHubSub.PushLua(l, val);
        //    }
        //}

        // TODO: delegate precompiled?
    }

    public static partial class LuaHub
    {
        static LuaHub()
        {
#if UNITY_EDITOR
            if (SafeInitializerUtils.CheckShouldDelay()) return;
#endif
#if (UNITY_ENGINE || UNITY_5_3_OR_NEWER)
            var asset = UnityEngine.Resources.Load<LuaPrecompileLoader>("LuaPrecompileLoaderEx");
            if (asset)
            {
                asset.Init();
            }
#else
            LuaHubEx.Init();
#endif
        }
    }
}
