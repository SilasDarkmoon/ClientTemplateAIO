﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using UnityEngineEx;

using LuaLib;
using lua = LuaLib.LuaCoreLib;
using lual = LuaLib.LuaAuxLib;
using luae = LuaLib.LuaLibEx;
using static LuaLib.LuaPack;

namespace LuaLib
{
    public static partial class LuaHubEx
    {
        private static void InitLuaProtobufBridge(IntPtr l)
        {
            l.newtable();
            l.pushcfunction(ProtoDelCreateMessage);
            l.SetField(-2, "new");
            l.SetGlobal("proto");

            if (LuaProtobufNative.InitFunc != null)
            {
                try
                {
                    LuaProtobufNative.InitFunc(l);
                }
                catch { }
            }
        }

        public static readonly lua.CFunction ProtoDelCreateMessage = new lua.CFunction(ProtoFuncCreateMessage);
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ProtoFuncCreateMessage(IntPtr l)
        {
            var argcnt = l.gettop();
            string name = null;
            if (argcnt >= 1)
            {
                name = l.GetString(1);
            }
            if (argcnt >= 2 && l.istable(2))
            {
                l.pushvalue(2);
            }
            else
            {
                l.newtable();
            }
            l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS); // #trans
            l.pushlightuserdata(LuaLib.LuaProtobufBridge._ProtobufTrans.r);
            l.settable(-3);
            if (!string.IsNullOrEmpty(name))
            {
                l.PushString(name);
                l.SetField(-2, LuaLib.LuaProtobufBridge.LS_messageName);
            }
            return 1;
        }

        private static LuaExLibs.LuaExLibItem _LuaExLib_Protobuf_Instance = new LuaExLibs.LuaExLibItem(InitLuaProtobufBridge, 200);

        public static class LuaProtobufNative
        {
            public static readonly Action<IntPtr> InitFunc;

#if UNITY_IPHONE && !UNITY_EDITOR
            public const string LIB_PATH = "__Internal";
#else
#if DLLIMPORT_NAME_FULL
            public const string LIB_PATH = "libLuaProtobuf.so";
#else
            public const string LIB_PATH = "LuaProtobuf";
#endif
#endif
            static LuaProtobufNative()
            {
                InitFunc = null;
#if !UNITY_ENGINE && !UNITY_5_3_OR_NEWER
                UnityEngineEx.PluginManager.LoadLib(LIB_PATH);
#endif
                try
                {
                    InitFunc = InitLuaProtobufPlugin;
                }
                catch (Exception e)
                {
                    PlatDependant.LogError(e);
                }
            }

            [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
            public static extern void InitLuaProtobufPlugin(IntPtr l);
        }
    }
}

