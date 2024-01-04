using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using UnityEngineEx;
using LuaLib;

using lua = LuaLib.LuaCoreLib;
using lual = LuaLib.LuaAuxLib;
using luae = LuaLib.LuaLibEx;

namespace LuaLib
{
    public static partial class LuaExLibs
    {
        private static LuaExLibItem _LuaExLib_Assembly_Instance = new LuaExLibItem(Assembly2Lua.Init, -100);
    }

    public static class Assembly2Lua
    {
        public static void Init(IntPtr L)
        {
#if NETFX_CORE
            //// .NET Core
            _SearchAssemblies.Add(typeof(List<>).GetTypeInfo().Assembly);
            //_SearchAssemblies.Add(typeof(System.Collections.Concurrent.ConcurrentBag<>).GetTypeInfo().Assembly);
            //_SearchAssemblies.Add(typeof(System.Dynamic.DynamicObject).GetTypeInfo().Assembly);
            //_SearchAssemblies.Add(typeof(System.Globalization.Calendar).GetTypeInfo().Assembly);
            _SearchAssemblies.Add(typeof(System.IO.Stream).GetTypeInfo().Assembly);
            //_SearchAssemblies.Add(typeof(System.IO.Compression.ZipArchive).GetTypeInfo().Assembly);
            _SearchAssemblies.Add(typeof(System.Linq.Enumerable).GetTypeInfo().Assembly);
            _SearchAssemblies.Add(typeof(System.Linq.Expressions.Expression).GetTypeInfo().Assembly);
            //_SearchAssemblies.Add(typeof(System.Linq.ParallelEnumerable).GetTypeInfo().Assembly);
            //_SearchAssemblies.Add(typeof(System.Linq.Queryable).GetTypeInfo().Assembly);
            //_SearchAssemblies.Add(typeof(System.Net.Http.HttpClient).GetTypeInfo().Assembly);
            //_SearchAssemblies.Add(typeof(System.Net.Cookie).GetTypeInfo().Assembly);
            //_SearchAssemblies.Add(typeof(System.Net.WebRequest).GetTypeInfo().Assembly);
            //_SearchAssemblies.Add(typeof(Assembly).GetTypeInfo().Assembly);
            _SearchAssemblies.Add(typeof(int).GetTypeInfo().Assembly);
            _SearchAssemblies.Add(typeof(Random).GetTypeInfo().Assembly);
            //_SearchAssemblies.Add(typeof(Marshal).GetTypeInfo().Assembly);
            //_SearchAssemblies.Add(typeof(System.Numerics.Complex).GetTypeInfo().Assembly);
            //_SearchAssemblies.Add(typeof(System.Xml.XmlDictionary).GetTypeInfo().Assembly);
            //_SearchAssemblies.Add(typeof(Windows.UI.Color).GetTypeInfo().Assembly);
            //_SearchAssemblies.Add(typeof(Windows.UI.Xaml.Media.Media3D.Matrix3D).GetTypeInfo().Assembly);
            //_SearchAssemblies.Add(typeof(System.Text.Encoding).GetTypeInfo().Assembly);
            _SearchAssemblies.Add(typeof(System.Text.UTF8Encoding).GetTypeInfo().Assembly);
            _SearchAssemblies.Add(typeof(System.Text.RegularExpressions.Regex).GetTypeInfo().Assembly);
            _SearchAssemblies.Add(typeof(System.Threading.Interlocked).GetTypeInfo().Assembly);
            _SearchAssemblies.Add(typeof(System.Threading.Tasks.Task).GetTypeInfo().Assembly);
            //_SearchAssemblies.Add(typeof(System.Threading.Tasks.Parallel).GetTypeInfo().Assembly);
            _SearchAssemblies.Add(typeof(System.Threading.Timer).GetTypeInfo().Assembly);
            //_SearchAssemblies.Add(typeof(System.Xml.XmlReader).GetTypeInfo().Assembly);
            //_SearchAssemblies.Add(typeof(System.Xml.Linq.XNode).GetTypeInfo().Assembly);

            //// Unity Engine
            _SearchAssemblies.Add(typeof(UnityEngine.WWW).GetTypeInfo().Assembly);
            _SearchAssemblies.Add(typeof(UnityEngine.UI.Button).GetTypeInfo().Assembly);

            //// Self
            _SearchAssemblies.Add(typeof(Assembly2Lua).GetTypeInfo().Assembly);
#endif
            L.newtable(); // clr
            L.pushvalue(-1); // clr clr
            L.SetGlobal("clr"); // clr
            L.PushString(""); // clr ""
            L.SetField(-2, "___path"); // clr
            L.PushClrHierarchyMetatable(); // clr meta
            L.setmetatable(-2); // clr
            L.pushcfunction(ClrDelWrap); // clr func
            L.SetField(-2, "wrap"); // clr
            L.pushcfunction(ClrDelUnwrap); // clr func
            L.SetField(-2, "unwrap"); // clr
            L.pushcfunction(ClrDelConvert); // clr func
            L.SetField(-2, "as"); // clr
            L.pushcfunction(ClrDelIs); // clr func
            L.SetField(-2, "is"); // clr
            L.pushcfunction(ClrDelIsObj); // clr func
            L.SetField(-2, "isobj"); // clr
            L.pushcfunction(ClrDelType); // clr func
            L.SetField(-2, "type"); // clr
            L.pushcfunction(ClrDelExtend);
            L.SetField(-2, "extend");
            L.pushcfunction(ClrDelUnextend);
            L.SetField(-2, "unextend");
            L.pushcfunction(ClrDelArray); // clr func
            L.SetField(-2, "array"); // clr
            L.pushcfunction(ClrDelDict); // clr func
            L.SetField(-2, "dict"); // clr
#if !UNITY_ENGINE && !UNITY_5_3_OR_NEWER || NET_4_6 || NET_STANDARD_2_0
            L.pushcfunction(ClrDelTuple); // clr func
            L.SetField(-2, "tuple"); // clr
            L.pushcfunction(ClrDelUntuple); // clr func
            L.SetField(-2, "untuple"); // clr
#endif
            L.pushcfunction(ClrDelTable); // clr func
            L.SetField(-2, "table"); // clr
            L.pushcfunction(ClrDelNext); // clr func
            L.SetField(-2, "next"); // clr
            L.pushcfunction(ClrDelPairs); // clr func
            L.SetField(-2, "pairs"); // clr
            L.pushcfunction(ClrDelEx); // clr func
            L.SetField(-2, "ex"); // clr
            L.pushcfunction(ClrDelDisposeDelegate); // clr func
            L.SetField(-2, "closedel"); // clr
            L.pushcfunction(ClrDelGetMethodInfo); // clr func
            L.SetField(-2, "methodinfo"); // clr
            L.pushcfunction(ClrDelCreateDelForMethodInfo); // clr func
            L.SetField(-2, "methodfunc"); // clr
            L.PushLuaObject(null);
            L.SetField(-2, "null");
            L.pop(1);
        }

