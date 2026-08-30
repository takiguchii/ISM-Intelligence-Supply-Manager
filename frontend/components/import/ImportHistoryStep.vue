<script setup lang="ts">
import { ref, computed, watch, onMounted } from "vue";
import {
  importService,
  type ImportAuditDto,
  type ImportAuditDetailDto
} from "~/services/modules/importService";

const props = defineProps<{ restaurantId: number | null; refreshToken: number }>();

const history = ref<ImportAuditDto[]>([]);
const loading = ref(false);

// ===== Filtros =====
const PAGE_SIZE = 10;
const page = ref(1);
const filterEntity = ref<string>("todos");
const filterStatus = ref<string>("todos");
const searchText = ref("");

const entityOptions = computed(() => {
  const set = new Set(history.value.map((h) => h.targetEntity));
  return [...set];
});
const hasActiveFilters = computed(
  () => filterEntity.value !== "todos" || filterStatus.value !== "todos" || searchText.value.trim() !== ""
);

const filtered = computed(() =>
  history.value.filter((h) => {
    if (filterEntity.value !== "todos" && h.targetEntity !== filterEntity.value) return false;
    if (filterStatus.value === "ok" && h.recordsFailed > 0) return false;
    if (filterStatus.value === "erros" && h.recordsFailed === 0) return false;
    const q = searchText.value.trim().toLowerCase();
    if (q && !`${h.dataSourceName} ${h.dataSourceType}`.toLowerCase().includes(q)) return false;
    return true;
  })
);

const totalPages = computed(() => Math.max(1, Math.ceil(filtered.value.length / PAGE_SIZE)));
const paged = computed(() => filtered.value.slice((page.value - 1) * PAGE_SIZE, page.value * PAGE_SIZE));

watch([filterEntity, filterStatus, searchText], () => (page.value = 1));
watch(filtered, () => {
  if (page.value > totalPages.value) page.value = totalPages.value;
});

const loadHistory = async () => {
  loading.value = true;
  try {
    const restId = props.restaurantId;
    history.value = (await importService.history(100, restId)) || [];
  } finally {
    loading.value = false;
  }
};
watch(() => props.refreshToken, loadHistory);

// ===== Modal de detalhes =====
const modalOpen = ref(false);
const loadingDetail = ref(false);
const detail = ref<ImportAuditDetailDto | null>(null);
const detailErrorsPage = ref(1);

const openDetail = async (id: string) => {
  modalOpen.value = true;
  loadingDetail.value = true;
  detailErrorsPage.value = 1;
  detail.value = null;
  try {
    detail.value = await importService.detail(id);
  } finally {
    loadingDetail.value = false;
  }
};

onMounted(loadHistory);

const formatDate = (s?: string | null) => {
  if (!s) return "-";
  return new Date(s).toLocaleString("pt-BR", { timeZone: "America/Sao_Paulo" });
};
const statusClass = (recordsFailed: number) =>
  recordsFailed === 0
    ? "bg-emerald-500/10 text-emerald-300 border-emerald-500/20"
    : "bg-amber-500/10 text-amber-300 border-amber-500/20";
const entityLabel = (e: string) =>
  ({
    Product: "Estoque / Produtos",
    Fornecedor: "Fornecedores",
    Category: "Categorias",
    Dish: "Pratos",
    SalesOrder: "Pedidos / Vendas"
  } as Record<string, string>)[e] || e;
</script>

