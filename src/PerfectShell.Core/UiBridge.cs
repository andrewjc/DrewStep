// ───────────────────────────────────────────────────────────────────────────
//  UiBridge.cs  ·  facade exposed to Lua
//  PerfectShell.Core  (refactored, key‑value ThemeRuntime)
// ───────────────────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Debugging;

namespace PerfectShell.Core
{
    [MoonSharpUserData]
    public sealed class UiBridge
    {
        // ------------------------------------------------------------------
        //  sub‑systems
        // ------------------------------------------------------------------
        private readonly LayoutNodeManager _nodes;
        private readonly StyleManager _style;
        private readonly AnimationManager _anim;
        private readonly InteractionManager _inter;
        private TimerManager _timers = null!;
        private DataProvider _data = null!;
        private readonly WallpaperManager _wall;
        private readonly Canvas _rootCanvas;

        private Script? _lua;

        // ------------------------------------------------------------------
        //  ctor
        // ------------------------------------------------------------------
        public UiBridge(Canvas rootCanvas)
        {
            ThemeRuntime.Initialise();              // ensure DPI, defaults

            _rootCanvas = rootCanvas;
            _nodes = new LayoutNodeManager(rootCanvas);
            _style = new StyleManager(_nodes);
            _anim = new AnimationManager(_nodes);
            _inter = new InteractionManager(_nodes);
            _wall = new WallpaperManager(rootCanvas);
        }

        /// <summary>
        /// Must be called after Script is created so managers can fire into Lua.
        /// Also exposes UI.* methods for theme setters & pub/sub.
        /// </summary>
        public void AttachScript(Script lua)
        {
            _lua = lua;
            _inter.AttachScript(lua);
            _timers = new TimerManager(lua);
            _data = new DataProvider(lua);

            // Table that Lua users will interact with
            var uiTbl = new Table(lua);

            var indexTable = new Table(lua);
            var bridgeInstance = this;

            // --- Manually add ALL public methods intended for Lua to indexTable ---
            // Theme/Defaults
            RegisterFunctions_Settings(lua, indexTable, bridgeInstance);

            // Layout Creation
            RegisterFunctions_NodeTypes(lua, indexTable);

            // Styling
            RegisterFunctions_Style(lua, indexTable);

            // Animation
            RegisterFunctions_Animation(lua, indexTable);

            // Interaction
            indexTable["enable_resize"] = DynValue.FromObject(lua, (Action<string, int?>)((id, grip) => bridgeInstance.enable_resize(id, grip ?? 6))); // Handle optional grip
            indexTable["register_click"] = DynValue.FromObject(lua, (Action<string, string>)bridgeInstance.register_click);
            indexTable["register_size_callback"] = DynValue.FromObject(lua, (Action<string, string>)bridgeInstance.register_size_callback);

            // Timers
            indexTable["start_timer"] = DynValue.FromObject(lua, (Action<int, string>)bridgeInstance.start_timer);
            indexTable["stop_timer"] = DynValue.FromObject(lua, (Action<string>)bridgeInstance.stop_timer);

            // Bounds / Screen / Helpers
            indexTable["get_bounds"] = DynValue.FromObject(lua, (Func<string, DynValue>)bridgeInstance.get_bounds);
            indexTable["get_screen"] = DynValue.FromObject(lua, (Func<DynValue>)bridgeInstance.get_screen);
            indexTable["get_parent"] = DynValue.FromObject(lua, (Func<DynValue>)bridgeInstance.get_parent);
            indexTable["clear_contents"] = DynValue.FromObject(lua, (Action<string>)bridgeInstance.clear_contents);
            indexTable["set_text"] = DynValue.FromObject(lua, (Action<string, string>)bridgeInstance.set_text);

            // Pub/Sub
            indexTable["publish"] = DynValue.FromObject(lua, (Action<string, DynValue>)bridgeInstance.publish);
            indexTable["subscribe"] = DynValue.FromObject(lua, (Action<string, DynValue>)bridgeInstance.subscribe);

            // Shell Utilities / Data Providers
            indexTable["launch"] = DynValue.FromObject(lua, (Action<string, string?>)((exe, args) => bridgeInstance.launch(exe, args ?? ""))); // Handle optional args
            indexTable["connect_wifi"] = DynValue.FromObject(lua, (Action<string>)bridgeInstance.connect_wifi);
            indexTable["get_wifi_networks"] = DynValue.FromObject(lua, (Func<int, DynValue>)bridgeInstance.get_wifi_networks);
            indexTable["get_recent_apps"] = DynValue.FromObject(lua, (Func<int, int, DynValue>)bridgeInstance.get_recent_apps);

            // Wallpaper Manager registration
            _wall.RegisterFunctions(lua, indexTable);

            // Set the metatable and point __index to our function table
            var mt = new Table(lua);
            mt["__index"] = indexTable; // <<< Use the table with explicit functions
            uiTbl.MetaTable = mt;

            // Expose the configured table to Lua globally as "UI"
            lua.Globals["UI"] = uiTbl;

            // Optional: Diagnostic print after setup
            var checkUI = lua.Globals.Get("UI");
            var checkMeta = checkUI.Table?.MetaTable;
            var checkIndex = checkMeta?.RawGet("__index");
            var checkSetUiDefault = checkIndex?.Table?.Get("set_ui_default"); // Check specific function in index table
            Console.WriteLine($"AttachScript Completed: UI type={checkUI.Type}, MetaTable?={checkMeta != null}, Index type={checkIndex?.Type}, set_ui_default in index?={checkSetUiDefault?.Type}");
            // Expected: AttachScript Completed: UI type=Table, MetaTable?=True, Index type=Table, set_ui_default in index?=Function
        }

