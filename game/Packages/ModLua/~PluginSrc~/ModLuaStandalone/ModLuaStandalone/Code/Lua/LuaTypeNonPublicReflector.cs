using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngineEx;

using lua = LuaLib.LuaCoreLib;
using lual = LuaLib.LuaAuxLib;
using luae = LuaLib.LuaLibEx;

namespace LuaLib
{
    public static class LuaTypeNonPublicReflector
    {
        private static readonly lua.CFunction LuaFuncInstanceIndex = new lua.CFunction(LuaMetaInstanceIndex);
        private static readonly lua.CFunction LuaFuncInstanceNewIndex = new lua.CFunction(LuaMetaInstanceNewIndex);
        private static readonly lua.CFunction LuaFuncStaticIndex = new lua.CFunction(LuaMetaStaticIndex);
        private static readonly lua.CFunction LuaFuncStaticNewIndex = new lua.CFunction(LuaMetaStaticNewIndex);
        private static readonly lua.CFunction LuaFuncStaticCall = new lua.CFunction(LuaMetaStaticCall);
        public static readonly lua.CFunction LuaFuncCreateInstanceReflector = new lua.CFunction(LuaMetaCreateInstanceReflector);
        public static readonly lua.CFunction LuaFuncCreateStaticReflector = new lua.CFunction(LuaMetaCreateStaticReflector);
        private static readonly lua.CFunction LuaFuncReflectorlIndex = new lua.CFunction(LuaMetaReflectorlIndex);
        public static readonly lua.CFunction LuaFuncCreateReflector = new lua.CFunction(LuaMetaCreateReflector);

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        private static int LuaMetaInstanceIndex(IntPtr l)
        {
            // Try get from cache.
            l.pushlightuserdata(LuaConst.LRKEY_GETTER); // #getter
            l.rawget(1); // getter
            if (l.istable(-1))
            {
                l.pushvalue(2); // getter name
                l.gettable(-2); // getter getterinfo
                var lt = l.type(-1);
                if (lt == lua.LUA_TBOOLEAN)
                { // when lt is boolean, the value is always false, which means null. 
                    l.pop(2);
                    return 0;
                }
                else if (lt == lua.LUA_TNONE || lt == lua.LUA_TNIL)
                { // non-cached
                    l.pop(2); // X
                }
                else
                {
                    var getterinfo = l.GetLua(-1);
                    var fi = getterinfo as FieldInfo;
                    if (fi != null)
                    {
                        l.pop(2); // X
                        l.pushlightuserdata(LuaConst.LRKEY_TARGET); // #tar
                        l.rawget(1); // tar
                        var target = l.GetLua(-1);
                        l.pop(1); // X

                        if (target == null)
                        {
                            l.LogError("Must provide a instance to get " + fi.ToString());
                            return 0;
                        }
                        else
                        {
                            object result = null;
                            try
                            {
                                result = fi.GetValue(target);
                            }
                            catch (Exception ex)
                            {
                                l.LogError(ex);
                                return 0;
                            }
                            l.PushLua(result);
                            return 1;
                        }
                    }
                    var pi = getterinfo as PropertyInfo;
                    if (pi != null)
                    {
                        l.pop(2); // X
                        l.pushlightuserdata(LuaConst.LRKEY_TARGET); // #tar
                        l.rawget(1); // tar
                        var target = l.GetLua(-1);
                        l.pop(1); // X

                        if (target == null)
                        {
                            l.LogError("Must provide a instance to get " + pi.ToString());
                            return 0;
                        }
                        else
                        {
                            object result = null;
                            try
                            {
                                result = pi.GetValue(target);
                            }
                            catch (Exception ex)
                            {
                                l.LogError(ex);
                                return 0;
                            }
                            l.PushLua(result);
                            return 1;
                        }
                    }

                    l.remove(-2); // getterinfo(func)
                    // cache it to npub-table
                    l.pushvalue(2); // func name
                    l.pushvalue(-2); // func name func
                    l.rawset(1); // func
                    return 1;
                }
            }
            else
            {
                l.pop(1); // X
                l.pushlightuserdata(LuaConst.LRKEY_GETTER); // #getter
                l.newtable(); // #getter getter
                l.rawset(1); // X
            }

            l.pushlightuserdata(LuaConst.LRKEY_TARGET); // #tar
            l.rawget(1); // tar
            var tar = l.GetLua(-1);
            Type type = l.GetType(-1);
            l.pop(1); // X
            string name;
            l.GetLua(2, out name);
            if (string.IsNullOrEmpty(name))
            {
                return 0;
            }

            var searchingType = type;
            MemberInfo[] members = null;
            while ((members == null || members.Length == 0) && searchingType != null)
            {
                members = searchingType.GetMember(name, BindingFlags.Instance | BindingFlags.NonPublic);
                searchingType = searchingType.BaseType;
            }
            if (members != null && members.Length > 0)
            {
                switch (members[0].MemberType)
                {
                    case MemberTypes.Field:
                        {
                            var fi = members[0] as FieldInfo;
                            // cache it!
                            l.pushlightuserdata(LuaConst.LRKEY_GETTER); // #getter
                            l.rawget(1); // getter
                            l.pushvalue(2); // getter name
                            l.PushLuaObject(fi); // getter name fi
                            l.settable(-3); // getter
                            bool updateDataAfterCall = type.IsValueType;
                            l.pushlightuserdata(LuaConst.LRKEY_SETTER); // getter #setter
                            l.pushboolean(updateDataAfterCall); // getter #setter updateDataAfterCall
                            l.rawset(-3); // getter
                            l.pop(1); // X

                            if (tar == null)
                            {
                                l.LogError("Must provide a instance to get " + fi.ToString());
                                return 0;
                            }
                            object result = null;
                            try
                            {
                                result = fi.GetValue(tar);
                            }
                            catch (Exception ex)
                            {
                                l.LogError(ex);
                                return 0;
                            }
                            l.PushLua(result);
                        }
                        return 1;
                    case MemberTypes.Property:
                        {
                            var pi = members[0] as PropertyInfo;
                            // cache it!
                            l.pushlightuserdata(LuaConst.LRKEY_GETTER); // #getter
                            l.rawget(1); // getter
                            l.pushvalue(2); // getter name
                            l.PushLuaObject(pi); // getter name pi
                            l.settable(-3); // getter
                            bool updateDataAfterCall = type.IsValueType;
                            l.pushlightuserdata(LuaConst.LRKEY_SETTER); // getter #setter
                            l.pushboolean(updateDataAfterCall); // getter #setter updateDataAfterCall
                            l.rawset(-3); // getter
                            l.pop(1); // X

                            if (tar == null)
                            {
                                l.LogError("Must provide a instance to get " + pi.ToString());
                                return 0;
                            }
                            object result = null;
                            try
                            {
                                result = pi.GetValue(tar);
                            }
                            catch (Exception ex)
                            {
                                l.LogError(ex);
                                return 0;
                            }
                            l.PushLua(result);
                        }
                        return 1;
                    case MemberTypes.Method:
                        {
                            List<MethodBase> fmethods = new List<MethodBase>();
                            List<MethodBase> gmethods = new List<MethodBase>();
                            for (int i = 0; i < members.Length; ++i)
                            {
                                var method = members[i] as MethodInfo;
                                if (method.ContainsGenericParameters)
                                {
                                    gmethods.Add(method);
                                }
                                else
                                {
                                    fmethods.Add(method);
                                }
                            }
                            var meta = GenericMethodMeta.CreateMethodMeta(fmethods.ToArray(), gmethods.ToArray(), type.IsValueType);
                            l.PushFunction(meta);
                            meta.WrapFunctionByTable(l);
                        }
                        // cache it
                        l.pushlightuserdata(LuaConst.LRKEY_GETTER); // func #getter
                        l.rawget(1); // func getter
                        l.pushvalue(2); // func getter name
                        l.pushvalue(-3); // func getter name func
                        l.settable(-3); // func getter
                        l.pop(1); // func
                        // cache it to npub-table
                        l.pushvalue(2); // func name
                        l.pushvalue(-2); // func name func
                        l.rawset(1); // func
                        return 1;
                    case MemberTypes.Event: // TODO: events?
                    default:
                        break;
                }
            }
            // cache it!
            l.pushlightuserdata(LuaConst.LRKEY_GETTER); // #getter
            l.rawget(1); // getter
            l.pushvalue(2); // getter name
            l.pushboolean(false); // getter name false
            l.settable(-3); // getter
            l.pop(1); // X
            return 0;
        }
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        private static int LuaMetaInstanceNewIndex(IntPtr l)
        {
            // Try get from cache.
            l.pushlightuserdata(LuaConst.LRKEY_GETTER); // #getter
            l.rawget(1); // getter
            if (l.istable(-1))
            {
                l.pushvalue(2); // getter name
                l.gettable(-2); // getter getterinfo
                var lt = l.type(-1);
                if (lt == lua.LUA_TBOOLEAN)
                { // when lt is boolean, the value is always false, which means null. 
                    l.pop(2);
                    l.LogError("Cannot set value on non-public-reflector: not found");
                    return 0;
                }
                else if (lt == lua.LUA_TNONE || lt == lua.LUA_TNIL)
                { // non-cached
                    l.pop(2); // X
                }
                else
                {
                    bool updateDataAfterCall;
                    l.pushlightuserdata(LuaConst.LRKEY_SETTER); // getter getterinfo #setter
                    l.rawget(-3); // getter getterinfo updateDataAfterCall
                    l.GetLua(-1, out updateDataAfterCall);
                    var getterinfo = l.GetLua(-2);
                    l.pop(3); // X
                    var fi = getterinfo as FieldInfo;
                    if (fi != null)
                    {
                        l.pushlightuserdata(LuaConst.LRKEY_TARGET); // #tar
                        l.rawget(1); // tar
                        var target = l.GetLua(-1);

                        if (target == null)
                        {
                            l.LogError("Must provide a instance to set " + fi.ToString());
                            l.pop(1); // X
                            return 0;
                        }
                        else
                        {
                            var value = l.GetLua(3);
                            try
                            {
                                fi.SetValue(target, value.ConvertTypeRaw(fi.FieldType));
                                if (updateDataAfterCall)
                                {
                                    l.UpdateData(-1, target);
                                }
                            }
                            catch (Exception e)
                            {
                                l.LogError(e);
                            }
                            l.pop(1); // X
                            return 0;
                        }
                    }
                    var pi = getterinfo as PropertyInfo;
                    if (pi != null)
                    {
                        l.pushlightuserdata(LuaConst.LRKEY_TARGET); // #tar
                        l.rawget(1); // tar
                        var target = l.GetLua(-1);

                        if (target == null)
                        {
                            l.LogError("Must provide a instance to set " + pi.ToString());
                            l.pop(1); // X
                            return 0;
                        }
                        else
                        {
                            var value = l.GetLua(3);
                            try
                            {
                                pi.SetValue(target, value.ConvertTypeRaw(pi.PropertyType));
                                if (updateDataAfterCall)
                                {
                                    l.UpdateData(-1, target);
                                }
                            }
                            catch (Exception e)
                            {
                                l.LogError(e);
                            }
                            l.pop(1); // X
                            return 0;
                        }
                    }

                    // TODO: sync the getterinfo(method) to the npub-table(at index 1).
                    l.LogError("Cannot overwrite method on non-public reflector.");
                    return 0;
                }
            }
            else
            {
                l.pop(1); // X
                l.pushlightuserdata(LuaConst.LRKEY_GETTER); // #getter
                l.newtable(); // #getter getter
                l.rawset(1); // X
            }

            string name;
            l.GetLua(2, out name);
            if (string.IsNullOrEmpty(name))
            {
                return 0;
            }
            l.pushlightuserdata(LuaConst.LRKEY_TARGET); // #tar
            l.rawget(1); // tar
            var tar = l.GetLua(-1);
            if (tar == null)
            {
                l.LogError("Must provide a instance to set " + name);
                l.pop(1); // X
                return 0;
            }
            var type = tar.GetType();
            var val = l.GetLua(3);
            var searchingType = type;
            MemberInfo[] members = null;
            while ((members == null || members.Length == 0) && searchingType != null)
            {
                members = searchingType.GetMember(name, BindingFlags.Instance | BindingFlags.NonPublic);
                searchingType = searchingType.BaseType;
            }
            if (members != null && members.Length > 0)
            {
                switch (members[0].MemberType)
                {
                    case MemberTypes.Field:
                        {
                            var fi = members[0] as FieldInfo;
                            // cache it!
                            l.pushlightuserdata(LuaConst.LRKEY_GETTER); // #getter
                            l.rawget(1); // getter
                            l.pushvalue(2); // getter name
                            l.PushLuaObject(fi); // getter name fi
                            l.settable(-3); // getter
                            bool updateDataAfterCall = type.IsValueType;
                            l.pushlightuserdata(LuaConst.LRKEY_SETTER); // getter #setter
                            l.pushboolean(updateDataAfterCall); // getter #setter updateDataAfterCall
                            l.rawset(-3); // getter
                            l.pop(1); // X

                            try
                            {
                                fi.SetValue(tar, val.ConvertTypeRaw(fi.FieldType));
                                if (updateDataAfterCall)
                                {
                                    l.UpdateData(-1, tar);
                                }
                            }
                            catch (Exception ex)
                            {
                                l.LogError(ex);
                            }
                            l.pop(1);
                        }
                        return 0;
                    case MemberTypes.Property:
                        {
                            var pi = members[0] as PropertyInfo;
                            // cache it!
                            l.pushlightuserdata(LuaConst.LRKEY_GETTER); // #getter
                            l.rawget(1); // getter
                            l.pushvalue(2); // getter name
                            l.PushLuaObject(pi); // getter name pi
                            l.settable(-3); // getter
                            bool updateDataAfterCall = type.IsValueType;
                            l.pushlightuserdata(LuaConst.LRKEY_SETTER); // getter #setter
                            l.pushboolean(updateDataAfterCall); // getter #setter updateDataAfterCall
                            l.rawset(-3); // getter
                            l.pop(1); // X

                            try
                            {
                                pi.SetValue(tar, val.ConvertTypeRaw(pi.PropertyType));
                                if (updateDataAfterCall)
                                {
                                    l.UpdateData(-1, tar);
                                }
                            }
                            catch (Exception ex)
                            {
                                l.LogError(ex);
                            }
                            l.pop(1);
                        }
                        return 0;
                    case MemberTypes.Method:
                        {
                            List<MethodBase> fmethods = new List<MethodBase>();
                            List<MethodBase> gmethods = new List<MethodBase>();
                            for (int i = 0; i < members.Length; ++i)
                            {
                                var method = members[i] as MethodInfo;
                                if (method.ContainsGenericParameters)
                                {
                                    gmethods.Add(method);
                                }
                                else
                                {
                                    fmethods.Add(method);
                                }
                            }
                            var meta = GenericMethodMeta.CreateMethodMeta(fmethods.ToArray(), gmethods.ToArray(), type.IsValueType);
                            l.PushFunction(meta);
                            meta.WrapFunctionByTable(l);
                        }
                        // cache it
                        l.pushlightuserdata(LuaConst.LRKEY_GETTER); // func #getter
                        l.rawget(1); // func getter
                        l.pushvalue(2); // func getter name
                        l.pushvalue(-3); // func getter name func
                        l.settable(-3); // func getter
                        l.pop(1); // func
                        // cache it to npub-table
                        l.pushvalue(2); // func name
                        l.pushvalue(-2); // func name func
                        l.rawset(1); // func
                        l.pop(1); // X
                        l.LogError("Cannot overwrite method on non-public reflector: " + name);
                        return 0;
                    case MemberTypes.Event: // TODO: events?
                        l.pop(1);
                        l.LogError("Cannot overwrite event on non-public reflector: " + name);
                        break;
                    default:
                        l.pop(1);
                        l.LogError("Cannot set value on non-public-reflector: not found: " + name);
                        break;
                }
            }
            // cache it!
            l.pushlightuserdata(LuaConst.LRKEY_GETTER); // #getter
            l.rawget(1); // getter
            l.pushvalue(2); // getter name
            l.pushboolean(false); // getter name false
            l.settable(-3); // getter
            l.pop(1); // X
            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        private static int LuaMetaStaticIndex(IntPtr l)
        {
            // Try get from cache.
            l.pushlightuserdata(LuaConst.LRKEY_GETTER); // #getter
            l.rawget(1); // getter
            if (l.istable(-1))
            {
                l.pushvalue(2); // getter name
                l.gettable(-2); // getter getterinfo
                var lt = l.type(-1);
                if (lt == lua.LUA_TBOOLEAN)
                { // when lt is boolean, the value is always false, which means null. 
                    l.pop(2);
                    return 0;
                }
                else if (lt == lua.LUA_TNONE || lt == lua.LUA_TNIL)
                { // non-cached
                    l.pop(2); // X
                }
                else
                {
                    var getterinfo = l.GetLua(-1);
                    l.pop(2); // X
                    var fi = getterinfo as FieldInfo;
                    if (fi != null)
                    {
                        object result = null;
                        try
                        {
                            result = fi.GetValue(null);
                        }
                        catch (Exception ex)
                        {
                            l.LogError(ex);
                            return 0;
                        }
                        l.PushLua(result);
                        return 1;
                    }
                    var pi = getterinfo as PropertyInfo;
                    if (pi != null)
                    {
                        object result = null;
                        try
                        {
                            result = pi.GetValue(null);
                        }
                        catch (Exception ex)
                        {
                            l.LogError(ex);
                            return 0;
                        }
                        l.PushLua(result);
                        return 1;
                    }

                    return 0;
                }
            }
            else
            {
                l.pop(1); // X
                l.pushlightuserdata(LuaConst.LRKEY_GETTER); // #getter
                l.newtable(); // #getter getter
                l.rawset(1); // X
            }

            l.pushlightuserdata(LuaConst.LRKEY_TARGET); // #tar
            l.rawget(1); // tar
            Type type;
            l.GetLua(-1, out type);
            l.pop(1); // X
            if (type == null)
            {
                return 0;
            }
            string name;
            l.GetLua(2, out name);
            if (string.IsNullOrEmpty(name))
            {
                return 0;
            }
            var members = type.GetMember(name, BindingFlags.Static | BindingFlags.NonPublic);
            if (members != null && members.Length > 0)
            {
                switch (members[0].MemberType)
                {
                    case MemberTypes.Field:
                        {
                            var fi = members[0] as FieldInfo;
                            // cache it!
                            l.pushlightuserdata(LuaConst.LRKEY_GETTER); // #getter
                            l.rawget(1); // getter
                            l.pushvalue(2); // getter name
                            l.PushLuaObject(fi); // getter name fi
                            l.settable(-3); // getter
                            l.pop(1); // X

                            object result = null;
                            try
                            {
                                result = fi.GetValue(null);
                            }
                            catch (Exception ex)
                            {
                                l.LogError(ex);
                                return 0;
                            }
                            l.PushLua(result);
                        }
                        return 1;
                    case MemberTypes.Property:
                        {
                            var pi = members[0] as PropertyInfo;
                            // cache it!
                            l.pushlightuserdata(LuaConst.LRKEY_GETTER); // #getter
                            l.rawget(1); // getter
                            l.pushvalue(2); // getter name
                            l.PushLuaObject(pi); // getter name pi
                            l.settable(-3); // getter
                            l.pop(1); // X

                            object result = null;
                            try
                            {
                                result = pi.GetValue(null);
                            }
                            catch (Exception ex)
                            {
                                l.LogError(ex);
                                return 0;
                            }
                            l.PushLua(result);
                        }
                        return 1;
                    case MemberTypes.Method:
                        {
                            List<MethodBase> fmethods = new List<MethodBase>();
                            List<MethodBase> gmethods = new List<MethodBase>();
                            for (int i = 0; i < members.Length; ++i)
                            {
                                var method = members[i] as MethodInfo;
                                if (method.ContainsGenericParameters)
                                {
                                    gmethods.Add(method);
                                }
                                else
                                {
                                    fmethods.Add(method);
                                }
                            }
                            var meta = GenericMethodMeta.CreateMethodMeta(fmethods.ToArray(), gmethods.ToArray(), type.IsValueType);
                            l.PushFunction(meta);
                            meta.WrapFunctionByTable(l);
                        }
                        // cache it
                        l.pushvalue(2);
                        l.pushvalue(-2);
                        l.rawset(1);
                        // TODO: cache to getter table also.
                        return 1;
                    case MemberTypes.NestedType:
                        {
                            var nt = members[0] as Type;
                            l.PushLua(nt);
                        }
                        // cache it
                        l.pushvalue(2);
                        l.pushvalue(-2);
                        l.rawset(1);
                        // TODO: cache to getter table also.
                        return 1;
                    case MemberTypes.Event: // TODO: events?
                    default:
                        break;
                }
            }
            // cache it!
            l.pushlightuserdata(LuaConst.LRKEY_GETTER); // #getter
            l.rawget(1); // getter
            l.pushvalue(2); // getter name
            l.pushboolean(false); // getter name false
            l.settable(-3); // getter
            l.pop(1); // X
            return 0;
        }
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        private static int LuaMetaStaticNewIndex(IntPtr l)
        {
            // Try get from cache.
            l.pushlightuserdata(LuaConst.LRKEY_GETTER); // #getter
            l.rawget(1); // getter
            if (l.istable(-1))
            {
                l.pushvalue(2); // getter name
                l.gettable(-2); // getter getterinfo
                var lt = l.type(-1);
                if (lt == lua.LUA_TBOOLEAN)
                { // when lt is boolean, the value is always false, which means null. 
                    l.pop(2);
                    l.LogError("Cannot set value on non-public-reflector: not found");
                    return 0;
                }
                else if (lt == lua.LUA_TNONE || lt == lua.LUA_TNIL)
                { // non-cached
                    l.pop(2); // X
                }
                else
                {
                    var getterinfo = l.GetLua(-1);
                    l.pop(2); // X
                    var fi = getterinfo as FieldInfo;
                    if (fi != null)
                    {
                        var value = l.GetLua(3);
                        try
                        {
                            fi.SetValue(null, value.ConvertTypeRaw(fi.FieldType));
                        }
                        catch (Exception e)
                        {
                            l.LogError(e);
                        }
                        return 0;
                    }
                    var pi = getterinfo as PropertyInfo;
                    if (pi != null)
                    {
                        var value = l.GetLua(3);
                        try
                        {
                            pi.SetValue(null, value.ConvertTypeRaw(pi.PropertyType));
                        }
                        catch (Exception e)
                        {
                            l.LogError(e);
                        }
                        return 0;
                    }

                    l.LogError("Cannot overwrite method on non-public reflector.");
                    return 0;
                }
            }
            else
            {
                l.pop(1); // X
                l.pushlightuserdata(LuaConst.LRKEY_GETTER); // #getter
                l.newtable(); // #getter getter
                l.rawset(1); // X
            }

            var val = l.GetLua(3);
            l.pushlightuserdata(LuaConst.LRKEY_TARGET); // #tar
            l.rawget(1); // tar
            Type type;
            l.GetLua(-1, out type);
            l.pop(1); // X
            if (type == null)
            {
                return 0;
            }
            string name;
            l.GetLua(2, out name);
            if (string.IsNullOrEmpty(name))
            {
                return 0;
            }
            var members = type.GetMember(name, BindingFlags.Static | BindingFlags.NonPublic);
            if (members != null && members.Length > 0)
            {
                switch (members[0].MemberType)
                {
                    case MemberTypes.Field:
                        {
                            var fi = members[0] as FieldInfo;
                            // cache it!
                            l.pushlightuserdata(LuaConst.LRKEY_GETTER); // #getter
                            l.rawget(1); // getter
                            l.pushvalue(2); // getter name
                            l.PushLuaObject(fi); // getter name fi
                            l.settable(-3); // getter
                            l.pop(1); // X

                            try
                            {
                                fi.SetValue(null, val.ConvertTypeRaw(fi.FieldType));
                            }
                            catch (Exception ex)
                            {
                                l.LogError(ex);
                            }
                        }
                        return 0;
                    case MemberTypes.Property:
                        {
                            var pi = members[0] as PropertyInfo;
                            // cache it!
                            l.pushlightuserdata(LuaConst.LRKEY_GETTER); // #getter
                            l.rawget(1); // getter
                            l.pushvalue(2); // getter name
                            l.PushLuaObject(pi); // getter name pi
                            l.settable(-3); // getter
                            l.pop(1); // X

                            try
                            {
                                pi.SetValue(null, val.ConvertTypeRaw(pi.PropertyType));
                            }
                            catch (Exception ex)
                            {
                                l.LogError(ex);
                            }
                        }
                        return 0;
                    case MemberTypes.Method:
                        l.pushvalue(2); // name
                        {
                            List<MethodBase> fmethods = new List<MethodBase>();
                            List<MethodBase> gmethods = new List<MethodBase>();
                            for (int i = 0; i < members.Length; ++i)
                            {
                                var method = members[i] as MethodInfo;
                                if (method.ContainsGenericParameters)
                                {
                                    gmethods.Add(method);
                                }
                                else
                                {
                                    fmethods.Add(method);
                                }
                            }
                            var meta = GenericMethodMeta.CreateMethodMeta(fmethods.ToArray(), gmethods.ToArray(), type.IsValueType);
                            l.PushFunction(meta);
                            meta.WrapFunctionByTable(l);
                        } // name func
                        // cache it
                        l.rawset(1); // X
                        // TODO: cache to getter table also.
                        l.LogError("Cannot overwrite method on non-public reflector: " + name);
                        return 0;
                    case MemberTypes.NestedType:
                        l.pushvalue(2); // name
                        {
                            var nt = members[0] as Type;
                            l.PushLua(nt);
                        } // name type
                        // cache it
                        l.rawset(1); // X
                        // TODO: cache to getter table also.
                        l.LogError("Cannot overwrite nested-type on non-public reflector: " + name);
                        return 0;
                    case MemberTypes.Event: // TODO: events?
                        l.LogError("Cannot overwrite event on non-public reflector: " + name);
                        break;
                    default:
                        l.LogError("Cannot set value on non-public-reflector: not found: " + name);
                        break;
                }
            }
            // cache it!
            l.pushlightuserdata(LuaConst.LRKEY_GETTER); // #getter
            l.rawget(1); // getter
            l.pushvalue(2); // getter name
            l.pushboolean(false); // getter name false
            l.settable(-3); // getter
            l.pop(1); // X
            return 0;
        }
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        private static int LuaMetaStaticCall(IntPtr l)
        {
            l.pushlightuserdata(LuaConst.LRKEY_TARGET); // #tar
            l.rawget(1); // tar
            Type type;
            l.GetLua(-1, out type);
            l.pop(1); // X
            if (type == null)
            {
                return 0;
            }
            var members = type.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic);
            if (members == null || members.Length == 0)
            {
                return 0;
            }
            var meta = PackedMethodMeta.CreateMethodMeta(members, null, false);
            if (meta == null)
            {
                return 0;
            }
            var oldtop = l.gettop();
            meta.call(l, null);
            // cache it!
            l.getmetatable(1); // meta
            l.PushString(LuaConst.LS_META_KEY_CALL); // meta __call
            l.PushFunction(meta); // meta __call ctor
            l.rawset(-3); // meta
            l.pop(1); // X
            return l.gettop() - oldtop;
        }

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        private static int LuaMetaCreateInstanceReflector(IntPtr l)
        {
            l.newtable(); // reflector
            l.pushlightuserdata(LuaConst.LRKEY_TARGET); // refl #tar
            l.pushvalue(1); // refl #tar tar
            l.rawset(-3); // refl
            l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS); // refl #trans
            l.pushlightuserdata(LuaExtend.LuaTransExtend.Instance.r); // refl #trans trans
            l.rawset(-3); // refl
            l.newtable(); // refl meta
            l.pushcfunction(LuaFuncInstanceIndex); // refl meta index
            l.RawSet(-2, LuaConst.LS_META_KEY_INDEX); // refl meta
            l.pushcfunction(LuaFuncInstanceNewIndex); // refl meta newindex
            l.RawSet(-2, LuaConst.LS_META_KEY_NINDEX); // refl meta
            l.setmetatable(-2); // refl

