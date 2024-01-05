//#define DEBUG_LOG_REFIDS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngineEx;
using LuaLib;

using lua = LuaLib.LuaCoreLib;
using lual = LuaLib.LuaAuxLib;
using luae = LuaLib.LuaLibEx;

namespace LuaLib
{
    public class LuaState : BaseLua
    {
        public LuaState()
        {
            L = lual.newstate();
            L.openlibs();
            _Closer = new LuaStateCloser() { _L = L };
            LuaStateAttachmentManager.GetOrCreateAttachmentManager(this);
            _ObjCache = LuaObjCache.GetOrCreateObjCache(L);
        }
        public LuaState(IntPtr l)
        {
            L = l;
            if (L != IntPtr.Zero)
            {
                _ObjCache = LuaObjCache.GetOrCreateObjCache(L);
            }
        }

        public LuaOnStackTable _G
        {
            get
            {
                return new LuaOnStackTable(L, lua.LUA_GLOBALSINDEX);
            }
        }
        public LuaStateRecover CreateStackRecover()
        {
            return new LuaStateRecover(L);
        }
        protected object _ObjCache; // this is for asset holder

        protected internal override object GetFieldImp(object key)
        {
            object rawkey = key;//.UnwrapDynamic();
            if (rawkey is string)
            {
                return _G.GetHierarchical(rawkey as string);
            }
            else
            {
                return _G[key];
            }
        }
        protected internal override bool SetFieldImp(object key, object val)
        {
            object rawkey = key;//.UnwrapDynamic();
            if (rawkey is string)
            {
                return _G.SetHierarchical(rawkey as string, val);
            }
            else
            {
                _G[key] = val;
                return true;
            }
        }

        public static implicit operator IntPtr(LuaState l)
        {
            if (!object.ReferenceEquals(l, null))
                return l.L;
            return IntPtr.Zero;
        }
        public static implicit operator LuaState(IntPtr l)
        {
            return new LuaState(l);
        }

        public override string ToString()
        {
            return "LuaState:" + L.ToString();
        }

        public static bool IgnoreDispose = false;

        protected internal class LuaStateCloser : IDisposable
        {
            [ThreadStatic] protected internal static LinkedList<IntPtr> DelayedCloser;
            protected internal LinkedList<IntPtr> _DelayedCloser;

            protected internal IntPtr _L;
            protected internal bool _Disposed;

            public LuaStateCloser()
            {
                if (DelayedCloser == null)
                    DelayedCloser = new LinkedList<IntPtr>();
                _DelayedCloser = DelayedCloser;
                RawDispose();
            }
            ~LuaStateCloser()
            {
                Dispose(false);
            }
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            protected internal void Dispose(bool includeManagedRes)
            {
                if (IgnoreDispose)
                {
                    _Disposed = true;
                    return;
                }
                if (!_Disposed)
                {
                    _Disposed = true;
                    if (_L != IntPtr.Zero)
                    {
                        if (_DelayedCloser != null)
                        {
                            lock (_DelayedCloser)
                            {
                                _DelayedCloser.AddLast(_L);
                            }
                        }
                    }
                }
                RawDispose();
            }

            public static void RawDispose()
            {
                if (IgnoreDispose) return;
                if (DelayedCloser != null)
                {
                    int tick = Environment.TickCount;
                    while (DelayedCloser.Count > 0)
                    {
                        IntPtr l = IntPtr.Zero;
                        lock (DelayedCloser)
                        {
                            if (DelayedCloser.Count > 0)
                            {
                                l = DelayedCloser.First.Value;
                                DelayedCloser.RemoveFirst();
                            }
                        }
                        if (l != IntPtr.Zero)
                        {
                            l.close();
                        }
                        var newtick = Environment.TickCount;
                        //if (newtick < tick || newtick - tick > 200)
                        //{
                        //    break;
                        //}
                    }
                }
            }
        }
        protected internal LuaStateCloser _Closer = null;
        public override void Dispose()
        {
            //__UserDataCache = null;
            if (_Closer != null)
            {
                _Closer.Dispose();
            }
        }

        // TODO: more func of lualib import here.
        public int DoFileRaw(string filepath)
        {
            var l = L;
            var oldtop = l.gettop();
            l.pushcfunction(LuaHub.LuaFuncOnError);
            var code = l.dofile(filepath, oldtop + 1);
            l.remove(oldtop + 1);
            return code;
        }
        public void DoFile(string filepath)
        {
            using (var lr = CreateStackRecover())
            {
                DoFileRaw(filepath);
            }
        }
        public int DoStringRaw(string chunk)
        {
            var l = L;
            var oldtop = l.gettop();
            l.pushcfunction(LuaHub.LuaFuncOnError);
            var code = l.loadstring(chunk);
            if (code != 0)
            {
                return code;
            }
            var lrr = new LuaRunningStateRecorder(l);
            code = l.pcall(0, lua.LUA_MULTRET, oldtop + 1);
            lrr.Dispose();
            l.remove(oldtop + 1);
            return code;
        }
        public void DoString(string chunk)
        {
            using (var lr = CreateStackRecover())
            {
                DoStringRaw(chunk);
            }
        }
    }

