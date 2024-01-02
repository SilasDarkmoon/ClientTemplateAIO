using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
using UnityEngine;

using Object = UnityEngine.Object;
#endif

namespace UnityEngineEx
{
    public static class WeakRefExtensions
    {
        public static T GetWeakReference<T>(this System.WeakReference wr)
        {
            if (wr != null)
            {
                try
                {
                    if (wr.IsAlive)
                    {
                        var obj = wr.Target;
                        if (obj is T)
                        {
                            return (T)obj;
                        }
                    }
                }
                catch { }
            }
            return default(T);
        }
    }
}