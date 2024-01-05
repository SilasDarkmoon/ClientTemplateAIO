using System;
using System.Collections.Generic;
using UnityEngineEx;

using lua = LuaLib.LuaCoreLib;
using lual = LuaLib.LuaAuxLib;
using luae = LuaLib.LuaLibEx;

namespace LuaLib
{
    public static class LuaProfileHelper
    {
        public static void GetFuncInfo(this IntPtr l, int index, out string funcName, out string fileName, out int lineStart, out int lineCur)
        {
            funcName = null;
            fileName = null;
            lineStart = -1;
            lineCur = -1;
            if (index < -100000)
            {
                l.checkstack(3);
                l.GetGlobal(LuaConst.LS_LIB_DEBUG); // debug
                l.GetField(-1, LuaConst.LS_LIB_GETINFO); // debug getinfo
                l.pushnumber(index - int.MinValue + 2); // debug getinfo 2
                l.call(1, 1); // debug info
                if (l.istable(-1))
                {
                    l.GetField(-1, LuaConst.LS_LIB_NAME); // debug info name
                    funcName = l.GetString(-1);
                    l.pop(1); // debug info
                    l.GetField(-1, LuaConst.LS_LIB_SHORT_SRC); // debug info short_src
                    fileName = l.GetString(-1);
                    l.pop(1); // debug info
                    l.GetField(-1, LuaConst.LS_LIB_LINEDEFINED); // debug info linedefined
                    lineStart = (int)l.tonumber(-1);
                    l.pop(1); // debug info
                    l.GetField(-1, LuaConst.LS_LIB_CURRENTLINE); // debug info currentline
                    lineCur = (int)l.tonumber(-1);
                    l.pop(3); // X
                }
                else
                {
                    l.pop(2);
                }
            }
            else
            {
                if (l.isfunction(index))
                {
                    index = l.NormalizeIndex(index);
                    l.checkstack(3);
                    l.GetGlobal(LuaConst.LS_LIB_DEBUG); // debug
                    l.GetField(-1, LuaConst.LS_LIB_GETINFO); // debug getinfo
                    l.pushvalue(index); // debug getinfo func
                    l.call(1, 1); // debug info
                    l.GetField(-1, LuaConst.LS_LIB_NAME); // debug info name
                    funcName = l.GetString(-1);
                    l.pop(1); // debug info
                    l.GetField(-1, LuaConst.LS_LIB_SHORT_SRC); // debug info short_src
                    fileName = l.GetString(-1);
                    l.pop(1); // debug info
                    l.GetField(-1, LuaConst.LS_LIB_LINEDEFINED); // debug info linedefined
                    lineStart = (int)l.tonumber(-1);
                    l.pop(1); // debug info
                    l.GetField(-1, LuaConst.LS_LIB_CURRENTLINE); // debug info currentline
                    lineCur = (int)l.tonumber(-1);
                    l.pop(3); // X
                }
            }
        }
        public static string GetSimpleStackInfo(this IntPtr l, int level)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < level; ++i)
            {
                string funcName, fileName;
                int lineStart, lineCur;
                l.GetFuncInfo(int.MinValue + i, out funcName, out fileName, out lineStart, out lineCur);
                if (string.IsNullOrEmpty(fileName) && lineCur < 0)
                {
                    break;
                }
                if (i > 0)
                {
                    sb.Insert(0, ">");
                }
                if (lineCur > 0)
                {
                    sb.Insert(0, lineCur);
                    sb.Insert(0, ":");
                }
                int start = -1, end = -1;
                start = fileName.LastIndexOfAny(new[] { '/', '\\' });
                end = fileName.LastIndexOf('.');
                if (start >= 0 && end > start + 1)
                {
                    var file = fileName.Substring(start + 1, end - start - 1);
                    sb.Insert(0, file);
                }
                else
                {
                    sb.Insert(0, "?");
                }
            }
            return sb.ToString();
        }

        public static readonly lua.CFunction Del_CallFuncWithProfilerTag = new lua.CFunction(CallFuncWithProfilerTag);
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        private static int CallFuncWithProfilerTag(IntPtr l)
        {
            var tag = l.GetString(lua.upvalueindex(2));
            var argc = l.gettop();
            l.checkstack(argc + 1);
            l.pushvalue(lua.upvalueindex(1));
            for (int i = 0; i < argc; ++i)
            {
                l.pushvalue(i + 1);
            }
#if ENABLE_PROFILER && ENABLE_PROFILER_LUA
            using (var pcon = new UnityEngineEx.ProfilerContext(tag))
#endif
            {
                l.pcall(argc, lua.LUA_MULTRET, 0);
            }
            return l.gettop() - argc;
        }

        public static readonly lua.CFunction Del_ProfilerBeginSample = new lua.CFunction(ProfilerBeginSample);
        public static readonly lua.CFunction Del_ProfilerEndSample = new lua.CFunction(ProfilerEndSample);
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ProfilerBeginSample(IntPtr l)
        {
#if ENABLE_PROFILER
            var argc = l.gettop();
            if (argc <= 0)
            {
                var simplestack = l.GetSimpleStackInfo(8);
                UnityEngine.Profiling.Profiler.BeginSample(simplestack);
            }
            else
            {
                var name = l.GetString(1);
                if (argc == 1)
                {
                    UnityEngine.Profiling.Profiler.BeginSample(name);
                }
                else
                {
                    var args = new object[argc - 1];
                    for (int i = 0; i < args.Length; ++i)
                    {
                        args[i] = l.GetLua(2 + i);
                    }
                    try
                    {
                        name = string.Format(name, args);
                    }
                    catch (Exception e)
                    {
                        l.LogError(e);
                    }
                    UnityEngine.Profiling.Profiler.BeginSample(name);
                }
            }
#endif
            return 0;
        }
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int ProfilerEndSample(IntPtr l)
        {
#if ENABLE_PROFILER
            UnityEngine.Profiling.Profiler.EndSample();
#endif
            return 0;
        }

        public static readonly lua.CFunction Del_AppendProfilerMessage = new lua.CFunction(AppendProfilerMessage);
        [AOT.MonoPInvokeCallback(typeof(lua.CFunction))]
        public static int AppendProfilerMessage(IntPtr l)
        {
#if PROFILER_EX_FRAME_TIMER
            var argc = l.gettop();
            if (argc <= 0)
            {
            }
            else
            {
                var message = l.GetLua(1);
                if (argc == 1 || !(message is string))
                {
                    ProfilerEx.AppendFrameTimerMessage(message);
                }
                else
                {
                    var args = new object[argc - 1];
                    for (int i = 0; i < args.Length; ++i)
                    {
                        args[i] = l.GetLua(2 + i);
                    }
                    try
                    {
                        var strmess = string.Format((string)message, args);
                        ProfilerEx.AppendFrameTimerMessage(strmess);
                    }
                    catch (Exception e)
                    {
                        l.LogError(e);
                    }
                }
            }
#endif
            return 0;
        }
    }
}