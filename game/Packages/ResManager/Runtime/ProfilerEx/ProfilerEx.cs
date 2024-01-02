using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;

namespace UnityEngineEx
{
    public static class ProfilerEx
    {
        static ProfilerEx()
        {
#if PROFILER_EX_FRAME_TIMER_AUTO_LOG
            FrameTimerAutoLog = true;
#endif
#if PROFILER_EX_FRAME_TIMER_AUTO_LOG_ONLY_LAG
            FrameTimerAutoLogOnlyLag = true;
#endif
#if PROFILER_EX_FRAME_TIMER_AUTO_LOG_TO_ERROR
            FrameTimerAutoLogToError = true;
#endif
#if ENABLE_PROFILER
            UnityEngine.Profiling.Profiler.maxUsedMemory = 512 * 1024 * 1024;
#endif
        }

        public static bool IsProfilerEnabled()
        {
#if ENABLE_PROFILER
            return true;
#else
            return false;
#endif
        }

        public static bool IsDeepProfiling()
        {
#if ENABLE_PROFILER
            List<Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle> allrecorders = new List<Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle>();
            Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle.GetAvailable(allrecorders);
            for (int i = 0; i < allrecorders.Count; ++i)
            {
                var recorder = allrecorders[i];
                var desc = Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle.GetDescription(recorder);
                if ((desc.Flags & Unity.Profiling.LowLevel.MarkerFlags.ScriptDeepProfiler) != 0)
                {
                    return true;
                }
            }
#endif
            return false;
        }

        public static void AppendFrameTimerMessage(string message)
        {
#if PROFILER_EX_FRAME_TIMER
            _FrameTimerExtra.Append(message);
#endif
        }
        public static void AppendFrameTimerMessage<T>(T message)
        {
#if PROFILER_EX_FRAME_TIMER
            _FrameTimerExtra.Append(message);
#endif
        }
        public static void AppendFrameTimerMessageLine(string message)
        {
#if PROFILER_EX_FRAME_TIMER
            _FrameTimerExtra.AppendLine(message);
#endif
        }
        public static void AppendFrameTimerMessageLine<T>(T message)
        {
#if PROFILER_EX_FRAME_TIMER
            _FrameTimerExtra.Append(message);
            _FrameTimerExtra.AppendLine();
#endif
        }

        public static bool FrameTimerAutoLog = false;
        public static bool FrameTimerAutoLogOnlyLag = false;
        public static bool FrameTimerAutoLogToError = false;

        private static System.Text.StringBuilder _FrameTimerExtra = new System.Text.StringBuilder();
        private static int _FrameTimerFrameIndex = 0;
        private static double _FrameTimerLastInterval = 0;
        private static System.Diagnostics.Stopwatch _FrameTimerWatch;
        private static void FrameTimerUpdate()
        {
            if (_FrameTimerWatch == null)
            {
                _FrameTimerWatch = new System.Diagnostics.Stopwatch();
                _FrameTimerFrameIndex = UnityEngine.Time.frameCount;
                _FrameTimerExtra.Clear();
                _FrameTimerWatch.Start();
            }
            else
            {
                _FrameTimerWatch.Stop();
                _FrameTimerLastInterval = _FrameTimerWatch.Elapsed.TotalMilliseconds;
                if (FrameTimerAutoLog)
                {
                    if (!FrameTimerAutoLogOnlyLag || Application.targetFrameRate <= 0 || _FrameTimerLastInterval > 1050.0 / Application.targetFrameRate)
                    {
                        if (FrameTimerAutoLogToError)
                        {
                            Debug.LogErrorFormat("Frame time {0}: {1} ms.\n{2}", _FrameTimerFrameIndex, _FrameTimerLastInterval, _FrameTimerExtra);
                        }
                        else
                        {
                            Debug.LogFormat("Frame time {0}: {1} ms.\n{2}", _FrameTimerFrameIndex, _FrameTimerLastInterval, _FrameTimerExtra);
                        }
                    }
                }
                _FrameTimerFrameIndex = UnityEngine.Time.frameCount;
                _FrameTimerExtra.Clear();
                _FrameTimerWatch.Restart();
            }
        }

        public static void InitFrameTimer()
        {
            var oldloop = UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();
            var subs = oldloop.subSystemList;
            for (int i = 0; i < subs.Length; ++i)
            {
                var oldsub = subs[i];
                if (oldsub.type == typeof(ProfilerEx))
                {
                    return;
                }
            }

            var newsubs = new UnityEngine.LowLevel.PlayerLoopSystem[subs.Length + 1];
            for (int i = 0; i < subs.Length; ++i)
            {
                newsubs[i + 1] = subs[i];
            }
            newsubs[0] = new UnityEngine.LowLevel.PlayerLoopSystem()
            {
                type = typeof(ProfilerEx),
                updateDelegate = FrameTimerUpdate,
            };

            oldloop.subSystemList = newsubs;
            UnityEngine.LowLevel.PlayerLoop.SetPlayerLoop(oldloop);
        }

