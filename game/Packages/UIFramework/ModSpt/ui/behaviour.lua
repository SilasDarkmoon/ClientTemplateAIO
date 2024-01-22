local behaviour = class()

function behaviour:ctor()
    self.actions = {}
    local list = self.GetActionList()
    for i, v in ipairs(list) do
        self.actions[v] = action.new()
    end
    self:_RegisterEvent()
end

-- function behaviour:OnEnterScene()
--     self.actions = {}
--     local list = self.GetActionList()
--     for i, v in ipairs(list) do
--         self.actions[v] = action.new()
--     end
--     self:_RegisterEvent()
-- end

function behaviour:onDestroy()
    if self.unityEventListCache then
        for i, unityEvent in ipairs(self.unityEventListCache) do
            unityEvent:RemoveAllListeners()
        end
        self.unityEventListCache = nil
    end

    if self.actions then
        for i,v in ipairs(self.actions) do
            v:RemoveAllHandler()
        end
        self.actions = nil
    end

    if self.___ex then
        for k,v in pairs(self.___ex) do
            self[k] = nil
        end
    end

    self:_UnregisterEvent()
    self.___ex = nil
end

-- function behaviour:OnExitScene()
--     if res.IsClrNull(self) then return end
--     if self.actions then
--         for i,v in ipairs(self.actions) do
--             v:RemoveAllHandler()
--         end
--         self.actions = nil
--     end
--     self:_UnregisterEvent()
-- end

function behaviour:RegHandler(actionName, func)
    self.actions[actionName]:AddHandler(func)
end

function behaviour:Trigger(actionName, ...)
    self.actions[actionName]:Trigger(...)
end

-- 用于UnityEvent类型对象（例如Button组件的onClick, Slider组件的onValueChanged）增加监听函数
-- 这个方法缓存了UnityEvent对象，是为了便于在destroy之后能够调用各个UnityEvent对象的RemoveAllListeners函数，使之能够正确的被GC处理
-- 注意继承behavior的类如果有实现onDestroy则需要调用父类的onDestroy
function behaviour:AddListener(unityEvent, func, sound)
    unityEvent:AddListener(function(...)
        sound = sound or "Scene/Click.mp3"
        clr.UISoundManager.Play(sound)
        func(...)
    end)

    if not self.unityEventListCache then
        self.unityEventListCache = {}
    end
    table.insert(self.unityEventListCache, unityEvent)
end

function behaviour:AddListenerWithoutSound(unityEvent, func)
    unityEvent:AddListener(function(...)
        func(...)
    end)
    if not self.unityEventListCache then
        self.unityEventListCache = {}
    end
    table.insert(self.unityEventListCache, unityEvent)
end

-- 需要从controller向view注册的action列表
function behaviour:GetActionList()
    return {}
end

function behaviour:GetEventList()
    return {}
end

function behaviour:_RegisterEvent()
    local eventList = self:GetEventList()
    for eventName, func in pairs(eventList) do
        self["_eventFunc_" .. eventName] = function(...)
            if self.gameObject.activeInHierarchy then
                func(...)
            end
        end
        require("EventSystem").AddEvent(eventName, self, self["_eventFunc_" .. eventName])
    end
end

function behaviour:_UnregisterEvent()
    local eventList = self:GetEventList()
    for eventName, func in pairs(eventList) do
        if self["_eventFunc_" .. eventName] then
            require("EventSystem").RemoveEvent(eventName, self, self["_eventFunc_" .. eventName])
            self["_eventFunc_" .. eventName] = nil
        end
    end
end

behaviour.coroutine = clr.bcoroutine
behaviour.async = unity.basync

-- function behaviour.getmt(base)
--     return function(table, key)
--         local cs = rawget(table, '___cs')
--         if cs then
--             local v = cs[key]
--             if v then
--                 if type(v) == 'function' then
--                     return function(obj, ...)
--                         return v(cs, ...)
--                     end
--                 end
--                 return v
--             end
--         end
--         return base[key]
--     end
-- end

-- *** valid when view call behave's DealVersionVisibleList ***
-- *** valid when view call behave's DealVersionVisibleList ***
-- *** valid when view call behave's DealVersionVisibleList ***
--version visible List Format
-- function behaviour:GetVersionVisibleList()
--    return {
--           ["cn"]=
--          {
--               GameObject1 = true,--(show)
--                GameObject2= false,--(hide)
--          },
--         ["jp"]=
--         {
--             GameObject1 = true,--(show)
--             GameObject3 = false,--(hide)
--         }
--     }
-- end

function behaviour:GetVersionVisibleList()
    return {}
end

function behaviour:DealVersionVisibleList()
    -- local platform = luaevt.trig("Platform_Branch")
    -- if platform == nil or platform == "" then
    --     platform = "cn"
    -- end
   
    -- local list = self:GetVersionVisibleList()[platform]
    -- if list then
    --     if type(list) == "table" then
    --         for obj, isShow in pairs(list) do
    --             if obj and not clr.isnull(obj) then
    --                 obj:SetActive(isShow)
    --             end
    --         end
    --     end
    -- end
end

-- *** valid when view call behave's DealVersionSpriteList ***
-- *** valid when view call behave's DealVersionSpriteList ***
-- *** valid when view call behave's DealVersionSpriteList ***
--version loadSprite List Format
-- function behaviour:GetVersionVisibleList()
--    return {
--           ["cn"]=
--          {
--               GameObject1 = "Assets/ModRes/Game/UI/Common/Images/whatever.png",
--               GameObject2= "Assets/ModRes/Game/UI/Common/Images/whatever.png",
--          },
--         ["jp"]=
--         {
--             GameObject1 = "Assets/ModRes/Game/UI/Common/Images/whatever.png",
--             GameObject3 = "Assets/ModRes/Game/UI/Common/Images/whatever.png",
--         }
--     }
-- end

function behaviour:GetVersionLoadSprite()
    return {}
end


function behaviour:DealVersionSpriteList()
    -- local platform = luaevt.trig("Platform_Branch")
    -- if platform == nil or platform == "" then
    --     platform = "cn"
    -- end

    -- local list = self:GetVersionLoadSprite()[platform]
    -- if list then
    --     if type(list) == "table" then
    --         for obj, path in pairs(list) do
    --             if obj and not CS.isnull(obj) then
    --                 obj:SetAtlasSprite(path)
    --             end
    --         end
    --     end
    -- end
end

setmetatable(behaviour, unity.asyncmeta)

_G["behaviour"] = behaviour
return behaviour
