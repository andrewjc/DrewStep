---------------------------------------------------------------------------
--  Windows 7 · 2025 Edition
---------------------------------------------------------------------------

--------------------------------------------------------------------
-- token overrides
--------------------------------------------------------------------
UI.set_ui_default("GridBaseUnit",    10)
UI.set_ui_default("RadiusDefault",   24)
UI.set_ui_default("SurfaceARGB",     0xB8FFFFFF)
UI.set_ui_default("PrimaryARGB",     0xFF3490F7)
UI.set_ui_default("DefaultFont",     "Segoe UI Variable Display")
UI.set_ui_default("BlurLow",         25)
UI.set_ui_default("BlurHigh",        45)

local G   = UI.get_ui_default("GridBaseUnit")
local RAD = UI.get_ui_default("RadiusDefault")

--------------------------------------------------------------------
-- wallpaper
--------------------------------------------------------------------
Wallpaper("Config/walls/win7_blur.svg")
  :blur(UI.get_ui_default("BlurLow"))
  :build()

--------------------------------------------------------------------
-- taskbar (bottom)
--------------------------------------------------------------------
Panel("taskbar")
  :horizontal()
  :background(0x55000000)
  :radius(RAD)
  :size(UI.get_screen().w, 6*G)
  :position(0, UI.get_screen().h - 7*G)
  :shadow(true)
  :contents(list.of(
      -- Start button
      Panel("startBtn")
        :horizontal()
        :size(6*G, 6*G)
        :background(UI.get_ui_default("PrimaryARGB").Number)
        :radius(RAD)
        :contents(list.of(
            Vector("Config/icons/windows_logo.xaml",4*G,4*G)
        )),
      -- Quick‑launch icons
      Panel("ql")
        :horizontal()
        :size(50*G, 6*G)
        :background(0x22FFFFFF)
        :contents(list.of(
            Vector("Config/icons/edge.xaml",4*G,4*G),
            Vector("Config/icons/explorer.xaml",4*G,4*G),
            Vector("Config/icons/settings.xaml",4*G,4*G)
        )),
      -- Clock section
      Panel("clock")
        :horizontal()
        :center("Vertical")
        :size(18*G, 6*G)
        :contents(list.of(
            Text("00:00"):style("title")
        ))
  ))
  :build()

-- update clock
function tickClock() UI.set_text("clock", os.date("%I:%M %p")) end
UI.start_timer(1000,"tickClock")

--------------------------------------------------------------------
-- start menu
--------------------------------------------------------------------
Panel("startMenu")
  :vertical()
  :size(45*G, 60*G)
  :position(2*G, UI.get_screen().h - 60*G - 9*G)
  :background(0xEE10264C)
  :radius(RAD)
  :shadow(true)
  :contents(list.of(
      Panel("allColumn"):vertical():background(0xAA0B1F3A):size(20*G,nil)
        :contents(list.of(Text("All"):style("title") )),
      Panel("pinColumn"):vertical():background(0xAA0B1F3A):size(20*G,nil)
        :contents(list.of(Text("Pinned"):style("title")))
  ))
  :build()
UI:animate_opacity("startMenu",0,0)

UI.register_click("startBtn","toggle_start")
function toggle_start()
  local opa = UI.get_bounds("startMenu").h > 0 and 0 or 1
  UI:animate_opacity("startMenu", opa, 200)
end
