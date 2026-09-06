<script setup lang="ts">
import { useAuthStore } from "~/stores/auth";

const emit = defineEmits<{
  (e: "toggleSidebar"): void;
}>();

const runtimeConfig = useRuntimeConfig();
const authStore = useAuthStore();
const router = useRouter();

const handleLogout = () => {
  authStore.logout();
  router.push("/login");
};
</script>

<template>
  <header class="h-16 border-b border-zinc-800/80 bg-zinc-900/60 backdrop-blur-xl sticky top-0 z-30 px-4 sm:px-6 flex items-center justify-between">
    <div class="flex items-center gap-4">
      <button
        @click="emit('toggleSidebar')"
        type="button"
        class="p-2 rounded-xl text-zinc-300 hover:text-white hover:bg-zinc-800/80 transition-all duration-200 focus:outline-none focus:ring-2 focus:ring-zinc-600"
        title="Abrir Menu Lateral"
        aria-label="Abrir Menu Lateral"
      >
        <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"></path>
        </svg>
      </button>

      <NuxtLink to="/" class="flex items-center gap-3">
        <span class="font-bold text-lg text-white tracking-tight">ISM</span>
        <span class="hidden sm:inline-block text-xs uppercase tracking-widest text-zinc-400 font-mono border-l border-zinc-700/60 pl-3">
          {{ runtimeConfig.public.appName }}
        </span>
      </NuxtLink>
    </div>

    <div class="flex items-center gap-3">
      <div v-if="authStore.isAuthenticated" class="flex items-center gap-3">
        <span class="hidden md:inline-block text-xs text-zinc-400 font-medium">
          {{ authStore.currentUser?.name }} ({{ authStore.currentUser?.role }})
        </span>
        <button
          @click="handleLogout"
          type="button"
          class="px-4 py-2 text-xs font-semibold rounded-xl bg-zinc-800 hover:bg-zinc-700 text-zinc-200 hover:text-white border border-zinc-700/60 transition-all duration-200 flex items-center gap-2"
        >
          <svg class="w-4 h-4 text-zinc-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"></path>
          </svg>
          <span>Sair</span>
        </button>
      </div>
      <NuxtLink
        v-else
        to="/login"
        class="px-4 py-2 text-xs font-semibold rounded-xl bg-zinc-800 hover:bg-zinc-700 text-zinc-200 hover:text-white border border-zinc-700/60 transition-all duration-200 flex items-center gap-2"
      >
        <svg class="w-4 h-4 text-zinc-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 16l-4-4m0 0l4-4m-4 4h14m-5 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h7a3 3 0 013 3v1"></path>
        </svg>
        <span>Login</span>
      </NuxtLink>
    </div>
  </header>
</template>
