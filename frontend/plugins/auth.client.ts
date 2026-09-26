export default defineNuxtPlugin(() => {
  const authStore = useAuthStore();
  const themeStore = useThemeStore();
  authStore.initFromStorage();
  themeStore.init();
});
