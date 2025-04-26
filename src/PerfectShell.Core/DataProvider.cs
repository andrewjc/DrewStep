// ───────────────────────────────────────────────────────────────────────────
//  DataProvider.cs
//  PerfectShell.Core
//  • Process launch helpers
//  • Wi‑Fi enumeration & connect
//  • Recent‑apps query
//  Exposed to Lua through UiBridge.
// ───────────────────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MoonSharp.Interpreter;

namespace PerfectShell.Core
{
    internal sealed class DataProvider : IDisposable
    {
        // ------------------------------------------------------------------
        //  state
        // ------------------------------------------------------------------
        private readonly Script _lua;
        public Script Script => _lua;            // expose for UiBridge helpers

        public DataProvider(Script lua) => _lua = lua;

        // ------------------------------------------------------------------
        //  process / shell operations
        // ------------------------------------------------------------------
        public void Launch(string exe, string args = "")
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = args,
                    UseShellExecute = true
                });
            }
            catch { /* swallow */ }
        }

        public void ConnectWifi(string ssid)
        {
            try
            {
                Process.Start(new ProcessStartInfo("netsh",
                    $@"wlan connect name=""{ssid}""")
                { UseShellExecute = true, CreateNoWindow = true });
            }
            catch { /* swallow */ }
        }

        // ------------------------------------------------------------------
        //  Wi‑Fi enumeration
        // ------------------------------------------------------------------
        public DynValue WifiNetworks(int max = 20)
        {
            // call netsh once
            var psi = new ProcessStartInfo("netsh", "wlan show networks mode=bssid")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            string output;
            using (var p = Process.Start(psi))
            {
                output = p!.StandardOutput.ReadToEnd();
                p.WaitForExit();
            }

            var list = new List<(string ssid, int signal)>();
            string? currentSsid = null;
            int currentSig = 0;

            foreach (string raw in output.Split('\n'))
            {
                string line = raw.Trim();

                if (line.StartsWith("SSID ", StringComparison.Ordinal))
                {
                    int idx = line.IndexOf(':');
                    if (idx >= 0)
                        currentSsid = line[(idx + 1)..].Trim();
                }
                else if (line.StartsWith("Signal", StringComparison.Ordinal))
                {
                    int idx = line.IndexOf(':');
                    if (idx >= 0)
                        int.TryParse(line[(idx + 1)..].Trim().TrimEnd('%'), out currentSig);
                }
                else if (line.StartsWith("Authentication", StringComparison.Ordinal))
                {
                    // end of block
                    if (!string.IsNullOrEmpty(currentSsid))
                        list.Add((currentSsid!, currentSig));

                    currentSsid = null;
                }
            }

            var strongest = list
                .OrderByDescending(t => t.signal)
                .Take(max)
                .ToList();

            var tbl = new Table(_lua);
            foreach (var n in strongest)
            {
                var t = new Table(_lua);
                t["ssid"] = n.ssid;
                t["signal"] = n.signal;
                tbl.Append(DynValue.NewTable(t));
            }
            return DynValue.NewTable(tbl);
        }

        // ------------------------------------------------------------------
        //  recent‑apps enumeration
        // ------------------------------------------------------------------
        public DynValue RecentApps(int days, int max)
        {
            var recentDir = Environment.GetFolderPath(Environment.SpecialFolder.Recent);
            var cutoff = DateTime.Now.AddDays(-days);

            var apps = Directory.EnumerateFiles(recentDir, "*.lnk")
                        .Where(f => File.GetLastAccessTime(f) >= cutoff)
                        .GroupBy(Path.GetFileNameWithoutExtension)
                        .Select(g => (Name: g.Key, Count: g.Count()))
                        .OrderByDescending(t => t.Count)
                        .Take(max)
                        .Select(t => t.Name)
                        .ToList();

            var tbl = new Table(_lua);
            foreach (var app in apps) tbl.Append(DynValue.NewString(app));
            return DynValue.NewTable(tbl);
        }

        public void Dispose()
        {
            
        }
    }
}
