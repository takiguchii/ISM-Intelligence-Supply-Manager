import { computed, ref } from "vue";
import { assistantService } from "~/services/modules/assistant/assistantService";
import type { AssistantConversation, AssistantMessage, AssistantStatus, AssistantThread } from "~/types/assistant";

const threads = ref<AssistantThread[]>([]);
const currentThread = ref<AssistantThread | null>(null);
const messages = ref<AssistantMessage[]>([]);
const status = ref<AssistantStatus | null>(null);
const pending = ref(false);
const loadingHistory = ref(false);
const error = ref("");
const lastMessage = ref("");
let loaded = false;

const applyConversation = (conversation: AssistantConversation) => {
  currentThread.value = conversation.thread;
  messages.value = conversation.messages;
  threads.value = [conversation.thread, ...threads.value.filter(thread => thread.id !== conversation.thread.id)].sort((a, b) => new Date(b.lastMessageAtUtc).getTime() - new Date(a.lastMessageAtUtc).getTime());
};

const load = async (originAlertId: number | null = null) => {
  loadingHistory.value = true;
  error.value = "";
  try {
    status.value = await assistantService.status();
    threads.value = await assistantService.threads({ limit: 30 });
    const existing = originAlertId ? threads.value.find(thread => thread.originAlertId === originAlertId) : currentThread.value || threads.value[0];
    if (existing) applyConversation(await assistantService.thread(existing.id));
    else applyConversation(await assistantService.createThread({ originAlertId }));
    loaded = true;
  } catch (cause: any) {
    error.value = cause?.data?.detail || "Não foi possível carregar o histórico do assistente.";
  } finally {
    loadingHistory.value = false;
  }
};

export const useAssistantChat = () => ({
  threads,
  currentThread,
  messages,
  status,
  pending,
  loadingHistory,
  error,
  context: computed(() => currentThread.value?.context || null),
  ensureReady: async (originAlertId: number | null = null) => {
    if (!loaded || (originAlertId !== null && currentThread.value?.originAlertId !== originAlertId)) await load(originAlertId);
  },
  selectThread: async (thread: AssistantThread) => {
    try {
      error.value = "";
      applyConversation(await assistantService.thread(thread.id));
    } catch (cause: any) {
      error.value = cause?.data?.detail || "Não foi possível abrir esta conversa.";
    }
  },
  newConversation: async (parentThreadId: number | null = null) => {
    try {
      error.value = "";
      applyConversation(await assistantService.createThread({ parentThreadId }));
    } catch (cause: any) {
      error.value = cause?.data?.detail || "Não foi possível iniciar uma nova conversa.";
    }
  },
  send: async (text: string) => {
    const value = text.trim();
    if (!value || !currentThread.value || pending.value) return false;
    pending.value = true;
    lastMessage.value = value;
    error.value = "";
    try {
      applyConversation(await assistantService.sendMessage(currentThread.value.id, value));
      return true;
    } catch (cause: any) {
      error.value = cause?.data?.detail || "Não foi possível enviar a mensagem.";
      return false;
    } finally {
      pending.value = false;
    }
  },
  retry: async () => {
    if (lastMessage.value) return (await useAssistantChat().send(lastMessage.value));
    return false;
  },
  reload: async () => { loaded = false; await load(currentThread.value?.originAlertId || null); }
});
