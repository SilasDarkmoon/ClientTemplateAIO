using System;
using System.Collections.Generic;

using lua = LuaLib.LuaCoreLib;
using lual = LuaLib.LuaAuxLib;
using luae = LuaLib.LuaLibEx;

namespace LuaLib
{
    public static partial class LuaConst
    {
        public static readonly LuaString LS_META_KEY_GC =       new LuaString("__gc");
        public static readonly LuaString LS_META_KEY_CALL =     new LuaString("__call");
        public static readonly LuaString LS_META_KEY_INDEX =    new LuaString("__index");
        public static readonly LuaString LS_META_KEY_NINDEX =   new LuaString("__newindex");
        public static readonly LuaString LS_META_KEY_MODE =     new LuaString("__mode");
        public static readonly LuaString LS_META_KEY_EQ =       new LuaString("__eq");
        public static readonly LuaString LS_META_KEY_ADD =      new LuaString("__add");
        public static readonly LuaString LS_META_KEY_SUB =      new LuaString("__sub");
        public static readonly LuaString LS_META_KEY_MUL =      new LuaString("__mul");
        public static readonly LuaString LS_META_KEY_DIV =      new LuaString("__div");
        public static readonly LuaString LS_META_KEY_MOD =      new LuaString("__mod");
        public static readonly LuaString LS_META_KEY_LT =       new LuaString("__lt");
        public static readonly LuaString LS_META_KEY_LE =       new LuaString("__le");
        public static readonly LuaString LS_META_KEY_UNM =      new LuaString("__unm");
        public static readonly LuaString LS_META_KEY_TOSTRING = new LuaString("__tostring");

        public static readonly LuaString LS_COMMON_EMPTY =      new LuaString("");
        public static readonly LuaString LS_COMMON_K =          new LuaString("k");
        public static readonly LuaString LS_COMMON_V =          new LuaString("v");

        public static readonly LuaString LS_SP_KEY_TYPE =       new LuaString("@type");
        public static readonly LuaString LS_SP_KEY_OBJMETA =    new LuaString("@objmeta");
        public static readonly LuaString LS_SP_KEY_OBJMETHODS = new LuaString("@objmethods");
        public static readonly LuaString LS_SP_KEY_OBJGETTER =  new LuaString("@objgetter");
        public static readonly LuaString LS_SP_KEY_OBJSETTER =  new LuaString("@objsetter");
        public static readonly LuaString LS_SP_KEY_GETTER =     new LuaString("@getter");
        public static readonly LuaString LS_SP_KEY_SETTER =     new LuaString("@setter");
        public static readonly LuaString LS_SP_KEY_INDEX =      new LuaString("@index");
        public static readonly LuaString LS_SP_KEY_ADD =        new LuaString("@+");
        public static readonly LuaString LS_SP_KEY_SUB =        new LuaString("@-");
        public static readonly LuaString LS_SP_KEY_MUL =        new LuaString("@*");
        public static readonly LuaString LS_SP_KEY_DIV =        new LuaString("@/");
        public static readonly LuaString LS_SP_KEY_MOD =        new LuaString("@%");
        public static readonly LuaString LS_SP_KEY_LT =         new LuaString("@<");
        public static readonly LuaString LS_SP_KEY_LE =         new LuaString("@<=");
        public static readonly LuaString LS_SP_KEY_EQ =         new LuaString("@==");
        public static readonly LuaString LS_SP_KEY_EXT =        new LuaString("@ext");
        public static readonly LuaString LS_SP_KEY_NONPUBLIC =  new LuaString("@npub");
        public static readonly LuaString LS_SP_KEY_REFLECTOR =  new LuaString("@refl");

        public static readonly LuaString LS_LIB_DEBUG =         new LuaString("debug");
        public static readonly LuaString LS_LIB_TRACEBACK =     new LuaString("traceback");
        public static readonly LuaString LS_LIB_GETINFO =       new LuaString("getinfo");
        public static readonly LuaString LS_LIB_NAME =          new LuaString("name");
        public static readonly LuaString LS_LIB_SHORT_SRC =     new LuaString("short_src");
        public static readonly LuaString LS_LIB_LINEDEFINED =   new LuaString("linedefined");
        public static readonly LuaString LS_LIB_CURRENTLINE =   new LuaString("currentline");
        
        public static readonly IntPtr LRKEY_STR_CACHE =         new IntPtr(1001);
        public static readonly IntPtr LRKEY_OBJ_CACHE =         new IntPtr(1002);
        public static readonly IntPtr LRKEY_OBJ_CACHE_REG =     new IntPtr(1003);
        public static readonly IntPtr LRKEY_REF_MAN =           new IntPtr(1004);
        public static readonly IntPtr LRKEY_DEL_CACHE =         new IntPtr(1005);
        public static readonly IntPtr LRKEY_REF_THREAD =        new IntPtr(1006);
        public static readonly IntPtr LRKEY_REF_ATTACH =        new IntPtr(1007);
        public static readonly IntPtr LRKEY_OBJ_GC_TRACKER =    new IntPtr(1008);
        public static readonly IntPtr LRKEY_OBJ_META =          new IntPtr(2101);
        public static readonly IntPtr LRKEY_OBJ_META_EX =       new IntPtr(2102);
        public static readonly IntPtr LRKEY_OBJ_META_RAW =      new IntPtr(2103);
        public static readonly IntPtr LRKEY_OBJ_META_CACHED =   new IntPtr(2109);
        public static readonly IntPtr LRKEY_GETTER =            new IntPtr(2104);
        public static readonly IntPtr LRKEY_SETTER =            new IntPtr(2105);
        public static readonly IntPtr LRKEY_METHODS =           new IntPtr(2106);
        public static readonly IntPtr LRKEY_OBJ_META_EQ =       new IntPtr(2107);
        public static readonly IntPtr LRKEY_OBJ_META_BIN =      new IntPtr(2108);
        public static readonly IntPtr LRKEY_TARGET =            new IntPtr(2201);
        public static readonly IntPtr LRKEY_CALL_METHOD =       new IntPtr(2202);
        public static readonly IntPtr LRKEY_TYPE_OBJ =          new IntPtr(2301);
        public static readonly IntPtr LRKEY_TYPE_HUB =          new IntPtr(2302);
        public static readonly IntPtr LRKEY_TYPE_TRANS =        new IntPtr(2303);
        public static readonly IntPtr LRKEY_EXTENDED =          new IntPtr(2401);
        public static readonly IntPtr LRKEY_EXT_CALLEE =        new IntPtr(2402);
        public static readonly IntPtr LRKEY_GENERIC_CACHE =     new IntPtr(2501);
        public static readonly IntPtr LRKEY_DEL_WRAP =          new IntPtr(2601);
        public static readonly IntPtr LRKEY_HOTFIX_ROOT =       new IntPtr(3001);
        public static readonly IntPtr LRKEY_CO_FINALLY =        new IntPtr(3101);
        public static readonly IntPtr LRKEY_CO_CONTINUE =       new IntPtr(3102);
    }
}