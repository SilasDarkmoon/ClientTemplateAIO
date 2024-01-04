using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using LuaLib;

using lua = LuaLib.LuaCoreLib;
using lual = LuaLib.LuaAuxLib;
using luae = LuaLib.LuaLibEx;

namespace LuaLib
{
    public sealed class LuaOnStackTable : BaseLuaOnStack
    {
        internal class LuaTableFieldsProvider : BaseFieldsProvider
        {
            public BaseLuaOnStack _Tab;
            public LuaTableFieldsProvider(BaseLuaOnStack tab)
            {
                _Tab = tab;
            }
            public int ArrLength
            {
                get
                {
                    if (_Tab.L != IntPtr.Zero)
                    {
                        return _Tab.L.getn(_Tab.StackPos);
                    }
                    return 0;
                }
            }

            protected internal override object GetValueImp(object key)
            {
                return _Tab[key];
            }
            protected internal override object SetValueImp(object key, object val)
            {
                return _Tab[key] = val;
            }
            protected internal override IEnumerator<KeyValuePair<object, object>> GetEnumeratorImp()
            {
                if (_Tab.L != IntPtr.Zero)
                {
                    return _Tab.L.GetOnStackTableEnumerator(_Tab.StackPos);
                }
                return LuaTableHelper.GetEmptyTableEnumerator();
            }
        }

        internal LuaTableFieldsProvider _FieldsProvider = null;
        public IFieldsProvider FieldsProvider
        {
            get
            {
                if (_FieldsProvider == null)
                {
                    _FieldsProvider = new LuaTableFieldsProvider(this);
                }
                return _FieldsProvider;
            }
        }

        public override string ToString()
        {
            return "LuaTableOnStack:" + StackPos.ToString();
        }
        protected internal override object GetFieldImp(object key)
        {
            if (_L != IntPtr.Zero)
            {
                if (_L.istable(StackPos))
                {
                    using (LuaStateRecover lr = new LuaStateRecover(_L))
                    {
                        _L.pushvalue(StackPos);
                        _L.PushLua(key);
                        _L.gettable(-2);
                        return _L.GetLua(-1);
                    }
                }
                else
                {
                    DynamicHelper.LogInfo("luatable on stack: not a table.");
                }
            }
            else
            {
                DynamicHelper.LogInfo("luatable on stack: null state.");
            }
            return null;
        }
        protected internal override bool SetFieldImp(object key, object val)
        {
            if (_L != IntPtr.Zero)
            {
                if (_L.istable(StackPos))
                {
                    using (LuaStateRecover lr = new LuaStateRecover(_L))
                    {
                        _L.pushvalue(StackPos);
                        _L.PushLua(key);
                        _L.PushLua(val);
                        _L.settable(-3);
                        return true;
                    }
                }
                else
                {
                    DynamicHelper.LogInfo("luatable on stack: not a table.");
                }
            }
            else
            {
                DynamicHelper.LogInfo("luatable on stack: null state.");
            }
            return false;
        }

        public LuaOnStackTable(IntPtr l)
        {
            L = l;
            if (l != IntPtr.Zero)
            {
                l.newtable();
                StackPos = l.gettop();
            }
        }
        public LuaOnStackTable(IntPtr l, int stackpos)
        {
            L = l;
            if (l != IntPtr.Zero)
            {
                StackPos = stackpos;
            }
        }
    }

    public class LuaTable : BaseLua
    {
        internal class LuaTableFieldsProvider : BaseFieldsProvider
        {
            public BaseLua _Tab;
            public LuaTableFieldsProvider(BaseLua tab)
            {
                _Tab = tab;
            }
            public int ArrLength
            {
                get
                {
                    if (_Tab.L != IntPtr.Zero && _Tab.Refid != 0)
                    {
                        using (LuaStateRecover lr = new LuaStateRecover(_Tab.L))
                        {
                            _Tab.L.getref(_Tab.Refid);
                            return _Tab.L.getn(-1);
                        }
                    }
                    return 0;
                }
            }

            protected internal override object GetValueImp(object key)
            {
                return _Tab[key];
            }
            protected internal override object SetValueImp(object key, object val)
            {
                return _Tab[key] = val;
            }
            protected internal override IEnumerator<KeyValuePair<object, object>> GetEnumeratorImp()
            {
                if (_Tab.L != IntPtr.Zero && _Tab.Refid != 0)
                {
                    return _Tab.L.GetTableEnumerator(_Tab);
                }
                return LuaTableHelper.GetEmptyTableEnumerator();
            }
        }

        internal LuaTableFieldsProvider _FieldsProvider = null;
        public IFieldsProvider FieldsProvider
        {
            get
            {
                if (_FieldsProvider == null)
                {
                    _FieldsProvider = new LuaTableFieldsProvider(this);
                }
                return _FieldsProvider;
            }
        }

