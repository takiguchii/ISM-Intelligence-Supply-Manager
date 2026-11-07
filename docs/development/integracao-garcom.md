# Integração da área do garçom

Base: develop, commit 8de3969892c153cbcfecee38f385a4609995b327.
Branch: feat/frontend/integracao-garcom.

## Resumo simples

As telas do garçom passam a buscar o cardápio e os pedidos no backend. Enviar um pedido grava os dados pela API; confirmar entrega atualiza o pedido salvo. Os pedidos deixam de desaparecer ao recarregar a página. O rascunho do carrinho continua temporário: é compartilhado entre rotas, mas não é salvo no banco nem sobrevive a um recarregamento completo.

## Alterações e motivos

| Arquivo | Alteração | Motivo |
| --- | --- | --- |
| frontend/composables/useWaiter.ts | Novo composable com consultas e mutações usando menuService e orderService | Separar comunicação/estado da apresentação e aproveitar os contratos existentes |
| frontend/composables/useWaiterMock.ts | Removido | Eliminar pratos, pedidos e identificadores locais fictícios |
| frontend/components/waiter/WaiterPanel.vue | Substitui WaiterMock.vue, preservando a interface; ações assíncronas, atualização manual, mensagens e bloqueio durante envio | Usar dados reais e informar falhas sem perder o carrinho |
| frontend/pages/garcom.vue, atendimento.vue, pedidos.vue e cardapio.vue | Referências ao componente real | Integrar as quatro telas do perfil Waiter |
| frontend/tests/unit/waiter.test.cjs | Seis testes da lógica do composable com serviços simulados | Validar falhas, duplicação, concorrência e status sem depender de um banco externo |
| frontend/package.json | Comando npm run test:waiter | Facilitar a execução dos testes |

O backend, as migrations, os serviços HTTP existentes e as telas específicas dos outros perfis não foram alterados.

## Funcionamento detalhado

- Cardápio: GET /api/menu/dishes e GET /api/menu/categories; categoria resolvida pelo categoryId. Pratos inativos permanecem visíveis com adição bloqueada. Usa urlImage com imagem padrão como alternativa.
- Pedidos: GET /api/orders no carregamento, no botão Atualizar dados e a cada 10 segundos enquanto a aba está visível. O timer é encerrado ao desmontar o componente. Não há WebSocket.
- Criação: POST /api/orders com requestId UUID, tableReference e itens (dishId, quantity, observation). Nome, preço e custo oficiais continuam sendo definidos pelo backend.
- Repetição: falha não limpa o carrinho. Um mesmo conteúdo preserva o UUID nas tentativas seguintes; alterar mesa/itens/observações gera outro UUID. Essa proteção vale durante a sessão de navegação, não após recarregar a página.
- Validação: mesa de até 100 caracteres, de 1 a 100 linhas, quantidade inteira entre 1 e 1000 e observação de até 500 caracteres. O backend continua sendo a autoridade final.
- Entrega: PUT /api/orders/{id}/status com Completed e a versão atual do pedido. Em caso de falha, consulta novamente os pedidos, mantendo a mensagem do erro original.
- Concorrência: consultas iniciadas antes de uma criação/entrega não podem sobrescrever o resultado mais recente da ação.
- Rascunho: estado compartilhado por usuário e restaurante para evitar mistura de carrinhos entre identidades. Envio bloqueia alterações e cliques repetidos.
- Status: Enviado, Em preparo, Pronto, Entregue, Pago e Cancelado. Somente os três primeiros contam como ativos e mesas em atendimento. Essa contagem representa pedidos ainda em preparo/entrega, não ocupação física ou mesas com pagamento pendente.
- Permissões: removida a simulação de cozinha; o garçom confirma entrega somente de pedidos prontos. A API mantém suas regras de autorização e isolamento por restaurante.
- Histórico: Pago e Cancelado podem ser consultados desmarcando Somente ativos. Não foram adicionados botões de pagamento ou cancelamento nesta entrega.

## Verificação realizada

- npm ci --no-audit --no-fund: concluído, usando o lockfile existente.
- npm run build: aprovado (Nuxt, cliente e servidor).
- npm run test:waiter: seis testes aprovados.
- node --experimental-strip-types tests/unit/navigation.test.ts: aprovado.
- git diff --check: aprovado.
- Comparação com develop: base permanece no commit informado acima na revisão realizada.

Os testes do composable usam Vue real, serviços substituídos e ciclo de vida controlado. Cobrem categorias e pratos inativos, seis status, UUID em novas tentativas, preservação/limpeza do carrinho, bloqueio de envio duplicado, versão da entrega, recuperação de conflito, proteção contra consultas antigas, encerramento do timer e validação antes do envio.

Limitações da verificação: Docker e .NET não estão instalados neste ambiente; não foi possível executar o backend, MySQL ou OrderFlowTests. A tentativa de teste visual com Chromium foi impedida pelo ambiente (criação de socket não permitida). Portanto, o fluxo entre dois usuários com backend real e a inspeção visual desktop/mobile devem ser executados localmente antes de integrar à develop. O build não substitui esses testes. Não existe script lint no package.json atual.

## Como testar localmente

1. Com seu trabalho local salvo, buscar a branch:

   ```bash
   git fetch origin
   git switch --track origin/feat/frontend/integracao-garcom
   ```

   Se ela já existir localmente, usar git switch feat/frontend/integracao-garcom.

2. Manter seu .env local; caso ainda não exista, usar .env.example como referência. Subir a aplicação conforme o setup do projeto:

   ```bash
   docker compose up --build
   ```

3. Entrar como Waiter vinculado a um restaurante com pratos/categorias cadastrados. Em outra janela/perfil do navegador, entrar como Chef do mesmo restaurante.
4. No atendimento do garçom, conferir pratos, categorias, busca e indisponibilidade. Informar mesa, adicionar pratos, alterar quantidades e observações.
5. Navegar para Cardápio e voltar: o rascunho deve continuar. Revisar e enviar: o botão deve ficar bloqueado durante a requisição; somente o sucesso limpa o carrinho.
6. Conferir o pedido no garçom e na cozinha. No chef, iniciar preparo e marcar pronto. No garçom, aguardar até 10 segundos ou usar Atualizar dados.
7. Confirmar a entrega e verificar que o pedido sai dos ativos; desmarcar Somente ativos para encontrá-lo como Entregue. Recarregar a página e conferir a persistência.
8. Simular indisponibilidade de rede ao enviar: a mensagem deve aparecer e o carrinho permanecer. Restaurar a conexão e repetir sem mudar o conteúdo; verificar que não foram criados dois pedidos.
9. Alterar o mesmo pedido em duas janelas: uma versão antiga deve gerar mensagem e atualização da listagem, sem fingir que a ação teve sucesso.
10. Conferir em largura mobile o cardápio, abertura do carrinho, revisão e mensagens de erro.
11. Opcionalmente executar os testes do frontend:

    ```bash
    cd frontend
    npm ci
    npm run test:waiter
    node --experimental-strip-types tests/unit/navigation.test.ts
    npm run build
    ```

Nenhum PR foi aberto. A branch foi criada localmente. A publicação remota foi bloqueada: o conector GitHub retornou 403 (Resource not accessible by integration) e o git push local não dispõe de credenciais. O pacote de entrega inclui um Git bundle para importar a branch no clone do usuário.
