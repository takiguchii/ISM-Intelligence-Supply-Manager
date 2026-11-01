<script setup lang="ts">
import { computed, ref } from 'vue'
import { useWaiterMock } from '~/composables/useWaiterMock'
import type { WaiterStatus } from '~/composables/useWaiterMock'

type View = 'home' | 'service' | 'orders' | 'menu'

const props = defineProps<{ view: View }>()

const {
  dishes,
  categories,
  table,
  cart,
  orders,
  cartTotal,
  activeOrders,
  readyOrders,
  occupiedTables,
  total,
  money,
  addDish,
  changeQuantity,
  submitOrder,
  advanceOrder
} = useWaiterMock()

const search = ref('')
const category = ref('Todos')
const status = ref('Todos')
const feedback = ref('')

const titles: Record<View, string> = {
  home: 'Área do Garçom',
  service: 'Novo atendimento',
  orders: 'Pedidos',
  menu: 'Cardápio'
}

const descriptions: Record<View, string> = {
  home: 'Acompanhe o movimento do salão.',
  service: 'Identifique a mesa e monte a comanda.',
  orders: 'Acompanhe o preparo e a entrega dos pedidos.',
  menu: 'Consulte os pratos, preços e disponibilidade.'
}

const statuses: WaiterStatus[] = [
  'Enviado',
  'Em preparo',
  'Pronto',
  'Entregue'
]

const statusClasses: Record<WaiterStatus, string> = {
  Enviado:
    'bg-indigo-100 text-indigo-700 dark:bg-indigo-950 dark:text-indigo-300',
  'Em preparo':
    'bg-amber-100 text-amber-800 dark:bg-amber-950 dark:text-amber-300',
  Pronto:
    'bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300',
  Entregue:
    'bg-zinc-100 text-zinc-700 dark:bg-zinc-800 dark:text-zinc-300'
}

const actionLabels: Partial<Record<WaiterStatus, string>> = {
  Enviado: 'Simular início do preparo',
  'Em preparo': 'Simular pedido pronto',
  Pronto: 'Confirmar entrega'
}

function normalize(value: string) {
  return value
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .trim()
}

const filteredDishes = computed(() => {
  const term = normalize(search.value)

  return dishes.value.filter(dish => {
    const matchesCategory =
      category.value === 'Todos' || dish.category === category.value

    const matchesSearch = normalize(
      `${dish.name} ${dish.description}`
    ).includes(term)

    return matchesCategory && matchesSearch
  })
})

const filteredOrders = computed(() => {
  const term = normalize(search.value)

  return orders.value.filter(order => {
    const matchesStatus =
      status.value === 'Todos' || order.status === status.value

    const content = [
      order.id,
      order.table,
      ...order.items.map(item => item.name)
    ].join(' ')

    return matchesStatus && normalize(content).includes(term)
  })
})

const metrics = computed(() => [
  { label: 'Mesas em atendimento', value: occupiedTables.value },
  { label: 'Pedidos em andamento', value: activeOrders.value.length },
  { label: 'Prontos para entrega', value: readyOrders.value.length }
])

function sendOrder() {
  const id = submitOrder()

  feedback.value = id
    ? `Pedido #${id} criado na demonstração. Consulte a aba Pedidos.`
    : 'Informe a mesa e adicione pelo menos um item.'
}
</script>

