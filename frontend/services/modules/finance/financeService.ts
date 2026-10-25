import { apiClientWithAuth } from "~/services/api/client";

export enum FinanceEntryType { Income, Expense }
export interface FinanceEntry { id: number; description: string; type: FinanceEntryType; amount: number; dueDate: string; settledAtUtc: string | null }
export interface FinanceEntryRequest { description: string; type: FinanceEntryType; amount: number; dueDate: string; isSettled: boolean }
export interface FinanceOverview { summary: { salesRevenue: number; otherIncome: number; expenses: number; accountsReceivable: number; accountsPayable: number }; entries: FinanceEntry[] }
export const financeService = {
  get: () => apiClientWithAuth<FinanceOverview>("/api/finance"),
  create: (data: FinanceEntryRequest) => apiClientWithAuth<FinanceEntry>("/api/finance", { method: "POST", body: data }),
  update: (id: number, data: FinanceEntryRequest) => apiClientWithAuth<FinanceEntry>(`/api/finance/${id}`, { method: "PUT", body: data }),
  delete: (id: number) => apiClientWithAuth<void>(`/api/finance/${id}`, { method: "DELETE" })
};
