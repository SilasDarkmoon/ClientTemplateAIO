using System;

using LuaLib;
using lua = LuaLib.LuaCoreLib;
using lual = LuaLib.LuaAuxLib;
using luae = LuaLib.LuaLibEx;

namespace LuaLib
{
    public static class LuaCrossTransfer
    {
        public static void Transfer(this IntPtr from, int index, IntPtr to)
        {
            switch (from.type(index))
            {
                case lua.LUA_TNONE:
                case lua.LUA_TNIL:
                    to.pushnil();
                    break;
                case lua.LUA_TBOOLEAN:
                    to.pushboolean(from.toboolean(index));
                    break;
                case lua.LUA_TLIGHTUSERDATA:
                    to.pushlightuserdata(from.touserdata(index));
                    break;
                case lua.LUA_TNUMBER:
                    to.pushnumber(from.tonumber(index));
                    break;
                case lua.LUA_TSTRING:
                    to.pushbuffer(from.tolstring(index));
                    break;
                case lua.LUA_TTABLE:
                    {
                        var absindex = from.NormalizeIndex(index);
                        from.checkstack(3);
                        to.checkstack(3);
                        to.newtable();
                        var totabindex = to.NormalizeIndex(-1);
                        from.pushvalue(absindex);
                        from.pushnil();
                        while (from.next(-2))
                        {
                            Transfer(from, -2, to);
                            Transfer(from, -1, to);
                            to.settable(totabindex);
                            from.pop(1);
                        }
                        from.pop(1);
                    }
                    // TODO: metatable.
                    break;
                case lua.LUA_TFUNCTION:
                case lua.LUA_TUSERDATA:
                case lua.LUA_TTHREAD:
                    // TODO: can these values be copied?
                    break;
                default:
                    break;
            }
        }
        public static BaseLua Transfer(this BaseLua from, IntPtr to)
        {
            var froml = from.L;
            froml.PushLua(from);
            Transfer(froml, -1, to);
            froml.pop(1);
            return new BaseLua(to, to.refer());
        }

        private static void TransferSafe(this IntPtr from, int index, IntPtr to, int mapindex)
        {
            switch (from.type(index))
            {
                case lua.LUA_TNONE:
                case lua.LUA_TNIL:
                    to.pushnil();
                    break;
                case lua.LUA_TBOOLEAN:
                    to.pushboolean(from.toboolean(index));
                    break;
                case lua.LUA_TLIGHTUSERDATA:
                    to.pushlightuserdata(from.touserdata(index));
                    break;
                case lua.LUA_TNUMBER:
                    to.pushnumber(from.tonumber(index));
                    break;
                case lua.LUA_TSTRING:
                    to.pushbuffer(from.tolstring(index));
                    break;
                case lua.LUA_TTABLE:
                    TransferTableSafe(from, index, to, mapindex);
                    // TODO: metatable.
                    break;
                case lua.LUA_TFUNCTION:
                case lua.LUA_TUSERDATA:
                case lua.LUA_TTHREAD:
                    // TODO: can these values be copied?
                    break;
                default:
                    break;
            }
        }
        private static void TransferTableSafe(this IntPtr from, int index, IntPtr to, int mapindex)
        {
            to.checkstack(3);
            var p = from.topointer(index);
            to.pushlightuserdata(p);
            to.gettable(mapindex);
            if (to.istable(-1))
            {
                return;
            }

            var absindex = from.NormalizeIndex(index);
            from.checkstack(3);
            to.newtable();
            var totabindex = to.NormalizeIndex(-1);
            from.pushvalue(absindex);
            from.pushnil();
            while (from.next(-2))
            {
                TransferSafe(from, -2, to, mapindex);
                TransferSafe(from, -1, to, mapindex);
                to.settable(totabindex);
                from.pop(1);
            }
            from.pop(1);

            to.pushlightuserdata(p);
            to.pushvalue(-2);
            to.settable(mapindex);
        }
        public static void TransferSafe(this IntPtr from, int index, IntPtr to)
        {
            switch (from.type(index))
            {
                case lua.LUA_TNONE:
                case lua.LUA_TNIL:
                    to.pushnil();
                    break;
                case lua.LUA_TBOOLEAN:
                    to.pushboolean(from.toboolean(index));
                    break;
                case lua.LUA_TLIGHTUSERDATA:
                    to.pushlightuserdata(from.touserdata(index));
                    break;
                case lua.LUA_TNUMBER:
                    to.pushnumber(from.tonumber(index));
                    break;
                case lua.LUA_TSTRING:
                    to.pushbuffer(from.tolstring(index));
                    break;
                case lua.LUA_TTABLE:
                    {
                        var absindex = from.NormalizeIndex(index);
                        to.newtable();
                        var mapindex = to.NormalizeIndex(-1);
                        TransferTableSafe(from, absindex, to, mapindex);
                        to.remove(mapindex);
                    }
                    // TODO: metatable.
                    break;
                case lua.LUA_TFUNCTION:
                case lua.LUA_TUSERDATA:
                case lua.LUA_TTHREAD:
                    // TODO: can these values be copied?
                    break;
                default:
                    break;
            }
        }
        public static BaseLua TransferSafe(this BaseLua from, IntPtr to)
        {
            var froml = from.L;
            froml.PushLua(from);
            TransferSafe(froml, -1, to);
            froml.pop(1);
            return new BaseLua(to, to.refer());
        }

