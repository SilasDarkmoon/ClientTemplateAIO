local _Mathf = clr.UnityEngine.Mathf
local Mathf = clr.unextend(_Mathf)

local math = math
local floor = math.floor
local abs = math.abs

rawset(Mathf, "Deg2Rad", math.rad(1))
rawset(Mathf, "Epsilon", 1.4013e-45)
rawset(Mathf, "Infinity", math.huge)
rawset(Mathf, "NegativeInfinity", -math.huge)
rawset(Mathf, "PI", math.pi)
rawset(Mathf, "Rad2Deg", math.deg(1))

rawset(Mathf, "Abs", math.abs)
rawset(Mathf, "Acos", math.acos)
rawset(Mathf, "Asin", math.asin)
rawset(Mathf, "Atan", math.atan)
rawset(Mathf, "Atan2", math.atan2)
rawset(Mathf, "Ceil", math.ceil)
rawset(Mathf, "Cos", math.cos)
rawset(Mathf, "Exp", math.exp)
rawset(Mathf, "Floor", math.floor)
rawset(Mathf, "Log", math.log)
rawset(Mathf, "Log10", math.log10)
rawset(Mathf, "Max", math.max)
rawset(Mathf, "Min", math.min)
rawset(Mathf, "Pow", math.pow)
rawset(Mathf, "Sin", math.sin)
rawset(Mathf, "Sqrt", math.sqrt)
rawset(Mathf, "Tan", math.tan)
rawset(Mathf, "Deg", math.deg)
rawset(Mathf, "Rad", math.rad)
rawset(Mathf, "Random", math.random)

rawset(Mathf, "Approximately", function(a, b)
    return abs(b - a) < math.max(1e-6 * math.max(abs(a), abs(b)), 1.121039e-44)
end)

rawset(Mathf, "Clamp", function(value, min, max)
	if value < min then
		value = min
	elseif value > max then
		value = max    
	end
	
	return value
end)

rawset(Mathf, "Clamp01", function(value)
	if value < 0 then
		return 0
	elseif value > 1 then
		return 1   
	end
	
	return value
end)

rawset(Mathf, "DeltaAngle", function(current, target)    
	local num = Mathf.Repeat(target - current, 360)  --Lua assist checked flag

	if num > 180 then
		num = num - 360
	end

	return num
end)

rawset(Mathf, "Gamma", function(value, absmax, gamma) 
	local flag = false
	
    if value < 0 then    
        flag = true
    end
	
    local num = abs(value)
	
    if num > absmax then    
        return (not flag) and num or -num
    end
	
    local num2 = math.pow(num / absmax, gamma) * absmax
    return (not flag) and num2 or -num2
end)

rawset(Mathf, "InverseLerp", function(from, to, value)
	if from < to then      
		if value < from then 
			return 0
		end

		if value > to then      
			return 1
		end

		value = value - from
		value = value/(to - from)
		return value
	end

	if from <= to then
		return 0
	end

	if value < to then
		return 1
	end

	if value > from then
        return 0
	end

	return 1 - ((value - to) / (from - to))
end)

rawset(Mathf, "Lerp", function(from, to, t)
	return from + (to - from) * Mathf.Clamp01(t)  --Lua assist checked flag
end)

rawset(Mathf, "LerpAngle", function(a, b, t)
	local num = Mathf.Repeat(b - a, 360)  --Lua assist checked flag

	if num > 180 then
		num = num - 360
	end

	return a + num * Mathf.Clamp01(t)  --Lua assist checked flag
end)

rawset(Mathf, "LerpUnclamped", function(a, b, t)
    return a + (b - a) * t;
end)

rawset(Mathf, "MoveTowards", function(current, target, maxDelta)
	if abs(target - current) <= maxDelta then
		return target
	end

	return current + Mathf.Sign(target - current) * maxDelta  --Lua assist checked flag
end)

rawset(Mathf, "MoveTowardsAngle", function(current, target, maxDelta)
	target = current + Mathf.DeltaAngle(current, target)  --Lua assist checked flag
	return Mathf.MoveTowards(current, target, maxDelta)  --Lua assist checked flag
end)

rawset(Mathf, "PingPong", function(t, length)
    t = Mathf.Repeat(t, length * 2)  --Lua assist checked flag
    return length - abs(t - length)
end)

rawset(Mathf, "Repeat", function(t, length)    
	return t - (floor(t / length) * length)
end)

rawset(Mathf, "Round", function(num)
	return floor(num + 0.5)
end)

rawset(Mathf, "Sign", function(num)  
	if num > 0 then
		num = 1
	elseif num < 0 then
		num = -1
	else 
		num = 0
	end

	return num
end)

rawset(Mathf, "SmoothDamp", function(current, target, currentVelocity, smoothTime, maxSpeed, deltaTime)
	maxSpeed = maxSpeed or Mathf.Infinity
	deltaTime = deltaTime or Time.deltaTime
    smoothTime = math.max(0.0001, smoothTime)
    local num = 2 / smoothTime
    local num2 = num * deltaTime
    local num3 = 1 / (1 + num2 + 0.48 * num2 * num2 + 0.235 * num2 * num2 * num2)
    local num4 = current - target
    local num5 = target
    local max = maxSpeed * smoothTime
    num4 = Mathf.Clamp(num4, -max, max)  --Lua assist checked flag
    target = current - num4
    local num7 = (currentVelocity + (num * num4)) * deltaTime
    currentVelocity = (currentVelocity - num * num7) * num3
    local num8 = target + (num4 + num7) * num3
	
    if (num5 > current) == (num8 > num5)  then    
        num8 = num5
        currentVelocity = (num8 - num5) / deltaTime		
    end
	
    return num8,currentVelocity
end)

rawset(Mathf, "SmoothDampAngle", function(current, target, currentVelocity, smoothTime, maxSpeed, deltaTime)
	deltaTime = deltaTime or Time.deltaTime
	maxSpeed = maxSpeed or Mathf.Infinity	
	target = current + Mathf.DeltaAngle(current, target)  --Lua assist checked flag
    return Mathf.SmoothDamp(current, target, currentVelocity, smoothTime, maxSpeed, deltaTime)  --Lua assist checked flag
end)

rawset(Mathf, "SmoothStep", function(from, to, t)
    t = Mathf.Clamp01(t)  --Lua assist checked flag
    t = -2 * t * t * t + 3 * t * t
    return to * t + from * (1 - t)
end)

rawset(Mathf, "HorizontalAngle", function(dir) 
	return math.deg(math.atan2(dir.x, dir.z))
end)

rawset(Mathf, "IsNan", function(number)
	return not (number == number)
end)

return _Mathf
