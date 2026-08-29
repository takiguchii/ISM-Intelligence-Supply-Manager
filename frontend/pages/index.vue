<script setup lang="ts">
import { ref, computed, onMounted } from "vue";
import AppSidebar from "~/components/layout/AppSidebar.vue";
import AppLoader from "~/components/base/AppLoader.vue";
import StatusOverviewCard from "~/components/layout/StatusOverviewCard.vue";
import RevenueVsCostChart from "~/components/dashboard/RevenueVsCostChart.vue";
import WeeklyRevenueChart from "~/components/dashboard/WeeklyRevenueChart.vue";
import AverageMarginIndicator from "~/components/dashboard/AverageMarginIndicator.vue";
import StockTurnoverIndicator from "~/components/dashboard/StockTurnoverIndicator.vue";
import RiskItemsPanel from "~/components/dashboard/RiskItemsPanel.vue";
import AgentNotificationsPanel from "~/components/dashboard/AgentNotificationsPanel.vue";
import { useAuthStore } from "~/stores/auth";
import { useDashboardMetrics } from "~/composables/useDashboardMetrics";

definePageMeta({
  layout: false
});

const runtimeConfig = useRuntimeConfig();
const authStore = useAuthStore();
const router = useRouter();

const isLoading = ref(true);

const { data, pending, error, refresh } = await useSystemStatus();

// Indicadores principais (Receita vs. Custo, Receita Semanal, Margem Média,
// Giro de Estoque, Itens de Risco, Notificações dos Agentes).
const {
  data: metrics,
  pending: metricsPending,
  refresh: refreshMetrics
} = useDashboardMetrics();

const isSidebarOpen = ref(false);

const toggleSidebar = () => {
  isSidebarOpen.value = !isSidebarOpen.value;
};

