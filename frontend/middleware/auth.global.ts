export default defineNuxtRouteMiddleware(async (to) => {
  if (import.meta.server) return;

  const authStore = useAuthStore();
  authStore.initFromStorage();

  const isLoginPage = to.path === "/login";
  const isAuthenticated = authStore.isAuthenticated;

  if (isAuthenticated && isLoginPage) {
    return await navigateTo("/", { replace: true });
  }

  if (!isAuthenticated && !isLoginPage) {
    const redirect = to.fullPath;
    const target =
      redirect && redirect !== "/"
        ? `/login?redirect=${encodeURIComponent(redirect)}`
        : "/login";
    return await navigateTo(target, { replace: true });
  }
});
