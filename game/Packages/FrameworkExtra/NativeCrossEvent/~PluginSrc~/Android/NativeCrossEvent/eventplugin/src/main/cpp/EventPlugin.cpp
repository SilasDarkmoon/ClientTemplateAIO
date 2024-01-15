#include <stddef.h>
#include <assert.h>
#define Assert assert

#include "IUnityInterface.h"

// Event plugin Functions
typedef void (*Del_CEventHandler)(const char* cate);
// Reg & Unreg
typedef int (*Del_RegHandler)(const char* cate, Del_CEventHandler handler);
typedef int (*Del_RegJavaHandler)(const char* cate, void* prun);
typedef void (*Del_UnregHandler)(const char* cate, int refid);
typedef void (*Del_SetHandlerOrder)(const char* cate, int refid, int order);
// Call Other
typedef void (*Del_TrigEvent)(const char* cate);
// Get Value
typedef int (*Del_GetValType)();
typedef int (*Del_GetValBool)();
typedef double (*Del_GetValNum)();
typedef void* (*Del_GetValPtr)();
typedef int (*Del_GetValStrTo)(char* buffer);
// Set Value
typedef void (*Del_SetValBool)(int val);
typedef void (*Del_SetValNum)(double num);
typedef void (*Del_SetValPtr)(void* ptr);
typedef void (*Del_SetValStr)(const char* str);
typedef void (*Del_UnsetVal)();
// Get Params
typedef int (*Del_GetParamCount)(int token);
typedef void (*Del_SetParamCount)(int token, int cnt);
typedef void (*Del_GetParam)(int token, int index);
typedef void (*Del_SetParam)(int token, int index);
typedef void (*Del_GetParamName)(int token, int index);
typedef void (*Del_SetParamName)(int token, int index);
// Global Val
typedef void (*Del_GetGlobal)(const char* name);
typedef void (*Del_SetGlobal)(const char* name);
// Dict & List
typedef int (*Del_NewList)();
typedef void (*Del_GetValListTo)(int list);
typedef void (*Del_SetValList)(int list);

struct EventPluginInterface : IUnityInterface
{
    Del_RegHandler Func_RegHandler              = 0;
    Del_RegJavaHandler Func_RegJavaHandler      = 0;
    Del_UnregHandler Func_UnregHandler          = 0;
    Del_SetHandlerOrder Func_SetHandlerOrder    = 0;
    Del_TrigEvent Func_TrigEvent                = 0;
    Del_GetValType Func_GetValType              = 0;
    Del_GetValBool Func_GetValBool              = 0;
    Del_GetValNum Func_GetValNum                = 0;
    Del_GetValPtr Func_GetValPtr                = 0;
    Del_GetValStrTo Func_GetValStrTo            = 0;
    Del_SetValBool Func_SetValBool              = 0;
    Del_SetValNum Func_SetValNum                = 0;
    Del_SetValPtr Func_SetValPtr                = 0;
    Del_SetValStr Func_SetValStr                = 0;
    Del_UnsetVal Func_UnsetVal                  = 0;
    Del_GetParamCount Func_GetParamCount        = 0;
    Del_SetParamCount Func_SetParamCount        = 0;
    Del_GetParam Func_GetParam                  = 0;
    Del_SetParam Func_SetParam                  = 0;
    Del_GetParamName Func_GetParamName          = 0;
    Del_SetParamName Func_SetParamName          = 0;
    Del_GetGlobal Func_GetGlobal                = 0;
    Del_SetGlobal Func_SetGlobal                = 0;
    Del_NewList Func_NewList                    = 0;
    Del_GetValListTo Func_GetValListTo          = 0;
    Del_SetValList Func_SetValList              = 0;
} l_EventPluginInterface;

// from the java plugin, see below
static void JavaInit();
static void CallJavaRunnable(void* obj);
static void ReleaseJavaObject(void* pobj);

