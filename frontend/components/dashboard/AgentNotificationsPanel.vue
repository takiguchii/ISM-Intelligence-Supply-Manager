<script setup lang="ts">
import { computed } from "vue";
import type { AgentNotification } from "~/composables/useDashboardMetrics";

const props = defineProps<{
  notifications: AgentNotification[];
}>();

const severityMap = {
  info: { dot: "bg-sky-400" },
  atencao: { dot: "bg-amber-400" },
  critico: { dot: "bg-red-400" }
} as const;

const items = computed(() => props.notifications.map((n) => ({ ...n, meta: severityMap[n.severity] })));
</script>

<template>
  <div class="p-6 rounded-2xl bg-zinc-900/80 border border-zinc-800/80 shadow-lg space-y-4 h-full flex flex-col">
    <div class="flex items-center justify-between">
      <span class="text-xs font-mono uppercase tracking-wider text-zinc-400">Notificações dos Agentes</span>
      <span class="w-2 h-2 rounded-full bg-emerald-400 animate-pulse"></span>
    </div>

    <div v-if="items.length === 0" class="flex-1 flex items-center justify-center text-sm text-zinc-500 py-6">
      Nenhuma notificação no momento.
    </div>

    <ul v-else class="space-y-3 overflow-y-auto max-h-72 pr-1">
      <li v-for="item in items" :key="item.id" class="p-3 rounded-xl bg-zinc-950/60 border border-zinc-800/60 space-y-1.5">
        <div class="flex items-center justify-between gap-2 flex-wrap">
          <div class="flex items-center gap-2 min-w-0">
            <span class="w-1.5 h-1.5 rounded-full shrink-0" :class="item.meta.dot"></span>
            <span class="text-xs font-semibold text-zinc-300 truncate">{{ item.agent }}</span>
          </div>
          <span class="text-[10px] text-zinc-500 shrink-0 font-mono">{{ item.time }}</span>
        </div>
        <p class="text-xs text-zinc-400 leading-relaxed">{{ item.message }}</p>
      </li>
    </ul>
  </div>
</template>