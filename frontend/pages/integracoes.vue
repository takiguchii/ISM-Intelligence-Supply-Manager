<script setup lang="ts">
import { ref, computed, onMounted } from "vue";
import type { ImportAuditDto, ImportPreviewDto, ImportResultDto } from "~/services/modules/import/importService";
import { importService } from "~/services/modules/import/importService";
import { apiClientWithAuth } from "~/services/api/client";
import { useAuthStore } from "~/stores/auth";
import ImportConfirmationStep from "~/components/import/ImportConfirmationStep.vue";
import ImportHistoryStep from "~/components/import/ImportHistoryStep.vue";


const runtimeConfig = useRuntimeConfig();
const authStore = useAuthStore();
const router = useRouter();

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

// Super Admin restaurante alvo
const isSuperAdmin = computed(() => authStore.currentUser?.role === "Admin" && !authStore.currentUser?.restaurantId);
const targetRestaurantId = ref<number | null>(authStore.currentUser?.restaurantId ?? null);
const restaurants = ref<Array<{ id: number; name: string }>>([]);
const restaurantsError = ref("");

watch(targetRestaurantId, () => {
  currentPreview.value = null;
  currentFile.value = null;
  lastResults.value = null;
  activeTab.value = "skills";
});

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
interface IntegrationSkill {
  id: string;
  name: string;
  description: string;
  icon: string;
}

const skills: IntegrationSkill[] = [
  { id: "ifood", name: "iFood", description: "Integração de pedidos e cardápio.", icon: "ifood" },
  { id: "99food", name: "99 Food", description: "Integração de vendas e entregas.", icon: "99food" },
  { id: "planilhas", name: "Planilhas", description: "Importação manual de arquivos.", icon: "sheet" },
  { id: "cardapio_digital", name: "Cardápio Digital", description: "Integração de itens e preços.", icon: "menu" },
  { id: "stone", name: "Stone", description: "Integração de recebíveis e taxas.", icon: "stone" },
  { id: "fornecedores", name: "Fornecedores", description: "Integração de catálogos e pedidos.", icon: "supplier" }
];

function openImport() {
  activeTab.value = "skills";
  document.getElementById("dropzone-card")?.scrollIntoView({ behavior: "smooth" });
}

const handleLogout = () => {
  authStore.logout();
  router.push("/login");
};

onMounted(async () => {
  authStore.initFromStorage();
  if (!authStore.isAuthenticated) {
    router.push("/login");
    return;
  }
  if (isSuperAdmin.value) {
    targetRestaurantId.value = null;
    try {
      restaurants.value = await apiClientWithAuth<Array<{ id: number; name: string }>>("/api/restaurants");
    } catch (error) {
      console.error("Erro ao carregar restaurantes:", error);
      restaurantsError.value = "Não foi possível carregar os restaurantes. Recarregue a página para tentar novamente.";
    }
  } else {
    targetRestaurantId.value = authStore.currentUser?.restaurantId ?? null;
  }
});
</script>

<template>
  <div class="light-page max-w-7xl w-full mx-auto px-4 sm:px-6 py-8 space-y-8">

    <!-- Top Title & Tag Section -->
      <div class="space-y-2">
        <div class="inline-flex items-center px-3 py-1 rounded-full bg-zinc-900/90 border border-zinc-800 text-[11px] font-mono tracking-wider text-zinc-300 uppercase">
          INTEGRAÇÕES
        </div>
        <h1 class="text-3xl sm:text-4xl font-bold text-white tracking-tight">Conexões & importação</h1>
        <p class="text-zinc-400 text-sm sm:text-base max-w-3xl leading-relaxed">
          Conecte seus sistemas ou envie dados manualmente — o ISM consolida tudo num único cérebro.
        </p>
      </div>

      <!-- Modo Super Admin -->
      <div v-if="isSuperAdmin" class="p-4 rounded-2xl border border-amber-500/30 bg-amber-500/5 flex flex-col sm:flex-row sm:items-center gap-3 sm:justify-between">
        <div class="flex items-start gap-3">
          <div class="w-8 h-8 rounded-xl bg-amber-500/10 border border-amber-500/20 text-amber-400 flex items-center justify-center shrink-0">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"></path></svg>
          </div>
          <div>
            <div class="text-xs font-semibold text-amber-300">Modo Super Admin</div>
            <p class="text-[11px] text-zinc-400">Selecione o restaurante alvo para gerenciar conexões e cargas de dados.</p>
          </div>
        </div>
        <label class="flex items-center gap-2">
          <span class="text-xs text-zinc-400 font-mono">Restaurante:</span>
          <select v-model.number="targetRestaurantId" class="bg-zinc-950 border border-zinc-700/80 rounded-xl px-3 py-1.5 text-xs text-white focus:outline-none focus:ring-2 focus:ring-amber-500/40">
            <option :value="null">Selecione um restaurante</option>
            <option v-for="restaurant in restaurants" :key="restaurant.id" :value="restaurant.id">{{ restaurant.id }} · {{ restaurant.name }}</option>
          </select>
        </label>
      </div>

      <p v-if="restaurantsError" role="alert" class="text-sm text-red-400">{{ restaurantsError }}</p>

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

                <div class="px-2.5 py-1 rounded-full text-[11px] font-medium border border-zinc-700/60 text-zinc-400">
                  {{ skill.id === 'planilhas' ? 'Importação manual' : 'Indisponível' }}
                </div>
              </div>

              <!-- Title & Description -->
              <h3 class="font-bold text-white text-base tracking-tight">{{ skill.name }}</h3>
              <p class="text-xs text-zinc-400 mt-1 line-clamp-2 leading-relaxed">{{ skill.description }}</p>
            </div>

            <div class="mt-6">
              <button v-if="skill.id === 'planilhas'" type="button" @click="openImport" class="w-full py-2.5 rounded-xl border border-zinc-800 bg-[#18181b] hover:bg-zinc-800 text-xs font-semibold text-zinc-200">
                Importar arquivo
              </button>
              <p v-else class="text-xs text-zinc-500">Esta integração ainda não está disponível.</p>
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


  </div>
