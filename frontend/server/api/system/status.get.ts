import type { SystemStatus } from "~/types/system";

export default defineEventHandler(async () => {
  const config = useRuntimeConfig();

  return await $fetch<SystemStatus>("/api/system/status", {
    baseURL: config.apiBase
  });
});
