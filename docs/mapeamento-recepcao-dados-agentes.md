# Mapeamento de Receção de Dados e Integração com Agentes - ISM

> Objetivo: Documentar **de onde vem os dados**, **por qual método são recebidos**, **como são captados e transformados**, e **qual entidade/agente do ISM consome a informação final** — garantindo auditabilidade e rastreabilidade (requisito da aceleradora).

---

## 0. Arquitetura Geral do Fluxo de Dados

```
┌──────────────────────────────────────┐     ┌─────────────────────────────┐     ┌───────────────────────────────┐
│         1. FONTE DE DADOS           │────▶│  2. CAMADA DE CAPTURA /      │────▶│  3. CAMADA DE TRANSFORMAÇÃO    │
│ (sistema ERP, planilha da gerente,  │     │      ADAPTADOR DE ENTRADA    │     │      e NORMALIZAÇÃO           │
│  e-mail XML da NF, PDV, etc.)       │     │  (CSV / Excel / XML / API)   │     │  (validação, merge, dedupe)   │
└──────────────────────────────────────┘     └─────────────────────────────┘     └───────────────┬───────────────┘
                                                                                                │
                                                                                                ▼
                                   ┌────────────────────────────────────────────────────────────────────────────┐
                                   │ 4. AUDITORIA / PROVENIÊNCIA (DataLineage)                                 │
                                   │    - Qual fonte? id único da importação                                   │
                                   │    - Quem subiu / qual API / qual agente?                                │
                                   │    - Hash SHA256 do arquivo original + data de recepção                   │
                                   └───────────────┬────────────────────────────────────────────────────────────┘
                                                   │
                                                   ▼
                                   ┌────────────────────────────────────────────────────────────────────────────┐
                                   │ 5. ENTIDADES ISM (MySQL)       ←──── 6. AGENTES / CONSUMIDORES              │
                                   │                               │       - IA de precificação inteligente     │
                                   │ • Products (estoque)          │       - IA de predição de consumo / venda  │
                                   │ • Fornecedores                │       - IA de sugestão de compra           │
                                   │ • Categories + Dishes         │       - Dashboard financeiro / BI           │
                                   │ • (futuras) Sales / Orders    │       - Alerta de estoque baixo             │
                                   └────────────────────────────────────────────────────────────────────────────┘
```

---

## 1. Lista de Fonte de Dados × Método de Entrada × Formato × Entidade ISM

Abaixo, **TABELA-MESTRE** auto-explicativa:

| #  | FONTE DE DADOS (ORIGEM)                         | MÉTODO DE ENTRADA                                      | FORMATO ARQUIVO / PAYLOAD       | ENTIDADE ALVO NO ISM (Modules)              | TIPO DE CAPTAÇÃO               | FREQ. TÍPICA | AGENTE / OUTPUT CONSUMIDOR                                                | AUDITORIA OBRIGATÓRIA? |
|----|--------------------------------------------------|--------------------------------------------------------|---------------------------------|----------------------------------------------|--------------------------------|--------------|---------------------------------------------------------------------------|------------------------|
| 1  | Planilha da cozinheira/gerente (estoque inicial)| Upload manual pelo gerente (web UI "Importar Estoque") | `.xlsx`, `.xls`, `.csv`         | Stock → **Product** + opcional Fornecedor   | Drag & drop no Frontend        | Mensal/Adhoc | Agente IA → Sugestão de reposição + Alerta estoque baixo                 | ✅ Sim                 |
| 2  | Sistema ERP do restaurante (ex: ContaAzul, SoftRestaurant) | Webhook cadastrado no ERP / Pull API agendado       | `JSON` REST (ou .csv export)    | Stock → Product; Fornecedores; Orders       | HTTPS POST (ERP → ISM) ou Job | Diário       | Agente IA → Previsão de demanda; BI dashboard de vendas                  | ✅ Sim                 |
| 3  | Nota Fiscal de Entrada (compra de mercadorias)  | Upload XML da NF-e (portal da SEFAZ / e-mail)         | **XML NF-e** (padrão nacional) + opcional `.pdf` | Stock → Product (ajuste entrada); Fornecedor; (futuro) AccountsPayable | Upload manual XML ou API email | Diário       | Agente IA → Custo médio atualizado (AverageCost Product); Valida NF vs pedido | ✅ Sim (NF é doc fiscal) |
| 4  | Relatório de vendas (saídas) vindo do PDV/iFood/Delivery | Export do sistema PDV ou CSV do ifood dash.         | `.csv`, `.xlsx`, ou JSON        | (futuro) **Sales / OrderItems**             | Upload ou SFTP ou API Pull    | Diário       | Agente IA → Ranking de pratos; Predição de vendas sazonais; Sugestão de alta de preço em pico | ✅ Sim |
| 5  | Cardápio exportado de app anterior              | CSV export do sistema legado                           | `.csv`                          | Menu → **Category**, **Dish**, DishIngredient | Upload ou POST API         | OneShot/Mês  | Agente IA → Custo ideal de prato; Sazonalidade; Sugestão de highlight   | ✅ Sim                 |
| 6  | Cadastro manual pelo usuário (Web UI)           | POST / PUT via Swagger / Frontend                      | JSON (DTOs)                     | Product, Dish, Category, Fornecedores, Users | HTTPS REST autenticado JWT | Contínuo     | Todos os agentes                                                          | ✅ Sim (log de usuário)|
| 7  | Lista de fornecedores em planilha Excel         | Upload manual pelo gerente (Importar Fornecedores)     | `.xlsx`, `.csv`                 | **Fornecedor**                               | Drag & drop Frontend           | Anual/Adhoc  | Agente IA → Comparar preço de 3 fornecedores no mesmo produto            | ✅ Sim                 |
| 8  | (Futuro) Balança integrada IoT                  | MQTT / WebSocket → Backend (Job agregador)             | JSON leitura contínua           | Product.CurrentQuantity                      | Tempo Real (15min snapshots) | Contínuo     | Alerta de estoque em tempo real; Predição de ruptura                     | ✅ Sim                 |

