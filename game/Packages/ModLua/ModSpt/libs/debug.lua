--[[--

Debug functions.

## Functions ##

-   echo
-   echoInfo
-   echoError
-   printf

]]

function echo(...)
    if LogEnabled == false or LogInfoEnabled == false then return end
    local arr = {}
    for i = 1, select('#', ...) do
        local arg = select(i, ...)
        arr[#arr + 1] = tostring(arg)
    end
    print(table.concat(arr, "\t"))
end

--[[--

Output a formatted string.

Depends on the platform, output to console or log file. @see echo().

@param string format
@param mixed ...

@see echo

]]
function printf(fmt, ...)
    if LogEnabled == false or LogInfoEnabled == false then return end
    echo(string.format(tostring(fmt), ...))
end

function echoError(fmt, ...)
    if LogEnabled == false or LogInfoEnabled == false then return end
    local msg = string.format("[ERR] %s", string.format(tostring(fmt), ...))
    echo(msg)
end

function echoInfo(fmt, ...)
    if LogEnabled == false or LogInfoEnabled == false then return end
    echo("[INFO] " .. string.format(tostring(fmt), ...))
end

function echoLog(tag, fmt, ...)
    if LogEnabled == false or LogInfoEnabled == false then return end
    echo(string.format("[%s] %s", string.upper(tostring(tag)), string.format(tostring(fmt), ...)))
end

function getPackageName(moduleName)
    local packageName = ""
    local pos = string.find(string.reverse(moduleName), "%.")
    if pos then
        packageName = string.sub(moduleName, 1, string.len(moduleName) - pos + 1)
    end
    return packageName
end

--[[--

Dumps information about a variable.

@param mixed object
@param string label
@param bool isReturnContents
@param int nesting
@return nil|string

]]
function dump(object, label, israw)
    if LogEnabled == false or LogInfoEnabled == false then return end
    local message = dumpq(object, label, israw)
    print(message)
    return message
end

function dumpq(object, label, israw)
    local result = vardump(object, label, israw)
    return table.concat(result, "\n")
end

function dumpw(object, label, israw)
    if LogEnabled == false or LogWarningEnabled == false then return end
    printw(dumpq(object, label, israw))
end

function dumpe(object, label, israw)
    if LogEnabled == false or LogErrorEnabled == false then return end
    printe(dumpq(object, label, israw))
end

function dumprawq(object, label)
    return dumpq(object, label, true)
end

function dumpraw(object, label)
    if LogEnabled == false or LogInfoEnabled == false then return end
    local message = dumprawq(object, label)
    print(message)
    return message
end

function dumpraww(object, label)
    if LogEnabled == false or LogWarningEnabled == false then return end
    printw(dumprawq(object, label))
end

function dumprawe(object, label)
    if LogEnabled == false or LogErrorEnabled == false then return end
    printe(dumprawq(object, label))
end

function ndump(...)
    if LogEnabled == false or LogInfoEnabled == false then return end
    local str = ndumpq(...)
    print(str)
    return str
end

function ndumpq(...)
    local str
    for i = 1, select('#', ...) do
        local arg = select(i, ...)
        local result = vardump(arg, i)
        if str then
            str = str..'\n'
        else
            str = ""
        end
        str = str..table.concat(result, "\n")..','
    end
    return str
end

function ndumpw(...)
    if LogEnabled == false or LogWarningEnabled == false then return end
    printw(ndumpq(...))
end

function ndumpe(...)
    if LogEnabled == false or LogErrorEnabled == false then return end
    printe(ndumpq(...))
end

local typeClrBytes
local function isClrBytes(val)
    if not typeClrBytes then
        typeClrBytes = clr.array(clr.System.Byte)
    end
    return rawequal(clr.type(val), typeClrBytes)
end

--[[--

Outputs or returns a parsable string representation of a variable.

@param mixed object
@param string label
@return table each line

]]
function vardump(object, label, israw)
    if ENABLE_PROFILER_LUA then
        clr.beginsample("vardump")
    end

    local lookupTable = {}
    local indexed = {}
    local result = {}

    local function _v(v)
        if type(v) == "string" then
            return "\""..clr.luastr(v).."\""
        end
        if type(v) == 'number' or type(v) == 'boolean' then
            return tostring(v)
        end
        if clr and clr.isobj(v) then
            if v == clr.null then
                return "'"..tostring(clr.type(v))..", null'"
            elseif isClrBytes(v) then
                return "\""..clr.datastr(v).."\""
            else
                return "'"..tostring(clr.type(v))..", "..tostring(v).."'"
            end
        end
        return "'"..tostring(v).."'"
    end

    local _vardump -- local function _vardump
    local function _k(k, indent, nest)
        if type(k) == "string" then
            local str = clr.luastr(k)
            if str ~= "" then
                local firstChar = string.byte (str, 1)
                if firstChar >= 65 and firstChar <= 90 or firstChar == 95 or firstChar >= 97 and firstChar <= 122 then
                    --a-zA-Z_
                    if not string.find(str, "\\", 1, true) then
                        return str
                    end
                end
            end
            return string.format('["%s"]', str)
        end
        if type(k) == 'number' or type(k) == 'boolean' then
            return string.format("[%s]", tostring(k))
        end
        if type(k) == 'table' or table.isudtable(k) then
            if lookupTable[k] then
                local line = lookupTable[k]
                local rv = string.format("['*%s*']", tostring(line))
                if not indexed[line] then
                    result[line] = result[line]..string.format(" --*%s*", line)
                    indexed[line] = line
                end
                return rv
            else
                local line = #result + 1
                local rv = string.format("['*%s*']", tostring(line))
                local kkey = string.format("['*%s*:']", tostring(line))
                _vardump(k, kkey, indent, nest)
                if nest == 1 then
                    result[#result] = result[#result]..','
                end
                --lookupTable[k] = line
                indexed[line] = line
                return rv
            end
        end
        return string.format("['%s']", tostring(k))
    end

    local function clrobj2str(obj)
        local metatable = getmetatable(obj)
        if not metatable or not metatable.__tostring then
            return clr.System.String.Format("{0}", obj)
        else
            return tostring(obj)
        end
    end

    function _vardump(object, label, indent, nest, isArr)
        if (label == nil) then label = "var" end
        local postfix = ""
        if nest > 1 then postfix = "," end
        local reallabel = indent
        if not isArr then
            local key = _k(label, indent, nest)
            reallabel = string.format("%s%s = ", indent, key)
        end
        if type(object) ~= "table" and not table.isudtable(object) then
            result[#result +1] = string.format("%s%s%s", reallabel, _v(object), postfix)
        elseif lookupTable[object] then
            local line = lookupTable[object]
            result[#result +1] = string.format("%s'*%s*'%s", reallabel, line, postfix)
            if not indexed[line] then
                result[line] = result[line]..string.format(" --*%s*", line)
                indexed[line] = line
            end
        else
            local line = #result + 1
            lookupTable[object] = line

            if not israw and not table.isudtable(object) and clr and clr.isobj(object) then
                if object == clr.null then
                    result[line] = string.format("%s'%s, null'", reallabel, tostring(clr.type(object)))
                elseif isClrBytes(object) then
                    result[line] = reallabel.." \""..clr.datastr(object).."\""
                else
                    result[line] = string.format("%s'%s, %s'", reallabel, tostring(clr.type(object)), clrobj2str(object))
                end
            else
                if getmetatable(object) and getmetatable(object).__isobject then
                    result[line] = string.format("%s{ -- __isobject", reallabel)
                else
                    result[line] = string.format("%s{", reallabel)
                end

                local indent2 = indent .. "    "
                local keys = {}
                local allKeyIsInt = true
                local minKey, maxKey = #object, 0
                for k, v in pairs(object) do
                    keys[#keys + 1] = k
                    if type(k) == 'number' and k == math.floor(k) then
                        if k > maxKey then
                            maxKey = k
                        end
                        if k < minKey then
                            minKey = k
                        end
                    else
                        allKeyIsInt = false
                    end
                end
                local isObjArr = allKeyIsInt and #keys == maxKey and minKey == 1
                table.sort(keys, function(a, b)
                    if type(a) == "number" and type(b) == "number" then
                        return a < b
                    else
                        return tostring(a) < tostring(b)
                    end
                end)
                for i, k in ipairs(keys) do
                    _vardump(object[k], k, indent2, nest + 1, isObjArr)
                end
                result[#result +1] = string.format("%s}%s", indent, postfix)
            end
        end
    end
    _vardump(object, label, "", 1)

    if ENABLE_PROFILER_LUA then
        clr.endsample()
    end
    return result
end

function ppcall(f, ...)
    local rvs = table.pack(xpcall(f, printe, ...))
    if rvs[1] then
        return unpack(rvs, 2, rvs.n)
    else
        return nil
    end
end

function ccall(condition, f, ...)
    if condition then
        return f(...)
    end
end

function debug.mem()
    return collectgarbage("count")
end

function debug.regcnt()
    local reg = clr.luareg()
    local cnt = 0
    for k, v in pairs(reg) do
        if type(k) == "number" then
            if type(v) ~= "number" then
                cnt = cnt + 1
            end
        end
    end
    return cnt
end