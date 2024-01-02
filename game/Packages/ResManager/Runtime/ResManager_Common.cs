using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using uobj = UnityEngine.Object;

namespace UnityEngineEx
{
    public static partial class ResManager
    {
        public static class LifetimeOrders
        {
            public const int Zero                   = 0;    // 0
            public const int EditorPrepare          = 20;   // Editor do startup check.
            public const int CrossEvent             = 50;   // Cross Event.
            public const int ABLoader               = 100;  // Check Obb State.
            public const int EntrySceneBgLoad       = 200;  // Load EntrySceneBg (The loaded maybe old)
            public const int EntrySceneBgUnload     = 300;  // Unload All
            public const int ResLoader              = 400;  // Reinit resloader, after this, the res loaded should be updated.
            public const int PostResLoader          = 500;  // In Update, we only init part of the init items between [ResLoader, PostResLoader]
            public const int PreEntrySceneDone      = 900;  // Nearly Done. Before change to the next scene.
            public const int EntrySceneDone         = 1000; // Change to the next scene.
        }
        public interface ILifetime
        {
            int Order { get; }
            void Prepare();
            void Init();
            void Cleanup();
        }
        public interface IInitPrepareAsync
        {
            IEnumerator PrepareAsync();
        }
        public interface IInitAsync
        {
            IEnumerator InitAsync();
        }
        public delegate void ProgressReportDelegate(string key, object attached, double val);
        public interface IInitProgressReporter
        {
            string GetPhaseDesc();
            int CountWorkStep();
            event ProgressReportDelegate ReportProgress;
        }
        public class ActionInitItem : ILifetime
        {
            private readonly int _Order;
            private readonly Action _Pre;
            private readonly Action _Act;
            private readonly Action _Clean;
            public ActionInitItem(int order, Action act)
            {
                _Order = order;
                _Act = act;
            }
            public ActionInitItem(int order, Action pre, Action act)
            {
                _Order = order;
                _Pre = pre;
                _Act = act;
            }
            public ActionInitItem(int order, Action pre, Action act, Action clean)
            {
                _Order = order;
                _Pre = pre;
                _Act = act;
                _Clean = clean;
            }

