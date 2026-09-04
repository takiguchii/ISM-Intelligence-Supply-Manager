<script setup lang="ts">
import { useThemeStore } from "~/stores/theme";

defineProps<{
  label: string;
  value: string | number;
  accent?: "aqua" | "ember";
}>();

const themeStore = useThemeStore();
</script>

<template>
  <div
    :class="[
      'rounded-3xl border p-5 backdrop-blur',
      themeStore.isDark
        ? 'border-white/10 bg-white/5'
        : 'border-zinc-200 bg-white shadow-sm',
      accent === 'ember'
        ? (themeStore.isDark ? 'shadow-ember' : 'shadow-orange-100')
        : (themeStore.isDark ? 'shadow-glow' : 'shadow-indigo-100')
    ]"
  >
    <p
      :class="[
        'font-mono text-xs uppercase tracking-[0.35em]',
        themeStore.isDark ? 'text-mist/80' : 'text-zinc-500'
      ]"
    >
      {{ label }}
    </p>
    <p
      :class="[
        'mt-3 font-display text-3xl font-semibold',
        themeStore.isDark ? 'text-white' : 'text-zinc-900'
      ]"
    >
      {{ value }}
    </p>
  </div>
</template>
