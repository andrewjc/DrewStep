// ───────────────────────────────────────────────────────────────────────────
//  TimerManager.cs
//  PerfectShell.Core
//  Wraps TimerHub so Lua can start/stop recurring callbacks
//  that survive hot‑reloads.
// ───────────────────────────────────────────────────────────────────────────
using MoonSharp.Interpreter;
using System;

namespace PerfectShell.Core
{
    internal sealed class TimerManager
    {
        private readonly Script _lua;

        public TimerManager(Script lua) => _lua = lua;

        /// <summary>
        /// Start (or join) a DispatcherTimer identified by <paramref name="id"/>.
        /// Every <paramref name="intervalMs"/> it calls the global Lua function
        /// whose name equals <paramref name="id"/>.
        /// </summary>
        public void Start(string id, int intervalMs)
        {
            Timers.Start(id, intervalMs, () =>
            {
                var fn = _lua.Globals.Get(id);
                if (!fn.IsNil())
                    _lua.Call(fn);
            });
        }

        /// <summary>Stop and dispose the timer identified by <paramref name="id"/>.</summary>
        public void Stop(string id) => Timers.Stop(id);

        internal void Dispose()
        {
            
        }
    }
}