    public class LuaOnStackThread : LuaState
    {
        protected internal bool _IsDone = false;
        public bool IsDone { get { return _IsDone; } }

        public override string ToString()
        {
            return "LuaThreadRaw:" + L.ToString() + ", of ref:" + Refid.ToString();
        }
        public LuaOnStackThread(IntPtr l) : base(IntPtr.Zero)
        {
            L = l;
        }

        public LuaOnStackThread(int refid, IntPtr l) : base(IntPtr.Zero)
        {
            L = l;
            Refid = refid;
        }
        public LuaOnStackThread(IntPtr parentl, int stackpos)
        {
            L = parentl.tothread(stackpos);
            parentl.pushvalue(stackpos);
            Refid = parentl.refer();
        }

        public void Resume()
        {
            DoResume();
            L.settop(0);
        }
        public object[] Resume(params object[] args)
        {
            var l = L;
            if (l != IntPtr.Zero)
            {
                var oldtop = l.gettop();
                if (args != null)
                {
                    for (int i = 0; i < args.Length; ++i)
                    {
                        l.PushLua(args[i]);
                    }
                }
                DoResume(oldtop);
                object[] rv = ObjectPool.GetReturnValueFromPool(l.gettop());
                for (int i = 0; i < rv.Length; ++i)
                {
                    rv[i] = l.GetLua(i + 1);
                }
                l.settop(0);
                return rv;
            }
            return null;
        }

        protected internal void DoResume()
        {
            DoResume(-1);
        }
        protected internal virtual void DoResume(int oldtop)
        {
            if (oldtop < 0)
            {
                oldtop = L.gettop();
            }
#if ENABLE_PROFILER && ENABLE_PROFILER_LUA && ENABLE_PROFILER_LUA_DEEP && !DISABLE_PROFILER_LUA_COROUTINE
            string simpleStack = L.GetSimpleStackInfo(8);
            using (var pcon = ProfilerContext.Create("LuaCoroutine: {0}", simpleStack))
#endif
            ResumeRaw(oldtop);
        }
        protected internal void ResumeRaw(int oldtop)
        {
            var l = L;
            if (_IsDone)
            {
                l.settop(0);
                return;
            }
            var argc = l.gettop() - oldtop;
            if (argc < 0)
            {
                l.LogWarning("Lua stack is not correct when resume lua coroutine.");
                argc = 0;
            }
            int status = 0;
            var lrr = new LuaStateHelper.LuaRunningThreadRecorder(l);
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
            var lar = new LuaStateHelper.LuaCoroutineAborterRecorder(l);
            var currentcoroutine = UnityEngineEx.CoroutineRunner.CurrentCoroutineInfo;
            if (currentcoroutine != null)
            {
                var map = LuaStateHelper.LuaThreadToCoroutineInfo;
                map.ForwardMap[l] = currentcoroutine;
                map.BackwardMap[currentcoroutine] = l;
            }
            try
            {
#endif
            status = l.resume(argc);
            while (status == 0)
            {
                int confuncresult = l.GetContinuationFunc();
                if (confuncresult <= 0)
                {
                    break;
                }
                l.pushnil();
                l.SetContinuationFunc(-1);
                l.pop(1);
                status = l.resume(confuncresult - 1);
            }
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
            }
            catch (LuaStateHelper.LuaCoroutineAbortedException ea)
            {
                ea.IsHandled = true;
                l.LogError("Current lua coroutine aborted!");
                l.settop(0);
                _IsDone = true;
            }
            lar.Dispose();
#endif
            lrr.Dispose();
            if (!_IsDone)
            {
                if (status == lua.LUA_YIELD || status == 0)
                {
                    if (status == 0)
                    {
                        _IsDone = true;
                    }
                }
                else
                {
                    l.pushcfunction(LuaHub.LuaFuncOnError);
                    l.insert(-2);
                    l.pcall(1, 0, 0);
                    l.settop(0);
                    _IsDone = true;
                }
            }
        }

        public virtual bool IsRunning
        {
            get
            {
                if (L != IntPtr.Zero)
                {
                    return L.status() == lua.LUA_YIELD;
                }
                return false;
            }
        }

