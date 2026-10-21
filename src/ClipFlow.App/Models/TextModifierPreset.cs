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
    private string _id = Guid.NewGuid().ToString("N");
    private string _label = string.Empty;
    private string _text = string.Empty;
    private ModifierPosition _position = ModifierPosition.Prefix;
    private ModifierSpacing _spacing = ModifierSpacing.DoubleLine;
    private bool _autoReset = true;
    private bool _isEnabled = true;
    private bool _isSelected;
    private int _shortcutIndex;

    public string Id
    {
        get => _id;
        set
        {
            if (_id == value) return;
            _id = value;
            OnPropertyChanged();
        }
    }

    public string Label
    {
        get => _label;
        set
        {
            if (_label == value) return;
            _label = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TooltipText));
        }
    }

    public string Text
    {
        get => _text;
        set
        {
            if (_text == value) return;
            _text = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TooltipText));
        }
    }

    public ModifierPosition Position
    {
        get => _position;
        set
        {
            if (_position == value) return;
            _position = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PositionDescription));
            OnPropertyChanged(nameof(TooltipText));
        }
    }

    public ModifierSpacing Spacing
    {
        get => _spacing;
        set
        {
            if (_spacing == value) return;
            _spacing = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SpacingDescription));
            OnPropertyChanged(nameof(TooltipText));
        }
    }

    public bool AutoReset
    {
        get => _autoReset;
        set
        {
            if (_autoReset == value) return;
            _autoReset = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TooltipText));
        }
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled == value) return;
            _isEnabled = value;
            OnPropertyChanged();
        }
    }

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
