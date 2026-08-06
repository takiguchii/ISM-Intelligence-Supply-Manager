export interface LoginCredentials {
  username: string;
  password?: string;
  rememberMe?: boolean;
}

export interface UserProfile {
  id: string;
  username: string;
  name: string;
  role: string;
}

export interface AuthResponse {
  token: string;
  user: UserProfile;
}
