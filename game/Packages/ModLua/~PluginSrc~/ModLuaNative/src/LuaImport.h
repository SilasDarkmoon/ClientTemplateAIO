#ifndef HEADER_LUA_IMPORT
#define HEADER_LUA_IMPORT

#include <stddef.h>
#include <stdio.h>
#include <assert.h>
#define Assert assert
#if __APPLE__
#include "TargetConditionals.h"
#endif

#define lua_State void
#define LUA_IDSIZE	60
#define LUAL_BUFFERSIZE	(BUFSIZ > 16384 ? 8192 : BUFSIZ)
typedef double lua_Number;
typedef ptrdiff_t lua_Integer;
typedef struct lua_Debug {
    int event;
    const char *name;	/* (n) */
    const char *namewhat;	/* (n) `global', `local', `field', `method' */
    const char *what;	/* (S) `Lua', `C', `main', `tail' */
    const char *source;	/* (S) */
    int currentline;	/* (l) */
    int nups;		/* (u) number of upvalues */
    int linedefined;	/* (S) */
    int lastlinedefined;	/* (S) */
    char short_src[LUA_IDSIZE]; /* (S) */
    /* private part */
    int i_ci;  /* active function */
} lua_Debug;
typedef int(*lua_CFunction) (lua_State *L);
typedef const char * (*lua_Reader) (lua_State *L, void *ud, size_t *sz);
typedef int(*lua_Writer) (lua_State *L, const void* p, size_t sz, void* ud);
typedef void * (*lua_Alloc) (void *ud, void *ptr, size_t osize, size_t nsize);
typedef void(*lua_Hook) (lua_State *L, lua_Debug *ar);
typedef struct luaL_Reg {
    const char *name;
    lua_CFunction func;
} luaL_Reg;
typedef struct luaL_Buffer {
    char *p;			/* current position in buffer */
    int lvl;  /* number of strings in the stack (level) */
    lua_State *L;
    char buffer[LUAL_BUFFERSIZE];
} luaL_Buffer;

#define LUA_REGISTRYINDEX	(-10000)
#define LUA_ENVIRONINDEX	(-10001)
#define LUA_GLOBALSINDEX	(-10002)

#define LUA_OK		0
#define LUA_YIELD	1
#define LUA_ERRRUN	2
#define LUA_ERRSYNTAX	3
#define LUA_ERRMEM	4
#define LUA_ERRERR	5

#define LUA_TNONE		(-1)
#define LUA_TNIL		0
#define LUA_TBOOLEAN		1
#define LUA_TLIGHTUSERDATA	2
#define LUA_TNUMBER		3
#define LUA_TSTRING		4
#define LUA_TTABLE		5
#define LUA_TFUNCTION		6
#define LUA_TUSERDATA		7
#define LUA_TTHREAD		8

#define LUA_HOOKCALL	0
#define LUA_HOOKRET	1
#define LUA_HOOKLINE	2
#define LUA_HOOKCOUNT	3
#define LUA_HOOKTAILRET 4

#define LUA_MASKCALL	(1 << LUA_HOOKCALL)
#define LUA_MASKRET	(1 << LUA_HOOKRET)
#define LUA_MASKLINE	(1 << LUA_HOOKLINE)
#define LUA_MASKCOUNT	(1 << LUA_HOOKCOUNT)

#define LUA_MINSTACK	20
#define LUA_MULTRET	(-1)
#define LUA_NOREF       (-2)
#define LUA_REFNIL      (-1)

#define LUA_GCSTOP		0
#define LUA_GCRESTART		1
#define LUA_GCCOLLECT		2
#define LUA_GCCOUNT		3
#define LUA_GCCOUNTB		4
#define LUA_GCSTEP		5
#define LUA_GCSETPAUSE		6
#define LUA_GCSETSTEPMUL	7
#define LUA_GCISRUNNING		9

