using System;
using System.Collections.Generic;

using lua = LuaLib.LuaCoreLib;
using lual = LuaLib.LuaAuxLib;
using luae = LuaLib.LuaLibEx;
using System.Runtime.InteropServices;

namespace LuaLib
{
    public class LuaObjCache
    {
        public static LuaObjCache GetObjCache(IntPtr l)
        {
            var attachman = LuaStateAttachmentManager.GetAttachmentManager(l);
            if (attachman != null)
            {
                return attachman.ObjCache;
            }
            return null;
            //l.checkstack(1);
            //l.pushlightuserdata(LuaConst.LRKEY_OBJ_CACHE); // key
            //l.gettable(lua.LUA_REGISTRYINDEX); // cache
            //var rv = l.GetLuaObject(-1) as LuaObjCache;
            //l.pop(1);
            //return rv;
        }

        public static LuaObjCache GetOrCreateObjCache(IntPtr l)
        {
            return LuaStateAttachmentManager.GetOrCreateAttachmentManager(l).ObjCache;
            //var rv = GetObjCache(l);
            //if (rv == null)
            //{
            //    l.checkstack(2);
            //    rv = new LuaObjCache();
            //    l.pushlightuserdata(LuaConst.LRKEY_OBJ_CACHE); // key
            //    l.PushLuaRawObject(rv); // key cache
            //    l.settable(lua.LUA_REGISTRYINDEX); // X
            //}
            //return rv;
        }

        public static void PushObjCacheReg(IntPtr l)
        {
            l.checkstack(1);
            l.pushlightuserdata(LuaConst.LRKEY_OBJ_CACHE_REG); // key
            l.gettable(lua.LUA_REGISTRYINDEX); // reg
        }

        public static void PushOrCreateObjCacheReg(IntPtr l)
        {
            l.checkstack(5);
            l.pushlightuserdata(LuaConst.LRKEY_OBJ_CACHE_REG); // key
            l.gettable(lua.LUA_REGISTRYINDEX); // reg
            if (!l.istable(-1))
            {
                l.pop(1); // X
                l.newtable(); // reg
                l.pushlightuserdata(LuaConst.LRKEY_OBJ_CACHE_REG); // reg key
                l.pushvalue(-2); // reg key reg
                l.settable(lua.LUA_REGISTRYINDEX); // reg
                l.newtable(); // reg meta
                l.PushString(LuaConst.LS_COMMON_V); // reg meta "v"
                l.SetField(-2, LuaConst.LS_META_KEY_MODE); // reg meta
                l.newtable(); // reg meta index
                l.SetField(-2, LuaConst.LS_META_KEY_INDEX); // reg meta
                l.setmetatable(-2); // reg
            }
        }

        public static bool PushObjFromCache(IntPtr l, object obj)
        {
            if (LuaObjCacheSlim.TryPush(l, obj))
            {
                return true;
            }

            var cache = GetObjCache(l);
            if (cache != null)
            {
                IntPtr h;
                if (cache._Map.TryGetValue(obj, out h))
                {
                    l.checkstack(5);
                    PushObjCacheReg(l); // reg
                    if (!l.istable(-1))
                    {
                        l.pop(1);
                        return false;
                    }
                    l.pushlightuserdata(h); // reg h
                    l.gettable(-2); // reg ud
                    if (l.isnoneornil(-1))
                    {
                        l.pop(2); // X
                        return false;
                    }
                    l.remove(-2); // ud
                    return true;
                }
            }
            return false;
        }

        internal static void RegObj(IntPtr l, object obj, int index, IntPtr h)
        {
            var pos = l.NormalizeIndex(index);
            if (obj != null)
            {
                LuaObjCacheSlim.Record(l, obj, pos);
            }

            var cache = GetOrCreateObjCache(l);
            cache._Map[obj] = h;

            l.checkstack(5);
            PushOrCreateObjCacheReg(l); // reg
            l.pushlightuserdata(h); // reg h
            l.pushvalue(pos); // reg h ud
            l.settable(-3); // reg
            l.pop(1); // X
        }

