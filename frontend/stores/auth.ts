import { defineStore } from "pinia";
import type { AuthResponse, LoginRequest, RegisterRequest, User } from "~/types/auth";
import { authService } from "~/services/modules/authService";

interface AuthState {
  token: string | null;
  user: User | null;
  loading: boolean;
}

export const useAuthStore = defineStore("auth", {
  state: (): AuthState => ({
    token: null,
    user: null,
    loading: false
  }),

  getters: {
    isAuthenticated: (state): boolean => !!state.token && !!state.user,
    currentUser: (state): User | null => state.user,
    userRole: (state): string | null => state.user?.role ?? null,
    isLoading: (state): boolean => state.loading
  },

  actions: {
    initFromStorage() {
      if (process.client) {
        const token = localStorage.getItem("auth_token");
        const userStr = localStorage.getItem("auth_user");
        if (token) {
          this.token = token;
        }
        if (userStr) {
          try {
            this.user = JSON.parse(userStr) as User;
          } catch {
            this.clearAuth();
          }
        }
      }
    },

    async login(credentials: LoginRequest): Promise<AuthResponse> {
      this.loading = true;
      try {
        const response = await authService.login(credentials);
        this.setAuth(response);
        return response;
      } finally {
        this.loading = false;
      }
    },

    async register(data: RegisterRequest): Promise<AuthResponse> {
      this.loading = true;
      try {
        const response = await authService.register(data);
        this.setAuth(response);
        return response;
      } finally {
        this.loading = false;
      }
    },

    async fetchMe(): Promise<User | null> {
      if (!this.token) return null;
      this.loading = true;
      try {
        const user = await authService.me();
        this.user = user;
        if (process.client) {
          localStorage.setItem("auth_user", JSON.stringify(user));
        }
        return user;
      } catch {
        this.clearAuth();
        return null;
      } finally {
        this.loading = false;
      }
    },

    setAuth(response: AuthResponse) {
      this.token = response.token;
      this.user = response.user;
      if (process.client) {
        localStorage.setItem("auth_token", response.token);
        localStorage.setItem("auth_user", JSON.stringify(response.user));
      }
    },

    clearAuth() {
      this.token = null;
      this.user = null;
      if (process.client) {
        localStorage.removeItem("auth_token");
        localStorage.removeItem("auth_user");
      }
    },

    logout() {
      this.clearAuth();
    }
  }
});
