# 03. Modelo de Dados, Fixação (Pin) e Regras de Negócio

Este documento detalha a estrutura de dados do **ClipFlow**, as regras do sistema de **Fixação (Pin)**, os **Textos Rápidos (Modificadores)**, deduplicação, classificação inteligente e preferências salvas em `AppSettings`.

---

## 1. Entidade Principal: `ClipItem`

Cada item capturado da área de transferência é representado pela seguinte estrutura de modelo:

| Campo | Tipo | Descrição |
| :--- | :--- | :--- |
| `Id` | `string` (GUID) | Identificador único do registro. |
| `Kind` | `ClipKind` | Tipo primário: `Text`, `Image`, `Files`. |
| `SubKind` | `ClipSubKind` | Subtipo detectado: `PlainText`, `Url`, `Email`, `HexColor`, `FilePath`, `Code`. |
| `TextContent` | `string?` | Texto completo copiado (ou lista de caminhos separados para `Files`). |
| `PreviewText` | `string` | Versão resumida/truncada (até 300 caracteres, sem quebras excessivas) para renderização rápida na UI. |
| `HtmlContent` | `string?` | Conteúdo HTML opcional quando copiado de navegadores ou editores ricos. |
| `RtfContent` | `string?` | Conteúdo RTF opcional. |
| `ImagePath` | `string?` | Caminho relativo da imagem salva em disco (`%LOCALAPPDATA%/ClipFlow/images/{hash}.png`). |
| `ContentHash` | `string` (SHA-256) | Hash único do conteúdo para evitar itens duplicados. |
| `IsPinned` | `bool` | **Indica se o item está FIXADO para acesso permanente.** |
| `PinOrder` | `int` | Ordem customizada entre os itens fixados. |
| `TitleOrNote` | `string?` | Apelido/título opcional (ex.: *"Assinatura de Email"*, *"PIX da Empresa"*, *"Comando Docker"*). |
| `SourceAppName` | `string?` | Nome do aplicativo de onde o conteúdo foi copiado (ex.: `chrome`, `Code`, `notepad`). |
| `UseCount` | `int` | Quantidade de vezes que o item foi colado pelo usuário. |
| `CharacterCount` | `int` | Tamanho do texto em caracteres ou dimensões da imagem (`LxA`). |
| `CreatedAt` | `DateTime` | Data/hora da primeira captura. |
| `UpdatedAt` | `DateTime` | Data/hora da última cópia ou uso. |

---

## 2. Entidade de Textos Rápidos: `TextModifierPreset`

Permite ao usuário pré-definir saudações, introduções ou sufixos que são anexados dinamicamente ao texto copiado no momento da colagem:

| Propriedade | Tipo | Descrição |
| :--- | :--- | :--- |
| `Id` | `string` (GUID) | Identificador único do modificador. |
| `Label` | `string` | Nome curto exibido no Chip da barra de atalhos (ex.: *"Bom dia"*, *"Protocolo"*). |
| `Text` | `string` | Conteúdo do texto a ser inserido (ex.: *"Bom dia!\n\n"* ou *"Atenciosamente,\nAlcides"*). |
| `Position` | `ModifierPosition` | `Prefix` (inserir antes do texto) ou `Suffix` (inserir depois do texto). |
| `Spacing` | `ModifierSpacing` | `DoubleBreak` (`\n\n`), `SingleBreak` (`\n`), `Space` (`" "`) ou `None` (`""`). |
| `AutoReset` | `bool` | Se `true`, retorna automaticamente a seleção para "Nenhum" após colar uma vez. |
| `IsEnabled` | `bool` | Controla se o modificador está disponível para uso na barra de atalhos. |

### Regra de Aplicação (`TextModifierPreset.Apply`):
```csharp
public static string Apply(string baseText, TextModifierPreset modifier)
{
    string separator = modifier.Spacing switch
    {
        ModifierSpacing.DoubleBreak => "\n\n",
        ModifierSpacing.SingleBreak => "\n",
        ModifierSpacing.Space => " ",
        ModifierSpacing.None => string.Empty,
        _ => "\n\n"
    };

    return modifier.Position == ModifierPosition.Prefix
        ? $"{modifier.Text}{separator}{baseText}"
        : $"{baseText}{separator}{modifier.Text}";
}
```

