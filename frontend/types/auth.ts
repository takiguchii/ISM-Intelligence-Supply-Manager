export interface User {
  id: number;
  name: string;
  email: string;
  role: string;
  restaurantId: number | null;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  name: string;
  email: string;
  password: string;
  role?: string;
  restaurantId?: number | null;
}

export interface AuthResponse {
  token: string;
  expiresAt: string;
  user: User;
}

export interface ApiError {
  title: string;
  detail: string;
  status: number;
  traceId?: string;
  message?: string;
}