        public override string ToString()
        {
            return "LuaTable:" + Refid.ToString();
        }
        protected internal override object GetFieldImp(object key)
        {
            if (_L != IntPtr.Zero && Refid != 0)
            {
                using (LuaStateRecover lr = new LuaStateRecover(_L))
                {
                    _L.getref(Refid);
                    _L.PushLua(key);
                    _L.gettable(-2);
                    return _L.GetLua(-1);
                }
            }
            DynamicHelper.LogInfo("luatable: null ref");
            return null;
        }
        protected internal override bool SetFieldImp(object key, object val)
        {
            if (_L != IntPtr.Zero && Refid != 0)
            {
                using (LuaStateRecover lr = new LuaStateRecover(_L))
                {
                    _L.getref(Refid);
                    _L.PushLua(key);
                    _L.PushLua(val);
                    _L.settable(-3);
                    return true;
                }
            }
            DynamicHelper.LogInfo("luatable: null ref");
            return false;
        }

        public LuaTable(IntPtr l)
        {
            L = l;
            if (l != IntPtr.Zero)
            {
                l.newtable();
                Refid = l.refer();
            }
        }
        public LuaTable(IntPtr l, int stackpos)
        {
            L = l;
            if (l != IntPtr.Zero)
            {
                if (l.istable(stackpos))
                {
                    l.pushvalue(stackpos);
                    Refid = l.refer();
                }
            }
        }
        protected internal LuaTable(IntPtr l, int refid, int reserved)
        {
            L = l;
            Refid = refid;
        }
        protected internal LuaTable(LuaRef lref)
        {
            Ref = lref;
            _L = lref.L;
        }

        public static implicit operator LuaTable(LuaOnStackTable val)
        {
            if (val != null && val.L != IntPtr.Zero)
                return new LuaTable(val.L, val.StackPos);
            return null;
        }
        public static implicit operator LuaOnStackTable(LuaTable val)
        {
            if (val != null && val.L != IntPtr.Zero && val.Refid != 0)
            {
                val.L.getref(val.Refid);
                return new LuaOnStackTable(val.L, val.L.gettop());
            }
            return null;
        }
    }

    public sealed class LuaOnStackRawTable : BaseLuaOnStack
    {
        internal LuaOnStackTable.LuaTableFieldsProvider _FieldsProvider = null;
        public IFieldsProvider FieldsProvider
        {
            get
            {
                if (_FieldsProvider == null)
                {
                    _FieldsProvider = new LuaOnStackTable.LuaTableFieldsProvider(this);
                }
                return _FieldsProvider;
            }
        }

        public override string ToString()
        {
            return "LuaTableRawOnStack:" + StackPos.ToString();
        }
        protected internal override object GetFieldImp(object key)
        {
            if (_L != IntPtr.Zero)
            {
                if (_L.istable(StackPos))
                {
                    using (LuaStateRecover lr = new LuaStateRecover(_L))
                    {
                        _L.pushvalue(StackPos);
                        _L.PushLua(key);
                        _L.rawget(-2);
                        return _L.GetLua(-1);
                    }
                }
                else
                {
                    DynamicHelper.LogInfo("luatable raw on stack: not a table.");
                }
            }
            else
            {
                DynamicHelper.LogInfo("luatable raw on stack: null state.");
            }
            return null;
        }
        protected internal override bool SetFieldImp(object key, object val)
        {
            if (_L != IntPtr.Zero)
            {
                if (_L.istable(StackPos))
                {
                    using (LuaStateRecover lr = new LuaStateRecover(_L))
                    {
                        _L.pushvalue(StackPos);
                        _L.PushLua(key);
                        _L.PushLua(val);
                        _L.rawset(-3);
                        return true;
                    }
                }
                else
                {
                    DynamicHelper.LogInfo("luatable raw on stack: not a table.");
                }
            }
            else
            {
                DynamicHelper.LogInfo("luatable raw on stack: null state.");
            }
            return false;
        }

        public LuaOnStackRawTable(IntPtr l)
        {
            L = l;
            if (l != IntPtr.Zero)
            {
                l.newtable();
                StackPos = l.gettop();
            }
        }
        public LuaOnStackRawTable(IntPtr l, int stackpos)
        {
            L = l;
            if (l != IntPtr.Zero)
            {
                StackPos = stackpos;
            }
        }