        private void RegisterFunctions_Animation(Script lua, Table indexTable)
        {
            indexTable["animate_opacity"] = DynValue.FromObject(lua, (Action<string, double, double?>)((id, to, ms) => _anim.Opacity(id, to, ms)(id, to, ms ?? 300)));
            indexTable["animate_layout"] = DynValue.FromObject(lua, (Action<string, double?, double?, double?, double?, double?>)((id, x, y, w, h, ms) => _anim.Layout(id, x, y, w, h, ms ?? 300)));
        }

        private void RegisterFunctions_Style(Script lua, Table indexTable)
        {
            indexTable["set_background"] = DynValue.FromObject(lua, (Action<string, uint>)_style.Background);
            indexTable["set_corner_radius"] = DynValue.FromObject(lua, (Action<string, double>)_style.CornerRadius);
            indexTable["set_shadow"] = DynValue.FromObject(lua, (Action<string, double?, double?, double?, double?>)((id, blur, op, ox, oy) => _style.Shadow(id, blur ?? 40, op ?? .3, ox ?? 0, oy ?? 8)));
            indexTable["set_z"] = DynValue.FromObject(lua, (Action<string, int>)_style.Z);
        }

        private static void RegisterFunctions_Settings(Script lua, Table indexTable, UiBridge bridgeInstance)
        {
            indexTable["set_ui_default"] = DynValue.FromObject(lua, (Action<string, DynValue>)bridgeInstance.set_ui_default);
            indexTable["get_ui_default"] = DynValue.FromObject(lua, (Func<string, DynValue>)bridgeInstance.get_ui_default);
        }

        private void RegisterFunctions_NodeTypes(Script lua, Table indexTable)
        {
            indexTable["add_panel"] = DynValue.FromObject(lua, (Action<string, double, double, double, double, bool>)_nodes.AddPanel);
            indexTable["add_text"] = DynValue.FromObject(lua, (Action<string, string, string?>)((host, txt, style) => _nodes.AddText(host, txt, style ?? "body")));
            indexTable["add_vector"] = DynValue.FromObject(lua, (Action<string, string, double, double>)_nodes.AddVector);
            indexTable["add_image"] = DynValue.FromObject(lua, (Action<string, string, double, double>)_nodes.AddImage);
        }

