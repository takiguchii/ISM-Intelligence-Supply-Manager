<script setup lang="ts">
definePageMeta({
  middleware: [
    function () {
      return navigateTo("/", { replace: true });
    }
  ]
});
</script>

<template>
  <div />
</template>
