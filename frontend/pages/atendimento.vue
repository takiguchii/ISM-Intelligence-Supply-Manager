<script setup lang="ts">
import { computed } from 'vue'
import { useAuthStore } from '~/stores/auth'
import WaiterPanel from '~/components/waiter/WaiterPanel.vue'
import AtendimentoAtual from '~/components/atendimento/AtendimentoAtual.vue'

definePageMeta({ layout: 'default' })

const authStore = useAuthStore()
const isWaiter = computed(
  () => authStore.currentUser?.role === 'Waiter'
)
</script>

<template>
  <WaiterPanel v-if="isWaiter" view="service" />
  <AtendimentoAtual v-else />
</template>