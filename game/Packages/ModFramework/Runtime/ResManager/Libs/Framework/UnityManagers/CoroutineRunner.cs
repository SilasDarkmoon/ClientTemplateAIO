using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;

namespace UnityEngineEx
{
    public static class CoroutineRunner
    {
        public class CoroutineInfo
        {
            public MonoBehaviour behav;
            public IEnumerator work;
            public Coroutine coroutine;
        }
        public static readonly HashSet<CoroutineInfo> RunningCoroutines = new HashSet<CoroutineInfo>();
        public static readonly Dictionary<Coroutine, CoroutineInfo> RunningCoroutinesMap = new Dictionary<Coroutine, CoroutineInfo>();

        public static CoroutineInfo GetCoroutineInfo(Coroutine co)
        {
            CoroutineInfo info = null;
            if (co != null)
            {
                RunningCoroutinesMap.TryGetValue(co, out info);
            }
            return info;
        }

        private static GameObject CoroutineRunnerObj;
        private static CoroutineRunnerBehav CoroutineRunnerBehav;

        public static Coroutine CurrentCoroutine { get; private set; }
        public static CoroutineInfo CurrentCoroutineInfo { get; private set; }

        public static Coroutine StartCoroutine(this IEnumerator work)
        {
            if (CoroutineRunnerObj != null && !CoroutineRunnerObj.activeInHierarchy)
            {
                Object.Destroy(CoroutineRunnerObj);
                CoroutineRunnerObj = null;
            }
            if (!CoroutineRunnerObj)
            {
                CoroutineRunnerObj = new GameObject();
                CoroutineRunnerObj.hideFlags = HideFlags.HideAndDontSave;
                Object.DontDestroyOnLoad(CoroutineRunnerObj);
            }
            if (!CoroutineRunnerBehav)
            {
                CoroutineRunnerBehav = CoroutineRunnerObj.AddComponent<CoroutineRunnerBehav>();
            }
            var info = new CoroutineInfo() { behav = CoroutineRunnerBehav, work = work };
            CurrentCoroutineInfo = info;
            info.coroutine = CoroutineRunnerBehav.StartCoroutine(SafeEnumerator(work, info));
            if (info.coroutine != null)
            {
                RunningCoroutinesMap[info.coroutine] = info;
            }
            return info.coroutine;
        }
        public static Coroutine StartCoroutine(this IEnumerable work)
        {
            if (work == null)
            {
                return null;
            }
            return StartCoroutine(work.GetEnumerator());
        }
        public static void StopCoroutine(this Coroutine c)
        {
            try
            {
                CoroutineInfo info;
                if (RunningCoroutinesMap.TryGetValue(c, out info))
                {
                    info.behav.StopCoroutine(c);
                }
                else
                {
                    if (CoroutineRunnerBehav)
                    {
                        CoroutineRunnerBehav.StopCoroutine(c);
                    }
                }
            }
            catch (Exception e)
            {
                PlatDependant.LogError(e);
            }
            DisposeDeadCoroutines();
        }
        public static void StopCoroutine(this CoroutineInfo info)
        {
            try
            {
                info.behav.StopCoroutine(info.coroutine);
            }
            catch (Exception e)
            {
                PlatDependant.LogError(e);
            }
            DisposeDeadCoroutines();
        }
        internal static bool SafeMoveNext(IEnumerator work, CoroutineInfo info, out object result)
        {
            bool success = false;
            result = null;
            try
            {
                success = work.MoveNext();
                if (success)
                {
                    result = work.Current;
                }
            }
            catch
            {
                RunningCoroutines.Remove(info);
                if (info.coroutine != null)
                {
                    RunningCoroutinesMap.Remove(info.coroutine);
                }
                if (info.work is IDisposable)
                {
                    try
                    {
                        ((IDisposable)info.work).Dispose();
                    }
                    catch (Exception e)
                    {
                        PlatDependant.LogError(e);
                    }
                }
                throw;
            }
            finally
            {
                CurrentCoroutine = null;
                CurrentCoroutineInfo = null;
            }
            return success;
        }
        public static IEnumerator SafeEnumerator(this IEnumerator work, CoroutineInfo info)
        {
            RunningCoroutines.Add(info);
            if (work != null)
            {
                CurrentCoroutine = info.coroutine;
                CurrentCoroutineInfo = info;
                object result;
                while (SafeMoveNext(work, info, out result))
                {
                    yield return result;
                    CurrentCoroutine = info.coroutine;
                    CurrentCoroutineInfo = info;
                }
            }
            RunningCoroutines.Remove(info);
            if (info.coroutine != null)
            {
                RunningCoroutinesMap.Remove(info.coroutine);
            }
        }

