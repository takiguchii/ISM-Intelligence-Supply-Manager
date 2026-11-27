<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from "vue";
import { useAssistantUi } from "~/composables/useAssistantUi";
import { useAssistantChat } from "~/composables/useAssistantChat";
import AgentMascot2D from "~/components/assistant/AgentMascot2D.vue";
import { moodForAlert, type AgentMood } from "~/types/agentExperience";

const ui = useAssistantUi();
const chat = useAssistantChat();
const draft = ref("");
const isChatOpen = computed(() => ui.chatOpen.value);
const requestedOrigin = computed(() => ui.requestedAlert.value?.id ?? null);
const contextLabel = computed(() => chat.context.value?.entityName || null);
const contextFacts = computed(() => Object.entries(chat.context.value?.facts || {}).slice(0, 5));
const chatAgent = computed(() => {
  return chat.currentThread.value?.specialistId || ui.requestedSpecialist.value;
});
const chatAgentLabel = computed(() => ({ stock: "Estoque", pricing: "Precificação", suppliers: "Fornecedores", finance: "Financeiro", general: "Assistente geral" }[chatAgent.value]));
const chatMood = computed<AgentMood>(() => {
  if (chat.pending.value || chat.loadingHistory.value) return "thinking";
  if (chat.error.value) return "error";
  return chat.context.value ? moodForAlert(chat.context.value) : "curious";
});
const inputRef = ref<HTMLTextAreaElement | null>(null);
const attachmentInputRef = ref<HTMLInputElement | null>(null);
const cameraInputRef = ref<HTMLInputElement | null>(null);
const attachment = ref<File | null>(null);
const attachmentPreview = ref("");
const attachmentError = ref("");
const historyOpen = ref(false);
const archivingThreadId = ref<number | null>(null);
const canSubmit = computed(() => Boolean(draft.value.trim() || attachment.value));
const dialogRef = ref<HTMLElement | null>(null);
let returnFocusTo: HTMLElement | null = null;

const parseFormattedSegments = (content: string) => {
  if (!content) return [];
  const parts = content.split(/(\*\*.*?\*\*)/gs);
  return parts.map((part) => {
    if (part.startsWith("**") && part.endsWith("**") && part.length >= 4) {
      return { bold: true, text: part.slice(2, -2) };
    }
    return { bold: false, text: part };
  });
};

const imageTypeFor = (file: File) => {
  if (file.type) return file.type;
  if (/\.png$/i.test(file.name)) return "image/png";
  if (/\.webp$/i.test(file.name)) return "image/webp";
  if (/\.jpe?g$/i.test(file.name)) return "image/jpeg";
  return "";
};
const clearAttachment = () => {
  if (attachmentPreview.value) URL.revokeObjectURL(attachmentPreview.value);
  attachment.value = null;
  attachmentPreview.value = "";
  attachmentError.value = "";
};
const chooseAttachment = (selected: File | undefined) => {
  if (!selected) return;
  const contentType = imageTypeFor(selected);
  if (!/^image\/(png|jpeg|webp)$/i.test(contentType)) {
    attachmentError.value = "Anexe uma imagem PNG, JPEG ou WEBP.";
    return;
  }
  if (selected.size > 12 * 1024 * 1024) {
    attachmentError.value = "A imagem deve ter no máximo 12 MB.";
    return;
  }
  clearAttachment();
  attachment.value = selected.type ? selected : new File([selected], selected.name, { type: contentType });
  attachmentPreview.value = URL.createObjectURL(attachment.value);
};
const handleAttachmentChange = (event: Event) => {
  chooseAttachment((event.target as HTMLInputElement).files?.[0]);
  (event.target as HTMLInputElement).value = "";
};
const openAttachmentPicker = () => attachmentInputRef.value?.click();
const openCamera = () => cameraInputRef.value?.click();

