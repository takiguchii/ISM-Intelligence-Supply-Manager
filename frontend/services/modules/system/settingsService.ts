import { apiClientWithAuth } from "~/services/api/client";

export interface RestaurantSettings {
  id: number;
  name: string;
  cnpj: string;
  timeZone: string;
  currency: string;
  language: string;
  address: string | null;
  phone: string | null;
  contactEmail: string | null;
  openingHours: string | null;
  logoDataUrl: string | null;
  serviceFeePercent: number;
  deliveryFee: number;
  packagingFee: number;
  planName?: string | null;
  monthlyPrice?: number;
  isPlanActive?: boolean;
  trialEndAtUtc?: string | null;
}

export function profileUrl(restaurantId: number, superAdmin: boolean) {
  return superAdmin ? `/api/restaurants/${restaurantId}/profile` : "/api/me/restaurant/profile";
}

export function settingsError(error: any): string {
  const body = error?.data;
  if (body?.errors) return Object.values(body.errors).flat().join(" ");
  return body?.message || body?.detail || (typeof body === "string" ? body : null) || body?.title || "Não foi possível concluir. Tente novamente.";
}

export interface AccountSession {
  id: string;
  device: string;
  createdAtUtc: string;
  expiresAtUtc: string;
  isCurrent: boolean;
}

export interface NotificationPreferences {
  stock: boolean;
  suppliers: boolean;
  menu: boolean;
  emailEnabled?: boolean;
  whatsAppEnabled?: boolean;
  emailAddress?: string | null;
  whatsAppPhone?: string | null;
}

export const settingsService = {
  getProfile: (id: number, superAdmin: boolean) => apiClientWithAuth<RestaurantSettings>(profileUrl(id, superAdmin)),
  saveProfile: (id: number, superAdmin: boolean, profile: RestaurantSettings) =>
    apiClientWithAuth(profileUrl(id, superAdmin), { method: "PUT", body: profile }),
  changePassword: (currentPassword: string, newPassword: string) =>
    apiClientWithAuth("/api/auth/change-password", { method: "POST", body: { currentPassword, newPassword } }),
  sessions: () => apiClientWithAuth<AccountSession[]>("/api/auth/sessions"),
  revokeSession: (id: string) => apiClientWithAuth(`/api/auth/sessions/${encodeURIComponent(id)}`, { method: "DELETE" }),
  notifications: () => apiClientWithAuth<NotificationPreferences>("/api/me/notification-preferences"),
  saveNotifications: (preferences: NotificationPreferences) =>
    apiClientWithAuth("/api/me/notification-preferences", { method: "PUT", body: preferences })
};
