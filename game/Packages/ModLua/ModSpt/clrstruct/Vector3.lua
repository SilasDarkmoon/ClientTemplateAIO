local _Vector3 = clr.UnityEngine.Vector3
local Vector3 = clr.unextend(_Vector3)
if type(Vector3["@luareset"]) == "function" then
    Vector3["@luareset"]()
end
local omVector3 = Vector3["@objmeta"]
local objmethods = Vector3["@objmethods"]
local objgetter = Vector3["@objgetter"]
local objsetter = Vector3["@objsetter"]

local Mathf = clr.unextend(clr.UnityEngine.Mathf)

local acos	= math.acos
local sqrt 	= math.sqrt
local max 	= math.max
local min 	= math.min
local clamp = Mathf.Clamp
local cos	= math.cos
local sin	= math.sin
local abs	= math.abs
local sign	= Mathf.Sign

local rad2Deg = 57.295779513082
local deg2Rad = 0.017453292519943

local function _new(x, y, z)				
	local t = {x = x or 0, y = y or 0, z = z or 0}
	setmetatable(t, omVector3)						
	return t
end
local function _clone(v)
    return setmetatable({x = v.x, y = v.y, z = v.z}, omVector3)
end
local function _set(v, x, y, z)
    v.x = x
    v.y = y
    v.z = z
end
local function _add(v, v2)
    v.x = v.x + v2.x
    v.y = v.y + v2.y
    v.z = v.z + v2.z
    return v
end
local function _sub(v, v2)
    v.x = v.x - v2.x
    v.y = v.y - v2.y
    v.z = v.z - v2.z
    return v
end
local function _mul(v, d)
    v.x = v.x * d
    v.y = v.y * d
    v.z = v.z * d
    return v
end
local function _div(v, d)
    v.x = v.x / d
    v.y = v.y / d
    v.z = v.z / d
    return v
end

getmetatable(Vector3).__call = function(t, x, y, z)
	local t = {x = x or 0, y = y or 0, z = z or 0}
	setmetatable(t, omVector3)						
	return t
end

rawset(Vector3, "Distance", function(va, vb)
    local dx, dy, dz = va.x - vb.x, va.y - vb.y, va.z - vb.z
	return sqrt(dx * dx + dy * dy + dz * dz)
end)

rawset(Vector3, "Dot", function(lhs, rhs)
	return lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z
end)

rawset(Vector3, "Lerp", function(from, to, t)	
	t = clamp(t, 0, 1)
	return _new(from.x + (to.x - from.x) * t, from.y + (to.y - from.y) * t, from.z + (to.z - from.z) * t)
end)

rawset(Vector3, "Magnitude", function(v)
	return sqrt(v.x * v.x + v.y * v.y + v.z * v.z)
end)

rawset(Vector3, "Max", function(lhs, rhs)
	return _new(max(lhs.x, rhs.x), max(lhs.y, rhs.y), max(lhs.z, rhs.z))
end)

rawset(Vector3, "Min", function(lhs, rhs)
	return _new(min(lhs.x, rhs.x), min(lhs.y, rhs.y), min(lhs.z, rhs.z))
end)

rawset(Vector3, "Normalize", function(v)
	local x,y,z = v.x, v.y, v.z		
	local num = sqrt(x * x + y * y + z * z)	
	
	if num > 1e-5 then		
        return _new(x / num, y / num, z / num)
    else
        return _new(0, 0, 0)
    end
end)

rawset(Vector3, "SqrMagnitude", function(v)
	return v.x * v.x + v.y * v.y + v.z * v.z
end)

local _dot = Vector3.Dot
local _norm = Vector3.Normalize
local _sqrlen = Vector3.SqrMagnitude

rawset(Vector3, "Angle", function(from, to)
	return acos(clamp(_dot(_norm(from), _norm(to)), -1, 1)) * rad2Deg
end)