#if TARGET_OS_IPHONE
extern "C" {
lua_CFunction   (lua_atpanic) (lua_State *L, lua_CFunction panicf);
void            (lua_call) (lua_State *L, int nargs, int nresults);
int             (lua_checkstack) (lua_State *L, int extra);
void            (lua_close) (lua_State *L);
void            (lua_concat) (lua_State *L, int n);
int             (lua_cpcall) (lua_State *L, lua_CFunction func, void *ud);
void            (lua_createtable) (lua_State *L, int narr, int nrec);
int             (lua_dump) (lua_State *L, lua_Writer writer, void *data);
int             (lua_equal) (lua_State *L, int index1, int index2);
int             (lua_error) (lua_State *L);
int             (lua_gc) (lua_State *L, int what, int data);
lua_Alloc       (lua_getallocf) (lua_State *L, void **ud);
void            (lua_getfenv) (lua_State *L, int index);
void            (lua_getfield) (lua_State *L, int index, const char *k);
int             (lua_getmetatable) (lua_State *L, int index);
void            (lua_gettable) (lua_State *L, int index);
int             (lua_gettop) (lua_State *L);
void            (lua_insert) (lua_State *L, int index);
int             (lua_iscfunction) (lua_State *L, int index);
int             (lua_isnumber) (lua_State *L, int index);
int             (lua_isstring) (lua_State *L, int index);
int             (lua_isuserdata) (lua_State *L, int index);
int             (lua_lessthan) (lua_State *L, int index1, int index2);
int             (lua_load) (lua_State *L, lua_Reader reader, void *data, const char *chunkname);
lua_State *     (lua_newstate) (lua_Alloc f, void *ud);
lua_State *     (lua_newthread) (lua_State *L);
void *          (lua_newuserdata) (lua_State *L, size_t size);
int             (lua_next) (lua_State *L, int index);
size_t          (lua_objlen) (lua_State *L, int index);
int             (lua_pcall) (lua_State *L, int nargs, int nresults, int errfunc);
void            (lua_pushboolean) (lua_State *L, int b);
void            (lua_pushcclosure) (lua_State *L, lua_CFunction fn, int n);
const char *    (lua_pushfstring) (lua_State *L, const char *fmt, ...);
void            (lua_pushinteger) (lua_State *L, lua_Integer n);
void            (lua_pushlightuserdata) (lua_State *L, void *p);
void            (lua_pushlstring) (lua_State *L, const char *s, size_t len);
void            (lua_pushnil) (lua_State *L);
void            (lua_pushnumber) (lua_State *L, lua_Number n);
void            (lua_pushstring) (lua_State *L, const char *s);
int             (lua_pushthread) (lua_State *L);
void            (lua_pushvalue) (lua_State *L, int index);
const char *    (lua_pushvfstring) (lua_State *L, const char *fmt, va_list argp);
int             (lua_rawequal) (lua_State *L, int index1, int index2);
void            (lua_rawget) (lua_State *L, int index);
void            (lua_rawgeti) (lua_State *L, int index, int n);
void            (lua_rawset) (lua_State *L, int index);
void            (lua_rawseti) (lua_State *L, int index, int n);
void            (lua_remove) (lua_State *L, int index);
void            (lua_replace) (lua_State *L, int index);
int             (lua_resume) (lua_State *L, int narg);
void            (lua_setallocf) (lua_State *L, lua_Alloc f, void *ud);
int             (lua_setfenv) (lua_State *L, int index);
void            (lua_setfield) (lua_State *L, int index, const char *k);
int             (lua_setmetatable) (lua_State *L, int index);
void            (lua_settable) (lua_State *L, int index);
void            (lua_settop) (lua_State *L, int index);
int             (lua_status) (lua_State *L);
int             (lua_toboolean) (lua_State *L, int index);
lua_CFunction   (lua_tocfunction) (lua_State *L, int index);
lua_Integer     (lua_tointeger) (lua_State *L, int index);
const char *    (lua_tolstring) (lua_State *L, int index, size_t *len);
lua_Number      (lua_tonumber) (lua_State *L, int index);
const void *    (lua_topointer) (lua_State *L, int index);
lua_State *     (lua_tothread) (lua_State *L, int index);
void *          (lua_touserdata) (lua_State *L, int index);
int             (lua_type) (lua_State *L, int index);
const char *    (lua_typename) (lua_State *L, int tp);
void            (lua_xmove) (lua_State *from, lua_State *to, int n);
int             (lua_yield)  (lua_State *L, int nresults);

lua_Hook        (lua_gethook) (lua_State *L);
int             (lua_gethookcount) (lua_State *L);
int             (lua_gethookmask) (lua_State *L);
int             (lua_getinfo) (lua_State *L, const char *what, lua_Debug *ar);
const char *    (lua_getlocal) (lua_State *L, lua_Debug *ar, int n);
int             (lua_getstack) (lua_State *L, int level, lua_Debug *ar);
const char *    (lua_getupvalue) (lua_State *L, int funcindex, int n);
int             (lua_sethook) (lua_State *L, lua_Hook f, int mask, int count);
const char *    (lua_setlocal) (lua_State *L, lua_Debug *ar, int n);
const char *    (lua_setupvalue) (lua_State *L, int funcindex, int n);

void            (luaL_addlstring) (luaL_Buffer *B, const char *s, size_t l);
void            (luaL_addstring) (luaL_Buffer *B, const char *s);
void            (luaL_addvalue) (luaL_Buffer *B);
int             (luaL_argerror) (lua_State *L, int narg, const char *extramsg);
void            (luaL_buffinit) (lua_State *L, luaL_Buffer *B);
int             (luaL_callmeta) (lua_State *L, int obj, const char *e);
void            (luaL_checkany) (lua_State *L, int narg);
lua_Integer     (luaL_checkinteger) (lua_State *L, int narg);
const char *    (luaL_checklstring) (lua_State *L, int narg, size_t *l);
lua_Number      (luaL_checknumber) (lua_State *L, int narg);
int             (luaL_checkoption) (lua_State *L, int narg, const char *def, const char *const lst[]);
void            (luaL_checkstack) (lua_State *L, int sz, const char *msg);
void            (luaL_checktype) (lua_State *L, int narg, int t);
void *          (luaL_checkudata) (lua_State *L, int narg, const char *tname);
int             (luaL_error) (lua_State *L, const char *fmt, ...);
int             (luaL_getmetafield) (lua_State *L, int obj, const char *e);
const char *    (luaL_gsub) (lua_State *L, const char *s, const char *p, const char *r);
int             (luaL_loadbuffer) (lua_State *L, const char *buff, size_t sz, const char *name);
int             (luaL_loadfile) (lua_State *L, const char *filename);
int             (luaL_loadstring) (lua_State *L, const char *s);
int             (luaL_newmetatable) (lua_State *L, const char *tname);
lua_State *     (luaL_newstate) (void);
void            (luaL_openlibs) (lua_State *L);
lua_Integer     (luaL_optinteger) (lua_State *L, int narg, lua_Integer d);
const char *    (luaL_optlstring) (lua_State *L, int narg, const char *d, size_t *l);
lua_Number      (luaL_optnumber) (lua_State *L, int narg, lua_Number d);
char *          (luaL_prepbuffer) (luaL_Buffer *B);
void            (luaL_pushresult) (luaL_Buffer *B);
int             (luaL_ref) (lua_State *L, int t);
void            (luaL_register) (lua_State *L, const char *libname, const luaL_Reg *l);
int             (luaL_typerror) (lua_State *L, int narg, const char *tname);
void            (luaL_unref) (lua_State *L, int t, int ref);
void            (luaL_where) (lua_State *L, int lvl);
}

