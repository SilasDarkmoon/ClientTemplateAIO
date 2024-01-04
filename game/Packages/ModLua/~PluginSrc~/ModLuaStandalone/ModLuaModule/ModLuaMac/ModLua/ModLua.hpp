//
//  ModLua.hpp
//  ModLua
//
//  Created by Silas on 2021/4/12.
//

#ifndef ModLua_
#define ModLua_

/* The classes below are exported */
#pragma GCC visibility push(default)

//class ModLua
//{
//    public:
//    void HelloWorld(const char *);
//};

extern "C"
{
int luapoen_ModLua(void* l);
}

#pragma GCC visibility pop
#endif
