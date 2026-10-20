# 04. Design de Interface (UI/UX) e Atalhos de Teclado

Este documento define a experiência de uso do **ClipFlow**, combinando a agilidade do `Win + V` nativo do Windows 11 com recursos avançados de busca, fixação (Pin) e navegação 100% por teclado.

---

## 1. Anatomia da Janela Flutuante (`PopupWindow`)

A janela do **ClipFlow** possui dimensões compactas (`400px` de largura por `540px` de altura), bordas arredondadas (`CornerRadius="12"`), tema escuro moderno (Fluent Dark) e sombra suave.

```text
┌──────────────────────────────────────────────────────────┐
│  ⚡ ClipFlow                           [📌 4]  [⚙️] [✕]  │ <- Cabeçalho + Contador de Fixados
├──────────────────────────────────────────────────────────┤
│  🔍 Buscar no histórico ou apelido...          [Ctrl+F]  │ <- Busca Instantânea (Foco Automático)
├──────────────────────────────────────────────────────────┤
│  [ Tudo ]   [ 📌 Fixados ]   [ 📝 Texto ]   [ 🖼️ Mídia ] │ <- Abas de Filtro Rápido (Tab)
├──────────────────────────────────────────────────────────┤
│  ┌────────────────────────────────────────────────────┐  │
│  │ 📌 Assinatura Comercial                   [1]  📌  │  │ <- Item Fixado c/ Apelido
│  │ Atenciosamente, Alcides Diógenes - Diretor...      │  │
│  │ 🕒 Fixado • Code.exe • 84 chars           [✏️] [🗑️] │  │
│  └────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────┐  │
│  │ 🔗 https://github.com/EcoPasteHub/EcoPaste [2] 📍  │  │ <- Item com Badge de URL
│  │ 🕒 Há 2 min • chrome.exe                           │  │
│  └────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────┐  │
│  │ 🎨 #3B82F6 (Azul Fluent)                   [3] 📍  │  │ <- Item de Cor c/ Preview Real
│  │ 🕒 Há 5 min • Figma.exe                            │  │
│  └────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────┐  │
│  │ 🖼️ Captura de Tela (1280 x 720)            [4] 📍  │  │ <- Miniatura de Imagem
│  │ [ Miniatura Visual da Imagem Copiada ]             │  │
│  └────────────────────────────────────────────────────┘  │
├──────────────────────────────────────────────────────────┤
│  ↵ Colar  │ ⇧↵ Texto Puro │ Ctrl+P Fixar │ 1-9 Rápido    │ <- Rodapé de Dicas de Atalho
└──────────────────────────────────────────────────────────┘
```

---

## 2. Fluxo de Posicionamento Inteligente na Tela

Quando o usuário aciona o atalho global:
1. O **ClipFlow** lê a posição atual do cursor do mouse (`GetCursorPos`) e identifica o monitor ativo (`Screen.FromPoint`).
2. Posiciona a janela próxima ao cursor (ou centralizada no monitor ativo, conforme preferência), garantindo que a janela **nunca ultrapasse as bordas da área de trabalho (`WorkingArea`)**.
3. Ao clicar fora da janela (`Window.Deactivated`) ou apertar `Esc`, a janela se esconde suavemente sem encerrar o serviço em segundo plano.

---

## 3. Mapa Completo de Atalhos de Teclado

O **ClipFlow** foi desenhado para que você nunca precise tirar a mão do teclado:

| Atalho | Ação |
| :--- | :--- |
| **`Ctrl + Shift + V`** *(ou `Alt + V`)* | Abre / Fecha a janela do **ClipFlow** em qualquer lugar do Windows. |
| **`↑` / `↓`** | Navega entre os itens da lista. |
| **`Enter`** *(ou Clique)* | **Cola imediatamente** o item selecionado na janela onde você estava digitando. |
| **`Shift + Enter`** | **Cola como Texto Puro** (remove formatação, cores, fontes e HTML). |
| **`Alt + 1` até `Alt + 9`** *(ou `Ctrl + 1..9`)* | Cola instantaneamente o item de número `1` a `9` da lista visível. |
| **`Ctrl + P`** *(ou clicar no ícone 📌)* | **Fixa / Desafixa (Pin/Unpin)** o item selecionado. |
| **`F2`** *(ou `Ctrl + E`)* | Adiciona ou edita o **Apelido/Nota** do item selecionado (ótimo para itens fixados). |
| **`Tab` / `Shift + Tab`** | Alterna entre as abas: *Tudo* → *Fixados 📌* → *Texto* → *Imagens*. |
| **`Delete`** *(ou `Ctrl + D`)* | Remove o item selecionado do histórico (se não estiver fixado, ou pede confirmação). |
| **`Esc`** | Limpa o texto da busca (se houver texto) ou fecha a janela do ClipFlow. |
| **Digitar qualquer letra** | Já escreve direto na barra de busca instantânea e filtra em tempo real (< 5ms). |

---

## 4. Paleta de Cores (Fluent Dark Theme — Windows 11)

| Elemento | Cor Hex | Descrição |
| :--- | :--- | :--- |
| **Fundo da Janela** | `#18181B` | Cinza grafite profundo (estilo Windows 11 Dark). |
| **Fundo do Card (Normal)** | `#242429` | Superfície elevada para cada item copiado. |
| **Fundo do Card (Selecionado/Hover)** | `#2E323B` | Destaque suave com borda azul `#3B82F6`. |
| **Fundo do Card (Fixado 📌)** | `#1E2638` | Tom sutilmente azulado com borda dourada/azul para destacar itens fixados. |
| **Cor de Destaque Principal (Accent)** | `#3B82F6` | Azul vibrante moderno. |
| **Cor do Ícone de Pin Ativo** | `#F59E0B` | Âmbar / Dourado para fácil identificação visual. |
| **Texto Principal** | `#F4F4F5` | Branco suave de alta legibilidade. |
| **Texto Secundário (Metadados)** | `#A1A1AA` | Cinza médio para tempo relativo, tamanho e app de origem. |
