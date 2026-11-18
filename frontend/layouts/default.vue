<script setup lang="ts">
const authReady = inject<Ref<boolean>>("authReady", ref(true));
const route = useRoute();
const isLoginPage = computed(() => route.path === "/login");
const showContent = computed(() => isLoginPage.value || authReady.value);

const themeStore = useThemeStore();
const authStore = useAuthStore();
const canShowAssistant = computed(() => !isLoginPage.value && authReady.value && ["Admin", "Manager"].includes(authStore.userRole || "") && Boolean(authStore.currentUser?.restaurantId));
const isSidebarOpen = ref(false);
const toggleSidebar = () => {
  isSidebarOpen.value = !isSidebarOpen.value;
};
const closeSidebarOnOutsideClick = (event: MouseEvent) => {
  const target = event.target;
  if (target instanceof Element && target.closest('[data-sidebar-toggle]')) return;
  isSidebarOpen.value = false;
};
provide("toggleSidebar", toggleSidebar);
</script>

<template>
  <div
    :class="[
      'min-h-screen flex flex-col font-sans transition-colors duration-200',
      themeStore.isDark
        ? 'bg-zinc-950 text-white selection:bg-zinc-800 selection:text-white'
        : 'bg-white text-[#2B2B2B] selection:bg-[#D4D4D4] selection:text-[#2B2B2B]'
    ]"
  >
    <template v-if="showContent">
      <template v-if="isLoginPage">
        <slot />
      </template>

      <template v-else>
        <Transition name="mobile-nav-backdrop">
          <button v-if="isSidebarOpen" type="button" class="fixed inset-0 z-40 bg-zinc-950/40 backdrop-blur-[1px] lg:hidden" aria-label="Fechar menu de navegação" @click="isSidebarOpen = false" />
        </Transition>
        <ClientOnly>
          <AppSidebar :isOpen="isSidebarOpen" @close="isSidebarOpen = false" />
        </ClientOnly>
        <div
          @click.capture="closeSidebarOnOutsideClick"
          :class="[
            'flex-1 flex flex-col min-w-0 transition-[padding] duration-[400ms] ease-[cubic-bezier(0.16,1,0.3,1)] motion-reduce:transition-none',
            isSidebarOpen ? 'pl-0 lg:pl-[256px]' : 'pl-0'
          ]"
        >
          <AppNavbar @toggleSidebar="toggleSidebar" />
          <main v-card-reveal class="flex-1 min-w-0">
            <slot />
          </main>
          <AppFooter />
          <ClientOnly>
            <AssistantMascot v-if="canShowAssistant" />
          </ClientOnly>
        </div>
      </template>
    </template>

    <div v-else class="flex min-h-screen items-center justify-center">
      <div class="flex flex-col items-center gap-4">
        <div
          :class="[
            'h-10 w-10 animate-spin rounded-full border-2 border-t-transparent',
            themeStore.isDark ? 'border-indigo-400' : 'border-indigo-600'
          ]"
        />
        <p
          :class="[
            'font-mono text-xs uppercase tracking-widest',
            themeStore.isDark ? 'text-zinc-400/60' : 'text-zinc-500/80'
          ]"
        >Verificando acesso...</p>
      </div>
    </div>
  </div>
</template>

<style scoped>
.mobile-nav-backdrop-enter-active, .mobile-nav-backdrop-leave-active { transition: opacity 180ms ease; }
.mobile-nav-backdrop-enter-from, .mobile-nav-backdrop-leave-to { opacity: 0; }
@media (prefers-reduced-motion: reduce) { .mobile-nav-backdrop-enter-active, .mobile-nav-backdrop-leave-active { transition-duration: 1ms; } }
</style>