<template>
  <main class="waiter-page">
    <header class="flex flex-wrap items-start justify-between gap-4">
      <div>
        <p
          class="text-xs font-semibold uppercase tracking-widest
            text-indigo-600 dark:text-indigo-400"
        >
          Operação do salão
        </p>

        <h1 class="mt-2 text-2xl font-bold sm:text-3xl">
          {{ titles[view] }}
        </h1>

        <p class="mt-2 text-sm text-zinc-500 dark:text-zinc-400">
          {{ descriptions[view] }}
        </p>
      </div>

      <span
        class="badge bg-indigo-100 text-indigo-700
          dark:bg-indigo-950 dark:text-indigo-300"
      >
        Demonstração
      </span>
    </header>

    <p
      class="rounded-xl border border-indigo-200 bg-indigo-50 p-3
        text-sm text-indigo-800 dark:border-indigo-900
        dark:bg-indigo-950/40 dark:text-indigo-200"
    >
      Dados fictícios. As ações são simuladas e reiniciam ao atualizar
      a página.
    </p>

    <!-- INÍCIO -->
    <template v-if="view === 'home'">
      <section
        class="grid gap-4 sm:grid-cols-3"
        aria-label="Resumo do salão"
      >
        <article
          v-for="metric in metrics"
          :key="metric.label"
          class="panel"
        >
          <p class="text-sm text-zinc-500 dark:text-zinc-400">
            {{ metric.label }}
          </p>

          <p class="mt-3 text-3xl font-bold">{{ metric.value }}</p>
        </article>
      </section>

      <nav
        class="flex flex-wrap justify-center gap-3"
        aria-label="Ações do garçom"
      >
        <NuxtLink to="/atendimento" class="action-button">
          <span class="action-text">Novo atendimento</span>
        </NuxtLink>

        <NuxtLink to="/pedidos" class="action-button">
          <span class="action-text">Acompanhar pedidos</span>
        </NuxtLink>

        <NuxtLink to="/cardapio" class="action-button">
          <span class="action-text">Consultar cardápio</span>
        </NuxtLink>
      </nav>

      <section class="panel">
        <h2 class="text-lg font-semibold">Prontos para entrega</h2>

        <p
          v-if="!readyOrders.length"
          class="mt-4 text-sm text-zinc-500 dark:text-zinc-400"
        >
          Nenhum pedido aguardando entrega.
        </p>

        <ul v-else class="mt-4 space-y-3">
          <li
            v-for="order in readyOrders"
            :key="order.id"
            class="flex flex-wrap items-center justify-between gap-3
              rounded-xl bg-zinc-50 p-4 dark:bg-zinc-800"
          >
            <div>
              <p class="font-semibold">
                Mesa {{ order.table }} · Pedido #{{ order.id }}
              </p>

              <p class="mt-1 text-sm text-zinc-500 dark:text-zinc-400">
                {{ order.items.map(item =>
                  `${item.quantity}× ${item.name}`
                ).join(', ') }}
              </p>
            </div>

            <button
              type="button"
              class="primary"
              @click="advanceOrder(order.id)"
            >
              Confirmar entrega
            </button>
          </li>
        </ul>
      </section>
    </template>

    <!-- ATENDIMENTO E CARDÁPIO -->
    <template v-else-if="view === 'service' || view === 'menu'">
      <div
        class="grid items-start gap-6"
        :class="{ 'xl:grid-cols-3': view === 'service' }"
      >
        <section
          class="space-y-4"
          :class="{ 'xl:col-span-2': view === 'service' }"
        >
          <div class="panel grid gap-3 sm:grid-cols-2">
            <label class="field-label">
              Buscar prato
              <input
                v-model="search"
                type="search"
                class="field"
                placeholder="Nome ou descrição"
              />
            </label>

            <label class="field-label">
              Categoria
              <select v-model="category" class="field">
                <option
                  v-for="item in categories"
                  :key="item"
                  :value="item"
                >
                  {{ item }}
                </option>
              </select>
            </label>
          </div>

          <p
            v-if="!filteredDishes.length"
            class="panel text-sm text-zinc-500 dark:text-zinc-400"
          >
            Nenhum prato encontrado com esses filtros.
          </p>

          <div v-else class="grid gap-4 sm:grid-cols-2">
            <article
              v-for="dish in filteredDishes"
              :key="dish.id"
              class="panel flex flex-col"
            >
              <div
                class="flex flex-wrap items-center justify-between gap-2"
              >
                <span
                  class="text-xs font-semibold text-indigo-600
                    dark:text-indigo-400"
                >
                  {{ dish.category }}
                </span>

                <span
                  class="badge"
                  :class="dish.available
                    ? 'bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300'
                    : 'bg-zinc-100 text-zinc-600 dark:bg-zinc-800 dark:text-zinc-400'"
                >
                  {{ dish.available ? 'Disponível' : 'Indisponível' }}
                </span>
              </div>

              <h2 class="mt-3 text-lg font-semibold">
                {{ dish.name }}
              </h2>

              <p
                class="mt-2 flex-1 text-sm text-zinc-500
                  dark:text-zinc-400"
              >
                {{ dish.description }}
              </p>

              <div
                class="mt-5 flex items-center justify-between gap-3"
              >
                <strong>{{ money(dish.price) }}</strong>

                <button
                  v-if="view === 'service'"
                  type="button"
                  class="primary"
                  :disabled="!dish.available"
                  :aria-label="`Adicionar ${dish.name}`"
                  @click="addDish(dish)"
                >
                  Adicionar
                </button>
              </div>
            </article>
          </div>
        </section>

        <!-- COMANDA -->
        <aside
          v-if="view === 'service'"
          class="panel space-y-4"
        >
          <h2 class="text-lg font-semibold">Comanda atual</h2>

          <label class="field-label">
            Mesa / identificação
            <input
              v-model="table"
              class="field"
              maxlength="30"
              placeholder="Ex.: 04 ou João"
            />
          </label>

          <p
            v-if="!cart.length"
            class="py-6 text-center text-sm text-zinc-500
              dark:text-zinc-400"
          >
            Adicione itens do cardápio.
          </p>

          <ul v-else class="space-y-4">
            <li
              v-for="(item, index) in cart"
              :key="index"
              class="space-y-3 border-t border-zinc-200 pt-4
                dark:border-zinc-800"
            >
              <div class="flex justify-between gap-3 text-sm">
                <strong>{{ item.name }}</strong>
                <span>
                  {{ money(item.price * item.quantity) }}
                </span>
              </div>

              <div class="flex items-center gap-3">
                <button
                  type="button"
                  class="secondary"
                  :aria-label="`Diminuir quantidade de ${item.name}`"
                  @click="changeQuantity(index, -1)"
                >
                  −
                </button>

                <span class="text-sm">{{ item.quantity }}</span>

                <button
                  type="button"
                  class="secondary"
                  :aria-label="`Aumentar quantidade de ${item.name}`"
                  @click="changeQuantity(index, 1)"
                >
                  +
                </button>

                <button
                  type="button"
                  class="ml-auto text-sm text-red-600
                    dark:text-red-400"
                  :aria-label="`Remover ${item.name}`"
                  @click="cart.splice(index, 1)"
                >
                  Remover
                </button>
              </div>

              <label class="field-label">
                Observação
                <input
                  v-model="item.observation"
                  class="field"
                  maxlength="200"
                  placeholder="Ex.: sem cebola"
                />
              </label>
            </li>
          </ul>

          <div
            class="flex justify-between border-t border-zinc-200
              pt-4 font-bold dark:border-zinc-800"
          >
            <span>Total</span>
            <span>{{ money(cartTotal) }}</span>
          </div>

          <button
            type="button"
            class="primary w-full"
            :disabled="!table.trim() || !cart.length"
            @click="sendOrder"
          >
            Enviar pedido simulado
          </button>

          <p
            v-if="feedback"
            role="status"
            class="text-sm text-indigo-600 dark:text-indigo-300"
          >
            {{ feedback }}
          </p>
        </aside>
      </div>
    </template>

    <!-- PEDIDOS -->
    <template v-else>
      <div class="panel grid gap-3 sm:grid-cols-2">
        <label class="field-label">
          Buscar pedido
          <input
            v-model="search"
            class="field"
            type="search"
            placeholder="Número, mesa ou prato"
          />
        </label>

        <label class="field-label">
          Situação
          <select v-model="status" class="field">
            <option value="Todos">Todos</option>

            <option
              v-for="item in statuses"
              :key="item"
              :value="item"
            >
              {{ item }}
            </option>
          </select>
        </label>
      </div>

      <p
        v-if="!filteredOrders.length"
        class="panel text-sm text-zinc-500 dark:text-zinc-400"
      >
        Nenhum pedido encontrado com esses filtros.
      </p>

      <section
        v-else
        class="grid gap-4 md:grid-cols-2 xl:grid-cols-3"
      >
        <article
          v-for="order in filteredOrders"
          :key="order.id"
          class="panel flex flex-col"
        >
          <div
            class="flex flex-wrap items-start justify-between gap-3"
          >
            <div>
              <h2 class="text-lg font-semibold">
                Mesa {{ order.table }}
              </h2>

              <p class="text-xs text-zinc-500 dark:text-zinc-400">
                Pedido #{{ order.id }}
              </p>
            </div>

            <span
              class="badge"
              :class="statusClasses[order.status]"
            >
              {{ order.status }}
            </span>
          </div>

          <ul class="my-5 flex-1 space-y-3 text-sm">
            <li
              v-for="(item, index) in order.items"
              :key="index"
            >
              <div class="flex justify-between gap-3">
                <span>{{ item.quantity }}× {{ item.name }}</span>
                <span>
                  {{ money(item.price * item.quantity) }}
                </span>
              </div>

              <p
                v-if="item.observation"
                class="mt-1 text-xs text-zinc-500
                  dark:text-zinc-400"
              >
                Obs.: {{ item.observation }}
              </p>
            </li>
          </ul>

          <div
            class="flex justify-between border-t border-zinc-200
              pt-4 font-bold dark:border-zinc-800"
          >
            <span>Total</span>
            <span>{{ money(total(order.items)) }}</span>
          </div>

          <button
            v-if="actionLabels[order.status]"
            type="button"
            class="primary mt-4 w-full"
            @click="advanceOrder(order.id)"
          >
            {{ actionLabels[order.status] }}
          </button>
        </article>
      </section>
    </template>
  </main>
