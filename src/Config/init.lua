-- ─────────────────────────────────────────────────────────────────────────
--  init.lua  ·  PerfectShell bootstrap
--  Defines onInitialize() which loads the active theme.
-- ─────────────────────────────────────────────────────────────────────────


-- DSL helpers
require("ui_dsl")
require("icon")

-- minimal fall‑backs (only if theme hasn't set them later)
UI.set_ui_default("GridBaseUnit",    10)
UI.set_ui_default("RadiusDefault",   24)
UI.set_ui_default("SurfaceARGB",     0xB8FFFFFF)
UI.set_ui_default("PrimaryARGB",     0xFF3490F7)
UI.set_ui_default("DefaultFont",     "Segoe UI Variable Display")
UI.set_ui_default("BlurLow",         25)
UI.set_ui_default("BlurHigh",        45)


-- callback fired from MainWindow after UiBridge is fully attached
function onInitialize()
  Wallpaper("Config/walls/win7_blur.svg")
  ---:blur(UI.get_ui_default("BlurLow"))
  :build()
  

  Panel("taskbar")
  :horizontal()
  :background(0x55000000)
  :radius(RAD)
  :size(UI.get_screen().w, 48)
  :position(0, UI.get_screen().h)
  :shadow(true)
  :build()

  
  Panel("sidebar")
  :vertical()
  :radius(RAD)
  :size(350, UI.get_screen().h)
  :position(0, 0)
  :zorder(-1)
  :shadow(true)

  :build()

  -- swap file below to change theme
  --dofile("Config/themes/win7_2025.lua")
end
