local ResManager = clr.UnityEngineEx.ResManager
local UIResManager = clr.UnityEngineEx.UIResManager
local UnityEngine = clr.UnityEngine
local Object = UnityEngine.Object
local GameObject = UnityEngine.GameObject
local Canvas = UnityEngine.Canvas
local RectTransform = UnityEngine.RectTransform
local Vector2 = UnityEngine.Vector2
local RenderMode = UnityEngine.RenderMode
local LuaBehav = clr.LuaBehav
local Time = UnityEngine.Time

local res = require("unity.res")

local unmanagedDialogs = {}
local unmanagedBlockDialogs =
{
    ["Game/UI/Common/Template/Loading/WaitForPost2.prefab"] = true,
}


-- 阴影类型 现在dialog阴影分为：White；Black
res.shadowType = {
    White = {
        blurRadius = 8,
        iteration = 2,
        rtDownScaling = 2,
        time = 0,
        rate = 1,
    },
    Black = {
        blurRadius = 0,
        iteration = 1,
        rtDownScaling = 1,
        time = 0,
        rate = 0.3,
    }
}

local function ShowDialogPrefabName(dialog)
    if not res.IsClrNull(dialog) then
        local safeArea = dialog:GetComponentInChildren(clr.SafeAreaRect).gameObject
        local spt = safeArea:GetComponentInChildren(LuaBehav)
        if not res.IsClrNull(spt) then
            return spt.gameObject.name
        end
    end

    return nil
end

--#region Basic and Override
function res.IsClrNull(obj)
    return obj == nil or obj == clr.null
end

function res.SetUICamera(obj, camera)
    if obj then
        local canvas = obj:GetComponent(Canvas)
        if not res.IsClrNull(canvas) then
            canvas.worldCamera = camera
        end
    end
end

function res.SetCanvasSortingLayer(obj, layerName)
    if obj then
        local canvas = obj:GetComponent(Canvas)
        if not res.IsClrNull(canvas) and canvas.sortingLayerName ~= layerName then
            canvas.sortingLayerName = layerName
        end
    end
end

local __old_Instantiate = res.Instantiate
function res.Instantiate(name)
    local obj, spt = __old_Instantiate(name)
    if obj and obj ~= clr.null and obj.GetComponent then
        local canvas = obj:GetComponent(Canvas)
        if canvas and canvas ~= clr.null then
            res.SetUICamera(obj, UIResManager.FindUICamera())
        end
    end
    return obj, spt
end

function res.GetDialogCamera()
    return UIResManager.GetUIDialogCamera()
end

function res.GetMainCamera()
    return UIResManager.FindUICamera()
end
--#endregion Basic and Override

--#region View Cache Stack
--[[
res.curSceneInfo = {
    view = nil,
    ctrl = nil,
    path = nil,
    blur = true,
    dialogs = {
        {
            view = nil,
            ctrl = nil,
            path = nil,
            order = xxx,
        },
        {
            view = nil,
            ctrl = nil,
            path = nil,
            order = xxx,
        },
        {
            view = nil,
            ctrl = nil,
            path = nil,
            order = xxx,
        },
    },
}
--]]

res.LoadType = {
    Change = "change",
    Push = "push",
    Pop = "pop",
}

res.sceneSeq = 0
res.sceneCache = {}
--[[
{
    path = {
        objs = xxx,
        view = xxx
        seq = 0,
        ctrl = xxx,
    },
    ...
}
--]]

local function GetSceneSeq()
    res.sceneSeq = res.sceneSeq + 1
    return res.sceneSeq
end

local sceneCacheMax = 2

function res.SetSceneCacheMax(cnt)
    if type(cnt) ~= "number" or cnt < 1 then
        cnt = 1
    end
    sceneStackMax = cnt
end

function res.GetSceneCacheMax()
    return sceneCacheMax
end

-- 等判断出机型等级后，再设置
function res.SetSceneCacheMax(level)
    if level == 3 then
        sceneCacheMax = 4
    elseif level == 2 then
        sceneCacheMax = 2
    else
        sceneCacheMax = 2
    end
end

function res.DestroyGameObjectList(objs)
    if res.IsClrNull(objs) then return end
    local lst = clr.table(objs)
    for i, v in ipairs(lst) do
        if not res.IsClrNull(v) then
            Object.Destroy(v)
        end
    end
end

local function TrimSceneCache(isNoCollectGarbage)
    if table.nums(res.sceneCache) <= sceneCacheMax then return end
    local sceneTable = {}
    for k, v in pairs(res.sceneCache) do
        local sceneInfo = v
        sceneInfo.path = k
        table.insert(sceneTable, sceneInfo)
    end
    table.sort(sceneTable, function(a, b) return a.seq > b.seq end)

    res.sceneCache = {}
    for i, v in ipairs(sceneTable) do
        if i <= sceneCacheMax then
            local path = v.path
            res.sceneCache[path] = v
        elseif not res.IsClrNull(v.objs) then
            res.DestroyGameObjectList(v.objs)
            v.objs = nil
            v.view = nil
        end
    end
    if isNoCollectGarbage ~= true and #sceneTable > sceneCacheMax then
        res.CollectGarbage(1)
    end
end

function res.ClearSceneCache()
    for k, v in pairs(res.sceneCache) do
        res.DestroyGameObjectList(v.objs)
        v.objs = nil
    end
    res.sceneCache = {}
    res.sceneSeq = 0
end