        public void DetachScript()
        {
            // Nothing to do if no script is attached
            if (_lua == null)
                return;

            try
            {
                // ───────────────────────────────────────────────────────────────
                // 1)  Timers – stop every Lua-scheduled timer and dispose the hub
                // ───────────────────────────────────────────────────────────────
                _timers?.Dispose();
                _timers = null!;  // will be recreated on the next AttachScript

                // ───────────────────────────────────────────────────────────────
                // 2)  InteractionManager – unhook routed events & delegates
                //     (works even if older InteractionManager didn’t expose a
                //     DetachScript method by falling back to IDisposable)
                // ───────────────────────────────────────────────────────────────
                var detachMethod = _inter.GetType().GetMethod("DetachScript");
                if (detachMethod != null)
                {
                    detachMethod.Invoke(_inter, Array.Empty<object?>());
                }
                else if (_inter is IDisposable disposableInter)
                {
                    disposableInter.Dispose();
                }

                // ───────────────────────────────────────────────────────────────
                // 3)  DataProvider – stop background workers, event hooks, etc.
                // ───────────────────────────────────────────────────────────────
                (_data as IDisposable)?.Dispose();
                _data = null!;

                // ───────────────────────────────────────────────────────────────
                // 4)  Pub/Sub – remove every subscription owned by this Script
                // ───────────────────────────────────────────────────────────────
                PubSub.UnsubscribeAll(_lua);

                // ───────────────────────────────────────────────────────────────
                // 5)  Lua side global cleanup
                //     • Make UI table nil so stale coroutines cannot call us.
                //     • Remove any dynamically injected helpers (e.g. the
                //       _hide_lua_error_* functions created by show_lua_error).
                // ───────────────────────────────────────────────────────────────
                _lua.Globals["UI"] = DynValue.Nil;

                foreach (var pair in _lua.Globals.Keys
                                               .Where(k => k.Type == DataType.String &&
                                                           k.String!.StartsWith("_hide_lua_error_"))
                                               .ToArray())
                {
                    _lua.Globals[pair] = DynValue.Nil;
                }
            }
            catch (Exception ex)
            {
                // Hot-reload must never crash the host – log and continue running.
                Console.WriteLine($"[UiBridge] DetachScript failed: {ex}");
            }
            finally
            {
                // ───────────────────────────────────────────────────────────────
                // 6)  Break the final reference so the old VM can be collected
                // ───────────────────────────────────────────────────────────────
                _lua = null;
            }
        }

