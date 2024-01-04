local UnityEngine = clr.UnityEngine
local GameObject = UnityEngine.GameObject
local Component = UnityEngine.Component
local Application = UnityEngine.Application
local Time = UnityEngine.Time
local LuaBehav = clr.LuaBehav

local function setIndexHierarchical(table, indexmeta)
    local meta
    -- if type(table) == 'userdata' then
    --     meta = clr.ex(table)
    --     if type(meta) == 'table' then
    --         local mmeta = getmetatable(meta)
    --         if not mmeta then
    --             mmeta = { __index = indexmeta }
    --             setmetatable(meta, mmeta)
    --             return table
    --         end
    --         meta = mmeta
    --     else
    --         return table
    --     end
    -- else
        meta = getmetatable(table)
        if not meta then
            return setmetatable(table, { __index = indexmeta })
        end
    -- end

    if not meta.__index then
        meta.__index = indexmeta
        return table
    else
        if type(meta.__index) == 'function' then
            local oldmeta = meta.__index
            meta.__index = function(table, key)
                local rv = oldmeta(table, key)
                if rv ~= nil then
                    return rv
                else
                    if type(indexmeta) == 'function' then
                        return indexmeta(table, key)
                    elseif type(indexmeta) == 'table' or type(indexmeta) == 'userdata' then
                        return indexmeta[key]
                    end
                end
            end
            return table
        elseif type(meta.__index) == 'table' or type(meta.__index) == 'userdata' then
            setIndexHierarchical(meta.__index, indexmeta)
            return table
        else
            meta.__index = indexmeta
            return table
        end
    end

    return table
end

local function setNewIndexHierarchical(table, indexmeta)
    local meta
    -- if type(table) == 'userdata' then
    --     meta = clr.ex(table)
    --     if type(meta) == 'table' then
    --         local mmeta = getmetatable(meta)
    --         if not mmeta then
    --             mmeta = { __newindex = indexmeta }
    --             setmetatable(meta, mmeta)
    --             return table
    --         end
    --         meta = mmeta
    --     else
    --         return table
    --     end
    -- else
        meta = getmetatable(table)
        if not meta then
            return setmetatable(table, { __newindex = indexmeta })
        end
    -- end

    if not meta.__newindex then
        meta.__newindex = indexmeta
        return table
    else
        if type(meta.__newindex) == 'function' then
            -- ??
            meta.__newindex = indexmeta
            return table
        elseif type(meta.__newindex) == 'table' or type(meta.__newindex) == 'userdata' then
            setIndexHierarchical(meta.__newindex, indexmeta)
            return table
        else
            meta.__newindex = indexmeta
            return table
        end
    end

    return table
end

local function checkDevelopmentBuild()
    local devflag = clr.UnityEngine.Debug.isDebugBuild
    if type(devflag) == "boolean" then
        if devflag then
            __DEVELOPMENT_BUILD_FLAG = true
            __DEBUG_FLAG = true
        end
    end
end

xpcall(checkDevelopmentBuild, printe)

unity = {}

function unity.waitForEndOfFrame()
    local curFrame = UnityEngine.Time.frameCount
    if unity.waitForEndOfFrameIndex ~= curFrame then
        coroutine.yield(UnityEngine.WaitForEndOfFrame())
        unity.waitForEndOfFrameIndex = curFrame
    end
end

function unity.waitForNextEndOfFrame()
    coroutine.yield(UnityEngine.WaitForEndOfFrame())
    local curFrame = UnityEngine.Time.frameCount
    unity.waitForEndOfFrameIndex = curFrame
end

unity.await = {}
unity.await.eof = unity.waitForEndOfFrame
unity.await.neof = unity.waitForNextEndOfFrame
unity.await.time = function(seconds)
    coroutine.yield(clr.UnityEngine.WaitForSeconds(seconds))
end
unity.await.rtime = function(seconds)
    coroutine.yield(clr.UnityEngine.WaitForSecondsRealtime(seconds))
end
unity.await.update = function()
    coroutine.yield()
end
unity.await.fupdate = function()
    coroutine.yield(clr.UnityEngine.WaitForFixedUpdate())
end
unity.await.to = function(func)
    coroutine.yield(clr.UnityEngine.WaitUntil(func))
end
unity.await.when = function(func)
    coroutine.yield(clr.UnityEngine.WaitWhile(func))
end
setmetatable(unity.await, { __call = function(t, ...) coroutine.yield(...) end })

function unity.updateCanvases()
    UnityEngine.Canvas.ForceUpdateCanvases()
end

function unity.alloc()
    local go = GameObject()
    local behav = go:AddComponent(LuaBehav)
    return behav
end

unity.base = class(unity.alloc)

unity.base.coroutine = clr.bcoroutine

unity.base.destroySelf = function(self)
    UnityEngine.Object.Destroy(self.gameObject)
end

unity.base.destroyRoot = function(self)
    UnityEngine.Object.Destroy(self.transform.root.gameObject)
end

