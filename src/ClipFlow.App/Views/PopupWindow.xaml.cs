using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using ClipFlow.App.Models;
using ClipFlow.App.Platform;
using ClipFlow.App.Services;
using ClipFlow.App.ViewModels;

namespace ClipFlow.App.Views;

public partial class PopupWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly ClipboardService _clipboardService;
    private readonly ForegroundTracker _foregroundTracker;
    private readonly DispatcherTimer _outsideClickTimer;

    private SettingsWindow? _openSettingsWindow;
    private bool _isPasting;
    private bool _wasMousePressed;
    private DateTime _openedAtUtc;

    public PopupWindow(
        MainViewModel viewModel,
        ClipboardService clipboardService,
        ForegroundTracker foregroundTracker)
    {
        InitializeComponent();
        Icon = AppIconBuilder.CreateWpfWindowIcon();
        _viewModel = viewModel;
        _clipboardService = clipboardService;
        _foregroundTracker = foregroundTracker;
        DataContext = _viewModel;

        _outsideClickTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(30)
        };
        _outsideClickTimer.Tick += OnOutsideClickTimerTick;

        _viewModel.SettingsApplied += ApplyVisualSettings;
        ApplyVisualSettings();
    }

    public void ApplyVisualSettings()
    {
        var s = _clipboardService.Settings;
        Width = Math.Clamp(s.WindowWidth, 360, 800);
        Height = Math.Clamp(s.WindowHeight, 420, 900);
        Opacity = Math.Clamp(s.WindowOpacity, 0.5, 1.0);
        Topmost = s.AlwaysOnTop;
    }

    public void ShowAndFocusNearCursor()
    {
        ApplyVisualSettings();
        _viewModel.PrepareForPopupOpen();
        _ = new WindowInteropHelper(this).EnsureHandle();
        PositionWindowSmartly();
        ShowAndActivateReliably();
        PositionWindowSmartly();
    }

    public void ShowDockedAtScreenEdge(bool isLeftEdge)
    {
        if (IsVisible) return;

        ApplyVisualSettings();
        _viewModel.PrepareForPopupOpen();
        _ = new WindowInteropHelper(this).EnsureHandle();
        PositionAtEdgeInternal(isLeftEdge);
        ShowAndActivateReliably();
        PositionAtEdgeInternal(isLeftEdge);
    }

    private void PositionAtEdgeInternal(bool isLeftEdge)
    {
        var hwnd = new WindowInteropHelper(this).EnsureHandle();
        if (!NativeMethods.GetCursorPos(out var pt))
        {
            PositionWindowSmartly();
            return;
        }

        var screen = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point(pt.X, pt.Y));
        var workArea = screen.WorkingArea;
        var (winW, winH) = GetPhysicalWindowSize(hwnd);

        int minX = workArea.Left;
        int maxX = Math.Max(minX, workArea.Right - winW);
        int minY = workArea.Top + 8;
        int maxY = Math.Max(minY, workArea.Bottom - winH - 8);

        int targetX = isLeftEdge ? minX : maxX;
        int targetY = Math.Clamp(pt.Y - (winH / 2), minY, maxY);

        MoveWindowPhysical(hwnd, targetX, targetY);
    }

    private (int WidthPx, int HeightPx) GetPhysicalWindowSize(nint hwnd)
    {
        if (hwnd != 0 && NativeMethods.GetWindowRect(hwnd, out var rect))
        {
            int w = rect.Right - rect.Left;
            int h = rect.Bottom - rect.Top;
            if (w > 100 && h > 100)
            {
                return (w, h);
            }
        }

        var dpi = VisualTreeHelper.GetDpi(this);
        double scaleX = dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0;
        double scaleY = dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0;
        return ((int)Math.Round(Width * scaleX), (int)Math.Round(Height * scaleY));
    }

    private void MoveWindowPhysical(nint hwnd, int xPx, int yPx)
    {
        if (hwnd != 0)
        {
            NativeMethods.SetWindowPos(
                hwnd,
                0,
                xPx,
                yPx,
                0,
                0,
                NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE);
        }
    }

    private void ShowAndActivateReliably()
    {
        _openedAtUtc = DateTime.UtcNow;
        _wasMousePressed = IsAnyMouseButtonDown();

        Show();
        ForceForegroundFocus();
        Activate();
        Focus();
        SearchBox.Focus();
        SearchBox.SelectAll();

        _outsideClickTimer.Start();
    }

    public void HidePopup()
    {
        _outsideClickTimer.Stop();
        Hide();
    }

    private void ForceForegroundFocus()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == 0) return;

        var fgHwnd = NativeMethods.GetForegroundWindow();
        if (fgHwnd == hwnd) return;

        uint fgThread = NativeMethods.GetWindowThreadProcessId(fgHwnd, out _);
        uint currentThread = NativeMethods.GetCurrentThreadId();

        try
        {
            if (fgThread != 0 && fgThread != currentThread)
            {
                NativeMethods.AttachThreadInput(currentThread, fgThread, true);
            }

            // Simula acionamento neutro da tecla Alt para desarmar o Lock Timeout do Windows
            NativeMethods.keybd_event((byte)NativeMethods.VK_MENU, 0, 0, 0);
            NativeMethods.SetForegroundWindow(hwnd);
            NativeMethods.BringWindowToTop(hwnd);
            NativeMethods.keybd_event((byte)NativeMethods.VK_MENU, 0, NativeMethods.KEYEVENTF_KEYUP, 0);

            if (fgThread != 0 && fgThread != currentThread)
            {
                NativeMethods.AttachThreadInput(currentThread, fgThread, false);
            }
        }
        catch
        {
            NativeMethods.SetForegroundWindow(hwnd);
            NativeMethods.BringWindowToTop(hwnd);
        }
    }

    private void OnOutsideClickTimerTick(object? sender, EventArgs e)
    {
        if (!IsVisible)
        {
            _outsideClickTimer.Stop();
            return;
        }

        if (_isPasting || _openSettingsWindow is not null || !_clipboardService.Settings.CloseOnBlur)
        {
            return;
        }

        // Janela de proteção contra cliques residuais logo após abrir pela borda
        if ((DateTime.UtcNow - _openedAtUtc).TotalMilliseconds < 450)
        {
            _wasMousePressed = IsAnyMouseButtonDown();
            return;
        }

        bool isDown = IsAnyMouseButtonDown();
        bool justClicked = isDown && !_wasMousePressed;
        _wasMousePressed = isDown;

        if (!justClicked) return;

        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == 0) return;

        if (NativeMethods.GetCursorPos(out var pt) && NativeMethods.GetWindowRect(hwnd, out var rect))
        {
            // Margem de tolerância para a borda transparente e sombras
            bool insideWindow = pt.X >= (rect.Left - 6) && pt.X <= (rect.Right + 6) &&
                                pt.Y >= (rect.Top - 6) && pt.Y <= (rect.Bottom + 6);

            if (!insideWindow)
            {
                HidePopup();
            }
        }
    }

    private static bool IsAnyMouseButtonDown()
    {
        return (NativeMethods.GetAsyncKeyState(NativeMethods.VK_LBUTTON) & 0x8000) != 0 ||
               (NativeMethods.GetAsyncKeyState(NativeMethods.VK_RBUTTON) & 0x8000) != 0 ||
               (NativeMethods.GetAsyncKeyState(NativeMethods.VK_MBUTTON) & 0x8000) != 0;
    }

    public void OpenSettingsDialog(int initialSectionIndex = 0)
    {
        if (_openSettingsWindow is { IsLoaded: true })
        {
            _openSettingsWindow.SelectSectionByIndex(initialSectionIndex);
            _openSettingsWindow.Activate();
            return;
        }

        _openSettingsWindow = new SettingsWindow(_viewModel, initialSectionIndex);
        _openSettingsWindow.Closed += (_, _) =>
        {
            _openSettingsWindow = null;
            ShowAndFocusNearCursor();
        };
        _openSettingsWindow.Show();
        _openSettingsWindow.Activate();
    }

    private void PositionWindowSmartly()
    {
        var hwnd = new WindowInteropHelper(this).EnsureHandle();
        NativeMethods.GetCursorPos(out var pt);

        var screen = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point(pt.X, pt.Y));
        var workArea = screen.WorkingArea;
        var (winW, winH) = GetPhysicalWindowSize(hwnd);

        int minX = workArea.Left + 8;
        int maxX = Math.Max(minX, workArea.Right - winW - 8);
        int minY = workArea.Top + 10;
        int maxY = Math.Max(minY, workArea.Bottom - winH - 10);

        int targetX;
        int targetY;

        switch (_clipboardService.Settings.SpawnPosition)
        {
            case WindowSpawnPosition.CenterScreen:
                targetX = workArea.Left + (workArea.Width - winW) / 2;
                targetY = workArea.Top + (workArea.Height - winH) / 2;
                break;

            case WindowSpawnPosition.LeftEdge:
                targetX = minX;
                targetY = Math.Clamp(pt.Y - (winH / 2), minY, maxY);
                break;

            case WindowSpawnPosition.RightEdge:
                targetX = maxX;
                targetY = Math.Clamp(pt.Y - (winH / 2), minY, maxY);
                break;

            case WindowSpawnPosition.NearestEdge:
                bool closerToLeft = (pt.X - workArea.Left) <= (workArea.Right - pt.X);
                targetX = closerToLeft ? minX : maxX;
                targetY = Math.Clamp(pt.Y - (winH / 2), minY, maxY);
                break;

            case WindowSpawnPosition.BottomRight:
                targetX = maxX;
                targetY = maxY;
                break;

            case WindowSpawnPosition.TopRight:
                targetX = maxX;
                targetY = minY;
                break;

            case WindowSpawnPosition.TopLeft:
                targetX = minX;
                targetY = minY;
                break;

            case WindowSpawnPosition.BottomLeft:
                targetX = minX;
                targetY = maxY;
                break;

            case WindowSpawnPosition.NearCursor:
            default:
                targetX = Math.Clamp(pt.X - (winW / 2), minX, maxX);
                targetY = Math.Clamp(pt.Y + 18, minY, maxY);
                break;
        }

        targetX = Math.Clamp(targetX, minX, maxX);
        targetY = Math.Clamp(targetY, minY, maxY);

        MoveWindowPhysical(hwnd, targetX, targetY);
    }

    public async Task PasteSelectedItemAsync(ClipItem? item, bool forcePlainText = false)
    {
        if (item is null || _isPasting) return;
        _isPasting = true;

        try
        {
            var s = _clipboardService.Settings;
            bool plainText = forcePlainText || s.DefaultPlainTextPaste;
            var targetHwnd = _foregroundTracker.LastTargetWindow;

            var activeMod = _viewModel.ActiveModifier;
            ClipItem itemToPaste = item;

            if (activeMod is not null && !string.IsNullOrEmpty(item.TextContent))
            {
                var transformed = TextModifierPreset.Apply(item.TextContent, activeMod);
                itemToPaste = new ClipItem
                {
                    Id = item.Id,
                    Kind = ClipKind.Text,
                    TextContent = transformed,
                    HtmlContent = null,
                    PreviewText = ContentDetector.BuildPreviewText(transformed),
                    ContentHash = item.ContentHash,
                    SourceAppName = item.SourceAppName,
                    TitleOrNote = item.TitleOrNote,
                    IsPinned = item.IsPinned
                };
            }

            bool copied = _clipboardService.PlaceItemOnClipboard(itemToPaste, plainText);
            _viewModel.NotifyItemPasted(item);

            if (activeMod is { AutoReset: true })
            {
                _viewModel.ClearModifier();
            }

            HidePopup();

            if (copied && s.PasteDirectly)
            {
                await PasteInjector.RestoreFocusAndPasteAsync(targetHwnd, s.PasteDelayMs);
            }
        }
        finally
        {
            _isPasting = false;
        }
    }

    private async void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (_viewModel.IsEditingNote)
        {
            return;
        }

        if (e.Key == Key.Escape)
        {
            if (_viewModel.HasActiveModifier)
            {
                _viewModel.ClearModifier();
            }
            else if (!string.IsNullOrEmpty(_viewModel.SearchText))
            {
                _viewModel.SearchText = string.Empty;
            }
            else
            {
                HidePopup();
            }
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Down)
        {
            MoveSelection(1);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Up)
        {
            MoveSelection(-1);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter && _viewModel.SelectedItem is not null)
        {
            bool plainOnly = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
            e.Handled = true;
            await PasteSelectedItemAsync(_viewModel.SelectedItem, plainOnly);
            return;
        }

        if (e.Key == Key.P && Keyboard.Modifiers == ModifierKeys.Control)
        {
            _viewModel.TogglePin(_viewModel.SelectedItem);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.F2 && _viewModel.SelectedItem is not null)
        {
            _viewModel.StartEditNote(_viewModel.SelectedItem);
            _ = Dispatcher.BeginInvoke(() =>
            {
                NoteEditBox.Focus();
                NoteEditBox.SelectAll();
            });
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Tab)
        {
            bool reverse = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
            _viewModel.CycleTab(reverse);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Delete && (Keyboard.Modifiers == ModifierKeys.Control || string.IsNullOrEmpty(_viewModel.SearchText)))
        {
            _viewModel.DeleteItem(_viewModel.SelectedItem);
            e.Handled = true;
            return;
        }

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (Keyboard.Modifiers == ModifierKeys.Alt && key is >= Key.D1 and <= Key.D9)
        {
            int digit = (int)(key - Key.D1 + 1);
            _viewModel.SelectModifierByIndex(digit);
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Alt && key == Key.D0)
        {
            _viewModel.ClearModifier();
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && key is >= Key.D1 and <= Key.D9)
        {
            int index = key - Key.D1;
            if (index >= 0 && index < _viewModel.FilteredItems.Count)
            {
                e.Handled = true;
                await PasteSelectedItemAsync(_viewModel.FilteredItems[index]);
                return;
            }
        }
    }

    private void NoneModifierChip_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ClearModifier();
        SearchBox.Focus();
    }

    private void ModifierChip_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: TextModifierPreset preset })
        {
            _viewModel.SelectModifier(preset);
            SearchBox.Focus();
        }
    }

    private void ManageModifiersButton_Click(object sender, RoutedEventArgs e)
    {
        OpenSettingsDialog(6);
    }

    private void MoveSelection(int delta)
    {
        if (_viewModel.FilteredItems.Count == 0) return;

        int currentIndex = _viewModel.SelectedItem is not null
            ? _viewModel.FilteredItems.IndexOf(_viewModel.SelectedItem)
            : 0;

        int nextIndex = Math.Clamp(currentIndex + delta, 0, _viewModel.FilteredItems.Count - 1);
        _viewModel.SelectedItem = _viewModel.FilteredItems[nextIndex];
        ClipsListBox.ScrollIntoView(_viewModel.SelectedItem);
    }

    private async void ClipsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel.SelectedItem is not null)
        {
            bool plainOnly = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
            await PasteSelectedItemAsync(_viewModel.SelectedItem, plainOnly);
        }
    }

    private void PinButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: ClipItem item })
        {
            _viewModel.TogglePin(item);
        }
    }

    private void EditNoteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: ClipItem item })
        {
            _viewModel.StartEditNote(item);
            _ = Dispatcher.BeginInvoke(() =>
            {
                NoteEditBox.Focus();
                NoteEditBox.SelectAll();
            });
        }
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: ClipItem item })
        {
            _viewModel.DeleteItem(item);
        }
    }

    private void NoteEditBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _viewModel.SaveEditedNote();
            SearchBox.Focus();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            _viewModel.CancelEditNote();
            SearchBox.Focus();
            e.Handled = true;
        }
    }

    private void SaveNote_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SaveEditedNote();
        SearchBox.Focus();
    }

    private void CancelNote_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.CancelEditNote();
        SearchBox.Focus();
    }

    private void TabAll_Click(object sender, RoutedEventArgs e) => _viewModel.ActiveTab = FilterTab.All;
    private void TabPinned_Click(object sender, RoutedEventArgs e) => _viewModel.ActiveTab = FilterTab.Pinned;
    private void TabText_Click(object sender, RoutedEventArgs e) => _viewModel.ActiveTab = FilterTab.Text;
    private void TabMedia_Click(object sender, RoutedEventArgs e) => _viewModel.ActiveTab = FilterTab.Media;

    private void PauseCapture_Click(object sender, RoutedEventArgs e) => _viewModel.TogglePauseCapture();
    private void ClearHistory_Click(object sender, RoutedEventArgs e) => _viewModel.ClearUnpinnedHistory();
    private void ShortcutBadge_Click(object sender, MouseButtonEventArgs e) => _viewModel.CycleGlobalShortcut();
    private void SettingsButton_Click(object sender, RoutedEventArgs e) => OpenSettingsDialog();
    private void CloseButton_Click(object sender, RoutedEventArgs e) => HidePopup();

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        if (_isPasting || _openSettingsWindow is not null || !_clipboardService.Settings.CloseOnBlur)
        {
            return;
        }

        // Se a janela acabou de ser exibida (menos de 500ms), ignora desativações transitórias
        // causadas pelo ajuste de ativação inicial do Windows.
        if ((DateTime.UtcNow - _openedAtUtc).TotalMilliseconds < 500)
        {
            return;
        }

        HidePopup();
    }
}
