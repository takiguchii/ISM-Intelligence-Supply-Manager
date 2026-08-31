<script setup lang="ts">
import { ref, onMounted } from "vue";
import AppSidebar from "~/components/layout/AppSidebar.vue";
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

definePageMeta({
  layout: false
});

const runtimeConfig = useRuntimeConfig();
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

const isSidebarOpen = ref(false);

const toggleSidebar = () => {
  isSidebarOpen.value = !isSidebarOpen.value;
};

const handleLogout = () => {
  authStore.logout();
  router.push("/login");
};

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

  // Garante que a animação rode por no MÍNIMO 2000ms (2 segundos)
  // ou mais caso os dados da página ainda estejam sendo carregados
  const elapsedTime = Date.now() - startTime;
  const minDuration = 2000;
  const remainingTime = Math.max(0, minDuration - elapsedTime);

  setTimeout(() => {
    isLoading.value = false;
  }, remainingTime);
});
</script>

<template>
  <div class="min-h-screen bg-zinc-950 text-zinc-100 flex flex-col font-sans selection:bg-zinc-800 selection:text-white">
    <!-- Overlay de Animação de Carregamento -->
    <AppLoader :visible="isLoading" />

    <!-- Navbar Header -->
    <header class="h-16 border-b border-zinc-800/80 bg-zinc-900/60 backdrop-blur-xl sticky top-0 z-30 px-4 sm:px-6 flex items-center justify-between">
      <div class="flex items-center gap-4">
        <button
          @click="toggleSidebar"
          class="p-2 rounded-xl text-zinc-300 hover:text-white hover:bg-zinc-800/80 transition-all duration-200 focus:outline-none focus:ring-2 focus:ring-zinc-600"
          title="Abrir Menu Lateral"
        >
          <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"></path>
          </svg>
        </button>

        <div class="flex items-center gap-3">
          <span class="font-bold text-lg text-white tracking-tight">ISM</span>
          <span class="hidden sm:inline-block text-xs uppercase tracking-widest text-zinc-400 font-mono border-l border-zinc-700/60 pl-3">
            {{ runtimeConfig.public.appName }}
          </span>
        </div>
      </div>

      <div class="flex items-center gap-3">
        <div v-if="authStore.isAuthenticated" class="flex items-center gap-3">
          <span class="hidden md:inline-block text-xs text-zinc-400 font-medium">
            {{ authStore.currentUser?.name }} ({{ authStore.currentUser?.role }})
          </span>
          <button
            @click="handleLogout"
            class="px-4 py-2 text-xs font-semibold rounded-xl bg-zinc-800 hover:bg-zinc-700 text-zinc-200 hover:text-white border border-zinc-700/60 transition-all duration-200 flex items-center gap-2"
          >
            <svg class="w-4 h-4 text-zinc-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"></path>
            </svg>
            <span>Sair</span>
          </button>
        </div>
        <NuxtLink
          v-else
          to="/login"
          class="px-4 py-2 text-xs font-semibold rounded-xl bg-zinc-800 hover:bg-zinc-700 text-zinc-200 hover:text-white border border-zinc-700/60 transition-all duration-200 flex items-center gap-2"
        >
          <svg class="w-4 h-4 text-zinc-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 16l-4-4m0 0l4-4m-4 4h14m-5 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h7a3 3 0 013 3v1"></path>
          </svg>
          <span>Login</span>
        </NuxtLink>
      </div>
    </header>

    <AppSidebar :isOpen="isSidebarOpen" @close="isSidebarOpen = false" />

    <!-- Main Content Area -->
    <main class="flex-1 max-w-7xl w-full mx-auto px-4 sm:px-6 py-8 space-y-8">
      <!-- Cabeçalho da página (substitui o antigo hero) -->
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
                @click="toggleSidebar"
                class="px-5 py-2.5 bg-white hover:bg-zinc-200 text-zinc-950 font-semibold rounded-xl text-xs sm:text-sm transition-all duration-200 flex items-center gap-2 shadow-md"
              >
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"></path>
                </svg>
                <span>Navegar pelos Módulos</span>
              </button>
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

    <!-- Footer -->
    <footer class="mt-auto border-t border-zinc-800/80 bg-zinc-950 py-6 text-center text-xs text-zinc-500">
      <div class="max-w-7xl mx-auto px-4 flex flex-col sm:flex-row items-center justify-between gap-3">
        <p>&copy; 2026 ISM — Intelligence Supply Manager. Todos os direitos reservados.</p>
        <div class="flex items-center gap-4 text-zinc-400">
          <NuxtLink to="/" class="hover:text-white transition-colors">Início</NuxtLink>
          <NuxtLink to="/login" class="hover:text-white transition-colors">Login</NuxtLink>
          <a href="http://localhost:8080/swagger" target="_blank" class="hover:text-white transition-colors">Swagger API</a>
        </div>
      </div>
    </footer>
  </div>
</template>