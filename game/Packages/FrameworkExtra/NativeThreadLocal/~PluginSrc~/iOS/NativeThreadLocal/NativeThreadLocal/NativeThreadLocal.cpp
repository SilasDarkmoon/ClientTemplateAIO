//
//  NativeThreadLocal.cpp
//  NativeThreadLocal
//
//  Created by Silas on 2018/5/22.
//  Copyright © 2018年 Silas. All rights reserved.
//

#include <pthread.h>

typedef void (*FuncDisposeObj)(void* obj);
struct ThrealLocalContainer
{
public:
    ThrealLocalContainer()
    {
        funcDispose = nullptr;
        innerobj = nullptr;
    }
    ThrealLocalContainer(void* obj, FuncDisposeObj func_dispose)
    {
        funcDispose = func_dispose;
        innerobj = obj;
    }
    ~ThrealLocalContainer()
    {
        if (funcDispose && innerobj)
        {
            funcDispose(innerobj);
        }
    }
    ThrealLocalContainer(ThrealLocalContainer&& other)
    {
        funcDispose = other.funcDispose;
        innerobj = other.innerobj;
        other.funcDispose = nullptr;
        other.innerobj = nullptr;
    }
    ThrealLocalContainer(ThrealLocalContainer&) = delete;
    
    void SetContainer(void* obj, FuncDisposeObj func_dispose)
    {
        this->~ThrealLocalContainer();
        funcDispose = func_dispose;
        innerobj = obj;
    }
    void* GetContainer()
    {
        return innerobj;
    }
private:
    FuncDisposeObj funcDispose;
    void* innerobj;
};

static void ThreadLocalDispose(void* rawcontainer)
{
    if (rawcontainer)
    {
        ThrealLocalContainer* pcontainer = (ThrealLocalContainer*)rawcontainer;
        delete pcontainer;
    }
}
struct ContainerKeyInitializer
{
public:
    pthread_key_t key;
    ContainerKeyInitializer()
    {
        pthread_key_create(&key, ThreadLocalDispose);
    }
};
static ContainerKeyInitializer ContainerKey;
static pthread_key_t GetContainerKey()
{
    return ContainerKey.key;
}

extern "C"
{
    void RegThreadLocalContainer(void* obj, FuncDisposeObj func_dispose)
    {
        void* rawold = pthread_getspecific(GetContainerKey());
        if (!rawold)
        {
            rawold = new ThrealLocalContainer();
            pthread_setspecific(GetContainerKey(), rawold);
        }
        ThrealLocalContainer* pcontainer = (ThrealLocalContainer*)rawold;
        pcontainer->SetContainer(obj, func_dispose);
    }
    void* GetThreadLocalContainer()
    {
        void* rawold = pthread_getspecific(GetContainerKey());
        if (rawold)
        {
            ThrealLocalContainer* pcontainer = (ThrealLocalContainer*)rawold;
            return pcontainer->GetContainer();
        }
        return 0;
    }
    void* GetThreadID()
    {
        auto id = pthread_self();
        return *(void**)&id;
    }
}