#if UNITY_ENGINE || UNITY_5_3_OR_NEWER || LUA_STANDALONE_USE_PB_IN_CS
namespace LuaLib
{
    using ModNet;
    public static partial class LuaHubEx
    {
        private class DynamicProtobufMessageHub : LuaTypeHub.TypeHubValueType, ILuaTrans<ProtobufMessage>, ILuaPush<ProtobufMessage>, ILuaNative, ILuaConvert
        {
            public override bool Nonexclusive { get { return true; } }
            private static Dictionary<ProtobufNativeType, Action<IntPtr, ProtobufParsedValue>> _TypedSlotValuePushFuncs = new Dictionary<ProtobufNativeType, Action<IntPtr, ProtobufParsedValue>>()
            {
                { ProtobufNativeType.TYPE_DOUBLE, (l, val) => l.pushnumber(val.Double) },
                { ProtobufNativeType.TYPE_FLOAT, (l, val) => l.pushnumber(val.Single) },
                { ProtobufNativeType.TYPE_INT64, (l, val) => l.pushnumber(val.Int64) },
                { ProtobufNativeType.TYPE_UINT64, (l, val) => l.pushnumber(val.UInt64) },
                { ProtobufNativeType.TYPE_INT32, (l, val) => l.pushnumber(val.Int32) },
                { ProtobufNativeType.TYPE_FIXED64, (l, val) => l.pushnumber(val.UInt64) },
                { ProtobufNativeType.TYPE_FIXED32, (l, val) => l.pushnumber(val.UInt32) },
                { ProtobufNativeType.TYPE_BOOL, (l, val) => l.pushboolean(val.Boolean) },
                { ProtobufNativeType.TYPE_STRING, (l, val) => l.pushstring(val.String) },
                { ProtobufNativeType.TYPE_MESSAGE, (l, val) => PushLuaRaw(l, val.Message) },
                { ProtobufNativeType.TYPE_BYTES, (l, val) => l.pushbuffer(val.Bytes) },
                { ProtobufNativeType.TYPE_UINT32, (l, val) => l.pushnumber(val.UInt32) },
                { ProtobufNativeType.TYPE_ENUM, (l, val) => l.pushnumber(val.UInt64) },
                { ProtobufNativeType.TYPE_SFIXED32, (l, val) => l.pushnumber(val.Int32) },
                { ProtobufNativeType.TYPE_SFIXED64, (l, val) => l.pushnumber(val.Int64) },
                { ProtobufNativeType.TYPE_SINT32, (l, val) => l.pushnumber(val.Int32) },
                { ProtobufNativeType.TYPE_SINT64, (l, val) => l.pushnumber(val.Int64) },
            };
            private static void PushSlotValue(IntPtr l, ProtobufParsedValue val)
            {
                Action<IntPtr, ProtobufParsedValue> pushFunc;
                if (_TypedSlotValuePushFuncs.TryGetValue(val.NativeType, out pushFunc))
                {
                    pushFunc(l, val);
                }
                else
                {
                    l.pushnil();
                }
            }
            private static void PushSlotValue(IntPtr l, ProtobufMessage.SlotValueAccessor val)
            {
                Action<IntPtr, ProtobufParsedValue> pushFunc;
                if (_TypedSlotValuePushFuncs.TryGetValue(val.NativeType, out pushFunc))
                {
                    pushFunc(l, val.FirstValue);
                }
                else
                {
                    l.pushnil();
                }
            }
            public static void SetDataRaw(IntPtr l, int index, ProtobufMessage val)
            {
                l.pushvalue(index);
                foreach (var kvp in val.AsDict())
                {
                    l.PushString(kvp.Key);
                    if (kvp.Value.IsRepeated)
                    {
                        l.newtable();
                        for (int i = 0; i < kvp.Value.Count; ++i)
                        {
                            l.pushnumber(i + 1);
                            var slotval = kvp.Value[i];
                            PushSlotValue(l, slotval);
                            l.rawset(-3);
                        }
                    }
                    else
                    {
                        PushSlotValue(l, kvp.Value);
                    }
                    l.rawset(-3);
                }
                l.pop(1);
            }
            private static void AddMessageValue(IntPtr l, int index, ProtobufMessage.SlotValueAccessor slot)
            {
                switch (l.type(-1))
                {
                    case lua.LUA_TBOOLEAN:
                        slot.Booleans.Add(l.toboolean(index));
                        break;
                    case lua.LUA_TLIGHTUSERDATA:
                        slot.IntPtrs.Add(l.touserdata(index));
                        break;
                    case lua.LUA_TNUMBER:
                        slot.Doubles.Add(l.tonumber(index));
                        break;
                    case lua.LUA_TSTRING:
                        {
                            var data = l.tolstring(index);
                            var chars = PlatDependant.GetCharsDataString(data);
                            if (PlatDependant.ContainUTF8DecodeFailure(chars))
                            {
                                slot.RepeatedBytes.Add(data);
                            }
                            else
                            {
                                slot.Strings.Add(new string(chars));
                            }
                            break;
                        }
                    case lua.LUA_TTABLE:
                        slot.Messages.Add(GetLuaRaw(l, index));
                        break;
                    case lua.LUA_TNONE:
                    case lua.LUA_TNIL:
                        slot.Objects.Add(null);
                        break;
                    case lua.LUA_TFUNCTION:
                    case lua.LUA_TUSERDATA:
                    case lua.LUA_TTHREAD:
                        break;
                    default:
                        break;
                }
            }
            private static void GetMessageValue(IntPtr l, int index, ProtobufMessage.SlotValueAccessor slot)
            {
                switch (l.type(-1))
                {
                    case lua.LUA_TBOOLEAN:
                        slot.Boolean = l.toboolean(index);
                        break;
                    case lua.LUA_TLIGHTUSERDATA:
                        slot.IntPtr = l.touserdata(index);
                        break;
                    case lua.LUA_TNUMBER:
                        slot.Double = l.tonumber(index);
                        break;
                    case lua.LUA_TSTRING:
                        {
                            var data = l.tolstring(index);
                            var chars = PlatDependant.GetCharsDataString(data);
                            if (PlatDependant.ContainUTF8DecodeFailure(chars))
                            {
                                slot.Bytes = data;
                            }
                            else
                            {
                                slot.String = new string(chars);
                            }
                            break;
                        }
                    case lua.LUA_TTABLE:
                        {
                            if (l.IsArray(index))
                            {
                                slot.IsRepeated = true;
                                var cnt = l.getn(index);
                                l.pushvalue(index);
                                for (int i = 1; i <= cnt; ++i)
                                {
                                    l.pushnumber(i);
                                    l.rawget(-2);
                                    AddMessageValue(l, -1, slot);
                                    l.pop(1);
                                }
                                l.pop(1);
                            }
                            else
                            {
                                slot.Message = GetLuaRaw(l, index);
                            }
                        }
                        break;
                    case lua.LUA_TNONE:
                    case lua.LUA_TNIL:
                    case lua.LUA_TFUNCTION:
                    case lua.LUA_TUSERDATA:
                    case lua.LUA_TTHREAD:
                        break;
                    default:
                        break;
                }
            }
            public static ProtobufMessage GetLuaRaw(IntPtr l, int index)
            {
                if (!l.istable(index))
                {
                    return null;
                }
                using (var lr = l.CreateStackRecover())
                {
                    l.pushvalue(index); // otab
                    l.pushlightuserdata(LuaConst.LRKEY_TARGET); // otab #tar
                    l.rawget(-2); // otab obj
                    if (l.IsUserData(-1))
                    {
                        return l.GetLuaRawObject(-1) as ProtobufMessage;
                    }
                    l.pop(1); // otab

                    ProtobufMessage message = new ProtobufMessage();
                    //l.pushvalue(index);
                    l.pushnil();
                    while (l.next(-2))
                    {
                        if (l.IsString(-2))
                        {
                            string key = l.GetString(-2);
                            if (!string.IsNullOrEmpty(key))
                            {
                                var slot = message[key];
                                GetMessageValue(l, -1, slot);
                            }
                        }
                        l.pop(1);
                    }
                    return message;
                }
            }
            public static void PushLuaRaw(IntPtr l, ProtobufMessage val)
            {
                if (object.ReferenceEquals(val, null))
                {
                    _DynamicProtobufMessageHub.PushLuaCommon(l, null);
                }
                else
                {
                    l.newtable();
                    SetDataRaw(l, -1, val);
                }
            }

