<script setup lang="ts">
defineProps<{ icon: string }>();
const brandAssets: Record<string, string> = {
  ifood: '/integrations/ifood.svg',
  '99food': '/integrations/99food.png',
  telegram: '/integrations/telegram.svg',
  stone: '/integrations/stone.png'
};
</script>
<template>
  <img v-if="brandAssets[icon]" :src="brandAssets[icon]" alt="" aria-hidden="true" :class="icon === 'stone' ? 'h-7 w-9' : 'h-8 w-8'" class="object-contain" />
  <span v-else class="flex h-8 w-8 items-center justify-center rounded-lg text-white" :class="icon === 'menu' ? 'bg-blue-600' : icon === 'supplier' ? 'bg-violet-600' : 'bg-emerald-600'" aria-hidden="true">
                  <!-- Cardápio Digital / Menu -->
                  <svg aria-hidden="true" focusable="false" v-if="icon === 'menu'" class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253"></path>
                  </svg>

                  <!-- Fornecedores / Truck -->
                  <svg aria-hidden="true" focusable="false" v-else-if="icon === 'supplier'" class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M9 17a2 2 0 11-4 0 2 2 0 014 0zM19 17a2 2 0 11-4 0 2 2 0 014 0z"></path>
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M13 16V6a1 1 0 00-1-1H4a1 1 0 00-1 1v10a1 1 0 001 1h1m8-1a1 1 0 01-1 1H9m4-1V8h4l3 3v5h-2m-6 0h2"></path>
                  </svg>

                  <!-- Planilhas / Sheet -->
                  <svg aria-hidden="true" focusable="false" v-else class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M4 4h16v16H4zM4 9h16M9 9v11M14 9v11M4 14h16"></path>
                  </svg>

  </span>
</template>
