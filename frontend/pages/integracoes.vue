<script setup lang="ts">
import { ref, computed, onMounted } from "vue";
import { importService, type ImportAuditDto, type ImportAuditDetailDto, type ImportResultDto } from "~/services/modules/importService";
import { useAuthStore } from "~/stores/auth";
import AppSidebar from "~/components/layout/AppSidebar.vue";
import AppLoader from "~/components/base/AppLoader.vue";

definePageMeta({ layout: false });

const runtimeConfig = useRuntimeConfig();
const authStore = useAuthStore();
const router = useRouter();

const isLoading = ref(true);
const isSidebarOpen = ref(false);
const toggleSidebar = () => (isSidebarOpen.value = !isSidebarOpen.value);

// Uploads state
const uploadingStock = ref(false);
const uploadingFornecedores = ref(false);
const lastStockResult = ref<ImportResultDto | null>(null);
const lastFornecedoresResult = ref<ImportResultDto | null>(null);

const history = ref<ImportAuditDto[]>([]);
const loadingHistory = ref(false);

// Super Admin seleciona restaurante alvo
const isSuperAdmin = computed(() => authStore.currentUser?.role === "Admin" && !authStore.currentUser?.restaurantId);
const targetRestaurantId = ref<number | null>(isSuperAdmin.value ? 1 : authStore.currentUser?.restaurantId ?? null);

// Detail modal
const modalOpen = ref(false);
const loadingDetail = ref(false);
const detail = ref<ImportAuditDetailDto | null>(null);
const pageErrors = ref(1);

const handleLogout = () => {
  authStore.logout();
  router.push("/login");
};

const onDrop = async (evt: DragEvent, kind: "stock" | "fornecedores") => {
  evt.preventDefault();
  const files = evt.dataTransfer?.files;
  if (!files || files.length === 0) return;
  await uploadCsv(files[0], kind);
};

const onFilePicked = async (evt: Event, kind: "stock" | "fornecedores") => {
  const input = evt.target as HTMLInputElement;
  if (!input.files || input.files.length === 0) return;
  await uploadCsv(input.files[0], kind);
};

const uploadCsv = async (file: File, kind: "stock" | "fornecedores") => {
  const restId = targetRestaurantId.value;
  if (!restId || restId <= 0) {
    alert("Selecione um restaurante alvo para importar.");
    return;
  }
  if (kind === "stock") {
    uploadingStock.value = true;
    lastStockResult.value = null;
    try {
      lastStockResult.value = await importService.uploadEstoqueCsv(file, undefined, restId);
    } catch (e: any) {
      lastStockResult.value = {
        importId: crypto.randomUUID(),
        dataSourceName: file.name,
        targetEntity: "Product",
        totalRecordsInSource: 0,
        recordsSucceeded: 0,
        recordsFailed: 1,
        receivedAtUtc: new Date().toISOString(),
        finishedAtUtc: new Date().toISOString(),
        newOrUpdatedEntityIds: [],
        errors: [{ sourceRowNumber: 0, errorMessage: e?.data?.message || e?.message || "Erro desconhecido ao enviar arquivo." }]
      };
    } finally {
      uploadingStock.value = false;
    }
  } else {
    uploadingFornecedores.value = true;
    lastFornecedoresResult.value = null;
    try {
      lastFornecedoresResult.value = await importService.uploadFornecedoresCsv(file, undefined, restId);
    } catch (e: any) {
      lastFornecedoresResult.value = {
        importId: crypto.randomUUID(),
        dataSourceName: file.name,
        targetEntity: "Fornecedor",
        totalRecordsInSource: 0,
        recordsSucceeded: 0,
        recordsFailed: 1,
        receivedAtUtc: new Date().toISOString(),
        finishedAtUtc: new Date().toISOString(),
        newOrUpdatedEntityIds: [],
        errors: [{ sourceRowNumber: 0, errorMessage: e?.data?.message || e?.message || "Erro desconhecido ao enviar arquivo." }]
      };
    } finally {
      uploadingFornecedores.value = false;
    }
  }
  await loadHistory();
};

