import { computed, ref } from "vue";
import type { SystemAlertResponse } from "~/types/system";

const chatOpen = ref(false);
const requestedAlert = ref<SystemAlertResponse | null>(null);
const requestedDraft = ref("");
const openRequest = ref(0);

const open = (alert: SystemAlertResponse | null, draft: string) => {
  requestedAlert.value = alert;
  requestedDraft.value = draft;
  openRequest.value++;
  chatOpen.value = true;
};

export const useAssistantUi = () => {
  const auth = useAuthStore();
  return {
    canUse: computed(() => !!auth.currentUser?.restaurantId && ["Admin", "Manager"].includes(auth.userRole ?? "")),
    chatOpen,
    requestedAlert,
    requestedDraft,
    openRequest,
    openChat: (alert: SystemAlertResponse | null = null, draft = "") => open(alert, draft),
    openGeneralChat: (draft = "") => open(null, draft),
    closeChat: () => { chatOpen.value = false; }
  };
};
