
local Model = class(nil, "Model")

-- Model.__index = function(t, key)
--     if t.class[key] then 
--         return t.class[key]
--     else
--         assert(type(key) == "string" and t.data ~= nil)
--         return t.data[t.ModelProtoMap[key] and tostring(t.ModelProtoMap[key]) or key]
--     end
-- end

function Model:ctor()
    self:Init()
end

-- virtual method
function Model:Init()
end

_G["Model"] = Model
-- return Model
