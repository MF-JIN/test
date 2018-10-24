
local CS = CS
local UnityEngine = CS.UnityEngine
local GameObject = UnityEngine.GameObject
local util = require "lua.utility.xlua.util"

local boot = {
    1, 2, 3, 4,
    s = "sssss",
    {5, 6, 7, 8, 9,},
    num = 9999,
    hahahaah = {
        dd = 999,
        11,22,33,44
    }
}
local self = boot

local yield_return = util.async_to_sync(function (to_yield, callback)
    mono:YieldAndCallback(to_yield, callback)
end)

-- local printbak = _G.print
-- _G.print = function(...)
--     printbak(table.unpack({...}), debug.traceback())
-- end

function boot.coroutine_boot(first, ...)
    -- local args = {...}
    util.coroutine_call(function(...)
        print(debug.traceback("test traceback"))
        -- print(table.unpack({...}), debug.traceback( "coroutine_boot "..tostring({...})  ))
        -- yield_return(CS.UnityEngine.WaitForSeconds(1))
        local obj = nil
        yield_return(CS.AssetSys.Instance:GetAsset("ui/loading/loading.prefab", function(asset)
            obj = asset
        end))
        local loading = CS.UnityEngine.GameObject.Instantiate(obj)

		yield_return(CS.UpdateSys.Instance:Init())
        print("UpdateSys.Init 1")
        
		yield_return(CS.UpdateSys.Instance:CheckUpdate())
        print("UpdateSys.CheckUpdate 1")
        
        obj = nil
        yield_return(CS.AssetSys.Instance:GetAsset("common/manager/manager.prefab", function(asset)
            obj = asset
        end))
        local manager = CS.UnityEngine.GameObject.Instantiate(obj)

		yield_return(CS.NetSys.Instance:Init())
        print("NetSys 1")

	    obj = nil
	    yield_return(CS.AssetSys.Instance:GetAsset("ui/login/login.prefab", function(asset)
	        obj = asset;
	    end))
	    print("lua login 1", obj);
	    local login = CS.UnityEngine.GameObject.Instantiate(obj);

        -- yield_return(CS.UnityEngine.WaitForSeconds(3))

	    loading:SetActive(false)

    end)
end

--AutoGenInit Begin
function boot.AutoGenInit()
end
--AutoGenInit End

function boot.Awake()
	boot.AutoGenInit()
    boot.coroutine_boot(1,2,2,4)
end

-- function boot.OnEnable()
--     print("boot.OnEnable")

-- end

-- function boot.Start()
--     print("boot.Start")


-- end

-- function boot.FixedUpdate()

-- end

-- function boot.Update()

-- end

-- function boot.LateUpdate()

-- end

-- function boot.OnDestroy()
--     print("boot.OnDestroy")

-- end
    
return boot
