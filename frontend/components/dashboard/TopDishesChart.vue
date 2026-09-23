<script setup lang="ts">
import { computed } from "vue";
import { Bar } from "vue-chartjs";
import { Chart as ChartJS, CategoryScale, LinearScale, BarElement, Tooltip } from "chart.js";
import { useChartTheme } from "~/composables/useChartTheme";

ChartJS.register(CategoryScale, LinearScale, BarElement, Tooltip);

const props = defineProps<{
    data: { name: string; pedidos: number }[];
}>();

const theme = useChartTheme();

const chartData = computed(() => ({
    labels: props.data.map((d) => d.name),
    datasets: [
        {
            label: "Pedidos",
            data: props.data.map((d) => d.pedidos),
            backgroundColor: `rgb(${theme.value.positive})`,
            borderRadius: 6,
            barThickness: 18
        }
    ]
}));

const chartOptions = computed(() => ({
    indexAxis: "y" as const,
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
        legend: { display: false },
        tooltip: {
            ...theme.value.tooltip,
            borderWidth: 1,
            padding: 10,
            cornerRadius: 8
        }
    },
    scales: {
        x: {
            grid: { color: theme.value.grid },
            ticks: { color: theme.value.tick, font: { size: 10 } }
        },
        y: {
            grid: { display: false },
            ticks: { color: theme.value.tickStrong, font: { size: 11 } }
        }
    }
}));
</script>

<template>
    <div class="p-6 rounded-2xl bg-white dark:bg-zinc-900/80 border border-zinc-200 dark:border-zinc-800/80 shadow-sm dark:shadow-lg space-y-4 h-full">
        <div>
            <h3 class="text-sm font-semibold text-zinc-900 dark:text-white">Pratos mais pedidos</h3>
            <p class="text-xs text-zinc-500">Top 5 — últimos 7 dias</p>
        </div>
        <div class="h-64">
            <Bar :data="chartData" :options="chartOptions" />
        </div>
    </div>
</template>