            public class DynamicProtobufMessageHubNative : LuaHub.LuaPushNativeBase<ProtobufMessage>
            {
                public override ProtobufMessage GetLua(IntPtr l, int index)
                {
                    return GetLuaRaw(l, index);
                }
                public override IntPtr PushLua(IntPtr l, ProtobufMessage val)
                {
                    PushLuaRaw(l, val);
                    return IntPtr.Zero;
                }
            }
            public static readonly DynamicProtobufMessageHubNative LuaHubNative = new DynamicProtobufMessageHubNative();

            public DynamicProtobufMessageHub() : base(null)
            {
                t = typeof(ProtobufMessage);
                PutIntoCache();

                _ConvertFromFuncs = new[]
                {
                    new KeyValuePair<Type, LuaConvertFunc>(typeof(LuaTable), LuaConvertFuncFromLuaTable),
                    new KeyValuePair<Type, LuaConvertFunc>(typeof(LuaOnStackTable), LuaConvertFuncFromLuaTable),
                    new KeyValuePair<Type, LuaConvertFunc>(typeof(LuaRawTable), LuaConvertFuncFromLuaTable),
                    new KeyValuePair<Type, LuaConvertFunc>(typeof(LuaOnStackRawTable), LuaConvertFuncFromLuaTable),
                };
                _ConvertFuncs[typeof(LuaTable)] = LuaConvertFuncToLuaTable;
                _ConvertFuncs[typeof(LuaOnStackTable)] = LuaConvertFuncToLuaOnStackTable;
                _ConvertFuncs[typeof(LuaRawTable)] = LuaConvertFuncToLuaRawTable;
                _ConvertFuncs[typeof(LuaOnStackRawTable)] = LuaConvertFuncToLuaOnStackRawTable;
            }
            protected override bool UpdateDataAfterCall
            {
                get { return true; }
            }

