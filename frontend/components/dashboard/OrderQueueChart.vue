<script setup lang="ts">
import { computed } from "vue";
import { Bar } from "vue-chartjs";
import { BarElement, CategoryScale, Chart as ChartJS, LinearScale, Tooltip } from "chart.js";
import type { OrderQueue } from "~/composables/useDashboardMetrics";

ChartJS.register(CategoryScale, LinearScale, BarElement, Tooltip);
const props = withDefaults(defineProps<{ data?: OrderQueue[] }>(), { data: () => [] });
const activeCount = computed(() => props.data.reduce((total, item) => total + (item.pedidos || 0), 0));
const chartData = computed(() => ({
  labels: props.data.map(item => item.status),
  datasets: [{
    label: "Pedidos agora",
    data: props.data.map(item => item.pedidos),
    backgroundColor: ["rgba(99, 102, 241, .76)", "rgba(245, 158, 11, .78)", "rgba(16, 185, 129, .78)"],
    borderRadius: 7,
    maxBarThickness: 42
  }]
}));
const chartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  animation: { duration: typeof window !== "undefined" && window.matchMedia("(prefers-reduced-motion: reduce)").matches ? 0 : 800 },
  plugins: {
    legend: { display: false },
    tooltip: { backgroundColor: "rgba(24, 24, 27, .94)", titleColor: "#fff", bodyColor: "#e4e4e7", padding: 10, callbacks: { label: (item: { parsed: { y: number } }) => ` ${item.parsed.y} ${item.parsed.y === 1 ? "pedido" : "pedidos"}` } }
  },
  scales: {
    x: { grid: { display: false }, ticks: { color: "rgb(113, 113, 122)", font: { size: 10 } } },
    y: { beginAtZero: true, grid: { color: "rgba(161, 161, 170, .15)" }, ticks: { color: "rgb(113, 113, 122)", precision: 0 } }
  }
};
</script>

<template>
  <section class="h-full space-y-4 rounded-2xl border border-zinc-200 bg-white p-6 shadow-sm transition-colors dark:border-zinc-800/80 dark:bg-zinc-900/80 dark:shadow-lg">
    <div class="flex items-start justify-between gap-3">
      <div>
        <h3 class="text-sm font-bold text-zinc-900 dark:text-white">Fila de pedidos agora</h3>
        <p class="text-xs text-zinc-500 dark:text-zinc-400">Resumo operacional · atualize a dashboard para recarregar</p>
      </div>
      <span class="rounded-full border border-indigo-200 bg-indigo-50 px-2.5 py-1 text-xs font-semibold text-indigo-700 dark:border-indigo-400/20 dark:bg-indigo-400/10 dark:text-indigo-200">{{ activeCount }} abertos</span>
    </div>
    <ChartOnVisible><Bar :data="chartData" :options="chartOptions" /></ChartOnVisible>
    <p v-if="activeCount === 0" class="text-center text-xs text-zinc-500">Nenhum pedido aguardando atendimento neste momento.</p>
  </section>
</template>
