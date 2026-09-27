<script setup lang="ts">
import { useThemeStore } from "~/stores/theme";

const themeStore = useThemeStore();
</script>

<template>
  <footer :class="['mt-auto border-t py-6 text-center text-xs', themeStore.isDark ? 'border-zinc-800/80 bg-zinc-950 text-zinc-500' : 'border-[#D4D4D4] bg-white text-[#2B2B2B]']">
    <div class="max-w-7xl mx-auto px-4 flex items-center justify-center">
      <p>&copy; 2026 ISM — Intelligence Supply Manager. Todos os direitos reservados.</p>
    </div>
  </footer>
</template>