            public override IntPtr PushLua(IntPtr l, object val)
            {
                PushLuaRaw(l, (ProtobufMessage)val);
                return IntPtr.Zero;
            }
            public override void SetData(IntPtr l, int index, object val)
            {
                SetDataRaw(l, index, (ProtobufMessage)val);
            }
            public override object GetLua(IntPtr l, int index)
            {
                return GetLuaRaw(l, index);
            }
            public IntPtr PushLua(IntPtr l, ProtobufMessage val)
            {
                PushLuaRaw(l, val);
                return IntPtr.Zero;
            }
            public void SetData(IntPtr l, int index, ProtobufMessage val)
            {
                SetDataRaw(l, index, val);
            }
            ProtobufMessage ILuaTrans<ProtobufMessage>.GetLua(IntPtr l, int index)
            {
                return GetLuaRaw(l, index);
            }

            public void Wrap(IntPtr l, int index)
            {
                l.pushvalue(index);
                // set trans
                l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS); // #trans
                l.pushlightuserdata(_DynamicProtobufMessageHub.r); // #trans trans
                l.settable(-3);
            }
            public void Unwrap(IntPtr l, int index)
            {
                l.pushvalue(index);
                // set trans
                l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS); // #trans
                l.pushnil(); // #trans nil
                l.settable(-3);
            }
            public int LuaType { get { return LuaCoreLib.LUA_TTABLE; } }

            private static int LuaConvertFuncToLuaOnStackTable(IntPtr l, int index)
            {
                //// this is already the lua-table
                //l.pushvalue(index);
                ////l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS); // #trans
                ////l.pushnil(); // #trans nil
                ////l.settable(-3);

                var pos = l.NormalizeIndex(index);
                var inst = new LuaOnStackTable(l, pos);
                l.PushLuaObject(inst);
                return 1;
            }
            private static int LuaConvertFuncToLuaTable(IntPtr l, int index)
            {
                //// this is already the lua-table
                //l.pushvalue(index);
                ////l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS); // #trans
                ////l.pushnil(); // #trans nil
                ////l.settable(-3);

                var inst = new LuaTable(l, index);
                l.PushLuaObject(inst);
                return 1;
            }
            private static int LuaConvertFuncToLuaOnStackRawTable(IntPtr l, int index)
            {
                //// this is already the lua-table
                //l.pushvalue(index);
                ////l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS); // #trans
                ////l.pushnil(); // #trans nil
                ////l.settable(-3);

                var pos = l.NormalizeIndex(index);
                var inst = new LuaOnStackRawTable(l, pos);
                l.PushLuaObject(inst);
                return 1;
            }
            private static int LuaConvertFuncToLuaRawTable(IntPtr l, int index)
            {
                //// this is already the lua-table
                //l.pushvalue(index);
                ////l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS); // #trans
                ////l.pushnil(); // #trans nil
                ////l.settable(-3);

                var inst = new LuaRawTable(l, index);
                l.PushLuaObject(inst);
                return 1;
            }
            private static int LuaConvertFuncFromLuaTable(IntPtr l, int index)
            {
                int typecode;
                bool isobj;
                l.GetType(index, out typecode, out isobj);
                if (!isobj)
                {
                    l.pushvalue(index);
                    // set trans
                    l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS); // #trans
                    l.pushlightuserdata(_DynamicProtobufMessageHub.r); // #trans trans
                    l.settable(-3);
                }
                else
                {
                    l.PushLua(l.GetLua(index));
                    // set trans
                    l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS); // #trans
                    l.pushlightuserdata(_DynamicProtobufMessageHub.r); // #trans trans
                    l.settable(-3);
                }
                return 1;
            }
        }

        private static DynamicProtobufMessageHub _DynamicProtobufMessageHub = new DynamicProtobufMessageHub();
    }
}
#endif

