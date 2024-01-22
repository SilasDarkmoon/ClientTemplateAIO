-- 异步加载场景需要继承这个View类
local BaseAsyncScene = class(behaviour)

function BaseAsyncScene:ctor()
    BaseAsyncScene.super.ctor(self)
    cache.setGlobalTempData(self, "MainManager")
end

return BaseAsyncScene