#include <alloca.h>
extern "C" {
typedef void (*CEventHandler)(const char*);
int RegHandler(const char* cate, CEventHandler handler)
{
    return l_EventPluginInterface.Func_RegHandler(cate, handler);
}
int RegJavaHandler(const char* cate, void* prun)
{
    return l_EventPluginInterface.Func_RegJavaHandler(cate, prun);
}
void UnregHandler(const char* cate, int refid)
{
    l_EventPluginInterface.Func_UnregHandler(cate, refid);
}
void SetHandlerOrder(const char* cate, int refid, int order)
{
    l_EventPluginInterface.Func_SetHandlerOrder(cate, refid, order);
}
void TrigEvent(const char* cate)
{
    l_EventPluginInterface.Func_TrigEvent(cate);
}
int GetValType()
{
    return l_EventPluginInterface.Func_GetValType();
}
int GetValBool()
{
    return l_EventPluginInterface.Func_GetValBool();
}
double GetValNum()
{
    return l_EventPluginInterface.Func_GetValNum();
}
void* GetValPtr()
{
    return l_EventPluginInterface.Func_GetValPtr();
}
int GetValStrTo(char* buffer)
{
    return l_EventPluginInterface.Func_GetValStrTo(buffer);
}
char* FillValStr(char* buffer)
{
    GetValStrTo(buffer);
    return buffer;
}
#define GetValStr() FillValStr((char*)alloca(GetValStrTo(0)))
void SetValBool(int val)
{
    l_EventPluginInterface.Func_SetValBool(val);
}
void SetValNum(double num)
{
    l_EventPluginInterface.Func_SetValNum(num);
}
void SetValPtr(void* ptr)
{
    l_EventPluginInterface.Func_SetValPtr(ptr);
}
void SetValStr(const char* str)
{
    l_EventPluginInterface.Func_SetValStr(str);
}
void UnsetVal()
{
    l_EventPluginInterface.Func_UnsetVal();
}
int GetParamCount(int token)
{
    return l_EventPluginInterface.Func_GetParamCount(token);
}
void SetParamCount(int token, int cnt)
{
    l_EventPluginInterface.Func_SetParamCount(token, cnt);
}
void GetParam(int token, int index)
{
    l_EventPluginInterface.Func_GetParam(token, index);
}
void SetParam(int token, int index)
{
    l_EventPluginInterface.Func_SetParam(token, index);
}
void GetParamName(int token, int index)
{
    l_EventPluginInterface.Func_GetParamName(token, index);
}
void SetParamName(int token, int index)
{
    l_EventPluginInterface.Func_SetParamName(token, index);
}
void GetGlobal(const char* name)
{
    l_EventPluginInterface.Func_GetGlobal(name);
}
void SetGlobal(const char* name)
{
    l_EventPluginInterface.Func_SetGlobal(name);
}
int NewList()
{
    return l_EventPluginInterface.Func_NewList();
}
void GetValListTo(int list)
{
    l_EventPluginInterface.Func_GetValListTo(list);
}
void SetValList(int list)
{
    l_EventPluginInterface.Func_SetValList(list);
}
#define TOKEN_ARGS 0
#define TOKEN_RETS 1
#define TOKEN_CALL 2

void cevent_calljava(void* prun)
{
    CallJavaRunnable(prun);
}

void cevent_releasejava(void* pobj)
{
    ReleaseJavaObject(pobj);
}

void cevent_init(
        const char* libPath
        , Del_RegHandler func_RegHandler
        , Del_RegJavaHandler func_RegJavaHandler
        , Del_UnregHandler func_UnregHandler
        , Del_SetHandlerOrder func_SetHandlerOrder
        , Del_TrigEvent func_TrigEvent
        , Del_GetValType func_GetValType
        , Del_GetValBool func_GetValBool
        , Del_GetValNum func_GetValNum
        , Del_GetValPtr func_GetValPtr
        , Del_GetValStrTo func_GetValStrTo
        , Del_SetValBool func_SetValBool
        , Del_SetValNum func_SetValNum
        , Del_SetValPtr func_SetValPtr
        , Del_SetValStr func_SetValStr
        , Del_UnsetVal func_UnsetVal
        , Del_GetParamCount func_GetParamCount
        , Del_SetParamCount func_SetParamCount
        , Del_GetParam func_GetParam
        , Del_SetParam func_SetParam
        , Del_GetParamName func_GetParamName
        , Del_SetParamName func_SetParamName
        , Del_GetGlobal func_GetGlobal
        , Del_SetGlobal func_SetGlobal
        , Del_NewList func_NewList
        , Del_GetValListTo func_GetValListTo
        , Del_SetValList func_SetValList
)
{
    l_EventPluginInterface.Func_RegHandler = func_RegHandler;
    l_EventPluginInterface.Func_RegJavaHandler = func_RegJavaHandler;
    l_EventPluginInterface.Func_UnregHandler = func_UnregHandler;
    l_EventPluginInterface.Func_SetHandlerOrder = func_SetHandlerOrder;
    l_EventPluginInterface.Func_TrigEvent = func_TrigEvent;
    l_EventPluginInterface.Func_GetValType = func_GetValType;
    l_EventPluginInterface.Func_GetValBool = func_GetValBool;
    l_EventPluginInterface.Func_GetValNum = func_GetValNum;
    l_EventPluginInterface.Func_GetValPtr = func_GetValPtr;
    l_EventPluginInterface.Func_GetValStrTo = func_GetValStrTo;
    l_EventPluginInterface.Func_SetValBool = func_SetValBool;
    l_EventPluginInterface.Func_SetValNum = func_SetValNum;
    l_EventPluginInterface.Func_SetValPtr = func_SetValPtr;
    l_EventPluginInterface.Func_SetValStr = func_SetValStr;
    l_EventPluginInterface.Func_UnsetVal = func_UnsetVal;
    l_EventPluginInterface.Func_GetParamCount = func_GetParamCount;
    l_EventPluginInterface.Func_SetParamCount = func_SetParamCount;
    l_EventPluginInterface.Func_GetParam = func_GetParam;
    l_EventPluginInterface.Func_SetParam = func_SetParam;
    l_EventPluginInterface.Func_GetParamName = func_GetParamName;
    l_EventPluginInterface.Func_SetParamName = func_SetParamName;
    l_EventPluginInterface.Func_GetGlobal = func_GetGlobal;
    l_EventPluginInterface.Func_SetGlobal = func_SetGlobal;
    l_EventPluginInterface.Func_NewList = func_NewList;
    l_EventPluginInterface.Func_GetValListTo = func_GetValListTo;
    l_EventPluginInterface.Func_SetValList = func_SetValList;

    JavaInit();
}
}

