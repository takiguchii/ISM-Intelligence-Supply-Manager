<script setup lang="ts">
import { ref } from "vue";
import AppSidebar from "~/components/layout/AppSidebar.vue";

definePageMeta({
  layout: false
});

const isSidebarOpen = ref(false);

const toggleSidebar = () => {
  isSidebarOpen.value = !isSidebarOpen.value;
};
</script>

<template>
  <div class="min-h-screen bg-zinc-950 text-zinc-100 flex flex-col font-sans selection:bg-zinc-800 selection:text-white">
    <!-- Navbar -->
    <header class="h-16 border-b border-zinc-800/80 bg-zinc-900/60 backdrop-blur-xl sticky top-0 z-30 px-4 sm:px-6 flex items-center justify-between">
      <!-- Left side: Hamburger Icon & System Name -->
      <div class="flex items-center gap-4">
        <!-- 3 Lines Hamburger Menu Button -->
        <button
          @click="toggleSidebar"
          class="p-2 rounded-xl text-zinc-300 hover:text-white hover:bg-zinc-800/80 transition-all duration-200 focus:outline-none focus:ring-2 focus:ring-zinc-600"
          title="Abrir Menu Lateral"
        >
          <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"></path>
          </svg>
        </button>

        <!-- System Name -->
        <div class="flex items-center gap-3">
          <span class="font-bold text-lg text-white tracking-tight">ISM</span>
          <span class="hidden sm:inline-block text-xs uppercase tracking-widest text-zinc-400 font-mono border-l border-zinc-700/60 pl-3">
            Intelligence Supply Manager
          </span>
        </div>
      </div>

      <!-- Right side: Actions & Account Link -->
      <div class="flex items-center gap-3">
        <NuxtLink
          to="/login"
          class="px-4 py-2 text-xs font-semibold rounded-xl bg-zinc-800 hover:bg-zinc-700 text-zinc-200 hover:text-white border border-zinc-700/60 transition-all duration-200 flex items-center gap-2"
        >
          <svg class="w-4 h-4 text-zinc-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 16l-4-4m0 0l4-4m-4 4h14m-5 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h7a3 3 0 013 3v1"></path>
          </svg>
          <span>Sair / Login</span>
        </NuxtLink>
      </div>
    </header>

    <!-- App Sidebar Component -->
    <AppSidebar :isOpen="isSidebarOpen" @close="isSidebarOpen = false" />

    <!-- Main Content Area -->
    <main class="flex-1 max-w-7xl w-full mx-auto px-4 sm:px-6 py-8">
      <!-- Hero / Welcome Banner -->
      <div class="p-8 rounded-2xl bg-gradient-to-r from-zinc-900 via-zinc-900/90 to-zinc-950 border border-zinc-800 shadow-xl mb-8 relative overflow-hidden">
        <div class="absolute -right-20 -bottom-20 w-80 h-80 bg-zinc-700/10 rounded-full blur-3xl pointer-events-none"></div>
        <div class="relative z-10 max-w-2xl space-y-4">
          <div class="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-zinc-800/80 border border-zinc-700/60 text-xs font-mono text-zinc-300">
            <span class="w-2 h-2 rounded-full bg-zinc-300 animate-pulse"></span>
            Painel Geral
          </div>
          <h1 class="text-3xl sm:text-4xl font-bold text-white tracking-tight">
            Bem-vindo ao ISM
          </h1>
          <p class="text-zinc-400 text-sm sm:text-base leading-relaxed">
            Sua central unificada para gestão inteligente de suprimentos, cardápio, estoque e integrações gastronômicas.
          </p>
          <div class="pt-2 flex flex-wrap gap-3">
            <button
              @click="toggleSidebar"
              class="px-5 py-2.5 bg-white hover:bg-zinc-200 text-zinc-950 font-semibold rounded-xl text-xs sm:text-sm transition-all duration-200 flex items-center gap-2 shadow-md"
            >
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"></path>
              </svg>
              <span>Navegar pelos Módulos</span>
            </button>
          </div>
        </div>
      </div>

      <!-- Quick Summary Cards (Monochrome Palette) -->
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5 mb-8">
        <div class="p-6 rounded-2xl bg-zinc-900/80 border border-zinc-800/80 shadow-lg space-y-2">
          <div class="flex items-center justify-between text-zinc-400">
            <span class="text-xs font-mono uppercase tracking-wider">Status do Sistema</span>
            <svg class="w-5 h-5 text-zinc-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path>
            </svg>
          </div>
          <p class="text-2xl font-bold text-white">Operacional</p>
          <p class="text-xs text-zinc-500">Backend & Banco de Dados sincronizados</p>
        </div>

        <div class="p-6 rounded-2xl bg-zinc-900/80 border border-zinc-800/80 shadow-lg space-y-2">
          <div class="flex items-center justify-between text-zinc-400">
            <span class="text-xs font-mono uppercase tracking-wider">Menu Lateral</span>
            <svg class="w-5 h-5 text-zinc-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"></path>
            </svg>
          </div>
          <p class="text-2xl font-bold text-white">6 Módulos</p>
          <p class="text-xs text-zinc-500">Dashboard, Financeiro, Cardápio, Estoque, Fornecedores, Integrações</p>
        </div>

        <div class="p-6 rounded-2xl bg-zinc-900/80 border border-zinc-800/80 shadow-lg space-y-2">
          <div class="flex items-center justify-between text-zinc-400">
            <span class="text-xs font-mono uppercase tracking-wider">Ambiente</span>
            <svg class="w-5 h-5 text-zinc-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10"></path>
            </svg>
          </div>
          <p class="text-2xl font-bold text-white">Desenvolvimento</p>
          <p class="text-xs text-zinc-500">Docker Containerizado (.NET + Nuxt + MySQL)</p>
        </div>
      </div>
    </main>

    <!-- Footer Básico -->
    <footer class="mt-auto border-t border-zinc-800/80 bg-zinc-950 py-6 text-center text-xs text-zinc-500">
      <div class="max-w-7xl mx-auto px-4 flex flex-col sm:flex-row items-center justify-between gap-3">
        <p>&copy; 2026 ISM — Intelligence Supply Manager. Todos os direitos reservados.</p>
        <div class="flex items-center gap-4 text-zinc-400">
          <NuxtLink to="/home" class="hover:text-white transition-colors">Início</NuxtLink>
          <NuxtLink to="/login" class="hover:text-white transition-colors">Login</NuxtLink>
          <a href="http://localhost:8080/swagger" target="_blank" class="hover:text-white transition-colors">Swagger API</a>
        </div>
      </div>
    </footer>
  </div>
</template>
