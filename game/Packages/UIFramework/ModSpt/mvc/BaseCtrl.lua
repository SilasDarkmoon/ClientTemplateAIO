local BaseCtrl = class()

BaseCtrl.viewPath = nil

BaseCtrl.dialogStatus = {
    touchClose = true,
    withShadow = true,
    unblockRaycast = false,
    noNeedSafeArea = false,
    shadowType = res.shadowType.Black,
}

BaseCtrl.withoutPop = false        -- true:当这个界面显示时，清除掉之前的ctrl的堆栈
BaseCtrl.needSceneCache = true     -- view显示对象是否需要缓存，且不需要添加到res.ctrlStack堆栈里

function BaseCtrl:GetLoadType()
    return self.__loadType
end

function BaseCtrl:ctor(view, ...)
    self.view = view
    self:Init(...)
end

function BaseCtrl:Init(...)

end

function BaseCtrl:Refresh(...)
    if self.withoutPop == true then
        res.ClearCtrlStack()
    end
    self:OnEnterScene()
end

function BaseCtrl:GetEventList()
    return {}
end

function BaseCtrl:OnEnterScene()
    local eventList = self:GetEventList()
    for eventName, func in pairs(eventList) do
        require("EventSystem").AddEvent(eventName, self, func)
    end
end

function BaseCtrl:OnExitScene()
    local eventList = self:GetEventList()
    for eventName, func in pairs(eventList) do
        require("EventSystem").RemoveEvent(eventName, self, func)
    end
    self:stopAllCoroutines()
end

function BaseCtrl:GetStatusData()

end

function BaseCtrl:checkDeadCoroutine()
    if self.runningcos then
        for coinfo, v in pairs(self.runningcos) do
            if not clr.getucoroutine(coinfo) then
                self.runningcos[coinfo] = nil
            end
        end
    end
end

function BaseCtrl:coroutine(func)
    self:checkDeadCoroutine()
    local coinfo
    local co = clr.coroutine(function()
        func()
        if self.runningcos and coinfo then
            self.runningcos[coinfo] = nil
        end
        self:checkDeadCoroutine()
    end)
    if co then
        coinfo = clr.getucoroutine(co)
        if not self.runningcos then
            self.runningcos = {}
        end
        self.runningcos[coinfo] = true
    end
    return co
end

function BaseCtrl:async(func, ...)
    self:checkDeadCoroutine()
    local coinfo
    coinfo = unity.async(function(...)
        func(...)
        if self.runningcos and coinfo then
            self.runningcos[coinfo] = nil
        end
        self:checkDeadCoroutine()
    end, ...)
    if coinfo then
        if not self.runningcos then
            self.runningcos = {}
        end
        self.runningcos[coinfo] = true
    end
    return coinfo
end

function BaseCtrl:abortAllCoroutines()
    self:checkDeadCoroutine()
    local curcoinfo = clr.getucoroutine()
    local thisco = nil
    if self.runningcos then
        for coinfo, v in pairs(self.runningcos) do
            if coinfo == curcoinfo then
                thisco = coinfo
            else
                unity.abort(coinfo)
            end
            self.runningcos[coinfo] = nil
        end
    end
    if thisco then
        unity.abort()
    end
end

function BaseCtrl:stopAllCoroutines()
    self:checkDeadCoroutine()
    local curcoinfo = clr.getucoroutine()
    local thisco = nil
    if self.runningcos then
        for coinfo, v in pairs(self.runningcos) do
            if coinfo == curcoinfo then
                thisco = coinfo
            else
                unity.abort(coinfo)
            end
            self.runningcos[coinfo] = nil
        end
    end
    if thisco then
        clr.UnityEngineEx.CoroutineRunner.StopCoroutine(thisco)
    end
end

setmetatable(BaseCtrl, unity.asyncmeta)

_G["BaseCtrl"] = BaseCtrl
-- return BaseCtrl
