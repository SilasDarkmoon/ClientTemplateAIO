#include "LuaImport.h"
#include "IUnityInterface.h"

#if !TARGET_OS_IPHONE

#if __cplusplus
extern "C" {
#endif

LuaPluginInterface* g_pLuaPluginInterface = 0;

void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces *unityInterfaces)
{
//#if __cplusplus
//    UnityInterfaceGUID guidlua(0xE472D7060BA74533UL, 0x88F82C3A4ADD9BBFUL);
//#else
//    UnityInterfaceGUID guidlua;
//    guidlua.m_GUIDHigh = 0xE472D7060BA74533UL;
//    guidlua.m_GUIDLow = 0x88F82C3A4ADD9BBFUL;
//#endif
//    g_pLuaPluginInterface = (LuaPluginInterface*)unityInterfaces->GetInterface(guidlua);
    g_pLuaPluginInterface = (LuaPluginInterface*)unityInterfaces->GetInterfaceSplit(0xE472D7060BA74533UL, 0x88F82C3A4ADD9BBFUL);
}

int UNITY_INTERFACE_EXPORT IsReady()
{
    return g_pLuaPluginInterface != 0;
}

#if __cplusplus
}
#endif
#endif //!TARGET_OS_IPHONE
