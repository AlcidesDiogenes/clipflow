using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ClipFlow.App.Platform;

/// <summary>
/// Monitora mudanças de foco de janelas no Windows via SetWinEventHook
/// para lembrar exatamente qual aplicativo estava ativo antes de abrir o ClipFlow.
/// </summary>
public sealed class ForegroundTracker : IDisposable
{
    private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
    private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
    private const uint WINEVENT_SKIPOWNPROCESS = 0x0002;

    private readonly WinEventDelegate _callback;
    private readonly nint _hook;

    public nint LastTargetWindow { get; private set; }

    public ForegroundTracker()
    {
        _callback = OnForegroundChanged;
        _hook = SetWinEventHook(
            EVENT_SYSTEM_FOREGROUND,
            EVENT_SYSTEM_FOREGROUND,
            0,
            _callback,
            0,
            0,
            WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);

        RememberWindow(NativeMethods.GetForegroundWindow());
    }

    private void OnForegroundChanged(nint hWinEventHook, uint eventType, nint hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        RememberWindow(hwnd);
    }

    public void RememberWindow(nint hwnd)
    {
        if (hwnd == 0 || !NativeMethods.IsWindow(hwnd)) return;

        NativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
        if (processId == Environment.ProcessId) return;

        var classNameBuilder = new StringBuilder(256);
        NativeMethods.GetClassName(hwnd, classNameBuilder, classNameBuilder.Capacity);
        var className = classNameBuilder.ToString();

        // Ignorar janelas do próprio Explorer / Bandeja do sistema / Menus de contexto
        if (className is "Shell_TrayWnd"
            or "Shell_SecondaryTrayWnd"
            or "NotifyIconOverflowWindow"
            or "Progman"
            or "WorkerW"
            or "#32768")
        {
            return;
        }

        LastTargetWindow = hwnd;
    }

    public void Dispose()
    {
        if (_hook != 0)
        {
            UnhookWinEvent(_hook);
        }
    }

    private delegate void WinEventDelegate(nint hWinEventHook, uint eventType, nint hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

    [DllImport("user32.dll")]
    private static extern nint SetWinEventHook(uint eventMin, uint eventMax, nint hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool UnhookWinEvent(nint hWinEventHook);
}
