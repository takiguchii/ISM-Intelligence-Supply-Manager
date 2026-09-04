<script setup lang="ts">
import { ref, computed, onMounted } from "vue";
import AppSidebar from "~/components/layout/AppSidebar.vue";
import AppLoader from "~/components/base/AppLoader.vue";
import StatusOverviewCard from "~/components/layout/StatusOverviewCard.vue";
import { useAuthStore } from "~/stores/auth";
import { useThemeStore } from "~/stores/theme";

definePageMeta({
  layout: false
});

const runtimeConfig = useRuntimeConfig();
const authStore = useAuthStore();
const themeStore = useThemeStore();
const router = useRouter();

const isLoading = ref(true);

const { data, pending, error, refresh } = await useSystemStatus();

const isSidebarOpen = ref(false);

const toggleSidebar = () => {
  isSidebarOpen.value = !isSidebarOpen.value;
};

const status = computed(() => data.value);

const handleLogout = () => {
  authStore.logout();
  router.push("/login");
};

onMounted(async () => {
  const startTime = Date.now();

  // Verifica autenticação no cliente
  authStore.initFromStorage();
  if (!authStore.isAuthenticated) {
    router.push("/login");
    return;
  }

  // Garante que a animação rode por no MÍNIMO 2000ms (2 segundos)
  // ou mais caso os dados da página ainda estejam sendo carregados
  const elapsedTime = Date.now() - startTime;
  const minDuration = 2000;
  const remainingTime = Math.max(0, minDuration - elapsedTime);

  setTimeout(() => {
    isLoading.value = false;
  }, remainingTime);
});
</script>

