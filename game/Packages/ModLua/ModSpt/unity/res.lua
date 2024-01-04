local UnityEngine = clr.UnityEngine
local ResManager = clr.UnityEngineEx.ResManager
local Object = UnityEngine.Object
local GameObject = UnityEngine.GameObject
local LuaBehav = clr.LuaBehav
local EventSystem = UnityEngine.EventSystems.EventSystem
local Mask = UnityEngine.UI.Mask
local Canvas = UnityEngine.Canvas
local WaitForEndOfFrame = UnityEngine.WaitForEndOfFrame

res = {}

res.preProcess = nil
res.postProcess = nil

function res.DoPreProcess(arg)
    if res.preProcess ~= nil and type(res.preProcess) == 'function' then
        res.preProcess(arg)
    end
end

function res.DoPostProcess(arg)
    if res.postProcess ~= nil and type(res.postProcess) == 'function' then
        res.postProcess(arg)
    end
end

--#region Instantiate and Scene Tree
function res.GetLuaScript(obj)
    if obj and obj ~= clr.null then
        return obj:GetComponent(LuaBehav)
    end
end

function res.Instantiate(name)
    if type(name) == "string" then
        local prefab = ResManager.LoadRes(name)
        if prefab then
            local obj = Object.Instantiate(prefab)
            if obj then
                return obj, res.GetLuaScript(obj)
            end
        end
    else
        local tempobj = name
        if tempobj and tempobj ~= clr.null and clr.is(tempobj, Object) then
            local obj = Object.Instantiate(tempobj)
            if obj then
                if obj.GetComponent then
                    return obj, res.GetLuaScript(obj)
                else
                    return obj
                end
            end
        end
    end
end

-- 异步加载
function res.InstantiateAsync(name)
    if type(name) == "string" then
        local loadinfo = ResManager.LoadResAsync(name)
        if loadinfo then
            while not loadinfo.Done do
                coroutine.yield(WaitForEndOfFrame())
                if not loadinfo or loadinfo == clr.null then
                    break
                end
            end
        end
        local prefab = loadinfo and loadinfo.Result
        if prefab then
            local obj = Object.Instantiate(prefab)
            if obj then
                return obj, res.GetLuaScript(obj)
            end
        end
    else
        local tempobj = name
        if tempobj and tempobj ~= clr.null and clr.is(tempobj, Object) then
            local obj = Object.Instantiate(tempobj)
            if obj then
                if obj.GetComponent then
                    return obj, res.GetLuaScript(obj)
                else
                    return obj
                end
            end
        end
    end
end

function res.Destroy(obj)
    Object.Destroy(obj)
end

function res.AddChild(parent, name)
    local child = res.Instantiate(name)
    if child then
        if parent then
            child.transform:SetParent(parent.transform, false)
        end
    end
    return child
end

function res.ClearChildren(parentTrans)
    if parentTrans and parentTrans.childCount > 0 then
        for i = 1, parentTrans.childCount do
            Object.Destroy(parentTrans:GetChild(i - 1).gameObject)
        end
    end
end

function res.ClearChildrenImmediate(parentTrans)
    if parentTrans and parentTrans.childCount > 0 then
        for i = 1, parentTrans.childCount do
            Object.DestroyImmediate(parentTrans:GetChild(0).gameObject)
        end
    end
end
--#endregion Instantiate and Scene Tree

--#region Dialog Order
local usedOrder = {}
local currentOrder = 0

function res.SetDialogOrder(order)
    if usedOrder[order] == 1 then  --如果即将设置的层级已被占用，则重新计算层级
        local newOrder = res.ApplyDialogOrder()
        usedOrder[newOrder] = 1
        if newOrder > currentOrder then
            currentOrder = newOrder
        end
        return newOrder
    end
    usedOrder[order] = 1
    if order > currentOrder then
        currentOrder = order
    end
    return order
end

function res.ApplyDialogOrder()
    currentOrder = currentOrder + 100
    usedOrder[currentOrder] = 1
    return currentOrder
end

function res.GetCurrentDialogOrder()
    return currentOrder
end

