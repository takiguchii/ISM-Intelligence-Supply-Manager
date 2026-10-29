# Matriz de Perfis, Autenticação e Acesso a Views (ISM)

Este documento descreve as funções de cada perfil de usuário no **ISM (Intelligence Supply Manager)**, a matriz de direcionamento de rotas (home route) e as permissões de acesso às views no frontend e endpoints no backend.

---

## 👥 Perfis de Usuário

| Perfil | Escopo / Papel | Home Route | Descrição Operacional |
| :--- | :--- | :--- | :--- |
| **Manager (Gerente / Admin do Restaurante)** | Restaurante (`restaurantId != null`) | `/` | Gestão operacional e financeira do restaurante. Responsável por analisar indicadores no Dashboard, controlar movimentações financeiras, gerenciar equipe, fornecedores, estoque e cardápio. |
| **Chef (Cozinha)** | Restaurante (`restaurantId != null`) | `/chef` | Operação do preparo e despacho de pedidos da cozinha. Acesso focado no KDS (Kitchen Display System), kanban de pedidos e consulta rápida à ficha do cardápio. |
| **Waiter (Atendimento / Garçom)** | Restaurante (`restaurantId != null`) | `/garcom` | Operação do salão e atendimento ao cliente. Responsável por lançar pedidos, gerenciar comandas/mesas, consultar cardápio e encaminhar pedidos para a cozinha/bar. |
| **Super Admin (Plataforma)** | Plataforma ISM (`restaurantId == null`) | `/` | **[EM CONSTRUÇÃO]** Gestão multi-tenant da plataforma ISM (onboarding de restaurantes, controle de planos, métricas globais e infraestrutura de IA). Não opera diretamente o fluxo do salão/cozinha de um restaurante. |

---

## 🧭 Matriz de Acesso às Views no Frontend (Nuxt 3)

O controle de visualização e navegação é implementado de forma unificada no composable/utilitário [`frontend/utils/navigation.ts`](file:///home/takiguchi/Documentos/github/ISM-Intelligence-Supply-Manager/frontend/utils/navigation.ts) e assegurado globalmente pelo middleware [`frontend/middleware/auth.global.ts`](file:///home/takiguchi/Documentos/github/ISM-Intelligence-Supply-Manager/frontend/middleware/auth.global.ts).

| Rota / View | Manager / Admin Restaurante | Chef (Cozinha) | Waiter (Garçom) | Super Admin |
| :--- | :---: | :---: | :---: | :---: |
| `/` (Dashboard / Início) |  Permitido | 🚫 Redireciona `/chef` | 🚫 Redireciona `/garcom` |  Permitido (Visão Plataforma) |
| `/chef` (KDS Cozinha Direto) | 🚫 (usa `/cozinha`) |  Permitido | 🚫 Redireciona `/garcom` | 🚫 Bloqueado |
| `/garcom` (Comandas Direto) | 🚫 (usa `/atendimento`) | 🚫 Redireciona `/chef` |  Permitido | 🚫 Bloqueado |
| `/financeiro` |  Permitido | 🚫 Redireciona `/chef` | 🚫 Redireciona `/garcom` | 🚫 Bloqueado |
| `/atendimento` |  Permitido | 🚫 Redireciona `/chef` |  Permitido | 🚫 Bloqueado |
| `/cozinha` |  Permitido |  Permitido | 🚫 Redireciona `/garcom` | 🚫 Bloqueado |
| `/pedidos` |  Permitido |  Permitido |  Permitido | 🚫 Bloqueado |
| `/cardapio` |  Permitido |  Permitido |  Permitido | 🚫 Bloqueado |
| `/estoque` |  Permitido | 🚫 Redireciona `/chef` | 🚫 Redireciona `/garcom` | 🚫 Bloqueado |
| `/fornecedores` |  Permitido | 🚫 Redireciona `/chef` | 🚫 Redireciona `/garcom` | 🚫 Bloqueado |
| `/funcionarios` |  Permitido | 🚫 Redireciona `/chef` | 🚫 Redireciona `/garcom` | 🚫 Bloqueado |
| `/integracoes` |  Permitido | 🚫 Redireciona `/chef` | 🚫 Redireciona `/garcom` | 🚫 Bloqueado |
| `/configuracoes` |  Permitido | 🚫 Redireciona `/chef` | 🚫 Redireciona `/garcom` | 🚫 Bloqueado |

> **Nota de Segurança no Frontend:** Qualquer tentativa de acesso via URL direta por um perfil não autorizado aciona o `auth.global.ts`, que cancela a rota e força o redirecionamento com `replace: true` para a rota principal do perfil correspondente.

---

## 🔒 Políticas de Autorização no Backend (.NET 8)

As políticas de acesso são validadas em nível de controller através de tokens JWT com claims de `role` e `restaurantId`:

1. **`SuperAdminOnly`**:
   - Requer `Role = Admin` e ausência da claim `restaurantId` (usuário de plataforma).
   - Bloqueia administradores e funcionários de restaurantes.
2. **`RestaurantManagerOrAbove`**:
   - Requer `Role in ("Admin", "Manager")` e presença de `restaurantId > 0`.
   - Utilizado em endpoints sensíveis: `/api/finance`, exclusão/edição de produtos em estoque, fornecedores e configurações da loja.
3. **`RestaurantAnyUser`**:
   - Requer `Role in ("Admin", "Manager", "Chef", "Waiter")` associado ao restaurante.
   - Utilizado nos endpoints operacionais compartilhados: listagem e atualização de pedidos (`/api/orders`), consulta de pratos e categorias (`/api/dishes`, `/api/categories`).
4. **`UserManagement`**:
   - Requer `SuperAdmin` (para criar/gerenciar qualquer restaurante) ou `Admin/Manager` de um restaurante específico para administrar os colaboradores daquele tenant.

---

## 🏗️ Seção: Super Admin (Em Construção)

> **Status:** **EM CONSTRUÇÃO**
>
> A área de Super Admin está reservada para o painel mestre da plataforma ISM. Suas funcionalidades planejadas incluem:
> - Cadastro e gerenciamento global de restaurantes parceiros (tenants).
> - Controle de faturamento, assinaturas e limites por plano.
> - Monitoramento de infraestrutura, logs de agentes de inteligência e telemetria geral.
>
> Enquanto a definição negocial e de arquitetura do Super Admin é finalizada, o foco operacional permanece 100% voltado aos três perfis de restaurante: **Manager**, **Chef** e **Waiter**.
