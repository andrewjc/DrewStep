
using MoonSharp.Interpreter;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using MoonSharp.Interpreter.Loaders;
using System.IO;
using System.Collections.Generic;
using System.Collections;

namespace PerfectShell.Core;

/// <summary>
/// Boots a MoonSharp runtime, loads YAML‑driven layout+theme, and exposes C# APIs into Lua.
/// </summary>
public sealed class LuaHost
{
    public Script Vm { get; }

    public LuaHost(string configFolder, UiBridge uiBridge)
    {
        UserData.RegisterType<UiBridge>();

        Script.DefaultOptions.ScriptLoader = new FileSystemScriptLoader
        {
            ModulePaths = new[]
            {
                $"{configFolder}/?.lua",
                $"{configFolder}/?/init.lua"
            }
        };

        Vm = new Script(CoreModules.Preset_Complete);

        // 1) Inject C# singletons
        Vm.Globals["Design"] = DesignTokensTable(Vm);
        Vm.Globals["UI"]     = uiBridge; // CLR object; MoonSharp auto‑proxies

        // 2) Load YAML config → push into Lua.
        Vm.Globals["Layout"] = LoadYamlIntoLuaTable(Vm, $"{configFolder}/layout.yaml");
        Vm.Globals["Theme"] = LoadYamlIntoLuaTable(Vm, $"{configFolder}/theme.yaml");

        // 3) Bootstrap
        Vm.DoFile($"{configFolder}/init.lua");
    }

    private static Table DesignTokensTable(Script script)
    {
        var t = new Table(script);          // ← correct ctor
        t["grid"] = DesignTokens.Space8;
        t["radius"] = DesignTokens.Radius16;
        t["primary"] = DesignTokens.Primary.ToUint();  // extension now works
        t["surface"] = DesignTokens.SurfaceGlass.ToUint();
        return t;
    }

    private static Table LoadYamlIntoLuaTable(Script script, string path)
    {
        if (!File.Exists(path))
            return new Table(script);                      // empty Lua table

        var yaml = File.ReadAllText(path);
        var deserial = new DeserializerBuilder()
                          .WithNamingConvention(CamelCaseNamingConvention.Instance)
                          .Build();

        var obj = deserial.Deserialize<object>(yaml);
        return ToLuaTable(script, obj);
    }

    /// <summary>
    /// Recursively converts arbitrary YAML‑deserialized objects
    /// (dictionaries, lists, scalars) into MoonSharp tables/values.
    /// </summary>
    private static Table ToLuaTable(Script script, object obj)
    {
        var table = new Table(script);

        switch (obj)
        {
            case IDictionary<object, object> map:
                foreach (var kv in map)
                    table[kv.Key.ToString()] = ToLuaDyn(script, kv.Value);
                break;

            case IList list:
                for (int i = 0; i < list.Count; i++)
                    table[i + 1] = ToLuaDyn(script, list[i]);   // Lua is 1‑based
                break;

            default:
                // scalar at the root – put it under ["value"]
                table["value"] = ToLuaDyn(script, obj);
                break;
        }

        return table;
    }

    private static DynValue ToLuaDyn(Script script, object value)
    {
        return value switch
        {
            null => DynValue.Nil,
            IDictionary<object, object> map => DynValue.NewTable(ToLuaTable(script, map)),
            IList list => DynValue.NewTable(ToLuaTable(script, list)),
            _ => DynValue.FromObject(script, value)
        };
    }
}
