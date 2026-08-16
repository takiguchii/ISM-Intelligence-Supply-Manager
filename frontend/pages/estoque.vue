<script setup lang="ts">
import { ref, onMounted } from "vue";
import AppSidebar from "~/components/layout/AppSidebar.vue";
import AppLoader from "~/components/base/AppLoader.vue";
import { useAuthStore } from "~/stores/auth";

definePageMeta({ layout: false });

const runtimeConfig = useRuntimeConfig();
const authStore = useAuthStore();
const router = useRouter();

const isLoading = ref(true);
const isSidebarOpen = ref(false);
const toggleSidebar = () => (isSidebarOpen.value = !isSidebarOpen.value);

const handleLogout = () => {
  authStore.logout();
  router.push("/login");
};

onMounted(async () => {
  const start = Date.now();
  authStore.initFromStorage();
  if (!authStore.isAuthenticated) {
    router.push("/login");
    return;
  }
  const wait = Math.max(0, 1200 - (Date.now() - start));
  setTimeout(() => {
    isLoading.value = false;
  }, wait);
});
</script>

<template>
  <div class="min-h-screen bg-zinc-950 text-zinc-100 flex flex-col font-sans selection:bg-zinc-800 selection:text-white">
    <AppLoader :visible="isLoading" />
    <header class="h-16 border-b border-zinc-800/80 bg-zinc-900/60 backdrop-blur-xl sticky top-0 z-30 px-4 sm:px-6 flex items-center justify-between">
      <div class="flex items-center gap-4">
        <button @click="toggleSidebar" class="p-2 rounded-xl text-zinc-300 hover:text-white hover:bg-zinc-800/80 transition-all duration-200">
          <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"></path></svg>
        </button>
        <div class="flex items-center gap-3">
          <span class="font-bold text-lg text-white tracking-tight">ISM</span>
          <span class="hidden sm:inline-block text-xs uppercase tracking-widest text-zinc-400 font-mono border-l border-zinc-700/60 pl-3">{{ runtimeConfig.public.appName }}</span>
        </div>
      </div>
      <div class="flex items-center gap-3">
        <span class="hidden md:inline-block text-xs text-zinc-400 font-medium">{{ authStore.currentUser?.name }} ({{ authStore.currentUser?.role }})</span>
        <button @click="handleLogout" class="px-4 py-2 text-xs font-semibold rounded-xl bg-zinc-800 hover:bg-zinc-700 text-zinc-200 hover:text-white border border-zinc-700/60 transition-all duration-200 flex items-center gap-2">
          <svg class="w-4 h-4 text-zinc-400" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"></path></svg>
          <span>Sair</span>
        </button>
      </div>
    </header>
    <AppSidebar :isOpen="isSidebarOpen" @close="isSidebarOpen = false" />
    <main class="flex-1 max-w-7xl w-full mx-auto px-4 sm:px-6 py-8">
      <div class="p-8 rounded-2xl bg-gradient-to-r from-zinc-900 via-zinc-900/90 to-zinc-950 border border-zinc-800 shadow-xl mb-8 relative overflow-hidden">
        <div class="absolute -right-20 -bottom-20 w-80 h-80 bg-zinc-700/10 rounded-full blur-3xl pointer-events-none"></div>
        <div class="relative z-10 max-w-2xl space-y-4">
          <h1 class="text-3xl sm:text-4xl font-bold text-white tracking-tight">Estoque</h1>
          <p class="text-zinc-400 text-sm sm:text-base leading-relaxed">Controle inteligente de produtos, quantidades e alertas de estoque baixo.</p>
          <p class="text-xs px-3 py-1.5 rounded-full inline-block bg-zinc-800/80 border border-zinc-700/60 text-zinc-300 font-mono">Em desenvolvimento. Enquanto isso, use a tela de <NuxtLink to="/integracoes" class="text-emerald-300 hover:text-emerald-200 underline">Integrações</NuxtLink> para importar estoque via CSV.</p>
        </div>
      </div>
    </main>
    <footer class="mt-auto border-t border-zinc-800/80 bg-zinc-950 py-6 text-center text-xs text-zinc-500">
      <div class="max-w-7xl mx-auto px-4 flex flex-col sm:flex-row items-center justify-between gap-3">
        <p>&copy; 2026 ISM — Intelligence Supply Manager. Todos os direitos reservados.</p>
      </div>
    </footer>
  </div>
</template>
