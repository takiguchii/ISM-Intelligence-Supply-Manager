import { apiClientWithAuth } from "~/services/api/client";

export enum FinanceEntryType { Income, Expense }
export interface FinanceEntry { id: number; description: string; category: string; type: FinanceEntryType; amount: number; dueDate: string; settledAtUtc: string | null }
export interface FinanceEntryRequest { description: string; category: string; type: FinanceEntryType; amount: number; dueDate: string; isSettled: boolean }
export interface FinanceEntryPage { items: FinanceEntry[]; pageNumber: number; pageSize: number; totalCount: number; totalPages: number; hasPreviousPage: boolean; hasNextPage: boolean }
export interface FinanceAnalytics { expensesByCategory: { label: string; amount: number }[]; topSellingItems: { name: string; quantity: number; revenue: number }[]; cashFlow: { month: string; revenue: number; expenses: number }[] }
export interface FinanceOverview { summary: { salesRevenue: number; otherIncome: number; expenses: number; accountsReceivable: number; accountsPayable: number }; entries: FinanceEntryPage; analytics: FinanceAnalytics }
export const financeService = {
  get: (pageNumber = 1, pageSize = 10) => apiClientWithAuth<FinanceOverview>("/api/finance", { query: { pageNumber, pageSize } }),
  create: (data: FinanceEntryRequest) => apiClientWithAuth<FinanceEntry>("/api/finance", { method: "POST", body: data }),
  update: (id: number, data: FinanceEntryRequest) => apiClientWithAuth<FinanceEntry>(`/api/finance/${id}`, { method: "PUT", body: data }),
  delete: (id: number) => apiClientWithAuth<void>(`/api/finance/${id}`, { method: "DELETE" })
};
