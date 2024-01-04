using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using LuaLib;
using UnityEngineEx;
using lua = LuaLib.LuaCoreLib;
using lual = LuaLib.LuaAuxLib;
using luae = LuaLib.LuaLibEx;

using Object = UnityEngine.Object;

[DataDictionaryComponentType(DataDictionaryComponentTypeAttribute.DataDictionaryComponentType.Main)]
public class LuaBehav : MonoBehaviour
#if COMPATIBLE_RESMANAGER_V1
    , ISerializationCallbackReceiver
#endif
{
    private class TypeHubPrecompiled_LuaBehav : LuaLib.LuaTypeHub.TypeHubCommonPrecompiled
    {
        public TypeHubPrecompiled_LuaBehav()
            : base(typeof(LuaBehav))
        {
        }

        public override IntPtr PushLua(IntPtr l, object val)
        {
            var h = base.PushLua(l, val);
            LuaObjCache.RegObj(l, val, -1, h);
            LuaBehav real = val as LuaBehav;
            if (real != null)
            {
                if (real.ShouldBindLua)
                {
                    real.BindLua(l, -1);
                }
            }
            return IntPtr.Zero;
        }
    }
    private static TypeHubPrecompiled_LuaBehav ___tp_LuaBehav;

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
#endif
    [UnityEngine.RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Init()
    {
        ___tp_LuaBehav = ___tp_LuaBehav ?? new TypeHubPrecompiled_LuaBehav();
    }

    #region Component Data
    public string InitLuaPath;
    public DataDictionary ExFields;
    #endregion
    [NonSerialized] protected internal BaseLua _Self = null;
    public BaseLua HostingUserData
    {
        get { return _Self; }
    }
    [NonSerialized] protected internal bool _LuaBinded = false;
    [NonSerialized] protected internal bool _Destroyed = false;

    [NonSerialized] protected internal bool _Awaken = false;
    [NonSerialized] protected internal static Dictionary<int, LuaBehav> _DestroyReg = new Dictionary<int, LuaBehav>();
    [NonSerialized] protected internal static int _DestroyRegNextIndex = 1;
    [NonSerialized] protected internal int _DestroyRegIndex = 0;
    protected internal static void CheckDestroyed()
    {
        for (int i = 1; i < _DestroyRegNextIndex; ++i)
        {
            if (_DestroyReg.ContainsKey(i))
            {
                var behav = _DestroyReg[i];
                if (object.ReferenceEquals(behav, null))
                {
                    RemoveDestroyRegIndex(i);
                }
                else if (behav == null)
                {
                    behav.OnDestroy();
                }
            }
        }
    }
    protected internal static void RemoveDestroyRegIndex(int index)
    {
        if (_DestroyReg.Remove(index))
        {
            if (index == _DestroyRegNextIndex - 1)
            {
                var max = 0;
                foreach (var kvp in _DestroyReg)
                {
                    if (kvp.Key > max)
                    {
                        max = kvp.Key;
                    }
                }
                _DestroyRegNextIndex = max + 1;
            }
        }
    }

    public bool ShouldBindLua
    {
        get
        {
            return !_LuaBinded && ReferenceEquals(_Self, null) && this != null && !_Destroyed;
        }
    }

    public void BindLua()
    {
#if UNITY_EDITOR
        if (!UnityEngine.Application.isPlaying)
        {
            return;
        }
#endif
        if (ShouldBindLua)
        {
            _LuaBinded = true;
            var l = GlobalLua.L.L;
            l.PushLua(this);
            BindLua(l, -1);
            l.pop(1);
        }
    }

    public void BindLua(IntPtr l, int index)
    {
        CheckDestroyed();
        _LuaBinded = true;
        _Self = BindBehav(l, this, index);
        if (!_Awaken && !(enabled && gameObject.activeInHierarchy))
        {
            if (_DestroyRegIndex <= 0)
            {
                _DestroyRegIndex = _DestroyRegNextIndex++;
                _DestroyReg[_DestroyRegIndex] = this;
            }
        }
    }

    public static void ExpandExFields(IntPtr l, LuaBehav behav, int index)
    {
        using (var lr = new LuaStateRecover(l))
        {
            l.pushvalue(index); // behav
            l.newtable(); // behav ex
            l.pushvalue(-1); // behav ex ex
            l.SetField(-3, "___ex"); // behav ex
            if (behav.ExFields != null)
            {
                foreach (var kvp in behav.ExFields)
                {
                    l.SetHierarchical(-1, kvp.Key, kvp.Value);
                }
            }
        }
    }
    public static void ExpandExFieldsToSelf(IntPtr l, LuaBehav behav, int index)
    {
        using (var lr = new LuaStateRecover(l))
        {
            l.pushvalue(index); // behav
            if (behav.ExFields != null)
            {
                foreach (var kvp in behav.ExFields)
                {
                    l.SetHierarchical(-1, kvp.Key, kvp.Value);
                }
            }
        }
    }

    public static BaseLua BindBehav(IntPtr l, LuaBehav behav, int index)
    {
#if ENABLE_PROFILER_LUA_DEEP
        using (var pcon = ProfilerContext.Create("LuaBehav - BindLua - ExpandExFields"))
#endif
        ExpandExFields(l, behav, index);
#if ENABLE_PROFILER_LUA_DEEP
        using (var pcon = ProfilerContext.Create("LuaBehav - BindLua - require and ctor"))
#endif
        using (var lr = new LuaStateRecover(l))
        {
            if (string.IsNullOrEmpty(behav.InitLuaPath))
            {
                l.pushvalue(index);
                var refid = l.refer();
                return new BaseLua(l, refid);
            }

            int oldtop = lr.Top;
            bool luaFileDone = false;
            l.pushcfunction(LuaHub.LuaFuncOnError);
            l.GetGlobal("require");
            l.PushString(behav.InitLuaPath);
            if (l.pcall(1, 1, -3) == 0)
            {
                if (l.gettop() > oldtop + 1 && l.istable(oldtop + 2))
                {
                    luaFileDone = true;
                }
                else
                {
                    DynamicHelper.LogError("Failed to init script by require " + behav.InitLuaPath + ". (Not a table.) Now Init it by file.");
                }
            }
            else
            {
                DynamicHelper.LogError(l.GetLua(-1));
                DynamicHelper.LogInfo("Failed to init script by require " + behav.InitLuaPath + ". Now Init it by file.");
            }
            if (!luaFileDone)
            {
                DynamicHelper.LogInfo("Init it by file. - Disabled");
                //string path = behav.InitLuaPath;
                //if (path.EndsWith(".lua"))
                //{
                //    path = path.Substring(0, path.Length - 4);
                //}
                //path = path.Replace('.', '/');
                //path = path.Replace('\\', '/');
                //if (!path.StartsWith("spt/"))
                //{
                //    path = "spt/" + path;
                //}
                //path += ".lua";
                //path = ResManager.UpdatePath + "/" + path;

                //l.settop(oldtop);
                //if (l.dofile(path) == 0)
                //{
                //    if (l.gettop() > oldtop && l.istable(oldtop + 1))
                //    {
                //        luaFileDone = true;
                //    }
                //    else
                //    {
                //        DynamicHelper.LogInfo("Failed to load script " + path + ". (Not a table.)");
                //    }
                //}
                //else
                //{
                //    DynamicHelper.LogInfo(l.GetLua(-1).UnwrapDynamic());
                //    DynamicHelper.LogInfo("Failed to load script " + path);
                //}
            }
            if (luaFileDone)
            {
                l.GetField(oldtop + 2, "___bind_ex_to_self");
                if (!l.isnoneornil(-1))
                {
                    bool bindex;
                    l.GetLua(-1, out bindex);
                    if (bindex)
                    {
                        ExpandExFieldsToSelf(l, behav, oldtop);
                    }
                }
                l.pop(1);

                l.GetField(oldtop + 2, "attach");
                if (l.isfunction(-1))
                {
                    l.pushvalue(oldtop);
                    if (l.pcall(1, 0, oldtop + 1) == 0)
                    {
                    }
                    else
                    {
                        DynamicHelper.LogError(l.GetLua(-1));
                    }
                    l.pushvalue(oldtop);
                    var refid = l.refer();
                    return new BaseLua(l, refid);
                }
            }
        }
        {
            l.pushvalue(index);
            var refid = l.refer();
            return new BaseLua(l, refid);
        }
    }

    public static void DisposeLuaBinding(BaseLua binding)
    {
        IntPtr l;
        int refid;
        if (!binding.IsClosed && (l = binding.L) != IntPtr.Zero && (refid = binding.Refid) != 0)
        {
            l.getref(refid);
            l.ForEach(-1, lstack =>
            {
                lstack.pushvalue(-2);
                lstack.pushnil();
                lstack.rawset(-5);
            });
            l.newtable();
            l.PushCommonEqFunc();
            l.RawSet(-2, LuaConst.LS_META_KEY_EQ);
            l.setmetatable(-2);
            l.pop(1);
        }
    }

    public static bool WillAutoDisposeLuaBinding = true;

    private int CallLuaFuncInternal(IntPtr l, int refid, string funcname, int oldtop)
    {
#if ENABLE_PROFILER && ENABLE_PROFILER_LUA
        using (var pcon = ProfilerContext.Create("{0}:{1}", InitLuaPath, funcname))
#endif
        {
#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
            {
                return lua.LUA_ERRERR;
            }
#endif
            l.checkstack(2);
            var pcnt = l.gettop() - oldtop;
            l.getref(refid); // args(*pcnt), this
            l.GetField(-1, funcname); // args(*pcnt), this, func
            if (l.isfunction(-1) || l.istable(-1) || l.IsUserData(-1))
            {
                l.insert(oldtop + 1); // func, args(*pcnt), this
                l.insert(oldtop + 2); // func, this, args(*pcnt)
                l.pushcfunction(LuaHub.LuaFuncOnError); // func, this, args(*pcnt), err
                l.insert(oldtop + 1); // err, func, this, args(*pcnt)
                var lrr = new LuaRunningStateRecorder(l);
                var code = l.pcall(pcnt + 1, lua.LUA_MULTRET, oldtop + 1); // err, rv(*x)
                lrr.Dispose();
                l.remove(oldtop + 1); // rv(*x)
                if (code != 0)
                {
                    DynamicHelper.LogError(l.GetLua(-1));
                }
                return code;
            }
            return lua.LUA_ERRERR;
        }
    }

    public void CallLuaFunc(string name)
    {
        if (!ReferenceEquals(_Self, null)
#if UNITY_EDITOR
            && !_Self.IsClosed
#endif
            )
        {
            var l = _Self._L;
            using (var lr = new LuaStateRecover(l))
            {
                var code = CallLuaFuncInternal(l, _Self.Refid, name, lr.Top);
            }
        }
    }
    public void CallLuaFunc<P0>(string name, P0 p0)
    {
        if (!ReferenceEquals(_Self, null)
#if UNITY_EDITOR
            && !_Self.IsClosed
#endif
            )
        {
            var l = _Self._L;
            using (var lr = new LuaStateRecover(l))
            {
                l.PushLua(p0);
                var code = CallLuaFuncInternal(l, _Self.Refid, name, lr.Top);
            }
        }
    }
    public void CallLuaFunc<P0, P1>(string name, P0 p0, P1 p1)
    {
        if (!ReferenceEquals(_Self, null)
#if UNITY_EDITOR
            && !_Self.IsClosed
#endif
            )
        {
            var l = _Self._L;
            using (var lr = new LuaStateRecover(l))
            {
                l.PushLua(p0);
                l.PushLua(p1);
                var code = CallLuaFuncInternal(l, _Self.Refid, name, lr.Top);
            }
        }
    }
    public void CallLuaFunc<P0, P1, P2>(string name, P0 p0, P1 p1, P2 p2)
    {
        if (!ReferenceEquals(_Self, null)
#if UNITY_EDITOR
            && !_Self.IsClosed
#endif
            )
        {
            var l = _Self._L;
            using (var lr = new LuaStateRecover(l))
            {
                l.PushLua(p0);
                l.PushLua(p1);
                l.PushLua(p2);
                var code = CallLuaFuncInternal(l, _Self.Refid, name, lr.Top);
            }
        }
    }
    public void CallLuaFunc<P0, P1, P2, P3>(string name, P0 p0, P1 p1, P2 p2, P3 p3)
    {
        if (!ReferenceEquals(_Self, null)
#if UNITY_EDITOR
            && !_Self.IsClosed
#endif
            )
        {
            var l = _Self._L;
            using (var lr = new LuaStateRecover(l))
            {
                l.PushLua(p0);
                l.PushLua(p1);
                l.PushLua(p2);
                l.PushLua(p3);
                var code = CallLuaFuncInternal(l, _Self.Refid, name, lr.Top);
            }
        }
    }
    public void CallLuaFunc<P0, P1, P2, P3, P4>(string name, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4)
    {
        if (!ReferenceEquals(_Self, null)
#if UNITY_EDITOR
            && !_Self.IsClosed
#endif
            )
        {
            var l = _Self._L;
            using (var lr = new LuaStateRecover(l))
            {
                l.PushLua(p0);
                l.PushLua(p1);
                l.PushLua(p2);
                l.PushLua(p3);
                l.PushLua(p4);
                var code = CallLuaFuncInternal(l, _Self.Refid, name, lr.Top);
            }
        }
    }
    public void CallLuaFunc<P0, P1, P2, P3, P4, P5>(string name, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5)
    {
        if (!ReferenceEquals(_Self, null)
#if UNITY_EDITOR
            && !_Self.IsClosed
#endif
            )
        {
            var l = _Self._L;
            using (var lr = new LuaStateRecover(l))
            {
                l.PushLua(p0);
                l.PushLua(p1);
                l.PushLua(p2);
                l.PushLua(p3);
                l.PushLua(p4);
                l.PushLua(p5);
                var code = CallLuaFuncInternal(l, _Self.Refid, name, lr.Top);
            }
        }
    }
    public bool CallLuaFunc<R>(string name, out R r)
    {
        if (!ReferenceEquals(_Self, null)
#if UNITY_EDITOR
            && !_Self.IsClosed
#endif
            )
        {
            var l = _Self._L;
            using (var lr = new LuaStateRecover(l))
            {
                var code = CallLuaFuncInternal(l, _Self.Refid, name, lr.Top);
                if (code == 0 && l.gettop() > lr.Top)
                {
                    R rv;
                    l.GetLua<R>(lr.Top + 1, out rv);
                    r = rv;
                    return true;
                }
            }
        }
        r = default(R);
        return false;
    }
    public bool CallLuaFunc<R, P0>(string name, out R r, P0 p0)
    {
        if (!ReferenceEquals(_Self, null)
#if UNITY_EDITOR
            && !_Self.IsClosed
#endif
            )
        {
            var l = _Self._L;
            using (var lr = new LuaStateRecover(l))
            {
                l.PushLua(p0);
                var code = CallLuaFuncInternal(l, _Self.Refid, name, lr.Top);
                if (code == 0 && l.gettop() > lr.Top)
                {
                    R rv;
                    l.GetLua<R>(lr.Top + 1, out rv);
                    r = rv;
                    return true;
                }
            }
        }
        r = default(R);
        return false;
    }
    public bool CallLuaFunc<R, P0, P1>(string name, out R r, P0 p0, P1 p1)
    {
        if (!ReferenceEquals(_Self, null)
#if UNITY_EDITOR
            && !_Self.IsClosed
#endif
            )
        {
            var l = _Self._L;
            using (var lr = new LuaStateRecover(l))
            {
                l.PushLua(p0);
                l.PushLua(p1);
                var code = CallLuaFuncInternal(l, _Self.Refid, name, lr.Top);
                if (code == 0 && l.gettop() > lr.Top)
                {
                    R rv;
                    l.GetLua<R>(lr.Top + 1, out rv);
                    r = rv;
                    return true;
                }
            }
        }
        r = default(R);
        return false;
    }
    public bool CallLuaFunc<R, P0, P1, P2>(string name, out R r, P0 p0, P1 p1, P2 p2)
    {
        if (!ReferenceEquals(_Self, null)
#if UNITY_EDITOR
            && !_Self.IsClosed
#endif
            )
        {
            var l = _Self._L;
            using (var lr = new LuaStateRecover(l))
            {
                l.PushLua(p0);
                l.PushLua(p1);
                l.PushLua(p2);
                var code = CallLuaFuncInternal(l, _Self.Refid, name, lr.Top);
                if (code == 0 && l.gettop() > lr.Top)
                {
                    R rv;
                    l.GetLua<R>(lr.Top + 1, out rv);
                    r = rv;
                    return true;
                }
            }
        }
        r = default(R);
        return false;
    }
    public bool CallLuaFunc<R, P0, P1, P2, P3>(string name, out R r, P0 p0, P1 p1, P2 p2, P3 p3)
    {
        if (!ReferenceEquals(_Self, null)
#if UNITY_EDITOR
            && !_Self.IsClosed
#endif
            )
        {
            var l = _Self._L;
            using (var lr = new LuaStateRecover(l))
            {
                l.PushLua(p0);
                l.PushLua(p1);
                l.PushLua(p2);
                l.PushLua(p3);
                var code = CallLuaFuncInternal(l, _Self.Refid, name, lr.Top);
                if (code == 0 && l.gettop() > lr.Top)
                {
                    R rv;
                    l.GetLua<R>(lr.Top + 1, out rv);
                    r = rv;
                    return true;
                }
            }
        }
        r = default(R);
        return false;
    }
    public bool CallLuaFunc<R, P0, P1, P2, P3, P4>(string name, out R r, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4)
    {
        if (!ReferenceEquals(_Self, null)
#if UNITY_EDITOR
            && !_Self.IsClosed
#endif
            )
        {
            var l = _Self._L;
            using (var lr = new LuaStateRecover(l))
            {
                l.PushLua(p0);
                l.PushLua(p1);
                l.PushLua(p2);
                l.PushLua(p3);
                l.PushLua(p4);
                var code = CallLuaFuncInternal(l, _Self.Refid, name, lr.Top);
                if (code == 0 && l.gettop() > lr.Top)
                {
                    R rv;
                    l.GetLua<R>(lr.Top + 1, out rv);
                    r = rv;
                    return true;
                }
            }
        }
        r = default(R);
        return false;
    }
    public bool CallLuaFunc<R, P0, P1, P2, P3, P4, P5>(string name, out R r, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5)
    {
        if (!ReferenceEquals(_Self, null)
#if UNITY_EDITOR
            && !_Self.IsClosed
#endif
            )
        {
            var l = _Self._L;
            using (var lr = new LuaStateRecover(l))
            {
                l.PushLua(p0);
                l.PushLua(p1);
                l.PushLua(p2);
                l.PushLua(p3);
                l.PushLua(p4);
                l.PushLua(p5);
                var code = CallLuaFuncInternal(l, _Self.Refid, name, lr.Top);
                if (code == 0 && l.gettop() > lr.Top)
                {
                    R rv;
                    l.GetLua<R>(lr.Top + 1, out rv);
                    r = rv;
                    return true;
                }
            }
        }
        r = default(R);
        return false;
    }
    public R CallLuaFunc<R>(string name)
    {
        R r;
        CallLuaFunc(name, out r);
        return r;
    }
    public R CallLuaFunc<R, P0>(string name, P0 p0)
    {
        R r;
        CallLuaFunc(name, out r, p0);
        return r;
    }
    public R CallLuaFunc<R, P0, P1>(string name, P0 p0, P1 p1)
    {
        R r;
        CallLuaFunc(name, out r, p0, p1);
        return r;
    }
    public R CallLuaFunc<R, P0, P1, P2>(string name, P0 p0, P1 p1, P2 p2)
    {
        R r;
        CallLuaFunc(name, out r, p0, p1, p2);
        return r;
    }
    public R CallLuaFunc<R, P0, P1, P2, P3>(string name, P0 p0, P1 p1, P2 p2, P3 p3)
    {
        R r;
        CallLuaFunc(name, out r, p0, p1, p2, p3);
        return r;
    }
    public R CallLuaFunc<R, P0, P1, P2, P3, P4>(string name, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4)
    {
        R r;
        CallLuaFunc(name, out r, p0, p1, p2, p3, p4);
        return r;
    }
    public R CallLuaFunc<R, P0, P1, P2, P3, P4, P5>(string name, P0 p0, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5)
    {
        R r;
        CallLuaFunc(name, out r, p0, p1, p2, p3, p4, p5);
        return r;
    }
    public object[] CallLuaFunc(string name, params object[] args)
    {
        if (!ReferenceEquals(_Self, null)
#if UNITY_EDITOR
            && !_Self.IsClosed
#endif
            )
        {
            var l = _Self._L;
            using (var lr = new LuaStateRecover(l))
            {
                if (args != null)
                {
                    for (int i = 0; i < args.Length; ++i)
                    {
                        l.PushLua(args[i]);
                    }
                }

                int oldtop = lr.Top;
                var code = CallLuaFuncInternal(l, _Self.Refid, name, oldtop);
                int newtop = l.gettop();
                if (code == 0 && newtop >= oldtop)
                {
                    var cnt = newtop - oldtop;
                    var rv = ObjectPool.GetReturnValueFromPool(cnt);
                    for (int i = 0; i < cnt; ++i)
                    {
                        rv[i] = l.GetLua(i + oldtop + 1);
                    }
                    return rv;
                }
            }
        }
        return null;
    }

    //public void EditorPrepareLuaRes()
    //{
    //    ResManager.ResLoader.Init();
    //    LanguageConverter.Init();
    //}
    void Awake()
    {
#if UNITY_EDITOR
        var awaken = GlobalLua.L["___EDITOR_AWAKEN"].ConvertType<int>();
        if (awaken == 0)
        {
            GlobalLua.EditorCheckRunningState();
            GlobalLua.L["___EDITOR_AWAKEN"] = 1;

            //Init();
            //EditorPrepareLuaRes();

            //var l = GlobalLua.L.L;
            //l.pushcfunction(LuaHub.LuaFuncOnError); // err
            //l.GetGlobal("require"); // err require
            //l.PushString("init"); // err require "main"
            //if (l.pcall(1, 0, -3) == 0)
            //{
            //    l.pop(1);
            //}
            //else
            //{
            //    DynamicHelper.LogError(l.GetLua(-1));
            //    l.pop(2);
            //}
        }
#endif
        _Awaken = true;
#if ENABLE_PROFILER_LUA_DEEP
        using (var pcon = ProfilerContext.Create("LuaBehav - Awake - RemoveDestroyRegIndex"))
#endif
        if (_DestroyRegIndex > 0)
        {
            RemoveDestroyRegIndex(_DestroyRegIndex);
            _DestroyRegIndex = 0;
        }
        TryAwake();
    }
    public bool TryAwake()
    {
        if (ShouldBindLua && !string.IsNullOrEmpty(InitLuaPath))
        {
            BindLua();
            CallLuaFunc("awake"); // Notice! The awake will NOT be called for the runtime binded behaviours!
            return true;
        }
        else
        {
            return false;
        }
    }
    protected internal void OnDestroy()
    {
        if (!_Destroyed)
        {
            _Destroyed = true;
            CallLuaFunc("onDestroy");
            if (!object.ReferenceEquals(_Self, null))
            {
                DisposeLuaBinding(_Self);
                _Self.Dispose();
                _Self = null;
            }
            if (_DestroyRegIndex > 0)
            {
                RemoveDestroyRegIndex(_DestroyRegIndex);
                _DestroyRegIndex = 0;
            }
            if (ExFields != null)
            {
                ExFields.Clear();
            }
            CheckDestroyed();
        }
    }

#if COMPATIBLE_RESMANAGER_V1
    [SerializeField] private List<string> ExFieldKeys;
    [SerializeField] private List<int> ExFieldTypes;
    [SerializeField] private List<int> ExFieldIndices;
    [SerializeField] private List<bool> ExFieldValsBool;
    [SerializeField] private List<int> ExFieldValsInt;
    [SerializeField] private List<double> ExFieldValsDouble;
    [SerializeField] private List<string> ExFieldValsString;
    [SerializeField] private List<Object> ExFieldValsObj;

    public bool HaveOldVersionData()
    {
        if (ExFieldKeys != null && ExFieldKeys.Count > 0) return true;
        //if (ExFieldTypes != null && ExFieldTypes.Count > 0) return true;
        //if (ExFieldIndices != null && ExFieldIndices.Count > 0) return true;
        //if (ExFieldValsBool != null && ExFieldValsBool.Count > 0) return true;
        //if (ExFieldValsInt != null && ExFieldValsInt.Count > 0) return true;
        //if (ExFieldValsDouble != null && ExFieldValsDouble.Count > 0) return true;
        //if (ExFieldValsString != null && ExFieldValsString.Count > 0) return true;
        //if (ExFieldValsObj != null && ExFieldValsObj.Count > 0) return true;
        return false;
    }
    public void OnBeforeSerialize()
    {
        ExFieldKeys = null;
        ExFieldTypes = null;
        ExFieldIndices = null;
        ExFieldValsBool = null;
        ExFieldValsInt = null;
        ExFieldValsDouble = null;
        ExFieldValsString = null;
        ExFieldValsObj = null;
    }

    public void OnAfterDeserialize()
    {
        if (HaveOldVersionData())
        {
            ExFields.SyncWithOther(ExFieldKeys, ExFieldTypes, ExFieldIndices, ExFieldValsBool, ExFieldValsInt, ExFieldValsDouble, ExFieldValsString, ExFieldValsObj);
        }
    }
#endif
}