        internal static void RegObjStrong(IntPtr l, object obj, int index, IntPtr h)
        {
            //if (PushObjFromCache(l, obj))
            //{
            //    l.pop(1);
            //    return;
            //}

            var cache = GetOrCreateObjCache(l);
            cache._Map[obj] = h;

            l.checkstack(5);
            l.pushvalue(index); // ud
            PushOrCreateObjCacheReg(l); // ud reg
            l.getmetatable(-1); // ud reg meta
            l.GetField(-1, LuaConst.LS_META_KEY_INDEX); // ud reg meta index
            l.insert(-4); // index ud reg meta
            l.pop(2); // index ud
            l.pushlightuserdata(h); // index ud h
            l.insert(-2); // index h ud
            l.settable(-3); // index
            l.pop(1); // X
        }

        private Dictionary<object, IntPtr> _Map = new Dictionary<object, IntPtr>();
        public void Remove(object obj)
        {
            LuaObjCacheSlim.Remove(obj);
            _Map.Remove(obj);
        }

        public static void PushObjFromCache(IntPtr l, IntPtr index)
        {
            l.checkstack(2);
            PushObjCacheReg(l); // reg
            if (!l.istable(-1))
            {
                l.pop(1);
                l.pushnil();
                return;
            }
            l.pushlightuserdata(index); // reg h
            l.gettable(-2); // reg ud
            l.remove(-2); // ud
        }
    }

    public class LuaObjLivenessTracker
    {
        private static void PushTrackerReg(IntPtr l)
        {
            l.checkstack(1);
            l.pushlightuserdata(LuaConst.LRKEY_OBJ_GC_TRACKER); // key
            l.gettable(lua.LUA_REGISTRYINDEX); // reg
        }

        private static void PushOrCreateTrackerReg(IntPtr l)
        {
            l.checkstack(5);
            l.pushlightuserdata(LuaConst.LRKEY_OBJ_GC_TRACKER); // key
            l.gettable(lua.LUA_REGISTRYINDEX); // reg
            if (!l.istable(-1))
            {
                l.pop(1); // X
                l.newtable(); // reg
                l.pushlightuserdata(LuaConst.LRKEY_OBJ_GC_TRACKER); // reg key
                l.pushvalue(-2); // reg key reg
                l.settable(lua.LUA_REGISTRYINDEX); // reg
                l.newtable(); // reg meta
                l.PushString(LuaConst.LS_COMMON_K); // reg meta "k"
                l.SetField(-2, LuaConst.LS_META_KEY_MODE); // reg meta
                l.setmetatable(-2); // reg
            }
        }

        public static void Track(IntPtr l, int index)
        {
#if !DISABLE_LUA_HUB_C
            if (LuaHub.LuaHubC.Ready)
            {
                LuaHub.LuaHubC.lua_trackLiveness(l, index);
                return;
            }
#endif
            l.pushvalue(index); // tab
            PushOrCreateTrackerReg(l); // tab reg
            l.insert(-2); // reg tab
            l.pushboolean(true); // reg tab true
            l.settable(-3); // reg
            l.pop(1); // X
        }
        public static bool IsAlive(IntPtr l, int index)
        {
#if !DISABLE_LUA_HUB_C
            if (LuaHub.LuaHubC.Ready)
            {
                return LuaHub.LuaHubC.lua_checkLiveness(l, index);
            }
#endif
            l.pushvalue(index); // tab
            PushTrackerReg(l); // tab reg
            if (!l.istable(-1))
            {
                l.pop(2);
                return false;
            }
            l.insert(-2); // reg tab
            l.gettable(-2); // reg alive?
            var valid = l.toboolean(-1);
            l.pop(2);
            return valid;
        }
    }

    public static class LuaObjCacheSlim
    {
#if DEBUG_LUA_PERFORMANCE
        private static int _TotalCnt = 0;
        private static int _HitCnt = 0;
#endif