            public int Order { get { return _Order; } }
            public void Prepare()
            {
                if (_Pre != null)
                {
                    _Pre();
                }
            }
            public void Init()
            {
                if (_Act != null)
                {
                    _Act();
                }
            }
            public void Cleanup()
            {
                if (_Clean != null)
                {
                    _Clean();
                }
            }
        }
        private static List<ILifetime> _InitList;
        private static List<ILifetime> InitList
        {
            get
            {
                if (_InitList == null)
                {
                    _InitList = new List<ILifetime>();
                }
                return _InitList;
            }
        }
        public static void AddInitItem(ILifetime item)
        {
            if (item != null)
            {
                InitList.Add(item);
            }
        }
        public static void AddInitItem(int order, Action act)
        {
            if (act != null)
            {
                InitList.Add(new ActionInitItem(order, act));
            }
        }
        public static ILifetime[] GetInitItems(int min, int max)
        {
            ILifetime[] found = null;
            var list = _InitList;
            if (list != null)
            {
                int start = -1, cnt = 0;
                list.Sort((ia, ib) => ia.Order - ib.Order);
                for (int i = 0; i < list.Count; ++i)
                {
                    var order = list[i].Order;
                    if (order >= min && order <= max)
                    {
                        if (start < 0)
                        {
                            start = i;
                        }
                        ++cnt;
                    }
                }
                if (start >= 0)
                {
                    found = new ILifetime[cnt];
                    list.CopyTo(start, found, 0, cnt);
                }
            }
            return found ?? new ILifetime[0];
        }
        public static ILifetime[] GetInitItems(int order)
        {
            return GetInitItems(order, order);
        }
        public static ILifetime GetFirstInitItem(int min, int max)
        {
            var list = _InitList;
            if (list != null)
            {
                list.Sort((ia, ib) => ia.Order - ib.Order);
                for (int i = 0; i < list.Count; ++i)
                {
                    var order = list[i].Order;
                    if (order >= min && order <= max)
                    {
                        return list[i];
                    }
                }
            }
            return null;
        }
        public static ILifetime GetInitItem(int order)
        {
            return GetFirstInitItem(order, order);
        }
        public static void RemoveInitItem(int min, int max)
        {
            var list = _InitList;
            if (list != null)
            {
                int start = -1, cnt = 0;
                list.Sort((ia, ib) => ia.Order - ib.Order);
                for (int i = 0; i < list.Count; ++i)
                {
                    var order = list[i].Order;
                    if (order >= min && order <= max)
                    {
                        if (start < 0)
                        {
                            start = i;
                        }
                        ++cnt;
                    }
                }
                if (start >= 0)
                {
                    list.RemoveRange(start, cnt);
                }
            }
        }
        public static void RemoveInitItem(int order)
        {
            RemoveInitItem(order, order);
        }
        public static void RemoveInitItem(ILifetime item)
        {
            var list = _InitList;
            list.Remove(item);
        }
        public static void Init()
        {
            var list = _InitList;
            if (list != null)
            {
                list.Sort((ia, ib) => ia.Order - ib.Order);
                for (int i = 0; i < list.Count; ++i)
                {
                    try
                    {
                        list[i].Prepare();
                    }
                    catch (Exception e)
                    {
                        PlatDependant.LogError(e);
                    }
                }
                for (int i = 0; i < list.Count; ++i)
                {
                    try
                    {
                        list[i].Init();
                    }
                    catch (Exception e)
                    {
                        PlatDependant.LogError(e);
                    }
                }
            }
        }
        public static IEnumerator InitAsync(ProgressReportDelegate reportProgress)
        {
            if (reportProgress != null)
            {
                reportProgress("Desc", "Startup", 0);
            }
            AsyncWorkTimer.Check();

            var list = _InitList;
            if (list != null)
            {
                list.Sort((ia, ib) => ia.Order - ib.Order);
                int totalStep = 0;
                int totalPhase = 0;
                int[] workSteps = null;
                if (reportProgress != null)
                {
                    workSteps = new int[list.Count];
                    for (int i = 0; i < list.Count; ++i)
                    {
                        var pr = list[i] as IInitProgressReporter;
                        var pasync = list[i] as IInitPrepareAsync;
                        if (pasync != null)
                        {
                            IEnumerator work = null;
                            try
                            {
                                work = pasync.PrepareAsync();
                            }
                            catch (Exception e)
                            {
                                PlatDependant.LogError(e);
                            }
                            if (work != null)
                            {
                                if (AsyncWorkTimer.Check())
                                {
                                    yield return null;
                                }
                                while (true)
                                {
                                    bool haveNext = false;
                                    try
                                    {
                                        haveNext = work.MoveNext();
                                    }
                                    catch (Exception e)
                                    {
                                        PlatDependant.LogError(e);
                                    }
                                    if (!haveNext)
                                    {
                                        break;
                                    }
                                    else
                                    {
                                        yield return work.Current;
                                    }
                                }
                            }
                        }
                        if (pr != null && pr is IInitAsync)
                        {
                            pr.ReportProgress += reportProgress;
                            int step = 0;
                            try
                            {
                                step = pr.CountWorkStep();
                            }
                            catch (Exception e)
                            {
                                PlatDependant.LogError(e);
                            }
                            totalStep += step;
                            ++totalPhase;
                            workSteps[i] = step;
                        }
                        else if (pasync == null)
                        {
                            try
                            {
                                list[i].Prepare();
                            }
                            catch (Exception e)
                            {
                                PlatDependant.LogError(e);
                            }
                        }
                    }
                    if (totalStep > 0)
                    {
                        reportProgress("HaveWorkToDo", null, 1);
                    }
                    reportProgress("TotalPhase", null, totalPhase);
                    reportProgress("TotalStep", null, totalStep);
                    reportProgress("WorkingPhase", null, 0);
                    reportProgress("WorkingStep", null, 0);
                }
                else
                {
                    for (int i = 0; i < list.Count; ++i)
                    {
                        try
                        {
                            list[i].Prepare();
                        }
                        catch (Exception e)
                        {
                            PlatDependant.LogError(e);
                        }
                    }
                }
                int workingPhase = 0;
                int workingStep = 0;
                for (int i = 0; i < list.Count; ++i)
                {
                    var init = list[i];
                    var inita = init as IInitAsync;
                    if (inita != null)
                    {
                        if (AsyncWorkTimer.Check())
                        {
                            yield return null;
                        }
                        if (reportProgress != null)
                        {
                            var pr = init as IInitProgressReporter;
                            if (pr != null)
                            {
                                reportProgress("WorkingPhase", null, ++workingPhase);
                                try
                                {
                                    var phaseDesc = pr.GetPhaseDesc();
                                    reportProgress("Desc", phaseDesc, 0);
                                }
                                catch (Exception e)
                                {
                                    PlatDependant.LogError(e);
                                }
                            }
                        }
                        IEnumerator work = null;
                        try
                        {
                            work = inita.InitAsync();
                        }
                        catch (Exception e)
                        {
                            PlatDependant.LogError(e);
                        }
                        if (work != null)
                        {
                            if (AsyncWorkTimer.Check())
                            {
                                yield return null;
                            }
                            while (true)
                            {
                                bool haveNext = false;
                                try
                                {
                                    haveNext = work.MoveNext();
                                }
                                catch (Exception e)
                                {
                                    PlatDependant.LogError(e);
                                }
                                if (!haveNext)
                                {
                                    break;
                                }
                                else
                                {
                                    yield return work.Current;
                                }
                            }
                        }
                        if (reportProgress != null)
                        {
                            if (workSteps[i] > 0)
                            {
                                workingStep += workSteps[i];
                                reportProgress("WorkingStep", null, workingStep);
                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            list[i].Init();
                        }
                        catch (Exception e)
                        {
                            PlatDependant.LogError(e);
                        }
                    }
                }
                if (reportProgress != null)
                {
                    for (int i = 0; i < list.Count; ++i)
                    {
                        var pr = list[i] as IInitProgressReporter;
                        if (pr != null && pr is IInitAsync)
                        {
                            pr.ReportProgress -= reportProgress;
                        }
                    }
                    reportProgress("AllDone", null, 1);
                }
            }
        }
        public static void Cleanup()
        {
            var list = _InitList;
            if (list != null)
            {
                list.Sort((ia, ib) => ib.Order - ia.Order);
                for (int i = 0; i < list.Count; ++i)
                {
                    try
                    {
                        list[i].Cleanup();
                    }
                    catch (Exception e)
                    {
                        PlatDependant.LogError(e);
                    }
                }
            }
        }
    }
}