        public static Coroutine StartSafeCoroutine(this MonoBehaviour behav, IEnumerator work)
        {
            if (behav != null && behav.isActiveAndEnabled)
            {
                if (CoroutineRunnerObj != null && !CoroutineRunnerObj.activeInHierarchy)
                {
                    Object.Destroy(CoroutineRunnerObj);
                    CoroutineRunnerObj = null;
                }
                if (!CoroutineRunnerObj)
                {
                    CoroutineRunnerObj = new GameObject();
                    CoroutineRunnerObj.hideFlags = HideFlags.HideAndDontSave;
                    Object.DontDestroyOnLoad(CoroutineRunnerObj);
                }
                if (!CoroutineRunnerBehav)
                {
                    CoroutineRunnerBehav = CoroutineRunnerObj.AddComponent<CoroutineRunnerBehav>();
                }
                var info = new CoroutineInfo() { behav = behav, work = work };
                CurrentCoroutineInfo = info;
                info.coroutine = behav.StartCoroutine(SafeEnumerator(work, info));
                if (info.coroutine != null)
                {
                    RunningCoroutinesMap[info.coroutine] = info;
                }
                return info.coroutine;
            }
            return null;
        }
        public static void StopSafeCoroutine(this MonoBehaviour behav, Coroutine coroutine)
        {
            if (behav)
            {
                behav.StopCoroutine(coroutine);
            }
            DisposeDeadCoroutines();
        }

        public static void DisposeDeadCoroutines()
        {
            if (RunningCoroutines.Count > 0)
            {
                RunningCoroutines.RemoveWhere(CheckDeadCoroutine);
            }
            RemoveDeadCoroutineFromMap();
        }
        internal static void RemoveDeadCoroutineFromMap()
        {
            LinkedList<Coroutine> toBeRemoved = new LinkedList<Coroutine>();
            foreach (var kvp in RunningCoroutinesMap)
            {
                if (!RunningCoroutines.Contains(kvp.Value))
                {
                    toBeRemoved.AddLast(kvp.Key);
                }
            }
            for (var node = toBeRemoved.First; node != null; node = node.Next)
            {
                RunningCoroutinesMap.Remove(node.Value);
            }
        }
        public static bool CheckDeadCoroutine(CoroutineInfo info)
        {
            if (!info.behav)
            {
                if (info.work is IDisposable)
                {
                    try
                    {
                        ((IDisposable)info.work).Dispose();
                    }
                    catch (Exception e)
                    {
                        PlatDependant.LogError(e);
                    }
                    info.work = null;
                }
                return true;
            }
            return false;
        }
        public static void DisposeAllCoroutines(MonoBehaviour onbehav)
        {
            LinkedList<IDisposable> toBeDisposed = new LinkedList<IDisposable>();
            foreach (var info in RunningCoroutines)
            {
                if (info.work is IDisposable && info.behav == onbehav)
                {
                    toBeDisposed.AddLast((IDisposable)info.work);
                }
            }
            RunningCoroutines.RemoveWhere(info => info.behav == onbehav);
            foreach (var work in toBeDisposed)
            {
                try
                {
                    work.Dispose();
                }
                catch (Exception e)
                {
                    PlatDependant.LogError(e);
                }
            }
            RemoveDeadCoroutineFromMap();
        }
        internal static void DisposeAllCoroutinesOnDestroyRunner(CoroutineRunnerBehav onbehav)
        {
            if (onbehav == CoroutineRunnerBehav)
            {
                CoroutineRunnerObj = null;
                CoroutineRunnerBehav = null;
            }
            DisposeAllCoroutines((MonoBehaviour)onbehav);
        }

        public static IEnumerable GetEnumerable(this IEnumerator work)
        {
            try
            {
                if (work != null)
                {
                    while (work.MoveNext())
                    {
                        yield return work.Current;
                    }
                }
            }
            finally
            {
                if (work is IDisposable)
                {
                    ((IDisposable)work).Dispose();
                }
            }
        }
        public static IEnumerator GetEmptyEnumerator()
        {
            yield break;
        }