        private const int StorageMaxCount = 20;
        private struct LuaObjCacheSlimStorageRecord
        {
            public LinkedListNode<LuaObjCacheSlimStorageRecord> Node;
            public object Obj;
            public IntPtr Pointer;
            public int StackPos;
        }
        private class LuaObjCacheSlimStorage
        {
            public readonly LinkedList<LuaObjCacheSlimStorageRecord> List = new LinkedList<LuaObjCacheSlimStorageRecord>();
            public readonly Dictionary<IntPtr, LuaObjCacheSlimStorageRecord> PointerMap = new Dictionary<IntPtr, LuaObjCacheSlimStorageRecord>();
            public readonly Dictionary<int, LuaObjCacheSlimStorageRecord> PosMap = new Dictionary<int, LuaObjCacheSlimStorageRecord>();
            public readonly Dictionary<object, LuaObjCacheSlimStorageRecord> ObjMap = new Dictionary<object, LuaObjCacheSlimStorageRecord>();

            public void Remove(object obj)
            {
                LuaObjCacheSlimStorageRecord record;
                if (ObjMap.TryGetValue(obj, out record))
                {
                    Remove(record);
                }
            }
            public void Remove(LuaObjCacheSlimStorageRecord record)
            {
                List.Remove(record.Node);
                PointerMap.Remove(record.Pointer);
                PosMap.Remove(record.StackPos);
                ObjMap.Remove(record.Obj);
            }
            public void RemoveFirst()
            {
                if (List.Count != 0)
                {
                    Remove(List.First.Value);
                }
            }
            public void Record(IntPtr l, object obj, int stackPos)
            {
                LuaObjCacheSlimStorageRecord record;
                if (ObjMap.TryGetValue(obj, out record))
                {
                    Remove(record);
                }

                var p = l.topointer(stackPos);
                record = new LuaObjCacheSlimStorageRecord()
                {
                    Obj = obj,
                    Pointer = p,
                    StackPos = stackPos,
                };
                var node = List.AddLast(record);
                record.Node = node;

                node.Value = record;
                PointerMap[p] = record;
                PosMap[stackPos] = record;
                ObjMap[obj] = record;

                if (List.Count > StorageMaxCount)
                {
                    RemoveFirst();
                }

                LuaObjLivenessTracker.Track(l, stackPos);
            }
        }
        [ThreadStatic] private static LuaObjCacheSlimStorage _Storage;
        private static LuaObjCacheSlimStorage Storage
        {
            get
            {
                var storage = _Storage;
                if (storage == null)
                {
                    _Storage = storage = new LuaObjCacheSlimStorage();
                }
                return storage;
            }
        }
        public static void Record(IntPtr l, object obj, int stackPos)
        {
            Storage.Record(l, obj, stackPos);
        }
        public static void Remove(object obj)
        {
            Storage.Remove(obj);
        }