function res.RestoreDialogOrder(order)
    usedOrder[order] = nil
    if order == currentOrder then
        currentOrder = 0
        for k, v in pairs(usedOrder) do
            if k > currentOrder then
                currentOrder = k
            end
        end
    end
end
--#endregion Dialog Order

--#region Raw Load and Cleanup
function res.DestroyAll()
    ResManager.DestroyAll()
end
function res.DestroyAllHard()
    ResManager.DestroyAllHard()
end

function res.WaitForCodeGC()
    collectgarbage("restart")
    
    if clr.UnityEngineEx.ResManager.GarbageCollector.FireAndWaitCodeGC then
        clr.UnityEngineEx.ResManager.GarbageCollector.FireAndWaitCodeGC()
        return
    end

    local isGCPaused = clr.UnityEngine.Scripting.GarbageCollector.GCMode == clr.UnityEngine.Scripting.GarbageCollector.Mode.Disabled
    if isGCPaused then
        clr.UnityEngine.Scripting.GarbageCollector.GCMode = clr.UnityEngine.Scripting.GarbageCollector.Mode.Enabled
    end

    local man = clr.LuaLib.LuaThreadRefHelper.GetOrCreateRefMan(clr.topointer(clr.thislua()))

    for i = 1, 3 do
        clr.System.GC.Collect()
        collectgarbage()
        if not clr.UnityEngine.Application.isEditor then
            if clr.UnityEngine.Scripting.GarbageCollector.isIncremental then
                while clr.UnityEngine.Scripting.GarbageCollector.CollectIncremental(10000000) do
                end
            end
        end
        man["@npub"]:DoPendingRecycle()
    end

    if isGCPaused then
        clr.UnityEngine.Scripting.GarbageCollector.GCMode = clr.UnityEngine.Scripting.GarbageCollector.Mode.Disabled
    end
end

