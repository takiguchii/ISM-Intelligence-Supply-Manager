<script setup lang="ts">
import { orderService, OrderStatus, type Order, type OrderPage, type UpdateOrder } from "~/services/modules/orders/orderService";

const page = ref<OrderPage | null>(null);
const loading = ref(false);
const saving = ref(false);
const error = ref("");
const success = ref("");
const editing = ref<Order | null>(null);
const editForm = ref<UpdateOrder>({ tableReference: "", items: [], version: 0 });

// Paginação & Busca Inteligente
const pageNumber = ref(1);
const pageSize = 10;
const searchQuery = ref("");
const statusFilter = ref<string>("");
let searchTimer: any = null;

const labels: Record<OrderStatus, string> = {
  [OrderStatus.Submitted]: "Enviado",
  [OrderStatus.Preparing]: "Em preparo",
  [OrderStatus.Ready]: "Pronto",
  [OrderStatus.Completed]: "Entregue",
  [OrderStatus.Paid]: "Pago",
  [OrderStatus.Cancelled]: "Cancelado"
};

const statusClasses: Record<OrderStatus, string> = {
  [OrderStatus.Submitted]: "bg-blue-500/10 text-blue-400 border border-blue-500/20",
  [OrderStatus.Preparing]: "bg-amber-500/10 text-amber-400 border border-amber-500/20",
  [OrderStatus.Ready]: "bg-purple-500/10 text-purple-400 border border-purple-500/20",
  [OrderStatus.Completed]: "bg-teal-500/10 text-teal-400 border border-teal-500/20",
  [OrderStatus.Paid]: "bg-indigo-500/15 text-indigo-400 border border-indigo-500/30",
  [OrderStatus.Cancelled]: "bg-rose-500/10 text-rose-400 border border-rose-500/20"
};

const money = (value: number) =>
  value.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });

const total = (order: Order) =>
  order.items.reduce((sum, item) => sum + item.unitPrice * item.quantity, 0);

async function refresh(targetPage = pageNumber.value) {
  loading.value = true;
  error.value = "";
  try {
    pageNumber.value = targetPage;
    page.value = await orderService.adminList(
      targetPage,
      pageSize,
      searchQuery.value.trim()
    );
  } catch (err: any) {
    error.value = err?.data?.detail || "Não foi possível carregar os pedidos administrativos.";
  } finally {
    loading.value = false;
  }
}

function onSearchInput() {
  if (searchTimer) clearTimeout(searchTimer);
  searchTimer = setTimeout(() => {
    refresh(1);
  }, 350);
}

function clearSearch() {
  searchQuery.value = "";
  refresh(1);
}

const filteredItems = computed(() => {
  if (!page.value) return [];
  if (!statusFilter.value) return page.value.items;
  return page.value.items.filter(order => String(order.status) === statusFilter.value);
});

function nextStatus(order: Order): { status: OrderStatus; label: string } | null {
  const next: Partial<Record<OrderStatus, OrderStatus>> = {
    [OrderStatus.Submitted]: OrderStatus.Preparing,
    [OrderStatus.Preparing]: OrderStatus.Ready,
    [OrderStatus.Ready]: OrderStatus.Completed,
    [OrderStatus.Completed]: OrderStatus.Paid
  };
  const status = next[order.status];
  return status === undefined
    ? null
    : {
        status,
        label:
          status === OrderStatus.Paid
            ? "Registrar pagamento"
            : `Marcar ${labels[status].toLowerCase()}`
      };
}

async function changeStatus(order: Order, status: OrderStatus) {
  if (saving.value) return;
  if (status === OrderStatus.Paid && !window.confirm(`Confirmar recebimento de ${money(total(order))}?`)) return;
  if (status === OrderStatus.Cancelled && !window.confirm(`Cancelar o pedido #${order.id}?`)) return;
  saving.value = true;
  error.value = "";
  try {
    await orderService.updateStatus(order, status);
    success.value = "Status do pedido atualizado.";
    await refresh();
  } catch (err: any) {
    error.value = err?.data?.detail || "Não foi possível atualizar o status.";
  } finally {
    saving.value = false;
  }
}

