using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LuaLib
{
    /// <summary>
    /// on Method / Constructor - easy to understand
    /// on Property - hot fix it's get method and set method
    /// on Event - (not implemented) hot fix it's add / remove / trig methods
    /// on Class - hot fix all it's methods / properties. notice: the nested types will not inherit this attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Property  | AttributeTargets.Event | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class LuaHotFixAttribute : Attribute
    {
        public bool Forbidden;
    }
}