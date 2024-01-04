local hotfixbase = class("hotfixbase")

local meta = {
    __index = function(tab, key)
        if type(key) == "string" then
            local suffix = string.sub(key, -5, -1)
            if suffix == " head" or suffix == " tail" then
                local raw = rawget(tab, "\022")
                if raw then
                    return raw[key]
                end
            end
        end
    end,
    __newindex = function(tab, key, val)
        if type(key) == "string" then
            local suffix = string.sub(key, -5, -1)
            if suffix == " head" or suffix == " tail" then
                local raw = rawget(tab, "\022")
                if not raw then
                    raw = {}
                    rawset(tab, "\022", raw)
                end
                raw[key] = val
                local hash = clr.LuaLib.HotFixCaller.GetTokenHash(key)
                rawset(tab, hash, val)
                return
            end
        end
        rawset(tab, key, val)
    end,
}

setmetatable(hotfixbase, meta)

return hotfixbase