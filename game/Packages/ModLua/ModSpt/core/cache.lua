cache = { }

function cache.isSimpleTable(tab)
    if type(tab) == "table" then
        if not clr.isobj(tab) then
            local metatable = getmetatable(tab)
            if not metatable or not metatable.__isobject then
                return true
            end
        end
    end
    return false
end

function cache.isObjWrapperTable(tab)
    if type(tab) == "table" then
        if not clr.isobj(tab) then
            local metatable = getmetatable(tab)
            if metatable and metatable.__isobject then
                return true
            end
        end
    end
    return false
end

function cache.getPersistentDataPath()
    return ___CONFIG__PERSISTENT_DATA_PATH or clr.UnityEngineEx.ThreadSafeValues.AppPersistentDataPath
end

local cacheData = { }
cacheData.data = { }
cacheData.listeners = { }
-- cacheData.data.device = {data = "", persistent = false}
-- cacheData.listeners.device = { listener1 = { onChanged = function() end, onSave = function() end, onUnsave = function() end } }

function cache.totable(...)
    local len = select('#', ...)
    if len == 1 then
        local tab = select(1, ...)
        if type(tab) == 'table' and not clr.isobj(tab) then
            return tab
        end
    end
    return {...}
end

function cache.foreach(func, ...)
    if type(func) == 'function' then
        local len = select('#', ...)
        if len == 1 then
            local tab = select(1, ...)
            if cache.isSimpleTable(tab) then
                for k, v in pairs(tab) do
                    if func(v) == 'break' then
                        break
                    end
                end
                return
            end
        end

        for i = 1, len do
            if func(select(i, ...)) == 'break' then
                break
            end
        end
    end
end

function cache.foreachi(func, ...)
    if type(func) == 'function' then
        local len = select('#', ...)
        if len == 1 then
            local tab = select(1, ...)
            if cache.isSimpleTable(tab) then
                for i, v in ipairs(tab) do
                    if func(v, i) == 'break' then
                        break
                    end
                end
                return
            end
        end

        for i = 1, len do
            if func(select(i, ...), i) == 'break' then
                break
            end
        end
    end
end

function cache.stacknew()
    return { v = { }, i = { }, c = { } }
end
function cache.stackremove(t, pos)
    if not t then return end
    if not pos then pos = #t end
    if t[pos] == nil then return end
    local last = true
    if pos < #t then
        t[pos + 1] = t[pos]
        last = false
    end
    local v = table.remove(t, pos)
    if last then return v end
end
function cache.stackpop(s, owner)
    if not s then return end
    local index
    if owner then
        index = s.i[owner]
        if not index then return end
        s.i[owner] = nil
        for i, v in ipairs(s.c) do
            if v > index then
                s.c[i] = v - 1
            end
        end
    else
        if #s.c <= 0 then return end
        index = table.remove(s.c)
    end
    for k, v in pairs(s.i) do
        if v > index then
            s.i[k] = v - 1
        end
    end
    return cache.stackremove(s.v, index)