        public override void Dispose()
        {
            // Try dispose lua-coroutine's "finally"
            if (!Ref.IsClosed)
            {
                var l = L;
                l.pushlightuserdata(LuaConst.LRKEY_CO_FINALLY); // #fin
                l.gettable(lua.LUA_REGISTRYINDEX); // fin
                if (l.istable(-1))
                {
                    l.pushthread(); // fin thd
                    l.gettable(-2); // fin func
                    if (l.isfunction(-1))
                    {
                        l.PushArgsAndCall(); // fin
                        l.pop(1);
                    }
                    else
                    {
                        l.pop(2); // X
                    }
                }
                else
                {
                    l.pop(1); // X
                }

            }
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
            var map = LuaStateHelper._LuaThreadToCoroutineInfo;
            if (map != null)
            {
                var l = L;
                var info = LuaStateHelper.GetUnityCoroutine(l);
                map.ForwardMap.Remove(l);
                if (info != null)
                {
                    map.BackwardMap.Remove(info);
                }
            }
#endif
            if (Ref != null)
            {
                Ref.Dispose();
                Ref = null;
            }
        }
    }

    public class LuaThread : LuaOnStackThread
    {
        public override string ToString()
        {
            return "LuaThreadRestartable:" + L.ToString() + ", of ref:" + Refid.ToString();
        }

        protected internal LuaFunc _Func;
        protected internal bool _NeedRestart = true;

        public LuaThread(LuaFunc func) : base(IntPtr.Zero)
        {
            if (!ReferenceEquals(func, null) && func.L != IntPtr.Zero)
            {
                var l = func.L;
                L = l;
                l.pushthread();
                Refid = l.refer();
                if (func.Refid != 0)
                {
                    l.getref(func.Refid);
                    _Func = new LuaFunc(l, -1);
                    l.pop(1);
                }
            }
            if (L != IntPtr.Zero)
            {
                _ObjCache = LuaObjCache.GetOrCreateObjCache(L);
            }
        }
        public LuaThread(LuaOnStackFunc func) : base(IntPtr.Zero)
        {
            if (!ReferenceEquals(func, null) && func.L != IntPtr.Zero)
            {
                var l = func.L;
                L = l;
                l.pushthread();
                Refid = l.refer();
                l.pushvalue(func.StackPos);
                var reffunc = l.refer();
                _Func = new LuaFunc();
                _Func.L = l;
                _Func.Refid = reffunc;
            }
            if (L != IntPtr.Zero)
            {
                _ObjCache = LuaObjCache.GetOrCreateObjCache(L);
            }
        }

#if ENABLE_PROFILER && ENABLE_PROFILER_LUA && ENABLE_PROFILER_LUA_DEEP && !DISABLE_PROFILER_LUA_COROUTINE
        protected string _ProfilerShownName;
#endif
        protected internal override void DoResume(int oldtop)
        {
            if (!ReferenceEquals(_Func, null) && L != IntPtr.Zero)
            {
                if (oldtop < 0)
                {
                    oldtop = L.gettop();
                }
                if (_NeedRestart)
                {
                    _NeedRestart = false;
                    _IsDone = false;
                    if (ReferenceEquals(_Func, null))
                    {
                        _IsDone = true;
                        return;
                    }
#if ENABLE_PROFILER && ENABLE_PROFILER_LUA && ENABLE_PROFILER_LUA_DEEP && !DISABLE_PROFILER_LUA_COROUTINE
                    if (_ProfilerShownName == null)
                    {
                        System.Text.StringBuilder sbName = new System.Text.StringBuilder();
                        sbName.Append("LuaCoroutine in ");
                        //string funcName, fileName;
                        //int lineStart, lineCur;
                        //L.GetFuncInfo(1, out funcName, out fileName, out lineStart, out lineCur);
                        //sbName.Append(fileName);
                        //sbName.Append(":");
                        //sbName.Append(lineStart);
                        ////sbName.Append(" ");
                        ////sbName.Append(funcName); // NOTICE: we cannot get the name directly from the func.
                        string simpleStack = L.GetSimpleStackInfo(8);
                        sbName.Append(simpleStack);
                        _ProfilerShownName = sbName.ToString();
                    }
#endif
                    L.pushnumber(Refid);
                    var l = L.newthread();
                    L.settable(lua.LUA_REGISTRYINDEX);
                    l.PushLua(_Func);
                    var newtop = L.gettop();
                    if (newtop > oldtop)
                    {
                        L.xmove(l, newtop - oldtop);
                    }
                    _L = l;
#if ENABLE_PROFILER && ENABLE_PROFILER_LUA && ENABLE_PROFILER_LUA_DEEP && !DISABLE_PROFILER_LUA_COROUTINE
                    using (var pcon = ProfilerContext.Create(_ProfilerShownName))
                    using (var pconi = ProfilerContext.Create("at start"))
#endif
                    ResumeRaw(1);
                }
                else if (IsRunning)
                {
#if ENABLE_PROFILER && ENABLE_PROFILER_LUA && ENABLE_PROFILER_LUA_DEEP && !DISABLE_PROFILER_LUA_COROUTINE
                    string simpleStack = L.GetSimpleStackInfo(8);
                    using (var pcon = ProfilerContext.Create(_ProfilerShownName))
                    using (var pconi = ProfilerContext.Create(simpleStack))
#endif
                    ResumeRaw(oldtop);
                }
            }
        }

