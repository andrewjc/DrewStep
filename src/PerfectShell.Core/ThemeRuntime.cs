using System;
using System.Collections.Concurrent;
using System.Windows;
using System.Windows.Media;
using MoonSharp.Interpreter;

namespace PerfectShell.Core
{
    /// <summary>
    /// Central *mutable* store for UI defaults.  Keys are strings.
    /// Lua (or C#) can overwrite any key at runtime:
    ///      UI.set_ui_default("GridBaseUnit", 8)
    /// </summary>
    public static class ThemeRuntime
    {
        // internal dictionary keeps object values
        private static readonly ConcurrentDictionary<string, object> _kv =
            new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        private static bool _init;

        /// <summary>Must be called once by App before first use.</summary>
        public static void Initialise()
        {
            if (_init) return;
            _init = true;

            // ----------- sensible defaults -------------
            Set("GridBaseUnit", 8.0);
            Set("RadiusDefault", 16.0);
            Set("SurfaceARGB", 0xA0FFFFFFu);
            Set("PrimaryARGB", 0xFF147EFBu);
            Set("DefaultFont", "Segoe UI Variable Text");
            Set("BlurLow", 15.0);
            Set("BlurHigh", 30.0);

            // DPI scale
            var dpi = VisualTreeHelper.GetDpi(new System.Windows.Controls.Canvas());
            Set("DpiScale", dpi.DpiScaleX);
        }

        // ---------------- setter / getter -----------------
        public static void Set(string key, object val) => _kv[key] = val;

        public static double GetDouble(string key) => Convert.ToDouble(_kv[key]);
        public static uint GetColor(string key) => Convert.ToUInt32(_kv[key]);
        public static string GetString(string key) => _kv[key]?.ToString() ?? string.Empty;
        public static object Get(string key) => _kv[key];

        // ---------------- Lua bridge helpers --------------
        public static void ExposeToLua(Script lua)
        {
            // 1. Ensure a global table called "UI" exists
            if (lua.Globals.Get("UI").Type != DataType.Table)
            {
                lua.Globals["UI"] = DynValue.NewTable(lua);
            }

            // 2. Work with the table directly
            Table ui = lua.Globals.Get("UI").Table;

            // 3. Push the delegates as DynValue objects
            ui["set_ui_default"] = DynValue.FromObject(lua,
                (Action<DynValue, DynValue>)((k, v) =>
                {
                    if (k.Type != DataType.String) return;

                    object boxed = v.Type switch
                    {
                        DataType.String => v.String,
                        DataType.Number => v.Number,
                        _ => v.ToObject()
                    };

                    Set(k.String, boxed);
                }));

            ui["get_ui_default"] = DynValue.FromObject(lua,
                (Func<DynValue, DynValue>)(key =>
                {
                    if (key.Type == DataType.String &&
                        _kv.TryGetValue(key.String, out var val))
                    {
                        return DynValue.FromObject(lua, val);
                    }

                    return DynValue.Nil;
                }));
        }

        // ---------------- convenience wrappers ------------
        public static double Grid => GetDouble("GridBaseUnit");
        public static double Radius => GetDouble("RadiusDefault");

        public static Brush SurfaceBrush => new SolidColorBrush(Color.FromArgb(
            (byte)(GetColor("SurfaceARGB") >> 24),
            (byte)(GetColor("SurfaceARGB") >> 16),
            (byte)(GetColor("SurfaceARGB") >> 8),
            (byte)GetColor("SurfaceARGB")));

        public static FontFamily DefaultFont => new FontFamily(GetString("DefaultFont"));
    }
}
