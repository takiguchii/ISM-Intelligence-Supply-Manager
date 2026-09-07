import { apiClientWithAuth } from "~/services/api/client";

export interface ProductResponse {
  id: number;
  restaurantId: number;
  name: string;
  unit: string;
  currentQuantity: number;
  minimumQuantity: number;
  averageCost: number;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
}

export interface PagedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface CreateProductRequest {
  restaurantId?: number;
  name: string;
  unit: string;
  currentQuantity: number;
  minimumQuantity: number;
  averageCost?: number;
}

export interface UpdateProductRequest {
  name: string;
  unit: string;
  currentQuantity: number;
  minimumQuantity: number;
  averageCost?: number;
}

export const productService = {
  async getPaged(params: {
    pageNumber?: number;
    pageSize?: number;
    search?: string;
    isCritical?: boolean;
  }): Promise<PagedResult<ProductResponse>> {
    const query = new URLSearchParams();
    if (params.pageNumber) query.append("pageNumber", params.pageNumber.toString());
    if (params.pageSize) query.append("pageSize", params.pageSize.toString());
    if (params.search?.trim()) query.append("search", params.search.trim());
    if (params.isCritical !== undefined) query.append("isCritical", params.isCritical.toString());

    const queryString = query.toString();
    const endpoint = `/api/stock/products/paged${queryString ? `?${queryString}` : ""}`;
    return apiClientWithAuth<PagedResult<ProductResponse>>(endpoint, {
      method: "GET"
    });
  },

  async getAll(): Promise<ProductResponse[]> {
    return apiClientWithAuth<ProductResponse[]>("/api/stock/products", {
      method: "GET"
    });
  },

  async getById(id: number): Promise<ProductResponse> {
    return apiClientWithAuth<ProductResponse>(`/api/stock/products/${id}`, {
      method: "GET"
    });
  },

  async create(data: CreateProductRequest): Promise<ProductResponse> {
    return apiClientWithAuth<ProductResponse>("/api/stock/products", {
      method: "POST",
      body: data
    });
  },

  async update(id: number, data: UpdateProductRequest): Promise<ProductResponse> {
    return apiClientWithAuth<ProductResponse>(`/api/stock/products/${id}`, {
      method: "PUT",
      body: data
    });
  },

  async delete(id: number): Promise<void> {
    return apiClientWithAuth<void>(`/api/stock/products/${id}`, {
      method: "DELETE"
    });
  }
};