        public void Restart()
        {
            _NeedRestart = true;
        }

        public override void Dispose()
        {
            base.Dispose();
            if (!object.ReferenceEquals(_Func, null))
            {
                _Func.Dispose();
            }
        }
    }

    internal class LuaThreadRefMan : ILuaMeta
    {
        internal bool IsClosed = false;
        private List<LuaRef> _RefCache = new List<LuaRef>(8);
        private List<LuaRef> _PendingRecycle = new List<LuaRef>(8);

        internal void ReturnLuaRef(LuaRef lr)
        {
            lr.l = IntPtr.Zero;
            lr.r = 0;
            lr.lr = 0;
            _RefCache.Add(lr);
        }
        public LuaRef GetLuaRef()
        {
            DoPendingRecycle();
            if (_RefCache.Count <= 0)
            {
                var rv = new LuaRef();
                rv.man = this;
                return rv;
            }
            else
            {
                var index = _RefCache.Count - 1;
                var rv = _RefCache[index];
                _RefCache.RemoveAt(index);
                GC.ReRegisterForFinalize(rv);
                return rv;
            }
        }
        internal void DoPendingRecycle()
        {
            if (IsClosed)
            {
                return;
            }
            if (_PendingRecycle.Count <= 0)
            {
                return;
            }
            List<LuaRef> list;
            lock (_PendingRecycle)
            {
                list = new List<LuaRef>(_PendingRecycle);
                _PendingRecycle.Clear();
            }
            for (int i = 0; i < list.Count; ++i)
            {
                var lr = list[i];
                lr.RawDispose();
                ReturnLuaRef(lr);
            }
        }
        internal void RegPendingRecycle(LuaRef lr)
        {
            //GC.SuppressFinalize(lr); // this should be called in finalizer.
            if (IsClosed)
            {
                return;
            }
            lock (_PendingRecycle)
            {
                _PendingRecycle.Add(lr);
            }
        }
        internal void DoImmediateRecycle(LuaRef lr)
        {
            GC.SuppressFinalize(lr);
            if (IsClosed)
            {
                return;
            }
            lr.RawDispose();
            ReturnLuaRef(lr);
        }

        public void Close()
        {
            IsClosed = true;
        }

        public IntPtr r
        {
            get;
            internal set;
        }

        public void gc(IntPtr l, object obj)
        {
            Close();
        }
        public void call(IntPtr l, object tar)
        {
        }


        public void index(IntPtr l, object tar, int kindex)
        {
        }

        public void newindex(IntPtr l, object tar, int kindex, int valindex)
        {
        }
    }

    public class LuaRef : IDisposable
    {
#if DEBUG_LOG_REFIDS
        [ThreadStatic] public static HashSet<int> AliveRefids;
        [ThreadStatic] public static HashSet<int> TrackingRefids;
        [ThreadStatic] public static Dictionary<int, string> PersistentRefidReason;
#endif
        public static void TrackRefid(int r)
        {
#if DEBUG_LOG_REFIDS
            var set = TrackingRefids = TrackingRefids ?? new HashSet<int>();
            set.Add(r);
#endif
        }
        public static HashSet<int> GetTrackingRefids()
        {
#if DEBUG_LOG_REFIDS
            return TrackingRefids;
#else
            return null;
#endif
        }
        public static void RegPersistentRefid(int r, string reason)
        {
#if DEBUG_LOG_REFIDS
            var dict = PersistentRefidReason = PersistentRefidReason ?? new Dictionary<int, string>();
            dict[r] = reason;
#endif
        }
        public static void RegPersistentRefid<T>(int r, string reasonformat, T t)
        {
#if DEBUG_LOG_REFIDS
            RegPersistentRefid(r, string.Format(reasonformat, t));
#endif
        }
        public static void RegPersistentRefid<T0, T1>(int r, string reasonformat, T0 t0, T1 t1)
        {
#if DEBUG_LOG_REFIDS
            RegPersistentRefid(r, string.Format(reasonformat, t0, t1));
#endif
        }
        public static void RegPersistentRefid<T0, T1, T2>(int r, string reasonformat, T0 t0, T1 t1, T2 t2)
        {
#if DEBUG_LOG_REFIDS
            RegPersistentRefid(r, string.Format(reasonformat, t0, t1, t2));
#endif
        }
        public static Dictionary<int, string> GetPersistentRefids()
        {
#if DEBUG_LOG_REFIDS
            return PersistentRefidReason;
#else
            return null;
#endif
        }