#else
typedef lua_CFunction   (*del_lua_atpanic) (lua_State *L, lua_CFunction panicf);
typedef void            (*del_lua_call) (lua_State *L, int nargs, int nresults);
typedef int             (*del_lua_checkstack) (lua_State *L, int extra);
typedef void            (*del_lua_close) (lua_State *L);
typedef void            (*del_lua_concat) (lua_State *L, int n);
typedef int             (*del_lua_cpcall) (lua_State *L, lua_CFunction func, void *ud);
typedef void            (*del_lua_createtable) (lua_State *L, int narr, int nrec);
typedef int             (*del_lua_dump) (lua_State *L, lua_Writer writer, void *data);
typedef int             (*del_lua_equal) (lua_State *L, int index1, int index2);
typedef int             (*del_lua_error) (lua_State *L);
typedef int             (*del_lua_gc) (lua_State *L, int what, int data);
typedef lua_Alloc       (*del_lua_getallocf) (lua_State *L, void **ud);
typedef void            (*del_lua_getfenv) (lua_State *L, int index);
typedef void            (*del_lua_getfield) (lua_State *L, int index, const char *k);
typedef int             (*del_lua_getmetatable) (lua_State *L, int index);
typedef void            (*del_lua_gettable) (lua_State *L, int index);
typedef int             (*del_lua_gettop) (lua_State *L);
typedef void            (*del_lua_insert) (lua_State *L, int index);
typedef int             (*del_lua_iscfunction) (lua_State *L, int index);
typedef int             (*del_lua_isnumber) (lua_State *L, int index);
typedef int             (*del_lua_isstring) (lua_State *L, int index);
typedef int             (*del_lua_isuserdata) (lua_State *L, int index);
typedef int             (*del_lua_lessthan) (lua_State *L, int index1, int index2);
typedef int             (*del_lua_load) (lua_State *L, lua_Reader reader, void *data, const char *chunkname);
typedef lua_State *     (*del_lua_newstate) (lua_Alloc f, void *ud);
typedef lua_State *     (*del_lua_newthread) (lua_State *L);
typedef void *          (*del_lua_newuserdata) (lua_State *L, size_t size);
typedef int             (*del_lua_next) (lua_State *L, int index);
typedef size_t          (*del_lua_objlen) (lua_State *L, int index);
typedef int             (*del_lua_pcall) (lua_State *L, int nargs, int nresults, int errfunc);
typedef void            (*del_lua_pushboolean) (lua_State *L, int b);
typedef void            (*del_lua_pushcclosure) (lua_State *L, lua_CFunction fn, int n);
typedef const char *    (*del_lua_pushfstring) (lua_State *L, const char *fmt, ...);
typedef void            (*del_lua_pushinteger) (lua_State *L, lua_Integer n);
typedef void            (*del_lua_pushlightuserdata) (lua_State *L, void *p);
typedef void            (*del_lua_pushlstring) (lua_State *L, const char *s, size_t len);
typedef void            (*del_lua_pushnil) (lua_State *L);
typedef void            (*del_lua_pushnumber) (lua_State *L, lua_Number n);
typedef void            (*del_lua_pushstring) (lua_State *L, const char *s);
typedef int             (*del_lua_pushthread) (lua_State *L);
typedef void            (*del_lua_pushvalue) (lua_State *L, int index);
typedef const char *    (*del_lua_pushvfstring) (lua_State *L, const char *fmt, va_list argp);
typedef int             (*del_lua_rawequal) (lua_State *L, int index1, int index2);
typedef void            (*del_lua_rawget) (lua_State *L, int index);
typedef void            (*del_lua_rawgeti) (lua_State *L, int index, int n);
typedef void            (*del_lua_rawset) (lua_State *L, int index);
typedef void            (*del_lua_rawseti) (lua_State *L, int index, int n);
typedef void            (*del_lua_remove) (lua_State *L, int index);
typedef void            (*del_lua_replace) (lua_State *L, int index);
typedef int             (*del_lua_resume) (lua_State *L, int narg);
typedef void            (*del_lua_setallocf) (lua_State *L, lua_Alloc f, void *ud);
typedef int             (*del_lua_setfenv) (lua_State *L, int index);
typedef void            (*del_lua_setfield) (lua_State *L, int index, const char *k);
typedef int             (*del_lua_setmetatable) (lua_State *L, int index);
typedef void            (*del_lua_settable) (lua_State *L, int index);
typedef void            (*del_lua_settop) (lua_State *L, int index);
typedef int             (*del_lua_status) (lua_State *L);
typedef int             (*del_lua_toboolean) (lua_State *L, int index);
typedef lua_CFunction   (*del_lua_tocfunction) (lua_State *L, int index);
typedef lua_Integer     (*del_lua_tointeger) (lua_State *L, int index);
typedef const char *    (*del_lua_tolstring) (lua_State *L, int index, size_t *len);
typedef lua_Number      (*del_lua_tonumber) (lua_State *L, int index);
typedef const void *    (*del_lua_topointer) (lua_State *L, int index);
typedef lua_State *     (*del_lua_tothread) (lua_State *L, int index);
typedef void *          (*del_lua_touserdata) (lua_State *L, int index);
typedef int             (*del_lua_type) (lua_State *L, int index);
typedef const char *    (*del_lua_typename) (lua_State *L, int tp);
typedef void            (*del_lua_xmove) (lua_State *from, lua_State *to, int n);
typedef int             (*del_lua_yield)  (lua_State *L, int nresults);

