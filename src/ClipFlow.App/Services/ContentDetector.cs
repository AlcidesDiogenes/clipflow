using System;
using System.IO;
using System.Text.RegularExpressions;
using ClipFlow.App.Models;

namespace ClipFlow.App.Services;

/// <summary>
/// Classifica automaticamente o texto copiado em URL, E-mail, Cor Hexadecimal,
/// Caminho de Arquivo ou Código-Fonte, além de gerar o texto de preview limpo.
/// </summary>
public static partial class ContentDetector
{
    [GeneratedRegex(@"^(https?|ftp|file)://[^\s]+$|^www\.[^\s]+\.[^\s]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex UrlRegex();

    [GeneratedRegex(@"^[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$", RegexOptions.Compiled)]
    private static partial Regex HexColorRegex();

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex WhitespaceRegex();

    public static (ClipSubKind SubKind, string? NormalizedHexColor) ClassifyText(string rawText)
    {
        var trimmed = rawText.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return (ClipSubKind.PlainText, null);

        // Textos de linha única podem ser URL, Email, Cor ou Caminho
        if (!trimmed.Contains('\n') && trimmed.Length <= 400)
        {
            if (HexColorRegex().IsMatch(trimmed))
            {
                return (ClipSubKind.HexColor, NormalizeHex(trimmed));
            }

            if (UrlRegex().IsMatch(trimmed))
            {
                return (ClipSubKind.Url, null);
            }

            if (EmailRegex().IsMatch(trimmed))
            {
                return (ClipSubKind.Email, null);
            }

            if (IsLikelyAbsolutePath(trimmed))
            {
                return (ClipSubKind.FilePath, null);
            }
        }

        if (IsLikelyCode(trimmed))
        {
            return (ClipSubKind.Code, null);
        }

        return (ClipSubKind.PlainText, null);
    }

    public static string BuildPreviewText(string text, int maxLength = 260)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        // Mantém até 3 linhas visíveis com limpeza de espaços repetidos
        var lines = text.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var joined = string.Join(" ↵ ", lines.Length > 3 ? lines[..3] : lines);
        joined = WhitespaceRegex().Replace(joined, " ").Trim();

        return joined.Length <= maxLength
            ? joined
            : string.Concat(joined.AsSpan(0, maxLength), "…");
    }

    private static string NormalizeHex(string hex)
    {
        if (hex.Length == 4) // #RGB -> #RRGGBB
        {
            return $"#{hex[1]}{hex[1]}{hex[2]}{hex[2]}{hex[3]}{hex[3]}".ToUpperInvariant();
        }
        return hex.ToUpperInvariant();
    }

    private static bool IsLikelyAbsolutePath(string value)
    {
        try
        {
            if (value.Length >= 3 && char.IsLetter(value[0]) && value[1] == ':' && (value[2] == '\\' || value[2] == '/'))
            {
                return Path.IsPathRooted(value);
            }
        }
        catch
        {
            // Ignora caracteres inválidos em caminhos
        }
        return false;
    }

    private static bool IsLikelyCode(string text)
    {
        int score = 0;
        if (text.Contains('{') && text.Contains('}')) score++;
        if (text.Contains("=>") || text.Contains("->")) score++;
        if (text.Contains("public ") || text.Contains("private ") || text.Contains("function ") || text.Contains("const ") || text.Contains("import ")) score++;
        if (text.Contains("SELECT ", StringComparison.OrdinalIgnoreCase) && text.Contains("FROM ", StringComparison.OrdinalIgnoreCase)) score += 2;
        if (text.Contains(";\n") || text.Contains(";\r\n")) score++;
        return score >= 2;
    }
}