const loadHistory = async () => {
  loadingHistory.value = true;
  try {
    const restId = isSuperAdmin.value ? null : targetRestaurantId.value;
    history.value = (await importService.history(50, restId)) || [];
  } finally {
    loadingHistory.value = false;
  }
};

const openDetail = async (id: string) => {
  modalOpen.value = true;
  loadingDetail.value = true;
  pageErrors.value = 1;
  detail.value = null;
  try {
    detail.value = await importService.detail(id);
  } catch (e: any) {
    detail.value = null;
  } finally {
    loadingDetail.value = false;
  }
};

const formatDate = (s?: string | null) => {
  if (!s) return "-";
  const d = new Date(s);
  return d.toLocaleString("pt-BR", { timeZone: "America/Sao_Paulo" });
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

onMounted(async () => {
  const start = Date.now();
  authStore.initFromStorage();
  if (!authStore.isAuthenticated) {
    router.push("/login");
    return;
  }
  targetRestaurantId.value = isSuperAdmin.value ? 1 : authStore.currentUser?.restaurantId ?? null;
  const min = 1200;
  const wait = Math.max(0, min - (Date.now() - start));
  setTimeout(async () => {
    isLoading.value = false;
    await loadHistory();
  }, wait);
});
</script>

<template>
  <div class="min-h-screen bg-zinc-950 text-zinc-100 flex flex-col font-sans selection:bg-zinc-800 selection:text-white">
    <AppLoader :visible="isLoading" />

    <header class="h-16 border-b border-zinc-800/80 bg-zinc-900/60 backdrop-blur-xl sticky top-0 z-30 px-4 sm:px-6 flex items-center justify-between">
      <div class="flex items-center gap-4">
        <button
          @click="toggleSidebar"
          class="p-2 rounded-xl text-zinc-300 hover:text-white hover:bg-zinc-800/80 transition-all duration-200 focus:outline-none focus:ring-2 focus:ring-zinc-600"
          title="Abrir Menu Lateral"
        >
          <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"></path>
          </svg>
        </button>
        <div class="flex items-center gap-3">
          <span class="font-bold text-lg text-white tracking-tight">ISM</span>
          <span class="hidden sm:inline-block text-xs uppercase tracking-widest text-zinc-400 font-mono border-l border-zinc-700/60 pl-3">
            {{ runtimeConfig.public.appName }}
          </span>
        </div>
      </div>
      <div class="flex items-center gap-3">
        <div v-if="authStore.isAuthenticated" class="flex items-center gap-3">
          <span class="hidden md:inline-block text-xs text-zinc-400 font-medium">
            {{ authStore.currentUser?.name }} ({{ authStore.currentUser?.role }})
          </span>
          <button
            @click="handleLogout"
            class="px-4 py-2 text-xs font-semibold rounded-xl bg-zinc-800 hover:bg-zinc-700 text-zinc-200 hover:text-white border border-zinc-700/60 transition-all duration-200 flex items-center gap-2"
          >
            <svg class="w-4 h-4 text-zinc-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"></path>
            </svg>
            <span>Sair</span>
          </button>
        </div>
      </div>
    </header>

    <AppSidebar :isOpen="isSidebarOpen" @close="isSidebarOpen = false" />

    <main class="flex-1 max-w-7xl w-full mx-auto px-4 sm:px-6 py-8">
      <div class="p-8 rounded-2xl bg-gradient-to-r from-zinc-900 via-zinc-900/90 to-zinc-950 border border-zinc-800 shadow-xl mb-8 relative overflow-hidden">
        <div class="absolute -right-20 -bottom-20 w-80 h-80 bg-indigo-700/10 rounded-full blur-3xl pointer-events-none"></div>
        <div class="relative z-10 max-w-3xl space-y-4">
          <div class="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-zinc-800/80 border border-zinc-700/60 text-xs font-mono text-zinc-300">
            <span class="w-2 h-2 rounded-full bg-indigo-400 animate-pulse"></span>
            Integrações & Importação de Dados
          </div>
          <h1 class="text-3xl sm:text-4xl font-bold text-white tracking-tight">Importe e acompanhe seus dados</h1>
          <p class="text-zinc-400 text-sm sm:text-base leading-relaxed">
            Rastreabilidade total. Todos os arquivos importados são auditados com hash de integridade, data de recebimento e lista de erros por linha.
          </p>
          <div class="pt-2 grid grid-cols-2 sm:grid-cols-3 gap-3 text-xs font-mono max-w-xl">
            <div class="p-3 rounded-xl border border-zinc-800/80 bg-zinc-950/60">
              <div class="text-zinc-500 uppercase tracking-widest">Estoque</div>
              <div class="text-white font-semibold mt-1">CSV / Excel*</div>
            </div>
            <div class="p-3 rounded-xl border border-zinc-800/80 bg-zinc-950/60">
              <div class="text-zinc-500 uppercase tracking-widest">Fornecedores</div>
              <div class="text-white font-semibold mt-1">CSV / Excel*</div>
            </div>
            <div class="p-3 rounded-xl border border-zinc-800/80 bg-zinc-950/60 col-span-2 sm:col-span-1">
              <div class="text-zinc-500 uppercase tracking-widest">Próximos</div>
              <div class="text-white font-semibold mt-1">XML NF-e / ERP</div>
            </div>
          </div>
        </div>
      </div>

      <div v-if="isSuperAdmin" class="mb-8 p-5 rounded-2xl border border-amber-500/30 bg-amber-500/5 flex flex-col sm:flex-row sm:items-center gap-3 sm:justify-between">
        <div class="flex items-start gap-3">
          <div class="w-9 h-9 rounded-xl bg-amber-500/10 border border-amber-500/20 text-amber-400 flex items-center justify-center flex-shrink-0">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"></path></svg>
          </div>
          <div>
            <div class="text-sm font-semibold text-amber-300">Modo Super Admin</div>
            <p class="text-xs text-zinc-400">Selecione o restaurante alvo antes de importar arquivos.</p>
          </div>
        </div>
        <label class="flex flex-col gap-1 min-w-[240px]">
          <span class="text-xs text-zinc-500 font-mono uppercase tracking-wider">Restaurante</span>
          <select v-model.number="targetRestaurantId" class="bg-zinc-950/80 border border-zinc-700/70 rounded-xl px-3 py-2 text-sm text-white focus:outline-none focus:ring-2 focus:ring-amber-500/40 focus:border-amber-500/50 transition">
            <option :value="1">1 - Gourmet ISM Restaurant</option>
          </select>
        </label>
      </div>

      <div class="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-10">
        <!-- CARD ESTOQUE CSV -->
        <div
          class="p-6 rounded-2xl bg-zinc-900/80 border border-zinc-800/80 shadow-lg space-y-4"
          @dragover.prevent
          @drop="(e) => onDrop(e, 'stock')"
        >
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-3">
              <div class="w-10 h-10 rounded-xl bg-emerald-500/10 border border-emerald-500/20 text-emerald-400 flex items-center justify-center">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4"></path></svg>
              </div>
              <div>
                <div class="font-semibold text-white">Estoque / Produtos</div>
                <div class="text-xs text-zinc-400">Envio em lote de estoque inicial</div>
              </div>
            </div>
            <label class="cursor-pointer inline-flex items-center gap-2 px-4 py-2 rounded-xl bg-emerald-500/90 hover:bg-emerald-500 text-white text-xs font-semibold transition-all shadow shadow-emerald-500/20">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12"></path></svg>
              <span>Selecionar CSV</span>
              <input type="file" accept=".csv,text/csv" class="hidden" @change="(e) => onFilePicked(e, 'stock')" />
            </label>
          </div>
          <div class="border-2 border-dashed rounded-xl p-8 text-center border-zinc-700/80 bg-zinc-950/40 hover:border-emerald-500/50 transition-colors">
            <svg v-if="!uploadingStock" class="w-10 h-10 text-zinc-400 mx-auto mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 13h6m-3-3v6m5 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"></path></svg>
            <p v-if="uploadingStock" class="text-sm text-emerald-300 font-medium">Enviando... aguarde</p>
            <p v-else class="text-sm text-zinc-400">Arraste e solte seu CSV aqui</p>
            <p class="text-xs text-zinc-500 mt-1">Colunas: <span class="font-mono text-zinc-300">Nome, Unidade, QuantidadeAtual, QuantidadeMinima, CustoMedio</span></p>
          </div>
          <div v-if="lastStockResult" class="p-4 rounded-xl border" :class="statusClass(lastStockResult.recordsFailed)">
            <div class="flex items-center justify-between">
              <div class="text-sm"><span class="font-semibold">Resultado:</span> {{ lastStockResult.recordsSucceeded }} ok · {{ lastStockResult.recordsFailed }} falhas</div>
              <button class="text-xs underline hover:text-white" @click="openDetail(lastStockResult.importId)">Ver detalhes</button>
            </div>
            <p v-if="lastStockResult.recordsFailed > 0 && lastStockResult.errors[0]" class="text-xs mt-2 opacity-80">
              {{ lastStockResult.errors[0].errorMessage }}
            </p>
          </div>
        </div>

        <!-- CARD FORNECEDORES CSV -->
        <div
          class="p-6 rounded-2xl bg-zinc-900/80 border border-zinc-800/80 shadow-lg space-y-4"
          @dragover.prevent
          @drop="(e) => onDrop(e, 'fornecedores')"
        >
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-3">
              <div class="w-10 h-10 rounded-xl bg-indigo-500/10 border border-indigo-500/20 text-indigo-400 flex items-center justify-center">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 17a2 2 0 11-4 0 2 2 0 014 0zM19 17a2 2 0 11-4 0 2 2 0 014 0z"></path><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16V6a1 1 0 00-1-1H4a1 1 0 00-1 1v10a1 1 0 001 1h1m8-1a1 1 0 01-1 1H9m4-1V8h4l3 3v5h-2m-6 0h2"></path></svg>
              </div>
              <div>
                <div class="font-semibold text-white">Fornecedores</div>
                <div class="text-xs text-zinc-400">Cadastro em massa de fornecedores</div>
              </div>
            </div>
            <label class="cursor-pointer inline-flex items-center gap-2 px-4 py-2 rounded-xl bg-indigo-500/90 hover:bg-indigo-500 text-white text-xs font-semibold transition-all shadow shadow-indigo-500/20">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12"></path></svg>
              <span>Selecionar CSV</span>
              <input type="file" accept=".csv,text/csv" class="hidden" @change="(e) => onFilePicked(e, 'fornecedores')" />
            </label>
          </div>
          <div class="border-2 border-dashed rounded-xl p-8 text-center border-zinc-700/80 bg-zinc-950/40 hover:border-indigo-500/50 transition-colors">
            <svg v-if="!uploadingFornecedores" class="w-10 h-10 text-zinc-400 mx-auto mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 13h6m-3-3v6m5 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"></path></svg>
            <p v-if="uploadingFornecedores" class="text-sm text-indigo-300 font-medium">Enviando... aguarde</p>
            <p v-else class="text-sm text-zinc-400">Arraste e solte seu CSV aqui</p>
            <p class="text-xs text-zinc-500 mt-1">Colunas: <span class="font-mono text-zinc-300">Nome, Categoria, Descricao, Email, Telefone</span></p>
          </div>
          <div v-if="lastFornecedoresResult" class="p-4 rounded-xl border" :class="statusClass(lastFornecedoresResult.recordsFailed)">
            <div class="flex items-center justify-between">
              <div class="text-sm"><span class="font-semibold">Resultado:</span> {{ lastFornecedoresResult.recordsSucceeded }} ok · {{ lastFornecedoresResult.recordsFailed }} falhas</div>
              <button class="text-xs underline hover:text-white" @click="openDetail(lastFornecedoresResult.importId)">Ver detalhes</button>
            </div>
            <p v-if="lastFornecedoresResult.recordsFailed > 0 && lastFornecedoresResult.errors[0]" class="text-xs mt-2 opacity-80">
              {{ lastFornecedoresResult.errors[0].errorMessage }}
            </p>
          </div>
        </div>
      </div>

      <!-- HISTORICO -->
      <div class="p-6 rounded-2xl bg-zinc-900/80 border border-zinc-800/80 shadow-lg space-y-5">
        <div class="flex items-center justify-between">
          <div>
            <div class="font-semibold text-white text-lg">Histórico de Importações</div>
            <div class="text-xs text-zinc-400">Acompanhe cada lote enviado e sua rastreabilidade.</div>
          </div>
          <button @click="loadHistory" class="px-4 py-2 rounded-xl bg-zinc-800 hover:bg-zinc-700 text-zinc-100 text-xs font-medium border border-zinc-700/60 flex items-center gap-2">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"></path></svg>
            Atualizar
          </button>
        </div>

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
              <tr v-if="loadingHistory">
                <td colspan="6" class="px-4 py-8 text-center text-zinc-400 text-xs">Carregando histórico...</td>
              </tr>
              <tr v-else-if="history.length === 0">
                <td colspan="6" class="px-4 py-8 text-center text-zinc-500 text-xs">Nenhuma importação realizada ainda. Envie um CSV acima!</td>
              </tr>
              <tr v-for="h in history" :key="h.importId" class="hover:bg-zinc-800/30">
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
      </div>
    </main>

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
              <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 text-xs">
                <div class="p-3 rounded-xl bg-zinc-900/80 border border-zinc-800/80">
                  <div class="text-zinc-500 uppercase tracking-widest">Arquivo</div>
                  <div class="text-white mt-1 break-all">{{ detail.sourceOriginalFilename || "—" }}</div>
                </div>
                <div class="p-3 rounded-xl bg-zinc-900/80 border border-zinc-800/80">
                  <div class="text-zinc-500 uppercase tracking-widest">Finalizado em</div>
                  <div class="text-white mt-1">{{ formatDate(detail.finishedAtUtc) }}</div>
                </div>
              </div>
              <div>
                <div class="flex items-center justify-between mb-2">
                  <div class="font-semibold text-white text-sm">Erros / Linhas com problema</div>
                  <div class="text-xs text-zinc-400">{{ detail.errors.length }} registros</div>
                </div>
                <div v-if="detail.errors.length === 0" class="p-5 rounded-xl bg-emerald-500/5 border border-emerald-500/20 text-center text-xs text-emerald-300">
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
                      <tr v-for="(e, idx) in detail.errors.slice((pageErrors - 1) * 50, pageErrors * 50)" :key="idx">
                        <td class="px-3 py-2 font-mono text-zinc-300">{{ e.sourceRowNumber || "-" }}</td>
                        <td class="px-3 py-2 text-zinc-100 max-w-[180px] truncate">{{ e.entityKeyValue || "—" }}</td>
                        <td class="px-3 py-2 text-amber-200/90">{{ e.errorMessage }}</td>
                      </tr>
                    </tbody>
                  </table>
                  <div v-if="detail.errors.length > 50" class="flex items-center justify-between px-3 py-2 border-t border-zinc-800/60 bg-zinc-900/40">
                    <span class="text-xs text-zinc-400">Página {{ pageErrors }} de {{ Math.ceil(detail.errors.length / 50) }}</span>
                    <div class="flex gap-2">
                      <button class="px-3 py-1 rounded-md bg-zinc-800 border border-zinc-700/60 text-zinc-200 text-xs" :disabled="pageErrors <= 1" @click="pageErrors--">Anterior</button>
                      <button class="px-3 py-1 rounded-md bg-zinc-800 border border-zinc-700/60 text-zinc-200 text-xs" :disabled="pageErrors * 50 >= detail.errors.length" @click="pageErrors++">Próxima</button>
                    </div>
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

    <footer class="mt-auto border-t border-zinc-800/80 bg-zinc-950 py-6 text-center text-xs text-zinc-500">
      <div class="max-w-7xl mx-auto px-4 flex flex-col sm:flex-row items-center justify-between gap-3">
        <p>&copy; 2026 ISM — Intelligence Supply Manager. Todos os direitos reservados.</p>
        <div class="flex items-center gap-4 text-zinc-400">
          <NuxtLink to="/" class="hover:text-white transition-colors">Início</NuxtLink>
          <NuxtLink to="/login" class="hover:text-white transition-colors">Login</NuxtLink>
          <a href="http://localhost:8080/swagger" target="_blank" class="hover:text-white transition-colors">Swagger API</a>
        </div>
      </div>
    </footer>
  </div>
</template>