typedef lua_Hook        (*del_lua_gethook) (lua_State *L);
typedef int             (*del_lua_gethookcount) (lua_State *L);
typedef int             (*del_lua_gethookmask) (lua_State *L);
typedef int             (*del_lua_getinfo) (lua_State *L, const char *what, lua_Debug *ar);
typedef const char *    (*del_lua_getlocal) (lua_State *L, lua_Debug *ar, int n);
typedef int             (*del_lua_getstack) (lua_State *L, int level, lua_Debug *ar);
typedef const char *    (*del_lua_getupvalue) (lua_State *L, int funcindex, int n);
typedef int             (*del_lua_sethook) (lua_State *L, lua_Hook f, int mask, int count);
typedef const char *    (*del_lua_setlocal) (lua_State *L, lua_Debug *ar, int n);
typedef const char *    (*del_lua_setupvalue) (lua_State *L, int funcindex, int n);

typedef void            (*del_luaL_addlstring) (luaL_Buffer *B, const char *s, size_t l);
typedef void            (*del_luaL_addstring) (luaL_Buffer *B, const char *s);
typedef void            (*del_luaL_addvalue) (luaL_Buffer *B);
typedef int             (*del_luaL_argerror) (lua_State *L, int narg, const char *extramsg);
typedef void            (*del_luaL_buffinit) (lua_State *L, luaL_Buffer *B);
typedef int             (*del_luaL_callmeta) (lua_State *L, int obj, const char *e);
typedef void            (*del_luaL_checkany) (lua_State *L, int narg);
typedef lua_Integer     (*del_luaL_checkinteger) (lua_State *L, int narg);
typedef const char *    (*del_luaL_checklstring) (lua_State *L, int narg, size_t *l);
typedef lua_Number      (*del_luaL_checknumber) (lua_State *L, int narg);
typedef int             (*del_luaL_checkoption) (lua_State *L, int narg, const char *def, const char *const lst[]);
typedef void            (*del_luaL_checkstack) (lua_State *L, int sz, const char *msg);
typedef void            (*del_luaL_checktype) (lua_State *L, int narg, int t);
typedef void *          (*del_luaL_checkudata) (lua_State *L, int narg, const char *tname);
typedef int             (*del_luaL_error) (lua_State *L, const char *fmt, ...);
typedef int             (*del_luaL_getmetafield) (lua_State *L, int obj, const char *e);
typedef const char *    (*del_luaL_gsub) (lua_State *L, const char *s, const char *p, const char *r);
typedef int             (*del_luaL_loadbuffer) (lua_State *L, const char *buff, size_t sz, const char *name);
typedef int             (*del_luaL_loadfile) (lua_State *L, const char *filename);
typedef int             (*del_luaL_loadstring) (lua_State *L, const char *s);
typedef int             (*del_luaL_newmetatable) (lua_State *L, const char *tname);
typedef lua_State *     (*del_luaL_newstate) (void);
typedef void            (*del_luaL_openlibs) (lua_State *L);
typedef lua_Integer     (*del_luaL_optinteger) (lua_State *L, int narg, lua_Integer d);
typedef const char *    (*del_luaL_optlstring) (lua_State *L, int narg, const char *d, size_t *l);
typedef lua_Number      (*del_luaL_optnumber) (lua_State *L, int narg, lua_Number d);
typedef char *          (*del_luaL_prepbuffer) (luaL_Buffer *B);
typedef void            (*del_luaL_pushresult) (luaL_Buffer *B);
typedef int             (*del_luaL_ref) (lua_State *L, int t);
typedef void            (*del_luaL_register) (lua_State *L, const char *libname, const luaL_Reg *l);
typedef int             (*del_luaL_typerror) (lua_State *L, int narg, const char *tname);
typedef void            (*del_luaL_unref) (lua_State *L, int t, int ref);
typedef void            (*del_luaL_where) (lua_State *L, int lvl);

