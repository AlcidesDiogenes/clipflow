using System;
using System.Windows.Forms;
using System.Windows.Threading;
using ClipFlow.App.Models;
using ClipFlow.App.Platform;

namespace ClipFlow.App.Services;

/// <summary>
/// Monitora a posição do cursor do mouse para acionar automaticamente o ClipFlow
/// quando o usuário encosta o mouse na borda esquerda, direita ou ambas as bordas da tela.
/// </summary>
public sealed class EdgeTriggerService : IDisposable
{
    private const int EdgePixelThreshold = 2;
    private const int CornerSafetyMarginPx = 48; // Evita conflito com botão Fechar (X) no topo ou Menu Iniciar na base
    private const int RearmDistancePx = 36;

    private readonly DispatcherTimer _pollTimer;
    private DateTime? _edgeEnteredAt;
    private bool _triggeredWhileOnEdge;
    private bool _disposed;

    public AppSettings Settings { get; set; }

    /// <summary>
    /// Disparado quando o mouse permanece na borda configurada pelo tempo necessário.
    /// Parâmetro: true se foi na borda esquerda, false se foi na borda direita.
    /// </summary>
    public event Action<bool>? EdgeTriggered;

    public EdgeTriggerService(AppSettings settings)
    {
        Settings = settings;
        _pollTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(45)
        };
        _pollTimer.Tick += OnPollTimerTick;
        _pollTimer.Start();
    }

    private void OnPollTimerTick(object? sender, EventArgs e)
    {
        if (_disposed || Settings.EdgeTrigger == EdgeTriggerMode.Disabled)
        {
            _edgeEnteredAt = null;
            _triggeredWhileOnEdge = false;
            return;
        }

        if (!NativeMethods.GetCursorPos(out var pt))
            return;

        var screen = Screen.FromPoint(new System.Drawing.Point(pt.X, pt.Y));
        var bounds = screen.Bounds;

        // Ignorar os cantos extremos superior e inferior (botão fechar janela / barra de tarefas)
        if (pt.Y <= bounds.Top + CornerSafetyMarginPx || pt.Y >= bounds.Bottom - CornerSafetyMarginPx)
        {
            _edgeEnteredAt = null;
            return;
        }

        bool onLeftEdge = pt.X <= bounds.Left + EdgePixelThreshold;
        bool onRightEdge = pt.X >= bounds.Right - EdgePixelThreshold - 1;

        bool matchesMode = Settings.EdgeTrigger switch
        {
            EdgeTriggerMode.LeftOnly => onLeftEdge,
            EdgeTriggerMode.RightOnly => onRightEdge,
            EdgeTriggerMode.BothEdges => onLeftEdge || onRightEdge,
            _ => false
        };

        if (!matchesMode)
        {
            // Rearma o gatilho assim que o mouse se afasta um pouco da borda
            if (pt.X > bounds.Left + RearmDistancePx && pt.X < bounds.Right - RearmDistancePx)
            {
                _triggeredWhileOnEdge = false;
            }
            _edgeEnteredAt = null;
            return;
        }

        if (_triggeredWhileOnEdge)
            return;

        if (_edgeEnteredAt is null)
        {
            _edgeEnteredAt = DateTime.UtcNow;
            return;
        }

        double elapsedMs = (DateTime.UtcNow - _edgeEnteredAt.Value).TotalMilliseconds;
        int requiredDelay = Math.Clamp(Settings.EdgeTriggerDelayMs, 50, 1500);

        if (elapsedMs >= requiredDelay)
        {
            _triggeredWhileOnEdge = true;
            _edgeEnteredAt = null;
            EdgeTriggered?.Invoke(onLeftEdge);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _pollTimer.Stop();
        _pollTimer.Tick -= OnPollTimerTick;
    }
}
