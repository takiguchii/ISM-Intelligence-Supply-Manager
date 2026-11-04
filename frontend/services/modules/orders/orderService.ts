import { apiClientWithAuth } from "~/services/api/client";

export enum OrderStatus {
  Submitted, Preparing, Ready, Completed, Paid, Cancelled
}

export interface OrderItem {
  dishId: number;
  dishName: string;
  quantity: number;
  unitPrice: number;
  unitCost: number;
  observation: string;
}

export interface Order {
  id: number;
  requestId: string;
  tableReference: string;
  status: OrderStatus;
  createdAtUtc: string;
  paidAtUtc: string | null;
  version: number;
  createdByUserId: number | null;
  createdByName: string | null;
  items: OrderItem[];
}

export interface OrderPage {
  items: Order[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface CreateOrder {
  requestId: string;
  tableReference: string;
  items: { dishId: number; quantity: number; observation: string }[];
}

export interface UpdateOrder {
  tableReference: string;
  items: { dishId: number; quantity: number; observation: string }[];
  version: number;
}

export const orderService = {
  list: () => apiClientWithAuth<Order[]>("/api/orders"),
  adminList: (pageNumber = 1, pageSize = 10, search?: string) => apiClientWithAuth<OrderPage>("/api/orders/admin", { query: { pageNumber, pageSize, search: search || undefined } }),
  create: (order: CreateOrder) => apiClientWithAuth<Order>("/api/orders", { method: "POST", body: order }),
  updateStatus: (order: Order, status: OrderStatus) => apiClientWithAuth<Order>(`/api/orders/${order.id}/status`, { method: "PUT", body: { status, version: order.version } }),
  update: (order: UpdateOrder, id: number) => apiClientWithAuth<Order>(`/api/orders/${id}`, { method: "PUT", body: order }),
  delete: (id: number) => apiClientWithAuth<void>(`/api/orders/${id}`, { method: "DELETE" })
};