end
function cache.stackpush(s, v, owner)
    local old
    if owner then
        old = cache.stackpop(s, owner)
    end
    table.insert(s.v, v)
    if owner then
        s.i[owner] = #s.v
    else
        table.insert(s.c, #s.v)
    end
    return old
end

function cache.setValueWithHistory(setfunc, getfunc)
    local valuestack = cache.stacknew()

    local function pushValue(value, owner)
        local oldvalue = getfunc()
        setfunc(value)

        oldvalue = cache.stackpush(valuestack, oldvalue, owner)
        if oldvalue ~= nil then
            valuestack.v[#valuestack.v] = oldvalue
        end
    end
    local function popValue(owner)
        local oldvalue = cache.stackpop(valuestack, owner)
        if oldvalue ~= nil then
            setfunc(oldvalue)
        end
    end
    local function getValue(owner)
        local index
        if owner then
            index = valuestack.i[owner]
        else
            if #valuestack.c <= 0 then return end
            index = valuestack.c[#valuestack.c]
        end
        if not index then return end
        return valuestack.v[index]
    end

    return pushValue, popValue, getValue
end

function cache.setValueWithHistoryAndPriority(setfunc)
    local valuestack = cache.stacknew()

    local function getValueOfMaxPriority()
        local maxv, maxp
        for i, vinfo in ipairs(valuestack.v) do
            local v, p = vinfo.v, vinfo.p
            if not maxp or maxp <= p then
                maxv = v
                maxp = p
            end
        end
        return maxv
    end
    local function setValueToMaxPriority()
        setfunc(getValueOfMaxPriority())
    end

    local function pushValue(value, owner, priority)
        priority = tonumber(priority)
        local vinfo = { v = value, p = priority }
        cache.stackpush(valuestack, vinfo, owner)
        setValueToMaxPriority()
    end
    local function popValue(owner)
        cache.stackpop(valuestack, owner)
        setValueToMaxPriority()
    end
    local function getValue(owner)
        if owner then
            local index = valuestack.i[owner]
            if not index then return end
            return valuestack.v[index]
        else
            return getValueOfMaxPriority()
        end
    end

    return pushValue, popValue, getValue
end

function cache.setValueWithHistoryAndCategory()
    local valuestacks = { }
    function valuestacks.getValue(cate, owner)
        if cate and valuestacks[cate] then
            local valuestack = valuestacks[cate]
            local index
            if owner then
                index = valuestack.i[owner]
            else
                if #valuestack.c <= 0 then return end
                index = valuestack.c[#valuestack.c]
            end
            if not index then return end
            local curVal = valuestack.v[index]
            return curVal and curVal[1]
        end
    end
    function valuestacks.pushValue(cate, val, owner)
        if cate then
            local valuestack = valuestacks[cate]
            if not valuestack then
                valuestack = cache.stacknew()
                valuestacks[cate] = valuestack
            end
            local oldvalue = { }
            if type(valuestacks.onGetValue) == 'function' then
                oldvalue = { valuestacks.onGetValue(cate) }
            end
            if type(valuestacks.onSetValue) == 'function' then
                valuestacks.onSetValue(cate, val)
            end

            oldvalue = cache.stackpush(valuestack, oldvalue, owner)
            if oldvalue ~= nil then
                valuestack.v[#valuestack.v] = oldvalue
            end
        end
    end
    function valuestacks.popValue(cate, owner)
        if cate and valuestacks[cate] then
            local valuestack = valuestacks[cate]
            local oldvalue = cache.stackpop(valuestack, owner)
            if oldvalue ~= nil then
                if type(valuestacks.onSetValue) == 'function' then
                    valuestacks.onSetValue(cate, oldvalue and oldvalue[1])
                end
            end
            if #valuestack.v <= 0 then
                valuestacks[cate] = nil
            end
        end
    end

    return valuestacks
end

function cache.setGlobalTempData(value, key)
    if key then
        if type(cache.globalTempData) ~= 'table' then
            if value == nil then return end
            cache.globalTempData = { [cacheData] = cache.globalTempData }
        end
        cache.globalTempData[key] = value
        if value == nil then
            if not next(cache.globalTempData) then
                cache.globalTempData = nil
            end
        end
    else
        if type(cache.globalTempData) == 'table' and cache.globalTempData[cacheData] ~= nil then
            cache.globalTempData[cacheData] = value
            if value == nil then
                if not next(cache.globalTempData) then
                    cache.globalTempData = nil
                end
            end
        else
            cache.globalTempData = value
        end
    end
end
function cache.getGlobalTempData(key)
    if key then
        if type(cache.globalTempData) == 'table' then
            return cache.globalTempData[key]
        end
    else
        if type(cache.globalTempData) == 'table' and cache.globalTempData[cacheData] ~= nil then
            return cache.globalTempData[cacheData]
        else
            return cache.globalTempData
        end
    end
end
function cache.removeGlobalTempData(key)
    local value = cache.getGlobalTempData(key)
    cache.setGlobalTempData(nil, key)
    return value
end

-- may need encrypt
function cache.getLocalData(key)
    if key == nil then
        return nil, false, true
    end
    local info = cacheData.data[key]
    if info then
        return info.data, info.persistent, info.readonly
    end
    local data = clr.UnityEngineEx.IsolatedPrefs.GetString(key)
    if data == nil or data == "" then
        info = { persistent = false }
        cacheData.data[key] = info
        return nil, false, false
    else
        info = { }
        cacheData.data[key] = info
        if data == 'file' then
            data = nil
            local f = io.open(cache.getPersistentDataPath() .. '/' .. key .. '.json')
            if f then
                data = f:read("*a")
                f:close()
            end
        end
        data = json.decode(data)
        info.persistent = true
        info.data = data
        return data, true, false
    end
end

function cache.isPersistent(key)
    local value, persistent = cache.getLocalData(key)
    return persistent
end

-- make the data of the key to be readonly
function cache.freeze(key)
    local info = cacheData.data[key]
    if not info then
        info = { }
        cacheData.data[key] = info
    end
    info.readonly = true
end

-- save a key-value pair to the disk to make it persistent
function cache.save(key, persistentflag)
    if key == nil then return false end
    local info = cacheData.data[key]
    if not info then
        info = { }
        cacheData.data[key] = info
    end
    
    if persistentflag == 'snapshot' then
        info.snapshot = true
    else
        info.persistent = true
        local value = info.data
        cache.callListener(key, 'onSave')
        if value == nil or value == "" then
            -- value is nil to remove the data binded to the key
            clr.UnityEngineEx.IsolatedPrefs.DeleteKey(key);
        else
            value = json.encode(value)
            if persistentflag == 'file' then
                clr.UnityEngineEx.IsolatedPrefs.SetString(key, 'file')
                local f = io.open(cache.getPersistentDataPath() .. '/' .. key .. '.json', 'w')
                if f then
                    f:write(value)
                    f:flush()
                    f:close()
                end
            else
                clr.UnityEngineEx.IsolatedPrefs.SetString(key, value)
            end
        end
        clr.UnityEngineEx.IsolatedPrefs.Save()
    end
    return true
end

-- to remove a key-value pair from the disk to make it not persistent. But the cached data is still available
function cache.unsave(key)
    if key == nil then return end
    if not cache.isPersistent(key) then return end
    cacheData.data[key].persistent = false
    cache.callListener(key, 'onUnsave')
    clr.UnityEngineEx.IsolatedPrefs.DeleteKey(key);
end

-- on writing persistent data, we must set the third param to true or it will fail!
function cache.setLocalData(key, value, persistent)
    if key == nil then return false end
    local oldValue, oldPersistent, isReadonly = cache.getLocalData(key)
    if isReadonly then
        return false
    end
    if oldPersistent and not persistent then
        -- cannot overwrite the persistent value with unpersistent one
        return false
    end
    local isEqual = (oldValue == value)
    if isEqual then
        if persistent and type(oldValue) == "table" then
            isEqual = table.compare(oldValue, value) == 0 and true or false
        end
    end
    if isEqual and persistent ~= oldPersistent then
        -- value not changed, only saving.
        cache.save(key, persistent)
        return true
    end
    if not isEqual then
        -- value changed
        local info = cacheData.data[key]
        if not info then
            info = { }
            cacheData.data[key] = info
        end
        info.data = value
        if persistent then
            cache.save(key, persistent)
        end
        if not persistent and value == nil then
            -- remove from the cache
            cacheData.data[key] = nil
        end
        cache.callListener(key, 'onChanged')
        -- else: value not changed and old persistent and calling persistent is the same. nothing happens.
    end

    return true
end

function cache.registerListener(key, listener, listenerName, eventName)
    if key == nil then return false end
    listenerName = listenerName or 'cacheDefaultListener'
    if listener == nil then
        if not eventName then
            local listeners = cacheData.listeners[key]
            if listeners then
                listeners[listenerName] = nil
                if not next(listeners) then
                    cacheData.listeners[key] = nil
                end
            end
        else
            local listeners = cacheData.listeners[key]
            if listeners then
                local info = listeners[listenerName]
                if info then
                    info[eventName] = nil
                    if not next(info) then
                        listeners[listenerName] = nil
                        if not next(listeners) then
                            cacheData.listeners[key] = nil
                        end
                    end
                end
            end
        end
    elseif type(listener) == 'function' then
        eventName = eventName or 'onChanged'
        local listeners = cacheData.listeners[key]
        if not listeners then
            listeners = { }
            cacheData.listeners[key] = listeners
        end
        local info = listeners[listenerName]
        if not info then
            info = { }
            listeners[listenerName] = info
        end
        info[eventName] = listener
    elseif type(listener) == 'table' then
        if eventName then
            if listener[eventName] then
                return cache.registerListener(key, listener[eventName], listenerName, eventName)
            end
        else
            local listeners = cacheData.listeners[key]
            if not listeners then
                listeners = { }
                cacheData.listeners[key] = listeners
            end
            listeners[listenerName] = listener
        end
    end
    return true
end

function cache.callListener(key, eventName)
    local listeners = cacheData.listeners[key]
    if listeners then
        for k, v in pairs(listeners) do
            local func = v
            if type(v) == 'table' then
                func = v[eventName]
            end
            if type(func) == 'function' then
                local value = cache.getLocalData(key)
                func(key, value, k, eventName)
            end
        end
    end
end

function cache.snapshot(filename)
    filename = tostring(filename or 'default')
    
    local value = {}
    for k, v in pairs(cacheData.data) do
        if v.snapshot then
            value[k] = v.data
        end
    end
    
    value = json.encode(value)
    local f = io.open(cache.getPersistentDataPath() .. '/cacheSnapshots/' .. filename .. '.json', 'w')
    if f then
        f:write(value)
        f:flush()
        f:close()
    end
end

function cache.loadSnapshot(filename)
    filename = tostring(filename or 'default')
    
    local data
    local f = io.open(cache.getPersistentDataPath() .. '/cacheSnapshots/' .. filename .. '.json')
    if f then
        data = f:read("*a")
        f:close()
    end
    data = json.decode(data)
    
    for k, v in pairs(data) do
        cache.setLocalData(k, v, 'snapshot')
    end
end

return cache
