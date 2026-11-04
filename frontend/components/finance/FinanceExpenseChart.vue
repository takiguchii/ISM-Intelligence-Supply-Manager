<script setup lang="ts">
import { computed } from "vue";
import { Doughnut } from "vue-chartjs";
import { ArcElement, Chart as ChartJS, Legend, Tooltip } from "chart.js";

ChartJS.register(ArcElement, Legend, Tooltip);
const props = defineProps<{ data: { label: string; amount: number }[] }>();
const colors = ["#10b981", "#6366f1", "#f59e0b", "#ef4444", "#8b5cf6", "#14b8a6", "#64748b"];
const money = (value: number) => value.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
const chartData = computed(() => ({
  labels: props.data.length ? props.data.map(item => item.label) : ["Sem despesas liquidadas"],
  datasets: [{ data: props.data.length ? props.data.map(item => item.amount) : [1], backgroundColor: props.data.length ? props.data.map((_, index) => colors[index % colors.length]) : ["#52525b"], borderWidth: 0 }] 
}));
const chartOptions = { responsive: true, maintainAspectRatio: false, cutout: "62%", plugins: { legend: { display: false }, tooltip: { callbacks: { label: (ctx: any) => ` ${money(Number(ctx.raw))}` } } } };
</script>

<template>
  <section class="rounded-2xl border border-zinc-200 dark:border-zinc-800/80 bg-white dark:bg-zinc-900/80 p-5 space-y-4">
    <div><h3 class="font-bold dark:text-white">Despesas por categoria</h3><p class="text-xs text-zinc-500">Despesas liquidadas no mês atual</p></div>
    <div class="h-56"><Doughnut :data="chartData" :options="chartOptions" /></div>
    <div class="grid grid-cols-2 gap-2 text-xs">
      <div v-for="(item, index) in data" :key="item.label" class="flex items-center gap-2 text-zinc-600 dark:text-zinc-300"><span class="h-2 w-2 rounded-full" :style="{ backgroundColor: colors[index % colors.length] }" />{{ item.label }} · {{ money(item.amount) }}</div>
    </div>
  </section>
</template>
