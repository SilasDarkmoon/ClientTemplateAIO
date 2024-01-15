//
//  EventPlugin.m
//  EventPlugin
//
//  Created by user on 14-12-2.
//  Copyright (c) 2014年 Silas. All rights reserved.
//

#import "EventPlugin.h"

typedef void (*Del_CEventHandler)(const char* cate);
// Reg & Unreg
typedef int (*Del_RegHandler)(const char* cate, Del_CEventHandler handler);
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

static Del_RegHandler Func_RegHandler           = 0;
static Del_UnregHandler Func_UnregHandler       = 0;
static Del_SetHandlerOrder Func_SetHandlerOrder = 0;
static Del_TrigEvent Func_TrigEvent             = 0;
static Del_GetValType Func_GetValType           = 0;
static Del_GetValBool Func_GetValBool           = 0;
static Del_GetValNum Func_GetValNum             = 0;
static Del_GetValPtr Func_GetValPtr             = 0;
static Del_GetValStrTo Func_GetValStrTo         = 0;
static Del_SetValBool Func_SetValBool           = 0;
static Del_SetValNum Func_SetValNum             = 0;
static Del_SetValPtr Func_SetValPtr             = 0;
static Del_SetValStr Func_SetValStr             = 0;
static Del_UnsetVal Func_UnsetVal               = 0;
static Del_GetParamCount Func_GetParamCount     = 0;
static Del_SetParamCount Func_SetParamCount     = 0;
static Del_GetParam Func_GetParam               = 0;
static Del_SetParam Func_SetParam               = 0;
static Del_GetParamName Func_GetParamName       = 0;
static Del_SetParamName Func_SetParamName       = 0;
static Del_GetGlobal Func_GetGlobal             = 0;
static Del_SetGlobal Func_SetGlobal             = 0;
static Del_NewList Func_NewList                 = 0;
static Del_GetValListTo Func_GetValListTo       = 0;
static Del_SetValList Func_SetValList           = 0;

int RegHandler(const char* cate, CEventHandler handler)
{
    return Func_RegHandler(cate, handler);
}
void UnregHandler(const char* cate, int refid)
{
    Func_UnregHandler(cate, refid);
}
void SetHandlerOrder(const char* cate, int refid, int order)
{
    Func_SetHandlerOrder(cate, refid, order);
}
void TrigEvent(const char* cate)
{
    Func_TrigEvent(cate);
}
int GetValType()
{
    return Func_GetValType();
}
int GetValBool()
{
    return Func_GetValBool();
}
double GetValNum()
{
    return Func_GetValNum();
}
void* GetValPtr()
{
    return Func_GetValPtr();
}
int GetValStrTo(char* buffer)
{
    return Func_GetValStrTo(buffer);
}
char* FillValStr(char* buffer)
{
    GetValStrTo(buffer);
    return buffer;
}
#define GetValStr() FillValStr((char*)alloca(GetValStrTo(0)))
void SetValBool(int val)
{
    Func_SetValBool(val);
}
void SetValNum(double num)
{
    Func_SetValNum(num);
}
void SetValPtr(void* ptr)
{
    Func_SetValPtr(ptr);
}
void SetValStr(const char* str)
{
    Func_SetValStr(str);
}
void UnsetVal()
{
    Func_UnsetVal();
}
int GetParamCount(int token)
{
    return Func_GetParamCount(token);
}
void SetParamCount(int token, int cnt)
{
    Func_SetParamCount(token, cnt);
}
void GetParam(int token, int index)
{
    Func_GetParam(token, index);
}
void SetParam(int token, int index)
{
    Func_SetParam(token, index);
}
void GetParamName(int token, int index)
{
    Func_GetParamName(token, index);
}
void SetParamName(int token, int index)
{
    Func_SetParamName(token, index);
}
void GetGlobal(const char* name)
{
    Func_GetGlobal(name);
}
void SetGlobal(const char* name)
{
    Func_SetGlobal(name);
}
int NewList()
{
    return Func_NewList();
}
void GetValListTo(int list)
{
    Func_GetValListTo(list);
}
void SetValList(int list)
{
    Func_SetValList(list);
}

void cevent_callhandler(const char* cate, CEventHandler pfunc)
{
    pfunc(cate);
}

void cevent_init(
 Del_RegHandler func_RegHandler
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
    Func_RegHandler = func_RegHandler;
    Func_UnregHandler = func_UnregHandler;
    Func_SetHandlerOrder = func_SetHandlerOrder;
    Func_TrigEvent = func_TrigEvent;
    Func_GetValType = func_GetValType;
    Func_GetValBool = func_GetValBool;
    Func_GetValNum = func_GetValNum;
    Func_GetValPtr = func_GetValPtr;
    Func_GetValStrTo = func_GetValStrTo;
    Func_SetValBool = func_SetValBool;
    Func_SetValNum = func_SetValNum;
    Func_SetValPtr = func_SetValPtr;
    Func_SetValStr = func_SetValStr;
    Func_UnsetVal = func_UnsetVal;
    Func_GetParamCount = func_GetParamCount;
    Func_SetParamCount = func_SetParamCount;
    Func_GetParam = func_GetParam;
    Func_SetParam = func_SetParam;
    Func_GetParamName = func_GetParamName;
    Func_SetParamName = func_SetParamName;
    Func_GetGlobal = func_GetGlobal;
    Func_SetGlobal = func_SetGlobal;
    Func_NewList = func_NewList;
    Func_GetValListTo = func_GetValListTo;
    Func_SetValList = func_SetValList;
}

