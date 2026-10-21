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
        // 1. Aguarda que o usuário solte fisicamente as teclas Ctrl / Alt / Shift (até 220ms)
        // Isso impede que atalhos como Ctrl+1 vazem para aplicativos com abas (Chrome, VS Code, etc.)
        await WaitForModifierKeysReleasedAsync(maxWaitMs: 220);

        // 2. Limpa o estado virtual das teclas modificadoras no Windows
        ReleaseAllModifierKeys();

        // 3. Restaura o foco na janela de destino
        if (targetWindow != 0 && NativeMethods.IsWindow(targetWindow))
        {
            NativeMethods.SetForegroundWindow(targetWindow);
            await Task.Delay(Math.Clamp(delayMs, 25, 500));
        }

        // 4. Injeta Ctrl+V limpo
        SendCtrlV();
    }

    private static async Task WaitForModifierKeysReleasedAsync(int maxWaitMs = 220)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(maxWaitMs);
        while (DateTime.UtcNow < deadline)
        {
            bool anyKeyPressed =
                (NativeMethods.GetAsyncKeyState(NativeMethods.VK_CONTROL) & 0x8000) != 0 ||
                (NativeMethods.GetAsyncKeyState(NativeMethods.VK_MENU) & 0x8000) != 0 ||
                (NativeMethods.GetAsyncKeyState(NativeMethods.VK_SHIFT) & 0x8000) != 0;

            if (!anyKeyPressed)
            {
                break;
            }

            await Task.Delay(15);
        }
    }

    private static void ReleaseAllModifierKeys()
    {
        static NativeMethods.Input KeyUp(ushort vk) => new()
        {
            Type = 1,
            Data = new NativeMethods.InputUnion
            {
                Keyboard = new NativeMethods.KeyboardInput
                {
                    Key = vk,
                    Flags = NativeMethods.KEYEVENTF_KEYUP
                }
            }
        };

        NativeMethods.Input[] releaseInputs =
        [
            KeyUp(NativeMethods.VK_CONTROL),
            KeyUp(NativeMethods.VK_MENU),
            KeyUp(NativeMethods.VK_SHIFT)
        ];

        NativeMethods.SendInput((uint)releaseInputs.Length, releaseInputs, Marshal.SizeOf<NativeMethods.Input>());
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

        // Garante que Ctrl e Alt estejam soltos antes de iniciar a sequência de Ctrl+V
        NativeMethods.Input[] inputs =
        [
            Key(NativeMethods.VK_MENU, NativeMethods.KEYEVENTF_KEYUP),
            Key(NativeMethods.VK_SHIFT, NativeMethods.KEYEVENTF_KEYUP),
            Key(NativeMethods.VK_CONTROL, NativeMethods.KEYEVENTF_KEYUP),
            Key(NativeMethods.VK_CONTROL, 0),
            Key(NativeMethods.VK_V, 0),
            Key(NativeMethods.VK_V, NativeMethods.KEYEVENTF_KEYUP),
            Key(NativeMethods.VK_CONTROL, NativeMethods.KEYEVENTF_KEYUP)
        ];

        var sent = NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeMethods.Input>());
        return sent == inputs.Length;
    }
}
