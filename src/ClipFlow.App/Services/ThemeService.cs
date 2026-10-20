using System.Windows;
using System.Windows.Media;
using ClipFlow.App.Models;

namespace ClipFlow.App.Services;

/// <summary>
/// Aplica dinamicamente o Tema Escuro (Dark) ou Tema Claro (Light) em toda a aplicação
/// atualizando os SolidColorBrushes no ResourceDictionary global.
/// </summary>
public static class ThemeService
{
    public static void ApplyTheme(AppTheme theme)
    {
        var res = System.Windows.Application.Current?.Resources;
        if (res is null) return;

        if (theme == AppTheme.Light)
        {
            SetBrush(res, "WindowBgBrush", "#F8FAFC");
            SetBrush(res, "WindowBorderBrush", "#CBD5E1");
            SetBrush(res, "HeaderFooterBgBrush", "#F1F5F9");
            SetBrush(res, "SidebarBgBrush", "#E2E8F0");
            SetBrush(res, "DividerBorderBrush", "#CBD5E1");
            SetBrush(res, "InputBgBrush", "#FFFFFF");
            SetBrush(res, "InputBorderBrush", "#CBD5E1");
            SetBrush(res, "CardBgBrush", "#FFFFFF");
            SetBrush(res, "CardBorderBrush", "#E2E8F0");
            SetBrush(res, "CardHoverBgBrush", "#F1F5F9");
            SetBrush(res, "CardHoverBorderBrush", "#94A3B8");
            SetBrush(res, "CardSelectedBgBrush", "#DBEAFE");
            SetBrush(res, "CardSelectedBorderBrush", "#2563EB");
            SetBrush(res, "CardPinnedBgBrush", "#EFF6FF");
            SetBrush(res, "CardPinnedBorderBrush", "#60A5FA");
            SetBrush(res, "ButtonBgBrush", "#E2E8F0");
            SetBrush(res, "ButtonBorderBrush", "#CBD5E1");
            SetBrush(res, "ButtonHoverBgBrush", "#CBD5E1");
            SetBrush(res, "PrimaryTextBrush", "#0F172A");
            SetBrush(res, "SecondaryTextBrush", "#475569");
            SetBrush(res, "AccentTextBrush", "#2563EB");
            SetBrush(res, "ShortcutBadgeBgBrush", "#DBEAFE");
            SetBrush(res, "NoteBarBgBrush", "#DBEAFE");
        }
        else
        {
            SetBrush(res, "WindowBgBrush", "#18181B");
            SetBrush(res, "WindowBorderBrush", "#33333D");
            SetBrush(res, "HeaderFooterBgBrush", "#1E1E24");
            SetBrush(res, "SidebarBgBrush", "#1B1B22");
            SetBrush(res, "DividerBorderBrush", "#2A2A34");
            SetBrush(res, "InputBgBrush", "#22222A");
            SetBrush(res, "InputBorderBrush", "#343440");
            SetBrush(res, "CardBgBrush", "#222229");
            SetBrush(res, "CardBorderBrush", "#30303A");
            SetBrush(res, "CardHoverBgBrush", "#292B36");
            SetBrush(res, "CardHoverBorderBrush", "#525260");
            SetBrush(res, "CardSelectedBgBrush", "#232E48");
            SetBrush(res, "CardSelectedBorderBrush", "#3B82F6");
            SetBrush(res, "CardPinnedBgBrush", "#1C2436");
            SetBrush(res, "CardPinnedBorderBrush", "#334E7B");
            SetBrush(res, "ButtonBgBrush", "#27272A");
            SetBrush(res, "ButtonBorderBrush", "#3F3F46");
            SetBrush(res, "ButtonHoverBgBrush", "#3F3F46");
            SetBrush(res, "PrimaryTextBrush", "#F8FAFC");
            SetBrush(res, "SecondaryTextBrush", "#9CA3AF");
            SetBrush(res, "AccentTextBrush", "#60A5FA");
            SetBrush(res, "ShortcutBadgeBgBrush", "#262F45");
            SetBrush(res, "NoteBarBgBrush", "#1E293B");
        }
    }

    private static void SetBrush(ResourceDictionary resources, string key, string hexColor)
    {
        var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hexColor);
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        resources[key] = brush;
    }
}
