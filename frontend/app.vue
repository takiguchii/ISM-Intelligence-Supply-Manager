<script setup lang="ts">
import { useAuthStore } from "~/stores/auth";
import { useThemeStore } from "~/stores/theme";

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();
const themeStore = useThemeStore();

const authReady = ref(false);

onMounted(() => {
  authStore.initFromStorage();
  themeStore.init();
  authReady.value = true;
});

watchEffect(() => {
  if (!authReady.value) return;
  if (!process.client) return;

  const isAuthenticated = authStore.isAuthenticated;
  const isLoginPage = route.path === "/login";

  if (isAuthenticated && isLoginPage) {
    const redirect = (route.query.redirect as string) || "/";
    router.replace(redirect);
    return;
  }

  if (!isAuthenticated && !isLoginPage) {
    const redirect = route.fullPath;
    const target =
      redirect && redirect !== "/"
        ? `/login?redirect=${encodeURIComponent(redirect)}`
        : "/login";
    router.replace(target);
  }
});

provide("authReady", authReady);
</script>

<template>
  <NuxtLayout>
    <NuxtPage />
  </NuxtLayout>
</template>
