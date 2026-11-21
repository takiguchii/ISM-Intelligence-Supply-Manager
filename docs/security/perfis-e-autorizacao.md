# Perfis, usuários e autorização no ISM

Este documento é a referência operacional dos perfis do ISM: quem pode entrar, qual é a responsabilidade de cada cargo e quais views/endpoints estão disponíveis.

## Credenciais de demonstração local

Quando `SEED_DEMO_DATA=true`, o backend cria os usuários abaixo de forma idempotente. Todos usam o valor de `SEED_PASSWORD`; se essa variável não for informada em ambiente de desenvolvimento, o fallback atual é `admin123`.

| Perfil | E-mail de demonstração | Senha local padrão | Home |
| :--- | :--- | :--- | :--- |
| Administrador (`Admin`) | `admin@ism.com.br` | valor de `SEED_PASSWORD` ou `admin123` | `/` |
| Gerente (`Manager`) | `gerente@ism.com.br` | valor de `SEED_PASSWORD` ou `admin123` | `/` |
| Chef / Cozinha (`Chef`) | `chef@ism.com.br` | valor de `SEED_PASSWORD` ou `admin123` | `/chef` |
| Garçom / Atendimento (`Waiter`) | `garcom@ism.com.br` | valor de `SEED_PASSWORD` ou `admin123` | `/garcom` |
| Caixa (`Cashier`) | `caixa@ism.com.br` | valor de `SEED_PASSWORD` ou `admin123` | `/caixa` |

Essas credenciais são exclusivamente para desenvolvimento/demo. Em qualquer ambiente compartilhado ou produtivo, configure uma senha forte em `SEED_PASSWORD`, altere as senhas depois do primeiro acesso e não reutilize a senha de demonstração.

## Perfis e responsabilidades

| Perfil | Escopo | Pode fazer | Deve fazer | Não deve fazer |
| :--- | :--- | :--- | :--- | :--- |
| `Admin` | Plataforma, quando sem `restaurantId`; restaurante, quando vinculado | Administrar a configuração compatível com seu escopo, usuários e permissões administrativas | Manter tenant, segurança e responsáveis do restaurante | Operar caixa como rotina; o Caixa é um perfil separado |
| `Manager` | Restaurante | Dashboard, financeiro, pedidos administrativos, estoque, fornecedores, cardápio, integrações, configurações e equipe | Coordenar operação, compras, custos, metas e colaboradores | Receber pagamentos no módulo exclusivo do Caixa |
| `Chef` | Restaurante | Cozinha/KDS, pedidos de produção e consulta de cardápio | Atualizar preparo, disponibilidade e despacho da cozinha | Acessar financeiro, estoque administrativo ou caixa |
| `Waiter` | Restaurante | Atendimento, comandas, pedidos e consulta de cardápio | Registrar pedidos corretamente e acompanhar o atendimento do salão | Editar cadastros administrativos ou receber pagamentos no Caixa |
| `Cashier` | Restaurante | Somente a view `/caixa`, consulta de pedidos concluídos, turnos, movimentações e comprovantes locais | Conferir pedido, forma de pagamento, troco e fechamento do turno | Criar/alterar pedidos, acessar estoque, financeiro, equipe, fornecedores, integrações ou configurações |

`Admin` é um único role técnico. O escopo é definido pela claim `restaurantId`: sem essa claim é Super Admin da plataforma; com ela é administrador daquele restaurante. O usuário seed `admin@ism.com.br` é criado vinculado ao restaurante demo.

## Acesso às views

| View | Admin | Manager | Chef | Waiter | Cashier |
| :--- | :---: | :---: | :---: | :---: | :---: |
| Dashboard `/` | Sim | Sim | Não | Não | Não |
| Financeiro `/financeiro` | Sim* | Sim | Não | Não | Não |
| Caixa `/caixa` | Não | Não | Não | Não | Sim |
| Pedidos `/pedidos` | Sim | Sim | Sim | Sim | Não |
| Cardápio `/cardapio` | Sim | Sim | Sim | Sim | Não |
| Cozinha `/cozinha` e `/chef` | Não** | Não** | Sim | Não | Não |
| Atendimento `/atendimento` e `/garcom` | Não** | Não** | Não | Sim | Não |
| Estoque `/estoque` | Sim | Sim | Não | Não | Não |
| Fornecedores `/fornecedores` | Sim | Sim | Não | Não | Não |
| Funcionários `/funcionarios` | Sim | Sim | Não | Não | Não |
| Integrações `/integracoes` | Sim | Sim | Não | Não | Não |
| Configurações `/configuracoes` | Sim | Sim | Não | Não | Não |

\* O acesso financeiro exige um administrador de restaurante ou gerente com tenant; o Super Admin de plataforma não opera o financeiro de um restaurante sem seleção de tenant.

\*\* Administradores e gerentes podem consultar os fluxos administrativos de pedidos, mas as rotas operacionais dedicadas `/chef` e `/garcom` são exclusivas dos respectivos perfis.

O middleware global do Nuxt impede acesso direto a uma rota não autorizada e redireciona para a home do perfil. A sidebar é filtrada pela mesma matriz.

## Policies do backend

- `SuperAdminOnly`: `Admin` sem `restaurantId`, para operações da plataforma.
- `RestaurantAdminOnly`: `Admin` com `restaurantId`.
- `UserManagement`: `Admin` ou `Manager`, com escopo de restaurante válido ou Super Admin.
- `RestaurantManagerOrAbove`: `Admin` ou `Manager` vinculados a um restaurante, para operações administrativas.
- `RestaurantAnyUser`: leitura/rotinas compartilhadas de um restaurante, incluindo `Cashier`.
- `RestaurantOrderOperator`: `Admin`, `Manager`, `Chef` ou `Waiter`; exclui `Cashier` de criação e mudança de status de pedidos.

O frontend não é a barreira de segurança. Controllers e policies do backend continuam sendo a autoridade final; o token JWT precisa conter `role` e `restaurantId` compatíveis com a operação.

## Regra de administração de equipe

Administradores podem criar e gerenciar `Manager`, `Chef`, `Waiter` e `Cashier` do próprio restaurante. Gerentes podem criar e gerenciar `Chef`, `Waiter` e `Cashier`, mas não outro `Manager` ou `Admin`. Chef, Waiter e Cashier não administram usuários.
