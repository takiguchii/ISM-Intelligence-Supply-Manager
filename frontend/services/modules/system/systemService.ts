import { apiClient } from "~/services/api/client";
import type { SystemStatus, SystemAlertResponse, PagedResult } from "~/types/system";

export const getSystemStatus = () =>
  apiClient<SystemStatus>("/api/system/status", {
    method: "GET"
  });

export const getSystemAlerts = (params?: { pageNumber?: number; pageSize?: number; isRead?: boolean; isDismissed?: boolean }) =>
  apiClient<PagedResult<SystemAlertResponse>>("/api/system/alerts", {
    method: "GET",
    params
  });

export const markAlertAsRead = (id: number) =>
  apiClient<SystemAlertResponse>(`/api/system/alerts/${id}/mark-read`, {
    method: "PATCH"
  });

export const dismissAlert = (id: number) =>
  apiClient<SystemAlertResponse>(`/api/system/alerts/${id}/dismiss`, {
    method: "PATCH"
  });

