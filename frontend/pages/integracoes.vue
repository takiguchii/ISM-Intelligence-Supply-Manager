<script setup lang="ts">
import { ref, computed, onMounted } from "vue";
import type { ImportAuditDto, ImportPreviewDto, ImportResultDto } from "~/services/modules/importService";
import { useAuthStore } from "~/stores/auth";
import AppSidebar from "~/components/layout/AppSidebar.vue";
import AppLoader from "~/components/base/AppLoader.vue";
import ImportUploadStep from "~/components/import/ImportUploadStep.vue";
import ImportConfirmationStep from "~/components/import/ImportConfirmationStep.vue";
import ImportHistoryStep from "~/components/import/ImportHistoryStep.vue";

definePageMeta({ layout: false });

const runtimeConfig = useRuntimeConfig();
const authStore = useAuthStore();
const router = useRouter();

const isLoading = ref(true);
const isSidebarOpen = ref(false);
const toggleSidebar = () => (isSidebarOpen.value = !isSidebarOpen.value);

// ===== Wizard =====
type Step = "upload" | "confirmacao" | "historico";
const step = ref<Step>("upload");

const steps: { key: Step; label: string; hint: string }[] = [
  { key: "upload", label: "1. Enviar arquivo", hint: "Planilha, XML, foto ou PDF" },
  { key: "confirmacao", label: "2. Confirmar dados", hint: "Revise por categoria antes de gravar" },
  { key: "historico", label: "3. Histórico", hint: "Auditoria das importações" }
];

const currentPreview = ref<ImportPreviewDto | null>(null);
const currentFile = ref<File | null>(null);
const currentIsPhoto = ref(false);
const lastResults = ref<ImportResultDto[] | null>(null);

// Super Admin seleciona restaurante alvo
const isSuperAdmin = computed(() => authStore.currentUser?.role === "Admin" && !authStore.currentUser?.restaurantId);
const targetRestaurantId = ref<number | null>(isSuperAdmin.value ? 1 : authStore.currentUser?.restaurantId ?? null);

function goTo(target: Step) {
  // Não deixar ir para confirmação sem preview
  if (target === "confirmacao" && !currentPreview.value) return;
  step.value = target;
}

function onAnalyzed(preview: ImportPreviewDto, file: File, isPhoto: boolean) {
  currentPreview.value = preview;
  currentFile.value = file;
  currentIsPhoto.value = isPhoto;
  lastResults.value = null;
  step.value = "confirmacao";
}

function onConfirmed(results: ImportResultDto[]) {
  lastResults.value = results;
  currentPreview.value = null;
  currentFile.value = null;
  historyRefreshToken.value++;
  step.value = "historico";
}

const historyRefreshToken = ref(0);

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

    <main class="flex-1 max-w-6xl w-full mx-auto px-4 sm:px-6 py-8">
      <!-- Hero compacto -->
      <div class="p-6 rounded-2xl bg-gradient-to-r from-zinc-900 via-zinc-900/90 to-zinc-950 border border-zinc-800 shadow-xl mb-8 relative overflow-hidden">
        <div class="absolute -right-20 -bottom-20 w-80 h-80 bg-indigo-700/10 rounded-full blur-3xl pointer-events-none"></div>
        <div class="relative z-10 max-w-2xl space-y-2">
          <div class="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-zinc-800/80 border border-zinc-700/60 text-xs font-mono text-zinc-300">
            <span class="w-2 h-2 rounded-full bg-indigo-400 animate-pulse"></span>
            Integrações & Importação de Dados
          </div>
          <h1 class="text-2xl sm:text-3xl font-bold text-white tracking-tight">Importe e acompanhe seus dados</h1>
          <p class="text-zinc-400 text-sm leading-relaxed">
            Envie planilhas, fotos ou PDFs escaneados. Tudo passa por análise e <strong class="text-zinc-300">confirmação antes de gravar</strong> — com auditoria completa (hash SHA256, autor, erros por linha).
          </p>
        </div>
      </div>

      <!-- Seletor Super Admin -->
      <div v-if="isSuperAdmin" class="mb-6 p-5 rounded-2xl border border-amber-500/30 bg-amber-500/5 flex flex-col sm:flex-row sm:items-center gap-3 sm:justify-between">
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

      <!-- Wizard: indicador de etapas -->
      <nav class="mb-6 grid grid-cols-3 gap-2 sm:gap-3" aria-label="Etapas da importação">
        <button
          v-for="(s, i) in steps"
          :key="s.key"
          @click="goTo(s.key)"
          :disabled="s.key === 'confirmacao' && !currentPreview"
          class="p-3 sm:p-4 rounded-2xl border text-left transition-all duration-200 disabled:opacity-40 disabled:cursor-not-allowed"
          :class="step === s.key
            ? 'bg-indigo-500/10 border-indigo-500/40 ring-1 ring-indigo-500/30'
            : 'bg-zinc-900/60 border-zinc-800/80 hover:border-zinc-700'"
        >
          <div class="flex items-center gap-2 mb-1">
            <span
              class="w-6 h-6 rounded-full flex items-center justify-center text-xs font-bold flex-shrink-0"
              :class="step === s.key ? 'bg-indigo-500 text-white' : 'bg-zinc-800 text-zinc-400 border border-zinc-700/60'"
            >{{ i + 1 }}</span>
            <span class="text-sm font-semibold truncate" :class="step === s.key ? 'text-white' : 'text-zinc-300'">{{ s.label.replace(/^\d\. /, "") }}</span>
          </div>
          <p class="text-[11px] text-zinc-500 leading-snug hidden sm:block">{{ s.hint }}</p>
        </button>
      </nav>

      <!-- Conteúdo da etapa -->
      <section>
        <ImportUploadStep
          v-if="step === 'upload'"
          :restaurant-id="targetRestaurantId"
          @analyzed="onAnalyzed"
        />

        <template v-else-if="step === 'confirmacao'">
          <ImportConfirmationStep
            v-if="currentPreview && currentFile"
            :preview="currentPreview"
            :file="currentFile"
            :is-photo="currentIsPhoto"
            :restaurant-id="targetRestaurantId"
            @back="goTo('upload')"
            @confirmed="onConfirmed"
          />
        </template>

        <!-- Resultado pós-confirmação + histórico -->
        <div v-else-if="step === 'historico'" class="space-y-6">
          <div v-if="lastResults && lastResults.length > 0" class="p-5 rounded-2xl border bg-emerald-500/5 border-emerald-500/30 space-y-2">
            <div class="text-sm font-semibold text-emerald-300 flex items-center gap-2">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
              Importação concluída!
            </div>
            <ul class="text-xs text-zinc-300 space-y-1">
              <li v-for="r in lastResults" :key="r.importId">
                <span class="font-medium text-white">{{ r.dataSourceName }}</span> — {{ r.recordsSucceeded }} registro(s) importado(s),
                <span :class="r.recordsFailed > 0 ? 'text-amber-300' : 'text-emerald-300'">{{ r.recordsFailed }} falha(s)</span>.
                <button class="underline hover:text-white ml-1" @click="goTo('historico')">Ver no histórico</button>
              </li>
            </ul>
          </div>

          <ImportHistoryStep :restaurant-id="targetRestaurantId" :refresh-token="historyRefreshToken" />
        </div>
      </section>
    </main>

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
