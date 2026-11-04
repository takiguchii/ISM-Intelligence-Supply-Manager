<script setup lang="ts">
import { computed, ref, watch } from 'vue'
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
  simulateKitchenStep,
  confirmDelivery
} = useWaiterMock()

const search = ref('')
const category = ref('Todos')
const status = ref('Todos')
const showActiveOnly = ref(true)
const feedback = ref('')
const orderFeedback = ref('')
const isReviewOpen = ref(false)
const isMobileCartOpen = ref(false)
const isSubmitting = ref(false)
const dishPage = ref(1)
const recentlyAddedDishId = ref<number | null>(null)
let addFeedbackTimeout: ReturnType<typeof setTimeout> | undefined

const dishesPerPage = 6
const simulatingOrderId = ref<number | null>(null)
const deliveringOrderId = ref<number | null>(null)
const statusChangingOrderId = ref<number | null>(null)
let statusFeedbackTimeout: ReturnType<typeof setTimeout> | undefined

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
      `${dish.name} ${dish.description} ${dish.category}`
    ).includes(term)

    return matchesCategory && matchesSearch
  })
})

const categoryCounts = computed(() => {
  const counts = new Map<string, number>()

  for (const dish of dishes.value) {
    counts.set(dish.category, (counts.get(dish.category) || 0) + 1)
  }

  return counts
})

const totalDishPages = computed(() =>
  Math.max(1, Math.ceil(filteredDishes.value.length / dishesPerPage))
)

const paginatedDishes = computed(() => {
  const start = (dishPage.value - 1) * dishesPerPage
  return filteredDishes.value.slice(start, start + dishesPerPage)
})

watch([search, category], () => {
  dishPage.value = 1
})

watch(totalDishPages, (pages) => {
  if (dishPage.value > pages) dishPage.value = pages
})

function dishImage(categoryName: string) {
  const images: Record<string, string> = {
    Entradas: '/images/waiter/dish-entrada.svg',
    'Pratos principais': '/images/waiter/dish-principal.svg',
    Bebidas: '/images/waiter/dish-bebida.svg',
    Sobremesas: '/images/waiter/dish-sobremesa.svg'
  }

  return images[categoryName] || '/images/waiter/dish-principal.svg'
}

function addDishToCart(dish: (typeof dishes.value)[number]) {
  addDish(dish)
  recentlyAddedDishId.value = dish.id

  if (addFeedbackTimeout) clearTimeout(addFeedbackTimeout)
  addFeedbackTimeout = setTimeout(() => {
    recentlyAddedDishId.value = null
  }, 520)
}

const filteredOrders = computed(() => {
  const term = normalize(search.value)

  return orders.value
    .filter(order => {
    const matchesStatus =
      status.value === 'Todos' || order.status === status.value
    const matchesActive = !showActiveOnly.value || order.status !== 'Entregue'

    const content = [
      order.id,
      order.tableReference,
      ...order.items.flatMap(item => [item.name, item.observation])
    ].join(' ')

      return matchesStatus && matchesActive && normalize(content).includes(term)
    })
    .sort((first, second) => {
      const priority: Record<WaiterStatus, number> = {
        Pronto: 0,
        'Em preparo': 1,
        Enviado: 2,
        Entregue: 3
      }

      return priority[first.status] - priority[second.status]
    })
})

const activeOrderCount = computed(() => activeOrders.value.length)

const kitchenSimulationOrders = computed(() =>
  orders.value.filter(order => order.status === 'Enviado' || order.status === 'Em preparo')
)

const metrics = computed(() => [
  { label: 'Mesas em atendimento', value: occupiedTables.value },
  { label: 'Pedidos em andamento', value: activeOrders.value.length },
  { label: 'Prontos para entrega', value: readyOrders.value.length }
])

const cartItemsCount = computed(() =>
  cart.value.reduce((count, item) => count + item.quantity, 0)
)

const canReviewOrder = computed(() =>
  Boolean(table.value.trim() && cart.value.length && !isSubmitting.value)
)

