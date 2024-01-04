// LuaJsonNative.cpp : 定义 DLL 应用程序的导出函数。
//

#include <limits>
#include <cstdio>
#include <vector>
#include <algorithm>

#include "LuaImport.h"

#include "rapidjson/document.h"
#include "rapidjson/encodedstream.h"
#include "rapidjson/error/en.h"
#include "rapidjson/error/error.h"
#include "rapidjson/filereadstream.h"
#include "rapidjson/filewritestream.h"
#include "rapidjson/prettywriter.h"
#include "rapidjson/rapidjson.h"
#include "rapidjson/reader.h"
#include "rapidjson/schema.h"
#include "rapidjson/stringbuffer.h"
#include "rapidjson/writer.h"

using namespace rapidjson;

#define LRKEY_TYPE_TRANS    ((void*)2303)
#define LRKEY_TARGET        ((void*)2201)

struct ToLuaHandler {
    explicit ToLuaHandler(lua_State* aL) : L(aL) { stack_.reserve(32); }

    bool NullObj()
    {
        lua_getglobal(L, "clr");
        if (!lua_istable(L, -1))
        {
            lua_pop(L, 1);
            return false;
        }
        lua_getfield(L, -1, "null");
        lua_remove(L, -2);
        if (!lua_istable(L, -1))
        {
            lua_pop(L, 1);
            return false;
        }
        return true;
    }

    bool Null() {
        if (!NullObj())
        {
            lua_pushnil(L);
        }
        context_.submit(L);
        return true;
    }
    bool Bool(bool b) {
        lua_pushboolean(L, b);
        context_.submit(L);
        return true;
    }
    bool Int(int i) {
        lua_pushinteger(L, i);
        context_.submit(L);
        return true;
    }
    bool Uint(unsigned u) {
        if (sizeof(lua_Integer) > sizeof(unsigned int) || u <= static_cast<unsigned>(std::numeric_limits<lua_Integer>::max()))
            lua_pushinteger(L, static_cast<lua_Integer>(u));
        else
            lua_pushnumber(L, static_cast<lua_Number>(u));
        context_.submit(L);
        return true;
    }
    bool Int64(int64_t i) {
        if (sizeof(lua_Integer) >= sizeof(int64_t) || (i <= std::numeric_limits<lua_Integer>::max() && i >= std::numeric_limits<lua_Integer>::min()))
            lua_pushinteger(L, static_cast<lua_Integer>(i));
        else
            lua_pushnumber(L, static_cast<lua_Number>(i));
        context_.submit(L);
        return true;
    }
    bool Uint64(uint64_t u) {
        if (sizeof(lua_Integer) > sizeof(uint64_t) || u <= static_cast<uint64_t>(std::numeric_limits<lua_Integer>::max()))
            lua_pushinteger(L, static_cast<lua_Integer>(u));
        else
            lua_pushnumber(L, static_cast<lua_Number>(u));
        context_.submit(L);
        return true;
    }
    bool Double(double d) {
        lua_pushnumber(L, static_cast<lua_Number>(d));
        context_.submit(L);
        return true;
    }
    bool RawNumber(const char* str, rapidjson::SizeType length, bool copy) {
        lua_getglobal(L, "tonumber");
        lua_pushlstring(L, str, length);
        lua_call(L, 1, 1);
        context_.submit(L);
        return true;
    }
    bool String(const char* str, rapidjson::SizeType length, bool copy) {
        lua_pushlstring(L, str, length);
        context_.submit(L);
        return true;
    }
    bool StartObject() {
        if (!lua_checkstack(L, 2)) // make sure there's room on the stack
            return false;

        lua_createtable(L, 0, 0);							// [..., object]

        // mark as object.
        luaL_getmetatable(L, "json.object");	//[..., object, json.object]
        lua_setmetatable(L, -2);							// [..., object]

        stack_.push_back(context_);
        context_ = Ctx::Object();
        return true;
    }
    bool Key(const char* str, rapidjson::SizeType length, bool copy) const {
        lua_pushlstring(L, str, length);
        return true;
    }
    bool EndObject(rapidjson::SizeType memberCount) {
        context_ = stack_.back();
        stack_.pop_back();
        context_.submit(L);
        return true;
    }
    bool StartArray() {
        if (!lua_checkstack(L, 2)) // make sure there's room on the stack
            return false;

        lua_createtable(L, 0, 0);

        // mark as array.
        luaL_getmetatable(L, "json.array");  //[..., array, json.array]
        lua_setmetatable(L, -2); // [..., array]

        stack_.push_back(context_);
        context_ = Ctx::Array();
        return true;
    }
    bool EndArray(rapidjson::SizeType elementCount) {
        assert(elementCount == context_.index_);
        context_ = stack_.back();
        stack_.pop_back();
        context_.submit(L);
        return true;
    }
private:


    struct Ctx {
        Ctx() : index_(0), fn_(&topFn) {}
        Ctx(const Ctx& rhs) : index_(rhs.index_), fn_(rhs.fn_)
        {
        }
        const Ctx& operator=(const Ctx& rhs) {
            if (this != &rhs) {
                index_ = rhs.index_;
                fn_ = rhs.fn_;
            }
            return *this;
        }
        static Ctx Object() {
            return Ctx(&objectFn);
        }
        static Ctx Array()
        {
            return Ctx(&arrayFn);
        }
        void submit(lua_State* L)
        {
            fn_(L, this);
        }

        int index_;
        void(*fn_)(lua_State* L, Ctx* ctx);
    private:
        explicit Ctx(void(*f)(lua_State* L, Ctx* ctx)) : index_(0), fn_(f) {}


        static void objectFn(lua_State* L, Ctx* ctx)
        {
            lua_rawset(L, -3);
        }

        static void arrayFn(lua_State* L, Ctx* ctx)
        {
            lua_rawseti(L, -2, ++ctx->index_);
        }
        static void topFn(lua_State* L, Ctx* ctx)
        {
        }
    };

    lua_State* L;
    std::vector < Ctx > stack_;
    Ctx context_;
};

template<typename Stream>
int decode(lua_State* L, Stream* s)
{
    int top = lua_gettop(L);
    ToLuaHandler handler(L);
    Reader reader;
    ParseResult r = reader.Parse(*s, handler);

    if (!r) {
        lua_settop(L, top);
        lua_pushnil(L);
        lua_pushfstring(L, "%s (%d)", GetParseError_En(r.Code()), r.Offset());
        return 2;
    }

    return 1;
}

static int json_decode(lua_State* L)
{
    size_t len = 0;
    const char* contents = luaL_checklstring(L, 1, &len);
    StringStream s(contents);
    return decode(L, &s);
}

class Encoder {

    struct Key
    {
        Key(const char* k, SizeType l) : key(k), size(l) {}
        bool operator<(const Key& rhs) const {
            return strcmp(key, rhs.key) < 0;
        }
        const char* key;
        SizeType size;
    };

    static inline bool hasJsonType(lua_State* L, int idx, bool& isarray)
    {
        bool has = false;
        if (lua_getmetatable(L, idx)) {
            // [metatable]
            lua_getfield(L, -1, "__jsontype"); // [metatable, metatable.__jsontype]
            if (lua_isstring(L, -1))
            {
                size_t len;
                const char* s = lua_tolstring(L, -1, &len);
                isarray = strncmp(s, "array", 6) == 0;
                has = true;
            }
            lua_pop(L, 2); // []
        }

        return has;
    }
    static inline bool isarray(lua_State* L, int idx, bool empty_table_as_array = false) {
        bool arr = false;
        if (hasJsonType(L, idx, arr)) // any table with a meta field __jsontype set to 'array' are arrays
            return arr;

        lua_pushvalue(L, idx);
        lua_pushnil(L);
        if (lua_next(L, -2) != 0) {
            lua_pop(L, 3);

            return lua_objlen(L, idx) > 0; // any non empty table has length > 0 are treat as array.
        }

        lua_pop(L, 1);
        // Now it comes empty table
        return empty_table_as_array;
    }
    static inline bool isobj(lua_State* L, int idx)
    {
        int index = abs_index(L, idx);
        lua_pushlightuserdata(L, LRKEY_TYPE_TRANS); // #trans
        lua_gettable(L, index); // trans
        bool isobj = lua_islightuserdata(L, -1);
        lua_pop(L, 1);
        if (isobj)
        {
            lua_pushlightuserdata(L, LRKEY_TARGET); // #tar
            lua_rawget(L, index); // tar
            isobj = !lua_isnoneornil(L, -1);
            lua_pop(L, 1);
        }
        return isobj;
    }
    static inline bool isinteger(lua_State* L, int idx, int64_t* out = NULL)
    {
        double intpart;
        if (std::modf(lua_tonumber(L, idx), &intpart) == 0.0)
        {
            if (std::numeric_limits<lua_Integer>::min() <= intpart
                && intpart <= std::numeric_limits<lua_Integer>::max())
            {
                if (out)
                    *out = static_cast<int64_t>(intpart);
                return true;
            }
        }
        return false;
    }
    static inline bool optboolfield(lua_State* L, int idx, const char* name, bool def)
    {
        bool v = def;
        lua_getfield(L, idx, name);  // [field]
        if (!lua_isnoneornil(L, -1))
            v = lua_toboolean(L, -1) != 0;;
        lua_pop(L, 1);

        return v;
    }
    static inline int optintfield(lua_State* L, int idx, const char* name, int def)
    {
        int v = def;
        lua_getfield(L, idx, name);  // [field]
        if (lua_isnumber(L, -1))
            v = static_cast<int>(lua_tointeger(L, -1));
        lua_pop(L, 1);
        return v;
    }