__attribute__((destructor)) void EventPlugin_Close()
{
}

// below is for the java plugin
#include <jni.h>

static JavaVM* l_jvm = 0;
extern "C" {
JNIEXPORT jint JNI_OnLoad(JavaVM* vm, void* reserved)
{
    l_jvm = vm;
    JavaInit();
    return JNI_VERSION_1_2;
}
}

static void JavaInit()
{
    if (l_jvm && l_EventPluginInterface.Func_TrigEvent)
    {
        JNIEnv* jenv = 0;
        l_jvm->AttachCurrentThread(&jenv, 0);
        jclass jcls = jenv->FindClass("host/silas/anative/android/eventplugin/EventPlugin");
        if (jcls)
        {
            jmethodID mid = jenv->GetStaticMethodID(jcls, "Init", "()V");
            if (mid)
            {
                jenv->CallStaticVoidMethod(jcls, mid);
            }
        }
    }
}
static void CallJavaRunnable(void* pobj)
{
    jobject obj = (jobject)pobj;
    if (l_jvm && obj)
    {
        JNIEnv* jenv = 0;
        l_jvm->AttachCurrentThread(&jenv, 0);
        { // run!
            jclass cls = jenv->GetObjectClass(obj);
            if (cls)
            {
                jmethodID mid = jenv->GetMethodID(cls, "run", "()V");
                if (mid)
                {
                    jenv->CallVoidMethod(obj, mid);
                }
            }
        }
    }
}
static void ReleaseJavaObject(void* pobj)
{
    jobject obj = (jobject)pobj;
    if (l_jvm && obj)
    {
        JNIEnv* jenv = 0;
        l_jvm->AttachCurrentThread(&jenv, 0);
        jenv->DeleteGlobalRef(obj);
    }
}

