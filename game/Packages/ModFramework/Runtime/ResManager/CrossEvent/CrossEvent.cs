using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UnityEngineEx
{
    public static class CrossEvent
    {
        // token: 0 means params passed to me; 1 means my returns; 2 means params for the next event I triggered.
        public const int TOKEN_ARGS = 0;
        public const int TOKEN_RETS = 1;
        public const int TOKEN_CALL = 2;
        private const int TOKEN_PERSISTENT_COUNT = 3;

        // types
        public const int PARAM_TYPE_NULL = 0;
        public const int PARAM_TYPE_BOOL = 1;
        public const int PARAM_TYPE_NUMBER = 2;
        public const int PARAM_TYPE_STRING = 3;
        public const int PARAM_TYPE_LIST = 4;
        public const int PARAM_TYPE_OBJECT = 5;

        #region NativeEventPlugin
#if (UNITY_WP8 || UNITY_METRO) && !UNITY_EDITOR
        // Each Handler
        public delegate void CEventHandler(string cate);
        // Reg & Unreg
        public delegate int Del_RegHandler(string cate, CEventHandler handler);
        public delegate void Del_UnregHandler(string cate, int refid);
        public delegate void Del_SetHandlerOrder(string cate, int refid, int order);
        // Call Other
        public delegate void Del_TrigEvent(string cate);
        // Get Value
        public delegate int Del_GetValType();
        public delegate bool Del_GetValBool();
        public delegate double Del_GetValNum();
        public delegate IntPtr Del_GetValPtr();
        public delegate string Del_GetValStr();
        public delegate int Del_GetValStrTo(IntPtr pstr);
        // Set Value
        public delegate void Del_SetValBool(bool val);
        public delegate void Del_SetValNum(double num);
        public delegate void Del_SetValPtr(IntPtr ptr);
        public delegate void Del_SetValStr(string str);
        public delegate void Del_UnsetVal();
        // Get Params
        public delegate int Del_GetParamCount(int token);
        public delegate void Del_SetParamCount(int token, int cnt);
        public delegate void Del_GetParam(int token, int index);
        public delegate void Del_SetParam(int token, int index);
        public delegate void Del_GetParamName(int token, int index);
        public delegate void Del_SetParamName(int token, int index);
        // Global Val
        public delegate void Del_GetGlobal(string name);
        public delegate void Del_SetGlobal(string name);
        // Dict & List
        public delegate int Del_NewList();
        public delegate void Del_GetValListTo(int list);
        public delegate void Del_SetValList(int list);

        public delegate void del_cevent_init
            (Del_RegHandler func_RegHandler
            , Del_UnregHandler func_UnregHandler
            , Del_SetHandlerOrder func_SetHandlerOrder
            , Del_TrigEvent func_TrigEvent
            , Del_GetValType func_GetValType
            , Del_GetValBool func_GetValBool
            , Del_GetValNum func_GetValNum
            , Del_GetValPtr func_GetValPtr
            , Del_GetValStr func_GetValStr
            , Del_SetValBool func_SetValBool
            , Del_SetValNum func_SetValNum
            , Del_SetValPtr func_SetValPtr
            , Del_SetValStr func_SetValStr
            , Del_UnsetVal func_UnsetVal
            , Del_GetParamCount func_GetParamCount
            , Del_SetParamCount func_SetParamCount
            , Del_GetParam func_GetParam
            , Del_SetParam func_SetParam
            , Del_GetParamName func_GetParamName
            , Del_SetParamName func_SetParamName
            , Del_GetGlobal func_GetGlobal
            , Del_SetGlobal func_SetGlobal
            , Del_NewList func_NewList
            , Del_GetValListTo func_GetValListTo
            , Del_SetValList func_SetValList
            );
        public static del_cevent_init func_cevent_init;
        // Init Func to push the global funcs from C# to C -- This is the whole cross-event-plugin's entrance.
        // In C plugin code, after receiving these funcs, we can do further init in this call, i.e. load the distribute-plugin
        public static void cevent_init
            (Del_RegHandler func_RegHandler
            , Del_UnregHandler func_UnregHandler
            , Del_SetHandlerOrder func_SetHandlerOrder
            , Del_TrigEvent func_TrigEvent
            , Del_GetValType func_GetValType
            , Del_GetValBool func_GetValBool
            , Del_GetValNum func_GetValNum
            , Del_GetValPtr func_GetValPtr
            , Del_GetValStr func_GetValStr
            , Del_GetValStrTo func_GetValStrTo
            , Del_SetValBool func_SetValBool
            , Del_SetValNum func_SetValNum
            , Del_SetValPtr func_SetValPtr
            , Del_SetValStr func_SetValStr
            , Del_UnsetVal func_UnsetVal
            , Del_GetParamCount func_GetParamCount
            , Del_SetParamCount func_SetParamCount
            , Del_GetParam func_GetParam
            , Del_SetParam func_SetParam
            , Del_GetParamName func_GetParamName
            , Del_SetParamName func_SetParamName
            , Del_GetGlobal func_GetGlobal
            , Del_SetGlobal func_SetGlobal
            , Del_NewList func_NewList
            , Del_GetValListTo func_GetValListTo
            , Del_SetValList func_SetValList
            )
        {
            if (func_cevent_init != null)
            {
                func_cevent_init
                    (func_RegHandler
                    , func_UnregHandler
                    , func_SetHandlerOrder
                    , func_TrigEvent
                    , func_GetValType
                    , func_GetValBool
                    , func_GetValNum
                    , func_GetValPtr
                    , func_GetValStr
                    , func_SetValBool
                    , func_SetValNum
                    , func_SetValPtr
                    , func_SetValStr
                    , func_UnsetVal
                    , func_GetParamCount
                    , func_SetParamCount
                    , func_GetParam
                    , func_SetParam
                    , func_GetParamName
                    , func_SetParamName
                    , func_GetGlobal
                    , func_SetGlobal
                    , func_NewList
                    , func_GetValListTo
                    , func_SetValList
                    );
            }
        }
#elif UNITY_IOS && !UNITY_EDITOR
        // Each Handler
        public delegate void CEventHandler(string cate);
        // Reg & Unreg
        public delegate int Del_RegHandler(string cate, CEventHandler handler);
        public delegate void Del_UnregHandler(string cate, int refid);
        public delegate void Del_SetHandlerOrder(string cate, int refid, int order);
        // Call Other
        public delegate void Del_TrigEvent(string cate);
        // Get Value
        public delegate int Del_GetValType();
        public delegate bool Del_GetValBool();
        public delegate double Del_GetValNum();
        public delegate IntPtr Del_GetValPtr();
        public delegate string Del_GetValStr();
        public delegate int Del_GetValStrTo(IntPtr pstr);
        // Set Value
        public delegate void Del_SetValBool(bool val);
        public delegate void Del_SetValNum(double num);
        public delegate void Del_SetValPtr(IntPtr ptr);
        public delegate void Del_SetValStr(string str);
        public delegate void Del_UnsetVal();
        // Get Params
        public delegate int Del_GetParamCount(int token);
        public delegate void Del_SetParamCount(int token, int cnt);
        public delegate void Del_GetParam(int token, int index);
        public delegate void Del_SetParam(int token, int index);
        public delegate void Del_GetParamName(int token, int index);
        public delegate void Del_SetParamName(int token, int index);
        // Global Val
        public delegate void Del_GetGlobal(string name);
        public delegate void Del_SetGlobal(string name);
        // Dict & List
        public delegate int Del_NewList();
        public delegate void Del_GetValListTo(int list);
        public delegate void Del_SetValList(int list);

        // Wrapper for AOT
        public delegate int Del_RegHandlerRaw(IntPtr czcate, IntPtr pfunc);
        public delegate void Del_UnregHandlerRaw(IntPtr czcate, int refid);
        public delegate void Del_SetHandlerOrderRaw(IntPtr czcate, int refid, int order);
        public delegate void Del_TrigEventRaw(IntPtr czcate);
        public delegate void Del_SetValStrRaw(IntPtr czstr);
        public delegate void Del_GetGlobalRaw(IntPtr czname);
        public delegate void Del_SetGlobalRaw(IntPtr czname);

        internal static readonly Del_RegHandlerRaw Func_RegHandlerRaw = new Del_RegHandlerRaw(RegHandlerRaw);
        internal static readonly Del_UnregHandlerRaw Func_UnregHandlerRaw = new Del_UnregHandlerRaw(UnregHandlerRaw);
        internal static readonly Del_SetHandlerOrderRaw Func_SetHandlerOrderRaw = new Del_SetHandlerOrderRaw(SetHandlerOrderRaw);
        internal static readonly Del_TrigEventRaw Func_TrigEventRaw = new Del_TrigEventRaw(TrigEventRaw);
        internal static readonly Del_SetValStrRaw Func_SetValStrRaw = new Del_SetValStrRaw(SetValStrRaw);
        internal static readonly Del_GetGlobalRaw Func_GetGlobalRaw = new Del_GetGlobalRaw(GetGlobalRaw);
        internal static readonly Del_SetGlobalRaw Func_SetGlobalRaw = new Del_SetGlobalRaw(SetGlobalRaw);

        [AOT.MonoPInvokeCallback(typeof(Del_RegHandlerRaw))]
        public static int RegHandlerRaw(IntPtr czcate, IntPtr pfunc)
        {
            return RegHandler(Marshal.PtrToStringAnsi(czcate), (cate) => cevent_callhandler(cate, pfunc));
        }
        [AOT.MonoPInvokeCallback(typeof(Del_UnregHandlerRaw))]
        public static void UnregHandlerRaw(IntPtr czcate, int refid)
        {
            UnregHandler(Marshal.PtrToStringAnsi(czcate), refid);
        }
        [AOT.MonoPInvokeCallback(typeof(Del_SetHandlerOrderRaw))]
        public static void SetHandlerOrderRaw(IntPtr czcate, int refid, int order)
        {
            SetHandlerOrder(Marshal.PtrToStringAnsi(czcate), refid, order);
        }
        [AOT.MonoPInvokeCallback(typeof(Del_TrigEventRaw))]
        public static void TrigEventRaw(IntPtr czcate)
        {
            TrigEvent(Marshal.PtrToStringAnsi(czcate));
        }
        [AOT.MonoPInvokeCallback(typeof(Del_SetValStrRaw))]
        public static void SetValStrRaw(IntPtr czstr)
        {
            if (czstr == IntPtr.Zero)
            {
                SetValStr(null);
            }
            else
            {
                List<byte> bytes = new List<byte>();
                try
                {
                    int off = -1;
                    while (true)
                    {
                        byte b = Marshal.ReadByte(czstr, ++off);
                        if (b == 0)
                        {
                            break;
                        }
                        else
                        {
                            bytes.Add(b);
                        }
                    }
                }
                catch { }
                SetValStr(System.Text.Encoding.UTF8.GetString(bytes.ToArray()));
            }
        }
        [AOT.MonoPInvokeCallback(typeof(Del_GetGlobalRaw))]
        public static void GetGlobalRaw(IntPtr czname)
        {
            GetGlobal(Marshal.PtrToStringAnsi(czname));
        }
        [AOT.MonoPInvokeCallback(typeof(Del_SetGlobalRaw))]
        public static void SetGlobalRaw(IntPtr czname)
        {
            SetGlobal(Marshal.PtrToStringAnsi(czname));
        }

#if DUMMY_NATIVE_EVENTS || !MOD_NATIVECROSSEVENT
        public static void cevent_callhandler(string cate, IntPtr pfunc) { }
        public static void cevent_init(
            Del_RegHandlerRaw func_RegHandler
            , Del_UnregHandlerRaw func_UnregHandler
            , Del_SetHandlerOrderRaw func_SetHandlerOrder
            , Del_TrigEventRaw func_TrigEvent
            , Del_GetValType func_GetValType
            , Del_GetValBool func_GetValBool
            , Del_GetValNum func_GetValNum
            , Del_GetValPtr func_GetValPtr
            , Del_GetValStrTo func_GetValStrTo
            , Del_SetValBool func_SetValBool
            , Del_SetValNum func_SetValNum
            , Del_SetValPtr func_SetValPtr
            , Del_SetValStrRaw func_SetValStr
            , Del_UnsetVal func_UnsetVal
            , Del_GetParamCount func_GetParamCount
            , Del_SetParamCount func_SetParamCount
            , Del_GetParam func_GetParam
            , Del_SetParam func_SetParam
            , Del_GetParamName func_GetParamName
            , Del_SetParamName func_SetParamName
            , Del_GetGlobalRaw func_GetGlobal
            , Del_SetGlobalRaw func_SetGlobal
            , Del_NewList func_NewList
            , Del_GetValListTo func_GetValListTo
            , Del_SetValList func_SetValList
            ) { }
#else
        [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
        public static extern void cevent_callhandler(string cate, IntPtr pfunc);

        [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
        // Init Func to push the global funcs from C# to C -- This is the whole cross-event-plugin's entrance.
        // In C plugin code, after receiving these funcs, we can do further init in this call, i.e. load the distribute-plugin
        public static extern void cevent_init(
            Del_RegHandlerRaw func_RegHandler
            , Del_UnregHandlerRaw func_UnregHandler
            , Del_SetHandlerOrderRaw func_SetHandlerOrder
            , Del_TrigEventRaw func_TrigEvent
            , Del_GetValType func_GetValType
            , Del_GetValBool func_GetValBool
            , Del_GetValNum func_GetValNum
            , Del_GetValPtr func_GetValPtr
            , Del_GetValStrTo func_GetValStrTo
            , Del_SetValBool func_SetValBool
            , Del_SetValNum func_SetValNum
            , Del_SetValPtr func_SetValPtr
            , Del_SetValStrRaw func_SetValStr
            , Del_UnsetVal func_UnsetVal
            , Del_GetParamCount func_GetParamCount
            , Del_SetParamCount func_SetParamCount
            , Del_GetParam func_GetParam
            , Del_SetParam func_SetParam
            , Del_GetParamName func_GetParamName
            , Del_SetParamName func_SetParamName
            , Del_GetGlobalRaw func_GetGlobal
            , Del_SetGlobalRaw func_SetGlobal
            , Del_NewList func_NewList
            , Del_GetValListTo func_GetValListTo
            , Del_SetValList func_SetValList
            );
#endif

        public static void cevent_init(
            Del_RegHandler func_RegHandler
            , Del_UnregHandler func_UnregHandler
            , Del_SetHandlerOrder func_SetHandlerOrder
            , Del_TrigEvent func_TrigEvent
            , Del_GetValType func_GetValType
            , Del_GetValBool func_GetValBool
            , Del_GetValNum func_GetValNum
            , Del_GetValPtr func_GetValPtr
            , Del_GetValStr func_GetValStr
            , Del_GetValStrTo func_GetValStrTo
            , Del_SetValBool func_SetValBool
            , Del_SetValNum func_SetValNum
            , Del_SetValPtr func_SetValPtr
            , Del_SetValStr func_SetValStr
            , Del_UnsetVal func_UnsetVal
            , Del_GetParamCount func_GetParamCount
            , Del_SetParamCount func_SetParamCount
            , Del_GetParam func_GetParam
            , Del_SetParam func_SetParam
            , Del_GetParamName func_GetParamName
            , Del_SetParamName func_SetParamName
            , Del_GetGlobal func_GetGlobal
            , Del_SetGlobal func_SetGlobal
            , Del_NewList func_NewList
            , Del_GetValListTo func_GetValListTo
            , Del_SetValList func_SetValList
            )
        {
            try
            {
                cevent_init(
                    Func_RegHandlerRaw
                    , Func_UnregHandlerRaw
                    , Func_SetHandlerOrderRaw
                    , Func_TrigEventRaw
                    , func_GetValType
                    , func_GetValBool
                    , func_GetValNum
                    , func_GetValPtr
                    , func_GetValStrTo
                    , func_SetValBool
                    , func_SetValNum
                    , func_SetValPtr
                    , Func_SetValStrRaw
                    , func_UnsetVal
                    , func_GetParamCount
                    , func_SetParamCount
                    , func_GetParam
                    , func_SetParam
                    , func_GetParamName
                    , func_SetParamName
                    , Func_GetGlobalRaw
                    , Func_SetGlobalRaw
                    , func_NewList
                    , func_GetValListTo
                    , func_SetValList
                    );
            }
            catch (Exception e)
            {
                PlatDependant.LogError(e);
            }
        }
#elif UNITY_ANDROID && !UNITY_EDITOR
        // Each Handler
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void CEventHandler(string cate);
        // Reg & Unreg
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int Del_RegHandler(string cate, CEventHandler handler);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Del_UnregHandler(string cate, int refid);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Del_SetHandlerOrder(string cate, int refid, int order);
        // Call Other
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Del_TrigEvent(string cate);
        // Get Value
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int Del_GetValType();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool Del_GetValBool();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double Del_GetValNum();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr Del_GetValPtr();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate string Del_GetValStr();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int Del_GetValStrTo(IntPtr pstr);
        // Set Value
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Del_SetValBool(bool val);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Del_SetValNum(double num);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Del_SetValPtr(IntPtr ptr);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Del_SetValStr(string str);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Del_UnsetVal();
        // Get Params
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int Del_GetParamCount(int token);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Del_SetParamCount(int token, int cnt);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Del_GetParam(int token, int index);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Del_SetParam(int token, int index);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Del_GetParamName(int token, int index);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Del_SetParamName(int token, int index);
        // Global Val
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Del_GetGlobal(string name);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Del_SetGlobal(string name);
        // Dict & List
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int Del_NewList();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Del_GetValListTo(int list);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Del_SetValList(int list);

        // Functions for Java Plugin
        internal class JavaEventHandler : ICEventHandler
        {
            internal IntPtr _Runnable;
            public JavaEventHandler(IntPtr runnable)
            {
                _Runnable = runnable;
            }

            public void Call(string cate)
            {
                if (_Runnable != IntPtr.Zero)
                {
                    try
                    {
                        cevent_calljava(_Runnable);
                    }
                    catch (Exception e)
                    {
                        PlatDependant.LogError(e);
                    }
                }
            }

            public void Dispose()
            {
                if (_Runnable != IntPtr.Zero)
                {
                    cevent_releasejava(_Runnable);
                    _Runnable = IntPtr.Zero;
                }
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int Del_RegJavaHandler(string cate, IntPtr runnable);

        internal static readonly Del_RegJavaHandler Func_RegJavaHandler = new Del_RegJavaHandler(RegJavaHandler);

        [AOT.MonoPInvokeCallback(typeof(Del_RegJavaHandler))]
        public static int RegJavaHandler(string cate, IntPtr runnable)
        {
            return RegHandler(cate, new JavaEventHandler(runnable));
        }

#if DUMMY_NATIVE_EVENTS || !MOD_NATIVECROSSEVENT
        public static void cevent_calljava(IntPtr pRunnable) { }
        public static void cevent_releasejava(IntPtr pObj) { }
        public static void cevent_init(
            string libPath
            , Del_RegHandler func_RegHandler
            , Del_RegJavaHandler func_RegJavaHandler
            , Del_UnregHandler func_UnregHandler
            , Del_SetHandlerOrder func_SetHandlerOrder
            , Del_TrigEvent func_TrigEvent
            , Del_GetValType func_GetValType
            , Del_GetValBool func_GetValBool
            , Del_GetValNum func_GetValNum
            , Del_GetValPtr func_GetValPtr
            , Del_GetValStrTo func_GetValStrTo
            , Del_SetValBool func_SetValBool
            , Del_SetValNum func_SetValNum
            , Del_SetValPtr func_SetValPtr
            , Del_SetValStr func_SetValStr
            , Del_UnsetVal func_UnsetVal
            , Del_GetParamCount func_GetParamCount
            , Del_SetParamCount func_SetParamCount
            , Del_GetParam func_GetParam
            , Del_SetParam func_SetParam
            , Del_GetParamName func_GetParamName
            , Del_SetParamName func_SetParamName
            , Del_GetGlobal func_GetGlobal
            , Del_SetGlobal func_SetGlobal
            , Del_NewList func_NewList
            , Del_GetValListTo func_GetValListTo
            , Del_SetValList func_SetValList
            ) { }
#else
        [DllImport("EventPlugin", CallingConvention = CallingConvention.Cdecl)]
        public static extern void cevent_calljava(IntPtr pRunnable);

        [DllImport("EventPlugin", CallingConvention = CallingConvention.Cdecl)]
        public static extern void cevent_releasejava(IntPtr pObj);

        [DllImport("EventPlugin", CallingConvention = CallingConvention.Cdecl)]
        // Init Func to push the global funcs from C# to C -- This is the whole cross-event-plugin's entrance.
        // In C plugin code, after receiving these funcs, we can do further init in this call, i.e. load the distribute-plugin
        public static extern void cevent_init(
            string libPath
            , Del_RegHandler func_RegHandler
            , Del_RegJavaHandler func_RegJavaHandler
            , Del_UnregHandler func_UnregHandler
            , Del_SetHandlerOrder func_SetHandlerOrder
            , Del_TrigEvent func_TrigEvent
            , Del_GetValType func_GetValType
            , Del_GetValBool func_GetValBool
            , Del_GetValNum func_GetValNum
            , Del_GetValPtr func_GetValPtr
            , Del_GetValStrTo func_GetValStrTo
            , Del_SetValBool func_SetValBool
            , Del_SetValNum func_SetValNum
            , Del_SetValPtr func_SetValPtr
            , Del_SetValStr func_SetValStr
            , Del_UnsetVal func_UnsetVal
            , Del_GetParamCount func_GetParamCount
            , Del_SetParamCount func_SetParamCount
            , Del_GetParam func_GetParam
            , Del_SetParam func_SetParam
            , Del_GetParamName func_GetParamName
            , Del_SetParamName func_SetParamName
            , Del_GetGlobal func_GetGlobal
            , Del_SetGlobal func_SetGlobal
            , Del_NewList func_NewList
            , Del_GetValListTo func_GetValListTo
            , Del_SetValList func_SetValList
            );
#endif

        public static void cevent_init(
            Del_RegHandler func_RegHandler
            , Del_UnregHandler func_UnregHandler
            , Del_SetHandlerOrder func_SetHandlerOrder
            , Del_TrigEvent func_TrigEvent
            , Del_GetValType func_GetValType
            , Del_GetValBool func_GetValBool
            , Del_GetValNum func_GetValNum
            , Del_GetValPtr func_GetValPtr
            , Del_GetValStr func_GetValStr
            , Del_GetValStrTo func_GetValStrTo
            , Del_SetValBool func_SetValBool
            , Del_SetValNum func_SetValNum
            , Del_SetValPtr func_SetValPtr
            , Del_SetValStr func_SetValStr
            , Del_UnsetVal func_UnsetVal
            , Del_GetParamCount func_GetParamCount
            , Del_SetParamCount func_SetParamCount
            , Del_GetParam func_GetParam
            , Del_SetParam func_SetParam
            , Del_GetParamName func_GetParamName
            , Del_SetParamName func_SetParamName
            , Del_GetGlobal func_GetGlobal
            , Del_SetGlobal func_SetGlobal
            , Del_NewList func_NewList
            , Del_GetValListTo func_GetValListTo
            , Del_SetValList func_SetValList
            )
        {
            try
            {
                string appName = UnityEngine.Application.identifier;
                cevent_init(
                    appName
                    , func_RegHandler
                    , Func_RegJavaHandler
                    , func_UnregHandler
                    , func_SetHandlerOrder
                    , func_TrigEvent
                    , func_GetValType
                    , func_GetValBool
                    , func_GetValNum
                    , func_GetValPtr
                    , func_GetValStrTo
                    , func_SetValBool
                    , func_SetValNum
                    , func_SetValPtr
                    , func_SetValStr
                    , func_UnsetVal
                    , func_GetParamCount
                    , func_SetParamCount
                    , func_GetParam
                    , func_SetParam
                    , func_GetParamName
                    , func_SetParamName
                    , func_GetGlobal
                    , func_SetGlobal
                    , func_NewList
                    , func_GetValListTo
                    , func_SetValList
                    );
            }
            catch (Exception e)
            {
                PlatDependant.LogError(e);
            }
        }
#else
        // Each Handler
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void CEventHandler(string cate);
        // Reg & Unreg
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int Del_RegHandler(string cate, CEventHandler handler);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Del_UnregHandler(string cate, int refid);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Del_SetHandlerOrder(string cate, int refid, int order);
        // Call Other
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Del_TrigEvent(string cate);
        // Get Value
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int Del_GetValType();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool Del_GetValBool();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double Del_GetValNum();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr Del_GetValPtr();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate string Del_GetValStr();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int Del_GetValStrTo(IntPtr pstr);
        // Set Value
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Del_SetValBool(bool val);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Del_SetValNum(double num);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Del_SetValPtr(IntPtr ptr);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Del_SetValStr(string str);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Del_UnsetVal();
        // Get Params
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int Del_GetParamCount(int token);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Del_SetParamCount(int token, int cnt);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Del_GetParam(int token, int index);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Del_SetParam(int token, int index);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Del_GetParamName(int token, int index);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Del_SetParamName(int token, int index);
        // Global Val
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Del_GetGlobal(string name);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Del_SetGlobal(string name);
        // Dict & List
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int Del_NewList();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Del_GetValListTo(int list);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Del_SetValList(int list);


        // Init Func to push the global funcs from C# to C -- This is the whole cross-event-plugin's entrance.
        // In C plugin code, after receiving these funcs, we can do further init in this call, i.e. load the distribute-plugin
        public static void cevent_init(
            Del_RegHandler func_RegHandler
            , Del_UnregHandler func_UnregHandler
            , Del_SetHandlerOrder func_SetHandlerOrder
            , Del_TrigEvent func_TrigEvent
            , Del_GetValType func_GetValType
            , Del_GetValBool func_GetValBool
            , Del_GetValNum func_GetValNum
            , Del_GetValPtr func_GetValPtr
            , Del_GetValStr func_GetValStr
            , Del_GetValStrTo func_GetValStrTo
            , Del_SetValBool func_SetValBool
            , Del_SetValNum func_SetValNum
            , Del_SetValPtr func_SetValPtr
            , Del_SetValStr func_SetValStr
            , Del_UnsetVal func_UnsetVal
            , Del_GetParamCount func_GetParamCount
            , Del_SetParamCount func_SetParamCount
            , Del_GetParam func_GetParam
            , Del_SetParam func_SetParam
            , Del_GetParamName func_GetParamName
            , Del_SetParamName func_SetParamName
            , Del_GetGlobal func_GetGlobal
            , Del_SetGlobal func_SetGlobal
            , Del_NewList func_NewList
            , Del_GetValListTo func_GetValListTo
            , Del_SetValList func_SetValList
            )
        { }
#endif
        #endregion

        public interface ICrossEventEx
        {
            void Reset();
            void UnregHandler(string cate, int refid);
            void HandleEvent(string cate, int refid);
            object GetGlobal(string name);
            void SetGlobal(string name, object val);
        }
        public static readonly List<ICrossEventEx> CrossEventEx = new List<ICrossEventEx>();

        internal static readonly Del_RegHandler Func_RegHandler = new Del_RegHandler(RegHandler);
        internal static readonly Del_UnregHandler Func_UnregHandler = new Del_UnregHandler(UnregHandler);
        internal static readonly Del_SetHandlerOrder Func_SetHandlerOrder = new Del_SetHandlerOrder(SetHandlerOrder);
        internal static readonly Del_TrigEvent Func_TrigEvent = new Del_TrigEvent(TrigEvent);
        internal static readonly Del_GetValType Func_GetValType = new Del_GetValType(GetValType);
        internal static readonly Del_GetValBool Func_GetValBool = new Del_GetValBool(GetValBool);
        internal static readonly Del_GetValNum Func_GetValNum = new Del_GetValNum(GetValNum);
        internal static readonly Del_GetValPtr Func_GetValPtr = new Del_GetValPtr(GetValPtr);
        internal static readonly Del_GetValStr Func_GetValStr = new Del_GetValStr(GetValStr);
        internal static readonly Del_GetValStrTo Func_GetValStrTo = new Del_GetValStrTo(GetValStrTo);
        internal static readonly Del_SetValBool Func_SetValBool = new Del_SetValBool(SetValBool);
        internal static readonly Del_SetValNum Func_SetValNum = new Del_SetValNum(SetValNum);
        internal static readonly Del_SetValPtr Func_SetValPtr = new Del_SetValPtr(SetValPtr);
        internal static readonly Del_SetValStr Func_SetValStr = new Del_SetValStr(SetValStr);
        internal static readonly Del_UnsetVal Func_UnsetVal = new Del_UnsetVal(UnsetVal);
        internal static readonly Del_GetParamCount Func_GetParamCount = new Del_GetParamCount(GetParamCount);
        internal static readonly Del_SetParamCount Func_SetParamCount = new Del_SetParamCount(SetParamCount);
        internal static readonly Del_GetParam Func_GetParam = new Del_GetParam(GetParam);
        internal static readonly Del_SetParam Func_SetParam = new Del_SetParam(SetParam);
        internal static readonly Del_GetParamName Func_GetParamName = new Del_GetParamName(GetParamName);
        internal static readonly Del_SetParamName Func_SetParamName = new Del_SetParamName(SetParamName);
        internal static readonly Del_GetGlobal Func_GetGlobal = new Del_GetGlobal(GetGlobal);
        internal static readonly Del_SetGlobal Func_SetGlobal = new Del_SetGlobal(SetGlobal);
        internal static readonly Del_NewList Func_NewList = new Del_NewList(NewList);
        internal static readonly Del_GetValListTo Func_GetValListTo = new Del_GetValListTo(GetValListTo);
        internal static readonly Del_SetValList Func_SetValList = new Del_SetValList(SetValList);

        public interface ICEventHandler : IDisposable
        {
            void Call(string cate);
        }
        internal class NativeEventHandler : ICEventHandler
        {
            private CEventHandler _Raw;
            public NativeEventHandler(CEventHandler func)
            {
                _Raw = func;
            }
            public void Call(string cate)
            {
                if (_Raw != null)
                {
                    try
                    {
                        _Raw(cate);
                    }
                    catch (Exception e)
                    {
                        PlatDependant.LogError(e);
                    }
                }
            }
            public void Dispose()
            {
            }
        }
        internal struct EventHandlerRegEntry
        {
            public ICEventHandler Handler;
            public int Order;
        }

        private static bool _Inited = false;
        internal static Dictionary<string, List<EventHandlerRegEntry>> EventHandlers = new Dictionary<string, List<EventHandlerRegEntry>>();

        public static int RegHandler(string cate, ICEventHandler handler)
        {
            if (cate == null)
            {
                return 0;
            }
            if (handler != null)
            {
                if (cate.StartsWith("?"))
                {
                    if (cate == "?Run")
                    {
                        handler.Call("?Run");
                        handler.Dispose();
                        return 0;
                    }
                    else if (cate == "?RunInUnityThread")
                    {
                        UnityThreadDispatcher.RunInUnityThread(() =>
                        {
                            handler.Call("?RunInUnityThread");
                            handler.Dispose();
                        });
                        return 0;
                    }

                }
            }

            List<EventHandlerRegEntry> handlers;
            var allhandles = EventHandlers;
            if (!allhandles.TryGetValue(cate, out handlers))
            {
                handlers = new List<EventHandlerRegEntry>();
                allhandles[cate] = handlers;
            }

            handlers.Add(new EventHandlerRegEntry() { Handler = handler });
            var hindex = handlers.Count;
            return hindex;
        }

        // TODO: IsHandlerReged(XXX)
        public static int GetHandlerCount(string cate)
        {
            if (cate == null)
            {
                int count = 0;
                foreach (var handlers in EventHandlers)
                {
                    count += handlers.Value.Count;
                }
                return count;
            }
            else
            {
                List<EventHandlerRegEntry> handlers;
                var allhandles = EventHandlers;
                if (!allhandles.TryGetValue(cate, out handlers))
                {
                    return 0;
                }
                else
                {
                    return handlers.Count;
                }
            }
        }

        [AOT.MonoPInvokeCallback(typeof(Del_RegHandler))]
        public static int RegHandler(string cate, CEventHandler handler)
        {
            return RegHandler(cate, handler == null ? null : new NativeEventHandler(handler));
        }

        [AOT.MonoPInvokeCallback(typeof(Del_UnregHandler))]
        public static void UnregHandler(string cate, int refid)
        {
            if (cate == null)
            {
                return;
            }

            List<EventHandlerRegEntry> handlers;
            var allhandles = EventHandlers;
            if (!allhandles.TryGetValue(cate, out handlers))
            {
                handlers = new List<EventHandlerRegEntry>();
                allhandles[cate] = handlers;
            }

            if (refid <= 0)
            {
                foreach (var handler in handlers)
                {
                    if (handler.Handler != null)
                    {
                        handler.Handler.Dispose();
                    }
                }
                handlers.Clear();
            }
            else if (refid <= handlers.Count)
            {
                {
                    var handler = handlers[refid - 1];
                    if (handler.Handler != null)
                    {
                        handler.Handler.Dispose();
                    }
                }
                handlers.RemoveAt(refid - 1);
            }

            for (int i = 0; i < CrossEventEx.Count; ++i)
            {
                CrossEventEx[i].UnregHandler(cate, refid);
            }
        }

        [AOT.MonoPInvokeCallback(typeof(Del_SetHandlerOrder))]
        public static void SetHandlerOrder(string cate, int refid, int order)
        {
            if (refid > 0)
            {
                List<EventHandlerRegEntry> handlers;
                var allhandles = EventHandlers;
                if (allhandles.TryGetValue(cate, out handlers))
                {
                    if (refid <= handlers.Count)
                    {
                        var handler = handlers[refid - 1];
                        handler.Order = order;
                    }
                }
            }
        }

        [AOT.MonoPInvokeCallback(typeof(Del_TrigEvent))]
        public static void TrigEvent(string cate)
        {
            if (cate == null)
            {
                return;
            }

            List<EventHandlerRegEntry> handlers;
            var allhandles = EventHandlers;
            if (!allhandles.TryGetValue(cate, out handlers))
            {
                handlers = new List<EventHandlerRegEntry>();
                allhandles[cate] = handlers;
            }

            var context = CurrentContext;
            // clear temp tokens
            if (context._P.Count > TOKEN_PERSISTENT_COUNT)
            {
                context._P.RemoveRange(TOKEN_PERSISTENT_COUNT, context._P.Count - TOKEN_PERSISTENT_COUNT);
            }
            var contextnew = PushContext();
            contextnew._P[TOKEN_ARGS].AddRange(context._P[TOKEN_CALL]);

            int[] orders = new int[handlers.Count];
            for (int i = 0; i < orders.Length; ++i)
            {
                orders[i] = i;
            }
            Array.Sort(orders, (i1, i2) =>
            {
                var result = handlers[i1].Order - handlers[i2].Order;
                if (result == 0)
                {
                    result = i1 - i2;
                }
                return result;
            });

            for (int i = 0; i < orders.Length; ++i)
            {
                var index = orders[i];
                var handler = handlers[index];
                if (handler.Handler == null)
                {
                    for (int j = 0; j < CrossEventEx.Count; ++j)
                    {
                        CrossEventEx[j].HandleEvent(cate, index + 1);
                    }
                }
                else
                {
                    handler.Handler.Call(cate);
                }
            }

            context._P[TOKEN_CALL].Clear();
            context._P[TOKEN_CALL].AddRange(contextnew._P[TOKEN_RETS]);
            RemoveContext(contextnew);
        }

        [AOT.MonoPInvokeCallback(typeof(Del_GetValType))]
        public static int GetValType()
        {
            var val = ContextExchangeObj;
            if (val == null)
            {
                return PARAM_TYPE_NULL;
            }
            else if (val is string)
            {
                return PARAM_TYPE_STRING;
            }
            else if (val is bool)
            {
                return PARAM_TYPE_BOOL;
            }
            else if (val is List<EventParam>)
            {
                return PARAM_TYPE_LIST;
            }
            else if (val.IsObjIConvertible())
            {
                return PARAM_TYPE_NUMBER;
            }
            else
            {
                return PARAM_TYPE_OBJECT;
            }
        }

        [AOT.MonoPInvokeCallback(typeof(Del_GetValBool))]
        public static bool GetValBool()
        {
            var val = ContextExchangeObj;
            if (val is bool)
            {
                return (bool)val;
            }
            else if (val is UnityEngine.Object)
            {
                return (bool)(UnityEngine.Object)val;
            }
            else if (val.IsObjIConvertible())
            {
                try
                {
                    return Convert.ToBoolean(val);
                }
                catch (Exception e)
                {
                    PlatDependant.LogError(e);
                }
            }
            return false;
        }

        [AOT.MonoPInvokeCallback(typeof(Del_GetValNum))]
        public static double GetValNum()
        {
            var val = ContextExchangeObj;
            if (val is double)
            {
                return (double)val;
            }
            else if (val is IntPtr)
            {
                return (double)(long)(IntPtr)val;
            }
            //else if (val is UIntPtr)
            //{
            //    return (double)(ulong)(UIntPtr)val;
            //}
            else if (val.IsObjIConvertible())
            {
                try
                {
                    return Convert.ToDouble(val);
                }
                catch (Exception e)
                {
                    PlatDependant.LogError(e);
                }
            }
            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(Del_GetValPtr))]
        public static IntPtr GetValPtr()
        {
            var val = ContextExchangeObj;
            if (val is IntPtr)
            {
                return (IntPtr)val;
            }
            return IntPtr.Zero;
        }

        [AOT.MonoPInvokeCallback(typeof(Del_GetValStr))]
        public static string GetValStr()
        {
            var val = ContextExchangeObj;
            if (val != null)
            {
                return val.ToString();
            }
            return "";
        }

        [AOT.MonoPInvokeCallback(typeof(Del_GetValStrTo))]
        public static int GetValStrTo(IntPtr pstr)
        {
#if (UNITY_WP8 || UNITY_METRO) && !UNITY_EDITOR
            var enc = System.Text.Encoding.UTF8;
#elif UNITY_IOS && !UNITY_EDITOR
            var enc = System.Text.Encoding.UTF8;
#else
            var enc = System.Text.Encoding.Default;
#endif
            var str = GetValStr();
            if (pstr == IntPtr.Zero)
            {
                return enc.GetMaxByteCount(str.Length) + 1;
            }
            else
            {
                var bytes = new byte[enc.GetMaxByteCount(str.Length + 1)];
                var enclen = enc.GetBytes(str, 0, str.Length, bytes, 0);
                Marshal.Copy(bytes, 0, pstr, enclen);
                Marshal.WriteByte(pstr, enclen, 0);
                return bytes.Length + 1;
            }
        }

        [AOT.MonoPInvokeCallback(typeof(Del_SetValBool))]
        public static void SetValBool(bool val)
        {
            ContextExchangeObj = val;
        }

        [AOT.MonoPInvokeCallback(typeof(Del_SetValNum))]
        public static void SetValNum(double num)
        {
            ContextExchangeObj = num;
        }

        [AOT.MonoPInvokeCallback(typeof(Del_SetValPtr))]
        public static void SetValPtr(IntPtr ptr)
        {
            ContextExchangeObj = ptr;
        }

        [AOT.MonoPInvokeCallback(typeof(Del_SetValStr))]
        public static void SetValStr(string str)
        {
            ContextExchangeObj = str;
        }

        [AOT.MonoPInvokeCallback(typeof(Del_UnsetVal))]
        public static void UnsetVal()
        {
            ContextExchangeObj = null;
        }

        [AOT.MonoPInvokeCallback(typeof(Del_GetParamCount))]
        public static int GetParamCount(int token)
        {
            int cnt = 0;
            var context = CurrentContext;
            if (context != null)
            {
                if (token >= 0 && token < context._P.Count)
                {
                    var p = context._P[token];
                    cnt = p.Count;
                }
            }
            return cnt;
        }

        [AOT.MonoPInvokeCallback(typeof(Del_SetParamCount))]
        public static void SetParamCount(int token, int cnt)
        {
            var context = CurrentContext;
            if (context != null)
            {
                if (token >= 0 && token < context._P.Count)
                {
                    var p = context._P[token];
                    if (cnt > p.Count)
                    {
                        for (int i = p.Count; i < cnt; ++i)
                        {
                            p.Add(new EventParam());
                        }
                    }
                    else if (cnt < p.Count)
                    {
                        p.RemoveRange(cnt, p.Count - cnt);
                    }
                }
            }
        }

        [AOT.MonoPInvokeCallback(typeof(Del_GetParam))]
        public static void GetParam(int token, int index)
        {
            ContextExchangeObj = null;
            var context = CurrentContext;
            if (context != null)
            {
                if (token >= 0 && token < context._P.Count)
                {
                    var p = context._P[token];
                    if (index >= 0 && index < p.Count)
                    {
                        ContextExchangeObj = p[index].Value;
                    }
                }
            }
        }

        [AOT.MonoPInvokeCallback(typeof(Del_SetParam))]
        public static void SetParam(int token, int index)
        {
            var context = CurrentContext;
            if (context != null)
            {
                if (token >= 0 && token < context._P.Count)
                {
                    var p = context._P[token];
                    if (index >= 0)
                    {
                        if (index >= p.Count)
                        {
                            SetParamCount(token, index + 1);
                        }
                        var param = p[index];
                        param.Value = ContextExchangeObj;
                        p[index] = param;
                    }
                }
            }
        }

        [AOT.MonoPInvokeCallback(typeof(Del_GetParamName))]
        public static void GetParamName(int token, int index)
        {
            ContextExchangeObj = null;
            var context = CurrentContext;
            if (context != null)
            {
                if (token >= 0 && token < context._P.Count)
                {
                    var p = context._P[token];
                    if (index >= 0 && index < p.Count)
                    {
                        ContextExchangeObj = p[index].Name;
                    }
                }
            }
        }

        [AOT.MonoPInvokeCallback(typeof(Del_SetParamName))]
        public static void SetParamName(int token, int index)
        {
            var context = CurrentContext;
            if (context != null)
            {
                if (token >= 0 && token < context._P.Count)
                {
                    var p = context._P[token];
                    if (index >= 0)
                    {
                        if (index >= p.Count)
                        {
                            SetParamCount(token, index + 1);
                        }
                        var param = p[index];
                        param.Name = GetValStr();
                        p[index] = param;
                    }
                }
            }
        }

        [AOT.MonoPInvokeCallback(typeof(Del_GetGlobal))]
        public static void GetGlobal(string name)
        {
            object exobj = null;
            for (int i = 0; i < CrossEventEx.Count; ++i)
            {
                exobj = CrossEventEx[i].GetGlobal(name) ?? exobj;
            }
            if (exobj != null)
            {
                lock (GlobalValues)
                {
                    GlobalValues[name] = exobj;
                }
            }
            else
            {
                lock (GlobalValues)
                {
                    GlobalValues.TryGetValue(name, out exobj);
                }
            }

            ContextExchangeObj = exobj;
        }

        [AOT.MonoPInvokeCallback(typeof(Del_SetGlobal))]
        public static void SetGlobal(string name)
        {
            if (name != null)
            {
                var exobj = ContextExchangeObj;
                lock (GlobalValues)
                {
                    GlobalValues[name] = exobj;
                }
                for (int i = 0; i < CrossEventEx.Count; ++i)
                {
                    CrossEventEx[i].SetGlobal(name, exobj);
                }
            }
        }

        [AOT.MonoPInvokeCallback(typeof(Del_NewList))]
        public static int NewList()
        {
            var context = CurrentContext;
            if (context != null)
            {
                int index = context._P.Count;
                context._P.Add(new List<EventParam>());
                return index;
            }
            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(Del_GetValListTo))]
        public static void GetValListTo(int listtoken)
        {
            var context = CurrentContext;
            if (context != null)
            {
                List<EventParam> list = context._O as List<EventParam>;
                if (list != null && listtoken >= 0 && listtoken < context._P.Count)
                {
                    var dest = context._P[listtoken];
                    dest.Clear();
                    dest.AddRange(list);
                }
            }
        }

        [AOT.MonoPInvokeCallback(typeof(Del_SetValList))]
        public static void SetValList(int listtoken)
        {
            var context = CurrentContext;
            if (context != null)
            {
                if (listtoken >= 0 && listtoken < context._P.Count)
                {
                    var src = context._P[listtoken];
                    context._O = src;
                }
            }
        }

        public static void InitCrossEvent()
        {
            if (_Inited)
            {
                return;
            }
            _Inited = true;
            cevent_init(
                Func_RegHandler
                , Func_UnregHandler
                , Func_SetHandlerOrder
                , Func_TrigEvent
                , Func_GetValType
                , Func_GetValBool
                , Func_GetValNum
                , Func_GetValPtr
                , Func_GetValStr
                , Func_GetValStrTo
                , Func_SetValBool
                , Func_SetValNum
                , Func_SetValPtr
                , Func_SetValStr
                , Func_UnsetVal
                , Func_GetParamCount
                , Func_SetParamCount
                , Func_GetParam
                , Func_SetParam
                , Func_GetParamName
                , Func_SetParamName
                , Func_GetGlobal
                , Func_SetGlobal
                , Func_NewList
                , Func_GetValListTo
                , Func_SetValList
                );
        }
        public static void ResetCrossEvent()
        {
            _Inited = false;
            foreach (var handlers in EventHandlers)
            {
                foreach (var handler in handlers.Value)
                {
                    if (handler.Handler != null)
                    {
                        handler.Handler.Dispose();
                    }
                }
            }
            EventHandlers.Clear();

            for (int i = 0; i < CrossEventEx.Count; ++i)
            {
                CrossEventEx[i].Reset();
            }
        }

        public static object[] TrigClrEvent(string cate, params object[] args)
        {
            var token = TOKEN_CALL;
            int argcnt = args == null ? 0 : args.Length;
            SetParamCount(token, argcnt);
            for (int i = 0; i < argcnt; ++i)
            {
                var val = args[i];
                if (val is List<EventParam>)
                {
                }
                else if (val is IDictionary)
                {
                    val = SetDict(val as IDictionary);
                }
                else if (val is IList)
                {
                    val = SetList(val as IList);
                }
                ContextExchangeObj = val;
                SetParam(token, i);
            }
            TrigEvent(cate);
            int rvcnt = GetParamCount(token);
            var rv = new object[rvcnt];
            for (int i = 0; i < rvcnt; ++i)
            {
                GetParam(token, i);
                var val = ContextExchangeObj;
                if (val is List<EventParam>)
                {
                    val = GetListOrDict(val as List<EventParam>);
                }
                rv[i] = val;
            }
            return rv;
        }
        public static T TrigClrEvent<T>(string cate, params object[] args)
        {
            var rv = TrigClrEvent(cate, args);
            if (rv == null || rv.Length < 1)
            {
                return default(T);
            }

            var obj = rv[0];
            return obj.Convert<T>();
            //if (obj is T)
            //{
            //    return (T)obj;
            //}
            //if (obj.IsObjIConvertible() && typeof(T).IsTypeIConvertible())
            //{
            //    try
            //    {
            //        return (T)Convert.ChangeType(obj, typeof(T));
            //    }
            //    catch (Exception e)
            //    {
            //        PlatDependant.LogError(e);
            //    }
            //}
            ////if (obj.IsObjIConvertible() && typeof(T) == typeof(IntPtr))
            ////{
            ////    try
            ////    {
            ////        return (T)(object)(IntPtr)Convert.ToInt64(obj);
            ////    }
            ////    catch (Exception e)
            ////    {
            ////        PlatDependant.LogError(e);
            ////    }
            ////}
            ////if (obj is IntPtr && typeof(T).IsTypeIConvertible())
            ////{
            ////    try
            ////    {
            ////        return (T)Convert.ChangeType((long)(IntPtr)obj, typeof(T));
            ////    }
            ////    catch (Exception e)
            ////    {
            ////        PlatDependant.LogError(e);
            ////    }
            ////}

            //return default(T);
        }
        public static bool IsList(List<EventParam> list)
        {
            if (list == null)
            {
                return false;
            }
            else
            {
                if (list.Count == 0)
                {
                    return true;
                }
                for (int i = 0; i < list.Count; ++i)
                {
                    if (list[i].Name == null)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        public static object GetListOrDict(List<EventParam> list)
        {
            if (IsList(list))
            {
                return GetList(list);
            }
            else
            {
                return GetDict(list);
            }
        }
        public static List<object> GetList(List<EventParam> list)
        {
            var rvs = new List<object>(list.Count);
            for (int i = 0; i < list.Count; ++i)
            {
                var val = list[i].Value;
                if (val is List<EventParam>)
                {
                    rvs.Add(GetListOrDict(val as List<EventParam>));
                }
                else
                {
                    rvs.Add(val);
                }
            }
            return rvs;
        }
        public static Dictionary<string, object> GetDict(List<EventParam> list)
        {
            var rvs = new Dictionary<string, object>();
            for (int i = 0; i < list.Count; ++i)
            {
                var par = list[i];
                if (par.Name != null)
                {
                    var val = par.Value;
                    if (val is List<EventParam>)
                    {
                        rvs[par.Name] = GetListOrDict(val as List<EventParam>);
                    }
                    else
                    {
                        rvs[par.Name] = val;
                    }
                }
            }
            return rvs;
        }
        public static List<EventParam> SetList(IList list)
        {
            var pars = new List<EventParam>();
            for (int i = 0; i < list.Count; ++i)
            {
                var val = list[i];
                if (val is List<EventParam>)
                {
                }
                else if (val is IDictionary)
                {
                    val = SetDict(val as IDictionary);
                }
                else if (val is IList)
                {
                    val = SetList(val as IList);
                }
                pars.Add(new EventParam() { Value = val });
            }
            return pars;
        }
        public static List<EventParam> SetDict(IDictionary dict)
        {
            var pars = new List<EventParam>();
            foreach (DictionaryEntry kvp in dict)
            {
                var key = kvp.Key as string;
                if (key != null)
                {
                    var val = kvp.Value;
                    if (val is List<EventParam>)
                    {
                    }
                    else if (val is IDictionary)
                    {
                        val = SetDict(val as IDictionary);
                    }
                    else if (val is IList)
                    {
                        val = SetList(val as IList);
                    }
                    pars.Add(new EventParam() { Value = val, Name = key });
                }
            }
            return pars;
        }

        public struct EventParam
        {
            public object Value;
            public string Name;
        }
        public class EventContext
        {
            public object _O;
            public List<List<EventParam>> _P = new List<List<EventParam>>();
        }
        public class IndexedStack<T>
        {
            public LinkedList<T> Stack = new LinkedList<T>();
            public Dictionary<T, LinkedListNode<T>> Index = new Dictionary<T, LinkedListNode<T>>();

            public void Push(T val)
            {
                if (!Index.ContainsKey(val))
                {
                    Index[val] = Stack.AddLast(val);
                }
            }
            public void Remove(T val)
            {
                LinkedListNode<T> node;
                if (Index.TryGetValue(val, out node))
                {
                    Index.Remove(val);
                    Stack.Remove(node);
                }
            }
            public int Count
            {
                get { return Stack.Count; }
            }
        }
        private static readonly ThreadLocalObj<IndexedStack<EventContext>> ContextStack = new ThreadLocalObj<IndexedStack<EventContext>>(() => new IndexedStack<EventContext>());
        public static EventContext CurrentContext
        {
            get
            {
                var stack = ContextStack.Value;
                if (stack.Count == 0)
                {
                    return PushContext();
                }
                else
                {
                    return stack.Stack.Last.Value;
                }
            }
        }
        public static EventContext PushContext()
        {
            var stack = ContextStack.Value;
            var context = new EventContext();
            context._P.Add(new List<EventParam>()); // TOKEN_ARGS
            context._P.Add(new List<EventParam>()); // TOKEN_RETS
            context._P.Add(new List<EventParam>()); // TOKEN_CALL
            stack.Push(context);
            return context;
        }
        public static void PopContext()
        {
            var stack = ContextStack.Value;
            if (stack.Count > 1)
            {
                var context = stack.Stack.Last.Value;
                stack.Stack.RemoveLast();
                stack.Index.Remove(context);
            }
        }
        public static void RemoveContext(EventContext context)
        {
            var stack = ContextStack.Value;
            if (stack.Count > 1)
            {
                stack.Remove(context);
            }
        }
        public static object ContextExchangeObj
        {
            get
            {
                var context = CurrentContext;
                if (context != null)
                {
                    return context._O;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                var context = CurrentContext;
                if (context != null)
                {
                    context._O = value;
                }
            }
        }
        private static readonly Dictionary<string, object> GlobalValues = new Dictionary<string, object>();

        public class RawEventData
        {
            public RawEventData() { }
            public RawEventData(object data)
            {
                Data = data;
            }
            public object Data;
        }
        public class RawEventData<T>
        {
            public RawEventData() { }
            public RawEventData(T data)
            {
                Data = data;
            }
            public T Data;
        }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnUnityStart()
        {
            ResManager.AddInitItem(new ResManager.ActionInitItem(ResManager.LifetimeOrders.CrossEvent, InitCrossEvent, null, ResetCrossEvent));
        }
    }
}