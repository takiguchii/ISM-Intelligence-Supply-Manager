import type { User } from "~/types/auth";

export type AppRole = "Admin" | "Manager" | "Chef" | "Waiter";

export type NavigationItem = {
  id: string;
  label: string;
  icon: string;
  route: string;
  roles: readonly AppRole[];
  requiresRestaurant?: boolean;
};

const restaurantRoles = ["Admin", "Manager", "Chef", "Waiter"] as const;
const managementRoles = ["Admin", "Manager"] as const;

export const navigationItems: readonly NavigationItem[] = [
  { id: "dashboard", label: "Dashboard", icon: "dashboard", route: "/", roles: ["Admin", "Manager"] },
  { id: "financeiro", label: "Financeiro", icon: "wallet", route: "/financeiro", roles: managementRoles, requiresRestaurant: true },
  { id: "atendimento", label: "Atendimento", icon: "tray", route: "/atendimento", roles: ["Admin", "Manager", "Waiter"], requiresRestaurant: true },
  { id: "cozinha", label: "Cozinha", icon: "tray", route: "/cozinha", roles: ["Chef"], requiresRestaurant: true },
  { id: "cardapio", label: "Cardápio", icon: "menu", route: "/cardapio", roles: restaurantRoles, requiresRestaurant: true },
  { id: "pedidos", label: "Pedidos", icon: "tray", route: "/pedidos", roles: restaurantRoles, requiresRestaurant: true },
  { id: "estoque", label: "Estoque", icon: "boxes", route: "/estoque", roles: managementRoles, requiresRestaurant: true },
  { id: "fornecedores", label: "Fornecedores", icon: "truck", route: "/fornecedores", roles: managementRoles, requiresRestaurant: true },
  { id: "funcionarios", label: "Funcionários", icon: "users", route: "/funcionarios", roles: ["Admin"] },
  { id: "integracoes", label: "Integrações", icon: "plug", route: "/integracoes", roles: managementRoles, requiresRestaurant: true },
  { id: "configuracoes", label: "Configurações", icon: "gear", route: "/configuracoes", roles: managementRoles, requiresRestaurant: true }
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
    roles: [user.role as AppRole]
  };

  return [homeItem, ...navigationItems.filter((item) =>
    item.route !== home && canAccessRoute(user, item.route)
  )];
}
