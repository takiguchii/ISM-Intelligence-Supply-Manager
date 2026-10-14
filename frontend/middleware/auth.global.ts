export default defineNuxtRouteMiddleware(async (to) => {
  if (import.meta.server) return;

  const authStore = useAuthStore();
  authStore.initFromStorage();

  const isLoginPage = to.path === "/login";
  const isAuthenticated = authStore.isAuthenticated;

  if (isAuthenticated && isLoginPage) {
    const redirect = to.query.redirect as string;
    const target = redirect && redirect.startsWith("/") && canAccessRoute(authStore.currentUser, redirect)
      ? redirect
      : homeForUser(authStore.currentUser);
    return await navigateTo(target, { replace: true });
  }

  if (!isAuthenticated && !isLoginPage) {
    const redirect = to.fullPath;
    const target =
      redirect && redirect !== "/"
        ? `/login?redirect=${encodeURIComponent(redirect)}`
        : "/login";
    return await navigateTo(target, { replace: true });
  }

  if (isAuthenticated && !isLoginPage && !canAccessRoute(authStore.currentUser, to.path)) {
    return await navigateTo(homeForUser(authStore.currentUser), { replace: true });
  }
});
