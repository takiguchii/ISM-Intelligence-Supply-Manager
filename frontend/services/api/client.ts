export function apiClient<T>(url: string, options?: Parameters<typeof $fetch<T>>[1]) {
  return $fetch<T>(url, {
    ...options
  });
}
