import type { User } from "~/types/auth";

export type AppRole = "Admin" | "Manager" | "Chef" | "Waiter";

export type NavigationCategory =
  | "VISÃO GERAL"
  | "GESTÃO OPERACIONAL"
  | "FINANCEIRO"
  | "ADMINISTRAÇÃO"
  | "MOVIMENTAÇÕES";

export type NavigationItem = {
  id: string;
  label: string;
  icon: string;
  route: string;
  roles: readonly AppRole[];
  navigationRoles?: readonly AppRole[];
  category?: NavigationCategory;
  requiresRestaurant?: boolean;
};

const restaurantRoles = ["Admin", "Manager", "Chef", "Waiter"] as const;
const managementRoles = ["Admin", "Manager"] as const;

export const navigationItems: readonly NavigationItem[] = [
  { id: "dashboard", label: "Dashboard", icon: "dashboard", route: "/", roles: ["Admin", "Manager"], category: "VISÃO GERAL" },
  { id: "financeiro", label: "Financeiro", icon: "wallet", route: "/financeiro", roles: managementRoles, category: "FINANCEIRO", requiresRestaurant: true },
  { id: "atendimento", label: "Atendimento", icon: "tray", route: "/atendimento", roles: ["Admin", "Manager", "Waiter"], navigationRoles: ["Waiter"], category: "MOVIMENTAÇÕES", requiresRestaurant: true },
  { id: "cozinha", label: "Cozinha", icon: "chef-hat", route: "/cozinha", roles: ["Admin", "Manager", "Chef"], navigationRoles: ["Chef"], category: "MOVIMENTAÇÕES", requiresRestaurant: true },
  { id: "pedidos", label: "Pedidos", icon: "clipboard-list", route: "/pedidos", roles: restaurantRoles, category: "GESTÃO OPERACIONAL", requiresRestaurant: true },
  { id: "cardapio", label: "Cardápio", icon: "menu", route: "/cardapio", roles: restaurantRoles, category: "GESTÃO OPERACIONAL", requiresRestaurant: true },
  { id: "estoque", label: "Estoque", icon: "boxes", route: "/estoque", roles: managementRoles, category: "GESTÃO OPERACIONAL", requiresRestaurant: true },
  { id: "fornecedores", label: "Fornecedores", icon: "truck", route: "/fornecedores", roles: managementRoles, category: "GESTÃO OPERACIONAL", requiresRestaurant: true },
  { id: "funcionarios", label: "Funcionários", icon: "users", route: "/funcionarios", roles: managementRoles, category: "ADMINISTRAÇÃO", requiresRestaurant: true },
  { id: "integracoes", label: "Integrações", icon: "plug", route: "/integracoes", roles: managementRoles, category: "ADMINISTRAÇÃO", requiresRestaurant: true },
  { id: "configuracoes", label: "Configurações", icon: "gear", route: "/configuracoes", roles: managementRoles, category: "ADMINISTRAÇÃO", requiresRestaurant: true }
];

const roleHome: Record<AppRole, string> = {
  Admin: "/",
  Manager: "/",
  Chef: "/chef",
  Waiter: "/garcom"
};

function isAppRole(role: string | undefined): role is AppRole {
  return role === "Admin" || role === "Manager" || role === "Chef" || role === "Waiter";
}

export function homeForUser(user: User | null): string {
  return user && isAppRole(user.role) ? roleHome[user.role] : "/login";
}

export function canAccessRoute(user: User | null, path: string): boolean {
  if (!user || !isAppRole(user.role)) return false;

  if (path === "/chef") return user.role === "Chef" && !!user.restaurantId;
  if (path === "/garcom") return user.role === "Waiter" && !!user.restaurantId;

  const item = navigationItems.find((candidate) => candidate.route === path);
  if (!item) return true;

  return item.roles.includes(user.role) && (!item.requiresRestaurant || !!user.restaurantId);
}

export function navigationForUser(user: User | null): NavigationItem[] {
  if (!user) return [];

  const home = homeForUser(user);
  const homeItem: NavigationItem = {
    id: "inicio",
    label: user.role === "Chef" || user.role === "Waiter" ? "Início" : "Dashboard",
    icon: "dashboard",
    route: home,
    roles: [user.role as AppRole],
    category: "VISÃO GERAL"
  };

  return [homeItem, ...navigationItems.filter((item) =>
    item.route !== home &&
    (item.navigationRoles ?? item.roles).includes(user.role) &&
    canAccessRoute(user, item.route)
  )];
}
