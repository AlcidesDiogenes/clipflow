using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ClipFlow.App.Models;
using ClipFlow.App.Services;
using ClipFlow.App.ViewModels;

namespace ClipFlow.App.Views;

public partial class SettingsWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly AppTheme _originalTheme;
    private bool _saved;

    public SettingsWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        Icon = AppIconBuilder.CreateWpfWindowIcon();
        _viewModel = viewModel;
        _originalTheme = _viewModel.Settings.Theme;
        LoadFromSettings(_viewModel.Settings);
    }

    private void LoadFromSettings(AppSettings s)
    {
        // 1. Geral
        ChkStartWithWindows.IsChecked = StartupRegistryService.IsRegistered() || s.StartWithWindows;
        ChkStartMinimized.IsChecked = s.StartMinimized;
        ChkCloseOnBlur.IsChecked = s.CloseOnBlur;
        ChkAlwaysOnTop.IsChecked = s.AlwaysOnTop;

        // 2. Atalhos, Bordas & Colagem
        SelectComboByTag(CmbEdgeTrigger, ((int)s.EdgeTrigger).ToString());
        SelectComboByTag(CmbEdgeDelay, s.EdgeTriggerDelayMs.ToString());
        SelectComboByTag(CmbShortcut, ((int)s.Shortcut).ToString());
        ChkPasteDirectly.IsChecked = s.PasteDirectly;
        ChkDefaultPlainText.IsChecked = s.DefaultPlainTextPaste;
        ChkMoveToTopOnPaste.IsChecked = s.MoveToTopOnPaste;
        SelectComboByTag(CmbPasteDelay, s.PasteDelayMs.ToString());

        // 3. Histórico & Captura
        SelectComboByTag(CmbMaxHistory, s.MaxHistoryCount.ToString());
        ChkShowPinnedInTextTab.IsChecked = s.ShowPinnedInTextTab;
        ChkCaptureText.IsChecked = s.CaptureText;
        ChkCaptureImages.IsChecked = s.CaptureImages;
        ChkCaptureFiles.IsChecked = s.CaptureFiles;
        SelectComboByTag(CmbMaxTextSize, s.MaxTextSizeKb.ToString());

        // 4. Aparência & Tema
        SelectComboByTag(CmbTheme, ((int)s.Theme).ToString());
        SelectComboByTag(CmbSpawnPosition, ((int)s.SpawnPosition).ToString());
        SelectComboByTag(CmbEdgeTriggerAppearance, ((int)s.EdgeTrigger).ToString());
        SelectComboByTag(CmbWindowSize, $"{(int)s.WindowWidth}x{(int)s.WindowHeight}");
        SelectComboByTag(CmbOpacity, s.WindowOpacity.ToString("0.0#", CultureInfo.InvariantCulture));
        ChkShowFooterHints.IsChecked = s.ShowFooterHints;

        // 5. Privacidade
        ChkIgnorePasswordManagers.IsChecked = s.IgnorePasswordManagers;
        ChkIgnoreSecrets.IsChecked = s.IgnoreSecretTokens;
        TxtExcludedApps.Text = string.Join(", ", s.ExcludedApps);

        // 6. Dados
        TxtDataFolderPath.Text = _viewModel.Storage.BaseDir;
        RefreshStatsLabel();
    }

    private void CmbTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (int.TryParse(GetSelectedTag(CmbTheme), out int themeIdx))
        {
            ThemeService.ApplyTheme((AppTheme)themeIdx);
        }
    }

    private void CmbEdgeTriggerAppearance_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbEdgeTrigger is null) return;
        var tag = GetSelectedTag(CmbEdgeTriggerAppearance);
        if (!string.IsNullOrEmpty(tag))
        {
            SelectComboByTag(CmbEdgeTrigger, tag);
        }
    }

    private void CmbEdgeTrigger_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbEdgeTriggerAppearance is null) return;
        var tag = GetSelectedTag(CmbEdgeTrigger);
        if (!string.IsNullOrEmpty(tag))
        {
            SelectComboByTag(CmbEdgeTriggerAppearance, tag);
        }
    }

    private void RefreshStatsLabel()
    {
        TxtStorageStats.Text = $"Itens salvos atualmente: {_viewModel.TotalCount} no total ({_viewModel.PinnedCount} fixados no topo).";
    }

    private static void SelectComboByTag(System.Windows.Controls.ComboBox combo, string targetTag)
    {
        foreach (var obj in combo.Items)
        {
            if (obj is ComboBoxItem item && string.Equals(item.Tag?.ToString(), targetTag, StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedItem = item;
                return;
            }
        }
        if (combo.Items.Count > 0) combo.SelectedIndex = 0;
    }

    private static string? GetSelectedTag(System.Windows.Controls.ComboBox combo)
        => (combo.SelectedItem as ComboBoxItem)?.Tag?.ToString();

    private void NavSection_Checked(object sender, RoutedEventArgs e)
    {
        if (PanelGeneral is null) return;

        PanelGeneral.Visibility = NavGeneral.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        PanelShortcuts.Visibility = NavShortcuts.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        PanelHistory.Visibility = NavHistory.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        PanelAppearance.Visibility = NavAppearance.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        PanelPrivacy.Visibility = NavPrivacy.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        PanelData.Visibility = NavData.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

        RefreshStatsLabel();
    }

    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        var s = _viewModel.Settings;

        // 1. Geral
        s.StartWithWindows = ChkStartWithWindows.IsChecked == true;
        s.StartMinimized = ChkStartMinimized.IsChecked == true;
        s.CloseOnBlur = ChkCloseOnBlur.IsChecked == true;
        s.AlwaysOnTop = ChkAlwaysOnTop.IsChecked == true;

        // 2. Atalhos, Bordas & Colagem
        if (int.TryParse(GetSelectedTag(CmbEdgeTrigger), out int edgeIdx))
        {
            s.EdgeTrigger = (EdgeTriggerMode)edgeIdx;
        }
        if (int.TryParse(GetSelectedTag(CmbEdgeDelay), out int edgeDelayMs))
        {
            s.EdgeTriggerDelayMs = edgeDelayMs;
        }
        if (int.TryParse(GetSelectedTag(CmbShortcut), out int shortcutIdx))
        {
            s.Shortcut = (ShortcutChoice)shortcutIdx;
        }
        s.PasteDirectly = ChkPasteDirectly.IsChecked == true;
        s.DefaultPlainTextPaste = ChkDefaultPlainText.IsChecked == true;
        s.MoveToTopOnPaste = ChkMoveToTopOnPaste.IsChecked == true;
        if (int.TryParse(GetSelectedTag(CmbPasteDelay), out int delayMs))
        {
            s.PasteDelayMs = delayMs;
        }

        // 3. Histórico & Captura
        if (int.TryParse(GetSelectedTag(CmbMaxHistory), out int maxHist))
        {
            s.MaxHistoryCount = maxHist;
        }
        s.ShowPinnedInTextTab = ChkShowPinnedInTextTab.IsChecked == true;
        s.CaptureText = ChkCaptureText.IsChecked == true;
        s.CaptureImages = ChkCaptureImages.IsChecked == true;
        s.CaptureFiles = ChkCaptureFiles.IsChecked == true;
        if (int.TryParse(GetSelectedTag(CmbMaxTextSize), out int maxKb))
        {
            s.MaxTextSizeKb = maxKb;
        }

        // 4. Aparência & Tema
        if (int.TryParse(GetSelectedTag(CmbTheme), out int themeIdx))
        {
            s.Theme = (AppTheme)themeIdx;
        }
        if (int.TryParse(GetSelectedTag(CmbSpawnPosition), out int spawnIdx))
        {
            s.SpawnPosition = (WindowSpawnPosition)spawnIdx;
            s.OpenNearCursor = s.SpawnPosition == WindowSpawnPosition.NearCursor;
        }
        if (int.TryParse(GetSelectedTag(CmbEdgeTriggerAppearance), out int edgeAppIdx))
        {
            s.EdgeTrigger = (EdgeTriggerMode)edgeAppIdx;
        }
        var sizeTag = GetSelectedTag(CmbWindowSize) ?? "440x580";
        var parts = sizeTag.Split('x');
        if (parts.Length == 2 && double.TryParse(parts[0], out double w) && double.TryParse(parts[1], out double h))
        {
            s.WindowWidth = w;
            s.WindowHeight = h;
        }
        if (double.TryParse(GetSelectedTag(CmbOpacity), NumberStyles.Float, CultureInfo.InvariantCulture, out double opacity))
        {
            s.WindowOpacity = Math.Clamp(opacity, 0.5, 1.0);
        }
        s.ShowFooterHints = ChkShowFooterHints.IsChecked == true;

        // 5. Privacidade
        s.IgnorePasswordManagers = ChkIgnorePasswordManagers.IsChecked == true;
        s.IgnoreSecretTokens = ChkIgnoreSecrets.IsChecked == true;
        s.ExcludedApps = TxtExcludedApps.Text
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        _saved = true;
        _viewModel.ApplySettingsChanges(s.Shortcut);
        Close();
    }

    private void OpenDataFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _viewModel.Storage.BaseDir,
                UseShellExecute = true
            });
        }
        catch { }
    }

    private void ClearUnpinned_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ClearUnpinnedHistory();
        RefreshStatsLabel();
        TxtSaveFeedback.Text = "Itens não-fixados foram removidos.";
    }

    private void ClearAll_Click(object sender, RoutedEventArgs e)
    {
        var result = System.Windows.MessageBox.Show(
            this,
            "Tem certeza que deseja apagar TODO o histórico, incluindo seus itens FIXADOS?",
            "Confirmar Limpeza Total",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            _viewModel.ClearAllHistoryIncludingPinned();
            RefreshStatsLabel();
            TxtSaveFeedback.Text = "Todo o histórico foi apagado.";
        }
    }

    private void ResetDefaults_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ResetDefaultSettings();
        LoadFromSettings(_viewModel.Settings);
        TxtSaveFeedback.Text = "Configurações padrão restauradas!";
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosed(EventArgs e)
    {
        if (!_saved)
        {
            ThemeService.ApplyTheme(_originalTheme);
        }
        base.OnClosed(e);
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }
}
