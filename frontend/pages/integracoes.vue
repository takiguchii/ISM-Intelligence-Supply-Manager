<script setup lang="ts">
import { ref, computed, onMounted } from "vue";
import type { ImportAuditDto, ImportPreviewDto, ImportResultDto } from "~/services/modules/import/importService";
import { importService } from "~/services/modules/import/importService";
import { apiClientWithAuth } from "~/services/api/client";
import { useAuthStore } from "~/stores/auth";
import ImportConfirmationStep from "~/components/import/ImportConfirmationStep.vue";
import TelegramSettings from "~/components/settings/TelegramSettings.vue";
import ImportUploadPanel from "~/components/integrations/ImportUploadPanel.vue";
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
const analyzing = ref(false);
const uploadError = ref<string | null>(null);
const lastFileName = ref<string | null>(null);

const isPhotoFile = (file: File) =>
  file.type.startsWith("image/") || file.type === "application/pdf" ||
  /\.(jpe?g|png|webp|gif|pdf)$/i.test(file.name);

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
  { id: "telegram", name: "Telegram", description: "Assistente, agentes e alertas do seu restaurante.", icon: "telegram" },
  { id: "ifood", name: "iFood", description: "Integração de pedidos e cardápio.", icon: "ifood" },
  { id: "99food", name: "99Food", description: "Integração de vendas e entregas.", icon: "99food" },
  { id: "planilhas", name: "Planilhas", description: "Importação manual de arquivos.", icon: "sheet" },
  { id: "cardapio_digital", name: "Cardápio Digital", description: "Integração de itens e preços.", icon: "menu" },
  { id: "stone", name: "Stone", description: "Integração de recebíveis e taxas.", icon: "stone" },
  { id: "fornecedores", name: "Fornecedores", description: "Integração de catálogos e pedidos.", icon: "supplier" }
];

const integrations = computed(() => [skills.find(s => s.id === 'telegram')!, skills.find(s => s.id === 'planilhas')!, ...skills.filter(s => !['telegram', 'planilhas'].includes(s.id))]);
const expandedIntegration = ref<string | null>(null);
const telegramConnected = ref(false);
function integrationStatus(id: string) { return id === 'telegram' ? (telegramConnected.value ? 'Conectado' : 'Configurar') : id === 'planilhas' ? 'Disponível' : 'Indisponível'; }
function onIntegrationToggle(event: Event, id: string) {
  const open = (event.currentTarget as HTMLDetailsElement).open;
  if (open) expandedIntegration.value = id;
  else if (expandedIntegration.value === id) expandedIntegration.value = null;
}
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
  if (route.hash === "#telegram") { activeTab.value = "conexoes"; expandedIntegration.value = "telegram"; }
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
    <section v-if="activeTab === 'conexoes'" id="integration-panel-conexoes" role="tabpanel" aria-labelledby="integration-tab-conexoes">
      <div class="overflow-hidden rounded-2xl border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-900/30">
        <details v-for="integration in integrations" :key="integration.id" name="ism-integrations" :open="expandedIntegration === integration.id" class="integration-disclosure group border-b border-zinc-200 last:border-b-0 dark:border-zinc-800" @toggle="onIntegrationToggle($event, integration.id)">
          <summary class="flex min-h-24 cursor-pointer list-none items-center gap-4 px-4 py-5 transition hover:bg-zinc-50 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-[-3px] focus-visible:outline-indigo-500 dark:hover:bg-zinc-800/40 sm:px-6">
            <span class="flex h-11 w-11 shrink-0 items-center justify-center overflow-hidden rounded-xl bg-zinc-100 ring-1 ring-zinc-200 dark:bg-zinc-800/60 dark:ring-zinc-700/50"><IntegrationIcon :icon="integration.icon" /></span>
            <span class="min-w-0 flex-1"><span class="block text-sm font-semibold sm:text-base">{{ integration.name }}</span><span class="mt-1 block text-xs leading-relaxed text-zinc-600 dark:text-zinc-400 sm:text-sm">{{ integration.description }}</span><span class="mt-1 block text-xs text-zinc-600 dark:text-zinc-400 sm:hidden">{{ integrationStatus(integration.id) }}</span></span>
            <span class="hidden shrink-0 rounded-full px-2.5 py-1 text-xs sm:inline-block" :class="integration.id === 'telegram' && telegramConnected ? 'bg-emerald-50 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-300' : 'text-zinc-500 dark:text-zinc-400'">{{ integrationStatus(integration.id) }}</span>
            <svg aria-hidden="true" class="h-4 w-4 shrink-0 text-zinc-500 transition-transform group-open:rotate-180" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path d="m6 9 6 6 6-6" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" /></svg>
          </summary>
          <div class="border-t border-zinc-100 bg-zinc-50/50 px-4 py-5 dark:border-zinc-800 dark:bg-zinc-950/30 sm:px-6">
            <template v-if="integration.id === 'telegram'">
              <TelegramSettings v-if="!isSuperAdmin" embedded @connection="telegramConnected = $event" />
              <p v-else class="text-sm text-zinc-600 dark:text-zinc-400">Entre com uma conta gerencial do restaurante para conectar seu Telegram.</p>
            </template>
            <ImportUploadPanel v-else-if="integration.id === 'planilhas'" :analyzing="analyzing" :disabled="!targetRestaurantId" :file-name="lastFileName" :error="uploadError" @file="handleFile" />
            <div v-else class="space-y-2 text-sm"><p class="font-medium">{{ integration.name }} ainda não está disponível.</p><p class="text-zinc-600 dark:text-zinc-400">Não há um conector ativo para este serviço. Quando estiver disponível, as opções de conexão aparecerão aqui.</p></div>
          </div>
        </details>
      </div>
    </section>
    <section v-else-if="activeTab === 'importar'" id="integration-panel-importar" role="tabpanel" aria-labelledby="integration-tab-importar" class="space-y-5">
      <div><h2 class="text-xl font-semibold">Importar dados</h2><p class="mt-1 text-sm text-zinc-600 dark:text-zinc-400">Escolha um arquivo. Você poderá revisar os dados antes de confirmar a importação.</p></div>
      <ImportUploadPanel :analyzing="analyzing" :disabled="!targetRestaurantId" :file-name="lastFileName" :error="uploadError" @file="handleFile" />
    </section>
    <section v-else-if="activeTab === 'confirmacao'" id="integration-panel-confirmacao" role="tabpanel" aria-labelledby="integration-tab-confirmacao"><ImportConfirmationStep v-if="currentPreview && currentFile" :preview="currentPreview" :file="currentFile" :is-photo="currentIsPhoto" :restaurant-id="targetRestaurantId" @back="activeTab = 'importar'" @confirmed="onConfirmed" /></section>
    <section v-else-if="activeTab === 'historico'" id="integration-panel-historico" role="tabpanel" aria-labelledby="integration-tab-historico" class="space-y-5">
      <div><h2 class="text-xl font-semibold">Histórico de importações</h2><p class="mt-1 text-sm text-zinc-600 dark:text-zinc-400">Confira arquivos processados e o resultado de cada importação.</p></div>
      <el-alert v-if="lastResults?.length" title="Importação concluída. Confira os resultados abaixo." type="success" show-icon :closable="false" />
      <ImportHistoryStep :restaurant-id="targetRestaurantId" :refresh-token="historyRefreshToken" />
    </section>
  </main>
</template>

<style scoped>
.integration-disclosure > summary::-webkit-details-marker { display: none; }
</style>
