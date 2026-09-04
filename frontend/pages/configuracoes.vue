<script setup lang="ts">
import {
  aiConfigService,
  MODEL_PRESETS,
  GEMINI_DEFAULT_ENDPOINT,
  GEMINI_MODEL_HINTS,
  normalizeGeminiModelName,
  type RestaurantAiPhotoConfigDto,
  type TestRestaurantAiPhotoConfigResultDto,
  type TestRestaurantAiPhotoConfigRequestDto,
  type UpdateRestaurantAiPhotoConfigDto
} from "~/services/modules/importService";
import AppSidebar from "~/components/layout/AppSidebar.vue";
import AppLoader from "~/components/base/AppLoader.vue";

definePageMeta({ layout: false });

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();

const isLoading = ref(true);
const sidebarOpen = ref(true);
function toggleSidebar(value?: boolean) {
  sidebarOpen.value = typeof value === "boolean" ? value : !sidebarOpen.value;
}
const isSuperAdmin = computed(() => authStore.currentUser?.role === "Admin" && !authStore.currentUser?.restaurantId);
const isRestaurantUser = computed(() => !!authStore.currentUser?.restaurantId);

const targetRestaurantId = ref<number | null>(null);
const restaurants = ref<Array<{ id: number; name: string }>>([]);

const activeSectionId = ref<ConfigSectionId>("ai");
type ConfigSectionId = "ai" | "general" | "notifications" | "security" | "billing";

const sections: Array<{
  id: ConfigSectionId;
  label: string;
  subtitle: string;
  icon: "sparkles" | "sliders" | "bell" | "shield" | "credit";
  locked?: boolean;
}> = [
  {
    id: "ai",
    label: "Inteligência Artificial",
    subtitle: "Chave de API, modelo e visão computacional",
    icon: "sparkles"
  },
  {
    id: "general",
    label: "Geral",
    subtitle: "Nome do restaurante, fuso, moeda, idioma",
    icon: "sliders"
  },
  {
    id: "notifications",
    label: "Notificações",
    subtitle: "Email, push e alertas do sistema",
    icon: "bell"
  },
  {
    id: "security",
    label: "Segurança",
    subtitle: "Senha, 2FA e sessões ativas",
    icon: "shield"
  },
  {
    id: "billing",
    label: "Assinatura & Pagamento",
    subtitle: "Plano atual e forma de pagamento",
    icon: "credit"
  }
];

const activeAiRestaurantId = computed<number | null>(() => {
  if (isSuperAdmin.value) return targetRestaurantId.value;
  return authStore.currentUser?.restaurantId ?? null;
});

const aiConfig = ref<RestaurantAiPhotoConfigDto | null>(null);
const aiConfigLoading = ref(false);
const aiConfigSaving = ref(false);
const aiConfigTesting = ref(false);
const aiConfigError = ref<string | null>(null);
const aiConfigSaveSuccess = ref(false);
const aiConfigTestResult = ref<TestRestaurantAiPhotoConfigResultDto | null>(null);
const aiApiKey = ref("");
const aiShowApiKey = ref(false);
const aiEndpoint = ref("");
const aiModelPreset = ref<(typeof MODEL_PRESETS)[number]["value"]>("gemini-3.5-flash-lite");

const aiEffectiveModel = computed(() => normalizeGeminiModelName(aiModelPreset.value));

const aiCanSave = computed(() => {
  if (aiConfigSaving.value || aiConfigTesting.value) return false;
  if (!activeAiRestaurantId.value) return false;
  return true;
});

function applyAiPreset() {
  aiEndpoint.value = GEMINI_DEFAULT_ENDPOINT;
}

watch(aiModelPreset, () => applyAiPreset());

async function loadAiConfig() {
  const rid = activeAiRestaurantId.value;
  if (!rid) {
    aiConfig.value = null;
    return;
  }
  aiConfigLoading.value = true;
  aiConfigError.value = null;
  try {
    aiConfig.value = isSuperAdmin.value
      ? await aiConfigService.getByRestaurantId(rid)
      : await aiConfigService.getMyRestaurant();
    const model = normalizeGeminiModelName(aiConfig.value.model);
    const endpointOk =
      !!aiConfig.value.apiEndpoint &&
      aiConfig.value.apiEndpoint.toLowerCase().includes("generativelanguage.googleapis.com");
    aiEndpoint.value = endpointOk && aiConfig.value.apiEndpoint
      ? aiConfig.value.apiEndpoint
      : GEMINI_DEFAULT_ENDPOINT;
    aiModelPreset.value = "gemini-3.5-flash-lite";
    applyAiPreset();
  } catch (err: any) {
    aiConfigError.value = err?.data?.message ?? err?.message ?? "Não foi possível carregar a configuração.";
  } finally {
    aiConfigLoading.value = false;
  }
}

async function saveAiConfig() {
  if (!aiCanSave.value) return;
  aiConfigSaving.value = true;
  aiConfigError.value = null;
  aiConfigSaveSuccess.value = false;
  aiConfigTestResult.value = null;
  try {
    const rid = activeAiRestaurantId.value;
    const model = normalizeGeminiModelName(aiModelPreset.value);
    const payload: UpdateRestaurantAiPhotoConfigDto = {
      apiEndpoint: GEMINI_DEFAULT_ENDPOINT,
      model: model || null
    };
    if (aiApiKey.value.length > 0) {
      payload.apiKey = aiApiKey.value.trim();
    }
    if (isSuperAdmin.value) {
      await aiConfigService.updateByRestaurantId(rid!, payload);
    } else {
      await aiConfigService.updateMyRestaurant(payload);
    }
    aiApiKey.value = "";
    aiConfigSaveSuccess.value = true;
    setTimeout(() => (aiConfigSaveSuccess.value = false), 3500);
    await loadAiConfig();
  } catch (err: any) {
    const body = err?.data;
    const flat =
      typeof body === "string"
        ? body
        : body?.title ?? body?.message ?? err?.message ?? "Erro ao salvar configuração.";
    aiConfigError.value = flat;
  } finally {
    aiConfigSaving.value = false;
  }
}

async function clearAiKey() {
  const rid = activeAiRestaurantId.value;
  if (!rid) {
    aiConfigError.value = "Nenhum restaurante selecionado.";
    return;
  }
  aiConfigSaving.value = true;
  aiConfigError.value = null;
  aiConfigTestResult.value = null;
  try {
    if (isSuperAdmin.value) {
      await aiConfigService.updateByRestaurantId(rid, { clearKey: true });
    } else {
      await aiConfigService.updateMyRestaurant({ clearKey: true });
    }
    aiApiKey.value = "";
    await loadAiConfig();
  } catch (err: any) {
    aiConfigError.value = err?.data?.message ?? err?.message ?? "Erro ao remover chave.";
  } finally {
    aiConfigSaving.value = false;
  }
}

async function testAiConfig() {
  const rid = activeAiRestaurantId.value;
  if (!rid) {
    aiConfigError.value = "Nenhum restaurante selecionado.";
    return;
  }
  aiConfigTesting.value = true;
  aiConfigError.value = null;
  aiConfigTestResult.value = null;
  try {
    const formModel = normalizeGeminiModelName(aiModelPreset.value);
    const formEndpoint = aiEndpoint.value?.trim() ?? GEMINI_DEFAULT_ENDPOINT;
    const formKey = aiApiKey.value.length > 0 ? aiApiKey.value.trim() : null;

    const savedEndpoint = aiConfig.value?.apiEndpoint
      ? aiConfig.value.apiEndpoint.trim()
      : null;
    const savedModel = aiConfig.value?.model ? normalizeGeminiModelName(aiConfig.value.model) : null;

    const endpointDiffers = savedEndpoint == null || formEndpoint.toLowerCase() !== savedEndpoint.toLowerCase();
    const modelDiffers = savedModel == null || formModel !== savedModel;
    const keyDiffers = formKey != null;

    const overrideRequest: TestRestaurantAiPhotoConfigRequestDto | null =
      endpointDiffers || modelDiffers || keyDiffers
        ? {
            overrideApiKey: keyDiffers ? formKey : null,
            overrideEndpoint: endpointDiffers ? formEndpoint : null,
            overrideModel: modelDiffers ? formModel || null : null
          }
        : null;

    const result = isSuperAdmin.value
      ? await aiConfigService.testByRestaurantId(rid, overrideRequest)
      : await aiConfigService.testMyRestaurant(overrideRequest);
    aiConfigTestResult.value = result;
    if (!result.success) {
      aiConfigError.value = result.errorMessage ?? "Falha no teste (verifique chave/endpoint/modelo).";
    }
  } catch (err: any) {
    aiConfigError.value =
      "Falha no teste: " +
      (err?.data?.message ?? err?.data?.title ?? err?.message ?? "verifique chave/endpoint/modelo.");
  } finally {
    aiConfigTesting.value = false;
  }
}

function formatLatency(ms: number | null | undefined) {
  if (ms == null) return "";
  if (ms < 1000) return `${Math.round(ms)} ms`;
  return `${(ms / 1000).toFixed(2)} s`;
}

watch(targetRestaurantId, async (newId, oldId) => {
  if (!isSuperAdmin.value) return;
  if (newId && newId !== oldId && !isLoading.value) {
    aiApiKey.value = "";
    aiConfigSaveSuccess.value = false;
    aiConfigError.value = null;
    aiConfigTestResult.value = null;
    await loadAiConfig();
  }
});

