<script setup lang="ts">
import { ref, computed, onMounted } from "vue";
import type { ImportAuditDto, ImportPreviewDto, ImportResultDto } from "~/services/modules/import/importService";
import { importService } from "~/services/modules/import/importService";
import { apiClientWithAuth } from "~/services/api/client";
import { useAuthStore } from "~/stores/auth";
import ImportConfirmationStep from "~/components/import/ImportConfirmationStep.vue";
import TelegramSettings from "~/components/settings/TelegramSettings.vue";
import IntegrationIcon from "~/components/integrations/IntegrationIcon.vue";
import ImportHistoryStep from "~/components/import/ImportHistoryStep.vue";


const authStore = useAuthStore();
const router = useRouter();
const route = useRoute();



// ===== View / Navigation State =====
type MainTab = "conexoes" | "importar" | "confirmacao" | "historico";
const activeTab = ref<MainTab>("conexoes");

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
  activeTab.value = "conexoes";
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

function openImport() { activeTab.value = "importar"; }
const tabs = computed(() => [
  { id: "conexoes" as MainTab, label: "Conexões" },
  { id: "importar" as MainTab, label: "Importar dados" },
  ...(currentPreview.value ? [{ id: "confirmacao" as MainTab, label: "Revisar importação" }] : []),
  { id: "historico" as MainTab, label: "Histórico" }
]);
function moveTab(event: KeyboardEvent, index: number) {
  if (!["ArrowLeft", "ArrowRight", "Home", "End"].includes(event.key)) return;
  event.preventDefault();
  const next = event.key === "Home" ? 0 : event.key === "End" ? tabs.value.length - 1 : (index + (event.key === "ArrowRight" ? 1 : -1) + tabs.value.length) % tabs.value.length;
  activeTab.value = tabs.value[next].id;
  nextTick(() => document.getElementById(`integration-tab-${activeTab.value}`)?.focus());
}

