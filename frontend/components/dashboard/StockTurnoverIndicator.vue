<script setup lang="ts">
import { computed } from "vue";

const props = defineProps<{
  value: number;
  trend: number;
  status: "saudavel" | "lento" | "critico";
}>();

const statusMap = {
  saudavel: { label: "Saudável", color: "text-emerald-400", bar: "bg-emerald-400", badgeBg: "bg-emerald-400/10 border-emerald-400/30" },
  lento: { label: "Lento", color: "text-amber-400", bar: "bg-amber-400", badgeBg: "bg-amber-400/10 border-amber-400/30" },
  critico: { label: "Crítico", color: "text-red-400", bar: "bg-red-400", badgeBg: "bg-red-400/10 border-red-400/30" }
} as const;

const current = computed(() => statusMap[props.status]);

// Escala de referência apenas para a barra visual (0 a 8 giros/mês).
const barWidth = computed(() => `${Math.min((props.value / 8) * 100, 100)}%`);

const trendPositive = computed(() => props.trend >= 0);
</script>

<template>
  <div class="p-6 rounded-2xl bg-zinc-900/80 border border-zinc-800/80 shadow-lg space-y-3 h-full">
    <div class="flex items-center justify-between">
      <span class="text-xs font-mono uppercase tracking-wider text-zinc-400">Giro de Estoque</span>
      <span class="text-xs font-mono px-2.5 py-1 rounded-full border" :class="[current.color, current.badgeBg]">
        {{ current.label }}
      </span>
    </div>

    <p class="text-3xl font-bold text-white">
      {{ value.toFixed(1) }}x <span class="text-sm font-normal text-zinc-500">/ mês</span>
    </p>

    <div class="h-2 w-full rounded-full bg-zinc-800 overflow-hidden">
      <div class="h-full rounded-full transition-all duration-500" :class="current.bar" :style="{ width: barWidth }"></div>
    </div>

    <p class="text-xs font-mono flex items-center gap-1" :class="trendPositive ? 'text-emerald-400' : 'text-red-400'">
      <span>{{ trendPositive ? "▲" : "▼" }}</span>
      <span>{{ Math.abs(trend).toFixed(1) }} vs. período anterior</span>
    </p>
  </div>
</template>