import { test } from "node:test";
import assert from "node:assert/strict";
import { normalizeSystemAlert } from "../../utils/systemAlerts.ts";
import type { SystemAlertResponse } from "../../types/system.ts";

const alert = { id: 1, title: "Estoque baixo", isRead: false } as SystemAlertResponse;

test("adapta enums numéricos da API para cores e filtros da interface", () => {
  const types = ["StockRuptureRisk48h", "StockBelowMinimum", "SupplierPriceSpike15Percent", "SupplierBetterAlternative", "DishCmvDefasagem"];
  types.forEach((type, index) => {
    const normalized = normalizeSystemAlert({ ...alert, alertType: index, severity: 2 });
    assert.equal(normalized.alertType, type);
    assert.equal(normalized.severity.toLowerCase(), "critical");
    assert.equal(normalized.id, 1);
  });
  assert.equal(normalizeSystemAlert({ ...alert, alertType: 0, severity: 1 }).severity, "Warning");
  assert.equal(normalizeSystemAlert({ ...alert, alertType: 0, severity: 0 }).severity, "Info");
});

test("aceita enums por nome e preserva o estado após marcar lida", () => {
  const normalized = normalizeSystemAlert({ ...alert, isRead: true, alertType: "DishCmvDefasagem", severity: "Danger" });
  assert.equal(normalized.severity, "Critical");
  assert.equal(normalized.alertType.includes("Dish"), true);
  assert.equal(normalized.isRead, true);
  assert.equal(normalizeSystemAlert({ ...alert, alertType: 99, severity: 99 }).severity, "Info");
});