        public static implicit operator LuaOnStackTable(LuaOnStackRawTable val)
        {
            if (val != null && val.L != IntPtr.Zero)
                return new LuaOnStackTable(val.L, val.StackPos);
            return null;
        }
        public static implicit operator LuaOnStackRawTable(LuaOnStackTable val)
        {
            if (val != null && val.L != IntPtr.Zero && val.Refid != 0)
                return new LuaOnStackRawTable(val.L, val.StackPos);
            return null;
        }
        public static implicit operator LuaTable(LuaOnStackRawTable val)
        {
            if (val != null && val.L != IntPtr.Zero)
                return new LuaTable(val.L, val.StackPos);
            return null;
        }
        public static implicit operator LuaOnStackRawTable(LuaTable val)
        {
            if (val != null && val.L != IntPtr.Zero && val.Refid != 0)
            {
                val.L.getref(val.Refid);
                return new LuaOnStackRawTable(val.L, val.L.gettop());
            }
            return null;
        }
    }

    public class LuaRawTable : BaseLua
    {
        internal LuaTable.LuaTableFieldsProvider _FieldsProvider = null;
        public IFieldsProvider FieldsProvider
        {
            get
            {
                if (_FieldsProvider == null)
                {
                    _FieldsProvider = new LuaTable.LuaTableFieldsProvider(this);
                }
                return _FieldsProvider;
            }
        }

        public override string ToString()
        {
            return "LuaRawTable:" + Refid.ToString();
        }
        protected internal override object GetFieldImp(object key)
        {
            if (_L != IntPtr.Zero && Refid != 0)
            {
                using (LuaStateRecover lr = new LuaStateRecover(_L))
                {
                    _L.getref(Refid);
                    _L.PushLua(key);
                    _L.rawget(-2);
                    return _L.GetLua(-1);
                }
            }
            DynamicHelper.LogInfo("luatable raw: null ref");
            return null;
        }
        protected internal override bool SetFieldImp(object key, object val)
        {
            if (_L != IntPtr.Zero && Refid != 0)
            {
                using (LuaStateRecover lr = new LuaStateRecover(_L))
                {
                    _L.getref(Refid);
                    _L.PushLua(key);
                    _L.PushLua(val);
                    _L.rawset(-3);
                    return true;
                }
            }
            DynamicHelper.LogInfo("luatable raw: null ref");
            return false;
        }

        public LuaRawTable(IntPtr l)
        {
            L = l;
            if (l != IntPtr.Zero)
            {
                l.newtable();
                Refid = l.refer();
            }
        }
        public LuaRawTable(IntPtr l, int stackpos)
        {
            L = l;
            if (l != IntPtr.Zero)
            {
                if (l.istable(stackpos))
                {
                    l.pushvalue(stackpos);
                    Refid = l.refer();
                }
            }
        }
        protected internal LuaRawTable(IntPtr l, int refid, int reserved)
        {
            L = l;
            Refid = refid;
        }
        protected internal LuaRawTable(LuaRef lref)
        {
            Ref = lref;
            _L = lref.L;
        }

        public static implicit operator LuaRawTable(LuaOnStackTable val)
        {
            if (val != null && val.L != IntPtr.Zero)
                return new LuaRawTable(val.L, val.StackPos);
            return null;
        }
        public static implicit operator LuaOnStackTable(LuaRawTable val)
        {
            if (val != null && val.L != IntPtr.Zero && val.Refid != 0)
            {
                val.L.getref(val.Refid);
                return new LuaOnStackTable(val.L, val.L.gettop());
            }
            return null;
        }
        public static implicit operator LuaRawTable(LuaOnStackRawTable val)
        {
            if (val != null && val.L != IntPtr.Zero)
                return new LuaRawTable(val.L, val.StackPos);
            return null;
        }
        public static implicit operator LuaOnStackRawTable(LuaRawTable val)
        {
            if (val != null && val.L != IntPtr.Zero && val.Refid != 0)
            {
                val.L.getref(val.Refid);
                return new LuaOnStackRawTable(val.L, val.L.gettop());
            }
            return null;
        }
        public static implicit operator LuaRawTable(LuaTable val)
        {
            if (val != null)
                return new LuaRawTable(val.Ref);
                //return new LuaRawTable(val.L, val.Refid, 0);
            return null;
        }
        public static implicit operator LuaTable(LuaRawTable val)
        {
            if (val != null)
                return new LuaTable(val.Ref);
                //return new LuaTable(val.L, val.Refid, 0);
            return null;
        }
    }

