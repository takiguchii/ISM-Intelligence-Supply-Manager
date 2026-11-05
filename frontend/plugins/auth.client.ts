export default defineNuxtPlugin(() => {
  const authStore = useAuthStore();
  const themeStore = useThemeStore();
  authStore.initFromStorage();
  themeStore.init();
  watch(() => authStore.currentUser?.id, (id) => {
    if (id && authStore.isAuthenticated) void themeStore.loadPreferences().catch(() => {});
    else ++themeStore.preferenceVersion;
  }, { immediate: true });
});
