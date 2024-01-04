local action = class()

function action:ctor()
    self.handlerList = {}
end

function action:AddHandler(func)
    table.insert(self.handlerList, func)
end

function action:RemoveAllHandler()
    self.handlerList = {}
end

function action:Trigger(...)
    for i, func in ipairs(self.handlerList) do
        func(...)
    end
end

_G["action"] = action