local function SaveCurrentSceneInfo()
    local dialogObjs = {}
    if type(res.curSceneInfo) == "table" and type(res.curSceneInfo.dialogs) == "table" then
        for i, v in ipairs(res.curSceneInfo.dialogs) do
            table.insert(dialogObjs, v.view.dialog.gameObject)
            res.RestoreDialogOrder(v.view.dialog.currentOrder)
        end
    end
    local dialogObjsArr = clr.array(dialogObjs, GameObject)
    local pack = UIResManager.PackSceneAndDialogs(dialogObjsArr)
    local sgos = pack.SceneObjs

    local dialogObjs = pack.DialogObjs
    local dgos = clr.table(dialogObjs)
    local dgosDisable = {}
    local sgoDisable = true
    local isTrimSceneCache = false

    local sceneCacheItem
    if type(res.curSceneInfo) == "table" and not res.IsClrNull(res.curSceneInfo.view) then
        sceneCacheItem = res.sceneCache[res.curSceneInfo.path]
        if not sceneCacheItem then
            sceneCacheItem = {
                objs = sgos,
                view = res.curSceneInfo.view,
                seq = GetSceneSeq(),
                ctrl = res.curSceneInfo.ctrl,
                pack = pack,
            }
            if res.curSceneInfo.ctrl.needSceneCache then
                res.sceneCache[res.curSceneInfo.path] = sceneCacheItem
                isTrimSceneCache = true
            end
        else
            sceneCacheItem.seq = GetSceneSeq()
            if res.IsClrNull(sceneCacheItem.obj) then
                sceneCacheItem.obj = sgos
                sceneCacheItem.ctrl = res.curSceneInfo.ctrl
            end
        end
    else
        sceneCacheItem = {
            objs = sgos,
            pack = pack,
        }
    end

    if type(res.curSceneInfo) == "table" and res.curSceneInfo.ctrl then
        if type(res.curSceneInfo.ctrl.OnExitScene) == "function" then
            ppcall(function()
                res.curSceneInfo.ctrl:OnExitScene()
            end)
        end
    end

    for i, dgo in ipairs(dgos) do
        if type(res.curSceneInfo) == "table" and type(res.curSceneInfo.dialogs) == "table" then
            local curDialogInfo = res.curSceneInfo.dialogs[i]
            if type(curDialogInfo) == "table" and curDialogInfo.view ~= clr.null then
                table.insert(dgosDisable, true)
                local dgosItem = res.sceneCache[curDialogInfo.path]
                if not dgosItem then
                    if res.curSceneInfo.ctrl.needSceneCache then
                        res.sceneCache[curDialogInfo.path] = {
                            obj = dgo,
                            view = curDialogInfo.view,
                            seq = GetSceneSeq(),
                            ctrl = curDialogInfo.ctrl,
                        }
                        isTrimSceneCache = true
                    end
                else
                    dgosItem.seq = GetSceneSeq()
                    if res.CacheObjIsClrNull(dgosItem.obj) then
                        dgosItem.obj = dgo
                        dgosItem.ctrl = curDialogInfo.ctrl
                    end
                end
            else
                table.insert(dgosDisable, false)
            end
            if type(curDialogInfo) == "table" then
                if curDialogInfo.ctrl and type(curDialogInfo.ctrl.OnExitScene) == "function" then
                    curDialogInfo.ctrl:OnExitScene()
                end
            end
        else
            table.insert(dgosDisable, false)
        end
    end

    sceneCacheItem.dgos = dgos
    sceneCacheItem.dgosDisable = dgosDisable

    if isTrimSceneCache == true then
        TrimSceneCache()
    end

    return sceneCacheItem
end

local function DisableOrDestroyCurrentSceneObj(isLoadScene, sgos, dialogObjs)
    if not res.CacheObjIsClrNull(sgos) then
        if sgoDisable  then
            ResManager.SetCacheActive(sgos,true,false)
        else
            res.DestroyGameObjectList(sgos)
        end
    end
    if type(dgos) == "table" then
        ResManager.SetCacheActive(dialogObjs,true,false)
        for i, dgo in ipairs(dgos) do
            if not res.IsClrNull(dgo) and not dgosDisable[i] then
               Object.Destroy(dgo)
            end
        end
    end
     if isLoadScene == true then
        res.ClearSceneCache()
    end
end

local function CloseDialog()
    if type(res.curSceneInfo) == "table" and type(res.curSceneInfo.dialogs) == "table" and #res.curSceneInfo.dialogs > 0 then
        local maxIndex = 0
        local maxOrder = -1
        for i, v in ipairs(res.curSceneInfo.dialogs) do
            local order = v.order
            if maxOrder < order then
                maxOrder = order
                maxIndex = i
            end
        end
        if maxIndex > 0 then
            local dialog = res.curSceneInfo.dialogs[maxIndex]
            if type(dialog) == "table" and dialog.view and not res.IsClrNull(dialog.view) and type(dialog.view.closeDialog) == "function" then
                dialog.view:closeDialog()
                return true
            end
        end
    end
end

local dontDestroyRootForSavedScene
local function MoveToDontDestroy(sceneCacheItem)
    if sceneCacheItem and sceneCacheItem.pack then
        if res.IsClrNull(dontDestroyRootForSavedScene) then
            dontDestroyRootForSavedScene = GameObject("DontDestroyRootForSavedScene")
            Object.DontDestroyOnLoad(dontDestroyRootForSavedScene)
            --dontDestroyRootForSavedScene:SetActive(false)
        end
        local sgos = clr.table(sceneCacheItem.pack.SceneObjs)
        for i, v in ipairs(sgos) do
            if not res.IsClrNull(v) and not res.IsClrNull(v.transform) then
                v.transform:SetParent(dontDestroyRootForSavedScene.transform, false)
            end
        end
    end
end

local sceneAndDialogCache
local SceneAndDialogCacheLayer = 17
local function MoveToSceneAndDialogCache(sceneCacheItem)
    if sceneCacheItem and sceneCacheItem.pack then
        if res.IsClrNull(sceneAndDialogCache) then
            sceneAndDialogCache = GameObject("SceneAndDialogCache")
            sceneAndDialogCache:AddComponent(Canvas)
            sceneAndDialogCache:SetActive(false)
            res.ChangeGameObjectLayer(sceneAndDialogCache, SceneAndDialogCacheLayer)
        end
        local sgos = clr.table(sceneCacheItem.pack.SceneObjs)
        for i, v in ipairs(sgos) do
            if not res.IsClrNull(v) and not res.IsClrNull(v.transform) then
                v.transform:SetParent(sceneAndDialogCache.transform, false)
                v:SetActive(false)
            end
        end
        local dgos = clr.table(sceneCacheItem.pack.DialogObjs)
        for i,v in ipairs(dgos) do
            if not res.IsClrNull(v) and not res.IsClrNull(v.transform) then
                v.transform:SetParent(sceneAndDialogCache.transform, false)
                v:SetActive(false)
            end
        end
    end
end