rawset(Vector3, "ClampMagnitude", function(v, maxLength)	
    if _sqrlen(v) > (maxLength * maxLength) then
        local rv = _norm(v)
		rv.x = rv.x * maxLength
		rv.y = rv.y * maxLength
        rv.z = rv.z * maxLength
        return rv
    else
        return _clone(v) 
    end
end)

rawset(Vector3, "Project", function(vector, onNormal)
	local num = _sqrlen(onNormal)
	
	if num < 1.175494e-38 then	
		return _new(0,0,0)
	end
	
    local num2 = _dot(vector, onNormal) / num
    return _new(onNormal.x * num2, onNormal.y * num2, onNormal.z * num2)
end)

local _proj = Vector3.Project

rawset(Vector3, "ProjectOnPlane", function(vector, planeNormal)
    local v3 = _proj(vector, planeNormal)
    return _sub(_clone(vector), v3)
end)

local function Normalize(v)
	local x,y,z = v.x, v.y, v.z		
	local num = sqrt(x * x + y * y + z * z)	
	
	if num > 1e-5 then		
        v.x = x / num
        v.y = y / num
        v.z = z / num
    else
        v.x = 0
        v.y = 0
        v.z = 0
    end
end

rawset(Vector3, "OrthoNormalize", function(va, vb, vc)
    Normalize(va)
    Normalize(_sub(vb, _proj(vb, va)))
	
	if vc == nil then
		return nil, va, vb
	end
    
    vc = _sub(vc, _proj(vc, va))
    vc = _sub(vc, _proj(vc, vb))
	Normalize(vc)		
	return nil, va, vb, vc
end)

rawset(Vector3, "MoveTowards", function(current, target, maxDistanceDelta)	
	local delta = _sub(_clone(target), current)	
    local sqrDelta = _sqrlen(delta)
	local sqrDistance = maxDistanceDelta * maxDistanceDelta
	
    if sqrDelta > sqrDistance then    
		local magnitude = sqrt(sqrDelta)
		
        if magnitude > 1e-6 then
            return _add(_mul(delta, maxDistanceDelta / magnitude), current)
		else
			return _clone(current)
		end
    end
	
    return _clone(target)
end)

local function ClampedMove(lhs, rhs, clampedDelta)
	local delta = rhs - lhs
	
	if delta > 0 then
		return lhs + min(delta, clampedDelta)
	else
		return lhs - min(-delta, clampedDelta)
	end
end
local overSqrt2 = 0.7071067811865475244008443621048490
local function OrthoNormalVector(vec)
	local res = _new()
	
	if abs(vec.z) > overSqrt2 then			
		local a = vec.y * vec.y + vec.z * vec.z
		local k = 1 / sqrt (a)
		res.x = 0
		res.y = -vec.z * k
		res.z = vec.y * k
	else			
		local a = vec.x * vec.x + vec.y * vec.y
		local k = 1 / sqrt (a)
		res.x = -vec.y * k
		res.y = vec.x * k
		res.z = 0
	end
	
	return res
end
local Quaternion = clr.unextend(clr.UnityEngine.Quaternion)

rawset(Vector3, "RotateTowards", function(current, target, maxRadiansDelta, maxMagnitudeDelta)
	local len1 = sqrt(_sqrlen(current))
	local len2 = sqrt(_sqrlen(target))
	
	if len1 > 1e-6 and len2 > 1e-6 then	
		local from = current / len1
		local to = target / len2		
		local cosom = _dot(from, to)
				
		if cosom > 1 - 1e-6 then		
			return Vector3.MoveTowards(current, target, maxMagnitudeDelta)		  --Lua assist checked flag
		elseif cosom < -1 + 1e-6 then		
			local axis = OrthoNormalVector(from)						
			local q = Quaternion.AngleAxis(maxRadiansDelta * rad2Deg, axis)	  --Lua assist checked flag
			local rotated = q * from
			local delta = ClampedMove(len1, len2, maxMagnitudeDelta)
			_mul(rotated, delta)
			return rotated
		else		
			local angle = acos(cosom)
			local axis = Vector3.Cross(from, to)  --Lua assist checked flag
			Normalize(axis)
			local q = Quaternion.AngleAxis(min(maxRadiansDelta, angle) * rad2Deg, axis)			  --Lua assist checked flag
			local rotated = q * from
			local delta = ClampedMove(len1, len2, maxMagnitudeDelta)
			_mul(rotated, delta)
			return rotated
		end
	end
		
	return Vector3.MoveTowards(current, target, maxMagnitudeDelta)  --Lua assist checked flag
end)