onMounted(async () => {
  authStore.initFromStorage();
  if (route.hash === "#telegram") activeTab.value = "conexoes";
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
  <main class="mx-auto w-full max-w-6xl space-y-7 px-4 py-7 text-zinc-900 dark:text-zinc-100 sm:px-6 sm:py-9">
    <header class="space-y-2">
      <p class="text-xs font-semibold uppercase tracking-widest text-indigo-600 dark:text-indigo-400">Seu restaurante conectado</p>
      <h1 class="text-3xl font-bold tracking-tight">Integrações</h1>
      <p class="max-w-2xl text-sm leading-relaxed text-zinc-600 dark:text-zinc-400">Conecte seus canais, importe dados e acompanhe os envios em um só lugar.</p>
    </header>
    <section v-if="isSuperAdmin" class="rounded-xl border border-amber-300 bg-amber-50 p-4 dark:border-amber-800 dark:bg-amber-950/20" aria-label="Restaurante da importação">
      <label for="integration-restaurant" class="block text-sm font-semibold">Restaurante para importar dados</label>
      <select id="integration-restaurant" v-model="targetRestaurantId" class="mt-2 w-full rounded-lg border border-zinc-300 bg-white p-3 dark:border-zinc-700 dark:bg-zinc-900">
        <option :value="null">Selecione um restaurante</option>
        <option v-for="restaurant in restaurants" :key="restaurant.id" :value="restaurant.id">{{ restaurant.name }}</option>
      </select>
      <p v-if="restaurantsError" role="alert" class="mt-2 text-sm text-red-600 dark:text-red-400">{{ restaurantsError }}</p>
    </section>
    <nav role="tablist" aria-label="Áreas de integrações" class="flex flex-wrap gap-1 border-b border-zinc-200 dark:border-zinc-800">
      <button v-for="(tab, index) in tabs" :id="`integration-tab-${tab.id}`" :key="tab.id" role="tab" :aria-selected="activeTab === tab.id" :aria-controls="`integration-panel-${tab.id}`" :tabindex="activeTab === tab.id ? 0 : -1" class="shrink-0 border-b-2 px-4 py-3 text-sm font-semibold transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-[-3px] focus-visible:outline-indigo-500" :class="activeTab === tab.id ? 'border-indigo-500 text-indigo-700 dark:text-indigo-300' : 'border-transparent text-zinc-500 hover:text-zinc-900 dark:hover:text-zinc-100'" @click="activeTab = tab.id" @keydown="moveTab($event, index)">{{ tab.label }}</button>
    </nav>
    <section v-if="activeTab === 'conexoes'" id="integration-panel-conexoes" role="tabpanel" aria-labelledby="integration-tab-conexoes" class="space-y-6">
      <TelegramSettings v-if="!isSuperAdmin" />
      <div v-else class="rounded-xl border border-zinc-200 p-5 text-sm dark:border-zinc-800">A conexão do Telegram é pessoal. Entre com uma conta gerencial do restaurante para configurar.</div>
      <article class="flex flex-col gap-4 rounded-2xl border border-zinc-200 bg-white p-5 dark:border-zinc-800 dark:bg-zinc-900/40 sm:flex-row sm:items-center sm:justify-between">
        <div class="flex items-center gap-4"><span class="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl border border-zinc-200 dark:border-zinc-700"><IntegrationIcon icon="sheet" /></span><div><h2 class="font-semibold">Importação de arquivos</h2><p class="mt-1 text-sm text-zinc-500 dark:text-zinc-400">Traga dados de planilhas, documentos ou fotos. Revise antes de salvar.</p></div></div>
        <el-button @click="openImport">Importar dados</el-button>
      </article>
      <details class="rounded-xl border border-zinc-200 dark:border-zinc-800">
        <summary class="cursor-pointer px-5 py-4 text-sm font-semibold focus-visible:outline focus-visible:outline-indigo-500">Outras integrações <span class="ml-2 font-normal text-zinc-600 dark:text-zinc-400">Em breve</span></summary>
        <ul class="divide-y divide-zinc-100 px-5 pb-2 dark:divide-zinc-800"><li v-for="skill in skills.filter(s => s.id !== 'planilhas')" :key="skill.id" class="flex items-center gap-3 py-4"><span class="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-zinc-100 dark:bg-zinc-900"><IntegrationIcon :icon="skill.icon" /></span><div><h3 class="text-sm font-medium">{{ skill.name }}</h3><p class="text-xs text-zinc-600 dark:text-zinc-400">{{ skill.description }}</p></div><span class="ml-auto text-xs text-zinc-600 dark:text-zinc-400">Indisponível</span></li></ul>
      </details>
    </section>
    <section v-else-if="activeTab === 'importar'" id="integration-panel-importar" role="tabpanel" aria-labelledby="integration-tab-importar" class="space-y-5">
      <div><h2 class="text-xl font-semibold">Importar dados</h2><p class="mt-1 text-sm text-zinc-600 dark:text-zinc-400">Escolha um arquivo. Você poderá revisar os dados antes de confirmar a importação.</p></div>
      <div class="rounded-2xl border-2 border-dashed p-8 text-center sm:p-12" :class="dragging ? 'border-indigo-500 bg-indigo-500/5' : 'border-zinc-300 bg-white dark:border-zinc-700 dark:bg-zinc-900/40'" @dragover.prevent="dragging = true" @dragleave.prevent="dragging = false" @drop.prevent="!analyzing && onDrop($event)">
        <div v-if="analyzing" role="status" aria-live="polite"><span class="mx-auto mb-4 block h-8 w-8 animate-spin rounded-full border-2 border-indigo-500 border-t-transparent" aria-hidden="true" /><p class="font-semibold">Analisando arquivo…</p><p class="mt-2 break-all text-sm text-zinc-600 dark:text-zinc-400">{{ lastFileName }}</p></div>
        <template v-else><span class="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-xl bg-indigo-50 dark:bg-indigo-500/10"><IntegrationIcon icon="sheet" /></span><p class="font-semibold">Arraste um arquivo até aqui</p><p class="mb-5 mt-2 text-sm text-zinc-600 dark:text-zinc-400">Planilhas, documentos, PDFs e fotos.</p><el-button type="primary" :disabled="!targetRestaurantId" @click="triggerFileSelect">Selecionar arquivo</el-button></template>
        <input ref="fileInput" type="file" :accept="ACCEPT_FORMATS" class="sr-only" tabindex="-1" aria-label="Arquivo para importar" :disabled="analyzing" @change="onFileChange" />
      </div>
      <el-alert v-if="uploadError" role="alert" :title="uploadError" type="error" show-icon :closable="false" />
    </section>
    <section v-else-if="activeTab === 'confirmacao'" id="integration-panel-confirmacao" role="tabpanel" aria-labelledby="integration-tab-confirmacao"><ImportConfirmationStep v-if="currentPreview && currentFile" :preview="currentPreview" :file="currentFile" :is-photo="currentIsPhoto" :restaurant-id="targetRestaurantId" @back="activeTab = 'importar'" @confirmed="onConfirmed" /></section>
    <section v-else-if="activeTab === 'historico'" id="integration-panel-historico" role="tabpanel" aria-labelledby="integration-tab-historico" class="space-y-5">
      <div><h2 class="text-xl font-semibold">Histórico de importações</h2><p class="mt-1 text-sm text-zinc-600 dark:text-zinc-400">Confira arquivos processados e o resultado de cada importação.</p></div>
      <el-alert v-if="lastResults?.length" title="Importação concluída. Confira os resultados abaixo." type="success" show-icon :closable="false" />
      <ImportHistoryStep :restaurant-id="targetRestaurantId" :refresh-token="historyRefreshToken" />
    </section>
  </main>
</template>