local function DisableCachedScene(sceneCacheItem)
    if sceneCacheItem and sceneCacheItem.pack then
        if sceneCacheItem.view and sceneCacheItem.ctrl.needSceneCache then
            MoveToSceneAndDialogCache(sceneCacheItem)
        else
            local pack = sceneCacheItem.pack
            local sgos = clr.table(pack.SceneObjs)
            for i, v in ipairs(sgos) do
                if not res.IsClrNull(v) then
                    Object.Destroy(v)
                end
            end
            local dgos = clr.table(pack.DialogObjs)
            for i,v in ipairs(dgos) do
                if not res.IsClrNull(v) then
                    Object.Destroy(v)
                end
            end
        end
    end
end

local function EnableCachedScene(sceneCacheItem)
    if sceneCacheItem and sceneCacheItem.view and sceneCacheItem.pack then
        local pack = sceneCacheItem.pack
        local sgos = clr.table(pack.SceneObjs)
        local sceneObjsActive = clr.table(pack.InitialSceneObjsActive)
        for i, v in ipairs(sgos) do
            if not res.IsClrNull(v) then
                v.transform:SetParent(nil, false)
                v:SetActive(sceneObjsActive[i])
            end
        end

        local dgos = clr.table(pack.DialogObjs)
        local dialogObjsActive = clr.table(pack.InitialDialogObjsActive)
        for i,v in ipairs(dgos) do
            if not res.IsClrNull(v) then
                v.transform:SetParent(nil, false)
                v:SetActive(dialogObjsActive[i])
            end
        end
    end
end

local function EnableCachedDialog(dialogCacheItem)
    if dialogCacheItem and dialogCacheItem.obj then
        dialogCacheItem.obj.transform:SetParent(nil, false)
        dialogCacheItem.obj:SetActive(true)
    end
end

local function ClearCurrentSceneInfo()
    res.curSceneInfo = nil
end
--#endregion View Cache Stack

--#region Controller Cache Stack
res.ctrlStack = {}
--[[
{
    {
        path = xxx,
        args = xxx,
        argc = xxx,
        blur = true,
        dialogs = {
            {
                path = nil,
                order = xxx,
                args = xxx,
                argc = xxx,
            },
            {
                path = nil,
                order = xxx,
                args = xxx,
                argc = xxx,
            },
            {
                path = nil,
                order = xxx,
                args = xxx,
                argc = xxx,
            },
        },
    },
}
--]]

local ctrlStackMax = math.max_int32 

function res.SetCtrlStackMax(cnt)
    if type(cnt) ~= "number" or cnt < 1 then
        cnt = 1
    end
    ctrlStackMax = cnt
end

function res.GetCtrlStackMax()
    return ctrlStackMax
end

local function TrimCtrlStack()
    if #res.ctrlStack > ctrlStackMax then
        for i = ctrlStackMax + 1, #res.ctrlStack do
            res.ctrlStack[i] = nil
        end
    end
end

function res.ClearCtrlStack()
    res.ctrlStack = {}
end

