<script setup lang="ts">
import { ref, computed, onMounted } from "vue";
import AppSidebar from "~/components/layout/AppSidebar.vue";
import AppLoader from "~/components/base/AppLoader.vue";
import { useAuthStore } from "~/stores/auth";
import { useThemeStore } from "~/stores/theme";
import {
  supplierService,
  type SupplierResponse,
  type CreateSupplierRequest,
  type UpdateSupplierRequest
} from "~/services/modules/supplierService";

definePageMeta({ layout: false });

const authStore = useAuthStore();
const themeStore = useThemeStore();
const router = useRouter();

// State UI
const isLoading = ref(true);
const isSidebarOpen = ref(false);
const listSearch = ref("");
const selectedCategory = ref<string>("ALL");

// Data State
const suppliers = ref<SupplierResponse[]>([]);
const selectedSupplier = ref<SupplierResponse | null>(null);

// Modals
const showSupplierModal = ref(false);
const isEditing = ref(false);
const isSaving = ref(false);
const isDeleting = ref(false);
const showDeleteConfirm = ref(false);

// Messages
const errorMessage = ref("");
const successMessage = ref("");

// Form
const supplierForm = ref<{
  name: string;
  category: string;
  description: string;
  email: string;
  phone: string;
}>({
  name: "",
  category: "Geral",
  description: "",
  email: "",
  phone: ""
});

const categoriesList = [
  "Frutos do Mar",
  "Hortifrúti",
  "Laticínios",
  "Carnes",
  "Bebidas",
  "Embalagens",
  "Geral"
];

const toggleSidebar = () => (isSidebarOpen.value = !isSidebarOpen.value);

const handleLogout = () => {
  authStore.logout();
  router.push("/login");
};

// Filtered Suppliers
const filteredSuppliers = computed(() => {
  let list = suppliers.value;

  const query = listSearch.value.trim().toLowerCase();
  if (query) {
    list = list.filter(
      (s) =>
        s.name.toLowerCase().includes(query) ||
        s.category.toLowerCase().includes(query) ||
        s.email.toLowerCase().includes(query) ||
        s.phone.toLowerCase().includes(query)
    );
  }

  if (selectedCategory.value !== "ALL") {
    list = list.filter(
      (s) => s.category.toUpperCase() === selectedCategory.value.toUpperCase()
    );
  }

  return list;
});

// Load suppliers from backend
const loadSuppliers = async () => {
  try {
    const data = await supplierService.getAll();
    suppliers.value = data || [];
    if (suppliers.value.length > 0 && !selectedSupplier.value) {
      selectedSupplier.value = suppliers.value[0];
    } else if (selectedSupplier.value) {
      const found = suppliers.value.find((s) => s.id === selectedSupplier.value?.id);
      selectedSupplier.value = found || suppliers.value[0] || null;
    }
  } catch (err: any) {
    errorMessage.value = err?.data?.message || err?.message || "Erro ao carregar lista de fornecedores.";
  }
};

const openCreateModal = () => {
  isEditing.value = false;
  supplierForm.value = {
    name: "",
    category: "Frutos do Mar",
    description: "",
    email: "",
    phone: ""
  };
  errorMessage.value = "";
  showSupplierModal.value = true;
};

const openEditModal = (supplier: SupplierResponse) => {
  isEditing.value = true;
  supplierForm.value = {
    name: supplier.name,
    category: supplier.category || "Geral",
    description: supplier.description || "",
    email: supplier.email,
    phone: supplier.phone
  };
  errorMessage.value = "";
  showSupplierModal.value = true;
};

