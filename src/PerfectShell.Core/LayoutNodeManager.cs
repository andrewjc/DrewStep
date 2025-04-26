// ───────────────────────────────────────────────────────────────────────────
//  LayoutNodeManager.cs
//  PerfectShell.Core
//  Creates and manages panels, plus adding text / images / vectors.
// ───────────────────────────────────────────────────────────────────────────
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MoonSharp.Interpreter;

namespace PerfectShell.Core
{
    internal sealed class LayoutNodeManager
    {
        // ------------------------------------------------------------------
        //  state
        // ------------------------------------------------------------------
        private readonly Canvas _root;
        private readonly Dictionary<string, Border> _nodes = new();

        public LayoutNodeManager(Canvas root) => _root = root;

        // ------------------------------------------------------------------
        //  creation helpers
        // ------------------------------------------------------------------
        public void AddPanel(string id,
                             double x, double y,
                             double w, double h,
                             bool vertical)
        {
            Utils.Run(() =>
            {
                var border = new Border
                {
                    Background = ThemeRuntime.SurfaceBrush,
                    CornerRadius = new System.Windows.CornerRadius(
                                        ThemeRuntime.GetDouble("RadiusDefault")),
                    Padding = new System.Windows.Thickness(
                                        ThemeRuntime.GetDouble("GridBaseUnit")),
                    Child = new StackPanel
                    {
                        Orientation = vertical
                                      ? Orientation.Vertical
                                      : Orientation.Horizontal
                    }
                };

                border.Width = w;
                border.Height = h;
                Canvas.SetLeft(border, x);
                Canvas.SetTop(border, y);

                _root.Children.Add(border);
                _nodes[id] = border;
            });
        }

        // ------------------------------------------------------------------
        //  add children
        // ------------------------------------------------------------------
        public void AddText(string hostId, string text, string style)
        {
            Utils.Run(() =>
            {
                if (!(_nodes.TryGetValue(hostId, out var host) &&
                      host.Child is Panel panel)) return;

                var tb = new TextBlock
                {
                    Text = text,
                    Foreground = Brushes.White,
                    FontFamily = ThemeRuntime.DefaultFont,
                    TextWrapping = System.Windows.TextWrapping.NoWrap,
                    FontSize = style switch
                    {
                        "large" => 24,
                        "title" => 18,
                        "caption" => 12,
                        _ => 14
                    }
                };
                panel.Children.Add(tb);
            });
        }

        public void AddVector(string hostId, string xamlPath, double w, double h)
        {
            Utils.Run(() =>
            {
                if (!(_nodes.TryGetValue(hostId, out var host) &&
                      host.Child is Panel panel)) return;

                // DPI‑aware target pixel size
                double px = w * ThemeRuntime.GetDouble("DpiScale");
                var img = new Image
                {
                    Width = w,
                    Height = h,
                    Stretch = Stretch.Uniform,
                    Source = VectorAssets.Get(xamlPath, px)
                };
                panel.Children.Add(img);
            });
        }

        public void AddImage(string hostId, string path, double w, double h)
        {
            Utils.Run(() =>
            {
                if (!(_nodes.TryGetValue(hostId, out var host) &&
                      host.Child is Panel panel)) return;

                var img = new Image
                {
                    Width = w,
                    Height = h,
                    Stretch = Stretch.Uniform,
                    Source = new BitmapImage(
                                new System.Uri(path, System.UriKind.RelativeOrAbsolute))
                };
                panel.Children.Add(img);
            });
        }

        // ------------------------------------------------------------------
        //  queries & mutations
        // ------------------------------------------------------------------
        public bool TryGet(string id, out Border node) => _nodes.TryGetValue(id, out node);

        public DynValue GetBounds(Script lua, string id)
        {
            if (!_nodes.TryGetValue(id, out var n)) return DynValue.Nil;

            var t = new Table(lua)
            {
                ["x"] = Canvas.GetLeft(n),
                ["y"] = Canvas.GetTop(n),
                ["w"] = n.Width,
                ["h"] = n.Height
            };
            return DynValue.NewTable(t);
        }

        public void ClearContents(string id)
        {
            Utils.Run(() =>
            {
                if (_nodes.TryGetValue(id, out var b) && b.Child is Panel p)
                    p.Children.Clear();
            });
        }
    }
}
