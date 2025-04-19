using System;
using System.Collections.Concurrent;
using System.Windows.Threading;

namespace PerfectShell.Core
{
    /// <summary>
    /// One DispatcherTimer per ID, shared across Lua reloads.
    /// </summary>
    internal static class Timers
    {
        private static readonly ConcurrentDictionary<string, DispatcherTimer> _timers = new();

        public static void Start(string id, int ms, Action tick)
        {
            var t = _timers.GetOrAdd(id, _ =>
            {
                var d = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(ms)
                };
                d.Start();
                return d;
            });
            t.Tick += (_, _) => tick();
        }

        public static void Stop(string id)
        {
            if (_timers.TryRemove(id, out var t))
                t.Stop();
        }
    }
}
