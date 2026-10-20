using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace ClipFlow.App.Services;

/// <summary>
/// Gerencia a inicialização automática do ClipFlow junto com o Windows
/// no registro do usuário atual (HKCU\Software\Microsoft\Windows\CurrentVersion\Run).
/// </summary>
public static class StartupRegistryService
{
    private const string RunRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppRegistryName = "ClipFlow";

    public static bool IsRegistered()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistryPath, writable: false);
            return key?.GetValue(AppRegistryName) is string value && !string.IsNullOrWhiteSpace(value);
        }
        catch
        {
            return false;
        }
    }

    public static void SetStartup(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistryPath, writable: true);
            if (key is null) return;

            if (enable)
            {
                var exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrWhiteSpace(exePath))
                {
                    key.SetValue(AppRegistryName, $"\"{exePath}\" --minimized");
                }
            }
            else
            {
                if (key.GetValue(AppRegistryName) is not null)
                {
                    key.DeleteValue(AppRegistryName, throwOnMissingValue: false);
                }
            }
        }
        catch
        {
            // Ignora caso políticas de grupo bloqueiem edição de HKCU Run
        }
    }
}