rawset(Vector3, "SmoothDamp", function(current, target, currentVelocity, smoothTime, maxSpeed, deltaTime)
	maxSpeed = maxSpeed or Mathf.Infinity
	deltaTime = deltaTime or clr.UnityEngine.Time.deltaTime
    smoothTime = max(0.0001, smoothTime)
    local num = 2 / smoothTime
    local num2 = num * deltaTime
    local num3 = 1 / (1 + num2 + 0.48 * num2 * num2 + 0.235 * num2 * num2 * num2)    
    local vector2 = _clone(target)
    local maxLength = maxSpeed * smoothTime
	local vector = current - target
    vector = Vector3.ClampMagnitude(vector, maxLength)  --Lua assist checked flag
    target = current - vector
    local vec3 = (currentVelocity + (vector * num)) * deltaTime
    currentVelocity = (currentVelocity - (vec3 * num)) * num3
    local vector4 = target + (vector + vec3) * num3	
	
    if _dot(vector2 - current, vector4 - vector2) > 0 then    
        vector4 = vector2
        _set(currentVelocity,0,0,0)
    end
	
    return vector4, currentVelocity
end)

rawset(Vector3, "Scale", function(a, b)
	local x = a.x * b.x
	local y = a.y * b.y
	local z = a.z * b.z	
	return _new(x, y, z)
end)

rawset(Vector3, "Cross", function(lhs, rhs)
	local x = lhs.y * rhs.z - lhs.z * rhs.y
	local y = lhs.z * rhs.x - lhs.x * rhs.z
	local z = lhs.x * rhs.y - lhs.y * rhs.x
	return _new(x,y,z)	
end)

rawset(Vector3, "Reflect", function(inDirection, inNormal)
    local num = -2 * _dot(inNormal, inDirection)
    return _add(_mul(_clone(inNormal), num), inDirection)
end)

rawset(Vector3, "Slerp", function(from, to, t)
	local omega, sinom, scale0, scale1

	if t <= 0 then		
		return _clone(from)
	elseif t >= 1 then		
		return _clone(to)
	end
	
	local v1 	= _clone(from)
	local v2 	= _clone(to)
	local len1 	= sqrt(_sqrlen(from))
    local len2 	= sqrt(_sqrlen(to))
    _div(v1, len1)
    _div(v2, len2)

	local len 	= (len2 - len1) * t + len1
	local cosom = v1.x * v2.x + v1.y * v2.y + v1.z * v2.z
	
	if cosom > 1 - 1e-6 then
		scale0 = 1 - t
		scale1 = t
	elseif cosom < -1 + 1e-6 then		
		local axis = OrthoNormalVector(from)		
		local q = Quaternion.AngleAxis(180.0 * t, axis)		  --Lua assist checked flag
		local v = q * from
		_mul(v, len)				
		return v
	else
		omega 	= acos(cosom)
		sinom 	= sin(omega)
		scale0 	= sin((1 - t) * omega) / sinom
		scale1 	= sin(t * omega) / sinom	
	end

	_mul(v1, scale0)
	_mul(v2, scale1)
	_add(v2, v1)
	_mul(v2, len)
	return v2
end)

local function _eq(a, b)
    return a.x == b.x and a.y == b.y and a.z == b.z
end
local function _tostring(v)
    return "("..v.x..", "..v.y..", "..v.z..")"
end

objmethods["Normalize"] = Normalize
objmethods[Normalize] = true

