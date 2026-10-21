namespace ClipFlow.App.Models;

public sealed class AppSettings
{
    // 1. Geral & Sistema
    public bool StartWithWindows { get; set; } = false;
    public bool StartMinimized { get; set; } = false;
    public bool CloseOnBlur { get; set; } = true;
    public bool AlwaysOnTop { get; set; } = true;

    // 2. Atalhos, Borda da Tela & Colagem
    public ShortcutChoice Shortcut { get; set; } = ShortcutChoice.CtrlShiftV;
    public EdgeTriggerMode EdgeTrigger { get; set; } = EdgeTriggerMode.BothEdges;
    public int EdgeTriggerDelayMs { get; set; } = 220;
    public bool PasteDirectly { get; set; } = true;
    public bool DefaultPlainTextPaste { get; set; } = false;
    public int PasteDelayMs { get; set; } = 35;
    public bool MoveToTopOnPaste { get; set; } = true;

    // 3. Histórico & Captura
    public int MaxHistoryCount { get; set; } = 200;
    public bool ShowPinnedInTextTab { get; set; } = false;
    public bool CaptureText { get; set; } = true;
    public bool CaptureImages { get; set; } = true;
    public bool CaptureFiles { get; set; } = true;
    public int MaxTextSizeKb { get; set; } = 1024;

    // 4. Aparência & Posicionamento
    public AppTheme Theme { get; set; } = AppTheme.Dark;
    public WindowSpawnPosition SpawnPosition { get; set; } = WindowSpawnPosition.NearCursor;
    public bool OpenNearCursor { get; set; } = true;
    public double WindowOpacity { get; set; } = 1.0;
    public double WindowWidth { get; set; } = 440;
    public double WindowHeight { get; set; } = 580;
    public bool ShowFooterHints { get; set; } = true;

    // 5. Privacidade & Segurança
    public bool IgnorePasswordManagers { get; set; } = true;
    public bool IgnoreSecretTokens { get; set; } = false;
    public string[] ExcludedApps { get; set; } =
    [
        "1Password",
        "Bitwarden",
        "KeePass",
        "KeePassXC",
        "LastPass",
        "Dashlane"
    ];

    // 6. Textos Rápidos & Modificadores (Saudações, Despedidas, Modelos)
    public List<TextModifierPreset> TextModifiers { get; set; } =
    [
        new()
        {
            Id = "mod-bom-dia",
            Label = "Bom dia",
            Text = "Bom dia!",
            Position = ModifierPosition.Prefix,
            Spacing = ModifierSpacing.DoubleLine,
            AutoReset = true,
            IsEnabled = true
        },
        new()
        {
            Id = "mod-boa-tarde",
            Label = "Boa tarde",
            Text = "Boa tarde!",
            Position = ModifierPosition.Prefix,
            Spacing = ModifierSpacing.DoubleLine,
            AutoReset = true,
            IsEnabled = true
        },
        new()
        {
            Id = "mod-atenciosamente",
            Label = "Atenciosamente",
            Text = "Atenciosamente,",
            Position = ModifierPosition.Suffix,
            Spacing = ModifierSpacing.DoubleLine,
            AutoReset = true,
            IsEnabled = true
        }
    ];
}
