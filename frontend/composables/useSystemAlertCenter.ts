import { computed, ref } from "vue";
import { useAuthStore } from "~/stores/auth";
import { dismissAlert, dismissAllAlerts, getSystemAlerts, markAlertAsRead } from "~/services/modules/system/systemService";
import { settingsService, type NotificationPreferences } from "~/services/modules/system/settingsService";
import type { SystemAlertResponse } from "~/types/system";
import { shouldAnnounceNewAlert } from "~/types/agentExperience";

const alerts = ref<SystemAlertResponse[]>([]);
const preferences = ref<NotificationPreferences>({ stock: true, suppliers: true, menu: true });
const loading = ref(false);
const error = ref("");
const initialized = ref(false);
const baselineComplete = ref(false);
const newAlert = ref<SystemAlertResponse | null>(null);
const groupedNewCount = ref(0);
let requestInFlight = false;
let alertVersion = 0;
let pollTimer: ReturnType<typeof setTimeout> | null = null;
let started = false;
let scopeKey = "";
const seenIds = new Set<number>();
const announcedAt = new Map<number, number>();
let visibilityListener: (() => void) | null = null;
let preferenceListener: (() => void) | null = null;

const currentScope = () => {
  const auth = useAuthStore();
  const user = auth.currentUser;
  return `${user?.id ?? "anonymous"}:${user?.restaurantId ?? "platform"}:${user?.role ?? "guest"}`;
};

const resetForScope = (nextScope: string) => {
  if (scopeKey === nextScope) return;
  scopeKey = nextScope;
  alerts.value = [];
  initialized.value = false;
  baselineComplete.value = false;
  newAlert.value = null;
  groupedNewCount.value = 0;
  seenIds.clear();
  announcedAt.clear();
};

const alertsSignature = (items: SystemAlertResponse[]) => items
  .map(item => `${item.id}:${item.isRead ? 1 : 0}:${item.isDismissed ? 1 : 0}:${item.updatedAtUtc ?? item.generatedAtUtc}`)
  .join("|");

const isVisibleByPreference = (alert: SystemAlertResponse) => {
  const type = String(alert.alertType);
  if (type.includes("Stock")) return preferences.value.stock;
  if (type.includes("Supplier")) return preferences.value.suppliers;
  if (type.includes("Dish") || type.includes("Cmv")) return preferences.value.menu;
  return true;
};

const schedule = () => {
  if (!started || typeof window === "undefined" || document.hidden) return;
  if (pollTimer) clearTimeout(pollTimer);
  pollTimer = setTimeout(() => void refresh(true), 45_000);
};

const refresh = async (announce = false) => {
  if (requestInFlight) return;
  resetForScope(currentScope());
  requestInFlight = true;
  loading.value = alerts.value.length === 0;
  error.value = "";
  const version = alertVersion;
  const scope = scopeKey;
  try {
    const [result, savedPreferences] = await Promise.all([
      getSystemAlerts({ pageSize: 200, isDismissed: false }),
      settingsService.notifications()
    ]);
    if (version !== alertVersion || scope !== currentScope()) return;
    preferences.value = savedPreferences;
    const nextAlerts = result.items.filter(isVisibleByPreference);
    const candidates = initialized.value && announce
      ? nextAlerts.filter(item => shouldAnnounceNewAlert(item.id, baselineComplete.value, seenIds) && !item.isRead && !item.isDismissed)
      : [];
    for (const item of nextAlerts) seenIds.add(item.id);
    if (candidates.length) {
      const now = Date.now();
      const eligible = candidates.filter(item => !announcedAt.has(item.id) || now - (announcedAt.get(item.id) || 0) > 60_000);
      if (eligible.length) {
        eligible.forEach(item => announcedAt.set(item.id, now));
        newAlert.value = eligible[0];
        groupedNewCount.value = eligible.length;
      }
    }
    if (alertsSignature(alerts.value) !== alertsSignature(nextAlerts)) alerts.value = nextAlerts;
    initialized.value = true;
    baselineComplete.value = true;
  } catch (cause: any) {
    error.value = cause?.data?.detail || "Não foi possível atualizar os alertas.";
  } finally {
    requestInFlight = false;
    loading.value = false;
    schedule();
  }
};

const ensureLoaded = async () => {
  if (!initialized.value) await refresh(false);
};

const start = () => {
  if (started || typeof window === "undefined") return;
  started = true;
  visibilityListener = () => {
    if (document.hidden) {
      if (pollTimer) clearTimeout(pollTimer);
      pollTimer = null;
    } else {
      void refresh(true);
    }
  };
  preferenceListener = () => void refresh(false);
  window.addEventListener("visibilitychange", visibilityListener);
  window.addEventListener("ism-notification-preferences", preferenceListener);
  void refresh(false);
  schedule();
};

const stop = () => {
  if (!started || typeof window === "undefined") return;
  if (pollTimer) clearTimeout(pollTimer);
  pollTimer = null;
  if (visibilityListener) window.removeEventListener("visibilitychange", visibilityListener);
  if (preferenceListener) window.removeEventListener("ism-notification-preferences", preferenceListener);
  visibilityListener = null;
  preferenceListener = null;
  started = false;
};

const markRead = async (alert: SystemAlertResponse) => {
  if (alert.isRead) return alert;
  const updated = await markAlertAsRead(alert.id);
  const index = alerts.value.findIndex(item => item.id === alert.id);
  if (index >= 0) alerts.value[index] = updated;
  if (newAlert.value?.id === alert.id) newAlert.value = null;
  return updated;
};

const dismiss = async (alert: SystemAlertResponse) => {
  const updated = await dismissAlert(alert.id);
  alerts.value = alerts.value.filter(item => item.id !== alert.id);
  if (newAlert.value?.id === alert.id) newAlert.value = null;
  return updated;
};

const dismissAll = async () => {
  const scope = currentScope();
  const { count } = await dismissAllAlerts();
  if (scope === currentScope()) {
    alertVersion++;
    alerts.value = [];
    newAlert.value = null;
    groupedNewCount.value = 0;
    await refresh(false);
  }
  return count;
};

export const useSystemAlertCenter = () => ({
  alerts,
  preferences,
  loading,
  error,
  newAlert,
  groupedNewCount,
  unreadCount: computed(() => alerts.value.filter(item => !item.isRead && !item.isDismissed).length),
  ensureLoaded,
  refresh,
  start,
  stop,
  markRead,
  dismiss,
  dismissAll,
  clearNewAlert: () => { newAlert.value = null; groupedNewCount.value = 0; }
});
