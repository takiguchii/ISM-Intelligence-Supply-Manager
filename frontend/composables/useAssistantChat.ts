import { computed, ref } from "vue";
import { assistantService } from "~/services/modules/assistant/assistantService";
import type { AssistantConversation, AssistantMessage, AssistantStatus, AssistantThread } from "~/types/assistant";

const threads = ref<AssistantThread[]>([]);
const currentThread = ref<AssistantThread | null>(null);
const messages = ref<AssistantMessage[]>([]);
const status = ref<AssistantStatus | null>(null);
const pending = ref(false);
const loadingHistory = ref(false);
const ready = ref(false);
const error = ref("");
let revision = 0;
let retryAction: (() => Promise<unknown>) | null = null;

function resetConversation() {
  revision++;
  currentThread.value = null;
  messages.value = [];
  ready.value = false;
  pending.value = false;
  loadingHistory.value = false;
  error.value = "";
  retryAction = null;
  return revision;
}

function applyConversation(conversation: AssistantConversation) {
  currentThread.value = conversation.thread;
  messages.value = conversation.messages;
  threads.value = [conversation.thread, ...threads.value.filter(thread => thread.id !== conversation.thread.id)]
    .sort((a, b) => Date.parse(b.lastMessageAtUtc) - Date.parse(a.lastMessageAtUtc));
}

async function ensureReady(originAlertId: number | null) {
  const request = resetConversation();
  threads.value = [];
  loadingHistory.value = true;
  try {
    const [nextStatus, history] = await Promise.all([assistantService.status(), assistantService.threads({ limit: 30 })]);
    if (request !== revision) return;
    status.value = nextStatus;
    threads.value = history;
    if (originAlertId !== null) {
      const existing = history.find(thread => thread.originAlertId === originAlertId);
      const conversation = existing ? await assistantService.thread(existing.id) : await assistantService.createThread({ originAlertId });
      if (request !== revision) return;
      applyConversation(conversation);
    }
    ready.value = true;
  } catch (cause: any) {
    if (request !== revision) return;
    error.value = cause?.data?.detail || "Não foi possível carregar a conversa.";
    retryAction = () => ensureReady(originAlertId);
  } finally {
    if (request === revision) loadingHistory.value = false;
  }
}

async function selectThread(id: number) {
  const request = resetConversation();
  loadingHistory.value = true;
  try {
    const conversation = await assistantService.thread(id);
    if (request !== revision) return;
    applyConversation(conversation);
    ready.value = true;
  } catch (cause: any) {
    if (request !== revision) return;
    error.value = cause?.data?.detail || "Não foi possível abrir esta conversa.";
    retryAction = () => selectThread(id);
  } finally {
    if (request === revision) loadingHistory.value = false;
  }
}

async function send(text: string): Promise<boolean> {
  const value = text.trim();
  if (!value || !ready.value || pending.value || loadingHistory.value) return false;
  const request = revision;
  pending.value = true;
  error.value = "";
  retryAction = null;
  try {
    if (!currentThread.value) {
      const created = await assistantService.createThread({ subject: value.slice(0, 150) });
      if (request !== revision) return false;
      applyConversation(created);
    }
    const response = await assistantService.sendMessage(currentThread.value!.id, value);
    if (request !== revision) return false;
    applyConversation(response);
    return true;
  } catch (cause: any) {
    if (request !== revision) return false;
    error.value = cause?.data?.detail || "Não foi possível enviar a mensagem.";
    retryAction = () => send(value);
    return false;
  } finally {
    if (request === revision) pending.value = false;
  }
}

export const useAssistantChat = () => ({
  threads,
  currentThread,
  messages,
  status,
  pending,
  loadingHistory,
  ready,
  error,
  context: computed(() => currentThread.value?.context || null),
  ensureReady,
  selectThread,
  resetConversation,
  send,
  retry: () => retryAction?.()
});
