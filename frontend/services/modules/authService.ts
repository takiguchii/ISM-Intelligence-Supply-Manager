import type { AuthResponse, LoginRequest, RegisterRequest, User } from "~/types/auth";
import { apiClient } from "~/services/api/client";

export const authService = {
  async login(data: LoginRequest): Promise<AuthResponse> {
    return await apiClient<AuthResponse>("/api/auth/login", {
      method: "POST",
      body: data
    });
  },

  async register(data: RegisterRequest): Promise<AuthResponse> {
    return await apiClient<AuthResponse>("/api/auth/register", {
      method: "POST",
      body: data
    });
  },

  async me(): Promise<User> {
    return await apiClient<User>("/api/auth/me", {
      method: "GET"
    });
  }
};
