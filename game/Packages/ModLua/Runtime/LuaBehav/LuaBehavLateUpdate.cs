using UnityEngine;
using System.Collections;
using LuaLib;
using UnityEngineEx;

public class LuaBehavLateUpdate : LuaBehavEx
{
    private void LateUpdate()
    {
        this.CallLuaFunc("lateUpdate");
    }
}