        // Coroutine Abort
        public class CoroutineAbortedException : Exception
        {
            public CoroutineAbortedException() : base("Coroutine aborted!") { }
        }
        public class CoroutineAbortedYieldable : IEnumerator
        {
            public static readonly CoroutineAbortedYieldable Instance = new CoroutineAbortedYieldable();
            public object Current { get { throw new CoroutineAbortedException(); } }
            public bool MoveNext()
            {
                throw new CoroutineAbortedException();
            }
            public void Reset()
            {
                throw new CoroutineAbortedException();
            }
        }

        public static Action AbortCoroutineDelegate;
        public static void AbortCoroutine()
        {
            if (AbortCoroutineDelegate != null)
            {
                AbortCoroutineDelegate();
            }
            else
            {
                throw new CoroutineAbortedException();
            }
        }

        public static void AbortCoroutine(Coroutine c)
        {
            if (c == null)
            {
                return; // if we need abort current coroutine, we'd batter use AbortCoroutine() without parameters
            }
            else if (c == CurrentCoroutine)
            {
                AbortCoroutine();
            }
            else
            {
                StopCoroutine(c);
            }
        }
        public static void AbortCoroutine(CoroutineInfo info)
        {
            if (info == null)
            {
                return; // if we need abort current coroutine, we'd batter use AbortCoroutine() without parameters
            }
            else if (info == CurrentCoroutineInfo)
            {
                AbortCoroutine();
            }
            else
            {
                StopCoroutine(info);
            }
        }
    }

    namespace CoroutineTasks
    {
        // Work - Wrap a IEnumerator with return value and done flag.
        // Await - Wait for an started work.
        // Monitor - Starts inner work and monitor the started work on another coroutine.
        // Task - Is monitor, the inner work is changing, but the task obj itself is not changed, and can be created by concat and concurrent.

        public abstract class CoroutineWork : IEnumerator, IDisposable
        {
            protected bool _Started = false;
            protected bool _Done = false;
            protected object _Result = null;
            protected bool _Suspended = false;

            protected long _Total = 10000;
            protected long _Progress = 0;
            public virtual long Total { get { return _Total; } set { _Total = value; } }
            public virtual long Progress { get { return _Progress; } set { _Progress = value; } }
            public float NormalizedProgress { get { return (float)Progress / (float)Total; } }

            public abstract object Current { get; }
            public abstract bool MoveNext();
            public virtual void Reset() { }
            public abstract void Dispose();

            public event Action OnDone = () => { };

            public bool TryStart()
            {
                if (!_Started)
                {
                    _Started = true;
                    Start();
                    return true;
                }
                return false;
            }
            protected virtual void Start() { }
            public virtual bool Done {
                get { return _Done; }
                protected set
                {
                    var old = _Done;
                    _Done = value;
                    if (!old && value)
                    {
                        OnDone();
                    }
                }
            }
            public virtual object Result
            {
                get { return _Result; }
                set { _Result = value; }
            }
        }
        public class CoroutineWorkSingle : CoroutineWork
        {
            public CoroutineWorkSingle() { }
            public CoroutineWorkSingle(IEnumerator work)
            {
                _Inner = work;
            }
            protected IEnumerator _Inner;

            public override object Current
            {
                get
                {
                    if (_Suspended || _Inner == null)
                    {
                        return null;
                    }
                    else
                    {
                        return _Inner.Current;
                    }
                }
            }
            public override bool MoveNext()
            {
                if (_Done)
                {
                    return false;
                }
                if (_Inner == null)
                {
                    Done = true;
                    return false;
                }
                if (_Suspended)
                {
                    return true;
                }
                if (_Inner.MoveNext())
                {
                    return true;
                }
                else
                {
                    Done = true;
                    return false;
                }
            }
            public override void Dispose()
            {
                var dis = _Inner as IDisposable;
                if (dis != null)
                {
                    dis.Dispose();
                }
            }

            public void SetWork(IEnumerator work)
            {
                _Inner = work;
            }
        }
        public class CoroutineWorkQueue : CoroutineWork
        {
            protected readonly List<CoroutineWork> _Works = new List<CoroutineWork>();
            protected int _CurWorkIndex = 0;
            protected CoroutineWork Work
            {
                get
                {
                    if (_Works.Count > _CurWorkIndex)
                    {
                        return _Works[_CurWorkIndex];
                    }
                    return null;
                }
            }

