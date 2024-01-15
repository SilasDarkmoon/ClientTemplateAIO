#pragma once
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
};

extern "C"
{
    extern EventPluginInterface* g_pEventPluginInterface;
    void UnityPluginLoad(IUnityInterfaces *unityInterfaces);
}

#include <alloca.h>
extern "C" {
typedef void (*CEventHandler)(const char *);
inline __always_inline int RegHandler(const char *cate, CEventHandler handler)
{
    return g_pEventPluginInterface->Func_RegHandler(cate, handler);
}
inline __always_inline int RegJavaHandler(const char *cate, void *prun)
{
    return g_pEventPluginInterface->Func_RegJavaHandler(cate, prun);
}
inline __always_inline void UnregHandler(const char *cate, int refid)
{
    g_pEventPluginInterface->Func_UnregHandler(cate, refid);
}
inline __always_inline void SetHandlerOrder(const char *cate, int refid, int order)
{
    g_pEventPluginInterface->Func_SetHandlerOrder(cate, refid, order);
}
inline __always_inline void TrigEvent(const char *cate)
{
    g_pEventPluginInterface->Func_TrigEvent(cate);
}
inline __always_inline int GetValType() {
    return g_pEventPluginInterface->Func_GetValType();
}
inline __always_inline int GetValBool() {
    return g_pEventPluginInterface->Func_GetValBool();
}
inline __always_inline double GetValNum() {
    return g_pEventPluginInterface->Func_GetValNum();
}
inline __always_inline void *GetValPtr() {
    return g_pEventPluginInterface->Func_GetValPtr();
}
inline __always_inline int GetValStrTo(char *buffer) {
    return g_pEventPluginInterface->Func_GetValStrTo(buffer);
}
inline __always_inline char *FillValStr(char *buffer) {
    GetValStrTo(buffer);
    return buffer;
}
#define GetValStr() FillValStr((char*)alloca(GetValStrTo(0)))
inline __always_inline void SetValBool(int val) {
    g_pEventPluginInterface->Func_SetValBool(val);
}
inline __always_inline void SetValNum(double num) {
    g_pEventPluginInterface->Func_SetValNum(num);
}
inline __always_inline void SetValPtr(void *ptr) {
    g_pEventPluginInterface->Func_SetValPtr(ptr);
}
inline __always_inline void SetValStr(const char *str) {
    g_pEventPluginInterface->Func_SetValStr(str);
}
inline __always_inline void UnsetVal() {
    g_pEventPluginInterface->Func_UnsetVal();
}
inline __always_inline int GetParamCount(int token) {
    return g_pEventPluginInterface->Func_GetParamCount(token);
}
inline __always_inline void SetParamCount(int token, int cnt) {
    g_pEventPluginInterface->Func_SetParamCount(token, cnt);
}
inline __always_inline void GetParam(int token, int index) {
    g_pEventPluginInterface->Func_GetParam(token, index);
}
inline __always_inline void SetParam(int token, int index) {
    g_pEventPluginInterface->Func_SetParam(token, index);
}
inline __always_inline void GetParamName(int token, int index) {
    g_pEventPluginInterface->Func_GetParamName(token, index);
}
inline __always_inline void SetParamName(int token, int index) {
    g_pEventPluginInterface->Func_SetParamName(token, index);
}
inline __always_inline void GetGlobal(const char *name) {
    g_pEventPluginInterface->Func_GetGlobal(name);
}
inline __always_inline void SetGlobal(const char *name) {
    g_pEventPluginInterface->Func_SetGlobal(name);
}
inline __always_inline int NewList() {
    return g_pEventPluginInterface->Func_NewList();
}
inline __always_inline void GetValListTo(int list) {
    g_pEventPluginInterface->Func_GetValListTo(list);
}
inline __always_inline void SetValList(int list) {
    g_pEventPluginInterface->Func_SetValList(list);
}
#define TOKEN_ARGS 0
#define TOKEN_RETS 1
#define TOKEN_CALL 2
#define PARAM_TYPE_NULL 0
#define PARAM_TYPE_BOOL 1
#define PARAM_TYPE_NUMBER 2
#define PARAM_TYPE_STRING 3
#define PARAM_TYPE_LIST 4
#define PARAM_TYPE_OBJECT 5
}