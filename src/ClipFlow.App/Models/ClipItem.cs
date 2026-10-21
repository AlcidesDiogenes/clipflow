using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace ClipFlow.App.Models;

public sealed class ClipItem : INotifyPropertyChanged
{
    private bool _isPinned;
    private string? _titleOrNote;
    private int _useCount = 1;
    private DateTime _updatedAt = DateTime.UtcNow;
    private int _quickIndex;

    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public ClipKind Kind { get; set; } = ClipKind.Text;
    public ClipSubKind SubKind { get; set; } = ClipSubKind.PlainText;
    public string? TextContent { get; set; }
    public string PreviewText { get; set; } = string.Empty;
    public string? HtmlContent { get; set; }
    public string? RtfContent { get; set; }
    public string? ImagePath { get; set; }
    public string[]? FilePaths { get; set; }
    public string ContentHash { get; set; } = string.Empty;
    public string? ColorHex { get; set; }
    public string? SourceAppName { get; set; }
    public int CharacterCount { get; set; }
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsPinned
    {
        get => _isPinned;
        set
        {
            if (_isPinned == value) return;
            _isPinned = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PinIcon));
            OnPropertyChanged(nameof(PinButtonText));
            OnPropertyChanged(nameof(PinTooltip));
        }
    }

    public int PinOrder { get; set; }

    public string? TitleOrNote
    {
        get => _titleOrNote;
        set
        {
            if (_titleOrNote == value) return;
            _titleOrNote = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasNote));
        }
    }

    public int UseCount
    {
        get => _useCount;
        set
        {
            if (_useCount == value) return;
            _useCount = value;
            OnPropertyChanged();
        }
    }

    public DateTime UpdatedAt
    {
        get => _updatedAt;
        set
        {
            if (_updatedAt == value) return;
            _updatedAt = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(RelativeTime));
        }
    }

    [JsonIgnore]
    public int QuickIndex
    {
        get => _quickIndex;
        set
        {
            if (_quickIndex == value) return;
            _quickIndex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(QuickIndexLabel));
            OnPropertyChanged(nameof(HasQuickIndex));
        }
    }

    [JsonIgnore]
    public bool HasQuickIndex => _quickIndex is >= 1 and <= 9;

    [JsonIgnore]
    public string QuickIndexLabel => HasQuickIndex ? $"Ctrl+{_quickIndex}" : string.Empty;

    [JsonIgnore]
    public bool HasNote => !string.IsNullOrWhiteSpace(_titleOrNote);

    [JsonIgnore]
    public bool IsColor => SubKind == ClipSubKind.HexColor && !string.IsNullOrWhiteSpace(ColorHex);

    [JsonIgnore]
    public bool IsImage => Kind == ClipKind.Image && !string.IsNullOrWhiteSpace(ImagePath);

    [JsonIgnore]
    public string BadgeIcon => Kind switch
    {
        ClipKind.Image => "🖼️",
        ClipKind.Files => "📁",
        _ => SubKind switch
        {
            ClipSubKind.Url => "🔗",
            ClipSubKind.Email => "✉️",
            ClipSubKind.HexColor => "🎨",
            ClipSubKind.FilePath => "📂",
            ClipSubKind.Code => "</>",
            _ => "📝"
        }
    };

    [JsonIgnore]
    public string BadgeLabel => Kind switch
    {
        ClipKind.Image => $"Imagem ({ImageWidth}×{ImageHeight})",
        ClipKind.Files => $"Arquivos ({FilePaths?.Length ?? 0})",
        _ => SubKind switch
        {
            ClipSubKind.Url => "Link",
            ClipSubKind.Email => "E-mail",
            ClipSubKind.HexColor => "Cor",
            ClipSubKind.FilePath => "Caminho",
            ClipSubKind.Code => "Código",
            _ => $"{CharacterCount} chars"
        }
    };

    [JsonIgnore]
    public string PinIcon => IsPinned ? "📌" : "📍";

    [JsonIgnore]
    public string PinButtonText => IsPinned ? "Desafixar" : "Fixar";

    [JsonIgnore]
    public string PauseShortLabel => "Pausar";

    [JsonIgnore]
    public string PinTooltip => IsPinned ? "Desafixar item (Ctrl+P)" : "Fixar no topo para uso rápido (Ctrl+P)";

    [JsonIgnore]
    public string RelativeTime
    {
        get
        {
            var span = DateTime.UtcNow - UpdatedAt;
            if (span.TotalSeconds < 45) return "Agora";
            if (span.TotalMinutes < 60) return $"Há {(int)span.TotalMinutes} min";
            if (span.TotalHours < 24) return $"Há {(int)span.TotalHours} h";
            if (span.TotalDays < 7) return $"Há {(int)span.TotalDays} d";
            return UpdatedAt.ToLocalTime().ToString("dd/MM HH:mm");
        }
    }

    [JsonIgnore]
    public string MetaSummary
    {
        get
        {
            var app = string.IsNullOrWhiteSpace(SourceAppName) ? "Sistema" : SourceAppName;
            return $"{RelativeTime}  •  {app}  •  {BadgeLabel}";
        }
    }

    public void RefreshTimeBinding()
    {
        OnPropertyChanged(nameof(RelativeTime));
        OnPropertyChanged(nameof(MetaSummary));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
