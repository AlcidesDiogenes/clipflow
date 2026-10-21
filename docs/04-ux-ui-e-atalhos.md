# 04. Design de Interface (UI/UX) e Atalhos de Teclado

Este documento define a experiência visual e a ergonomia de uso do **ClipFlow**, combinando a praticidade do `Win + V` nativo com ferramentas profissionais de busca, fixação (Pins), modificadores de texto rápidos e uma central de configurações fluente.

---

## 1. Anatomia da Janela Flutuante (`PopupWindow`)

A janela principal do **ClipFlow** possui dimensões padrão de `440px` por `580px` (configurável), cantos arredondados (`12px`), sombra projetada em camadas e suporte dinâmico aos temas **Fluent Dark** e **Fluent Light**.

```text
┌──────────────────────────────────────────────────────────────┐
│  ⚡ ClipFlow                               [📌 4]  [⚙️] [✕]  │ <- Cabeçalho com Brand + Contador
├──────────────────────────────────────────────────────────────┤
│  🔍 Buscar no histórico ou apelido...              [Ctrl+F]  │ <- Busca Instantânea (< 5ms)
├──────────────────────────────────────────────────────────────┤
│  [✕] [ 1 Bom dia ] [ 2 Boa tarde ] [ 3 Protocolo ]   [ + ]   │ <- Barra de Chips de Modificadores (Alt+1..9)
├──────────────────────────────────────────────────────────────┤
│  [ Tudo ]   [ 📌 Fixados ]   [ 📝 Texto ]   [ 🖼️ Mídia ]     │ <- Abas de Filtro de Conteúdo (Tab)
├──────────────────────────────────────────────────────────────┤
│  ┌────────────────────────────────────────────────────────┐  │
│  │ 📌 Resposta Padrão Suporte                          📌 │  │ <- Item Fixado c/ Apelido
│  │ Olá! Recebemos sua solicitação e já estamos...         │  │
│  │ 🕒 Fixado • chrome.exe • 112 chars            [✏️] [🗑️] │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ 🔗 https://github.com/alcidesdiogenes/clipflow      📍 │  │ <- Item com Badge de Link
│  │ 🕒 Há 2 min • Code.exe • 44 chars                      │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ 🎨 #3B82F6 (Azul Fluent)                            📍 │  │ <- Amostra de Cor Real + Hex
│  │ 🕒 Há 5 min • Figma.exe                                │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ 🖼️ Captura de Tela (1920 x 1080)                     📍 │  │ <- Miniatura Visual de Print/Foto
│  │ [ Miniatura gerada localmente em alta resolução ]      │  │
│  └────────────────────────────────────────────────────────┘  │
├──────────────────────────────────────────────────────────────┤
│  ↵ Colar  │ ⇧↵ Texto Puro │ Alt+1..9 Modificador │ F2 Apelido│ <- Rodapé de Atalhos e Dicas
└──────────────────────────────────────────────────────────────┘
```

---

## 2. Barra de Textos Rápidos & Modificadores

Localizada logo abaixo da caixa de busca, a barra de modificadores permite anexar introduções ou encerramentos a qualquer texto copiado antes de colar:

* **Ativação por Atalho (`Alt + 1` até `Alt + 9`):** Pressione `Alt + 1` para selecionar instantaneamente o primeiro modificador (ex.: *"Bom dia"*).
* **Indicador Visual:** O chip selecionado recebe realce azul vibrante e uma borda destacada.
* **Auto-Reset Inteligente:** Ao pressionar `Enter` para colar, o modificador é injetado no aplicativo de destino e a barra pode retornar automaticamente para o estado neutro.
* **Botão Limpar `[✕]`:** Remove a seleção ativa a qualquer momento.
* **Botão Gerenciar `[+]`:** Abre diretamente a aba de modificadores nas Configurações.

---

## 3. Central de Ajustes & Configurações (`SettingsWindow`)

Janela dedicada de **`820 × 630`** com design nativo Windows 11 Fluent, dividida em 7 seções com ícones vetoriais SVG e controles modernos:

```text
┌──────────────────────────────────────────────────────────────────────────────┐
│  ⚡ Configurações do ClipFlow                              [ Esc / ✕ Fechar ]│
├─────────────────────────┬────────────────────────────────────────────────────┤
│  PREFERÊNCIAS           │  Geral e Integração com o Sistema                  │
│                         │                                                    │
│  ⚙️  Geral & Sistema     │  ┌──────────────────────────────────────────────┐  │
│  ⌨️  Atalhos & Borda    │  │ Iniciar junto com o Windows             [●─] │  │
│  📋 Histórico & Captura │  └──────────────────────────────────────────────┘  │
│  💬 Textos Rápidos      │  ┌──────────────────────────────────────────────┐  │
│  🎨 Aparência & Tema    │  │ Ocultar ao clicar fora da janela        [●─] │  │
│  🔒 Privacidade         │  └──────────────────────────────────────────────┘  │
│  💾 Dados & Limpeza     │  ┌──────────────────────────────────────────────┐  │
│                         │  │ Manter janela sempre visível no topo    [●─] │  │
│                         │  └──────────────────────────────────────────────┘  │
├─────────────────────────┴────────────────────────────────────────────────────┤
│  ● Altere qualquer opção e clique em Salvar Alterações.  [Cancelar] [Salvar] │
└──────────────────────────────────────────────────────────────────────────────┘
```

