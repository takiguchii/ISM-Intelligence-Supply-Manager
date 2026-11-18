<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from "vue";
import { useAssistantUi } from "~/composables/useAssistantUi";
import { useAssistantChat } from "~/composables/useAssistantChat";
import AgentMascot2D from "~/components/assistant/AgentMascot2D.vue";
import { agentForAlert, moodForAlert, type AgentMood } from "~/types/agentExperience";

const ui = useAssistantUi();
const chat = useAssistantChat();
const draft = ref("");
const isChatOpen = computed(() => ui.chatOpen.value);
const requestedOrigin = computed(() => ui.requestedAlert.value?.id ?? null);
const contextLabel = computed(() => chat.context.value?.entityName || null);
const contextFacts = computed(() => Object.entries(chat.context.value?.facts || {}).slice(0, 5));
const chatAgent = computed(() => {
  const context = chat.context.value;
  return context ? agentForAlert(context) : "general";
});
const chatMood = computed<AgentMood>(() => {
  if (chat.pending.value || chat.loadingHistory.value) return "thinking";
  if (chat.error.value) return "error";
  return chat.context.value ? moodForAlert(chat.context.value) : "curious";
});
const inputRef = ref<HTMLTextAreaElement | null>(null);
const dialogRef = ref<HTMLElement | null>(null);
let returnFocusTo: HTMLElement | null = null;

const prepare = async (request: number) => {
  draft.value = ui.requestedDraft.value;
  const preparation = chat.ensureReady(requestedOrigin.value);
  await nextTick();
  if (ui.chatOpen.value && request === ui.openRequest.value) dialogRef.value?.focus();
  await preparation;
  await nextTick();
  if (ui.chatOpen.value && request === ui.openRequest.value && document.activeElement === dialogRef.value) {
    inputRef.value?.focus();
  }
};
const selectHistory = (event: Event) => {
  const id = Number((event.target as HTMLSelectElement).value);
  if (id > 0) { draft.value = ""; void chat.selectThread(id); }
};
const retry = async () => { if (await chat.retry() === true) draft.value = ""; };
const submit = async () => { const sent = await chat.send(draft.value); if (sent) draft.value = ""; };
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
onBeforeUnmount(() => { window.removeEventListener("keydown", handleKeydown); restoreBody(); });
</script>