</template>

<style scoped>
.waiter-page {
  @apply mx-auto w-full max-w-7xl space-y-6 px-4 py-8
    text-zinc-900 sm:px-6 dark:text-zinc-100;
}

.panel {
  @apply rounded-2xl border border-zinc-200 bg-white p-5
    shadow-sm dark:border-zinc-800 dark:bg-zinc-900;
}

.field-label {
  @apply block text-sm font-medium;
}

.field {
  @apply mt-2 block w-full rounded-lg border border-zinc-300
    bg-white px-3 py-2 text-sm text-zinc-900 outline-none
    focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/20
    dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-100;
}

.primary {
  @apply inline-flex items-center justify-center rounded-lg
    bg-indigo-600 px-4 py-2 text-sm font-semibold text-white
    transition-colors hover:bg-indigo-700
    focus-visible:outline focus-visible:outline-2
    focus-visible:outline-offset-2 focus-visible:outline-indigo-500
    disabled:cursor-not-allowed disabled:opacity-50;
}

.secondary {
  @apply inline-flex items-center justify-center rounded-lg
    border border-zinc-300 bg-white px-3 py-2 text-sm
    font-medium hover:bg-zinc-100 dark:border-zinc-700
    dark:bg-zinc-900 dark:hover:bg-zinc-800;
}

.action-button {
  @apply inline-flex items-center justify-center rounded-lg border-none
    bg-indigo-600 px-4 py-2.5 text-sm font-semibold text-white
    outline outline-2 outline-indigo-600 outline-offset-[-2px]
    focus-visible:outline focus-visible:outline-2
    focus-visible:outline-offset-2 focus-visible:outline-indigo-500;
  transition: 400ms;
}

.action-button .action-text {
  @apply font-bold;
  transition: 400ms;
}

.action-button:hover {
  @apply bg-transparent;
}

.action-button:hover .action-text {
  @apply text-indigo-600 dark:text-indigo-400;
}

.badge {
  @apply inline-flex rounded-full px-3 py-1 text-xs font-semibold;
}
</style>