            public override object Current
            {
                get
                {
                    if (_Suspended || Done)
                    {
                        return null;
                    }
                    var work = Work;
                    if (work == null)
                    {
                        return null;
                    }
                    else
                    {
                        return work.Current;
                    }
                }
            }
            public override bool MoveNext()
            {
                if (Done)
                {
                    return false;
                }
                var work = Work;
                if (work == null)
                {
                    Done = true;
                    return false;
                }
                if (_Suspended)
                {
                    return true;
                }
                TryStart();
                while (true)
                {
                    if (work.MoveNext())
                    {
                        return true;
                    }
                    else
                    {
                        var partResult = work.Result;
                        ++_CurWorkIndex;
                        work = Work;
                        if (work == null)
                        {
                            _Result = partResult;
                            break;
                        }
                        else
                        {
                            work.Result = partResult;
                        }
                    }
                }
                Done = true;
                return false;
            }
            public override void Dispose()
            {
                for (int i = 0; i < _Works.Count; ++i)
                {
                    _Works[i].Dispose();
                }
            }
            protected override void Start()
            {
                if (_Result != null)
                {
                    var work = Work;
                    if (work != null)
                    {
                        work.Result = _Result;
                    }
                }
            }

            public void AddWork(CoroutineWork work)
            {
                if (work != null)
                {
                    _Works.Add(work);
                }
            }
            public CoroutineWork FirstWork
            {
                get
                {
                    if (_Works.Count > 0)
                    {
                        return _Works[0];
                    }
                    return null;
                }
            }
            public CoroutineWork LastWork
            {
                get
                {
                    if (_Works.Count > 0)
                    {
                        return _Works[_Works.Count - 1];
                    }
                    return null;
                }
            }

            public override long Progress
            {
                get
                {
                    var myprog = Math.Pow(0.1, _CurWorkIndex);
                    var baseprog = 1.0 - myprog;

                    double curprog = 0;
                    var work = Work;
                    if (work != null)
                    {
                        curprog = ((double)work.Progress) / ((double)work.Total);
                    }
                    return _Progress = (long)((baseprog + (curprog * myprog)) * Total);
                }
                set { }
            }
        }

        public abstract class CoroutineMonitor : CoroutineWork
        {
            public override object Current
            {
                get
                {
                    return null;
                }
            }

        }
        public class CoroutineWorkAsyncOp : CoroutineMonitor
        {
            protected AsyncOperation _Inner;

            public CoroutineWorkAsyncOp() { }
            public CoroutineWorkAsyncOp(AsyncOperation op)
            {
                _Inner = op;
            }
            public void SetWork(AsyncOperation work)
            {
                _Inner = work;
            }

            public override bool MoveNext()
            {
                if (_Done)
                {
                    return false;
                }
                if (_Inner == null)
                {
                    Done = true;
                    return false;
                }
                _Progress = (long)(_Inner.progress * _Total);
                if (_Suspended)
                {
                    return true;
                }
                if (_Inner.isDone)
                {
                    Done = true;
                    if (_Inner is AssetBundleRequest)
                    {
                        _Result = ((AssetBundleRequest)_Inner).asset;
                    }
                    return false;
                }
                else
                {
                    return true;
                }
            }
            public override void Dispose()
            {
            }

            public override long Progress
            {
                get
                {
                    if (_Inner == null)
                    {
                        return 0;
                    }
                    else
                    {
                        return _Progress = (long)(_Inner.progress * _Total);
                    }
                }
                set { }
            }
            public override long Total
            {
                get
                {
                    MoveNext();
                    return _Total;
                }
                set { }
            }
            public override object Result
            {
                get
                {
                    MoveNext();
                    if (_Result == null && _Inner is AssetBundleRequest)
                    {
                        _Result = ((AssetBundleRequest)_Inner).asset;
                    }
                    return _Result;
                }
                set
                {
                    _Result = value;
                }
            }
            public override bool Done
            {
                get
                {
                    MoveNext();
                    if (_Inner != null)
                    {
                        base.Done = _Inner.isDone;
                    }
                    return base.Done;
                }
                protected set { base.Done = value; }
            }
        }
        public class CoroutineMonitorRaw : CoroutineWorkSingle
        {
            public CoroutineMonitorRaw() : base() { }
            public CoroutineMonitorRaw(IEnumerator work) : base(work)
            {
                TryStart();
            }

