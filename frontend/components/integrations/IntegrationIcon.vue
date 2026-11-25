<script setup lang="ts">
defineProps<{ icon: string }>();
</script>
<template>
                  <svg aria-hidden="true" focusable="false" v-if="icon === 'ifood'" class="w-5 h-5 text-red-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6"></path>
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M3 3h18v18H3z"></path>
                  </svg>

                  <!-- 99 Food / Delivery -->
                  <svg aria-hidden="true" focusable="false" v-else-if="icon === '99food'" class="w-5 h-5 text-amber-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M13 10V3L4 14h7v7l9-11h-7z"></path>
                  </svg>

                  <!-- Cardápio Digital / Menu -->
                  <svg aria-hidden="true" focusable="false" v-else-if="icon === 'menu'" class="w-5 h-5 text-blue-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253"></path>
                  </svg>

                  <!-- Stone / Card -->
                  <svg aria-hidden="true" focusable="false" v-else-if="icon === 'stone'" class="w-5 h-5 text-emerald-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z"></path>
                  </svg>

                  <!-- Fornecedores / Truck -->
                  <svg aria-hidden="true" focusable="false" v-else-if="icon === 'supplier'" class="w-5 h-5 text-purple-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M9 17a2 2 0 11-4 0 2 2 0 014 0zM19 17a2 2 0 11-4 0 2 2 0 014 0z"></path>
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M13 16V6a1 1 0 00-1-1H4a1 1 0 00-1 1v10a1 1 0 001 1h1m8-1a1 1 0 01-1 1H9m4-1V8h4l3 3v5h-2m-6 0h2"></path>
                  </svg>

                  <!-- Planilhas / Sheet -->
                  <svg aria-hidden="true" focusable="false" v-else class="w-5 h-5 text-zinc-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M9 17v-6a2 2 0 012-2h2m0 0h2a2 2 0 012 2v6m-6 0h6M3 3h18v18H3V3z"></path>
                  </svg>

</template>
