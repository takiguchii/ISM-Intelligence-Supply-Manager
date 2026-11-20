import { ref, type Ref } from "vue";
import { apiClientWithAuth } from "~/services/api/client";
import { useSystemAlertCenter } from "~/composables/useSystemAlertCenter";
import { aggregateAgentCards, type AgentCardViewModel, type AgentKind, type AgentSeverity } from "~/types/agentExperience";

export interface Kpi { id: string; label: string; value: string; delta: string; trend: "up" | "down"; icon: string; hint?: string; }
export type InsightSeverity = AgentSeverity;
export interface AgentInsight extends AgentCardViewModel { id: string; icon: AgentKind; }
export interface RevenueVsCostPoint { day: string; receita: number; custo: number; }
export interface TopDish { name: string; pedidos: number; }
export interface WeekdayOrders { day: string; pedidos: number; }
export interface PriceAdjustment { id: string; name: string; from: number; to: number; direction: "up" | "down"; reason: string; }
export interface DashboardMetrics { kpis: Kpi[]; aiInsights: AgentInsight[]; revenueVsCost: RevenueVsCostPoint[]; topDishes: TopDish[]; weekdayOrders: WeekdayOrders[]; priceAdjustments: PriceAdjustment[]; }

const errorCards = (message: string): AgentInsight[] => (["stock", "pricing", "suppliers"] as const).map(agent => ({
  id: `${agent}-error`, agent, icon: agent, label: agent === "stock" ? "Estoque" : agent === "pricing" ? "Precificação" : "Fornecedores",
  title: "Alertas indisponíveis", description: message, severity: "unknown", mood: "error", attentionLevel: "critical",
  alertId: null, alertIds: [], unreadCount: 0, generatedAtUtc: null, referenceEntityType: null, referenceEntityId: null,
  facts: {}, state: "error"
}));

export const useDashboardMetrics = (restaurantId: Ref<number | null>, requiresSelection: Ref<boolean>) => {
  const data = ref<DashboardMetrics>({ kpis: [], aiInsights: [], revenueVsCost: [], topDishes: [], weekdayOrders: [], priceAdjustments: [] });
  const pending = ref(false);
  const error = ref<string | null>(null);
  const available = ref(false);
  const alertCenter = useSystemAlertCenter();
  let generation = 0;

  const refresh = async () => {
    const requestGeneration = ++generation;
    const hadData = available.value;
    if (!hadData) available.value = false;
    if (requiresSelection.value && !restaurantId.value) {
      pending.value = false;
      error.value = "Selecione um restaurante para consultar os indicadores.";
      return;
    }
    pending.value = true;
    error.value = null;
    try {
      const [metricsResponse] = await Promise.all([
        apiClientWithAuth<Omit<DashboardMetrics, "aiInsights" | "priceAdjustments">>("/api/dashboard/metrics", { params: restaurantId.value ? { restaurantId: restaurantId.value } : undefined }),
        alertCenter.ensureLoaded()
      ]);
      if (requestGeneration !== generation) return;
      const scopedAlerts = alertCenter.alerts.value.filter(item => !requiresSelection.value || item.restaurantId === restaurantId.value);
      const aiInsights = alertCenter.error.value
        ? errorCards(alertCenter.error.value)
        : aggregateAgentCards(scopedAlerts).map(card => ({ ...card, id: `${card.agent}-summary`, icon: card.agent }));
      data.value = { ...metricsResponse, aiInsights, priceAdjustments: [] };
      available.value = true;
    } catch (err: any) {
      if (requestGeneration !== generation) return;
      error.value = err?.data?.detail || err?.data?.message || "Não foi possível carregar os indicadores.";
    } finally {
      if (requestGeneration === generation) pending.value = false;
    }
  };

  return { data, pending, error, available, refresh };
};
