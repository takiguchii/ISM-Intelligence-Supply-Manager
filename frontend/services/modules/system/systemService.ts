import { apiClient } from "~/services/api/client";
import type { SystemStatusResponse } from "~/types/system";

export const getSystemStatus = () =>
  apiClient<SystemStatusResponse>("/api/system/status", {
    method: "GET"
  });
