
-- Lua runs on MoonSharp inside PerfectShell.
-- Everything visual comes via UI.*  (UiBridge.cs exposure)
print("PerfectShell Lua bootstrap!")

-- Read panel specs from YAML already injected into the global `Layout`
for _, p in ipairs(Layout.panels) do
  UI:add_panel(
      p.id,
      tonumber(p.x),
      tonumber(p.y),
      tonumber(p.width),
      tonumber(p.height))
end

-- Populate panels ----------------------------------------------------------------

UI:add_text("weather", "Eskişehir", "title")
UI:add_text("weather", "24°C – Partly sunny", "body")

UI:add_text("stocks", "Watchlist", "title")
UI:add_text("stocks", "MSFT 241.22 ▲0.46", "caption")
UI:add_text("stocks", "TSLA 180.19 ▼1.63", "caption")

UI:add_text("todo", "My Day", "title")
UI:add_text("todo", "• Send invites for review", "body")
UI:add_text("todo", "• Buy groceries", "body")

UI:add_text("calendar", "12 November", "title")
UI:add_text("calendar", "14:00  Lunch – Selim", "body")
UI:add_text("calendar", "15:00  Team Presentation", "body")

-- Simple animation ‑‑ fade in whole UI
for _,p in ipairs(Layout["panels"]) do
  UI:animate_opacity(p["id"], 1.0, 600)
end
