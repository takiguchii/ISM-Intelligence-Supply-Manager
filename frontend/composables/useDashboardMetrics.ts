import { ref, type Ref } from "vue";
import { apiClientWithAuth } from "~/services/api/client";
import { getSystemAlerts } from "~/services/modules/system/systemService";

export interface Kpi {
  id: string;
  label: string;
  value: string;
  delta: string;
  trend: "up" | "down";
  icon: string;
  hint?: string;
}

export type InsightSeverity = "info" | "warning" | "critical";

export interface AgentInsight {
  id: string;
  icon: string;
  agent: string;
  severity: InsightSeverity;
  title: string;
  description: string;
  primaryLabel?: string;
  primarySuccessMessage?: string;
  secondaryLabel?: string;
}

export interface RevenueVsCostPoint {
  day: string;
  receita: number;
  custo: number;
}

export interface TopDish {
  name: string;
  pedidos: number;
}

export interface WeekdayOrders {
  day: string;
  pedidos: number;
}

export interface PriceAdjustment {
  id: string;
  name: string;
  from: number;
  to: number;
  direction: "up" | "down";
  reason: string;
}

export interface DashboardMetrics {
  kpis: Kpi[];
  aiInsights: AgentInsight[];
  revenueVsCost: RevenueVsCostPoint[];
  topDishes: TopDish[];
  weekdayOrders: WeekdayOrders[];
  priceAdjustments: PriceAdjustment[];
}

export const useDashboardMetrics = (restaurantId: Ref<number | null>, requiresSelection: Ref<boolean>) => {
  const data = ref<DashboardMetrics>({ kpis: [], aiInsights: [], revenueVsCost: [], topDishes: [], weekdayOrders: [], priceAdjustments: [] });
  const pending = ref(false);
  const error = ref<string | null>(null);
  const available = ref(false);
  let generation = 0;

  const refresh = async () => {
    const requestGeneration = ++generation;
    available.value = false;
    if (requiresSelection.value && !restaurantId.value) {
      pending.value = false;
      error.value = "Selecione um restaurante para consultar os indicadores.";
      return;
    }
    pending.value = true;
    error.value = null;
    try {
      const [metricsResponse, alertsResponse] = await Promise.all([
        apiClientWithAuth<Omit<DashboardMetrics, "aiInsights" | "priceAdjustments">>("/api/dashboard/metrics", {
          params: restaurantId.value ? { restaurantId: restaurantId.value } : undefined
        }),
        getSystemAlerts({ pageSize: 6, isDismissed: false }).catch(() => null)
      ]);
      if (requestGeneration !== generation) return;

      const aiInsights: AgentInsight[] = [];
      if (alertsResponse?.items) {
        for (const item of alertsResponse.items) {
          const sev: InsightSeverity =
            item.severity === "Critical" ? "critical" : item.severity === "Warning" ? "warning" : "info";
          aiInsights.push({
            id: `alert-${item.id}`,
            icon: item.referenceEntityType === "Product" ? "Warning" : "Bell",
            agent: item.referenceEntityType === "Product" ? "Agente de Estoque" : "Agente de Suprimentos",
            severity: sev,
            title: item.title,
            description: item.message,
            secondaryLabel: "Ver Alertas"
          });
        }
      }

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
