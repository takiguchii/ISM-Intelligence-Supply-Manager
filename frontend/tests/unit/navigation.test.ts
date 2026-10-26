import assert from "node:assert/strict";
import { canAccessRoute, homeForUser, navigationForUser, navigationItems } from "../../utils/navigation.ts";
import type { User } from "../../types/auth";

const managerUser: User = {
  id: "1",
  name: "Gerente Teste",
  email: "gerente@teste.com",
  role: "Manager",
  restaurantId: 10,
  restaurantName: "Restaurante Teste"
};

const chefUser: User = {
  id: "2",
  name: "Chef Teste",
  email: "chef@teste.com",
  role: "Chef",
  restaurantId: 10,
  restaurantName: "Restaurante Teste"
};

const waiterUser: User = {
  id: "3",
  name: "Garçom Teste",
  email: "garcom@teste.com",
  role: "Waiter",
  restaurantId: 10,
  restaurantName: "Restaurante Teste"
};

const superAdminUser: User = {
  id: "99",
  name: "Super Admin",
  email: "super@teste.com",
  role: "Admin",
  restaurantId: null,
  restaurantName: null
};

// 1. Validar Redirecionamento Inicial (homeForUser)
assert.equal(homeForUser(managerUser), "/", "Manager deve ser direcionado para '/' (Dashboard)");
assert.equal(homeForUser(chefUser), "/chef", "Chef deve ser direcionado para '/chef'");
assert.equal(homeForUser(waiterUser), "/garcom", "Waiter deve ser direcionado para '/garcom'");
assert.equal(homeForUser(superAdminUser), "/", "SuperAdmin sem tenant é direcionado para '/'");
assert.equal(homeForUser(null), "/login", "Usuário não autenticado deve ir para '/login'");

// 2. Validar canAccessRoute para Chef
assert.equal(canAccessRoute(chefUser, "/chef"), true, "Chef pode acessar /chef");
assert.equal(canAccessRoute(chefUser, "/cozinha"), true, "Chef pode acessar /cozinha");
assert.equal(canAccessRoute(chefUser, "/pedidos"), true, "Chef pode acessar /pedidos");
assert.equal(canAccessRoute(chefUser, "/cardapio"), true, "Chef pode acessar /cardapio");

assert.equal(canAccessRoute(chefUser, "/"), false, "Chef NÃO pode acessar / (Dashboard)");
assert.equal(canAccessRoute(chefUser, "/financeiro"), false, "Chef NÃO pode acessar /financeiro");
assert.equal(canAccessRoute(chefUser, "/atendimento"), false, "Chef NÃO pode acessar /atendimento");
assert.equal(canAccessRoute(chefUser, "/garcom"), false, "Chef NÃO pode acessar /garcom");
assert.equal(canAccessRoute(chefUser, "/estoque"), false, "Chef NÃO pode acessar /estoque");
assert.equal(canAccessRoute(chefUser, "/fornecedores"), false, "Chef NÃO pode acessar /fornecedores");
assert.equal(canAccessRoute(chefUser, "/funcionarios"), false, "Chef NÃO pode acessar /funcionarios");
assert.equal(canAccessRoute(chefUser, "/integracoes"), false, "Chef NÃO pode acessar /integracoes");
assert.equal(canAccessRoute(chefUser, "/configuracoes"), false, "Chef NÃO pode acessar /configuracoes");

// 3. Validar canAccessRoute para Waiter (Atendimento)
assert.equal(canAccessRoute(waiterUser, "/garcom"), true, "Waiter pode acessar /garcom");
assert.equal(canAccessRoute(waiterUser, "/atendimento"), true, "Waiter pode acessar /atendimento");
assert.equal(canAccessRoute(waiterUser, "/pedidos"), true, "Waiter pode acessar /pedidos");
assert.equal(canAccessRoute(waiterUser, "/cardapio"), true, "Waiter pode acessar /cardapio");

assert.equal(canAccessRoute(waiterUser, "/"), false, "Waiter NÃO pode acessar / (Dashboard)");
assert.equal(canAccessRoute(waiterUser, "/financeiro"), false, "Waiter NÃO pode acessar /financeiro");
assert.equal(canAccessRoute(waiterUser, "/chef"), false, "Waiter NÃO pode acessar /chef");
assert.equal(canAccessRoute(waiterUser, "/cozinha"), false, "Waiter NÃO pode acessar /cozinha");
assert.equal(canAccessRoute(waiterUser, "/estoque"), false, "Waiter NÃO pode acessar /estoque");
assert.equal(canAccessRoute(waiterUser, "/fornecedores"), false, "Waiter NÃO pode acessar /fornecedores");
assert.equal(canAccessRoute(waiterUser, "/funcionarios"), false, "Waiter NÃO pode acessar /funcionarios");
assert.equal(canAccessRoute(waiterUser, "/integracoes"), false, "Waiter NÃO pode acessar /integracoes");
assert.equal(canAccessRoute(waiterUser, "/configuracoes"), false, "Waiter NÃO pode acessar /configuracoes");

// 4. Validar canAccessRoute para Manager (Gerente)
assert.equal(canAccessRoute(managerUser, "/"), true, "Manager pode acessar /");
assert.equal(canAccessRoute(managerUser, "/financeiro"), true, "Manager pode acessar /financeiro");
assert.equal(canAccessRoute(managerUser, "/atendimento"), true, "Manager pode acessar /atendimento");
assert.equal(canAccessRoute(managerUser, "/cozinha"), true, "Manager pode acessar /cozinha");
assert.equal(canAccessRoute(managerUser, "/pedidos"), true, "Manager pode acessar /pedidos");
assert.equal(canAccessRoute(managerUser, "/cardapio"), true, "Manager pode acessar /cardapio");
assert.equal(canAccessRoute(managerUser, "/estoque"), true, "Manager pode acessar /estoque");
assert.equal(canAccessRoute(managerUser, "/fornecedores"), true, "Manager pode acessar /fornecedores");
assert.equal(canAccessRoute(managerUser, "/funcionarios"), true, "Manager pode acessar /funcionarios");
assert.equal(canAccessRoute(managerUser, "/integracoes"), true, "Manager pode acessar /integracoes");
assert.equal(canAccessRoute(managerUser, "/configuracoes"), true, "Manager pode acessar /configuracoes");
assert.equal(canAccessRoute(managerUser, "/chef"), false, "Manager não tem rota /chef dedicada (utiliza /cozinha)");
assert.equal(canAccessRoute(managerUser, "/garcom"), false, "Manager não tem rota /garcom dedicada (utiliza /atendimento)");

// 5. Validar menu de navegação lateral (navigationForUser)
const chefNav = navigationForUser(chefUser).map((item) => item.route);
assert.deepEqual(
  chefNav,
  ["/chef", "/cozinha", "/pedidos", "/cardapio"],
  "Chef deve ter estritamente as rotas operacionais de cozinha e cardápio na barra lateral"
);

const waiterNav = navigationForUser(waiterUser).map((item) => item.route);
assert.deepEqual(
  waiterNav,
  ["/garcom", "/atendimento", "/pedidos", "/cardapio"],
  "Waiter deve ter estritamente as rotas operacionais de atendimento, pedidos e cardápio na barra lateral"
);

console.log("Todos os testes de autorização e navegação de perfis passaram com sucesso!");
