using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Markup;

using SkiaSharp;
using Svg.Skia;
using MoonSharp.Interpreter;

namespace PerfectShell.Core
{
    /// <summary>
    /// Manages a full-screen wallpaper (raster, XAML vector, or SVG) and an optional blur.
    /// </summary>
    internal sealed class WallpaperManager
    {
        private readonly Canvas _root;
        private Border? _wallNode;

        public WallpaperManager(Canvas root) =>
            _root = root ?? throw new ArgumentNullException(nameof(root));

        /// <summary>
        /// Loads <paramref name="path"/> (PNG/JPG/BMP, XAML vector, or SVG), scales it to the
        /// primary screen, and applies an optional Gaussian blur.
        /// </summary>
        /// <param name="path">Absolute or relative path to the image file.</param>
        /// <param name="blurRadius">
        /// Radius for the blur in device-independent pixels (DIP). Pass 0 to disable.
        /// </param>
        public void Apply(string path, double blurRadius = 0)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));

            // Utils.Run must marshal to the UI thread; assumed to exist elsewhere.
            Utils.Run(() =>
            {
                EnsureWallNode();

                ImageBrush brush = Path.GetExtension(path).ToLowerInvariant() switch
                {
                    ".xaml" => CreateBrushFromXaml(path),
                    ".svg" => CreateBrushFromSvg(path),
                    _ => CreateBrushFromRaster(path)
                };
                brush.Freeze();

                _wallNode!.Width = SystemParameters.PrimaryScreenWidth;
                _wallNode.Height = SystemParameters.PrimaryScreenHeight;
                _wallNode.Background = brush;
                _wallNode.SnapsToDevicePixels = true;

                _wallNode.Effect = blurRadius > 0
                    ? new BlurEffect { Radius = blurRadius }
                    : null; // clear any previous effect
            });
        }

        // ───────────────────────────── private helpers ──────────────────────────

        private void EnsureWallNode()
        {
            if (_wallNode != null) return;

            _wallNode = new Border();
            Canvas.SetLeft(_wallNode, 0);
            Canvas.SetTop(_wallNode, 0);
            Canvas.SetZIndex(_wallNode, -10_000); // sit behind everything
            _root.Children.Add(_wallNode);
        }

        private static ImageBrush CreateBrushFromXaml(string path)
        {
            using var fs = File.OpenRead(path);
            var drawing = (Drawing)XamlReader.Load(fs);
            drawing.Freeze();

            return new ImageBrush(new DrawingImage(drawing))
            {
                Stretch = Stretch.Fill,
                AlignmentX = AlignmentX.Center,
                AlignmentY = AlignmentY.Center
            };
        }

        private static ImageBrush CreateBrushFromRaster(string path)
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
            bmp.CacheOption = BitmapCacheOption.OnLoad;           // detach from file
            bmp.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            bmp.EndInit();
            bmp.Freeze();

            return new ImageBrush(bmp) { Stretch = Stretch.UniformToFill };
        }

        private static ImageBrush CreateBrushFromSvg(string path)
        {
            int width = (int)Math.Ceiling(SystemParameters.PrimaryScreenWidth);
            int height = (int)Math.Ceiling(SystemParameters.PrimaryScreenHeight);

            // 1️⃣  Load the SVG
            using var svgStream = File.OpenRead(path);
            var svg = new SKSvg();
            var picture = svg.Load(svgStream);
            var bounds = picture.CullRect;

            if (bounds.Width <= 0 || bounds.Height <= 0)
                throw new InvalidOperationException($"Invalid SVG file: {path}");

            // 2️⃣  Render it onto an SKBitmap
            using var skBitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
            using (var canvas = new SKCanvas(skBitmap))
            {
                canvas.Clear(SKColors.Transparent);

                float scale = Math.Min(width / bounds.Width, height / bounds.Height);
                canvas.Scale(scale, scale);
                canvas.Translate(-bounds.Left, -bounds.Top);
                canvas.DrawPicture(picture);
                canvas.Flush();
            }

            // 3️⃣  Convert SKBitmap → WPF BitmapSource
            BitmapSource bitmapSource;
            using (var image = SKImage.FromBitmap(skBitmap))
            using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
            using (var ms = new MemoryStream(data.ToArray()))
            {
                var bimg = new BitmapImage();
                bimg.BeginInit();
                bimg.CacheOption = BitmapCacheOption.OnLoad;
                bimg.StreamSource = ms;
                bimg.EndInit();
                bimg.Freeze();
                bitmapSource = bimg;
            }

            return new ImageBrush(bitmapSource) { Stretch = Stretch.UniformToFill };
        }

        internal void RegisterFunctions(Script lua, Table indexTable)
        {
            throw new NotImplementedException();
        }
    }
}
