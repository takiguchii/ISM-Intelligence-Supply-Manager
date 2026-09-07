import { apiClientWithAuth } from "~/services/api/client";

export interface SupplierResponse {
  id: number;
  restaurantId: number;
  name: string;
  category: string;
  description?: string | null;
  email: string;
  phone: string;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
}

export interface CreateSupplierRequest {
  restaurantId?: number;
  name: string;
  category: string;
  description?: string;
  email: string;
  phone: string;
}

export interface UpdateSupplierRequest {
  name: string;
  category: string;
  description?: string;
  email: string;
  phone: string;
}

export const supplierService = {
  async getAll(): Promise<SupplierResponse[]> {
    return apiClientWithAuth<SupplierResponse[]>("/api/suppliers", {
      method: "GET"
    });
  },

  async getById(id: number): Promise<SupplierResponse> {
    return apiClientWithAuth<SupplierResponse>(`/api/suppliers/${id}`, {
      method: "GET"
    });
  },

  async create(data: CreateSupplierRequest): Promise<SupplierResponse> {
    return apiClientWithAuth<SupplierResponse>("/api/suppliers", {
      method: "POST",
      body: data
    });
  },

  async update(id: number, data: UpdateSupplierRequest): Promise<SupplierResponse> {
    return apiClientWithAuth<SupplierResponse>(`/api/suppliers/${id}`, {
      method: "PUT",
      body: data
    });
  },

  async delete(id: number): Promise<void> {
    return apiClientWithAuth<void>(`/api/suppliers/${id}`, {
      method: "DELETE"
    });
  }
};