namespace LuaLib
{
    public static partial class LuaProtobufBridge
    {
        public delegate void SyncDataFunc(IntPtr l, object data);
        public delegate object CreateFunc();
        public class TypedDataBridge
        {
            public string Name { get; protected set; }
            public Type Type { get; protected set; }
            public SyncDataFunc PushFunc { get; protected set; }
            public SyncDataFunc ReadFunc { get; protected set; }
            public CreateFunc Create { get; protected set; }

            protected TypedDataBridge() { }
        }
        public class TypedDataBridgeReg : TypedDataBridge
        {
            public TypedDataBridgeReg(Type type, string name, SyncDataFunc pushFunc, SyncDataFunc readFunc, CreateFunc create)
            {
                Name = name;
                Type = type;
                PushFunc = pushFunc;
                ReadFunc = readFunc;
                Create = create;
                TypedSyncFuncs[type] = this;
                NamedSyncFuncs[name] = this;
                NameToType[name] = type;
                TypeToName[type] = name;
            }
        }

        private readonly static byte[] EmptyBuffer = new byte[0];

        private static Dictionary<Type, TypedDataBridge> _TypedSyncFuncs;
        public static Dictionary<Type, TypedDataBridge> TypedSyncFuncs
        {
            get
            {
                if (_TypedSyncFuncs == null)
                {
                    _TypedSyncFuncs = new Dictionary<Type, TypedDataBridge>();
                }
                return _TypedSyncFuncs;
            }
        }
        private static Dictionary<string, TypedDataBridge> _NamedSyncFuncs;
        public static Dictionary<string, TypedDataBridge> NamedSyncFuncs
        {
            get
            {
                if (_NamedSyncFuncs == null)
                {
                    _NamedSyncFuncs = new Dictionary<string, TypedDataBridge>();
                }
                return _NamedSyncFuncs;
            }
        }
        private static Dictionary<string, Type> _NameToType;
        public static Dictionary<string, Type> NameToType
        {
            get
            {
                if (_NameToType == null)
                {
                    _NameToType = new Dictionary<string, Type>();
                }
                return _NameToType;
            }
        }
        private static Dictionary<Type, string> _TypeToName;
        public static Dictionary<Type, string> TypeToName
        {
            get
            {
                if (_TypeToName == null)
                {
                    _TypeToName = new Dictionary<Type, string>();
                }
                return _TypeToName;
            }
        }

        public static bool WriteProtocolData(this IntPtr l, object data)
        {
            if (data != null)
            {
                TypedDataBridge reg;
                if (TypedSyncFuncs.TryGetValue(data.GetType(), out reg))
                {
                    reg.PushFunc(l, data);
                    return true;
                }
            }
            return false;
        }
        public static bool ReadProtocolData(this IntPtr l, object data)
        {
            if (data != null)
            {
                TypedDataBridge reg;
                if (TypedSyncFuncs.TryGetValue(data.GetType(), out reg))
                {
                    reg.ReadFunc(l, data);
                    return true;
                }
            }
            return false;
        }
        public static void PushProtocol(this IntPtr l, object data)
        {
            l.newtable();
            l.WriteProtocolData(data);
        }
        public static object GetProtocol(this IntPtr l, int index)
        {
            return ProtobufTrans.GetLuaRaw(l, index);
        }
        public static T GetProtocol<T>(this IntPtr l, int index) where T : new()
        {
            var rv = new T();
            l.pushvalue(index);
            l.ReadProtocolData(rv);
            l.pop(1);
            return rv;
        }

        internal sealed class ProtobufTrans : SelfHandled, LuaLib.ILuaTrans
        {
            public bool ShouldCache { get { return false; } }
            public bool Nonexclusive { get { return true; } }

            private static string GetName(IntPtr l, int index)
            {
                l.GetField(index, LS_messageName);
                string str = l.GetString(-1);
                l.pop(1);
                return str;
            }

            public static object GetLuaRaw(IntPtr l, int index)
            {
                var name = GetName(l, index);
                TypedDataBridge bridge;
                if (NamedSyncFuncs.TryGetValue(name, out bridge))
                {
                    var obj = bridge.Create();
                    l.pushvalue(index);
                    bridge.ReadFunc(l, obj);
                    l.pop(1);
                    return obj;
                }