---

## 2. Rastreabilidade / Proveniência (Requisito Aceleradora)

**TODA** importação ou recepção de dados deve gravar um registro de **Auditoria / Lineage**.
Campos mínimos obrigatórios:

| Campo                          | Descrição                                                                 |
|--------------------------------|---------------------------------------------------------------------------|
| `ImportId` (GUID)              | ID único por lote de importação                                           |
| `DataSourceName`               | Nome da fonte (ex: "ERP ContaAzul", "NF-e #123987", "Planilha gerente.csv")|
| `DataSourceType`               | Enum: `Csv`, `Excel`, `XmlNfe`, `RestWebhook`, `RestApiPull`, `Manual`   |
| `SourceOriginalFilename`       | Nome do arquivo original (se upload) ou URL do endpoint                  |
| `SourceContentHashSha256`      | Hash do payload original — 100% auditável                                 |
| `ReceivedAtUtc`                | Data/hora recebimento                                                      |
| `ReceivedByUserId`             | Id do usuário que fez upload / agente (se automático)                    |
| `TotalRecordsInSource`         | Qtd linhas no arquivo / items na NF / eventos                            |
| `RecordsSucceeded`             | Quantidade inserida/atualizada com sucesso                                |
| `RecordsFailed`                | Quantidade com erro (grava motivo em `ImportErrorLog`)                   |
| `TargetEntity`                 | `Product`, `Fornecedor`, `Dish`, `Category`, `SalesOrder`, etc.          |
| `StrategyUpsert`               | `InsertOnly` ou `MergeById` ou `MergeByName+RestaurantId` (definido por adaptador) |
| `LineageSerialized` (opcional) | JSON listando quais IDs do banco vieram de quais linhas do arquivo       |

---

## 3. Estratégia de Transformação (CAPTAÇÃO → MAPEAMENTO de colunas)

Cada adaptador de entrada define um **Map de Colunas/XML paths → Campos da Entidade ISM**. Exemplos:

### 3.1 Planilha de Estoque (CSV / Excel) → Stock.Product

| Coluna exemplo planilha | Campo ISM Product            | Observação                                           |
|-------------------------|------------------------------|------------------------------------------------------|
| Código do Produto       | `Id` (opcional, Upsert)      | Se vazio → Insert novo                               |
| Nome do Item            | `Name`                       | Required; Trim; Único por RestaurantId+Nome          |
| Unidade de Medida       | `Unit`                       | `"kg"`, `"L"`, `"un"` — obrigatório                  |
| Quantidade Atual        | `CurrentQuantity`            | decimal (tratar vírgula BR e ponto como separador)   |
| Quantidade Mínima       | `MinimumQuantity`            | padrão 5 se vazio                                    |
| Custo Médio Unitário    | `AverageCost`                | Valor monetário (tratar R$ prefixo, mascaras)        |
| Nome do Fornecedor      | → busca/lookup Fornecedor    | Se não existir, criar como rascunho                  |
| Categoria (tag)         | `Category` (metadado futuro) | Pode ser usado na categorização de produto           |

### 3.2 XML NF-e (padrão oficial SEFAZ) → Product + Fornecedor