        internal IntPtr l;
        internal int r;
        internal int lr;
        internal LuaThreadRefMan man;

        public void RawDispose()
        {
            if (l != IntPtr.Zero)
            {
                l.unref(lr);
                if (r != 0)
                {
                    l.unref(r);
#if DEBUG_LOG_REFIDS
                    if (AliveRefids != null)
                    {
                        AliveRefids.Remove(r);
                    }
                    if (TrackingRefids != null)
                    {
                        TrackingRefids.Remove(r);
                    }
                    LuaHub.LogError(l, "Releasing Refid: " + r + "!. ");
#endif
                }
            }
        }

        public IntPtr L
        {
            get { return l; }
            set
            {
                if (man.IsClosed)
                {
                    return;
                }
                if (l == value)
                {
                    return;
                }
                var old = l;
                l = value;
                if (old != IntPtr.Zero)
                {
                    old.unref(lr);
                }
                if (l != IntPtr.Zero)
                {
                    l.pushthread();
                    lr = l.refer();
                }
                else
                {
                    lr = 0;
                }
            }
        }

        public int Refid
        {
            get { return r; }
            set
            {
                r = value;
#if DEBUG_LOG_REFIDS
                if (value != 0)
                {
                    AliveRefids = AliveRefids ?? new HashSet<int>();
                    AliveRefids.Add(value);
                    if (L == IntPtr.Zero)
                    {
                        UnityEngine.Debug.LogErrorFormat("Setting Refid: {0}!.. ", value);
                    }
                    else
                    {
                        LuaHub.LogError(L, "Setting Refid: " + value + "!. ");
                    }
                }
#endif
            }
        }

        internal LuaRef()
        {
        }
        ~LuaRef()
        {
            man.RegPendingRecycle(this);
        }
        public void Dispose()
        {
            man.DoImmediateRecycle(this);
        }

        public bool IsClosed { get { return man == null || man.IsClosed; } }
    }

    internal static class LuaThreadRefHelper
    {
        public static LuaThreadRefMan GetOrCreateRefMan(this IntPtr l)
        {
            l.checkstack(1);
            l.pushlightuserdata(LuaConst.LRKEY_REF_MAN); // #man
            l.gettable(lua.LUA_REGISTRYINDEX); // man
            if (l.isuserdata(-1))
            {
                LuaThreadRefMan man = null;
                try
                {
                    IntPtr pud = l.touserdata(-1);
                    if (pud != IntPtr.Zero)
                    {
                        IntPtr hval = Marshal.ReadIntPtr(pud);
                        GCHandle handle = (GCHandle)hval;
                        man = handle.Target as LuaThreadRefMan;
                    }
                }
                catch { }
                l.pop(1); // X
                return man;
            }
            else
            {
                l.checkstack(5);
                l.pop(1); // X
                l.pushlightuserdata(LuaConst.LRKEY_REF_MAN); // #man
                LuaThreadRefMan man = new LuaThreadRefMan();
                var h = l.PushLuaRawObject(man); // #man man
                l.PushCommonMetaTable(); // #man man meta
                l.setmetatable(-2); // #man man
                l.newtable(); // #man man env
                l.pushlightuserdata(LuaConst.LRKEY_OBJ_META_EX); // #man man env #meta
                l.pushlightuserdata(h); // #man man env #meta meta
                l.settable(-3); // #man man env
                l.setfenv(-2); // #man man
                l.settable(lua.LUA_REGISTRYINDEX); // X
                return man;
            }
        }
        public static void DisposeRefMan(this IntPtr l)
        {
            l.checkstack(1);
            l.pushlightuserdata(LuaConst.LRKEY_REF_MAN); // #man
            l.gettable(lua.LUA_REGISTRYINDEX); // man
            if (l.isuserdata(-1))
            {
                LuaThreadRefMan man = null;
                try
                {
                    IntPtr pud = l.touserdata(-1);
                    if (pud != IntPtr.Zero)
                    {
                        IntPtr hval = Marshal.ReadIntPtr(pud);
                        GCHandle handle = (GCHandle)hval;
                        man = handle.Target as LuaThreadRefMan;
                    }
                }
                catch { }
                if (man != null)
                {
                    man.Close();
                }
                l.pop(1); // X
                l.pushlightuserdata(LuaConst.LRKEY_REF_MAN); // #man
                l.pushnil(); // #man nil
                l.settable(lua.LUA_REGISTRYINDEX); // X
            }
            else
            {
                l.pop(1); // X
            }
        }
    }

