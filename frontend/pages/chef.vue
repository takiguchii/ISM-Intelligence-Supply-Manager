<script setup lang="ts">
import { useAuthStore } from "~/stores/auth";

const authStore = useAuthStore();
const router = useRouter();

onMounted(() => {
  authStore.initFromStorage();
  if (authStore.currentUser?.role !== "Chef") {
    router.replace("/");
  }
});
</script>

<template>
  <main class="min-h-[calc(100vh-8rem)] flex items-center justify-center px-6">
    <p class="text-center text-xl font-semibold text-zinc-700 dark:text-zinc-200">
      Perfil em contrução...
    </p>
  </main>
</template>