    bool pretty;
    bool sort_keys;
    bool empty_table_as_array;
    int max_depth;
    static const int MAX_DEPTH_DEFAULT = 128;
public:
    Encoder(lua_State*L, int opt) : pretty(false), sort_keys(false), empty_table_as_array(false), max_depth(MAX_DEPTH_DEFAULT), error(0)
    {
        if (!lua_istable(L, opt))
            return;

        pretty = optboolfield(L, opt, "pretty", false);
        sort_keys = optboolfield(L, opt, "sort_keys", false);
        empty_table_as_array = optboolfield(L, opt, "empty_table_as_array", false);
        max_depth = optintfield(L, opt, "max_depth", MAX_DEPTH_DEFAULT);
    }

    const char* error;

private:
    template<typename Writer>
    void encodeValue(lua_State* L, Writer* writer, int idx, int depth = 0)
    {
        size_t len;
        const char* s;
        int64_t integer;
        int t = lua_type(L, idx);
        switch (t) {
        case LUA_TBOOLEAN:
            writer->Bool(lua_toboolean(L, idx) != 0);
            return;
        case LUA_TNUMBER:
            if (isinteger(L, idx, &integer))
                writer->Int64(integer);
            else
            {
                double d = lua_tonumber(L, idx);
                if (d != d)
                { // nan
                    writer->String("NaN");
                    error = "can not encode NaN.";
                }
                else if (d == 1e+300 * 1e+300)
                { // +Inf
                    writer->String("Inf");
                    error = "can not encode Inf.";
                }
                else if (d == -(1e+300 * 1e+300))
                { // +Inf
                    writer->String("-Inf");
                    error = "can not encode -Inf.";
                }
                //else if (!writer->Double(d))
                //{
                //    writer->String("null");
                //    error = "error while encode double value.";
                //}
                else
                {
                    writer->Double(d);
                }
            }
            return;
        case LUA_TSTRING:
            s = lua_tolstring(L, idx, &len);
            writer->String(s, static_cast<SizeType>(len));
            return;
        case LUA_TTABLE:
            // TODO: should we encode clr.null?
            return encodeTable(L, writer, idx, depth + 1);
        case LUA_TNIL:
            writer->Null();
            return;
        case LUA_TFUNCTION:
            error = "unsupported type: func";
            writer->Null();
            return;
        case LUA_TLIGHTUSERDATA: // fall thought
            error = "unsupported type: light ud";
            writer->Null();
            return;
        case LUA_TUSERDATA: // fall thought
            error = "unsupported type: ud";
            writer->Null();
            return;
        case LUA_TTHREAD: // fall thought
            error = "unsupported type: thread";
            writer->Null();
            return;
        case LUA_TNONE: // fall thought
            writer->Null();
            return;
        default:
            error = "unsupported type: unknown";
            writer->Null();
            return;
        }
    }