function res.DestroyAllHardSafe()
    _ = nil
    clr.UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset = res.defaultRenderPipelineAsset

    if clr.UnityEngineEx.ResManager.DestroyAllHardSafe then
        clr.UnityEngineEx.ResManager.DestroyAllHardSafe()
    else
        local ddobjsraw = clr.UnityEngineEx.DontDestroyOnLoadManager.GetAllDontDestroyOnLoadObjs()
        local ddobjs = clr.table(ddobjsraw)
        local destroyHandlers = clr.table(clr.UnityEngineEx.ResManager.DestroyHandlers)
        for i, v in ipairs(destroyHandlers) do
            v:PreDestroy(ddobjsraw)
        end
        for i, v in ipairs(ddobjs) do
            Object.Destroy(v)
        end
        res.DestroyAll()
    end

    if not clr.LuaBehav.WillAutoDisposeLuaBinding then
        local reg = _G["@cinstreg"]
        if reg then
            for k, v in pairs(reg) do
                if k == clr.null then
                    --local kc = k.class
                    for ki, vi in pairs(k) do
                        if type(ki) ~= "userdata" then
                            k[ki] = nil
                        end
                    end
                    reg[k] = nil
                end
            end
        end
    end

    res.WaitForCodeGC()

    -- local list = clr.table(clr.LuaLib.LuaRef.AliveRefids)
    -- local map = {}
    -- for i, v in ipairs(list) do
    --     map[v] = true
    -- end
    -- if not ___TEMP__DEBUG_LIST_REFIDS then
    --     ___TEMP__DEBUG_LIST_REFIDS = map
    -- else
    --     local old = ___TEMP__DEBUG_LIST_REFIDS
    --     ___TEMP__DEBUG_LIST_REFIDS = map
    --     local delta = {}
    --     for k, v in pairs(map) do
    --         if not old[k] then
    --             delta[#delta + 1] = k
    --         end
    --     end
    --     for i, v in ipairs(delta) do
    --         clr.LuaLib.LuaRef.TrackRefid(v)
    --     end
    --     if next(delta) then
    --         dumpe(delta, "DeltaRefids")
    --     end
    -- end
end

function res.RestoreAfterDestroyAllHardSafe()
    local _ = clr.StackingMainCamera.Instance
    clr.UnityEngineEx.UIResManager.FindUICamera()
    require("ui.debug.DebugEntry").Init()
    luaevt.trig("___EVENT__SHOW_CHANGE_SERVER_BUTTON")
end

function res.Cleanup()
    ResManager.Cleanup()
end

function res.LoadRes(name, type)
    return ResManager.LoadRes(name, type)
end

function res.LoadResAsync(name, type)
    return ResManager.LoadResAsync(name, type)
end

function res.LoadScene(name, additive)
    ResManager.LoadScene(name, additive)
end

function res.LoadSceneAsync(name, additive)
    return ResManager.LoadSceneAsync(name, additive)
end

function res.UnloadAllRes(unloadPermanentBundle)
    ResManager.UnloadAllRes(not not unloadPermanentBundle)
end

function res.GetLuaMemory()
    local memory = collectgarbage("count")
    return string.format("%.2fM", (memory * 0.001))
end
--#endregion Raw Load and Cleanup

--#region Res Cache
local ResCache = {}

function res.CacheRes(name)
	local handle = ResCache[name]
	if handle then
		handle.AddRef()  --Lua assist checked flag
	else
		local obj = ResManager.LoadRes(name)
		if obj then
            local handle = {}
            handle.obj = obj
			ResCache[name] = handle

			local RefCnt = 1
			handle.AddRef = function()
				RefCnt = RefCnt + 1
			end
			handle.Release = function()
				RefCnt = RefCnt - 1
				if RefCnt <= 0 then
					handle.Destroy()  --Lua assist checked flag
				end
			end
			handle.Destroy = function()
				handle.obj = nil
				ResCache[name] = nil
			end
		end
    end
end

function res.UncacheRes(name)
    local handle = ResCache[name]
    if handle then
        handle.Release()  --Lua assist checked flag
    end
end

function res.ClearResCache()
	local handles = {}
	for k, v in pairs(ResCache) do
		handles[#handles + 1] = v
	end
	for i, v in ipairs(handles) do
		v.Destroy()  --Lua assist checked flag
	end
end

function res.DontDestroyOnLoad(obj)
    Object.DontDestroyOnLoad(obj)
end
--#endregion Res Cache

function res.GetCurrentEventSystem()
    if res.currentEventSystem and res.currentEventSystem ~= clr.null then
        return res.currentEventSystem
    end

    local esObj = GameObject.Find("UICameraAndEventSystem(Clone)/EventSystem")
    if esObj ~= nil and esObj ~= clr.null then
        local eventSystemComp = esObj:GetComponent(EventSystem)
        if eventSystemComp and eventSystemComp ~= clr.null then
            res.currentEventSystem = eventSystemComp
            return eventSystemComp
        end
    end
    
    local eventSystemComp = EventSystem.current
    if eventSystemComp and eventSystemComp ~= clr.null then
        res.currentEventSystem = eventSystemComp
        return eventSystemComp
    end

    --TODO: Find first disabled EventSystem.
    res.currentEventSystem = nil
    return nil
end

function res.SetCurrentEventSystemEnabled(enabled)
    local eventSystem = res.GetCurrentEventSystem()
    res.SetEventSystemEnabled(eventSystem, enabled)
end

function res.SetEventSystemEnabled(eventSystemComp, enabled)
    if eventSystemComp then
        eventSystemComp.enabled = enabled
    end
end

-- 判断物体是否在mask中，
-- 主要用于自己写的shader在mask和非mask下的模板缓冲id的设置
-- tf:transform
function res.IsInMask(tf)
    if tf and tf ~= clr.null then
        local mask = tf:GetComponentInParent(Mask)
        return mask and mask ~= clr.null
    end
    return false
end

function res.FindCanvasLayerNameAndOrder(tf)
    local lLayerName, order
    if tf and tf ~= clr.null then
        local canvas = tf:GetComponentInParent(Canvas)
        if canvas and canvas ~= clr.null then
            order = canvas.sortingOrder
            lLayerName = canvas.sortingLayerName
        end
    end
    return lLayerName, order
end

res.defaultRenderPipelineAsset = clr.UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset

return res
