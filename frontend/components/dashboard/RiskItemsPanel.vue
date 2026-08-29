<script setup lang="ts">
import { computed } from "vue";
import type { RiskItem } from "~/composables/useDashboardMetrics";

const props = defineProps<{
  items: RiskItem[];
}>();

const typeMap = {
  baixo_estoque: { label: "Estoque Baixo", color: "text-red-400", dot: "bg-red-400", badgeBg: "bg-red-400/10 border-red-400/30" },
  excesso_estoque: { label: "Excesso", color: "text-amber-400", dot: "bg-amber-400", badgeBg: "bg-amber-400/10 border-amber-400/30" },
  validade_proxima: { label: "Validade Próxima", color: "text-orange-400", dot: "bg-orange-400", badgeBg: "bg-orange-400/10 border-orange-400/30" }
} as const;

const items = computed(() => props.items.map((item) => ({ ...item, meta: typeMap[item.type] })));
</script>

<template>
  <div class="p-6 rounded-2xl bg-zinc-900/80 border border-zinc-800/80 shadow-lg space-y-4 h-full flex flex-col">
    <div class="flex items-center justify-between">
      <span class="text-xs font-mono uppercase tracking-wider text-zinc-400">Itens de Risco</span>
      <span class="text-xs font-mono text-zinc-500">{{ items.length }} ocorrência(s)</span>
    </div>

    <div v-if="items.length === 0" class="flex-1 flex items-center justify-center text-sm text-zinc-500 py-6">
      Nenhum item em risco no momento.
    </div>

    <ul v-else class="space-y-3 overflow-y-auto max-h-72 pr-1">
      <li
        v-for="item in items"
        :key="item.id"
        class="flex items-start gap-3 p-3 rounded-xl bg-zinc-950/60 border border-zinc-800/60"
      >
        <span class="w-2 h-2 rounded-full mt-1.5 shrink-0" :class="item.meta.dot"></span>
        <div class="flex-1 min-w-0">
          <div class="flex items-center justify-between gap-2 flex-wrap">
            <p class="text-sm font-medium text-white truncate">{{ item.name }}</p>
            <span class="text-[10px] font-mono px-2 py-0.5 rounded-full border shrink-0" :class="[item.meta.color, item.meta.badgeBg]">
              {{ item.meta.label }}
            </span>
          </div>
          <p class="text-xs text-zinc-500 mt-0.5">{{ item.detail }}</p>
        </div>
      </li>
    </ul>
  </div>
</template>