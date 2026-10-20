# 03. Modelo de Dados, Fixação (Pin) e Regras de Negócio

Este documento detalha a estrutura de dados do **ClipFlow**, as regras do sistema de **Fixação (Pin)**, deduplicação, classificação inteligente e proteção de privacidade.

---

## 1. Entidade Principal: `ClipItem`

Cada item capturado da área de transferência é representado pela seguinte estrutura:

| Campo | Tipo | Descrição |
| :--- | :--- | :--- |
| `Id` | `string` (GUID) | Identificador único do registro. |
| `Kind` | `ClipKind` | Tipo primário: `Text`, `Image`, `Files`. |
| `SubKind` | `ClipSubKind` | Subtipo detectado: `PlainText`, `Url`, `Email`, `HexColor`, `FilePath`, `Code`. |
| `TextContent` | `string?` | Texto completo copiado (ou lista de caminhos separados para `Files`). |
| `PreviewText` | `string` | Versão resumida/truncada (até 300 caracteres, sem quebras excessivas) para renderização rápida na UI. |
| `HtmlContent` | `string?` | Conteúdo HTML opcional quando copiado de navegadores/editores ricos. |
| `RtfContent` | `string?` | Conteúdo RTF opcional. |
| `ImagePath` | `string?` | Caminho relativo da imagem salva em disco (`%LOCALAPPDATA%/ClipFlow/images/{hash}.png`). |
| `ContentHash` | `string` (SHA-256) | Hash único do conteúdo para evitar itens duplicados. |
| `IsPinned` | `bool` | **Indica se o item está FIXADO para acesso rápido.** |
| `PinOrder` | `int` | Ordem customizada entre os itens fixados. |
| `TitleOrNote` | `string?` | Apelido/título opcional (ex.: *"Assinatura de Email"*, *"PIX da Empresa"*, *"Comando Docker"*). |
| `SourceAppName` | `string?` | Nome do aplicativo de onde o conteúdo foi copiado (ex.: `chrome`, `Code`, `notepad`). |
| `UseCount` | `int` | Quantidade de vezes que o item foi colado pelo usuário. |
| `CharacterCount` | `int` | Tamanho do texto ou dimensões da imagem (`LxA`). |
| `CreatedAt` | `DateTime` | Data/hora da primeira captura. |
| `UpdatedAt` | `DateTime` | Data/hora da última cópia ou uso. |

---

## 2. Regras do Sistema de Fixação (Pin) 📌

O grande diferencial do **ClipFlow** é tornar os itens fixados extremamente rápidos de acessar e organizar:

1. **Proteção Permanente:**
   * Itens com `IsPinned = true` **nunca expiram** e **nunca são removidos** quando o histórico atinge o limite máximo (ex.: 200 itens) ou quando o usuário clica em *"Limpar Histórico"*.
2. **Acesso Rápido na Interface:**
   * Na aba **"Todos"**, os itens podem ser visualizados com destaque (ícone 📌 colorido) ou filtrados instantaneamente na aba dedicada **"Fixados 📌"** (acessível com `Tab` ou `Alt + 2`).
3. **Apelidos / Etiquetas Rápidas (`TitleOrNote`):**
   * O usuário pode dar um título curto a um item fixado (ex.: *"Meu CPF"*, *"Link do LinkedIn"*, *"Resposta Padrão Cliente"*).
   * A barra de busca pesquisa tanto pelo **conteúdo** quanto pelo **título/apelido** do item fixado!
4. **Comportamento ao Recopiar ou Usar:**
   * Quando um item normal é copiado novamente, seu `UpdatedAt` vai para o topo do histórico.
   * Quando um item fixado é usado, ele incrementa o `UseCount` e mantém sua posição privilegiada.

---

## 3. Regra de Deduplicação Inteligente (SHA-256)

Para evitar que copiar o mesmo texto várias vezes polua o menu:
1. Ao capturar um novo texto ou imagem, o **ClipFlow** calcula o `ContentHash` (SHA-256 do texto normalizado ou dos bytes da imagem).
2. Verifica se já existe um item com o mesmo `ContentHash`:
   * **Se já existir:** Não cria um item duplicado. Apenas atualiza o `UpdatedAt = DateTime.UtcNow`, move o item para o topo do histórico recente e preserva seu status `IsPinned` e `TitleOrNote`.
   * **Se não existir:** Insere o novo registro no topo da lista.

---

## 4. Detecção Automática de Subtipos (`ContentDetector`)

Para enriquecer a visualização no estilo moderno:
* **Cor (`HexColor`):** Se o texto fizer match com `^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$` ou `^rgba?\(.*\)$`, a interface exibe um **círculo/quadrado pintado com a cor real** ao lado do código hexadecimal.
* **Link (`Url`):** Se iniciar com `http://`, `https://` ou `www.`, exibe badge de Link `🔗`.
* **E-mail (`Email`):** Se corresponder ao padrão `usuario@dominio.ext`, exibe badge `✉️`.
* **Código (`Code`):** Se contiver múltiplos indicadores de código (`{`, `}`, `=>`, `function`, `public class`, `SELECT `, `import `), exibe badge `</>`.
* **Caminho (`FilePath`):** Se for um caminho absoluto válido do Windows (`C:\...`), exibe badge `📁`.

---

## 5. Regras de Privacidade e Segurança (`PrivacyGuard`)

1. **Flags Nativas de Gerenciadores de Senha:**
   * Se o `IDataObject` contiver `ExcludeClipboardContentFromMonitorProcessing` ou `CanIncludeInClipboardHistory == 0`, o ClipFlow **ignora imediatamente** a captura.
2. **Lista de Aplicativos Ignorados (`ExcludedApps`):**
   * Por padrão, ignora processos conhecidos de cofres de senha (`1Password`, `Bitwarden`, `KeePass`, `KeePassXC`, `LastPass`, `Dashlane`). O usuário pode adicionar qualquer app à lista.
3. **Limites Seguros de Memória:**
   * Textos maiores que **1 MB** ou imagens maiores que **20 Megapixels / 15 MB** são ignorados ou truncados com segurança para manter o uso de RAM sempre baixo.