function unity.create(prefab)
    local go = GameObject.Instantiate(prefab)
    if go then
        local behav = go:GetComponent(LuaBehav)
        if not behav then
            local behav = go:AddComponent(LuaBehav)
        end
        behav.coroutine = clr.bcoroutine
        return behav
    end
end

function unity.component(go, comp)
    if clr.is(go, Component) then
        go = go.gameObject
    elseif not clr.is(go, GameObject) then
        go = GameObject()
    end
    go:AddComponent(comp)
    local behav = go:GetComponent(LuaBehav)
    if not behav then
        behav = go:AddComponent(LuaBehav)
        behav.coroutine = clr.bcoroutine
    end
    return behav
end

function unity.restart()
    -- The global referenced asset that may be loaded from ab should be restored to default.
    clr.UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset = res.defaultRenderPipelineAsset

    res.Cleanup()
    for k,v in pairs(package.loaded) do
        package.loaded[k] = nil
    end

    res.DestroyAllHard()
    res.CollectGarbage(2)
    res.ClearSceneCache()
    Application.LoadLevel(0)
    Time.timeScale = 1
end

function unity.dirtyRestart()
    Application.LoadLevel(0)
    Time.timeScale = 1
end

function unity.changeServerTo(url, onModifyConfig)
    -- The global referenced asset that may be loaded from ab should be restored to default.
    clr.UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset = res.defaultRenderPipelineAsset

    res.Cleanup()
    for k,v in pairs(package.loaded) do
        package.loaded[k] = nil
    end

    res.DestroyAllHard()
    res.CollectGarbage(2)
    res.ClearSceneCache()

    pcall(require, "config")
    ___CONFIG__ACCOUNT_URL = url
    ___CONFIG__BASE_URL = ___CONFIG__ACCOUNT_URL
    ___CONFIG__TCP_URL = nil
    ___CONFIG__PVP_URL = nil
    if type(onModifyConfig) == "function" then
        onModifyConfig()
    end

    Application.LoadLevel(0)
    Time.timeScale = 1
end

local luacomp = class()
unity.luacomp = luacomp

function luacomp:ctor(parent)
    self:addto(parent)
end

function luacomp:addto(parent)
    self.__parent = parent
    if parent then
        local comps = parent.__comps
        if type(comps) ~= 'table' then
            comps = {}
            parent.__comps = comps
        end
        local name = parent.__cname
        if type(name) ~= 'string' then
            name = tostring(self)
        end
        comps[name] = self
    end
end

function luacomp:coroutine(...)
    return self.__parent:coroutine(...)
end

luacomp.__index = function(tab, key)
    local parent = rawget(tab, '__parent')
    if parent then
        local rv = parent[key]
        if rv ~= nil then
            return rv
        end
    end
    local rv = rawget(getmetatable(tab), key)
    if rv ~= nil then
        return rv
    end
end

local composed = class(unity.base)
unity.composed = composed

function composed:ctor()
    setIndexHierarchical(self, function(table, key)
        if type(key) == 'string' then
            local key2 = string.upper(string.sub(key, 1, 1))
            if key2 ~= string.sub(key, 1, 1) then
                if type(self.__comps) == 'table' then
                    for k, v in pairs(self.__comps) do
                        local func = rawget(v, key) or (v.class and rawget(v.class, key))
                        if type(func) == 'function' then
                            return function(table, ...)
                                self:callcomps(key, ...)
                            end
                        end
                    end
                end
            end
            return nil
        end
    end)
end

function composed:callcomps(funcname, ...)
    if type(self.__comps) == 'table' then
        local cnt = table.nums(self.__comps)
        for k, v in pairs(self.__comps) do
            cnt = cnt - 1
            local func = rawget(v, funcname) or (v.class and rawget(v.class, funcname))
            if type(func) == 'function' then
                if cnt == 0 then
                    return func(v, ...)
                else
                    func(v, ...)
                end
            end
        end
    end
end

local secomp = class(unity.base)
unity.secomp = secomp

function secomp:start()
    self.se_started = true
    if type(self.onStarted) == 'function' then
        self:onStarted()
    end
    if self.se_enabled then
        self:callEnabled()
    end
end

function secomp:onEnable()
    self.se_enabled = true
    if self.se_started then
        self:callEnabled()
    end
end

function secomp:callEnabled()
    if type(self.onEnabled) == 'function' then
        self:onEnabled()
    end
    if not self.se_fenabled then
        self.se_fenabled = true
        if type(self.onFirstEnabled) == 'function' then
            self:onFirstEnabled()
        end
    end
end

local uscene = class(secomp)
unity.scene = uscene

function uscene:ctor()
    if type(res.curSceneInfo) == 'table' then
        if type(res.curSceneInfo.args) == 'table' then
            self:init(unpack(res.curSceneInfo.args, 1, res.curSceneInfo.argc))
            return
        end
    end
    self:init()
end

function uscene:onEnabled()
    if type(res.curSceneInfo) == 'table' then
        if type(res.curSceneInfo.args) == 'table' then
            self:refresh(unpack(res.curSceneInfo.args, 1, res.curSceneInfo.argc))
            return
        end
    end
    self:refresh()
