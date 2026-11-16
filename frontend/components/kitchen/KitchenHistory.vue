<script setup lang="ts">
import { computed, ref } from 'vue'
import { useKitchen } from '~/composables/useKitchen'
import { OrderStatus } from '~/services/modules/orders/orderService'

const { orders, loading, error, lastUpdated, refresh } = useKitchen()
const search = ref('')
const status = ref('')
const labels: Record<OrderStatus, string> = {
  [OrderStatus.Submitted]: 'Enviado', [OrderStatus.Preparing]: 'Em preparo', [OrderStatus.Ready]: 'Pronto',
  [OrderStatus.Completed]: 'Entregue', [OrderStatus.Paid]: 'Pago', [OrderStatus.Cancelled]: 'Cancelado'
}
const normalize = (value: string) => value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase().trim()
const filtered = computed(() => orders.value.filter(order =>
  (status.value === '' || order.status === Number(status.value)) &&
  normalize([order.id, order.tableReference, ...order.items.flatMap(item => [item.dishName, item.observation])].join(' ')).includes(normalize(search.value))))
</script>

<template>
  <div class="mx-auto w-full max-w-7xl space-y-6 px-4 py-8 text-zinc-900 dark:text-zinc-100 sm:px-6">
    <header class="flex flex-wrap items-start justify-between gap-4">
      <div>
        <h1 class="text-3xl font-bold">Pedidos da cozinha</h1>
        <p class="mt-2 text-sm text-zinc-500">Consulte pedidos em andamento, entregues, pagos e cancelados.</p>
      </div>
      <NuxtLink to="/cozinha" class="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white">Abrir fila da cozinha</NuxtLink>
    </header>
    <div class="flex flex-wrap items-center gap-3">
      <button type="button" :disabled="loading" class="rounded-lg border border-zinc-300 px-4 py-2 text-sm dark:border-zinc-700 disabled:opacity-50" @click="refresh()">Atualizar pedidos</button>
      <p class="text-xs text-zinc-500">Atualização automática a cada 10 segundos<span v-if="lastUpdated"> · {{ lastUpdated.toLocaleTimeString('pt-BR') }}</span></p>
    </div>
    <p v-if="error" role="alert" class="rounded-xl bg-red-50 p-4 text-red-700 dark:bg-red-950 dark:text-red-200">{{ error }}</p>
    <div class="grid gap-4 sm:grid-cols-2">
      <label class="text-sm font-medium">Buscar pedido
        <input v-model="search" type="search" placeholder="Mesa, pedido, prato ou observação" class="mt-2 min-h-11 w-full rounded-lg border border-zinc-300 bg-white px-3 py-2 dark:border-zinc-700 dark:bg-zinc-900" />
      </label>
      <label class="text-sm font-medium">Situação
        <select v-model="status" class="mt-2 min-h-11 w-full rounded-lg border border-zinc-300 bg-white px-3 py-2 dark:border-zinc-700 dark:bg-zinc-900">
          <option value="">Todas as situações</option>
          <option v-for="(label, value) in labels" :key="value" :value="String(value)">{{ label }}</option>
        </select>
      </label>
    </div>
    <p v-if="loading && !orders.length" role="status" class="text-sm text-zinc-500">Carregando pedidos...</p>
    <p v-else-if="!error && !filtered.length" class="text-sm text-zinc-500">Nenhum pedido encontrado.</p>
    <div class="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
      <article v-for="order in filtered" :key="order.id" class="min-w-0 rounded-2xl border border-zinc-200 bg-white p-4 dark:border-zinc-800 dark:bg-zinc-900">
        <div class="flex flex-wrap items-start justify-between gap-2">
          <div class="min-w-0"><h2 class="break-words font-semibold">Mesa / comanda {{ order.tableReference }}</h2><p class="mt-1 text-xs text-zinc-500">Pedido #{{ order.id }}</p></div>
          <span class="rounded-full bg-indigo-50 px-2 py-1 text-xs text-indigo-700 dark:bg-indigo-950 dark:text-indigo-300">{{ labels[order.status] }}</span>
        </div>
        <details class="mt-4 text-sm">
          <summary class="cursor-pointer font-medium">Ver itens e observações</summary>
          <ul class="mt-3 space-y-2">
            <li v-for="(item, index) in order.items" :key="index" class="break-words">{{ item.quantity }}× {{ item.dishName }}<p v-if="item.observation" class="text-zinc-500">Obs.: {{ item.observation }}</p></li>
          </ul>
        </details>
      </article>
    </div>
  </div>
</template>
