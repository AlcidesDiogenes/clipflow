using System;
using System.Windows.Forms;

namespace ClipFlow.App.Platform;

/// <summary>
/// Janela invisível (HWND_MESSAGE = -3) usada exclusivamente para receber
/// eventos globais do Windows (WM_CLIPBOARDUPDATE e WM_HOTKEY) sem aparecer
/// na barra de tarefas nem no Alt+Tab.
/// </summary>
internal sealed class MessageWindow : NativeWindow, IDisposable
{
    private readonly Func<int, nint, nint, bool> _messageHandler;

    public MessageWindow(Func<int, nint, nint, bool> messageHandler)
    {
        _messageHandler = messageHandler;
        CreateHandle(new CreateParams
        {
            Caption = "ClipFlow_MessageListener",
            Parent = new nint(-3) // HWND_MESSAGE
        });
    }

    protected override void WndProc(ref Message m)
    {
        if (_messageHandler(m.Msg, m.WParam, m.LParam))
        {
            m.Result = 0;
            return;
        }

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        if (Handle != 0)
        {
            DestroyHandle();
        }
    }
}