</template>

<style scoped>
.skills-grid {
  perspective: 1000px;
}

.skill-card {
  transition: transform 400ms cubic-bezier(0.4, 0, 0.2, 1),
              filter 400ms cubic-bezier(0.4, 0, 0.2, 1),
              border-color 200ms ease,
              box-shadow 200ms ease;
  will-change: transform, filter;
}

.skills-grid:hover > .skill-card:not(:hover) {
  filter: blur(6px) opacity(0.5);
  transform: scale(0.96);
}

.skill-card:hover {
  transform: scale(1.04);
  filter: blur(0px) opacity(1);
  z-index: 10;
}

/* ESTILOS DA PASTA ANIMADA 3D COMPACTA (SEM FUNDO AZUL) */
.folder-3d-wrapper {
  position: relative;
  width: 70px;
  height: 52px;
  margin: 0 auto 14px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
}

.animated-folder {
  position: relative;
  width: 64px;
  height: 42px;
  animation: floatFolder 2.5s infinite ease-in-out;
  transition: transform 350ms ease;
}

.animated-folder:hover,
.dropzone-container:hover .animated-folder {
  transform: scale(1.08);
}

.animated-folder .front-side,
.animated-folder .back-side {
  position: absolute;
  top: 0;
  left: 0;
  width: 64px;
  height: 42px;
  transition: transform 350ms cubic-bezier(0.4, 0, 0.2, 1);
  transform-origin: bottom center;
}

.animated-folder .back-side::before,
.animated-folder .back-side::after {
  content: "";
  display: block;
  background-color: #ffffff;
  opacity: 0.55;
  width: 64px;
  height: 42px;
  position: absolute;
  top: 0;
  left: 0;
  transform-origin: bottom center;
  border-radius: 8px;
  transition: transform 350ms cubic-bezier(0.4, 0, 0.2, 1);
  z-index: 0;
}

.dropzone-container:hover .back-side::before,
.folder-3d-wrapper:hover .back-side::before {
  transform: rotateX(-8deg) skewX(6deg) translateY(-2px);
}

.dropzone-container:hover .back-side::after,
.folder-3d-wrapper:hover .back-side::after {
  transform: rotateX(-18deg) skewX(12deg) translateY(-4px);
}

.animated-folder .front-side {
  z-index: 1;
}

.dropzone-container:hover .front-side,
.folder-3d-wrapper:hover .front-side {
  transform: rotateX(-38deg) skewX(14deg);
}

.animated-folder .tip {
  background: linear-gradient(135deg, #ff9a56, #ff6f56);
  width: 44px;
  height: 12px;
  border-radius: 6px 6px 0 0;
  box-shadow: 0 3px 8px rgba(0, 0, 0, 0.2);
  position: absolute;
  top: -6px;
  left: 0;
  z-index: 2;
}

.animated-folder .cover {
  background: linear-gradient(135deg, #ffe563, #ffc663);
  width: 64px;
  height: 42px;
  box-shadow: 0 8px 20px rgba(0, 0, 0, 0.35);
  border-radius: 7px;
}

@keyframes floatFolder {
  0% {
    transform: translateY(0px);
  }
  50% {
    transform: translateY(-8px);
  }
  100% {
    transform: translateY(0px);
  }
}
</style>
