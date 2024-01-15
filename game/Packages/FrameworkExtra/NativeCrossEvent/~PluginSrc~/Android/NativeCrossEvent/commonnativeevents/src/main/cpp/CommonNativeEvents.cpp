#include "EventPluginImport.h"

static void GetEncryptKey(const char* cate)
{
    SetParamCount(TOKEN_RETS, 1);
    SetValStr("6uQ0y4MOtWovdWqkI8ZaiyT4dPVnGU0y");
    SetParam(TOKEN_RETS, 0);
}

extern "C"
{
void Init_CommonNativeEvents()
{
    RegHandler("Get_EncryptKey", GetEncryptKey);
}
}
