using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ClipFlow.App.Models;
using ClipFlow.App.Platform;

namespace ClipFlow.App.Services;

/// <summary>
/// Protege a privacidade do usuário verificando flags nativas de cofres de senha
/// (Bitwarden, 1Password, KeePass), aplicativos excluídos e tokens secretos.
/// </summary>
public static partial class PrivacyGuard
{
    [GeneratedRegex(@"(-----BEGIN (?:[A-Z0-9 ]+ )?PRIVATE KEY-----|gh[pousr]_[A-Za-z0-9_]{36,}|github_pat_[A-Za-z0-9_]{40,}|sk-[A-Za-z0-9_\-]{32,}|AKIA[A-Z0-9]{16})", RegexOptions.Compiled)]
    private static partial Regex HighConfidenceSecretRegex();

    public static bool ShouldIgnoreDataObject(System.Windows.IDataObject data, AppSettings settings)
    {
        if (!settings.IgnorePasswordManagers) return false;

        try
        {
            // Padrão oficial da Microsoft para aplicativos sinalizarem conteúdo sensível
            if (data.GetDataPresent("ExcludeClipboardContentFromMonitorProcessing", false))
                return true;

            if (data.GetDataPresent("CanIncludeInClipboardHistory", false))
            {
                if (data.GetData("CanIncludeInClipboardHistory", false) is MemoryStream stream && stream.Length >= 4)
                {
                    stream.Position = 0;
                    using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
                    if (reader.ReadUInt32() == 0)
                        return true;
                }
            }
        }
        catch
        {
            // Se falhar ao ler metadados de exclusão, segue fluxo normal
        }

        return false;
    }

    public static (bool Allowed, string? SourceProcessName) CheckClipboardSourceApp(AppSettings settings)
    {
        var ownerHwnd = NativeMethods.GetClipboardOwner();
        if (ownerHwnd == 0)
        {
            ownerHwnd = NativeMethods.GetForegroundWindow();
        }

        if (ownerHwnd == 0)
            return (true, null);

        NativeMethods.GetWindowThreadProcessId(ownerHwnd, out var pid);
        if (pid == 0)
            return (true, null);

        try
        {
            using var proc = Process.GetProcessById((int)pid);
            var procName = proc.ProcessName;

            if (settings.IgnorePasswordManagers && settings.ExcludedApps.Length > 0)
            {
                var isExcluded = settings.ExcludedApps.Any(excluded =>
                    procName.Contains(excluded, StringComparison.OrdinalIgnoreCase));

                if (isExcluded)
                    return (false, procName);
            }

            return (true, procName);
        }
        catch
        {
            return (true, null);
        }
    }

    public static bool ContainsHighConfidenceSecret(string text, AppSettings settings)
    {
        if (!settings.IgnoreSecretTokens || string.IsNullOrWhiteSpace(text))
            return false;

        return HighConfidenceSecretRegex().IsMatch(text);
    }
}
