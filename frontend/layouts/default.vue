<script setup lang="ts">
import { ref, computed, inject, type Ref } from "vue";
import AppNavbar from "~/components/layout/AppNavbar.vue";
import AppSidebar from "~/components/layout/AppSidebar.vue";
import AppFooter from "~/components/layout/AppFooter.vue";

const authReady = inject<Ref<boolean>>("authReady", ref(true));
const route = useRoute();
const isLoginPage = computed(() => route.path === "/login");
const showContent = computed(() => isLoginPage.value || authReady.value);

const isSidebarOpen = ref(false);
</script>

<template>
  <div class="min-h-screen bg-zinc-950 text-zinc-100 flex flex-col font-sans selection:bg-zinc-800 selection:text-white">
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
        <div class="h-10 w-10 animate-spin rounded-full border-2 border-white border-t-transparent" />
        <p class="font-mono text-xs uppercase tracking-widest text-zinc-400">Verificando acesso...</p>
      </div>
    </div>
  </div>
</template>
