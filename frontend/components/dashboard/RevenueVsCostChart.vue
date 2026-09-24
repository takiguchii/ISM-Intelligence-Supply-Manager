<script setup lang="ts">
import { computed } from "vue";
import { Line } from "vue-chartjs";
import {
    Chart as ChartJS,
    CategoryScale,
    LinearScale,
    PointElement,
    LineElement,
    Filler,
    Tooltip
} from "chart.js";
import type { TooltipItem } from "chart.js";

ChartJS.register(CategoryScale, LinearScale, PointElement, LineElement, Filler, Tooltip);

const props = defineProps<{
    data: { day: string; receita: number; custo: number }[];
}>();

const currency = (value: number) =>
    value.toLocaleString("pt-BR", { style: "currency", currency: "BRL", maximumFractionDigits: 0 });

const chartData = computed(() => ({
    labels: props.data.map((d) => d.day),
    datasets: [
        {
            label: "Receita",
            data: props.data.map((d) => d.receita),
            borderColor: "rgb(16, 185, 129)",
            backgroundColor: "rgba(16, 185, 129, 0.15)",
            fill: true,
            tension: 0.35,
            pointRadius: 3,
            pointBackgroundColor: "rgb(255, 255, 255)",
            pointBorderColor: "rgb(16, 185, 129)",
            pointBorderWidth: 2
        },
        {
            label: "Custo",
            data: props.data.map((d) => d.custo),
            borderColor: "rgb(245, 158, 11)",
            backgroundColor: "rgba(245, 158, 11, 0.12)",
            fill: true,
            tension: 0.35,
            pointRadius: 3,
            pointBackgroundColor: "rgb(255, 255, 255)",
            pointBorderColor: "rgb(245, 158, 11)",
            pointBorderWidth: 2
        }
    ]
}));

const chartOptions = computed(() => ({
    responsive: true,
    maintainAspectRatio: false,
    interaction: { mode: "index" as const, intersect: false },
    plugins: {
        legend: { display: false },
        tooltip: {
            backgroundColor: "rgba(24, 24, 27, 0.9)",
            borderColor: "rgba(63, 63, 70, 0.5)",
            borderWidth: 1,
            titleColor: "rgb(255, 255, 255)",
            bodyColor: "rgb(228, 228, 231)",
            padding: 10,
            cornerRadius: 8,
            callbacks: {
                label: (ctx: TooltipItem<"line">) => ` ${ctx.dataset.label}: ${currency(ctx.parsed.y)}`
            }
        }
    },
    scales: {
        x: {
            grid: { display: false },
            ticks: { color: "rgb(113, 113, 122)", font: { size: 10, family: "monospace" } }
        },
        y: {
            grid: { color: "rgba(161, 161, 170, 0.15)" },
            ticks: {
                color: "rgb(113, 113, 122)",
                font: { size: 10 },
                callback: (value: number | string) => `R$${(Number(value) / 1000).toFixed(0)}k`
            }
        }
    }
}));

const totals = computed(() => {
    const receita = props.data.reduce((sum, d) => sum + d.receita, 0);
    const custo = props.data.reduce((sum, d) => sum + d.custo, 0);
    return { receita, custo, margem: receita > 0 ? ((receita - custo) / receita) * 100 : 0 };
});
</script>

<template>
    <div class="p-6 rounded-2xl bg-white dark:bg-zinc-900/80 border border-zinc-200 dark:border-zinc-800/80 shadow-sm dark:shadow-lg space-y-4 transition-colors duration-200">
        <div class="flex items-center justify-between flex-wrap gap-3">
            <div>
                <h3 class="text-sm font-bold text-zinc-900 dark:text-white">Receita vs. Custo</h3>
                <p class="text-xs text-zinc-500 dark:text-zinc-400">Últimos 7 dias</p>
            </div>
            <div class="flex items-center gap-4 text-xs">
                <span class="flex items-center gap-1.5 text-zinc-600 dark:text-zinc-400">
                    <span class="w-2 h-2 rounded-full bg-emerald-500"></span> Receita
                </span>
                <span class="flex items-center gap-1.5 text-zinc-600 dark:text-zinc-400">
                    <span class="w-2 h-2 rounded-full bg-amber-500"></span> Custo
                </span>
            </div>
        </div>

        <div class="h-64">
            <Line :data="chartData" :options="chartOptions" />
        </div>

        <div class="flex items-center justify-between text-xs text-zinc-500 dark:text-zinc-400 pt-3 border-t border-zinc-200 dark:border-zinc-800/80">
            <span>Receita total: <span class="text-zinc-900 dark:text-zinc-200 font-bold">{{ currency(totals.receita) }}</span></span>
            <span>Margem: <span class="text-zinc-900 dark:text-zinc-200 font-bold">{{ totals.margem.toFixed(1) }}%</span></span>
        </div>
    </div>
</template>