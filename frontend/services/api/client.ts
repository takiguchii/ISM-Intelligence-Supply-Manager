import type { FetchOptions } from "ofetch";

export function apiClient<T>(url: string, options?: FetchOptions) {
  const runtimeConfig = useRuntimeConfig();
  const token = process.client ? localStorage.getItem("auth_token") : null;

  const defaultHeaders: Record<string, string> = {
    "Content-Type": "application/json"
  };

  if (token) {
    defaultHeaders["Authorization"] = `Bearer ${token}`;
  }

  const mergedOptions: FetchOptions = {
    baseURL: runtimeConfig.public.apiBase,
    headers: {
      ...defaultHeaders,
      ...(options?.headers || {})
    },
    credentials: "include",
    ...options
  };

  return $fetch<T>(url, mergedOptions);
}

export function apiClientWithAuth<T>(url: string, options?: FetchOptions) {
  return apiClient<T>(url, {
    ...options,
    onResponseError: ({ response }) => {
      if (response.status === 401) {
        if (process.client) {
          localStorage.removeItem("auth_token");
          localStorage.removeItem("auth_user");
        }
        const router = useRouter();
        router.push("/login?redirect=" + encodeURIComponent(window.location.pathname));
      }
    }
  });
}