const status = computed(() => data.value);

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
  <div
    class="min-h-screen bg-zinc-950 text-zinc-100 flex flex-col font-sans selection:bg-zinc-800 selection:text-white">
    <!-- Overlay de Animação de Carregamento -->
    <AppLoader :visible="isLoading" />

    <!-- Navbar Header -->
    <header
      class="h-16 border-b border-zinc-800/80 bg-zinc-900/60 backdrop-blur-xl sticky top-0 z-30 px-4 sm:px-6 flex items-center justify-between">
      <!-- Left side: Hamburger Icon & System Name -->
      <div class="flex items-center gap-4">
        <!-- 3 Lines Hamburger Menu Button -->
        <button @click="toggleSidebar"
          class="p-2 rounded-xl text-zinc-300 hover:text-white hover:bg-zinc-800/80 transition-all duration-200 focus:outline-none focus:ring-2 focus:ring-zinc-600"
          title="Abrir Menu Lateral">
          <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"></path>
          </svg>
        </button>

        <!-- System Name -->
        <div class="flex items-center gap-3">
          <span class="font-bold text-lg text-white tracking-tight">ISM</span>
          <span
            class="hidden sm:inline-block text-xs uppercase tracking-widest text-zinc-400 font-mono border-l border-zinc-700/60 pl-3">
            {{ runtimeConfig.public.appName }}
          </span>
        </div>
      </div>

      <!-- Right side: Actions & User Info / Logout -->
      <div class="flex items-center gap-3">
        <div v-if="authStore.isAuthenticated" class="flex items-center gap-3">
          <span class="hidden md:inline-block text-xs text-zinc-400 font-medium">
            {{ authStore.currentUser?.name }} ({{ authStore.currentUser?.role }})
          </span>
          <button @click="handleLogout"
            class="px-4 py-2 text-xs font-semibold rounded-xl bg-zinc-800 hover:bg-zinc-700 text-zinc-200 hover:text-white border border-zinc-700/60 transition-all duration-200 flex items-center gap-2">
            <svg class="w-4 h-4 text-zinc-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"></path>
            </svg>
            <span>Sair</span>
          </button>
        </div>
        <NuxtLink v-else to="/login"
          class="px-4 py-2 text-xs font-semibold rounded-xl bg-zinc-800 hover:bg-zinc-700 text-zinc-200 hover:text-white border border-zinc-700/60 transition-all duration-200 flex items-center gap-2">
          <svg class="w-4 h-4 text-zinc-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M11 16l-4-4m0 0l4-4m-4 4h14m-5 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h7a3 3 0 013 3v1"></path>
          </svg>
          <span>Login</span>
        </NuxtLink>
      </div>
    </header>

    <!-- App Sidebar Component -->
    <AppSidebar :isOpen="isSidebarOpen" @close="isSidebarOpen = false" />

    <!-- Main Content Area -->
    <main class="flex-1 max-w-7xl w-full mx-auto px-4 sm:px-6 py-8 space-y-8">
      <!-- Hero / Welcome Banner -->
      <div
        class="p-8 rounded-2xl bg-gradient-to-r from-zinc-900 via-zinc-900/90 to-zinc-950 border border-zinc-800 shadow-xl relative overflow-hidden">
        <div class="absolute -right-20 -bottom-20 w-80 h-80 bg-zinc-700/10 rounded-full blur-3xl pointer-events-none">
        </div>
        <div class="relative z-10 max-w-2xl space-y-4">
          <div
            class="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-zinc-800/80 border border-zinc-700/60 text-xs font-mono text-zinc-300">
            <span class="w-2 h-2 rounded-full bg-emerald-400 animate-pulse"></span>
            Painel Geral
          </div>
          <h1 class="text-3xl sm:text-4xl font-bold text-white tracking-tight">
            Bem-vindo ao ISM, {{ authStore.currentUser?.name || "Usuário" }}
          </h1>
          <p class="text-zinc-400 text-sm sm:text-base leading-relaxed">
            Sua central unificada para gestão inteligente de suprimentos, cardápio, estoque e integrações gastronômicas.
          </p>
          <div class="pt-2 flex flex-wrap gap-3">
            <button @click="toggleSidebar"
              class="px-5 py-2.5 bg-white hover:bg-zinc-200 text-zinc-950 font-semibold rounded-xl text-xs sm:text-sm transition-all duration-200 flex items-center gap-2 shadow-md">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16">
                </path>
              </svg>
              <span>Navegar pelos Módulos</span>
            </button>
            <button @click="handleRefreshAll"
              class="px-5 py-2.5 bg-zinc-800 hover:bg-zinc-700 text-white font-medium rounded-xl text-xs sm:text-sm border border-zinc-700/60 transition-all duration-200 flex items-center gap-2">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                  d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15">
                </path>
              </svg>
              <span>Atualizar Status</span>
            </button>
          </div>
        </div>
      </div>

      <!-- Indicadores Principais -->
      <section class="space-y-5">
        <div class="flex items-center justify-between">
          <div>
            <h2 class="text-lg font-semibold text-white tracking-tight">📊 Indicadores Principais</h2>
            <p class="text-xs text-zinc-500 mt-0.5">Visão consolidada de receita, margem, estoque e alertas dos agentes.
            </p>
          </div>
          <span v-if="metricsPending" class="text-xs font-mono text-zinc-500 flex items-center gap-2">
            <span class="w-1.5 h-1.5 rounded-full bg-zinc-500 animate-pulse"></span>
            Atualizando…
          </span>
        </div>

        <!-- Receita vs. Custo / Receita Semanal -->
        <div class="grid grid-cols-1 lg:grid-cols-2 gap-5">
          <RevenueVsCostChart :data="metrics.revenueVsCost" />
          <WeeklyRevenueChart :data="metrics.weeklyRevenue" />
        </div>

        <!-- Margem Média / Giro de Estoque -->
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-5">
          <AverageMarginIndicator :value="metrics.averageMargin.value" :trend="metrics.averageMargin.trend" />
          <StockTurnoverIndicator :value="metrics.stockTurnover.value" :trend="metrics.stockTurnover.trend"
            :status="metrics.stockTurnover.status" />
        </div>

        <!-- Itens de Risco / Notificações dos Agentes -->
        <div class="grid grid-cols-1 lg:grid-cols-2 gap-5">
          <RiskItemsPanel :items="metrics.riskItems" />
          <AgentNotificationsPanel :notifications="metrics.agentNotifications" />
        </div>
      </section>

      <!-- Overview Cards Grid -->
      <div class="grid grid-cols-1 lg:grid-cols-[1.1fr_0.9fr] gap-8">
        <!-- Summary Info Grid -->
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-5">
          <div class="p-6 rounded-2xl bg-zinc-900/80 border border-zinc-800/80 shadow-lg space-y-2">
            <div class="flex items-center justify-between text-zinc-400">
              <span class="text-xs font-mono uppercase tracking-wider">Status do Sistema</span>
              <svg class="w-5 h-5 text-emerald-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path>
              </svg>
            </div>
            <p class="text-2xl font-bold text-white">Operacional</p>
            <p class="text-xs text-zinc-500">Backend & Banco de Dados sincronizados</p>
          </div>

          <div class="p-6 rounded-2xl bg-zinc-900/80 border border-zinc-800/80 shadow-lg space-y-2">
            <div class="flex items-center justify-between text-zinc-400">
              <span class="text-xs font-mono uppercase tracking-wider">Menu Lateral</span>
              <svg class="w-5 h-5 text-zinc-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16">
                </path>
              </svg>
            </div>
            <p class="text-2xl font-bold text-white">6 Módulos</p>
            <p class="text-xs text-zinc-500">Dashboard, Financeiro, Cardápio, Estoque, Fornecedores, Integrações</p>
          </div>

          <div class="p-6 rounded-2xl bg-zinc-900/80 border border-zinc-800/80 shadow-lg space-y-2 sm:col-span-2">
            <div class="flex items-center justify-between text-zinc-400">
              <span class="text-xs font-mono uppercase tracking-wider">Perfil da Conta</span>
              <span class="text-xs font-mono text-emerald-400 uppercase tracking-wider">{{ authStore.currentUser?.role
                }}</span>
            </div>
            <p class="text-lg font-semibold text-white">{{ authStore.currentUser?.name }}</p>
            <p class="text-xs text-zinc-400">{{ authStore.currentUser?.email }}</p>
          </div>
        </div>

        <!-- System Status Card -->
        <StatusOverviewCard :status="status" :pending="pending" />
      </div>
    </main>

    <!-- Footer -->
    <footer class="mt-auto border-t border-zinc-800/80 bg-zinc-950 py-6 text-center text-xs text-zinc-500">
      <div class="max-w-7xl mx-auto px-4 flex flex-col sm:flex-row items-center justify-between gap-3">
        <p>&copy; 2026 ISM — Intelligence Supply Manager. Todos os direitos reservados.</p>
        <div class="flex items-center gap-4 text-zinc-400">
          <NuxtLink to="/" class="hover:text-white transition-colors">Início</NuxtLink>
          <NuxtLink to="/login" class="hover:text-white transition-colors">Login</NuxtLink>
          <a href="http://localhost:8080/swagger" target="_blank" class="hover:text-white transition-colors">Swagger
            API</a>
        </div>
      </div>
    </footer>
  </div>
</template>