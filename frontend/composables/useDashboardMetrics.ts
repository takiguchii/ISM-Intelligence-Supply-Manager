import { ref } from "vue";

export interface RevenueVsCostPoint {
  label: string;
  receita: number;
  custo: number;
}

export interface WeeklyRevenuePoint {
  day: string;
  value: number;
}

export type RiskItemType = "baixo_estoque" | "excesso_estoque" | "validade_proxima";

export interface RiskItem {
  id: string;
  name: string;
  type: RiskItemType;
  detail: string;
}

export type NotificationSeverity = "info" | "atencao" | "critico";

export interface AgentNotification {
  id: string;
  agent: string;
  message: string;
  severity: NotificationSeverity;
  time: string;
}

export interface DashboardMetrics {
  revenueVsCost: RevenueVsCostPoint[];
  weeklyRevenue: WeeklyRevenuePoint[];
  averageMargin: {
    value: number; // percentual (0-100)
    trend: number; // variação em p.p. vs período anterior
  };
  stockTurnover: {
    value: number; // giros por mês
    trend: number; // variação vs período anterior
    status: "saudavel" | "lento" | "critico";
  };
  riskItems: RiskItem[];
  agentNotifications: AgentNotification[];
}

const buildMockData = (): DashboardMetrics => ({
  revenueVsCost: [
    { label: "Sem 1", receita: 18400, custo: 12100 },
    { label: "Sem 2", receita: 21200, custo: 13800 },
    { label: "Sem 3", receita: 19850, custo: 14200 },
    { label: "Sem 4", receita: 24700, custo: 15600 }
  ],
  weeklyRevenue: [
    { day: "Seg", value: 2800 },
    { day: "Ter", value: 3200 },
    { day: "Qua", value: 2950 },
    { day: "Qui", value: 3600 },
    { day: "Sex", value: 4100 },
    { day: "Sáb", value: 4800 },
    { day: "Dom", value: 3250 }
  ],
  averageMargin: {
    value: 34.6,
    trend: 2.1
  },
  stockTurnover: {
    value: 4.2,
    trend: -0.3,
    status: "saudavel"
  },
  riskItems: [
    {
      id: "1",
      name: "Filé de Frango",
      type: "baixo_estoque",
      detail: "Restam 3 kg (mínimo recomendado: 15 kg)"
    },
    {
      id: "2",
      name: "Molho de Tomate",
      type: "excesso_estoque",
      detail: "42 un. em estoque (consumo médio: 10 un./semana)"
    },
    {
      id: "3",
      name: "Queijo Muçarela",
      type: "validade_proxima",
      detail: "Vence em 2 dias — 8 kg em estoque"
    },
    {
      id: "4",
      name: "Farinha de Trigo",
      type: "baixo_estoque",
      detail: "Restam 5 kg (mínimo recomendado: 20 kg)"
    }
  ],
  agentNotifications: [
    {
      id: "1",
      agent: "Agente de Compras",
      message: "Sugestão: antecipar pedido de Filé de Frango para evitar ruptura em 2 dias.",
      severity: "atencao",
      time: "há 12 min"
    },
    {
      id: "2",
      agent: "Agente Financeiro",
      message: "Margem média subiu 2,1 p.p. na última semana — custo de insumos em queda.",
      severity: "info",
      time: "há 1 h"
    },
    {
      id: "3",
      agent: "Agente de Estoque",
      message: "Queijo Muçarela próximo do vencimento. Recomenda-se promoção ou uso prioritário.",
      severity: "critico",
      time: "há 3 h"
    }
  ]
});

export const useDashboardMetrics = () => {
  const data = ref<DashboardMetrics>(buildMockData());
  const pending = ref(false);
  const error = ref<unknown>(null);

  const refresh = async () => {
    pending.value = true;
    try {
      // TODO: substituir pela chamada real, por exemplo:
      // const response = await $fetch<DashboardMetrics>("/api/dashboard/metrics");
      // data.value = response;
      await new Promise((resolve) => setTimeout(resolve, 400));
      data.value = buildMockData();
      error.value = null;
    } catch (err) {
      error.value = err;
    } finally {
      pending.value = false;
    }
  };

  return { data, pending, error, refresh };
};