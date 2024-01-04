using UnityEngine;
using System.Collections;
using LuaLib;
using UnityEngineEx;

public class LuaBehavUpdate : LuaBehavEx
{
    private void Update()
    {
        this.CallLuaFunc("update");
    }
}