                LuaTable tab = new LuaTable(l, index);
                return tab; // fallback to LuaTable
            }
            public object GetLua(IntPtr l, int index)
            {
                return GetLuaRaw(l, index);
            }

            public static Type GetTypeRaw(IntPtr l, int index)
            {
                var name = GetName(l, index);
                Type type;
                if (NameToType.TryGetValue(name, out type))
                {
                    return type;
                }
                return typeof(LuaTable); // fallback to LuaTable
            }
            public Type GetType(IntPtr l, int index)
            {
                return GetTypeRaw(l, index);
            }

            public static void SetDataRaw(IntPtr l, int index, object val)
            {
                var name = GetName(l, index);
                TypedDataBridge bridge;
                if (NamedSyncFuncs.TryGetValue(name, out bridge))
                {
                    l.pushvalue(index);
                    bridge.PushFunc(l, val);
                    l.pop(1);
                }
            }
            public void SetData(IntPtr l, int index, object val)
            {
                SetDataRaw(l, index, val);
            }
        }
        internal static ProtobufTrans _ProtobufTrans = new ProtobufTrans();

        public class TypeHubProtocolPrecompiled<T> : LuaLib.LuaTypeHub.TypeHubClonedValuePrecompiled<T>, ILuaNative where T : new()
        {
            public override bool Nonexclusive { get { return true; } }
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
                l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS); // #trans
                l.pushnil();
                l.settable(-3);
                PushToLuaCached(l); // ud type
                l.pushlightuserdata(LuaConst.LRKEY_OBJ_META); // ud type #meta
                l.rawget(-2); // ud type meta
                l.setmetatable(-3); // ud type
                l.pop(1); // ud
                l.pushlightuserdata(LuaConst.LRKEY_TYPE_TRANS); // ud #trans
                l.pushvalue(-1); // ud #trans #trans
                l.gettable(-3); // ud #trans trans
                l.rawset(-3); // ud
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

            public static void SetDataRaw(IntPtr l, int index, T val)
            {
                TypedDataBridge bridge;
                if (TypedSyncFuncs.TryGetValue(typeof(T), out bridge))
                {
                    l.pushvalue(index);
                    bridge.PushFunc(l, val);
                    l.pop(1);
                }
            }
            public static T GetLuaRaw(IntPtr l, int index)
            {
                TypedDataBridge bridge;
                if (TypedSyncFuncs.TryGetValue(typeof(T), out bridge))
                {
                    var obj = new T();
                    l.pushvalue(index);
                    bridge.ReadFunc(l, obj);
                    l.pop(1);
                    return obj;
                }
                return default(T);
            }
            public void Wrap(IntPtr l, int index)
            {
                T val = GetLua(l, index);
                PushLua(l, val);
            }
            public void Unwrap(IntPtr l, int index)
            {
                var val = GetLuaRaw(l, index);
                l.newtable(); // ud
                SetDataRaw(l, -1, val);
            }
            public int LuaType { get { return lua.LUA_TTABLE; } }

            public static readonly LuaNativeProtocol<T> LuaHubNative = new LuaNativeProtocol<T>();
        }
        public class LuaNativeProtocol<T> : LuaLib.LuaHub.LuaPushNativeBase<T> where T : new()
        {
            public override T GetLua(IntPtr l, int index)
            {
                return TypeHubProtocolPrecompiled<T>.GetLuaRaw(l, index);
            }
            public override IntPtr PushLua(IntPtr l, T val)
            {
                l.newtable(); // ud
                TypeHubProtocolPrecompiled<T>.SetDataRaw(l, -1, val);
                return IntPtr.Zero;
            }
        }

#if !UNITY_ENGINE && !UNITY_5_3_OR_NEWER && !LUA_STANDALONE_USE_PB_IN_CS
        public static readonly LuaString LS_messageName = new LuaString("messageName");
#endif
    }

