using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using lua = LuaLib.LuaCoreLib;
using lual = LuaLib.LuaAuxLib;
using luae = LuaLib.LuaLibEx;

namespace LuaLib
{
    public static partial class LuaHub
    {
        public static class LuaHubC
        {
            public const int LIB_VER = 15;
            public static readonly bool Ready;
            #if DISABLE_LUA_PRECOMPILE
            public const bool LuaPrecompileEnabled = false;
            #else
            public const bool LuaPrecompileEnabled = true;
#endif

#if UNITY_EDITOR
#if UNITY_EDITOR_OSX
            public const string LIB_PATH = "ModLuaNative";
#else
            public const string LIB_PATH = "ModLuaNative";
#endif
#elif UNITY_IPHONE
            public const string LIB_PATH = "__Internal";
#else
#if DLLIMPORT_NAME_FULL
            public const string LIB_PATH = "libModLuaNative.so";
#else
            public const string LIB_PATH = "ModLuaNative";
#endif
#endif

            static LuaHubC()
            {
                Ready = false;
                #if !DISABLE_LUA_HUB_C
                lua.PrepareLib();
                #if !UNITY_ENGINE && !UNITY_5_3_OR_NEWER
                UnityEngineEx.PluginManager.LoadLib(LIB_PATH);
                #endif
                try
                {
                    if (lua_checkVer(LIB_VER))
                    {
                        lua_setCSFuncs(
                        Func_PushType
                        , LuaCommonMeta.LuaFuncRawGC
                        , LuaCommonMeta.LuaFuncCachedGC
                        , LuaCommonMeta.LuaFuncRawToStr
                        , LuaHub.LuaFuncOnError
                        , LuaTypeHub.TypeHubBase.LuaFuncGenericIndex
                        , LuaTypeHub.TypeHubBase.LuaFuncGenericNewIndex
                        , LuaCommonMeta.LuaFuncRawEq
                        , LuaExtend.LuaTransExtend.Instance.r
                        );
                        Ready = true;
                    }
                    else
                    {
                        UnityEngineEx.PlatDependant.LogError("Can not initialize ModLuaNative");
                    }
                }
                catch (Exception e)
                {
                    UnityEngineEx.PlatDependant.LogError(e);
                }
                #endif
            }
            
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void Del_PushType(IntPtr l, IntPtr r);
            private static readonly Del_PushType Func_PushType = new Del_PushType(CB_PushType);
            [AOT.MonoPInvokeCallback(typeof(Del_PushType))]
            private static void CB_PushType(IntPtr l, IntPtr r)
            {
                ILuaTypeHub trans = null;
                try
                {
                    System.Runtime.InteropServices.GCHandle handle = (System.Runtime.InteropServices.GCHandle)r;
                    trans = handle.Target as ILuaTypeHub;
                }
                catch { }
                if (trans != null)
                {
                    trans.PushLuaTypeRaw(l);
                }
                else
                {
                    l.pushnil();
                }
            }
            
