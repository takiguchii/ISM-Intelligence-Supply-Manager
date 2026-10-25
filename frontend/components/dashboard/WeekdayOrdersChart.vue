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

const chartData = computed(() => {
    const defaultDays = ["seg.", "ter.", "qua.", "qui.", "sex.", "sáb.", "dom."];
    const hasData = props.data && props.data.length > 0;
    const labels = hasData ? props.data.map((d) => d.day) : defaultDays;
    const values = hasData ? props.data.map((d) => d.pedidos) : defaultDays.map(() => 0);
    return {
        labels,
        datasets: [
            {
                label: "Pedidos",
                data: values,
                backgroundColor: values.map((p) =>
                    p >= effectiveThreshold.value ? "rgb(245, 158, 11)" : "rgba(16, 185, 129, 0.65)"
                ),
                borderRadius: 6,
                barThickness: 28
            }
        ]
    };
});

const chartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    animation: {
        duration: typeof window !== "undefined" && window.matchMedia("(prefers-reduced-motion: reduce)").matches ? 0 : 1200,
        easing: "easeOutQuart" as const
    },
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
            grid: { display: false },
            ticks: { color: "rgb(113, 113, 122)", font: { size: 11 } }
        },
        y: {
            beginAtZero: true,
            grid: { color: "rgba(161, 161, 170, 0.15)" },
            ticks: { color: "rgb(113, 113, 122)", font: { size: 10 } }
        }
    }
};
</script>

<template>
    <div class="p-6 rounded-2xl bg-white dark:bg-zinc-900/80 border border-zinc-200 dark:border-zinc-800/80 shadow-sm dark:shadow-lg space-y-4 h-full transition-colors duration-200">
        <div>
            <h3 class="text-sm font-bold text-zinc-900 dark:text-white">Pedidos por dia da semana</h3>
            <p class="text-xs text-zinc-500 dark:text-zinc-400">Padrão das últimas 4 semanas</p>
        </div>
        <ChartOnVisible>
            <Bar :data="chartData" :options="chartOptions" />
        </ChartOnVisible>
    </div>
</template>