<script setup lang="ts">
import { computed } from "vue";
import { Bar } from "vue-chartjs";
import { BarElement, CategoryScale, Chart as ChartJS, LinearScale, Tooltip } from "chart.js";

ChartJS.register(CategoryScale, LinearScale, BarElement, Tooltip);
const props = defineProps<{ data: { name: string; quantity: number; revenue: number }[] }>();
const money = (value: number) => value.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
const chartData = computed(() => ({ labels: props.data.length ? props.data.map(item => item.name) : ["Sem vendas no mês"], datasets: [{ label: "Receita", data: props.data.length ? props.data.map(item => item.revenue) : [0], backgroundColor: "#10b981", borderRadius: 6, barThickness: 18 }] }));
const chartOptions = { indexAxis: "y" as const, responsive: true, maintainAspectRatio: false, plugins: { legend: { display: false }, tooltip: { callbacks: { label: (ctx: any) => ` ${money(Number(ctx.raw))}` } } }, scales: { x: { beginAtZero: true, ticks: { callback: (value: number | string) => `R$${(Number(value) / 1000).toFixed(0)}k` } }, y: { ticks: { color: "#a1a1aa" } } } };
</script>

<template>
  <section class="rounded-2xl border border-zinc-200 dark:border-zinc-800/80 bg-white dark:bg-zinc-900/80 p-5 space-y-4">
    <div><h3 class="font-bold dark:text-white">Itens de maior rotatividade</h3><p class="text-xs text-zinc-500">Top 5 por receita de pedidos pagos no mês</p></div>
    <div class="h-56"><Bar :data="chartData" :options="chartOptions" /></div>
    <div v-if="data.length" class="flex flex-wrap gap-3 text-xs text-zinc-500 dark:text-zinc-400"><span v-for="item in data" :key="item.name">{{ item.quantity }}x {{ item.name }}</span></div>
  </section>
</template>
