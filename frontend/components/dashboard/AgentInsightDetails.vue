<script setup lang="ts">
import type { AgentInsight } from "~/composables/useDashboardMetrics";

const props = defineProps<{ insight: AgentInsight | null; open: boolean }>();
const emit = defineEmits<{ close: []; converse: [] }>();
const factLabels: Record<string, string> = {
  CurrentQuantity: "Quantidade atual", MinimumQuantity: "Mínimo", Unit: "Unidade", MovingAverageConsumption: "Consumo médio", SuggestedPrice: "Preço sugerido", CurrentCmvPercent: "CMV atual", TargetMarginPercent: "Margem alvo"
};
</script>

<template>
  <Teleport to="body">
    <div v-if="open && insight" class="fixed inset-0 z-[55] flex items-end justify-center bg-black/25 p-3 sm:items-center" role="presentation" @click.self="emit('close')">
      <section class="max-h-[min(620px,calc(100vh-1.5rem))] w-full max-w-lg overflow-y-auto rounded-2xl border border-zinc-200 bg-white p-5 shadow-2xl dark:border-zinc-700 dark:bg-zinc-900" role="dialog" aria-modal="true" :aria-labelledby="`insight-title-${insight.id}`">
        <header class="flex items-start justify-between gap-4">
          <div><p class="text-[10px] font-bold uppercase tracking-wider text-zinc-500">{{ insight.label }} · {{ insight.severity === 'critical' ? 'Crítico' : insight.severity === 'warning' ? 'Atenção' : insight.severity === 'unknown' ? 'Sem dados' : 'Informativo' }}</p><h2 :id="`insight-title-${insight.id}`" class="mt-1 text-lg font-bold text-zinc-900 dark:text-white">{{ insight.title }}</h2></div>
          <button type="button" class="rounded-lg p-2 text-zinc-500 hover:bg-zinc-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:hover:bg-zinc-800" aria-label="Fechar detalhes" @click="emit('close')">×</button>
        </header>
        <p class="mt-4 text-sm leading-6 text-zinc-700 dark:text-zinc-300">{{ insight.description }}</p>
        <dl v-if="Object.keys(insight.facts || {}).length" class="mt-5 grid grid-cols-2 gap-2">
          <div v-for="(value, key) in insight.facts" :key="key" class="rounded-xl bg-zinc-50 px-3 py-2 dark:bg-zinc-950/60"><dt class="text-[10px] text-zinc-500">{{ factLabels[key] || key }}</dt><dd class="mt-0.5 text-sm font-semibold text-zinc-800 dark:text-zinc-200">{{ value }}</dd></div>
        </dl>
        <p v-if="insight.alertId && insight.generatedAtUtc" class="mt-4 text-[11px] text-zinc-500">Origem: alerta persistido #{{ insight.alertId }} · {{ new Date(insight.generatedAtUtc).toLocaleString('pt-BR') }}</p>
        <p v-else class="mt-4 text-[11px] text-zinc-500">Este especialista não possui uma ocorrência persistida para detalhar agora.</p>
        <footer class="mt-5 flex justify-end gap-2 border-t border-zinc-100 pt-4 dark:border-zinc-800"><button type="button" class="rounded-lg px-3 py-2 text-xs font-semibold text-zinc-600 hover:bg-zinc-100 dark:text-zinc-300 dark:hover:bg-zinc-800" @click="emit('close')">Fechar</button><button type="button" class="rounded-lg bg-indigo-600 px-3 py-2 text-xs font-semibold text-white hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500" @click="emit('converse')">Conversar sobre isso</button></footer>
      </section>
    </div>
  </Teleport>
</template>
