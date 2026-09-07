import { apiClientWithAuth } from "~/services/api/client";
import type { AuthResponse } from "~/types/auth";

export const EMPLOYEE_ROLES = ["Admin", "Manager", "Chef", "Waiter"] as const;

export type EmployeeRole = (typeof EMPLOYEE_ROLES)[number];

/** Espelha UserDetailsDto (backend). */
export interface EmployeeResponse {
  id: number;
  name: string;
  email: string;
  role: string;
  restaurantId: number | null;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
}

/** Espelha RegisterRequest (backend). */
export interface CreateEmployeeRequest {
  name: string;
  email: string;
  password: string;
  role: string;
  restaurantId?: number | null;
}

/** Espelha UpdateUserRequest (backend). */
export interface UpdateEmployeeRequest {
  name: string;
  email: string;
  role: string;
  restaurantId?: number | null;
  isActive: boolean;
}

export const employeeService = {
  /**
   * GET /api/users
   * Sem restaurantId (Super Admin) a API devolve todos os usuários visíveis.
   * A rota não expõe busca nem paginação: devolve a lista completa.
   */
  async getByRestaurant(restaurantId: number | null): Promise<EmployeeResponse[]> {
    return apiClientWithAuth<EmployeeResponse[]>("/api/users", {
      method: "GET",
      query: restaurantId ? { restaurantId } : undefined
    });
  },

  /**
   * POST /api/auth/register — única rota do backend que cria usuários.
   * Retorna um AuthResponse com o token do funcionário criado; por isso a chamada
   * é feita pelo service e não por authStore.register(), que trocaria a sessão
   * do gestor logado pela do funcionário recém-cadastrado.
   */
  async create(data: CreateEmployeeRequest): Promise<AuthResponse> {
    return apiClientWithAuth<AuthResponse>("/api/auth/register", {
      method: "POST",
      body: data
    });
  },

  async update(id: number, data: UpdateEmployeeRequest): Promise<EmployeeResponse> {
    return apiClientWithAuth<EmployeeResponse>(`/api/users/${id}`, {
      method: "PUT",
      body: data
    });
  },

  async delete(id: number): Promise<void> {
    return apiClientWithAuth<void>(`/api/users/${id}`, {
      method: "DELETE"
    });
  }
};
