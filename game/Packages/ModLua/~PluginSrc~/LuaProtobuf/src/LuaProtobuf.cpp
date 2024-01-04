// LuaProtobuf.cpp : 定义 DLL 应用程序的导出函数。
//

#include "LuaImport.h"

#if _WIN32
#define EXPORT_API __declspec(dllexport)
#else
#define EXPORT_API
#endif

extern "C"
{
    extern int luaopen_pb_io(lua_State *L);
    extern int luaopen_pb_conv(lua_State *L);
    extern int luaopen_pb_buffer(lua_State *L);
    extern int luaopen_pb_slice(lua_State *L);
    extern int luaopen_pb_unsafe(lua_State *L);
    extern int luaopen_pb(lua_State *L);
    
    EXPORT_API void InitLuaProtobufPlugin(void* l)
    {
        lua_getglobal(l, "package");
        if (!lua_istable(l, -1))
        {
            lua_pop(l, 1);
            lua_newtable(l);
            lua_pushvalue(l, -1);
            lua_setglobal(l, "package");
        }
        lua_getfield(l, -1, "preload");
        if (!lua_istable(l, -1))
        {
            lua_pop(l, 1);
            lua_newtable(l);
            lua_pushvalue(l, -1);
            lua_setfield(l, -3, "preload");
        }

        lua_pushcfunction(l, luaopen_pb);
        lua_setfield(l, -2, "pb");
        lua_pushcfunction(l, luaopen_pb_io);
        lua_setfield(l, -2, "pb.io");
        lua_pushcfunction(l, luaopen_pb_conv);
        lua_setfield(l, -2, "pb.conv");
        lua_pushcfunction(l, luaopen_pb_buffer);
        lua_setfield(l, -2, "pb.buffer");
        lua_pushcfunction(l, luaopen_pb_slice);
        lua_setfield(l, -2, "pb.slice");
        lua_pushcfunction(l, luaopen_pb_unsafe);
        lua_setfield(l, -2, "pb.unsafe");

        lua_pop(l, 2);
    }
}
