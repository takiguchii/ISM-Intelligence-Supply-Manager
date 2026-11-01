<script setup lang="ts">
import { computed } from 'vue'
import { useAuthStore } from '~/stores/auth'
import WaiterMock from '~/components/waiter/WaiterMock.vue'
import OrderBoard from '~/components/orders/OrderBoard.vue'

definePageMeta({ layout: 'default' })

const authStore = useAuthStore()
const isWaiter = computed(
  () => authStore.currentUser?.role === 'Waiter'
)
</script>

<template>
  <WaiterMock v-if="isWaiter" view="orders" />
  <OrderBoard v-else />
</template>