        [RuntimeInitializeOnLoadMethod]
        private static void InitFrameTimerConditional()
        {
#if PROFILER_EX_FRAME_TIMER
            InitFrameTimer();
#if PROFILER_EX_FRAME_TIMER_RENDER
            InitFrameTimerForRendering();
#endif
#endif
        }

        public static bool FindPlayerLoopSystem(string name, out UnityEngine.LowLevel.PlayerLoopSystem foundparent, out int foundindex)
        {
            var parent = UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();
            if (name.Contains("."))
            {
                string[] parts = name.Split('.', StringSplitOptions.RemoveEmptyEntries);
                foundindex = -1;
                for (int i = 0; i < parts.Length; ++i)
                {
                    foundindex = -1;
                    var child = parts[i];
                    var subs = parent.subSystemList;
                    if (subs != null)
                    {
                        for (int j = 0; j < subs.Length; ++j)
                        {
                            var sub = subs[j];
                            if (subs[j].ToString() == child)
                            {
                                foundindex = j;
                                break;
                            }
                        }
                    }
                    if (foundindex < 0 || i == parts.Length - 1)
                    {
                        break;
                    }
                    parent = subs[foundindex];
                }
                if (foundindex >= 0)
                {
                    foundparent = parent;
                    return true;
                }
                else
                {
                    foundparent = default(UnityEngine.LowLevel.PlayerLoopSystem);
                    return false;
                }
            }
            else
            {
                FindPlayerLoopSystem(name, parent, out foundparent, out foundindex);
                return foundindex >= 0;
            }
        }

        private static void FindPlayerLoopSystem(string name, UnityEngine.LowLevel.PlayerLoopSystem parent, out UnityEngine.LowLevel.PlayerLoopSystem foundparent, out int foundindex)
        {
            var subs = parent.subSystemList;
            if (subs != null)
            {
                for (int j = 0; j < subs.Length; ++j)
                {
                    if (subs[j].ToString() == name)
                    {
                        foundindex = j;
                        foundparent = parent;
                        return;
                    }
                }
                for (int j = 0; j < subs.Length; ++j)
                {
                    var sub = subs[j];
                    FindPlayerLoopSystem(name, sub, out foundparent, out foundindex);
                    if (foundindex >= 0)
                    {
                        return;
                    }
                }
            }
            foundparent = default(UnityEngine.LowLevel.PlayerLoopSystem);
            foundindex = -1;
            return;
        }

