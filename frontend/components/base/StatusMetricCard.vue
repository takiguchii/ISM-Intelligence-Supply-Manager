<script setup lang="ts">
defineProps<{
  label: string;
  value: string | number;
  accent?: "aqua" | "ember";
}>();
</script>

<template>
  <div
    class="rounded-3xl border border-white/10 bg-white/5 p-5 backdrop-blur"
    :class="accent === 'ember' ? 'shadow-ember' : 'shadow-glow'"
  >
    <p class="font-mono text-xs uppercase tracking-[0.35em] text-mist/80">
      {{ label }}
    </p>
    <p class="mt-3 font-display text-3xl font-semibold text-white">
      {{ value }}
    </p>
  </div>
</template>