typedef struct LuaPluginInterface
{
    del_lua_atpanic             func_lua_atpanic;
    del_lua_call                func_lua_call;
    del_lua_checkstack          func_lua_checkstack;
    del_lua_close               func_lua_close;
    del_lua_concat              func_lua_concat;
    del_lua_cpcall              func_lua_cpcall;
    del_lua_createtable         func_lua_createtable;
    del_lua_dump                func_lua_dump;
    del_lua_equal               func_lua_equal;
    del_lua_error               func_lua_error;
    del_lua_gc                  func_lua_gc;
    del_lua_getallocf           func_lua_getallocf;
    del_lua_getfenv             func_lua_getfenv;
    del_lua_getfield            func_lua_getfield;
    del_lua_getmetatable        func_lua_getmetatable;
    del_lua_gettable            func_lua_gettable;
    del_lua_gettop              func_lua_gettop;
    del_lua_insert              func_lua_insert;
    del_lua_iscfunction         func_lua_iscfunction;
    del_lua_isnumber            func_lua_isnumber;
    del_lua_isstring            func_lua_isstring;
    del_lua_isuserdata          func_lua_isuserdata;
    del_lua_lessthan            func_lua_lessthan;
    del_lua_load                func_lua_load;
    del_lua_newstate            func_lua_newstate;
    del_lua_newthread           func_lua_newthread;
    del_lua_newuserdata         func_lua_newuserdata;
    del_lua_next                func_lua_next;
    del_lua_objlen              func_lua_objlen;
    del_lua_pcall               func_lua_pcall;
    del_lua_pushboolean         func_lua_pushboolean;
    del_lua_pushcclosure        func_lua_pushcclosure;
    del_lua_pushfstring         func_lua_pushfstring;
    del_lua_pushinteger         func_lua_pushinteger;
    del_lua_pushlightuserdata   func_lua_pushlightuserdata;
    del_lua_pushlstring         func_lua_pushlstring;
    del_lua_pushnil             func_lua_pushnil;
    del_lua_pushnumber          func_lua_pushnumber;
    del_lua_pushstring          func_lua_pushstring;
    del_lua_pushthread          func_lua_pushthread;
    del_lua_pushvalue           func_lua_pushvalue;
    del_lua_pushvfstring        func_lua_pushvfstring;
    del_lua_rawequal            func_lua_rawequal;
    del_lua_rawget              func_lua_rawget;
    del_lua_rawgeti             func_lua_rawgeti;
    del_lua_rawset              func_lua_rawset;
    del_lua_rawseti             func_lua_rawseti;
    del_lua_remove              func_lua_remove;
    del_lua_replace             func_lua_replace;
    del_lua_resume              func_lua_resume;
    del_lua_setallocf           func_lua_setallocf;
    del_lua_setfenv             func_lua_setfenv;
    del_lua_setfield            func_lua_setfield;
    del_lua_setmetatable        func_lua_setmetatable;
    del_lua_settable            func_lua_settable;
    del_lua_settop              func_lua_settop;
    del_lua_status              func_lua_status;
    del_lua_toboolean           func_lua_toboolean;
    del_lua_tocfunction         func_lua_tocfunction;
    del_lua_tointeger           func_lua_tointeger;
    del_lua_tolstring           func_lua_tolstring;
    del_lua_tonumber            func_lua_tonumber;
    del_lua_topointer           func_lua_topointer;
    del_lua_tothread            func_lua_tothread;
    del_lua_touserdata          func_lua_touserdata;
    del_lua_type                func_lua_type;
    del_lua_typename            func_lua_typename;
    del_lua_xmove               func_lua_xmove;
    del_lua_yield               func_lua_yield;

    del_lua_gethook             func_lua_gethook;
    del_lua_gethookcount        func_lua_gethookcount;
    del_lua_gethookmask         func_lua_gethookmask;
    del_lua_getinfo             func_lua_getinfo;
    del_lua_getlocal            func_lua_getlocal;
    del_lua_getstack            func_lua_getstack;
    del_lua_getupvalue          func_lua_getupvalue;
    del_lua_sethook             func_lua_sethook;
    del_lua_setlocal            func_lua_setlocal;
    del_lua_setupvalue          func_lua_setupvalue;

    del_luaL_addlstring         func_luaL_addlstring;
    del_luaL_addstring          func_luaL_addstring;
    del_luaL_addvalue           func_luaL_addvalue;
    del_luaL_argerror           func_luaL_argerror;
    del_luaL_buffinit           func_luaL_buffinit;
    del_luaL_callmeta           func_luaL_callmeta;
    del_luaL_checkany           func_luaL_checkany;
    del_luaL_checkinteger       func_luaL_checkinteger;
    del_luaL_checklstring       func_luaL_checklstring;
    del_luaL_checknumber        func_luaL_checknumber;
    del_luaL_checkoption        func_luaL_checkoption;
    del_luaL_checkstack         func_luaL_checkstack;
    del_luaL_checktype          func_luaL_checktype;
    del_luaL_checkudata         func_luaL_checkudata;
    del_luaL_error              func_luaL_error;
    del_luaL_getmetafield       func_luaL_getmetafield;
    del_luaL_gsub               func_luaL_gsub;
    del_luaL_loadbuffer         func_luaL_loadbuffer;
    del_luaL_loadfile           func_luaL_loadfile;
    del_luaL_loadstring         func_luaL_loadstring;
    del_luaL_newmetatable       func_luaL_newmetatable;
    del_luaL_newstate           func_luaL_newstate;
    del_luaL_openlibs           func_luaL_openlibs;
    del_luaL_optinteger         func_luaL_optinteger;
    del_luaL_optlstring         func_luaL_optlstring;
    del_luaL_optnumber          func_luaL_optnumber;
    del_luaL_prepbuffer         func_luaL_prepbuffer;
    del_luaL_pushresult         func_luaL_pushresult;
    del_luaL_ref                func_luaL_ref;
    del_luaL_register           func_luaL_register;
    del_luaL_typerror           func_luaL_typerror;
    del_luaL_unref              func_luaL_unref;
    del_luaL_where              func_luaL_where;

} LuaPluginInterface;

