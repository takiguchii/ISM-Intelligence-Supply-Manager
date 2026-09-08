<script setup lang="ts">
import { ref, onMounted } from "vue";
import AppLoader from "~/components/base/AppLoader.vue";
import PageHeader from "~/components/dashboard/PageHeader.vue";
import KpiCard from "~/components/dashboard/KpiCard.vue";
import AiInsightCard from "~/components/dashboard/AiInsightCard.vue";
import RevenueVsCostChart from "~/components/dashboard/RevenueVsCostChart.vue";
import TopDishesChart from "~/components/dashboard/TopDishesChart.vue";
import WeekdayOrdersChart from "~/components/dashboard/WeekdayOrdersChart.vue";
import PriceAdjustmentsList from "~/components/dashboard/PriceAdjustmentsList.vue";
import { useAuthStore } from "~/stores/auth";
import { useDashboardMetrics } from "~/composables/useDashboardMetrics";

const authStore = useAuthStore();
const router = useRouter();

const isLoading = ref(true);

const { refresh } = await useSystemStatus();

// Indicadores principais (KPIs, insights dos agentes, gráficos e sugestões de preço).
const {
  data: metrics,
  pending: metricsPending,
  refresh: refreshMetrics
} = useDashboardMetrics();

const handleRefreshAll = () => {
  refresh();
  refreshMetrics();
};

onMounted(async () => {
  const startTime = Date.now();

  // Verifica autenticação no cliente
  authStore.initFromStorage();
  if (!authStore.isAuthenticated) {
    router.push("/login");
    return;
  }

  const elapsedTime = Date.now() - startTime;
  const minDuration = 1000;
  const remainingTime = Math.max(0, minDuration - elapsedTime);

  setTimeout(() => {
    isLoading.value = false;
  }, remainingTime);
});
</script>

<template>
  <div class="space-y-8 max-w-7xl w-full mx-auto px-4 sm:px-6 py-8">
    <!-- Overlay de Animação de Carregamento -->
    <AppLoader :visible="isLoading" />

    <!-- Main Content Area -->
    <main class="flex-1 space-y-8">
      <!-- Cabeçalho da página -->
      <div class="p-8 rounded-2xl bg-gradient-to-r from-zinc-900 via-zinc-900/90 to-zinc-950 border border-zinc-800 shadow-xl relative overflow-hidden">
        <div class="absolute -right-20 -bottom-20 w-80 h-80 bg-zinc-700/10 rounded-full blur-3xl pointer-events-none"></div>
        <div class="relative z-10">
          <PageHeader
            eyebrow="Painel Geral"
            :title="`Bem-vindo, ${authStore.currentUser?.name || 'Usuário'}`"
            description="Sua central unificada para gestão inteligente de suprimentos, cardápio, estoque e integrações gastronômicas. Resumo consolidado de ontem."
          >
            <template #actions>
              <button
                @click="handleRefreshAll"
                class="px-5 py-2.5 bg-zinc-800 hover:bg-zinc-700 text-white font-medium rounded-xl text-xs sm:text-sm border border-zinc-700/60 transition-all duration-200 flex items-center gap-2"
              >
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"></path>
                </svg>
                <span>Atualizar Status</span>
              </button>
            </template>
          </PageHeader>
        </div>
      </div>

      <!-- Indicadores Principais -->
      <section class="space-y-5">
        <div class="flex items-center justify-between">
          <h2 class="text-lg font-semibold text-white tracking-tight">Indicadores Principais</h2>
          <span v-if="metricsPending" class="text-xs font-mono text-zinc-500 flex items-center gap-2">
            <span class="w-1.5 h-1.5 rounded-full bg-zinc-500 animate-pulse"></span>
            Atualizando…
          </span>
        </div>

        <!-- KPIs -->
        <div class="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          <KpiCard v-for="kpi in metrics.kpis" :key="kpi.id" v-bind="kpi" />
        </div>

        <!-- Insights dos Agentes de IA -->
        <div class="grid gap-4 lg:grid-cols-3">
          <AiInsightCard v-for="insight in metrics.aiInsights" :key="insight.id" v-bind="insight" />
        </div>

        <!-- Receita vs. Custo -->
        <RevenueVsCostChart :data="metrics.revenueVsCost" />

        <!-- Pratos mais pedidos / Pedidos por dia da semana -->
        <div class="grid gap-5 lg:grid-cols-2">
          <TopDishesChart :data="metrics.topDishes" />
          <WeekdayOrdersChart :data="metrics.weekdayOrders" />
        </div>

        <!-- Sugestões de reajuste de preço -->
        <PriceAdjustmentsList :items="metrics.priceAdjustments" />
      </section>
    </main>
  </div>
</template>