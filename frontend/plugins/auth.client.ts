export default defineNuxtPlugin(() => {
  const authStore = useAuthStore();
  if (process.client) {
    authStore.initFromStorage();
  }
});
