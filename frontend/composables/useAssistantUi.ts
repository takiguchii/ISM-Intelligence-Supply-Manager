import { ref } from "vue";
import type { SystemAlertResponse } from "~/types/system";

const chatOpen = ref(false);
const requestedAlert = ref<SystemAlertResponse | null>(null);

export const useAssistantUi = () => ({
  chatOpen,
  requestedAlert,
  openChat: (alert: SystemAlertResponse | null = null) => { requestedAlert.value = alert; chatOpen.value = true; },
  openGeneralChat: () => { requestedAlert.value = null; chatOpen.value = true; },
  closeChat: () => { chatOpen.value = false; }
});
