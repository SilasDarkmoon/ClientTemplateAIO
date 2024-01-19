using System;
using System.Collections.Generic;
using UnityEngine;

public static class SafeAreaConfig
{
    public struct SafeAreaParam
    {
        public SafeAreaParam(List<float> list)
        {
            this.ScreenWidth = list[0];
            this.ScreenHeight = list[1];
            this.NotchSize = list[2];
        }
        public float ScreenWidth;
        public float ScreenHeight;
        public float NotchSize;
    }

    //模拟刘海机型配置表
    private static Dictionary<SafeAreaRect.SimulateOption, SafeAreaParam> SimulationModelConfig = new Dictionary<SafeAreaRect.SimulateOption, SafeAreaParam>
    {
        {SafeAreaRect.SimulateOption.IphoneXLandscape, new SafeAreaParam(new List<float>{2436, 1125, 90})},
        {SafeAreaRect.SimulateOption.IphoneXPortrait, new SafeAreaParam(new List<float>{2436, 1125, 90})},
        {SafeAreaRect.SimulateOption.OPPOR15, new SafeAreaParam(new List<float>{2280, 1080, 85})}
    };

    public static bool HasConfig()
    {
#if UNITY_2018_3_OR_NEWER
        return Screen.safeArea.width != Screen.width || Screen.safeArea.height != Screen.height;
#else
#if UNITY_IOS
#if UNITY_5_6_OR_NEWER
        var config = LuaTableUtils.GetXLuaTable("ui.common.SafeAreaConfig").Get<LuaTable>("IOSModelConfigNew");
        return config.ContainsKey<UnityEngine.iOS.DeviceGeneration>(UnityEngine.iOS.Device.generation);
#else
        var config = LuaTableUtils.GetXLuaTable("ui.common.SafeAreaConfig").Get<LuaTable>("IOSModelConfig");
        return config.ContainsKey<string>(UnityEngine.SystemInfo.deviceModel.ToLower());
#endif
#elif UNITY_ANDROID
        var config = LuaTableUtils.GetXLuaTable("ui.common.SafeAreaConfig").Get<LuaTable>("AndroidModelConfig");
        return config.ContainsKey<string>(UnityEngine.SystemInfo.deviceModel.ToLower());
#endif
        return false;
#endif
    }

    public static SafeAreaParam GetConfig()
    {
#if UNITY_2018_3_OR_NEWER
        float width = Math.Max(Screen.width, Screen.height);
        float height = Math.Min(Screen.width, Screen.height);
        float notchSize = Math.Max(Math.Max(Screen.safeArea.xMin, Screen.safeArea.yMin), Math.Max(Screen.width - Screen.safeArea.xMax, Screen.height - Screen.safeArea.yMax));
        var safeAreaParam = new SafeAreaParam(new List<float>(new float[3] { width, height, notchSize }));
        return safeAreaParam;
#else
#if UNITY_IOS
#if UNITY_5_6_OR_NEWER
        var config = LuaTableUtils.GetXLuaTable("ui.common.SafeAreaConfig").Get<LuaTable>("IOSModelConfigNew");
        var safeAreaParam = new SafeAreaParam(config.Get<UnityEngine.iOS.DeviceGeneration, List<float>>(UnityEngine.iOS.Device.generation));
        return safeAreaParam;
#else
        var config = LuaTableUtils.GetXLuaTable("ui.common.SafeAreaConfig").Get<LuaTable>("IOSModelConfig");
        var safeAreaParam = new SafeAreaParam(config.Get<List<float>>(UnityEngine.SystemInfo.deviceModel.ToLower()));
        return safeAreaParam;
#endif
#elif UNITY_ANDROID
        var config = LuaTableUtils.GetXLuaTable("ui.common.SafeAreaConfig").Get<LuaTable>("AndroidModelConfig");
        var safeAreaParam = new SafeAreaParam(config.Get<List<float>>(UnityEngine.SystemInfo.deviceModel.ToLower()));
        return safeAreaParam;
#endif
        return default(SafeAreaParam);
#endif
    }

    public static SafeAreaParam GetSimulateConfig(SafeAreaRect.SimulateOption simulate)
    {
        return SimulationModelConfig[simulate];
    }
}
