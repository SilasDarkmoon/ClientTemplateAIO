local UnityEngine = clr.UnityEngine
local GameObject = UnityEngine.GameObject
local AudioSource = UnityEngine.AudioSource

local AudioManager = {}

local managers = {}
setmetatable(managers, {__mode = 'v'})

local audioEventListeners = {}

local function callListener(key, eventName, ...)
    local listeners = audioEventListeners[key]
    if listeners then
        for k, v in pairs(listeners) do
            local func = v
            if type(v) == 'table' then
                func = v[eventName]
            end
            if type(func) == 'function' then
                func(key, managers[key], ...)
            end
        end
    end
end

function AudioManager.SetGlobalVol(vol)
    AudioManager.globalVolume = vol
    cache.setLocalData("keyAudioGlobalVolume_global", vol, true)
    for cate, man in pairs(managers) do
        if man and man ~= clr.null then
            man.ApplyVolume()
        end
    end
end
function AudioManager.GetGlobalVol()
    local vol = AudioManager.globalVolume or cache.getLocalData("keyAudioGlobalVolume_global") or 1
    vol = tonumber(vol)
    return vol
end

function AudioManager.GetPlayer(cate)
    cate = cate or 'music'
    local man = managers[cate]

    if man and man ~= clr.null then
        return man
    end

    local oldPlaying
    local oldOnComplete
    if man and man ~= clr.null and man.isPrePlaying then
        -- Destroyed by destroyAll...
        oldPlaying = true
        oldOnComplete = man.curOnComplete
    end

    local go = GameObject("AudioManager:"..tostring(cate))
    local man = go:AddComponent(AudioSource)
    managers[cate] = man
    res.DontDestroyOnLoadAndDestroyAll(go)

    local pause = man.Pause
    man.Pause = function(...)
        if man.isPlaying then
            man.isPaused = true
        end
        pause(man, ...)
    end
    local unpause = man.UnPause
    man.UnPause = function(...)
        man.isPaused = nil
        unpause(man, ...)
    end
    local play = man.Play
    man.Play = function(...)
        man.isPaused = nil
        man.isPrePlaying = true
        play(man, ...)
    end
    local playDelayed = man.PlayDelayed
    man.PlayDelayed = function(...)
        man.isPaused = nil
        man.isPrePlaying = true
        playDelayed(man, ...)
    end
    local playOneShot = man.PlayOneShot
    man.PlayOneShot = function(...)
        man.isPaused = nil
        man.isPrePlaying = true
        playOneShot(man, ...)
    end
    local playScheduled = man.PlayScheduled
    man.PlayScheduled = function(...)
        man.isPaused = nil
        man.isPrePlaying = true
        playScheduled(man, ...)
    end
    local stop = man.Stop
    man.Stop = function(...)
        man.isPaused = nil
        stop(man, ...)
    end
    man.PlayAudio = function(path, vol, onComplete)
        vol = vol or 1
        vol = tonumber(vol)
        man.clipVolume = vol
        man.ApplyVolume()

        local oldOnComplete = man.curOnComplete
        man.curOnComplete = onComplete
        if type(oldOnComplete) == 'function' then
            oldOnComplete(false)
        end
        man.isPrePlaying = true
        local clip = res.LoadRes(path)
        if clip then
            man.clip = clip
            man.Play()  --Lua assist checked flag
        end
    end
    man.SetGlobalVol = function(vol)
        man.globalVolume = vol
        cache.setLocalData("keyAudioGlobalVolume_"..cate, vol, true)
        man.ApplyVolume()
    end
    man.GetGlobalVol = function()
        local vol = man.globalVolume or cache.getLocalData("keyAudioGlobalVolume_"..cate) or 1
        vol = tonumber(vol)
        return vol
    end
    man.ApplyVolume = function()
        man.volume = tonumber(man.clipVolume or 1) * man.GetGlobalVol() * AudioManager.GetGlobalVol()
    end

    local isOpen = nil
    if cate == "music" then
        isOpen = cache.getLocalData("keySettingsMusicOpen") or 1
    else
        isOpen = cache.getLocalData("keySettingsSoundEffectOpen") or 1
    end
    if isOpen == 1 then
        local volume = nil
        if cate == "music" then
            volume = cache.getLocalData("keySettingsMusicVolume") or 1
        else
            volume = cache.getLocalData("keySettingsSoundEffectVolume") or 1
        end
        man.SetGlobalVol(volume)
    else
        man.SetGlobalVol(0)
    end

    clr.bcoroutine(go:AddComponent(clr.DummyBehav), function()
        while true do
            if not man.isPlaying and not man.isPaused then
                if man.isPrePlaying then
                    man.isPrePlaying = nil
                    local oldOnComplete = man.curOnComplete
                    man.curOnComplete = nil
                    callListener(cate, 'onComplete')
                    if type(oldOnComplete) == 'function' then
                        oldOnComplete(true)
                    end
                end
            else
                man.isPrePlaying = true
            end
            coroutine.yield()
        end
    end)

    if oldPlaying then
        -- Destroyed by destroyAll...
        callListener(cate, 'onComplete', 'destroyed')
        if type(oldOnComplete) == 'function' then
            oldOnComplete(false)
        end
    end

    return man
end

function AudioManager.SetAudioOnOff(cate, isOpen)
    if cate == "music" then
        local man = managers[cate]
        if man and man ~= clr.null then
            if isOpen == 1 then
                local volume = cache.getLocalData("keySettingsMusicVolume") or 1
                man.SetGlobalVol(volume)
            else
                man.SetGlobalVol(0)
            end
        end
    else
        for cate, man in pairs(managers) do
            if cate ~= "music" and man and man ~= clr.null then
                if isOpen == 1 then
                    local volume = cache.getLocalData("keySettingsSoundEffectVolume") or 1
                    man.SetGlobalVol(volume)
                else
                    man.SetGlobalVol(0)
                end
            end
        end
    end
end

function AudioManager.SetAudioVolume(cate, volume)
    if cate == "music" then
        local man = managers[cate]
        if man and man ~= clr.null then
            man.SetGlobalVol(volume)
        end
    else
        for cate, man in pairs(managers) do
            if cate ~= "music" and man and man ~= clr.null then
                man.SetGlobalVol(volume)
            end
        end
    end
end

function AudioManager.RegListener(key, listener, listenerName, eventName)
    key = key or 'music'
    listenerName = listenerName or 'defaultListener'
    if listener == nil then
        if not eventName then
            local listeners = audioEventListeners[key]
            if listeners then
                listeners[listenerName] = nil
                if not next(listeners) then
                    audioEventListeners[key] = nil
                end
            end
        else
            local listeners = audioEventListeners[key]
            if listeners then
                local info = listeners[listenerName]
                if info then
                    info[eventName] = nil
                    if not next(info) then
                        listeners[listenerName] = nil
                        if not next(listeners) then
                            audioEventListeners[key] = nil
                        end
                    end
                end
            end
        end
    elseif type(listener) == 'function' then
        eventName = eventName or 'onComplete'
        local listeners = audioEventListeners[key]
        if not listeners then
            listeners = { }
            audioEventListeners[key] = listeners
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
                return AudioManager.RegListener(key, listener[eventName], listenerName, eventName)
            end
        else
            local listeners = audioEventListeners[key]
            if not listeners then
                listeners = { }
                audioEventListeners[key] = listeners
            end
            listeners[listenerName] = listener
        end
    end
    return true
end

audio = AudioManager

return AudioManager