<template>
  <div class="p-6 rounded-2xl bg-zinc-900/80 border border-zinc-800/80 shadow-lg space-y-5">
    <div class="flex items-center justify-between flex-wrap gap-3">
      <div>
        <div class="font-semibold text-white text-lg">Histórico de Importações</div>
        <div class="text-xs text-zinc-400">Rastreabilidade: hash de integridade, autor e erros por linha.</div>
      </div>
      <button @click="loadHistory" class="px-4 py-2 rounded-xl bg-zinc-800 hover:bg-zinc-700 text-zinc-100 text-xs font-medium border border-zinc-700/60 flex items-center gap-2">
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"></path></svg>
        Atualizar
      </button>
    </div>

    <!-- Filtros -->
    <div class="grid grid-cols-1 sm:grid-cols-3 gap-3">
      <label class="flex flex-col gap-1">
        <span class="text-xs text-zinc-500 font-mono uppercase tracking-wider">Buscar</span>
        <input v-model="searchText" type="text" placeholder="Nome do arquivo ou fonte..." class="bg-zinc-950/80 border border-zinc-700/70 rounded-xl px-3 py-2 text-sm text-white placeholder:text-zinc-600 focus:outline-none focus:ring-2 focus:ring-indigo-500/40 focus:border-indigo-500/50 transition" />
      </label>
      <label class="flex flex-col gap-1">
        <span class="text-xs text-zinc-500 font-mono uppercase tracking-wider">Entidade</span>
        <select v-model="filterEntity" class="bg-zinc-950/80 border border-zinc-700/70 rounded-xl px-3 py-2 text-sm text-white focus:outline-none focus:ring-2 focus:ring-indigo-500/40 transition">
          <option value="todos">Todas</option>
          <option v-for="e in entityOptions" :key="e" :value="e">{{ entityLabel(e) }}</option>
        </select>
      </label>
      <label class="flex flex-col gap-1">
        <span class="text-xs text-zinc-500 font-mono uppercase tracking-wider">Status</span>
        <select v-model="filterStatus" class="bg-zinc-950/80 border border-zinc-700/70 rounded-xl px-3 py-2 text-sm text-white focus:outline-none focus:ring-2 focus:ring-indigo-500/40 transition">
          <option value="todos">Todos</option>
          <option value="ok">Somente sucesso</option>
          <option value="erros">Com erros</option>
        </select>
      </label>
    </div>

    <!-- Tabela -->
    <div class="overflow-x-auto rounded-xl border border-zinc-800/60">
      <table class="w-full text-sm">
        <thead>
          <tr class="bg-zinc-950/60 text-xs uppercase tracking-widest text-zinc-400">
            <th class="text-left px-4 py-3">Fonte / Arquivo</th>
            <th class="text-left px-4 py-3">Entidade</th>
            <th class="text-left px-4 py-3">Status</th>
            <th class="text-left px-4 py-3">Registros</th>
            <th class="text-left px-4 py-3">Enviado em</th>
            <th class="text-right px-4 py-3">Ação</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-zinc-800/60 text-zinc-200">
          <tr v-if="loading">
            <td colspan="6" class="px-4 py-8 text-center text-zinc-400 text-xs">Carregando histórico...</td>
          </tr>
          <tr v-else-if="paged.length === 0">
            <td colspan="6" class="px-4 py-8 text-center text-zinc-500 text-xs">
              {{ hasActiveFilters ? "Nenhuma importação corresponde aos filtros." : "Nenhuma importação realizada ainda." }}
            </td>
          </tr>
          <tr v-for="h in paged" :key="h.importId" class="hover:bg-zinc-800/30">
            <td class="px-4 py-3">
              <div class="font-medium text-white truncate max-w-xs">{{ h.dataSourceName }}</div>
              <div class="text-xs text-zinc-500">{{ h.dataSourceType }}</div>
            </td>
            <td class="px-4 py-3"><span class="text-xs px-2 py-1 rounded-md bg-zinc-800 border border-zinc-700/60">{{ entityLabel(h.targetEntity) }}</span></td>
            <td class="px-4 py-3"><span class="inline-flex text-xs px-2 py-1 rounded-md border" :class="statusClass(h.recordsFailed)">{{ h.recordsFailed === 0 ? "Sucesso" : `Com erros (${h.recordsFailed})` }}</span></td>
            <td class="px-4 py-3 text-xs"><span class="text-emerald-300">{{ h.recordsSucceeded }} ok</span> / <span class="text-amber-300">{{ h.recordsFailed }} falha</span> / <span class="text-zinc-400">{{ h.totalRecordsInSource }} total</span></td>
            <td class="px-4 py-3 text-xs text-zinc-300 font-mono whitespace-nowrap">{{ formatDate(h.receivedAtUtc) }}</td>
            <td class="px-4 py-3 text-right">
              <button @click="openDetail(h.importId)" class="text-xs px-3 py-1.5 rounded-lg bg-zinc-800 hover:bg-zinc-700 text-zinc-100 border border-zinc-700/60 font-medium">Detalhes</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Paginador -->
    <div class="flex items-center justify-between">
      <span class="text-xs text-zinc-500">
        {{ filtered.length }} registro(s)
        <template v-if="totalPages > 1"> · página {{ page }} de {{ totalPages }}</template>
      </span>
      <div class="flex gap-2">
        <button @click="page--" :disabled="page <= 1" class="px-3 py-1.5 rounded-lg bg-zinc-800 border border-zinc-700/60 text-zinc-200 text-xs disabled:opacity-40">← Anterior</button>
        <button @click="page++" :disabled="page >= totalPages" class="px-3 py-1.5 rounded-lg bg-zinc-800 border border-zinc-700/60 text-zinc-200 text-xs disabled:opacity-40">Próxima →</button>
      </div>
    </div>

    <!-- Modal Detalhe -->
    <Teleport to="body">
      <div v-if="modalOpen" class="fixed inset-0 z-50 flex items-end sm:items-center justify-center bg-black/70 backdrop-blur-sm p-0 sm:p-4" @click.self="modalOpen = false">
        <div class="w-full sm:max-w-3xl max-h-[90vh] bg-zinc-950 rounded-t-3xl sm:rounded-3xl border border-zinc-800 shadow-2xl flex flex-col">
          <div class="flex items-center justify-between p-5 border-b border-zinc-800/80">
            <div>
              <div class="font-semibold text-white">Detalhes da Importação</div>
              <div class="text-xs text-zinc-500 font-mono mt-0.5">{{ detail?.importId || "—" }}</div>
            </div>
            <button @click="modalOpen = false" class="p-2 rounded-xl text-zinc-400 hover:text-white hover:bg-zinc-800/60 transition-colors">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path></svg>
            </button>
          </div>
          <div class="p-5 overflow-y-auto space-y-5">
            <div v-if="loadingDetail" class="text-sm text-zinc-400 text-center py-8">Carregando detalhes...</div>
            <template v-else-if="detail">
              <div class="grid grid-cols-2 sm:grid-cols-4 gap-3 text-xs">
                <div class="p-3 rounded-xl bg-zinc-900/80 border border-zinc-800/80">
                  <div class="text-zinc-500 uppercase tracking-widest">Entidade</div>
                  <div class="text-white font-semibold mt-1">{{ entityLabel(detail.targetEntity) }}</div>
                </div>
                <div class="p-3 rounded-xl bg-zinc-900/80 border border-zinc-800/80">
                  <div class="text-zinc-500 uppercase tracking-widest">Sucesso</div>
                  <div class="text-emerald-300 font-semibold mt-1">{{ detail.recordsSucceeded }}</div>
                </div>
                <div class="p-3 rounded-xl bg-zinc-900/80 border border-zinc-800/80">
                  <div class="text-zinc-500 uppercase tracking-widest">Falhas</div>
                  <div class="text-amber-300 font-semibold mt-1">{{ detail.recordsFailed }}</div>
                </div>
                <div class="p-3 rounded-xl bg-zinc-900/80 border border-zinc-800/80">
                  <div class="text-zinc-500 uppercase tracking-widest">Total</div>
                  <div class="text-white font-semibold mt-1">{{ detail.totalRecordsInSource }}</div>
                </div>
              </div>
              <div class="p-3 rounded-xl bg-emerald-500/5 border border-emerald-500/20 text-xs text-emerald-300" v-if="detail.errors.length === 0">
                Nenhuma falha registrada. Todos os registros foram processados com sucesso.
              </div>
              <div v-else class="overflow-x-auto rounded-xl border border-zinc-800/60">
                <table class="w-full text-xs">
                  <thead>
                    <tr class="bg-zinc-900/60 text-zinc-400 uppercase tracking-widest">
                      <th class="text-left px-3 py-2">Linha</th>
                      <th class="text-left px-3 py-2">Chave</th>
                      <th class="text-left px-3 py-2">Erro</th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-zinc-800/60">
                    <tr v-for="(e, idx) in detail.errors.slice((detailErrorsPage - 1) * 50, detailErrorsPage * 50)" :key="idx">
                      <td class="px-3 py-2 font-mono text-zinc-300">{{ e.sourceRowNumber || "-" }}</td>
                      <td class="px-3 py-2 text-zinc-100 max-w-[180px] truncate">{{ e.entityKeyValue || "—" }}</td>
                      <td class="px-3 py-2 text-amber-200/90">{{ e.errorMessage }}</td>
                    </tr>
                  </tbody>
                </table>
                <div v-if="detail.errors.length > 50" class="flex items-center justify-between px-3 py-2 border-t border-zinc-800/60 bg-zinc-900/40">
                  <span class="text-xs text-zinc-400">Página {{ detailErrorsPage }} de {{ Math.ceil(detail.errors.length / 50) }}</span>
                  <div class="flex gap-2">
                    <button class="px-3 py-1 rounded-md bg-zinc-800 border border-zinc-700/60 text-zinc-200 text-xs" :disabled="detailErrorsPage <= 1" @click="detailErrorsPage--">Anterior</button>
                    <button class="px-3 py-1 rounded-md bg-zinc-800 border border-zinc-700/60 text-zinc-200 text-xs" :disabled="detailErrorsPage * 50 >= detail.errors.length" @click="detailErrorsPage++">Próxima</button>
                  </div>
                </div>
              </div>
            </template>
          </div>
          <div class="p-4 border-t border-zinc-800/80 flex items-center justify-end">
            <button @click="modalOpen = false" class="px-4 py-2 rounded-xl bg-zinc-800 hover:bg-zinc-700 text-zinc-100 text-xs font-medium border border-zinc-700/60">Fechar</button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>