function openEdit(order: Order) {
  editing.value = order;
  editForm.value = {
    tableReference: order.tableReference,
    version: order.version,
    items: order.items.map(item => ({
      dishId: item.dishId,
      quantity: item.quantity,
      observation: item.observation
    }))
  };
}

async function saveEdit() {
  if (!editing.value || saving.value) return;
  saving.value = true;
  error.value = "";
  try {
    await orderService.update(editForm.value, editing.value.id);
    editing.value = null;
    success.value = "Pedido atualizado com sucesso.";
    await refresh();
  } catch (err: any) {
    error.value = err?.data?.detail || "Não foi possível editar o pedido.";
  } finally {
    saving.value = false;
  }
}

async function remove(order: Order) {
  if (
    saving.value ||
    !window.confirm(
      `Excluir permanentemente o pedido #${order.id}? Esta ação afeta os indicadores financeiros.`
    )
  )
    return;
  saving.value = true;
  error.value = "";
  try {
    await orderService.delete(order.id);
    success.value = "Pedido excluído.";
    await refresh(page.value?.pageNumber ?? 1);
  } catch (err: any) {
    error.value = err?.data?.detail || "Não foi possível excluir o pedido.";
  } finally {
    saving.value = false;
  }
}

onMounted(() => refresh());
</script>

