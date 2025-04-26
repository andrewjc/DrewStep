-- usage:
--- local Icon = require("icon")
--- Icon("explorer",  4*G),
local M = {}
function Icon(name, px)
  local path = "Config/icons/" .. name .. ".xaml"
  return Vector(path, px, px)
end
return M