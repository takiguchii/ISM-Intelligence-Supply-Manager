# Fluxo de comandas

## Objetivo

Implementar o ciclo completo de uma comanda, desde o envio do pedido pelo garçom até o recebimento no caixa, com persistência no backend, agrupamento de rodadas por mesa, proteção contra pagamentos duplicados e contabilização administrativa.

Base de desenvolvimento: `2a2fe85` (`origin/develop`).

Branch: `feat/fullstack/fluxo-comandas`.

## Causa principal encontrada

O caixa renderizava a fila usando `GET /api/orders`, criando um cartão para cada pedido. O backend, porém, já tratava o recebimento no nível de comanda (`Tab`). Assim, duas rodadas da mesma mesa apareciam em cartões separados, embora o pagamento pudesse liquidar as duas.

Também foi identificado um problema de ambiente: o container `ism-comandas-backend-1` estava servindo um assembly compilado antes da alteração que tornava `tableReference` opcional. Por isso, `GET /api/orders/tabs/open` sem query string retornava `400`, com validação automática em `errors.tableReference`.

## Backend

### Comandas e persistência

- Criada a entidade `Tab`, persistida na tabela `order_tabs`.
- Adicionado `Order.PaidTabId` para vincular cada pedido pago à comanda que o liquidou.
- Criada a migration `AddOrderTabs`.
- A mesma mesa pode abrir uma nova comanda depois que a anterior é paga.
- Pedidos pagos não podem ser editados ou excluídos.

### Endpoints

#### Listar comandas abertas

```http
GET /api/orders/tabs/open
```

Resposta resumida:

```json
[
  {
    "id": 14,
    "tableReference": "Mesa 5 Raul",
    "total": 242.80,
    "canPay": true,
    "orderIds": [371, 372]
  }
]
```

Também é possível consultar uma mesa específica:

```http
GET /api/orders/tabs/open?tableReference=Mesa%205%20Raul
```

#### Receber uma comanda

```http
POST /api/orders/tabs/{tabId}/payments
```

Exemplo:

```json
{
  "idempotencyKey": "uuid-da-operacao",
  "paymentMethod": 3,
  "total": 242.80
}
```

O endpoint é autorizado para `Cashier`, `Manager` e `Admin`.

### Atomicidade e retry do MySQL

O MySQL utiliza `MySqlRetryingExecutionStrategy`. Uma transação manual fora da estratégia causava:

> MySqlRetryingExecutionStrategy does not support user-initiated transactions.

O fluxo atual executa dentro de `Database.CreateExecutionStrategy().ExecuteAsync(...)`:

1. Limpa o `ChangeTracker` para permitir novas tentativas.
2. Lê a comanda e os pedidos pendentes.
3. Valida estado, total, restaurante e chave de idempotência.
4. Abre a transação serializável.
5. Marca a comanda como paga.
6. Marca todos os pedidos concluídos como `Paid` e grava `PaidTabId`.
7. Salva e faz commit.

Uma repetição com a mesma chave retorna o mesmo recibo. Outra operação concorrente recebe conflito ou o mesmo resultado idempotente, conforme a chave utilizada.

## Frontend do caixa

`frontend/pages/caixa.vue` passou a:

- carregar `orderService.list()` para os detalhes dos itens;
- carregar `tabService.listOpen()` para montar a fila;
- renderizar um cartão por comanda, e não por pedido;
- exibir todos os números de pedido da comanda;
- exibir o total agregado;
- permitir uma única ação `Receber` por comanda;
- bloquear recebimento quando ainda há pedidos em preparo;
- enviar o pagamento para o backend, em vez de persistir o recebimento apenas no `localStorage`.

Os turnos e comprovantes locais continuam no navegador, mas o status dos pedidos, o fechamento da comanda e os indicadores são persistidos no servidor.

## Dashboard e histórico

Os indicadores agrupam pedidos pagos por `PaidTabId`. Dessa forma, duas rodadas de uma mesa representam uma única venda/comanda, embora o valor dos itens de ambas as rodadas seja somado ao faturamento.

Após o pagamento:

- a comanda deixa de aparecer em `/caixa`;
- os pedidos permanecem disponíveis no histórico;
- a mesa pode receber novos pedidos em uma nova comanda;
- a venda é contabilizada uma única vez.

## Correções adicionais

- Restaurada a referência compartilhada de normalização no filtro de pedidos do garçom, eliminando `ReferenceError: normalize is not defined` em `/pedidos`.
- Preservada a busca de pratos por ingrediente.
- Adicionada cobertura para pesquisa de pedidos por mesa e número.
- A fila da cozinha continua recebendo mesa, itens, quantidades e observações.

## Validações executadas

### Testes automatizados

- Testes frontend: 6 aprovados.
- Testes unitários .NET: 288 aprovados.
- Testes de integração .NET: 78 aprovados na suíte completa.
- Testes específicos de comandas: 12 aprovados.
- Teste de arquitetura: 1 aprovado.
- Build Nuxt: aprovado.
- Build .NET: aprovado quando o cache NuGet estava disponível.

### MySQL real no Docker

No ambiente `docker compose -p ism-comandas`:

- os pedidos `#371` e `#372` foram encontrados em `Mesa 5 Raul`;
- a fila retornou uma única comanda, total `R$ 242,80`;
- um pagamento marcou os dois pedidos como `Paid`;
- a comanda foi removida da fila;
- uma repetição idempotente retornou o mesmo recibo;
- duas confirmações concorrentes em outra comanda retornaram o mesmo recibo e persistiram apenas um pagamento.

Dados observados para a comanda da Mesa 5 Raul:

```text
Tab: 14
Pedidos: 371, 372
Total: 242,80
PaidTabId dos pedidos: 14
```

O valor persistido no MySQL para essa venda foi `R$ 242,80`, sem duplicação.

## Arquivos principais

- `backend/src/ISM.Domain/Modules/Orders/Tab.cs`
- `backend/src/ISM.Domain/Modules/Orders/Order.cs`
- `backend/src/ISM.API/Controllers/Orders/OrdersController.cs`
- `backend/src/ISM.Infrastructure/Repositories/Orders/OrderRepository.cs`
- `backend/src/ISM.Application/Services/Dashboard/DashboardService.cs`
- `frontend/pages/caixa.vue`
- `frontend/services/modules/orders/tabService.ts`
- `backend/tests/ISM.IntegrationTests/TabPaymentTests.cs`

## Observações de ambiente

- O container backend precisou ser reiniciado para carregar o assembly atualizado.
- O código é montado por volume no container e compilado pelo `dotnet watch`.
- Em algumas execuções no host, o build .NET ficou bloqueado por pacotes NuGet ausentes no cache local; o restore resolveu o problema quando executado com acesso permitido.
- Não foram usados comandos destrutivos nem recriados os volumes do MySQL.
