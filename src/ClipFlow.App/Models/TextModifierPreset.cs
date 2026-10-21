using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ClipFlow.App.Models;

public enum ModifierPosition
{
    Prefix = 0, // No Começo
    Suffix = 1  // No Fim
}

public enum ModifierSpacing
{
    DoubleLine = 0,  // Quebra dupla (\r\n\r\n - novo parágrafo)
    SingleLine = 1,  // Quebra simples (\r\n)
    SingleSpace = 2, // Espaço simples (" ")
    None = 3         // Sem espaço (direto)
}

public sealed class TextModifierPreset : INotifyPropertyChanged
{
    private bool _isSelected;
    private int _shortcutIndex;

    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Label { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public ModifierPosition Position { get; set; } = ModifierPosition.Prefix;
    public ModifierSpacing Spacing { get; set; } = ModifierSpacing.DoubleLine;
    public bool AutoReset { get; set; } = true;
    public bool IsEnabled { get; set; } = true;

    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            OnPropertyChanged();
        }
    }

    [System.Text.Json.Serialization.JsonIgnore]
    public int ShortcutIndex
    {
        get => _shortcutIndex;
        set
        {
            if (_shortcutIndex == value) return;
            _shortcutIndex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShortcutBadge));
            OnPropertyChanged(nameof(TooltipText));
        }
    }

    [System.Text.Json.Serialization.JsonIgnore]
    public string ShortcutBadge => _shortcutIndex is >= 1 and <= 9 ? $"Alt+{_shortcutIndex}" : string.Empty;

    [System.Text.Json.Serialization.JsonIgnore]
    public bool HasShortcut => _shortcutIndex is >= 1 and <= 9;

    [System.Text.Json.Serialization.JsonIgnore]
    public string SpacingDescription => Spacing switch
    {
        ModifierSpacing.DoubleLine => "Quebra dupla (parágrafo)",
        ModifierSpacing.SingleLine => "Quebra simples (nova linha)",
        ModifierSpacing.SingleSpace => "Espaço simples",
        ModifierSpacing.None => "Sem espaço",
        _ => "Quebra dupla"
    };

    [System.Text.Json.Serialization.JsonIgnore]
    public string PositionDescription => Position switch
    {
        ModifierPosition.Prefix => "No começo (Prefixo)",
        ModifierPosition.Suffix => "No fim (Sufixo)",
        _ => "No começo"
    };

    [System.Text.Json.Serialization.JsonIgnore]
    public string TooltipText =>
        $"{Label}\n{PositionDescription}: \"{Text}\"\nEspaçamento: {SpacingDescription}\nAtalho: {ShortcutBadge}\nAuto-reset pós colagem: {(AutoReset ? "Sim" : "Não")}";

    public static string Apply(string originalText, TextModifierPreset? modifier)
    {
        if (modifier is null || string.IsNullOrEmpty(modifier.Text))
        {
            return originalText;
        }

        string sep = modifier.Spacing switch
        {
            ModifierSpacing.DoubleLine => "\r\n\r\n",
            ModifierSpacing.SingleLine => "\r\n",
            ModifierSpacing.SingleSpace => " ",
            ModifierSpacing.None => "",
            _ => "\r\n\r\n"
        };

        return modifier.Position switch
        {
            ModifierPosition.Prefix => modifier.Text + sep + originalText,
            ModifierPosition.Suffix => originalText + sep + modifier.Text,
            _ => originalText
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
