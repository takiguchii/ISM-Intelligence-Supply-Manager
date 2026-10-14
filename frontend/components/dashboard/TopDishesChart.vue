<script setup lang="ts">
import { computed } from "vue";
import { Bar } from "vue-chartjs";
import { Chart as ChartJS, CategoryScale, LinearScale, BarElement, Tooltip } from "chart.js";

ChartJS.register(CategoryScale, LinearScale, BarElement, Tooltip);

const props = defineProps<{
    data: { name: string; pedidos: number }[];
}>();

const chartData = computed(() => ({
    labels: props.data.map((d) => d.name),
    datasets: [
        {
            label: "Pedidos",
            data: props.data.map((d) => d.pedidos),
            backgroundColor: "rgb(16, 185, 129)",
            borderRadius: 6,
            barThickness: 18
        }
    ]
}));

const chartOptions = {
    indexAxis: "y" as const,
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
        legend: { display: false },
        tooltip: {
            backgroundColor: "rgba(24, 24, 27, 0.9)",
            borderColor: "rgba(63, 63, 70, 0.5)",
            borderWidth: 1,
            titleColor: "rgb(255, 255, 255)",
            bodyColor: "rgb(228, 228, 231)",
            padding: 10,
            cornerRadius: 8
        }
    },
    scales: {
        x: {
            grid: { color: "rgba(161, 161, 170, 0.15)" },
            ticks: { color: "rgb(113, 113, 122)", font: { size: 10 } }
        },
        y: {
            grid: { display: false },
            ticks: { color: "rgb(113, 113, 122)", font: { size: 11 } }
        }
    }
};
</script>

<template>
    <div class="p-6 rounded-2xl bg-white dark:bg-zinc-900/80 border border-zinc-200 dark:border-zinc-800/80 shadow-sm dark:shadow-lg space-y-4 h-full transition-colors duration-200">
        <div>
            <h3 class="text-sm font-bold text-zinc-900 dark:text-white">Pratos mais pedidos</h3>
            <p class="text-xs text-zinc-500 dark:text-zinc-400">Top 5 — últimos 7 dias</p>
        </div>
        <div class="h-64">
            <Bar :data="chartData" :options="chartOptions" />
        </div>
    </div>
</template>