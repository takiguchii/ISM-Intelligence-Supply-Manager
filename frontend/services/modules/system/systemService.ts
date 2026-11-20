import { apiClient } from "~/services/api/client";
import type { SystemStatus, SystemAlertResponse, PagedResult } from "~/types/system";
import { normalizeSystemAlert } from "~/utils/systemAlerts";

type AlertPayload = Parameters<typeof normalizeSystemAlert>[0];

export const getSystemStatus = () =>
  apiClient<SystemStatus>("/api/system/status", {
    method: "GET"
  });

export const getSystemAlerts = async (params?: { pageNumber?: number; pageSize?: number; isRead?: boolean; isDismissed?: boolean }): Promise<PagedResult<SystemAlertResponse>> => {
  const result = await apiClient<PagedResult<AlertPayload>>("/api/system/alerts", {
    method: "GET",
    params
  });
  return { ...result, items: result.items.map(normalizeSystemAlert) };
};

export const markAlertAsRead = async (id: number) =>
  normalizeSystemAlert(await apiClient<AlertPayload>(`/api/system/alerts/${id}/mark-read`, {
    method: "PATCH"
  }));

export const dismissAlert = async (id: number) =>
  normalizeSystemAlert(await apiClient<AlertPayload>(`/api/system/alerts/${id}/dismiss`, {
    method: "PATCH"
  }));

export const dismissAllAlerts = () =>
  apiClient<{ count: number }>("/api/system/alerts/dismiss-all", { method: "PATCH" });

