local _WaitForSeconds = clr.UnityEngine.WaitForSeconds
local WaitForSeconds = clr.unextend(_WaitForSeconds)
if type(WaitForSeconds["@luareset"]) == "function" then
    WaitForSeconds["@luareset"]()
end

local cache_WaitForSeconds = {}
local oldCtor_WaitForSeconds = getmetatable(WaitForSeconds).__call
getmetatable(WaitForSeconds).__call = function(t, time)
    time = time or 0
    local cached = cache_WaitForSeconds[time]
    if not cached then
        cached = oldCtor_WaitForSeconds(t, time)
        cache_WaitForSeconds[time] = cached
    end
    return cached
end

rawset(WaitForSeconds, "@luareset", function()
    getmetatable(WaitForSeconds).__call = oldCtor_WaitForSeconds
end)

local _WaitForEndOfFrame = clr.UnityEngine.WaitForEndOfFrame
local WaitForEndOfFrame = clr.unextend(_WaitForEndOfFrame)
if type(WaitForEndOfFrame["@luareset"]) == "function" then
    WaitForEndOfFrame["@luareset"]()
end

local cache_WaitForEndOfFrame
local oldCtor_WaitForEndOfFrame = getmetatable(WaitForEndOfFrame).__call
getmetatable(WaitForEndOfFrame).__call = function(t)
    local cached = cache_WaitForEndOfFrame
    if not cached then
        cached = oldCtor_WaitForEndOfFrame(t)
        cache_WaitForEndOfFrame = cached
    end
    return cached
end

rawset(WaitForEndOfFrame, "@luareset", function()
    getmetatable(WaitForEndOfFrame).__call = oldCtor_WaitForEndOfFrame
end)