end

function uscene:init(...)
end

function uscene:refresh(...)
end

local newuscene = class(secomp)
unity.newscene = newuscene

function newuscene:ctor()
    cache.setGlobalTempData(self, "MainManager")
end

if clr.UnityEngine.Application.isEditor then
    if clr.type(clr.Unity.EditorCoroutines.Editor.EditorCoroutineUtility) == clr.System.Type then
        function unity.ecoroutine(func, ...)
            local args = {...}
            local argc = select('#', ...)
            local act = function()
                func(unpack(args, 1, argc))
            end

            clr.Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutineOwnerless(clr.UnityEngineEx.GlobalLua.EnumLuaCoroutine(act))
        end
    end
end

function unity.curcoinfo()
    return clr.getucoroutine()
end

function unity.async(func, ...)
    if clr.runningco() then
        func(...)
        return clr.getucoroutine()
    else
        local args = {...}
        local argc = select('#', ...)
        local coinfo
        local co = clr.coroutine(function()
            coinfo = clr.getucoroutine()
            func(unpack(args, 1, argc))
        end)
        if co then
            return coinfo
        else
            return nil
        end
    end
end

function unity.basync(self, func, ...)
    if clr.runningco() then
        func(...)
        return clr.getucoroutine()
    else
        local args = {...}
        local argc = select('#', ...)
        local coinfo
        local co = clr.bcoroutine(self, function()
            coinfo = clr.getucoroutine()
            func(unpack(args, 1, argc))
        end)
        if co then
            return coinfo
        else
            return nil
        end
    end
end

local abortyieldable
local function abortCurrentCoroutine()
    if not abortyieldable then
        abortyieldable = clr.UnityEngineEx.CoroutineRunner.CoroutineAbortedYieldable.Instance
    end
    coroutine.yield(abortyieldable)
end
function unity.abort(co)
    if not co then
        abortCurrentCoroutine()
    else
        local curcoinfo, curco = clr.getucoroutine()
        if co == curco or co == curcoinfo or co == clr.runningco() then
            abortCurrentCoroutine()
        else
            if type(co) == "thread" then
                local info = clr.getucoroutine(co)
                if info then
                    clr.UnityEngineEx.CoroutineRunner.StopCoroutine(info)
                end
            else
                clr.UnityEngineEx.CoroutineRunner.StopCoroutine(co)
            end
        end
    end
end

function unity.isrunning(co)
    if co then
        local curcoinfo, curco = clr.getucoroutine()
        return co == curco or co == curcoinfo or co == clr.runningco()
    else
        return not not clr.runningco()
    end
end

function unity.running()
    local curcoinfo, curco = clr.getucoroutine()
    return curcoinfo, curco, clr.runningco()
end

function unity.cofinally(co, func)
    if type(co) == "function" and not func then
        func = co
        co = nil
        if not clr.runningco() then
            func()
        else
            clr.cofinally(combinefunc(clr.cofinally(), func))
        end
    else
        if not co then
            func()
        else
            if not clr.getucoroutine(co) then
                func()
            else
                clr.cofinally(co, combinefunc(clr.cofinally(co), func))
            end
        end
    end
end

function unity.cocontinue(co, func)
    if type(co) == "function" and not func then
        func = co
        co = nil
        if not clr.runningco() then
            func()
        else
            clr.cocontinue(combinefunc(clr.cocontinue(), func))
        end
    else
        if not co then
            func()
        else
            if not clr.getucoroutine(co) then
                func()
            else
                clr.cocontinue(co, combinefunc(clr.cocontinue(co), func))
            end
        end
    end
end

coroutine.abort = unity.abort
coroutine.isrunning = unity.isrunning
coroutine.running = unity.running
coroutine.finally = unity.cofinally
coroutine.continue = unity.cocontinue

function unity.asyncf(func)
    return function(...)
        return unity.async(func, ...)
    end
end

local asyncmeta = {} -- TODO: for pairs/ipairs, next, #, __call, ..., make it a udtable?
asyncmeta.__index = function(cls, key)
    if type(key) == "string" and string.sub(key, -5) == "Async" then
        local regtab = rawget(cls, "\022")
        if regtab then
            return regtab[key]
        end
    end
end
asyncmeta.__newindex = function(cls, key, value)
    if type(key) == "string" and string.sub(key, -5) == "Async" then
        local regtab = rawget(cls, "\022")
        if not regtab then
            regtab = {}
            rawset(cls, "\022", regtab)
        end
        regtab[key] = value
        if type(value) == "function" then
            local funcasync = function(self, ...)
                if type(self) == "table" and type(self.async) == "function" then
                    return self:async(value, self, ...)
                else
                    return unity.async(value, self, ...)
                end
            end
            local keyasync = string.sub(key, 1, -6)
            rawset(cls, keyasync, funcasync)
        end
    else
        rawset(cls, key, value)
    end
end

unity.asyncmeta = asyncmeta

unity.asyncclass = class("asyncclass")
setmetatable(unity.asyncclass, asyncmeta)
setmetatable(unity.base, asyncmeta)

return unity