    public static class LuaStateHelper
    {
        public static void DisposeRefMan(this IntPtr l)
        {
            LuaThreadRefHelper.DisposeRefMan(l);
        }

        public static bool GetHierarchicalRaw(this IntPtr l, int index, string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;
            var hkeys = key.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (hkeys == null)
                return false;
            if (l == IntPtr.Zero)
                return false;

            //var oldtop = l.gettop();
            l.pushvalue(index); // table
            l.pushnil(); // table result
            l.insert(-2); // result table
            for (int i = 0; i < hkeys.Length; ++i)
            {
                if (l.istable(-1) || l.IsUserData(-1))
                {
                    l.GetField(-1, hkeys[i]); // result table newresult
                    l.replace(-3); // newresult table
                    l.pop(1); // newresult
                    l.pushvalue(-1); // newresult newtable
                }
                else
                {
                    l.pushnil();
                    l.replace(-3); // newresult table
                    break;
                }
            }
            l.pop(1); // result
            return true;
        }
        public static object GetHierarchical(this IntPtr l, int index, string key)
        {
            if (GetHierarchicalRaw(l, index, key))
            {
                var rv = l.GetLua(-1);
                l.pop(1);
                return rv;
            }
            return null;
        }
        public static object GetHierarchical(this LuaOnStackTable tab, string key)
        {
            if (ReferenceEquals(tab, null) || tab.L == IntPtr.Zero)
            {
                return null;
            }
            return GetHierarchical(tab.L, tab.StackPos, key);
        }
        public static object GetHierarchical(this LuaTable tab, string key)
        {
            if (ReferenceEquals(tab, null) || tab.L == IntPtr.Zero)
            {
                return null;
            }
            var l = tab.L;
            l.PushLua(tab);
            var rv = GetHierarchical(l, -1, key);
            l.pop(1);
            return rv;
        }
        //public static object GetHierarchical(this LuaOnStackUserData ud, string key)
        //{
        //    if (ReferenceEquals(ud, null) || ud.L == IntPtr.Zero)
        //    {
        //        return null;
        //    }
        //    var l = ud.L;
        //    using (var lr = new LuaStateRecover(l))
        //    {
        //        if (ud.PushToLua())
        //        {
        //            return GetHierarchical(l, -1, key);
        //        }
        //        return null;
        //    }
        //}

        public static bool SetHierarchicalRaw(this IntPtr l, int index, string key, int valindex)
        {
            if (string.IsNullOrEmpty(key))
                return false;
            var hkeys = key.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (hkeys == null || hkeys.Length < 1)
                return false;
            if (l == IntPtr.Zero)
                return false;

            var rindex = l.NormalizeIndex(valindex);
            l.pushvalue(index); // table
            for (int i = 0; i < hkeys.Length - 1; ++i)
            {
                if (l.istable(-1) || l.IsUserData(-1))
                {
                    l.GetField(-1, hkeys[i]); // table result
                    if (l.isnoneornil(-1))
                    {
                        l.pop(1); // table
                        l.newtable(); // table result
                        l.pushvalue(-1); // table result result
                        l.SetField(-3, hkeys[i]); // table result
                    }
                    l.remove(-2); // result
                }
                else
                {
                    l.pop(1);
                    return false;
                }
            }
            if (l.istable(-1) || l.IsUserData(-1))
            {
                l.pushvalue(rindex); // table val
                l.SetField(-2, hkeys[hkeys.Length - 1]); // table
                l.pop(1);
                return true;
            }
            else
            {
                l.pop(1);
                return false;
            }
        }
        public static bool SetHierarchical(this IntPtr l, int index, string key, object val)
        {
            if (string.IsNullOrEmpty(key))
                return false;
            var hkeys = key.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (hkeys == null || hkeys.Length < 1)
                return false;
            if (l == IntPtr.Zero)
                return false;

            var rindex = l.NormalizeIndex(index);
            l.PushLua(val);
            var rv = SetHierarchicalRaw(l, rindex, key, -1);
            l.pop(1);
            return rv;
        }
        public static bool SetHierarchical(this LuaOnStackTable tab, string key, object val)
        {
            if (ReferenceEquals(tab, null) || tab.L == IntPtr.Zero)
            {
                return false;
            }
            return SetHierarchical(tab.L, tab.StackPos, key, val);
        }
        public static bool SetHierarchical(this LuaTable tab, string key, object val)
        {
            if (ReferenceEquals(tab, null) || tab.L == IntPtr.Zero)
            {
                return false;
            }
            var l = tab.L;
            l.PushLua(tab);
            var rv = SetHierarchical(l, -1, key, val);
            l.pop(1);
            return rv;
        }
        //public static bool SetHierarchical(this LuaOnStackUserData ud, string key, object val)
        //{
        //    if (ReferenceEquals(ud, null) || ud.L == IntPtr.Zero)
        //    {
        //        return false;
        //    }
        //    var l = ud.L;
        //    using (var lr = new LuaStateRecover(l))
        //    {
        //        if (ud.PushToLua())
        //        {
        //            return SetHierarchical(l, -1, key, val);
        //        }
        //        return false;
        //    }
        //}

