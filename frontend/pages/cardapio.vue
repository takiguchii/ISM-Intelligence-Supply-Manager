<script setup lang="ts">
import { computed } from 'vue'
import { useAuthStore } from '~/stores/auth'
import WaiterMock from '~/components/waiter/WaiterMock.vue'
import CardapioAtual from '~/components/cardapio/CardapioAtual.vue'

definePageMeta({ layout: 'default' })

const authStore = useAuthStore()
const isWaiter = computed(
  () => authStore.currentUser?.role === 'Waiter'
)
</script>

<template>
  <WaiterMock v-if="isWaiter" view="menu" />
  <CardapioAtual v-else />
</template>