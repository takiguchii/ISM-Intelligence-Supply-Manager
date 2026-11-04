<script setup lang="ts">
import { financeService, FinanceEntryType, type FinanceEntry, type FinanceEntryRequest, type FinanceOverview } from "~/services/modules/finance/financeService";
import FinanceCashFlowChart from "~/components/finance/FinanceCashFlowChart.vue";
import FinanceExpenseChart from "~/components/finance/FinanceExpenseChart.vue";
import FinanceTopSellingChart from "~/components/finance/FinanceTopSellingChart.vue";

const overview = ref<FinanceOverview | null>(null);
const loading = ref(false);
const saving = ref(false);
const error = ref("");
const success = ref("");
const editingId = ref<number | null>(null);
const showForm = ref(false);
const pageNumber = ref(1);
const pageSize = 10;
const blankForm = (): FinanceEntryRequest => ({ description: "", category: "Outros", type: FinanceEntryType.Expense, amount: 0, dueDate: "", isSettled: false });
const form = ref<FinanceEntryRequest>(blankForm());
const money = (value: number) => value.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
const cards = computed(() => overview.value ? [
  { label: "Receita de pedidos pagos", value: overview.value.summary.salesRevenue },
  { label: "Outras receitas recebidas", value: overview.value.summary.otherIncome },
  { label: "Despesas pagas", value: overview.value.summary.expenses },
  { label: "Contas a receber", value: overview.value.summary.accountsReceivable },
  { label: "Contas a pagar", value: overview.value.summary.accountsPayable }
] : []);

async function refresh() {
  loading.value = true;
  error.value = "";
  try { overview.value = await financeService.get(pageNumber.value, pageSize); }
  catch (err: any) { error.value = err?.data?.detail || "Não foi possível carregar o financeiro."; }
  finally { loading.value = false; }
}
function openForm(entry?: FinanceEntry) {
  error.value = ""; success.value = ""; editingId.value = entry?.id ?? null;
  form.value = entry ? { description: entry.description, category: entry.category, type: entry.type, amount: entry.amount, dueDate: entry.dueDate.slice(0, 10), isSettled: !!entry.settledAtUtc } : blankForm();
  showForm.value = true;
}
async function changePage(target: number) {
  if (!overview.value?.entries || target < 1 || target > overview.value.entries.totalPages || target === pageNumber.value) return;
  pageNumber.value = target;
  await refresh();
}
async function save() {
  if (saving.value) return;
  saving.value = true; error.value = ""; success.value = "";
  try {
    if (editingId.value === null) await financeService.create(form.value);
    else await financeService.update(editingId.value, form.value);
    showForm.value = false; success.value = "Movimentação salva.";
    await refresh();
  } catch (err: any) { error.value = err?.data?.detail || err?.data?.title || "Não foi possível salvar a movimentação."; }
  finally { saving.value = false; }
}
async function remove(entry: FinanceEntry) {
  if (saving.value || !window.confirm(`Excluir a movimentação “${entry.description}”?`)) return;
  saving.value = true; error.value = ""; success.value = "";
  try { await financeService.delete(entry.id); success.value = "Movimentação excluída."; await refresh(); }
  catch (err: any) { error.value = err?.data?.detail || "Não foi possível excluir a movimentação."; }
  finally { saving.value = false; }
}
onMounted(refresh);
</script>

