using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using ClipFlow.App.Platform;
using ClipFlow.App.Services;
using ClipFlow.App.ViewModels;
using ClipFlow.App.Views;

namespace ClipFlow.App;

public partial class App : System.Windows.Application
{
    private Mutex? _singleInstanceMutex;
    private StorageService? _storage;
    private ClipboardService? _clipboardService;
    private EdgeTriggerService? _edgeTriggerService;
    private ForegroundTracker? _foregroundTracker;
    private MainViewModel? _viewModel;
    private PopupWindow? _popupWindow;
    private NotifyIcon? _trayIcon;
    private Icon? _generatedIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstanceMutex = new Mutex(true, @"Local\ClipFlow_SingleInstance_Mutex", out bool isNewInstance);
        if (!isNewInstance)
        {
            Shutdown();
            return;
        }

        _storage = new StorageService();
        var settings = _storage.LoadSettings();
        ThemeService.ApplyTheme(settings.Theme);

        _foregroundTracker = new ForegroundTracker();
        _clipboardService = new ClipboardService(_storage, settings);
        _edgeTriggerService = new EdgeTriggerService(settings);
        _viewModel = new MainViewModel(_storage, _clipboardService);
        _popupWindow = new PopupWindow(_viewModel, _clipboardService, _foregroundTracker);

        _viewModel.SettingsApplied += () =>
        {
            if (_edgeTriggerService is not null)
            {
                _edgeTriggerService.Settings = _clipboardService.Settings;
            }
        };

        _clipboardService.ItemCaptured += item =>
        {
            Dispatcher.Invoke(() => _viewModel.OnNewItemCaptured(item));
        };

        _clipboardService.TogglePopupRequested += () =>
        {
            Dispatcher.Invoke(TogglePopupWindow);
        };

        _clipboardService.ExitRequested += () =>
        {
            Dispatcher.Invoke(ExitApplication);
        };

        _edgeTriggerService.EdgeTriggered += isLeftEdge =>
        {
            Dispatcher.Invoke(() => _popupWindow.ShowDockedAtScreenEdge(isLeftEdge));
        };

        InitializeSystemTray();

        bool startSilently = settings.StartMinimized || e.Args.Any(a => string.Equals(a, "--minimized", StringComparison.OrdinalIgnoreCase));
        if (!startSilently)
        {
            _popupWindow.ShowAndFocusNearCursor();
        }
    }

    private void TogglePopupWindow()
    {
        if (_popupWindow is null) return;

        if (_popupWindow.IsVisible)
        {
            _popupWindow.HidePopup();
        }
        else
        {
            _popupWindow.ShowAndFocusNearCursor();
        }
    }

    private void InitializeSystemTray()
    {
        // Usa o novo ícone vetorial moderno com gradiente e supersampling
        _generatedIcon = AppIconBuilder.CreateTrayIcon(32);

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("⚡ Abrir ClipFlow", null, (_, _) => Dispatcher.Invoke(TogglePopupWindow));
        contextMenu.Items.Add("📌 Ver Itens Fixados", null, (_, _) => Dispatcher.Invoke(() =>
        {
            if (_viewModel is not null) _viewModel.ActiveTab = FilterTab.Pinned;
            _popupWindow?.ShowAndFocusNearCursor();
        }));
        contextMenu.Items.Add("⚙️ Configurações...", null, (_, _) => Dispatcher.Invoke(() => _popupWindow?.OpenSettingsDialog()));
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("⏸ Pausar / Retomar Captura", null, (_, _) => Dispatcher.Invoke(() => _viewModel?.TogglePauseCapture()));
        contextMenu.Items.Add("🧹 Limpar Histórico Não-Fixado", null, (_, _) => Dispatcher.Invoke(() => _viewModel?.ClearUnpinnedHistory()));
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("✕ Sair do ClipFlow", null, (_, _) => Dispatcher.Invoke(ExitApplication));

        _trayIcon = new NotifyIcon
        {
            Icon = _generatedIcon,
            Text = "ClipFlow — Histórico e Fixador de Área de Transferência",
            ContextMenuStrip = contextMenu,
            Visible = true
        };

        _trayIcon.DoubleClick += (_, _) => Dispatcher.Invoke(TogglePopupWindow);
    }

    private void ExitApplication()
    {
        if (_trayIcon is not null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }

        _popupWindow?.Close();
        _edgeTriggerService?.Dispose();
        _clipboardService?.Dispose();
        _foregroundTracker?.Dispose();
        _singleInstanceMutex?.Dispose();
        Shutdown();
    }
}