function openReview() {
  feedback.value = ''

  if (!table.value.trim() || !cart.value.length) {
    feedback.value = 'Informe a mesa e adicione pelo menos um item.'
    return
  }

  isReviewOpen.value = true
}

function closeReview() {
  if (!isSubmitting.value) isReviewOpen.value = false
}

function confirmOrder() {
  if (!canReviewOrder.value) return

  isSubmitting.value = true
  const id = submitOrder()
  isSubmitting.value = false
  isReviewOpen.value = false
  isMobileCartOpen.value = false

  feedback.value = id
    ? `Pedido #${id} criado na demonstração.`
    : 'Não foi possível criar o pedido. Revise a mesa e os itens.'
}

function simulateKitchen(id: number) {
  if (simulatingOrderId.value !== null) return

  simulatingOrderId.value = id
  simulateKitchenStep(id)
  markStatusChange(id)
  orderFeedback.value = `Atualização demonstrativa da cozinha aplicada ao pedido #${id}.`
  simulatingOrderId.value = null
}

function markStatusChange(id: number) {
  statusChangingOrderId.value = id

  if (statusFeedbackTimeout) clearTimeout(statusFeedbackTimeout)
  statusFeedbackTimeout = setTimeout(() => {
    statusChangingOrderId.value = null
  }, 600)
}