<template>
  <div class="max-w-7xl w-full mx-auto px-4 sm:px-6 py-8 space-y-6">
    <div class="flex flex-wrap items-center justify-between gap-4">
      <div><h1 class="text-2xl font-bold dark:text-white">Financeiro</h1><p class="text-sm text-zinc-500">Receitas e despesas do mês atual; contas pendentes de todos os períodos.</p></div>
      <div class="flex gap-2"><button class="px-4 py-2 rounded-xl border border-zinc-300 dark:border-zinc-700 text-sm" :disabled="loading" @click="refresh">Atualizar</button><button class="px-4 py-2 rounded-xl bg-indigo-600 text-white text-sm" @click="openForm()">Nova movimentação</button></div>
    </div>
    <p v-if="error" role="alert" class="p-4 rounded-xl bg-red-50 dark:bg-red-950/40 text-red-700 dark:text-red-300">{{ error }}</p>
    <p v-if="success" role="status" class="text-sm text-emerald-600">{{ success }}</p>
    <p v-if="loading && !overview" class="text-sm text-zinc-500">Carregando financeiro...</p>
    <div v-if="overview" class="grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
      <div v-for="card in cards" :key="card.label" class="rounded-2xl border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-900 p-5"><p class="text-xs text-zinc-500">{{ card.label }}</p><p class="mt-2 text-xl font-bold dark:text-white">{{ money(card.value) }}</p></div>
    </div>
    <section v-if="overview" class="grid gap-5 lg:grid-cols-2">
      <FinanceExpenseChart :data="overview.analytics.expensesByCategory" />
      <FinanceTopSellingChart :data="overview.analytics.topSellingItems" />
      <div class="lg:col-span-2"><FinanceCashFlowChart :data="overview.analytics.cashFlow" /></div>
    </section>
    <section v-if="showForm" class="rounded-2xl border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-900 p-5">
      <h2 class="font-bold dark:text-white">{{ editingId === null ? 'Nova movimentação' : 'Editar movimentação' }}</h2>
      <form class="mt-4 grid gap-4 sm:grid-cols-2" @submit.prevent="save">
        <label class="text-sm dark:text-zinc-200">Descrição<input v-model="form.description" required maxlength="150" class="mt-1 block w-full rounded-lg border border-zinc-300 dark:border-zinc-700 bg-transparent p-2" /></label>
        <label class="text-sm dark:text-zinc-200">Categoria<input v-model="form.category" required maxlength="80" placeholder="Ex.: Insumos, Pessoal, Utilidades" class="mt-1 block w-full rounded-lg border border-zinc-300 dark:border-zinc-700 bg-transparent p-2" /></label>
        <label class="text-sm dark:text-zinc-200">Tipo<select v-model.number="form.type" class="mt-1 block w-full rounded-lg border border-zinc-300 dark:border-zinc-700 bg-white dark:bg-zinc-900 p-2"><option :value="FinanceEntryType.Income">Receita avulsa</option><option :value="FinanceEntryType.Expense">Despesa</option></select></label>
        <label class="text-sm dark:text-zinc-200">Valor<input v-model.number="form.amount" required type="number" min="0.01" max="99999999.99" step="0.01" class="mt-1 block w-full rounded-lg border border-zinc-300 dark:border-zinc-700 bg-transparent p-2" /></label>
        <label class="text-sm dark:text-zinc-200">Vencimento<input v-model="form.dueDate" required type="date" class="mt-1 block w-full rounded-lg border border-zinc-300 dark:border-zinc-700 bg-transparent p-2" /></label>
        <label class="flex items-center gap-2 text-sm dark:text-zinc-200"><input v-model="form.isSettled" type="checkbox" /> Recebida ou paga</label>
        <div class="flex gap-2 sm:col-span-2"><button type="submit" :disabled="saving" class="px-4 py-2 rounded-lg bg-indigo-600 text-white disabled:opacity-50">Salvar</button><button type="button" class="px-4 py-2 rounded-lg border border-zinc-300 dark:border-zinc-700" @click="showForm = false">Cancelar</button></div>
      </form>
    </section>
    <section v-if="overview" class="rounded-2xl border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-900 p-5 space-y-4">
      <h2 class="font-bold dark:text-white">Movimentações avulsas</h2>
      <p v-if="!overview.entries.items.length" class="text-sm text-zinc-500">Nenhuma movimentação cadastrada.</p>
      <div v-for="entry in overview.entries.items" :key="entry.id" class="flex flex-wrap items-center justify-between gap-3 border-t border-zinc-200 dark:border-zinc-800 pt-4">
        <div><p class="font-semibold dark:text-white">{{ entry.description }}</p><p class="text-xs text-zinc-500">{{ entry.category }} · {{ entry.type === FinanceEntryType.Income ? 'Receita' : 'Despesa' }} · vencimento {{ entry.dueDate.slice(0, 10).split('-').reverse().join('/') }} · {{ entry.settledAtUtc ? 'Quitada' : 'Pendente' }}</p></div>
        <div class="flex items-center gap-3"><span class="font-bold dark:text-white">{{ money(entry.amount) }}</span><button class="text-xs text-indigo-600" @click="openForm(entry)">Editar</button><button class="text-xs text-red-600" :disabled="saving" @click="remove(entry)">Excluir</button></div>
      </div>
      <div v-if="overview.entries.totalPages > 1" class="flex items-center justify-between border-t border-zinc-200 dark:border-zinc-800 pt-4 text-sm">
        <span class="text-zinc-500">{{ overview.entries.totalCount }} movimentações · página {{ overview.entries.pageNumber }} de {{ overview.entries.totalPages }}</span>
        <div class="flex gap-2"><button class="rounded-lg border border-zinc-300 dark:border-zinc-700 px-3 py-1.5 disabled:opacity-40" :disabled="!overview.entries.hasPreviousPage || loading" @click="changePage(pageNumber - 1)">Anterior</button><button class="rounded-lg border border-zinc-300 dark:border-zinc-700 px-3 py-1.5 disabled:opacity-40" :disabled="!overview.entries.hasNextPage || loading" @click="changePage(pageNumber + 1)">Próxima</button></div>
      </div>
    </section>
  </div>
</template>
