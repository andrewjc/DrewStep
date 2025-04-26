using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace PerfectShell.Core
{
    /// <summary>
    /// Global broker that survives hot‑reloads of Lua scripts.
    /// Subscribers are weak‑referenced so old Script instances
    /// get GC’d naturally when a new init.lua is loaded.
    /// </summary>
    internal static class PubSub
    {
        private sealed class Subscriber
        {
            public readonly WeakReference<Script> ScriptRef;
            public readonly DynValue Callback;

            public Subscriber(Script s, DynValue fn)
            {
                ScriptRef = new WeakReference<Script>(s);
                Callback = fn;
            }
        }

        private static readonly ConcurrentDictionary<string,
            List<Subscriber>> _topics = new();

        // --------------- API called from UiBridge -----------------

        public static void Publish(string topic, DynValue payload)
        {
            if (!_topics.TryGetValue(topic, out var list)) return;

            lock (list)
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (!list[i].ScriptRef.TryGetTarget(out var scr))
                    {
                        list.RemoveAt(i);                  // dead script
                        continue;
                    }
                    scr.Call(list[i].Callback, payload);   // fire
                }
            }
        }

        public static void Subscribe(Script script, string topic, DynValue fn)
        {
            if (!fn.IsNotNil()) return;

            var list = _topics.GetOrAdd(topic, _ => new List<Subscriber>());

            lock (list)
            {
                list.Add(new Subscriber(script, fn));
            }
        }

        internal static void UnsubscribeAll(Script lua)
        {
            lock(_topics)
            {
                foreach (var list in _topics.Values)
                {
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        if (list[i].ScriptRef.TryGetTarget(out var scr) && scr == lua)
                            list.RemoveAt(i);
                    }
                }
            }
        }
    }
}
