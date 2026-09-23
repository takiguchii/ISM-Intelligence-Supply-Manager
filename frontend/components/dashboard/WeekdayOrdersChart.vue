<script setup lang="ts">
import { computed } from "vue";
import { Bar } from "vue-chartjs";
import { Chart as ChartJS, CategoryScale, LinearScale, BarElement, Tooltip } from "chart.js";
import { useChartTheme } from "~/composables/useChartTheme";

ChartJS.register(CategoryScale, LinearScale, BarElement, Tooltip);

const props = defineProps<{
    data: { day: string; pedidos: number }[];
    /** Pedidos a partir daqui são destacados como pico (padrão: 120). */
    threshold?: number;
}>();

const effectiveThreshold = computed(() => props.threshold ?? 120);
const theme = useChartTheme();

const chartData = computed(() => ({
    labels: props.data.map((d) => d.day),
    datasets: [
        {
            label: "Pedidos",
            data: props.data.map((d) => d.pedidos),
            backgroundColor: props.data.map((d) =>
                d.pedidos >= effectiveThreshold.value ? `rgb(${theme.value.warning})` : `rgba(${theme.value.positive}, 0.55)`
            ),
            borderRadius: 6,
            barThickness: 28
        }
    ]
}));

const chartOptions = computed(() => ({
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
            grid: { display: false },
            ticks: { color: theme.value.tick, font: { size: 11 } }
        },
        y: {
            grid: { color: theme.value.grid },
            ticks: { color: theme.value.tick, font: { size: 10 } }
        }
    }
}));
</script>

<template>
    <div class="p-6 rounded-2xl bg-white dark:bg-zinc-900/80 border border-zinc-200 dark:border-zinc-800/80 shadow-sm dark:shadow-lg space-y-4 h-full">
        <div>
            <h3 class="text-sm font-semibold text-zinc-900 dark:text-white">Pedidos por dia da semana</h3>
            <p class="text-xs text-zinc-500">Padrão das últimas 4 semanas</p>
        </div>
        <div class="h-64">
            <Bar :data="chartData" :options="chartOptions" />
        </div>
    </div>
</template>