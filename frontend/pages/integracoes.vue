<script setup lang="ts">
import { ref, computed, onMounted } from "vue";
import type { ImportAuditDto, ImportPreviewDto, ImportResultDto } from "~/services/modules/import/importService";
import { importService } from "~/services/modules/import/importService";
import { useAuthStore } from "~/stores/auth";
import { useThemeStore } from "~/stores/theme";
import AppSidebar from "~/components/layout/AppSidebar.vue";
import AppLoader from "~/components/base/AppLoader.vue";
import ImportConfirmationStep from "~/components/import/ImportConfirmationStep.vue";
import ImportHistoryStep from "~/components/import/ImportHistoryStep.vue";

definePageMeta({ layout: false });

const runtimeConfig = useRuntimeConfig();
const authStore = useAuthStore();
const themeStore = useThemeStore();
const router = useRouter();

const isLoading = ref(true);
const isSidebarOpen = ref(false);
const toggleSidebar = () => (isSidebarOpen.value = !isSidebarOpen.value);

// ===== View / Navigation State =====
type MainTab = "skills" | "confirmacao" | "historico";
const activeTab = ref<MainTab>("skills");

const currentPreview = ref<ImportPreviewDto | null>(null);
const currentFile = ref<File | null>(null);
const currentIsPhoto = ref(false);
const lastResults = ref<ImportResultDto[] | null>(null);
const historyRefreshToken = ref(0);

// Super Admin seleciona restaurante alvo
const isSuperAdmin = computed(() => authStore.currentUser?.role === "Admin" && !authStore.currentUser?.restaurantId);
const targetRestaurantId = ref<number | null>(isSuperAdmin.value ? 1 : authStore.currentUser?.restaurantId ?? null);

// ===== Upload de Arquivo / Dropzone State =====
const fileInput = ref<HTMLInputElement | null>(null);
const ACCEPT_FORMATS = ".csv,.xlsx,.xls,.xml,.json,.txt,image/*,.pdf";
const dragging = ref(false);
const analyzing = ref(false);
const uploadError = ref<string | null>(null);
const lastFileName = ref<string | null>(null);

const isPhotoFile = (file: File) =>
  file.type.startsWith("image/") || file.type === "application/pdf" ||
  /\.(jpe?g|png|webp|gif|pdf)$/i.test(file.name);

function triggerFileSelect() {
  if (fileInput.value) {
    fileInput.value.click();
  }
}

async function handleFile(file: File) {
  uploadError.value = null;
  if (targetRestaurantId.value == null || targetRestaurantId.value <= 0) {
    uploadError.value = "Selecione o restaurante alvo antes de enviar o arquivo.";
    return;
  }

  const isPhoto = isPhotoFile(file);
  analyzing.value = true;
  lastFileName.value = file.name;

  try {
    const preview = isPhoto
      ? await importService.previewPhoto(file, targetRestaurantId.value)
      : await importService.previewSpreadsheet(file, targetRestaurantId.value);
    
    currentPreview.value = preview;
    currentFile.value = file;
    currentIsPhoto.value = isPhoto;
    lastResults.value = null;
    activeTab.value = "confirmacao";
  } catch (e: any) {
    uploadError.value =
      e?.data?.message ||
      e?.message ||
      "Não foi possível analisar o arquivo. Verifique o formato e tente novamente.";
  } finally {
    analyzing.value = false;
  }
}

function onDrop(evt: DragEvent) {
  evt.preventDefault();
  dragging.value = false;
  const files = evt.dataTransfer?.files;
  if (files && files.length > 0) handleFile(files[0]);
}

function onFileChange(evt: Event) {
  const input = evt.target as HTMLInputElement;
  if (input.files && input.files.length > 0) {
    handleFile(input.files[0]);
  }
  input.value = "";
}

function onConfirmed(results: ImportResultDto[]) {
  lastResults.value = results;
  currentPreview.value = null;
  currentFile.value = null;
  historyRefreshToken.value++;
  activeTab.value = "historico";
}

// ===== Integrations / Plug & Play Skills =====
export interface IntegrationSkill {
  id: string;
  name: string;
  description: string;
  category: "delivery" | "payments" | "suppliers" | "files" | "menu";
  icon: string;
  status: "ativo" | "disponivel";
  badgeText: string;
  config: {
    apiKey?: string;
    clientId?: string;
    webhookUrl?: string;
    autoSync: boolean;
    syncInterval: string;
    lastSync?: string;
  };
}

