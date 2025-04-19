
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Animation;
using MoonSharp.Interpreter;
using System.Reflection;

namespace PerfectShell.Core;

/// <summary>
/// Cross‑thread‑safe façade whose public methods are callable from Lua
/// to construct or mutate WPF visual elements without exposing raw WPF types.
/// </summary>

[MoonSharpUserData]
public sealed class UiBridge
{
    private readonly Panel _root;
    private readonly Dictionary<string, FrameworkElement> _registry = new();

    public UiBridge(Panel root) => _root = root;

    // -- Panel/node creation -------------------------------------------------

    public void add_panel(string id, double x, double y, double w, double h)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            double dx = Convert.ToDouble(x);
            double dy = Convert.ToDouble(y);
            double dw = Convert.ToDouble(w);
            double dh = Convert.ToDouble(h);

            var card = MakePanel(out var inner);
            card.Width = dw;
            card.Height = dh;
            Canvas.SetLeft(card, dx);
            Canvas.SetTop(card, dy);

            _root.Children.Add(card);
            _registry[id] = inner;
        });
    }

    public void add_text(string panelId, string text, string style = "body")
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (!_registry.TryGetValue(panelId, out var panel)) return;

            var tb = new TextBlock { Text = text, Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap };
            switch (style)
            {
                case "display": tb.FontSize = DesignTokens.FontDisplay;  break;
                case "headline":tb.FontSize = DesignTokens.FontHeadline; break;
                case "title":   tb.FontSize = DesignTokens.FontTitle;    break;
                case "caption": tb.FontSize = DesignTokens.FontCaption;  break;
                default:        tb.FontSize = DesignTokens.FontBody;     break;
            }
            ((Panel)panel).Children.Add(tb);
        });
    }

    public void animate_opacity(string id, double to, double ms = 300)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (!_registry.TryGetValue(id, out var fe)) return;
            var da = new DoubleAnimation(to, TimeSpan.FromMilliseconds(ms))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };
            fe.BeginAnimation(UIElement.OpacityProperty, da);
        });
    }

    // -- Helpers -------------------------------------------------------------

    private static Border MakePanel(out StackPanel inner)
    {
        inner = new StackPanel();
        return new Border
        {
            CornerRadius = new CornerRadius(DesignTokens.Radius16),
            Background = DesignTokens.BrSurfaceGlass,
            Padding = new Thickness(DesignTokens.Space16),
            Effect = new BlurEffect { Radius = DesignTokens.ElevationLowBlur },
            Child = inner
        };
    }
}
