//
//  ModLua.cpp
//  ModLua
//
//  Created by Silas on 2021/4/12.
//

#include <iostream>
#include "ModLua.hpp"
#include "ModLuaPriv.hpp"

//void ModLua::HelloWorld(const char * s)
//{
//    ModLuaPriv *theObj = new ModLuaPriv;
//    theObj->HelloWorldPriv(s);
//    delete theObj;
//};
//
//void ModLuaPriv::HelloWorldPriv(const char * s)
//{
//    std::cout << s << std::endl;
//};

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Standard headers
#include <stdio.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>
#include <assert.h>
#include <iostream>

// Provided by the AppHost NuGet package and installed as an SDK pack
#include <nethost.h>

// Header files copied from https://github.com/dotnet/core-setup
#include <coreclr_delegates.h>
#include <hostfxr.h>

#ifdef WINDOWS
#include <Windows.h>

#define STR(s) L ## s
#define CH(c) L ## c
#define DIR_SEPARATOR L'\\'

#else
#include <dlfcn.h>
#include <limits.h>

#define STR(s) s
#define CH(c) c
#define DIR_SEPARATOR '/'
#define MAX_PATH PATH_MAX

#endif

#if __APPLE__
#include <mach-o/dyld.h>
#endif

using string_t = std::basic_string<char_t>;

namespace
{
    // Globals to hold hostfxr exports
    hostfxr_initialize_for_runtime_config_fn init_fptr;
    hostfxr_set_runtime_property_value_fn runtimeset_fptr;
    hostfxr_get_runtime_delegate_fn get_delegate_fptr;
    hostfxr_close_fn close_fptr;

    // Forward declarations
    bool load_hostfxr();
    load_assembly_and_get_function_pointer_fn get_dotnet_load_assembly(const char_t *assembly, const char_t* exeroot);
}

#pragma GCC visibility push(default)
extern "C"
{
int luaopen_ModLua(void* l)
{
    //std::cout << "begin init ModLua" << std::endl;
    
    char_t host_path[MAX_PATH];
#if WINDOWS
    auto size = ::GetFullPathNameW(argv[0], sizeof(host_path) / sizeof(char_t), host_path, nullptr);
    assert(size != 0);
#elif __APPLE__
    uint32_t exefolderlen = 0;
    _NSGetExecutablePath(0, &exefolderlen);
    if (exefolderlen >= MAX_PATH)
    {
        assert(false && "Failure: executable path too long.");
        return EXIT_FAILURE;
    }
    _NSGetExecutablePath(host_path, &exefolderlen);
    --exefolderlen;
    host_path[exefolderlen] = 0;
//    for (int i = exefolderlen - 1; i >= 0; --i, --exefolderlen)
//    {
//      char ch = host_path[i];
//      if (ch == '\\' || ch == '/')
//      {
//        break;
//      }
//      else
//      {
//          host_path[i] = 0;
//      }
//    }
#else
    auto resolved = realpath(argv[0], host_path);
    assert(resolved != nullptr);
#endif

    string_t root_path = host_path;
    auto pos = root_path.find_last_of(DIR_SEPARATOR);
    assert(pos != string_t::npos);
    root_path = root_path.substr(0, pos + 1);

    //
    // STEP 1: Load HostFxr and get exported hosting functions
    //
    if (!load_hostfxr())
    {
        assert(false && "Failure: load_hostfxr()");
        return EXIT_FAILURE;
    }
    
    //std::cout << root_path << std::endl;

    //
    // STEP 2: Initialize and start the .NET Core runtime
    //
    const string_t config_path = root_path + STR("ModLuaStandalone.runtimeconfig.json");
    load_assembly_and_get_function_pointer_fn load_assembly_and_get_function_pointer = nullptr;
    load_assembly_and_get_function_pointer = get_dotnet_load_assembly(config_path.c_str(), root_path.c_str());
    assert(load_assembly_and_get_function_pointer != nullptr && "Failure: get_dotnet_load_assembly()");

    //
    // STEP 3: Load managed assembly and get function pointer to a managed method
    //
    const string_t dotnetlib_path = root_path + STR("ModLuaStandalone.dll");
    const char_t *dotnet_type = STR("UnityEngineEx.GlobalLua, ModLuaStandalone");
    // Function pointer to managed delegate with non-default signature
    typedef void (CORECLR_DELEGATE_CALLTYPE *luaopen_fn)(void *);
    luaopen_fn custom = nullptr;
    int rc = load_assembly_and_get_function_pointer(
        dotnetlib_path.c_str(),
        dotnet_type,
        STR("luaopen_ModLua") /*method_name*/,
        STR("LuaLib.LuaCoreLib+CFunction, ModLuaStandalone") /*delegate_type_name*/,
        nullptr,
        (void**)&custom);
    assert(rc == 0 && custom != nullptr && "Failure: load_assembly_and_get_function_pointer()");

    //
    // STEP 4: Run managed code
    //
    custom(l);
    return 0;
}
}
#pragma GCC visibility pop

