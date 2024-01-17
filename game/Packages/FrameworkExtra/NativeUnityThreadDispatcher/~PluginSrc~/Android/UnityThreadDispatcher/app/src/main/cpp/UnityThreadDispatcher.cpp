//
// Created by Silas on 2018/5/28.
//

#include <stddef.h>
#include <assert.h>
#define Assert assert

#include "IUnityEventQueue.h"

// Structure and GUID
struct DispatcherEvent {};
REGISTER_EVENT_ID(0x20BF8ACA12714A78ULL,0xA7E3EC2701896039ULL,DispatcherEvent)

static DispatcherEvent l_DispatcherEventInstance;
static UnityEventQueue::IUnityEventQueue* lp_EventQueue = 0;

typedef void (*DispatcherEventDelegate)();
static DispatcherEventDelegate lf_DispatcherEventHandlerEx = 0;

static void DispatcherEventHandlerMain (const DispatcherEvent & payload)
{
    if (lf_DispatcherEventHandlerEx)
    {
        lf_DispatcherEventHandlerEx();
    }
}
static UnityEventQueue::StaticFunctionEventHandler<DispatcherEvent> l_DispatcherEventHandlerInstance (&DispatcherEventHandlerMain);

extern "C"
{
void UnityPluginLoad(IUnityInterfaces* unityInterfaces)
{
    lp_EventQueue = unityInterfaces->Get<UnityEventQueue::IUnityEventQueue>();
    if (lp_EventQueue)
    {
        lp_EventQueue->AddHandler(&l_DispatcherEventHandlerInstance);
    }
}

int IsDispatcherReady()
{
    return lp_EventQueue != 0;
}
void RegDispatcherEventHandler(DispatcherEventDelegate handler)
{
    lf_DispatcherEventHandlerEx = handler;
}
void TrigDispatcherEvent()
{
    if (lp_EventQueue)
    {
        lp_EventQueue->SendEvent(l_DispatcherEventInstance);
    }
}
}