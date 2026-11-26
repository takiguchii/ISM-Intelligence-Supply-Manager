import { computed, ref } from "vue";
import type { SystemAlertResponse } from "~/types/system";
import { agentForAlert, type AgentKind } from "~/types/agentExperience";

const chatOpen = ref(false);
const requestedAlert = ref<SystemAlertResponse | null>(null);
const requestedDraft = ref("");
const requestedSpecialist = ref<AgentKind>("general");
const requestedSourceView = ref<string | null>(null);
const requestedSubjectEntityType = ref<string | null>(null);
const requestedSubjectEntityId = ref<number | null>(null);
const requestedSubject = ref<string | null>(null);
const openRequest = ref(0);

export interface AssistantOpenOptions {
  alert?: SystemAlertResponse | null;
  sourceView?: string | null;
  subjectEntityType?: string | null;
  subjectEntityId?: number | null;
  subject?: string | null;
}

const open = (specialist: AgentKind, draft: string, options: AssistantOpenOptions = {}) => {
  requestedAlert.value = options.alert ?? null;
  requestedDraft.value = draft;
  requestedSpecialist.value = specialist;
  requestedSourceView.value = options.sourceView ?? null;
  requestedSubjectEntityType.value = options.subjectEntityType ?? null;
  requestedSubjectEntityId.value = options.subjectEntityId ?? null;
  requestedSubject.value = options.subject ?? null;
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
    requestedSpecialist,
    requestedSourceView,
    requestedSubjectEntityType,
    requestedSubjectEntityId,
    requestedSubject,
    openRequest,
    openChat: (alert: SystemAlertResponse | null = null, draft = "") => open(alert ? agentForAlert(alert) : "general", draft, {
      alert,
      sourceView: alert ? "alert" : null,
      subjectEntityType: alert?.referenceEntityType ?? null,
      subjectEntityId: alert?.referenceEntityId ?? null,
      subject: alert?.title ?? null
    }),
    openSpecialistChat: (specialist: AgentKind, options: AssistantOpenOptions = {}, draft = "") => open(specialist, draft, options),
    openGeneralChat: (draft = "") => open("general", draft),
    closeChat: () => { chatOpen.value = false; }
  };
};
