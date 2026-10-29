<script setup lang="ts">
import { orderService, OrderStatus, type Order } from "~/services/modules/orders/orderService";

const props = withDefaults(defineProps<{ kitchen?: boolean }>(), { kitchen: false });
const authStore = useAuthStore();
const orders = ref<Order[]>([]);
const loading = ref(false);
const error = ref("");
const updatingId = ref<number | null>(null);
const labels: Record<OrderStatus, string> = {
  [OrderStatus.Submitted]: "Enviado", [OrderStatus.Preparing]: "Em preparo", [OrderStatus.Ready]: "Pronto",
  [OrderStatus.Completed]: "Entregue", [OrderStatus.Paid]: "Pago", [OrderStatus.Cancelled]: "Cancelado"
};
const visibleOrders = computed(() => props.kitchen
  ? orders.value.filter(order => [OrderStatus.Submitted, OrderStatus.Preparing, OrderStatus.Ready].includes(order.status))
  : orders.value);
const money = (value: number) => value.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
const total = (order: Order) => order.items.reduce((sum, item) => sum + item.unitPrice * item.quantity, 0);

async function refresh() {
  loading.value = true;
  error.value = "";
  try { orders.value = await orderService.list(); }
  catch (err: any) { error.value = err?.data?.detail || "Não foi possível carregar os pedidos."; }
  finally { loading.value = false; }
}

function actions(order: Order): { status: OrderStatus; label: string }[] {
  const role = authStore.currentUser?.role;
  const manager = role === "Admin" || role === "Manager";
  const kitchen = manager || role === "Chef";
  const service = manager || role === "Waiter";
  const result: { status: OrderStatus; label: string }[] = [];
  if (kitchen && order.status === OrderStatus.Submitted) result.push({ status: OrderStatus.Preparing, label: "Iniciar preparo" });
  if (kitchen && order.status === OrderStatus.Preparing) result.push({ status: OrderStatus.Ready, label: "Marcar pronto" });
  if (service && order.status === OrderStatus.Ready) result.push({ status: OrderStatus.Completed, label: "Marcar entregue" });
  if (service && order.status === OrderStatus.Completed) result.push({ status: OrderStatus.Paid, label: "Registrar pagamento" });
  if (service && [OrderStatus.Submitted, OrderStatus.Preparing, OrderStatus.Ready, OrderStatus.Completed].includes(order.status)) result.push({ status: OrderStatus.Cancelled, label: "Cancelar pedido" });
  return result;
}

async function update(order: Order, status: OrderStatus) {
  if (updatingId.value !== null) return;
  if (status === OrderStatus.Cancelled && !window.confirm(`Cancelar o pedido #${order.id}?`)) return;
  if (status === OrderStatus.Paid && !window.confirm(`Confirmar recebimento de ${money(total(order))} para o pedido #${order.id}?`)) return;
  updatingId.value = order.id;
  error.value = "";
  try {
    const updated = await orderService.updateStatus(order, status);
    orders.value = orders.value.map(item => item.id === updated.id ? updated : item);
  } catch (err: any) { error.value = err?.data?.detail || "Não foi possível atualizar o pedido. Atualize a lista e tente novamente."; }
  finally { updatingId.value = null; }
}

onMounted(refresh);
</script>

<template>
  <div class="max-w-7xl mx-auto w-full px-4 sm:px-6 py-8 space-y-6">
    <div class="flex flex-wrap items-center justify-between gap-4">
      <div>
        <h1 class="text-2xl font-bold dark:text-white">{{ kitchen ? 'Cozinha' : 'Pedidos' }}</h1>
        <p class="text-sm text-zinc-500 dark:text-zinc-400">{{ kitchen ? 'Acompanhe o preparo dos pedidos.' : 'Acompanhe as comandas do restaurante.' }}</p>
      </div>
      <div class="flex gap-3">
        <NuxtLink v-if="authStore.currentUser?.role !== 'Chef'" to="/atendimento" class="px-4 py-2 rounded-xl bg-indigo-600 text-white text-sm">Novo atendimento</NuxtLink>
        <button type="button" :disabled="loading" class="px-4 py-2 rounded-xl border border-zinc-300 dark:border-zinc-700 text-sm" @click="refresh">Atualizar</button>
      </div>
    </div>
    <p v-if="error" role="alert" class="p-4 rounded-xl bg-red-50 dark:bg-red-950/40 text-red-700 dark:text-red-300">{{ error }}</p>
    <p v-if="loading && !orders.length" class="text-sm text-zinc-500">Carregando pedidos...</p>
    <p v-else-if="!error && !visibleOrders.length" class="text-sm text-zinc-500">Nenhum pedido encontrado.</p>
    <div v-else class="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
      <article v-for="order in visibleOrders" :key="order.id" class="rounded-2xl border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-900 p-5 space-y-4">
        <div class="flex justify-between gap-3"><div><h2 class="font-bold dark:text-white">Comanda {{ order.tableReference }}</h2><p class="text-xs text-zinc-500">Pedido #{{ order.id }} · {{ new Date(order.createdAtUtc).toLocaleString('pt-BR') }}</p></div><span class="text-xs font-semibold text-indigo-600 dark:text-indigo-400">{{ labels[order.status] }}</span></div>
        <ul class="space-y-2 text-sm dark:text-zinc-200"><li v-for="(item, index) in order.items" :key="index">{{ item.quantity }}× {{ item.dishName }} <span v-if="item.observation" class="block text-xs text-zinc-500">{{ item.observation }}</span></li></ul>
        <p class="font-bold dark:text-white">{{ money(total(order)) }}</p>
        <div class="flex flex-wrap gap-2"><button v-for="action in actions(order)" :key="action.status" :disabled="updatingId !== null" type="button" class="px-3 py-2 rounded-lg text-xs font-semibold bg-indigo-600 text-white disabled:opacity-50" @click="update(order, action.status)">{{ action.label }}</button></div>
      </article>
    </div>
  </div>
</template>
