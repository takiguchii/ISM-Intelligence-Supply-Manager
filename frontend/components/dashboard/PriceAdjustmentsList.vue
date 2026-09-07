<script setup lang="ts">
import { computed } from "vue";
import type { PriceAdjustment } from "~/composables/useDashboardMetrics";

const props = defineProps<{
    items: PriceAdjustment[];
}>();

const rows = computed(() =>
    props.items.map((item) => ({
        ...item,
        diff: (((item.to - item.from) / item.from) * 100).toFixed(1)
    }))
);

const price = (value: number) => value.toLocaleString("pt-BR", { minimumFractionDigits: 2 });
</script>

<template>
    <div class="p-6 rounded-2xl bg-zinc-900/80 border border-zinc-800/80 shadow-lg space-y-4">
        <div class="flex items-center justify-between flex-wrap gap-2">
            <div>
                <h3 class="text-sm font-semibold text-white">Sugestões de reajuste — Agente de Precificação</h3>
                <p class="text-xs text-zinc-500">Top {{ items.length }} itens com maior potencial de ganho</p>
            </div>
            <span
                class="inline-flex items-center gap-1.5 rounded-full border border-amber-400/30 bg-amber-400/10 px-2.5 py-1 text-[10px] font-semibold uppercase tracking-wider text-amber-400">
                <el-icon :size="12">
                    <Opportunity />
                </el-icon> IA
            </span>
        </div>

        <div class="divide-y divide-zinc-800/60">
            <div v-for="row in rows" :key="row.id" class="flex items-center justify-between gap-4 py-3 flex-wrap">
                <div class="min-w-0 flex-1">
                    <p class="text-sm font-medium text-white truncate">{{ row.name }}</p>
                    <p class="text-xs text-zinc-500">{{ row.reason }}</p>
                </div>
                <div class="flex items-center gap-3 text-sm shrink-0">
                    <span class="text-zinc-500 line-through">R$ {{ price(row.from) }}</span>
                    <span class="font-semibold text-white">R$ {{ price(row.to) }}</span>
                    <span class="inline-flex items-center gap-1 rounded-md border px-2 py-0.5 text-[11px] font-semibold"
                        :class="row.direction === 'up'
                            ? 'border-emerald-400/30 bg-emerald-400/10 text-emerald-400'
                            : 'border-sky-400/30 bg-sky-400/10 text-sky-400'">
                        <el-icon :size="10">
                            <component :is="row.direction === 'up' ? 'ArrowUpBold' : 'ArrowDownBold'" />
                        </el-icon>
                        {{ row.direction === "up" ? "+" : "" }}{{ row.diff }}%
                    </span>
                </div>
            </div>
        </div>
    </div>
</template>