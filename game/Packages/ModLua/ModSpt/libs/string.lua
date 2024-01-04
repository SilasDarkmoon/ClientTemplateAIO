
function string.utf8char(dig)
    if dig < 128 then
        return string.char(dig)
    elseif dig < 2048 then
        return string.char(math.floor(dig / 64) + 192, dig % 64 + 128)
    elseif dig < 65536 then
        return string.char(math.floor(dig / 4096) + 224, math.floor((dig % 4096) / 64) + 128, dig % 64 + 128)
    else
        -- TODO: this is out of ucs2 range
        return ""
    end
end

--- 将字符串拆成单个字符，存在一个table中
function string.utf8tochars(input)
    local isOk, list = pcall(function ()
        local list = {}
        local len  = string.len(input)
        local index = 1
        local arr  = {0xc0, 0xe0, 0xf0, 0xf8, 0xfc}

        while index <= len do
            local c = string.byte(input, index)
            local offset = 1

            for i, v in ipairs(arr) do
                if c < v then
                    offset = i
                    break
                end
            end

            local str = string.sub(input, index, index + offset - 1)
            index = index + offset
            table.insert(list, str)
        end

        return list
    end, input)

    if not isOk then
        return {input}
    else
        return list
    end
end

--- 将秒数保留至小数点后两位并转换为xx'yy''格式
-- @param second 秒数，类型number/string (10.22)
-- @return string (10'22'')
function string.convertSecondToTimeString(second)
    local result = string.format("%.2f", tostring(second))
    result = string.gsub(result, "%.", "\'")
    result = result .. "\'\'"
    return result
end

--[[--

Convert special characters to HTML entities.

The translations performed are:

-   '&' (ampersand) becomes '&amp;'
-   '"' (double quote) becomes '&quot;'
-   "'" (single quote) becomes '&#039;'
-   '<' (less than) becomes '&lt;'
-   '>' (greater than) becomes '&gt;'

@param string input
@return string

]]
function string.htmlspecialchars(input)
    for k, v in pairs(string._htmlspecialchars_set) do
        input = string.gsub(input, k, v)
    end
    return input
end
string._htmlspecialchars_set = {}
string._htmlspecialchars_set["&"] = "&amp;"
string._htmlspecialchars_set["\""] = "&quot;"
string._htmlspecialchars_set["'"] = "&#039;"
string._htmlspecialchars_set["<"] = "&lt;"
string._htmlspecialchars_set[">"] = "&gt;"

--[[--

Inserts HTML line breaks before all newlines in a string.

Returns string with '<br />' inserted before all newlines (\n).

@param string input
@return string

]]
function string.nl2br(input)
    return string.gsub(input, "\n", "<br />")
end

--[[--

Returns a HTML entities formatted version of string.

@param string input
@return string

]]
function string.text2html(input)
    input = string.gsub(input, "\t", "    ")
    input = string.htmlspecialchars(input)
    input = string.gsub(input, " ", "&nbsp;")
    input = string.nl2br(input)
    return input
end

--[[--

Split a string by string.

@param string str
@param string delimiter
@return table

]]
function string.split(str, delimiter)
    if (delimiter=='') then return false end
    local pos,arr = 0, {}
    -- for each divider found
    for st,sp in function() return string.find(str, delimiter, pos, true) end do
        table.insert(arr, string.sub(str, pos, st - 1))
        pos = sp + 1
    end
    table.insert(arr, string.sub(str, pos))
    return arr
end

--[[--

Strip whitespace (or other characters) from the beginning of a string.

@param string str
@return string

]]
function string.ltrim(str)
    return string.gsub(str, "^[ \t\n\r]+", "")
end

--[[--

Strip whitespace (or other characters) from the end of a string.

@param string str
@return string

]]
function string.rtrim(str)
    return string.gsub(str, "[ \t\n\r]+$", "")
end

--[[--

Strip whitespace (or other characters) from the beginning and end of a string.

@param string str
@return string

]]
function string.trim(str)
    str = string.gsub(str, "^[ \t\n\r]+", "")
    return string.gsub(str, "[ \t\n\r]+$", "")
end

--[[--

Make a string's first character uppercase.

@param string str
@return string

]]
function string.ucfirst(str)
    return string.upper(string.sub(str, 1, 1)) .. string.sub(str, 2)
end

--[[--

@param string str
@return string

]]
function string.urlencodeChar(char)
    return "%" .. string.format("%02X", string.byte(char))
end

--[[--

URL-encodes string.

@param string str
@return string

]]
function string.urlencode(str)
    -- convert line endings
    str = string.gsub(tostring(str), "\n", "\r\n")
    -- escape all characters but alphanumeric, '.' and '-'
    str = string.gsub(str, "([^%w%.%- ])", string.urlencodeChar)
    -- convert spaces to "+" symbols
    return string.gsub(str, " ", "+")
end

--[[--

Get UTF8 string length.

@param string str
@return int

]]
function string.utf8len(str)
    local len  = #str
    local left = len
    local cnt  = 0
    local arr  = {0, 0xc0, 0xe0, 0xf0, 0xf8, 0xfc}
    while left ~= 0 do
        local tmp = string.byte(str, -left)
        local i   = #arr
        while arr[i] do
            if tmp >= arr[i] then
                left = left - i
                break
            end
            i = i - 1
        end
        cnt = cnt + 1
    end
    return cnt
end

function string.capital(s)
    if type(s) == 'string' and string.len(s) > 0 then
        return string.upper(string.sub(s, 1, 1)) .. string.sub(s, 2)
    else
        return s
    end
end

function string.formatIntWithComma(amount)
    if type(amount) ~= 'number' then
        return tostring(amount)
    end
    amount = math.floor(amount)
    local formatted = tostring(amount)
    while true do
        formatted, k = string.gsub(formatted, "^(-?%d+)(%d%d%d)", '%1,%2')
        if k == 0 then
            break
        end
    end
    return formatted
end

--- 换算数字，超过1万数字形式为xx万，超过1亿数字形式为xx亿
-- @param num 数字，类型number
-- @param n 保留小数位数（默认两位）
-- @return 数字转换后的字符串形式，类型string
function string.formatNumWithUnit(num, n)
    num = tonumber(num)
    if not n then
        n = 2
    else
        n = tonumber(n)
    end
    local baseUnit = 10000
    local baseBit = 4
    -- if unity.checkPlatform("ww","en") then-- @des 为了配合全球版显示 ，如果是全球版则要做一下处理
    --     baseUnit = 1000
    --     baseBit = 3
    -- end

    if num < baseUnit then
        return tostring(num)
    elseif num < baseUnit * baseUnit then
        num = math.floor(num / math.pow(10, baseBit - n)) / math.pow(10, n)
        return clr.transstr("logogramMoneyTenThousand", num)
    else
        num = math.floor(num / math.pow(10, 2*baseBit - n)) / math.pow(10, n)
        return clr.transstr("logogramMoneyHundredMillon", num)
    end
end

--[[--

Strip zero or dot from the end of a numerical string.

@param string str
@return string

]]
function string.trimNumber(str)
    if string.find(str, ".") then
        str = string.gsub(str, "[0]+$", "")
        str = string.gsub(str, "[.]+$", "")
    end
    return str
end

function string.formatIntToCap(num)
    local map = {
        [0] = '零',
        [1] = '一',
        [2] = '二',
        [3] = '三',
        [4] = '四',
        [5] = '五',
        [6] = '六',
        [7] = '七',
        [8] = '八',
        [9] = '九',
        [10] = '十',
        [11] = '十一',
        [12] = '十二',
        [13] = '十三',
        [14] = '十四',
        [15] = '十五',
        [16] = '十六',
        [17] = '十七',
        [18] = '十八',
        [19] = '十九',
    }
    if map[num] then
        return map[num]
    end
    if num > 999 and num < 10000 then
        local  str = map[math.floor(num/1000)]..'千'
        local  bai = math.floor((num % 1000) / 100)
        local  shi = math.floor((num % 100) / 10)
        local  ge = math.floor(num % 10)
        if bai ~= 0 then
            str = str..map[bai]..'百'
            if shi ~= 0 then
                str = str..map[shi]..'十'
                if ge~=0 then
                    str = str..map[ge]
                end
                elseif ge~=0 then
                str = str..'零'..map[ge]
            end
            elseif shi ~= 0 then
                str = str..'零'..map[shi]..'十'
                if ge ~= 0 then
                    str = str..map[ge]
                end
            elseif ge~=0 then
                str = str..'零'..map[ge]

        end
        return str
    end
    if num > 9999 and num < 100000000 then
        if math.floor(num%10000) ~= 0 --万后无数
            then
            str = math.formatIntToCap(math.floor(num/10000))..'万'..math.formatIntToCap(math.floor(num%10000))
            if math.floor(num/10000)%10 == 0 and math.floor(num/1000)%10 ~= 0 then
            str = math.formatIntToCap(math.floor(num/10000))..'万'..'零'..math.formatIntToCap(math.floor(num%10000))
            end
            else
            str = math.formatIntToCap(math.floor(num/10000))..'万'
        end
    return str
    end

    if num >= 100 then
        local str = map[math.floor(num / 100)]..'百'
        local dig = math.floor((num % 100) / 10)
        if dig ~= 0 then
            str = str..map[dig]..'十'
        elseif num%100 ~= 0 then
            str = str..'零'
        end
        dig = num % 10
        if dig ~= 0 then
            str = str..map[dig]
        end
        return str
    else
        local str = map[math.floor(num / 10)]..'十'
        local dig = num % 10
        if dig ~= 0 then
            str = str..map[dig]
        end
        return str
    end
end

function string.formatIntWithTenThousands(amount)
    if type(amount) ~= 'number' then
        return tostring(amount)
    end
    local strResult = ''
    if amount >= 100000000 then
        local part = math.floor(amount / 100000000)
        strResult = part..'亿'
        amount = amount % 100000000
        amount = math.floor(amount / 10000)
        if amount > 0 then
            strResult = strResult..amount..'万'
        end
    elseif amount >= 10000 then
        amount = math.floor(amount / 100) / 100
        strResult = amount..'万'
    else
        strResult = tostring(amount)
    end
    return strResult
end

function string.formatNumberByUnit(amount)
    if type(amount) ~= 'number' then
        return tostring(amount)
    end
    local conversionData  = {}
    if amount >= 100000000 then
        local part = math.floor(amount / 100000000)
        conversionData.yi = part
        conversionData.strResult = part..'亿'
        amount = amount % 100000000
        amount = math.floor(amount / 10000)
        if amount > 0 then
            conversionData.wan = amount
            conversionData.strResult = conversionData.strResult..amount..'万'
        end
    elseif amount >= 10000 then
        amount = math.floor(amount / 100) / 100
        conversionData.strResult = amount..'万'
        conversionData.wan = amount
    else
        conversionData.strResult = tostring(amount)
    end
    return conversionData
end


-- 年，月，日
function string.convertSecondToYearAndMonthAndDay(num)
    local date = {}
    local dataNum = os.date("*t", num)
    date.year = dataNum.year
    date.month = dataNum.month
    date.day = dataNum.day
    return date
end

function string.formatIntWithTenThousandsWithoutUnit(amount)
    if type(amount) ~= 'number' then
        return tostring(amount)
    end
    local strResult = ''
    if amount >= 10000 then
        amount = math.floor(amount / 100) / 100
        strResult = amount
    else
        strResult = tostring(amount)
    end
    return strResult
end

--- 格式化时间戳为年月日
function string.formatTimestampBetweenYearAndDay(timestamp)
    local year = os.date("%Y", timestamp)
    local month = os.date("%m", timestamp)
    local day = os.date("%d", timestamp)
    return year .. clr.transstr("year") .. month .. clr.transstr("month") .. day .. clr.transstr("day")
end

--- 格式化时间戳为2018年8月21日 12:00:00
function string.formatFullDateTime(timestamp)
    local year = os.date("%Y", timestamp)
    local month = os.date("%m", timestamp)
    local day = os.date("%d", timestamp)
    local time = os.date("%X", timestamp)

    return clr.trans("full_datetime_format", year, month, day, time)
end

--- 转换时间为秒数
-- @param day 天数，类型number
-- @param hour 小时数，类型number
-- @param minute 分钟数，类型number
-- @param second 秒数，类型number
function string.convertTimeToSecond(day, hour, minute, second)

    local totalSecond = 0

    if day ~= nil then
        day = tonumber(day)
        totalSecond = day * 86400
    end

    if hour ~= nil then
        hour = tonumber(hour)
        totalSecond = totalSecond + hour * 3600
    end

    if minute ~= nil then
        minute = tonumber(minute)
        totalSecond = totalSecond + minute * 60
    end

    if second ~= nil then
        second = tonumber(second)
        totalSecond = totalSecond + second
    end

    return totalSecond
end

--- 转换时间为月，日，时间等
--- 例：3月3日4:00
function string.convertSecondToMonth(num)
    local timeTable = os.date("*t", num)
    local month = timeTable.month .. clr.transstr("month")
    local day = timeTable.day .. clr.transstr("day_1")
    local hour = timeTable.hour
    local minute = timeTable.min
    if minute == 0 then minute = minute .. "0" end
    return month .. day .. hour .. ":" .. minute
end

-- 转换为日期范围
-- 例：4月4日~4月8日
function string.convertSecondToMonthAndDayRange(startTime, endTime)
    local daySecond = 24 * 60 * 60
    local range  = {}
    for time = startTime, endTime, daySecond do
        local timeTable = os.date("*t", time)
        local dayTable = {}
        dayTable.month = timeTable.month
        dayTable.day = timeTable.day
        table.insert(range, dayTable)
    end
    return range
end

function string.convertSecondToMonthAndDay(num)
    local date = {}
    date.month = os.date("*t", num).month
    date.day = os.date("*t", num).day
    return date
end

function string.convertSecondToHourAndMinute(num)
    local date = {}
    local time = os.date("*t", num)
    date.hour = time.hour
    date.minute = time.min
    if string.len(date.minute) < 2 then
        date.minute = "0" .. date.minute
    end
    return date
end
--- 转换秒数为时间字符串
-- @param secondNum 秒数，类型number
-- @return string
function string.convertSecondToTime(secondNum)
    local function formateNum(num)
        if string.len(num) < 2 then
            return '0' .. num
        else
            return tostring(num)
        end
    end

    local timeTable = string.convertSecondToTimeTable(secondNum)
    local timeStr = nil

    if timeTable.day > 0 then
        timeStr = timeTable.day .. clr.transstr('day') .. ' ' .. formateNum(timeTable.hour) .. ':' .. formateNum(timeTable.minute) .. ':' .. formateNum(timeTable.second)
    else

        if timeTable.hour > 0 then
            timeStr = formateNum(timeTable.hour) .. ':' .. formateNum(timeTable.minute) .. ':' .. formateNum(timeTable.second)
        else
            timeStr = formateNum(timeTable.minute) .. ':' .. formateNum(timeTable.second)
        end
    end

    return timeStr
end

function string.convertSecondToTimeTrans(secondNum)
    local timeTable = string.convertSecondToTimeTable(secondNum)
    local trans = ""
    if timeTable.day > 0 then
        trans = trans .. timeTable.day .. clr.transstr("day")
    end
    if timeTable.hour > 0 then
        trans = trans .. timeTable.hour .. clr.transstr("hour")
    end
    if timeTable.minute > 0 then
        trans = trans .. timeTable.minute .. clr.transstr("minute")
    end
    if timeTable.second > 0 then
        trans = trans .. timeTable.second .. clr.transstr("second")
    end

    return trans
end

--- 转换秒数为时间table
-- @param secondNum 秒数，类型number
-- @return table
function string.convertSecondToTimeTable(secondNum)
    secondNum = math.round(secondNum)
    local day = math.floor(secondNum / 86400)
    secondNum = secondNum % 86400
    local hour = math.floor(secondNum / 3600)
    secondNum = secondNum % 3600
    local minute = math.floor(secondNum / 60)
    secondNum = secondNum % 60
    local second = secondNum

    local timeTable = {}
    timeTable.day = day
    timeTable.hour = hour
    timeTable.minute = minute
    timeTable.second = second

    return timeTable
end

function string.formatTimeClock(time, precision, symbol)
    local finalTime = ''
    local _hour = ''
    local _minute = ''
    local _second = ''
    local symbol = symbol or ':'
    local hour = math.floor(time / 3600)
    if precision == 3600 then
        _hour = string.format("%02d", 0) .. symbol
        _minute = string.format("%02d", 0) .. symbol
    elseif precision == 60 then
        _minute = string.format("%02d", 0) .. symbol
    end

    if hour > 0 then
        time = time % 3600
        _hour = string.format("%02d", hour) .. symbol
        _minute = string.format("%02d", 0) .. symbol
    end
    finalTime = _hour

    local minute = math.floor(time / 60)
    if minute > 0 then
        time = time % 60
        _minute = string.format("%02d", minute) .. symbol
    end
    finalTime = finalTime .. _minute

    local second = math.floor(time)
    _second = string.format("%02d", second)
    finalTime = finalTime .. _second
    return finalTime
end

-- 把字符串中汉字和数字分开
function string.splitWordAndNumber(str)
    local i = 1
    while i <= #str do
        local curByte = string.byte(str, i)
        local byteCount = 1;
        if curByte>0 and curByte<=127 then
            byteCount = 1
            break
        elseif curByte>=192 and curByte<223 then
            byteCount = 2
        elseif curByte>=224 and curByte<239 then
            byteCount = 3
        elseif curByte>=240 and curByte<=247 then
            byteCount = 4
        end
        i = i + byteCount
    end
    local word = string.sub(str, 1, i - 1)
    local number = string.sub(str, i)
    return word, number
end

-- 拆分字符串(在unity编辑器上得需要在PlayerSetting 中 定义LUA_USE_UTF8_ON_EDITOR_WIN)
function string.splitCharacter(text, symbol)
    local textTab = {}
    for uchar in string.gfind(text, "[%z\1-\127\194-\244][\128-\191]*") do
        textTab[#textTab+1] = uchar
    end
    text = table.concat(textTab, symbol)
    return text, textTab
end