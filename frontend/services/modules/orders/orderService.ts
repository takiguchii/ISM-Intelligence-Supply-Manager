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
  items: OrderItem[];
}

export interface CreateOrder {
  requestId: string;
  tableReference: string;
  items: { dishId: number; quantity: number; observation: string }[];
}

export const orderService = {
  list: () => apiClientWithAuth<Order[]>("/api/orders"),
  create: (order: CreateOrder) => apiClientWithAuth<Order>("/api/orders", { method: "POST", body: order }),
  updateStatus: (order: Order, status: OrderStatus) => apiClientWithAuth<Order>(`/api/orders/${order.id}/status`, { method: "PUT", body: { status, version: order.version } })
};
