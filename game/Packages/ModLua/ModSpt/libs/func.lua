
local tonumber_ = tonumberraw or tonumber
tonumberraw = tonumber_
--[[--

Convert to number.

@param mixed v
@return number

]]
function tonumber(v, base)
    return tonumber_(v, base) or 0
end

if clr then
    function tolong(str)
        if type(str) == "string" then
            clr.System.Int64.TryParse(str, 0)
            return clr.lastlong()
        elseif clr.isobj(str) then
            return clr.as(str, clr.System.Int64)
        else
            local num = tonumber(str)
            return clr.as(num, clr.System.Int64)
        end
    end

    function toulong(str)
        if type(str) == "string" then
            clr.System.UInt64.TryParse(str, 0)
            return clr.lastulong()
        elseif clr.isobj(str) then
            return clr.as(str, clr.System.UInt64)
        else
            local num = tonumber(str)
            return clr.as(num, clr.System.UInt64)
        end
    end
end

local combinedFuncReg = {}
setmetatable(combinedFuncReg, {__mode = "k"})

function getInvocationList(func)
    return combinedFuncReg[func]
end

local function funcWithList(funcs)
    local combined = function(...)
        local args = table.pack(...)
        for i = 1, #funcs do
            local func = funcs[i]
            if i == #funcs then
                local results
                xpcall(function()
                    results = table.pack(func(table.unpack(args)))
                end, printe)
                if results then
                    return table.unpack(results)
                else
                    return nil
                end
            else
                -- TODO: combine each result.
                xpcall(function()
                    func(table.unpack(args))
                end, printe)
            end
        end
    end
    combinedFuncReg[combined] = funcs
    return combined
end

