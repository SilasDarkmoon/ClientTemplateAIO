using System;
using System.Collections;
using System.Collections.Generic;

namespace LuaLib
{
    /// <summary>
    /// if some code is with this attribute, it is supposed to be precompiled.
    /// if some code is with this attribute and ignore is true, it is forbidden to be precompiled.
    /// if some code is not with this attribute, we must precompile it with - 1. Manually, 2. Write a white list, 3. Precompile while running.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
    public class LuaPrecompileAttribute : Attribute
    {
        public bool Ignore { get; set; }
    }

//#if UNITY_INCLUDE_TESTS
//    #region TESTS
//    public class ReflectAnalyzerTestClass
//    {
//        public class TestGenericClass<T>
//        {
//            public class TestNestedGenericClass<U>
//            {

//            }

//            public static void TestFuncGenericFunc<Y>(ref Y y) { }
//        }

//        public class TestNormalClass
//        {
//            [LuaPrecompile]
//            public void TestFunc() { }
//            [LuaPrecompile(Ignore = true)]
//            public void TestIgnoreFunc() { }
//        }

//        public class TestGenBase<T, U>
//        {
//            public void Func1(T t, U u) { }
//        }
//        public class TestGenBase<T> : TestGenBase<T, int>
//        {
//            public void Func2(T t) { }
//        }
//        public class TestGenChild : TestGenBase<float>
//        {
//        }
//    }
//    #endregion
//#endif
}
