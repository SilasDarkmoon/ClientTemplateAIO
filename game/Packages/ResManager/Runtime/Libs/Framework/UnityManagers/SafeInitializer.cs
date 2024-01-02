namespace UnityEngineEx
{
    using System;
    using System.Collections.Generic;

    public static class SafeInitializerUtils
    {
#if UNITY_EDITOR
        public static bool IsInitializingInUnityCtor
        {
            get
            {
                try
                {
                    var _ = UnityEngine.Application.GetStackTraceLogType(UnityEngine.LogType.Log);
                    return false;
                }
                catch (UnityEngine.UnityException e)
                {
                    return true;
                }
            }
        }
        internal static List<System.Reflection.ConstructorInfo> DelayedInitializers;

        public static void DoDelayedInitialize()
        {
            if (IsInitializingInUnityCtor) return;
            var initializers = DelayedInitializers;
            if (initializers != null)
            {
                DelayedInitializers = null;
                for (int i = 0; i < initializers.Count; ++i)
                {
                    var init = initializers[i];
                    try
                    {
                        init.Invoke(null, null);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogException(e);
                    }
                }
            }
        }

        [UnityEditor.InitializeOnLoad]
        internal class UnityInitializer
        {
            static UnityInitializer()
            {
                DoDelayedInitialize();
            }
        }

        public static bool IsInitializingInInitializeOnLoadAttribute
        {
            get
            {
                var stacktrace = Environment.StackTrace;
                return stacktrace.Contains("UnityEditor.EditorAssemblies.ProcessInitializeOnLoadAttributes");
            }
        }
#endif
        internal static bool CheckShouldDelay(int depth)
        {
#if UNITY_EDITOR
            if (!UnityObjectExtensions.CurrentThreadIsMainThread())
            {
                return false;
            }
            if (IsInitializingInUnityCtor)
            {
                var stackframe = new System.Diagnostics.StackTrace(1 + depth, false).GetFrame(0);
                var cctor = stackframe.GetMethod() as System.Reflection.ConstructorInfo;
                if (DelayedInitializers == null)
                {
                    DelayedInitializers = new List<System.Reflection.ConstructorInfo>();
                    EditorBridge.OnDelayedCallOnce += DoDelayedInitialize;
                }
                DelayedInitializers.Add(cctor);
                return true;
            }
            else
            {
                return false;
            }
#else
            return false;
#endif
        }

        public static bool CheckShouldDelay()
        {
            return CheckShouldDelay(1);
        }
    }

    //public struct SafeInitializer : IDisposable
    //{
    //    public void Dispose()
    //    {
    //        SafeInitializerUtils.CheckShouldDelay(1);
    //    }
    //}
}