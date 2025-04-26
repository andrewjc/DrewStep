// ───────────────────────────────────────────────────────────────────────────
//  Utils.cs  ·  shared helper methods
//  Part of PerfectShell.Core
// ───────────────────────────────────────────────────────────────────────────
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PerfectShell.Core
{
    /// <summary>
    /// Miscellaneous helpers shared by the bridge sub‑systems.
    /// </summary>
    internal static class Utils
    {
        // ------------------------------------------------------------------
        //  Colors
        // ------------------------------------------------------------------
        /// <summary>Convert 0xAARRGGBB to a WPF Color.</summary>
        internal static Color ARGB(uint v) =>
            Color.FromArgb(
                (byte)(v >> 24),
                (byte)(v >> 16),
                (byte)(v >> 8),
                (byte)v);

        // ------------------------------------------------------------------
        //  Animation helpers
        // ------------------------------------------------------------------
        internal static readonly IEasingFunction Quad =
            new QuadraticEase { EasingMode = EasingMode.EaseInOut };

        internal static DoubleAnimation DA(double from, double to, double ms) =>
            new DoubleAnimation(to, TimeSpan.FromMilliseconds(ms))
            {
                From = from,
                EasingFunction = Quad
            };

        // ------------------------------------------------------------------
        //  Thread helper
        // ------------------------------------------------------------------
        /// <summary>
        /// Ensure <paramref name="action"/> runs on WPF’s UI thread.
        /// </summary>
        internal static void Run(Action action) =>
            Application.Current.Dispatcher.Invoke(action);
    }
}
