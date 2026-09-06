import { getSystemStatus } from "~/services/modules/system/systemService";

export async function useSystemStatus() {
  return await useAsyncData("system-status", () => getSystemStatus(), {
    default: () => null
  });
}