extern "C" {
jint JNICALL Java_host_silas_anative_android_eventplugin_EventPlugin_RegHandler(JNIEnv* env, jobject thiz, jstring cate, jobject runnable)
{
    jobject prun = env->NewGlobalRef(runnable);
    const char* szcate = env->GetStringUTFChars(cate, 0);
    jint rv = RegJavaHandler(szcate, prun);
    env->ReleaseStringUTFChars(cate, szcate);
    return rv;
}
void JNICALL Java_host_silas_anative_android_eventplugin_EventPlugin_UnregHandler(JNIEnv* env, jobject thiz, jstring cate, jint refid)
{
    const char* szcate = env->GetStringUTFChars(cate, 0);
    UnregHandler(szcate, refid);
    env->ReleaseStringUTFChars(cate, szcate);
}
void JNICALL Java_host_silas_anative_android_eventplugin_EventPlugin_SetHandlerOrder(JNIEnv* env, jobject thiz, jstring cate, jint refid, jint order)
{
    const char* szcate = env->GetStringUTFChars(cate, 0);
    SetHandlerOrder(szcate, refid, order);
    env->ReleaseStringUTFChars(cate, szcate);
}
void JNICALL Java_host_silas_anative_android_eventplugin_EventPlugin_TrigEvent(JNIEnv* env, jobject thiz, jstring cate)
{
    const char* szcate = env->GetStringUTFChars(cate, 0);
    TrigEvent(szcate);
    env->ReleaseStringUTFChars(cate, szcate);
}
jint JNICALL Java_host_silas_anative_android_eventplugin_EventPlugin_GetValType(JNIEnv* env, jobject thiz)
{
    return GetValType();
}
jboolean JNICALL Java_host_silas_anative_android_eventplugin_EventPlugin_GetValBool(JNIEnv* env, jobject thiz)
{
    return GetValBool();
}
jdouble JNICALL Java_host_silas_anative_android_eventplugin_EventPlugin_GetValNum(JNIEnv* env, jobject thiz)
{
    return GetValNum();
}
jlong JNICALL Java_host_silas_anative_android_eventplugin_EventPlugin_GetValPtr(JNIEnv* env, jobject thiz)
{
    return (jlong)GetValPtr();
}
jstring JNICALL Java_host_silas_anative_android_eventplugin_EventPlugin_GetValStr(JNIEnv* env, jobject thiz)
{
    return env->NewStringUTF(GetValStr());
}
void JNICALL Java_host_silas_anative_android_eventplugin_EventPlugin_SetValBool(JNIEnv* env, jobject thiz, jboolean val)
{
    SetValBool(val);
}
void JNICALL Java_host_silas_anative_android_eventplugin_EventPlugin_SetValNum(JNIEnv* env, jobject thiz, jdouble val)
{
    SetValNum(val);
}
void JNICALL Java_host_silas_anative_android_eventplugin_EventPlugin_SetValPtr(JNIEnv* env, jobject thiz, jlong val)
{
    SetValPtr((void*)val);
}
void JNICALL Java_host_silas_anative_android_eventplugin_EventPlugin_SetValStr(JNIEnv* env, jobject thiz, jstring val)
{
    if (val != 0)
    {
        const char* sz = env->GetStringUTFChars(val, 0);
        SetValStr(sz);
        env->ReleaseStringUTFChars(val, sz);
    }
    else
    {
        UnsetVal();
    }
}
void JNICALL Java_host_silas_anative_android_eventplugin_EventPlugin_UnsetVal(JNIEnv* env, jobject thiz)
{
    UnsetVal();
}
jint JNICALL Java_host_silas_anative_android_eventplugin_EventPlugin_GetParamCount(JNIEnv* env, jobject thiz, jint token)
{
    return GetParamCount(token);
}
void JNICALL Java_host_silas_anative_android_eventplugin_EventPlugin_SetParamCount(JNIEnv* env, jobject thiz, jint token, jint cnt)
{
    SetParamCount(token, cnt);
}
void JNICALL Java_host_silas_anative_android_eventplugin_EventPlugin_GetParam(JNIEnv* env, jobject thiz, jint token, jint index)
{
    GetParam(token, index);
}
void JNICALL Java_host_silas_anative_android_eventplugin_EventPlugin_SetParam(JNIEnv* env, jobject thiz, jint token, jint index)
{
    SetParam(token, index);
}
void JNICALL Java_host_silas_anative_android_eventplugin_EventPlugin_GetParamName(JNIEnv* env, jobject thiz, jint token, jint index)
{
    GetParamName(token, index);
}
void JNICALL Java_host_silas_anative_android_eventplugin_EventPlugin_SetParamName(JNIEnv* env, jobject thiz, jint token, jint index)
{
    SetParamName(token, index);
}
void JNICALL Java_host_silas_anative_android_eventplugin_EventPlugin_GetGlobal(JNIEnv* env, jobject thiz, jstring name)
{
    const char* sz = env->GetStringUTFChars(name, 0);
    GetGlobal(sz);
    env->ReleaseStringUTFChars(name, sz);
}
void JNICALL Java_host_silas_anative_android_eventplugin_EventPlugin_SetGlobal(JNIEnv* env, jobject thiz, jstring name)
{
    const char* sz = env->GetStringUTFChars(name, 0);
    SetGlobal(sz);
    env->ReleaseStringUTFChars(name, sz);
}
jint JNICALL Java_host_silas_anative_android_eventplugin_EventPlugin_NewList(JNIEnv* env, jobject thiz)
{
    return NewList();
}
void JNICALL Java_host_silas_anative_android_eventplugin_EventPlugin_GetValListTo(JNIEnv* env, jobject thiz, jint list)
{
    GetValListTo(list);
}
void JNICALL Java_host_silas_anative_android_eventplugin_EventPlugin_SetValList(JNIEnv* env, jobject thiz, jint list)
{
    SetValList(list);
}
}

static IUnityInterfaces* l_pUnityInterfaces = 0;
static UnityInterfaceGUID l_GUID_EventPlugin(0x3B1B5C026DE0459EUL, 0xAC5BB55679C7DA71UL);

extern "C"
{
void UnityPluginLoad(IUnityInterfaces *unityInterfaces) {
    l_pUnityInterfaces = unityInterfaces;
    unityInterfaces->RegisterInterface(l_GUID_EventPlugin, &l_EventPluginInterface);
}
}