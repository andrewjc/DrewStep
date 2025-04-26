// ───────────────────────────────────────────────────────────────────────────
//  AnimationManager.cs
//  PerfectShell.Core
//  Handles opacity and layout tweens with uniform easing.
// ───────────────────────────────────────────────────────────────────────────
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace PerfectShell.Core
{
    internal sealed class AnimationManager
    {
        // ------------------------------------------------------------------
        //  state
        // ------------------------------------------------------------------
        private readonly LayoutNodeManager _nodes;

        public AnimationManager(LayoutNodeManager nodes) => _nodes = nodes;

        // ------------------------------------------------------------------
        //  opacity tween
        // ------------------------------------------------------------------
        public void Opacity(string id, double to, double ms)
        {
            if (!_nodes.TryGet(id, out var node)) return;

            Utils.Run(() =>
            {
                node.BeginAnimation(UIElement.OpacityProperty,
                    new DoubleAnimation(to, System.TimeSpan.FromMilliseconds(ms))
                    { EasingFunction = Utils.Quad });
            });
        }

        // ------------------------------------------------------------------
        //  layout tween (x / y / w / h are optional)
        // ------------------------------------------------------------------
        public void Layout(string id,
                           double? x, double? y,
                           double? w, double? h,
                           double ms)
        {
            if (!_nodes.TryGet(id, out var node)) return;

            Utils.Run(() =>
            {
                if (x.HasValue)
                {
                    node.BeginAnimation(Canvas.LeftProperty,
                        Utils.DA(Canvas.GetLeft(node), x.Value, ms));
                }
                if (y.HasValue)
                {
                    node.BeginAnimation(Canvas.TopProperty,
                        Utils.DA(Canvas.GetTop(node), y.Value, ms));
                }
                if (w.HasValue)
                {
                    node.BeginAnimation(FrameworkElement.WidthProperty,
                        Utils.DA(node.Width, w.Value, ms));
                }
                if (h.HasValue)
                {
                    node.BeginAnimation(FrameworkElement.HeightProperty,
                        Utils.DA(node.Height, h.Value, ms));
                }
            });
        }
    }
}
