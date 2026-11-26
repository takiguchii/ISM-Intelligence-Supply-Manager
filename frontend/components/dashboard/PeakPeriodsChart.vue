<script setup lang="ts">
import { computed } from "vue";
import { Bar } from "vue-chartjs";
import { BarElement, CategoryScale, Chart as ChartJS, LinearScale, Tooltip } from "chart.js";
import type { PeakPeriod } from "~/composables/useDashboardMetrics";

ChartJS.register(CategoryScale, LinearScale, BarElement, Tooltip);
const props = withDefaults(defineProps<{ data?: PeakPeriod[] }>(), { data: () => [] });
const chartData = computed(() => ({
  labels: props.data.map(item => item.period),
  datasets: [{
    label: "Pedidos médios por semana",
    data: props.data.map(item => item.pedidos),
    backgroundColor: ["rgba(99, 102, 241, .72)", "rgba(16, 185, 129, .78)", "rgba(20, 184, 166, .72)", "rgba(245, 158, 11, .78)", "rgba(113, 113, 122, .55)"],
    borderRadius: 7,
    maxBarThickness: 34
  }]
}));
const chartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  animation: { duration: typeof window !== "undefined" && window.matchMedia("(prefers-reduced-motion: reduce)").matches ? 0 : 800 },
  plugins: {
    legend: { display: false },
    tooltip: { backgroundColor: "rgba(24, 24, 27, .94)", titleColor: "#fff", bodyColor: "#e4e4e7", padding: 10, callbacks: { label: (item: { parsed: { y: number } }) => ` ${item.parsed.y.toLocaleString("pt-BR", { maximumFractionDigits: 1 })} pedidos/semana` } }
  },
  scales: {
    x: { grid: { display: false }, ticks: { color: "rgb(113, 113, 122)", font: { size: 10 } } },
    y: { beginAtZero: true, grid: { color: "rgba(161, 161, 170, .15)" }, ticks: { color: "rgb(113, 113, 122)", precision: 0 } }
  }
};
</script>

<template>
  <section class="h-full space-y-4 rounded-2xl border border-zinc-200 bg-white p-6 shadow-sm transition-colors dark:border-zinc-800/80 dark:bg-zinc-900/80 dark:shadow-lg">
    <div>
      <h3 class="text-sm font-bold text-zinc-900 dark:text-white">Horários de maior movimento</h3>
      <p class="text-xs text-zinc-500 dark:text-zinc-400">Média de pedidos pagos por semana · últimas 4 semanas</p>
    </div>
    <ChartOnVisible><Bar :data="chartData" :options="chartOptions" /></ChartOnVisible>
  </section>
</template>
