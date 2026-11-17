export type AssistantProposalStatus = "Pending" | "Approved" | "Rejected" | "Stale" | "Failed" | "Expired";
export type AssistantVisualState = "idle" | "watching" | "thinking" | "attention" | "approval" | "error" | "offline" | "success";

export interface AssistantAction {
  id: number;
  alertId: number;
  specialist: string;
  actionType: string;
  entityType: string;
  entityId: number;
  entityName: string;
  status: AssistantProposalStatus | number;
  originalValue: number;
  proposedValue: number;
  snapshotCost: number;
  snapshotTargetMarginPercent: number | null;
  evidenceJson: string;
  generatedAtUtc: string;
  decidedByUserId: number | null;
  decidedAtUtc: string | null;
  expiresAtUtc: string;
  version: number;
}

export interface AssistantStatus {
  enabled: boolean;
  providerReachable: boolean;
  model: string;
  message: string | null;
}

export interface AssistantOverview {
  status: AssistantStatus;
  pendingActions: number;
  unreadAlerts: number;
  actions: AssistantAction[];
}

export interface AssistantChatResponse {
  mode: "llm" | "deterministic_fallback";
  message: string;
  source: string;
  warning: string | null;
  specialist?: string | null;
  toolName?: string | null;
  decisionCode?: number | null;
  evidence?: Array<{ sourceType: string; sourceId: string; restaurantId: number; observedAtUtc: string; asOfUtc: string | null; summary: string }>;
  generatedAtUtc?: string;
}

export interface AssistantContext {
  alertId: number;
  alertType: string;
  severity: string;
  title: string;
  message: string;
  referenceEntityType: string | null;
  referenceEntityId: number | null;
  entityName: string;
  facts: Record<string, string>;
  generatedAtUtc: string;
}

export interface AssistantThread {
  id: number;
  parentThreadId: number | null;
  originAlertId: number | null;
  originType: string;
  subject: string;
  context: AssistantContext | null;
  createdAtUtc: string;
  lastMessageAtUtc: string;
}

export interface AssistantMessage {
  id: number;
  role: "user" | "assistant";
  content: string;
  mode: string | null;
  source: string | null;
  createdAtUtc: string;
}

export interface AssistantConversation {
  thread: AssistantThread;
  messages: AssistantMessage[];
}
