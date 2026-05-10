import { getSystemStatus } from "~/services/modules/systemService";

export async function useSystemStatus() {
  return await useAsyncData("system-status", () => getSystemStatus(), {
    default: () => null
  });
}
