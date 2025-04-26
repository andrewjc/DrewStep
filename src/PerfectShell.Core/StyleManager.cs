using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace PerfectShell.Core
{
    internal sealed class StyleManager
    {
        private readonly LayoutNodeManager _nodes;
        public StyleManager(LayoutNodeManager n) => _nodes = n;

        private void Style(string id, System.Action<Border> fn)
        {
            if (_nodes.TryGet(id, out var b))
                Utils.Run(() => fn(b));
        }

        public void Background(string id, uint argb) =>
            Style(id, b => b.Background = new SolidColorBrush(Utils.ARGB(argb)));

        public void CornerRadius(string id, double r) =>
            Style(id, b => b.CornerRadius = new System.Windows.CornerRadius(r));

        public void Shadow(string id, double blur, double op, double ox, double oy) =>
            Style(id, b => b.Effect = new DropShadowEffect
            {
                BlurRadius = blur,
                Opacity = op,
                ShadowDepth = System.Math.Sqrt(ox * ox + oy * oy),
                Direction = System.Math.Atan2(oy, ox) * 180 / System.Math.PI,
                Color = Colors.Black
            });

        public void Z(string id, int z) =>
            Style(id, b => Canvas.SetZIndex(b, z));
    }
}