const prepare = async (request: number) => {
  draft.value = ui.requestedDraft.value;
  clearAttachment();
  const preparation = chat.ensureReady({
    originAlertId: requestedOrigin.value,
    specialistId: ui.requestedSpecialist.value,
    sourceView: ui.requestedSourceView.value,
    subjectEntityType: ui.requestedSubjectEntityType.value,
    subjectEntityId: ui.requestedSubjectEntityId.value,
    subject: ui.requestedSubject.value
  });
  await nextTick();
  if (ui.chatOpen.value && request === ui.openRequest.value) dialogRef.value?.focus();
  await preparation;
  await nextTick();
  if (ui.chatOpen.value && request === ui.openRequest.value && document.activeElement === dialogRef.value) {
    inputRef.value?.focus();
  }
};
const selectHistory = (id: number) => { draft.value = ""; clearAttachment(); void chat.selectThread(id); historyOpen.value = false; };
const removeThread = async (id: number) => {
  archivingThreadId.value = id;
  try { await chat.archiveThread(id); }
  catch (cause: any) { chat.error.value = cause?.data?.detail || "Não foi possível excluir esta conversa."; }
  finally { archivingThreadId.value = null; }
};
const loadMoreHistory = (event: Event) => {
  const list = event.target as HTMLElement;
  if (list.scrollTop + list.clientHeight >= list.scrollHeight - 36) void chat.loadMoreThreads();
};
const retry = async () => { if (await chat.retry() === true) draft.value = ""; };
const submit = async () => {
  if (!canSubmit.value) return;
  const sent = await chat.send(draft.value || "Analise a imagem anexada.", attachment.value);
  if (sent) { draft.value = ""; clearAttachment(); }
};
const handleKeydown = (event: KeyboardEvent) => {
  if (!ui.chatOpen.value) return;
  if (event.key === "Escape") { ui.closeChat(); return; }
  if (event.key !== "Tab" || !dialogRef.value) return;
  const focusable = [...dialogRef.value.querySelectorAll<HTMLElement>('button:not(:disabled), select:not(:disabled), textarea:not(:disabled), [href], [tabindex]:not([tabindex="-1"])')];
  const first = focusable[0]; const last = focusable.at(-1);
  if (event.shiftKey && (document.activeElement === first || document.activeElement === dialogRef.value)) { event.preventDefault(); last?.focus(); }
  else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first?.focus(); }
};
const restoreBody = () => { if (typeof document !== "undefined") document.body.style.overflow = ""; };

watch([ui.chatOpen, ui.openRequest], ([open, request], [wasOpen]) => {
  if (open && !wasOpen && typeof document !== "undefined") returnFocusTo = document.activeElement as HTMLElement;
  if (typeof document !== "undefined") document.body.style.overflow = ui.chatOpen.value ? "hidden" : "";
  if (!open && wasOpen) void nextTick(() => returnFocusTo?.focus());
  if (open) void prepare(request);
  else chat.resetConversation();
});
onMounted(() => window.addEventListener("keydown", handleKeydown));
onBeforeUnmount(() => { window.removeEventListener("keydown", handleKeydown); restoreBody(); clearAttachment(); });
</script>

