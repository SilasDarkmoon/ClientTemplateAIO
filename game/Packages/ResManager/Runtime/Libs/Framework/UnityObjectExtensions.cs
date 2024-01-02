using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;

namespace UnityEngineEx
{
    public static class UnityObjectExtensions
    {
        static UnityObjectExtensions()
        {
            _Func_CurrentThreadIsMainThread = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), typeof(Object).GetMethod("CurrentThreadIsMainThread", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic));
            _Func_IsPersistent = (Func<Object, bool>)Delegate.CreateDelegate(typeof(Func<Object, bool>), typeof(Object).GetMethod("IsPersistent", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic));
            _Func_DoesObjectWithInstanceIDExist = (Func<int, bool>)Delegate.CreateDelegate(typeof(Func<int, bool>), typeof(Object).GetMethod("DoesObjectWithInstanceIDExist", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic));
            _Func_FindObjectFromInstanceID = (Func<int, Object>)Delegate.CreateDelegate(typeof(Func<int, Object>), typeof(Object).GetMethod("FindObjectFromInstanceID", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic));
            _Func_ForceLoadFromInstanceID = (Func<int, Object>)Delegate.CreateDelegate(typeof(Func<int, Object>), typeof(Object).GetMethod("ForceLoadFromInstanceID", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic));
        }
        private readonly static Func<bool> _Func_CurrentThreadIsMainThread;
        private readonly static Func<Object, bool> _Func_IsPersistent;
        private readonly static Func<int, bool> _Func_DoesObjectWithInstanceIDExist;
        private readonly static Func<int, Object> _Func_FindObjectFromInstanceID;
        private readonly static Func<int, Object> _Func_ForceLoadFromInstanceID;

        public static bool CurrentThreadIsMainThread()
        {
            return _Func_CurrentThreadIsMainThread();
        }
        public static bool IsPersistent(this Object obj)
        {
            return _Func_IsPersistent(obj);
        }
        public static bool DoesObjectWithInstanceIDExist(int instanceID)
        {
            return _Func_DoesObjectWithInstanceIDExist(instanceID);
        }
        public static Object FindObjectFromInstanceID(int instanceID)
        {
            return _Func_FindObjectFromInstanceID(instanceID);
        }
        public static Object ForceLoadFromInstanceID(int instanceID)
        {
            return _Func_ForceLoadFromInstanceID(instanceID);
        }
    }
}