extern LuaPluginInterface* g_pLuaPluginInterface;

#define lua_atpanic             g_pLuaPluginInterface->func_lua_atpanic
#define lua_call                g_pLuaPluginInterface->func_lua_call
#define lua_checkstack          g_pLuaPluginInterface->func_lua_checkstack
#define lua_close               g_pLuaPluginInterface->func_lua_close
#define lua_concat              g_pLuaPluginInterface->func_lua_concat
#define lua_cpcall              g_pLuaPluginInterface->func_lua_cpcall
#define lua_createtable         g_pLuaPluginInterface->func_lua_createtable
#define lua_dump                g_pLuaPluginInterface->func_lua_dump
#define lua_equal               g_pLuaPluginInterface->func_lua_equal
#define lua_error               g_pLuaPluginInterface->func_lua_error
#define lua_gc                  g_pLuaPluginInterface->func_lua_gc
#define lua_getallocf           g_pLuaPluginInterface->func_lua_getallocf
#define lua_getfenv             g_pLuaPluginInterface->func_lua_getfenv
#define lua_getfield            g_pLuaPluginInterface->func_lua_getfield
#define lua_getmetatable        g_pLuaPluginInterface->func_lua_getmetatable
#define lua_gettable            g_pLuaPluginInterface->func_lua_gettable
#define lua_gettop              g_pLuaPluginInterface->func_lua_gettop
#define lua_insert              g_pLuaPluginInterface->func_lua_insert
#define lua_iscfunction         g_pLuaPluginInterface->func_lua_iscfunction
#define lua_isnumber            g_pLuaPluginInterface->func_lua_isnumber
#define lua_isstring            g_pLuaPluginInterface->func_lua_isstring
#define lua_isuserdata          g_pLuaPluginInterface->func_lua_isuserdata
#define lua_lessthan            g_pLuaPluginInterface->func_lua_lessthan
#define lua_load                g_pLuaPluginInterface->func_lua_load
#define lua_newstate            g_pLuaPluginInterface->func_lua_newstate
#define lua_newthread           g_pLuaPluginInterface->func_lua_newthread
#define lua_newuserdata         g_pLuaPluginInterface->func_lua_newuserdata
#define lua_next                g_pLuaPluginInterface->func_lua_next
#define lua_objlen              g_pLuaPluginInterface->func_lua_objlen
#define lua_pcall               g_pLuaPluginInterface->func_lua_pcall
#define lua_pushboolean         g_pLuaPluginInterface->func_lua_pushboolean
#define lua_pushcclosure        g_pLuaPluginInterface->func_lua_pushcclosure
#define lua_pushfstring         g_pLuaPluginInterface->func_lua_pushfstring
#define lua_pushinteger         g_pLuaPluginInterface->func_lua_pushinteger
#define lua_pushlightuserdata   g_pLuaPluginInterface->func_lua_pushlightuserdata
#define lua_pushlstring         g_pLuaPluginInterface->func_lua_pushlstring
#define lua_pushnil             g_pLuaPluginInterface->func_lua_pushnil
#define lua_pushnumber          g_pLuaPluginInterface->func_lua_pushnumber
#define lua_pushstring          g_pLuaPluginInterface->func_lua_pushstring
#define lua_pushthread          g_pLuaPluginInterface->func_lua_pushthread
#define lua_pushvalue           g_pLuaPluginInterface->func_lua_pushvalue
#define lua_pushvfstring        g_pLuaPluginInterface->func_lua_pushvfstring
#define lua_rawequal            g_pLuaPluginInterface->func_lua_rawequal
#define lua_rawget              g_pLuaPluginInterface->func_lua_rawget
#define lua_rawgeti             g_pLuaPluginInterface->func_lua_rawgeti
#define lua_rawset              g_pLuaPluginInterface->func_lua_rawset
#define lua_rawseti             g_pLuaPluginInterface->func_lua_rawseti
#define lua_remove              g_pLuaPluginInterface->func_lua_remove
#define lua_replace             g_pLuaPluginInterface->func_lua_replace
#define lua_resume              g_pLuaPluginInterface->func_lua_resume
#define lua_setallocf           g_pLuaPluginInterface->func_lua_setallocf
#define lua_setfenv             g_pLuaPluginInterface->func_lua_setfenv
#define lua_setfield            g_pLuaPluginInterface->func_lua_setfield
#define lua_setmetatable        g_pLuaPluginInterface->func_lua_setmetatable
#define lua_settable            g_pLuaPluginInterface->func_lua_settable
#define lua_settop              g_pLuaPluginInterface->func_lua_settop
#define lua_status              g_pLuaPluginInterface->func_lua_status
#define lua_toboolean           g_pLuaPluginInterface->func_lua_toboolean
#define lua_tocfunction         g_pLuaPluginInterface->func_lua_tocfunction
#define lua_tointeger           g_pLuaPluginInterface->func_lua_tointeger
#define lua_tolstring           g_pLuaPluginInterface->func_lua_tolstring
#define lua_tonumber            g_pLuaPluginInterface->func_lua_tonumber
#define lua_topointer           g_pLuaPluginInterface->func_lua_topointer
#define lua_tothread            g_pLuaPluginInterface->func_lua_tothread
#define lua_touserdata          g_pLuaPluginInterface->func_lua_touserdata
#define lua_type                g_pLuaPluginInterface->func_lua_type
#define lua_typename            g_pLuaPluginInterface->func_lua_typename
#define lua_xmove               g_pLuaPluginInterface->func_lua_xmove
#define lua_yield               g_pLuaPluginInterface->func_lua_yield