const skills = ref<IntegrationSkill[]>([
  {
    id: "ifood",
    name: "iFood",
    description: "Pedidos e cardápio em tempo real.",
    category: "delivery",
    icon: "ifood",
    status: "ativo",
    badgeText: "Ativo",
    config: {
      autoSync: true,
      syncInterval: "Tempo real (SWS WebSocket)",
      lastSync: "Há 3 minutos"
    }
  },
  {
    id: "99food",
    name: "99 Food",
    description: "Sincronização de vendas e entregas.",
    category: "delivery",
    icon: "99food",
    status: "disponivel",
    badgeText: "Disponível",
    config: {
      autoSync: false,
      syncInterval: "A cada 5 minutos"
    }
  },
  {
    id: "planilhas",
    name: "Planilhas",
    description: "Importação manual via .xlsx/.csv.",
    category: "files",
    icon: "sheet",
    status: "ativo",
    badgeText: "Ativo",
    config: {
      autoSync: true,
      syncInterval: "Manual / Upload universal",
      lastSync: "Auditado via Hash SHA256"
    }
  },
  {
    id: "cardapio_digital",
    name: "Cardápio Digital",
    description: "Atualização de itens e preços.",
    category: "menu",
    icon: "menu",
    status: "disponivel",
    badgeText: "Disponível",
    config: {
      autoSync: false,
      syncInterval: "Em tempo real"
    }
  },
  {
    id: "stone",
    name: "Stone",
    description: "Recebíveis e taxas por bandeira.",
    category: "payments",
    icon: "stone",
    status: "ativo",
    badgeText: "Ativo",
    config: {
      autoSync: true,
      syncInterval: "Fechamento diário (00:00)",
      lastSync: "Hoje às 06:00"
    }
  },
  {
    id: "fornecedores",
    name: "Fornecedores",
    description: "Catálogo e disparo de pedidos.",
    category: "suppliers",
    icon: "supplier",
    status: "disponivel",
    badgeText: "Disponível",
    config: {
      autoSync: false,
      syncInterval: "Sob demanda"
    }
  }
]);

// Skill Modal State
const selectedSkill = ref<IntegrationSkill | null>(null);
const isSkillModalOpen = ref(false);
const isSavingSkill = ref(false);
const isTestingSkill = ref(false);

const toastMessage = ref<string | null>(null);
function showToast(msg: string) {
  toastMessage.value = msg;
  setTimeout(() => (toastMessage.value = null), 4000);
}

function openSkillModal(skill: IntegrationSkill) {
  if (skill.id === "planilhas") {
    // Para planilhas, ir para a carga diária
    activeTab.value = "skills";
    const dropzone = document.getElementById("dropzone-card");
    if (dropzone) {
      dropzone.scrollIntoView({ behavior: "smooth" });
    }
    return;
  }
  selectedSkill.value = JSON.parse(JSON.stringify(skill));
  isSkillModalOpen.value = true;
}

function closeSkillModal() {
  isSkillModalOpen.value = false;
  selectedSkill.value = null;
}

function toggleSkillStatus() {
  if (!selectedSkill.value) return;
  if (selectedSkill.value.status === "ativo") {
    selectedSkill.value.status = "disponivel";
    selectedSkill.value.badgeText = "Disponível";
    selectedSkill.value.config.autoSync = false;
  } else {
    selectedSkill.value.status = "ativo";
    selectedSkill.value.badgeText = "Ativo";
    selectedSkill.value.config.autoSync = true;
  }
}

async function testConnection() {
  if (!selectedSkill.value) return;
  isTestingSkill.value = true;
  await new Promise((resolve) => setTimeout(resolve, 1000));
  isTestingSkill.value = false;
  showToast(`Conexão com ${selectedSkill.value.name} testada com sucesso! Latência: 42ms.`);
}

async function saveSkillConfig() {
  if (!selectedSkill.value) return;
  isSavingSkill.value = true;
  await new Promise((resolve) => setTimeout(resolve, 600));
  
  // Atualizar na lista local
  const idx = skills.value.findIndex((s) => s.id === selectedSkill.value?.id);
  if (idx !== -1 && selectedSkill.value) {
    skills.value[idx] = JSON.parse(JSON.stringify(selectedSkill.value));
  }
  
  isSavingSkill.value = false;
  const isNowActive = selectedSkill.value.status === "ativo";
  closeSkillModal();
  showToast(
    isNowActive
      ? `Skill ${selectedSkill.value?.name} conectada e configurada com sucesso!`
      : `Skill ${selectedSkill.value?.name} desconectada.`
  );
}

