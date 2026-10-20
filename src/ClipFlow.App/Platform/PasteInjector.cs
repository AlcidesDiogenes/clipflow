using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ClipFlow.App.Platform;

/// <summary>
/// Restaura o foco na janela anterior do usuário e injeta o comando Ctrl+V
/// via Win32 SendInput (liberando antes teclas modificadoras eventualmente pressionadas).
/// </summary>
public static class PasteInjector
{
    public static async Task RestoreFocusAndPasteAsync(nint targetWindow, int delayMs = 35)
    {
        if (targetWindow != 0 && NativeMethods.IsWindow(targetWindow))
        {
            NativeMethods.SetForegroundWindow(targetWindow);
            await Task.Delay(Math.Clamp(delayMs, 10, 500));
        }

        SendCtrlV();
    }

    public static bool SendCtrlV()
    {
        static NativeMethods.Input Key(ushort virtualKey, uint flags) => new()
        {
            Type = 1, // INPUT_KEYBOARD
            Data = new NativeMethods.InputUnion
            {
                Keyboard = new NativeMethods.KeyboardInput
                {
                    Key = virtualKey,
                    Flags = flags
                }
            }
        };

        // Libera Alt/Shift caso o usuário tenha acabado de usar um atalho como Alt+1 ou Shift+Enter
        NativeMethods.Input[] inputs =
        [
            Key(NativeMethods.VK_MENU, NativeMethods.KEYEVENTF_KEYUP),
            Key(NativeMethods.VK_SHIFT, NativeMethods.KEYEVENTF_KEYUP),
            Key(NativeMethods.VK_CONTROL, 0),
            Key(NativeMethods.VK_V, 0),
            Key(NativeMethods.VK_V, NativeMethods.KEYEVENTF_KEYUP),
            Key(NativeMethods.VK_CONTROL, NativeMethods.KEYEVENTF_KEYUP)
        ];

        var sent = NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeMethods.Input>());
        return sent == inputs.Length;
    }
}
