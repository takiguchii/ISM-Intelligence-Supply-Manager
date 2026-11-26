<script setup lang="ts">
import { computed, onMounted, ref, watch } from "vue";
import { apiClientWithAuth } from "~/services/api/client";
import PageHeader from "~/components/dashboard/PageHeader.vue";
import KpiCard from "~/components/dashboard/KpiCard.vue";
import RevenueVsCostChart from "~/components/dashboard/RevenueVsCostChart.vue";
import TopDishesChart from "~/components/dashboard/TopDishesChart.vue";
import WeekdayOrdersChart from "~/components/dashboard/WeekdayOrdersChart.vue";
import PeakPeriodsChart from "~/components/dashboard/PeakPeriodsChart.vue";
import OrderQueueChart from "~/components/dashboard/OrderQueueChart.vue";
import AiInsightCard from "~/components/dashboard/AiInsightCard.vue";
import AgentInsightDetails from "~/components/dashboard/AgentInsightDetails.vue";
import { useAuthStore } from "~/stores/auth";
import { homeForUser } from "~/utils/navigation";
import { useDashboardMetrics, type Kpi } from "~/composables/useDashboardMetrics";
import { useAssistantUi } from "~/composables/useAssistantUi";
import { useSystemAlertCenter } from "~/composables/useSystemAlertCenter";

const authStore = useAuthStore();
const assistantUi = useAssistantUi();
const alertCenter = useSystemAlertCenter();
const router = useRouter();
const isSuperAdmin = computed(() => authStore.currentUser?.role === "Admin" && !authStore.currentUser?.restaurantId);
const dashboardRestaurantId = ref<number | null>(null);
const restaurants = ref<Array<{ id: number; name: string }>>([]);
const restaurantsError = ref("");


const { refresh: refreshStatus } = await useSystemStatus();

// Indicadores principais (KPIs, insights dos agentes, gráficos e sugestões de preço).
const {
  data: metrics,
  pending: metricsPending,
  error: metricsError,
  available: metricsAvailable,
  refresh: refreshMetrics
} = useDashboardMetrics(dashboardRestaurantId, isSuperAdmin);
const selectedInsight = ref<typeof metrics.value.aiInsights[number] | null>(null);
const selectedKpi = ref<Kpi | null>(null);
const kpiDetails = computed(() => {
  const kpi = selectedKpi.value;
  if (!kpi) return { description: "", rows: [] as Array<{ label: string; value: string }> };
  const currency = (value: number) => value.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
  if (kpi.id === "estoque") {
    return { description: "Produtos ativos no nível mínimo ou abaixo, ordenados pela maior necessidade de reposição.", rows: metrics.value.criticalStock.map(product => ({ label: product.name, value: `${product.currentQuantity.toLocaleString("pt-BR")} ${product.unit} · mínimo ${product.minimumQuantity.toLocaleString("pt-BR")} ${product.unit}` })) };
  }
  if (["receita", "margem"].includes(kpi.id)) {
    return { description: "Composição diária dos pedidos pagos nos últimos 7 dias.", rows: metrics.value.revenueVsCost.map(day => ({ label: day.day, value: kpi.id === "receita" ? currency(day.receita) : day.receita > 0 ? `${(((day.receita - day.custo) / day.receita) * 100).toLocaleString("pt-BR", { maximumFractionDigits: 1 })}% de margem` : "Sem vendas" })) };
  }
  if (kpi.id === "pedidos") {
    return { description: "Pratos mais pedidos na última semana e média de pedidos por dia da semana no último mês.", rows: [...metrics.value.topDishes.map(dish => ({ label: dish.name, value: `${dish.pedidos} unidades vendidas` })), ...metrics.value.weekdayOrders.map(day => ({ label: `Média de ${day.day}`, value: `${day.pedidos.toLocaleString("pt-BR", { maximumFractionDigits: 1 })} pedidos/semana` }))] };
  }
  return { description: kpi.hint || "Detalhamento do indicador.", rows: [] };
});