---

## 3. Configurações Globais: `AppSettings`

Armazenadas em `%LOCALAPPDATA%/ClipFlow/settings.json`, definem todo o comportamento do aplicativo:

| Grupo | Chaves / Propriedades | Valores Padrão & Descrição |
| :--- | :--- | :--- |
| **Geral** | `StartWithWindows`<br>`StartMinimized`<br>`CloseOnBlur`<br>`AlwaysOnTop` | Iniciar com o Windows, iniciar na bandeja do sistema, fechar ao clicar fora e manter janela no topo. |
| **Atalhos & Borda** | `Shortcut`<br>`EdgeTrigger`<br>`EdgeTriggerDelayMs`<br>`PasteDirectly`<br>`DefaultPlainTextPaste`<br>`MoveToTopOnPaste`<br>`PasteDelayMs` | Atalho global (`Ctrl+Shift+V`, `Alt+V`), gatilho de borda (`Disabled`, `Left`, `Right`, `Both`), delay de sensibilidade de borda (`220ms`), injeção automática de `Ctrl+V` e delay de foco (`35ms`). |
| **Histórico** | `MaxHistoryCount`<br>`ShowPinnedInTextTab`<br>`CaptureText`<br>`CaptureImages`<br>`CaptureFiles`<br>`MaxTextSizeKb` | Limite padrão de 200 itens normais (Pins não contam), filtros de tipos de captura e limite de tamanho por texto (`1024 KB`). |
| **Aparência** | `Theme`<br>`SpawnPosition`<br>`WindowWidth`<br>`WindowHeight`<br>`WindowOpacity`<br>`ShowFooterHints` | Tema visual (`Dark`, `Light`), posição de abertura (`NearCursor`, `Center`, etc.), dimensões (`440x580`), opacidade (`100%`) e dicas no rodapé. |
| **Privacidade** | `IgnorePasswordManagers`<br>`IgnoreSecretTokens`<br>`ExcludedApps` | Ignora gerenciadores de senhas, bloqueia tokens (`sk-`, `ghp_`, `AKIA`, `PRIVATE KEY`) e lista de executáveis ignorados. |
| **Modificadores** | `TextModifiers` | Lista ordenada de objetos `TextModifierPreset` cadastrados pelo usuário. |

---

## 4. Regras do Sistema de Fixação (Pin) 📌

1. **Proteção Permanente:** Itens com `IsPinned = true` **nunca expiram** e **nunca são removidos** pelo limite máximo de histórico ou pelo comando *"Limpar Histórico Não-Fixado"*.
2. **Acesso Dedicado:** Podem ser vistos com destaque na aba *"Tudo"* ou exclusivamente na aba *"Fixados 📌"*.
3. **Apelidos / Notas (`TitleOrNote`):** Editáveis a qualquer momento com `F2`. A barra de busca pesquisa simultaneamente pelo texto copiado e pelo apelido.

---

## 5. Deduplicação Inteligente (SHA-256)

1. Ao capturar um conteúdo, calcula-se o hash `SHA-256` do texto normalizado ou dos bytes da imagem.
2. **Se o hash já existir:** Atualiza `UpdatedAt = DateTime.UtcNow`, incrementa contadores, move para o topo da lista recente e preserva seu status `IsPinned` e apelido.
3. **Se o hash for novo:** Insere um novo item no topo do histórico.

---

## 6. Detecção Automática de Subtipos (`ContentDetector`)

* **Cor (`HexColor`):** Detecta formatos `#RGB`, `#RRGGBB`, `#AARRGGBB` e renderiza uma amostra da cor real na UI.
* **Link (`Url`):** Detecta `http://`, `https://` ou `www.` e exibe badge interativo `🔗`.
* **E-mail (`Email`):** Detecta endereços válidos de correio eletrônico `✉️`.
* **Código (`Code`):** Detecta palavras-chave e símbolos de linguagens de programação `</>`.
* **Caminho de Arquivo (`FilePath`):** Detecta caminhos locais do Windows `📁`.
