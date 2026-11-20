<script setup lang="ts">
import { computed } from "vue";
import type { AgentInsight } from "~/composables/useDashboardMetrics";
import AgentMascot2D from "~/components/assistant/AgentMascot2D.vue";

const props = defineProps<{ insight: AgentInsight }>();
const emit = defineEmits<{ details: []; converse: [] }>();
const labels = { stock: "Estoque", pricing: "Precificação", suppliers: "Fornecedores" } as const;
const styles = {
  info: "border-sky-200 bg-sky-50/40 dark:border-sky-400/20 dark:bg-sky-400/5",
  warning: "border-amber-200 bg-amber-50/50 dark:border-amber-400/20 dark:bg-amber-400/5",
  critical: "border-red-200 bg-red-50/40 dark:border-red-400/20 dark:bg-red-400/5",
  success: "border-emerald-200 bg-emerald-50/40 dark:border-emerald-400/20 dark:bg-emerald-400/5",
  unknown: "border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-900/80"
} as const;
const severityLabels = { info: "Informativo", warning: "Atenção", critical: "Crítico", success: "Confirmado", unknown: "Sem dados" } as const;
const label = computed(() => labels[props.insight.agent as keyof typeof labels] || props.insight.label);
const relativeTime = computed(() => {
  if (!props.insight.generatedAtUtc) return "sem data";
  const delta = Math.max(0, Date.now() - new Date(props.insight.generatedAtUtc).getTime());
  const minutes = Math.floor(delta / 60000);
  if (minutes < 1) return "agora";
  if (minutes < 60) return `há ${minutes} min`;
  const hours = Math.floor(minutes / 60);
  return hours < 24 ? `há ${hours} h` : new Date(props.insight.generatedAtUtc).toLocaleDateString("pt-BR");
});
</script>

<template>
  <article class="group flex min-h-[188px] flex-col rounded-2xl border p-4 shadow-sm transition hover:-translate-y-0.5 hover:shadow-md dark:shadow-black/10" :class="styles[insight.severity]" :data-agent-card="insight.agent">
    <div class="flex min-h-0 flex-1 items-center gap-3">
      <div class="flex shrink-0 items-center rounded-xl">
        <AgentMascot2D :agent="insight.agent" :mood="insight.mood" size="md" :attention-level="insight.attentionLevel" :interactive="true" @activate="emit('details')" />
      </div>
      <div class="min-w-0 self-stretch py-1">
        <div class="flex flex-wrap items-center gap-2">
          <p class="text-[10px] font-bold uppercase tracking-[.14em] text-zinc-500">{{ label }}</p>
          <span class="rounded-full border border-current/20 px-2 py-0.5 text-[10px] font-semibold" :class="insight.severity === 'critical' ? 'text-red-700 dark:text-red-300' : insight.severity === 'warning' ? 'text-amber-700 dark:text-amber-300' : 'text-zinc-600 dark:text-zinc-300'">{{ severityLabels[insight.severity] }}</span>
          <span v-if="insight.unreadCount" class="text-[10px] font-semibold text-indigo-600 dark:text-indigo-300">{{ insight.unreadCount }} nova(s)</span>
        </div>
        <h3 class="mt-2 line-clamp-2 text-sm font-bold leading-5 text-zinc-900 dark:text-white">{{ insight.title }}</h3>
        <p class="mt-1 line-clamp-3 text-xs leading-4 text-zinc-600 dark:text-zinc-400">{{ insight.description }}</p>
        <p class="mt-2 text-[10px] text-zinc-400">{{ relativeTime }}<span v-if="insight.alertIds.length"> · {{ insight.alertIds.length }} ocorrência(s)</span></p>
      </div>
    </div>
    <div class="mt-3 flex items-center justify-end gap-2 border-t border-zinc-900/10 pt-3 dark:border-white/10">
      <button type="button" class="min-h-8 rounded-lg px-2.5 text-[11px] font-semibold text-indigo-700 hover:bg-indigo-500/10 focus:outline-none focus-visible:ring-2 focus-visible:ring-indigo-500 dark:text-indigo-300" @click="emit('details')">Detalhes</button>
      <button type="button" class="min-h-8 rounded-lg bg-zinc-900 px-2.5 text-[11px] font-semibold text-white hover:bg-zinc-700 focus:outline-none focus-visible:ring-2 focus-visible:ring-indigo-500 dark:bg-white dark:text-zinc-900 dark:hover:bg-zinc-200" @click="emit('converse')">Conversar</button>
    </div>
  </article>
</template>
