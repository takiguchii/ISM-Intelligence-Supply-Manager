import { ref } from "vue";
import { loginUser } from "~/services/modules/authService";
import type { LoginCredentials, UserProfile } from "~/types/auth";

export function useAuth() {
  const user = useState<UserProfile | null>("auth-user", () => null);
  const isAuthenticated = computed(() => !!user.value);
  const loading = ref(false);
  const error = ref<string | null>(null);

  const login = async (credentials: LoginCredentials) => {
    loading.value = true;
    error.value = null;
    try {
      if (!credentials.username || !credentials.username.trim()) {
        throw new Error("Por favor, informe o seu usuário.");
      }
      const response = await loginUser(credentials);
      user.value = response.user;
      return response;
    } catch (err: any) {
      error.value = err?.message || "Ocorreu um erro ao realizar o login. Verifique suas credenciais.";
      throw err;
    } finally {
      loading.value = false;
    }
  };

  const logout = () => {
    user.value = null;
  };

  return {
    user,
    isAuthenticated,
    loading,
    error,
    login,
    logout
  };
}
