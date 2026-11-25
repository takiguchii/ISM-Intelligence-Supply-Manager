import { apiClientWithAuth } from '~/services/api/client';

export interface TelegramStatus { configured: boolean; connected: boolean; alertsEnabled: boolean; botUsername: string | null; botId: number | null; telegramUserId: number | null }
export interface TelegramLink { url: string; expiresAtUtc: string }
export const telegramService = {
  status: () => apiClientWithAuth<TelegramStatus>('/api/me/telegram'),
  connect: (botId: number, telegramUserId: number) => apiClientWithAuth<TelegramLink>('/api/me/telegram/link', { method: 'POST', body: { botId, telegramUserId } }),
  test: () => apiClientWithAuth('/api/me/notifications/test', { method: 'POST', body: { channel: 'telegram', scenario: 'connectivity' } }),
  disconnect: () => apiClientWithAuth('/api/me/telegram', { method: 'DELETE' }),
  alerts: (enabled: boolean) => apiClientWithAuth('/api/me/telegram/alerts', { method: 'PUT', body: { enabled } })
};
