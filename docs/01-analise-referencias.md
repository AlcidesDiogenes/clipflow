# 01. Análise Técnica dos Projetos de Referência

Este documento registra a engenharia reversa e análise arquitetural dos 3 projetos clonados em `Desktop/projetos/referencias`, servindo como base técnica para a construção do **ClipFlow**.

---

## Visão Geral Comparativa

| Critério | **Clippboard** (`shardapps/clippboard`) | **EcoPaste** (`EcoPasteHub/EcoPaste`) | **Clip** (`Sukarth/Clip`) | **Decisão para o ClipFlow** |
| :--- | :--- | :--- | :--- | :--- |
| **Stack** | C# / .NET / WPF | Rust (Tauri) + React + TypeScript | Electron + React + C++ Nativo | **C# / .NET 10 + WPF Moderno** |
| **Consumo de RAM** | ~25–45 MB (Muito Baixo) | ~60–90 MB (WebView2) | ~180–300 MB (Chromium) | **< 45 MB (Nativo WPF)** |
| **Escuta de Clipboard** | `AddClipboardFormatListener` via `HWND_MESSAGE` | `clipboard-rs` / Win32 API | Polling + Helper Nativo | **`AddClipboardFormatListener` (`HWND_MESSAGE`)** |
| **Banco de Dados** | Arquivo local criptografado (DPAPI) | SQLite + FTS5 (`trigram`) | SQLite / Cache em memória | **SQLite Local + Índices Rápidos** |
| **Sistema de Fixação (Pin)** | Retenção de notas | `is_pinned` + `is_favorite` + Grupos | `pinned` booleano no topo da lista | **`IsPinned` no topo + Atalho rápido + Notas/Tags** |
| **Injeção de Colagem** | `SendInput` (`Ctrl+V` nativo em C#) | `enigo` / Simulação de teclas | Executável externo `SendPaste.exe` (C++) | **`SendInput` + Fallback `WM_PASTE` em C# puro** |
| **Proteção de Senhas** | Flags da Microsoft (`CanIncludeInClipboardHistory`) | Regex de Tokens/Chaves + Exclusão de Apps | Básica | **Flags Microsoft + Regex de Segredos + App Ignore** |

---

## 1. Análise do Projeto `Clippboard` (C# / .NET / WPF)

📁 **Caminho Local:** [clippboard](file:///C:/Users/alcidesdiogenes/Desktop/projetos/referencias/clippboard)

### Pontos Fortes que Adotaremos:
1. **Janela Invisível de Mensagens (`MessageWindow.cs`):**
   * Cria um `NativeWindow` com `Parent = new nint(-3)` (`HWND_MESSAGE`).
   * **Vantagem:** Recebe eventos do sistema operacional (`WM_CLIPBOARDUPDATE = 0x031D` e `WM_HOTKEY = 0x0312`) sem criar janela na barra de tarefas, sem aparecer no `Alt + Tab` e sem roubar foco.
2. **Tratamento de Concorrência COM (`ClipboardService.cs`):**
   * No Windows, quando um programa copia algo, o evento `WM_CLIPBOARDUPDATE` dispara imediatamente, muitas vezes enquanto o programa de origem ainda mantém a área de transferência bloqueada (`OpenClipboard`), gerando `COMException`.
   * **Solução:** Uso de um `DispatcherTimer` de `80ms` com até 8 tentativas e validação de `GetClipboardSequenceNumber()`.
3. **Rastreamento de Janela Ativa (`ForegroundTracker.cs`):**
   * Usa `SetWinEventHook` (`EVENT_SYSTEM_FOREGROUND = 3`) para registrar continuamente qual janela estava ativa antes do gerenciador ser aberto.
   * Filtra janelas do próprio sistema (`Shell_TrayWnd`, `NotifyIconOverflowWindow`, `#32768`) para garantir que, mesmo abrindo pelo ícone da bandeja (System Tray), o app saiba onde colar o texto.
4. **Respeito aos Padrões de Privacidade do Windows (`ClipboardService.cs` linhas 138–148):**
   * Verifica os formatos `ExcludeClipboardContentFromMonitorProcessing` e `CanIncludeInClipboardHistory`. Gerenciadores de senha como **Bitwarden, 1Password e KeePass** injetam essas flags para impedir que senhas fiquem salvas no histórico.

### O que não gostamos / Vamos fazer diferente:
* A interface do `Clippboard` usa uma metáfora visual de "corda puxada do topo da tela" (`App.Overlay.cs`), que foge do padrão ágil e compacto estilo `Win + V`. No **ClipFlow**, faremos um pop-up flutuante limpo, direto e focado em produtividade.

---

## 2. Análise do Projeto `EcoPaste` (Rust / Tauri + React)

📁 **Caminho Local:** [EcoPaste](file:///C:/Users/alcidesdiogenes/Desktop/projetos/referencias/EcoPaste)

### Pontos Fortes que Adotaremos:
1. **Mecanismo Anti-Loop de Escrita (`WritebackGuard` em `guard.rs`):**
   * Quando o usuário escolhe um item antigo ou fixado para colar, o gerenciador precisa escrevê-lo de volta no Clipboard do Windows. Isso dispara um novo evento `WM_CLIPBOARDUPDATE`.
   * **Solução Combinada:** Registrar tanto o `GetClipboardSequenceNumber()` quanto o `ContentHash` (com TTL de 2 segundos) antes de gravar no Clipboard, evitando duplicar ou reordenar acidentalmente quando o próprio ClipFlow faz a escrita.
2. **Classificação Automática de Subtipos (`detect.rs`):**
   * Ao capturar um texto, classifica automaticamente se o conteúdo é:
     * **URL / Link** (`http://`, `https://`, `www.`)
     * **E-mail** (`usuario@dominio.com`)
     * **Cor** (`#RRGGBB`, `rgb()`, `hsl()`) — permitindo mostrar um quadradinho colorido ao lado do texto!
     * **Caminho de Arquivo** (`C:\...`)
     * **Código / Texto Comum**
3. **Modelagem de Dados Completa (`0001_init.sql`):**
   * Campos essenciais: `content_hash` (para deduplicação instantânea), `is_pinned` (fixado), `use_count` (contador de uso), `note` (etiqueta/apelido opcional para itens fixados) e `source_app` (qual programa originou a cópia).
4. **Detecção de Chaves e Tokens (`secrets.rs`):**
   * Detecta chaves privadas (`-----BEGIN PRIVATE KEY-----`), tokens da OpenAI (`sk-...`), GitHub (`ghp_...`), AWS (`AKIA...`) e JWTs (`eyJ...`) para protegê-los ou não armazená-los em texto aberto.
5. **Política de Limpeza Protegendo Fixados (`cleanup.rs`):**
   * O job de limpeza remove itens antigos ou excedentes ao limite máximo (ex.: manter últimos 200 itens), mas **jamais apaga itens com `is_pinned = 1`**.

---

## 3. Análise do Projeto `Clip` (`Sukarth/Clip`)

📁 **Caminho Local:** [Clip](file:///C:/Users/alcidesdiogenes/Desktop/projetos/referencias/Clip)

### Pontos Fortes que Adotaremos:
1. **Fluxo de Colagem Direta (`SendPaste.cpp`):**
   * Mostra o ciclo exato para colar sem que o usuário precise apertar `Ctrl + V` manualmente:
     1. Esconde o pop-up do gerenciador.
     2. Restaura o foco na janela anterior (`SetForegroundWindow(targetHwnd)`).
     3. Verifica a classe da janela (`ConsoleWindowClass`, `Edit`, `RichEdit`) ou dispara `SendInput` simulando `Ctrl Down -> V Down -> V Up -> Ctrl Up`.
2. **Experiência de Usuário (UX) inspirada no `Win + V` (`App.tsx`):**
   * Janela compacta (ex.: `380px x 520px`), posicionada próxima ao cursor do mouse ou centro da tela ativa.
   * Fecha automaticamente ao perder o foco (`Deactivated` / `Blur`) ou ao pressionar `Esc`.
   * Botão de **Pin (Fixar)** diretamente visível em cada cartão do histórico.

### O que não gostamos / Vamos fazer diferente:
* O `Clip` depende do Electron + um executável externo `AutoHotkey.exe` (1.2 MB) e outro `SendPaste.exe` (490 KB) invocados via linha de comando a cada colagem, o que gera atraso (latência) e alto uso de memória.
* No **ClipFlow**, tudo isso rodará **em um único processo nativo .NET 10**, sem processos externos, com latência próxima de zero (< 5ms).