### Seções Disponíveis:
1. **⚙️ Geral & Sistema:** Inicialização com o Windows, modo silencioso na bandeja, fechamento ao clicar fora e fixação no topo (*Always on Top*).
2. **⌨️ Atalhos & Borda:** Gatilho nas bordas da tela (*Edge Trigger*), delay de sensibilidade, atalho global do teclado, injeção de colagem automática e delay de foco.
3. **📋 Histórico & Captura:** Limite de itens normais salvos, filtros de captura (Textos, Imagens, Arquivos) e limite máximo de tamanho por texto.
4. **💬 Textos Rápidos (Modificadores):** Editor completo com título, posição (Prefixo/Sufixo), quebras de linha personalizadas, pré-visualização ao vivo e ordenação (▲/▼).
5. **🎨 Aparência & Tema:** Alternância entre Tema Escuro (Dark) e Claro (Light), posição de abertura na tela, dimensões da janela e opacidade.
6. **🔒 Privacidade & Senhas:** Bloqueio automático de gerenciadores de senhas, descarte de chaves privadas (`sk-`, `ghp_`, `AKIA`) e lista de programas bloqueados.
7. **💾 Dados & Limpeza:** Acesso rápido à pasta `%LOCALAPPDATA%/ClipFlow`, estatísticas de uso, limpeza de não-fixados e restauração de padrões.

---

## 4. Mapa Completo de Atalhos de Teclado

| Atalho | Ação |
| :--- | :--- |
| **`Ctrl + Shift + V`** *(ou `Alt + V`)* | Abre / Oculta a janela do **ClipFlow** em qualquer aplicativo. |
| **`↑` / `↓`** | Navega entre os itens da lista do histórico. |
| **`Enter`** *(ou Duplo Clique)* | **Cola imediatamente** o item selecionado no aplicativo onde você estava trabalhando. |
| **`Shift + Enter`** | **Cola como Texto Puro** (remove estilos, fontes, tabelas e tags HTML). |
| **`Alt + 1` até `Alt + 9`** | Ativa/desativa o **Texto Rápido / Modificador** de posição 1 a 9 na barra de chips. |
| **`Ctrl + P`** *(ou clicar no ícone 📌)* | **Fixa / Desafixa (Pin/Unpin)** o item selecionado. |
| **`F2`** *(ou `Ctrl + E`)* | Adiciona ou edita o **Apelido/Nota** do item selecionado. |
| **`Tab` / `Shift + Tab`** | Alterna entre as abas de categoria: *Tudo* → *Fixados 📌* → *Texto* → *Mídia*. |
| **`Delete`** *(ou `Ctrl + D`)* | Remove o item selecionado do histórico (itens fixados pedem confirmação). |
| **`Esc`** | Limpa a caixa de pesquisa (se preenchida) ou fecha o pop-up do ClipFlow. |
| **Digitação Direta** | Ao abrir a janela, qualquer letra digitada já pesquisa instantaneamente sem precisar clicar. |

> **Nota sobre `Ctrl + 1..9`:** O ClipFlow removeu intencionalmente os atalhos `Ctrl + 1..9` diretos para colagem de cards a fim de evitar conflitos com a alternância de abas em navegadores (Chrome, Edge) e IDEs (VS Code). A colagem de cards é feita com extrema rapidez através da tecla `Enter`.

---

## 5. Tipografia e Nitidez Visual (ClearType)

Para garantir máxima legibilidade tanto no tema Claro quanto no tema Escuro:
* **Família de Fontes:** `Segoe UI, Segoe UI Variable Text` (substituindo a variante *Display*, cujos traços finos causavam embaçamento em tamanhos pequenos).
* **Renderização Subpixel:** Ativação forçada de `RenderOptions.ClearTypeHint="Enabled"` na camada do cartão principal, permitindo que o motor de renderização do Windows desenhe os subpixels RGB com nitidez total mesmo em janelas com transparência ativa (`AllowsTransparency="True"`).
* **Fixação por Pixel:** `TextOptions.TextHintingMode="Fixed"` e `TextOptions.TextFormattingMode="Display"` para alinhamento exato na grade de pixels do monitor.