watch(dashboardRestaurantId, () => { if (authStore.isAuthenticated) refreshMetrics(); });
watch(alertCenter.alerts, () => { if (metricsAvailable.value) refreshMetrics(); });

const alertForInsight = (insight: typeof selectedInsight.value) => insight ? alertCenter.alerts.value.find(alert => alert.id === insight.alertId) || null : null;
const openInsightDetails = (insight: typeof selectedInsight.value) => { selectedInsight.value = insight; };
const converseAboutInsight = (insight: typeof selectedInsight.value) => {
  const alert = alertForInsight(insight);
  selectedInsight.value = null;
  if (alert) assistantUi.openChat(alert);
  else if (insight) assistantUi.openSpecialistChat(insight.agent, { sourceView: "dashboard", subject: `${insight.label} — consulta sem alerta` });
};

const handleRefreshAll = () => {
  refreshStatus();
  refreshMetrics();
};

onMounted(async () => {
  // Verifica autenticação no cliente
  authStore.initFromStorage();
  if (!authStore.isAuthenticated) {
    router.push("/login");
    return;
  }

  if (isSuperAdmin.value) {
    try { restaurants.value = await apiClientWithAuth<Array<{ id: number; name: string }>>("/api/restaurants"); }
    catch (error) { console.error("Erro ao carregar restaurantes:", error); restaurantsError.value = "Não foi possível carregar os restaurantes."; }
  }

  const home = homeForUser(authStore.currentUser);
  if (home !== "/") {
    await router.replace(home);
    return;
  }
  await refreshMetrics();


});
</script>

