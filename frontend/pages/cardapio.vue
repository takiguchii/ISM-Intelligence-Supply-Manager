<script setup lang="ts">
import { computed } from 'vue'
import { useAuthStore } from '~/stores/auth'
import WaiterPanel from '~/components/waiter/WaiterPanel.vue'
import CardapioAtual from '~/components/cardapio/CardapioAtual.vue'
import KitchenMenu from '~/components/kitchen/KitchenMenu.vue'

definePageMeta({ layout: 'default' })

const authStore = useAuthStore()
const isWaiter = computed(
  () => authStore.currentUser?.role === 'Waiter'
)
</script>

<template>
  <WaiterPanel v-if="isWaiter" view="menu" />
  <KitchenMenu v-else-if="authStore.currentUser?.role === 'Chef'" />
  <CardapioAtual v-else />
</template>