| XPath XML NF-e                          | Campo ISM                                | Observação fiscal                                             |
|-----------------------------------------|------------------------------------------|----------------------------------------------------------------|
| `/NFe/infNFe/Id`                        | `AuditImport.SourceOriginalFilename`     | Chave da NFe, único                                            |
| `/NFe/infNFe/emit/xNome`                | `Fornecedor.Name`                        | Emitente da NF → Fornecedor                                    |
| `/NFe/infNFe/emit/CNPJ`                 | `Fornecedor.???` (novo campo CNPJ? )     | Futuramente add CNPJ em Fornecedor se o grupo quiser          |
| `/NFe/infNFe/emit/enderEmit/*`          | `Fornecedor.Phone/Email` (tentar achar)  | Email e telefone da nota (se existir)                         |
| `/NFe/infNFe/det[]/prod/xProd`          | `Product.Name`                           | Nome do item                                                   |
| `/NFe/infNFe/det[]/prod/uCom`           | `Product.Unit`                           | Unidade (ex: CX, KG, UN)                                      |
| `/NFe/infNFe/det[]/prod/qCom`           | `Product.CurrentQuantity` (SOMAR entrada)| Ajuste positivo no estoque                                     |
| `/NFe/infNFe/det[]/prod/vUnCom`         | `Product.AverageCost` (atualizar média)  | Média ponderada nova entrada + estoque existente              |
| `/NFe/infNFe/ide/dhEmi`                 | `AuditImport.ReceivedAtUtc` (data doc)   | Data da NF (diferente de data de upload)                      |

### 3.3 CSV/iFood de Vendas → (futura) Sale / OrderItem

| Coluna CSV exemplo       | Campo (futuro) ISM Sale             | Observação                                 |
|--------------------------|-------------------------------------|-------------------------------------------|
| Pedido / Id              | `Sale.ExternalOrderId`               | Para evitar duplicata (UK RestaurantId+ExternalOrderId)|
| Data do Pedido           | `Sale.OrderedAtUtc`                  | Convertido para UTC                       |
| Item / Prato            | `OrderItem.Dish.Name` → lookup Dish  | Match por nome único + RestaurantId       |
| Qtd vendida             | `OrderItem.Quantity`                 |                                           |
| Valor Total Item        | `OrderItem.UnitPrice * Qtd`          |                                           |
| Taxa serviço / entrega  | `Sale.Surcharge`                     |                                           |
| Status pedido           | `Sale.Status` = `Paid`, `Canceled`   | Cancelado não soma no ranking             |

---

## 4. Arquitetura da Camada de Importação no Backend (Implementação sugerida)

### 4.1 Interfaces centrais (Adaptador Pattern)

```
IFileImporter
├── CsvProductImporter       (lê .csv planilha estoque)
├── ExcelProductImporter     (lê .xlsx planilha estoque)
└── XmlNfeImporter           (lê XML NF-e e faz upsert produto + fornecedor)
```

Método base:

```csharp
Task<ImportResult> ImportAsync(
    Stream payload,
    ImportContext context,   // restaurantId, userId, fileName, strategy
    CancellationToken ct);
```

Saída `ImportResult`:
- `TotalProcessed`, `Succeeded`, `Failed`, `NewIds[]`, `Errors[]`, `ImportId` (guid).

### 4.2 Controller de Importação (API)

| Endpoint                                 | Método | Autorização                  | Payload                     |
|------------------------------------------|--------|------------------------------|-----------------------------|
| `POST /api/import/stock/products`        | POST   | ManagerOrAbove               | `multipart/form-data` file  |
| `POST /api/import/fornecedores`          | POST   | ManagerOrAbove               | `multipart/form-data` file  |
| `POST /api/import/menu/dishes`           | POST   | ManagerOrAbove               | `multipart/form-data` file  |
| `POST /api/import/nfe/xml`               | POST   | ManagerOrAbove               | `multipart/form-data` XML   |
| `POST /api/integrations/erp/webhook`     | POST   | API Key / HMAC (header `X-Hmac-SHA256`) | JSON payload |
| `GET  /api/import/history`               | GET    | ManagerOrAbove (SuperAdmin all) | Lista últimos imports |
| `GET  /api/import/history/{importId}`    | GET    | ManagerOrAbove               | Detalhes + erros do lote    |

### 4.3 Responsabilidade de cada camada