        internal static int GetContinuationFunc(this IntPtr l)
        {
            l.pushlightuserdata(LuaConst.LRKEY_CO_CONTINUE); // #con
            l.gettable(lua.LUA_REGISTRYINDEX); // con
            if (l.istable(-1))
            {
                l.pushthread(); // con thd
                l.gettable(-2); // con func
                l.remove(-2); // func
                if (l.isfunction(-1))
                {
                    return 1;
                }
            }
            l.pop(1); // X
            return 0;
        }
        internal static void SetContinuationFunc(this IntPtr l, int index)
        {
            index = l.NormalizeIndex(index);
            l.pushlightuserdata(LuaConst.LRKEY_CO_CONTINUE); // #con
            l.gettable(lua.LUA_REGISTRYINDEX); // con
            if (!l.istable(-1))
            {
                l.pop(1); // X
                if (l.isnoneornil(index))
                {
                    return;
                }
                l.newtable(); // con
                l.pushlightuserdata(LuaConst.LRKEY_CO_CONTINUE); // con #con
                l.pushvalue(-2); // con #con con
                l.settable(lua.LUA_REGISTRYINDEX); // con
                l.newtable(); // con meta
                l.PushString(LuaConst.LS_COMMON_K); // con meta "k"
                l.SetField(-2, LuaConst.LS_META_KEY_MODE); // con meta
                l.setmetatable(-2); // con
            }
            l.pushthread(); // con thd
            l.pushvalue(index); // con thd func
            l.settable(-3); // con
            l.pop(1); // X
        }

#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
        [ThreadStatic] internal static BiDict<IntPtr, UnityEngineEx.CoroutineRunner.CoroutineInfo> _LuaThreadToCoroutineInfo;
        internal static BiDict<IntPtr, UnityEngineEx.CoroutineRunner.CoroutineInfo> LuaThreadToCoroutineInfo
        {
            get
            {
                var map = _LuaThreadToCoroutineInfo;
                if (map == null)
                {
                    _LuaThreadToCoroutineInfo = map = new BiDict<IntPtr, UnityEngineEx.CoroutineRunner.CoroutineInfo>();
                }
                return map;
            }
        }
        public static UnityEngineEx.CoroutineRunner.CoroutineInfo GetUnityCoroutine(IntPtr l)
        {
            UnityEngineEx.CoroutineRunner.CoroutineInfo info = null;
            var map = _LuaThreadToCoroutineInfo;
            if (map != null)
            {
                map.ForwardMap.TryGetValue(l, out info);
            }
            return info;
        }
        public static IntPtr GetLuaCoroutine(UnityEngineEx.CoroutineRunner.CoroutineInfo info)
        {
            IntPtr l = IntPtr.Zero;
            var map = _LuaThreadToCoroutineInfo;
            if (map != null)
            {
                map.BackwardMap.TryGetValue(info, out l);
            }
            return l;
        }

        public class LuaCoroutineAbortedException : CoroutineRunner.CoroutineAbortedException
        {
            public bool IsHandled = false;

            public override string ToString()
            {
                if (IsHandled)
                {
                    return base.ToString();
                }
                else
                {
                    throw this;
                }
            }
            public override string Message
            {
                get
                {
                    if (IsHandled)
                    {
                        return base.Message;
                    }
                    else
                    {
                        throw this;
                    }
                }
            }
        }
        public static void AbortLuaCoroutine()
        {
            throw new LuaCoroutineAbortedException();
        }
        public static Action DelAbortLuaCoroutine = AbortLuaCoroutine;

        public struct LuaCoroutineAborterRecorder : IDisposable
        {
            private Action _Old;

            public LuaCoroutineAborterRecorder(IntPtr l)
            {
                _Old = CoroutineRunner.AbortCoroutineDelegate;
                CoroutineRunner.AbortCoroutineDelegate = DelAbortLuaCoroutine;
            }

