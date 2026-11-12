import type { SystemAlertResponse } from "../types/system";

type AlertPayload = Omit<SystemAlertResponse, "severity" | "alertType"> & {
  severity: number | string;
  alertType: number | string;
};

const alertTypes = ["StockRuptureRisk48h", "StockBelowMinimum", "SupplierPriceSpike15Percent", "SupplierBetterAlternative", "DishCmvDefasagem"];

// O backend serializa enums numericamente; a interface usa nomes para filtros e cores.
export function normalizeSystemAlert(alert: AlertPayload): SystemAlertResponse {
  const severity = String(alert.severity).toLowerCase();
  return {
    ...alert,
    alertType: typeof alert.alertType === "number" ? (alertTypes[alert.alertType] ?? "Unknown") : alert.alertType,
    severity: ["2", "danger", "critical"].includes(severity) ? "Critical"
      : ["1", "warning"].includes(severity) ? "Warning" : "Info"
  };
}
