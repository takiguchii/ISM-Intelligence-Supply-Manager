<script setup lang="ts">
import { computed } from "vue";
import type { WeeklyRevenuePoint } from "~/composables/useDashboardMetrics";

const props = defineProps<{
  data: WeeklyRevenuePoint[];
}>();

const chartWidth = 520;
const chartHeight = 220;
const paddingLeft = 8;
const paddingRight = 8;
const paddingTop = 16;
const paddingBottom = 28;

const plotWidth = chartWidth - paddingLeft - paddingRight;
const plotHeight = chartHeight - paddingTop - paddingBottom;

const maxValue = computed(() => Math.max(...props.data.map((d) => d.value), 1) * 1.15);
const minValue = computed(() => Math.min(...props.data.map((d) => d.value), 0) * 0.85);

const points = computed(() =>
  props.data.map((point, index) => {
    const x =
      props.data.length > 1
        ? paddingLeft + (index / (props.data.length - 1)) * plotWidth
        : paddingLeft + plotWidth / 2;
    const range = maxValue.value - minValue.value || 1;
    const ratio = (point.value - minValue.value) / range;
    const y = paddingTop + plotHeight - ratio * plotHeight;
    return { ...point, x, y };
  })
);

const linePath = computed(() =>
  points.value.map((p, i) => `${i === 0 ? "M" : "L"} ${p.x} ${p.y}`).join(" ")
);

const areaPath = computed(() => {
  if (points.value.length === 0) return "";
  const last = points.value[points.value.length - 1];
  const first = points.value[0];
  return `${linePath.value} L ${last.x} ${paddingTop + plotHeight} L ${first.x} ${paddingTop + plotHeight} Z`;
});

const currency = (value: number) =>
  value.toLocaleString("pt-BR", { style: "currency", currency: "BRL", maximumFractionDigits: 0 });

const weekTotal = computed(() => props.data.reduce((sum, d) => sum + d.value, 0));

const growth = computed(() => {
  if (props.data.length < 2) return 0;
  const first = props.data[0].value;
  const last = props.data[props.data.length - 1].value;
  return first > 0 ? ((last - first) / first) * 100 : 0;
});
</script>

<template>
  <div class="p-6 rounded-2xl bg-zinc-900/80 border border-zinc-800/80 shadow-lg space-y-4 h-full">
    <div class="flex items-center justify-between gap-3 flex-wrap">
      <div>
        <span class="text-xs font-mono uppercase tracking-wider text-zinc-400">Receita Semanal</span>
        <p class="text-lg font-semibold text-white mt-1">{{ currency(weekTotal) }}</p>
      </div>
      <span
        class="text-xs font-mono px-2.5 py-1 rounded-full border"
        :class="growth >= 0
          ? 'text-emerald-400 border-emerald-400/30 bg-emerald-400/10'
          : 'text-red-400 border-red-400/30 bg-red-400/10'"
      >
        {{ growth >= 0 ? "▲" : "▼" }} {{ Math.abs(growth).toFixed(1) }}%
      </span>
    </div>

    <svg :viewBox="`0 0 ${chartWidth} ${chartHeight}`" class="w-full h-auto">
      <defs>
        <linearGradient id="weeklyRevenueGradient" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stop-color="rgb(52 211 153)" stop-opacity="0.35" />
          <stop offset="100%" stop-color="rgb(52 211 153)" stop-opacity="0" />
        </linearGradient>
      </defs>

      <path :d="areaPath" fill="url(#weeklyRevenueGradient)" />
      <path :d="linePath" fill="none" stroke="rgb(52 211 153)" stroke-width="2.5" stroke-linejoin="round" stroke-linecap="round" />

      <g v-for="p in points" :key="p.day">
        <circle :cx="p.x" :cy="p.y" r="3.5" fill="rgb(9 9 11)" stroke="rgb(52 211 153)" stroke-width="2" />
        <text :x="p.x" :y="chartHeight - 8" text-anchor="middle" fill="rgb(113 113 122)" font-size="10" font-family="monospace">
          {{ p.day }}
        </text>
      </g>
    </svg>
  </div>
</template>