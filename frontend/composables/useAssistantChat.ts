import { computed, ref } from "vue";
import { assistantService } from "~/services/modules/assistant/assistantService";
import type { AssistantConversation, AssistantMessage, AssistantStatus, AssistantThread } from "~/types/assistant";
import type { AgentKind } from "~/types/agentExperience";

const threads = ref<AssistantThread[]>([]);
const loadingMoreThreads = ref(false);
const hasMoreThreads = ref(true);
const threadPage = ref(1);
const currentThread = ref<AssistantThread | null>(null);
const messages = ref<AssistantMessage[]>([]);
const status = ref<AssistantStatus | null>(null);
const pending = ref(false);
const loadingHistory = ref(false);
const ready = ref(false);
const error = ref("");
let revision = 0;
let retryAction: (() => Promise<unknown>) | null = null;
type ThreadRequest = {
  originAlertId: number | null;
  specialistId: AgentKind;
  sourceView: string | null;
  subjectEntityType: string | null;
  subjectEntityId: number | null;
  subject: string | null;
};
let activeThreadRequest: ThreadRequest = {
  originAlertId: null,
  specialistId: "general",
  sourceView: null,
  subjectEntityType: null,
  subjectEntityId: null,
  subject: null
};

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

async function ensureReady(requestOptions: ThreadRequest | number | null) {
  const options: ThreadRequest = typeof requestOptions === "number" || requestOptions === null
    ? { ...activeThreadRequest, originAlertId: requestOptions }
    : requestOptions;
  activeThreadRequest = options;
  const request = resetConversation();
  threads.value = [];
  threadPage.value = 1;
  hasMoreThreads.value = true;
  loadingHistory.value = true;
  try {
    const [nextStatus, history] = await Promise.all([assistantService.status(), assistantService.threads({ limit: 30 })]);
    if (request !== revision) return;
    status.value = nextStatus;
    threads.value = history;
    hasMoreThreads.value = history.length === 30;
    const shouldCreateThread = options.originAlertId !== null || options.specialistId !== "general";
    if (shouldCreateThread) {
      const existing = history.find(thread =>
        thread.originAlertId === options.originAlertId &&
        thread.specialistId === options.specialistId &&
        (options.subjectEntityId === null || thread.subjectEntityId === options.subjectEntityId));
      const conversation = existing ? await assistantService.thread(existing.id) : await assistantService.createThread({
        originAlertId: options.originAlertId,
        specialistId: options.specialistId,
        sourceView: options.sourceView,
        subjectEntityType: options.subjectEntityType,
        subjectEntityId: options.subjectEntityId,
        subject: options.subject
      });
      if (request !== revision) return;
      applyConversation(conversation);
    }
    ready.value = true;
  } catch (cause: any) {
    if (request !== revision) return;
    error.value = cause?.data?.detail || "Não foi possível carregar a conversa.";
    retryAction = () => ensureReady(options);
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

async function loadMoreThreads() {
  if (loadingMoreThreads.value || !hasMoreThreads.value) return;
  loadingMoreThreads.value = true;
  try {
    const nextPage = threadPage.value + 1;
    const history = await assistantService.threads({ page: nextPage, limit: 30 });
    threads.value = [...threads.value, ...history.filter(item => !threads.value.some(existing => existing.id === item.id))];
    threadPage.value = nextPage;
    hasMoreThreads.value = history.length === 30;
  } catch (cause: any) {
    error.value = cause?.data?.detail || "Não foi possível carregar mais conversas.";
  } finally {
    loadingMoreThreads.value = false;
  }
}

async function archiveThread(id: number) {
  await assistantService.archiveThread(id);
  threads.value = threads.value.filter(thread => thread.id !== id);
  if (currentThread.value?.id === id) {
    currentThread.value = null;
    messages.value = [];
    ready.value = true;
    error.value = "";
  }
}

async function send(text: string, image: File | null = null): Promise<boolean> {
  const value = text.trim();
  if ((!value && !image) || !ready.value || pending.value || loadingHistory.value) return false;
  const request = revision;
  pending.value = true;
  error.value = "";
  retryAction = null;
  try {
    if (!currentThread.value) {
      const created = await assistantService.createThread({
        specialistId: activeThreadRequest.specialistId,
        sourceView: activeThreadRequest.sourceView,
        subjectEntityType: activeThreadRequest.subjectEntityType,
        subjectEntityId: activeThreadRequest.subjectEntityId,
        subject: activeThreadRequest.subject || (value || "Imagem anexada ao ISM Assist").slice(0, 150)
      });
      if (request !== revision) return false;
      applyConversation(created);
    }
    const response = image
      ? await assistantService.sendMessageWithImage(currentThread.value!.id, value, image)
      : await assistantService.sendMessage(currentThread.value!.id, value);
    if (request !== revision) return false;
    applyConversation(response);
    return true;
  } catch (cause: any) {
    if (request !== revision) return false;
    error.value = cause?.data?.detail || "Não foi possível enviar a mensagem.";
    retryAction = () => send(value, image);
    return false;
  } finally {
    if (request === revision) pending.value = false;
  }
}

export const useAssistantChat = () => ({
  threads,
  loadingMoreThreads,
  hasMoreThreads,
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
  loadMoreThreads,
  archiveThread,
  resetConversation,
  send,
  retry: () => retryAction?.()
});