function combinefunc(...)
    local funcs = {}
    local cnt = select('#', ...)
    for i = 1, cnt do
        local func = select(i, ...)
        local list = getInvocationList(func)
        if list and #list > 0 then
            for _, item in ipairs(list) do
                funcs[#funcs + 1] = item
            end
        else
            funcs[#funcs + 1] = func
        end
    end

    return funcWithList(funcs)
end

function uncombinefunc(mainfunc, subfunc)
    local list = getInvocationList(mainfunc)
    if not list then
        return mainfunc
    end
    local funcs = {}
    for _, item in ipairs(list) do
        if item ~= subfunc then
            funcs[#funcs + 1] = item
        end
    end
    if #funcs <= 0 then
        return nil
    else
        return funcWithList(funcs)
    end
end

--[[--

Convert to integer.

@param mixed v
@return number(integer)

]]
function toint(v)
    return math.round(tonumber(v))
end

--[[--

Convert to boolean.

@param mixed v
@return boolean

]]
local falsestrings = {
    [""] = true,
    ["n"] = true,
    ["no"] = true,
    ["f"] = true,
    ["false"] = true,
}
function tobool(v)
    if not v then
        return false
    end
    local tv = type(v)
    if clr then
        if clr.isobj(v) then
            return clr.unwrap(clr.as(v, clr.System.Boolean))
        elseif tv == "userdata" then
            return v ~= clr.topointer(0)
        end
    end
    if tv == "boolean" then
        return v
    elseif tv == "string" then
        local nv = tonumber_(v)
        if nv then
            return nv ~= 0
        end
        local lower = string.lower(v)
        if falsestrings[lower] then
            return false
        else
            return true
        end
    else
        local nv = tonumber_(v)
        return nv == nil or nv ~= 0
    end
end

--[[--

Convert to table.

@param mixed v
@return table

]]
function totable(v)
    if table.isudtable(v) then v = table.getraw(v)
    elseif type(v) ~= "table" then v = {}
    end
    return v
end

--[[--

Returns a formatted version of its variable number of arguments following the description given in its first argument (which must be a string). string.format() alias.

@param string format
@param mixed ...
@return string

]]
function format(...)
    return string.format(...)
end

--[[--

Creating a copy of an table with fully replicated properties.

**Usage:**

    -- Creating a reference of an table:
    local t1 = {a = 1, b = 2}
    local t2 = t1
    t2.b = 3    -- t1 = {a = 1, b = 3} <-- t1.b changed

    -- Createing a copy of an table:
    local t1 = {a = 1, b = 2}
    local t2 = clone(t1)
    t2.b = 3    -- t1 = {a = 1, b = 2} <-- t1.b no change


@param mixed object
@return mixed

]]
function clone(object)
    local lookup_table = {}
    local function _copy(object)
        if table.isudtable(object) then
            return _copy(table.getraw(object))
        end
        if type(object) ~= "table" then
            return object
        elseif lookup_table[object] then
            return lookup_table[object]
        end
        local new_table = {}
        lookup_table[object] = new_table
        for key, value in pairs(object) do
            new_table[_copy(key)] = _copy(value)
        end
        return setmetatable(new_table, getmetatable(object))
    end
    return _copy(object)
end

function shallowClone(object)
    if table.isudtable(object) then
        return shallowClone(table.getraw(object))
    end
    if type(object) ~= "table" then
        return object
    end
    local new_table = {}
    for key, value in pairs(object) do
        new_table[key] = value
    end
    return setmetatable(new_table, getmetatable(object))
end

function cloneData(data)
    local function _clone(tab)
        local cloned = {}
        for k, v in pairs(tab) do
            if type(v) == "table" or table.isudtable(v) then
                cloned[k] = _clone(v)
            else
                cloned[k] = v
            end
        end
        return cloned
    end
    if type(data) == "table" or table.isudtable(data) then
        return _clone(data)
    else
        return data
    end
end

-- 判断val是否在t里
function isInArray(t, val)
    for _, v in ipairs(t) do
        if v == val then
            return true
        end
    end
    return false
end

local function class_instancebase(self)
    local cached = rawget(self, "__instance_base")
    if cached ~= nil then
        if not cached then
            return nil
        else
            return cached
        end
    end

    local curtype = rawget(self, "__instance_type")
    if not curtype then
        curtype = self
    end

    local super = curtype.super
    if type(super) == "table" then
        local basei = {
            ["__instance_type"] = super,
            class = super,
            super = super.super,
        }
        local basem = {
            __index = function(tab, k)
                local v = rawget(self, k)
                if v ~= nil then
                    return v
                end
                v = super[k]
                return v
            end,
            __newindex = function(tab, k, v)
                rawset(self, k, v)
            end,
        }
        setmetatable(basei, basem)
        rawset(self, "__instance_base", basei)
        return basei
    else
        rawset(self, "__instance_base", false)
        return nil
    end
end

local function class_findfunction_inclass(class, func)
    for k, v in pairs(class) do
        if v == func then
            return k
        end
    end
end

local function class_findfunction_inparent(class, func, name)
    if class then
        if not name then
            while class do
                name = class_findfunction_inclass(class, func)
                if name then
                    break
                end
                class = class.super            
            end
            if not name then
                return
            end
        else
            while class do
                if class[name] == func then
                    break
                end
                class = class.super
            end
            if not class then
                return
            end
        end
        while class do
            local super = class.super
            if not super then
                break
            end
            if super[name] ~= func then
                break
            end
            class = super
        end
        return class, name
    end
end

local function class_findfunction(self, func, name)
    local class = self.class
    return class_findfunction_inparent(class, func, name)
end

local class_instancecallbasefunc_name
local function class_instancecallbasefunc(self, name, ...)
    local old_class_instancecallbasefunc_name = class_instancecallbasefunc_name

    local info = debug.getinfo(2)
    local func = info.func
    local caller_name
    if info.name == "__base_func" then
        caller_name = old_class_instancecallbasefunc_name
    end
    local caller_class = self.class
    if caller_name == nil then
        caller_name = info.name
        caller_class = class_findfunction(self, func, caller_name)
        if not caller_class then
            caller_class = class_findfunction(self, func, nil)
        end
    else
        caller_class = class_findfunction(self, func, caller_name)
    end

    local results
    if caller_class then
        local thisfunc = caller_class[name]
        local super = caller_class.super
        while super do
            if super[name] ~= thisfunc then
                break
            end
            super = super.super
        end
        if super then
            class_instancecallbasefunc_name = name
            local __base_func = super[name]
            if type(__base_func) == "function" then
                results = table.pack(__base_func(self, ...))
            end
        end
    end

    class_instancecallbasefunc_name = old_class_instancecallbasefunc_name
    if results then
        return table.unpack(results)
    end
end

local function class_instancecallbase(self, ...)
    old_class_instancecallbasefunc_name = class_instancecallbasefunc_name

    local info = debug.getinfo(2)
    local func = info.func
    local caller_name
    if info.name == "__base_func" then
        caller_name = old_class_instancecallbasefunc_name
    end
    local caller_class = self.class
    if caller_name == nil then
        caller_name = info.name
        caller_class = class_findfunction(self, func, caller_name)
        if not caller_class then
            caller_class, caller_name = class_findfunction(self, func, nil)
        end
    else
        caller_class = class_findfunction(self, func, caller_name)
    end

    local results
    if caller_class then
        local super = caller_class.super
        if super then
            class_instancecallbasefunc_name = caller_name
            local __base_func = super[caller_name]
            if type(__base_func) == "function" then
                results = table.pack(__base_func(self, ...))
            end
        end
    end

    class_instancecallbasefunc_name = old_class_instancecallbasefunc_name
    if results then
        return table.unpack(results)
    end
end

function notailcall(...)
    return ...
end

local function class_isSubClassOf(cls, parent)
    if not parent then return false end
    while cls do
        if cls == parent then
            return true
        end
        if type(cls) == "table" then
            cls = cls.super
        end
    end
end

local function class_isTypeOf(inst, parent)
    if not parent then return false end
    local cls = inst.class
    return class_isSubClassOf(cls, parent)
end

function regcinst(instance)
    if clr.LuaBehav.WillAutoDisposeLuaBinding then
        return
    end
    local reg = _G["@cinstreg"]
    if not reg then
        reg = {}
        setmetatable(reg, { __mode = "k" })
        _G["@cinstreg"] = reg
    else
        -- for k, v in pairs(reg) do
        --     if k == clr.null then
        --         --local kc = k.class
        --         for ki, vi in pairs(k) do
        --             if type(ki) ~= "userdata" then
        --                 k[ki] = nil
        --             end
        --         end
        --         reg[k] = nil
        --     end
        -- end
    end
    reg[instance] = true
end

--[[--

Create an class.

**Usage:**

    local Shape = class("Shape")

    -- base class
    function Shape:ctor(shapeName)
        self.shapeName = shapeName
        printf("Shape:ctor(%s)", self.shapeName)
    end

    function Shape:draw()
        printf("draw %s", self.shapeName)
    end

    --

    local Circle = class("Circle", Shape)

    function Circle:ctor()
        Circle.super.ctor(self, "circle")   -- call super-class method
        self.radius = 100
    end

    function Circle:setRadius(radius)
        self.radius = radius
    end

    function Circle:draw()                  -- overrideing super-class method
        printf("draw %s, raidus = %0.2f", self.shapeName, self.raidus)
    end

    --

    local Rectangle = class("Rectangle", Shape)

    function Rectangle:ctor()
        Rectangle.super.ctor(self, "rectangle")
    end

    --

    local circle = Circle.new()             -- output: Shape:ctor(circle)
    circle:setRaidus(200)
    circle:draw()                           -- output: draw circle, radius = 200.00

    local rectangle = Rectangle.new()       -- output: Shape:ctor(rectangle)
    rectangle:draw()                        -- output: draw rectangle


@param string classname
@param table|function super-class
@return table

]]
function class(super, classname)
    local superType = type(super)
    if superType == "string" then
        local classnametype = type(classname)
        if classname == nil or classnametype == "table" or classnametype == "function" then
            local realsuper = classname
            classname = super
            super = realsuper
            superType = classnametype
        end
    end
    local cls

    if superType ~= "function" and superType ~= "table" then
        superType = nil
        super = nil
    end

    if superType == "function" or (super and super.__ctype == 1) then
        -- inherited from native C++ Object
        cls = {}

        if superType == "table" then
            -- copy fields from super
            for k,v in pairs(super) do cls[k] = v end
            cls.__create = super.__create
            cls.super    = super
        else
            cls.__create = super
            cls.ctor = function() end
        end

        cls.__cname = classname
        cls.__ctype = 1

        function cls.new(...)
            local instance = cls.__create(...)
            -- copy fields from class to native object
            for k,v in pairs(cls) do instance[k] = v end
            instance.class = cls
            instance:ctor(...)
            return instance
        end

    else
        -- inherited from Lua Object
        if super then
            cls = clone(super)
            cls.super = super
            if super.__index == super then
                cls.__index = nil
            end
        else
            cls = {ctor = function() end}
        end

        cls.__cname = classname
        cls.__ctype = 2 -- lua
        if not cls.__index then
            cls.__index = cls
        end

        function cls.new(...)
            local instance = setmetatable({}, cls)
            instance.class = cls
            instance:ctor(...)
            return instance
        end
    end

    cls.base = class_instancebase
    cls.callbase = class_instancecallbase
    cls.callbasefunc = class_instancecallbasefunc
    cls.isSubClassOf = class_isSubClassOf
    cls.isTypeOf = class_isTypeOf

    function cls.attach(instance, ...)
        regcinst(instance)
        for k,v in pairs(cls) do instance[k] = v end
        instance.class = cls
        instance:ctor(...)
        return instance
    end

    return cls
end

--[[--

hecks if the given key or index exists in the table.

@param table arr
@param mixed key
@return boolean

]]
function isset(arr, key)
    return (type(arr) == "table" or table.isudtable(arr)) and arr[key] ~= nil
end

--[[--

Rounds a float.

@param number num
@return number(integer)

]]
function math.round(num)
    return math.floor(num + 0.5)
end

--[[--

Count all elements in an table.

@param table t
@return number(integer)

]]
function table.nums(t)
    local count = 0
    for k, v in pairs(t) do
        count = count + 1
    end
    return count
end

--[[--

Return all the keys or a subset of the keys of an table.

**Usage:**

    local t = {a = 1, b = 2, c = 3}
    local keys = table.keys(t)
    -- keys = {"a", "b", "c"}


@param table t
@return table

]]
function table.keys(t)
    local keys = {}
    for k, v in pairs(t) do
        keys[#keys + 1] = k
    end
    return keys
end

--[[--

Return all the values of an table.

**Usage:**

    local t = {a = "1", b = "2", c = "3"}
    local values = table.values(t)
    -- values = {1, 2, 3}


@param table t
@return table

]]
function table.values(t)
    local values = {}
    for k, v in pairs(t) do
        values[#values + 1] = v
    end
    return values
end

--[[--

Merge tables.

**Usage:**

    local dest = {a = 1, b = 2}
    local src  = {c = 3, d = 4}
    table.merge(dest, src)
    -- dest = {a = 1, b = 2, c = 3, d = 4}


@param table dest
@param table src

]]
function table.merge(dest, src)
    for k, v in pairs(src) do
        dest[k] = v
    end
end

function table.deepmerge(dst, src)
    local visited = {}
    local function mergeTable(dst, src)
        visited[src] = dst
        for k, v in pairs(src) do
            if type(v) ~= "table" and not table.isudtable(v) then
                dst[k] = v
            else
                if visited[v] then
                    dst[k] = visited[v]
                else
                    local olddstval = dst[k]
                    if type(olddstval) ~= "table" and not table.isudtable(olddstval) then
                        dst[k] = v
                    else
                        if not table.isreadonly(olddstval) then
                            mergeTable(olddstval, v)
                        end
                    end
                end
            end
        end
    end

    mergeTable(dst, src)
    return dst
end

function table.mergeData(dst, src)
    local function _merge(dst, src)
        if src[""] == "\026" then
            src[""] = nil
            return src
        elseif src[""] == "\027" then
            src[""] = nil
            return cloneData(src)
        -- elseif src[""] == "\024" then
        --     return nil
        else
            if not dst then
                dst = {}
            end
            local isarray = dst[1] ~= nil or type(next(src)) == "number" or src[1] ~= nil or src[0] ~= nil
            local resortarray
            for k, v in pairs(src) do
                if v == "\024" then
                    dst[k] = nil
                    resortarray = true
                else
                    local validkey = true
                    if isarray then
                        if type(k) ~= "number" then
                            validkey = false
                        else
                            if k < #dst and dst[k] == nil then
                                resortarray = true
                            end
                        end
                    end
                    if validkey then
                        if type(v) == "table" or table.isudtable(v) then
                            if isarray and v[""] == "\025" then
                                if v.to then
                                    local val = dst[k]
                                    dst[k] = nil
                                    dst[v.to] = val
                                    resortarray = true
                                end
                            else
                                dst[k] = _merge(dst[k], v)
                            end
                        else
                            dst[k] = v
                        end
                    end
                end
            end
            if isarray and resortarray then
                local plain = {}
                for k, v in pairs(dst) do
                    plain[#plain + 1] = { k = k, v = v }
                    dst[k] = nil
                end
                table.sort(plain, function(a, b) return a.k < b.k end)
                for i, v in ipairs(plain) do
                    dst[i] = v.v
                end
            end
            return dst
        end
    end

    if type(src) == "table" or table.isudtable(src) then
        return _merge(dst, src)
    else
        return src
    end
end

--[[--

Merge arraies.

**Usage:**

    local dest = {1, 2}
    local src  = {3, 4}
    table.imerge(dest, src)
    -- dest = {1, 2, 3, 4}


@param table dest
@param table src

]]
function table.imerge(dest, src)
    for i, v in ipairs(src) do
        table.insert(dest, v)
    end
end

function table.clear(tab)
    for k, v in pairs(tab) do
        tab[k] = nil
    end
end

function table.removeRange(tab, from, to)
    local cnt = #tab
    if cnt >= from then
        local removeCnt = to - from + 1
        for i = from, cnt do
            tab[i] = tab[i + removeCnt]
        end
    end
end

function table.unwrap(tab)
    if clr then
        if clr.isobj(tab) then
            return clr.unwrap(tab)
        elseif type(tab) == "table" then
            local newtab = {}
            for k, v in pairs(tab) do
                newtab[k] = table.unwrap(v)
            end
            return newtab
        else
            return tab
        end
    else
        return tab
    end
end

--[[--

Return formatted string with a comma (",") between every group of thousands.

**Usage:**

    local value = math.comma("232423.234") -- value = "232,423.234"


@param number num
@return string

]]
function string.formatNumberThousands(num)
    local formatted = tostring(tonumber(num))
    while true do
        formatted, k = string.gsub(formatted, "^(-?%d+)(%d%d%d)", '%1,%2')  --Lua assist checked flag
        if k == 0 then break end
    end
    return formatted
end

math.nan = 0/0
math.max_int32 = 0x7FFFFFFF
math.min_int32 = -math.max_int32 - 1
math.eps = 1e-6

function math.isNan(num)
    return num ~= num
end

function math.clamp(num, min, max)
    if num < min then num = min end
    if num > max then num = max end
    return num
end

function math.cmpf(a, b)
    if a < b - math.eps then
        return -1
    elseif a > b + math.eps then
        return 1
    else
        return 0
    end
end

function math.sign(num)
    if num > math.eps then
        return 1
    elseif num < -math.eps then
        return -1
    end
    return 0
end

--[Comment]
--return a < b ? { min = a, max = b } : { min = b, max = a }
function math.range(a, b)
    return a < b and { min = a, max = b } or { min = b, max = a }
end

--[Comment]
--return x >= min and x <= max
function math.isInRange(x, min, max)
    return x >= min and x <= max
end

--[Comment]
--return a random real number in range [min, max)
function math.randomInRange(min, max)
    return min + math.random() * (max - min)
end

--[Comment]
--ceil the number to precision: math.ceilEx(7.44, 0.1) = 7.5
--return math.ceil(number / precision) * precision
function math.ceilEx(number, precision)
    return math.ceil(number / precision) * precision
end

function math.lerp(a, b, bweight)
    bweight = math.clamp(bweight, 0, 1)
    return a * (1 - bweight) + b * bweight
end

function math.isInt(num)
    return type(num) == "number" and num == math.floor(num)
end

-- TODO: why not use next(tab)?
function table.isEmpty(tab)
    if tab == nil then return true end
    if type(tab) ~= 'table' and not table.isudtable(tab) then return false end
    local empty = true
    for k, v in pairs(tab) do
        empty = false
        break
    end
    return empty
end

function table.compare(data1, data2)
    if (type(data1) == 'table' or table.isudtable(data1)) and (type(data2) == 'table' or table.isudtable(data2)) then
        local allKeys = {}
        for k,v in pairs(data1) do
            allKeys[k] = v
        end
        for k,v in pairs(data2) do
            allKeys[k] = v
        end
        local part2 = 0
        for k,v in pairs(allKeys) do
            local v1 = data1[k]
            local v2 = data2[k]
            local part = table.compare(v1, v2)
            if math.isNan(part) then
                return part
            elseif part ~= 0 then
                if (part2 > 0 and part < 0) or (part2 < 0 and part > 0) then
                    return math.nan
                end
                part2 = part
            end
        end
        return part2
    elseif data1 == nil and data2 == nil then
        return 0
    elseif data1 == nil then
        return -1
    elseif data2 == nil then
        return 1
    elseif type(data1) ~= type(data2) then
        return math.nan
    elseif type(data1) == 'number' then
        return data1 - data2
    elseif type(data1) == 'string' then
        if data1 < data2 then
            return -1
        elseif data1 > data2 then
            return 1
        else
            return 0
        end
    elseif data1 == data2 then
        return 0
    else
        return math.nan
    end
end

function table.compareArray(data1, data2)
    if (type(data1) == 'table' or table.isudtable(data1)) and (type(data2) == 'table' or table.isudtable(data2)) then
        for i,v1 in ipairs(data1) do
            local v2 = data2[i]
            local part = table.compareArray(v1, v2)
            if part ~= 0 then
                return part
            end
        end
        if #data2 > #data1 then
            return -1
        else
            return 0
        end
    elseif data1 == nil and data2 == nil then
        return 0
    elseif data1 == nil then
        return -1
    elseif data2 == nil then
        return 1
    elseif type(data1) ~= type(data2) then
        return math.nan
    elseif type(data1) == 'number' then
        return data1 - data2
    elseif type(data1) == 'string' then
        if data1 < data2 then
            return -1
        elseif data1 > data2 then
            return 1
        else
            return 0
        end
    elseif data1 == data2 then
        return 0
    else
        return math.nan
    end
end

function table.shuffleArray(t)
    for i = #t, 1, -1 do
        local j = math.random(i)
        t[i], t[j] = t[j], t[i]
    end
end

if not table.pack then
    function table.pack(...)
        local result = {...}
        result.n = select("#", ...)
        return result
    end
end

if not table.unpack then
    function table.unpack(list)
        return unpack(list, 1, list.n)
    end
end

if clr then
    function table.readonly(t)
        if table.isreadonly(t) then
            return t
        end
        local readonlymeta = {
            __newindex = function() error("Try modify readonly table!") end,
            __index = t,
            __len = function() return #t end,
            __isobject = false,
            __udtabletype = "readonly",
            __raw = t,
        }
        return clr.newud(readonlymeta)
    end
    function table.getraw(t)
        if table.isudtable(t) then
            return table.getraw(getmetatable(t).__raw)
        else
            return t
        end
    end
    function table.isudtable(ud)
        return not not table.udtabletype(ud)
    end
    function table.udtabletype(ud)
        if type(ud) == "userdata" then
            local meta = getmetatable(ud)
            if meta then
                return meta.__udtabletype
            end
        end
    end
    function table.isreadonly(ud)
        return table.udtabletype(ud) == "readonly"
    end
    local oldnext = table.__cache_oldnext
    if not oldnext then
        oldnext = next
        table.__cache_oldnext = oldnext
    end
    function next(t, key)
        if table.isudtable(t) then
            return next(getmetatable(t).__raw, key)
        else
            return oldnext(t, key)
        end
    end
    local oldpairs = table.__cache_oldpairs
    if not oldpairs then
        oldpairs = pairs
        table.__cache_oldpairs = oldpairs
    end
    function pairs(t)
        if table.isudtable(t) then
            return pairs(getmetatable(t).__raw)
        else
            return oldpairs(t)
        end
    end
    local oldipairs = table.__cache_oldipairs
    if not oldipairs then
        oldipairs = ipairs
        table.__cache_oldipairs = oldipairs
    end
    function ipairs(t)
        if table.isudtable(t) then
            return ipairs(getmetatable(t).__raw)
        else
            return oldipairs(t)
        end
    end
    local oldconcat = table.__cache_concat
    if not oldconcat then
        oldconcat = table.concat
        table.__cache_concat = oldconcat
    end
    function table.concat(t, ...)
        if table.isudtable(t) then
            return table.concat(getmetatable(t).__raw, ...)
        else
            return oldconcat(t, ...)
        end
    end
    local oldmaxn = table.__cache_maxn
    if not oldmaxn then
        oldmaxn = table.maxn
        table.__cache_maxn = oldmaxn
    end
    function table.maxn(t)
        if table.isudtable(t) then
            return table.maxn(getmetatable(t).__raw)
        else
            return oldmaxn(t)
        end
    end
else
    local readonlymeta = {
        __newindex = function() error("Try modify readonly table!") end
    }
    function table.readonly(t)
        return setmetatable(t, readonlymeta)
    end
    function table.isudtable(ud)
        return false
    end
    function table.udtabletype(ud)
        return nil
    end
    function table.isreadonly(t)
        return getmetatable(t) == readonlymeta
    end
    function table.getraw(t)
        return t
    end
end

--- 换算数字，超过1万数字形式为xx万，超过1亿数字形式为xx亿
-- @param num 数字，类型number
-- @param n 保留小数位数（默认两位）
-- @return 数字转换后的字符串形式，类型string
-- function string.formatNumWithUnit(num, n)
--     num = tonumber(num)
--     if not n then
--         n = 2
--     else
--         n = tonumber(n)
--     end

--     if num < 10000 then
--         return tostring(num)
--     elseif num < 100000000 then
--         num = math.floor(num / math.pow(10, 4 - n)) / math.pow(10, n)
--         return lang.transstr("logogramMoneyTenThousand", num)
--     else
--         num = math.floor(num / math.pow(10, 8 - n)) / math.pow(10, n)
--         return lang.transstr("logogramMoneyHundredMillon", num)
--     end
-- end

--- 换算数字，超过1万数字形式为xx万，超过1亿数字形式为xx亿
-- @param num 数字，类型number
-- @param n 保留小数位数（默认两位）
-- @return 第一个string为数字，第二个string为单位
-- function string.formatNumSplitUnit(num, n)
--     num = tonumber(num)
--     if not n then
--         n = 2
--     else
--         n = tonumber(n)
--     end

--     if num < 10000 then
--         return tostring(num)
--     elseif num < 100000000 then
--         return string.trimNumber(string.format("%.2f", num / 10000)), lang.transstr("tenThousand")
--     else
--         return string.trimNumber(string.format("%.2f", num / 100000000)), lang.transstr("hundredMillon")
--     end
-- end

function math.concatCompareFunc(...)
    local funcs = {}
    local reverse = {}
    for i = 1, select('#', ...) do
        local arg = select(i, ...)
        if type(arg) == 'function' then
            funcs[#funcs + 1] = arg
        elseif type(arg) == 'table' and type(arg[1]) == 'function' then
            funcs[#funcs + 1] = arg[1]
            reverse[#funcs] = arg[2]
        end
    end
    local function concatedCompare(a, b)
        for i,v in ipairs(funcs) do
            local pv = v(a, b)
            if type(pv) == 'number' then
                if pv ~= 0 then
                    if reverse[i] then
                        return -pv
                    else
                        return pv
                    end
                end
            end
        end
        return 0
    end
    return concatedCompare
end

function tcompare(a, b)
    local ta = type(a)
    local tb = type(b)
    if ta ~= tb then
        return ta < tb
    end

    if ta == "number" or ta == "string" then
        return a < b
    end
    -- TODO: if they are full-userdata, we can return a < b
    -- Now, we return false. means a == b
    return false
end

-- 按指定的顺序遍历table
function spairs(t, f)
    local a = {}
    for n in pairs(t) do a[#a + 1] = n end
    table.sort(a, f or tcompare)
    local i = 0
    return function ()
        i = i + 1
        return a[i], t[a[i]]
    end
end

function sspairs(t, f)
    local a = {}
    for k in pairs(t) do
        if type(k) == "string" then
            a[#a + 1] = k
        end
    end
    table.sort(a, f)
    local i = 0
    return function ()
        i = i + 1
        return a[i], t[a[i]]
    end
end

-- 将一个number转换为二进制表示的table
-- 如10转换为二进制是1010
-- 传入参数mask=10
-- 返回{false, true, false, true}
-- 低位在table的前面，高位在table的后面
function math.getMaskTable(mask)
    mask = tonumber(mask)
    local tab = {}
    while mask > 0 do
        table.insert(tab, mask % 2 == 1 and true or false)
        mask = math.floor(mask / 2)
    end
    return tab
end

function ToBase64(source_str)
    local b64chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/'
    local s64 = ''
    local str = source_str

    while #str > 0 do
        local bytes_num = 0
        local buf = 0

        for byte_cnt=1,3 do
            buf = (buf * 256)
            if #str > 0 then
                buf = buf + string.byte(str, 1, 1)
                str = string.sub(str, 2)
                bytes_num = bytes_num + 1
            end
        end

        for group_cnt=1,(bytes_num+1) do
            b64char = math.fmod(math.floor(buf/262144), 64) + 1
            s64 = s64 .. string.sub(b64chars, b64char, b64char)
            buf = buf * 64
        end

        for fill_cnt=1,(3-bytes_num) do
            s64 = s64 .. '='
        end
    end

    return s64
end

function FromBase64(str64)
    local b64chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/'
    local temp={}
    for i=1,64 do
        temp[string.sub(b64chars,i,i)] = i
    end
    temp['=']=0
    local str=""
    for i=1,#str64,4 do
        if i>#str64 then
            break
        end
        local data = 0
        local str_count=0
        for j=0,3 do
            local str1=string.sub(str64,i+j,i+j)
            if not temp[str1] then
                return
            end
            if temp[str1] < 1 then
                data = data * 64
            else
                data = data * 64 + temp[str1]-1
                str_count = str_count + 1
            end
        end
        for j=16,0,-8 do
            if str_count > 0 then
                str=str..string.char(math.floor(data/math.pow(2,j)))
                data=math.fmod(data,math.pow(2,j))
                str_count = str_count - 1
            end
        end
    end
    return str
end

-- import("abc") => require("abc")
-- import("./abc") => require(CURRENT_DIR .. "abc")
-- import("../abc") => require(PARENT_DIR .. "abc")
function import(lib)
    lib = string.gsub(lib, "\\", "/")
    local cnt = 0
    repeat
        lib, cnt = string.gsub(lib, "//", "/")
    until cnt <= 0

    -- if not specified relative path, use default require
    if string.sub(lib, 1, 2) ~= "./" and string.sub(lib, 1, 3) ~= "../" then
        lib = string.gsub(lib, "/", ".")
        return require(lib)
    end

    -- deal with relative path
    local _, package = debug.getlocal(3, 1)

    if type(package) ~= "string" then -- this is run by command "luajit XXX.lua"
        package = arg[0]
        if string.sub(package, -4) == ".lua" then
            package = string.sub(package, 1, -5)
        end
    end

    local cnt, level = 1, 1
    while cnt ~= 0 do
        -- eat up any "./" prefix
        local t = 1
        while t ~= 0 do
            lib, t = string.gsub(lib, "^(%./)", "", 2)
            if t ~= 0 then
                cnt = 1
            end
        end

        -- count "../", add to level
        lib, cnt = string.gsub(lib, "^(%.%./)", "", 3)
        if cnt ~= 0 then
            level = level + 1
        end
    end

    for i = 1, level do
        package = (package):match("^(.+)[%./][^%./]+") or ""
    end

    lib = package .. "." .. lib
    lib = string.gsub(lib, "/", ".")
    return require(lib)
end

function isolatedenv()
    local env = {
        exports = _G,
    }
    env["_G"] = env
    local envmeta = {
        __index = _G,
        __master = clr.thislua(),
    }
    setmetatable(env, envmeta)
    return env, envmeta
end

function isolate(tab, env)
    if not env then
        env = isolatedenv()
    end
    local emuthd = coroutine.create(function()
        setfenv(0, env)
        local func, args
        local results = table.pack()
        while true do
            func, args = coroutine.yield(results)
            results = table.pack(ppcall(func, table.unpack(args)))
        end
    end)
    local success, message = coroutine.resume(emuthd)
    if not success then
        printe(message)
    end

    local isorunning = false
    local function isorun(raw, ...)
        if isorunning or coroutine.running() == emuthd then -- when access in C, we could directly call funcs in emuthd, in this condition, isorunning == false but cannot coroutine.resume
            return raw(...)
        end
        isorunning = true
        setfenv(raw, env)
        local success, result = coroutine.resume(emuthd, raw, table.pack(...))
        isorunning = false
        if not success then
            printe(result)
            return
        end
        return table.unpack(result)
    end
    local function isofunc(raw)
        return function(...)
            return isorun(raw, ...)
        end
    end

    if type(tab) == "function" then
        tab = isorun(tab)
    elseif type(tab) == "string" then
        tab = isorun(function()
            local cls = require(tab)
            if cls.__ctype and type(cls.new) == "function" then
                return cls.new()
            else
                return cls
            end
        end)
    end

    local iso = { __run = isorun, __func = isofunc }
    local isometa = {
        __index = isofunc(function(t, k)
            local raw = tab[k]
            if type(raw) == "function" then
                return isofunc(raw)
            end
            return raw
        end),
        __newindex = isofunc(function(t, k, v)
            tab[k] = v
        end),
    }
    local oldmeta = getmetatable(tab)
    if oldmeta then
        local isometameta = {
            __index = oldmeta
        }
        setmetatable(isometa, isometameta)
    end
    setmetatable(iso, isometa)
    return iso
    -- TODO: for pairs/ipairs, next, #, __call, ..., make it a udtable?
end

-- protected table. can read but can not write directly. can set value through rv.forceset[XX] = XX.
local ptable_criticalkeys = { forceset = true, protect = true }
local function ptable_movefields(from, to, fields, pkeys)
    if not fields then
        for k, v in pairs(from) do
            if not ptable_criticalkeys[k] then
                rawset(to, k, v)
                rawset(from, k, nil)
            end
        end
    else
        for i, k in ipairs(fields) do
            rawset(to, k, rawget(from, k))
            rawset(from, k, nil)
            if pkeys then
                pkeys[k] = true
            end
        end
        for k, v in pairs(fields) do
            if v == true then
                rawset(to, k, rawget(from, k))
                rawset(from, k, nil)
                if pkeys then
                    pkeys[k] = true
                end
            elseif v == false then
                if not ptable_criticalkeys[k] then
                    rawset(from, k, rawget(to, k))
                    rawset(to, k, nil)
                    if pkeys then
                        pkeys[k] = nil
                    end
                end
            end
        end
    end
end

function ptable(tab, fields)
    if tab.forceset then
        if fields == false then
            local ptab = tab.forceset
            ptab.forceset = nil
            ptab.protect = nil
            for k, v in pairs(ptab) do
                rawset(tab, k, v)
            end
            local meta = getmetatable(tab)
            local oldmeta = meta.__oldmeta
            return setmetatable(tab, oldmeta)
        else
            local ptab = tab.forceset
            local meta = getmetatable(tab)
            if not fields or fields == true then
                meta.__pallkeys = true
                ptable_movefields(tab, ptab)
            else
                if not meta.__pallkeys then
                    if type(fields) == "table" then
                        local pkeys = meta.__pkeys
                        ptable_movefields(tab, ptab, fields, pkeys)
                    elseif not ptable_criticalkeys[fields] then
                        local k = fields
                        local pkeys = meta.__pkeys
                        rawset(ptab, k, rawget(tab, k))
                        rawset(tab, k, nil)
                        pkeys[k] = true
                    end
                end
            end
            return tab
        end
    else
        if fields == false then
            return tab
        end
        local ptab = {}
        local oldmeta = getmetatable(tab)
        local pkeys = { forceset = true, protect = true }
        local meta = { __oldmeta = oldmeta, __pkeys = pkeys }
        if oldmeta then
            for k, v in pairs(oldmeta) do
                if not meta[k] then
                    meta[k] = v
                end
            end
        end
        if not fields or fields == true then
            meta.__pallkeys = true
            ptable_movefields(tab, ptab)
        else
            if type(fields) == "table" then
                ptable_movefields(tab, ptab, fields, pkeys)
            else
                local k = fields
                rawset(ptab, k, rawget(tab, k))
                rawset(tab, k, nil)
                pkeys[k] = true
            end
        end
        ptab.forceset = ptab
        ptab.protect = function(k)
            return ptable(tab, k)
        end

        meta.__index = ptab
        meta.__newindex = function(t, k, v)
            if meta.__pallkeys or pkeys[k] then
                error("Can not modify protected table directly. Use .forceset instead.")
            else
                ptab[k] = v
                if rawget(ptab, k) == v then
                    rawset(ptab, k, nil)
                    rawset(tab, k, v)
                end
            end
        end
        setmetatable(ptab, oldmeta)
        return setmetatable(tab, meta)
    end
end

local pclock_extra
local pclock_last
local pclock_lasttime
function os.pclock()
    local curclock = os.clock()
    local curtime = os.time()
    if not pclock_last then
        pclock_extra = 0
    elseif pclock_last > curclock then
        local delta = os.difftime(curtime, pclock_lasttime)
        if delta <= 0 then
            delta = 0.001
            pclock_extra = pclock_extra + (pclock_last + delta - curclock)
        end
    end
    pclock_last = curclock
    pclock_lasttime = curtime
    return curclock + pclock_extra
end

local function dolazyrequire(t)
    setmetatable(t, nil)
    local lib = t.__lib
    t.__lib = nil
    t.DoLazyRequire = nil
    if lib then
        local raw = require(lib)
        if type(raw) == "table" then
            for k, v in pairs(raw) do
                t[k] = v
            end
            setmetatable(t, getmetatable(raw))
            package.loaded[lib] = t
        else
            t.value = raw
        end
    end
end
local lazyrequiremeta = {
    __index = function(t, k)
        dolazyrequire(t)
        return t[k]
    end,
    __newindex = function(t, k, v)
        dolazyrequire(t)
        t[k] = v
    end,
}
function lazyrequire(lib)
    local existing = package.loaded[lib]
    if existing ~= nil then
        return existing
    end
    local wrapper = { __lib = lib }
    wrapper.DoLazyRequire = dolazyrequire
    return setmetatable(wrapper, lazyrequiremeta)
end

local appendableFuncs = {}
setmetatable(appendableFuncs, { __mode = "k" })
function appendable(func)
    local funcs = { func }
    local function wrapper(...)
        local results
        local cur = 0
        while true do
            cur = cur + 1
            local curfunc = funcs[cur]
            if not curfunc then
                if not results then
                    return
                else
                    return table.unpack(results)
                end
            end
            results = table.pack(curfunc(...))
        end
    end
    appendableFuncs[wrapper] = funcs
    return wrapper
end

function appendfunc(main, func)
    local funcs = appendableFuncs[main]
    if not funcs then
        return false
    end
    funcs[#funcs + 1] = func
    return true
end