        internal static readonly lua.CFunction ClrFuncIndex = new lua.CFunction(ClrMetaIndex);
        internal static readonly lua.CFunction ClrDelWrap = new lua.CFunction(ClrFuncWrap);
        internal static readonly lua.CFunction ClrDelUnwrap = new lua.CFunction(ClrFuncUnwrap);
        internal static readonly lua.CFunction ClrDelConvert = new lua.CFunction(ClrFuncConvert);
        internal static readonly lua.CFunction ClrDelIs = new lua.CFunction(ClrFuncIs);
        internal static readonly lua.CFunction ClrDelIsObj = new lua.CFunction(ClrFuncIsObj);
        internal static readonly lua.CFunction ClrDelType = new lua.CFunction(ClrFuncType);
        internal static readonly lua.CFunction ClrDelExtend = new lua.CFunction(ClrFuncExtend);
        internal static readonly lua.CFunction ClrDelUnextend = new lua.CFunction(ClrFuncUnextend);
        internal static readonly lua.CFunction ClrDelArray = new lua.CFunction(ClrFuncArray);
        internal static readonly lua.CFunction ClrDelDict = new lua.CFunction(ClrFuncDict);
#if !UNITY_ENGINE && !UNITY_5_3_OR_NEWER || NET_4_6 || NET_STANDARD_2_0
        internal static readonly lua.CFunction ClrDelTuple = new lua.CFunction(ClrFuncTuple);
        internal static readonly lua.CFunction ClrDelUntuple = new lua.CFunction(ClrFuncUntuple);
#endif
        internal static readonly lua.CFunction ClrDelTable = new lua.CFunction(ClrFuncTable);
        internal static readonly lua.CFunction ClrDelNext = new lua.CFunction(ClrFuncNext);
        internal static readonly lua.CFunction ClrDelPairs = new lua.CFunction(ClrFuncPairs);
        internal static readonly lua.CFunction ClrDelEx = new lua.CFunction(ClrFuncEx);
        internal static readonly lua.CFunction ClrDelDisposeDelegate = new lua.CFunction(ClrFuncDisposeDelegate);
        internal static readonly lua.CFunction ClrDelGetMethodInfo = new lua.CFunction(ClrFuncGetMethodInfo);
        internal static readonly lua.CFunction ClrDelCreateDelForMethodInfo = new lua.CFunction(ClrFuncCreateDelForMethodInfo);

