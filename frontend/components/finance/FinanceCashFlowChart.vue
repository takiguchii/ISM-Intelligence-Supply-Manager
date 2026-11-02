<script setup lang="ts">
import { computed } from "vue";
import { Bar } from "vue-chartjs";
import { BarElement, CategoryScale, Chart as ChartJS, LinearScale, Tooltip } from "chart.js";

ChartJS.register(CategoryScale, LinearScale, BarElement, Tooltip);
const props = defineProps<{ data: { month: string; revenue: number; expenses: number }[] }>();
const currency = (value: number) => value.toLocaleString("pt-BR", { style: "currency", currency: "BRL", maximumFractionDigits: 0 });
const chartData = computed(() => ({ labels: props.data.map(item => item.month), datasets: [{ label: "Entradas", data: props.data.map(item => item.revenue), backgroundColor: "#10b981", borderRadius: 5 }, { label: "Saídas", data: props.data.map(item => item.expenses), backgroundColor: "#f59e0b", borderRadius: 5 }] }));
const chartOptions = { responsive: true, maintainAspectRatio: false, plugins: { legend: { labels: { color: "#a1a1aa" } }, tooltip: { callbacks: { label: (ctx: any) => ` ${ctx.dataset.label}: ${currency(Number(ctx.raw))}` } } }, scales: { x: { ticks: { color: "#a1a1aa" }, grid: { display: false } }, y: { beginAtZero: true, ticks: { color: "#a1a1aa", callback: (value: number | string) => `R$${(Number(value) / 1000).toFixed(0)}k` } } } };
</script>

<template>
  <section class="rounded-2xl border border-zinc-200 dark:border-zinc-800/80 bg-white dark:bg-zinc-900/80 p-5 space-y-4">
    <div><h3 class="font-bold dark:text-white">Fluxo financeiro mensal</h3><p class="text-xs text-zinc-500">Pedidos pagos e lançamentos liquidados dos últimos 6 meses</p></div>
    <div class="h-64"><Bar :data="chartData" :options="chartOptions" /></div>
  </section>
</template>
