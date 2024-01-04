using System;
using System.Collections.Generic;

using lua = LuaLib.LuaCoreLib;
using lual = LuaLib.LuaAuxLib;
using luae = LuaLib.LuaLibEx;

namespace LuaLib
{
    public class LuaString
    {
        public readonly string Str;
        public readonly int Index;

        private static int NextIndex = -1;

        private static Dictionary<string, LuaString> CacheMap = new Dictionary<string, LuaString>();
        private static Dictionary<int, LuaString> CacheRevMap = new Dictionary<int, LuaString>();

#if DEBUG_LUA_THREADSAFE
        private static readonly object _ThreadCheckLock = new object();
        private static void CheckThread()
        {
            if (System.Threading.Monitor.TryEnter(_ThreadCheckLock))
            {
                System.Threading.Monitor.Exit(_ThreadCheckLock);
            }
            else
            {
                UnityEngineEx.PlatDependant.LogError("Please do not register LuaString when using them in another thread!");
            }
        }
#endif

        public LuaString(string str)
            : this(str, 0)
        {
        }
        public LuaString(string str, int index)
        {
#if DEBUG_LUA_THREADSAFE
            System.Threading.Monitor.Enter(_ThreadCheckLock);
            try
            { 
#endif
            Str = str ?? "";
            LuaString old;
            if (CacheMap.TryGetValue(Str, out old))
            {
                Index = old.Index;
            }
            else
            {
                bool valid = true;
                if (index >= 0)
                {
                    valid = false;
                }
                else if (CacheRevMap.TryGetValue(index, out old))
                {
                    valid = false;
                }
                if (valid)
                {
                    Index = index;
                    if (NextIndex >= index)
                    {
                        NextIndex = index - 1;
                    }
                }
                else
                {
                    Index = NextIndex--;
                }
                CacheMap[Str] = this;
                CacheRevMap[Index] = this;
            }
#if DEBUG_LUA_THREADSAFE
            }
            finally
            {
                System.Threading.Monitor.Exit(_ThreadCheckLock);
            }
#endif
        }

        public void PushString(IntPtr l)
        {
            if (!LuaStringTransHelper.PushString(l, Index))
            {
                LuaStringTransHelper.PushAndRegString(l, Index, Str);
            }
        }

        public static LuaString GetString(string str)
        {
            if (str == null) return null;
#if DEBUG_LUA_THREADSAFE
            CheckThread();
#endif
            LuaString val;
            CacheMap.TryGetValue(str, out val);
            return val;
        }
        public static LuaString GetString(int index)
        {
#if DEBUG_LUA_THREADSAFE
            CheckThread();
#endif
            LuaString val;
            CacheRevMap.TryGetValue(index, out val);
            return val;
        }

        public static string EscapeToLuaString(byte[] data)
        {
            if (data == null)
            {
                return null;
            }
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < data.Length; ++i)
            {
                sb.Append("\\");
                sb.Append(data[i].ToString("000"));
            }
            return sb.ToString();
        }
        public static string EscapeToLuaString(string str)
        {
            return EscapeToLuaString(System.Text.Encoding.UTF8.GetBytes(str));
        }
        //public static string UnescapeLuaString(string lua)
        //{
        //}
        //public static byte[] UnescapeLuaStringToData(string lua)
        //{
        //}