<template>
  <Teleport to="body">
    <Transition name="assistant-chat">
      <div v-if="isChatOpen" class="fixed inset-0 z-[60]">
        <button class="assistant-chat-backdrop absolute inset-0 h-full w-full bg-zinc-950/35 backdrop-blur-[2px]" aria-label="Fechar chat" @click="ui.closeChat" />
        <aside ref="dialogRef" class="assistant-chat-panel absolute inset-y-0 right-0 flex w-full max-w-[480px] flex-col border-l border-zinc-200/80 bg-white/95 shadow-2xl shadow-zinc-950/20 backdrop-blur-xl dark:border-zinc-800 dark:bg-zinc-950/95" role="dialog" aria-modal="true" aria-labelledby="assistant-chat-title" tabindex="-1">
          <header class="border-b border-zinc-200/80 px-5 pb-4 pt-[calc(1.25rem+env(safe-area-inset-top))] dark:border-zinc-800">
            <div class="flex items-center justify-between gap-4">
              <div class="flex min-w-0 items-center gap-3"><div class="flex h-12 w-14 items-center justify-center rounded-2xl bg-indigo-50 dark:bg-indigo-400/10"><AgentMascot2D :agent="chatAgent" :mood="chatMood" size="sm" /></div><div class="min-w-0"><p class="text-[10px] font-semibold uppercase tracking-[.18em] text-indigo-600 dark:text-indigo-300">Assistente ISM</p><h2 id="assistant-chat-title" class="mt-0.5 truncate text-lg font-semibold tracking-tight text-zinc-900 dark:text-white">Como posso ajudar?</h2><p class="mt-1 truncate text-xs text-zinc-500">{{ chat.status.value?.providerReachable ? `Conectado · ${chat.status.value.model}` : chat.status.value?.enabled ? 'Modelo local indisponível' : 'Conversação local desativada' }}</p></div></div>
              <button type="button" class="grid h-10 w-10 shrink-0 place-items-center rounded-xl text-xl text-zinc-500 transition hover:bg-zinc-100 hover:text-zinc-900 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-indigo-500 dark:hover:bg-zinc-800 dark:hover:text-white" aria-label="Fechar chat" @click="ui.closeChat">×</button>
            </div>
            <div class="mt-4 flex gap-2"><select class="min-w-0 flex-1 rounded-xl border border-zinc-200 bg-zinc-50/70 px-3 py-2.5 text-xs text-zinc-700 outline-none transition focus:border-indigo-400 focus:ring-2 focus:ring-indigo-500/20 dark:border-zinc-700 dark:bg-zinc-900 dark:text-zinc-300" aria-label="Histórico de conversas" :value="chat.currentThread.value?.id || ''" @change="selectHistory($event)"><option value="" disabled>{{ chat.threads.value.length ? "Selecione uma conversa" : "Sem histórico" }}</option><option v-for="thread in chat.threads.value" :key="thread.id" :value="thread.id">{{ thread.subject }}</option></select><button type="button" class="shrink-0 rounded-xl border border-zinc-200 px-3 py-2 text-xs font-semibold text-zinc-600 transition hover:border-indigo-300 hover:bg-indigo-50 dark:border-zinc-700 dark:text-zinc-300 dark:hover:bg-zinc-800" @click="ui.openGeneralChat()">Nova conversa</button></div>
            <div v-if="contextLabel" class="mt-3 rounded-xl border border-indigo-100 bg-indigo-50/70 px-3 py-2.5 text-xs text-indigo-800 dark:border-indigo-400/15 dark:bg-indigo-400/5 dark:text-indigo-200"><p class="truncate font-semibold">Contexto: {{ contextLabel }}</p><div v-if="contextFacts.length" class="mt-1.5 flex flex-wrap gap-x-3 gap-y-1 opacity-80"><span v-for="fact in contextFacts" :key="fact[0]">{{ fact[0] }}: {{ fact[1] }}</span></div></div>
          </header>
          <div class="min-h-0 flex-1 space-y-5 overflow-y-auto bg-gradient-to-b from-zinc-50/70 to-white px-5 py-6 dark:from-zinc-900/40 dark:to-zinc-950" aria-live="polite">
            <p v-if="chat.loadingHistory.value" class="py-10 text-center text-sm text-zinc-500">Carregando conversa…</p>
            <div v-else-if="!chat.messages.value.length" class="mx-auto flex max-w-xs flex-col items-center py-16 text-center"><span class="rounded-full border border-indigo-200 bg-white px-3 py-1 text-[10px] font-semibold uppercase tracking-[.16em] text-indigo-600 shadow-sm dark:border-indigo-400/20 dark:bg-zinc-900 dark:text-indigo-300">Pronto para ajudar</span><p class="mt-4 text-base font-semibold tracking-tight text-zinc-800 dark:text-zinc-100">Por onde começamos?</p><p class="mt-2 text-xs leading-5 text-zinc-500">Pergunte sobre os dados e alertas disponíveis no seu ISM.</p></div>
            <div v-else class="space-y-5"><article v-for="message in chat.messages.value" :key="message.id" class="flex flex-col" :class="message.role === 'user' ? 'items-end' : 'items-start'"><p class="mb-1.5 px-1 text-[10px] font-semibold uppercase tracking-wider text-zinc-400">{{ message.role === 'user' ? 'Você' : 'ISM Assist' }}</p><div class="max-w-[90%] rounded-2xl px-4 py-3 text-[14px] leading-6 shadow-sm" :class="message.role === 'user' ? 'rounded-br-md bg-indigo-600 text-white shadow-indigo-900/10' : 'rounded-bl-md border border-zinc-200/80 bg-white text-zinc-700 dark:border-zinc-800 dark:bg-zinc-900 dark:text-zinc-200'"><p class="whitespace-pre-wrap break-words">{{ message.content }}</p><p v-if="message.role === 'assistant' && message.mode" class="mt-2 border-t border-zinc-200/70 pt-1.5 text-[10px] opacity-55 dark:border-zinc-700">{{ message.source || (message.mode === 'llm' ? 'Assistente local' : 'Dados do ISM') }}</p></div></article><p v-if="chat.pending.value" class="animate-pulse px-1 text-xs text-zinc-500">Consultando informações autorizadas…</p></div>
          </div>
          <div v-if="chat.error.value" class="mx-5 mb-3 rounded-xl border border-red-200 bg-red-50 px-3 py-2.5 text-xs text-red-700 dark:border-red-900/60 dark:bg-red-950/20 dark:text-red-300" role="alert">{{ chat.error.value }} <button type="button" class="ml-1 font-semibold underline underline-offset-2" @click="retry">Tentar novamente</button></div>
          <form class="border-t border-zinc-200/80 bg-white/90 p-4 pb-[calc(1rem+env(safe-area-inset-bottom))] dark:border-zinc-800 dark:bg-zinc-950/90" @submit.prevent="submit"><label for="assistant-chat-input" class="sr-only">Pergunta para o ISM Assist</label><div class="flex items-end gap-2 rounded-2xl border border-zinc-200 bg-zinc-50 p-2 transition focus-within:border-indigo-400 focus-within:ring-2 focus-within:ring-indigo-500/15 dark:border-zinc-700 dark:bg-zinc-900"><textarea id="assistant-chat-input" ref="inputRef" v-model="draft" rows="2" maxlength="1000" class="min-w-0 flex-1 resize-none bg-transparent px-2 py-2 text-sm leading-5 text-zinc-800 outline-none placeholder:text-zinc-400 dark:text-zinc-100" placeholder="Escreva sua pergunta…" :disabled="!chat.ready.value || chat.pending.value" @keydown.enter.exact.prevent="submit"/><button type="submit" class="rounded-xl bg-indigo-600 px-4 py-2.5 text-xs font-semibold text-white shadow-sm transition hover:bg-indigo-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-indigo-500 focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-45 dark:focus-visible:ring-offset-zinc-950" :disabled="!chat.ready.value || chat.pending.value || !draft.trim()">Enviar</button></div><p class="mt-2 px-1 text-[10px] text-zinc-400">Enter para enviar · Shift + Enter para nova linha</p></form>
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
@media (prefers-reduced-motion: reduce) { .assistant-chat-enter-active, .assistant-chat-leave-active, .assistant-chat-enter-active .assistant-chat-panel, .assistant-chat-leave-active .assistant-chat-panel { transition-duration: 1ms; } }
</style>
