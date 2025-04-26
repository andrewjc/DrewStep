-- ─────────────────────────────────────────────────────────────────────────
--  ui_dsl.lua  ·  Fluent / functional UI builder for PerfectShell
-- ─────────────────────────────────────────────────────────────────────────

-- simple var‑arg helper
local list = {}; function list.of(...) return {...} end; _G.list = list

---------------------------------------------------------------------------
-- panel builder
---------------------------------------------------------------------------
local PanelBuilder = {}
PanelBuilder.__index = PanelBuilder

function PanelBuilder:position(x,y)      self.x,self.y = x,y; return self end
function PanelBuilder:size(w,h)          self.w,self.h = w,h; return self end
function PanelBuilder:horizontal()       self.vert=false; return self end
function PanelBuilder:vertical()         self.vert=true;  return self end
function PanelBuilder:background(argb)   self.bg   = argb; return self end
function PanelBuilder:radius(r)          self.rad  = r;   return self end
function PanelBuilder:shadow(on)         self.doShadow = on~=false; return self end
function PanelBuilder:zorder(zidx)       self.z    = zidx;return self end
function PanelBuilder:padding(px)        self.pad  = px;  return self end
function PanelBuilder:resizable()        self.resize=true;return self end
function PanelBuilder:center(axis)
  local a = (axis or "Both"):lower()
  self.centerH = a=="horizontal" or a=="both"
  self.centerV = a=="vertical"   or a=="both"
  return self
end
function PanelBuilder:contents(children) self.children = children; return self end

-- internal helpers --------------------------------------------------------
local function pBounds(parentId)
  if not parentId then
    local scr = UI:get_screen(); return {x=0,y=0,w=scr.w,h=scr.h}
  end
  local b = UI:get_bounds(parentId)
  return {x=b.x,y=b.y,w=b.w,h=b.h}
end

local function finalizePos(self,parentId)
  local pb = pBounds(parentId)
  if self.centerH and self.w then self.x = pb.x + (pb.w - self.w)/2 end
  if self.centerV and self.h then self.y = pb.y + (pb.h - self.h)/2 end
end

local function common(id,self)
  if self.bg       then UI:set_background   (id,self.bg) end
  if self.rad      then UI:set_corner_radius(id,self.rad) end
  if self.z        then UI:set_z            (id,self.z) end
  if self.doShadow then UI:set_shadow       (id) end
  if self.resize   then UI:enable_resize    (id) end
end

function PanelBuilder:build()
  self:build_in(nil); return self
end

function PanelBuilder:build_in(parentId)
  finalizePos(self,parentId)
  UI:add_panel(self.id,
               self.x or 0, self.y or 0,
               self.w or 100, self.h or 100,
               self.vert~=false)
  common(self.id,self)

  -- recursive build
  if self.children then
    for _,child in ipairs(self.children) do
      child:build_in(self.id)
    end
  end
end

function Panel(id)
  return setmetatable({id=id,vert=true,doShadow=true}, PanelBuilder)
end
_G.Panel = Panel

---------------------------------------------------------------------------
-- text builder
---------------------------------------------------------------------------
local TextBuilder = {}; TextBuilder.__index = TextBuilder
function TextBuilder:style(s) self.style=s; return self end
function TextBuilder:build_in(host) UI:add_text(host, self.txt, self.style or "body") end
function Text(s) return setmetatable({txt=s}, TextBuilder) end
_G.Text = Text

---------------------------------------------------------------------------
-- vector builder
---------------------------------------------------------------------------
local VecBuilder = {}; VecBuilder.__index = VecBuilder
function VecBuilder:build_in(host) UI:add_vector(host, self.path, self.w, self.h) end
function Vector(path,w,h) return setmetatable({path=path,w=w,h=h}, VecBuilder) end
_G.Vector = Vector

---------------------------------------------------------------------------
-- image builder
---------------------------------------------------------------------------
local ImgBuilder = {}; ImgBuilder.__index = ImgBuilder
function ImgBuilder:build_in(host) UI:add_image(host, self.path, self.w, self.h) end
function Image(path,w,h) return setmetatable({path=path,w=w,h=h}, ImgBuilder) end
_G.Image = Image

------------------------------------------------------------------------
--  wallpaper builder
------------------------------------------------------------------------
local WallBuilder = {}; WallBuilder.__index = WallBuilder
function WallBuilder:blur(px)  self._blur = px; return self end
function WallBuilder:tint(arg) self._tint = arg; return self end -- reserved

function WallBuilder:build() UI:apply_wallpaper(self.path, self._blur or 0) end
function Wallpaper(path) return setmetatable({path = path}, WallBuilder) end
_G.Wallpaper = Wallpaper

return {
  Panel     = Panel,
  Text      = Text,
  Vector    = Vector,
  Image     = Image,
  Wallpaper = Wallpaper,
  list      = list
}
