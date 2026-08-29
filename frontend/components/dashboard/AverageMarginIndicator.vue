<script setup lang="ts">
import { computed } from "vue";

const props = defineProps<{
  value: number;
  trend: number;
}>();

const radius = 46;
const circumference = 2 * Math.PI * radius;

const offset = computed(() => circumference - (Math.min(Math.max(props.value, 0), 100) / 100) * circumference);
const trendPositive = computed(() => props.trend >= 0);
</script>

<template>
  <div class="p-6 rounded-2xl bg-zinc-900/80 border border-zinc-800/80 shadow-lg flex items-center gap-5 h-full">
    <svg viewBox="0 0 120 120" class="w-24 h-24 shrink-0 -rotate-90">
      <circle cx="60" cy="60" :r="radius" fill="none" stroke="rgb(39 39 42)" stroke-width="10" />
      <circle
        cx="60"
        cy="60"
        :r="radius"
        fill="none"
        stroke="rgb(52 211 153)"
        stroke-width="10"
        stroke-linecap="round"
        :stroke-dasharray="circumference"
        :stroke-dashoffset="offset"
        style="transition: stroke-dashoffset 0.6s ease"
      />
    </svg>
    <div class="space-y-1 min-w-0">
      <span class="text-xs font-mono uppercase tracking-wider text-zinc-400">Margem Média</span>
      <p class="text-3xl font-bold text-white">{{ value.toFixed(1) }}%</p>
      <p class="text-xs font-mono flex items-center gap-1" :class="trendPositive ? 'text-emerald-400' : 'text-red-400'">
        <span>{{ trendPositive ? "▲" : "▼" }}</span>
        <span>{{ Math.abs(trend).toFixed(1) }} p.p. vs. período anterior</span>
      </p>
    </div>
  </div>
</template>