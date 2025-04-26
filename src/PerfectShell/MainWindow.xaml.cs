using System;
using System.IO;
using System.Windows;
using PerfectShell.Core;
using MoonSharp.Interpreter;
using HarfBuzzSharp;
using MoonSharp.Interpreter.Loaders;
using Script = MoonSharp.Interpreter.Script;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;

namespace PerfectShell
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _debounce = new() { Interval = TimeSpan.FromMilliseconds(200) };

        private string _root;
        private Script? _script;
        private UiBridge? _bridge;
        private FileSystemWatcher _watcher;

        public MainWindow()
        {
            InitializeComponent();

            LoadVm();
            StartHotReload();
            
        }


        private void LoadVm()
        {

            // 1) create Lua VM + bridge
            _root = getCurrentBaseDirectory();
            _bridge = new UiBridge(RootCanvas);

            _script = new Script(CoreModules.Preset_Complete);
            _bridge.AttachScript(_script);

            // 2) load bootstrap (defines onInitialize)
            string initPath = Path.Combine(
                getCurrentBaseDirectory(), "Config/init.lua");
            var loader = new FileSystemScriptLoader();
            loader.ModulePaths = new[]
            {
                "?/init.lua",
                "?.lua",
                "Config/?.lua",
                "Config/?/init.lua"
            };
            _script.Options.ScriptLoader = loader;
            try
            {
                _script.DoFile(initPath);
                var initFn = _script.Globals.Get("onInitialize");
                if (!initFn.IsNil()) _script.Call(initFn);
            }
            catch (SyntaxErrorException ex)
            {
                _bridge.show_lua_error("Lua syntax error", ex);
            }
            catch (ScriptRuntimeException ex)
            {
                _bridge.show_lua_error("Lua runtime error", ex);
            }
        }

        private string getCurrentBaseDirectory()
        {

            if (Debugger.IsAttached)
            {

                return "F:\\Development\\DrewStep\\src";
            } else
            {
                String deployedPath = AppDomain.CurrentDomain.BaseDirectory;

                return deployedPath;
            }

        }

        private void StartHotReload()
        {
            _watcher = new FileSystemWatcher(_root, "*.lua")
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
            };
            _watcher.Changed += OnLuaFileTouched;
            _watcher.Renamed += OnLuaFileTouched;
            _watcher.Created += OnLuaFileTouched;

            _debounce.Tick += (_, __) => { _debounce.Stop(); ReloadVm(); };

            _watcher.EnableRaisingEvents = true;
        }

        private void OnLuaFileTouched(object? _, FileSystemEventArgs __)
        {
            // restart the 200 ms timer each time we get a notification
            _debounce.Stop();
            _debounce.Start();
        }

        private void ReloadVm()
        {
            _bridge.DetachScript();   // whatever you need to tear down

            _script = null;
            _bridge = null;

            // clear the RootCanvas
            RootCanvas.Children.Clear();

            LoadVm();                 // build a fresh VM
        }

        public void Dispose()
        {
            _watcher?.Dispose();
        }
    }

}
