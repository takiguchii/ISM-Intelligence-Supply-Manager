import { ref } from "vue";

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

const buildMockData = (): DashboardMetrics => ({
  kpis: [
    {
      id: "receita-semanal",
      label: "Receita Semanal",
      value: "R$ 40,4k",
      delta: "+12,4%",
      trend: "up",
      icon: "Money",
      hint: "vs. semana anterior",
    },
    {
      id: "margem-media",
      label: "Margem Média",
      value: "58,2%",
      delta: "+3,1pp",
      trend: "up",
      icon: "TrendCharts",
      hint: "meta 55%",
    },
    {
      id: "giro-estoque",
      label: "Giro de Estoque",
      value: "2,4x",
      delta: "-0,3x",
      trend: "down",
      icon: "Refresh",
      hint: "abaixo da meta",
    },
    {
      id: "itens-risco",
      label: "Itens em Risco",
      value: "7",
      delta: "+2",
      trend: "down",
      icon: "WarnTriangleFilled",
      hint: "estoque crítico",
    },
  ],
  aiInsights: [
    {
      id: "precificacao",
      icon: "Opportunity",
      agent: "Agente de Precificação",
      severity: "warning",
      title: "Margem baixa no Polvo Grelhado",
      description:
        'Margem caiu para <strong class="text-white">31%</strong>. Sugiro ajuste de <strong class="text-white">R$ 78 → R$ 86</strong>.',
      primaryLabel: "Aplicar",
      primarySuccessMessage: "Novo preço aplicado",
      secondaryLabel: "Detalhes",
    },
    {
      id: "estoque",
      icon: "ShoppingCart",
      agent: "Agente de Estoque",
      severity: "critical",
      title: "Compra urgente: Camarão",
      description:
        'Estoque <strong class="text-white">2,1 kg</strong> · ruptura em <strong class="text-red-400">~18h</strong>.',
      primaryLabel: "Gerar pedido",
      primarySuccessMessage: "Pedido enviado",
      secondaryLabel: "Adiar",
    },
    {
      id: "cardapio",
      icon: "ForkSpoon",
      agent: "Agente de Cardápio",
      severity: "info",
      title: "Tiramisù em alta",
      description:
        '<strong class="text-white">+38%</strong> em vendas com margem de <strong class="text-emerald-400">72%</strong>.',
      primaryLabel: "Promover",
    },
  ],
  revenueVsCost: [
    { day: "Seg", receita: 4200, custo: 2100 },
    { day: "Ter", receita: 3800, custo: 1900 },
    { day: "Qua", receita: 5100, custo: 2400 },
    { day: "Qui", receita: 4800, custo: 2200 },
    { day: "Sex", receita: 7200, custo: 3100 },
    { day: "Sáb", receita: 8400, custo: 3500 },
    { day: "Dom", receita: 6900, custo: 3000 },
  ],
  topDishes: [
    { name: "Burguer Trufado", pedidos: 184 },
    { name: "Risoto Funghi", pedidos: 142 },
    { name: "Tiramisù", pedidos: 128 },
    { name: "Salada Caesar", pedidos: 96 },
    { name: "Polvo Grelhado", pedidos: 71 },
  ],
  weekdayOrders: [
    { day: "Seg", pedidos: 62 },
    { day: "Ter", pedidos: 54 },
    { day: "Qua", pedidos: 78 },
    { day: "Qui", pedidos: 84 },
    { day: "Sex", pedidos: 142 },
    { day: "Sáb", pedidos: 168 },
    { day: "Dom", pedidos: 121 },
  ],
  priceAdjustments: [
    {
      id: "polvo",
      name: "Polvo Grelhado",
      from: 78,
      to: 86,
      direction: "up",
      reason: "Margem 31% (meta 45%)",
    },
    {
      id: "risoto",
      name: "Risoto Funghi",
      from: 62,
      to: 68,
      direction: "up",
      reason: "Custo do funghi +14%",
    },
    {
      id: "burguer",
      name: "Burguer Trufado",
      from: 48,
      to: 52,
      direction: "up",
      reason: "Alta demanda + elasticidade",
    },
    {
      id: "caesar",
      name: "Salada Caesar",
      from: 32,
      to: 29,
      direction: "down",
      reason: "Baixa rotação, ganhar volume",
    },
    {
      id: "tiramisu",
      name: "Tiramisù",
      from: 26,
      to: 24,
      direction: "down",
      reason: "Estimular ticket de sobremesa",
    },
  ],
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
