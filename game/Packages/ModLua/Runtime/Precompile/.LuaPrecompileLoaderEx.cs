using System.Collections;
using System.Collections.Generic;

namespace LuaLib
{
#if (UNITY_ENGINE || UNITY_5_3_OR_NEWER)
    public class LuaPrecompileLoaderEx : LuaPrecompileLoader
    {
        public override void Init()
        {
            LuaHubEx.Init();
        }
    }
#endif
    public static partial class LuaHubEx
    {
        static LuaHubEx() { }
        public static void Init() { }
    }
}