#if UNITY_INCLUDE_TESTS
    #region TESTS
    public static class LuaBridgeGeneratorTest
    {
#if UNITY_EDITOR
        [UnityEditor.MenuItem("Test/Protobuf Converter/Test Lua", priority = 200010)]
        public static void TestLua()
        {
            var l = GlobalLua.L.L;
            ModNet.ProtobufMessage val;
            l.DoString(out val, "return { field1 = 0, field2 = 'dsadfgdf', field3 = { field1 = 666, field2 = '\\1\\217\\3大家', field3 = '\\1\\2\\3大家', field4 = {1,2,3,4,5} } }");
            UnityEngine.Debug.LogError(val.ToJson());

            UnityEngine.Debug.LogError(val["field2"].String);

            l.PushLua(val);
            l.CallGlobal("dump", Pack(l.OnStackTop()));
            l.pop(1);
        }
#endif
    }
    #endregion
#endif
}

#if UNITY_ENGINE || UNITY_5_3_OR_NEWER || LUA_STANDALONE_USE_PB_IN_CS
namespace LuaProto
{
    using pb = global::Google.Protobuf;
    using pbc = global::Google.Protobuf.Collections;
    using pbr = global::Google.Protobuf.Reflection;
    using scg = global::System.Collections.Generic;

    public interface IBidirectionConvertible
    {
        void CopyFrom(object message);
        void CopyTo(object message);
    }
    public interface IBidirectionConvertible<T>
    {
        void CopyFrom(T message);
        void CopyTo(T message);
    }
    public interface IProtoConvertible : IBidirectionConvertible
    {
        object Convert();
    }
    public interface IProtoConvertible<T> : IBidirectionConvertible<T>, IProtoConvertible
    {
        new T Convert();
    }
    public interface IWrapperConvertible : IBidirectionConvertible
    {
        object Convert(IntPtr l);
    }
    public interface IWrapperConvertible<T> : IBidirectionConvertible<T>, IWrapperConvertible
    {
        new T Convert(IntPtr l);
    }

    public abstract class BaseLuaProtoWrapper<TWrapper, TProto> : BaseLuaWrapper<TWrapper>, pb::IMessage<TWrapper>, IProtoConvertible<TProto>
        where TWrapper : BaseLuaWrapper, pb::IMessage<TWrapper>, IProtoConvertible<TProto>, new()
        where TProto : pb::IMessage<TProto>, new()
    {
        public BaseLuaProtoWrapper() { }
        public BaseLuaProtoWrapper(IntPtr l) : base(l) { }

        public override BaseLua Binding
        {
            get { return base.Binding; }
            set
            {
                if (!ReferenceEquals(value, null))
                {
                    var l = value.L;
                    using (var lr = l.CreateStackRecover())
                    {
                        l.PushLua(value);
                        l.PushString("messageName");
                        l.PushString(ProtoTemplate.Descriptor.FullName);
                        l.settable(-3);
                    }
                }
                base.Binding = value;
            }
        }

        protected static readonly TProto ProtoTemplate = new TProto();
        public pbr.MessageDescriptor Descriptor { get { return ProtoTemplate.Descriptor; } }
        // read data to raw proto obj from stream. And then read data from raw proto obj.
        public void MergeFrom(pb.CodedInputStream input)
        {
            var template = new TProto();
            template.MergeFrom(input);
            CopyFrom(template);
        }
        // write data to raw proto obj. And then write raw proto obj to stream.
        public void WriteTo(pb.CodedOutputStream output)
        {
            Convert().WriteTo(output);
        }
        // convert to raw proto obj and calculate size.
        public int CalculateSize()
        {
            return Convert().CalculateSize();
        }
        public TWrapper Clone()
        {
            return new TWrapper() { Binding = Binding.Clone() };
        }
        // compares whether they point to the same lua-table.
        public bool Equals(TWrapper other)
        {
            return Equals(Binding, other == null ? null : other.Binding);
        }
        // just point to the same lua-table. not really copy data.
        public void MergeFrom(TWrapper other)
        {
            Binding = other == null ? null : other.Binding;
        }

        public static TProto Convert(TWrapper wrapper)
        {
            if (wrapper == null)
            {
                return default(TProto);
            }
            var result = new TProto();
            wrapper.CopyTo(result);
            return result;
        }
        public TProto Convert()
        {
            var result = new TProto();
            CopyTo(result);
            return result;
        }

        public abstract void CopyFrom(TProto message);
        public abstract void CopyTo(TProto message);

        void IBidirectionConvertible.CopyFrom(object message)
        {
            if (message is TProto)
            {
                CopyFrom((TProto)message);
            }
        }
        void IBidirectionConvertible.CopyTo(object message)
        {
            if (message is TProto)
            {
                CopyTo((TProto)message);
            }
        }
        object IProtoConvertible.Convert()
        {
            return Convert();
        }
    }