<template>
  <Teleport to="body">
    <Transition name="assistant-chat">
      <div v-if="isChatOpen" class="fixed inset-0 z-[60]">
        <button class="assistant-chat-backdrop absolute inset-0 h-full w-full bg-zinc-950/35 backdrop-blur-[2px]" aria-label="Fechar chat" @click="ui.closeChat" />
        <aside ref="dialogRef" class="assistant-chat-panel absolute inset-y-0 right-0 flex w-full max-w-[480px] flex-col border-l border-zinc-200/80 bg-white/95 shadow-2xl shadow-zinc-950/20 backdrop-blur-xl dark:border-zinc-800 dark:bg-zinc-950/95" role="dialog" aria-modal="true" aria-labelledby="assistant-chat-title" tabindex="-1">
          <header class="border-b border-zinc-200/80 px-5 pb-4 pt-[calc(1.25rem+env(safe-area-inset-top))] dark:border-zinc-800">
            <div class="flex items-center justify-between gap-4">
              <div class="flex min-w-0 items-center gap-3"><div class="flex h-12 w-14 items-center justify-center rounded-2xl bg-indigo-50 dark:bg-indigo-400/10"><AgentMascot2D :agent="chatAgent" :mood="chatMood" size="sm" /></div><div class="min-w-0"><p class="text-[10px] font-semibold uppercase tracking-[.18em] text-indigo-600 dark:text-indigo-300">{{ chatAgentLabel }}</p><h2 id="assistant-chat-title" class="mt-0.5 truncate text-lg font-semibold tracking-tight text-zinc-900 dark:text-white">Como posso ajudar?</h2><p class="mt-1 truncate text-xs text-zinc-500">{{ chat.status.value?.providerReachable ? `Conectado · ${chat.status.value.model}` : chat.status.value?.enabled ? 'Provedor de IA indisponível' : 'Conversação por IA desativada' }}</p></div></div>
              <button type="button" class="grid h-10 w-10 shrink-0 place-items-center rounded-xl text-xl text-zinc-500 transition hover:bg-zinc-100 hover:text-zinc-900 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-indigo-500 dark:hover:bg-zinc-800 dark:hover:text-white" aria-label="Fechar chat" @click="ui.closeChat">×</button>
            </div>
            <div class="mt-4 flex gap-2"><button type="button" class="flex min-w-0 flex-1 items-center justify-between rounded-xl border border-zinc-200 bg-zinc-50/70 px-3 py-2.5 text-left text-xs text-zinc-700 transition hover:border-indigo-300 dark:border-zinc-700 dark:bg-zinc-900 dark:text-zinc-300" :aria-expanded="historyOpen" @click="historyOpen = !historyOpen"><span class="font-semibold">Histórico de conversas</span><span class="ml-2 rounded-full bg-zinc-200/80 px-2 py-0.5 text-[10px] dark:bg-zinc-800">{{ chat.threads.value.length }}</span></button><button type="button" class="shrink-0 rounded-xl border border-zinc-200 px-3 py-2 text-xs font-semibold text-zinc-600 transition hover:border-indigo-300 hover:bg-indigo-50 dark:border-zinc-700 dark:text-zinc-300 dark:hover:bg-zinc-800" @click="historyOpen = false; ui.openGeneralChat()">Nova conversa</button></div>
            <Transition name="history-list"><div v-if="historyOpen" class="mt-2 overflow-hidden rounded-xl border border-zinc-200 bg-white shadow-lg dark:border-zinc-800 dark:bg-zinc-900"><div class="border-b border-zinc-100 px-3 py-2 text-[10px] font-semibold uppercase tracking-wider text-zinc-400 dark:border-zinc-800">Suas conversas</div><div class="max-h-52 overflow-y-auto overscroll-contain p-1.5" @scroll="loadMoreHistory"><p v-if="!chat.threads.value.length" class="px-3 py-5 text-center text-xs text-zinc-500">Nenhuma conversa salva ainda.</p><div v-for="thread in chat.threads.value" :key="thread.id" class="group flex items-center gap-1 rounded-lg" :class="chat.currentThread.value?.id === thread.id ? 'bg-indigo-50 dark:bg-indigo-400/10' : 'hover:bg-zinc-50 dark:hover:bg-zinc-800/70'"><button type="button" class="min-w-0 flex-1 px-2.5 py-2 text-left" @click="selectHistory(thread.id)"><span class="block truncate text-xs font-medium text-zinc-700 dark:text-zinc-200">{{ thread.subject }}</span><span class="mt-0.5 block text-[10px] text-zinc-400">{{ new Date(thread.lastMessageAtUtc).toLocaleDateString('pt-BR') }}</span></button><button type="button" class="mr-1 grid h-8 w-8 shrink-0 place-items-center rounded-lg text-zinc-400 transition hover:bg-rose-100 hover:text-rose-600 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-rose-400 disabled:opacity-40 dark:hover:bg-rose-400/10 dark:hover:text-rose-300" :aria-label="`Excluir conversa: ${thread.subject}`" title="Excluir conversa" :disabled="archivingThreadId === thread.id" @click.stop="removeThread(thread.id)"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true" class="h-4 w-4"><path stroke-linecap="round" stroke-linejoin="round" d="M4 7h16m-10 4v6m4-6v6M5 7l1 13h12l1-13M9 7V4h6v3"/></svg></button></div><p v-if="chat.loadingMoreThreads.value" class="px-3 py-2 text-center text-[10px] text-zinc-400">Carregando mais conversas…</p><p v-else-if="!chat.hasMoreThreads.value && chat.threads.value.length" class="px-3 py-2 text-center text-[10px] text-zinc-400">Fim do histórico</p></div></div></Transition>
            <div v-if="contextLabel" class="mt-3 rounded-xl border border-indigo-100 bg-indigo-50/70 px-3 py-2.5 text-xs text-indigo-800 dark:border-indigo-400/15 dark:bg-indigo-400/5 dark:text-indigo-200"><p class="truncate font-semibold">Contexto: {{ contextLabel }}</p><div v-if="contextFacts.length" class="mt-1.5 flex flex-wrap gap-x-3 gap-y-1 opacity-80"><span v-for="fact in contextFacts" :key="fact[0]">{{ fact[0] }}: {{ fact[1] }}</span></div></div>
          </header>
          <div class="min-h-0 flex-1 space-y-5 overflow-y-auto bg-gradient-to-b from-zinc-50/70 to-white px-5 py-6 dark:from-zinc-900/40 dark:to-zinc-950" aria-live="polite">
            <p v-if="chat.loadingHistory.value" class="py-10 text-center text-sm text-zinc-500">Carregando conversa…</p>
            <div v-else-if="!chat.messages.value.length" class="mx-auto flex max-w-xs flex-col items-center py-16 text-center"><span class="rounded-full border border-indigo-200 bg-white px-3 py-1 text-[10px] font-semibold uppercase tracking-[.16em] text-indigo-600 shadow-sm dark:border-indigo-400/20 dark:bg-zinc-900 dark:text-indigo-300">Pronto para ajudar</span><p class="mt-4 text-base font-semibold tracking-tight text-zinc-800 dark:text-zinc-100">Por onde começamos?</p><p class="mt-2 text-xs leading-5 text-zinc-500">Pergunte sobre os dados e alertas disponíveis no seu ISM.</p></div>
            <div v-else class="space-y-5"><article v-for="message in chat.messages.value" :key="message.id" class="flex flex-col" :class="message.role === 'user' ? 'items-end' : 'items-start'"><p class="mb-1.5 px-1 text-[10px] font-semibold uppercase tracking-wider text-zinc-400">{{ message.role === 'user' ? 'Você' : 'ISM Assist' }}</p><div class="max-w-[90%] rounded-2xl px-4 py-3 text-[14px] leading-6 shadow-sm" :class="message.role === 'user' ? 'rounded-br-md bg-indigo-600 text-white shadow-indigo-900/10' : 'rounded-bl-md border border-zinc-200/80 bg-white text-zinc-700 dark:border-zinc-800 dark:bg-zinc-900 dark:text-zinc-200'"><p class="whitespace-pre-wrap break-words"><template v-for="(seg, idx) in parseFormattedSegments(message.content)" :key="idx"><strong v-if="seg.bold" class="font-semibold">{{ seg.text }}</strong><template v-else>{{ seg.text }}</template></template></p><p v-if="message.role === 'assistant' && message.mode" class="mt-2 border-t border-zinc-200/70 pt-1.5 text-[10px] opacity-55 dark:border-zinc-700">{{ message.source || (message.mode === 'llm' ? 'Assistente de IA' : 'Dados do ISM') }}</p></div></article><p v-if="chat.pending.value" class="animate-pulse px-1 text-xs text-zinc-500">Consultando informações autorizadas…</p></div>
          </div>
          <div v-if="chat.error.value" class="mx-5 mb-3 rounded-xl border border-red-200 bg-red-50 px-3 py-2.5 text-xs text-red-700 dark:border-red-900/60 dark:bg-red-950/20 dark:text-red-300" role="alert">{{ chat.error.value }} <button type="button" class="ml-1 font-semibold underline underline-offset-2" @click="retry">Tentar novamente</button></div>
          <form class="assistant-composer-shell border-t border-zinc-200/80 bg-white/90 p-3 pb-[calc(0.75rem+env(safe-area-inset-bottom))] dark:border-zinc-800 dark:bg-zinc-950/90" @submit.prevent="submit">
            <input ref="attachmentInputRef" type="file" accept="image/png,image/jpeg,image/webp" class="sr-only" @change="handleAttachmentChange" />
            <input ref="cameraInputRef" type="file" accept="image/png,image/jpeg,image/webp" capture="environment" class="sr-only" @change="handleAttachmentChange" />
            <div v-if="attachment" class="mb-2 flex items-center gap-2 rounded-xl border border-indigo-200/80 bg-indigo-50/70 px-2.5 py-2 dark:border-indigo-400/20 dark:bg-indigo-400/10">
              <img :src="attachmentPreview" alt="Prévia da imagem anexada" class="h-10 w-10 rounded-lg object-cover" />
              <span class="min-w-0 flex-1 truncate text-xs font-medium text-indigo-800 dark:text-indigo-100">{{ attachment.name }}</span>
              <button type="button" class="grid h-7 w-7 place-items-center rounded-lg text-indigo-500 transition hover:bg-indigo-100 hover:text-indigo-800 dark:hover:bg-indigo-400/20 dark:hover:text-white" aria-label="Remover imagem anexada" @click="clearAttachment">×</button>
            </div>
            <div class="assistant-composer flex items-center gap-1.5 rounded-[1.35rem] border border-zinc-200 bg-zinc-50 px-2 py-2 shadow-sm transition focus-within:border-indigo-400 focus-within:ring-2 focus-within:ring-indigo-500/15 dark:border-zinc-700 dark:bg-zinc-900">
              <button type="button" class="assistant-composer-action" aria-label="Tirar foto" title="Tirar foto" @click="openCamera">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" d="M4 8.5h3l1.2-2h3.6l1.2 2H16a3 3 0 0 1 3 3v5a3 3 0 0 1-3 3H4a3 3 0 0 1-3-3v-5a3 3 0 0 1 3-3Z"/><circle cx="10" cy="14" r="3"/></svg>
              </button>
              <button type="button" class="assistant-composer-action" aria-label="Anexar imagem" title="Anexar imagem" @click="openAttachmentPicker">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><rect width="18" height="18" x="3" y="3" rx="2"/><circle cx="8.5" cy="8.5" r="1.5"/><path stroke-linecap="round" stroke-linejoin="round" d="m21 15-3.1-3.1a2 2 0 0 0-2.8 0L6 21"/></svg>
              </button>
              <button type="button" class="assistant-composer-action" aria-label="Selecionar arquivo de imagem" title="Selecionar arquivo de imagem" @click="openAttachmentPicker">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" d="m3 8 1.4-3.1A2 2 0 0 1 6.2 3.7h3.1l1.5 1.8h7.1a3 3 0 0 1 3 3v8.8a2.7 2.7 0 0 1-2.7 2.7H5.8A2.8 2.8 0 0 1 3 17.2V8Z"/><path stroke-linecap="round" d="M3 9h18"/></svg>
              </button>
              <label for="assistant-chat-input" class="sr-only">Pergunta para o ISM Assist</label>
              <textarea id="assistant-chat-input" ref="inputRef" v-model="draft" rows="1" maxlength="1000" class="min-h-10 min-w-0 flex-1 resize-none bg-transparent px-2 py-2.5 text-sm leading-5 text-zinc-800 outline-none placeholder:text-zinc-400 dark:text-zinc-100" placeholder="Pergunte ao ISM Assist…" :disabled="!chat.ready.value || chat.pending.value" @keydown.enter.exact.prevent="submit" />
              <button type="submit" class="assistant-composer-send" aria-label="Enviar mensagem" :disabled="!chat.ready.value || chat.pending.value || !canSubmit">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" d="m5 12 14-7-4 14-3-6-7-1Z"/><path stroke-linecap="round" d="m12 13 3-2"/></svg>
              </button>
            </div>
            <p v-if="attachmentError" role="alert" class="mt-2 px-1 text-[11px] text-rose-600 dark:text-rose-300">{{ attachmentError }}</p>
            <p class="mt-2 px-1 text-[10px] text-zinc-400">Enter para enviar · imagens até 12 MB · Shift + Enter para nova linha</p>
          </form>
        </aside>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
