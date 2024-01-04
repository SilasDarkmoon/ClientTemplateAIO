// ModLua.cpp : 定义 DLL 的导出函数。
//

#include "framework.h"
#include "ModLua.h"


extern "C" MODLUA_API int luaopen_ModLua(void* l)
{
	UnityEngineEx::GlobalLua::luaopen_ModLua(System::IntPtr(l));
	return 0;
}