#define lua_gethook             g_pLuaPluginInterface->func_lua_gethook
#define lua_gethookcount        g_pLuaPluginInterface->func_lua_gethookcount
#define lua_gethookmask         g_pLuaPluginInterface->func_lua_gethookmask
#define lua_getinfo             g_pLuaPluginInterface->func_lua_getinfo
#define lua_getlocal            g_pLuaPluginInterface->func_lua_getlocal
#define lua_getstack            g_pLuaPluginInterface->func_lua_getstack
#define lua_getupvalue          g_pLuaPluginInterface->func_lua_getupvalue
#define lua_sethook             g_pLuaPluginInterface->func_lua_sethook
#define lua_setlocal            g_pLuaPluginInterface->func_lua_setlocal
#define lua_setupvalue          g_pLuaPluginInterface->func_lua_setupvalue

#define luaL_addlstring         g_pLuaPluginInterface->func_luaL_addlstring
#define luaL_addstring          g_pLuaPluginInterface->func_luaL_addstring
#define luaL_addvalue           g_pLuaPluginInterface->func_luaL_addvalue
#define luaL_argerror           g_pLuaPluginInterface->func_luaL_argerror
#define luaL_buffinit           g_pLuaPluginInterface->func_luaL_buffinit
#define luaL_callmeta           g_pLuaPluginInterface->func_luaL_callmeta
#define luaL_checkany           g_pLuaPluginInterface->func_luaL_checkany
#define luaL_checkinteger       g_pLuaPluginInterface->func_luaL_checkinteger
#define luaL_checklstring       g_pLuaPluginInterface->func_luaL_checklstring
#define luaL_checknumber        g_pLuaPluginInterface->func_luaL_checknumber
#define luaL_checkoption        g_pLuaPluginInterface->func_luaL_checkoption
#define luaL_checkstack         g_pLuaPluginInterface->func_luaL_checkstack
#define luaL_checktype          g_pLuaPluginInterface->func_luaL_checktype
#define luaL_checkudata         g_pLuaPluginInterface->func_luaL_checkudata
#define luaL_error              g_pLuaPluginInterface->func_luaL_error
#define luaL_getmetafield       g_pLuaPluginInterface->func_luaL_getmetafield
#define luaL_gsub               g_pLuaPluginInterface->func_luaL_gsub
#define luaL_loadbuffer         g_pLuaPluginInterface->func_luaL_loadbuffer
#define luaL_loadfile           g_pLuaPluginInterface->func_luaL_loadfile
#define luaL_loadstring         g_pLuaPluginInterface->func_luaL_loadstring
#define luaL_newmetatable       g_pLuaPluginInterface->func_luaL_newmetatable
#define luaL_newstate           g_pLuaPluginInterface->func_luaL_newstate
#define luaL_openlibs           g_pLuaPluginInterface->func_luaL_openlibs
#define luaL_optinteger         g_pLuaPluginInterface->func_luaL_optinteger
#define luaL_optlstring         g_pLuaPluginInterface->func_luaL_optlstring
#define luaL_optnumber          g_pLuaPluginInterface->func_luaL_optnumber
#define luaL_prepbuffer         g_pLuaPluginInterface->func_luaL_prepbuffer
#define luaL_pushresult         g_pLuaPluginInterface->func_luaL_pushresult
#define luaL_ref                g_pLuaPluginInterface->func_luaL_ref
#define luaL_register           g_pLuaPluginInterface->func_luaL_register
#define luaL_typerror           g_pLuaPluginInterface->func_luaL_typerror
#define luaL_unref              g_pLuaPluginInterface->func_luaL_unref
#define luaL_where              g_pLuaPluginInterface->func_luaL_where

