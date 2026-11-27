import { apiClientWithAuth } from '~/services/api/client';

export interface TelegramStatus { configured: boolean; connected: boolean; alertsEnabled: boolean; botUsername: string | null; botId: number | null; telegramUserId: number | null }
export interface TelegramLink { url: string; expiresAtUtc: string }
const endpoint = (path: string, restaurantId?: number | null, superAdmin = false) =>
  superAdmin && restaurantId ? `/api/admin/telegram/restaurants/${restaurantId}${path}` : `/api/me/telegram${path}`;
export const telegramService = {
  status: (restaurantId?: number | null, superAdmin = false) => apiClientWithAuth<TelegramStatus>(endpoint('', restaurantId, superAdmin)),
  connect: (botId: number, telegramUserId: number, restaurantId?: number | null, superAdmin = false) => apiClientWithAuth<TelegramLink>(endpoint('/link', restaurantId, superAdmin), { method: 'POST', body: { botId, telegramUserId } }),
  test: () => apiClientWithAuth('/api/me/notifications/test', { method: 'POST', body: { channel: 'telegram', scenario: 'connectivity' } }),
  disconnect: (restaurantId?: number | null, superAdmin = false) => apiClientWithAuth(endpoint('', restaurantId, superAdmin), { method: 'DELETE' }),
  alerts: (enabled: boolean, restaurantId?: number | null, superAdmin = false) => apiClientWithAuth(endpoint('/alerts', restaurantId, superAdmin), { method: 'PUT', body: { enabled } })
};
