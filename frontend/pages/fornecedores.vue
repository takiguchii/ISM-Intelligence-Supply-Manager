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
const handleLogout = () => { authStore.logout(); router.push("/login"); };

onMounted(async () => {
  const start = Date.now();
  authStore.initFromStorage();
  if (!authStore.isAuthenticated) { router.push("/login"); return; }
  setTimeout(() => { isLoading.value = false; }, Math.max(0, 1200 - (Date.now() - start)));
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
        <span class="font-bold text-lg text-white tracking-tight">ISM</span>
      </div>
      <button @click="handleLogout" class="px-4 py-2 text-xs font-semibold rounded-xl bg-zinc-800 hover:bg-zinc-700 text-zinc-200 hover:text-white border border-zinc-700/60 transition-all duration-200">Sair</button>
    </header>
    <AppSidebar :isOpen="isSidebarOpen" @close="isSidebarOpen = false" />
    <main class="flex-1 max-w-7xl w-full mx-auto px-4 sm:px-6 py-8">
      <div class="p-8 rounded-2xl bg-gradient-to-r from-zinc-900 via-zinc-900/90 to-zinc-950 border border-zinc-800 shadow-xl mb-8">
        <h1 class="text-3xl sm:text-4xl font-bold text-white tracking-tight mb-3">Fornecedores</h1>
        <p class="text-zinc-400 text-sm sm:text-base mb-3">Gerencie seus fornecedores, categorias e formas de contato.</p>
        <p class="text-xs px-3 py-1.5 rounded-full inline-block bg-zinc-800/80 border border-zinc-700/60 text-zinc-300 font-mono">Em desenvolvimento. Use <NuxtLink to="/integracoes" class="text-indigo-300 hover:text-indigo-200 underline">Integrações</NuxtLink> para importar fornecedores via CSV.</p>
      </div>
    </main>
  </div>
</template>