        private static UnityEngineEx.PlatDependant.DataStringFormat _LuaDataStringFormat = new UnityEngineEx.PlatDependant.DataStringFormat()
        {
            EscapeChars = new HashSet<char>() { '\\', '\"', '\'' },
            PreUnicodeEscape = "\\",
            UnicodeEscapeFormat = "000",
        };
        public static string FormatLuaString(byte[] data)
        {
            return UnityEngineEx.PlatDependant.FormatDataString(data, _LuaDataStringFormat);
        }
        public static string FormatLuaString(string raw)
        {
            return UnityEngineEx.PlatDependant.FormatDataString(raw, _LuaDataStringFormat);
        }
    }

    public static class LuaStringTransHelper
    {
        public class LuaStringCache
        {
            public const int InternVisitCount = 100;
            public const int CacheMaxCount = 5000;
            public const int CachedStringMinLen = 100;
            public const int CachedStringMaxLen = 1000;

            public IntPtr L = IntPtr.Zero;
            public int LastId = 0;
            public LinkedList<LuaCachedStringInfo> CacheList = new LinkedList<LuaCachedStringInfo>();
            public Dictionary<string, LuaCachedStringInfo> CacheMap = new Dictionary<string, LuaCachedStringInfo>();
            public Dictionary<int, LuaCachedStringInfo> CacheRevMap = new Dictionary<int, LuaCachedStringInfo>();
            public LinkedListNode<LuaCachedStringInfo>[] CacheIndexStartNode = new LinkedListNode<LuaCachedStringInfo>[InternVisitCount];

            public class LuaCachedStringInfo
            {
                public LuaStringCache Cache;
                public string Str;
                //public byte[] Coded;
                public int Id;
                public LinkedListNode<LuaCachedStringInfo> Node;
                public int VisitCount;
                public bool IsInterned;

                public void Intern()
                {
                    if (!IsInterned)
                    {
                        IsInterned = true;
                        Str = string.Intern(Str);

                        if (Node != null)
                        {
                            Cache.RemoveListNode(Node);
                            Node = null;
                        }
                    }
                }
                public void AddVisitCount()
                {
                    if (Node != null)
                    {
                        Cache.RemoveListNode(Node);
                    }
                    ++VisitCount;
                    if (!IsInterned)
                    {
                        if (VisitCount >= InternVisitCount)
                        {
                            Node = null;
                            Intern();
                        }
                    }
                    if (Node != null)
                    {
                        if (Cache.CacheIndexStartNode[VisitCount] != null)
                        {
                            Node = Cache.CacheList.AddBefore(Cache.CacheIndexStartNode[VisitCount], this);
                            Cache.CacheIndexStartNode[VisitCount] = Node;
                        }
                        else
                        {
                            int vi = VisitCount - 1;
                            for (; vi >= 0; --vi)
                            {
                                if (Cache.CacheIndexStartNode[vi] != null)
                                {
                                    break;
                                }
                            }
                            if (vi >= 0)
                            {
                                Node = Cache.CacheList.AddBefore(Cache.CacheIndexStartNode[vi], this);
                                Cache.CacheIndexStartNode[VisitCount] = Node;
                            }
                            else
                            {
                                Node = Cache.CacheList.AddLast(this);
                                Cache.CacheIndexStartNode[VisitCount] = Node;
                            }
                        }
                    }
                }
            }

            private void RemoveListNode(LinkedListNode<LuaCachedStringInfo> node)
            {
                if (node != null)
                {
                    var info = node.Value;
                    if (CacheIndexStartNode[info.VisitCount] == node)
                    {
                        CacheIndexStartNode[info.VisitCount] = null;
                        var next = node.Next;
                        if (next != null)
                        {
                            if (next.Value.VisitCount == info.VisitCount)
                            {
                                CacheIndexStartNode[info.VisitCount] = next;
                            }
                        }
                    }
                    CacheList.Remove(node);
                }
            }

            public bool TryGetCacheInfo(string val, out LuaCachedStringInfo info)
            {
                var found = CacheMap.TryGetValue(val, out info);
                if (found)
                {
                    info.AddVisitCount();
                }
                return found;
            }
            public bool TryGetCacheInfo(int id, out LuaCachedStringInfo info)
            {
                var found = CacheRevMap.TryGetValue(id, out info);
                if (found)
                {
                    info.AddVisitCount();
                }
                return found;
            }
            public LuaCachedStringInfo PutIntoCache(string str)
            {
                if (str == null)
                {
                    return null;
                }
                LuaCachedStringInfo rv;
                if (TryGetCacheInfo(str, out rv))
                {
                    return rv;
                }
                if (str.Length > CachedStringMaxLen)
                {
                    return null;
                }
                rv = new LuaCachedStringInfo();
                rv.Str = str;
                rv.Cache = this;
                rv.Id = ++LastId;

                if (string.IsInterned(str) != null)
                {
                    rv.IsInterned = true;
                }
                else
                {
                    if (CacheList.Count >= CacheMaxCount)
                    {
                        var last = CacheList.Last.Value;
                        RemoveFromCache(last);
                    }
                    rv.Node = CacheList.AddLast(rv);
                }
                CacheMap[str] = rv;
                CacheRevMap[rv.Id] = rv;
                rv.AddVisitCount();

                var id = rv.Id;
                var l = L;

                RegString(l, id, str);

                return rv;
            }
            public void RemoveFromCache(LuaCachedStringInfo info)
            {
                if (info.Node != null)
                {
                    RemoveListNode(info.Node);
                    info.Node = null;
                }
                CacheMap.Remove(info.Str);
                CacheRevMap.Remove(info.Id);

                var id = info.Id;
                var l = L;

                UnregString(l, id);
            }
        }

        #region for LuaHubC
        //        public static byte[] EncodeString(string str)
        //        {
        //            System.Text.Encoding encoding;
        //#if UNITY_EDITOR_WIN && LUA_USE_SYSTEM_ENCODING_ON_EDITOR_WIN
        //            encoding = System.Text.Encoding.Default;
        //#elif UNITY_EDITOR
        //            encoding = System.Text.Encoding.UTF8;
        //#elif UNITY_WP8 || UNITY_METRO
        //            encoding = System.Text.Encoding.UTF8;
        //#else
        //            encoding = System.Text.Encoding.UTF8;
        //#endif
        //            var len = encoding.GetByteCount(str) + 1;
        //            var buffer = new byte[len];
        //            encoding.GetBytes(str, 0, str.Length, buffer, 0);
        //            return buffer;
        //        }
        internal static bool PushString(IntPtr l, int id)
        {
#if !DISABLE_LUA_HUB_C
            if (LuaHub.LuaHubC.Ready)
            {
                return LuaHub.LuaHubC.lua_pushString(l, id);
            }
#endif
            l.checkstack(10);
            l.pushlightuserdata(LuaConst.LRKEY_STR_CACHE); // rkey
            l.gettable(lua.LUA_REGISTRYINDEX); // reg
            if (!l.istable(-1))
            {
                l.pop(1); // X
                l.newtable(); // reg
                l.pushlightuserdata(LuaConst.LRKEY_STR_CACHE); // reg rkey
                l.pushvalue(-2); // reg rkey reg
                l.settable(lua.LUA_REGISTRYINDEX); // reg
            }

            l.pushnumber(1); // reg 1
            l.gettable(-2); // reg map
            if (!l.istable(-1))
            {
                l.pop(1); // reg
                l.newtable(); // reg map
                l.pushnumber(1); // reg map 1
                l.pushvalue(-2); // reg map 1 map
                l.settable(-4); // reg map
            }

            l.pushnumber(id); // reg map id
            l.gettable(-2); // reg map str
            if (l.type(-1) == LuaCoreLib.LUA_TSTRING)
            {
                l.insert(-3); // str reg map
                l.pop(2); // str
                return true;
            }
            else
            {
                l.pop(1); // reg map
                l.pushnumber(2); // reg map 2
                l.gettable(-3); // reg map revmap
                if (!l.istable(-1))
                {
                    l.pop(1); // reg map
                    l.newtable(); // reg map revmap
                    l.pushnumber(2); // reg map revmap 2
                    l.pushvalue(-2); // reg map revmap 2 revmap
                    l.settable(-5); // reg map revmap
                }
                return false;
            }
        }
        internal static void PushAndRegString(IntPtr l, int id, string str)
        {
#if !DISABLE_LUA_HUB_C
            if (LuaHub.LuaHubC.Ready)
            {
                LuaHub.LuaHubC.lua_pushAndRegString(l, id, str);
                return;
            }
#endif
            l.pushstring(str); // reg map revmap str
            l.pushnumber(id); // reg map revmap str id
            l.pushvalue(-2); // reg map revmap str id str
            l.pushvalue(-1); // reg map revmap str id str str
            l.pushvalue(-3); // reg map revmap str id str str id
            l.settable(-6); // reg map revmap str id str
            l.settable(-5); // reg map revmap str
            l.insert(-4); // str reg map revmap
            l.pop(3); // str
        }
        internal static void PushAndRegString(IntPtr l, int id, byte[] encoded)
        {
#if !DISABLE_LUA_HUB_C
            if (LuaHub.LuaHubC.Ready)
            {
                LuaHub.LuaHubC.lua_pushAndRegString(l, id, encoded);
                return;
            }
#endif
            l.pushbuffer(encoded); // reg map revmap str
            l.pushnumber(id); // reg map revmap str id
            l.pushvalue(-2); // reg map revmap str id str
            l.pushvalue(-1); // reg map revmap str id str str
            l.pushvalue(-3); // reg map revmap str id str str id
            l.settable(-6); // reg map revmap str id str
            l.settable(-5); // reg map revmap str
            l.insert(-4); // str reg map revmap
            l.pop(3); // str
        }
        internal static void RegString(IntPtr l, int id, string str)
        {
#if !DISABLE_LUA_HUB_C
            if (LuaHub.LuaHubC.Ready)
            {
                LuaHub.LuaHubC.lua_regString(l, id, str);
                return;
            }
#endif
            l.checkstack(8);
            l.pushlightuserdata(LuaConst.LRKEY_STR_CACHE); // rkey
            l.gettable(lua.LUA_REGISTRYINDEX); // reg
            if (!l.istable(-1))
            {
                l.pop(1); // X
                l.newtable(); // reg
                l.pushlightuserdata(LuaConst.LRKEY_STR_CACHE); // reg rkey
                l.pushvalue(-2); // reg rkey reg
                l.settable(lua.LUA_REGISTRYINDEX); // reg
            }

            l.pushnumber(1); // reg 1
            l.gettable(-2); // reg map
            if (!l.istable(-1))
            {
                l.pop(1); // reg
                l.newtable(); // reg map
                l.pushnumber(1); // reg map 1
                l.pushvalue(-2); // reg map 1 map
                l.settable(-4); // reg map
            }
            l.pushnumber(2); // reg map 2
            l.gettable(-3); // reg map revmap
            if (!l.istable(-1))
            {
                l.pop(1); // reg map
                l.newtable(); // reg map revmap
                l.pushnumber(2); // reg map revmap 2
                l.pushvalue(-2); // reg map revmap 2 revmap
                l.settable(-5); // reg map revmap
            }

            l.pushnumber(id); // reg map revmap id
            l.pushstring(str); // reg map revmap id str
            l.pushvalue(-1); // reg map revmap id str str
            l.pushvalue(-3); // reg map revmap id str str id
            l.settable(-5); // reg map revmap id str
            l.settable(-4); // reg map revmap
            l.pop(3); // X
        }
        internal static void RegString(IntPtr l, int id, byte[] encoded)
        {
#if !DISABLE_LUA_HUB_C
            if (LuaHub.LuaHubC.Ready)
            {
                LuaHub.LuaHubC.lua_regString(l, id, encoded);
                return;
            }
#endif
            l.checkstack(8);
            l.pushlightuserdata(LuaConst.LRKEY_STR_CACHE); // rkey
            l.gettable(lua.LUA_REGISTRYINDEX); // reg
            if (!l.istable(-1))
            {
                l.pop(1); // X
                l.newtable(); // reg
                l.pushlightuserdata(LuaConst.LRKEY_STR_CACHE); // reg rkey
                l.pushvalue(-2); // reg rkey reg
                l.settable(lua.LUA_REGISTRYINDEX); // reg
            }

            l.pushnumber(1); // reg 1
            l.gettable(-2); // reg map
            if (!l.istable(-1))
            {
                l.pop(1); // reg
                l.newtable(); // reg map
                l.pushnumber(1); // reg map 1
                l.pushvalue(-2); // reg map 1 map
                l.settable(-4); // reg map
            }
            l.pushnumber(2); // reg map 2
            l.gettable(-3); // reg map revmap
            if (!l.istable(-1))
            {
                l.pop(1); // reg map
                l.newtable(); // reg map revmap
                l.pushnumber(2); // reg map revmap 2
                l.pushvalue(-2); // reg map revmap 2 revmap
                l.settable(-5); // reg map revmap
            }

            l.pushnumber(id); // reg map revmap id
            l.pushbuffer(encoded); // reg map revmap id str
            l.pushvalue(-1); // reg map revmap id str str
            l.pushvalue(-3); // reg map revmap id str str id
            l.settable(-5); // reg map revmap id str
            l.settable(-4); // reg map revmap
            l.pop(3); // X
        }
        internal static void UnregString(IntPtr l, int id)
        {
#if !DISABLE_LUA_HUB_C
            if (LuaHub.LuaHubC.Ready)
            {
                LuaHub.LuaHubC.lua_unregString(l, id);
                return;
            }
#endif
            l.checkstack(8);
            l.pushlightuserdata(LuaConst.LRKEY_STR_CACHE); // rkey
            l.gettable(lua.LUA_REGISTRYINDEX); // reg
            if (l.istable(-1))
            {
                l.pushnumber(1); // reg 1
                l.gettable(-2); // reg map
                l.pushnumber(2); // reg map 2
                l.gettable(-3); // reg map revmap
                if (l.istable(-2))
                {
                    l.pushnumber(id); // reg map revmap id
                    l.pushvalue(-1); // reg map revmap id id
                    l.gettable(-4); // reg map revmap id str
                    l.pushvalue(-2); // reg map revmap id str id
                    l.pushnil(); // reg map revmap id str id nil
                    l.settable(-6); // reg map revmap id str
                    if (l.type(-1) == LuaCoreLib.LUA_TSTRING && l.istable(-3))
                    {
                        l.pushnil(); // reg map revmap id str nil
                        l.settable(-4); // reg map revmap id
                        l.pop(1); // reg map revmap
                    }
                    else
                    {
                        l.pop(2); // reg map revmap
                    }
                }
                l.pop(3); // X
            }
            else
            {
                l.pop(1); // X
            }
        }
        internal static int GetStringRegId(IntPtr l, int index)
        {
#if !DISABLE_LUA_HUB_C
            if (LuaHub.LuaHubC.Ready)
            {
                return LuaHub.LuaHubC.lua_getStringRegId(l, index);
            }
#endif
            if (l.type(index) == LuaCoreLib.LUA_TSTRING)
            {
                l.checkstack(5);
                l.pushvalue(index); // lstr
                l.pushlightuserdata(LuaConst.LRKEY_STR_CACHE); // lstr rkey
                l.gettable(lua.LUA_REGISTRYINDEX); // lstr reg
                if (l.istable(-1))
                {
                    l.pushnumber(2); // lstr reg 2
                    l.gettable(-2); // lstr reg revmap

                    if (l.istable(-1))
                    {
                        l.pushvalue(-3); // lstr reg revmap lstr
                        l.gettable(-2); // lstr reg revmap id

                        if (l.isnumber(-1))
                        {
                            var id = (int)l.tonumber(-1);
                            l.pop(4); // X
                            return id;
                        }
                        else
                        {
                            l.pop(4); // X
                        }
                    }
                    else
                    {
                        l.pop(3); // X
                    }
                }
                else
                {
                    l.pop(2); // X
                }
            }
            return 0;
        }
        #endregion

#if DEBUG_LUA_PERFORMANCE
        [ThreadStatic] private static System.Diagnostics.Stopwatch _PushOrGetStringTimingWatch;
        private static System.Diagnostics.Stopwatch PushOrGetStringTimingWatch
        {
            get
            {
                var watch = _PushOrGetStringTimingWatch;
                if (watch == null)
                {
                    _PushOrGetStringTimingWatch = watch = new System.Diagnostics.Stopwatch();
                }
                return watch;
            }
        }
        private static long PushOrGetStringCallCount = 0;
        private static long PushOrGetStringCallTotalTime = 0;
#endif

        public static void PushString(this IntPtr l, string str)
        {
            if (str == null)
            {
                l.pushnil();
            }
            else
            {
                l.pushstring(str);
            }
        }

        public static void PushString(this IntPtr l, LuaString str)
        {
            str.PushString(l);
        }

        public static string GetString(this IntPtr l, int index)
        {
            if (l.isnoneornil(index))
            {
                return null;
            }
            else
            {
                l.pushvalue(index);
                var str = l.tostring(-1);
                l.pop(1);
                return str;
            }
        }

        public static void GetField(this IntPtr l, int index, string key)
        {
            var top = l.gettop();
            if (index < 0 && -index <= top)
            {
                index = top + 1 + index;
            }

            l.PushString(key);
            l.gettable(index);
        }

        public static void GetField(this IntPtr l, int index, LuaString key)
        {
            var top = l.gettop();
            if (index < 0 && -index <= top)
            {
                index = top + 1 + index;
            }

            key.PushString(l);
            l.gettable(index);
        }

        public static void SetField(this IntPtr l, int index, string key)
        {
            var top = l.gettop();
            if (index < 0 && -index <= top)
            {
                index = top + 1 + index;
            }

            l.PushString(key);
            l.insert(-2);
            l.settable(index);
        }

        public static void SetField(this IntPtr l, int index, LuaString key)
        {
            var top = l.gettop();
            if (index < 0 && -index <= top)
            {
                index = top + 1 + index;
            }

            key.PushString(l);
            l.insert(-2);
            l.settable(index);
        }

        public static void RawGet(this IntPtr l, int index, string key)
        {
            var top = l.gettop();
            if (index < 0 && -index <= top)
            {
                index = top + 1 + index;
            }

            l.PushString(key);
            l.rawget(index);
        }

        public static void RawGet(this IntPtr l, int index, LuaString key)
        {
            var top = l.gettop();
            if (index < 0 && -index <= top)
            {
                index = top + 1 + index;
            }

            key.PushString(l);
            l.rawget(index);
        }

        public static void RawSet(this IntPtr l, int index, string key)
        {
            var top = l.gettop();
            if (index < 0 && -index <= top)
            {
                index = top + 1 + index;
            }

            l.PushString(key);
            l.insert(-2);
            l.rawset(index);
        }

        public static void RawSet(this IntPtr l, int index, LuaString key)
        {
            var top = l.gettop();
            if (index < 0 && -index <= top)
            {
                index = top + 1 + index;
            }

            key.PushString(l);
            l.insert(-2);
            l.rawset(index);
        }

        public static void GetGlobal(this IntPtr l, string key)
        {
            GetField(l, lua.LUA_GLOBALSINDEX, key);
        }

        public static void GetGlobal(this IntPtr l, LuaString key)
        {
            GetField(l, lua.LUA_GLOBALSINDEX, key);
        }

        public static void SetGlobal(this IntPtr l, string key)
        {
            SetField(l, lua.LUA_GLOBALSINDEX, key);
        }

        public static void SetGlobal(this IntPtr l, LuaString key)
        {
            SetField(l, lua.LUA_GLOBALSINDEX, key);
        }
    }
}