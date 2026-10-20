# 05. Roadmap de Implementação — ClipFlow

Este documento define o plano de construção passo a passo do **ClipFlow**, dividido em fases incrementais e testáveis.

---

## Fase 1: Fundação Nativa e Motor de Clipboard (Core)
- [x] **1.1.** Criar a solução `ClipFlow.sln` e o projeto `ClipFlow.App` em `.NET 10 (WPF)` dentro de `C:\Users\alcidesdiogenes\Desktop\projetos\clipflow`.
- [x] **1.2.** Implementar `NativeMethods.cs` com as chamadas P/Invoke da `user32.dll` (`AddClipboardFormatListener`, `RegisterHotKey`, `SetWinEventHook`, `SendInput`, `GetClipboardSequenceNumber`).
- [x] **1.3.** Implementar `MessageWindow.cs` (`HWND_MESSAGE`) para escuta invisível de eventos do Windows.
- [x] **1.4.** Implementar `ForegroundTracker.cs` para memorizar a última janela ativa do usuário antes de acionar o ClipFlow.
- [x] **1.5.** Implementar `WritebackGuard.cs` e `PrivacyGuard.cs` (proteção contra loops de colagem e exclusão de senhas).
- [x] **1.6.** Implementar `ClipboardService.cs` com retry automático para `COMException`, captura de textos, imagens e arquivos, e detecção de subtipos (`ContentDetector.cs`).

---

## Fase 2: Persistência, Histórico e Sistema de Fixação (Pin)
- [x] **2.1.** Implementar `ClipItem.cs` e `StorageService.cs` (armazenamento local persistente em `%LOCALAPPDATA%\ClipFlow` com cache em memória para busca instantânea em 0ms).
- [x] **2.2.** Implementar deduplicação por hash `SHA-256` (mover item repetido para o topo sem duplicar).
- [x] **2.3.** Implementar lógica de **Fixar / Desafixar (`TogglePin`)**, suporte a **Apelidos/Notas (`TitleOrNote`)** para itens fixados e proteção contra exclusão na limpeza automática.
- [x] **2.4.** Implementar salvamento otimizado de imagens copiadas em disco com geração de miniatura leve.

---

## Fase 3: Interface Moderna (Estilo Win + V) e Teclado
- [x] **3.1.** Construir o `PopupWindow.xaml` com visual moderno Windows 11 Dark, cantos arredondados, cabeçalho compacto e posicionamento inteligente próximo ao cursor/centro da tela.
- [x] **3.2.** Implementar barra de busca instantânea (filtra por texto copiado, apelido de item fixado, tipo ou aplicativo de origem).
- [x] **3.3.** Implementar abas rápidas: **Tudo**, **Fixados 📌**, **Textos**, **Mídia**.
- [x] **3.4.** Implementar cards ricos com:
  * Ícone de Pin interativo 📌
  * Preview de cor real quando copiar códigos `#HEX`
  * Badges para Links `🔗`, E-mails `✉️`, Código `</>` e Imagens `🖼️`
  * Indicador numérico `Alt+1..9` para colagem rápida
  * Edição rápida de apelido/etiqueta para itens fixados (`F2` ou `✏️`)
- [x] **3.5.** Implementar `PasteInjector.cs` para colar automaticamente ao dar `Enter` ou duplo clique (`Ctrl+V` nativo + `Shift+Enter` para colar sem formatação).

---

## Fase 4: Bandeja do Sistema (System Tray), Configurações e Polimento
- [x] **4.1.** Adicionar ícone na Bandeja do Sistema (`NotifyIcon`) com menu de contexto:
  * *Abrir ClipFlow (`Ctrl+Shift+V`)*
  * *Ver Itens Fixados 📌*
  * *Pausar / Retomar Captura*
  * *Limpar Histórico Não-Fixado*
  * *Sair*
- [x] **4.2.** Permitir alternar atalho global diretamente pelo badge no topo da janela (`Ctrl+Shift+V`, `Alt+V`, `Ctrl+Alt+V`).
- [x] **4.3.** Compilação Release limpa (`0 Erros, 0 Avisos`).