<template>
  <div class="max-w-7xl mx-auto w-full px-4 sm:px-6 py-8 space-y-6">
    <!-- Header -->
    <div class="flex flex-wrap items-center justify-between gap-4">
      <div>
        <h1 class="text-2xl font-bold text-white tracking-tight">Pedidos</h1>
        <p class="text-sm text-zinc-400 mt-0.5">
          Consulta administrativa de pedidos, autores, datas e valores.
        </p>
      </div>

      <button
        type="button"
        :disabled="loading"
        class="inline-flex items-center gap-2 px-4 py-2 rounded-xl border border-zinc-700 bg-zinc-800/80 hover:bg-zinc-700 text-sm font-semibold text-zinc-200 transition-all duration-200 hover:scale-105 active:scale-95 disabled:opacity-50 shadow-sm"
        @click="refresh()"
      >
        <svg
          xmlns="http://www.w3.org/2000/svg"
          class="w-4 h-4 transition-transform duration-500"
          :class="{ 'animate-spin': loading }"
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
        >
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"
          />
        </svg>
        <span>Atualizar</span>
      </button>
    </div>

    <!-- Barra de Controle: Search Bar Inteligente + Filtro de Status -->
    <div class="p-4 rounded-2xl border border-zinc-800 bg-zinc-900/90 backdrop-blur-md shadow-lg flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-4">
      <!-- Search bar inteligente -->
      <div class="relative flex-1">
        <div class="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3.5 text-zinc-500">
          <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
        </div>
        <input
          v-model="searchQuery"
          @input="onSearchInput"
          type="text"
          placeholder="Buscar por #pedido, mesa, comanda ou prato..."
          class="w-full rounded-xl border border-zinc-800 bg-zinc-950/80 py-2.5 pl-10 pr-9 text-sm text-white placeholder:text-zinc-500 outline-none transition-all duration-200 focus:border-zinc-600 focus:bg-zinc-950 focus:ring-2 focus:ring-zinc-700/50"
        />
        <button
          v-if="searchQuery"
          type="button"
          @click="clearSearch"
          class="absolute inset-y-0 right-0 flex items-center pr-3 text-zinc-500 hover:text-zinc-300 transition-colors"
          title="Limpar busca"
        >
          <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4" viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd" />
          </svg>
        </button>
      </div>

      <!-- Filtro de status & Contador -->
      <div class="flex items-center gap-3">
        <select
          v-model="statusFilter"
          class="h-10 rounded-xl border border-zinc-800 bg-zinc-950/80 px-3 py-2 text-xs font-semibold text-zinc-300 outline-none transition-all duration-200 hover:border-zinc-700 focus:border-zinc-600 cursor-pointer"
        >
          <option value="">Todos os status</option>
          <option v-for="(lbl, key) in labels" :key="key" :value="String(key)">
            {{ lbl }}
          </option>
        </select>

        <span v-if="page" class="hidden md:inline-flex items-center px-3 py-2 rounded-xl bg-zinc-800/60 border border-zinc-800 text-xs font-medium text-zinc-400">
          {{ page.totalCount }} pedidos
        </span>
      </div>
    </div>

    <!-- Alertas -->
    <p v-if="error" role="alert" class="p-4 rounded-xl bg-rose-950/40 border border-rose-900/60 text-rose-300 text-sm">
      {{ error }}
    </p>
    <p v-if="success" role="status" class="p-3 rounded-xl bg-emerald-950/40 border border-emerald-900/60 text-emerald-300 text-sm">
      {{ success }}
    </p>

    <!-- Tabela com Transição Suave (Fade) -->
    <section class="rounded-2xl border border-zinc-800 bg-zinc-950 shadow-xl overflow-hidden relative">
      <!-- Loading indicator no topo -->
      <div v-if="loading" class="absolute top-0 left-0 right-0 h-0.5 bg-indigo-500 animate-pulse z-10"></div>

      <transition name="page-fade" mode="out-in">
        <div :key="pageNumber + '-' + searchQuery + '-' + statusFilter" class="overflow-x-auto">
          <div v-if="!filteredItems.length && !loading" class="p-12 text-center text-sm text-zinc-500">
            <svg xmlns="http://www.w3.org/2000/svg" class="w-10 h-10 mx-auto text-zinc-600 mb-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
            </svg>
            <p class="font-medium text-zinc-400">Nenhum pedido encontrado.</p>
            <p class="text-xs text-zinc-600 mt-1">Tente ajustar a busca ou os filtros de status.</p>
          </div>

          <table v-else class="w-full text-left text-sm border-collapse">
            <thead class="bg-zinc-900/80 border-b border-zinc-800/80 text-[11px] font-bold uppercase tracking-wider text-zinc-400">
              <tr>
                <th class="px-5 py-4">Pedido</th>
                <th class="px-5 py-4">Quem fez</th>
                <th class="px-5 py-4">Data e hora</th>
                <th class="px-5 py-4">Itens</th>
                <th class="px-5 py-4">Total</th>
                <th class="px-5 py-4 text-center">Status</th>
                <th class="px-5 py-4 text-right">Ações</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-zinc-900">
              <tr
                v-for="order in filteredItems"
                :key="order.id"
                class="align-top text-zinc-200 transition-colors duration-200 hover:bg-zinc-900/60"
              >
                <!-- Pedido -->
                <td class="px-5 py-4 whitespace-nowrap">
                  <span class="font-bold text-white text-base block">#{{ order.id }}</span>
                  <span class="text-xs text-zinc-400 font-medium">{{ order.tableReference }}</span>
                </td>

                <!-- Quem fez -->
                <td class="px-5 py-4 text-zinc-300">
                  <span class="inline-block">{{ order.createdByName || 'Não identificado' }}</span>
                </td>

                <!-- Data e hora -->
                <td class="px-5 py-4 whitespace-nowrap text-zinc-400 text-xs">
                  {{ new Date(order.createdAtUtc).toLocaleString('pt-BR') }}
                </td>

                <!-- Itens -->
                <td class="px-5 py-4 min-w-[200px]">
                  <div class="space-y-1">
                    <div
                      v-for="item in order.items"
                      :key="`${order.id}-${item.dishId}`"
                      class="text-xs font-medium text-zinc-300"
                    >
                      <span class="text-zinc-500 font-semibold">{{ item.quantity }}×</span>
                      {{ item.dishName }}
                    </div>
                  </div>
                </td>

                <!-- Total -->
                <td class="px-5 py-4 whitespace-nowrap font-bold text-white">
                  {{ money(total(order)) }}
                </td>

                <!-- Status -->
                <td class="px-5 py-4 text-center whitespace-nowrap">
                  <span
                    :class="[
                      'inline-block px-3 py-1 rounded-full text-xs font-semibold shadow-sm transition-all duration-200',
                      statusClasses[order.status] || 'bg-zinc-800 text-zinc-300'
                    ]"
                  >
                    {{ labels[order.status] }}
                  </span>
                </td>

                <!-- Ações com botões animados e vívidos -->
                <td class="px-5 py-4 text-right whitespace-nowrap">
                  <div class="inline-flex items-center justify-end gap-2">
                    <!-- Botão de Próximo Status (se houver) -->
                    <button
                      v-if="nextStatus(order)"
                      type="button"
                      :disabled="saving"
                      class="px-3 py-1.5 rounded-lg bg-indigo-600 hover:bg-indigo-500 active:scale-95 text-xs font-semibold text-white shadow-sm shadow-indigo-600/30 transition-all duration-200 disabled:opacity-50"
                      @click="changeStatus(order, nextStatus(order)!.status)"
                    >
                      {{ nextStatus(order)!.label }}
                    </button>

                    <!-- Cancelar -->
                    <button
                      v-if="![OrderStatus.Paid, OrderStatus.Cancelled].includes(order.status)"
                      type="button"
                      :disabled="saving"
                      class="px-2.5 py-1.5 rounded-lg border border-amber-800/60 bg-amber-950/20 hover:bg-amber-900/40 text-amber-300 text-xs font-semibold transition-all duration-200 hover:scale-105 active:scale-95 disabled:opacity-50"
                      @click="changeStatus(order, OrderStatus.Cancelled)"
                    >
                      Cancelar
                    </button>

                    <!-- Editar -->
                    <button
                      v-if="order.status !== OrderStatus.Cancelled"
                      type="button"
                      :disabled="saving"
                      class="px-2.5 py-1.5 rounded-lg border border-zinc-700 bg-zinc-800/80 hover:bg-zinc-700 text-zinc-200 text-xs font-semibold transition-all duration-200 hover:scale-105 active:scale-95 disabled:opacity-50"
                      @click="openEdit(order)"
                    >
                      Editar
                    </button>

                    <!-- Excluir -->
                    <button
                      type="button"
                      :disabled="saving"
                      class="px-2.5 py-1.5 rounded-lg border border-rose-900/50 bg-rose-950/20 hover:bg-rose-900/40 text-rose-400 hover:text-rose-300 text-xs font-semibold transition-all duration-200 hover:scale-105 active:scale-95 disabled:opacity-50"
                      @click="remove(order)"
                    >
                      Excluir
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </transition>

      <!-- Paginação Elegante (10 itens por página com botões vívidos) -->
      <div
        v-if="page"
        class="flex flex-col sm:flex-row items-center justify-between gap-4 border-t border-zinc-800/80 bg-zinc-900/60 px-6 py-4 text-xs font-medium text-zinc-400"
      >
        <div>
          Exibindo até <strong class="text-zinc-200">{{ pageSize }}</strong> itens por página ·
          Total de <strong class="text-zinc-200">{{ page.totalCount }}</strong> pedidos
        </div>

        <div class="flex items-center gap-2">
          <span class="mr-2">
            Página <strong class="text-white">{{ page.pageNumber }}</strong> de
            <strong class="text-white">{{ page.totalPages || 1 }}</strong>
          </span>

          <button
            :disabled="!page.hasPreviousPage || loading"
            class="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl border border-zinc-700 bg-zinc-800/90 hover:bg-zinc-700 text-zinc-200 font-semibold transition-all duration-200 hover:scale-105 active:scale-95 disabled:opacity-40 disabled:cursor-not-allowed disabled:hover:scale-100 shadow-sm"
            @click="refresh(page.pageNumber - 1)"
          >
            <svg xmlns="http://www.w3.org/2000/svg" class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
            </svg>
            <span>Anterior</span>
          </button>

          <button
            :disabled="!page.hasNextPage || loading"
            class="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl border border-zinc-700 bg-zinc-800/90 hover:bg-zinc-700 text-zinc-200 font-semibold transition-all duration-200 hover:scale-105 active:scale-95 disabled:opacity-40 disabled:cursor-not-allowed disabled:hover:scale-100 shadow-sm"
            @click="refresh(page.pageNumber + 1)"
          >
            <span>Próxima</span>
            <svg xmlns="http://www.w3.org/2000/svg" class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
            </svg>
          </button>
        </div>
      </div>
    </section>

    <!-- Modal de Edição -->
    <div
      v-if="editing"
      class="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-sm p-4"
      @click.self="editing = null"
    >
      <form
        class="w-full max-w-xl space-y-5 rounded-2xl border border-zinc-800 bg-zinc-950 p-6 shadow-2xl"
        @submit.prevent="saveEdit"
      >
        <div class="flex items-center justify-between border-b border-zinc-800 pb-4">
          <div>
            <h2 class="text-lg font-bold text-white">Editar pedido #{{ editing.id }}</h2>
            <p class="text-xs text-zinc-400 mt-0.5">Ajuste os itens ou a identificação da comanda</p>
          </div>
          <button
            type="button"
            class="h-8 w-8 rounded-lg bg-zinc-800 text-zinc-400 hover:text-white flex items-center justify-center transition"
            @click="editing = null"
          >
            ✕
          </button>
        </div>

        <label class="block text-sm text-zinc-300">
          Mesa / Comanda
          <input
            v-model="editForm.tableReference"
            required
            maxlength="100"
            class="mt-1.5 w-full rounded-xl border border-zinc-800 bg-zinc-900 p-2.5 text-sm text-white outline-none focus:border-zinc-600"
          />
        </label>

        <div class="space-y-3">
          <p class="text-sm font-semibold text-white">Itens do Pedido</p>
          <div
            v-for="(item, index) in editForm.items"
            :key="item.dishId"
            class="grid grid-cols-[1fr_100px] gap-3 items-center"
          >
            <div class="rounded-xl border border-zinc-800 bg-zinc-900/60 p-2.5 text-xs font-semibold text-zinc-200 truncate">
              {{ editing.items[index]?.dishName }}
            </div>
            <input
              v-model.number="item.quantity"
              required
              min="1"
              max="1000"
              type="number"
              class="rounded-xl border border-zinc-800 bg-zinc-900 p-2.5 text-sm text-white text-center outline-none focus:border-zinc-600"
            />
          </div>
        </div>

        <div class="flex justify-end gap-3 pt-4 border-t border-zinc-800">
          <button
            type="button"
            class="rounded-xl border border-zinc-700 bg-zinc-900 px-4 py-2 text-sm font-semibold text-zinc-300 hover:bg-zinc-800 transition"
            @click="editing = null"
          >
            Cancelar
          </button>
          <button
            type="submit"
            :disabled="saving"
            class="rounded-xl bg-indigo-600 hover:bg-indigo-500 px-5 py-2 text-sm font-semibold text-white transition-all duration-200 active:scale-95 disabled:opacity-50 shadow-md shadow-indigo-600/30"
          >
            {{ saving ? 'Salvando...' : 'Salvar alterações' }}
          </button>
        </div>
      </form>
    </div>
  </div>
</template>

<style scoped>
/* Transição leve de desvanecimento (fade) entre páginas e buscas */
.page-fade-enter-active,
.page-fade-leave-active {
  transition: opacity 0.22s ease-in-out, transform 0.22s ease-in-out;
}

.page-fade-enter-from {
  opacity: 0;
  transform: translateY(4px);
}

.page-fade-leave-to {
  opacity: 0;
  transform: translateY(-4px);
}
</style>
