// ───────────────────────────────────────────────────────────────────────────
//  InteractionManager.cs
//  PerfectShell.Core
//  • resize‑grip handling (bottom‑right corner)
//  • click event hookup to Lua functions
//  • size‑changed callbacks back to Lua
// ───────────────────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MoonSharp.Interpreter;

namespace PerfectShell.Core
{
    internal sealed class InteractionManager : IDisposable
    {
        // ------------------------------------------------------------------
        //  constants
        // ------------------------------------------------------------------
        private const int GripPx = 16; // size of the resize grip
        // ------------------------------------------------------------------
        //  constructor
        // ------------------------------------------------------------------

        // ------------------------------------------------------------------
        //  state
        // ------------------------------------------------------------------
        private readonly LayoutNodeManager _nodes;
        private readonly Dictionary<string, DynValue> _sizeCbs
            = new Dictionary<string, DynValue>();
        private Script? _lua;

        public InteractionManager(LayoutNodeManager nodes) => _nodes = nodes;

        public void AttachScript(Script lua) => _lua = lua;

        // ------------------------------------------------------------------
        //  public API
        // ------------------------------------------------------------------
        /// <summary>Adds a bottom‑right resize grip to a panel.</summary>
        public void EnableResize(string id, int gripPx)
        {
            if (!_nodes.TryGet(id, out var border)) return;

            bool dragging = false;
            Point origin = default;
            double startW = 0;
            double startH = 0;

            border.MouseMove += (_, e) =>
            {
                var pos = e.GetPosition(border);

                bool overGrip = pos.X >= border.ActualWidth - gripPx &&
                                pos.Y >= border.ActualHeight - gripPx;

                if (!dragging)
                {
                    border.Cursor = overGrip ? Cursors.SizeNWSE : Cursors.Arrow;
                    return;
                }

                // dragging
                var rootPos = e.GetPosition((UIElement)border.Parent);
                border.Width = System.Math.Max(48, startW + (rootPos.X - origin.X));
                border.Height = System.Math.Max(48, startH + (rootPos.Y - origin.Y));

                FireSizeCb(id, border.Width, border.Height);
            };

            border.MouseLeftButtonDown += (_, e) =>
            {
                var pos = e.GetPosition(border);
                if (pos.X < border.ActualWidth - gripPx ||
                    pos.Y < border.ActualHeight - gripPx) return;

                dragging = true;
                origin = e.GetPosition((UIElement)border.Parent);
                startW = border.Width;
                startH = border.Height;

                border.CaptureMouse();
                e.Handled = true;
            };

            border.MouseLeftButtonUp += (_, _) =>
            {
                if (!dragging) return;
                dragging = false;
                border.ReleaseMouseCapture();
            };
        }

        /// <summary>Hook a Lua function (global) to left‑click on a node.</summary>
        public void RegisterClick(string id, string luaFnName)
        {
            if (_lua == null || !_nodes.TryGet(id, out var border)) return;

            border.MouseLeftButtonDown += (_, _) =>
            {
                var fn = _lua!.Globals.Get(luaFnName);
                if (!fn.IsNil())
                    _lua.Call(fn);
            };
        }

        /// <summary>Register Lua callback `(w,h)` invoked whenever EnableResize changes size.</summary>
        public void RegisterSizeCb(string id, string luaFnName)
        {
            if (_lua == null) return;
            _sizeCbs[id] = _lua.Globals.Get(luaFnName);
        }

        // ------------------------------------------------------------------
        //  helpers
        // ------------------------------------------------------------------
        private void FireSizeCb(string id, double w, double h)
        {
            if (_lua != null &&
                _sizeCbs.TryGetValue(id, out var fn) &&
                !fn.IsNil())
            {
                _lua.Call(fn, DynValue.NewNumber(w), DynValue.NewNumber(h));
            }
        }

        private void Dispose(bool disposing)
        {
        }


        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
