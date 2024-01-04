using UnityEngine;
using System.Collections;
using LuaLib;
using UnityEngineEx;

public class LuaBehavStart : LuaBehavEx
{
    private void Start()
    {
        this.CallLuaFunc("start");
    }
}