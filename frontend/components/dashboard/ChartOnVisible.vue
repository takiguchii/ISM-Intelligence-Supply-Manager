<script setup lang="ts">
const container = ref<HTMLElement | null>(null);
const visible = ref(false);
let observer: IntersectionObserver | undefined;

onMounted(() => {
  if (!container.value) return;
  observer = new IntersectionObserver((entries) => {
    if (entries.some((entry) => entry.isIntersecting)) {
      visible.value = true;
      observer?.disconnect();
    }
  }, { threshold: 0.05 });
  observer.observe(container.value);
});

onBeforeUnmount(() => observer?.disconnect());
</script>

<template>
  <div ref="container" class="h-64">
    <slot v-if="visible" />
  </div>
</template>
