using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using ClipFlow.App.Models;
using ClipFlow.App.Services;

namespace ClipFlow.App.ViewModels;

public enum FilterTab
{
    All = 0,
    Pinned = 1,
    Text = 2,
    Media = 3
}

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly StorageService _storage;
    private readonly ClipboardService _clipboardService;
    private readonly List<ClipItem> _allItems;

    private string _searchText = string.Empty;
    private FilterTab _activeTab = FilterTab.All;
    private ClipItem? _selectedItem;
    private ClipItem? _editingNoteItem;
    private string _editingNoteText = string.Empty;
    private string _statusMessage = "Pronto";

    public ObservableCollection<ClipItem> FilteredItems { get; } = [];
    public event Action? SettingsApplied;

    public MainViewModel(StorageService storage, ClipboardService clipboardService)
    {
        _storage = storage;
        _clipboardService = clipboardService;
        _allItems = _storage.LoadHistory();
        UpdateHotkeyStatusText();
        RefreshFilter();
    }

    public StorageService Storage => _storage;
    public ClipboardService ClipboardService => _clipboardService;
    public AppSettings Settings => _clipboardService.Settings;

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText == value) return;
            _searchText = value;
            OnPropertyChanged();
            RefreshFilter();
        }
    }

    public FilterTab ActiveTab
    {
        get => _activeTab;
        set
        {
            if (_activeTab == value) return;
            _activeTab = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsTabAll));
            OnPropertyChanged(nameof(IsTabPinned));
            OnPropertyChanged(nameof(IsTabText));
            OnPropertyChanged(nameof(IsTabMedia));
            RefreshFilter();
        }
    }

    public bool IsTabAll => _activeTab == FilterTab.All;
    public bool IsTabPinned => _activeTab == FilterTab.Pinned;
    public bool IsTabText => _activeTab == FilterTab.Text;
    public bool IsTabMedia => _activeTab == FilterTab.Media;

    public ClipItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (_selectedItem == value) return;
            _selectedItem = value;
            OnPropertyChanged();
        }
    }

    public ClipItem? EditingNoteItem
    {
        get => _editingNoteItem;
        set
        {
            if (_editingNoteItem == value) return;
            _editingNoteItem = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsEditingNote));
        }
    }

    public bool IsEditingNote => _editingNoteItem is not null;

    public string EditingNoteText
    {
        get => _editingNoteText;
        set
        {
            if (_editingNoteText == value) return;
            _editingNoteText = value;
            OnPropertyChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage == value) return;
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public int TotalCount => _allItems.Count;
    public int PinnedCount => _allItems.Count(x => x.IsPinned);
    public bool IsEmpty => FilteredItems.Count == 0;
    public bool IsCapturePaused => _clipboardService.IsPaused;
    public string PauseButtonLabel => IsCapturePaused ? "Retomar" : "Pausar";
    public bool ShowFooterHints => _clipboardService.Settings.ShowFooterHints;

    public string ShortcutBadgeText => FormatShortcutName(_clipboardService.Settings.Shortcut);

    public static string FormatShortcutName(ShortcutChoice choice) => choice switch
    {
        ShortcutChoice.AltV => "Alt + V",
        ShortcutChoice.CtrlAltV => "Ctrl + Alt + V",
        ShortcutChoice.CtrlShiftC => "Ctrl + Shift + C",
        ShortcutChoice.AltC => "Alt + C",
        _ => "Ctrl + Shift + V"
    };

    public void OnNewItemCaptured(ClipItem incoming)
    {
        var existing = _allItems.FirstOrDefault(x =>
            string.Equals(x.ContentHash, incoming.ContentHash, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            existing.UpdatedAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(incoming.SourceAppName))
            {
                existing.SourceAppName = incoming.SourceAppName;
            }
        }
        else
        {
            _allItems.Insert(0, incoming);
            _storage.EnforceRetentionLimit(_allItems, _clipboardService.Settings.MaxHistoryCount);
        }

        PersistAndRefresh();
    }

    public void TogglePin(ClipItem? item)
    {
        if (item is null) return;
        item.IsPinned = !item.IsPinned;
        item.UpdatedAt = DateTime.UtcNow;
        StatusMessage = item.IsPinned
            ? "Item fixado no topo!"
            : "Item desafixado.";
        PersistAndRefresh(keepSelection: item);
    }

    public void StartEditNote(ClipItem? item)
    {
        if (item is null) return;
        EditingNoteItem = item;
        EditingNoteText = item.TitleOrNote ?? string.Empty;
    }

    public void SaveEditedNote()
    {
        if (_editingNoteItem is null) return;
        var target = _editingNoteItem;
        target.TitleOrNote = string.IsNullOrWhiteSpace(_editingNoteText) ? null : _editingNoteText.Trim();
        if (target.HasNote && !target.IsPinned)
        {
            target.IsPinned = true;
        }
        EditingNoteItem = null;
        EditingNoteText = string.Empty;
        StatusMessage = "Apelido salvo com sucesso!";
        PersistAndRefresh(keepSelection: target);
    }

    public void CancelEditNote()
    {
        EditingNoteItem = null;
        EditingNoteText = string.Empty;
    }

    public void DeleteItem(ClipItem? item)
    {
        if (item is null) return;
        var index = FilteredItems.IndexOf(item);
        _allItems.Remove(item);
        _storage.TryDeleteOrphanImage(item, _allItems);
        StatusMessage = "Item removido.";
        PersistAndRefresh();

        if (FilteredItems.Count > 0)
        {
            SelectedItem = FilteredItems[Math.Clamp(index, 0, FilteredItems.Count - 1)];
        }
    }

    public void ClearUnpinnedHistory()
    {
        var unpinned = _allItems.Where(x => !x.IsPinned).ToList();
        if (unpinned.Count == 0)
        {
            StatusMessage = "Nenhum item não-fixado para limpar.";
            return;
        }

        foreach (var item in unpinned)
        {
            _allItems.Remove(item);
            _storage.TryDeleteOrphanImage(item, _allItems);
        }

        StatusMessage = $"{unpinned.Count} itens limpos (Fixados preservados).";
        PersistAndRefresh();
    }

    public void ClearAllHistoryIncludingPinned()
    {
        var allCopy = _allItems.ToList();
        _allItems.Clear();
        foreach (var item in allCopy)
        {
            _storage.TryDeleteOrphanImage(item, _allItems);
        }

        StatusMessage = "Todo o histórico foi apagado.";
        PersistAndRefresh();
    }

    public void TogglePauseCapture()
    {
        _clipboardService.IsPaused = !_clipboardService.IsPaused;
        OnPropertyChanged(nameof(IsCapturePaused));
        OnPropertyChanged(nameof(PauseButtonLabel));
        StatusMessage = _clipboardService.IsPaused
            ? "Captura pausada temporariamente."
            : "Captura ativa.";
    }

    public void CycleGlobalShortcut()
    {
        var next = _clipboardService.Settings.Shortcut switch
        {
            ShortcutChoice.CtrlShiftV => ShortcutChoice.AltV,
            ShortcutChoice.AltV => ShortcutChoice.CtrlAltV,
            ShortcutChoice.CtrlAltV => ShortcutChoice.CtrlShiftC,
            ShortcutChoice.CtrlShiftC => ShortcutChoice.AltC,
            _ => ShortcutChoice.CtrlShiftV
        };

        ApplySettingsChanges(newShortcut: next);
    }

    public void ApplySettingsChanges(ShortcutChoice? newShortcut = null)
    {
        if (newShortcut.HasValue)
        {
            _clipboardService.RegisterConfiguredHotkey(newShortcut.Value);
        }
        else
        {
            _clipboardService.RegisterConfiguredHotkey(_clipboardService.Settings.Shortcut);
        }

        StartupRegistryService.SetStartup(_clipboardService.Settings.StartWithWindows);
        ThemeService.ApplyTheme(_clipboardService.Settings.Theme);
        _storage.EnforceRetentionLimit(_allItems, _clipboardService.Settings.MaxHistoryCount);
        _storage.SaveSettings(_clipboardService.Settings);
        _storage.SaveHistory(_allItems);

        UpdateHotkeyStatusText();
        OnPropertyChanged(nameof(ShortcutBadgeText));
        OnPropertyChanged(nameof(ShowFooterHints));
        RefreshFilter(SelectedItem);
        SettingsApplied?.Invoke();
    }

    public void ResetDefaultSettings()
    {
        var defaults = new AppSettings();
        _clipboardService.Settings = defaults;
        ApplySettingsChanges(defaults.Shortcut);
        StatusMessage = "Configurações padrão restauradas.";
    }

    public void NotifyItemPasted(ClipItem item)
    {
        item.UseCount++;
        if (_clipboardService.Settings.MoveToTopOnPaste)
        {
            item.UpdatedAt = DateTime.UtcNow;
        }
        PersistAndRefresh(keepSelection: item);
    }

    public void CycleTab(bool reverse = false)
    {
        int next = ((int)ActiveTab + (reverse ? 3 : 1)) % 4;
        ActiveTab = (FilterTab)next;
    }

    public void PrepareForPopupOpen()
    {
        foreach (var item in _allItems)
        {
            item.RefreshTimeBinding();
        }
        RefreshFilter();
    }

    public void RefreshFilter(ClipItem? keepSelection = null)
    {
        var query = _searchText.Trim();

        IEnumerable<ClipItem> queryable = StorageService.SortItems(_allItems);

        queryable = _activeTab switch
        {
            FilterTab.Pinned => queryable.Where(x => x.IsPinned),
            FilterTab.Text => _clipboardService.Settings.ShowPinnedInTextTab
                ? queryable.Where(x => x.Kind == ClipKind.Text)
                : queryable.Where(x => x.Kind == ClipKind.Text && !x.IsPinned),
            FilterTab.Media => queryable.Where(x => x.Kind is ClipKind.Image or ClipKind.Files),
            _ => queryable
        };

        if (!string.IsNullOrEmpty(query))
        {
            queryable = queryable.Where(x =>
                (x.TextContent?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.PreviewText?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.TitleOrNote?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.SourceAppName?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                x.BadgeLabel.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        var resultList = queryable.ToList();

        FilteredItems.Clear();
        for (int i = 0; i < resultList.Count; i++)
        {
            var item = resultList[i];
            item.QuickIndex = i < 9 ? i + 1 : 0;
            FilteredItems.Add(item);
        }

        SelectedItem = keepSelection is not null && FilteredItems.Contains(keepSelection)
            ? keepSelection
            : FilteredItems.FirstOrDefault();

        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(PinnedCount));
        OnPropertyChanged(nameof(IsEmpty));
    }

    private void PersistAndRefresh(ClipItem? keepSelection = null)
    {
        var sorted = StorageService.SortItems(_allItems);
        _allItems.Clear();
        _allItems.AddRange(sorted);
        _storage.SaveHistory(_allItems);
        RefreshFilter(keepSelection);
    }

    private void UpdateHotkeyStatusText()
    {
        StatusMessage = _clipboardService.HotkeyRegistered
            ? $"Atalho Global: {ShortcutBadgeText}"
            : $"Atalho {ShortcutBadgeText} indisponível";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