objmethods["Set"] = _set
objmethods[_set] = true

local function Scale(a, b)
	a.x = a.x * b.x
	a.y = a.y * b.y
	a.z = a.z * b.z	
end
objmethods["Scale"] = Scale
objmethods[Scale] = true

local function Equals(a, b)
    if clr.is(b, Vector3) then
        return _eq(a, b)
    end
    return false
end
objmethods["Equals"] = Equals
objmethods[Equals] = true

local ToStringOld = objmethods["ToString"]
local function ToString(v, format)
    if not format then
        return _tostring(v)
    elseif type(format) == "string" or clr.is(format, clr.System.String) then
        return ToStringOld(v, format)
    else
        return _tostring(v)
    end
end
objmethods["ToString"] = ToString
objmethods[ToString] = true

objmethods["@+"] = function(lhs, rhs)
    if clr.is(lhs, Vector3) and clr.is(rhs, Vector3) then
        return _add(_clone(lhs), rhs)
    end
    return nil, true
end

objmethods["@-"] = function(lhs, rhs)
    if clr.is(lhs, Vector3) and clr.is(rhs, Vector3) then
        return _sub(_clone(lhs), rhs)
    end
    return nil, true
end

objmethods["@*"] = function(lhs, rhs)
    if clr.is(lhs, Vector3) and type(rhs) == "number" then
        return _mul(_clone(lhs), rhs)
    elseif clr.is(rhs, Vector3) and type(lhs) == "number" then
        return _mul(_clone(rhs), lhs)
    end
    return nil, true
end

objmethods["@/"] = function(lhs, rhs)
    if clr.is(lhs, Vector3) and type(rhs) == "number" then
        return _div(_clone(lhs), rhs)
    end
    return nil, true
end

objmethods["@=="] = function(lhs, rhs)
    if clr.is(lhs, Vector3) and clr.is(rhs, Vector3) then
        return _eq(lhs, rhs)
    end
    return nil, true
end

omVector3.__unm = function(v)
    return _new(-v.x, -v.y, -v.z)
end

omVector3.__tostring = function(v)
    return _tostring(v)
end

rawset(Vector3, "left", Vector3.left)
rawset(Vector3, "down", Vector3.down)
rawset(Vector3, "up", Vector3.up)
rawset(Vector3, "back", Vector3.back)
rawset(Vector3, "forward", Vector3.forward)
rawset(Vector3, "one", Vector3.one)
rawset(Vector3, "zero", Vector3.zero)
rawset(Vector3, "right", Vector3.right)
rawset(Vector3, "fwd", Vector3.fwd)

objgetter.normalized = _norm
objsetter.normalized = function() error("Can NOT set normalized on Vector3.") end
objgetter.magnitude = Vector3.Magnitude
objsetter.magnitude = function() error("Can NOT set magnitude on Vector3.") end
objgetter.sqrMagnitude = _sqrlen
objsetter.sqrMagnitude = function() error("Can NOT set sqrMagnitude on Vector3.") end

objgetter["@index"] = function(v, index)
    if index == 0 then
        return v.x
    elseif index == 1 then
        return v.y
    elseif index == 2 then
        return v.z
    elseif type(index) == "number" then
        error("index-getter: Invalid Vector3 index!")
    else
        -- nothing happens
    end
end
objsetter["@index"] = function(v, index, val)
    if index == 0 then
        v.x = val
    elseif index == 1 then
        v.y = val
    elseif index == 2 then
        v.z = val
    elseif type(index) == "number" then
        error("index-setter: Invalid Vector3 index!")
    else
        -- this means this indexer give up, and let the __index-func goto rawset process.
        return true
    end
end

rawset(Vector3, "@luareset", function()
    objmethods[Normalize] = nil
    objmethods[_set] = nil
    objmethods[Scale] = nil
    objmethods[Equals] = nil
    objmethods["ToString"] = ToStringOld
    objmethods[ToString] = nil
end)

return _Vector3
