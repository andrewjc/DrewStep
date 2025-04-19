
using System.Windows;
using System.Windows.Media;

namespace PerfectShell.Core;

/// <summary>
/// Central repository of immutable design primitives.  
/// Values are multiples of 4 px, rounded to nearest 8 where applicable.
/// </summary>
public static class DesignTokens
{
    // Grid – multiples of 4, exposed for Lua
    public const double Space4   = 4;
    public const double Space8   = 8;
    public const double Space16  = 16;
    public const double Space24  = 24;
    public const double Space32  = 32;
    public const double Space64  = 64;
    public const double Radius16 = 16;

    // Elevation (z‑depth → blur radius & shadow opacity)
    public const double ElevationLowBlur    = 6;
    public const double ElevationLowShadow  = .1;
    public const double ElevationMidBlur    = 12;
    public const double ElevationMidShadow  = .18;

    // Typography scale (Google Material style steps but tuned)
    public static readonly Typeface TypefacePrimary =
        new(new FontFamily("Segoe UI Variable Display"),   // ← changed
            FontStyles.Normal,
            FontWeights.SemiBold,
            FontStretches.Normal);

    public static readonly double FontDisplay   = 48;
    public static readonly double FontHeadline  = 32;
    public static readonly double FontTitle     = 20;
    public static readonly double FontBody      = 14;
    public static readonly double FontCaption   = 12;

    // Colour palette
    public static readonly Color SurfaceTint     = Color.FromRgb(79, 84, 255);
    public static readonly Color SurfaceGlass    = Color.FromArgb(160, 255, 255, 255); // 62 % opacity white
    public static readonly Color Primary         = Color.FromRgb(79, 84, 255);
    public static readonly Color Error           = Color.FromRgb(255, 71, 87);
    public static readonly Color Success         = Color.FromRgb( 52, 199, 89);

    // Derived brushes
    public static readonly SolidColorBrush BrSurfaceTint  = new(SurfaceTint)  { Opacity = .12 };
    public static readonly SolidColorBrush BrSurfaceGlass = new(SurfaceGlass);
    public static readonly SolidColorBrush BrPrimary      = new(Primary);
    public static readonly SolidColorBrush BrError        = new(Error);
    public static readonly SolidColorBrush BrSuccess      = new(Success);

    public static uint ToUint(this Color c) =>
        ((uint)c.A << 24) | ((uint)c.R << 16) | ((uint)c.G << 8) | c.B;
}