#endif

#define lua_getglobal(L,s)	lua_getfield(L, LUA_GLOBALSINDEX, (s))
#define lua_isboolean(L,n)	(lua_type(L, (n)) == LUA_TBOOLEAN)
#define lua_isfunction(L,n)	(lua_type(L, (n)) == LUA_TFUNCTION)
#define lua_islightuserdata(L,n)	(lua_type(L, (n)) == LUA_TLIGHTUSERDATA)
#define lua_isnil(L,n)		(lua_type(L, (n)) == LUA_TNIL)
#define lua_isnone(L,n)		(lua_type(L, (n)) == LUA_TNONE)
#define lua_isnoneornil(L, n)	(lua_type(L, (n)) <= 0)
#define lua_istable(L,n)	(lua_type(L, (n)) == LUA_TTABLE)
#define lua_isthread(L,n)	(lua_type(L, (n)) == LUA_TTHREAD)
#define lua_newtable(L)		lua_createtable(L, 0, 0)
#define lua_pop(L,n)		lua_settop(L, -(n)-1)
#define lua_pushcfunction(L,f)	lua_pushcclosure(L, (f), 0)
#define lua_pushliteral(L, s)	\
	lua_pushlstring(L, "" s, (sizeof(s)/sizeof(char))-1)
#define lua_setglobal(L,s)	lua_setfield(L, LUA_GLOBALSINDEX, (s))
#define lua_register(L,n,f) (lua_pushcfunction(L, (f)), lua_setglobal(L, (n)))
#define lua_tostring(L,i)	lua_tolstring(L, (i), NULL)
#define lua_upvalueindex(i)	(LUA_GLOBALSINDEX-(i))

#define luaL_addchar(B,c) \
  ((void)((B)->p < ((B)->buffer+LUAL_BUFFERSIZE) || luaL_prepbuffer(B)), \
   (*(B)->p++ = (char)(c)))
#define luaL_addsize(B,n)	((B)->p += (n))
#define luaL_argcheck(L, cond,numarg,extramsg)	\
		((void)((cond) || luaL_argerror(L, (numarg), (extramsg))))
#define luaL_checkint(L,n)	((int)luaL_checkinteger(L, (n)))
#define luaL_checklong(L,n)	((long)luaL_checkinteger(L, (n)))
#define luaL_checkstring(L,n)	(luaL_checklstring(L, (n), NULL))
#define luaL_dofile(L, fn) \
	(luaL_loadfile(L, fn) || lua_pcall(L, 0, LUA_MULTRET, 0))
#define luaL_dostring(L, s) \
	(luaL_loadstring(L, s) || lua_pcall(L, 0, LUA_MULTRET, 0))
#define luaL_getmetatable(L,n)	(lua_getfield(L, LUA_REGISTRYINDEX, (n)))
#define luaL_optint(L,n,d)	((int)luaL_optinteger(L, (n), (d)))
#define luaL_optlong(L,n,d)	((long)luaL_optinteger(L, (n), (d)))
#define luaL_optstring(L,n,d)	(luaL_optlstring(L, (n), (d), NULL))
#define luaL_typename(L,i)	lua_typename(L, lua_type(L,(i)))
#define abs_index(L, i) \
  ((i) > 0 || (i) <= LUA_REGISTRYINDEX ? (i) : lua_gettop(L) + (i) + 1)

#endif // HEADER_LUA_IMPORT
