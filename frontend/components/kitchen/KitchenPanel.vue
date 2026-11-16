<script setup lang="ts">
import { computed, ref } from 'vue'
import { useKitchen } from '~/composables/useKitchen'
import { OrderStatus } from '~/services/modules/orders/orderService'
import KitchenOrderCard from './KitchenOrderCard.vue'

const { activeOrders, loading, updatingId, error, feedback, lastUpdated, refresh, advance } = useKitchen()
const search = ref('')
const normalize = (value: string) => value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase().trim()
const columns = [
  { status: OrderStatus.Submitted, title: 'Recebidos', help: 'Pedidos enviados pelo salão', color: 'border-indigo-300 dark:border-indigo-800' },
  { status: OrderStatus.Preparing, title: 'Em preparo', help: 'Pedidos em execução na cozinha', color: 'border-amber-300 dark:border-amber-800' },
  { status: OrderStatus.Ready, title: 'Prontos para retirada', help: 'Despachados para o garçom', color: 'border-emerald-300 dark:border-emerald-800' }
]
const filtered = computed(() => {
  const term = normalize(search.value)
  return activeOrders.value.filter(order => normalize([
    order.id, order.tableReference, ...order.items.flatMap(item => [item.dishName, item.observation])
  ].join(' ')).includes(term))
})
const count = (status: OrderStatus) => activeOrders.value.filter(order => order.status === status).length
</script>

<template>
  <div class="mx-auto w-full max-w-7xl space-y-6 px-4 py-8 text-zinc-900 dark:text-zinc-100 sm:px-6">
    <header class="flex flex-wrap items-start justify-between gap-4">
      <div>
        <p class="text-xs font-semibold uppercase tracking-widest text-indigo-600 dark:text-indigo-400">Operação da cozinha</p>
        <h1 class="mt-2 text-3xl font-bold">Cozinha</h1>
        <p class="mt-2 text-sm text-zinc-500 dark:text-zinc-400">Receba os pedidos do garçom, prepare e despache para o salão.</p>
      </div>
      <div class="flex flex-wrap gap-3">
        <NuxtLink to="/cardapio" class="rounded-lg border border-zinc-300 px-4 py-2 text-sm font-semibold dark:border-zinc-700">Consultar cardápio</NuxtLink>
        <button type="button" :disabled="loading || updatingId !== null" class="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white disabled:opacity-50" @click="refresh()">{{ loading ? 'Atualizando...' : 'Atualizar pedidos' }}</button>
      </div>
    </header>
    <div class="flex flex-wrap items-center justify-between gap-3 text-sm">
      <p role="status">{{ activeOrders.length }} pedidos na cozinha · Atualização automática a cada 10 segundos</p>
      <p v-if="lastUpdated" class="text-xs text-zinc-500">Última atualização: {{ lastUpdated.toLocaleTimeString('pt-BR') }}</p>
    </div>
    <p v-if="error" role="alert" class="rounded-xl bg-red-50 p-4 text-sm text-red-700 dark:bg-red-950 dark:text-red-200">{{ error }} Os dados exibidos podem estar desatualizados.</p>
    <p v-if="feedback" role="status" class="rounded-xl bg-emerald-50 p-4 text-sm text-emerald-800 dark:bg-emerald-950 dark:text-emerald-200">{{ feedback }}</p>
    <div>
      <label for="kitchen-search" class="block text-sm font-medium">Buscar pedido na cozinha</label>
      <input id="kitchen-search" v-model="search" type="search" placeholder="Mesa, pedido, prato ou observação"
        class="mt-2 min-h-11 w-full rounded-lg border border-zinc-300 bg-white px-3 py-2 dark:border-zinc-700 dark:bg-zinc-900" />
    </div>
    <p v-if="loading && !activeOrders.length" class="text-sm text-zinc-500">Carregando pedidos...</p>
    <div class="grid items-start gap-4 lg:grid-cols-3">
      <section v-for="column in columns" :key="column.status" :aria-label="column.title"
        class="min-w-0 space-y-3 rounded-2xl border-t-4 bg-zinc-50 p-3 dark:bg-zinc-950" :class="column.color">
        <header class="p-1">
          <h2 class="font-semibold">{{ column.title }} <span class="ml-2 text-sm text-zinc-500">{{ count(column.status) }}</span></h2>
          <p class="mt-1 text-xs text-zinc-500">{{ column.help }}</p>
        </header>
        <KitchenOrderCard v-for="order in filtered.filter(order => order.status === column.status)" :key="order.id"
          :order="order" :busy="updatingId !== null" :updating="updatingId === order.id" @advance="advance" />
        <p v-if="!loading && !filtered.some(order => order.status === column.status)" class="p-3 text-sm text-zinc-500">{{ search ? 'Nenhum pedido com essa busca.' : 'Nenhum pedido nesta etapa.' }}</p>
      </section>
    </div>
    <p class="text-xs text-zinc-500">Pedidos mais antigos aparecem primeiro. Após a entrega confirmada pelo garçom, o pedido sai da fila da cozinha.</p>
  </div>
</template>
