<script setup lang="ts">
const authReady = inject<Ref<boolean>>("authReady", ref(true));
const route = useRoute();
const isLoginPage = computed(() => route.path === "/login");
const showContent = computed(() => isLoginPage.value || authReady.value);
const themeStore = useThemeStore();
</script>

<template>
  <div
    :class="[
      'min-h-screen transition-colors duration-200',
      themeStore.isDark
        ? 'bg-abyss text-white'
        : 'bg-zinc-50 text-zinc-900'
    ]"
  >
    <slot v-if="showContent" />
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