const saveSupplier = async () => {
  if (!supplierForm.value.name.trim()) {
    errorMessage.value = "O nome do fornecedor é obrigatório.";
    return;
  }
  if (!supplierForm.value.email.trim() || !supplierForm.value.email.includes("@")) {
    errorMessage.value = "Informe um e-mail válido.";
    return;
  }
  if (!supplierForm.value.phone.trim()) {
    errorMessage.value = "O telefone de contato é obrigatório.";
    return;
  }

  isSaving.value = true;
  errorMessage.value = "";

  try {
    if (isEditing.value && selectedSupplier.value) {
      const updatePayload: UpdateSupplierRequest = {
        name: supplierForm.value.name.trim(),
        category: supplierForm.value.category.trim(),
        description: supplierForm.value.description.trim() || undefined,
        email: supplierForm.value.email.trim(),
        phone: supplierForm.value.phone.trim()
      };
      const updated = await supplierService.update(selectedSupplier.value.id, updatePayload);
      successMessage.value = "Fornecedor atualizado com sucesso!";
      await loadSuppliers();
      selectedSupplier.value = updated;
    } else {
      const restId = authStore.currentUser?.restaurantId || 1;
      const createPayload: CreateSupplierRequest = {
        restaurantId: restId,
        name: supplierForm.value.name.trim(),
        category: supplierForm.value.category.trim(),
        description: supplierForm.value.description.trim() || undefined,
        email: supplierForm.value.email.trim(),
        phone: supplierForm.value.phone.trim()
      };
      const created = await supplierService.create(createPayload);
      successMessage.value = "Novo fornecedor cadastrado com sucesso!";
      await loadSuppliers();
      selectedSupplier.value = created;
    }

    showSupplierModal.value = false;
    setTimeout(() => {
      successMessage.value = "";
    }, 4000);
  } catch (err: any) {
    errorMessage.value =
      err?.data?.message || err?.message || "Erro ao salvar informações do fornecedor.";
  } finally {
    isSaving.value = false;
  }
};

const deleteSupplier = async () => {
  if (!selectedSupplier.value) return;
  isDeleting.value = true;
  errorMessage.value = "";

  try {
    await supplierService.delete(selectedSupplier.value.id);
    successMessage.value = "Fornecedor removido com sucesso!";
    selectedSupplier.value = null;
    showDeleteConfirm.value = false;
    await loadSuppliers();
    setTimeout(() => {
      successMessage.value = "";
    }, 4000);
  } catch (err: any) {
    errorMessage.value = err?.data?.message || err?.message || "Erro ao excluir fornecedor.";
  } finally {
    isDeleting.value = false;
  }
};

onMounted(async () => {
  const start = Date.now();
  authStore.initFromStorage();

  if (!authStore.isAuthenticated) {
    router.push("/login");
    return;
  }

  await loadSuppliers();
  setTimeout(() => {
    isLoading.value = false;
  }, Math.max(0, 800 - (Date.now() - start)));
});
</script>