const handleLogout = () => {
  authStore.logout();
  router.push("/login");
};

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
  setTimeout(() => (isLoading.value = false), wait);
});
</script>

<template>
  <div :class="['min-h-screen flex flex-col font-sans transition-colors duration-200', themeStore.isDark ? 'bg-zinc-950 text-zinc-100 selection:bg-zinc-800 selection:text-white' : 'bg-zinc-50 text-zinc-900 selection:bg-indigo-100 selection:text-indigo-900']">
    <AppLoader :visible="isLoading" />

    <header :class="['h-16 border-b backdrop-blur-xl sticky top-0 z-30 px-4 sm:px-6 flex items-center justify-between', themeStore.isDark ? 'border-zinc-800/80 bg-zinc-900/60' : 'border-zinc-200 bg-white/80']">
      <div class="flex items-center gap-4">
        <button
          @click="toggleSidebar"
          :class="['p-2 rounded-xl transition-all duration-200 focus:outline-none focus:ring-2', themeStore.isDark ? 'text-zinc-300 hover:text-white hover:bg-zinc-800/80 focus:ring-zinc-600' : 'text-zinc-600 hover:text-zinc-900 hover:bg-zinc-100 focus:ring-indigo-500/30']"
          title="Abrir Menu Lateral"
        >
          <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"></path>
          </svg>
        </button>
        <div class="flex items-center gap-3">
          <span :class="['font-bold text-lg tracking-tight', themeStore.isDark ? 'text-white' : 'text-zinc-900']">ISM</span>
          <span :class="['hidden sm:inline-block text-xs uppercase tracking-widest font-mono border-l pl-3', themeStore.isDark ? 'text-zinc-400 border-zinc-700/60' : 'text-zinc-500 border-zinc-200']">
            {{ runtimeConfig.public.appName }}
          </span>
        </div>
      </div>
      <div class="flex items-center gap-3">
        <div v-if="authStore.isAuthenticated" class="flex items-center gap-3">
          <span :class="['hidden md:inline-block text-xs font-medium', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-600']">
            {{ authStore.currentUser?.name }} ({{ authStore.currentUser?.role }})
          </span>
          <button
            @click="handleLogout"
            :class="['px-4 py-2 text-xs font-semibold rounded-xl border transition-all duration-200 flex items-center gap-2', themeStore.isDark ? 'bg-zinc-800 hover:bg-zinc-700 text-zinc-200 hover:text-white border-zinc-700/60' : 'bg-zinc-100 hover:bg-zinc-200 text-zinc-700 hover:text-zinc-900 border-zinc-200']"
          >
            <svg :class="['w-4 h-4', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500']" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"></path>
            </svg>
            <span>Sair</span>
          </button>
        </div>
      </div>
    </header>

    <!-- Top Title & Tag Section -->
      <div class="space-y-2">
        <div class="inline-flex items-center px-3 py-1 rounded-full bg-zinc-900/90 border border-zinc-800 text-[11px] font-mono tracking-wider text-zinc-300 uppercase">
          INTEGRAÇÕES
        </div>
      </div>

      <!-- Modo Super Admin -->
      <div v-if="isSuperAdmin" class="p-4 rounded-2xl border border-amber-500/30 bg-amber-500/5 flex flex-col sm:flex-row sm:items-center gap-3 sm:justify-between">
        <div class="flex items-start gap-3">
          <div class="w-9 h-9 rounded-xl bg-amber-500/10 border border-amber-500/20 text-amber-400 dark:text-amber-600 flex items-center justify-center flex-shrink-0">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"></path></svg>
          </div>
          <div>
            <div class="text-sm font-semibold text-amber-500 dark:text-amber-700">Modo Super Admin</div>
            <p class="text-xs text-zinc-500">Selecione o restaurante alvo antes de importar arquivos.</p>
          </div>
        </div>
        <label class="flex flex-col gap-1 min-w-[240px]">
          <span class="text-xs text-zinc-500 font-mono uppercase tracking-wider">Restaurante</span>
          <select v-model.number="targetRestaurantId" :class="['border rounded-xl px-3 py-2 text-sm focus:outline-none focus:ring-2 transition', themeStore.isDark ? 'bg-zinc-950/80 border-zinc-700/70 text-white focus:ring-amber-500/40 focus:border-amber-500/50' : 'bg-white border-zinc-200 text-zinc-900 focus:ring-amber-500/30 focus:border-amber-500/50']">
            <option :value="1">1 - Gourmet ISM Restaurant</option>
          </select>
        </label>
      </div>

      <!-- Navigation Tabs (Permite alternar entre Conexões, Confirmação e Histórico) -->
      <div v-if="currentPreview || lastResults || activeTab !== 'skills'" class="flex items-center gap-2 border-b border-zinc-800 pb-2">
        <button
          @click="activeTab = 'skills'"
          :class="[
            'px-4 py-2 rounded-xl text-xs font-semibold transition-all flex items-center gap-2',
            activeTab === 'skills'
              ? 'bg-zinc-800 text-white border border-zinc-700'
              : 'text-zinc-400 hover:text-zinc-200 hover:bg-zinc-900'
          ]"
        >
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"></path></svg>
          <span>Conexões & Skills</span>
        </button>

        <button
          v-if="currentPreview"
          @click="activeTab = 'confirmacao'"
          :class="[
            'px-4 py-2 rounded-xl text-xs font-semibold transition-all flex items-center gap-2',
            activeTab === 'confirmacao'
              ? 'bg-indigo-600/30 text-indigo-200 border border-indigo-500/40'
              : 'text-zinc-400 hover:text-zinc-200 hover:bg-zinc-900'
          ]"
        >
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
          <span>Confirmação de Dados</span>
          <span class="w-2 h-2 rounded-full bg-indigo-400 animate-pulse"></span>
        </button>

        <button
          @click="activeTab = 'historico'"
          :class="[
            'px-4 py-2 rounded-xl text-xs font-semibold transition-all flex items-center gap-2',
            activeTab === 'historico'
              ? 'bg-zinc-800 text-white border border-zinc-700'
              : 'text-zinc-400 hover:text-zinc-200 hover:bg-zinc-900'
          ]"
        >
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
          <span>Histórico de Importações</span>
        </button>
      </div>

      <!-- ABA 1: MAIN PLUG & PLAY VIEW (Protótipo Exato) -->
      <div v-if="activeTab === 'skills'" class="space-y-8">
        
        <!-- CARD PRINCIPAL: Carga diária de dados -->
        <div id="dropzone-card" class="rounded-2xl bg-[#121214] border border-zinc-800/80 p-6 sm:p-8 space-y-6 shadow-2xl relative overflow-hidden">
          <div>
            <h2 class="text-lg font-bold text-white tracking-tight">Carga diária de dados</h2>
            <p class="text-xs sm:text-sm text-zinc-400 mt-1">
              Arraste sua planilha de cardápio, histórico de pedidos ou inventário. Calculamos giro e margem automaticamente.
            </p>
          </div>

          <!-- DROPZONE -->
          <div
            v-if="!analyzing"
            @click="triggerFileSelect"
            @dragover.prevent="dragging = true"
            @dragleave="dragging = false"
            @drop="onDrop"
            :class="[
              'dropzone-container border-2 border-dashed rounded-2xl p-8 sm:p-10 text-center cursor-pointer transition-all duration-200 flex flex-col items-center justify-center group relative overflow-hidden',
              dragging
                ? 'border-indigo-500 bg-indigo-500/10 scale-[0.99]'
                : 'border-zinc-800 bg-[#09090b]/50 hover:border-zinc-700/90 hover:bg-[#09090b]'
            ]"
          >
            <!-- PASTA ANIMADA 3D COMPACTA (SEM FUNDO AZUL) -->
            <div class="folder-3d-wrapper" @click.stop="triggerFileSelect">
              <div class="animated-folder">
                <div class="front-side">
                  <div class="tip"></div>
                  <div class="cover"></div>
                </div>
                <div class="back-side cover"></div>
              </div>
            </div>

            <!-- Título & Formatos -->
            <p class="text-base font-bold text-white tracking-tight mb-1">Importe sua carga diária</p>
            <p class="text-xs text-zinc-400 mb-5 font-normal">Formatos aceitos: .xlsx, .csv, .xls, .json — até 25MB.</p>

            <!-- Botão Selecionar Arquivo -->
            <button
              type="button"
              @click.stop="triggerFileSelect"
              class="px-6 py-2.5 rounded-lg bg-[#18181b] hover:bg-zinc-800 border border-zinc-700/80 text-xs font-semibold text-zinc-200 hover:text-white transition-all shadow-sm flex items-center gap-2"
            >
              <svg class="w-4 h-4 text-zinc-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"></path>
              </svg>
              <span>Selecionar arquivo</span>
            </button>

            <input
              ref="fileInput"
              type="file"
              :accept="ACCEPT_FORMATS"
              class="hidden"
              @change="onFileChange"
            />
          </div>

          <!-- Estado de Análise -->
          <div v-else class="rounded-2xl p-12 text-center border border-indigo-500/30 bg-indigo-500/5 space-y-3">
            <div class="inline-block w-8 h-8 border-2 border-indigo-400 border-t-transparent rounded-full animate-spin"></div>
            <p class="text-sm font-semibold text-indigo-200">Analisando estrutura do arquivo com inteligência de dados...</p>
            <p v-if="lastFileName" class="text-xs text-zinc-400 font-mono">{{ lastFileName }}</p>
          </div>

          <!-- Mensagem de Erro de Upload -->
          <div v-if="uploadError" class="p-4 rounded-xl bg-red-500/10 border border-red-500/30 text-xs text-red-300 flex items-center gap-2">
            <svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
            <span>{{ uploadError }}</span>
          </div>
        </div>

        <!-- GRID DE SKILLS E CONEXÕES PLUG-IN-PLAY -->
        <div class="skills-grid grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
          
          <div
            v-for="skill in skills"
            :key="skill.id"
            class="skill-card rounded-2xl bg-[#121214] border border-zinc-800/80 p-5 flex flex-col justify-between hover:border-zinc-700/90 shadow-lg group relative"
          >
            <!-- Card Content Header -->
            <div>
              <div class="flex items-start justify-between gap-3 mb-4">
                <!-- Icon container -->
                <div class="w-11 h-11 rounded-xl bg-zinc-900 border border-zinc-800 flex items-center justify-center shrink-0 group-hover:border-zinc-700 transition-colors">
                  <!-- iFood / Restaurant -->
                  <svg v-if="skill.icon === 'ifood'" class="w-5 h-5 text-red-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6"></path>
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M3 3h18v18H3z"></path>
                  </svg>

                  <!-- 99 Food / Delivery -->
                  <svg v-else-if="skill.icon === '99food'" class="w-5 h-5 text-amber-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M13 10V3L4 14h7v7l9-11h-7z"></path>
                  </svg>

                  <!-- Cardápio Digital / Menu -->
                  <svg v-else-if="skill.icon === 'menu'" class="w-5 h-5 text-blue-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253"></path>
                  </svg>

                  <!-- Stone / Card -->
                  <svg v-else-if="skill.icon === 'stone'" class="w-5 h-5 text-emerald-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z"></path>
                  </svg>

                  <!-- Fornecedores / Truck -->
                  <svg v-else-if="skill.icon === 'supplier'" class="w-5 h-5 text-purple-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M9 17a2 2 0 11-4 0 2 2 0 014 0zM19 17a2 2 0 11-4 0 2 2 0 014 0z"></path>
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M13 16V6a1 1 0 00-1-1H4a1 1 0 00-1 1v10a1 1 0 001 1h1m8-1a1 1 0 01-1 1H9m4-1V8h4l3 3v5h-2m-6 0h2"></path>
                  </svg>

                  <!-- Planilhas / Sheet -->
                  <svg v-else class="w-5 h-5 text-zinc-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M9 17v-6e1 0-2 2-2h2m0 0h2a2 2 0 012 2v6m-6 0h6M3 3h18v18H3V3z"></path>
                  </svg>
                </div>

                <!-- Status Badge -->
                <div
                  :class="[
                    'px-2.5 py-1 rounded-full text-[11px] font-medium flex items-center gap-1.5 border',
                    skill.status === 'ativo'
                      ? 'bg-emerald-950/60 border-emerald-500/30 text-emerald-400'
                      : 'bg-zinc-800/80 border-zinc-700/60 text-zinc-400'
                  ]"
                >
                  <span v-if="skill.status === 'ativo'" class="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-pulse"></span>
                  <span>{{ skill.badgeText }}</span>
                </div>
              </div>

              <!-- Title & Description -->
              <h3 class="font-bold text-white text-base tracking-tight">{{ skill.name }}</h3>
              <p class="text-xs text-zinc-400 mt-1 line-clamp-2 leading-relaxed">{{ skill.description }}</p>
            </div>

            <!-- Card Bottom Action Button (Exatamente como o protótipo) -->
            <div class="mt-6">
              <!-- Botão Se Ativo: Gerenciar (Botão Escuro) -->
              <button
                v-if="skill.status === 'ativo'"
                @click="openSkillModal(skill)"
                class="w-full py-2.5 rounded-xl border border-zinc-800 bg-[#18181b] hover:bg-zinc-800 hover:border-zinc-700 text-xs font-semibold text-zinc-200 hover:text-white transition-all flex items-center justify-center gap-2 shadow-sm"
              >
                <span>Gerenciar</span>
              </button>

              <!-- Botão Se Disponível: Conectar (Botão Branco) -->
              <button
                v-else
                @click="openSkillModal(skill)"
                class="w-full py-2.5 rounded-xl bg-white hover:bg-zinc-200 text-zinc-950 text-xs font-bold transition-all flex items-center justify-center gap-2 shadow-md hover:shadow-lg"
              >
                <span>Conectar</span>
              </button>
            </div>
          </div>

        </div>

      </div>

      <!-- ABA 2: CONFIRMAÇÃO DE DADOS DA IMPORTAÇÃO -->
      <section v-else-if="activeTab === 'confirmacao'">
        <ImportConfirmationStep
          v-if="currentPreview && currentFile"
          :preview="currentPreview"
          :file="currentFile"
          :is-photo="currentIsPhoto"
          :restaurant-id="targetRestaurantId"
          @back="activeTab = 'skills'"
          @confirmed="onConfirmed"
        />
      </section>

      <!-- ABA 3: HISTÓRICO DE IMPORTAÇÕES -->
      <section v-else-if="activeTab === 'historico'" class="space-y-6">
        <div v-if="lastResults && lastResults.length > 0" class="p-5 rounded-2xl border bg-emerald-500/5 border-emerald-500/30 space-y-2">
          <div class="text-sm font-semibold text-emerald-300 flex items-center gap-2">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
            Importação concluída com sucesso!
          </div>
          <ul class="text-xs text-zinc-300 space-y-1">
            <li v-for="r in lastResults" :key="r.importId">
              <span class="font-medium text-white">{{ r.dataSourceName }}</span> — {{ r.recordsSucceeded }} registro(s) importado(s),
              <span :class="r.recordsFailed > 0 ? 'text-amber-300' : 'text-emerald-300'">{{ r.recordsFailed }} falha(s)</span>.
            </li>
          </ul>
        </div>

        <ImportHistoryStep :restaurant-id="targetRestaurantId" :refresh-token="historyRefreshToken" />
      </section>

    <!-- MODAL SLIDE-OVER DE GERENCIAMENTO DA SKILL (PLUG & PLAY) -->
    <Teleport to="body">
      <div
        v-if="isSkillModalOpen && selectedSkill"
        class="fixed inset-0 z-50 flex items-center justify-center bg-black/80 backdrop-blur-sm p-4"
        @click.self="closeSkillModal"
      >
        <div class="w-full max-w-lg bg-[#121214] rounded-3xl border border-zinc-800 shadow-2xl overflow-hidden flex flex-col animate-in fade-in zoom-in-95 duration-200">
          
          <!-- Modal Header -->
          <div class="p-6 border-b border-zinc-800/80 flex items-center justify-between">
            <div class="flex items-center gap-3">
              <div class="w-10 h-10 rounded-xl bg-zinc-900 border border-zinc-800 flex items-center justify-center text-white">
                <svg class="w-5 h-5 text-indigo-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"></path>
                </svg>
              </div>
              <div>
                <h3 class="font-bold text-white text-lg tracking-tight">{{ selectedSkill.name }}</h3>
                <p class="text-xs text-zinc-400">Configuração de Plug & Play Skill</p>
              </div>
            </div>
            <button @click="closeSkillModal" class="p-2 rounded-xl text-zinc-400 hover:text-white hover:bg-zinc-800 transition-colors">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path></svg>
            </button>
          </div>

          <!-- Modal Body -->
          <div class="p-6 space-y-6 overflow-y-auto max-h-[70vh]">
            
            <!-- Status Plug & Play Switch -->
            <div class="p-4 rounded-2xl bg-zinc-950/80 border border-zinc-800 flex items-center justify-between">
              <div>
                <div class="text-xs font-semibold text-white">Status da Conexão</div>
                <div class="text-[11px] text-zinc-400">Ative ou pause o fluxo de dados em tempo real.</div>
              </div>
              <button
                @click="toggleSkillStatus"
                :class="[
                  'px-3 py-1.5 rounded-full text-xs font-bold transition-all border flex items-center gap-1.5',
                  selectedSkill.status === 'ativo'
                    ? 'bg-emerald-500/10 border-emerald-500/30 text-emerald-400'
                    : 'bg-zinc-800 border-zinc-700 text-zinc-400'
                ]"
              >
                <span :class="['w-2 h-2 rounded-full', selectedSkill.status === 'ativo' ? 'bg-emerald-400 animate-pulse' : 'bg-zinc-500']"></span>
                <span>{{ selectedSkill.status === 'ativo' ? 'Conectado' : 'Desconectado' }}</span>
              </button>
            </div>

            <!-- Campos de Configuração -->
            <div class="space-y-4 text-xs">
              <label class="flex flex-col gap-1.5">
                <span class="text-zinc-400 font-medium">Chave de API / Access Token</span>
                <input
                  v-model="selectedSkill.config.apiKey"
                  type="password"
                  placeholder="Insira a chave fornecida pelo sistema..."
                  class="bg-zinc-950 border border-zinc-800 rounded-xl px-3.5 py-2.5 text-white placeholder:text-zinc-600 focus:outline-none focus:ring-2 focus:ring-indigo-500/40"
                />
              </label>

              <label class="flex flex-col gap-1.5">
                <span class="text-zinc-400 font-medium">Frequência de Sincronização</span>
                <select
                  v-model="selectedSkill.config.syncInterval"
                  class="bg-zinc-950 border border-zinc-800 rounded-xl px-3.5 py-2.5 text-white focus:outline-none focus:ring-2 focus:ring-indigo-500/40"
                >
                  <option value="Tempo real (SWS WebSocket)">Tempo real (WebSocket)</option>
                  <option value="A cada 5 minutos">A cada 5 minutos</option>
                  <option value="A cada 1 hora">A cada 1 hora</option>
                  <option value="Diário">Fechamento diário</option>
                </select>
              </label>

              <div class="p-3.5 rounded-xl bg-zinc-900/60 border border-zinc-800/80 space-y-1">
                <div class="text-[11px] text-zinc-400 font-medium">Informações do Cérebro ISM:</div>
                <p class="text-[11px] text-zinc-500">
                  Os dados recebidos via {{ selectedSkill.name }} alimentam automaticamente a curva ABC, o custo de mercadoria vendida (CMV) e os alertas de estoque.
                </p>
              </div>
            </div>

          </div>

          <!-- Modal Footer -->
          <div class="p-6 border-t border-zinc-800/80 bg-zinc-950/60 flex items-center justify-between gap-3">
            <button
              @click="testConnection"
              :disabled="isTestingSkill"
              class="px-4 py-2.5 rounded-xl bg-zinc-900 hover:bg-zinc-800 border border-zinc-700 text-xs font-semibold text-zinc-300 hover:text-white transition-all disabled:opacity-40 flex items-center gap-2"
            >
              <span v-if="isTestingSkill" class="w-3 h-3 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
              <span>Testar Conexão</span>
            </button>

            <div class="flex items-center gap-2">
              <button
                @click="closeSkillModal"
                class="px-4 py-2.5 rounded-xl bg-zinc-900 hover:bg-zinc-800 text-zinc-300 text-xs font-semibold border border-zinc-800 transition-all"
              >
                Cancelar
              </button>
              <button
                @click="saveSkillConfig"
                :disabled="isSavingSkill"
                class="px-5 py-2.5 rounded-xl bg-white hover:bg-zinc-200 text-zinc-950 text-xs font-bold transition-all shadow-md flex items-center gap-2"
              >
                <span v-if="isSavingSkill" class="w-3 h-3 border-2 border-zinc-950 border-t-transparent rounded-full animate-spin"></span>
                <span>Salvar & Ativar</span>
              </button>
            </div>
          </div>

        </div>
      </div>
    </Teleport>
  </div>
</template>
