using System;
using System.Collections.ObjectModel;
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
    private readonly ObservableCollection<TextModifierPreset> _workingModifiers = [];
    private TextModifierPreset? _currentEditingModifier;
    private bool _saved;

    public SettingsWindow(MainViewModel viewModel, int initialSectionIndex = 0)
    {
        InitializeComponent();
        Icon = AppIconBuilder.CreateWpfWindowIcon();
        _viewModel = viewModel;
        _originalTheme = _viewModel.Settings.Theme;
        LoadFromSettings(_viewModel.Settings);
        SelectSectionByIndex(initialSectionIndex);
    }

    public void SelectSectionByIndex(int index)
    {
        switch (index)
        {
            case 0: NavGeneral.IsChecked = true; break;
            case 1: NavShortcuts.IsChecked = true; break;
            case 2: NavHistory.IsChecked = true; break;
            case 3: NavAppearance.IsChecked = true; break;
            case 4: NavPrivacy.IsChecked = true; break;
            case 5: NavData.IsChecked = true; break;
            case 6: NavModifiers.IsChecked = true; break;
            default: NavGeneral.IsChecked = true; break;
        }
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

        // 7. Textos & Modificadores
        _workingModifiers.Clear();
        foreach (var mod in s.TextModifiers)
        {
            _workingModifiers.Add(new TextModifierPreset
            {
                Id = mod.Id,
                Label = mod.Label,
                Text = mod.Text,
                Position = mod.Position,
                Spacing = mod.Spacing,
                AutoReset = mod.AutoReset,
                IsEnabled = mod.IsEnabled
            });
        }
        ModifiersItemsList.ItemsSource = _workingModifiers;
        ResetModifierEditor();
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
        PanelModifiers.Visibility = NavModifiers.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

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

        // 6. Textos Rápidos & Modificadores
        s.TextModifiers = _workingModifiers.ToList();

        _saved = true;
        _viewModel.ApplySettingsChanges(s.Shortcut);
        Close();
    }

    private void ModEditorField_Changed(object? sender, EventArgs? e)
    {
        UpdateModifierPreview();
    }

    private void UpdateModifierPreview()
    {
        if (TxtModPreview is null) return;

        string sample = "Prezados, segue o relatório em anexo.";
        string customText = TxtModText?.Text ?? string.Empty;

        int posIdx = 0;
        if (int.TryParse(GetSelectedTag(CmbModPosition), out int p)) posIdx = p;

        int spaceIdx = 0;
        if (int.TryParse(GetSelectedTag(CmbModSpacing), out int sp)) spaceIdx = sp;

        var tempMod = new TextModifierPreset
        {
            Label = TxtModLabel?.Text ?? string.Empty,
            Text = customText,
            Position = (ModifierPosition)posIdx,
            Spacing = (ModifierSpacing)spaceIdx,
            AutoReset = ChkModAutoReset?.IsChecked == true
        };

        TxtModPreview.Text = TextModifierPreset.Apply(sample, tempMod);
    }

    private void SaveModifierItem_Click(object sender, RoutedEventArgs e)
    {
        string label = TxtModLabel.Text.Trim();
        string text = TxtModText.Text;

        if (string.IsNullOrWhiteSpace(label))
        {
            System.Windows.MessageBox.Show(this, "Por favor, digite um nome curto para o chip (ex: 'Bom dia').", "Nome Obrigatório", MessageBoxButton.OK, MessageBoxImage.Information);
            TxtModLabel.Focus();
            return;
        }

        if (string.IsNullOrEmpty(text))
        {
            System.Windows.MessageBox.Show(this, "Por favor, digite o texto a ser inserido.", "Texto Obrigatório", MessageBoxButton.OK, MessageBoxImage.Information);
            TxtModText.Focus();
            return;
        }

        int posIdx = 0;
        if (int.TryParse(GetSelectedTag(CmbModPosition), out int p)) posIdx = p;

        int spaceIdx = 0;
        if (int.TryParse(GetSelectedTag(CmbModSpacing), out int sp)) spaceIdx = sp;

        if (_currentEditingModifier is not null)
        {
            _currentEditingModifier.Label = label;
            _currentEditingModifier.Text = text;
            _currentEditingModifier.Position = (ModifierPosition)posIdx;
            _currentEditingModifier.Spacing = (ModifierSpacing)spaceIdx;
            _currentEditingModifier.AutoReset = ChkModAutoReset.IsChecked == true;
        }
        else
        {
            var newPreset = new TextModifierPreset
            {
                Label = label,
                Text = text,
                Position = (ModifierPosition)posIdx,
                Spacing = (ModifierSpacing)spaceIdx,
                AutoReset = ChkModAutoReset.IsChecked == true,
                IsEnabled = true
            };
            _workingModifiers.Add(newPreset);
        }

        ResetModifierEditor();
        TxtSaveFeedback.Text = $"Modificador '{label}' preparado para salvar.";
    }

    private void CancelModifierItem_Click(object sender, RoutedEventArgs e)
    {
        ResetModifierEditor();
    }

    private void ResetModifierEditor()
    {
        _currentEditingModifier = null;
        if (TxtEditorTitle is not null) TxtEditorTitle.Text = "Adicionar Novo Texto / Modificador";
        if (BtnSaveModifier is not null) BtnSaveModifier.Content = "Salvar Modificador";
        if (TxtModLabel is not null) TxtModLabel.Text = string.Empty;
        if (TxtModText is not null) TxtModText.Text = string.Empty;
        if (CmbModPosition is not null) SelectComboByTag(CmbModPosition, "0");
        if (CmbModSpacing is not null) SelectComboByTag(CmbModSpacing, "0");
        if (ChkModAutoReset is not null) ChkModAutoReset.IsChecked = true;
        UpdateModifierPreview();
    }

    private void EditModifier_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: TextModifierPreset item })
        {
            _currentEditingModifier = item;
            TxtEditorTitle.Text = $"Editando Modificador: {item.Label}";
            BtnSaveModifier.Content = "Atualizar Modificador";
            TxtModLabel.Text = item.Label;
            TxtModText.Text = item.Text;
            SelectComboByTag(CmbModPosition, ((int)item.Position).ToString());
            SelectComboByTag(CmbModSpacing, ((int)item.Spacing).ToString());
            ChkModAutoReset.IsChecked = item.AutoReset;
            UpdateModifierPreview();
            TxtModLabel.Focus();
            TxtModLabel.SelectAll();
        }
    }

    private void DeleteModifier_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: TextModifierPreset item })
        {
            _workingModifiers.Remove(item);
            if (_currentEditingModifier?.Id == item.Id)
            {
                ResetModifierEditor();
            }
        }
    }

    private void MoveUpModifier_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: TextModifierPreset item })
        {
            int index = _workingModifiers.IndexOf(item);
            if (index > 0)
            {
                _workingModifiers.Move(index, index - 1);
            }
        }
    }

    private void MoveDownModifier_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: TextModifierPreset item })
        {
            int index = _workingModifiers.IndexOf(item);
            if (index >= 0 && index < _workingModifiers.Count - 1)
            {
                _workingModifiers.Move(index, index + 1);
            }
        }
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
