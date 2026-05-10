import { apiClient } from "~/services/api/client";
import type { SystemStatus } from "~/types/system";

export function getSystemStatus() {
  return apiClient<SystemStatus>("/api/system/status");
}
