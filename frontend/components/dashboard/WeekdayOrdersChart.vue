<script setup lang="ts">
import { computed } from "vue";
import { Bar } from "vue-chartjs";
import { Chart as ChartJS, CategoryScale, LinearScale, BarElement, Tooltip } from "chart.js";

ChartJS.register(CategoryScale, LinearScale, BarElement, Tooltip);

const props = defineProps<{
    data: { day: string; pedidos: number }[];
    /** Pedidos a partir daqui são destacados como pico (padrão: 120). */
    threshold?: number;
}>();

const effectiveThreshold = computed(() => props.threshold ?? 120);

const chartData = computed(() => ({
    labels: props.data.map((d) => d.day),
    datasets: [
        {
            label: "Pedidos",
            data: props.data.map((d) => d.pedidos),
            backgroundColor: props.data.map((d) =>
                d.pedidos >= effectiveThreshold.value ? "rgb(251, 191, 36)" : "rgba(52, 211, 153, 0.55)"
            ),
            borderRadius: 6,
            barThickness: 28
        }
    ]
}));

const chartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
        legend: { display: false },
        tooltip: {
            backgroundColor: "rgb(24, 24, 27)",
            borderColor: "rgb(63, 63, 70)",
            borderWidth: 1,
            titleColor: "rgb(228, 228, 231)",
            bodyColor: "rgb(161, 161, 170)",
            padding: 10,
            cornerRadius: 8
        }
    },
    scales: {
        x: {
            grid: { display: false },
            ticks: { color: "rgb(113, 113, 122)", font: { size: 11 } }
        },
        y: {
            grid: { color: "rgba(63, 63, 70, 0.5)" },
            ticks: { color: "rgb(113, 113, 122)", font: { size: 10 } }
        }
    }
};
</script>

<template>
    <div class="p-6 rounded-2xl bg-zinc-900/80 border border-zinc-800/80 shadow-lg space-y-4 h-full">
        <div>
            <h3 class="text-sm font-semibold text-white">Pedidos por dia da semana</h3>
            <p class="text-xs text-zinc-500">Padrão das últimas 4 semanas</p>
        </div>
        <div class="h-64">
            <Bar :data="chartData" :options="chartOptions" />
        </div>
    </div>
</template>