        public static bool SetPlayerLoopSystemSubs(string name, ref UnityEngine.LowLevel.PlayerLoopSystem parent, UnityEngine.LowLevel.PlayerLoopSystem[] newsubs)
        {
            if (name.Contains("."))
            {
                string[] parts = name.Split('.', StringSplitOptions.RemoveEmptyEntries);
                return SetPlayerLoopSystemSubs(parts, 0, ref parent, newsubs);
            }
            else
            {
                return SetPlayerLoopSystemSubsInChildren(name, ref parent, newsubs);
            }
        }
        public static bool SetPlayerLoopSystemSubs(string[] levelnames, int level, ref UnityEngine.LowLevel.PlayerLoopSystem parent, UnityEngine.LowLevel.PlayerLoopSystem[] newsubs)
        {
            if (level >= levelnames.Length)
            {
                return false;
            }
            var child = levelnames[level];
            var subs = parent.subSystemList;
            if (subs != null)
            {
                for (int j = 0; j < subs.Length; ++j)
                {
                    if (subs[j].ToString() == child)
                    {
                        if (level == levelnames.Length - 1)
                        {
                            subs[j].subSystemList = newsubs;
                            return true;
                        }
                        else
                        {
                            return SetPlayerLoopSystemSubs(levelnames, level + 1, ref subs[j], newsubs);
                        }
                    }
                }
            }
            return false;
        }
        public static bool SetPlayerLoopSystemSubsInChildren(string name, ref UnityEngine.LowLevel.PlayerLoopSystem parent, UnityEngine.LowLevel.PlayerLoopSystem[] newsubs)
        {
            var subs = parent.subSystemList;
            if (subs != null)
            {
                for (int j = 0; j < subs.Length; ++j)
                {
                    if (subs[j].ToString() == name)
                    {
                        subs[j].subSystemList = newsubs;
                        return true;
                    }
                }
                for (int j = 0; j < subs.Length; ++j)
                {
                    if (SetPlayerLoopSystemSubsInChildren(name, ref subs[j], newsubs))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private class FrameTimerPreRender
        {
            public static FrameTimerContext Context = FrameTimerContext.Create("Full Render");

            public static void Update()
            {
                Context.Restart();
            }
        }
        private class FrameTimerPostRender
        {
            public static void Update()
            {
                FrameTimerPreRender.Context.Dispose();
            }
        }
        private static void InitFrameTimerForRendering()
        {
            UnityEngine.LowLevel.PlayerLoopSystem parent;
            int index;
            if (FindPlayerLoopSystem("FinishFrameRendering", out parent, out index))
            {
                var subs = parent.subSystemList;
                if (index > 0 && subs[index - 1].type == typeof(FrameTimerPreRender)
                    && index < subs.Length - 1 && subs[index + 1].type == typeof(FrameTimerPostRender))
                {
                    return;
                }
                int insertedcnt = 0;
                var newsubs = new List<UnityEngine.LowLevel.PlayerLoopSystem>(subs);
                if (index <= 0 || subs[index - 1].type != typeof(FrameTimerPreRender))
                {
                    ++insertedcnt;
                    newsubs.Insert(index, new UnityEngine.LowLevel.PlayerLoopSystem()
                    {
                        type = typeof(FrameTimerPreRender),
                        updateDelegate = FrameTimerPreRender.Update,
                    });
                }
                if (index >= subs.Length - 1 || subs[index + 1].type != typeof(FrameTimerPostRender))
                {
                    newsubs.Insert(index + insertedcnt + 1, new UnityEngine.LowLevel.PlayerLoopSystem()
                    {
                        type = typeof(FrameTimerPostRender),
                        updateDelegate = FrameTimerPostRender.Update,
                    });
                }
                var root = UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();
                SetPlayerLoopSystemSubs(parent.ToString(), ref root, newsubs.ToArray());
                UnityEngine.LowLevel.PlayerLoop.SetPlayerLoop(root);
            }
        }
    }

    public struct FrameTimerContext : IDisposable
    {
        private System.Diagnostics.Stopwatch _Timer;
        private string _Message;
        private static Dictionary<string, System.Diagnostics.Stopwatch> _Timers = new Dictionary<string, System.Diagnostics.Stopwatch>();

        public static System.Diagnostics.Stopwatch GetTimer(string name)
        {
            System.Diagnostics.Stopwatch timer;
            if (!_Timers.TryGetValue(name, out timer))
            {
                timer = new System.Diagnostics.Stopwatch();
                _Timers.Add(name, timer);
            }
            return timer;
        }

        public FrameTimerContext(string name)
        {
#if PROFILER_EX_FRAME_TIMER
            _Timer = GetTimer(name);
            _Message = name;
            _Timer.Restart();
#else
            _Timer = null;
            _Message = null;
#endif
        }
        public FrameTimerContext(string name, string message)
        {
#if PROFILER_EX_FRAME_TIMER
            _Timer = GetTimer(name);
            _Message = message;
            _Timer.Restart();
#else
            _Timer = null;
            _Message = null;
#endif
        }
        public FrameTimerContext Restart(string overrideMessage)
        {
#if PROFILER_EX_FRAME_TIMER
            var newinst = new FrameTimerContext();
            newinst._Timer = _Timer;
            if (string.IsNullOrEmpty(overrideMessage))
            {
                newinst._Message = _Message;
            }
            else
            {
                newinst._Message = overrideMessage;
            }
            _Timer.Restart();
            return newinst;
#else
            return default(FrameTimerContext);
#endif
        }
        public FrameTimerContext Restart()
        {
            return Restart(null);
        }
        public FrameTimerContext Restart<T>(T name)
        {
#if PROFILER_EX_FRAME_TIMER
            return Restart(name.ToString());
#else
            return default(FrameTimerContext);
#endif
        }
        public FrameTimerContext Restart<P>(string nameformat, P p)
        {
#if PROFILER_EX_FRAME_TIMER
            return Restart(string.Format(nameformat, p));
#else
            return default(FrameTimerContext);
#endif
        }
        public FrameTimerContext Restart<P1, P2>(string nameformat, P1 p1, P2 p2)
        {
#if PROFILER_EX_FRAME_TIMER
            return Restart(string.Format(nameformat, p1, p2));
#else
            return default(FrameTimerContext);
#endif
        }
        public FrameTimerContext Restart<P1, P2, P3>(string nameformat, P1 p1, P2 p2, P3 p3)
        {
#if PROFILER_EX_FRAME_TIMER
            return Restart(string.Format(nameformat, p1, p2, p3));
#else
            return default(FrameTimerContext);
#endif
        }
        public FrameTimerContext Restart<P1, P2, P3, P4>(string nameformat, P1 p1, P2 p2, P3 p3, P4 p4)
        {
#if PROFILER_EX_FRAME_TIMER
            return Restart(string.Format(nameformat, p1, p2, p3, p4));
#else
            return default(FrameTimerContext);
#endif
        }
        public FrameTimerContext Restart<P1, P2, P3, P4, P5>(string nameformat, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5)
        {
#if PROFILER_EX_FRAME_TIMER
            return Restart(string.Format(nameformat, p1, p2, p3, p4, p5));
#else
            return default(FrameTimerContext);
#endif
        }
        public FrameTimerContext Restart<P1, P2, P3, P4, P5, P6>(string nameformat, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6)
        {
#if PROFILER_EX_FRAME_TIMER
            return Restart(string.Format(nameformat, p1, p2, p3, p4, p5, p6));
#else
            return default(FrameTimerContext);
#endif
        }
        public FrameTimerContext Restart<P1, P2, P3, P4, P5, P6, P7>(string nameformat, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7)
        {
#if PROFILER_EX_FRAME_TIMER
            return Restart(string.Format(nameformat, p1, p2, p3, p4, p5, p6, p7));
#else
            return default(FrameTimerContext);
#endif
        }
        public FrameTimerContext Restart<P1, P2, P3, P4, P5, P6, P7, P8>(string nameformat, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7, P8 p8)
        {
#if PROFILER_EX_FRAME_TIMER
            return Restart(string.Format(nameformat, p1, p2, p3, p4, p5, p6, p7, p8));
#else
            return default(FrameTimerContext);
#endif
        }
        public FrameTimerContext Restart(string nameformat, params object[] args)
        {
#if PROFILER_EX_FRAME_TIMER
            return Restart(string.Format(nameformat, args));
#else
            return default(FrameTimerContext);
#endif
        }

        public static FrameTimerContext Create(string name)
        {
#if PROFILER_EX_FRAME_TIMER
            return new FrameTimerContext(name);
#else
            return default(FrameTimerContext);
#endif
        }
        public static FrameTimerContext Create<T>(T name)
        {
#if PROFILER_EX_FRAME_TIMER
            return new FrameTimerContext(name.ToString());
#else
            return default(FrameTimerContext);
#endif
        }
        public static FrameTimerContext Create<P>(string nameformat, P p)
        {
#if PROFILER_EX_FRAME_TIMER
            return new FrameTimerContext(nameformat, string.Format(nameformat, p));
#else
            return default(FrameTimerContext);
#endif
        }
        public static FrameTimerContext Create<P1, P2>(string nameformat, P1 p1, P2 p2)
        {
#if PROFILER_EX_FRAME_TIMER
            return new FrameTimerContext(nameformat, string.Format(nameformat, p1, p2));
#else
            return default(FrameTimerContext);
#endif
        }
        public static FrameTimerContext Create<P1, P2, P3>(string nameformat, P1 p1, P2 p2, P3 p3)
        {
#if PROFILER_EX_FRAME_TIMER
            return new FrameTimerContext(nameformat, string.Format(nameformat, p1, p2, p3));
#else
            return default(FrameTimerContext);
#endif
        }
        public static FrameTimerContext Create<P1, P2, P3, P4>(string nameformat, P1 p1, P2 p2, P3 p3, P4 p4)
        {
#if PROFILER_EX_FRAME_TIMER
            return new FrameTimerContext(nameformat, string.Format(nameformat, p1, p2, p3, p4));
#else
            return default(FrameTimerContext);
#endif
        }
        public static FrameTimerContext Create<P1, P2, P3, P4, P5>(string nameformat, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5)
        {
#if PROFILER_EX_FRAME_TIMER
            return new FrameTimerContext(nameformat, string.Format(nameformat, p1, p2, p3, p4, p5));
#else
            return default(FrameTimerContext);
#endif
        }
        public static FrameTimerContext Create<P1, P2, P3, P4, P5, P6>(string nameformat, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6)
        {
#if PROFILER_EX_FRAME_TIMER
            return new FrameTimerContext(nameformat, string.Format(nameformat, p1, p2, p3, p4, p5, p6));
#else
            return default(FrameTimerContext);
#endif
        }
        public static FrameTimerContext Create<P1, P2, P3, P4, P5, P6, P7>(string nameformat, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7)
        {
#if PROFILER_EX_FRAME_TIMER
            return new FrameTimerContext(nameformat, string.Format(nameformat, p1, p2, p3, p4, p5, p6, p7));
#else
            return default(FrameTimerContext);
#endif
        }
        public static FrameTimerContext Create<P1, P2, P3, P4, P5, P6, P7, P8>(string nameformat, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7, P8 p8)
        {
#if PROFILER_EX_FRAME_TIMER
            return new FrameTimerContext(nameformat, string.Format(nameformat, p1, p2, p3, p4, p5, p6, p7, p8));
#else
            return default(FrameTimerContext);
#endif
        }
        public static FrameTimerContext Create(string nameformat, params object[] args)
        {
#if PROFILER_EX_FRAME_TIMER
            return new FrameTimerContext(nameformat, string.Format(nameformat, args));
#else
            return default(FrameTimerContext);
#endif
        }

        public void Dispose()
        {
#if PROFILER_EX_FRAME_TIMER
            _Timer.Stop();
            ProfilerEx.AppendFrameTimerMessage(_Message);
            ProfilerEx.AppendFrameTimerMessage(" (ms): ");
            ProfilerEx.AppendFrameTimerMessageLine(_Timer.Elapsed.TotalMilliseconds);
#endif
        }
    }

    public struct ProfilerContext : IDisposable
    {
        public ProfilerContext(string name)
        {
#if ENABLE_PROFILER
            UnityEngine.Profiling.Profiler.BeginSample(name);
#endif
        }

        public static ProfilerContext Create(string name)
        {
#if ENABLE_PROFILER
            return new ProfilerContext(name);
#else
            return default(ProfilerContext);
#endif
        }
        public static ProfilerContext Create<T>(T name)
        {
#if ENABLE_PROFILER
            return new ProfilerContext(name.ToString());
#else
            return default(ProfilerContext);
#endif
        }
        public static ProfilerContext Create<P>(string nameformat, P p)
        {
#if ENABLE_PROFILER
            return new ProfilerContext(string.Format(nameformat, p));
#else
            return default(ProfilerContext);
#endif
        }
        public static ProfilerContext Create<P1, P2>(string nameformat, P1 p1, P2 p2)
        {
#if ENABLE_PROFILER
            return new ProfilerContext(string.Format(nameformat, p1, p2));
#else
            return default(ProfilerContext);
#endif
        }
        public static ProfilerContext Create<P1, P2, P3>(string nameformat, P1 p1, P2 p2, P3 p3)
        {
#if ENABLE_PROFILER
            return new ProfilerContext(string.Format(nameformat, p1, p2, p3));
#else
            return default(ProfilerContext);
#endif
        }
        public static ProfilerContext Create<P1, P2, P3, P4>(string nameformat, P1 p1, P2 p2, P3 p3, P4 p4)
        {
#if ENABLE_PROFILER
            return new ProfilerContext(string.Format(nameformat, p1, p2, p3, p4));
#else
            return default(ProfilerContext);
#endif
        }
        public static ProfilerContext Create<P1, P2, P3, P4, P5>(string nameformat, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5)
        {
#if ENABLE_PROFILER
            return new ProfilerContext(string.Format(nameformat, p1, p2, p3, p4, p5));
#else
            return default(ProfilerContext);
#endif
        }
        public static ProfilerContext Create<P1, P2, P3, P4, P5, P6>(string nameformat, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6)
        {
#if ENABLE_PROFILER
            return new ProfilerContext(string.Format(nameformat, p1, p2, p3, p4, p5, p6));
#else
            return default(ProfilerContext);
#endif
        }
        public static ProfilerContext Create<P1, P2, P3, P4, P5, P6, P7>(string nameformat, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7)
        {
#if ENABLE_PROFILER
            return new ProfilerContext(string.Format(nameformat, p1, p2, p3, p4, p5, p6, p7));
#else
            return default(ProfilerContext);
#endif
        }
        public static ProfilerContext Create<P1, P2, P3, P4, P5, P6, P7, P8>(string nameformat, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5, P6 p6, P7 p7, P8 p8)
        {
#if ENABLE_PROFILER
            return new ProfilerContext(string.Format(nameformat, p1, p2, p3, p4, p5, p6, p7, p8));
#else
            return default(ProfilerContext);
#endif
        }
        public static ProfilerContext Create(string nameformat, params object[] args)
        {
#if ENABLE_PROFILER
            return new ProfilerContext(string.Format(nameformat, args));
#else
            return default(ProfilerContext);
#endif
        }

        public void Dispose()
        {
#if ENABLE_PROFILER
            UnityEngine.Profiling.Profiler.EndSample();
#endif
        }
    }
}