        public static bool Equals(this IntPtr from, int index, IntPtr to, int toindex)
        {
            var ftype = from.type(index);
            var ttype = to.type(toindex);
            if (ftype == lua.LUA_TNONE || ftype == lua.LUA_TNIL)
            {
                return ttype == lua.LUA_TNONE || ttype == lua.LUA_TNIL;
            }
            else
            {
                if (ftype != ttype)
                {
                    return false;
                }
            }
            switch (ftype)
            {
                case lua.LUA_TBOOLEAN:
                    return from.toboolean(index) == to.toboolean(toindex);
                case lua.LUA_TLIGHTUSERDATA:
                case lua.LUA_TUSERDATA:
                    return from.touserdata(index) == to.touserdata(toindex);
                case lua.LUA_TNUMBER:
                    return from.tonumber(index) == to.tonumber(toindex);
                case lua.LUA_TSTRING:
                    return from.tostring(index) == to.tostring(toindex);
                case lua.LUA_TTHREAD:
                    return from.tothread(index) == to.tothread(toindex);
                case lua.LUA_TTABLE:
                    return EqualsTable(from, index, to, toindex);
                // TODO: metatable.
                case lua.LUA_TFUNCTION:
                    return from.topointer(index) == to.topointer(toindex);
                default:
                    return false;
            }
        }
        public static bool Equals(this BaseLua from, BaseLua to)
        {
            var froml = from.L;
            froml.PushLua(from);
            var absfromindex = froml.NormalizeIndex(-1);
            var tol = to.L;
            tol.PushLua(to);
            var result = Equals(froml, absfromindex, tol, -1);
            tol.pop(1);
            froml.pop(1);
            return result;
        }
        private static bool EqualsTable(this IntPtr from, int index, IntPtr to, int toindex)
        {
            index = from.NormalizeIndex(index);
            toindex = to.NormalizeIndex(toindex);
            to.checkstack(3);

            to.newtable(); // copy
            var tocopyindex = to.NormalizeIndex(-1);
            to.pushnil();
            while (to.next(toindex))
            {
                var keytype = to.type(-2);
                var valtype = to.type(-1);
                if ((keytype == lua.LUA_TBOOLEAN || keytype == lua.LUA_TLIGHTUSERDATA || keytype == lua.LUA_TNUMBER || keytype == lua.LUA_TSTRING)
                    && (valtype == lua.LUA_TBOOLEAN || valtype == lua.LUA_TLIGHTUSERDATA || valtype == lua.LUA_TNUMBER || valtype == lua.LUA_TSTRING || valtype == lua.LUA_TTABLE))
                {
                    to.pushvalue(-2);
                    to.pushvalue(-2);
                    to.settable(tocopyindex);
                }
                to.pop(1);
            }

            bool same = true;
            from.checkstack(3);
            from.pushnil();
            while (from.next(index))
            {
                var fromvalindex = from.NormalizeIndex(-1);
                if (!TransferKey(from, -2, to))
                {
                    from.pop(1);
                    continue; // ignore invalid key. - should we return false for this?
                }
                to.pushvalue(-1); // key key
                to.gettable(tocopyindex); // key value
                switch (from.type(fromvalindex))
                {
                    case lua.LUA_TBOOLEAN:
                    case lua.LUA_TLIGHTUSERDATA:
                    case lua.LUA_TNUMBER:
                    case lua.LUA_TSTRING:
                    case lua.LUA_TTABLE:
                        same = same && Equals(from, fromvalindex, to, -1);
                        break;
                    case lua.LUA_TUSERDATA:
                    case lua.LUA_TFUNCTION:
                    case lua.LUA_TTHREAD:
                    case lua.LUA_TNONE:
                    case lua.LUA_TNIL:
                    default:
                        break; // ignore these complex types?
                }
                to.pop(1); // key
                to.pushnil(); // key nil
                to.settable(tocopyindex);

                if (!same)
                {
                    from.pop(2);
                    break;
                }
                from.pop(1);
            }

            if (same)
            {
                to.pushnil();
                if (to.next(tocopyindex))
                {
                    to.pop(2);
                    same = false;
                }
            }

            return same;
        }
        private static bool EqualsSafe(this IntPtr from, int index, IntPtr to, int toindex, int mapindex)
        {
            var ftype = from.type(index);
            var ttype = to.type(toindex);
            if (ftype == lua.LUA_TNONE || ftype == lua.LUA_TNIL)
            {
                return ttype == lua.LUA_TNONE || ttype == lua.LUA_TNIL;
            }
            else
            {
                if (ftype != ttype)
                {
                    return false;
                }
            }
            switch (ftype)
            {
                case lua.LUA_TBOOLEAN:
                    return from.toboolean(index) == to.toboolean(toindex);
                case lua.LUA_TLIGHTUSERDATA:
                case lua.LUA_TUSERDATA:
                    return from.touserdata(index) == to.touserdata(toindex);
                case lua.LUA_TNUMBER:
                    return from.tonumber(index) == to.tonumber(toindex);
                case lua.LUA_TSTRING:
                    return from.tostring(index) == to.tostring(toindex);
                case lua.LUA_TTHREAD:
                    return from.tothread(index) == to.tothread(toindex);
                case lua.LUA_TTABLE:
                    return EqualsTableSafe(from, index, to, toindex, mapindex);
                // TODO: metatable.
                case lua.LUA_TFUNCTION:
                    return from.topointer(index) == to.topointer(toindex);
                default:
                    return false;
            }
        }
        private static bool TransferKey(this IntPtr from, int index, IntPtr to)
        {
            switch (from.type(index))
            {
                case lua.LUA_TBOOLEAN:
                    to.pushboolean(from.toboolean(index));
                    return true;
                case lua.LUA_TLIGHTUSERDATA:
                    to.pushlightuserdata(from.touserdata(index));
                    return true;
                case lua.LUA_TNUMBER:
                    to.pushnumber(from.tonumber(index));
                    return true;
                case lua.LUA_TSTRING:
                    to.pushbuffer(from.tolstring(index));
                    return true;
                case lua.LUA_TTABLE:
                case lua.LUA_TUSERDATA:
                case lua.LUA_TFUNCTION:
                case lua.LUA_TTHREAD:
                case lua.LUA_TNONE:
                case lua.LUA_TNIL:
                default:
                    return false;
            }
        }
        private static bool EqualsTableSafe(this IntPtr from, int index, IntPtr to, int toindex, int mapindex)
        {
            index = from.NormalizeIndex(index);
            toindex = to.NormalizeIndex(toindex);
            to.checkstack(3);
            var p = from.topointer(index);
            to.pushlightuserdata(p);
            to.gettable(mapindex);
            if (to.equal(-1, toindex))
            {
                to.pop(1);
                return true;
            }
            else if (!to.isnoneornil(-1))
            {
                to.pop(1);
                return false;
            }
            to.pop(1);

            to.pushlightuserdata(p);
            to.pushvalue(toindex);
            to.settable(mapindex);

            to.newtable(); // copy
            var tocopyindex = to.NormalizeIndex(-1);
            to.pushnil();
            while (to.next(toindex))
            {
                var keytype = to.type(-2);
                var valtype = to.type(-1);
                if ((keytype == lua.LUA_TBOOLEAN || keytype == lua.LUA_TLIGHTUSERDATA || keytype == lua.LUA_TNUMBER || keytype == lua.LUA_TSTRING)
                    && (valtype == lua.LUA_TBOOLEAN || valtype == lua.LUA_TLIGHTUSERDATA || valtype == lua.LUA_TNUMBER || valtype == lua.LUA_TSTRING || valtype == lua.LUA_TTABLE))
                {
                    to.pushvalue(-2);
                    to.pushvalue(-2);
                    to.settable(tocopyindex);
                }
                to.pop(1);
            }

            bool same = true;
            from.checkstack(3);
            from.pushnil();
            while (from.next(index))
            {
                var fromvalindex = from.NormalizeIndex(-1);
                if (!TransferKey(from, -2, to))
                {
                    from.pop(1);
                    continue; // ignore invalid key. - should we return false for this?
                }
                to.pushvalue(-1); // key key
                to.gettable(tocopyindex); // key value
                switch (from.type(fromvalindex))
                {
                    case lua.LUA_TBOOLEAN:
                    case lua.LUA_TLIGHTUSERDATA:
                    case lua.LUA_TNUMBER:
                    case lua.LUA_TSTRING:
                    case lua.LUA_TTABLE:
                        same = same && EqualsSafe(from, fromvalindex, to, -1, mapindex);
                        break;
                    case lua.LUA_TUSERDATA:
                    case lua.LUA_TFUNCTION:
                    case lua.LUA_TTHREAD:
                    case lua.LUA_TNONE:
                    case lua.LUA_TNIL:
                    default:
                        break; // ignore these complex types?
                }
                to.pop(1); // key
                to.pushnil(); // key nil
                to.settable(tocopyindex);

                if (!same)
                {
                    from.pop(2);
                    break;
                }
                from.pop(1);
            }

            if (same)
            {
                to.pushnil();
                if (to.next(tocopyindex))
                {
                    to.pop(2);
                    same = false;
                }
            }

            if (!same)
            {
                to.pushlightuserdata(p);
                to.pushboolean(false);
                to.settable(mapindex);
            }

            return same;
        }
        public static bool EqualsSafe(this IntPtr from, int index, IntPtr to, int toindex)
        {
            var ftype = from.type(index);
            var ttype = to.type(toindex);
            if (ftype == lua.LUA_TNONE || ftype == lua.LUA_TNIL)
            {
                return ttype == lua.LUA_TNONE || ttype == lua.LUA_TNIL;
            }
            else
            {
                if (ftype != ttype)
                {
                    return false;
                }
            }
            switch (ftype)
            {
                case lua.LUA_TBOOLEAN:
                    return from.toboolean(index) == to.toboolean(toindex);
                case lua.LUA_TLIGHTUSERDATA:
                case lua.LUA_TUSERDATA:
                    return from.touserdata(index) == to.touserdata(toindex);
                case lua.LUA_TNUMBER:
                    return from.tonumber(index) == to.tonumber(toindex);
                case lua.LUA_TSTRING:
                    return from.tostring(index) == to.tostring(toindex);
                case lua.LUA_TTHREAD:
                    return from.tothread(index) == to.tothread(toindex);
                case lua.LUA_TTABLE:
                    {
                        var absfromindex = from.NormalizeIndex(index);
                        var abstoindex = to.NormalizeIndex(toindex);
                        to.newtable();
                        var mapindex = to.NormalizeIndex(-1);
                        var result = EqualsTableSafe(from, absfromindex, to, abstoindex, mapindex);
                        to.remove(mapindex);
                        return result;
                    }
                    // TODO: metatable.
                case lua.LUA_TFUNCTION:
                    return from.topointer(index) == to.topointer(toindex);
                default:
                    return false;
            }
        }
        public static bool EqualsSafe(this BaseLua from, BaseLua to)
        {
            var froml = from.L;
            froml.PushLua(from);
            var absfromindex = froml.NormalizeIndex(-1);
            var tol = to.L;
            tol.PushLua(to);
            var result = EqualsSafe(froml, absfromindex, tol, -1);
            tol.pop(1);
            froml.pop(1);
            return result;
        }
    }
}