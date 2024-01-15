// below is added to where we need to use the event system.
// may need extern "C" when included in C++ file.
#include <alloca.h>

typedef void(*CEventHandler)(const char*);
int RegHandler(const char* cate, CEventHandler handler);
void UnregHandler(const char* cate, int refid);
void SetHandlerOrder(const char* cate, int refid, int order);
void TrigEvent(const char* cate);
int GetValType();
int GetValBool();
double GetValNum();
void* GetValPtr();
int GetValStrTo(char* buffer);
char* FillValStr(char* buffer);
#define GetValStr() FillValStr((char*)alloca(GetValStrTo(0)))
void SetValBool(int val);
void SetValNum(double num);
void SetValPtr(void* ptr);
void SetValStr(const char* str);
void UnsetVal();
int GetParamCount(int token);
void SetParamCount(int token, int cnt);
void GetParam(int token, int index);
void SetParam(int token, int index);
void GetParamName(int token, int index);
void SetParamName(int token, int index);
void GetGlobal(const char* name);
void SetGlobal(const char* name);
int NewList();
void GetValListTo(int list);
void SetValList(int list);
#define TOKEN_ARGS 0
#define TOKEN_RETS 1
#define TOKEN_CALL 2
#define PARAM_TYPE_NULL 0
#define PARAM_TYPE_BOOL 1
#define PARAM_TYPE_NUMBER 2
#define PARAM_TYPE_STRING 3
#define PARAM_TYPE_LIST 4
#define PARAM_TYPE_OBJECT 5
#if __APPLE__
#import <Foundation/Foundation.h>
static void SetValNSString(NSString* str)
{
    if (str)
    {
        SetValStr([str UTF8String]);
    }
    else
    {
        SetValStr(0);
    }
}
#endif