    public static class LuaTableHelper
    {
        public static IEnumerator<KeyValuePair<object, object>> GetOnStackTableEnumerator(this IntPtr l, int index)
        {
            if (l != IntPtr.Zero && l.istable(index))
            {
                object tab = l.GetLua(index);
                return GetTableEnumerator(l, tab);
            }
            return GetEmptyTableEnumerator();
        }
        internal static IEnumerator<KeyValuePair<object, object>> GetTableEnumerator(this IntPtr l, object tab)
        {
            if (l != IntPtr.Zero)
            {
                object key = null;
                while (true)
                {
                    l.PushLua(tab);
                    l.PushLua(key);
                    if (l.next(-2))
                    {
                        key = l.GetLua(-2);
                        object val = l.GetLua(-1);
                        l.pop(3);
                        yield return new KeyValuePair<object, object>(key, val);
                    }
                    else
                    {
                        l.pop(1);
                        yield break;
                    }
                }
            }
        }
        public static IEnumerator<KeyValuePair<object, object>> GetEmptyTableEnumerator()
        {
            yield break;
        }
    }
}

namespace LuaLib
{
    public static partial class LuaHub
    {
        public static void PushLua(this IntPtr l, LuaLib.LuaOnStackTable val)
        {
            l.pushvalue(val.StackPos);
        }
        public static void PushLua(this IntPtr l, LuaLib.LuaTable val)
        {
            l.getref(val.Refid);
        }
        public static void PushLua(this IntPtr l, LuaLib.LuaOnStackRawTable val)
        {
            l.pushvalue(val.StackPos);
        }
        public static void PushLua(this IntPtr l, LuaLib.LuaRawTable val)
        {
            l.getref(val.Refid);
        }
        public static void GetLua(this IntPtr l, int index, out LuaLib.LuaTable val)
        {
            if (l.istable(index) || IsUserDataTable(l, index))
            {
                val = new LuaLib.LuaTable(l, index);
                return;
            }
            val = null;
            return;
        }
        public static void GetLua(this IntPtr l, int index, out LuaLib.LuaOnStackTable val)
        {
            if (l.istable(index) || IsUserDataTable(l, index))
            {
                val = new LuaLib.LuaOnStackTable(l, index);
                return;
            }
            val = null;
            return;
        }
        public static void GetLua(this IntPtr l, int index, out LuaLib.LuaRawTable val)
        {
            if (l.istable(index))
            {
                val = new LuaLib.LuaRawTable(l, index);
                return;
            }
            val = null;
            return;
        }
        public static void GetLua(this IntPtr l, int index, out LuaLib.LuaOnStackRawTable val)
        {
            if (l.istable(index))
            {
                val = new LuaLib.LuaOnStackRawTable(l, index);
                return;
            }
            val = null;
            return;
        }

        private class LuaPushNative_LuaOnStackTable : LuaPushNativeBase<LuaLib.LuaOnStackTable>
        {
            public override LuaOnStackTable GetLua(IntPtr l, int index)
            {
                return new LuaOnStackTable(l, index);
            }
            public override IntPtr PushLua(IntPtr l, LuaLib.LuaOnStackTable val)
            {
                l.pushvalue(val.StackPos);
                return IntPtr.Zero;
            }
        }
        private static LuaPushNative_LuaOnStackTable ___tpn_LuaOnStackTable = new LuaPushNative_LuaOnStackTable();
        private class LuaPushNative_LuaTable : LuaPushNativeBase<LuaLib.LuaTable>
        {
            public override LuaTable GetLua(IntPtr l, int index)
            {
                return new LuaTable(l, index);
            }
            public override IntPtr PushLua(IntPtr l, LuaLib.LuaTable val)
            {
                l.getref(val.Refid);
                return IntPtr.Zero;
            }
        }
        private static LuaPushNative_LuaTable ___tpn_LuaTable = new LuaPushNative_LuaTable();
        private class LuaPushNative_LuaOnStackRawTable : LuaPushNativeBase<LuaLib.LuaOnStackRawTable>
        {
            public override LuaOnStackRawTable GetLua(IntPtr l, int index)
            {
                return new LuaOnStackRawTable(l, index);
            }
            public override IntPtr PushLua(IntPtr l, LuaLib.LuaOnStackRawTable val)
            {
                l.pushvalue(val.StackPos);
                return IntPtr.Zero;
            }
        }
        private static LuaPushNative_LuaOnStackRawTable ___tpn_LuaOnStackRawTable = new LuaPushNative_LuaOnStackRawTable();
        private class LuaPushNative_LuaRawTable : LuaPushNativeBase<LuaLib.LuaRawTable>
        {
            public override LuaRawTable GetLua(IntPtr l, int index)
            {
                return new LuaRawTable(l, index);
            }
            public override IntPtr PushLua(IntPtr l, LuaLib.LuaRawTable val)
            {
                l.getref(val.Refid);
                return IntPtr.Zero;
            }
        }
        private static LuaPushNative_LuaRawTable ___tpn_LuaRawTable = new LuaPushNative_LuaRawTable();
    }
}