            protected override void Start()
            {
                if (_Inner != null)
                {
                    CoroutineRunner.StartCoroutine(RealWork());
                }
                else
                {
                    Done = true;
                }
            }
            protected IEnumerator RealWork()
            {
                while (_Inner.MoveNext())
                {
                    yield return _Inner.Current;
                }
                Done = true;
            }
            public override bool MoveNext()
            {
                TryStart();
                if (_Done)
                {
                    return false;
                }
                if (_Inner == null)
                {
                    Done = true;
                    return false;
                }
                if (_Suspended)
                {
                    return true;
                }
                return true;
            }

            public override long Progress
            {
                get
                {
                    if (_Started && !_Done)
                    {
                        MoveNext();
                    }
                    return _Progress;
                }
                set
                {
                    _Progress = value;
                }
            }
            public override long Total
            {
                get
                {
                    if (_Started && !_Done)
                    {
                        MoveNext();
                    }
                    return _Total;
                }
                set
                {
                    _Total = value;
                }
            }
            public override object Result
            {
                get
                {
                    if (_Started && !_Done)
                    {
                        MoveNext();
                    }
                    return _Result;
                }
                set
                {
                    _Result = value;
                }
            }
            public override bool Done
            {
                get
                {
                    if (_Started && !_Done)
                    {
                        MoveNext();
                    }
                    return base.Done;
                }
                protected set { base.Done = value; }
            }
        }
        public class CoroutineMonitorSingle : CoroutineMonitor
        {
            public CoroutineMonitorSingle() { }
            public CoroutineMonitorSingle(CoroutineWork work)
            {
                _Inner = work;
                TryStart();
            }

            protected CoroutineWork _Inner;

            public override bool MoveNext()
            {
                TryStart();
                if (_Done)
                {
                    return false;
                }
                if (_Inner == null)
                {
                    Done = true;
                    return false;
                }
                _Progress = _Inner.Progress;
                if (_Suspended)
                {
                    return true;
                }
                if (_Inner.Done)
                {
                    Done = true;
                    _Result = _Inner.Result;
                    return false;
                }
                else
                {
                    return true;
                }
            }
            public override void Dispose()
            {
                if (_Inner != null)
                {
                    _Inner.Dispose();
                }
            }
            protected override void Start()
            {
                if (_Inner != null)
                {
                    _Total = _Inner.Total;
                    if (_Result != null)
                    {
                        _Inner.Result = _Result;
                    }
                    _Inner.StartCoroutine();
                }
            }

            public override long Progress
            {
                get
                {
                    if (_Started && !_Done)
                    {
                        MoveNext();
                    }
                    if (_Inner != null)
                    {
                        _Progress = _Inner.Progress;
                    }
                    return _Progress;
                }
                set { }
            }
            public override long Total
            {
                get
                {
                    if (_Started && !_Done)
                    {
                        MoveNext();
                    }
                    if (_Inner != null)
                    {
                        _Total = _Inner.Total;
                    }
                    return _Total;
                }
                set { }
            }
            public override object Result
            {
                get
                {
                    if (_Started && !_Done)
                    {
                        MoveNext();
                    }
                    if (_Result == null && _Inner != null)
                    {
                        _Result = _Inner.Result;
                    }
                    return _Result;
                }
                set
                {
                    _Result = value;
                }
            }
            public override bool Done
            {
                get
                {
                    if (_Started && !_Done)
                    {
                        MoveNext();
                    }
                    if (_Inner != null)
                    {
                        base.Done = _Inner.Done;
                    }
                    return base.Done;
                }
                protected set { base.Done = value; }
            }

            public void SetWork(CoroutineWork work)
            {
                if (!_Started)
                {
                    _Inner = work;
                }
            }
        }
        public class CoroutineMonitorConcurrent : CoroutineMonitor
        {
            protected readonly List<CoroutineWork> _Works = new List<CoroutineWork>();

