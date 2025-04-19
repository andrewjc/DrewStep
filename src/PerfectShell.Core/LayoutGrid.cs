
namespace PerfectShell.Core;

/// <summary>
/// Utility to snap any coordinate or size to the nearest 8‑pixel grid unit.
/// </summary>
public static class LayoutGrid
{
    private const int Grid = (int)DesignTokens.Space8;
    public static double Snap(double value) => System.Math.Round(value / Grid) * Grid;
}
