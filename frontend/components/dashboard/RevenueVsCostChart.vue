<script setup lang="ts">
import { computed } from "vue";
import type { RevenueVsCostPoint } from "~/composables/useDashboardMetrics";

const props = defineProps<{
  data: RevenueVsCostPoint[];
}>();

const chartWidth = 520;
const chartHeight = 220;
const paddingLeft = 12;
const paddingRight = 16;
const paddingTop = 16;
const paddingBottom = 28;

const plotWidth = chartWidth - paddingLeft - paddingRight;
const plotHeight = chartHeight - paddingTop - paddingBottom;

const maxValue = computed(() => {
  const values = props.data.flatMap((d) => [d.receita, d.custo]);
  return Math.max(...values, 1) * 1.15;
});

const groupWidth = computed(() => plotWidth / Math.max(props.data.length, 1));
const barWidth = computed(() => Math.min(22, groupWidth.value / 3));

const bars = computed(() =>
  props.data.map((point, index) => {
    const groupX = paddingLeft + index * groupWidth.value + groupWidth.value / 2;
    const receitaHeight = (point.receita / maxValue.value) * plotHeight;
    const custoHeight = (point.custo / maxValue.value) * plotHeight;

    return {
      label: point.label,
      x: groupX,
      receitaX: groupX - barWidth.value - 2,
      custoX: groupX + 2,
      receitaY: paddingTop + plotHeight - receitaHeight,
      custoY: paddingTop + plotHeight - custoHeight,
      receitaHeight,
      custoHeight
    };
  })
);

const currency = (value: number) =>
  value.toLocaleString("pt-BR", { style: "currency", currency: "BRL", maximumFractionDigits: 0 });

const totals = computed(() => {
  const receita = props.data.reduce((sum, d) => sum + d.receita, 0);
  const custo = props.data.reduce((sum, d) => sum + d.custo, 0);
  return { receita, custo, margem: receita > 0 ? ((receita - custo) / receita) * 100 : 0 };
});
</script>

<template>
  <div class="p-6 rounded-2xl bg-zinc-900/80 border border-zinc-800/80 shadow-lg space-y-4 h-full">
    <div class="flex items-center justify-between gap-3 flex-wrap">
      <div>
        <span class="text-xs font-mono uppercase tracking-wider text-zinc-400">Receita vs. Custo</span>
        <p class="text-lg font-semibold text-white mt-1">
          Margem consolidada: {{ totals.margem.toFixed(1) }}%
        </p>
      </div>
      <div class="flex items-center gap-4 text-xs">
        <div class="flex items-center gap-1.5">
          <span class="w-2.5 h-2.5 rounded-sm bg-emerald-400"></span>
          <span class="text-zinc-400">Receita</span>
        </div>
        <div class="flex items-center gap-1.5">
          <span class="w-2.5 h-2.5 rounded-sm bg-amber-400"></span>
          <span class="text-zinc-400">Custo</span>
        </div>
      </div>
    </div>

    <svg :viewBox="`0 0 ${chartWidth} ${chartHeight}`" class="w-full h-auto">
      <line
        v-for="n in 4"
        :key="`grid-${n}`"
        :x1="paddingLeft"
        :x2="chartWidth - paddingRight"
        :y1="paddingTop + (n * plotHeight) / 4"
        :y2="paddingTop + (n * plotHeight) / 4"
        stroke="rgb(63 63 70 / 0.5)"
        stroke-width="1"
      />

      <g v-for="bar in bars" :key="bar.label">
        <rect :x="bar.receitaX" :y="bar.receitaY" :width="barWidth" :height="bar.receitaHeight" rx="3" fill="rgb(52 211 153)" />
        <rect :x="bar.custoX" :y="bar.custoY" :width="barWidth" :height="bar.custoHeight" rx="3" fill="rgb(251 191 36)" />
        <text :x="bar.x" :y="chartHeight - 10" text-anchor="middle" fill="rgb(113 113 122)" font-size="10" font-family="monospace">
          {{ bar.label }}
        </text>
      </g>
    </svg>

    <div class="flex items-center justify-between text-xs text-zinc-500 pt-3 border-t border-zinc-800/80">
      <span>Receita total: <span class="text-zinc-300 font-medium">{{ currency(totals.receita) }}</span></span>
      <span>Custo total: <span class="text-zinc-300 font-medium">{{ currency(totals.custo) }}</span></span>
    </div>
  </div>
</template>