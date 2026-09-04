<script setup lang="ts">
import { ref, computed, onMounted } from "vue";
import type { ImportAuditDto, ImportPreviewDto, ImportResultDto } from "~/services/modules/importService";
import { useAuthStore } from "~/stores/auth";
import { useThemeStore } from "~/stores/theme";
import AppSidebar from "~/components/layout/AppSidebar.vue";
import AppLoader from "~/components/base/AppLoader.vue";
import ImportUploadStep from "~/components/import/ImportUploadStep.vue";
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

    <AppSidebar :isOpen="isSidebarOpen" @close="isSidebarOpen = false" />

    <main class="flex-1 max-w-6xl w-full mx-auto px-4 sm:px-6 py-8">
      <!-- Hero compacto -->
      <div :class="['p-6 rounded-2xl border shadow-xl mb-8 relative overflow-hidden', themeStore.isDark ? 'bg-gradient-to-r from-zinc-900 via-zinc-900/90 to-zinc-950 border-zinc-800' : 'bg-gradient-to-r from-white via-white/90 to-zinc-50 border-zinc-200']">
        <div class="absolute -right-20 -bottom-20 w-80 h-80 bg-indigo-700/10 rounded-full blur-3xl pointer-events-none"></div>
        <div class="relative z-10 max-w-2xl space-y-2">
          <div :class="['inline-flex items-center gap-2 px-3 py-1 rounded-full border text-xs font-mono', themeStore.isDark ? 'bg-zinc-800/80 border-zinc-700/60 text-zinc-300' : 'bg-zinc-100 border-zinc-200 text-zinc-700']">
            <span class="w-2 h-2 rounded-full bg-indigo-400 animate-pulse"></span>
            Integrações & Importação de Dados
          </div>
          <h1 :class="['text-2xl sm:text-3xl font-bold tracking-tight', themeStore.isDark ? 'text-white' : 'text-zinc-900']">Importe e acompanhe seus dados</h1>
          <p :class="['text-sm leading-relaxed', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-600']">
            Envie planilhas, fotos ou PDFs escaneados. Tudo passa por análise e <strong :class="[themeStore.isDark ? 'text-zinc-300' : 'text-zinc-900']">confirmação antes de gravar</strong> — com auditoria completa (hash SHA256, autor, erros por linha).
          </p>
        </div>
      </div>

      <!-- Seletor Super Admin -->
      <div v-if="isSuperAdmin" class="mb-6 p-5 rounded-2xl border border-amber-500/30 bg-amber-500/5 flex flex-col sm:flex-row sm:items-center gap-3 sm:justify-between">
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
            : themeStore.isDark
              ? 'bg-zinc-900/60 border-zinc-800/80 hover:border-zinc-700'
              : 'bg-white border-zinc-200 hover:border-zinc-300'"
        >
          <div class="flex items-center gap-2 mb-1">
            <span
              class="w-6 h-6 rounded-full flex items-center justify-center text-xs font-bold flex-shrink-0"
              :class="step === s.key ? 'bg-indigo-500 text-white' : themeStore.isDark ? 'bg-zinc-800 text-zinc-400 border border-zinc-700/60' : 'bg-zinc-100 text-zinc-600 border border-zinc-200'"
            >{{ i + 1 }}</span>
            <span class="text-sm font-semibold truncate" :class="step === s.key ? (themeStore.isDark ? 'text-white' : 'text-indigo-900') : (themeStore.isDark ? 'text-zinc-300' : 'text-zinc-700')">{{ s.label.replace(/^\d\. /, "") }}</span>
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
            <div class="text-sm font-semibold text-emerald-600 dark:text-emerald-400 flex items-center gap-2">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
              Importação concluída!
            </div>
            <ul :class="['text-xs space-y-1', themeStore.isDark ? 'text-zinc-300' : 'text-zinc-700']">
              <li v-for="r in lastResults" :key="r.importId">
                <span :class="['font-medium', themeStore.isDark ? 'text-white' : 'text-zinc-900']">{{ r.dataSourceName }}</span> — {{ r.recordsSucceeded }} registro(s) importado(s),
                <span :class="r.recordsFailed > 0 ? 'text-amber-600 dark:text-amber-400' : 'text-emerald-600 dark:text-emerald-400'">{{ r.recordsFailed }} falha(s)</span>.
                <button :class="['underline ml-1', themeStore.isDark ? 'hover:text-white' : 'hover:text-zinc-900']" @click="goTo('historico')">Ver no histórico</button>
              </li>
            </ul>
          </div>

          <ImportHistoryStep :restaurant-id="targetRestaurantId" :refresh-token="historyRefreshToken" />
        </div>
      </section>
    </main>

    <footer :class="['mt-auto border-t py-6 text-center text-xs text-zinc-500', themeStore.isDark ? 'border-zinc-800/80 bg-zinc-950' : 'border-zinc-200 bg-white']">
      <div class="max-w-7xl mx-auto px-4 flex flex-col sm:flex-row items-center justify-between gap-3">
        <p>&copy; 2026 ISM — Intelligence Supply Manager. Todos os direitos reservados.</p>
        <div :class="['flex items-center gap-4', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-600']">
          <NuxtLink to="/" :class="['transition-colors', themeStore.isDark ? 'hover:text-white' : 'hover:text-zinc-900']">Início</NuxtLink>
          <NuxtLink to="/login" :class="['transition-colors', themeStore.isDark ? 'hover:text-white' : 'hover:text-zinc-900']">Login</NuxtLink>
          <a href="http://localhost:8080/swagger" target="_blank" :class="['transition-colors', themeStore.isDark ? 'hover:text-white' : 'hover:text-zinc-900']">Swagger API</a>
        </div>
      </div>
    </footer>
  </div>
</template>
