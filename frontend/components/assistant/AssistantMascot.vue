<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted } from "vue";
import { useAssistantUi } from "~/composables/useAssistantUi";
import { useAssistantChat } from "~/composables/useAssistantChat";
import { useSystemAlertCenter } from "~/composables/useSystemAlertCenter";
import AgentMascot2D from "~/components/assistant/AgentMascot2D.vue";
import AssistantChatDrawer from "~/components/assistant/AssistantChatDrawer.vue";
import type { AgentMood } from "~/types/agentExperience";

const ui = useAssistantUi();
const chat = useAssistantChat();
const alerts = useSystemAlertCenter();
const chatOpen = computed(() => ui.chatOpen.value);
const speechAlert = computed(() => alerts.newAlert.value);
const groupedNewCount = computed(() => alerts.groupedNewCount.value);
const unreadCount = computed(() => alerts.unreadCount.value);
const state = computed<AgentMood>(() => {
  if (chat.pending.value) return "thinking";
  if (chat.error.value && !chat.status.value?.providerReachable) return "offline";
  if (speechAlert.value) return "surprised";
  if (chatOpen.value) return "speaking";
  return "idle";
});

const openGeneral = () => ui.openGeneralChat();
const openNotifications = () => { if (typeof window !== "undefined") window.dispatchEvent(new CustomEvent("ism-notifications-open")); };
const handleExternalOpen = () => openGeneral();
const closeSpeech = () => alerts.clearNewAlert();
const openAlertChat = () => { if (speechAlert.value) ui.openChat(speechAlert.value); closeSpeech(); };

onMounted(() => {
  alerts.start();
  window.addEventListener("ism-assistant-open", handleExternalOpen);
});
onBeforeUnmount(() => { window.removeEventListener("ism-assistant-open", handleExternalOpen); alerts.stop(); });
</script>

<template>
  <div class="pointer-events-none fixed bottom-[calc(1rem+env(safe-area-inset-bottom))] right-[calc(1rem+env(safe-area-inset-right))] z-[45] flex flex-col items-end gap-2 sm:bottom-6 sm:right-6" data-assistant-shell>
    <div v-if="speechAlert" class="pointer-events-auto flex w-[min(340px,calc(100vw-2rem))] items-center gap-3 rounded-2xl border border-red-200 bg-white p-3 text-xs shadow-xl dark:border-red-500/30 dark:bg-zinc-900" role="status" aria-live="polite">
      <AgentMascot2D agent="general" :mood="state" size="sm" />
      <div class="min-w-0 flex-1">
        <p class="font-semibold text-zinc-900 dark:text-white">{{ groupedNewCount > 1 ? `${groupedNewCount} novos alertas` : speechAlert.title }}</p>
        <p class="mt-1 line-clamp-2 text-zinc-600 dark:text-zinc-400">{{ groupedNewCount > 1 ? "Confira o sino para ver os detalhes." : speechAlert.message }}</p>
        <div class="mt-2 flex flex-wrap gap-1.5"><button type="button" class="rounded-md px-2 py-1 font-semibold text-indigo-600 hover:bg-indigo-50 focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:text-indigo-300 dark:hover:bg-indigo-500/10" @click="openNotifications(); closeSpeech()">Ver alertas</button><button type="button" class="rounded-md px-2 py-1 font-semibold text-indigo-600 hover:bg-indigo-50 focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:text-indigo-300 dark:hover:bg-indigo-500/10" @click="openAlertChat">Conversar</button><button type="button" class="rounded-md px-2 py-1 font-semibold text-zinc-600 hover:bg-zinc-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:text-zinc-300 dark:hover:bg-zinc-800" @click="closeSpeech">Fechar</button></div>
      </div>
    </div>
    <div class="flex items-center gap-2">
      <button type="button" class="pointer-events-auto relative flex h-12 w-12 items-center justify-center rounded-xl border border-zinc-200 bg-white shadow-md transition hover:-translate-y-0.5 focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:border-zinc-700 dark:bg-zinc-900 motion-reduce:transform-none" :aria-expanded="chatOpen" aria-label="Abrir chat do ISM Assist" @click="openGeneral">
        <AgentMascot2D agent="general" :mood="state" size="xs" />
        <span v-if="unreadCount" class="absolute -right-1 -top-1 min-w-4 rounded-full bg-indigo-600 px-1 text-center text-[9px] font-bold leading-4 text-white">{{ unreadCount > 9 ? '9+' : unreadCount }}</span>
      </button>
    </div>
  </div>
  <AssistantChatDrawer />
</template>
