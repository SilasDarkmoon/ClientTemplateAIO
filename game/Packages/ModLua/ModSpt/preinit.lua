if jit then
    jit.off()
    jit.flush()
end

if jit and jit.os == "OSX" and (jit.arch == "arm" or jit.arch == "arm64") then
    jit = nil
end

function saferequire(libname)
    xpcall(function() require(libname) end, function(err) print(err) end)
end

function tryrequire(libname)
    xpcall(function() require(libname) end, function(err) end)
end

local post_init_items = {}
function postinit(libname, order)
    order = order or 0
    post_init_items[#post_init_items + 1] = { libname = libname, order = order }
end

function trig_post_init()
    local revmap = {}
    for i = 1, #post_init_items do
        revmap[post_init_items[i]] = i
    end
    table.sort(post_init_items, function(item1, item2)
        if item1.order ~= item2.order then
            return item1.order < item2.order
        else
            return revmap[item1] < revmap[item2]
        end
    end)
    for i = 1, #post_init_items do
        saferequire(post_init_items[i].libname)
    end
end

luaevt.trig('___EVENT__PRE_CONFIG')