            public void Dispose()
            {
                CoroutineRunner.AbortCoroutineDelegate = _Old;
            }
        }
#endif

        [ThreadStatic] private static IntPtr _RunningLuaThread;
        public static IntPtr RunningLuaThread { get { return _RunningLuaThread; } }

        public struct LuaRunningThreadRecorder : IDisposable
        {
            private IntPtr _oldRunningState;
            private IntPtr _oldRunningThread;

            public LuaRunningThreadRecorder(IntPtr l)
            {
                _oldRunningState = LuaHub.RunningLuaState;
                _oldRunningThread = _RunningLuaThread;
                LuaHub.RunningLuaState = l;
                _RunningLuaThread = l;
            }
            public void Dispose()
            {
                LuaHub.RunningLuaState = _oldRunningState;
                _RunningLuaThread = _oldRunningThread;
            }
        }
    }
}

namespace LuaLib
{
    public static partial class LuaHub
    {
        public static void PushLua(this IntPtr l, LuaLib.LuaState val)
        {
            if (l.Indicator() != val.L.Indicator())
            {
                l.PushLuaObject(val);
            }
            else
            {
                val.L.pushthread();
                if (val.L != l)
                {
                    val.L.xmove(l, 1);
                }
            }
        }
        public static void PushLua(this IntPtr l, LuaLib.LuaOnStackThread val)
        {
            if (l.Indicator() != val.L.Indicator())
            {
                l.PushLuaObject(val);
            }
            else
            {
                val.L.pushthread();
                if (val.L != l)
                {
                    val.L.xmove(l, 1);
                }
            }
        }
        public static void PushLua(this IntPtr l, LuaLib.LuaThread val)
        {
            if (l.Indicator() != val.L.Indicator())
            {
                l.PushLuaObject(val);
            }
            else
            {
                val.L.pushthread();
                if (val.L != l)
                {
                    val.L.xmove(l, 1);
                }
            }
        }
        public static void GetLua(this IntPtr l, int index, out LuaLib.LuaOnStackThread val)
        {
            if (l.isthread(index))
            {
                val = new LuaLib.LuaOnStackThread(l, index);
                return;
            }
            val = null;
            return;
        }

        private class LuaPushNative_LuaState : LuaPushNativeBase<LuaLib.LuaState>
        {
            public override LuaState GetLua(IntPtr l, int index)
            {
                return new LuaLib.LuaState(l.tothread(index));
            }
            public override IntPtr PushLua(IntPtr l, LuaLib.LuaState val)
            {
                if (l.Indicator() != val.L.Indicator())
                {
                    l.PushLuaObject(val);
                }
                else
                {
                    val.L.pushthread();
                    if (val.L != l)
                    {
                        val.L.xmove(l, 1);
                    }
                }
                return IntPtr.Zero;
            }
        }
        private static LuaPushNative_LuaState ___tpn_LuaState = new LuaPushNative_LuaState();
        private class LuaPushNative_LuaOnStackThread : LuaPushNativeBase<LuaLib.LuaOnStackThread>
        {
            public override LuaOnStackThread GetLua(IntPtr l, int index)
            {
                IntPtr lthd = l.tothread(index);
                l.pushvalue(index);
                int refid = l.refer();
                return new LuaLib.LuaOnStackThread(refid, lthd);
            }
            public override IntPtr PushLua(IntPtr l, LuaLib.LuaOnStackThread val)
            {
                if (l.Indicator() != val.L.Indicator())
                {
                    l.PushLuaObject(val);
                }
                else
                {
                    val.L.pushthread();
                    if (val.L != l)
                    {
                        val.L.xmove(l, 1);
                    }
                }
                return IntPtr.Zero;
            }
        }
        private static LuaPushNative_LuaOnStackThread ___tpn_LuaOnStackThread = new LuaPushNative_LuaOnStackThread();
        private class LuaPushNative_LuaThread : LuaPushNativeBase<LuaLib.LuaThread>
        {
            public override LuaThread GetLua(IntPtr l, int index)
            {
                return new LuaLib.LuaThread(new LuaFunc(l, index));
            }
            public override IntPtr PushLua(IntPtr l, LuaLib.LuaThread val)
            {
                if (l.Indicator() != val.L.Indicator())
                {
                    l.PushLuaObject(val);
                }
                else
                {
                    val.L.pushthread();
                    if (val.L != l)
                    {
                        val.L.xmove(l, 1);
                    }
                }
                return IntPtr.Zero;
            }
        }
        private static LuaPushNative_LuaThread ___tpn_LuaThread = new LuaPushNative_LuaThread();
    }
}