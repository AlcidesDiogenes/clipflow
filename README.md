# ⚡ ClipFlow

Gerenciador moderno de Área de Transferência (Clipboard Manager) para Windows, inspirado na praticidade do `Win + V`, mas construído para **alta velocidade, histórico persistente, busca instantânea e fixação avançada (Pins com apelidos)**.

---

## 📚 Documentação do Projeto

Toda a documentação de engenharia, análise das referências e arquitetura do **ClipFlow** encontra-se na pasta [`docs/`](./docs):

1. [**01. Análise Técnica dos Projetos de Referência**](./docs/01-analise-referencias.md)
   * Estudo comparativo detalhado dos projetos *Clippboard (C#/WPF)*, *EcoPaste (Rust/Tauri)* e *Clip (TS/C++)*.
2. [**02. Arquitetura do Sistema — ClipFlow**](./docs/02-arquitetura-clipflow.md)
   * Diagrama de camadas, estrutura de diretórios e especificação das APIs nativas do Windows (`HWND_MESSAGE`, `AddClipboardFormatListener`, `SetWinEventHook`, `SendInput`).
3. [**03. Modelo de Dados, Fixação (Pin) e Regras de Negócio**](./docs/03-modelo-dados-e-regras.md)
   * Estrutura do `ClipItem`, regras de itens fixados, apelidos/notas, deduplicação SHA-256, detecção de cores/links e filtros de privacidade.
4. [**04. Design de Interface (UI/UX) e Atalhos de Teclado**](./docs/04-ux-ui-e-atalhos.md)
   * Layout da janela flutuante estilo `Win + V`, paleta de cores Fluent Dark e mapa de atalhos de teclado.
5. [**05. Roadmap de Implementação**](./docs/05-roadmap-implementacao.md)
   * Plano de desenvolvimento incremental dividido em 4 fases.

---

## 🛠️ Stack Tecnológica
* **Plataforma:** Windows (Nativo)
* **Linguagem & Runtime:** C# 13 / .NET 10 (`net10.0-windows`)
* **Interface Gráfica:** WPF (Windows Presentation Foundation) com estilização Fluent Dark
* **Integração com SO:** Win32 API via P/Invoke (`user32.dll`)
