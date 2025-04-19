using MoonSharp.Interpreter;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace PerfectShell.Core
{
    internal class PubSub
    {
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid,Sub>> _topics = new();

        private sealed record Sub(WeakReference<Script> ScriptRef, DynValue Fn);

        public static void Publish(string topic, DynValue data)
        {
            if (!_topics.TryGetValue(topic, out var subs)) return;

            foreach (var (id, sub) in subs)
            {
                if (sub.ScriptRef.TryGetTarget(out var scr))
                {
                    scr.Call(sub.Fn, data);
                }
                else
                {
                    subs.TryRemove(id, out _);
                }
            }
        }

        public static Guid Subscribe(string topic, Script script, DynValue fn)
        {
            if (fn.Type != DataType.Function)
            {
                throw new ArgumentException("fn must be a function");
            }

            var sub = new Sub(new WeakReference<Script>(script), fn);
            var set = _topics.GetOrAdd(topic, _ => new ConcurrentDictionary<Guid, Sub>());
            var id = Guid.NewGuid();
            set[id] = sub;
            return id;
        }

        public static void Unsubscribe(string topic, Guid id)
        {
            if (_topics.TryGetValue(topic, out var subs))
            {
                subs.TryRemove(id, out _);
            }
        }
    }
}
