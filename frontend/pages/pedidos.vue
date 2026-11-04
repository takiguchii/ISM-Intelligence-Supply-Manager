<script setup lang="ts">
import { computed } from 'vue'
import { useAuthStore } from '~/stores/auth'
import WaiterMock from '~/components/waiter/WaiterMock.vue'
import OrderBoard from '~/components/orders/OrderBoard.vue'
import AdminOrderBoard from '~/components/orders/AdminOrderBoard.vue'

definePageMeta({ layout: 'default' })

const authStore = useAuthStore()
const isWaiter = computed(
  () => authStore.currentUser?.role === 'Waiter'
)
const isManager = computed(() => ['Admin', 'Manager'].includes(authStore.currentUser?.role ?? ''))
</script>

<template>
  <WaiterMock v-if="isWaiter" view="orders" />
  <AdminOrderBoard v-else-if="isManager" />
  <OrderBoard v-else />
</template>
