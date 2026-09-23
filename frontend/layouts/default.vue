<script setup lang="ts">
const authReady = inject<Ref<boolean>>("authReady", ref(true));
const route = useRoute();
const isLoginPage = computed(() => route.path === "/login");
const showContent = computed(() => isLoginPage.value || authReady.value);

const isSidebarOpen = ref(false);
</script>

<template>
  <div
    class="min-h-screen flex flex-col font-sans transition-colors duration-200 bg-zinc-50 text-zinc-900 selection:bg-indigo-100 selection:text-indigo-950 dark:bg-zinc-950 dark:text-white dark:selection:bg-zinc-800 dark:selection:text-white"
  >
    <template v-if="showContent">
      <template v-if="isLoginPage">
        <slot />
      </template>

      <template v-else>
        <AppNavbar @toggleSidebar="isSidebarOpen = !isSidebarOpen" />
        <AppSidebar :isOpen="isSidebarOpen" @close="isSidebarOpen = false" />
        <div class="flex-1">
          <slot />
        </div>
        <AppFooter />
      </template>
    </template>

    <div v-else class="flex min-h-screen items-center justify-center">
      <div class="flex flex-col items-center gap-4">
        <div
          class="h-10 w-10 animate-spin rounded-full border-2 border-indigo-600 border-t-transparent dark:border-indigo-400 dark:border-t-transparent"
        />
        <p class="font-mono text-xs uppercase tracking-widest text-zinc-500/80 dark:text-zinc-400/60">Verificando acesso...</p>
      </div>
    </div>
  </div>
</template>