        public static void PushClrHierarchyMetatable(this IntPtr l)
        {
            if (l != IntPtr.Zero)
            {
                l.GetField(lua.LUA_REGISTRYINDEX, "___clrmeta");
                if (l.istable(-1))
                    return;
                l.pop(1);
                CreateClrHierarchyMetatable(l);
                l.pushvalue(-1);
                l.SetField(lua.LUA_REGISTRYINDEX, "___clrmeta");
            }
        }

        internal static void CreateClrHierarchyMetatable(this IntPtr l)
        {
            l.newtable(); // meta
            l.pushcfunction(ClrFuncIndex); // meta func
            l.SetField(-2, "__index"); // meta
        }

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrMetaIndex(IntPtr l)
        {
            // ... = tab key
            var oldtop = l.gettop();
            switch (0)
            {
                default:
                    if (oldtop < 2)
                        break;
                    if (!l.istable(1))
                        break;
                    if (!l.IsString(2))
                        break;
                    string key = l.GetString(2);
                    if (key == null)
                    {
                        key = "";
                    }
                    l.PushString("___path"); // ... "___path"
                    l.rawget(1);  // ... path
                    string path = l.GetString(-1);
                    if (path == null)
                    {
                        path = "";
                    }
                    string full = path + key;
                    l.pop(1); // ...

                    Type ftype = null;
                    List<Type> gtypes = new List<Type>(2);
#if NETFX_CORE
                    foreach (var asm in _SearchAssemblies)
#else
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
#endif
                    {
                        if (ftype == null)
                        {
                            try
                            {
#if NETFX_CORE
                                ftype = asm.GetType(full);
#else
                                ftype = asm.GetType(full, false);
#endif
                            }
                            catch { }
                        }
                        for(int i = 0; i < _MaxGenericParamCount; ++i)
                        {
                            try
                            {
#if NETFX_CORE
                                var gtype = asm.GetType(full + "`" + i.ToString());
#else
                                var gtype = asm.GetType(full + "`" + i.ToString(), false);
#endif
                                if (gtype != null)
                                {
                                    gtypes.Add(gtype);
                                }
                            }
                            catch { }
                        }
                    }

                    if (ftype == null && gtypes.Count <= 0)
                    {
                        l.newtable(); // ... ntab
                        l.pushvalue(2); // ... ntab key
                        l.pushvalue(-2); // ... ntab key ntab
                        l.settable(1); // ... ntab
                        l.PushString(full + "."); // ... ntab npath
                        l.SetField(-2, "___path"); // ... ntab
                        l.PushClrHierarchyMetatable(); // ... ntab meta
                        l.setmetatable(-2); // ... ntab
                    }
                    else
                    {
                        var typehub = LuaTypeHub.GetTypeHub(ftype);
                        foreach (var gtype in gtypes)
                        {
                            typehub.RegGenericTypeDefinition(gtype);
                        }
                        l.PushLuaType(typehub); // ... type
                        if (l.IsExtended())
                        {
                            LuaExtend.MakeLuaExtend(l, -1);
                        }
                        l.pushvalue(2); // ... type key
                        l.pushvalue(-2); // ... type key type
                        l.settable(1); // ... type
                    }
                    break;
            }

            return l.gettop() - oldtop;
        }

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncWrap(IntPtr l)
        {
            var type = l.GetType(1);
            ILuaTypeHub sub = LuaTypeHub.GetTypeHub(type);
            ILuaNative nsub = sub as ILuaNative;
            if (nsub == null)
            {
                l.PushLuaObject(l.GetLua(1));
                return 1;
            }
            else
            {
                nsub.Wrap(l, 1);
                return 1;
            }
        }

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncUnwrap(IntPtr l)
        {
            if (l.istable(1))
            {
                l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS); // #trans
                l.gettable(1); // trans
                ILuaTrans trans = null;
                if (l.isuserdata(-1))
                {
                    trans = l.GetLuaLightObject(-1) as ILuaTrans;
                }
                l.pop(1);
                ILuaNative nsub = trans as ILuaNative;
                if (nsub != null)
                {
                    nsub.Unwrap(l, 1);
                    return 1;
                }
            }
            l.PushLua(l.GetLua(1));
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncConvert(IntPtr l)
        {
            try
            {
                bool srcIsObj;
                var stype = l.GetType(1, out srcIsObj);
                Type dtype;
                l.GetLua(2, out dtype);
                if (dtype == null)
                {
                    return ClrFuncWrap(l);
                }
                else
                {
                    if (stype == dtype)
                    {
                        if (srcIsObj)
                        {
                            l.pushvalue(1);
                            return 1;
                        }
                        else
                        {
                            return ClrFuncWrap(l);
                        }
                    }
                    else if (stype == null)
                    {
                        var nndtype = Nullable.GetUnderlyingType(dtype);
                        if (nndtype != null)
                        {
                            l.pushnil();
                        }
                        else
                        {
                            l.PushLuaObject(null, dtype);
                        }
                        return 1;
                    }
                    else
                    {
                        var nnstype = Nullable.GetUnderlyingType(stype);
                        var nndtype = Nullable.GetUnderlyingType(dtype);
                        stype = nnstype ?? stype;
                        dtype = nndtype ?? dtype;

                        ILuaTypeHub sub = LuaTypeHub.GetTypeHub(stype);
                        ILuaConvert nsub = sub as ILuaConvert;
                        if (nsub != null)
                        {
                            var meta = nsub.GetConverter(dtype);
                            if (meta != null)
                            {
                                return meta(l, 1);
                            }
                        }
                        ILuaTypeHub dsub = LuaTypeHub.GetTypeHub(dtype);
                        nsub = dsub as ILuaConvert;
                        if (nsub != null)
                        {
                            var meta = nsub.GetConverterFrom(stype);
                            if (meta != null)
                            {
                                return meta(l, 1);
                            }
                        }

                        { // NOTICE: if beblow makes bugs, restrict below to enum <-> number
                            ILuaNative snative = sub as ILuaNative;
                            if (snative != null)
                            {
                                ILuaNative dnative = dsub as ILuaNative;
                                if (dnative != null)
                                {
                                    if (snative.LuaType == dnative.LuaType)
                                    {
                                        if (l.IsObject(1))
                                        {
                                            snative.Unwrap(l, 1);
                                            dnative.Wrap(l, -1);
                                            l.remove(-2);
                                            return 1;
                                        }
                                        else
                                        {
                                            dnative.Wrap(l, 1);
                                            return 1;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                var val = l.GetLua(1);
                l.PushLuaObject(val.ConvertTypeEx(dtype), dtype);
                return 1;
            }
            catch (Exception e)
            {
                l.LogError(e);
                return 0;
            }
        }

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncIs(IntPtr l)
        {
            var stype = l.GetType(1);
            Type dtype;
            l.GetLua(2, out dtype);

            if (dtype == null || stype == null)
            {
                if (dtype == null && stype == null)
                {
                    l.pushboolean(true);
                    return 1;
                }
                l.pushboolean(false);
                return 1;
            }
            bool rv = dtype.IsAssignableFrom(stype);
            l.pushboolean(rv);
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncIsObj(IntPtr l)
        {
            var rv = l.IsObject(1);
            l.pushboolean(rv);
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncType(IntPtr l)
        {
            var type = l.GetType(1);
            l.PushLua(type);
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncExtend(IntPtr l)
        {
            l.pushvalue(1);
            LuaExtend.MakeLuaExtend(l, -1);
            return 1;
        }
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncUnextend(IntPtr l)
        {
            l.pushvalue(1);
            LuaExtend.MakeLuaUnextend(l, -1);
            return 1;
        }

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncArray(IntPtr l)
        {
            if (l.istable(1))
            {
                if (l.GetType(1) == typeof(Type))
                {
                    var t = l.GetLua<Type>(1);
                    int rank = 1;
                    if (l.IsNumber(2))
                    {
                        l.GetLua(2, out rank);
                    }
                    if (rank < 1)
                    {
                        rank = 1;
                    }
                    Type arrt;
                    if (rank == 1)
                    {
                        arrt = t.MakeArrayType();
                    }
                    else
                    {
                        arrt = t.MakeArrayType(rank);
                    }
                    l.PushLua(arrt);
                    return 1;
                }
                else
                {
                    Array arr = null;
                    using (var lr = new LuaStateRecover(l))
                    {
                        var len = l.getn(1);
                        var otype = l.GetLua<Type>(2);
                        if (otype == null)
                        {
                            // TODO: guess type
                            otype = typeof(object);
                        }
                        arr = Array.CreateInstance(otype, len);
                        for (int i = 0; i < len; ++i)
                        {
                            l.pushnumber(i + 1);
                            l.gettable(1);
                            arr.SetValue(l.GetLua(-1).ConvertType(otype), i);
                            l.pop(1);
                        }
                    }
                    l.PushLuaObject(arr);
                    return 1;
                }
            }
            else if (l.IsString(1))
            {
                var bytes = l.tolstring(1);
                l.PushLuaObject(bytes);
                return 1;
            }
            else if (l.IsNumber(1))
            {
                int len = (int)l.tonumber(1);
                if (len < 0)
                {
                    len = 0;
                }
                Type otype = l.GetLua<Type>(2);
                if (otype == null)
                {
                    otype = typeof(object);
                }
                Array arr = Array.CreateInstance(otype, len);
                l.PushLuaObject(arr);
                return 1;
            }
            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncDict(IntPtr l)
        {
            if (l.istable(1))
            {
                IDictionary dict = null;
                using (var lr = new LuaStateRecover(l))
                {
                    var ktype = l.GetLua<Type>(2);
                    var vtype = l.GetLua<Type>(3);
                    if (ktype == null)
                    {
                        ktype = typeof(object);
                    }
                    if (vtype == null)
                    {
                        vtype = typeof(object);
                    }
                    dict = Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(ktype, vtype)) as IDictionary;
                    //dict = typeof(Dictionary<,>).MakeGenericType(ktype, vtype).GetConstructor(new Type[0]).Invoke(null) as IDictionary;
                    l.pushnil();
                    while (l.next(1))
                    {
                        object key = l.GetLua(-2);
                        object val = l.GetLua(-1);
                        dict.Add(key.ConvertType(ktype), val.ConvertType(vtype));
                        l.pop(1);
                    }
                    //l.pop(1);
                }
                l.PushLuaObject(dict);
                return 1;
            }
            return 0;
        }

#if !UNITY_ENGINE && !UNITY_5_3_OR_NEWER || NET_4_6 || NET_STANDARD_2_0
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncTuple(IntPtr l)
        {
            if (l.istable(1))
            {
                object rv;
                var argcnt = l.gettop();
                var tuplelen = l.getn(1);
                if (tuplelen <= 0)
                {
                    rv = new ValueTuple();
                }
                else
                {
                    Type[] types = new Type[tuplelen];
                    object[] values = new object[tuplelen];
                    for (int i = 0; i < tuplelen; ++i)
                    {
                        l.pushnumber(i + 1);
                        l.rawget(1);
                        object value = l.GetLua(-1);
                        l.pop(1);

                        Type type = null;
                        if (value == null)
                        {
                            type = typeof(object);
                        }
                        else
                        {
                            type = value.GetType();
                        }

                        types[i] = type;
                        values[i] = value;
                    }
                    if (l.istable(2))
                    {
                        for (int i = 0; i < tuplelen; ++i)
                        {
                            l.pushnumber(i + 1);
                            l.rawget(2);
                            Type type;
                            l.GetLua(-1, out type);
                            l.pop(1);
                            if (type != null)
                            {
                                types[i] = type;
                                values[i] = values[i].ConvertTypeRaw(type);
                            }
                        }
                    }
                    bool isValueTuple = true;
                    if (!l.isnoneornil(3))
                    {
                        l.GetLua(3, out isValueTuple);
                    }
                    rv = LuaLib.LuaTupleUtils.CreateTuple(types, values, isValueTuple);
                }
                l.PushLuaObject(rv);
                return 1;
            }
            return 0;
        }
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncUntuple(IntPtr l)
        {
            var o = l.GetLua(1);
            return LuaLib.LuaTupleUtils.PushValueOrTuple(l, o);
        }
#endif

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncTable(IntPtr l)
        {
            if (l.IsObject(1))
            {
                var obj = l.GetLua(1);
                var lobj = obj.ConvertType(typeof(IList)) as IList;
                if (lobj != null)
                {
                    l.newtable();
                    for (int i = 0; i < lobj.Count; ++i)
                    {
                        l.pushnumber(i + 1);
                        l.PushLua(lobj[i]);
                        l.settable(-3);
                    }
                    return 1;
                }
                var dobj = obj.ConvertType(typeof(IDictionary)) as IDictionary;
                if (dobj != null)
                {
                    l.newtable();
                    var enumerator = dobj.GetEnumerator();
                    if (enumerator != null)
                    {
                        while (enumerator.MoveNext())
                        {
                            l.PushLua(enumerator.Key);
                            l.PushLua(enumerator.Value);
                            l.settable(-3);
                        }
                    }
                    return 1;
                }
                var eobj = obj.ConvertType<IEnumerable>();
                if (eobj != null)
                {
                    l.newtable();
                    int cnt = 0;
                    foreach (var item in eobj)
                    {
                        l.pushnumber(++cnt);
                        l.PushLua(item);
                        l.settable(-3);
                    }
                    return 1;
                }
                //if (l.isuserdata(1))
                //{
                //    l.getfenv(1); // ud, ex
                //    return 1;
                //}
            }
            else if (l.istable(1))
            {
                l.pushvalue(1);
                return 1;
            }
            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncNext(IntPtr l)
        {
            var list = l.GetLua<IList>(1);
            if (list != null)
            {
                int key = -1;
                if (l.isnumber(2))
                {
                    l.GetLua(2, out key);
                }
                ++key;
                if (key >= 0 && key < list.Count)
                {
                    l.pushnumber(key);
                    l.PushLua(list[key]);
                    return 2;
                }
                else
                {
                    return 0;
                }
            }
            var dict = l.GetLua<IDictionary>(1);
            if (dict != null)
            {
                var etor = l.GetLua<IDictionaryEnumerator>(lua.upvalueindex(1));
                if (etor == null || l.isnoneornil(2))
                {
                    etor = dict.GetEnumerator();
                    l.PushLua(etor);
                    l.replace(lua.upvalueindex(1));
                }
                if (etor.MoveNext())
                {
                    l.PushLua(etor.Key);
                    l.PushLua(etor.Value);
                    return 2;
                }
                else
                {
                    return 0;
                }    
            }
            var col = l.GetLua<IEnumerable>(1);
            if (col != null)
            {
                int key = -1;
                if (l.isnumber(2))
                {
                    l.GetLua(2, out key);
                }
                ++key;
                var etor = l.GetLua<IEnumerator>(lua.upvalueindex(1));
                if (etor == null || l.isnoneornil(2))
                {
                    etor = col.GetEnumerator();
                    l.PushLua(etor);
                    l.replace(lua.upvalueindex(1));
                }
                if (etor.MoveNext())
                {
                    l.pushnumber(key);
                    l.PushLua(etor.Current);
                    return 2;
                }
                else
                {
                    return 0;
                }
            }
            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncPairs(IntPtr l)
        {
            var list = l.GetLua<IList>(1);
            if (list != null)
            {
                l.pushcfunction(ClrDelNext);
                l.pushvalue(1);
                l.pushnil();
                return 3;
            }
            var dict = l.GetLua<IDictionary>(1);
            if (dict != null)
            {
                l.pushnil();
                l.pushcclosure(ClrDelNext, 1);
                l.pushvalue(1);
                l.pushnil();
                return 3;
            }
            var col = l.GetLua<IEnumerable>(1);
            if (col != null)
            {
                l.pushnil();
                l.pushcclosure(ClrDelNext, 1);
                l.pushvalue(1);
                l.pushnil();
                return 3;
            }
            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncEx(IntPtr l)
        {
            //l.getfenv(1);
            //return 1;
            l.LogError("clr.ex is obsoleted.");
            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncDisposeDelegate(IntPtr l)
        {
            var arg = l.GetLua(1);
            var func = arg as BaseLua;
            if (!ReferenceEquals(func, null))
            {
                LuaDelegateGenerator.DisposeDelegate(func);
            }
            else
            {
                var del = arg as Delegate;
                if (!ReferenceEquals(del, null))
                {
                    LuaDelegateGenerator.DisposeDelegate(del);
                }
            }
            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncGetMethodInfo(IntPtr l)
        {
            if (l.istable(1))
            {
                l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS); // #trans
                l.rawget(1); // trans
                if (l.islightuserdata(-1))
                {
                    var trans = l.GetLuaLightObject(-1);
                    l.pop(1);
                    var methodmeta = trans as BaseMethodMeta;
                    if (methodmeta != null)
                    {
                        return ClrFuncGetMethodInfoOfMethod(l, methodmeta);
                    }

                    if (l.GetType(1) == typeof(Type))
                    {
                        if (trans == LuaExtend.LuaTransExtend.Instance)
                        {
                            l.PushString(LuaConst.LS_SP_KEY_NONPUBLIC);
                            l.gettable(1);
                            if (l.isboolean(-1))
                            {
                                var isnpub = l.toboolean(-1);
                                l.pop(1);
                                if (isnpub)
                                {
                                    Type type;
                                    l.GetLua(1, out type);
                                    return ClrFuncGetMethodInfoOfNonPublicCtor(l, type);
                                }
                            }
                            else
                            {
                                l.pop(1);
                            }
                        }

                        {
                            Type type;
                            l.GetLua(1, out type);
                            return ClrFuncGetMethodInfoOfPublicCtor(l, type);
                        }
                    }
                }
                else
                {
                    l.pop(1);
                }
            }
            return 0;
        }
        internal static int ClrFuncGetMethodInfoOfMethod(IntPtr l, BaseMethodMeta meta)
        {
            var uniquemeta = meta as BaseUniqueMethodMeta;
            if (uniquemeta != null)
            {
                var method = uniquemeta.Method;
                l.PushLuaObject(method);
                return 1;
            }
            var overloadedmeta = meta as BaseOverloadedMethodMeta;
            if (overloadedmeta != null)
            {
                var index = 2;
                var top = l.gettop();
                Types types = new Types();
                for (; index <= top; ++index)
                {
                    Type t;
                    l.GetLua(index, out t);
                    types.Add(t);
                }

                uniquemeta = overloadedmeta.FindAppropriate(types);
                if (uniquemeta != null)
                {
                    var method = uniquemeta.Method;
                    l.PushLuaObject(method);
                    return 1;
                }
            }
            return 0;
        }
        internal static int ClrFuncGetMethodInfoOfNonPublicCtor(IntPtr l, Type t)
        {
            if (t == null)
            {
                return 0;
            }
            var index = 2;
            var top = l.gettop();
            Types types = new Types();
            for (; index <= top; ++index)
            {
                Type argt;
                l.GetLua(index, out argt);
                types.Add(argt);
            }
            var uniquemeta = LuaTypeNonPublicReflector.FindNonPublicCtor(t, types);
            if (uniquemeta != null)
            {
                var method = uniquemeta.Method;
                l.PushLuaObject(method);
                return 1;
            }
            return 0;
        }
        internal static int ClrFuncGetMethodInfoOfPublicCtor(IntPtr l, Type t)
        {
            if (t == null)
            {
                return 0;
            }
            //var index = 2;
            //var top = l.gettop();
            //Types types = new Types();
            //for (; index <= top; ++index)
            //{
            //    Type argt;
            //    l.GetLua(index, out argt);
            //    types.Add(argt);
            //}
            var hub = LuaTypeHub.GetTypeHub(t);
            if (hub != null)
            {
                var ctor = hub.Ctor;
                var meta = ctor._Method as CtorMethodMeta;
                if (meta != null)
                {
                    //if (types.Count <= 1 && t.IsValueType()) // TODO: the default ctor of value-type can not be gotten
                    //{

                    //}
                    var uniquemeta = meta.NormalCtor as BaseUniqueMethodMeta;
                    if (uniquemeta != null)
                    {
                        var method = uniquemeta.Method;
                        l.PushLuaObject(method);
                        return 1;
                    }
                    var overloadedmeta = meta.NormalCtor as BaseOverloadedMethodMeta;
                    if (overloadedmeta != null)
                    {
                        var index = 2;
                        var top = l.gettop();
                        Types types = new Types();
                        for (; index <= top; ++index)
                        {
                            Type argt;
                            l.GetLua(index, out argt);
                            types.Add(argt);
                        }

                        uniquemeta = overloadedmeta.FindAppropriate(types);
                        if (uniquemeta != null)
                        {
                            var method = uniquemeta.Method;
                            l.PushLuaObject(method);
                            return 1;
                        }
                    }
                }
            }
            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ClrFuncCreateDelForMethodInfo(IntPtr l)
        {
            MethodInfo mi;
            Type deltype;
            object target;
            l.GetLua(1, out mi);
            l.GetLua(2, out deltype);
            l.GetLua(3, out target);
            if (mi != null && deltype != null)
            {
                var del = mi.CreateDelegate(deltype, target);
                l.PushLua(del);
                return 1;
            }
            return 0;
        }

        internal const int _MaxGenericParamCount = 5;

#if NETFX_CORE
        internal static HashSet<Assembly> _SearchAssemblies = new HashSet<Assembly>();
#endif
    }
}