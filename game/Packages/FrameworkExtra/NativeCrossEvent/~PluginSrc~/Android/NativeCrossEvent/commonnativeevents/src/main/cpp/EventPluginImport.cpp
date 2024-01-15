#include "EventPluginImport.h"

static IUnityInterfaces* l_pUnityInterfaces = 0;
static UnityInterfaceGUID l_GUID_EventPlugin(0x3B1B5C026DE0459EUL, 0xAC5BB55679C7DA71UL);

extern "C"
{
EventPluginInterface* g_pEventPluginInterface;
void UnityPluginLoad(IUnityInterfaces *unityInterfaces)
{
    l_pUnityInterfaces = unityInterfaces;
    g_pEventPluginInterface = (EventPluginInterface*)unityInterfaces->GetInterface(l_GUID_EventPlugin);
}
}