            public override bool MoveNext()
            {
                TryStart();
                if (_Done)
                {
                    return false;
                }
                if (_Works.Count == 0)
                {
                    Done = true;
                    return false;
                }
                _Progress = CheckProgress();
                if (_Suspended)
                {
                    return true;
                }
                bool done = true;
                for (int i = 0; i < _Works.Count; ++i)
                {
                    if (!_Works[i].Done)
                    {
                        done = false;
                    }
                }
                if (!done)
                {
                    return true;
                }
                Done = true;
                CheckResult();
                return false;
            }
            public override void Dispose()
            {
                for (int i = 0; i < _Works.Count; ++i)
                {
                    _Works[i].Dispose();
                }
            }
            protected override void Start()
            {
                for (int i = 0; i < _Works.Count; ++i)
                {
                    _Works[i].StartCoroutine();
                }
            }

            protected long CheckProgress()
            {
                var total = base.Total;
                double nprog = 0.0;
                for (int i = 0; i < _Works.Count; ++i)
                {
                    var myfull = Math.Pow(0.1, i);
                    if (i < _Works.Count - 1)
                    {
                        myfull *= 0.9;
                    }
                    nprog += myfull * _Works[i].NormalizedProgress;
                }
                return (long)(nprog * (double)total);
            }
            protected void CheckResult()
            {
                var result = _Result as object[];
                if (result == null || result.Length < _Works.Count)
                {
                    result = new object[_Works.Count];
                    _Result = result;
                }
                for (int i = 0; i < _Works.Count; ++i)
                {
                    result[i] = _Works[i].Result;
                }
            }
            public override long Progress
            {
                get
                {
                    if (_Started && !_Done)
                    {
                        MoveNext();
                    }
                    _Progress = CheckProgress();
                    return _Progress;
                }
                set { }
            }
            public override long Total
            {
                get
                {
                    if (_Started && !_Done)
                    {
                        MoveNext();
                    }
                    return base.Total;
                }
                set { }
            }
            public override object Result
            {
                get
                {
                    if (_Started && !_Done)
                    {
                        MoveNext();
                    }
                    CheckResult();
                    return _Result;
                }
                set
                {
                    if (value is IList<object>)
                    {
                        var list = (IList<object>)value;

                        var result = _Result as object[];
                        if (result == null || result.Length < _Works.Count)
                        {
                            result = new object[_Works.Count];
                            _Result = result;
                        }
                        for (int i = 0; i < _Works.Count; ++i)
                        {
                            object r = null;
                            if (list.Count > i)
                            {
                                r = list[i];
                            }
                            result[i] = r;
                        }
                    }
                    else if (value is IList)
                    {
                        var list = (IList)value;

                        var result = _Result as object[];
                        if (result == null || result.Length < _Works.Count)
                        {
                            result = new object[_Works.Count];
                            _Result = result;
                        }
                        for (int i = 0; i < _Works.Count; ++i)
                        {
                            object r = null;
                            if (list.Count > i)
                            {
                                r = list[i];
                            }
                            result[i] = r;
                        }
                    }
                    else
                    {
                        var result = _Result as object[];
                        if (result == null || result.Length < _Works.Count)
                        {
                            result = new object[_Works.Count];
                            _Result = result;
                        }
                        for (int i = 0; i < _Works.Count; ++i)
                        {
                            result[i] = value;
                        }
                    }
                }
            }
            public override bool Done
            {
                get
                {
                    if (_Started && !_Done)
                    {
                        MoveNext();
                    }
                    bool done = true;
                    for (int i = 0; i < _Works.Count; ++i)
                    {
                        if (!_Works[i].Done)
                        {
                            done = false;
                        }
                    }
                    return base.Done = done;
                }
                protected set { base.Done = value; }
            }

            public int WorkCount { get { return _Works.Count; } }
            public void AddWork(CoroutineWork work)
            {
                if (work != null)
                {
                    _Works.Add(work);
                    if (_Started)
                    {
                        work.StartCoroutine();
                    }
                }
            }
            public void InsertWork(CoroutineWork work, int index)
            {
                if (work != null)
                {
                    _Works.Insert(index, work);
                    if (_Started)
                    {
                        work.StartCoroutine();
                    }
                }
            }
        }

        //public class CoroutineAwait : CoroutineWork
        //{
        //    protected CoroutineWork _Inner;