| Camada ISM                | Responsabilidade                                                                 |
|---------------------------|----------------------------------------------------------------------------------|
| API (Controllers)         | Receber upload + [Authorize]; chamar `IImportOrchestrator`; devolver `ImportResult` com HTTP 202 (processing) se for grande |
| Application (Services)    | Orquestração: escolher adaptador por `file.ContentType`; chamar validator; gravar AuditImport + ErrorLogs |
| Infrastructure (Parsers)  | Implementar adaptadores CSV (CsvHelper), Excel (ClosedXML/EPPLus), XML (XDocument/System.Text.Json) |
| Domain                    | Interfaces `IImportAuditRepository`, `IFileImporter<TTarget>`; Enums `DataSourceType` |
| Migrations                | Criar tabelas `ImportAudits`, `ImportErrorLogs` + índice `(RestaurantId, ImportId)` |

---

## 5. Mapeamento de Dados de Saída (ISM → Agentes / Consumidores)

Depois que os dados entram e ficam limpos no banco, os **agentes/relatórios** consomem da forma abaixo.
(pode anexar esse quadro no documento para a aceleradora ver que tem saída definida também).

| CONSUMIDOR / AGENTE                  | DADOS DE ENTRADA (lê do ISM)                 | MÉTODO DE CONSUMO            | OUTPUT / AÇÃO GERADA (SAÍDA)                                  |
|--------------------------------------|----------------------------------------------|------------------------------|----------------------------------------------------------------|
| Agente IA — Predição de Vendas       | Sales últimos 6m + Dish categories + eventos calendário | REST interno `/api/bi/sales-aggregates` (JSON) | `SugestaoAltaPrecos` list; Forecast 30 dias (JSON) → salva em `AgentRecommendations` |
| Agente IA — Reposição de Estoque     | Product (Current, Min, AvgCost) + Sales forecast | Join DB direto (Service)    | Alerta `estoque baixo` por email/WhatsApp + SUGESTÃO DE ORDEM DE COMPRA com 3 fornecedores |
| Agente IA — Custo Prato Ideal        | Dish.Ingredients → Product.AverageCost        | SQL join Dish+Ingredient+Product | Sugere novo `Dish.Price` + `Dish.Cost` atualizado toda semana |
| Dashboard BI (Frontend tela)         | Sales aggregation, Stock por categoria        | GET `/api/bi/*` JSON         | Tabelas + Gráficos (ChartJS)                                  |
| (Futuro) Exportação para Contador    | Fornecedores, NFs importadas, vendas mês      | GET `/api/exports/accounting-csv` | .zip com CSVs mês (ENVIAR EMAIL) |

---

## 6. Checklist para validar com a aceleradora / grupo

- [x] **Fonte claramente identificada** por importação (NF-e, ERP, planilha, XML, manual)
- [x] **Rastreabilidade**: Origem (nome arquivo + SHA256) + usuário + horário para TODO dado inserido
- [x] **Vendas**: mapeado caminho de entrada (CSV ifood, webhook ERP, export PDV) e tabela alvo (Sales)
- [x] **Estoque**: mapeado caminho de entrada (planilha gerente, XML NF-e, balança IoT, ERP) → Product
- [x] **Duplicatas**: estratégia `Upsert por RestaurantId+Name` (produto) / `CNPJ` (fornecedor) / `ExternalOrderId` (venda)
- [x] **Falhas**: lote importação grava `RecordsFailed` + motivo em `ImportErrorLogs` (não deixa dado "sumiço")
- [x] **Segurança**: upload `ManagerOrAbove`; webhook ERP com HMAC (exige header assinado); XML NF validado XSD oficial
- [x] **Consumidores definidos**: qual agente lê qual dado e qual output gera (não dado "sem dono")

---

## 7. Exemplos de arquivos modelo que o grupo pode testar (CSV)

### 7.1 `estoque_ism_template.csv`
```csv
Nome,Unidade,QuantidadeAtual,QuantidadeMinima,CustoMedio,NomeFornecedor
Arroz Branco Tipo 1,kg,120.5,25,4.89,Distribuidora Alimentos Ltda
Feijão Carioca,kg,80,20,7.20,Distribuidora Alimentos Ltda
Tomate Cereja,kg,15,5,9.50,Hortifruti Central
Picanha Bovina,kg,10,4,58.90,
```

### 7.2 `fornecedores_ism_template.csv`
```csv
Nome,Categoria,Descricao,Email,Telefone
Carnes Premium S.A.,Carnes,Fornecedor líder em cortes nobres,vendas@carnespremium.com.br,(11) 4002-8922
Bebidas Centrais,Bebidas,"Distribuidora de refrigerantes, cervejas e sucos em geral",pedidos@bebidasc.com,(11) 99888-1010
```

---

> Documento vivo: atualizar essa tabela sempre que surgir uma nova fonte de dados ou novo agente no projeto.