    template<typename Writer>
    void encodeTable(lua_State* L, Writer* writer, int idx, int depth)
    {
        if (depth > max_depth)
        {
            error = "nested too depth";
            writer->Null();
            return;
        }

        if (!lua_checkstack(L, 4)) // requires at least 4 slots in stack: table, key, value, key
        {
            error = "stack overflow";
            writer->Null();
            return;
        }

        lua_pushvalue(L, idx); // [table]
        if (isobj(L, -1))
        {
            error = "Try to encode an obj to json.";
            writer->Null();
            lua_pop(L, 1); // []
            return;
        }

        if (isarray(L, -1, empty_table_as_array))
        {
            encodeArray(L, writer, depth);
            lua_pop(L, 1); // []
            return;
        }

        // is object.
        if (!sort_keys)
        {
            encodeObject(L, writer, depth);
            lua_pop(L, 1); // []
            return;
        }

        lua_pushnil(L); // [table, nil]
        std::vector<Key> keys;

        while (lua_next(L, -2))
        {
            // [table, key, value]

            if (lua_type(L, -2) == LUA_TSTRING)
            {
                size_t len = 0;
                const char* key = lua_tolstring(L, -2, &len);
                keys.push_back(Key(key, static_cast<SizeType>(len)));
            }

            // pop value, leaving original key
            lua_pop(L, 1);
            // [table, key]
        }
        // [table]
        encodeObject(L, writer, depth, keys);
        lua_pop(L, 1);
    }

    template<typename Writer>
    void encodeObject(lua_State* L, Writer* writer, int depth)
    {
        writer->StartObject();

        // [table]
        lua_pushnil(L); // [table, nil]
        while (lua_next(L, -2))
        {
            // [table, key, value]
            if (lua_type(L, -2) == LUA_TSTRING)
            {
                size_t len = 0;
                const char* key = lua_tolstring(L, -2, &len);
                writer->Key(key, static_cast<SizeType>(len));
                encodeValue(L, writer, -1, depth);
            }

            // pop value, leaving original key
            lua_pop(L, 1);
            // [table, key]
        }
        // [table]
        writer->EndObject();
    }

    template<typename Writer>
    void encodeObject(lua_State* L, Writer* writer, int depth, std::vector<Key> &keys)
    {
        // [table]
        writer->StartObject();

        std::sort(keys.begin(), keys.end());

        std::vector<Key>::const_iterator i = keys.begin();
        std::vector<Key>::const_iterator e = keys.end();
        for (; i != e; ++i)
        {
            writer->Key(i->key, static_cast<SizeType>(i->size));
            lua_pushlstring(L, i->key, i->size); // [table, key]
            lua_gettable(L, -2); // [table, value]
            encodeValue(L, writer, -1, depth);
            lua_pop(L, 1); // [table]
        }
        // [table]
        writer->EndObject();
    }

    template<typename Writer>
    void encodeArray(lua_State* L, Writer* writer, int depth)
    {
        // [table]
        writer->StartArray();
        int MAX = static_cast<int>(lua_objlen(L, -1)); // lua_rawlen always returns value >= 0
        for (int n = 1; n <= MAX; ++n)
        {
            lua_rawgeti(L, -1, n); // [table, element]
            encodeValue(L, writer, -1, depth);
            lua_pop(L, 1); // [table]
        }
        writer->EndArray();
        // [table]
    }

public:
    template<typename Stream>
    void encode(lua_State* L, Stream* s, int idx)
    {
        if (pretty)
        {
            PrettyWriter<Stream> writer(*s);
            encodeValue(L, &writer, idx);
        }
        else
        {
            Writer<Stream> writer(*s);
            encodeValue(L, &writer, idx);
        }
    }
};

static int json_encode(lua_State* L)
{
    try {
        Encoder encode(L, 2);
        StringBuffer s;
        encode.encode(L, &s, 1);
        lua_pushlstring(L, s.GetString(), s.GetSize());
        if (encode.error)
        {
            lua_pushstring(L, encode.error);
            return 2;
        }
        return 1;
    }
    catch (...) {
        luaL_error(L, "error while encoding.");
    }
    return 0;
}

#if _WIN32
#define EXPORT_API __declspec(dllexport)
#else
#define EXPORT_API
#endif

extern "C"
{
    EXPORT_API void InitLuaJsonPlugin(void* l)
    {
        lua_newtable(l);
        lua_pushvalue(l, -1);
        lua_setglobal(l, "json");
        lua_pushstring(l, "encode");
        lua_pushcfunction(l, json_encode);
        lua_settable(l, -3);
        lua_pushstring(l, "decode");
        lua_pushcfunction(l, json_decode);
        lua_settable(l, -3);
        lua_pop(l, 1);
    }
}
