using UnityEngine;
using System.Collections;
using LuaLib;
using UnityEngineEx;

public class LuaBehavEnable : LuaBehavEx
{
    private void OnEnable()
    {
        this.CallLuaFunc("onEnable");
    }
    private void OnDisable()
    {
        this.CallLuaFunc("onDisable");
    }
}