            #if !DISABLE_LUA_HUB_C
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_wrapFunctionByTable(IntPtr l, IntPtr methodmeta);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_pushTypeIndex(IntPtr l, int n_upvalue);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_pushTypeNewIndex(IntPtr l, int n_upvalue);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_pushObjIndex(IntPtr l, int n_upvalue);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_pushObjNewIndex(IntPtr l, int n_upvalue);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_pushCommonBinaryOp(IntPtr l, int n_upvalue);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_pushCommonEq(IntPtr l, int n_upvalue);

            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_trackLiveness(IntPtr l, int index);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern bool lua_checkLiveness(IntPtr l, int index);
            
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void MakeExtend(IntPtr l, int index);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void MakeUnextend(IntPtr l, int index);
            
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            private static extern bool lua_checkVer(int ver);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            private static extern void lua_setCSFuncs(Del_PushType funcPushType, lua.CFunction func_Rawgc, lua.CFunction func_Cachedgc, lua.CFunction func_RawToStr, lua.CFunction func_OnError, lua.CFunction func_GGetter, lua.CFunction func_GSetter, lua.CFunction func_RawEq, IntPtr extTrans);
            
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_pushObject(IntPtr l, IntPtr obj, IntPtr type, bool shouldCache);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_setObject(IntPtr l, int index, IntPtr obj);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_getObject(IntPtr l, int index, out IntPtr obj);
            
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern bool lua_pushString(IntPtr l, int id);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_pushAndRegString(IntPtr l, int id, byte[] str);
            #if UNITY_EDITOR
            public static void lua_pushAndRegString(IntPtr l, int id, string str)
            {
                lua_pushAndRegString(l, id, str.DefaultEncode());
            }
            #else
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_pushAndRegString(IntPtr l, int id, string str);
            #endif
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_regString(IntPtr l, int id, byte[] str);
            #if UNITY_EDITOR
            public static void lua_regString(IntPtr l, int id, string str)
            {
                lua_regString(l, id, str.DefaultEncode());
            }
            #else
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_regString(IntPtr l, int id, string str);
            #endif
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_unregString(IntPtr l, int id);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern int lua_getStringRegId(IntPtr l, int index);
            
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_setTypeVector3(IntPtr type);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_pushVector3(IntPtr l, System.Single x, System.Single y, System.Single z);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_setVector3(IntPtr l, int index, System.Single x, System.Single y, System.Single z);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_getVector3(IntPtr l, int index, out System.Single x, out System.Single y, out System.Single z);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_setTypeLayerMask(IntPtr type);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_pushLayerMask(IntPtr l, System.Int32 value);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_setLayerMask(IntPtr l, int index, System.Int32 value);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_getLayerMask(IntPtr l, int index, out System.Int32 value);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_setTypeQuaternion(IntPtr type);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_pushQuaternion(IntPtr l, System.Single x, System.Single y, System.Single z, System.Single w);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_setQuaternion(IntPtr l, int index, System.Single x, System.Single y, System.Single z, System.Single w);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_getQuaternion(IntPtr l, int index, out System.Single x, out System.Single y, out System.Single z, out System.Single w);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_setTypeColor(IntPtr type);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_pushColor(IntPtr l, System.Single r, System.Single g, System.Single b, System.Single a);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_setColor(IntPtr l, int index, System.Single r, System.Single g, System.Single b, System.Single a);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_getColor(IntPtr l, int index, out System.Single r, out System.Single g, out System.Single b, out System.Single a);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_setTypeRect(IntPtr type);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_pushRect(IntPtr l, System.Single xMin, System.Single yMin, System.Single width, System.Single height);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_setRect(IntPtr l, int index, System.Single xMin, System.Single yMin, System.Single width, System.Single height);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_getRect(IntPtr l, int index, out System.Single xMin, out System.Single yMin, out System.Single width, out System.Single height);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_setTypeVector2(IntPtr type);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_pushVector2(IntPtr l, System.Single x, System.Single y);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_setVector2(IntPtr l, int index, System.Single x, System.Single y);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_getVector2(IntPtr l, int index, out System.Single x, out System.Single y);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_setTypeVector4(IntPtr type);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_pushVector4(IntPtr l, System.Single x, System.Single y, System.Single z, System.Single w);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_setVector4(IntPtr l, int index, System.Single x, System.Single y, System.Single z, System.Single w);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_getVector4(IntPtr l, int index, out System.Single x, out System.Single y, out System.Single z, out System.Single w);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_setTypeBounds(IntPtr type);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_pushBounds(IntPtr l, System.Single center_x, System.Single center_y, System.Single center_z, System.Single extents_x, System.Single extents_y, System.Single extents_z);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_setBounds(IntPtr l, int index, System.Single center_x, System.Single center_y, System.Single center_z, System.Single extents_x, System.Single extents_y, System.Single extents_z);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_getBounds(IntPtr l, int index, out System.Single center_x, out System.Single center_y, out System.Single center_z, out System.Single extents_x, out System.Single extents_y, out System.Single extents_z);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_setTypePlane(IntPtr type);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_pushPlane(IntPtr l, System.Single distance, System.Single normal_x, System.Single normal_y, System.Single normal_z);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_setPlane(IntPtr l, int index, System.Single distance, System.Single normal_x, System.Single normal_y, System.Single normal_z);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_getPlane(IntPtr l, int index, out System.Single distance, out System.Single normal_x, out System.Single normal_y, out System.Single normal_z);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_setTypeRay(IntPtr type);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_pushRay(IntPtr l, System.Single origin_x, System.Single origin_y, System.Single origin_z, System.Single direction_x, System.Single direction_y, System.Single direction_z);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_setRay(IntPtr l, int index, System.Single origin_x, System.Single origin_y, System.Single origin_z, System.Single direction_x, System.Single direction_y, System.Single direction_z);
            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void lua_getRay(IntPtr l, int index, out System.Single origin_x, out System.Single origin_y, out System.Single origin_z, out System.Single direction_x, out System.Single direction_y, out System.Single direction_z);
            #endif
        }
    }
}
