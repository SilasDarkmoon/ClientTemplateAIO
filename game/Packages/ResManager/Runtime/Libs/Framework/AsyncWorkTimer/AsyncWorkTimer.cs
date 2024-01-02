using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using uobj = UnityEngine.Object;

namespace UnityEngineEx
{
    public static class AsyncWorkTimer
    {
        private static int _CurFrame = 0;
        private static int _StartTick = 0;

        public const int MaxTickForWorkPerFrame = 50;

        public static void Reset()
        {
            _StartTick = Environment.TickCount;
        }

        public static bool Check()
        {
            var frame = Time.frameCount;
            if (frame == _CurFrame)
            {
                var tick = Environment.TickCount;
                if (tick - _StartTick >= MaxTickForWorkPerFrame)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                _CurFrame = frame;
                _StartTick = Environment.TickCount;
                return false;
            }
        }

        public static IEnumerator CheckAsync()
        {
            while (Check()) yield return null;
        }
    }
}