watch(
  activeAiRestaurantId,
  async (newId, oldId) => {
    if (isLoading.value) return;
    if (newId !== oldId) {
      aiApiKey.value = "";
      aiConfigSaveSuccess.value = false;
      aiConfigError.value = null;
      aiConfigTestResult.value = null;
      await loadAiConfig();
    }
  },
  { flush: "post" }
);

onMounted(async () => {
  const start = Date.now();
  authStore.initFromStorage();
  if (!authStore.isAuthenticated) {
    router.push("/login");
    return;
  }
  if (route.hash) {
    const id = route.hash.slice(1) as ConfigSectionId;
    if (sections.some((s) => s.id === id)) activeSectionId.value = id;
  }
  if (isSuperAdmin.value) {
    targetRestaurantId.value = 1;
    const { apiClientWithAuth } = await import("~/services/api/client");
    try {
      const list = (await apiClientWithAuth<Array<{ id: number; name: string }>>(`/api/restaurants`, {
        method: "GET"
      })) as Array<{ id: number; name: string }>;
      restaurants.value = list;
      if (!targetRestaurantId.value && list.length > 0) targetRestaurantId.value = list[0].id;
    } catch {
      restaurants.value = [{ id: 1, name: "Gourmet ISM Restaurant" }];
    }
  } else {
    targetRestaurantId.value = authStore.currentUser?.restaurantId ?? null;
  }
  await loadAiConfig();
  const min = 900;
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
          @click="toggleSidebar()"
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
            Intelligence Supply
          </span>
        </div>
      </div>

      <nav class="hidden md:flex items-center gap-1">
        <nuxt-link to="/" class="px-3 py-2 text-sm text-zinc-400 hover:text-white hover:bg-zinc-800/60 rounded-lg transition-colors">Início</nuxt-link>
        <nuxt-link to="/integracoes" class="px-3 py-2 text-sm text-zinc-400 hover:text-white hover:bg-zinc-800/60 rounded-lg transition-colors">Integrações</nuxt-link>
        <span class="px-3 py-2 text-sm text-white font-semibold bg-zinc-800/80 rounded-lg">Configurações</span>
      </nav>

      <div class="flex items-center gap-3">
        <button
          @click="authStore.logout()"
          class="px-3 py-2 rounded-lg text-xs font-semibold tracking-wide uppercase text-zinc-300 hover:text-white hover:bg-zinc-800/60 border border-zinc-700/60 transition-all"
        >
          Sair
        </button>
      </div>
    </header>

    <AppSidebar :is-open="sidebarOpen" @close="toggleSidebar(false)" />

    <div
      :class="[
        'flex flex-1 min-h-0 transition-[padding] duration-300 ease-in-out',
        sidebarOpen ? 'lg:pl-64' : 'lg:pl-0'
      ]"
    >
      <!-- Sidebar de seções interna (navegação vertical estilo SaaS) -->
      <aside class="hidden md:block w-72 shrink-0 border-r border-zinc-800/80 bg-zinc-950/60">
        <div class="sticky top-16 p-6 space-y-6">
          <div class="space-y-1">
            <div class="flex items-center gap-2 text-[11px] uppercase tracking-[0.14em] font-semibold text-zinc-500">
              <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z"></path>
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"></path>
              </svg>
              Configurações
            </div>
            <div v-if="isRestaurantUser" class="flex items-center gap-2 mt-3 p-3 rounded-xl bg-zinc-900/70 border border-zinc-800/70">
              <div class="w-9 h-9 rounded-lg bg-amber-500/10 border border-amber-500/20 text-amber-300 flex items-center justify-center shrink-0">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"></path>
                </svg>
              </div>
              <div class="min-w-0">
                <p class="text-xs uppercase tracking-wider text-zinc-500 font-semibold">Restaurante</p>
                <p class="text-sm font-semibold text-white truncate">{{ authStore.currentUser?.restaurantName || "Meu restaurante" }}</p>
              </div>
            </div>
            <div v-else-if="isSuperAdmin" class="mt-3 space-y-2">
              <label class="text-[11px] uppercase tracking-widest text-zinc-500 font-semibold">Restaurante alvo</label>
              <select
                v-model="targetRestaurantId"
                class="w-full rounded-xl px-3.5 py-2.5 text-sm bg-zinc-900/80 border border-zinc-700/70 text-zinc-100 focus:outline-none focus:ring-2 focus:ring-amber-400/40 focus:border-amber-400/40"
              >
                <option v-for="r in restaurants" :key="r.id" :value="r.id">{{ r.id }} · {{ r.name }}</option>
              </select>
            </div>
          </div>

          <nav class="space-y-1">
            <button
              v-for="s in sections"
              :key="s.id"
              @click="!s.locked && (activeSectionId = s.id)"
              :disabled="s.locked"
              :class="[
                'w-full group rounded-xl text-left flex items-start gap-3 px-3 py-3 transition-all duration-200',
                s.locked
                  ? 'opacity-50 cursor-not-allowed hover:bg-transparent'
                  : 'cursor-pointer hover:bg-zinc-900/70',
                activeSectionId === s.id
                  ? 'bg-indigo-500/10 border border-indigo-500/30 shadow-inner'
                  : 'border border-transparent'
              ]"
            >
              <div
                :class="[
                  'w-9 h-9 rounded-lg flex items-center justify-center shrink-0',
                  activeSectionId === s.id
                    ? 'bg-indigo-500/20 border border-indigo-500/30 text-indigo-300'
                    : 'bg-zinc-900/80 border border-zinc-800/80 text-zinc-400 group-hover:text-zinc-200'
                ]"
              >
                <svg v-if="s.icon === 'sparkles'" class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 3v4M3 5h4M6 17v4m-2-2h4m5-16l2.286 6.857L21 12l-5.714 2.143L13 21l-2.286-6.857L5 12l5.714-2.143L13 3z"></path>
                </svg>
                <svg v-else-if="s.icon === 'sliders'" class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6V4m0 2a2 2 0 100 4m0-4a2 2 0 110 4m-6 8a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4m6 6v10m6-2a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4"></path>
                </svg>
                <svg v-else-if="s.icon === 'bell'" class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"></path>
                </svg>
                <svg v-else-if="s.icon === 'shield'" class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"></path>
                </svg>
                <svg v-else-if="s.icon === 'credit'" class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z"></path>
                </svg>
              </div>
              <div class="flex-1 min-w-0">
                <div class="flex items-center gap-2">
                  <p :class="['text-sm font-semibold', activeSectionId === s.id ? 'text-white' : 'text-zinc-200']">{{ s.label }}</p>
                  <span v-if="s.locked" class="text-[10px] font-semibold uppercase tracking-widest px-2 py-0.5 rounded-full bg-zinc-800/80 border border-zinc-700/60 text-zinc-400">Em breve</span>
                </div>
                <p class="text-xs text-zinc-500 mt-0.5 truncate">{{ s.subtitle }}</p>
              </div>
            </button>
          </nav>

          <div class="rounded-xl border border-indigo-500/20 bg-indigo-500/5 p-4 space-y-2">
            <p class="text-[11px] uppercase tracking-widest text-indigo-300 font-semibold flex items-center gap-1.5">
              <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"></path>
              </svg>
              Documentação
            </p>
            <p class="text-xs text-zinc-300 leading-relaxed">Pegue sua chave gratuita no <a href="https://aistudio.google.com/apikey" target="_blank" class="underline text-indigo-300 hover:text-indigo-200 font-medium">Google AI Studio</a>.</p>
          </div>
        </div>
      </aside>

      <!-- Conteúdo principal -->
      <main class="flex-1 min-w-0 px-4 sm:px-6 lg:px-10 py-8 space-y-8">
        <header class="space-y-1">
          <div class="flex flex-wrap items-center gap-2 text-xs uppercase tracking-widest text-zinc-500 font-semibold">
            <nuxt-link to="/" class="hover:text-zinc-300">Início</nuxt-link>
            <span class="text-zinc-700">/</span>
            <span class="text-zinc-300">Configurações</span>
            <span class="text-zinc-700">/</span>
            <span class="text-indigo-300">{{ sections.find((s) => s.id === activeSectionId)?.label }}</span>
          </div>
          <div class="flex flex-col sm:flex-row sm:items-end sm:justify-between gap-4">
            <div>
              <h1 class="text-2xl sm:text-3xl font-bold text-white tracking-tight">
                {{ sections.find((s) => s.id === activeSectionId)?.label }}
              </h1>
              <p class="text-sm text-zinc-400 mt-1 max-w-2xl">{{ sections.find((s) => s.id === activeSectionId)?.subtitle }}</p>
            </div>
            <div v-if="activeSectionId === 'ai'" class="flex flex-wrap gap-2 md:hidden">
              <button
                @click="toggleSidebar()"
                class="px-3 py-2 rounded-lg border border-zinc-800 bg-zinc-900/60 text-xs font-semibold text-zinc-200"
              >
                Menu de seções
              </button>
            </div>
          </div>
        </header>

        <!-- Seletor mobile (seções) -->
        <div class="md:hidden -mx-4 overflow-x-auto px-4 pb-2">
          <div class="flex gap-2 min-w-max">
            <button
              v-for="s in sections"
              :key="s.id"
              @click="!s.locked && (activeSectionId = s.id)"
              :disabled="s.locked"
              :class="[
                'px-3 py-2 rounded-xl text-sm font-semibold whitespace-nowrap border transition-all',
                s.locked ? 'opacity-50 cursor-not-allowed' : '',
                activeSectionId === s.id
                  ? 'bg-indigo-500/10 text-indigo-300 border-indigo-500/30'
                  : 'bg-zinc-900/60 text-zinc-300 border-zinc-800 hover:bg-zinc-800/60'
              ]"
            >
              {{ s.label }}
              <span v-if="s.locked" class="ml-2 text-[10px] text-zinc-500">(em breve)</span>
            </button>
          </div>
        </div>

        <!-- AI Section -->
        <section v-if="activeSectionId === 'ai'" class="space-y-6">
          <div v-if="isSuperAdmin && restaurants.length > 0" class="rounded-2xl border border-amber-500/20 bg-amber-500/5 p-4 sm:p-5 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
            <div class="flex items-start gap-3">
              <div class="w-10 h-10 rounded-xl bg-amber-500/15 border border-amber-500/30 text-amber-300 flex items-center justify-center flex-shrink-0">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path>
                </svg>
              </div>
              <div class="space-y-0.5">
                <p class="text-sm font-semibold text-amber-100">Modo Super Admin</p>
                <p class="text-xs sm:text-sm text-amber-200/80 leading-relaxed">
                  Você está editando a configuração de outro restaurante. Use o menu ao lado (ou abaixo em mobile) para alternar o alvo.
                </p>
              </div>
            </div>
            <label class="block sm:w-80 shrink-0 space-y-1.5">
              <span class="text-[11px] uppercase tracking-widest text-amber-200/70 font-semibold">Restaurante alvo</span>
              <select
                v-model="targetRestaurantId"
                class="w-full rounded-xl px-3.5 py-2.5 text-sm bg-zinc-900/80 border border-amber-400/30 text-amber-50 focus:outline-none focus:ring-2 focus:ring-amber-400/50"
              >
                <option v-for="r in restaurants" :key="r.id" :value="r.id">{{ r.id }} · {{ r.name }}</option>
              </select>
            </label>
          </div>

          <div class="grid grid-cols-1 xl:grid-cols-3 gap-6">
            <div class="xl:col-span-2 rounded-2xl border bg-zinc-900/50 border-zinc-800 shadow-xl overflow-hidden">
              <div class="p-5 sm:p-6 flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4 border-b border-zinc-800/80">
                <div class="flex items-start gap-3 flex-1">
                  <div class="w-10 h-10 rounded-xl bg-indigo-500/15 border border-indigo-500/30 text-indigo-300 flex items-center justify-center flex-shrink-0">
                    <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 3v4M3 5h4M6 17v4m-2-2h4m5-16l2.286 6.857L21 12l-5.714 2.143L13 21l-2.286-6.857L5 12l5.714-2.143L13 3z"></path>
                    </svg>
                  </div>
                  <div class="space-y-0.5 flex-1">
                    <div class="flex flex-wrap items-center gap-2">
                      <h2 class="text-base sm:text-lg font-semibold text-white tracking-tight">
                        {{ isSuperAdmin ? 'Configuração de IA — Restaurante alvo' : 'Configuração de IA do Restaurante' }}
                      </h2>
                      <span
                        v-if="aiConfig"
                        class="text-[10px] sm:text-xs font-semibold uppercase tracking-wider px-2 py-0.5 rounded-full border"
                        :class="
                          aiConfig.hasCustomKey
                            ? 'bg-emerald-500/10 text-emerald-300 border-emerald-500/30'
                            : 'bg-zinc-800/70 text-zinc-400 border-zinc-700/60'
                        "
                      >
                        {{
                          aiConfig.hasCustomKey
                            ? aiConfig.keyLast4Digits
                              ? `Chave salva · •••• ${aiConfig.keyLast4Digits}`
                              : 'Chave salva'
                            : 'Chave não definida'
                        }}
                      </span>
                    </div>
                    <p class="text-xs sm:text-sm text-zinc-400 leading-relaxed max-w-2xl">
                      Configure sua chave de API de inteligência artificial para importar dados de foto/PDF via OCR com visão computacional.
                      <span v-if="!isSuperAdmin">Você paga apenas pela sua cota — nenhuma chave é compartilhada com outros restaurantes.</span>
                      <span v-else>Cada restaurante tem sua própria chave (nenhum compartilhamento).</span>
                    </p>
                  </div>
                </div>
              </div>

              <div v-if="aiConfigLoading" class="p-6 flex items-center gap-3 text-sm text-zinc-400">
                <svg class="w-5 h-5 animate-spin text-indigo-400" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z"></path>
                </svg>
                Carregando configuração…
              </div>

              <div v-else-if="!activeAiRestaurantId" class="p-6 flex items-center gap-3 text-sm text-amber-300 rounded-xl bg-amber-500/5 border border-amber-500/20 m-5 sm:m-6">
                <svg class="w-5 h-5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"></path>
                </svg>
                <span>{{ isSuperAdmin ? 'Selecione um restaurante alvo para editar sua configuração de IA.' : 'Usuário não está vinculado a um restaurante.' }}</span>
              </div>

              <div v-else class="p-5 sm:p-6 space-y-4 sm:space-y-5">
                <div v-if="aiConfig && aiConfig.warnings && aiConfig.warnings.length > 0" class="rounded-xl border border-amber-500/20 bg-amber-500/5 p-4 space-y-1.5">
                  <p class="text-xs uppercase tracking-widest text-amber-300 font-semibold">Avisos na configuração atual</p>
                  <ul class="list-disc list-inside space-y-1 pl-1 text-xs sm:text-sm text-amber-200/90">
                    <li v-for="(w, idx) in aiConfig.warnings" :key="idx">{{ w }}</li>
                  </ul>
                </div>

                <div class="space-y-3">
                  <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2">
                    <label class="text-xs uppercase tracking-widest text-zinc-400 font-semibold">
                      Chave da API
                    </label>
                    <div v-if="aiConfig?.hasCustomKey" class="inline-flex items-center gap-1.5 text-[11px] font-semibold px-2.5 py-1 rounded-lg bg-emerald-500/10 border border-emerald-500/20 text-emerald-300">
                      <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7"></path>
                      </svg>
                      Chave salva · {{ aiConfig?.keyLast4Digits ? `•••• ${aiConfig.keyLast4Digits}` : 'definida' }}
                    </div>
                    <div v-else class="inline-flex items-center gap-1.5 text-[11px] font-semibold px-2.5 py-1 rounded-lg bg-zinc-800/70 border border-zinc-700/60 text-zinc-400">
                      <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"></path>
                      </svg>
                      Nenhuma chave definida
                    </div>
                  </div>
                  <div class="relative">
                    <input
                      v-model="aiApiKey"
                      :type="aiShowApiKey ? 'text' : 'password'"
                      name="new-ai-api-key-not-password"
                      :placeholder="aiConfig?.hasCustomKey ? 'Deixe vazio para manter a chave atual, ou cole uma nova chave aqui…' : 'Cole sua chave de API do Google AI Studio (ex.: AIza…)'"
                      class="w-full rounded-xl pl-4 pr-12 py-3.5 text-sm bg-zinc-900/80 border border-zinc-700/70 text-zinc-100 placeholder:text-zinc-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/40 focus:border-indigo-500/50 font-mono tracking-tight"
                      autocomplete="new-password"
                      data-1p-ignore
                      data-lpignore="true"
                      data-form-type="other"
                      spellcheck="false"
                      maxlength="256"
                    />
                    <button
                      type="button"
                      @click="aiShowApiKey = !aiShowApiKey"
                      class="absolute right-2.5 top-1/2 -translate-y-1/2 p-2 rounded-lg text-zinc-400 hover:text-white hover:bg-zinc-800/80 transition-colors"
                      :title="aiShowApiKey ? 'Ocultar chave' : 'Mostrar chave'"
                    >
                      <svg v-if="!aiShowApiKey" class="w-4.5 h-4.5 w-[18px] h-[18px]" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"></path>
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"></path>
                      </svg>
                      <svg v-else class="w-[18px] h-[18px]" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21"></path>
                      </svg>
                    </button>
                  </div>
                  <p class="text-xs text-zinc-500 leading-relaxed">
                    Recomendado: obtenha gratuitamente em
                    <a href="https://aistudio.google.com/apikey" target="_blank" class="underline text-indigo-300 hover:text-indigo-200 font-medium">Google AI Studio (Gemini gratuito)</a>.
                    No momento, apenas o provedor Google Gemini é suportado.
                  </p>
                </div>

                <div class="grid grid-cols-1 lg:grid-cols-5 gap-4 lg:gap-5">
                  <div class="lg:col-span-3 space-y-2">
                    <label class="text-xs uppercase tracking-widest text-zinc-400 font-semibold">Modelo Gemini</label>
                    <select
                      v-model="aiModelPreset"
                      class="w-full rounded-xl px-3.5 py-3.5 text-sm bg-zinc-900/80 border border-zinc-700/70 text-zinc-100 focus:outline-none focus:ring-2 focus:ring-indigo-500/40 focus:border-indigo-500/50"
                    >
                      <option v-for="p in MODEL_PRESETS" :key="p.value" :value="p.value">
                        {{ p.label }}
                        <template v-if="p.tier && p.tier !== 'Custom'"> · {{ p.tier }}</template>
                      </option>
                    </select>
                    <p class="text-xs text-zinc-500 leading-relaxed">
                      Dicas:
                      <span v-for="(h, idx) in GEMINI_MODEL_HINTS" :key="idx">
                        <code class="px-1 py-0.5 rounded bg-zinc-800/70 border border-zinc-700/60 text-zinc-300 text-[11px] font-mono mr-1">{{ h.split(" ")[0] }}</code>
                      </span>
                    </p>
                  </div>
                  <div class="lg:col-span-2 space-y-2">
                    <label class="text-xs uppercase tracking-widest text-zinc-400 font-semibold">Endpoint base (Google)</label>
                    <input
                      v-model="aiEndpoint"
                      placeholder="https://generativelanguage.googleapis.com/v1"
                      class="w-full rounded-xl px-3.5 py-3.5 text-sm bg-zinc-800/40 border border-zinc-700/40 text-zinc-400 placeholder:text-zinc-600 focus:outline-none focus:ring-0 focus:border-zinc-700/40 font-mono opacity-90 cursor-not-allowed select-none"
                      maxlength="512"
                      readonly
                      disabled
                      title="Apenas o provedor Google Gemini é suportado nesta versão. Endereço bloqueado para edição."
                    />
                    <p class="text-xs text-zinc-500 leading-relaxed">🔒 Trava de segurança: endpoint fixo no Google Gemini.</p>
                  </div>
                </div>

                <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 pt-2">
                  <div class="flex flex-wrap items-center gap-2 sm:gap-3">
                    <button
                      type="button"
                      :disabled="!aiCanSave"
                      @click="saveAiConfig()"
                      class="inline-flex items-center gap-2 rounded-xl px-4 py-2.5 text-sm font-semibold text-white bg-indigo-500 hover:bg-indigo-400 disabled:bg-zinc-700/60 disabled:text-zinc-400 disabled:cursor-not-allowed border border-indigo-500/30 disabled:border-zinc-700/60 shadow-lg shadow-indigo-500/20 disabled:shadow-none transition-all"
                    >
                      <svg v-if="aiConfigSaving" class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
                        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z"></path>
                      </svg>
                      <svg v-else class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path>
                      </svg>
                      {{ aiConfigSaving ? 'Salvando…' : 'Salvar configuração' }}
                    </button>
                    <button
                      type="button"
                      :disabled="aiConfigTesting || !activeAiRestaurantId"
                      @click="testAiConfig()"
                      class="inline-flex items-center gap-2 rounded-xl px-4 py-2.5 text-sm font-semibold text-emerald-200 bg-emerald-500/10 hover:bg-emerald-500/15 disabled:bg-zinc-800/60 disabled:text-zinc-500 disabled:cursor-not-allowed border border-emerald-500/30 disabled:border-zinc-700/60 transition-all"
                    >
                      <svg v-if="aiConfigTesting" class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
                        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z"></path>
                      </svg>
                      <svg v-else class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"></path>
                      </svg>
                      {{ aiConfigTesting ? 'Testando…' : 'Testar conexão' }}
                    </button>
                    <button
                      v-if="aiConfig?.hasCustomKey"
                      type="button"
                      :disabled="aiConfigSaving"
                      @click="clearAiKey()"
                      class="inline-flex items-center gap-2 rounded-xl px-4 py-2.5 text-sm font-semibold text-rose-200 bg-rose-500/10 hover:bg-rose-500/15 disabled:bg-zinc-800/60 disabled:text-zinc-500 disabled:cursor-not-allowed border border-rose-500/30 disabled:border-zinc-700/60 transition-all"
                    >
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path>
                      </svg>
                      Remover chave
                    </button>
                  </div>
                  <div class="flex-1 min-w-0"></div>
                </div>

                <div v-if="aiConfigSaveSuccess" role="status" class="rounded-xl border border-emerald-500/30 bg-emerald-500/10 px-4 py-3 text-sm text-emerald-200 flex items-start gap-2.5">
                  <svg class="w-5 h-5 shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"></path>
                  </svg>
                  <span>Configuração salva com sucesso!</span>
                </div>
                <div v-if="aiConfigError" role="alert" class="rounded-xl border border-rose-500/30 bg-rose-500/10 px-4 py-3 text-sm text-rose-200 flex items-start gap-2.5">
                  <svg class="w-5 h-5 shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path>
                  </svg>
                  <div class="space-y-1.5 flex-1">
                    <p class="font-semibold">Algo deu errado</p>
                    <p class="text-rose-100/90 leading-relaxed whitespace-pre-wrap break-words">{{ aiConfigError }}</p>
                  </div>
                </div>

                <div
                  v-if="aiConfigTestResult"
                  :class="[
                    'rounded-xl border px-4 py-3.5 text-sm flex items-start gap-3',
                    aiConfigTestResult.success
                      ? 'border-emerald-500/30 bg-emerald-500/10 text-emerald-200'
                      : 'border-amber-500/30 bg-amber-500/10 text-amber-100'
                  ]"
                >
                  <svg v-if="aiConfigTestResult.success" class="w-5 h-5 shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"></path>
                  </svg>
                  <svg v-else class="w-5 h-5 shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path>
                  </svg>
                  <div class="space-y-1.5 flex-1 min-w-0">
                    <p class="font-semibold">
                      {{ aiConfigTestResult.success ? 'Conexão OK com o Google Gemini' : 'Falha na validação com o provedor' }}
                    </p>
                    <ul class="grid grid-cols-1 sm:grid-cols-3 gap-x-6 gap-y-1 text-xs text-emerald-50/90 *:break-all">
                      <li><span class="opacity-75">Modelo:</span> <code class="font-mono">{{ aiConfigTestResult.normalizedModel || '—' }}</code></li>
                      <li><span class="opacity-75">HTTP Provedor:</span> {{ aiConfigTestResult.httpStatusFromProvider ?? '—' }}</li>
                      <li><span class="opacity-75">Latência:</span> {{ formatLatency(aiConfigTestResult.latencyMs) || '—' }}</li>
                    </ul>
                    <p v-if="!aiConfigTestResult.success && aiConfigTestResult.errorMessage" class="text-amber-50 leading-relaxed break-words">
                      {{ aiConfigTestResult.errorMessage }}
                    </p>
                    <ul v-if="aiConfigTestResult.warnings && aiConfigTestResult.warnings.length > 0" class="list-disc list-inside space-y-0.5">
                      <li v-for="(w, idx) in aiConfigTestResult.warnings" :key="idx" class="text-xs">{{ w }}</li>
                    </ul>
                  </div>
                </div>
              </div>
            </div>

            <aside class="space-y-4 xl:space-y-5">
              <div class="rounded-2xl border bg-zinc-900/50 border-zinc-800 p-5 space-y-3 shadow-xl">
                <div class="flex items-center gap-2 text-xs uppercase tracking-[0.18em] text-zinc-500 font-semibold">
                  <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path>
                  </svg>
                  Dicas rápidas
                </div>
                <ol class="space-y-3 text-sm text-zinc-300">
                  <li class="flex gap-3">
                    <span class="w-5 h-5 shrink-0 rounded-full bg-indigo-500/20 text-indigo-300 border border-indigo-500/30 text-[11px] font-bold flex items-center justify-center">1</span>
                    <span>Crie sua chave gratuita no <a href="https://aistudio.google.com/apikey" target="_blank" class="underline text-indigo-300 hover:text-indigo-200 font-medium">Google AI Studio</a>.</span>
                  </li>
                  <li class="flex gap-3">
                    <span class="w-5 h-5 shrink-0 rounded-full bg-indigo-500/20 text-indigo-300 border border-indigo-500/30 text-[11px] font-bold flex items-center justify-center">2</span>
                    <span>Cole aqui. Recomendamos o modelo <code class="px-1.5 py-0.5 rounded bg-zinc-800/70 border border-zinc-700/60 font-mono text-xs">gemini-3.5-flash-lite</code> (melhor free tier).</span>
                  </li>
                  <li class="flex gap-3">
                    <span class="w-5 h-5 shrink-0 rounded-full bg-indigo-500/20 text-indigo-300 border border-indigo-500/30 text-[11px] font-bold flex items-center justify-center">3</span>
                    <span>Clique em <strong>Salvar</strong> e depois em <strong>Testar conexão</strong> — não precisa enviar nenhuma foto.</span>
                  </li>
                </ol>
              </div>

              <div class="rounded-2xl border bg-zinc-900/50 border-zinc-800 p-5 space-y-3 shadow-xl">
                <div class="flex items-center gap-2 text-xs uppercase tracking-[0.18em] text-zinc-500 font-semibold">
                  <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"></path>
                  </svg>
                  Segurança & privacidade
                </div>
                <ul class="space-y-2 text-xs text-zinc-400">
                  <li class="flex gap-2">
                    <span class="text-emerald-300 mt-0.5">✓</span>
                    <span>Chave gravada criptografada em banco, nunca exposta em APIs GET (apenas 4 últimos dígitos).</span>
                  </li>
                  <li class="flex gap-2">
                    <span class="text-emerald-300 mt-0.5">✓</span>
                    <span>Nenhuma chave compartilhada entre restaurantes.</span>
                  </li>
                  <li class="flex gap-2">
                    <span class="text-emerald-300 mt-0.5">✓</span>
                    <span>Cota e faturamento diretamente na sua conta Google (você controla limites).</span>
                  </li>
                </ul>
              </div>

              <nuxt-link to="/integracoes" class="block rounded-2xl border border-indigo-500/30 bg-gradient-to-br from-indigo-500/20 to-zinc-900/40 p-5 space-y-2 hover:from-indigo-500/30 transition-all">
                <div class="flex items-center gap-2 text-sm font-semibold text-indigo-200">
                  <svg class="w-4.5 h-4.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2"></path>
                  </svg>
                  Ir para Importação de dados
                </div>
                <p class="text-xs text-indigo-100/80">Tudo certo aqui? Volte para a tela de Integrações e importe uma foto do seu caderno de compras.</p>
              </nuxt-link>
            </aside>
          </div>
        </section>

        <!-- Seção Geral -->
        <section v-else-if="activeSectionId === 'general'" class="space-y-6">
          <div class="grid grid-cols-1 xl:grid-cols-3 gap-6">
            <div class="xl:col-span-2 rounded-2xl border bg-zinc-900/50 border-zinc-800 shadow-xl overflow-hidden">
              <div class="p-5 sm:p-6 flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4 border-b border-zinc-800/80">
                <div class="flex items-start gap-3 flex-1">
                  <div class="w-10 h-10 rounded-xl bg-indigo-500/15 border-indigo-500/30 text-indigo-300 flex items-center justify-center flex-shrink-0">
                    <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6V4m0 2a2 2 0 100 4m0-4a2 2 0 110 4m-6 8a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4m6 6v10m6-2a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4"></path>
                    </svg>
                  </div>
                  <div class="space-y-0.5 flex-1">
                    <h2 class="text-base sm:text-lg font-semibold text-white tracking-tight">Informações do restaurante</h2>
                    <p class="text-xs sm:text-sm text-zinc-400 leading-relaxed max-w-2xl">Dados básicos do estabelecimento e preferências regionais.</p>
                  </div>
                </div>
              </div>
              <div class="p-5 sm:p-6 space-y-4 sm:space-y-5">
                <div class="grid grid-cols-1 md:grid-cols-2 gap-4 lg:gap-5">
                  <div class="space-y-2">
                    <label class="flex items-center gap-2 text-xs uppercase tracking-widest text-zinc-400 font-semibold">
                      Nome fantasia
                      <span class="text-[10px] font-bold tracking-normal normal-case px-1.5 py-0.5 rounded-md bg-indigo-500/10 border-indigo-500/20 text-indigo-300">Lido do banco</span>
                    </label>
                    <input
                      :value="authStore.currentUser?.restaurantName || ''"
                      disabled
                      class="w-full rounded-xl px-3.5 py-3 text-sm bg-zinc-900/60 border border-zinc-700/70 text-zinc-300 opacity-80 cursor-not-allowed"
                      placeholder="Nome do restaurante"
                    />
                    <p class="text-xs text-zinc-500">Nome cadastrado no perfil do restaurante (não editável aqui).</p>
                  </div>
                  <div class="space-y-2">
                    <label class="flex items-center gap-2 text-xs uppercase tracking-widest text-zinc-400 font-semibold">
                      CNPJ
                      <span class="text-[10px] font-bold tracking-normal normal-case px-1.5 py-0.5 rounded-md bg-zinc-800/70 border border-zinc-700/60 text-zinc-400">🔒 Sem backend</span>
                    </label>
                    <input
                      disabled
                      class="w-full rounded-xl px-3.5 py-3 text-sm bg-zinc-900/40 border border-zinc-700/50 text-zinc-500 cursor-not-allowed placeholder:text-zinc-600 font-mono"
                      placeholder="00.000.000/0001-00"
                    />
                    <p class="text-xs text-zinc-500">Campo existe na model mas não há endpoint de edição — vamos discutir.</p>
                  </div>
                </div>

                <div class="grid grid-cols-1 md:grid-cols-3 gap-4 lg:gap-5 pt-2">
                  <div class="space-y-2">
                    <label class="flex items-center gap-2 text-xs uppercase tracking-widest text-zinc-400 font-semibold">
                      Fuso horário
                      <span class="text-[10px] font-bold tracking-normal normal-case px-1.5 py-0.5 rounded-md bg-zinc-800/70 border border-zinc-700/60 text-zinc-400">🔒 Sem backend</span>
                    </label>
                    <select disabled class="w-full rounded-xl px-3.5 py-3 text-sm bg-zinc-900/40 border border-zinc-700/50 text-zinc-500 cursor-not-allowed">
                      <option>America/Sao_Paulo (UTC-3)</option>
                    </select>
                  </div>
                  <div class="space-y-2">
                    <label class="flex items-center gap-2 text-xs uppercase tracking-widest text-zinc-400 font-semibold">
                      Moeda
                      <span class="text-[10px] font-bold tracking-normal normal-case px-1.5 py-0.5 rounded-md bg-zinc-800/70 border border-zinc-700/60 text-zinc-400">🔒 Sem backend</span>
                    </label>
                    <select disabled class="w-full rounded-xl px-3.5 py-3 text-sm bg-zinc-900/40 border border-zinc-700/50 text-zinc-500 cursor-not-allowed">
                      <option>BRL — Real (R$)</option>
                    </select>
                  </div>
                  <div class="space-y-2">
                    <label class="flex items-center gap-2 text-xs uppercase tracking-widest text-zinc-400 font-semibold">
                      Idioma
                      <span class="text-[10px] font-bold tracking-normal normal-case px-1.5 py-0.5 rounded-md bg-zinc-800/70 border border-zinc-700/60 text-zinc-400">🔒 Sem backend</span>
                    </label>
                    <select disabled class="w-full rounded-xl px-3.5 py-3 text-sm bg-zinc-900/40 border border-zinc-700/50 text-zinc-500 cursor-not-allowed">
                      <option>Português (Brasil)</option>
                    </select>
                  </div>
                </div>

                <div class="rounded-xl border border-dashed border-zinc-700/80 bg-zinc-900/30 p-4 sm:p-5 space-y-2">
                  <p class="text-[11px] uppercase tracking-widest text-zinc-500 font-semibold flex items-center gap-1.5">
                    <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"></path>
                    </svg>
                    Campos faltantes no backend (discutir)
                  </p>
                  <ul class="list-disc list-inside text-xs text-zinc-400 space-y-1">
                    <li>Endereço completo, telefone e email de contato do restaurante</li>
                    <li>Horário de funcionamento (dias/turnos para pedidos, fechamento semanal)</li>
                    <li>Logo do restaurante (upload de imagem)</li>
                    <li>Taxas padrão (serviço, entrega, embalagem)</li>
                  </ul>
                </div>

                <div class="flex flex-col sm:flex-row sm:items-center sm:justify-end gap-3 pt-2">
                  <button
                    disabled
                    class="inline-flex items-center gap-2 rounded-xl px-4 py-2.5 text-sm font-semibold text-zinc-500 bg-zinc-800/50 border border-zinc-700/60 cursor-not-allowed"
                    title="Edições do perfil do restaurante ainda não implementadas no backend"
                  >
                    🔒 Salvar alterações
                  </button>
                </div>
              </div>
            </div>

            <aside class="space-y-4 xl:space-y-5">
              <div class="rounded-2xl border bg-zinc-900/50 border-zinc-800 p-5 space-y-3 shadow-xl">
                <div class="flex items-center gap-2 text-xs uppercase tracking-[0.18em] text-zinc-500 font-semibold">
                  <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path>
                  </svg>
                  O que já temos vs o que falta
                </div>
                <ul class="space-y-2.5 text-xs">
                  <li class="flex items-start gap-2">
                    <span class="text-emerald-300 mt-0.5 shrink-0">✓</span>
                    <span class="text-zinc-300"><strong>Nome do restaurante</strong> — disponível via token do usuário.</span>
                  </li>
                  <li class="flex items-start gap-2">
                    <span class="text-amber-300 mt-0.5 shrink-0">◯</span>
                    <span class="text-zinc-300"><strong>CNPJ</strong> — campo existe na model mas não tem endpoint GET/PUT público.</span>
                  </li>
                  <li class="flex items-start gap-2">
                    <span class="text-rose-300 mt-0.5 shrink-0">✕</span>
                    <span class="text-zinc-300"><strong>Fuso / Moeda / Idioma</strong> — campos não existem na model Restaurant.</span>
                  </li>
                </ul>
              </div>
            </aside>
          </div>
        </section>

        <!-- Seção Notificações -->
        <section v-else-if="activeSectionId === 'notifications'" class="space-y-6">
          <div class="grid grid-cols-1 xl:grid-cols-3 gap-6">
            <div class="xl:col-span-2 space-y-6">
              <div class="rounded-2xl border bg-zinc-900/50 border-zinc-800 shadow-xl overflow-hidden">
                <div class="p-5 sm:p-6 flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4 border-b border-zinc-800/80">
                  <div class="flex items-start gap-3 flex-1">
                    <div class="w-10 h-10 rounded-xl bg-zinc-800/60 border-zinc-700/60 text-zinc-300 flex items-center justify-center flex-shrink-0">
                      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"></path>
                      </svg>
                    </div>
                    <div class="space-y-0.5 flex-1">
                      <h2 class="text-base sm:text-lg font-semibold text-white tracking-tight">Canais de notificação</h2>
                      <p class="text-xs sm:text-sm text-zinc-400 leading-relaxed max-w-2xl">Escolha onde receber os alertas do sistema — pedidos, estoque, pagamentos e muito mais.</p>
                    </div>
                  </div>
                </div>
                <div class="p-5 sm:p-6 space-y-5">
                  <div class="flex items-start sm:items-center justify-between gap-4 p-4 rounded-xl bg-zinc-900/70 border border-zinc-800/80">
                    <div class="flex items-start gap-3">
                      <div class="w-10 h-10 rounded-lg bg-indigo-500/10 border-indigo-500/20 text-indigo-300 flex items-center justify-center shrink-0">
                        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"></path>
                        </svg>
                      </div>
                      <div>
                        <p class="text-sm font-semibold text-white">Email</p>
                        <p class="text-xs text-zinc-400 mt-0.5">Notificações por email (ex: fatura fechada, falha no pagamento).</p>
                      </div>
                    </div>
                    <span class="text-[10px] font-bold tracking-wide uppercase px-2 py-1 rounded-md bg-zinc-800/70 border border-zinc-700/60 text-zinc-400 shrink-0">🔒 Sem backend</span>
                  </div>

                  <div class="flex items-start sm:items-center justify-between gap-4 p-4 rounded-xl bg-zinc-900/70 border border-zinc-800/80">
                    <div class="flex items-start gap-3">
                      <div class="w-10 h-10 rounded-lg bg-zinc-800/60 border-zinc-700/60 text-zinc-300 flex items-center justify-center shrink-0">
                        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 18h.01M8 21h8a2 2 0 002-2V5a2 2 0 00-2-2H8a2 2 0 00-2 2v14a2 2 0 002 2z"></path>
                        </svg>
                      </div>
                      <div>
                        <p class="text-sm font-semibold text-white">Push no navegador</p>
                        <p class="text-xs text-zinc-400 mt-0.5">Alertas instantâneos (ex: novo pedido, estoque baixo).</p>
                      </div>
                    </div>
                    <span class="text-[10px] font-bold tracking-wide uppercase px-2 py-1 rounded-md bg-zinc-800/70 border border-zinc-700/60 text-zinc-400 shrink-0">🔒 Sem backend</span>
                  </div>

                  <div class="flex items-start sm:items-center justify-between gap-4 p-4 rounded-xl bg-zinc-900/70 border border-zinc-800/80">
                    <div class="flex items-start gap-3">
                      <div class="w-10 h-10 rounded-lg bg-emerald-500/10 border border-emerald-500/20 text-emerald-300 flex items-center justify-center shrink-0">
                        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z"></path>
                        </svg>
                      </div>
                      <div>
                        <p class="text-sm font-semibold text-white">WhatsApp (opcional)</p>
                        <p class="text-xs text-zinc-400 mt-0.5">Alertas urgentes via WhatsApp Business.</p>
                      </div>
                    </div>
                    <span class="text-[10px] font-bold tracking-wide uppercase px-2 py-1 rounded-md bg-zinc-800/70 border border-zinc-700/60 text-zinc-400 shrink-0">🔒 Sem backend</span>
                  </div>
                </div>
              </div>

              <div class="rounded-2xl border bg-zinc-900/50 border-zinc-800 shadow-xl overflow-hidden">
                <div class="p-5 sm:p-6 border-b border-zinc-800/80">
                  <div class="flex items-start gap-3">
                    <div class="w-10 h-10 rounded-xl bg-zinc-800/60 border-zinc-700/60 text-zinc-300 flex items-center justify-center flex-shrink-0">
                      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 10h16M4 14h16M4 18h16"></path>
                      </svg>
                    </div>
                    <div class="space-y-0.5 flex-1">
                      <h2 class="text-base sm:text-lg font-semibold text-white tracking-tight">Tipos de alerta</h2>
                      <p class="text-xs sm:text-sm text-zinc-400 leading-relaxed max-w-2xl">Quais eventos você quer receber? Desmarque o que não for importante.</p>
                    </div>
                  </div>
                </div>
                <div class="p-5 sm:p-6 space-y-3">
                  <div class="flex items-center justify-between gap-3 p-3 rounded-xl bg-zinc-900/50 border border-zinc-800/70">
                    <div>
                      <p class="text-sm font-semibold text-zinc-200">Novos pedidos</p>
                      <p class="text-xs text-zinc-500">Quando um cliente fizer um novo pedido.</p>
                    </div>
                    <span class="text-[10px] font-bold uppercase px-2 py-1 rounded-md bg-zinc-800/70 border border-zinc-700/60 text-zinc-400">🔒</span>
                  </div>
                  <div class="flex items-center justify-between gap-3 p-3 rounded-xl bg-zinc-900/50 border border-zinc-800/70">
                    <div>
                      <p class="text-sm font-semibold text-zinc-200">Estoque baixo</p>
                      <p class="text-xs text-zinc-500">Quando um ingrediente atingir o ponto de reposição.</p>
                    </div>
                    <span class="text-[10px] font-bold uppercase px-2 py-1 rounded-md bg-zinc-800/70 border border-zinc-700/60 text-zinc-400">🔒</span>
                  </div>
                  <div class="flex items-center justify-between gap-3 p-3 rounded-xl bg-zinc-900/50 border border-zinc-800/70">
                    <div>
                      <p class="text-sm font-semibold text-zinc-200">Faturamento</p>
                      <p class="text-xs text-zinc-500">Fatura emitida, pagamento confirmado ou recusado.</p>
                    </div>
                    <span class="text-[10px] font-bold uppercase px-2 py-1 rounded-md bg-zinc-800/70 border border-zinc-700/60 text-zinc-400">🔒</span>
                  </div>
                  <div class="flex items-center justify-between gap-3 p-3 rounded-xl bg-zinc-900/50 border border-zinc-800/70">
                    <div>
                      <p class="text-sm font-semibold text-zinc-200">Segurança</p>
                      <p class="text-xs text-zinc-500">Login em dispositivos novos, troca de senha, chave de API alterada.</p>
                    </div>
                    <span class="text-[10px] font-bold uppercase px-2 py-1 rounded-md bg-zinc-800/70 border border-zinc-700/60 text-zinc-400">🔒</span>
                  </div>
                </div>
              </div>
            </div>

            <aside class="space-y-4 xl:space-y-5 h-fit">
              <div class="rounded-2xl border bg-zinc-900/50 border-zinc-800 p-5 space-y-3 shadow-xl">
                <div class="flex items-center gap-2 text-xs uppercase tracking-[0.18em] text-zinc-500 font-semibold">
                  <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z"></path>
                  </svg>
                  Discussão pendente
                </div>
                <div class="text-xs text-zinc-400 leading-relaxed space-y-2">
                  <p>Hoje não temos <strong>NENHUM</strong> endpoint ou tabela de notificações.</p>
                  <p>Pontos pra gente alinhar:</p>
                  <ul class="list-disc list-inside space-y-1 text-zinc-300">
                    <li>Notificações vão ser <em>por usuário</em> ou <em>por restaurante</em> (todos gerentes recebem)?</li>
                    <li>Vamos usar provider tipo <strong>Firebase Cloud Messaging</strong> pra push?</li>
                    <li>WhatsApp Business API tem custo — vai ser feature de plano pago?</li>
                    <li>Quem define as preferências: cada usuário ou o dono/gerente do restaurante?</li>
                  </ul>
                </div>
              </div>
            </aside>
          </div>
        </section>

        <!-- Seção Segurança -->
        <section v-else-if="activeSectionId === 'security'" class="space-y-6">
          <div class="grid grid-cols-1 xl:grid-cols-3 gap-6">
            <div class="xl:col-span-2 space-y-6">
              <div class="rounded-2xl border bg-zinc-900/50 border-zinc-800 shadow-xl overflow-hidden">
                <div class="p-5 sm:p-6 flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4 border-b border-zinc-800/80">
                  <div class="flex items-start gap-3 flex-1">
                    <div class="w-10 h-10 rounded-xl bg-emerald-500/15 border border-emerald-500/30 text-emerald-300 flex items-center justify-center flex-shrink-0">
                      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"></path>
                      </svg>
                    </div>
                    <div class="space-y-0.5 flex-1">
                      <h2 class="text-base sm:text-lg font-semibold text-white tracking-tight">Senha da conta</h2>
                      <p class="text-xs sm:text-sm text-zinc-400 leading-relaxed max-w-2xl">Mantenha sua senha forte e atualizada para proteger o acesso.</p>
                    </div>
                  </div>
                </div>
                <div class="p-5 sm:p-6 space-y-4 sm:space-y-5">
                  <div class="space-y-2">
                    <label class="text-xs uppercase tracking-widest text-zinc-400 font-semibold">Senha atual</label>
                    <input
                      disabled
                      type="password"
                      placeholder="••••••••"
                      class="w-full rounded-xl px-3.5 py-3.5 text-sm bg-zinc-900/40 border border-zinc-700/50 text-zinc-500 cursor-not-allowed placeholder:text-zinc-600 font-mono"
                    />
                  </div>
                  <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div class="space-y-2">
                      <label class="flex items-center gap-2 text-xs uppercase tracking-widest text-zinc-400 font-semibold">
                        Nova senha
                        <span class="text-[10px] font-bold tracking-normal normal-case px-1.5 py-0.5 rounded-md bg-zinc-800/70 border border-zinc-700/60 text-zinc-400">🔒 Sem backend</span>
                      </label>
                      <input disabled type="password" placeholder="Mínimo 8 caracteres" class="w-full rounded-xl px-3.5 py-3.5 text-sm bg-zinc-900/40 border border-zinc-700/50 text-zinc-500 cursor-not-allowed placeholder:text-zinc-600" />
                    </div>
                    <div class="space-y-2">
                      <label class="text-xs uppercase tracking-widest text-zinc-400 font-semibold">Confirmar nova senha</label>
                      <input disabled type="password" placeholder="Repita a nova senha" class="w-full rounded-xl px-3.5 py-3.5 text-sm bg-zinc-900/40 border border-zinc-700/50 text-zinc-500 cursor-not-allowed placeholder:text-zinc-600" />
                    </div>
                  </div>
                  <div class="flex flex-col sm:flex-row sm:items-center sm:justify-end gap-3 pt-1">
                    <button disabled class="inline-flex items-center gap-2 rounded-xl px-4 py-2.5 text-sm font-semibold text-zinc-500 bg-zinc-800/50 border border-zinc-700/60 cursor-not-allowed">
                      🔒 Atualizar senha
                    </button>
                  </div>
                </div>
              </div>

              <div class="rounded-2xl border bg-zinc-900/50 border-zinc-800 shadow-xl overflow-hidden">
                <div class="p-5 sm:p-6 flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4 border-b border-zinc-800/80">
                  <div class="flex items-start gap-3 flex-1">
                    <div class="w-10 h-10 rounded-xl bg-amber-500/15 border border-amber-500/30 text-amber-300 flex items-center justify-center flex-shrink-0">
                      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"></path>
                      </svg>
                    </div>
                    <div class="space-y-0.5 flex-1">
                      <h2 class="text-base sm:text-lg font-semibold text-white tracking-tight">Autenticação em dois fatores (2FA)</h2>
                      <p class="text-xs sm:text-sm text-zinc-400 leading-relaxed max-w-2xl">Camada extra de segurança — além da senha, exige um código temporário.</p>
                    </div>
                    <span class="text-[10px] font-bold tracking-wide uppercase px-2 py-1 rounded-md bg-zinc-800/70 border border-zinc-700/60 text-zinc-400 shrink-0">🔒 Sem backend</span>
                  </div>
                </div>
                <div class="p-5 sm:p-6">
                  <div class="rounded-xl border border-dashed border-zinc-700/70 bg-zinc-900/30 p-4 sm:p-5 space-y-3">
                    <div class="flex items-center gap-2">
                      <div class="w-9 h-9 rounded-lg bg-rose-500/10 border border-rose-500/20 text-rose-300 flex items-center justify-center shrink-0">
                        <svg class="w-4.5 h-4.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
                        </svg>
                      </div>
                      <div>
                        <p class="text-sm font-semibold text-zinc-200">2FA desativado</p>
                        <p class="text-xs text-zinc-500 mt-0.5">Quando implementado, recomendamos ativar para todos os usuários.</p>
                      </div>
                    </div>
                    <div class="text-xs text-zinc-400 leading-relaxed">
                      Métodos planejados: código via App (Google Authenticator, Authy) e fallback por SMS/WhatsApp.
                    </div>
                    <button disabled class="inline-flex items-center gap-2 rounded-xl px-4 py-2.5 text-sm font-semibold text-zinc-500 bg-zinc-800/50 border border-zinc-700/60 cursor-not-allowed w-full sm:w-auto">
                      🔒 Configurar 2FA
                    </button>
                  </div>
                </div>
              </div>

              <div class="rounded-2xl border bg-zinc-900/50 border-zinc-800 shadow-xl overflow-hidden">
                <div class="p-5 sm:p-6 flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4 border-b border-zinc-800/80">
                  <div class="flex items-start gap-3 flex-1">
                    <div class="w-10 h-10 rounded-xl bg-indigo-500/15 border border-indigo-500/30 text-indigo-300 flex items-center justify-center flex-shrink-0">
                      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"></path>
                      </svg>
                    </div>
                    <div class="space-y-0.5 flex-1">
                      <h2 class="text-base sm:text-lg font-semibold text-white tracking-tight">Sessões ativas</h2>
                      <p class="text-xs sm:text-sm text-zinc-400 leading-relaxed max-w-2xl">Dispositivos logados recentemente — feche os que você não reconhece.</p>
                    </div>
                    <span class="text-[10px] font-bold tracking-wide uppercase px-2 py-1 rounded-md bg-zinc-800/70 border border-zinc-700/60 text-zinc-400 shrink-0">🔒 Sem backend</span>
                  </div>
                </div>
                <div class="p-5 sm:p-6">
                  <div class="rounded-xl bg-zinc-900/50 border border-zinc-800/70 divide-y divide-zinc-800/60">
                    <div class="flex items-start sm:items-center justify-between gap-3 p-4">
                      <div class="flex items-center gap-3">
                        <div class="w-10 h-10 rounded-lg bg-indigo-500/10 border border-indigo-500/20 text-indigo-300 flex items-center justify-center shrink-0">
                          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"></path>
                          </svg>
                        </div>
                        <div>
                          <p class="text-sm font-semibold text-zinc-200">Este dispositivo <span class="text-[10px] font-bold uppercase tracking-wide px-2 py-0.5 rounded-md bg-emerald-500/10 border border-emerald-500/20 text-emerald-300 ml-1">Atual</span></p>
                          <p class="text-xs text-zinc-500 mt-0.5">Navegador Desktop · entrando agora · IP local</p>
                        </div>
                      </div>
                    </div>
                  </div>
                  <p class="text-xs text-zinc-500 mt-4">Histórico de sessões não está implementado no AuthService (grep: 0 métodos de sessão ativa).</p>
                </div>
              </div>
            </div>

            <aside class="space-y-4 xl:space-y-5 h-fit">
              <div class="rounded-2xl border bg-zinc-900/50 border-zinc-800 p-5 space-y-3 shadow-xl">
                <div class="flex items-center gap-2 text-xs uppercase tracking-[0.18em] text-zinc-500 font-semibold">
                  <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"></path>
                  </svg>
                  Discussão pendente
                </div>
                <ul class="space-y-2 text-xs text-zinc-400 list-disc list-inside">
                  <li><strong>AuthService</strong> não tem métodos <code class="px-1 py-0.5 rounded bg-zinc-800/70 border border-zinc-700/60 font-mono text-[11px]">ChangePassword</code>, <code class="px-1 py-0.5 rounded bg-zinc-800/70 border border-zinc-700/60 font-mono text-[11px]">ResetPassword</code> ou 2FA.</li>
                  <li>Armazenar tokens de refresh em tabela (não só JWT stateless) para listar / encerrar sessões.</li>
                  <li>Hash de senha já usa BCrypt? Confirmar no Identity/UserManager.</li>
                  <li>Política de senha: força mínima, expiração, histórico?</li>
                </ul>
              </div>
            </aside>
          </div>
        </section>

        <!-- Seção Assinatura & Pagamento -->
        <section v-else-if="activeSectionId === 'billing'" class="space-y-6">
          <div class="grid grid-cols-1 xl:grid-cols-3 gap-6">
            <div class="xl:col-span-2 space-y-6">
              <div class="rounded-2xl border bg-zinc-900/50 border-zinc-800 shadow-xl overflow-hidden">
                <div class="p-5 sm:p-6 flex flex-col sm:flex-row sm:items-start sm:justify-between gap-5">
                  <div class="flex items-start gap-3 flex-1">
                    <div class="w-10 h-10 rounded-xl bg-indigo-500/20 border border-indigo-500/40 text-indigo-300 flex items-center justify-center flex-shrink-0">
                      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z"></path>
                      </svg>
                    </div>
                    <div class="space-y-1 flex-1">
                      <p class="text-[11px] uppercase tracking-[0.18em] text-indigo-300 font-semibold">Plano atual</p>
                      <h2 class="text-xl sm:text-2xl font-bold text-white tracking-tight">
                        Período de demonstração
                      </h2>
                      <p class="text-xs sm:text-sm text-zinc-300 leading-relaxed max-w-xl">
                        O restaurante está no período trial (gratuita). Quando implementarmos o módulo de pagamentos, você escolherá um plano e cadastrará uma forma de pagamento.
                      </p>
                    </div>
                  </div>
                  <div class="shrink-0 rounded-xl border border-emerald-500/30 bg-emerald-500/10 px-4 py-3 text-center">
                    <p class="text-[10px] uppercase tracking-widest text-emerald-300 font-semibold">Status</p>
                    <p class="text-lg font-bold text-emerald-200 mt-0.5">Ativo (trial)</p>
                  </div>
                </div>
              </div>

              <div class="rounded-2xl border bg-zinc-900/50 border-zinc-800 shadow-xl overflow-hidden">
                <div class="p-5 sm:p-6 border-b border-zinc-800/80">
                  <div class="flex items-start justify-between gap-3">
                    <h2 class="text-base sm:text-lg font-semibold text-white tracking-tight">Planos disponíveis</h2>
                    <span class="text-[10px] font-bold tracking-wide uppercase px-2 py-1 rounded-md bg-zinc-800/70 border border-zinc-700/60 text-zinc-400 shrink-0">🔒 Sem checkout</span>
                  </div>
                </div>
                <div class="grid grid-cols-1 md:grid-cols-3 divide-y md:divide-y-0 md:divide-x divide-zinc-800/80">
                  <div class="p-5 sm:p-6 space-y-3">
                    <p class="text-xs uppercase tracking-widest text-zinc-500 font-semibold">Essencial</p>
                    <div class="flex items-baseline gap-1">
                      <span class="text-3xl font-bold text-white">R$ 89</span>
                      <span class="text-xs text-zinc-500">/mês</span>
                    </div>
                    <ul class="space-y-1.5 text-xs text-zinc-300 pt-2">
                      <li>✓ Até 2 usuários</li>
                      <li>✓ Importações ilimitadas</li>
                      <li>✓ Suporte por email</li>
                      <li class="text-zinc-500">— Relatórios avançados</li>
                      <li class="text-zinc-500">— API de integração</li>
                    </ul>
                    <button disabled class="w-full mt-3 rounded-xl px-3 py-2.5 text-sm font-semibold text-zinc-500 bg-zinc-800/50 border border-zinc-700/60 cursor-not-allowed">
                      🔒 Escolher plano
                    </button>
                  </div>
                  <div class="p-5 sm:p-6 space-y-3 bg-gradient-to-b from-indigo-500/5 to-transparent relative">
                    <div class="absolute top-3 right-3 text-[10px] font-bold uppercase tracking-wider px-2 py-0.5 rounded-md bg-indigo-500/20 border border-indigo-500/30 text-indigo-300">Popular</div>
                    <p class="text-xs uppercase tracking-widest text-indigo-300 font-semibold">Profissional</p>
                    <div class="flex items-baseline gap-1">
                      <span class="text-3xl font-bold text-white">R$ 179</span>
                      <span class="text-xs text-zinc-500">/mês</span>
                    </div>
                    <ul class="space-y-1.5 text-xs text-zinc-200 pt-2">
                      <li>✓ Até 10 usuários</li>
                      <li>✓ Importações ilimitadas</li>
                      <li>✓ Relatórios avançados</li>
                      <li>✓ Suporte prioritário</li>
                      <li>✓ API de integração</li>
                    </ul>
                    <button disabled class="w-full mt-3 rounded-xl px-3 py-2.5 text-sm font-semibold text-zinc-500 bg-zinc-800/50 border border-zinc-700/60 cursor-not-allowed">
                      🔒 Escolher plano
                    </button>
                  </div>
                  <div class="p-5 sm:p-6 space-y-3">
                    <p class="text-xs uppercase tracking-widest text-zinc-500 font-semibold">Empresarial</p>
                    <div class="flex items-baseline gap-1">
                      <span class="text-3xl font-bold text-white">Sob consulta</span>
                    </div>
                    <ul class="space-y-1.5 text-xs text-zinc-300 pt-2">
                      <li>✓ Usuários ilimitados</li>
                      <li>✓ Múltiplos restaurantes</li>
                      <li>✓ Onboarding dedicado</li>
                      <li>✓ Suporte 24/7</li>
                      <li>✓ SLA e contrato</li>
                    </ul>
                    <button disabled class="w-full mt-3 rounded-xl px-3 py-2.5 text-sm font-semibold text-zinc-500 bg-zinc-800/50 border border-zinc-700/60 cursor-not-allowed">
                      🔒 Falar com vendas
                    </button>
                  </div>
                </div>
              </div>

              <div class="rounded-2xl border bg-zinc-900/50 border-zinc-800 shadow-xl overflow-hidden">
                <div class="p-5 sm:p-6 flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4 border-b border-zinc-800/80">
                  <div class="flex items-start gap-3 flex-1">
                    <div class="w-10 h-10 rounded-xl bg-rose-500/10 border-rose-500/20 text-rose-300 flex items-center justify-center flex-shrink-0">
                      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z"></path>
                      </svg>
                    </div>
                    <div class="space-y-0.5 flex-1">
                      <h2 class="text-base sm:text-lg font-semibold text-white tracking-tight">Forma de pagamento</h2>
                      <p class="text-xs sm:text-sm text-zinc-400 leading-relaxed max-w-2xl">Cartão de crédito recorrente. Alterne ou remova métodos a qualquer momento.</p>
                    </div>
                    <span class="text-[10px] font-bold tracking-wide uppercase px-2 py-1 rounded-md bg-zinc-800/70 border border-zinc-700/60 text-zinc-400 shrink-0">🔒 Sem backend</span>
                  </div>
                </div>
                <div class="p-5 sm:p-6">
                  <div class="rounded-xl border border-dashed border-zinc-700/70 bg-zinc-900/30 p-5 space-y-3">
                    <div class="flex items-center gap-3">
                      <div class="w-12 h-8 rounded-md bg-zinc-800/80 border border-zinc-700/60 flex items-center justify-center text-zinc-500 text-[10px] font-bold tracking-widest shrink-0">CARD</div>
                      <div>
                        <p class="text-sm font-semibold text-zinc-400">Nenhum cartão cadastrado</p>
                        <p class="text-xs text-zinc-500 mt-0.5">Quando sair do trial, será obrigatório cadastrar um cartão.</p>
                      </div>
                    </div>
                    <button disabled class="inline-flex items-center gap-2 rounded-xl px-4 py-2.5 text-sm font-semibold text-zinc-500 bg-zinc-800/50 border border-zinc-700/60 cursor-not-allowed">
                      🔒 Cadastrar cartão
                    </button>
                  </div>
                </div>
              </div>

              <div class="rounded-2xl border bg-zinc-900/50 border-zinc-800 shadow-xl overflow-hidden">
                <div class="p-5 sm:p-6 flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4 border-b border-zinc-800/80">
                  <div class="flex items-start gap-3 flex-1">
                    <div class="w-10 h-10 rounded-xl bg-indigo-500/15 border-indigo-500/30 text-indigo-300 flex items-center justify-center flex-shrink-0">
                      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"></path>
                      </svg>
                    </div>
                    <div class="space-y-0.5 flex-1">
                      <h2 class="text-base sm:text-lg font-semibold text-white tracking-tight">Histórico de faturas</h2>
                      <p class="text-xs sm:text-sm text-zinc-400 leading-relaxed max-w-2xl">Visualize e baixe os comprovantes de pagamento.</p>
                    </div>
                    <span class="text-[10px] font-bold tracking-wide uppercase px-2 py-1 rounded-md bg-zinc-800/70 border border-zinc-700/60 text-zinc-400 shrink-0">🔒 Sem backend</span>
                  </div>
                </div>
                <div class="p-5 sm:p-6">
                  <div class="rounded-xl bg-zinc-900/50 border border-zinc-800/70 p-4 sm:p-5 text-center">
                    <p class="text-sm text-zinc-300">Nenhuma fatura emitida</p>
                    <p class="text-xs text-zinc-500 mt-1">Você ainda está no período trial — a primeira fatura será emitida ao final.</p>
                  </div>
                </div>
              </div>
            </div>

            <aside class="space-y-4 xl:space-y-5 h-fit">
              <div class="rounded-2xl border bg-zinc-900/50 border-zinc-800 p-5 space-y-3 shadow-xl">
                <div class="flex items-center gap-2 text-xs uppercase tracking-[0.18em] text-zinc-500 font-semibold">
                  <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path>
                  </svg>
                  O que já temos vs o que falta
                </div>
                <ul class="space-y-2.5 text-xs">
                  <li class="flex items-start gap-2">
                    <span class="text-emerald-300 mt-0.5 shrink-0">✓</span>
                    <span class="text-zinc-300"><strong>Campos PlanoId, TrialEndAtUtc, IsPlanActive</strong> na model Restaurant.</span>
                  </li>
                  <li class="flex items-start gap-2">
                    <span class="text-amber-300 mt-0.5 shrink-0">◯</span>
                    <span class="text-zinc-300"><strong>Plano</strong> navigation property — existe mas não tem endpoints de listagem/contratação.</span>
                  </li>
                  <li class="flex items-start gap-2">
                    <span class="text-rose-300 mt-0.5 shrink-0">✕</span>
                    <span class="text-zinc-300"><strong>Módulo pagamentos</strong> — sem gateway (Stripe/Asaas/Mercado Pago), sem invoices, sem webhooks.</span>
                  </li>
                </ul>
              </div>
              <div class="rounded-2xl border bg-zinc-900/50 border-zinc-800 p-5 space-y-3 shadow-xl">
                <div class="flex items-center gap-2 text-xs uppercase tracking-[0.18em] text-zinc-500 font-semibold">
                  <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path>
                  </svg>
                  Discussão pendente
                </div>
                <ul class="space-y-2 text-xs text-zinc-400 list-disc list-inside">
                  <li>Qual gateway? <strong>Stripe</strong> (internacional) vs <strong>Asaas</strong> / <strong>Mercado Pago</strong> (BR com Pix e boleto).</li>
                  <li>Preços dos planos acima são só placeholder — vocês definirem os valores reais.</li>
                  <li>Renovação automática? Juros em atraso? Período de carência?</li>
                  <li>Reembolso pro-rata no downgrade/cancelamento?</li>
                </ul>
              </div>
            </aside>
          </div>
        </section>

        <!-- Fallback genérico (não deve cair aqui) -->
        <section v-else class="rounded-2xl border border-zinc-800 bg-zinc-900/40 p-10 sm:p-16 text-center space-y-4">
          <div class="mx-auto w-14 h-14 rounded-2xl bg-zinc-800/80 border border-zinc-700/60 flex items-center justify-center text-zinc-400">
            <svg class="w-7 h-7" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path>
            </svg>
          </div>
          <h3 class="text-lg sm:text-xl font-semibold text-white tracking-tight">Seção não encontrada</h3>
          <p class="text-sm text-zinc-400 max-w-lg mx-auto leading-relaxed">
            Tente selecionar uma opção no menu ao lado.
          </p>
        </section>
      </main>
    </div>

    <footer class="border-t border-zinc-800/70 bg-zinc-950/80 px-4 sm:px-6 py-5 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 text-xs text-zinc-500">
      <p>© 2026 ISM — Intelligence Supply Manager. Todos os direitos reservados.</p>
      <div class="flex items-center gap-4">
        <nuxt-link to="/" class="hover:text-zinc-200">Início</nuxt-link>
        <nuxt-link to="/login" class="hover:text-zinc-200">Login</nuxt-link>
        <a href="http://localhost:8080/swagger" target="_blank" class="hover:text-zinc-200">Swagger API</a>
      </div>
    </footer>
  </div>
</template>
