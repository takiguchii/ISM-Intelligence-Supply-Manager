# Kit de testes — Importação de dados ISM

## ✅ Devem funcionar AGORA (CSV)

| Arquivo | O que testa | Resultado esperado na UI |
|---|---|---|
| `estoque-valido.csv` | Caminho feliz estoque | Categoria "estoque" verde, 10 linhas prontas |
| `fornecedores-validos.csv` | Caminho feliz fornecedores | Categoria "fornecedores", 6 linhas |
| `financas-validas.csv` | Categoria Finanças | Card "Em breve" (persistência futura) com 10 lançamentos |
| `planilha-mista.csv` | Colunas de 2 categorias misturadas | Detecta estoque + finanças no mesmo arquivo |
| `com-colunas-extras.csv` | Colunas não reconhecidas | Estoque ok + "ObservacaoGerente, Setor, UltimaContagem" em "Colunas não reconhecidas" |
| `com-erros.csv` | Validação por linha | Nome vazio (linha 3), email inválido (linha 4), número inválido (linha 5) destacados em âmbar |

## 🧪 Formatos ainda NÃO suportados pelo parser (comportamento: "não foi possível identificar categoria")

| Arquivo | Motivo |
|---|---|
| `estoque.xlsx` | XLSX real e válido — abre no Excel! Parser binário chega com o ExcelImporter (ClosedXML) |
| `nfe-exemplo.xml` | NF-e estruturada p/ implementação futura (XPaths já mapeados nos docs) |
| `planilha-legado.xml` | SpreadsheetML que abre no Excel |
| `webhook-vendas.json` | Payload simulado de webhook ERP |

> ⚠️ Enviar esses hoje deve mostrar o card vermelho "Não foi possível identificar nenhuma categoria" — é o comportamento correto por enquanto.

## 📷 Foto / PDF

Fotografe uma página de caderno com tabela (ou um print) e envie — exige `PHOTO_IMPORT__APIKEY` configurada no backend.

## 📦 ZIPs

- `kit-completo.zip` — todos os arquivos acima
- `csvs-validos.zip` — só os 3 CSVs de caminho feliz

**Atenção:** o sistema não aceita .zip no upload — extraia antes e envie arquivo a arquivo.
