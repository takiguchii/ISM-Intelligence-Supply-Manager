<script setup lang="ts">
const authReady = inject<Ref<boolean>>("authReady", ref(true));
const route = useRoute();
const isLoginPage = computed(() => route.path === "/login");
const showContent = computed(() => isLoginPage.value || authReady.value);
</script>

<template>
  <div class="min-h-screen bg-abyss text-white">
    <slot v-if="showContent" />
    <div v-else class="flex min-h-screen items-center justify-center">
      <div class="flex flex-col items-center gap-4">
        <div class="h-10 w-10 animate-spin rounded-full border-2 border-aqua border-t-transparent" />
        <p class="font-mono text-xs uppercase tracking-widest text-mist/60">Verificando acesso...</p>
      </div>
    </div>
  </div>
</template>
