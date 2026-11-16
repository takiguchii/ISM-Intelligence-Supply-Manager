# Cozinha integrada ao garçom

## Criar o acesso do chef

1. Entre como gerente do restaurante e abra **Funcionários**.
2. Cadastre nome, e-mail, senha e o cargo **Chef / Cozinha**.
3. O funcionário entra na tela de login com essas credenciais e é direcionado
   para `/chef`. O cadastro não troca a sessão do gerente pela do funcionário.
4. O acesso pertence ao mesmo restaurante do gerente. Para bloquear o acesso,
   edite o funcionário e altere o status para **Inativo**.

Com o seed de desenvolvimento habilitado, o acesso de demonstração continua
sendo `chef@ism.com.br`, com a senha configurada localmente em `SEED_PASSWORD`.

## Operação

- **Início / Cozinha:** painel com as etapas Recebidos, Em preparo e
  Prontos para retirada. Mostra mesa/comanda, número, horário local, quantidades
  e observações. Ordena os pedidos mais antigos primeiro e permite busca por
  mesa, número, prato ou observação, inclusive sem acentos.
- **Pedidos:** consulta de todos os estados, incluindo entregues, pagos e
  cancelados, com busca, filtro de situação e consulta dos itens e observações.
- **Receber:** pedidos criados pelo garçom no mesmo restaurante aparecem
  automaticamente. A consulta ocorre ao abrir a tela, a cada dez segundos
  enquanto a aba está visível e ao retornar à aba. Há atualização manual.
- **Preparar:** `Iniciar preparo` muda Submitted para Preparing.
- **Despachar:** `Despachar para o salão` muda Preparing para Ready. O pedido
  fica disponível para retirada e aparece como Pronto para o garçom.
- **Entregar:** o garçom confirma a entrega, mudando Ready para Completed.
  Na próxima consulta, o pedido sai da fila da cozinha. O despacho pelo chef
  não registra entrega nem pagamento.
- **Cardápio:** consulta real de pratos, descrições, preços, categorias e
  disponibilidade, com busca, filtro e atualização. O chef não edita o cardápio.

O chef não cria pedidos, confirma entregas, registra pagamentos, cancela
pedidos, gerencia funcionários ou acessa o financeiro. A API preserva as
permissões e o isolamento entre restaurantes, além das restrições da interface.

## Implementação

- `KitchenPanel` apresenta a fila, `KitchenOrderCard` apresenta cada pedido e
  `KitchenHistory` permite consultar todos os estados sem oferecer mutações.
- `useKitchen` coordena leitura, polling e transições com `orderService`.
  Usa a versão atual, bloqueia cliques repetidos, recarrega após conflitos e
  impede que uma consulta antiga desfaça uma atualização. O polling e o evento
  de visibilidade são removidos ao desmontar.
- `KitchenMenu` usa `useMenuCatalog` e `menuService` para consulta somente
  leitura. Nenhum cadastro, estado fictício ou acesso direto ao banco é usado.
- Cadastro, autenticação, políticas e estados do backend existentes foram
  reutilizados. Não houve alteração de schema nem de migrations.

## Validação — 12/11/2026

- Sete testes novos de frontend: fila ativa e ordenação, versões de preparo e
  despacho, clique duplicado, consulta atrasada, recuperação de conflito,
  polling/visibilidade/desmontagem, falha de rede e cardápio real.
- Teste de integração com chef e garçom novos cadastrados pelo gerente:
  autenticação, consulta do cardápio, pedido compartilhado, preparo, despacho,
  entrega pelo garçom, acesso proibido, conflito de versão, isolamento de outro
  restaurante e bloqueio de login após inativação do chef.
- Suíte completa: 59 testes unitários, 25 de integração e 1 de arquitetura no
  backend; 15 testes de frontend e validação de navegação por perfil.
- Builds do backend e do frontend aprovados; `git diff --check` aprovado.
- MySQL real: conta Chef criada pelo gerente, login, consulta e bloqueios;
  pedido `#366` enviado pelo garçom e recebido automaticamente no navegador.
  O chef iniciou preparo e despachou pela interface. A API do garçom consultou
  o mesmo pedido pronto e confirmou a entrega; a fila foi conferida sem ele.
  A conta e o pedido exclusivos desse teste foram removidos ao final.
- Navegador: cardápio real, busca sem acentos, filtro de categoria, bloqueio de
  `/funcionarios`, redirecionamento do login autenticado e layout mobile de
  390 × 844 sem transbordamento horizontal.

A confirmação da entrega foi posteriormente substituída por um modal próprio
compartilhado, com suporte a tema claro/escuro e mobile. O fluxo de voltar,
fechar por Escape e confirmar a entrega foi validado no navegador, incluindo
a persistência do status no banco. Isso resolve a limitação da confirmação
nativa registrada na revisão anterior. A mesma apresentação é usada nas
confirmações de pagamento, cancelamento e exclusão do sistema.
O polling ainda consulta a listagem completa de pedidos; para grandes volumes,
convém evoluir para uma consulta incremental ou apenas de pedidos ativos.