        //    public override object Current
        //    {
        //        get
        //        {
        //            return null;
        //        }
        //    }
        //    public override bool MoveNext()
        //    {
        //        if (Done)
        //        {
        //            return false;
        //        }
        //        if (_Inner == null)
        //        {
        //            Done = true;
        //            return false;
        //        }
        //        if (_Suspended)
        //        {
        //            return true;
        //        }
        //        if (_Inner.Done)
        //        {
        //            Done = true;
        //            _Result = _Inner.Result;
        //            return false;
        //        }
        //        else
        //        {
        //            return true;
        //        }
        //    }
        //    public override void Dispose()
        //    {
        //    }

        //    public void SetWork(CoroutineWork work)
        //    {
        //        _Inner = work;
        //    }
        //}

        //public class CoroutineTask : CoroutineMonitorSingle
        //{
        //    protected CoroutineWork GetRealWorkFromSubWork(IEnumerator work)
        //    {
        //        var realwork = work as CoroutineWork;
        //        if (realwork == null)
        //        {
        //            var worksingle = new CoroutineWorkSingle();
        //            worksingle.SetWork(work);
        //            realwork = worksingle;
        //        }
        //        else
        //        {
        //            var rtask = work as CoroutineTask;
        //            if (rtask != null)
        //            {
        //                realwork = rtask._Inner;
        //                var rawait = new CoroutineAwait();
        //                rawait.SetWork(realwork);
        //                rtask.SetWork(rawait);
        //            }
        //        }
        //        return realwork;
        //    }
        //    protected CoroutineWorkQueue MakeInnerQueue()
        //    {
        //        var queue = _Inner as CoroutineWorkQueue;
        //        if (queue == null)
        //        {
        //            queue = new CoroutineWorkQueue();
        //            queue.AddWork(_Inner);
        //            _Inner = queue;
        //        }
        //        return queue;
        //    }
        //    protected CoroutineMonitorConcurrent MakeInnerConcurrent()
        //    {
        //        var con = _Inner as CoroutineMonitorConcurrent;
        //        if (con == null)
        //        {
        //            con = new CoroutineMonitorConcurrent();
        //            con.AddWork(_Inner);
        //            _Inner = con;
        //        }
        //        return con;
        //    }

        //    public void Concat(IEnumerator work)
        //    {
        //        if (work == null)
        //        {
        //            return;
        //        }
        //        var realwork = GetRealWorkFromSubWork(work);

        //        if (_Inner == null)
        //        {
        //            _Inner = realwork;
        //        }
        //        else
        //        {
        //            var queue = MakeInnerQueue();
        //            queue.AddWork(realwork);
        //        }
        //    }
        //    public void Concurrent(IEnumerator work)
        //    {
        //        if (work == null)
        //        {
        //            return;
        //        }
        //        var realwork = GetRealWorkFromSubWork(work);

        //        if (_Inner == null)
        //        {
        //            _Inner = realwork;
        //        }
        //        else
        //        {
        //            var queue = MakeInnerConcurrent();
        //            queue.AddWork(realwork);
        //        }
        //    }
        //    public void ConcurrentLast(IEnumerator work)
        //    {
        //        if (work == null)
        //        {
        //            return;
        //        }
        //        var realwork = GetRealWorkFromSubWork(work);

        //        if (_Inner == null)
        //        {
        //            _Inner = realwork;
        //        }
        //        else
        //        {
        //            var queue = _Inner as CoroutineWorkQueue;
        //            if (queue == null)
        //            {
        //                var queuec = MakeInnerConcurrent();
        //                queuec.AddWork(realwork);
        //            }
        //            else
        //            {
        //                var last = queue.LastWork;
        //                if (last == null)
        //                {
        //                    queue.AddWork(realwork);
        //                }
        //                else
        //                {
        //                    var queuec = last as CoroutineMonitorConcurrent;
        //                    if (queuec != null)
        //                    {
        //                        queuec.AddWork(realwork);
        //                    }
        //                    else
        //                    {
        //                        queuec = new CoroutineMonitorConcurrent();
        //                        queuec.AddWork(last);
        //                        queuec.AddWork(realwork);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}
    }

    public class WaitForTickCount : CustomYieldInstruction
    {
        private int _ToTick;

        public WaitForTickCount(int delta)
        {
            SetDelta(delta);
        }
        public void SetDelta(int delta)
        {
            _ToTick = Environment.TickCount + delta;
        }

        public override bool keepWaiting
        {
            get
            {
                return _ToTick - Environment.TickCount > 0;
            }
        }
    }
}