namespace
{
    // Forward declarations
    void *load_library(const char_t *);
    void *get_export(void *, const char *);

#ifdef WINDOWS
    void *load_library(const char_t *path)
    {
        HMODULE h = ::LoadLibraryW(path);
        assert(h != nullptr);
        return (void*)h;
    }
    void *get_export(void *h, const char *name)
    {
        void *f = ::GetProcAddress((HMODULE)h, name);
        assert(f != nullptr);
        return f;
    }
#else
    void *load_library(const char_t *path)
    {
        void *h = dlopen(path, RTLD_LAZY | RTLD_LOCAL);
        assert(h != nullptr);
        return h;
    }
    void *get_export(void *h, const char *name)
    {
        void *f = dlsym(h, name);
        assert(f != nullptr);
        return f;
    }
#endif

    // <SnippetLoadHostFxr>
    // Using the nethost library, discover the location of hostfxr and get exports
    bool load_hostfxr()
    {
        // Pre-allocate a large buffer for the path to hostfxr
        char_t buffer[MAX_PATH];
        size_t buffer_size = sizeof(buffer) / sizeof(char_t);
        int rc = get_hostfxr_path(buffer, &buffer_size, nullptr);
        if (rc != 0)
            return false;

        // Load hostfxr and get desired exports
        void *lib = load_library(buffer);
        init_fptr = (hostfxr_initialize_for_runtime_config_fn)get_export(lib, "hostfxr_initialize_for_runtime_config");
        runtimeset_fptr = (hostfxr_set_runtime_property_value_fn)get_export(lib, "hostfxr_set_runtime_property_value");
        get_delegate_fptr = (hostfxr_get_runtime_delegate_fn)get_export(lib, "hostfxr_get_runtime_delegate");
        close_fptr = (hostfxr_close_fn)get_export(lib, "hostfxr_close");

        return (init_fptr && runtimeset_fptr && get_delegate_fptr && close_fptr);
    }
    // </SnippetLoadHostFxr>

    // <SnippetInitialize>
    // Load and initialize .NET Core and get desired function pointer for scenario
    load_assembly_and_get_function_pointer_fn get_dotnet_load_assembly(const char_t *config_path, const char_t* exeroot)
    {
        // Load .NET Core
        void *load_assembly_and_get_function_pointer = nullptr;
        hostfxr_handle cxt = nullptr;
        int rc = init_fptr(config_path, nullptr, &cxt);
        if (rc != 0 || cxt == nullptr)
        {
            std::cerr << "Init failed: " << std::hex << std::showbase << rc << std::endl;
            close_fptr(cxt);
            return nullptr;
        }
        
        rc = runtimeset_fptr(cxt, "APP_CONTEXT_BASE_DIRECTORY", exeroot);
        
        // Get the load assembly function pointer
        rc = get_delegate_fptr(
            cxt,
            hdt_load_assembly_and_get_function_pointer,
            &load_assembly_and_get_function_pointer);
        if (rc != 0 || load_assembly_and_get_function_pointer == nullptr)
            std::cerr << "Get delegate failed: " << std::hex << std::showbase << rc << std::endl;

        close_fptr(cxt);
        return (load_assembly_and_get_function_pointer_fn)load_assembly_and_get_function_pointer;
    }
    // </SnippetInitialize>
}