        public void show_lua_error(string title, Exception ex)
        {
            if (_lua == null)
            {
                MessageBox.Show($"Lua Script instance not available. Cannot display error:\n{title}\n{ex}", "Critical Shell Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // --- Build Detailed Error Message ---
            var sb = new StringBuilder();
            string? errorFilePath = null;
            int errorLine = -1;

            // 1. Exception Type and Main Message
            sb.AppendLine($"--- {ex.GetType().Name} ---");
            sb.AppendLine(ex.Message); // Main message first
            sb.AppendLine();

            // 2. Specific Error Details (Syntax vs Runtime)
            if (ex is ScriptRuntimeException rt)
            {
                sb.AppendLine("--- LUA STACK TRACE ---");
                // Attempt to get source ref from the top of the stack
                SourceRef? sourceRef = rt.CallStack?.FirstOrDefault()?.Location;
                if (sourceRef != null)
                {
                    errorFilePath = _lua.GetSourceCode(sourceRef.SourceIdx)?.Name;
                    errorLine = sourceRef.FromLine + 1; // MoonSharp lines are 0-based? Adjust if needed.
                }
                sb.AppendLine(FormatLuaStackTrace(rt.CallStack)); // Format stack trace
                sb.AppendLine();
            }
            else if (ex is SyntaxErrorException se)
            {
                sb.AppendLine("--- LOCATION ---");
                errorFilePath = se.Source; // SourceCode might be null if DoString used
                sb.AppendLine($"File: {errorFilePath ?? "Unknown"}");
                sb.AppendLine();
            }

            // 3. Source Code Snippet (if location is known)
            if (!string.IsNullOrEmpty(errorFilePath) && errorLine > 0)
            {
                sb.AppendLine("--- SOURCE CODE SNIPPET ---");
                if (TryReadSourceLines(errorFilePath, out string[]? lines))
                {
                    sb.AppendLine(FormatSourceSnippet(lines!, errorLine, 3)); // Show 3 lines context
                }
                else
                {
                    sb.AppendLine($"Error: Could not read source file: {errorFilePath}");
                }
                sb.AppendLine();
            }

            // 4. Inner C# Exceptions
            var inner = ex.InnerException;
            int innerLevel = 1;
            while (inner != null)
            {
                sb.AppendLine($"--- INNER EXCEPTION ({innerLevel}) ---");
                sb.AppendLine($"Type: {inner.GetType().FullName}");
                sb.AppendLine($"Message: {inner.Message}");
                // Optionally add inner stack trace if needed:
                // sb.AppendLine("Stack Trace:");
                // sb.AppendLine(inner.StackTrace ?? "N/A");
                sb.AppendLine();
                inner = inner.InnerException;
                innerLevel++;
            }

            // 5. Original Decorated Message (for reference)
            sb.AppendLine("--- ORIGINAL MOONSHARP MESSAGE ---");
            sb.AppendLine(ex switch
            {
                ScriptRuntimeException rtex => rtex.DecoratedMessage,
                SyntaxErrorException seex => seex.DecoratedMessage,
                _ => ex.ToString() // Fallback
            });

            // --- Create UI ---
            string detailedMsg = sb.ToString();
            const double width = 1000; // Wider for more info
            const double height = 600; // Taller

            string errorGuid = Guid.NewGuid().ToString("N");
            string panelId = "lua_err_" + errorGuid;
            string closeButtonId = panelId + "_close";
            string hideFunctionName = $"_hide_lua_error_{errorGuid}";

            Application.Current.Dispatcher.Invoke(() => {
                try // Add try-catch around UI creation itself
                {
                    // Check if panel already exists (less likely with GUID but good practice)
                    if (_nodes.TryGet(panelId, out _)) return;

                    // Root error panel
                    _nodes.AddPanel(panelId, 50, 50, width, height, true); // AddPanel creates Border + StackPanel
                    _style.Background(panelId, 0xDD200000); // Darker, less transparent red
                    _style.CornerRadius(panelId, 12);
                    _style.Shadow(panelId, 60, .45, 0, 10);
                    _style.Z(panelId, 50_000);

                    // Get the inner StackPanel created by AddPanel
                    if (!_nodes.TryGet(panelId, out var errorBorder) || !(errorBorder.Child is StackPanel errorPanel))
                    {
                        // This shouldn't happen if AddPanel works correctly
                        MessageBox.Show("Failed to create error panel structure.", "UI Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Header Title
                    errorPanel.Children.Add(new TextBlock
                    {
                        Text = title, // Use the original title passed in
                        Foreground = Brushes.White,
                        FontFamily = ThemeRuntime.DefaultFont,
                        FontSize = 20, // Title size
                        Margin = new Thickness(ThemeRuntime.Grid, ThemeRuntime.Grid / 2, ThemeRuntime.Grid, ThemeRuntime.Grid)
                    });

                    // ScrollViewer for detailed message
                    var scroll = new ScrollViewer
                    {
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, // Enable horizontal scroll
                        Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x1E, 0x1E, 0x1E)), // Even darker background
                        Padding = new Thickness(ThemeRuntime.Grid),
                        Margin = new Thickness(ThemeRuntime.Grid, 0, ThemeRuntime.Grid, ThemeRuntime.Grid),
                        BorderBrush = Brushes.DimGray,
                        BorderThickness = new Thickness(1),
                        Content = new TextBlock
                        {
                            Text = detailedMsg, // Use the detailed message
                            FontFamily = new FontFamily("Consolas"), // Monospace is crucial
                            FontSize = 12,
                            Foreground = Brushes.WhiteSmoke,
                            TextWrapping = TextWrapping.NoWrap // Important for stack traces etc.
                        }
                    };
                    errorPanel.Children.Add(scroll);

                    // Adjust ScrollViewer height (optional, could fill remaining space)
                    // This requires knowing the header size or using a different layout like Grid
                    // For now, let StackPanel manage size.


                    // Close button (absolute position on RootCanvas)
                    double closeButtonX = Canvas.GetLeft(errorBorder) + width - 30 - 10; // Position relative to error panel
                    double closeButtonY = Canvas.GetTop(errorBorder) + 10;
                    _nodes.AddPanel(closeButtonId, closeButtonX, closeButtonY, 30, 30, false);
                    _style.Background(closeButtonId, 0xAAFFFFFF);
                    _style.CornerRadius(closeButtonId, 15);
                    _style.Z(closeButtonId, 50_001); // On top

                    // Add 'X' text and center it
                    if (_nodes.TryGet(closeButtonId, out var closeBorder) && closeBorder.Child is Panel closePanel) // AddPanel now creates StackPanel
                    {
                        var closeText = new TextBlock
                        {
                            Text = "✕",
                            Foreground = Brushes.Black,
                            FontSize = 16, // Title size for 'X'
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(0, -2, 0, 0) // Fine-tune alignment
                        };
                        // Since AddPanel creates a StackPanel, add the TextBlock to it
                        closePanel.Children.Add(closeText);

                        // Make the StackPanel center its content (optional, needs testing)
                        //closePanel.HorizontalAlignment = HorizontalAlignment.Center;
                        //closePanel.VerticalAlignment = VerticalAlignment.Center;
                    }


                    // Register click handler
                    _inter.RegisterClick(closeButtonId, hideFunctionName);

                    // Define Lua hide function
                    _lua.Globals[hideFunctionName] = (System.Action)(() => {
                        _anim.Opacity(panelId, 0, 150);
                        // Optional: Add mechanism to fully remove the element after fade
                        // e.g., using a DispatcherTimer or animation Completed event
                        // _nodes.RemovePanel(panelId); // Requires implementing RemovePanel
                    });

                    // Ensure visibility
                    _anim.Opacity(panelId, 1, 150); // Fade in gently
                }
                catch (Exception uiEx)
                {
                    // Fallback if UI creation itself fails
                    MessageBox.Show($"Failed to display Lua error UI:\n{uiEx}\n\nOriginal Lua Error:\n{detailedMsg}",
                                    "Critical Shell UI Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });
        }

        private bool TryReadSourceLines(string filePath, out string[]? lines)
        {
            lines = null;
            try
            {
                if (File.Exists(filePath))
                {
                    lines = File.ReadAllLines(filePath);
                    return true;
                }
                return false;
            }
            catch (Exception ex) // Catch IO errors, security exceptions etc.
            {
                Console.WriteLine($"Error reading source file '{filePath}': {ex.Message}");
                return false;
            }
        }

        private string FormatSourceSnippet(string[] lines, int errorLine, int contextLines = 2)
        {
            if (lines == null || lines.Length == 0) return "Source not available.";

            var sb = new StringBuilder();
            // Adjust errorLine to be 0-based index
            int errorIndex = errorLine - 1;

            int startLine = Math.Max(0, errorIndex - contextLines);
            int endLine = Math.Min(lines.Length - 1, errorIndex + contextLines);

            for (int i = startLine; i <= endLine; i++)
            {
                string prefix = (i == errorIndex) ? ">> " : "   ";
                int displayLineNum = i + 1; // Show 1-based line numbers
                sb.AppendLine($"{prefix}{displayLineNum:D4}: {lines[i]}");
            }
            return sb.ToString();
        }

        private string FormatLuaStackTrace(IList<WatchItem>? callStack)
        {
            if (callStack == null || !callStack.Any()) return "  (No Lua stack trace available)\n";

            var sb = new StringBuilder();
            foreach (var frame in callStack)
            {
                string funcName = frame.Name ?? "(anonymous)";
                string location = "Unknown Source";
                if (frame.Location != null)
                {
                    string? fileName = _lua?.GetSourceCode(frame.Location.SourceIdx)?.Name;
                    // Adjust line number if MoonSharp uses 0-based internally for SourceRef
                    int line = frame.Location.FromLine + 1;
                    location = $"{fileName ?? "Source"}:{line}";
                }
                sb.AppendLine($"  at {funcName} in {location}");
            }
            return sb.ToString();
        }

        // ------------------------------------------------------------------
        //  layout creation
        // ------------------------------------------------------------------
        public void add_panel(string id, double x, double y, double w, double h, bool vert) =>
            _nodes.AddPanel(id, x, y, w, h, vert);

        public void add_text(string host, string txt, string style = "body") =>
            _nodes.AddText(host, txt, style);

        public void add_vector(string host, string path, double w, double h) =>
            _nodes.AddVector(host, path, w, h);

        public void add_image(string host, string path, double w, double h) =>
            _nodes.AddImage(host, path, w, h);

        // ------------------------------------------------------------------
        //  styling
        // ------------------------------------------------------------------
        public void set_background(string id, uint argb) => _style.Background(id, argb);
        public void set_corner_radius(string id, double r) => _style.CornerRadius(id, r);
        public void set_shadow(string id, double blur = 40, double op = .3,
                                      double ox = 0, double oy = 8) =>
                                      _style.Shadow(id, blur, op, ox, oy);
        public void set_z(string id, int z) => _style.Z(id, z);

        // ------------------------------------------------------------------
        //  animation
        // ------------------------------------------------------------------
        public void animate_opacity(string id, double to, double ms = 300) =>
            _anim.Opacity(id, to, ms);

        public void animate_layout(string id,
                                    double? x, double? y, double? w, double? h,
                                    double ms = 300) =>
            _anim.Layout(id, x, y, w, h, ms);

        // ------------------------------------------------------------------
        //  interaction
        // ------------------------------------------------------------------
        public void enable_resize(string id, int grip = 6) => _inter.EnableResize(id, grip);
        public void register_click(string id, string luaFn) => _inter.RegisterClick(id, luaFn);
        public void register_size_callback(string id, string luaFn) => _inter.RegisterSizeCb(id, luaFn);

        // ------------------------------------------------------------------
        //  timers  (TimerHub shared across reloads)
        // ------------------------------------------------------------------
        public void start_timer(int intervalMs, string luaFnId) => _timers.Start(luaFnId, intervalMs);
        public void stop_timer(string id) => _timers.Stop(id);

        // ------------------------------------------------------------------
        //  bounds / screen / helper setters
        // ------------------------------------------------------------------
        public DynValue get_bounds(string id) => _nodes.GetBounds(_data.Script, id);
        public DynValue get_screen()
        {
            var t = new Table(_data.Script);
            t["w"] = this._rootCanvas.ActualWidth;
            t["h"] = this._rootCanvas.ActualHeight;
            return DynValue.NewTable(t);
        }
        public DynValue get_parent()
        {
            if (_lua == null)
            {
                return DynValue.Nil;
            }

            return null;
        }

        public void clear_contents(string id) => _nodes.ClearContents(id);
        public void set_text(string id, string txt)
        {
            _nodes.ClearContents(id);
            _nodes.AddText(id, txt, "title");
        }

        // ------------------------------------------------------------------
        //  global pub/sub  (C# managed, hot‑reload safe)
        // ------------------------------------------------------------------
        private void Pub(string topic, DynValue payload) => PubSub.Publish(topic, payload);
        private void Sub(string topic, DynValue cb) => PubSub.Subscribe(_data.Script, topic, cb);
        public void publish(string topic, DynValue payload) => PubSub.Publish(topic, payload);
        public void subscribe(string topic, DynValue cb)
        {
            if (_lua == null) return;
            PubSub.Subscribe(_lua, topic, cb);
        }

        // ------------------------------------------------------------------
        //  shell utilities & data providers
        // ------------------------------------------------------------------
        public void launch(string exe, string args = "") => _data.Launch(exe, args);
        public void connect_wifi(string ssid) => _data.ConnectWifi(ssid);
        public DynValue get_wifi_networks(int max) =>
            _data.WifiNetworks(max);
        public DynValue get_recent_apps(int days, int max) =>
            _data.RecentApps(days, max);

        // ------------------------------------------------------------------
        //  theme key‑value store bridge (C# direct calls)
        // ------------------------------------------------------------------
        public void set_ui_default(string key, DynValue val)
        {
            if (val == null || val.IsVoid())
            {
                Console.WriteLine($"Warning: Attempted to set UI default '{key}' with nil value.");
                return;
            }

            object? o = val.Type switch
            {
                DataType.String => val.String,
                DataType.Number => val.Number,
                DataType.Boolean => val.Boolean,
                // Be careful with other types, ensure they are serializable or meaningful as settings
                _ => val.ToObject() // Use ToObject cautiously
            };

            if (o != null)
            {
                ThemeRuntime.Set(key, o);
            }
            else
            {
                Console.WriteLine($"Warning: Could not convert DynValue type {val.Type} for UI default '{key}'. Value: {val.ToString()}");
            }
        }
        public DynValue get_ui_default(string key)
        {
            // Use _lua directly, as _data might not be fully initialized if called early
            // Ensure _lua is available
            if (_lua == null) return DynValue.Nil;

            object? value = ThemeRuntime.Get(key);
            // Check if the value exists before trying to convert
            if (value == null) return DynValue.Nil;

            return DynValue.FromObject(_lua, value);
        }

        // ------------------------------------------------------------------
        //  wallpaper
        // ------------------------------------------------------------------
        public void apply_wallpaper(string path, double blur = 0) =>
            _wall.Apply(path, blur);

    }
}