function res.GetLastCtrlPath()
    if #res.ctrlStack > 0 then
        return res.ctrlStack[#res.ctrlStack].path
    end
end

-- function res.GetTopCtrl()
--     if type(res.curSceneInfo) == "table" then
--         if type(res.curSceneInfo.dialogs) == "table" then
--             if #res.curSceneInfo.dialogs > 0 then
--                 return res.curSceneInfo.dialogs[#res.curSceneInfo.dialogs].ctrl
--             end
--         end
--         return res.curSceneInfo.ctrl
--     end
-- end

-- function res.RemoveLastCtrlData()
--     if #res.ctrlStack > 0 then
--         res.ctrlStack[#res.ctrlStack] = nil
--     end
-- end

local function SaveCurrentStatusData()
    -- 如果之前的场景是有ctrl的prefab，则保存其信息
    if type(res.curSceneInfo) == "table" and res.curSceneInfo.ctrl then
        -- 如果这个ctrl不需要缓存，那么就不需要添加到ctrl栈帧里
        if not res.curSceneInfo.ctrl.needSceneCache then
            return
        end
        -- 保存ctrl恢复的数据信息
        local args = {res.curSceneInfo.ctrl:GetStatusData()}
        local argc = select('#', res.curSceneInfo.ctrl:GetStatusData())

        local ctrlInfo = {
            path = res.curSceneInfo.path,
            args = args,
            argc = argc,
            blur = res.curSceneInfo.blur,
        }

        table.insert(res.ctrlStack, ctrlInfo)
        TrimCtrlStack()
    end

    if type(res.curSceneInfo) == "table" and type(res.curSceneInfo.dialogs) == "table" and #res.curSceneInfo.dialogs > 0 then
        res.ctrlStack[#res.ctrlStack].dialogs = {}
        for i, dialogInfo in ipairs(res.curSceneInfo.dialogs) do
            local args = {dialogInfo.ctrl:GetStatusData()}
            local argc = select('#', dialogInfo.ctrl:GetStatusData())

            local ctrlInfo = {
                path = dialogInfo.path,
                args = args,
                argc = argc,
                order = dialogInfo.order,
            }

            table.insert(res.ctrlStack[#res.ctrlStack].dialogs, ctrlInfo)
        end
    end
end
--#endregion Controller Cache Stack

local function LoadPrefabDialog(loadType, ctrlPath, order, ...)
    cache.setGlobalTempData(true, "LoadingPrefabDialog")

    -- 记录当前窗口信息
    local dialogInfo = {}
    if type(res.curSceneInfo.dialogs) ~= "table" then
        res.curSceneInfo.dialogs = {}
    end
    table.insert(res.curSceneInfo.dialogs, dialogInfo)
    dialogInfo.path = ctrlPath

    local cachedSceneInfo = res.sceneCache[ctrlPath]
    res.sceneCache[ctrlPath] = nil
    local ctrlClass = require(ctrlPath)

    local args = {...}
    local argc = select('#', ...)

    local function CreateDialog()
        local viewPath = ctrlClass.viewPath
        local dialogStatus = ctrlClass.dialogStatus
        local dialog, dialogcomp = res.ShowDialog(viewPath, "camera", dialogStatus.touchClose, dialogStatus.withShadow, dialogStatus.unblockRaycast, true, nil,
                dialogStatus.noNeedSafeArea, dialogStatus.shadowType)
        dialogInfo.view = dialogcomp.contentcomp
        dialogInfo.order = dialog:GetComponent(Canvas).sortingOrder
        dialogInfo.ctrl = ctrlClass.new(dialogInfo.view, unpack(args, 1, argc))
        dialogInfo.ctrl.__loadType = loadType
        if ENABLE_PROFILER_LUA_DEEP then
            clr.beginsample("MVC-Control Refresh {0}", ctrlClass.__cname)
        end
        dialogInfo.ctrl:Refresh(unpack(args, 1, argc))
        if ENABLE_PROFILER_LUA_DEEP then
            clr.endsample()
        end
        res.GetLuaScript(dialog).OnExitScene = function ()
            if type(dialogInfo.ctrl.OnExitScene) == "function" then
                dialogInfo.ctrl:OnExitScene()
            end
        end
    end

    if type(cachedSceneInfo) == "table" then
        if not res.IsClrNull(cachedSceneInfo.obj) then
            dialogInfo.view = cachedSceneInfo.view
            dialogInfo.ctrl = cachedSceneInfo.ctrl
            dialogInfo.order = order
            dialogInfo.view.dialog:setOrder(order)
            dialogInfo.ctrl.__loadType = loadType
            if ENABLE_PROFILER_LUA_DEEP then
                clr.beginsample("MVC-Control Refresh {0}", dialogInfo.ctrl.__cname)
            end
            dialogInfo.ctrl:Refresh(unpack(args, 1, argc))
            if ENABLE_PROFILER_LUA_DEEP then
                clr.endsample()
            end

            -- ResManager.UnpackSceneObj(cachedSceneInfo.obj,true)
            -- EnableCachedScene(cachedSceneInfo)
            EnableCachedDialog(cachedSceneInfo)

            local scd = res.GetLastSCD(true)
            res.AdjustDialogCamera(scd, dialogInfo.view.gameObject, dialogInfo.view.dialog.withShadow)
            if ctrlClass.dialogStatus.withShadow then
                if res.NeedDialogCameraBlur() then
                    res.SetMainCameraBlur(true)
                end
            end
        else
            res.sceneCache[ctrlPath] = nil
            CreateDialog()
        end
    else
        CreateDialog()
    end

    cache.removeGlobalTempData("LoadingPrefabDialog")
    return dialogInfo.ctrl
end

--#region Load Prefab/Scene as Scene
local function LoadPrefabScene(loadType, ctrlPath, dialogData, ...)
    res.DoPreProcess(ctrlPath)
    if res.NeedDialogCameraBlur() then
        res.SetMainCameraBlur(false)
    end
    res.SetCurrentEventSystemEnabled(false)
    -- 记录当前场景信息res.curSceneInfo
    res.curSceneInfo = {
        path = ctrlPath,
    }
    local cachedSceneInfo = res.sceneCache[ctrlPath]
    res.sceneCache[ctrlPath] = nil
    local ctrlClass = require(ctrlPath)
    local args = {...}
    local argc = select('#', ...)

    local function CreateDialogs()
        if type(dialogData) == "table" then
            table.sort(dialogData, function(a, b) return tonumber(a.order) < tonumber(b.order) end)
            for i, v in ipairs(dialogData) do
                LoadPrefabDialog(loadType, v.path, v.order, unpack(v.args, 1, v.argc))
            end
        end
    end

    local function CreateScene()
        local viewPath = ctrlClass.viewPath
        clr.coroutine(function()
            if string.sub(viewPath, -6) == '.unity' then
                local loadinfo = ResManager.LoadScene(viewPath)
                if loadinfo then
                    coroutine.yield(loadinfo)
                    unity.waitForNextEndOfFrame()
                    res.curSceneInfo.view = cache.removeGlobalTempData("MainManager")
                else
                    local mainManager
                    local waitFrames = 0
                    repeat
                        mainManager = cache.removeGlobalTempData("MainManager")
                        unity.waitForNextEndOfFrame()
                        waitFrames = waitFrames + 1
                    until mainManager or waitFrames > 10
                    res.curSceneInfo.view = mainManager
                end

                res.ClearSceneCache()
                res.CollectGarbage(1)
            else
                local prefab = res.LoadRes(viewPath)
                if prefab then
                    local obj = Object.Instantiate(prefab)
                    local camera = UIResManager.FindUICamera()
                    res.SetUICamera(obj, camera)
                    res.SetCanvasSortingLayer(obj, "UI")
                    res.curSceneInfo.view = res.GetLuaScript(obj)
                end
            end

            res.curSceneInfo.ctrl = ctrlClass.new(res.curSceneInfo.view, unpack(args, 1, argc))
            res.curSceneInfo.ctrl.__loadType = loadType
            if ENABLE_PROFILER_LUA_DEEP then
                clr.beginsample("MVC-Control Refresh {0}", ctrlClass.__cname)
            end
            res.curSceneInfo.ctrl:Refresh(unpack(args, 1, argc))
            if ENABLE_PROFILER_LUA_DEEP then
                clr.endsample()
            end
            CreateDialogs()
            res.SetUIAudioListener(ctrlClass.viewPath)

            if res.curSceneInfo.ctrl and type(res.curSceneInfo.ctrl.OnLoadComplete) == "function" then
                res.curSceneInfo.ctrl:OnLoadComplete()
            end

            res.SetCurrentEventSystemEnabled(true)
            res.DoPostProcess(ctrlPath)
        end)
    end

    if type(cachedSceneInfo) == "table" then
        if not res.IsClrNull(cachedSceneInfo.objs) then
            res.curSceneInfo.ctrl = cachedSceneInfo.ctrl
            res.curSceneInfo.ctrl.__loadType = loadType
            EnableCachedScene(cachedSceneInfo)
            res.curSceneInfo.view = cachedSceneInfo.view
            if ENABLE_PROFILER_LUA_DEEP then
                clr.beginsample("MVC-Control Refresh {0}", res.curSceneInfo.ctrl.__cname)
            end
            res.curSceneInfo.ctrl:Refresh(unpack(args, 1, argc))
            if ENABLE_PROFILER_LUA_DEEP then
                clr.endsample()
            end

            CreateDialogs()
            res.SetUIAudioListener(ctrlClass.viewPath)
            if res.curSceneInfo.ctrl and type(res.curSceneInfo.ctrl.OnLoadComplete) == "function" then
                res.curSceneInfo.ctrl:OnLoadComplete()
            end
            res.SetCurrentEventSystemEnabled(true)
        else
            CreateScene()
        end
    else
        CreateScene()
    end
    return res.curSceneInfo.ctrl
end

local function LoadPrefabSceneAsync(loadType, ctrlPath, extra, ...)
    res.DoPreProcess(ctrlPath)
    if res.NeedDialogCameraBlur() then
        res.SetMainCameraBlur(false)
    end
    res.SetCurrentEventSystemEnabled(false)
    -- require("ui.control.button.LuaButton").frameCount = math.max_int32 - 3
    -- 记录当前场景信息res.curSceneInfo
    res.curSceneInfo = {
        path = ctrlPath,
    }
    local cachedSceneInfo = res.sceneCache[ctrlPath]
    res.sceneCache[ctrlPath] = nil
    local ctrlClass = require(ctrlPath)

    local args = {...}
    local argc = select('#', ...)

    local waitHandle = {}

    local function CreateDialogs()
        local dialogData = extra.dialogs
        if type(dialogData) == "table" then
            table.sort(dialogData, function(a, b) return a.order < b.order end)
            for i, v in ipairs(dialogData) do
                LoadPrefabDialog(loadType, v.path, v.order, unpack(v.args, 1, v.argc))
            end
        end
    end

    local function CreateScene()
        clr.coroutine(function()
            unity.waitForEndOfFrame()
            local viewPath = ctrlClass.viewPath
            local isLoadScene = string.sub(viewPath, -6) == ".unity" 
            if isLoadScene then
                -- 直接在下面DisableCachedScene
                -- if extra and extra.cacheItem then
                --     MoveToDontDestroy(extra.cacheItem)
                -- end
                local loadinfo = ResManager.LoadSceneAsync(viewPath)
                if loadinfo then
                    coroutine.yield(loadinfo)
                    unity.waitForNextEndOfFrame()
                    res.curSceneInfo.view = cache.removeGlobalTempData("MainManager")
                    res.curSceneInfo.ctrl = ctrlClass.new(res.curSceneInfo.view, unpack(args, 1, argc))
                    res.curSceneInfo.ctrl.__loadType = loadType
                    if ENABLE_PROFILER_LUA_DEEP then
                        clr.beginsample("MVC-Control Refresh {0}", ctrlClass.__cname)
                    end
                    res.curSceneInfo.ctrl:Refresh(unpack(args, 1, argc))
                    if ENABLE_PROFILER_LUA_DEEP then
                        clr.endsample()
                    end
                    waitHandle.ctrl = res.curSceneInfo.ctrl
                else
                    local mainManager
                    local waitFrames = 0
                    repeat
                        mainManager = cache.removeGlobalTempData("MainManager")
                        unity.waitForNextEndOfFrame()
                        waitFrames = waitFrames + 1
                    until mainManager or waitFrames > 10

                    res.curSceneInfo.view = mainManager
                    res.curSceneInfo.ctrl = ctrlClass.new(res.curSceneInfo.view, unpack(args, 1, argc))
                    res.curSceneInfo.ctrl.__loadType = loadType
                    if ENABLE_PROFILER_LUA_DEEP then
                        clr.beginsample("MVC-Control Refresh {0}", ctrlClass.__cname)
                    end
                    res.curSceneInfo.ctrl:Refresh(unpack(args, 1, argc))
                    if ENABLE_PROFILER_LUA_DEEP then
                        clr.endsample()
                    end
                    waitHandle.ctrl = res.curSceneInfo.ctrl
                end

                res.CollectGarbage(1)
            else
                local loadinfo = ResManager.LoadResAsync(ctrlClass.viewPath)
                if loadinfo then
                    coroutine.yield(loadinfo)
                    unity.waitForNextEndOfFrame()
                    local prefab = loadinfo.asset
                    if prefab then
                        local obj = Object.Instantiate(prefab)
                        local camera = UIResManager.FindUICamera()
                        res.SetUICamera(obj, camera)
                        res.curSceneInfo.view = res.GetLuaScript(obj)
                        res.curSceneInfo.ctrl = ctrlClass.new(res.curSceneInfo.view, unpack(args, 1, argc))
                        res.curSceneInfo.ctrl.__loadType = loadType
                        if ENABLE_PROFILER_LUA_DEEP then
                            clr.beginsample("MVC-Control Refresh {0}", ctrlClass.__cname)
                        end
                        res.curSceneInfo.ctrl:Refresh(unpack(args, 1, argc))
                        if ENABLE_PROFILER_LUA_DEEP then
                            clr.endsample()
                        end
                        waitHandle.ctrl = res.curSceneInfo.ctrl
                    end
                end
            end

            if extra and extra.cacheItem then
                DisableCachedScene(extra.cacheItem)
            end
            res.ClearSceneCache()
            CreateDialogs()
            res.SetUIAudioListener(ctrlClass.viewPath)
            waitHandle.done = true

            if res.curSceneInfo.ctrl and type(res.curSceneInfo.ctrl.OnLoadComplete) == "function" then
                res.curSceneInfo.ctrl:OnLoadComplete()
            end

            res.SetCurrentEventSystemEnabled(true)
            res.DoPostProcess(ctrlPath)
        end)
    end

    if type(cachedSceneInfo) == "table" then
        if not res.IsClrNull(cachedSceneInfo.objs) then
            EnableCachedScene(cachedSceneInfo)
            if extra and extra.cacheItem then
                DisableCachedScene(extra.cacheItem)
            end
            res.curSceneInfo.view = cachedSceneInfo.view
            res.curSceneInfo.ctrl = cachedSceneInfo.ctrl
            res.curSceneInfo.ctrl.__loadType = loadType
            if ENABLE_PROFILER_LUA_DEEP then
                clr.beginsample("MVC-Control Refresh {0}", res.curSceneInfo.ctrl.__cname)
            end
            res.curSceneInfo.ctrl:Refresh(unpack(args, 1, argc))
            if ENABLE_PROFILER_LUA_DEEP then
                clr.endsample()
            end

            CreateDialogs()
            waitHandle.done = true
            waitHandle.ctrl = res.curSceneInfo.ctrl
            res.SetUIAudioListener(ctrlClass.viewPath)
            if res.curSceneInfo.ctrl and type(res.curSceneInfo.ctrl.OnLoadComplete) == "function" then
                res.curSceneInfo.ctrl:OnLoadComplete()
            end

            res.SetCurrentEventSystemEnabled(true)
        else
            CreateScene()
        end
    else
        CreateScene()
    end

    return waitHandle
end
--#endregion Load Prefab/Scene as Scene

--#region Push/Pop Scenes
function res.LoadViewImmediate(name, ...)
    res.DoPreProcess(name)
    SaveCurrentStatusData()
    local cacheItem = SaveCurrentSceneInfo()
    ClearCurrentSceneInfo()

    if string.sub(name, -6) == '.unity' then
        -- cacheItem = nil
        ResManager.LoadScene(name)
        -- DisableCachedScene(cacheItem)
        res.ClearSceneCache()
        res.CollectGarbage(1)
        res.SetUIAudioListener(name)
        res.DoPostProcess(name)
    else
        local prefab = res.LoadRes(name)
        if prefab then
            local obj = Object.Instantiate(prefab)
            DisableCachedScene(cacheItem)
            local camera = UIResManager.FindUICamera()
            res.SetUICamera(obj, camera)
            res.SetUIAudioListener(name)
            res.DoPostProcess(name)
            return res.GetLuaScript(obj)
        end
    end
end

function res.LoadViewAsync(name, ...)
    res.DoPreProcess(name)
    SaveCurrentStatusData()
    local cacheItem = SaveCurrentSceneInfo()
    ClearCurrentSceneInfo()

    local waitHandle = {}
    clr.coroutine(function()
        unity.waitForEndOfFrame()
        local isLoadScene = string.sub(name, -6) == ".unity" 
        if isLoadScene then
            cacheItem = nil
            -- MoveToDontDestroy(cacheItem)
            local loadinfo = ResManager.LoadSceneAsync(name)
            if loadinfo then
                coroutine.yield(loadinfo)
                unity.waitForNextEndOfFrame()
            end

            res.ClearSceneCache()
            res.CollectGarbage(1)
        else
            local prefab
            local loadinfo = ResManager.LoadResAsync(name)
            if loadinfo then
                coroutine.yield(loadinfo)
                unity.waitForNextEndOfFrame()
                prefab = loadinfo.asset
            end
            if prefab then
                local obj = Object.Instantiate(prefab)
                local camera = UIResManager.FindUICamera()
                res.SetUICamera(obj, camera)
            end
        end
        DisableCachedScene(cacheItem)
        waitHandle.done = true
        res.SetUIAudioListener(name)
        res.DoPostProcess(name)
    end)
    return waitHandle
end

function res.LoadView(name, ...)
    res.DoPreProcess(name)
    local args = {...}
    local argc = select('#', ...)

    clr.coroutine(function()
        unity.waitForNextEndOfFrame()
        SaveCurrentStatusData()
        local cacheItem = SaveCurrentSceneInfo()
        ClearCurrentSceneInfo()
        if string.sub(name, -6) == ".unity" then
            -- cacheItem = nil
            ResManager.LoadScene(name)
            unity.waitForNextEndOfFrame()
            -- DisableCachedScene(cacheItem)
            res.ClearSceneCache()
            res.CollectGarbage(1)
            res.SetUIAudioListener(name)
            res.DoPostProcess(name)
        else
            local prefab = res.LoadRes(name)
            if prefab then
                local obj = Object.Instantiate(prefab)
                local camera = UIResManager.FindUICamera()
                unity.waitForNextEndOfFrame()
                DisableCachedScene(cacheItem)
                res.SetUICamera(obj, camera)
                res.SetUIAudioListener(name)
                res.DoPostProcess(name)
                return res.GetLuaScript(obj)
            end
        end
    end)
end

function res.PushSceneImmediate(ctrlPath, ...)
    SaveCurrentStatusData()
    local cacheItem = SaveCurrentSceneInfo()
    DisableCachedScene(cacheItem)
    ClearCurrentSceneInfo()

    return LoadPrefabScene(res.LoadType.Push, ctrlPath, nil, ...)
end

function res.PushSceneAsync(ctrlPath, ...)
    SaveCurrentStatusData()
    local cacheItem = SaveCurrentSceneInfo()
    ClearCurrentSceneInfo()

    return LoadPrefabSceneAsync(res.LoadType.Push, ctrlPath, { cacheItem = cacheItem }, ...)
end

function res.PushScene(ctrlPath, ...)
    local args = {...}
    local argc = select('#', ...)
    clr.coroutine(function()
        unity.waitForEndOfFrame()
        res.PushSceneImmediate(ctrlPath, unpack(args, 1, argc))
    end)
end

-- 如果当前最上层的是一个窗口，则只关闭这个窗口，否则关闭整个场景
function res.PopSceneImmediate(...)
    if not CloseDialog() then
        return res.PopSceneWithCurrentSceneImmediate(...)
    end
end

function res.PopSceneAsync(...)
    if not CloseDialog() then
        return res.PopSceneWithCurrentSceneAsync(...)
    end
end

-- 保存当前界面的现实对象缓存
function res.PopScene(...)
    if not CloseDialog() then
        return res.PopSceneWithCurrentScene(...)
    end
end

function res.PopSceneWithCurrentSceneImmediate(...)
    if #res.ctrlStack == 0 then return end

    local cacheItem = SaveCurrentSceneInfo()
    DisableCachedScene(cacheItem)
    ClearCurrentSceneInfo()
    -- restore old info
    local ctrlInfo = table.remove(res.ctrlStack)
    local ctrlPath = ctrlInfo.path
    local argc = select('#', ...)
    local args = {...}
    if argc == 0 then
        args = ctrlInfo.args
        argc = ctrlInfo.argc
    end
    --local isBlur = ctrlInfo.blur
    return LoadPrefabScene(res.LoadType.Pop, ctrlPath, ctrlInfo.dialogs, unpack(args, 1, argc))
end

function res.PopSceneWithCurrentSceneAsync(...)
    if #res.ctrlStack == 0 then return end

    local cacheItem = SaveCurrentSceneInfo()

    ClearCurrentSceneInfo()

    -- restore old info
    local ctrlInfo = table.remove(res.ctrlStack)
    local ctrlPath = ctrlInfo.path
    local argc = select('#', ...)
    local args = {...}
    if argc == 0 then
        args = ctrlInfo.args
        argc = ctrlInfo.argc
    end
    --local isBlur = ctrlInfo.blur

    local extra = {
        cacheItem = cacheItem,
        dialogs = ctrlInfo.dialogs,
    }
    return LoadPrefabSceneAsync(res.LoadType.Pop, ctrlPath, extra, unpack(args, 1, argc))
end

function res.PopSceneWithCurrentScene(...)
    local args = {...}
    local argc = select('#', ...)
    clr.coroutine(function()
        unity.waitForEndOfFrame()
        res.PopSceneWithCurrentSceneImmediate(unpack(args, 1, argc))
    end)
end

function res.PopSceneWithoutCurrentImmediate(...)
    if #res.ctrlStack == 0 then return end
    local sgos = UIResManager.PackSceneObj()
    res.DestroyGameObjectList(sgos)
    -- restore old info
    local ctrlInfo = table.remove(res.ctrlStack)
    local ctrlPath = ctrlInfo.path
    local argc = select('#', ...)
    local args = {...}
    if argc == 0 then
        args = ctrlInfo.args
        argc = ctrlInfo.argc
    end

    return LoadPrefabScene(res.LoadType.Pop, ctrlPath, nil, unpack(args, 1, argc))
end

function res.PopSceneWithoutCurrentAsync(...)
    if #res.ctrlStack == 0 then return end

    local pack = UIResManager.PackSceneAndDialogs()
    local cacheItem = { pack = pack, objs = pack.SceneObjs }
    -- restore old info
    local ctrlInfo = table.remove(res.ctrlStack)
    local ctrlPath = ctrlInfo.path
    local argc = select('#', ...)
    local args = {...}
    if argc == 0 then
        args = ctrlInfo.args
        argc = ctrlInfo.argc
    end

    local extra = {
        cacheItem = cacheItem,
        dialogs = ctrlInfo.dialogs,
    }
    return LoadPrefabSceneAsync(res.LoadType.Pop, ctrlPath, extra, unpack(args, 1, argc))
end

-- 不保存当前显示对象的缓存
function res.PopSceneWithoutCurrent(...)
    local args = {...}
    local argc = select('#', ...)
    clr.coroutine(function()
        unity.waitForEndOfFrame()
        res.PopSceneWithoutCurrentImmediate(unpack(args, 1, argc))
    end)
end

function res.ChangeSceneImmediate(ctrlPath, ...)
    SaveCurrentStatusData()
    local cacheItem = SaveCurrentSceneInfo()
    DisableCachedScene(cacheItem)
    res.ClearSceneCache()
    ClearCurrentSceneInfo()
    return LoadPrefabScene(res.LoadType.Change, ctrlPath, nil, ...)
end

function res.ChangeSceneAsync(ctrlPath, ...)
    SaveCurrentStatusData()
    local cacheItem = SaveCurrentSceneInfo()
    ClearCurrentSceneInfo()
    return LoadPrefabSceneAsync(res.LoadType.Change, ctrlPath, { cacheItem = cacheItem }, ...)
end

function res.ChangeScene(ctrlPath, ...)
    local args = {...}
    local argc = select('#', ...)
    clr.coroutine(function()
        unity.waitForEndOfFrame()
        res.ChangeSceneImmediate(ctrlPath, unpack(args, 1, argc))
    end)
end
--#endregion Push/Pop Scenes

function res.CacheHandle()
    SaveCurrentStatusData()
    local disableOrDestroySceneFunc = SaveCurrentSceneInfo()
    ClearCurrentSceneInfo()
end

function res.PushDialogImmediate(ctrlPath, ...)
    return LoadPrefabDialog(res.LoadType.Push, ctrlPath, nil, ...)
end

function res.PushDialog(ctrlPath, ...)
    local args = {...}
    local argc = select('#', ...)
    clr.coroutine(function()
        unity.waitForEndOfFrame()
        res.PushDialogImmediate(ctrlPath, unpack(args, 1, argc))
    end)
end

-- function res.RemoveCurrentSceneDialogsInfo()
--     if type(res.curSceneInfo) == "table" then
--         res.curSceneInfo.dialogs = nil
--     end
-- end


function res.ChangeGameObjectLayer(dialog, layer)
    UIResManager.ChangeGameObjectLayer(dialog, layer)
end

function res.ShowDialog(content, renderMode, touchClose, withShadow, unblockRaycast, withCtrl, overlaySortingOrder, noNeedSafeArea,
                        shadowType)
    -- local loadingType = cache.getGlobalTempData("LoadingPrefabDialog")
    -- local loadingInfo = { dialog = {} }
    -- if not loadingType then
    --     if unmanagedBlockDialogs[content] then
    --         loadingInfo.block = true
    --     end
    --     for i = #unmanagedDialogs, 1, -1 do
    --         local dialog = unmanagedDialogs[i].dialog
    --         if not dialog or res.IsClrNull(dialog) then
    --             table.remove(unmanagedDialogs, i)
    --         end
    --     end

    --     unmanagedDialogs[#unmanagedDialogs + 1] = loadingInfo
    -- end

    res.SetCurrentEventSystemEnabled(true)
    local dialog, dialogSpt, dummydialog, blockdialog, diagcomp, dummycomp, scd

    scd = res.GetLastSCD(false)
    if renderMode and renderMode ~= "overlay" then
        dialog, dialogSpt = res.Instantiate("Game/UI/Common/Dialog/CameraDialog.prefab")
        diagcomp = res.GetLuaScript(dialog)
        diagcomp.withCtrl = withCtrl
        -- cache.setGlobalTempData(true, "isDummyDialog")
        -- dummydialog = res.Instantiate("Game/UI/Common/Dialog/OverlayDialog.prefab")
        -- cache.removeGlobalTempData("isDummyDialog")
        -- local dummycanvas = dummydialog:GetComponent(Canvas)
        -- res.GetLuaScript(dummycanvas):setShadow(withShadow)
        -- diagcomp = res.GetLuaScript(dialog)
        -- dummycomp = res.GetLuaScript(dummydialog)
        
        if withShadow then
            diagcomp:setShadow(true, shadowType)
            if res.NeedDialogCameraBlur() then
                res.SetMainCameraBlur(true, shadowType)
            else
                res.GetLuaScript(dummycanvas):enableShadow()
                diagcomp:enableShadow()
            end
        else
            diagcomp:setShadow(false, shadowType)
        end
        if noNeedSafeArea then
            diagcomp:SetSafeAreaEnabled(false)
        end
    else
        if overlaySortingOrder then
            cache.setGlobalTempData(overlaySortingOrder, "overlaySortingOrder")
        end
        dialog, dialogSpt = res.Instantiate("Game/UI/Common/Dialog/OverlayDialog.prefab")
        cache.removeGlobalTempData("overlaySortingOrder")
        diagcomp = res.GetLuaScript(dialog)
        diagcomp.withCtrl = withCtrl
        if withShadow then
            diagcomp:setShadow(true, shadowType)
            res.SetMainCameraBlur(true, shadowType)
        else
            diagcomp:setShadow(false, shadowType)
        end
    end

    local objcontent
    if type(content) == "string" then
        objcontent = res.Instantiate(content)
        objcontent.transform:SetParent(dummydialog and dummydialog.transform or dialog and dialog.transform, false)
        if objcontent then
            diagcomp.content = objcontent
            local compcontent = res.GetLuaScript(objcontent)
            if compcontent then
                diagcomp.contentcomp = compcontent
                compcontent.dialog = diagcomp
                compcontent.closeDialog = diagcomp.closeDialog
            end
        end
    end

    if dialog then
        clr.coroutine(function()
            coroutine.yield(UnityEngine.WaitForEndOfFrame())
            if dialog ~= nil and not res.IsClrNull(dialog) then
                if objcontent then
                    objcontent.transform:SetParent(dialogSpt.safeArea or dialog.transform, false)
                    res.AdjustDialogCamera(scd, dialog, withShadow)
                end
            else
                Object.Destroy(objcontent)
            end
            -- Object.Destroy(dummydialog)
        end)
    end

    if touchClose then
        -- 点击之后关闭当前dialog
        if type(diagcomp.contentcomp.Close) == "function" then
            diagcomp:regOnButtonClick(function ()
                diagcomp.contentcomp:Close()
            end)
        else
            diagcomp:regOnButtonClick(diagcomp.closeDialog)
        end
    end
    if unblockRaycast then
        diagcomp.___ex.canvasGroup.blocksRaycasts = false
    end

    -- if not loadingType then
    --     for i = #unmanagedDialogs, 1, -1 do
    --         local dialog = unmanagedDialogs[i].dialog
    --         if not dialog or res.IsClrNull(dialog) then
    --             table.remove(unmanagedDialogs, i)
    --         end
    --     end

    --     loadingInfo.dialog = diagcomp
    -- end

    return dialog, diagcomp
end

local UILayers = 5
function res.ChangeCameraDialogToUI(dialog)
    res.SetUICamera(dialog, UIResManager.FindUICamera())
    res.ChangeGameObjectLayer(dialog, UILayers)
end

local DialogLayers = 23
function res.ChangeCameraDialogToDialog(dialog)
    res.SetUICamera(dialog, res.GetDialogCamera())
    res.ChangeGameObjectLayer(dialog, DialogLayers)
end

function res.ChangeCameraDialogToDefault(dialog)
    -- 把不显示的dialog放到HideLayer层
    res.ChangeGameObjectLayer(dialog, 17)
end

function res.AdjustDialogCamera(scd, dialog, withShadow)
    res.ChangeCameraDialogToDialog(dialog)
    if withShadow then
        if scd and not res.IsClrNull(scd) then
            res.ChangeCameraDialogToDefault(scd)
        end
    end
end

-- 获取最上层的带有shadow的camera dialog及其上面的所有不带shadow的camera dialog，
-- 并且不带shadow的camera dialog是按照order从小到大排好序的
-- withoutCurrent代表是否获取当前最顶层CameraDialog，true：获取第二层；false：获取第一层
-- 这个方法应该只在顶层是带有shadow的camera dialog是调用才有意义
function res.GetLastSCD(withoutCurrent)
    local canvases = clr.table(Object.FindObjectsOfType(Canvas))
    local cameraCanvas = {}
    local layer, parent
    for i, v in ipairs(canvases) do
        if v ~= nil and (not res.IsClrNull(v.gameObject)) then
            parent = v.transform.parent
            if (parent == nil or res.IsClrNull(parent)) and v.renderMode == RenderMode.ScreenSpaceCamera and v.sortingLayerName == "Dialog" then
                table.insert(cameraCanvas, v)
            end
        end
    end

    table.sort(cameraCanvas, function(a, b) return a.sortingOrder > b.sortingOrder end)
    local scd, nextScdShadow = nil, false
    local startIndex = withoutCurrent and 2 or 1

    for i = startIndex, #cameraCanvas do
        local v = cameraCanvas[i]
        local script = res.GetLuaScript(v)
        if script.withShadow then
            if scd == nil then
                scd = v.gameObject
            end
            local state = cameraCanvas[i + 1] or cameraCanvas[i - 1]
            if state then
                script = res.GetLuaScript(state)
                nextScdShadow = script.withShadow
            end
        end
    end

    return scd, nextScdShadow
end

function res.ChangeCameraDialogToDialog(dialog)
    res.SetUICamera(dialog, res.GetDialogCamera())
    -- set object to Dialog Layer
    res.ChangeGameObjectLayer(dialog, DialogLayers)
end

-- 是否需要开启弹出窗口背景模糊效果
function res.NeedDialogCameraBlur()
    return true -- device.level ~= "low"
end

-- 设置由MainCamera渲染的UI界面模糊
function res.SetMainCameraBlur(enabled, shadowType)
    local st = shadowType or res.shadowType.Black
    UIResManager.SetCameraBlur(enabled, st.blurRadius, st.iteration, st.rtDownScaling, st.time, st.rate)
end

-- 目前只接受leve = 2的垃圾回收
function res.CollectGarbage(level)
    if level == 2 then
        ResManager.GarbageCollector.StartGarbageCollect(level)
    end
end

-- 只弹出一个栈帧
function res.PopCtrlStack()
    if #res.ctrlStack == 0 then
        return
    end
    table.remove(res.ctrlStack)
end

function res.SetUIAudioListener(viewPath)
    UIResManager.SetUIAudioListener(viewPath)
end

--- 只是把ctrlPath推进场景栈里，并不加载Prefab
function res.ChangeSceneImmediateAndDoNotInstantiatePrefab(ctrlPath, ...)
    SaveCurrentStatusData()
    SaveCurrentSceneInfo()
    res.ClearSceneCache()
    ClearCurrentSceneInfo()

    res.curSceneInfo = {}
    local ctrl = require(ctrlPath)
    res.curSceneInfo.ctrl = ctrl.new()
    res.curSceneInfo.path = ctrlPath
    res.curSceneInfo.args = {...}
    res.curSceneInfo.argc = select("#", ...)
    res.curSceneInfo.blur = false
end

return res