        public static bool TryGet(IntPtr l, int index, out object obj)
        {
#if DEBUG_LUA_THREADSAFE
            LuaStateAttachmentManager.CheckThread(l);
#endif
#if DEBUG_LUA_PERFORMANCE
            System.Threading.Interlocked.Increment(ref _TotalCnt);
#endif
            var pointer = l.topointer(index);
            if (pointer != IntPtr.Zero)
            {
                LuaObjCacheSlimStorageRecord record;
                if (Storage.PointerMap.TryGetValue(pointer, out record))
                {
                    if (!LuaObjLivenessTracker.IsAlive(l, index))
                    {
                        Storage.Remove(record);
                        obj = null;
                        return false;
                    }
                    obj = record.Obj;
#if DEBUG_LUA_PERFORMANCE
                    System.Threading.Interlocked.Increment(ref _HitCnt);
                    UnityEngine.Debug.Log(((float)_HitCnt) / _TotalCnt);
#endif
                    return true;
                }
            }
            obj = null;
#if DEBUG_LUA_PERFORMANCE
            UnityEngine.Debug.Log(((float)_HitCnt) / _TotalCnt);
#endif
            return false;
        }
        public static bool TryPush(IntPtr l, object obj)
        {
#if DEBUG_LUA_THREADSAFE
            LuaStateAttachmentManager.CheckThread(l);
#endif
#if DEBUG_LUA_PERFORMANCE
            System.Threading.Interlocked.Increment(ref _TotalCnt);
#endif
            if (obj != null)
            {
                LuaObjCacheSlimStorageRecord record;
                if (Storage.ObjMap.TryGetValue(obj, out record))
                {
                    var pos = record.StackPos;
                    var pointer = record.Pointer;
                    if (l.topointer(pos) == pointer)
                    {
                        if (!LuaObjLivenessTracker.IsAlive(l, pos))
                        {
                            Storage.Remove(record);
                            return false;
                        }

                        l.pushvalue(pos);
#if DEBUG_LUA_PERFORMANCE
                        System.Threading.Interlocked.Increment(ref _HitCnt);
                        UnityEngine.Debug.Log(((float)_HitCnt) / _TotalCnt);
#endif
                        return true;
                    }
                }
            }
#if DEBUG_LUA_PERFORMANCE
            UnityEngine.Debug.Log(((float)_HitCnt) / _TotalCnt);
#endif
            return false;
        }
    }

    public class LuaStateAttachmentManager : ILuaMeta
    {
#if DEBUG_LUA_THREADSAFE
        public static void CheckThread(IntPtr l)
        {
            l.checkstack(2);
            l.pushlightuserdata(LuaConst.LRKEY_REF_THREAD); // #thread
            l.gettable(lua.LUA_REGISTRYINDEX); // thread
            if (l.IsNumber(-1))
            {
                var threadid = (int)l.tonumber(-1);
                if (threadid != System.Threading.Thread.CurrentThread.ManagedThreadId)
                {
                    UnityEngineEx.PlatDependant.LogError("Please use lua state only in its owner thread(the thread that created the lua state).");
                }
                l.pop(1);
            }
            else
            {
                l.pop(1); // X
                l.pushlightuserdata(LuaConst.LRKEY_REF_THREAD); // #thread
                l.pushnumber(System.Threading.Thread.CurrentThread.ManagedThreadId); // #thread thread
                l.settable(lua.LUA_REGISTRYINDEX); // X
            }
        }
#endif
        [ThreadStatic] private static Dictionary<IntPtr, LuaStateAttachmentManager> _Map;
        private static Dictionary<IntPtr, LuaStateAttachmentManager> Map
        {
            get
            {
                var map = _Map;
                if (map == null)
                {
                    _Map = map = new Dictionary<IntPtr, LuaStateAttachmentManager>();
                }
                return map;
            }
        }