<template>
  <div :class="['min-h-screen flex flex-col font-sans transition-colors duration-200', themeStore.isDark ? 'bg-zinc-950 text-zinc-100 selection:bg-zinc-800 selection:text-white' : 'bg-zinc-50 text-zinc-900 selection:bg-indigo-100 selection:text-indigo-900']">
    <!-- Overlay de Animação de Carregamento -->
    <AppLoader :visible="isLoading" />

    <!-- Navbar Header -->
    <header :class="['h-16 border-b backdrop-blur-xl sticky top-0 z-30 px-4 sm:px-6 flex items-center justify-between', themeStore.isDark ? 'border-zinc-800/80 bg-zinc-900/60' : 'border-zinc-200 bg-white/80']">
      <!-- Left side: Hamburger Icon & System Name -->
      <div class="flex items-center gap-4">
        <!-- 3 Lines Hamburger Menu Button -->
        <button
          @click="toggleSidebar"
          :class="['p-2 rounded-xl transition-all duration-200 focus:outline-none focus:ring-2', themeStore.isDark ? 'text-zinc-300 hover:text-white hover:bg-zinc-800/80 focus:ring-zinc-600' : 'text-zinc-600 hover:text-zinc-900 hover:bg-zinc-100 focus:ring-indigo-500/30']"
          title="Abrir Menu Lateral"
        >
          <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"></path>
          </svg>
        </button>

        <!-- System Name -->
        <div class="flex items-center gap-3">
          <span :class="['font-bold text-lg tracking-tight', themeStore.isDark ? 'text-white' : 'text-zinc-900']">ISM</span>
          <span :class="['hidden sm:inline-block text-xs uppercase tracking-widest font-mono border-l pl-3', themeStore.isDark ? 'text-zinc-400 border-zinc-700/60' : 'text-zinc-500 border-zinc-200']">
            {{ runtimeConfig.public.appName }}
          </span>
        </div>
      </div>

      <!-- Right side: Actions & User Info / Logout -->
      <div class="flex items-center gap-3">
        <div v-if="authStore.isAuthenticated" class="flex items-center gap-3">
          <span :class="['hidden md:inline-block text-xs font-medium', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-600']">
            {{ authStore.currentUser?.name }} ({{ authStore.currentUser?.role }})
          </span>
          <button
            @click="handleLogout"
            :class="['px-4 py-2 text-xs font-semibold rounded-xl border transition-all duration-200 flex items-center gap-2', themeStore.isDark ? 'bg-zinc-800 hover:bg-zinc-700 text-zinc-200 hover:text-white border-zinc-700/60' : 'bg-zinc-100 hover:bg-zinc-200 text-zinc-700 hover:text-zinc-900 border-zinc-200']"
          >
            <svg :class="['w-4 h-4', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500']" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"></path>
            </svg>
            <span>Sair</span>
          </button>
        </div>
        <NuxtLink
          v-else
          to="/login"
          :class="['px-4 py-2 text-xs font-semibold rounded-xl border transition-all duration-200 flex items-center gap-2', themeStore.isDark ? 'bg-zinc-800 hover:bg-zinc-700 text-zinc-200 hover:text-white border-zinc-700/60' : 'bg-zinc-100 hover:bg-zinc-200 text-zinc-700 hover:text-zinc-900 border-zinc-200']"
        >
          <svg :class="['w-4 h-4', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500']" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 16l-4-4m0 0l4-4m-4 4h14m-5 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h7a3 3 0 013 3v1"></path>
          </svg>
          <span>Login</span>
        </NuxtLink>
      </div>
    </header>

    <!-- App Sidebar Component -->
    <AppSidebar :isOpen="isSidebarOpen" @close="isSidebarOpen = false" />

    <!-- Main Content Area -->
    <main class="flex-1 max-w-7xl w-full mx-auto px-4 sm:px-6 py-8">
      <!-- Hero / Welcome Banner -->
      <div :class="['p-8 rounded-2xl border shadow-xl mb-8 relative overflow-hidden', themeStore.isDark ? 'bg-gradient-to-r from-zinc-900 via-zinc-900/90 to-zinc-950 border-zinc-800' : 'bg-gradient-to-r from-white via-white/90 to-zinc-50 border-zinc-200']">
        <div class="absolute -right-20 -bottom-20 w-80 h-80 bg-zinc-700/10 rounded-full blur-3xl pointer-events-none"></div>
        <div class="relative z-10 max-w-2xl space-y-4">
          <div :class="['inline-flex items-center gap-2 px-3 py-1 rounded-full border text-xs font-mono', themeStore.isDark ? 'bg-zinc-800/80 border-zinc-700/60 text-zinc-300' : 'bg-zinc-100 border-zinc-200 text-zinc-700']">
            <span class="w-2 h-2 rounded-full bg-emerald-400 animate-pulse"></span>
            Painel Geral
          </div>
          <h1 :class="['text-3xl sm:text-4xl font-bold tracking-tight', themeStore.isDark ? 'text-white' : 'text-zinc-900']">
            Bem-vindo ao ISM, {{ authStore.currentUser?.name || "Usuário" }}
          </h1>
          <p :class="['text-sm sm:text-base leading-relaxed', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-600']">
            Sua central unificada para gestão inteligente de suprimentos, cardápio, estoque e integrações gastronômicas.
          </p>
          <div class="pt-2 flex flex-wrap gap-3">
            <button
              @click="toggleSidebar"
              :class="['px-5 py-2.5 font-semibold rounded-xl text-xs sm:text-sm transition-all duration-200 flex items-center gap-2 shadow-md', themeStore.isDark ? 'bg-white hover:bg-zinc-200 text-zinc-950' : 'bg-indigo-600 hover:bg-indigo-700 text-white']"
            >
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"></path>
              </svg>
              <span>Navegar pelos Módulos</span>
            </button>
            <button
              @click="refresh()"
              :class="['px-5 py-2.5 font-medium rounded-xl text-xs sm:text-sm border transition-all duration-200 flex items-center gap-2', themeStore.isDark ? 'bg-zinc-800 hover:bg-zinc-700 text-white border-zinc-700/60' : 'bg-zinc-100 hover:bg-zinc-200 text-zinc-700 border-zinc-200']"
            >
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"></path>
              </svg>
              <span>Atualizar Status</span>
            </button>
          </div>
        </div>
      </div>

      <!-- Overview Cards Grid -->
      <div class="grid grid-cols-1 lg:grid-cols-[1.1fr_0.9fr] gap-8 mb-8">
        <!-- Summary Info Grid -->
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-5">
          <div :class="['p-6 rounded-2xl border shadow-lg space-y-2 transition-colors duration-200', themeStore.isDark ? 'bg-zinc-900/80 border-zinc-800/80' : 'bg-white border-zinc-200']">
            <div :class="['flex items-center justify-between', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-600']">
              <span class="text-xs font-mono uppercase tracking-wider">Status do Sistema</span>
              <svg class="w-5 h-5 text-emerald-500 dark:text-emerald-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path>
              </svg>
            </div>
            <p :class="['text-2xl font-bold', themeStore.isDark ? 'text-white' : 'text-zinc-900']">Operacional</p>
            <p :class="['text-xs', themeStore.isDark ? 'text-zinc-500' : 'text-zinc-500']">Backend & Banco de Dados sincronizados</p>
          </div>

          <div :class="['p-6 rounded-2xl border shadow-lg space-y-2 transition-colors duration-200', themeStore.isDark ? 'bg-zinc-900/80 border-zinc-800/80' : 'bg-white border-zinc-200']">
            <div :class="['flex items-center justify-between', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-600']">
              <span class="text-xs font-mono uppercase tracking-wider">Menu Lateral</span>
              <svg :class="['w-5 h-5', themeStore.isDark ? 'text-zinc-300' : 'text-zinc-500']" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"></path>
              </svg>
            </div>
            <p :class="['text-2xl font-bold', themeStore.isDark ? 'text-white' : 'text-zinc-900']">6 Módulos</p>
            <p class="text-xs text-zinc-500">Dashboard, Financeiro, Cardápio, Estoque, Fornecedores, Integrações</p>
          </div>

          <div :class="['p-6 rounded-2xl border shadow-lg space-y-2 sm:col-span-2 transition-colors duration-200', themeStore.isDark ? 'bg-zinc-900/80 border-zinc-800/80' : 'bg-white border-zinc-200']">
            <div :class="['flex items-center justify-between', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-600']">
              <span class="text-xs font-mono uppercase tracking-wider">Perfil da Conta</span>
              <span class="text-xs font-mono text-emerald-500 dark:text-emerald-400 uppercase tracking-wider">{{ authStore.currentUser?.role }}</span>
            </div>
            <p :class="['text-lg font-semibold', themeStore.isDark ? 'text-white' : 'text-zinc-900']">{{ authStore.currentUser?.name }}</p>
            <p :class="['text-xs', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-600']">{{ authStore.currentUser?.email }}</p>
          </div>
        </div>

        <!-- System Status Card -->
        <StatusOverviewCard :status="status" :pending="pending" />
      </div>
    </main>

    <!-- Footer -->
    <footer :class="['mt-auto border-t py-6 text-center text-xs transition-colors duration-200', themeStore.isDark ? 'border-zinc-800/80 bg-zinc-950 text-zinc-500' : 'border-zinc-200 bg-white text-zinc-600']">
      <div class="max-w-7xl mx-auto px-4 flex flex-col sm:flex-row items-center justify-between gap-3">
        <p>&copy; 2026 ISM — Intelligence Supply Manager. Todos os direitos reservados.</p>
        <div :class="['flex items-center gap-4', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500']">
          <NuxtLink to="/" :class="['transition-colors', themeStore.isDark ? 'hover:text-white' : 'hover:text-zinc-900']">Início</NuxtLink>
          <NuxtLink to="/login" :class="['transition-colors', themeStore.isDark ? 'hover:text-white' : 'hover:text-zinc-900']">Login</NuxtLink>
          <a href="http://localhost:8080/swagger" target="_blank" :class="['transition-colors', themeStore.isDark ? 'hover:text-white' : 'hover:text-zinc-900']">Swagger API</a>
        </div>
      </div>
    </footer>
  </div>
</template>
