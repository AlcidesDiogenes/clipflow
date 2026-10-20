using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ClipFlow.App.Platform;

/// <summary>
/// Implementa rolagem suave com inércia/momentum em alta taxa de quadros (60/120/144 FPS)
/// via CompositionTarget.Rendering para ListBox e ScrollViewer do WPF.
/// </summary>
public static class SmoothScrollBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(SmoothScrollBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    private sealed class ScrollState
    {
        public double TargetOffset;
        public double CurrentOffset;
        public bool IsAnimating;
        public DateTime LastFrameTime;
    }

    private static readonly Dictionary<ScrollViewer, ScrollState> States = [];

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element) return;

        if ((bool)e.NewValue)
        {
            element.PreviewMouseWheel += OnPreviewMouseWheel;
        }
        else
        {
            element.PreviewMouseWheel -= OnPreviewMouseWheel;
        }
    }

    private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not DependencyObject d) return;

        var scrollViewer = FindScrollViewer(d);
        if (scrollViewer is null || scrollViewer.ScrollableHeight <= 0)
            return;

        e.Handled = true;

        if (!States.TryGetValue(scrollViewer, out var state))
        {
            state = new ScrollState
            {
                CurrentOffset = scrollViewer.VerticalOffset,
                TargetOffset = scrollViewer.VerticalOffset,
                LastFrameTime = DateTime.UtcNow
            };
            States[scrollViewer] = state;
            scrollViewer.Unloaded += (_, _) => States.Remove(scrollViewer);
        }

        // Se não estava animando, sincroniza com a posição real atual (caso o usuário tenha rolado pela barra ou teclado)
        if (!state.IsAnimating)
        {
            state.CurrentOffset = scrollViewer.VerticalOffset;
            state.TargetOffset = scrollViewer.VerticalOffset;
        }

        // Passo suave proporcional ao giro da roda do mouse (~72px por clique da roda)
        double step = -(e.Delta / 120.0) * 76.0;
        state.TargetOffset = Math.Clamp(state.TargetOffset + step, 0.0, scrollViewer.ScrollableHeight);

        if (!state.IsAnimating)
        {
            state.IsAnimating = true;
            state.LastFrameTime = DateTime.UtcNow;
            CompositionTarget.Rendering += OnRendering;
        }
    }

    private static void OnRendering(object? sender, EventArgs e)
    {
        if (States.Count == 0)
        {
            CompositionTarget.Rendering -= OnRendering;
            return;
        }

        var now = DateTime.UtcNow;
        bool anyActive = false;

        foreach (var (sv, state) in States)
        {
            if (!state.IsAnimating) continue;

            double dt = Math.Clamp((now - state.LastFrameTime).TotalSeconds, 0.001, 0.05);
            state.LastFrameTime = now;

            // Garantir que o alvo respeite o tamanho atual do conteúdo
            state.TargetOffset = Math.Clamp(state.TargetOffset, 0.0, sv.ScrollableHeight);

            double diff = state.TargetOffset - state.CurrentOffset;
            if (Math.Abs(diff) < 0.35)
            {
                state.CurrentOffset = state.TargetOffset;
                sv.ScrollToVerticalOffset(state.CurrentOffset);
                state.IsAnimating = false;
            }
            else
            {
                // Interpolação exponencial independente de FPS (suave em 60Hz, 120Hz ou 144Hz)
                double factor = 1.0 - Math.Pow(0.00015, dt);
                state.CurrentOffset += diff * factor;
                sv.ScrollToVerticalOffset(state.CurrentOffset);
                anyActive = true;
            }
        }

        if (!anyActive)
        {
            CompositionTarget.Rendering -= OnRendering;
        }
    }

    private static ScrollViewer? FindScrollViewer(DependencyObject root)
    {
        if (root is ScrollViewer sv) return sv;

        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            var found = FindScrollViewer(child);
            if (found is not null) return found;
        }
        return null;
    }
}
