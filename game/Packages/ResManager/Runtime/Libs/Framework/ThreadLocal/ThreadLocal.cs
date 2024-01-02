using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using uobj = UnityEngine.Object;

namespace UnityEngineEx
{
    public class ThreadLocalObj
    {
        public interface INativeThreadLocal
        {
            bool Ready { get; }
            void SetContainer(object obj);
            T GetContainer<T>() where T : class;
            ulong GetThreadID();
        }
        public static INativeThreadLocal NativeThreadLocalWrapper;

        #region ThreadId
        private static long _LastThreadId = 0;
        [ThreadStatic] private static long _ThreadId;
        public static ulong GetThreadId()
        {
#if MOD_NATIVETHREADLOCAL
            //if (!System.Threading.Thread.CurrentThread.IsThreadPoolThread)
            {
                if (NativeThreadLocalWrapper != null && NativeThreadLocalWrapper.Ready)
                {
                    return NativeThreadLocalWrapper.GetThreadID();
                }
            }
#endif
            if (_ThreadId == 0)
            {
                _ThreadId = System.Threading.Interlocked.Increment(ref _LastThreadId);
            }
            return (ulong)_ThreadId;
        }
        #endregion

        private class ThreadLocalObjectContainer
        {
            public object Target;
        }
        private class ThreadInfo
        {
            public List<ThreadLocalObjectContainer> Storage = new List<ThreadLocalObjectContainer>();
        }
        [ThreadStatic] private static ThreadInfo _ThreadInfo;
        private static ThreadInfo GetThreadInfo()
        {
#if MOD_NATIVETHREADLOCAL
            if (!System.Threading.Thread.CurrentThread.IsThreadPoolThread)
            {
                if (NativeThreadLocalWrapper != null && NativeThreadLocalWrapper.Ready)
                {
                    var info = NativeThreadLocalWrapper.GetContainer<ThreadInfo>();
                    if (info == null)
                    {
                        info = new ThreadInfo();
                        NativeThreadLocalWrapper.SetContainer(info);
                    }
                    return info;
                }
            }
#endif
            if (_ThreadInfo == null)
            {
                _ThreadInfo = new ThreadInfo();
            }
            return _ThreadInfo;
        }

        private static int _NextSlotId = 0;
        private int _SlotId = System.Threading.Interlocked.Increment(ref _NextSlotId) - 1;

        protected Func<object> _InitFunc;
        public ThreadLocalObj() { }
        public ThreadLocalObj(Func<object> initFunc)
        {
            _InitFunc = initFunc;
        }

        public object Value
        {
            get
            {
                var con = GetThreadInfo();
                var list = con.Storage;
                while (_SlotId >= list.Count)
                {
                    list.Add(null);
                }
                var ocon = list[_SlotId];
                if (ocon == null)
                {
                    ocon = new ThreadLocalObjectContainer();
                    list[_SlotId] = ocon;
                    if (_InitFunc != null)
                    {
                        ocon.Target = _InitFunc();
                    }
                }
                return ocon.Target;
            }
            set
            {
                var con = GetThreadInfo();
                var list = con.Storage;
                while (_SlotId >= list.Count)
                {
                    list.Add(null);
                }
                var ocon = list[_SlotId];
                if (ocon == null)
                {
                    ocon = new ThreadLocalObjectContainer();
                    list[_SlotId] = ocon;
                    if (_InitFunc != null)
                    {
                        ocon.Target = _InitFunc();
                    }
                }
                ocon.Target = value;
            }
        }
    }
    public class ThreadLocalObj<T> : ThreadLocalObj
    {
        public ThreadLocalObj() { }
        public ThreadLocalObj(Func<T> initFunc)
        {
            if (initFunc != null)
            {
                _InitFunc = () => initFunc();
            }
        }

        public new T Value
        {
            get
            {
                var rv = base.Value;
                if (rv is T)
                {
                    return (T)rv;
                }
                return default(T);
            }
            set
            {
                base.Value = value;
            }
        }
    }
}