    public static class LuaProtoWrapperExtensions
    {
        public static void ConvertField<TDest, TSrc>(this IntPtr l, out TDest dest, TSrc src)
        {
            if (src is ILuaWrapper)
            {
                var convertible = src as IProtoConvertible<TDest>;
                dest = convertible.Convert();
            }
            else
            {
                var convertible = src as IWrapperConvertible<TDest>;
                dest = convertible.Convert(l);
            }
        }
        public static void ConvertField<TDest, TSrc>(this ILuaWrapper thiz, out TDest dest, TSrc src)
        {
            ConvertField(thiz.Binding.L, out dest, src);
        }
        public static void ConvertField<T>(this ILuaWrapper thiz, out T dest, T src)
        {
            dest = src;
        }
        public static void ConvertField<TDest, TSrc>(this ILuaWrapper thiz, pbc.RepeatedField<TDest> dest, LuaList<TSrc> src)
        {
            dest.Clear();
            src.ForEach(item =>
            {
                TDest ditem;
                ConvertField(thiz, out ditem, item);
                dest.Add(ditem);
            });
        }
        public static void ConvertField<T>(this pbc.RepeatedField<T> dest, LuaList<T> src)
        {
            dest.Clear();
            src.ForEach(item =>
            {
                dest.Add(item);
            });
        }
        public static void ConvertField(this pbc.RepeatedField<pb.ByteString> dest, LuaList<byte[]> src)
        {
            dest.Clear();
            src.ForEach(item =>
            {
                dest.Add(pb.ByteString.CopyFrom(item));
            });
        }
        public static void ConvertField<TDest, TSrc>(this ILuaWrapper thiz, LuaList<TDest> dest, pbc.RepeatedField<TSrc> src)
        {
            dest.Clear();
            for (int i = 0; i < src.Count; ++i)
            {
                var item = src[i];
                TDest ditem;
                ConvertField(thiz, out ditem, item);
                dest.Add(ditem);
            }
        }
        public static void ConvertField<T>(this LuaList<T> dest, pbc.RepeatedField<T> src)
        {
            dest.Clear();
            for (int i = 0; i < src.Count; ++i)
            {
                var item = src[i];
                dest.Add(item);
            }
        }
        public static void ConvertField(this LuaList<byte[]> dest, pbc.RepeatedField<pb.ByteString> src)
        {
            dest.Clear();
            for (int i = 0; i < src.Count; ++i)
            {
                var item = src[i];
                dest.Add(item.ToByteArray());
            }
        }
        public static pb.ByteString ToByteString(this byte[] data)
        {
            if (data == null)
            {
                return pb.ByteString.Empty;
            }
            else
            {
                return pb.ByteString.CopyFrom(data);
            }
        }
        //public static LuaList<TDest> ConvertField<TDest, TSrc>(this pbc.RepeatedField<TSrc> src, IntPtr l)
        //{
        //    var dest = new LuaList<TDest>(l);
        //    for (int i = 0; i < src.Count; ++i)
        //    {
        //        var item = src[i];
        //        TDest ditem;
        //        ConvertField(l, out ditem, item);
        //        dest.Add(ditem);
        //    }
        //    return dest;
        //}
        //public static LuaList<T> ConvertField<T>(this pbc.RepeatedField<T> src, IntPtr l)
        //{
        //    var dest = new LuaList<T>(l);
        //    for (int i = 0; i < src.Count; ++i)
        //    {
        //        var item = src[i];
        //        dest.Add(item);
        //    }
        //    return dest;
        //}
        public static T ConvertField<T>(this IWrapperConvertible<T> src, IntPtr l)
        {
            if (src == null)
            {
                return default(T);
            }
            else
            {
                return src.Convert(l);
            }
        }
        public static T ConvertField<T>(this IProtoConvertible<T> src)
        {
            if (src == null)
            {
                return default(T);
            }
            else
            {
                return src.Convert();
            }
        }
    }
}
#endif