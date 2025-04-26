using System.Collections.Concurrent;
using System.IO;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PerfectShell.Core
{
    internal static class VectorAssets
    {
        private static readonly ConcurrentDictionary<string, Drawing> _cache = new();

        public static ImageSource Get(string path, double targetPixels)
        {
            var dr = _cache.GetOrAdd(path, p => {
                using var fs = File.OpenRead(p);
                return (Drawing)XamlReader.Load(fs);
            });

            // scale drawing according to requested pixel size (assumes square)
            double scale = targetPixels / (dr.Bounds.Width);
            var group = new DrawingGroup();
            group.Children.Add(dr);
            group.Transform = new ScaleTransform(scale, scale);

            return new DrawingImage(group);
        }

        public static ImageSource GetIcon(string name, double px) =>
            Get(Path.Combine("Config", "icons", $"{name}.xaml"), px);
    }
}