<template>
  <div class="space-y-8 max-w-7xl w-full mx-auto px-4 sm:px-6 py-8">

    <!-- Main Content Area -->
    <main class="flex-1 space-y-8">
      <!-- Cabeçalho da página -->
      <div class="p-6 sm:p-8 rounded-2xl bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 shadow-sm dark:shadow-xl relative overflow-hidden transition-colors duration-200">
        <div class="absolute -right-20 -bottom-20 w-80 h-80 bg-indigo-500/5 dark:bg-zinc-700/10 rounded-full blur-3xl pointer-events-none"></div>
        <div class="relative z-10">
          <PageHeader
            eyebrow="Painel Geral"
            :title="`Bem-vindo, ${authStore.currentUser?.name || 'Usuário'}`"
            description="Sua central unificada para gestão inteligente de suprimentos, cardápio, estoque e integrações gastronômicas. Resumo consolidado."
          >
            <template #actions>
              <button
                @click="handleRefreshAll"
                type="button"
                class="px-5 py-2.5 bg-zinc-100 hover:bg-zinc-200 dark:bg-zinc-800 dark:hover:bg-zinc-700 text-zinc-800 dark:text-white font-medium rounded-xl text-xs sm:text-sm border border-zinc-300 dark:border-zinc-700/60 transition-all duration-200 flex items-center gap-2 shadow-sm"
              >
                <svg class="w-4 h-4 text-zinc-500 dark:text-zinc-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"></path>
                </svg>
                <span>Atualizar Dashboard</span>
              </button>
            </template>
          </PageHeader>
        </div>
      </div>

      <div v-if="isSuperAdmin" class="space-y-2">
        <label for="dashboard-restaurant" class="text-sm font-semibold dark:text-zinc-200">Restaurante</label>
        <select id="dashboard-restaurant" v-model="dashboardRestaurantId" class="block w-full max-w-sm rounded-xl border border-zinc-300 dark:border-zinc-700 bg-white dark:bg-zinc-900 p-3 text-sm dark:text-white">
          <option :value="null">Selecione um restaurante</option>
          <option v-for="restaurant in restaurants" :key="restaurant.id" :value="restaurant.id">{{ restaurant.name }}</option>
        </select>
        <p v-if="restaurantsError" role="alert" class="text-sm text-red-500">{{ restaurantsError }}</p>
      </div>

      <!-- Indicadores Principais -->
      <p v-if="metricsError" role="status" class="p-5 rounded-2xl border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-900 text-sm text-zinc-600 dark:text-zinc-400">{{ metricsError }}</p>
      <section v-if="metricsAvailable" class="space-y-6">
        <div class="flex items-center justify-between">
          <h2 class="text-lg font-bold text-zinc-900 dark:text-white tracking-tight">Indicadores Principais</h2>
          <span v-if="metricsPending" class="text-xs font-mono text-zinc-500 flex items-center gap-2">
            <span class="w-1.5 h-1.5 rounded-full bg-indigo-500 animate-pulse"></span>
            Atualizando dados…
          </span>
        </div>

        <!-- KPIs -->
        <div class="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          <KpiCard v-for="kpi in metrics.kpis" :key="kpi.id" v-bind="kpi" @click="selectedKpi = kpi" />
        </div>

        <section aria-labelledby="agent-insights-title" class="space-y-3">
          <div class="flex items-center justify-between gap-3"><div><h3 id="agent-insights-title" class="text-sm font-bold text-zinc-800 dark:text-zinc-200">Insights e alertas dos agentes</h3><p class="mt-0.5 text-xs text-zinc-500">Estoque, precificação e fornecedores em cartões compactos.</p></div><button v-if="metrics.aiInsights.length" type="button" class="text-xs font-semibold text-indigo-600 hover:underline dark:text-indigo-300" @click="alertCenter.refresh(false)">Atualizar alertas</button></div>
          <div v-if="metrics.aiInsights.length" class="grid gap-3 lg:grid-cols-3"><AiInsightCard v-for="insight in metrics.aiInsights" :key="insight.id" :insight="insight" @details="openInsightDetails(insight)" @converse="converseAboutInsight(insight)" /></div>
          <p v-else class="rounded-xl border border-dashed border-zinc-300 px-4 py-3 text-xs text-zinc-500 dark:border-zinc-700">Nenhum alerta persistido dos agentes neste momento.</p>
        </section>

        <section aria-labelledby="sales-analysis-title" class="space-y-3">
          <div><h3 id="sales-analysis-title" class="text-sm font-bold text-zinc-800 dark:text-zinc-200">Vendas e cardápio</h3><p class="mt-0.5 text-xs text-zinc-500">Tendência financeira e produtos com maior procura.</p></div>
          <RevenueVsCostChart :data="metrics.revenueVsCost" />
        <div class="grid gap-6 lg:grid-cols-2">
          <TopDishesChart :data="metrics.topDishes" />
          <WeekdayOrdersChart :data="metrics.weekdayOrders" />
        </div>
        </section>

        <section aria-labelledby="operations-overview-title" class="space-y-3">
          <div><h3 id="operations-overview-title" class="text-sm font-bold text-zinc-800 dark:text-zinc-200">Visão operacional</h3><p class="mt-0.5 text-xs text-zinc-500">Resumo da fila atual e dos períodos de maior movimento.</p></div>
          <div class="grid gap-6 lg:grid-cols-2"><OrderQueueChart :data="metrics.orderQueue" /><PeakPeriodsChart :data="metrics.peakPeriods" /></div>
        </section>

      </section>
      <Teleport to="body">
        <Transition name="kpi-detail">
          <div v-if="selectedKpi" class="fixed inset-0 z-[70] flex items-center justify-center p-4 sm:p-6" role="presentation" @keydown.esc="selectedKpi = null">
            <button class="absolute inset-0 h-full w-full bg-zinc-950/25 backdrop-blur-md" aria-label="Fechar detalhes do indicador" @click="selectedKpi = null" />
            <section class="kpi-detail-panel relative flex max-h-[min(80vh,680px)] w-full max-w-xl flex-col overflow-hidden rounded-3xl border border-zinc-200/80 bg-white shadow-2xl shadow-zinc-950/20 dark:border-zinc-700/80 dark:bg-zinc-950" role="dialog" aria-modal="true" aria-labelledby="kpi-detail-title">
              <header class="border-b border-zinc-200/70 bg-zinc-50/80 px-6 py-5 dark:border-zinc-800 dark:bg-zinc-900/70 sm:px-7">
                <div class="flex items-start justify-between gap-4"><div><p class="text-[10px] font-semibold uppercase tracking-[.2em] text-indigo-600 dark:text-indigo-300">Detalhamento do indicador</p><h2 id="kpi-detail-title" class="mt-1 text-xl font-bold tracking-tight text-zinc-900 dark:text-white">{{ selectedKpi.label }}</h2><p class="mt-1 text-sm text-zinc-500 dark:text-zinc-400">{{ kpiDetails.description }}</p></div><button type="button" class="grid h-9 w-9 shrink-0 place-items-center rounded-xl text-xl text-zinc-500 transition hover:bg-zinc-200/70 hover:text-zinc-900 dark:hover:bg-zinc-800 dark:hover:text-white" aria-label="Fechar" @click="selectedKpi = null">×</button></div>
                <p class="relative mt-5 text-3xl font-bold tracking-tight text-zinc-900 dark:text-white">{{ selectedKpi.value }}</p>
              </header>
              <div class="min-h-0 flex-1 overflow-y-auto p-5 sm:p-6">
                <div v-if="kpiDetails.rows.length" class="divide-y divide-zinc-100 dark:divide-zinc-800">
                  <article v-for="row in kpiDetails.rows" :key="row.label" class="flex items-start justify-between gap-4 py-3 first:pt-0 last:pb-0"><span class="min-w-0 text-sm font-medium text-zinc-700 dark:text-zinc-200">{{ row.label }}</span><span class="max-w-[65%] text-right text-sm text-zinc-500 dark:text-zinc-400">{{ row.value }}</span></article>
                </div>
                <p v-else class="rounded-2xl border border-dashed border-zinc-300 px-4 py-8 text-center text-sm text-zinc-500 dark:border-zinc-700">{{ selectedKpi.id === 'estoque' ? 'Nenhum produto está no nível crítico de estoque.' : 'Ainda não há dados suficientes para detalhar este indicador.' }}</p>
              </div>
              <footer class="flex justify-end border-t border-zinc-100 px-5 py-4 dark:border-zinc-800"><button type="button" class="rounded-xl bg-zinc-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-zinc-700 dark:bg-white dark:text-zinc-900 dark:hover:bg-zinc-200" @click="selectedKpi = null">Entendi</button></footer>
            </section>
          </div>
        </Transition>
      </Teleport>
      <AgentInsightDetails :insight="selectedInsight" :open="Boolean(selectedInsight)" @close="selectedInsight = null" @converse="converseAboutInsight(selectedInsight)" />
    </main>
  </div>
</template>

<style scoped>
.kpi-detail-enter-active, .kpi-detail-leave-active { transition: opacity 220ms ease; }
.kpi-detail-enter-active .kpi-detail-panel, .kpi-detail-leave-active .kpi-detail-panel { transition: transform 280ms cubic-bezier(.22,1,.36,1), opacity 220ms ease; }
.kpi-detail-enter-from, .kpi-detail-leave-to { opacity: 0; }
.kpi-detail-enter-from .kpi-detail-panel, .kpi-detail-leave-to .kpi-detail-panel { transform: translateY(14px) scale(.97); opacity: 0; }
@media (prefers-reduced-motion: reduce) { .kpi-detail-enter-active, .kpi-detail-leave-active, .kpi-detail-enter-active .kpi-detail-panel, .kpi-detail-leave-active .kpi-detail-panel { transition-duration: 1ms; } }
</style>
