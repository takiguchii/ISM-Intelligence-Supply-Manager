<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from "vue";
import { useAssistantUi } from "~/composables/useAssistantUi";
import { useAssistantChat } from "~/composables/useAssistantChat";

const ui = useAssistantUi();
const chat = useAssistantChat();
const draft = ref("");
const isChatOpen = computed(() => ui.chatOpen.value);
const requestedOrigin = computed(() => ui.requestedAlert.value?.id ?? null);
const contextLabel = computed(() => chat.context.value?.entityName || null);
const contextFacts = computed(() => Object.entries(chat.context.value?.facts || {}).slice(0, 5));
const inputRef = ref<HTMLTextAreaElement | null>(null);

const prepare = async () => { if (ui.chatOpen.value) { await chat.ensureReady(requestedOrigin.value); await nextTick(); inputRef.value?.focus(); } };
const submit = async () => { const sent = await chat.send(draft.value); if (sent) draft.value = ""; };
const handleKeydown = (event: KeyboardEvent) => { if (event.key === "Escape" && ui.chatOpen.value) ui.closeChat(); };
const restoreBody = () => { if (typeof document !== "undefined") document.body.style.overflow = ""; };

watch([ui.chatOpen, requestedOrigin], () => {
  if (typeof document !== "undefined") document.body.style.overflow = ui.chatOpen.value ? "hidden" : "";
  void prepare();
});
onMounted(() => window.addEventListener("keydown", handleKeydown));
onBeforeUnmount(() => { window.removeEventListener("keydown", handleKeydown); restoreBody(); });
</script>

<template>
  <Teleport to="body">
    <template v-if="isChatOpen">
      <div class="fixed inset-0 z-[60] bg-black/25 backdrop-blur-[1px]" aria-hidden="true" @click="ui.closeChat" />
      <aside class="fixed inset-y-0 right-0 z-[61] flex w-full max-w-[430px] flex-col border-l border-zinc-200 bg-white shadow-2xl dark:border-zinc-800 dark:bg-zinc-950" role="dialog" aria-modal="true" aria-labelledby="assistant-chat-title">
        <header class="border-b border-zinc-200 px-4 py-3 dark:border-zinc-800">
          <div class="flex items-start justify-between gap-3"><div><h2 id="assistant-chat-title" class="text-sm font-bold text-zinc-900 dark:text-white">ISM Assist</h2><p class="mt-0.5 text-[11px] text-zinc-500">{{ chat.status.value?.providerReachable ? `Ollama · ${chat.status.value.model}` : chat.status.value?.enabled ? 'Modelo local indisponível' : 'Conversação local desativada' }}</p></div><button type="button" class="rounded-lg p-2 text-zinc-500 hover:bg-zinc-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:hover:bg-zinc-800" aria-label="Fechar chat" @click="ui.closeChat">×</button></div>
          <div class="mt-3 flex gap-2"><select class="min-w-0 flex-1 rounded-lg border border-zinc-200 bg-transparent px-2 py-2 text-xs text-zinc-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:border-zinc-700 dark:text-zinc-300" aria-label="Histórico de conversas" :value="chat.currentThread.value?.id || ''" @change="chat.selectThread(chat.threads.value.find(thread => thread.id === Number(($event.target as HTMLSelectElement).value))!)"><option v-if="!chat.threads.value.length" value="">Sem histórico</option><option v-for="thread in chat.threads.value" :key="thread.id" :value="thread.id">{{ thread.subject }}</option></select><button type="button" class="shrink-0 rounded-lg border border-zinc-200 px-2.5 py-2 text-[11px] font-semibold text-zinc-600 hover:bg-zinc-100 dark:border-zinc-700 dark:text-zinc-300 dark:hover:bg-zinc-800" @click="chat.newConversation()">Nova</button></div>
          <div v-if="contextLabel" class="mt-2 rounded-md bg-indigo-50 px-2 py-1 text-[11px] text-indigo-700 dark:bg-indigo-500/10 dark:text-indigo-300"><p class="truncate font-semibold">Analisando: {{ contextLabel }}</p><div v-if="contextFacts.length" class="mt-1 flex flex-wrap gap-x-2 gap-y-0.5 font-normal opacity-80"><span v-for="fact in contextFacts" :key="fact[0]">{{ fact[0] }}: {{ fact[1] }}</span></div></div>
        </header>
        <div class="min-h-0 flex-1 overflow-y-auto px-4 py-4" aria-live="polite">
          <p v-if="chat.loadingHistory.value" class="py-8 text-center text-xs text-zinc-500">Carregando conversa…</p>
          <p v-else-if="!chat.messages.value.length" class="py-8 text-center text-xs text-zinc-500">Nenhuma mensagem nesta conversa.</p>
          <div v-else class="space-y-3"><article v-for="message in chat.messages.value" :key="message.id" class="max-w-[92%] rounded-xl px-3 py-2 text-sm" :class="message.role === 'user' ? 'ml-auto bg-indigo-600 text-white' : 'mr-auto bg-zinc-100 text-zinc-700 dark:bg-zinc-900 dark:text-zinc-200'"><p class="whitespace-pre-wrap break-words">{{ message.content }}</p><p v-if="message.role === 'assistant' && message.mode" class="mt-1 text-[10px] opacity-60">{{ message.mode === 'llm' ? 'Ollama local' : 'Fonte determinística do ISM' }}</p></article><p v-if="chat.pending.value" class="text-xs text-zinc-500">Analisando dados autorizados…</p></div>
        </div>
        <div v-if="chat.error.value" class="mx-4 mb-2 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-xs text-red-700 dark:border-red-900/60 dark:bg-red-950/20 dark:text-red-300" role="alert">{{ chat.error.value }} <button type="button" class="ml-1 underline" @click="chat.retry()">Tentar novamente</button></div>
        <form class="flex gap-2 border-t border-zinc-200 p-3 dark:border-zinc-800" @submit.prevent="submit"><label for="assistant-chat-input" class="sr-only">Pergunta para o ISM Assist</label><textarea id="assistant-chat-input" ref="inputRef" v-model="draft" rows="2" maxlength="1000" class="min-w-0 flex-1 resize-none rounded-lg border border-zinc-300 bg-transparent px-3 py-2 text-sm outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 dark:border-zinc-700" placeholder="Pergunte sobre estoque, preços ou fornecedores…" :disabled="chat.pending.value" @keydown.enter.exact.prevent="submit"/><button type="submit" class="self-end rounded-lg bg-indigo-600 px-3 py-2 text-xs font-semibold text-white hover:bg-indigo-700 disabled:opacity-50" :disabled="chat.pending.value || !draft.trim()">Enviar</button></form>
      </aside>
    </template>
  </Teleport>
</template>
