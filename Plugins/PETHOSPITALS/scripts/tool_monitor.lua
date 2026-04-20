-- patch8_verify v10
-- Monitorea campos candidatos en TODOS los animales del corral
-- para identificar cuál campo cambia de 2→3 cuando hay 3 conejos
-- vs 2→1 cuando un animal sale.
-- Candidatos: animal[0x1C4], animal[0x120], animal[0x128], animal[0x1CC]
-- También verifica si hay un índice 0-based único por animal.

local function read32(addr) return memory.readlong(addr) end
local function is_valid_ptr(addr)
    return addr ~= nil and addr ~= 0
       and addr >= 0x02000000 and addr <= 0x02400000
end
local function reg(name) return memory.getregister(name) end

local animals = {}  -- set de animal_ptrs vistos

memory.registerexec(0x02007950, function()
    local ap = reg("r0")
    if is_valid_ptr(ap) then animals[ap] = true end
end)
memory.registerexec(0x0203df2c, function()
    local ap = reg("r1")
    if is_valid_ptr(ap) then animals[ap] = true end
end)

local frames = 0
emu.registerbefore(function()
    frames = frames + 1
    if frames % 300 ~= 0 then return end

    local list = {}
    for ap in pairs(animals) do table.insert(list, ap) end
    if #list == 0 then
        print("(esperando animales...)")
        return
    end

    print(string.format("--- frame %d | %d animales ---", frames, #list))
    print(string.format("  %-12s  %5s  %5s  %5s  %5s  %5s  %5s  %s",
        "animal_ptr", "0x05C", "0x120", "0x128", "0x1C4", "0x1CC", "0x0F8", "0x1B0->0x28"))

    for _, ap in ipairs(list) do
        local scene = read32(ap + 0x1b0)
        local s28   = is_valid_ptr(scene) and read32(scene + 0x28) or -1
        -- ¿s28 es pequeño?
        local s28_str = s28 >= 0 and s28 <= 10 and tostring(s28) or string.format("0x%X", s28)

        print(string.format("  0x%08X  %5d  %5d  %5d  %5d  %5d  %5d  %s",
            ap,
            read32(ap + 0x05c),
            read32(ap + 0x120),
            read32(ap + 0x128),
            read32(ap + 0x1c4),
            read32(ap + 0x1cc),
            read32(ap + 0x0f8),
            s28_str))
    end
    print()
    print("  Instrucciones:")
    print("  1. Anota los valores con 2 conejos")
    print("  2. Adopta el 3er conejo y espera el siguiente dump")
    print("  3. El campo que cambia de N a N+1 en TODOS = conteo del corral")
    print("  4. O el campo unico por animal (0, 1, 2) = indice directo")
    print()
end)

print("v10: monitor de campos candidatos")
print("Dump cada 300 frames. Adopta/da animales y observa qué campo cambia.")