<template>
  <div
    :class="[
      'min-h-screen flex flex-col font-sans transition-colors duration-200',
      themeStore.isDark
        ? 'bg-zinc-950 text-zinc-100 selection:bg-zinc-800 selection:text-white'
        : 'bg-zinc-50 text-zinc-900 selection:bg-indigo-100 selection:text-indigo-900'
    ]"
  >
    <AppLoader :visible="isLoading" />

    <!-- ========================================================= -->
    <!-- NAVBAR PADRÃO (IDENTICA AS DEMAIS TELAS DO SISTEMA) -->
    <!-- ========================================================= -->
    <header
      :class="[
        'h-16 border-b backdrop-blur-xl sticky top-0 z-30 px-4 sm:px-6 flex items-center justify-between',
        themeStore.isDark
          ? 'border-zinc-800/80 bg-zinc-900/60'
          : 'border-zinc-200 bg-white/80 shadow-sm'
      ]"
    >
      <div class="flex items-center gap-4">
        <button
          type="button"
          @click="toggleSidebar"
          :class="[
            'p-2 rounded-xl transition',
            themeStore.isDark
              ? 'text-zinc-300 hover:text-white hover:bg-zinc-800/80'
              : 'text-zinc-600 hover:text-zinc-900 hover:bg-zinc-100'
          ]"
          aria-label="Abrir menu"
        >
          <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
          </svg>
        </button>

        <span
          :class="[
            'font-bold text-lg tracking-tight',
            themeStore.isDark ? 'text-white' : 'text-zinc-900'
          ]"
        >
          ISM
        </span>
      </div>

      <button
        type="button"
        @click="handleLogout"
        :class="[
          'px-4 py-2 text-xs font-semibold rounded-xl border transition',
          themeStore.isDark
            ? 'bg-zinc-800 hover:bg-zinc-700 text-zinc-200 hover:text-white border-zinc-700/60'
            : 'bg-zinc-100 hover:bg-zinc-200 text-zinc-700 border-zinc-200'
        ]"
      >
        Sair
      </button>
    </header>

    <!-- Sidebar Drawer -->
    <AppSidebar :isOpen="isSidebarOpen" @close="isSidebarOpen = false" />

    <!-- ========================================================= -->
    <!-- MAIN CONTENT -->
    <!-- ========================================================= -->
    <main class="flex-1 max-w-7xl w-full mx-auto px-4 sm:px-6 py-8">
      
      <!-- Notifications Alert -->
      <div v-if="successMessage" class="mb-6 p-4 rounded-xl bg-emerald-500/10 border border-emerald-500/30 text-emerald-600 dark:text-emerald-300 text-xs sm:text-sm flex items-center justify-between">
        <span>{{ successMessage }}</span>
        <button @click="successMessage = ''" class="text-emerald-600 dark:text-emerald-400 hover:text-emerald-700 dark:hover:text-emerald-200 font-bold ml-4">✕</button>
      </div>

      <div v-if="errorMessage" class="mb-6 p-4 rounded-xl bg-red-500/10 border border-red-500/30 text-red-600 dark:text-red-300 text-xs sm:text-sm flex items-center justify-between">
        <span>{{ errorMessage }}</span>
        <button @click="errorMessage = ''" class="text-red-600 dark:text-red-400 hover:text-red-700 dark:hover:text-red-200 font-bold ml-4">✕</button>
      </div>

      <!-- ========================================================= -->
      <!-- PAGE HEADER -->
      <!-- ========================================================= -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-6">
        <div>
          <div
            :class="[
              'inline-flex items-center gap-2 px-3 py-1 rounded-full border text-[11px] font-bold uppercase tracking-widest mb-2',
              themeStore.isDark
                ? 'border-zinc-800 bg-zinc-900/80 text-zinc-400'
                : 'border-zinc-200 bg-white text-zinc-500 shadow-sm'
            ]"
          >
            Fornecedores
          </div>
          <h1
            :class="[
              'text-2xl sm:text-3xl md:text-4xl font-extrabold tracking-tight',
              themeStore.isDark ? 'text-white' : 'text-zinc-900'
            ]"
          >
            Gestão de fornecedores
          </h1>
          <p
            :class="[
              'text-xs sm:text-sm mt-1 max-w-2xl',
              themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500'
            ]"
          >
            Centralize parceiros, analise notas e envie automaticamente os insumos para o estoque e para os agentes.
          </p>
        </div>

        <div class="flex items-center gap-3 self-start sm:self-center">
          <button
            @click="openCreateModal"
            class="bg-white text-zinc-950 hover:bg-zinc-200 font-semibold px-4 py-2.5 rounded-xl text-xs sm:text-sm transition-all duration-200 shadow-lg shadow-zinc-900/5 flex items-center gap-2 active:scale-95 border border-zinc-200 dark:shadow-white/5 dark:border-0"
          >
            <span class="text-base leading-none font-bold">+</span>
            Novo fornecedor
          </button>
        </div>
      </div>

      <!-- ========================================================= -->
      <!-- BANNER DO AGENTE (APENAS ÍCONE CAMINHÃO E MENSAGEM SIMPLIFICADA) -->
      <!-- ========================================================= -->
      <div
        :class="[
          'rounded-2xl p-4 sm:p-5 mb-8 border-l-4 border-l-cyan-500 shadow-xl flex items-center gap-3.5',
          themeStore.isDark
            ? 'bg-zinc-900/70 border border-zinc-800/80'
            : 'bg-white border border-zinc-200'
        ]"
      >
        <div class="w-10 h-10 rounded-xl bg-cyan-500/10 border border-cyan-500/20 text-cyan-600 dark:text-cyan-400 flex items-center justify-center shrink-0">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 17a2 2 0 11-4 0 2 2 0 014 0zM19 17a2 2 0 11-4 0 2 2 0 014 0z" />
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16V6a1 1 0 00-1-1H4a1 1 0 00-1 1v10a1 1 0 001 1h1m8-1a1 1 0 01-1 1H9m4-1V8h4l3 3v5h-2m-6 0h2" />
          </svg>
        </div>
        <span
          :class="[
            'text-sm font-semibold',
            themeStore.isDark ? 'text-zinc-200' : 'text-zinc-800'
          ]"
        >
          Agente de Fornecedores em construção
        </span>
      </div>

      <!-- ========================================================= -->
      <!-- SPLIT VIEW LAYOUT -->
      <!-- ========================================================= -->
      <div class="grid grid-cols-1 lg:grid-cols-12 gap-6">
        
        <!-- LEFT COLUMN: LISTA DE PARCEIROS ATIVOS -->
        <div class="lg:col-span-5 flex flex-col space-y-4">
          
          <!-- Column Header -->
          <div class="flex items-center justify-between px-1">
            <div>
              <h2
                :class="[
                  'text-base font-bold tracking-tight',
                  themeStore.isDark ? 'text-white' : 'text-zinc-900'
                ]"
              >Parceiros ativos</h2>
              <p
                :class="[
                  'text-xs',
                  themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500'
                ]"
              >{{ filteredSuppliers.length }} fornecedores</p>
            </div>
          </div>

          <!-- Mini Filter / Search Input -->
          <div class="relative">
            <svg
              :class="[
                'w-4 h-4 absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none',
                themeStore.isDark ? 'text-zinc-500' : 'text-zinc-400'
              ]"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
            </svg>
            <input
              v-model="listSearch"
              type="text"
              placeholder="Filtrar por nome ou categoria..."
              :class="[
                'w-full pl-9 pr-3 py-2 rounded-xl text-xs focus:outline-none transition',
                themeStore.isDark
                  ? 'bg-zinc-900/60 border border-zinc-800/80 text-zinc-200 placeholder-zinc-500 focus:border-zinc-700'
                  : 'bg-white border border-zinc-200 text-zinc-900 placeholder-zinc-400 focus:border-zinc-300 shadow-sm'
              ]"
            />
          </div>

          <!-- Supplier Items List -->
          <div class="space-y-2.5 max-h-[600px] overflow-y-auto pr-1">
            <div
              v-for="supplier in filteredSuppliers"
              :key="supplier.id"
              @click="selectedSupplier = supplier"
              :class="[
                'p-4 rounded-xl border transition-all duration-200 cursor-pointer flex items-center justify-between gap-3',
                selectedSupplier?.id === supplier.id
                  ? (themeStore.isDark
                      ? 'border-cyan-500/80 bg-zinc-900/90 shadow-md ring-1 ring-cyan-500/30'
                      : 'border-cyan-400 bg-cyan-50 shadow-md ring-1 ring-cyan-400/30')
                  : (themeStore.isDark
                      ? 'border-zinc-800/80 bg-zinc-900/40 hover:bg-zinc-900/80 hover:border-zinc-700'
                      : 'border-zinc-200 bg-white hover:bg-zinc-50 hover:border-zinc-300 shadow-sm')
              ]"
            >
              <div class="flex items-center gap-3.5 min-w-0">
                <!-- Icon -->
                <div class="w-10 h-10 rounded-xl bg-cyan-500/10 border border-cyan-500/20 text-cyan-600 dark:text-cyan-400 flex items-center justify-center shrink-0">
                  <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 17a2 2 0 11-4 0 2 2 0 014 0zM19 17a2 2 0 11-4 0 2 2 0 014 0z" />
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16V6a1 1 0 00-1-1H4a1 1 0 00-1 1v10a1 1 0 001 1h1m8-1a1 1 0 01-1 1H9m4-1V8h4l3 3v5h-2m-6 0h2" />
                  </svg>
                </div>

                <!-- Info -->
                <div class="min-w-0">
                  <h4
                    :class="[
                      'text-sm font-bold truncate leading-tight',
                      themeStore.isDark ? 'text-white' : 'text-zinc-900'
                    ]"
                  >
                    {{ supplier.name }}
                  </h4>
                  <p
                    :class="[
                      'text-xs truncate mt-0.5',
                      themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500'
                    ]"
                  >
                    {{ supplier.email }}
                  </p>
                  <p
                    :class="[
                      'text-[11px] truncate mt-0.5',
                      themeStore.isDark ? 'text-zinc-500' : 'text-zinc-400'
                    ]"
                  >
                    Tel: {{ supplier.phone }}
                  </p>
                </div>
              </div>

              <!-- Category Badge -->
              <div class="shrink-0">
                <span
                  :class="[
                    'px-2.5 py-1 rounded-full border text-[10px] font-bold uppercase tracking-wider',
                    themeStore.isDark
                      ? 'bg-zinc-800/90 border-zinc-700/60 text-zinc-300'
                      : 'bg-zinc-100 border-zinc-200 text-zinc-600'
                  ]"
                >
                  {{ supplier.category }}
                </span>
              </div>
            </div>

            <!-- Empty List State -->
            <div
              v-if="filteredSuppliers.length === 0"
              :class="[
                'p-8 text-center rounded-xl border text-xs',
                themeStore.isDark
                  ? 'bg-zinc-900/30 border-zinc-800/60 text-zinc-400'
                  : 'bg-white border-zinc-200 text-zinc-500'
              ]"
            >
              Nenhum fornecedor encontrado.
            </div>
          </div>
        </div>

        <!-- RIGHT COLUMN: DETALHES DO FORNECEDOR -->
        <div class="lg:col-span-7">
          <div
            :class="[
              'h-full rounded-2xl border p-6 sm:p-8 flex flex-col justify-between min-h-[500px]',
              themeStore.isDark
                ? 'bg-zinc-900/40 border-zinc-800/80'
                : 'bg-white border-zinc-200 shadow-sm'
            ]"
          >
            
            <!-- State: With Selected Supplier -->
            <div v-if="selectedSupplier" class="space-y-6">
              
              <!-- Header Info & Actions -->
              <div
                :class="[
                  'flex flex-col sm:flex-row sm:items-start justify-between gap-4 pb-6 border-b',
                  themeStore.isDark ? 'border-zinc-800/80' : 'border-zinc-200'
                ]"
              >
                <div>
                  <div class="flex items-center gap-2 mb-1.5">
                    <span class="px-2.5 py-0.5 rounded-full bg-cyan-500/10 border border-cyan-500/30 text-[10px] font-bold text-cyan-600 dark:text-cyan-400 uppercase tracking-wider">
                      {{ selectedSupplier.category }}
                    </span>
                    <span
                      :class="[
                        'px-2 py-0.5 rounded-full text-[10px] font-bold uppercase tracking-wider border',
                        selectedSupplier.isActive
                          ? 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border-emerald-500/30'
                          : (themeStore.isDark
                              ? 'bg-zinc-800 text-zinc-400 border-zinc-700'
                              : 'bg-zinc-100 text-zinc-500 border-zinc-300')
                      ]"
                    >
                      {{ selectedSupplier.isActive ? "Ativo" : "Inativo" }}
                    </span>
                  </div>

                  <h2
                    :class="[
                      'text-xl sm:text-2xl font-bold',
                      themeStore.isDark ? 'text-white' : 'text-zinc-900'
                    ]"
                  >
                    {{ selectedSupplier.name }}
                  </h2>
                  <p
                    :class="[
                      'text-xs sm:text-sm mt-1',
                      themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500'
                    ]"
                  >
                    {{ selectedSupplier.description || "Nenhuma observação cadastrada." }}
                  </p>
                </div>

                <!-- Action Buttons -->
                <div class="flex items-center gap-2 shrink-0">
                  <button
                    @click="openEditModal(selectedSupplier)"
                    :class="[
                      'px-3 py-1.5 rounded-lg text-xs font-semibold border transition flex items-center gap-1.5',
                      themeStore.isDark
                        ? 'bg-zinc-800 hover:bg-zinc-700 text-zinc-200 hover:text-white border-zinc-700/60'
                        : 'bg-zinc-100 hover:bg-zinc-200 text-zinc-700 border-zinc-200'
                    ]"
                  >
                    <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                    </svg>
                    Editar
                  </button>

                  <button
                    @click="showDeleteConfirm = true"
                    class="px-3 py-1.5 rounded-lg text-xs font-semibold border transition flex items-center gap-1.5 dark:bg-red-950/40 dark:hover:bg-red-900/60 dark:text-red-400 dark:hover:text-red-300 dark:border-red-800/40 bg-red-500/10 hover:bg-red-500/20 text-red-600 hover:text-red-700 border-red-500/30"
                  >
                    <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                    </svg>
                    Excluir
                  </button>
                </div>
              </div>

              <!-- Information Grid -->
              <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div
                  :class="[
                    'p-4 rounded-xl border',
                    themeStore.isDark
                      ? 'bg-zinc-900/60 border-zinc-800/80'
                      : 'bg-zinc-50 border-zinc-200'
                  ]"
                >
                  <span
                    :class="[
                      'text-[11px] font-semibold uppercase tracking-wider block',
                      themeStore.isDark ? 'text-zinc-500' : 'text-zinc-500'
                    ]"
                  >E-mail Comercial</span>
                  <span
                    :class="[
                      'text-sm font-medium mt-1 block truncate',
                      themeStore.isDark ? 'text-zinc-200' : 'text-zinc-800'
                    ]"
                  >{{ selectedSupplier.email }}</span>
                </div>

                <div
                  :class="[
                    'p-4 rounded-xl border',
                    themeStore.isDark
                      ? 'bg-zinc-900/60 border-zinc-800/80'
                      : 'bg-zinc-50 border-zinc-200'
                  ]"
                >
                  <span
                    :class="[
                      'text-[11px] font-semibold uppercase tracking-wider block',
                      themeStore.isDark ? 'text-zinc-500' : 'text-zinc-500'
                    ]"
                  >Telefone / Contato</span>
                  <span
                    :class="[
                      'text-sm font-medium mt-1 block',
                      themeStore.isDark ? 'text-zinc-200' : 'text-zinc-800'
                    ]"
                  >{{ selectedSupplier.phone }}</span>
                </div>

                <div
                  :class="[
                    'p-4 rounded-xl border',
                    themeStore.isDark
                      ? 'bg-zinc-900/60 border-zinc-800/80'
                      : 'bg-zinc-50 border-zinc-200'
                  ]"
                >
                  <span
                    :class="[
                      'text-[11px] font-semibold uppercase tracking-wider block',
                      themeStore.isDark ? 'text-zinc-500' : 'text-zinc-500'
                    ]"
                  >Categoria Principal</span>
                  <span
                    :class="[
                      'text-sm font-medium mt-1 block',
                      themeStore.isDark ? 'text-zinc-200' : 'text-zinc-800'
                    ]"
                  >{{ selectedSupplier.category }}</span>
                </div>

                <div
                  :class="[
                    'p-4 rounded-xl border',
                    themeStore.isDark
                      ? 'bg-zinc-900/60 border-zinc-800/80'
                      : 'bg-zinc-50 border-zinc-200'
                  ]"
                >
                  <span
                    :class="[
                      'text-[11px] font-semibold uppercase tracking-wider block',
                      themeStore.isDark ? 'text-zinc-500' : 'text-zinc-500'
                    ]"
                  >Identificador ID</span>
                  <span
                    :class="[
                      'text-sm font-mono font-medium mt-1 block',
                      themeStore.isDark ? 'text-zinc-200' : 'text-zinc-800'
                    ]"
                  >#{{ selectedSupplier.id }}</span>
                </div>
              </div>

              <!-- Additional Panel -->
              <div
                :class="[
                  'p-4 rounded-xl border text-xs space-y-2',
                  themeStore.isDark
                    ? 'bg-zinc-950/60 border-zinc-800/60 text-zinc-400'
                    : 'bg-zinc-50 border-zinc-200 text-zinc-600'
                ]"
              >
                <div class="flex items-center justify-between">
                  <span>Integração de Notas e Pedidos:</span>
                  <span class="text-emerald-600 dark:text-emerald-400 font-semibold">Pronto para cotação</span>
                </div>
                <div class="flex items-center justify-between">
                  <span>Insumos Vinculados:</span>
                  <NuxtLink to="/estoque" class="text-cyan-600 dark:text-cyan-400 hover:underline">Ver no estoque →</NuxtLink>
                </div>
              </div>
            </div>

            <!-- State: Empty (No Supplier Selected) -->
            <div v-else class="my-auto text-center py-12 flex flex-col items-center justify-center">
              <svg
                :class="[
                  'w-16 h-16 mx-auto mb-4',
                  themeStore.isDark ? 'text-zinc-700' : 'text-zinc-300'
                ]"
                fill="none" stroke="currentColor" viewBox="0 0 24 24"
              >
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 17a2 2 0 11-4 0 2 2 0 014 0zM19 17a2 2 0 11-4 0 2 2 0 014 0z" />
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M13 16V6a1 1 0 00-1-1H4a1 1 0 00-1 1v10a1 1 0 001 1h1m8-1a1 1 0 01-1 1H9m4-1V8h4l3 3v5h-2m-6 0h2" />
              </svg>
              <p
                :class="[
                  'text-sm max-w-xs',
                  themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500'
                ]"
              >
                Selecione um fornecedor para ver os detalhes e analisar insumos.
              </p>
            </div>

          </div>
        </div>
      </div>
    </main>

    <!-- ========================================================= -->
    <!-- MODAL: CRIAR / EDITAR FORNECEDOR -->
    <!-- ========================================================= -->
    <div
      v-if="showSupplierModal"
      class="fixed inset-0 bg-black/75 backdrop-blur-sm z-50 flex items-center justify-center p-4"
    >
      <div
        :class="[
          'w-full max-w-lg border rounded-2xl p-6 sm:p-8 shadow-2xl space-y-6',
          themeStore.isDark
            ? 'bg-zinc-900 border-zinc-800'
            : 'bg-white border-zinc-200'
        ]"
      >
        
        <div
          :class="[
            'flex items-center justify-between border-b pb-4',
            themeStore.isDark ? 'border-zinc-800' : 'border-zinc-200'
          ]"
        >
          <h3
            :class="[
              'text-lg font-bold',
              themeStore.isDark ? 'text-white' : 'text-zinc-900'
            ]"
          >
            {{ isEditing ? "Editar Fornecedor" : "Novo Fornecedor" }}
          </h3>
          <button
            @click="showSupplierModal = false"
            :class="[
              'p-1 rounded-lg transition',
              themeStore.isDark
                ? 'text-zinc-400 hover:text-white hover:bg-zinc-800'
                : 'text-zinc-500 hover:text-zinc-900 hover:bg-zinc-100'
            ]"
          >
            ✕
          </button>
        </div>

        <form @submit.prevent="saveSupplier" class="space-y-4">
          <div>
            <label
              :class="[
                'block text-xs font-semibold uppercase tracking-wider mb-1.5',
                themeStore.isDark ? 'text-zinc-300' : 'text-zinc-700'
              ]"
            >
              Nome do Fornecedor / Empresa *
            </label>
            <input
              v-model="supplierForm.name"
              type="text"
              required
              placeholder="Ex: Pescados Marítimos SP"
              :class="[
                'w-full px-3.5 py-2.5 border rounded-xl text-sm focus:outline-none focus:border-cyan-500 transition',
                themeStore.isDark
                  ? 'bg-zinc-950 border-zinc-800 text-white placeholder:text-zinc-500'
                  : 'bg-zinc-50 border-zinc-200 text-zinc-900 placeholder:text-zinc-400 focus:ring-1 focus:ring-cyan-500 shadow-sm'
              ]"
            />
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label
                :class="[
                  'block text-xs font-semibold uppercase tracking-wider mb-1.5',
                  themeStore.isDark ? 'text-zinc-300' : 'text-zinc-700'
                ]"
              >
                Categoria *
              </label>
              <select
                v-model="supplierForm.category"
                :class="[
                  'w-full px-3.5 py-2.5 border rounded-xl text-sm focus:outline-none focus:border-cyan-500 transition',
                  themeStore.isDark
                    ? 'bg-zinc-950 border-zinc-800 text-white'
                    : 'bg-zinc-50 border-zinc-200 text-zinc-900 focus:ring-1 focus:ring-cyan-500 shadow-sm'
                ]"
              >
                <option v-for="cat in categoriesList" :key="cat" :value="cat">
                  {{ cat }}
                </option>
              </select>
            </div>

            <div>
              <label
                :class="[
                  'block text-xs font-semibold uppercase tracking-wider mb-1.5',
                  themeStore.isDark ? 'text-zinc-300' : 'text-zinc-700'
                ]"
              >
                Telefone / Celular *
              </label>
              <input
                v-model="supplierForm.phone"
                type="text"
                required
                placeholder="(11) 99999-9999"
                :class="[
                  'w-full px-3.5 py-2.5 border rounded-xl text-sm focus:outline-none focus:border-cyan-500 transition',
                  themeStore.isDark
                    ? 'bg-zinc-950 border-zinc-800 text-white placeholder:text-zinc-500'
                    : 'bg-zinc-50 border-zinc-200 text-zinc-900 placeholder:text-zinc-400 focus:ring-1 focus:ring-cyan-500 shadow-sm'
                ]"
              />
            </div>
          </div>

          <div>
            <label
              :class="[
                'block text-xs font-semibold uppercase tracking-wider mb-1.5',
                themeStore.isDark ? 'text-zinc-300' : 'text-zinc-700'
              ]"
            >
              E-mail Comercial *
            </label>
            <input
              v-model="supplierForm.email"
              type="email"
              required
              placeholder="comercial@fornecedor.com.br"
              :class="[
                'w-full px-3.5 py-2.5 border rounded-xl text-sm focus:outline-none focus:border-cyan-500 transition',
                themeStore.isDark
                  ? 'bg-zinc-950 border-zinc-800 text-white placeholder:text-zinc-500'
                  : 'bg-zinc-50 border-zinc-200 text-zinc-900 placeholder:text-zinc-400 focus:ring-1 focus:ring-cyan-500 shadow-sm'
              ]"
            />
          </div>

          <div>
            <label
              :class="[
                'block text-xs font-semibold uppercase tracking-wider mb-1.5',
                themeStore.isDark ? 'text-zinc-300' : 'text-zinc-700'
              ]"
            >
              Descrição / Observações
            </label>
            <textarea
              v-model="supplierForm.description"
              rows="3"
              placeholder="Informações adicionais, prazos de entrega, condições de pagamento..."
              :class="[
                'w-full px-3.5 py-2.5 border rounded-xl text-sm focus:outline-none focus:border-cyan-500 transition resize-none',
                themeStore.isDark
                  ? 'bg-zinc-950 border-zinc-800 text-white placeholder:text-zinc-500'
                  : 'bg-zinc-50 border-zinc-200 text-zinc-900 placeholder:text-zinc-400 focus:ring-1 focus:ring-cyan-500 shadow-sm'
              ]"
            ></textarea>
          </div>

          <div
            :class="[
              'flex items-center justify-end gap-3 pt-4 border-t',
              themeStore.isDark ? 'border-zinc-800' : 'border-zinc-200'
            ]"
          >
            <button
              type="button"
              @click="showSupplierModal = false"
              :class="[
                'px-4 py-2 rounded-xl text-xs font-semibold transition',
                themeStore.isDark
                  ? 'text-zinc-400 hover:text-white hover:bg-zinc-800'
                  : 'text-zinc-600 hover:text-zinc-900 hover:bg-zinc-100'
              ]"
            >
              Cancelar
            </button>

            <button
              type="submit"
              :disabled="isSaving"
              class="px-5 py-2.5 rounded-xl bg-white text-zinc-950 font-bold text-xs hover:bg-zinc-200 transition shadow disabled:opacity-50 border border-zinc-200 dark:border-0"
            >
              {{ isSaving ? "Salvando..." : isEditing ? "Salvar Alterações" : "Cadastrar Fornecedor" }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- ========================================================= -->
    <!-- MODAL: CONFIRMAR EXCLUSÃO -->
    <!-- ========================================================= -->
    <div
      v-if="showDeleteConfirm"
      class="fixed inset-0 bg-black/75 backdrop-blur-sm z-50 flex items-center justify-center p-4"
    >
      <div
        :class="[
          'w-full max-w-sm border rounded-2xl p-6 shadow-2xl space-y-4',
          themeStore.isDark
            ? 'bg-zinc-900 border-zinc-800'
            : 'bg-white border-zinc-200'
        ]"
      >
        <h3
          :class="[
            'text-base font-bold',
            themeStore.isDark ? 'text-white' : 'text-zinc-900'
          ]"
        >Excluir Fornecedor</h3>
        <p
          :class="[
            'text-xs',
            themeStore.isDark ? 'text-zinc-400' : 'text-zinc-600'
          ]"
        >
          Tem certeza que deseja remover <strong :class="themeStore.isDark ? 'text-white' : 'text-zinc-900'">{{ selectedSupplier?.name }}</strong>? Esta ação não pode ser desfeita.
        </p>

        <div
          :class="[
            'flex items-center justify-end gap-3 pt-3 border-t',
            themeStore.isDark ? 'border-zinc-800' : 'border-zinc-200'
          ]"
        >
          <button
            type="button"
            @click="showDeleteConfirm = false"
            :class="[
              'px-3.5 py-2 rounded-xl text-xs font-semibold transition',
              themeStore.isDark
                ? 'text-zinc-400 hover:text-white'
                : 'text-zinc-600 hover:text-zinc-900'
            ]"
          >
            Cancelar
          </button>
          <button
            type="button"
            :disabled="isDeleting"
            @click="deleteSupplier"
            class="px-4 py-2 rounded-xl bg-red-600 hover:bg-red-500 text-white font-bold text-xs transition shadow disabled:opacity-50"
          >
            {{ isDeleting ? "Excluindo..." : "Confirmar Exclusão" }}
          </button>
        </div>
      </div>
    </div>

  </div>
</template>