        public static LuaStateAttachmentManager GetAttachmentManagerForIndicator(IntPtr indicator)
        {
            LuaStateAttachmentManager rv = null;
            var map = _Map;
            if (map != null)
            {
                map.TryGetValue(indicator, out rv);
            }
            return rv;
        }
        public static LuaStateAttachmentManager GetAttachmentManager(IntPtr l)
        {
#if DEBUG_LUA_THREADSAFE
            LuaStateAttachmentManager.CheckThread(l);
#endif
            var indicator = l.Indicator();
            LuaStateAttachmentManager rv = GetAttachmentManagerForIndicator(indicator);
            if (rv != null)
            {
                return rv;
            }

            l.checkstack(1);
            l.pushlightuserdata(LuaConst.LRKEY_REF_ATTACH); // #man
            l.gettable(lua.LUA_REGISTRYINDEX); // man
            if (l.isuserdata(-1))
            {
                LuaStateAttachmentManager man = null;
                try
                {
                    IntPtr pud = l.touserdata(-1);
                    if (pud != IntPtr.Zero)
                    {
                        IntPtr hval = Marshal.ReadIntPtr(pud);
                        GCHandle handle = (GCHandle)hval;
                        man = handle.Target as LuaStateAttachmentManager;
                    }
                }
                catch { }
                l.pop(1); // X
                if (man != null)
                {
                    Map[indicator] = man;
                }
                return man;
            }
            else
            {
                //l.checkstack(5);
                l.pop(1); // X
                //l.pushlightuserdata(LuaConst.LRKEY_REF_ATTACH); // #man
                //LuaStateAttachmentManager man = new LuaStateAttachmentManager(l);
                //var h = l.PushLuaRawObject(man); // #man man
                //l.PushCommonMetaTable(); // #man man meta
                //l.setmetatable(-2); // #man man
                //l.newtable(); // #man man env
                //l.pushlightuserdata(LuaConst.LRKEY_OBJ_META_EX); // #man man env #meta
                //l.pushlightuserdata(h); // #man man env #meta meta
                //l.settable(-3); // #man man env
                //l.setfenv(-2); // #man man
                //l.settable(lua.LUA_REGISTRYINDEX); // X
                //Map[indicator] = man;
                //return man;

                return null;
            }
        }
        public static LuaStateAttachmentManager GetOrCreateAttachmentManager(IntPtr l)
        {
#if DEBUG_LUA_THREADSAFE
            LuaStateAttachmentManager.CheckThread(l);
#endif
            var indicator = l.Indicator();
            LuaStateAttachmentManager rv = GetAttachmentManagerForIndicator(indicator);
            if (rv != null)
            {
                return rv;
            }

            l.checkstack(1);
            l.pushlightuserdata(LuaConst.LRKEY_REF_ATTACH); // #man
            l.gettable(lua.LUA_REGISTRYINDEX); // man
            if (l.isuserdata(-1))
            {
                LuaStateAttachmentManager man = null;
                try
                {
                    IntPtr pud = l.touserdata(-1);
                    if (pud != IntPtr.Zero)
                    {
                        IntPtr hval = Marshal.ReadIntPtr(pud);
                        GCHandle handle = (GCHandle)hval;
                        man = handle.Target as LuaStateAttachmentManager;
                    }
                }
                catch { }
                l.pop(1); // X
                if (man != null)
                {
                    Map[indicator] = man;
                }
                return man;
            }
            else
            {
                l.checkstack(5);
                l.pop(1); // X
                l.pushlightuserdata(LuaConst.LRKEY_REF_ATTACH); // #man
                LuaStateAttachmentManager man = new LuaStateAttachmentManager();
                var h = l.PushLuaRawObject(man); // #man man
                l.PushCommonMetaTable(); // #man man meta
                l.setmetatable(-2); // #man man
                l.newtable(); // #man man env
                l.pushlightuserdata(LuaConst.LRKEY_OBJ_META_EX); // #man man env #meta
                l.pushlightuserdata(h); // #man man env #meta meta
                l.settable(-3); // #man man env
                l.setfenv(-2); // #man man
                l.settable(lua.LUA_REGISTRYINDEX); // X
                Map[indicator] = man;
                return man;
            }
        }
        public static LuaStateAttachmentManager GetOrCreateAttachmentManager(LuaLib.LuaState L)
        {
            var man = GetOrCreateAttachmentManager(L.L);
            man.L = L;
            return man;
        }

        public LuaLib.LuaState L { get; protected set; }
        public readonly LuaObjCache ObjCache = new LuaObjCache();
        public readonly LuaStringTransHelper.LuaStringCache StrCache = new LuaStringTransHelper.LuaStringCache();

        public void call(IntPtr l, object tar)
        {
        }
        public void gc(IntPtr l, object obj)
        {
#if DEBUG_LUA_THREADSAFE
            LuaStateAttachmentManager.CheckThread(l);
#endif
            var map = _Map;
            if (map != null)
            {
                map.Remove(l.Indicator());
            }
        }
        public void index(IntPtr l, object tar, int kindex)
        {
        }
        public void newindex(IntPtr l, object tar, int kindex, int valindex)
        {
        }
        public IntPtr r { get; protected internal set; }
    }
}
