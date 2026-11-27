import { apiClientWithAuth } from '~/services/api/client';

export interface TelegramStatus {
  configured: boolean;
  connected: boolean;
  alertsEnabled: boolean;
  botUsername: string | null;
  botId: number | null;
  telegramUserId: number | null;
  botUrl?: string | null;
}

export interface TelegramLink {
  url: string;
  expiresAtUtc: string;
}

export interface TestTelegramResult {
  success: boolean;
  message: string;
}

const endpoint = (path: string, restaurantId?: number | null, superAdmin = false) =>
  superAdmin && restaurantId ? `/api/admin/telegram/restaurants/${restaurantId}${path}` : `/api/me/telegram${path}`;

export const telegramService = {
  status: (restaurantId?: number | null, superAdmin = false) =>
    apiClientWithAuth<TelegramStatus>(endpoint('', restaurantId, superAdmin)),

  save: (telegramUserId: number, restaurantId?: number | null, superAdmin = false) =>
    apiClientWithAuth<TelegramStatus>(endpoint('/save', restaurantId, superAdmin), {
      method: 'POST',
      body: { telegramUserId }
    }),

  testConnection: (restaurantId?: number | null, superAdmin = false) =>
    apiClientWithAuth<TestTelegramResult>(endpoint('/test', restaurantId, superAdmin), {
      method: 'POST'
    }),

  connect: (botId: number, telegramUserId: number, restaurantId?: number | null, superAdmin = false) =>
    apiClientWithAuth<TelegramLink>(endpoint('/link', restaurantId, superAdmin), {
      method: 'POST',
      body: { botId, telegramUserId }
    }),

  disconnect: (restaurantId?: number | null, superAdmin = false) =>
    apiClientWithAuth(endpoint('', restaurantId, superAdmin), { method: 'DELETE' }),

  alerts: (enabled: boolean, restaurantId?: number | null, superAdmin = false) =>
    apiClientWithAuth(endpoint('/alerts', restaurantId, superAdmin), {
      method: 'PUT',
      body: { enabled }
    })
};