function deliverOrder(id: number) {
  if (deliveringOrderId.value !== null) return

  const order = orders.value.find(item => item.id === id)
  if (!order || order.status !== 'Pronto') return

  if (process.client && !window.confirm(`Confirmar entrega do pedido #${id}?`)) return

  deliveringOrderId.value = id
  const delivered = confirmDelivery(id)
  if (delivered) markStatusChange(id)
  orderFeedback.value = delivered
    ? `Pedido #${id} marcado como entregue.`
    : 'O pedido só pode ser entregue quando estiver pronto.'
  deliveringOrderId.value = null
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
                Mesa {{ order.tableReference }} · Pedido #{{ order.id }}
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
              @click="deliverOrder(order.id)"
            >
              Confirmar entrega
            </button>
          </li>
        </ul>
      </section>
    </template>

    <!-- ATENDIMENTO E CARDÁPIO -->
    <template v-else-if="view === 'service' || view === 'menu'">
      <section
        v-if="view === 'service'"
        class="panel space-y-3"
      >
        <div>
          <p class="text-xs font-semibold uppercase tracking-widest text-indigo-600 dark:text-indigo-400">
            Comanda em edição
          </p>
          <h2 class="mt-1 text-lg font-semibold">Identifique a mesa</h2>
        </div>

        <label class="field-label">
          Mesa / identificação
          <input
            v-model="table"
            class="field"
            maxlength="30"
            placeholder="Ex.: 04 ou João"
            autocomplete="off"
          />
        </label>
      </section>

      <section
        v-else
        class="panel flex flex-wrap items-center justify-between gap-4"
      >
        <div>
          <p class="text-xs font-semibold uppercase tracking-widest text-indigo-600 dark:text-indigo-400">
            Consulta rápida
          </p>
          <h2 class="mt-1 text-lg font-semibold">Itens disponíveis no cardápio</h2>
          <p class="mt-1 text-sm text-zinc-500 dark:text-zinc-400">
            Consulte preços e disponibilidade. Nada é adicionado à comanda nesta tela.
          </p>
        </div>
        <NuxtLink to="/atendimento" class="action-button">
          <span class="action-text">Criar pedido</span>
        </NuxtLink>
      </section>

      <div
        class="grid items-start gap-6"
        :class="{ 'xl:grid-cols-3': view === 'service' }"
      >
        <section
          class="space-y-4"
          :class="{ 'xl:col-span-2': view === 'service' }"
        >
          <div class="panel space-y-4">
            <div>
              <label for="waiter-dish-search" class="field-label">
                Buscar prato
              </label>
              <form
                class="search-form mt-2"
                role="search"
                @submit.prevent
              >
                <button type="submit" aria-label="Buscar prato">
                  <svg
                    width="17"
                    height="16"
                    fill="none"
                    xmlns="http://www.w3.org/2000/svg"
                    aria-hidden="true"
                  >
                    <path
                      d="M7.667 12.667A5.333 5.333 0 107.667 2a5.333 5.333 0 000 10.667zM14.334 14l-2.9-2.9"
                      stroke="currentColor"
                      stroke-width="1.333"
                      stroke-linecap="round"
                      stroke-linejoin="round"
                    />
                  </svg>
                </button>
                <input
                  id="waiter-dish-search"
                  v-model="search"
                  class="search-input"
                  placeholder="Nome, ingrediente ou categoria"
                  type="search"
                  autocomplete="off"
                />
                <button
                  v-if="search"
                  class="search-reset"
                  type="button"
                  aria-label="Limpar busca"
                  @click="search = ''"
                >
                  <svg
                    xmlns="http://www.w3.org/2000/svg"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    stroke-width="2"
                    aria-hidden="true"
                  >
                    <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </form>
            </div>

            <div>
              <p class="field-label">Categoria</p>
              <div class="mt-2 flex flex-wrap gap-2">
                <button
                  v-for="item in categories"
                  :key="item"
                  type="button"
                  class="filter-button"
                  :class="category === item ? 'filter-button-active' : ''"
                  :aria-pressed="category === item"
                  @click="category = item"
                >
                  {{ item }}
                  <span class="ml-1 text-xs opacity-70">
                    {{ item === 'Todos' ? dishes.length : categoryCounts.get(item) || 0 }}
                  </span>
                </button>
              </div>
            </div>
          </div>

          <p
            v-if="!filteredDishes.length"
            class="panel text-sm text-zinc-500 dark:text-zinc-400"
          >
            Nenhum prato encontrado com esses filtros.
          </p>

          <TransitionGroup
            v-else
            name="dish-list"
            tag="div"
            class="grid min-w-0 gap-4 sm:grid-cols-2"
          >
            <article
              v-for="dish in paginatedDishes"
              :key="dish.id"
              class="dish-card panel flex min-w-0 flex-col overflow-hidden p-0"
              :class="{ 'dish-card-added': recentlyAddedDishId === dish.id }"
            >
              <img
                :src="dishImage(dish.category)"
                :alt="`Ilustração de ${dish.name}`"
                class="dish-image h-32 w-full object-cover sm:h-36"
              />

              <div class="flex flex-1 flex-col p-4">
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

              <details v-if="view === 'menu'" class="menu-details mt-3">
                <summary class="cursor-pointer text-sm font-semibold text-indigo-600 dark:text-indigo-400">
                  Ver detalhes
                </summary>
                <p class="mt-2 text-sm text-zinc-500 dark:text-zinc-400">
                  {{ dish.description || 'Descrição não informada.' }}
                </p>
              </details>

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
                  @click="addDishToCart(dish)"
                >
                  {{ recentlyAddedDishId === dish.id ? 'Adicionado' : 'Adicionar' }}
                </button>
              </div>
              </div>
            </article>
          </TransitionGroup>

          <nav
            v-if="filteredDishes.length > dishesPerPage"
            class="flex items-center justify-between gap-3"
            aria-label="Paginação do cardápio"
          >
            <button
              type="button"
              class="secondary min-h-11"
              :disabled="dishPage === 1"
              @click="dishPage--"
            >
              Anterior
            </button>
            <span class="text-sm text-zinc-500 dark:text-zinc-400">
              Página {{ dishPage }} de {{ totalDishPages }}
            </span>
            <button
              type="button"
              class="secondary min-h-11"
              :disabled="dishPage === totalDishPages"
              @click="dishPage++"
            >
              Próxima
            </button>
          </nav>
        </section>

        <!-- RESUMO MOBILE E COMANDA -->
        <button
          v-if="view === 'service'"
          type="button"
          class="panel flex w-full items-center justify-between text-left xl:hidden"
          :aria-expanded="isMobileCartOpen"
          @click="isMobileCartOpen = !isMobileCartOpen"
        >
          <span>
            <strong class="block">Comanda atual</strong>
            <span class="text-sm text-zinc-500 dark:text-zinc-400">
              {{ cartItemsCount }} {{ cartItemsCount === 1 ? 'item' : 'itens' }} · {{ money(cartTotal) }}
            </span>
          </span>
          <span class="text-sm font-semibold text-indigo-600 dark:text-indigo-400">
            {{ isMobileCartOpen ? 'Fechar' : 'Revisar' }}
          </span>
        </button>

        <aside
          v-if="view === 'service'"
          class="panel cart-panel space-y-4 xl:sticky xl:top-24"
          :class="isMobileCartOpen ? 'block' : 'hidden xl:block'"
        >
          <div class="flex items-center justify-between gap-3">
            <div>
              <h2 class="text-lg font-semibold">Comanda atual</h2>
              <p class="text-xs text-zinc-500 dark:text-zinc-400">
                {{ cartItemsCount }} {{ cartItemsCount === 1 ? 'item' : 'itens' }}
              </p>
            </div>
            <span class="text-sm font-bold">{{ money(cartTotal) }}</span>
          </div>

          <p
            v-if="!cart.length"
            class="py-6 text-center text-sm text-zinc-500
              dark:text-zinc-400"
          >
            Adicione itens do cardápio.
          </p>

          <ul v-else class="cart-list space-y-2">
            <li
              v-for="(item, index) in cart"
              :key="index"
              class="cart-item space-y-2 rounded-xl border border-zinc-200 p-3
                dark:border-zinc-800"
            >
              <div class="flex justify-between gap-3 text-sm">
                <strong class="min-w-0 truncate">{{ item.name }}</strong>
                <span>
                  {{ money(item.price * item.quantity) }}
                </span>
              </div>

              <div class="flex min-h-11 items-center gap-2">
                <button
                  type="button"
                  class="secondary min-h-11 min-w-11 px-2"
                  :aria-label="`Diminuir quantidade de ${item.name}`"
                  @click="changeQuantity(index, -1)"
                >
                  −
                </button>

                <span class="text-sm">{{ item.quantity }}</span>

                <button
                  type="button"
                  class="secondary min-h-11 min-w-11 px-2"
                  :aria-label="`Aumentar quantidade de ${item.name}`"
                  @click="changeQuantity(index, 1)"
                >
                  +
                </button>

                <button
                  type="button"
                  class="ml-auto min-h-11 px-2 text-sm text-red-600
                    dark:text-red-400"
                  :aria-label="`Remover ${item.name}`"
                  @click="cart.splice(index, 1)"
                >
                  Remover
                </button>
              </div>

              <details class="text-sm">
                <summary class="cursor-pointer select-none text-zinc-500 dark:text-zinc-400">
                  {{ item.observation ? 'Editar observação' : 'Adicionar observação' }}
                </summary>
                <input
                  v-model="item.observation"
                  class="field"
                  maxlength="200"
                  placeholder="Ex.: sem cebola"
                />
              </details>
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
            :disabled="!canReviewOrder"
            @click="openReview"
          >
            Revisar pedido
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

      <Transition name="fade">
        <div
          v-if="isReviewOpen"
          class="fixed inset-0 z-40 flex items-end justify-center bg-zinc-950/70 p-4 sm:items-center"
          role="presentation"
          @click.self="closeReview"
        >
          <section
            class="panel max-h-[90vh] w-full max-w-lg overflow-y-auto"
            role="dialog"
            aria-modal="true"
            aria-labelledby="review-title"
          >
            <div class="flex items-start justify-between gap-4">
              <div>
                <p class="text-xs font-semibold uppercase tracking-widest text-indigo-600 dark:text-indigo-400">
                  Confirmação
                </p>
                <h2 id="review-title" class="mt-1 text-xl font-bold">Revisar pedido</h2>
              </div>
              <button
                type="button"
                class="secondary min-h-11 min-w-11 px-3"
                aria-label="Fechar revisão do pedido"
                @click="closeReview"
              >
                ×
              </button>
            </div>

            <div class="mt-5 space-y-4 text-sm">
              <div class="rounded-xl bg-zinc-100 p-4 dark:bg-zinc-800">
                <span class="text-zinc-500 dark:text-zinc-400">Mesa / identificação</span>
                <strong class="mt-1 block text-base">{{ table }}</strong>
              </div>

              <ul class="space-y-3">
                <li
                  v-for="(item, index) in cart"
                  :key="`${item.dishId}-${index}`"
                  class="border-b border-zinc-200 pb-3 dark:border-zinc-800"
                >
                  <div class="flex justify-between gap-3">
                    <span>{{ item.quantity }}× {{ item.name }}</span>
                    <strong>{{ money(item.price * item.quantity) }}</strong>
                  </div>
                  <p v-if="item.observation" class="mt-1 text-xs text-zinc-500 dark:text-zinc-400">
                    Obs.: {{ item.observation }}
                  </p>
                </li>
              </ul>

              <div class="flex justify-between text-base font-bold">
                <span>Total</span>
                <span>{{ money(cartTotal) }}</span>
              </div>
            </div>

            <div class="mt-6 grid gap-3 sm:grid-cols-2">
              <button type="button" class="secondary min-h-11" :disabled="isSubmitting" @click="closeReview">
                Voltar e editar
              </button>
              <button type="button" class="primary min-h-11" :disabled="isSubmitting" @click="confirmOrder">
                {{ isSubmitting ? 'Enviando...' : 'Enviar pedido' }}
              </button>
            </div>
          </section>
        </div>
      </Transition>
    </template>

    <!-- PEDIDOS -->
    <template v-else>
      <section class="panel space-y-4">
        <div class="flex flex-wrap items-start justify-between gap-3">
          <div>
            <p class="text-xs font-semibold uppercase tracking-widest text-indigo-600 dark:text-indigo-400">
              Monitoramento operacional
            </p>
            <h2 class="mt-1 text-lg font-semibold">{{ activeOrderCount }} pedidos ativos</h2>
          </div>
          <span class="badge bg-indigo-100 text-indigo-700 dark:bg-indigo-950 dark:text-indigo-300">
            Prontos aparecem primeiro
          </span>
        </div>

        <div class="grid min-w-0 gap-3 lg:grid-cols-[minmax(0,1fr)_auto_auto] lg:items-end">
          <div class="min-w-0">
            <label for="waiter-order-search" class="field-label">Buscar pedido</label>
            <form class="search-form mt-2" role="search" @submit.prevent>
              <button type="submit" aria-label="Buscar pedido">
                <svg width="17" height="16" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
                  <path d="M7.667 12.667A5.333 5.333 0 107.667 2a5.333 5.333 0 000 10.667zM14.334 14l-2.9-2.9" stroke="currentColor" stroke-width="1.333" stroke-linecap="round" stroke-linejoin="round" />
                </svg>
              </button>
              <input
                id="waiter-order-search"
                v-model="search"
                class="search-input"
                type="search"
                placeholder="Mesa, #pedido, prato ou observação"
                autocomplete="off"
              />
              <button
                v-if="search"
                type="button"
                class="search-reset"
                aria-label="Limpar busca de pedidos"
                @click="search = ''"
              >
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </form>
            <p class="mt-1 text-xs text-zinc-500 dark:text-zinc-400">
              Busca instantânea com normalização de acentos.
            </p>
          </div>

          <label class="field-label min-w-0">
            Situação
            <select v-model="status" class="status-select field lg:min-w-40">
              <option value="Todos">Todos</option>
              <option v-for="item in statuses" :key="item" :value="item">
                {{ item }}
              </option>
            </select>
          </label>

          <label class="toggle-filter">
            <input v-model="showActiveOnly" type="checkbox" />
            <span class="toggle-track" aria-hidden="true"><span class="toggle-thumb" /></span>
            <span>Somente ativos</span>
          </label>
        </div>

        <p v-if="orderFeedback" role="status" class="rounded-xl border border-indigo-200 bg-indigo-50 p-3 text-sm text-indigo-800 dark:border-indigo-900 dark:bg-indigo-950/40 dark:text-indigo-200">
          {{ orderFeedback }}
        </p>
      </section>

      <section class="panel space-y-4">
        <div class="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 class="text-lg font-semibold">Simulação da cozinha</h2>
            <p class="text-sm text-zinc-500 dark:text-zinc-400">
              Controle demonstrativo separado das ações do garçom.
            </p>
          </div>
          <span class="badge bg-amber-100 text-amber-800 dark:bg-amber-950 dark:text-amber-300">
            Mock local
          </span>
        </div>

        <div v-if="!kitchenSimulationOrders.length" class="rounded-xl bg-zinc-100 p-4 text-sm text-zinc-500 dark:bg-zinc-800 dark:text-zinc-400">
          Nenhum pedido aguardando atualização da cozinha.
        </div>

        <ul v-else class="grid gap-3 md:grid-cols-2">
          <li
            v-for="order in kitchenSimulationOrders"
            :key="order.id"
            class="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-zinc-200 p-3 dark:border-zinc-800"
          >
            <div>
              <p class="font-semibold">Pedido #{{ order.id }} · Mesa {{ order.tableReference }}</p>
              <p class="text-xs text-zinc-500 dark:text-zinc-400">Estado atual: {{ order.status }}</p>
            </div>
            <button
              type="button"
              class="secondary min-h-11"
              :disabled="simulatingOrderId !== null"
              @click="simulateKitchen(order.id)"
            >
              {{ order.status === 'Enviado' ? 'Simular preparo' : 'Simular pronto' }}
            </button>
          </li>
        </ul>
      </section>

      <p v-if="!filteredOrders.length" class="panel text-sm text-zinc-500 dark:text-zinc-400">
        {{ showActiveOnly ? 'Nenhum pedido ativo encontrado com esses filtros.' : 'Nenhum pedido encontrado com esses filtros.' }}
      </p>

      <TransitionGroup
        v-else
        name="order-list"
        tag="section"
        class="grid gap-4 md:grid-cols-2 xl:grid-cols-3"
      >
        <article
          v-for="order in filteredOrders"
          :key="order.id"
          class="order-card panel flex min-w-0 flex-col"
          :class="[
            order.status === 'Pronto' ? 'order-card-ready' : '',
            statusChangingOrderId === order.id ? 'order-card-status-changing' : ''
          ]"
        >
          <div class="flex flex-wrap items-start justify-between gap-3">
            <div class="min-w-0">
              <h2 class="truncate text-lg font-semibold">Mesa {{ order.tableReference }}</h2>
              <p class="text-xs text-zinc-500 dark:text-zinc-400">Pedido #{{ order.id }}</p>
            </div>
            <span
              class="status-badge badge shrink-0"
              :class="[
                statusClasses[order.status],
                statusChangingOrderId === order.id ? 'status-badge-changing' : ''
              ]"
            >
              {{ order.status }}
            </span>
          </div>

          <div class="mt-4 flex items-center justify-between gap-3 text-sm">
            <span class="text-zinc-500 dark:text-zinc-400">{{ order.items.length }} linhas na comanda</span>
            <strong>{{ money(total(order.items)) }}</strong>
          </div>

          <details class="order-details mt-4">
            <summary class="cursor-pointer select-none text-sm font-semibold text-indigo-600 dark:text-indigo-400">
              Ver itens e observações
            </summary>
            <ul class="mt-3 space-y-3 text-sm">
              <li v-for="(item, index) in order.items" :key="index" class="border-b border-zinc-200 pb-3 last:border-0 dark:border-zinc-800">
                <div class="flex justify-between gap-3">
                  <span>{{ item.quantity }}× {{ item.name }}</span>
                  <span>{{ money(item.price * item.quantity) }}</span>
                </div>
                <p v-if="item.observation" class="mt-1 text-xs text-zinc-500 dark:text-zinc-400">
                  Obs.: {{ item.observation }}
                </p>
              </li>
            </ul>
          </details>

          <button
            v-if="order.status === 'Pronto'"
            type="button"
            class="primary mt-4 min-h-11 w-full"
            :disabled="deliveringOrderId !== null"
            @click="deliverOrder(order.id)"
          >
            {{ deliveringOrderId === order.id ? 'Confirmando...' : 'Confirmar entrega' }}
          </button>
          <p v-else-if="order.status !== 'Entregue'" class="mt-4 rounded-lg bg-zinc-100 p-3 text-xs text-zinc-600 dark:bg-zinc-800 dark:text-zinc-300">
            Aguardando atualização da cozinha.
          </p>
          <p v-else class="mt-4 text-xs text-zinc-500 dark:text-zinc-400">
            Entrega confirmada. Este pedido permanece no histórico.
          </p>
        </article>
      </TransitionGroup>
    </template>
  </main>
