<script setup lang="ts">
import { OrderStatus, type Order } from '~/services/modules/orders/orderService'

defineProps<{ order: Order; busy: boolean; updating: boolean }>()
defineEmits<{ advance: [id: number] }>()
const date = (value: string) => new Date(value.endsWith('Z') ? value : `${value}Z`).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })
</script>

<template>
  <article class="min-w-0 rounded-xl border border-zinc-200 bg-white p-4 shadow-sm dark:border-zinc-700 dark:bg-zinc-900">
    <div class="flex flex-wrap items-start justify-between gap-2">
      <div class="min-w-0">
        <h3 class="break-words font-semibold">Mesa / comanda {{ order.tableReference }}</h3>
        <p class="mt-1 text-xs text-zinc-500">Pedido #{{ order.id }} · Recebido às {{ date(order.createdAtUtc) }}</p>
      </div>
      <span class="rounded-full bg-zinc-100 px-2 py-1 text-xs dark:bg-zinc-800">{{ order.items.reduce((sum, item) => sum + item.quantity, 0) }} itens</span>
    </div>
    <ul class="my-4 space-y-3 text-sm">
      <li v-for="(item, index) in order.items" :key="index" class="break-words">
        <strong>{{ item.quantity }}× {{ item.dishName }}</strong>
        <p v-if="item.observation" class="mt-1 rounded-lg bg-amber-50 p-2 text-amber-900 dark:bg-amber-950 dark:text-amber-200">Obs.: {{ item.observation }}</p>
      </li>
    </ul>
    <button v-if="order.status !== OrderStatus.Ready" type="button" :disabled="busy"
      class="min-h-11 w-full rounded-lg bg-indigo-600 px-3 py-2 text-sm font-semibold text-white hover:bg-indigo-700 disabled:cursor-wait disabled:opacity-50"
      @click="$emit('advance', order.id)">
      {{ updating ? 'Atualizando...' : order.status === OrderStatus.Submitted ? 'Iniciar preparo' : 'Despachar para o salão' }}
    </button>
    <p v-else class="rounded-lg bg-emerald-50 p-3 text-sm text-emerald-800 dark:bg-emerald-950 dark:text-emerald-200">Pronto para retirada. Aguardando o garçom confirmar a entrega.</p>
  </article>
</template>
