local LuaHubC = clr.LuaLib.LuaHub.LuaHubC
local SupportedVer = 7
if LuaHubC.LuaPrecompileEnabled and LuaHubC.LIB_VER >= SupportedVer then
    require("clrstruct.Mathf")
    require("clrstruct.Vector3")
    require("clrstruct.YieldInstructions")
end