            // cache getter table
            if (l.getmetatable(1))
            { // refl objmeta
                l.GetField(-1, LuaConst.LS_SP_KEY_NONPUBLIC); // refl objmeta npgetter
                if (!l.istable(-1))
                {
                    l.pop(1); // refl objmeta
                    l.newtable(); // refl objmeta npgetter
                    l.pushvalue(-1); // refl objmeta npgetter npgetter
                    l.SetField(-3, LuaConst.LS_SP_KEY_NONPUBLIC); // refl objmeta npgetter
                }
                l.pushlightuserdata(LuaConst.LRKEY_GETTER); // refl objmeta npgetter #getter
                l.insert(-2); // refl objmeta #getter npgetter
                l.rawset(-4); // refl objmeta
                l.pop(1); // refl
            }

            // cache it!
            l.PushString(LuaConst.LS_SP_KEY_NONPUBLIC); // refl "@npub"
            l.pushvalue(-2); // refl "@npub" refl
            l.rawset(1); // refl
            return 1;
        }
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        private static int LuaMetaCreateStaticReflector(IntPtr l)
        {
            l.newtable(); // reflector
            l.PushString(LuaConst.LS_SP_KEY_NONPUBLIC);
            l.pushboolean(true);
            l.rawset(-3); // refl["@npub"] = true
            l.pushlightuserdata(LuaConst.LRKEY_TARGET); // refl #tar
            l.pushvalue(lua.upvalueindex(1)); // refl #tar tar
            l.rawset(-3); // refl
            l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS); // refl #trans
            l.pushlightuserdata(LuaExtend.LuaTransExtend.Instance.r); // refl #trans trans
            l.rawset(-3); // refl
            l.newtable(); // refl meta
            l.pushcfunction(LuaFuncStaticIndex); // refl meta index
            l.RawSet(-2, LuaConst.LS_META_KEY_INDEX); // refl meta
            l.pushcfunction(LuaFuncStaticNewIndex); // refl meta newindex
            l.RawSet(-2, LuaConst.LS_META_KEY_NINDEX); // refl meta
            l.pushcfunction(LuaFuncStaticCall); // refl meta call
            l.RawSet(-2, LuaConst.LS_META_KEY_CALL); // refl meta
            l.setmetatable(-2); // refl
            // cache it!
            l.PushString(LuaConst.LS_SP_KEY_NONPUBLIC); // refl "@npub"
            l.pushvalue(-2); // refl "@npub" refl
            l.rawset(lua.upvalueindex(1)); // refl
            return 1;
        }

        // Reflector
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        private static int LuaMetaReflectorlIndex(IntPtr l)
        {
            l.pushlightuserdata(LuaConst.LRKEY_TARGET); // #tar
            l.rawget(1); // tar
            Type type;
            l.GetLua(-1, out type);
            l.pop(1); // X
            if (type == null)
            {
                return 0;
            }
            string name;
            l.GetLua(2, out name);
            if (string.IsNullOrEmpty(name))
            {
                return 0;
            }
            var members = type.GetMember(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            if (members == null || members.Length == 0)
            {
                return 0;
            }
            switch (members[0].MemberType)
            {
                case MemberTypes.Field:
                    {
                        var fi = members[0] as FieldInfo;
                        l.PushLuaObject(fi);
                    }
                    // cache it
                    l.pushvalue(2);
                    l.pushvalue(-2);
                    l.rawset(1);
                    return 1;
                case MemberTypes.Property:
                    {
                        var pi = members[0] as PropertyInfo;
                        l.PushLuaObject(pi);
                    }
                    // cache it
                    l.pushvalue(2);
                    l.pushvalue(-2);
                    l.rawset(1);
                    return 1;
                case MemberTypes.Method:
                    {
                        List<MethodBase> fmethods = new List<MethodBase>();
                        List<MethodBase> gmethods = new List<MethodBase>();
                        for (int i = 0; i < members.Length; ++i)
                        {
                            var method = members[i] as MethodInfo;
                            if (method.ContainsGenericParameters)
                            {
                                gmethods.Add(method);
                            }
                            else
                            {
                                fmethods.Add(method);
                            }
                        }
                        var meta = GenericMethodMeta.CreateMethodMeta(fmethods.ToArray(), gmethods.ToArray(), type.IsValueType);
                        l.PushFunction(meta);
                        meta.WrapFunctionByTable(l);
                    }
                    // cache it
                    l.pushvalue(2);
                    l.pushvalue(-2);
                    l.rawset(1);
                    return 1;
                case MemberTypes.Event:
                    {
                        var ei = members[0] as EventInfo;
                        l.PushLuaObject(ei);
                    }
                    // cache it
                    l.pushvalue(2);
                    l.pushvalue(-2);
                    l.rawset(1);
                    return 1;
                case MemberTypes.Constructor:
                    {
                        List<MethodBase> fmethods = new List<MethodBase>();
                        for (int i = 0; i < members.Length; ++i)
                        {
                            var method = members[i] as ConstructorInfo;
                            fmethods.Add(method);
                        }
                        var meta = PackedMethodMeta.CreateMethodMeta(fmethods, null, false);
                        l.PushFunction(meta);
                        meta.WrapFunctionByTable(l);
                    }
                    // cache it
                    l.pushvalue(2);
                    l.pushvalue(-2);
                    l.rawset(1);
                    return 1;
                case MemberTypes.NestedType:
                    {
                        var nt = members[0] as Type;
                        //PushReflectorOfType(l, nt); // NOTICE: should we push reflector instead of Type?
                        l.PushLuaType(nt);
                    }
                    // cache it
                    l.pushvalue(2);
                    l.pushvalue(-2);
                    l.rawset(1);
                    return 1;
                default:
                    return 0;
            }
        }

        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        private static int LuaMetaCreateReflector(IntPtr l)
        {
            l.newtable(); // reflector
            l.pushlightuserdata(LuaConst.LRKEY_TARGET); // refl #tar
            l.pushvalue(lua.upvalueindex(1)); // refl #tar tar
            l.rawset(-3); // refl
            l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS); // refl #trans
            l.pushlightuserdata(LuaExtend.LuaTransExtend.Instance.r); // refl #trans trans
            l.rawset(-3); // refl
            l.newtable(); // refl meta
            l.pushcfunction(LuaFuncReflectorlIndex); // refl meta index
            l.RawSet(-2, LuaConst.LS_META_KEY_INDEX); // refl meta
            l.setmetatable(-2); // refl
            // cache it!
            l.PushString(LuaConst.LS_SP_KEY_REFLECTOR); // refl "@refl"
            l.pushvalue(-2); // refl "@refl" refl
            l.rawset(lua.upvalueindex(1)); // refl
            return 1;
        }

        public static void PushReflectorOfType(IntPtr l, Type t)
        {
            l.PushLuaType(t); // tar
            l.PushString(LuaConst.LS_SP_KEY_REFLECTOR); // tar "@refl"
            l.gettable(-2); // tar refl
            l.remove(-2); // refl
        }

        public static BaseUniqueMethodMeta FindNonPublicCtor(Type type, Types args)
        {
            var members = type.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic);
            if (members == null || members.Length == 0)
            {
                return null;
            }
            var meta = PackedMethodMeta.CreateMethodMeta(members, null, false);
            if (meta == null)
            {
                return null;
            }
            if (meta is BaseUniqueMethodMeta)
            {
                return (BaseUniqueMethodMeta)meta;
            }
            else if (meta is BaseOverloadedMethodMeta)
            {
                return ((BaseOverloadedMethodMeta)meta).FindAppropriate(args);
            }
            return null;
        }
    }
}