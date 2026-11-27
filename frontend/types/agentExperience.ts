import type { SystemAlertResponse } from "~/types/system";

export type AgentKind = "stock" | "pricing" | "suppliers" | "finance" | "general";
export type AgentMood =
  | "idle"
  | "curious"
  | "happy"
  | "worried"
  | "sad"
  | "surprised"
  | "thinking"
  | "speaking"
  | "waiting-approval"
  | "success"
  | "error"
  | "offline";
export type AgentSeverity = "info" | "warning" | "critical" | "success" | "unknown";
export type AgentAttention = "none" | "info" | "warning" | "critical";
export type AgentCardState = "ready" | "empty" | "error" | "loading" | "stale";

export interface AgentCardViewModel {
  agent: AgentKind;
  label: string;
  title: string;
  description: string;
  severity: AgentSeverity;
  mood: AgentMood;
  attentionLevel: AgentAttention;
  alertId: number | null;
  alertIds: number[];
  unreadCount: number;
  generatedAtUtc: string | null;
  referenceEntityType: string | null;
  referenceEntityId: number | null;
  facts: Record<string, string>;
  state: AgentCardState;
}

const AGENT_LABELS: Record<AgentKind, string> = {
  stock: "Estoque",
  pricing: "Precificação",
  suppliers: "Fornecedores",
  finance: "Financeiro",
  general: "Assistente"
};

const severityRank: Record<AgentSeverity, number> = {
  critical: 4,
  warning: 3,
  info: 2,
  success: 1,
  unknown: 0
};

const asText = (value: unknown) => (typeof value === "string" || typeof value === "number" || typeof value === "boolean" ? String(value) : "");

export const alertTypeName = (value: unknown) => {
  switch (String(value)) {
    case "0": return "StockRuptureRisk48h";
    case "1": return "StockBelowMinimum";
    case "2": return "SupplierPriceSpike15Percent";
    case "3": return "SupplierBetterAlternative";
    case "4": return "DishCmvDefasagem";
    default: return String(value);
  }
};

export const parseAlertFacts = (alert: SystemAlertResponse): Record<string, string> => {
  if (!alert.payloadSerializedJson) return {};
  try {
    const payload = JSON.parse(alert.payloadSerializedJson) as Record<string, unknown>;
    return Object.fromEntries(Object.entries(payload).flatMap(([key, value]) => {
      const text = asText(value);
      return text ? [[key, text]] : [];
    }));
  } catch {
    return {};
  }
};

export const agentForAlert = (alert: Pick<SystemAlertResponse, "alertType" | "referenceEntityType">): AgentKind => {
  switch (alertTypeName(alert.alertType)) {
    case "StockRuptureRisk48h":
    case "StockBelowMinimum":
      return "stock";
    case "SupplierPriceSpike15Percent":
    case "SupplierBetterAlternative":
      return "suppliers";
    case "DishCmvDefasagem":
      return "pricing";
    default:
      if (alert.referenceEntityType === "Product") return "stock";
      if (alert.referenceEntityType === "Dish") return "pricing";
      if (alert.referenceEntityType === "Supplier") return "suppliers";
      return "general";
  }
};

export const severityForAlert = (alert: { severity: string | number }): AgentSeverity => {
  switch (String(alert.severity)) {
    case "2":
    case "Critical":
    case "Danger":
      return "critical";
    case "1":
    case "Warning":
      return "warning";
    case "0":
    case "Info":
      return "info";
    default:
      return "unknown";
  }
};

export const moodForAlert = (alert: { alertType: string | number; severity: string | number }): AgentMood => {
  const severity = severityForAlert(alert);
  if (severity === "critical") return alertTypeName(alert.alertType) === "StockRuptureRisk48h" ? "worried" : "sad";
  if (severity === "warning") return "worried";
  if (severity === "info") return "curious";
  return "idle";
};

export const attentionForSeverity = (severity: AgentSeverity): AgentAttention => severity === "critical" ? "critical" : severity === "warning" ? "warning" : severity === "info" ? "info" : "none";

const compareAlerts = (left: SystemAlertResponse, right: SystemAlertResponse) => {
  const severityDifference = severityRank[severityForAlert(right)] - severityRank[severityForAlert(left)];
  if (severityDifference) return severityDifference;
  return new Date(right.generatedAtUtc || right.createdAtUtc).getTime() - new Date(left.generatedAtUtc || left.createdAtUtc).getTime();
};

const emptyCard = (agent: AgentKind): AgentCardViewModel => ({
  agent,
  label: AGENT_LABELS[agent],
  title: "Nenhuma ocorrência importante",
  description: "Não há alerta ativo suficiente para exibir um risco deste especialista.",
  severity: "unknown",
  mood: "idle",
  attentionLevel: "none",
  alertId: null,
  alertIds: [],
  unreadCount: 0,
  generatedAtUtc: null,
  referenceEntityType: null,
  referenceEntityId: null,
  facts: {},
  state: "empty"
});

export const aggregateAgentCards = (alerts: readonly SystemAlertResponse[]): AgentCardViewModel[] => {
  const cards = new Map<AgentKind, AgentCardViewModel>([
    ["stock", emptyCard("stock")],
    ["pricing", emptyCard("pricing")],
    ["suppliers", emptyCard("suppliers")]
  ]);

  for (const agent of ["stock", "pricing", "suppliers"] as const) {
    const grouped = alerts.filter(alert => agentForAlert(alert) === agent && !alert.isDismissed).sort(compareAlerts);
    const primary = grouped[0];
    if (!primary) continue;
    const severity = severityForAlert(primary);
    cards.set(agent, {
      agent,
      label: AGENT_LABELS[agent],
      title: primary.title,
      description: primary.message,
      severity,
      mood: moodForAlert(primary),
      attentionLevel: attentionForSeverity(severity),
      alertId: primary.id,
      alertIds: grouped.map(alert => alert.id),
      unreadCount: grouped.filter(alert => !alert.isRead).length,
      generatedAtUtc: primary.generatedAtUtc || primary.createdAtUtc || null,
      referenceEntityType: primary.referenceEntityType || null,
      referenceEntityId: primary.referenceEntityId ?? null,
      facts: parseAlertFacts(primary),
      state: "ready"
    });
  }

  return [...cards.values()];
};

export const shouldAnnounceNewAlert = (alertId: number, baselineComplete: boolean, seenIds: ReadonlySet<number>) => baselineComplete && !seenIds.has(alertId);