</template>

<style scoped>
.waiter-page {
  @apply mx-auto min-w-0 w-full max-w-7xl space-y-6 overflow-x-hidden px-4 py-8
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

.search-form {
  --timing: 0.3s;
  --height-of-input: 44px;
  --border-height: 2px;
  --input-bg: #fff;
  --border-color: #4f46e5;
  position: relative;
  display: flex;
  width: 100%;
  max-width: 100%;
  height: var(--height-of-input);
  align-items: center;
  padding-inline: 0.8em;
  border-radius: 30px;
  background: var(--input-bg);
  transition: border-radius 0.5s ease;
}

.dark .search-form {
  --input-bg: #27272a;
  --border-color: #818cf8;
}

.search-form button {
  border: none;
  background: none;
  color: #8b8ba7;
}

.search-form button:first-child {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 28px;
}

.search-form::before {
  position: absolute;
  bottom: 0;
  left: 0;
  width: 100%;
  height: var(--border-height);
  border-radius: 1px;
  background: var(--border-color);
  content: '';
  transform: scaleX(0);
  transform-origin: center;
  transition: transform var(--timing) ease;
}

.search-form:focus-within {
  border-radius: 1px;
}

.search-form:focus-within::before {
  transform: scale(1);
}

.search-input {
  width: 100%;
  height: 100%;
  border: none;
  background: transparent;
  padding-inline: 0.5em;
  padding-block: 0.7em;
  color: #18181b;
  font-size: 0.9rem;
}

.dark .search-input {
  color: #f4f4f5;
}

.search-input:focus {
  outline: none;
}

.search-input::placeholder {
  color: #a1a1aa;
}

.search-reset {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 28px;
  opacity: 1;
  visibility: visible;
}

.search-reset svg {
  width: 17px;
  height: 17px;
}

.dish-image {
  @apply border-b border-zinc-200 bg-zinc-100 dark:border-zinc-800
    dark:bg-zinc-800;
}

.dish-card {
  transition: transform 180ms ease, box-shadow 180ms ease;
}

.dish-card:hover {
  transform: translateY(-2px);
}

.dish-card-added {
  animation: dish-added 520ms ease;
}

@keyframes dish-added {
  0% { transform: scale(1); }
  45% { transform: scale(0.985); }
  100% { transform: scale(1); }
}

.cart-panel {
  min-width: 0;
}

.cart-list {
  max-height: min(52vh, 560px);
  overflow-y: auto;
  padding-right: 0.25rem;
}

.order-card {
  transition: transform 180ms ease, border-color 180ms ease, box-shadow 180ms ease;
}

.order-card-ready {
  border-color: rgb(16 185 129 / 0.45);
  box-shadow: 0 8px 24px rgb(16 185 129 / 0.08);
}

.order-card-status-changing {
  animation: order-status-change 600ms ease both;
}

.status-badge {
  transition: color 220ms ease, background-color 220ms ease, transform 220ms ease;
}

.status-badge-changing {
  animation: status-badge-change 600ms ease both;
}

@keyframes order-status-change {
  0% { transform: scale(1); }
  45% { transform: scale(1.012); }
  100% { transform: scale(1); }
}

@keyframes status-badge-change {
  0% { opacity: 0.55; transform: translateY(3px) scale(0.92); }
  55% { opacity: 1; transform: translateY(0) scale(1.05); }
  100% { opacity: 1; transform: translateY(0) scale(1); }
}

.status-select {
  cursor: pointer;
  transition: border-color 180ms ease, box-shadow 180ms ease, background-color 180ms ease;
}

.status-select:hover {
  border-color: rgb(99 102 241 / 0.7);
}

.status-select:focus {
  box-shadow: 0 0 0 3px rgb(99 102 241 / 0.16);
}

.toggle-filter {
  display: inline-flex;
  min-height: 44px;
  align-items: center;
  gap: 0.6rem;
  cursor: pointer;
  white-space: nowrap;
  font-size: 0.875rem;
  font-weight: 500;
}

.toggle-filter input {
  position: absolute;
  width: 1px;
  height: 1px;
  opacity: 0;
}

.toggle-track {
  position: relative;
  display: inline-flex;
  width: 2.75rem;
  height: 1.6rem;
  align-items: center;
  border-radius: 999px;
  background: #d4d4d8;
  padding: 0.2rem;
  transition: background-color 220ms ease, box-shadow 220ms ease;
}

.toggle-thumb {
  display: block;
  width: 1.2rem;
  height: 1.2rem;
  border-radius: 999px;
  background: white;
  box-shadow: 0 1px 3px rgb(0 0 0 / 0.2);
  transition: transform 220ms ease;
}

.toggle-filter input:checked + .toggle-track {
  background: #4f46e5;
  box-shadow: 0 0 0 3px rgb(99 102 241 / 0.14);
}

.toggle-filter input:checked + .toggle-track .toggle-thumb {
  transform: translateX(1.15rem);
}

.toggle-filter input:focus-visible + .toggle-track {
  outline: 2px solid #818cf8;
  outline-offset: 3px;
}

.order-details summary::marker {
  color: rgb(99 102 241);
}

.menu-details summary::marker {
  color: rgb(99 102 241);
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

.category-chip {
  @apply min-h-11 shrink-0 rounded-full border border-zinc-300 px-4 py-2
    text-sm font-medium text-zinc-600 transition-colors
    hover:bg-zinc-100 dark:border-zinc-700 dark:text-zinc-300
    dark:hover:bg-zinc-800;
}

.category-chip-active {
  @apply border-indigo-600 bg-indigo-600 text-white
    hover:bg-indigo-700 dark:border-indigo-500 dark:bg-indigo-600
    dark:hover:bg-indigo-500;
}

.filter-button {
  @apply inline-flex min-h-11 items-center justify-center rounded-lg
    border border-zinc-300 bg-white px-4 py-2 text-sm font-semibold
    text-zinc-600 dark:border-zinc-700 dark:bg-zinc-900 dark:text-zinc-300;
  transition: 400ms;
}

.filter-button:hover,
.filter-button:focus-visible {
  @apply border-indigo-500 bg-indigo-50 text-indigo-700
    dark:border-indigo-400 dark:bg-indigo-950/60 dark:text-indigo-300;
}

.filter-button-active {
  @apply border-indigo-600 bg-indigo-600 text-white shadow-sm
    dark:border-indigo-500 dark:bg-indigo-600;
}

.filter-button-active:hover,
.filter-button-active:focus-visible {
  @apply border-indigo-700 bg-indigo-700 text-white
    dark:border-indigo-400 dark:bg-indigo-500;
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

.fade-enter-active,
.fade-leave-active {
  transition: opacity 180ms ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

.dish-list-enter-active,
.dish-list-leave-active,
.dish-list-move {
  transition: opacity 180ms ease, transform 180ms ease;
}

.dish-list-enter-from,
.dish-list-leave-to {
  opacity: 0;
  transform: translateY(8px);
}

@media (prefers-reduced-motion: reduce) {
  .fade-enter-active,
  .fade-leave-active,
  .category-chip,
  .primary,
  .secondary,
  .search-form,
  .search-form::before,
  .filter-button,
  .dish-card,
  .dish-card-added,
  .order-card,
  .order-card-status-changing,
  .status-badge,
  .status-badge-changing,
  .status-select,
  .toggle-track,
  .toggle-thumb,
  .dish-list-enter-active,
  .dish-list-leave-active,
  .dish-list-move {
    transition: none;
    animation: none;
  }
}
</style>