.assistant-chat-enter-active, .assistant-chat-leave-active { transition: opacity 240ms ease; }
.assistant-chat-enter-active .assistant-chat-panel, .assistant-chat-leave-active .assistant-chat-panel { transition: transform 340ms cubic-bezier(.22,1,.36,1); }
.assistant-chat-enter-from, .assistant-chat-leave-to { opacity: 0; }
.assistant-chat-enter-from .assistant-chat-panel, .assistant-chat-leave-to .assistant-chat-panel { transform: translateX(32px); }
.history-list-enter-active, .history-list-leave-active { transition: opacity 180ms ease, transform 180ms ease; }
.history-list-enter-from, .history-list-leave-to { opacity: 0; transform: translateY(-6px); }
.assistant-composer-action,
.assistant-composer-send {
  display: grid;
  flex: 0 0 auto;
  place-items: center;
  border-radius: 9999px;
  transition: color 180ms ease, background-color 180ms ease, transform 180ms ease, opacity 180ms ease;
}

.assistant-composer-action {
  width: 2.25rem;
  height: 2.25rem;
  color: #a1a1aa;
}

.assistant-composer-action:hover,
.assistant-composer-action:focus-visible {
  background: rgb(99 102 241 / 0.1);
  color: #6366f1;
  outline: none;
}

.assistant-composer-action:active,
.assistant-composer-send:active { transform: scale(0.92); }

.assistant-composer-action svg,
.assistant-composer-send svg { width: 1.15rem; height: 1.15rem; }

.assistant-composer-send {
  width: 2.5rem;
  height: 2.5rem;
  color: white;
  background: linear-gradient(135deg, #6366f1, #8b5cf6);
  box-shadow: 0 5px 16px rgb(79 70 229 / 0.22);
}

.assistant-composer-send:hover:not(:disabled) { background: linear-gradient(135deg, #4f46e5, #7c3aed); }
.assistant-composer-send:focus-visible { outline: 2px solid #818cf8; outline-offset: 2px; }
.assistant-composer-send:disabled { cursor: not-allowed; opacity: 0.4; box-shadow: none; }
@media (prefers-reduced-motion: reduce) { .assistant-chat-enter-active, .assistant-chat-leave-active, .assistant-chat-enter-active .assistant-chat-panel, .assistant-chat-leave-active .assistant-chat-panel, .history-list-enter-active, .history-list-leave-active { transition-duration: 1ms; } }
</style>
