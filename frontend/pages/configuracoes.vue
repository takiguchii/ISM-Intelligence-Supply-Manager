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
import { useThemeStore } from "~/stores/theme";

definePageMeta({ layout: false });

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();
const themeStore = useThemeStore();

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
type ConfigSectionId = "ai" | "general" | "appearance" | "notifications" | "security" | "billing";

const sections: Array<{
  id: ConfigSectionId;
  label: string;
  subtitle: string;
  icon: "sparkles" | "sliders" | "palette" | "bell" | "shield" | "credit";
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
    id: "appearance",
    label: "Aparência",
    subtitle: "Tema claro, escuro e densidade visual",
    icon: "palette"
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

interface TmpAuthorizationDto {
  id: number;
  restaurantId: number;
  name: string;
  scope: string;
  description?: string | null;
  createdByUserId?: number | null;
  createdByUserName?: string | null;
  revokedByUserId?: number | null;
  revokedByUserName?: string | null;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
  expiresAtUtc: string;
  autoRotateAtUtc?: string | null;
  revokedAtUtc?: string | null;
  lastUsedAtUtc?: string | null;
  isActive: boolean;
  plainTokenPreview?: string | null;
}

interface CreateTmpAuthorizationRequest {
  name: string;
  scope: string;
  description?: string | null;
  validityHours: number;
  autoRotateHours?: number | null;
}

interface CreateTmpAuthorizationResult {
  token: TmpAuthorizationDto;
  plainToken: string;
}

type TokenComputedStatus = "active" | "expired" | "revoked" | "rotating-soon";

const tmpTokens = ref<TmpAuthorizationDto[]>([]);
const tmpTokensLoading = ref(false);
const tmpTokensError = ref<string | null>(null);

const createTokenOpen = ref(false);
const createTokenSubmitting = ref(false);
const createTokenForm = ref<CreateTmpAuthorizationRequest>({
  name: "",
  scope: "read",
  description: null,
  validityHours: 720,
  autoRotateHours: null
});

const lastCreatedModal = ref<{ visible: boolean; plainToken: string; tokenId: number; name: string }>({
  visible: false,
  plainToken: "",
  tokenId: 0,
  name: ""
});
const lastCreatedCopied = ref(false);

function computeTokenStatus(t: TmpAuthorizationDto): TokenComputedStatus {
  if (t.revokedAtUtc) return "revoked";
  const now = Date.now();
  const exp = new Date(t.expiresAtUtc).getTime();
  if (exp < now) return "expired";
  if (t.autoRotateAtUtc) {
    const rot = new Date(t.autoRotateAtUtc).getTime();
    const hours = (rot - now) / 3_600_000;
    if (hours > 0 && hours <= 24) return "rotating-soon";
  }
  return t.isActive ? "active" : "revoked";
}
function formatDateUtc(v: string | null | undefined) {
  if (!v) return "—";
  try {
    const d = new Date(v);
    return d.toLocaleString("pt-BR", {
      day: "2-digit",
      month: "2-digit",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
      timeZone: "America/Sao_Paulo"
    });
  } catch {
    return "—";
  }
}
function relativeHours(h: number | null | undefined) {
  if (h == null) return "—";
  if (h < 24) return `${h}h`;
  const days = Math.round(h / 24);
  return `${days}d`;
}

async function loadTmpTokens() {
  if (!isRestaurantUser.value && !isSuperAdmin.value) return;
  tmpTokensLoading.value = true;
  tmpTokensError.value = null;
  try {
    const { apiClientWithAuth } = await import("~/services/api/client");
    const list = (await apiClientWithAuth<TmpAuthorizationDto[]>("/api/tmp-authorizations", {
      method: "GET"
    })) as TmpAuthorizationDto[];
    tmpTokens.value = list || [];
  } catch (err: any) {
    tmpTokensError.value =
      err?.data?.title ?? err?.data?.message ?? err?.message ?? "Não foi possível carregar os tokens.";
  } finally {
    tmpTokensLoading.value = false;
  }
}

async function handleCreateToken() {
  if (!createTokenForm.value.name.trim()) return;
  createTokenSubmitting.value = true;
  tmpTokensError.value = null;
  try {
    const payload: CreateTmpAuthorizationRequest = {
      name: createTokenForm.value.name.trim(),
      scope: createTokenForm.value.scope || "read",
      description: createTokenForm.value.description?.trim() || null,
      validityHours: Math.max(1, createTokenForm.value.validityHours || 720),
      autoRotateHours:
        createTokenForm.value.autoRotateHours && createTokenForm.value.autoRotateHours > 0
          ? createTokenForm.value.autoRotateHours
          : null
    };
    const { apiClientWithAuth } = await import("~/services/api/client");
    const res = (await apiClientWithAuth<CreateTmpAuthorizationResult>("/api/tmp-authorizations", {
      method: "POST",
      body: JSON.stringify(payload)
    })) as CreateTmpAuthorizationResult;

    lastCreatedModal.value = {
      visible: true,
      plainToken: res.plainToken,
      tokenId: res.token.id,
      name: res.token.name
    };
    lastCreatedCopied.value = false;

    createTokenOpen.value = false;
    createTokenForm.value = {
      name: "",
      scope: "read",
      description: null,
      validityHours: 720,
      autoRotateHours: null
    };
    await loadTmpTokens();
  } catch (err: any) {
    tmpTokensError.value =
      err?.data?.title ?? err?.data?.message ?? err?.message ?? "Erro ao criar token.";
  } finally {
    createTokenSubmitting.value = false;
  }
}

async function handleRotateToken(id: number) {
  tmpTokensError.value = null;
  try {
    const { apiClientWithAuth } = await import("~/services/api/client");
    await apiClientWithAuth<TmpAuthorizationDto>(`/api/tmp-authorizations/${id}/rotate`, {
      method: "POST"
    });
    await loadTmpTokens();
  } catch (err: any) {
    tmpTokensError.value =
      err?.data?.title ?? err?.data?.message ?? err?.message ?? "Erro ao rotacionar token.";
  }
}

async function handleRevokeToken(id: number) {
  tmpTokensError.value = null;
  try {
    const { apiClientWithAuth } = await import("~/services/api/client");
    await apiClientWithAuth(`/api/tmp-authorizations/${id}`, {
      method: "DELETE"
    });
    await loadTmpTokens();
  } catch (err: any) {
    tmpTokensError.value =
      err?.data?.title ?? err?.data?.message ?? err?.message ?? "Erro ao revogar token.";
  }
}

async function copyPlainToken() {
  try {
    await navigator.clipboard.writeText(lastCreatedModal.value.plainToken);
    lastCreatedCopied.value = true;
    setTimeout(() => (lastCreatedCopied.value = false), 2500);
  } catch {
    lastCreatedCopied.value = false;
  }
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
  await loadTmpTokens();
  const min = 900;
  const wait = Math.max(0, min - (Date.now() - start));
  setTimeout(() => (isLoading.value = false), wait);
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

    <header
      :class="[
        'h-16 border-b sticky top-0 z-30 px-4 sm:px-6 flex items-center justify-between backdrop-blur-xl',
        themeStore.isDark
          ? 'border-zinc-800/80 bg-zinc-900/60'
          : 'border-zinc-200 bg-white/80 shadow-sm'
      ]"
    >
      <div class="flex items-center gap-4">
        <button
          @click="toggleSidebar()"
          :class="[
            'p-2 rounded-xl transition-all duration-200 focus:outline-none focus:ring-2',
            themeStore.isDark
              ? 'text-zinc-300 hover:text-white hover:bg-zinc-800/80 focus:ring-zinc-600'
              : 'text-zinc-600 hover:text-zinc-900 hover:bg-zinc-100 focus:ring-zinc-300'
          ]"
          title="Abrir Menu Lateral"
        >
          <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"></path>
          </svg>
        </button>
        <div class="flex items-center gap-3">
          <span
            :class="[
              'font-bold text-lg tracking-tight',
              themeStore.isDark ? 'text-white' : 'text-zinc-900'
            ]"
          >ISM</span>
          <span
            :class="[
              'hidden sm:inline-block text-xs uppercase tracking-widest font-mono border-l pl-3',
              themeStore.isDark
                ? 'text-zinc-400 border-zinc-700/60'
                : 'text-zinc-500 border-zinc-200'
            ]"
          >
            Intelligence Supply
          </span>
        </div>
      </div>

      <nav class="hidden md:flex items-center gap-1">
        <nuxt-link
          to="/"
          :class="[
            'px-3 py-2 text-sm rounded-lg transition-colors',
            themeStore.isDark
              ? 'text-zinc-400 hover:text-white hover:bg-zinc-800/60'
              : 'text-zinc-600 hover:text-zinc-900 hover:bg-zinc-100'
          ]"
        >Início</nuxt-link>
        <nuxt-link
          to="/integracoes"
          :class="[
            'px-3 py-2 text-sm rounded-lg transition-colors',
            themeStore.isDark
              ? 'text-zinc-400 hover:text-white hover:bg-zinc-800/60'
              : 'text-zinc-600 hover:text-zinc-900 hover:bg-zinc-100'
          ]"
        >Integrações</nuxt-link>
        <span
          :class="[
            'px-3 py-2 text-sm font-semibold rounded-lg',
            themeStore.isDark
              ? 'text-white bg-zinc-800/80'
              : 'text-zinc-900 bg-zinc-200/80'
          ]"
        >Configurações</span>
      </nav>

      <div class="flex items-center gap-3">
        <button
          @click="authStore.logout()"
          :class="[
            'px-3 py-2 rounded-lg text-xs font-semibold tracking-wide uppercase border transition-all',
            themeStore.isDark
              ? 'text-zinc-300 hover:text-white hover:bg-zinc-800/60 border-zinc-700/60'
              : 'text-zinc-600 hover:text-zinc-900 hover:bg-zinc-100 border-zinc-300'
          ]"
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
      <aside
        :class="[
          'hidden md:block w-72 shrink-0 border-r',
          themeStore.isDark
            ? 'border-zinc-800/80 bg-zinc-950/60'
            : 'border-zinc-200 bg-white/60'
        ]"
      >
        <div class="sticky top-16 p-6 space-y-6">
          <div class="space-y-1">
            <div
              :class="[
                'flex items-center gap-2 text-[11px] uppercase tracking-[0.14em] font-semibold',
                themeStore.isDark ? 'text-zinc-500' : 'text-zinc-500'
              ]"
            >
              <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z"></path>
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"></path>
              </svg>
              Configurações
            </div>
            <div
              v-if="isRestaurantUser"
              :class="[
                'flex items-center gap-2 mt-3 p-3 rounded-xl border',
                themeStore.isDark
                  ? 'bg-zinc-900/70 border-zinc-800/70'
                  : 'bg-zinc-50 border-zinc-200'
              ]"
            >
              <div class="w-9 h-9 rounded-lg bg-amber-500/10 border border-amber-500/20 text-amber-500 flex items-center justify-center shrink-0">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"></path>
                </svg>
              </div>
              <div class="min-w-0">
                <p
                  :class="[
                    'text-xs uppercase tracking-wider font-semibold',
                    themeStore.isDark ? 'text-zinc-500' : 'text-zinc-500'
                  ]"
                >Restaurante</p>
                <p
                  :class="[
                    'text-sm font-semibold truncate',
                    themeStore.isDark ? 'text-white' : 'text-zinc-900'
                  ]"
                >{{ authStore.currentUser?.restaurantName || "Meu restaurante" }}</p>
              </div>
            </div>
            <div v-else-if="isSuperAdmin" class="mt-3 space-y-2">
              <label
                :class="[
                  'text-[11px] uppercase tracking-widest font-semibold',
                  themeStore.isDark ? 'text-zinc-500' : 'text-zinc-500'
                ]"
              >Restaurante alvo</label>
              <select
                v-model="targetRestaurantId"
                :class="[
                  'w-full rounded-xl px-3.5 py-2.5 text-sm border focus:outline-none focus:ring-2 focus:ring-amber-400/40 focus:border-amber-400/40',
                  themeStore.isDark
                    ? 'bg-zinc-900/80 border-zinc-700/70 text-zinc-100'
                    : 'bg-white border-zinc-300 text-zinc-900 shadow-sm'
                ]"
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
                'w-full group rounded-xl text-left flex items-start gap-3 px-3 py-3 transition-all duration-200 border',
                s.locked
                  ? 'opacity-50 cursor-not-allowed hover:bg-transparent border-transparent'
                  : (
                      themeStore.isDark
                        ? 'cursor-pointer hover:bg-zinc-900/70 border-transparent'
                        : 'cursor-pointer hover:bg-zinc-100 border-transparent'
                    ),
                activeSectionId === s.id
                  ? (
                      themeStore.isDark
                        ? 'bg-indigo-500/10 border-indigo-500/30 shadow-inner'
                        : 'bg-indigo-50 border-indigo-200 shadow-sm'
                    )
                  : ''
              ]"
            >
              <div
                :class="[
                  'w-9 h-9 rounded-lg flex items-center justify-center shrink-0 border',
                  activeSectionId === s.id
                    ? (
                        themeStore.isDark
                          ? 'bg-indigo-500/20 border-indigo-500/30 text-indigo-300'
                          : 'bg-indigo-100 border-indigo-200 text-indigo-700'
                      )
                    : (
                        themeStore.isDark
                          ? 'bg-zinc-900/80 border-zinc-800/80 text-zinc-400 group-hover:text-zinc-200'
                          : 'bg-zinc-100 border-zinc-200 text-zinc-500 group-hover:text-zinc-700'
                      )
                ]"
              >
                <svg v-if="s.icon === 'sparkles'" class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 3v4M3 5h4M6 17v4m-2-2h4m5-16l2.286 6.857L21 12l-5.714 2.143L13 21l-2.286-6.857L5 12l5.714-2.143L13 3z"></path>
                </svg>
                <svg v-else-if="s.icon === 'sliders'" class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6V4m0 2a2 2 0 100 4m0-4a2 2 0 110 4m-6 8a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4m6 6v10m6-2a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4"></path>
                </svg>
                <svg v-else-if="s.icon === 'palette'" class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 21a4 4 0 01-4-4V5a2 2 0 012-2h4a2 2 0 012 2v12a4 4 0 01-4 4zm0 0h12a2 2 0 002-2v-4a2 2 0 00-2-2h-2.343M11 7.343l1.657-1.657a2 2 0 012.828 0l2.829 2.829a2 2 0 010 2.828l-8.486 8.485M7 17h.01"></path>
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
                  <p
                    :class="[
                      'text-sm font-semibold',
                      activeSectionId === s.id
                        ? (themeStore.isDark ? 'text-white' : 'text-zinc-900')
                        : (themeStore.isDark ? 'text-zinc-200' : 'text-zinc-700')
                    ]"
                  >{{ s.label }}</p>
                  <span
                    v-if="s.locked"
                    :class="[
                      'text-[10px] font-semibold uppercase tracking-widest px-2 py-0.5 rounded-full border',
                      themeStore.isDark
                        ? 'bg-zinc-800/80 border-zinc-700/60 text-zinc-400'
                        : 'bg-zinc-200 border-zinc-300 text-zinc-600'
                    ]"
                  >Em breve</span>
                </div>
                <p
                  :class="[
                    'text-xs mt-0.5 truncate',
                    themeStore.isDark ? 'text-zinc-500' : 'text-zinc-500'
                  ]"
                >{{ s.subtitle }}</p>
              </div>
            </button>
          </nav>

          <div
            :class="[
              'rounded-xl border p-4 space-y-2',
              themeStore.isDark
                ? 'border-indigo-500/20 bg-indigo-500/5'
                : 'border-indigo-200 bg-indigo-50/70'
            ]"
          >
            <p
              :class="[
                'text-[11px] uppercase tracking-widest font-semibold flex items-center gap-1.5',
                themeStore.isDark ? 'text-indigo-300' : 'text-indigo-700'
              ]"
            >
              <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"></path>
              </svg>
              Documentação
            </p>
            <p
              :class="[
                'text-xs leading-relaxed',
                themeStore.isDark ? 'text-zinc-300' : 'text-zinc-600'
              ]"
            >Pegue sua chave gratuita no <a href="https://aistudio.google.com/apikey" target="_blank" :class="['underline font-medium', themeStore.isDark ? 'text-indigo-300 hover:text-indigo-200' : 'text-indigo-600 hover:text-indigo-700']">Google AI Studio</a>.</p>
          </div>
        </div>
      </aside>

      <!-- Conteúdo principal -->
      <main class="flex-1 min-w-0 px-4 sm:px-6 lg:px-10 py-8 space-y-8">
        <header class="space-y-1">
          <div
            :class="[
              'flex flex-wrap items-center gap-2 text-xs uppercase tracking-widest font-semibold',
              themeStore.isDark ? 'text-zinc-500' : 'text-zinc-500'
            ]"
          >
            <nuxt-link :class="themeStore.isDark ? 'hover:text-zinc-300' : 'hover:text-zinc-700'" to="/">Início</nuxt-link>
            <span :class="themeStore.isDark ? 'text-zinc-700' : 'text-zinc-300'">/</span>
            <span :class="themeStore.isDark ? 'text-zinc-300' : 'text-zinc-700'">Configurações</span>
            <span :class="themeStore.isDark ? 'text-zinc-700' : 'text-zinc-300'">/</span>
            <span :class="themeStore.isDark ? 'text-indigo-300' : 'text-indigo-600'">{{ sections.find((s) => s.id === activeSectionId)?.label }}</span>
          </div>
          <div class="flex flex-col sm:flex-row sm:items-end sm:justify-between gap-4">
            <div>
              <h1
                :class="[
                  'text-2xl sm:text-3xl font-bold tracking-tight',
                  themeStore.isDark ? 'text-white' : 'text-zinc-900'
                ]"
              >
                {{ sections.find((s) => s.id === activeSectionId)?.label }}
              </h1>
              <p
                :class="[
                  'text-sm mt-1 max-w-2xl',
                  themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500'
                ]"
              >{{ sections.find((s) => s.id === activeSectionId)?.subtitle }}</p>
            </div>
            <div v-if="activeSectionId !== 'billing'" class="flex flex-wrap gap-2 md:hidden">
              <button
                @click="toggleSidebar()"
                :class="[
                  'px-3 py-2 rounded-lg border text-xs font-semibold',
                  themeStore.isDark
                    ? 'border-zinc-800 bg-zinc-900/60 text-zinc-200'
                    : 'border-zinc-300 bg-white text-zinc-700 shadow-sm'
                ]"
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
                  ? (
                      themeStore.isDark
                        ? 'bg-indigo-500/10 text-indigo-300 border-indigo-500/30'
                        : 'bg-indigo-50 text-indigo-700 border-indigo-200'
                    )
                  : (
                      themeStore.isDark
                        ? 'bg-zinc-900/60 text-zinc-300 border-zinc-800 hover:bg-zinc-800/60'
                        : 'bg-white text-zinc-700 border-zinc-200 hover:bg-zinc-50'
                    )
              ]"
            >
              {{ s.label }}
              <span
                v-if="s.locked"
                :class="['ml-2 text-[10px]', themeStore.isDark ? 'text-zinc-500' : 'text-zinc-500']"
              >(em breve)</span>
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
            <div class="xl:col-span-2 space-y-6">
              <div
                :class="[
                  'rounded-2xl border shadow-xl overflow-hidden',
                  themeStore.isDark
                    ? 'bg-zinc-900/50 border-zinc-800'
                    : 'bg-white border-zinc-200'
                ]"
              >
                <div
                  :class="[
                    'p-5 sm:p-6 flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4 border-b',
                    themeStore.isDark ? 'border-zinc-800/80' : 'border-zinc-200'
                  ]"
                >
                  <div class="flex items-start gap-3 flex-1">
                    <div
                      :class="[
                        'w-10 h-10 rounded-xl flex items-center justify-center flex-shrink-0 border',
                        themeStore.isDark
                          ? 'bg-indigo-500/15 border-indigo-500/30 text-indigo-300'
                          : 'bg-indigo-50 border-indigo-200 text-indigo-600'
                      ]"
                    >
                      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6V4m0 2a2 2 0 100 4m0-4a2 2 0 110 4m-6 8a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4m6 6v10m6-2a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4"></path>
                      </svg>
                    </div>
                    <div class="space-y-0.5 flex-1">
                      <h2
                        :class="[
                          'text-base sm:text-lg font-semibold tracking-tight',
                          themeStore.isDark ? 'text-white' : 'text-zinc-900'
                        ]"
                      >Informações do restaurante</h2>
                      <p
                        :class="[
                          'text-xs sm:text-sm leading-relaxed max-w-2xl',
                          themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500'
                        ]"
                      >Dados básicos do estabelecimento e preferências regionais.</p>
                    </div>
                  </div>
                </div>
                <div class="p-5 sm:p-6 space-y-4 sm:space-y-5">
                  <div class="grid grid-cols-1 md:grid-cols-2 gap-4 lg:gap-5">
                    <div class="space-y-2">
                      <label
                        :class="[
                          'flex items-center gap-2 text-xs uppercase tracking-widest font-semibold',
                          themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500'
                        ]"
                      >
                        Nome fantasia
                        <span
                          :class="[
                            'text-[10px] font-bold tracking-normal normal-case px-1.5 py-0.5 rounded-md border',
                            themeStore.isDark
                              ? 'bg-indigo-500/10 border-indigo-500/20 text-indigo-300'
                              : 'bg-indigo-50 border-indigo-200 text-indigo-600'
                          ]"
                        >Lido do banco</span>
                      </label>
                      <input
                        :value="authStore.currentUser?.restaurantName || ''"
                        disabled
                        :class="[
                          'w-full rounded-xl px-3.5 py-3 text-sm border opacity-80 cursor-not-allowed',
                          themeStore.isDark
                            ? 'bg-zinc-900/60 border-zinc-700/70 text-zinc-300'
                            : 'bg-zinc-100 border-zinc-200 text-zinc-600'
                        ]"
                        placeholder="Nome do restaurante"
                      />
                      <p :class="['text-xs', themeStore.isDark ? 'text-zinc-500' : 'text-zinc-400']">Nome cadastrado no perfil do restaurante (não editável aqui).</p>
                    </div>
                    <div class="space-y-2">
                      <label
                        :class="[
                          'flex items-center gap-2 text-xs uppercase tracking-widest font-semibold',
                          themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500'
                        ]"
                      >
                        CNPJ
                        <span
                          :class="[
                            'text-[10px] font-bold tracking-normal normal-case px-1.5 py-0.5 rounded-md border',
                            themeStore.isDark
                              ? 'bg-zinc-800/70 border-zinc-700/60 text-zinc-400'
                              : 'bg-zinc-200 border-zinc-300 text-zinc-600'
                          ]"
                        >🔒 Sem backend</span>
                      </label>
                      <input
                        disabled
                        :class="[
                          'w-full rounded-xl px-3.5 py-3 text-sm cursor-not-allowed placeholder font-mono border',
                          themeStore.isDark
                            ? 'bg-zinc-900/40 border-zinc-700/50 text-zinc-500 placeholder:text-zinc-600'
                            : 'bg-zinc-100 border-zinc-200 text-zinc-500 placeholder:text-zinc-400'
                        ]"
                        placeholder="00.000.000/0001-00"
                      />
                      <p :class="['text-xs', themeStore.isDark ? 'text-zinc-500' : 'text-zinc-400']">Campo existe na model mas não há endpoint de edição — vamos discutir.</p>
                    </div>
                  </div>

                  <div class="grid grid-cols-1 md:grid-cols-3 gap-4 lg:gap-5 pt-2">
                    <div class="space-y-2">
                      <label
                        :class="[
                          'flex items-center gap-2 text-xs uppercase tracking-widest font-semibold',
                          themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500'
                        ]"
                      >
                        Fuso horário
                        <span
                          :class="[
                            'text-[10px] font-bold tracking-normal normal-case px-1.5 py-0.5 rounded-md border',
                            themeStore.isDark
                              ? 'bg-zinc-800/70 border-zinc-700/60 text-zinc-400'
                              : 'bg-zinc-200 border-zinc-300 text-zinc-600'
                          ]"
                        >🔒 Sem backend</span>
                      </label>
                      <select
                        disabled
                        :class="[
                          'w-full rounded-xl px-3.5 py-3 text-sm cursor-not-allowed border',
                          themeStore.isDark
                            ? 'bg-zinc-900/40 border-zinc-700/50 text-zinc-500'
                            : 'bg-zinc-100 border-zinc-200 text-zinc-500'
                        ]"
                      >
                        <option>America/Sao_Paulo (UTC-3)</option>
                      </select>
                    </div>
                    <div class="space-y-2">
                      <label
                        :class="[
                          'flex items-center gap-2 text-xs uppercase tracking-widest font-semibold',
                          themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500'
                        ]"
                      >
                        Moeda
                        <span
                          :class="[
                            'text-[10px] font-bold tracking-normal normal-case px-1.5 py-0.5 rounded-md border',
                            themeStore.isDark
                              ? 'bg-zinc-800/70 border-zinc-700/60 text-zinc-400'
                              : 'bg-zinc-200 border-zinc-300 text-zinc-600'
                          ]"
                        >🔒 Sem backend</span>
                      </label>
                      <select
                        disabled
                        :class="[
                          'w-full rounded-xl px-3.5 py-3 text-sm cursor-not-allowed border',
                          themeStore.isDark
                            ? 'bg-zinc-900/40 border-zinc-700/50 text-zinc-500'
                            : 'bg-zinc-100 border-zinc-200 text-zinc-500'
                        ]"
                      >
                        <option>BRL — Real (R$)</option>
                      </select>
                    </div>
                    <div class="space-y-2">
                      <label
                        :class="[
                          'flex items-center gap-2 text-xs uppercase tracking-widest font-semibold',
                          themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500'
                        ]"
                      >
                        Idioma
                        <span
                          :class="[
                            'text-[10px] font-bold tracking-normal normal-case px-1.5 py-0.5 rounded-md border',
                            themeStore.isDark
                              ? 'bg-zinc-800/70 border-zinc-700/60 text-zinc-400'
                              : 'bg-zinc-200 border-zinc-300 text-zinc-600'
                          ]"
                        >🔒 Sem backend</span>
                      </label>
                      <select
                        disabled
                        :class="[
                          'w-full rounded-xl px-3.5 py-3 text-sm cursor-not-allowed border',
                          themeStore.isDark
                            ? 'bg-zinc-900/40 border-zinc-700/50 text-zinc-500'
                            : 'bg-zinc-100 border-zinc-200 text-zinc-500'
                        ]"
                      >
                        <option>Português (Brasil)</option>
                      </select>
                    </div>
                  </div>

                  <div
                    :class="[
                      'rounded-xl border-dashed border p-4 sm:p-5 space-y-2',
                      themeStore.isDark
                        ? 'border-zinc-700/80 bg-zinc-900/30'
                        : 'border-zinc-300 bg-zinc-50/60'
                    ]"
                  >
                    <p
                      :class="[
                        'text-[11px] uppercase tracking-widest font-semibold flex items-center gap-1.5',
                        themeStore.isDark ? 'text-zinc-500' : 'text-zinc-500'
                      ]"
                    >
                      <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"></path>
                      </svg>
                      Campos faltantes no backend (discutir)
                    </p>
                    <ul
                      :class="[
                        'list-disc list-inside text-xs space-y-1',
                        themeStore.isDark ? 'text-zinc-400' : 'text-zinc-600'
                      ]"
                    >
                      <li>Endereço completo, telefone e email de contato do restaurante</li>
                      <li>Horário de funcionamento (dias/turnos para pedidos, fechamento semanal)</li>
                      <li>Logo do restaurante (upload de imagem)</li>
                      <li>Taxas padrão (serviço, entrega, embalagem)</li>
                    </ul>
                  </div>

                  <div class="flex flex-col sm:flex-row sm:items-center sm:justify-end gap-3 pt-2">
                    <button
                      disabled
                      :class="[
                        'inline-flex items-center gap-2 rounded-xl px-4 py-2.5 text-sm font-semibold cursor-not-allowed border',
                        themeStore.isDark
                          ? 'text-zinc-500 bg-zinc-800/50 border-zinc-700/60'
                          : 'text-zinc-500 bg-zinc-200/60 border-zinc-300'
                      ]"
                      title="Edições do perfil do restaurante ainda não implementadas no backend"
                    >
                      🔒 Salvar alterações
                    </button>
                  </div>
                </div>
              </div>

              <!-- CARD: Marketplace & Entregas (iFood / escalável) -->
              <div
                :class="[
                  'rounded-2xl border shadow-xl overflow-hidden',
                  themeStore.isDark
                    ? 'bg-zinc-900/50 border-zinc-800'
                    : 'bg-white border-zinc-200'
                ]"
              >
                <div
                  :class="[
                    'p-5 sm:p-6 flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4 border-b',
                    themeStore.isDark ? 'border-zinc-800/80' : 'border-zinc-200'
                  ]"
                >
                  <div class="flex items-start gap-3 flex-1">
                    <div
                      :class="[
                        'w-10 h-10 rounded-xl flex items-center justify-center flex-shrink-0 border',
                        themeStore.isDark
                          ? 'bg-rose-500/15 border-rose-500/30 text-rose-300'
                          : 'bg-rose-50 border-rose-200 text-rose-600'
                      ]"
                    >
                      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 17a2 2 0 11-4 0 2 2 0 014 0zM19 17a2 2 0 11-4 0 2 2 0 014 0z"></path>
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16V6a1 1 0 00-1-1H4a1 1 0 00-1 1v10a1 1 0 001 1h1m8-1a1 1 0 01-1 1H9m4-1V8h4l3 3v5h-2m-6 0h2"></path>
                      </svg>
                    </div>
                    <div class="space-y-0.5 flex-1">
                      <h2
                        :class="[
                          'text-base sm:text-lg font-semibold tracking-tight',
                          themeStore.isDark ? 'text-white' : 'text-zinc-900'
                        ]"
                      >Marketplace & Entregas</h2>
                      <p
                        :class="[
                          'text-xs sm:text-sm leading-relaxed max-w-2xl',
                          themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500'
                        ]"
                      >
                        Integrações com plataformas externas e configuração de entrega.
                        <strong>iFood</strong> virá primeiro, mas a estrutura já está pensada para crescer (Rappi, Uber Eats, entrega própria).
                      </p>
                    </div>
                    <span
                      :class="[
                        'text-[10px] font-bold tracking-wide uppercase px-2.5 py-1 rounded-md border shrink-0 self-start sm:self-auto',
                        themeStore.isDark
                          ? 'bg-rose-500/10 border-rose-500/20 text-rose-300'
                          : 'bg-rose-50 border-rose-200 text-rose-700'
                      ]"
                    >Em breve · escalável</span>
                  </div>
                </div>

                <div class="p-5 sm:p-6 space-y-4 sm:space-y-5">
                  <div
                    :class="[
                      'grid gap-4',
                      'grid-cols-1 lg:grid-cols-3'
                    ]"
                  >
                    <div
                      :class="[
                        'rounded-xl border p-4 space-y-3 lg:col-span-1 transition-colors',
                        themeStore.isDark
                          ? 'border-zinc-800 bg-zinc-900/60'
                          : 'border-zinc-200 bg-zinc-50'
                      ]"
                    >
                      <div class="flex items-center justify-between gap-2">
                        <div class="flex items-center gap-2.5">
                          <div
                            class="w-9 h-9 rounded-lg flex items-center justify-center shrink-0 text-white font-black text-sm shadow-md"
                            style="background: linear-gradient(135deg, #e53e3e 0%, #dc2626 100%);"
                          >
                            iF
                          </div>
                          <div>
                            <p
                              :class="[
                                'text-sm font-bold',
                                themeStore.isDark ? 'text-white' : 'text-zinc-900'
                              ]"
                            >iFood</p>
                            <p
                              :class="[
                                'text-[10px] uppercase tracking-wider font-semibold',
                                themeStore.isDark ? 'text-rose-300' : 'text-rose-600'
                              ]"
                            >Prioridade 1</p>
                          </div>
                        </div>
                        <span
                          :class="[
                            'text-[10px] font-bold uppercase tracking-widest px-2 py-0.5 rounded-full border',
                            themeStore.isDark
                              ? 'bg-zinc-800/70 border-zinc-700/60 text-zinc-400'
                              : 'bg-zinc-200 border-zinc-300 text-zinc-600'
                          ]"
                        >Aguardando credenciais</span>
                      </div>
                      <div class="space-y-2 pt-1">
                        <label
                          :class="[
                            'text-[10px] uppercase tracking-widest font-semibold',
                            themeStore.isDark ? 'text-zinc-500' : 'text-zinc-500'
                          ]"
                        >Client ID / Merchant ID</label>
                        <input
                          disabled
                          placeholder="•••••••••••• (aguardando integração)"
                          :class="[
                            'w-full rounded-lg px-3 py-2 text-xs border font-mono cursor-not-allowed',
                            themeStore.isDark
                              ? 'bg-zinc-900/40 border-zinc-700/50 text-zinc-500 placeholder:text-zinc-600'
                              : 'bg-white border-zinc-200 text-zinc-500 placeholder:text-zinc-400'
                          ]"
                        />
                      </div>
                      <div class="space-y-2">
                        <label
                          :class="[
                            'text-[10px] uppercase tracking-widest font-semibold',
                            themeStore.isDark ? 'text-zinc-500' : 'text-zinc-500'
                          ]"
                        >Sincronização</label>
                        <div
                          :class="[
                            'rounded-lg border-dashed border p-3 space-y-2',
                            themeStore.isDark
                              ? 'border-zinc-700/70 bg-zinc-900/30'
                              : 'border-zinc-200 bg-white/50'
                          ]"
                        >
                          <div
                            :class="[
                              'flex items-center justify-between text-xs',
                              themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500'
                            ]"
                          >
                            <span>Puxar pedidos automaticamente</span>
                            <span>🔒</span>
                          </div>
                          <div
                            :class="[
                              'flex items-center justify-between text-xs',
                              themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500'
                            ]"
                          >
                            <span>Enviar cardápio sincronizado</span>
                            <span>🔒</span>
                          </div>
                          <div
                            :class="[
                              'flex items-center justify-between text-xs',
                              themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500'
                            ]"
                          >
                            <span>Webhook status entrega</span>
                            <span>🔒</span>
                          </div>
                        </div>
                      </div>
                    </div>

                    <div
                      :class="[
                        'rounded-xl border p-4 space-y-3 opacity-70 lg:col-span-1',
                        themeStore.isDark
                          ? 'border-zinc-800 bg-zinc-900/30'
                          : 'border-zinc-200 bg-zinc-50/60'
                      ]"
                    >
                      <div class="flex items-center gap-2.5">
                        <div
                          class="w-9 h-9 rounded-lg flex items-center justify-center shrink-0 font-black text-sm text-white shadow-md"
                          style="background: linear-gradient(135deg, #fb923c 0%, #f97316 100%);"
                        >
                          RE
                        </div>
                        <div>
                          <p
                            :class="[
                              'text-sm font-bold',
                              themeStore.isDark ? 'text-white' : 'text-zinc-900'
                            ]"
                          >Rappi / AiQfome</p>
                          <p
                            :class="[
                              'text-[10px] uppercase tracking-wider font-semibold',
                              themeStore.isDark ? 'text-zinc-500' : 'text-zinc-500'
                            ]"
                          >Roadmap</p>
                        </div>
                      </div>
                      <p
                        :class="[
                          'text-xs leading-relaxed',
                          themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500'
                        ]"
                      >
                        Modelo de integração será o mesmo do iFood —
                        reaproveitamos interface e camada de serviço.
                      </p>
                    </div>

                    <div
                      :class="[
                        'rounded-xl border p-4 space-y-3 opacity-70 lg:col-span-1',
                        themeStore.isDark
                          ? 'border-zinc-800 bg-zinc-900/30'
                          : 'border-zinc-200 bg-zinc-50/60'
                      ]"
                    >
                      <div class="flex items-center gap-2.5">
                        <div
                          class="w-9 h-9 rounded-lg flex items-center justify-center shrink-0 font-black text-sm text-white shadow-md"
                          style="background: linear-gradient(135deg, #22c55e 0%, #16a34a 100%);"
                        >
                          EP
                        </div>
                        <div>
                          <p
                            :class="[
                              'text-sm font-bold',
                              themeStore.isDark ? 'text-white' : 'text-zinc-900'
                            ]"
                          >Entrega própria</p>
                          <p
                            :class="[
                              'text-[10px] uppercase tracking-wider font-semibold',
                              themeStore.isDark ? 'text-zinc-500' : 'text-zinc-500'
                            ]"
                          >Configuração de taxas</p>
                        </div>
                      </div>
                      <div class="space-y-1.5 text-xs">
                        <div
                          :class="[
                            'flex items-center justify-between',
                            themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500'
                          ]"
                        >
                          <span>Taxa entrega padrão</span>
                          <span class="font-mono">R$ 0,00 🔒</span>
                        </div>
                        <div
                          :class="[
                            'flex items-center justify-between',
                            themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500'
                          ]"
                        >
                          <span>Taxa embalagem</span>
                          <span class="font-mono">R$ 0,00 🔒</span>
                        </div>
                        <div
                          :class="[
                            'flex items-center justify-between',
                            themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500'
                          ]"
                        >
                          <span>Tempo médio entrega</span>
                          <span>-- min 🔒</span>
                        </div>
                        <div
                          :class="[
                            'flex items-center justify-between',
                            themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500'
                          ]"
                        >
                          <span>Taxa serviço (10%)</span>
                          <span>-- % 🔒</span>
                        </div>
                      </div>
                    </div>
                  </div>

                  <div
                    :class="[
                      'rounded-xl border p-4 space-y-2 text-xs',
                      themeStore.isDark
                        ? 'border-indigo-500/20 bg-indigo-500/5'
                        : 'border-indigo-200 bg-indigo-50/60'
                    ]"
                  >
                    <p
                      :class="[
                        'text-[11px] uppercase tracking-widest font-semibold flex items-center gap-1.5',
                        themeStore.isDark ? 'text-indigo-300' : 'text-indigo-700'
                      ]"
                    >
                      <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path>
                      </svg>
                      Estratégia de escalabilidade (back-end)
                    </p>
                    <p
                      :class="[
                        'leading-relaxed',
                        themeStore.isDark ? 'text-zinc-300' : 'text-zinc-600'
                      ]"
                    >
                      Pensando em evitar retrabalho: vamos criar uma tabela genérica <code class="font-mono px-1.5 py-0.5 rounded border bg-black/20 border-zinc-700/60">restaurant_marketplace_integrations</code>
                      (provider varchar + JSON credentials + enabled + webhook secret). Assim iFood, Rappi e futuros provedores
                      cabem no mesmo schema sem precisar de migration a cada novo parceiro. Entrega própria fica em tabela separada
                      <code class="font-mono px-1.5 py-0.5 rounded border bg-black/20 border-zinc-700/60">restaurant_delivery_settings</code> (FK para restaurant).
                    </p>
                  </div>
                </div>
              </div>
            </div>

            <aside class="space-y-4 xl:space-y-5 xl:sticky xl:top-24 self-start">
              <div
                :class="[
                  'rounded-2xl border p-5 space-y-3 shadow-xl',
                  themeStore.isDark
                    ? 'bg-zinc-900/50 border-zinc-800'
                    : 'bg-white border-zinc-200'
                ]"
              >
                <div
                  :class="[
                    'flex items-center gap-2 text-xs uppercase tracking-[0.18em] font-semibold',
                    themeStore.isDark ? 'text-zinc-500' : 'text-zinc-500'
                  ]"
                >
                  <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path>
                  </svg>
                  O que já temos vs o que falta
                </div>
                <ul
                  :class="[
                    'space-y-2.5 text-xs',
                    themeStore.isDark ? 'text-zinc-300' : 'text-zinc-600'
                  ]"
                >
                  <li class="flex items-start gap-2">
                    <span
                      :class="[
                        'mt-0.5 shrink-0 font-bold',
                        themeStore.isDark ? 'text-emerald-300' : 'text-emerald-600'
                      ]"
                    >✓</span>
                    <span><strong>Nome do restaurante</strong> — disponível via token do usuário.</span>
                  </li>
                  <li class="flex items-start gap-2">
                    <span
                      :class="[
                        'mt-0.5 shrink-0 font-bold',
                        themeStore.isDark ? 'text-amber-300' : 'text-amber-600'
                      ]"
                    >◯</span>
                    <span><strong>CNPJ</strong> — campo existe na model mas não tem endpoint GET/PUT público.</span>
                  </li>
                  <li class="flex items-start gap-2">
                    <span
                      :class="[
                        'mt-0.5 shrink-0 font-bold',
                        themeStore.isDark ? 'text-rose-300' : 'text-rose-600'
                      ]"
                    >✕</span>
                    <span><strong>Fuso / Moeda / Idioma</strong> — campos não existem na model Restaurant.</span>
                  </li>
                  <li class="flex items-start gap-2">
                    <span
                      :class="[
                        'mt-0.5 shrink-0 font-bold',
                        themeStore.isDark ? 'text-amber-300' : 'text-amber-600'
                      ]"
                    >◯</span>
                    <span><strong>iFood</strong> — UI mockada + plano de migration genérica para múltiplos providers.</span>
                  </li>
                </ul>
              </div>
            </aside>
          </div>
        </section>

        <!-- Seção Aparência -->
        <section v-else-if="activeSectionId === 'appearance'" class="space-y-6">
          <div class="grid grid-cols-1 xl:grid-cols-3 gap-6">
            <div class="xl:col-span-2 space-y-6">
              <div
                :class="[
                  'rounded-2xl border shadow-xl overflow-hidden',
                  themeStore.isDark
                    ? 'bg-zinc-900/50 border-zinc-800'
                    : 'bg-white border-zinc-200'
                ]"
              >
                <div
                  :class="[
                    'p-5 sm:p-6 flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4 border-b',
                    themeStore.isDark ? 'border-zinc-800/80' : 'border-zinc-200'
                  ]"
                >
                  <div class="flex items-start gap-3 flex-1">
                    <div
                      :class="[
                        'w-10 h-10 rounded-xl flex items-center justify-center flex-shrink-0 border',
                        themeStore.isDark
                          ? 'bg-indigo-500/15 border-indigo-500/30 text-indigo-300'
                          : 'bg-indigo-50 border-indigo-200 text-indigo-600'
                      ]"
                    >
                      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 21a4 4 0 01-4-4V5a2 2 0 012-2h4a2 2 0 012 2v12a4 4 0 01-4 4zm0 0h12a2 2 0 002-2v-4a2 2 0 00-2-2h-2.343M11 7.343l1.657-1.657a2 2 0 012.828 0l2.829 2.829a2 2 0 010 2.828l-8.486 8.485M7 17h.01"></path>
                      </svg>
                    </div>
                    <div class="space-y-0.5 flex-1">
                      <h2
                        :class="[
                          'text-base sm:text-lg font-semibold tracking-tight',
                          themeStore.isDark ? 'text-white' : 'text-zinc-900'
                        ]"
                      >Tema da interface</h2>
                      <p
                        :class="[
                          'text-xs sm:text-sm leading-relaxed max-w-2xl',
                          themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500'
                        ]"
                      >
                        Escolha entre modo escuro (padrão, recomendado para ambientes internos) ou modo claro
                        (para ambientes externos ou preferência pessoal). A preferência é salva apenas neste navegador.
                      </p>
                    </div>
                  </div>
                  <button
                    @click="themeStore.toggle()"
                    :class="[
                      'inline-flex items-center gap-2 rounded-xl px-3.5 py-2 text-xs font-semibold uppercase tracking-wider border transition-all shrink-0 self-start sm:self-auto',
                      themeStore.isDark
                        ? 'bg-indigo-500/10 text-indigo-300 border-indigo-500/30 hover:bg-indigo-500/15'
                        : 'bg-indigo-50 text-indigo-700 border-indigo-200 hover:bg-indigo-100'
                    ]"
                  >
                    <svg v-if="themeStore.isDark" class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 3v1m0 16v1m9-9h-1M4 12H3m15.364 6.364l-.707-.707M6.343 6.343l-.707-.707m12.728 0l-.707.707M6.343 17.657l-.707.707M16 12a4 4 0 11-8 0 4 4 0 018 0z"></path>
                    </svg>
                    <svg v-else class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z"></path>
                    </svg>
                    {{ themeStore.isDark ? 'Usar modo claro' : 'Usar modo escuro' }}
                  </button>
                </div>
                <div class="p-5 sm:p-6">
                  <div class="grid grid-cols-1 sm:grid-cols-2 gap-4 lg:gap-5">
                    <!-- Card: Modo Escuro -->
                    <button
                      type="button"
                      @click="themeStore.set('dark')"
                      :class="[
                        'group relative rounded-2xl border p-4 sm:p-5 text-left transition-all',
                        themeStore.isDark
                          ? 'border-indigo-500/50 ring-2 ring-indigo-500/30 shadow-xl shadow-indigo-950/30'
                          : 'border-zinc-800/60 hover:border-zinc-700/70'
                      ]"
                      style="background: linear-gradient(135deg, #09090b 0%, #18181b 60%, #27272a 100%);"
                    >
                      <div class="flex items-start justify-between gap-3 mb-4">
                        <div
                          :class="[
                            'w-11 h-11 rounded-xl flex items-center justify-center shrink-0 border',
                            themeStore.isDark
                              ? 'bg-indigo-500/20 border-indigo-500/30 text-indigo-300'
                              : 'bg-zinc-800/60 border-zinc-700/60 text-zinc-400'
                          ]"
                        >
                          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z"></path>
                          </svg>
                        </div>
                        <span
                          v-if="themeStore.isDark"
                          class="inline-flex items-center gap-1 text-[10px] font-bold uppercase tracking-widest px-2 py-0.5 rounded-full bg-emerald-500/15 border border-emerald-500/30 text-emerald-300 shrink-0"
                        >
                          <span class="w-1.5 h-1.5 rounded-full bg-emerald-400"></span>
                          Ativo
                        </span>
                      </div>
                      <h3 class="text-sm font-bold text-white mb-1">Modo escuro</h3>
                      <p class="text-xs text-zinc-400 leading-relaxed">
                        Tema padrão do ISM. Reduz cansaço visual em ambientes fechados e destaca a paleta premium Zinc + Índigo.
                      </p>
                      <div class="mt-4 rounded-xl bg-zinc-950/60 border border-zinc-800/70 p-3 space-y-2">
                        <div class="h-2 w-16 rounded bg-indigo-500/50"></div>
                        <div class="h-2 w-full rounded bg-zinc-800/80"></div>
                        <div class="h-2 w-3/4 rounded bg-zinc-800/60"></div>
                      </div>
                    </button>

                    <!-- Card: Modo Claro -->
                    <button
                      type="button"
                      @click="themeStore.set('light')"
                      :class="[
                        'group relative rounded-2xl border p-4 sm:p-5 text-left transition-all',
                        !themeStore.isDark
                          ? 'border-indigo-300/80 ring-2 ring-indigo-200/60 shadow-xl shadow-indigo-100/40'
                          : 'border-zinc-200 hover:border-zinc-300/70'
                      ]"
                      style="background: linear-gradient(135deg, #ffffff 0%, #fafafa 55%, #f4f4f5 100%);"
                    >
                      <div class="flex items-start justify-between gap-3 mb-4">
                        <div
                          :class="[
                            'w-11 h-11 rounded-xl flex items-center justify-center shrink-0 border',
                            !themeStore.isDark
                              ? 'bg-indigo-100 border-indigo-200 text-indigo-600'
                              : 'bg-zinc-100 border-zinc-200 text-zinc-400'
                          ]"
                        >
                          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 3v1m0 16v1m9-9h-1M4 12H3m15.364 6.364l-.707-.707M6.343 6.343l-.707-.707m12.728 0l-.707.707M6.343 17.657l-.707.707M16 12a4 4 0 11-8 0 4 4 0 018 0z"></path>
                          </svg>
                        </div>
                        <span
                          v-if="!themeStore.isDark"
                          class="inline-flex items-center gap-1 text-[10px] font-bold uppercase tracking-widest px-2 py-0.5 rounded-full bg-emerald-50 border border-emerald-200 text-emerald-700 shrink-0"
                        >
                          <span class="w-1.5 h-1.5 rounded-full bg-emerald-500"></span>
                          Ativo
                        </span>
                      </div>
                      <h3 class="text-sm font-bold text-zinc-900 mb-1">Modo claro</h3>
                      <p class="text-xs text-zinc-500 leading-relaxed">
                        Ideal para ambientes externos, telas com brilho ou preferência pessoal. Alto contraste e leitura limpa.
                      </p>
                      <div class="mt-4 rounded-xl bg-white/70 border border-zinc-200 p-3 space-y-2">
                        <div class="h-2 w-16 rounded bg-indigo-400"></div>
                        <div class="h-2 w-full rounded bg-zinc-200"></div>
                        <div class="h-2 w-3/4 rounded bg-zinc-100"></div>
                      </div>
                    </button>
                  </div>

                  <div
                    :class="[
                      'mt-5 rounded-xl border-dashed border p-4 space-y-1.5',
                      themeStore.isDark
                        ? 'border-zinc-700/80 bg-zinc-900/30'
                        : 'border-zinc-300 bg-zinc-50/60'
                    ]"
                  >
                    <p
                      :class="[
                        'text-[11px] uppercase tracking-widest font-semibold',
                        themeStore.isDark ? 'text-zinc-500' : 'text-zinc-500'
                      ]"
                    >
                      Próximas preferências visuais (escalabilidade)
                    </p>
                    <ul
                      :class="[
                        'list-disc list-inside text-xs space-y-1',
                        themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500'
                      ]"
                    >
                      <li>Densidade de interface (Confortável / Compacta) para grids do financeiro e estoque</li>
                      <li>Redução de movimento — desativa animações em dispositivos mais lentos</li>
                      <li>Escala de fonte global (90% / 100% / 115%)</li>
                      <li>Contraste alto WCAG AA/AAA para acessibilidade</li>
                    </ul>
                  </div>
                </div>
              </div>
            </div>

            <aside class="space-y-4 xl:space-y-5 xl:sticky xl:top-24 self-start">
              <div
                :class="[
                  'rounded-2xl border p-5 space-y-3 shadow-xl',
                  themeStore.isDark
                    ? 'bg-zinc-900/50 border-zinc-800'
                    : 'bg-white border-zinc-200'
                ]"
              >
                <div
                  :class="[
                    'flex items-center gap-2 text-xs uppercase tracking-[0.18em] font-semibold',
                    themeStore.isDark ? 'text-zinc-500' : 'text-zinc-500'
                  ]"
                >
                  <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path>
                  </svg>
                  Como o tema é aplicado
                </div>
                <ul
                  :class="[
                    'space-y-2.5 text-xs',
                    themeStore.isDark ? 'text-zinc-300' : 'text-zinc-600'
                  ]"
                >
                  <li class="flex items-start gap-2">
                    <span :class="themeStore.isDark ? 'text-emerald-300' : 'text-emerald-600'" class="mt-0.5 shrink-0">✓</span>
                    <span><strong>Demonstração funcional</strong> — 6 telas principais já trocam cor de fundo, texto e bordas.</span>
                  </li>
                  <li class="flex items-start gap-2">
                    <span :class="themeStore.isDark ? 'text-emerald-300' : 'text-emerald-600'" class="mt-0.5 shrink-0">✓</span>
                    <span><strong>Persistência local</strong> — salva em <code class="font-mono px-1.5 py-0.5 rounded border bg-zinc-800/50 border-zinc-700/50">localStorage</code> e re-aplica ao abrir.</span>
                  </li>
                  <li class="flex items-start gap-2">
                    <span :class="themeStore.isDark ? 'text-amber-300' : 'text-amber-600'" class="mt-0.5 shrink-0">◯</span>
                    <span><strong>Telas restantes</strong> — dashboards financeiro/estoque/cardápio herdam tema em containers-base mas ainda podem precisar de polimento individual.</span>
                  </li>
                </ul>
              </div>

              <div
                :class="[
                  'rounded-2xl border p-5 space-y-3',
                  themeStore.isDark
                    ? 'border-indigo-500/20 bg-indigo-500/5'
                    : 'border-indigo-200 bg-indigo-50/60'
                ]"
              >
                <p
                  :class="[
                    'text-[11px] uppercase tracking-widest font-semibold flex items-center gap-1.5',
                    themeStore.isDark ? 'text-indigo-300' : 'text-indigo-700'
                  ]"
                >
                  <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 3v4M3 5h4M6 17v4m-2-2h4m5-16l2.286 6.857L21 12l-5.714 2.143L13 21l-2.286-6.857L5 12l5.714-2.143L13 3z"></path>
                  </svg>
                  Atalho
                </p>
                <p :class="['text-xs leading-relaxed', themeStore.isDark ? 'text-zinc-300' : 'text-zinc-600']">
                  Você também pode alternar rapidamente usando o botão <strong>lua / sol</strong> no topo da <em>sidebar</em> lateral esquerda — economiza alguns cliques.
                </p>
              </div>
            </aside>
          </div>
        </section>

        <!-- Seção Notificações -->
        <section v-else-if="activeSectionId === 'notifications'" class="space-y-6">
          <div class="grid grid-cols-1 xl:grid-cols-3 gap-6">
            <div class="xl:col-span-2 space-y-6">

              <!-- Card A: Canais de notificação -->
              <div :class="['rounded-2xl border shadow-xl overflow-hidden', themeStore.isDark ? 'bg-zinc-900/50 border-zinc-800' : 'bg-white border-zinc-200 shadow-zinc-900/5']">
                <div :class="['p-5 sm:p-6 flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4 border-b', themeStore.isDark ? 'border-zinc-800/80' : 'border-zinc-200/80']">
                  <div class="flex items-start gap-3 flex-1">
                    <div :class="['w-10 h-10 rounded-xl flex items-center justify-center flex-shrink-0', themeStore.isDark ? 'bg-zinc-800/60 border border-zinc-700/60 text-zinc-300' : 'bg-zinc-100 border border-zinc-200 text-zinc-600']">
                      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"></path>
                      </svg>
                    </div>
                    <div class="space-y-0.5 flex-1">
                      <h2 :class="['text-base sm:text-lg font-semibold tracking-tight', themeStore.isDark ? 'text-white' : 'text-zinc-900']">Canais de notificação</h2>
                      <p :class="['text-xs sm:text-sm leading-relaxed max-w-2xl', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500']">Escolha onde receber os alertas do sistema. Cada tipo de evento abaixo pode ser ligado/desligado por canal individualmente.</p>
                    </div>
                  </div>
                </div>
                <div class="p-5 sm:p-6 space-y-4 sm:space-y-5">
                  <!-- Email -->
                  <div :class="['flex items-start sm:items-center justify-between gap-4 p-4 rounded-xl border', themeStore.isDark ? 'bg-zinc-900/70 border-zinc-800/80' : 'bg-zinc-50 border-zinc-200/80']">
                    <div class="flex items-start gap-3">
                      <div :class="['w-10 h-10 rounded-lg flex items-center justify-center shrink-0 border', themeStore.isDark ? 'bg-indigo-500/10 border-indigo-500/20 text-indigo-300' : 'bg-indigo-50 border-indigo-100 text-indigo-600']">
                        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"></path>
                        </svg>
                      </div>
                      <div class="space-y-0.5 min-w-0">
                        <p :class="['text-sm font-semibold', themeStore.isDark ? 'text-white' : 'text-zinc-900']">Email</p>
                        <p :class="['text-xs mt-0.5', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500']">Faturamento, relatórios semanais e alertas de segurança.</p>
                      </div>
                    </div>
                    <span :class="['text-[10px] font-bold tracking-wide uppercase px-2 py-1 rounded-md shrink-0 border', themeStore.isDark ? 'bg-zinc-800/70 border-zinc-700/60 text-zinc-400' : 'bg-zinc-100 border-zinc-200 text-zinc-500']">🔒 Sem backend</span>
                  </div>

                  <!-- Push navegador -->
                  <div :class="['flex items-start sm:items-center justify-between gap-4 p-4 rounded-xl border', themeStore.isDark ? 'bg-zinc-900/70 border-zinc-800/80' : 'bg-zinc-50 border-zinc-200/80']">
                    <div class="flex items-start gap-3">
                      <div :class="['w-10 h-10 rounded-lg flex items-center justify-center shrink-0 border', themeStore.isDark ? 'bg-zinc-800/60 border-zinc-700/60 text-zinc-300' : 'bg-zinc-100 border-zinc-200 text-zinc-600']">
                        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 18h.01M8 21h8a2 2 0 002-2V5a2 2 0 00-2-2H8a2 2 0 00-2 2v14a2 2 0 002 2z"></path>
                        </svg>
                      </div>
                      <div class="space-y-0.5 min-w-0">
                        <p :class="['text-sm font-semibold', themeStore.isDark ? 'text-white' : 'text-zinc-900']">Push no navegador</p>
                        <p :class="['text-xs mt-0.5', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500']">Alertas instantâneos — novos pedidos, estoque, status em tempo real.</p>
                      </div>
                    </div>
                    <span :class="['text-[10px] font-bold tracking-wide uppercase px-2 py-1 rounded-md shrink-0 border', themeStore.isDark ? 'bg-zinc-800/70 border-zinc-700/60 text-zinc-400' : 'bg-zinc-100 border-zinc-200 text-zinc-500']">🔒 Sem backend</span>
                  </div>

                  <!-- WhatsApp -->
                  <div :class="['flex items-start sm:items-center justify-between gap-4 p-4 rounded-xl border', themeStore.isDark ? 'bg-zinc-900/70 border-zinc-800/80' : 'bg-zinc-50 border-zinc-200/80']">
                    <div class="flex items-start gap-3">
                      <div :class="['w-10 h-10 rounded-lg flex items-center justify-center shrink-0 border', themeStore.isDark ? 'bg-emerald-500/10 border-emerald-500/20 text-emerald-300' : 'bg-emerald-50 border-emerald-100 text-emerald-600']">
                        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z"></path>
                        </svg>
                      </div>
                      <div class="space-y-0.5 min-w-0">
                        <div class="flex items-center gap-2 flex-wrap">
                          <p :class="['text-sm font-semibold', themeStore.isDark ? 'text-white' : 'text-zinc-900']">WhatsApp (opcional)</p>
                          <span :class="['text-[9px] font-bold uppercase px-1.5 py-0.5 rounded border', themeStore.isDark ? 'bg-amber-500/10 border-amber-500/20 text-amber-300' : 'bg-amber-50 border-amber-200 text-amber-700']">Plano pago</span>
                        </div>
                        <p :class="['text-xs mt-0.5', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500']">Alertas urgentes via WhatsApp Business — pedidos e incidentes críticos.</p>
                      </div>
                    </div>
                    <span :class="['text-[10px] font-bold tracking-wide uppercase px-2 py-1 rounded-md shrink-0 border', themeStore.isDark ? 'bg-zinc-800/70 border-zinc-700/60 text-zinc-400' : 'bg-zinc-100 border-zinc-200 text-zinc-500']">🔒 Sem backend</span>
                  </div>
                </div>
              </div>

              <!-- Card B: Tabela Preferências por tipo de evento -->
              <div :class="['rounded-2xl border shadow-xl overflow-hidden', themeStore.isDark ? 'bg-zinc-900/50 border-zinc-800' : 'bg-white border-zinc-200 shadow-zinc-900/5']">
                <div :class="['p-5 sm:p-6 flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4 border-b', themeStore.isDark ? 'border-zinc-800/80' : 'border-zinc-200/80']">
                  <div class="flex items-start gap-3 flex-1">
                    <div :class="['w-10 h-10 rounded-xl flex items-center justify-center flex-shrink-0', themeStore.isDark ? 'bg-zinc-800/60 border border-zinc-700/60 text-zinc-300' : 'bg-zinc-100 border border-zinc-200 text-zinc-600']">
                      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4"></path>
                      </svg>
                    </div>
                    <div class="space-y-0.5 flex-1">
                      <h2 :class="['text-base sm:text-lg font-semibold tracking-tight', themeStore.isDark ? 'text-white' : 'text-zinc-900']">Preferências por tipo de evento</h2>
                      <p :class="['text-xs sm:text-sm leading-relaxed max-w-2xl', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500']">Para cada evento do sistema, escolha <strong>individualmente</strong> em qual canal quer receber. Linhas bloqueadas por cargo aparecem acinzentadas.</p>
                    </div>
                  </div>
                </div>

                <!-- Cabeçalho da tabela (desktop) -->
                <div :class="['hidden md:grid grid-cols-12 gap-3 px-5 sm:px-6 py-3.5 text-[11px] font-bold uppercase tracking-[0.14em] border-b', themeStore.isDark ? 'bg-zinc-900/70 border-zinc-800/80 text-zinc-500' : 'bg-zinc-50 border-zinc-200/80 text-zinc-500']">
                  <div class="col-span-4">Evento</div>
                  <div class="col-span-5">Descrição</div>
                  <div class="col-span-1 text-center">Email</div>
                  <div class="col-span-2 text-center">Push navegador</div>
                </div>

                <!-- Linhas tabela -->
                <div class="divide-y" :class="themeStore.isDark ? 'divide-zinc-800/60' : 'divide-zinc-200/60'">

                  <!-- Linha 1: Novos pedidos -->
                  <div :class="['px-5 sm:px-6 py-4 grid grid-cols-1 md:grid-cols-12 gap-3 md:gap-3 items-start md:items-center transition-colors', themeStore.isDark ? 'hover:bg-zinc-900/50' : 'hover:bg-zinc-50']">
                    <div class="md:col-span-4 flex items-start gap-2.5">
                      <div :class="['w-8 h-8 rounded-lg flex items-center justify-center shrink-0 mt-0.5 md:mt-0 border', themeStore.isDark ? 'bg-indigo-500/10 border-indigo-500/20 text-indigo-300' : 'bg-indigo-50 border-indigo-100 text-indigo-600']">
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2"></path>
                        </svg>
                      </div>
                      <div class="md:hidden block w-full">
                        <p :class="['text-sm font-semibold', themeStore.isDark ? 'text-white' : 'text-zinc-900']">Novos pedidos</p>
                        <p :class="['text-xs mt-1', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500']">Quando chegar um pedido novo ou vindo do iFood/Rappi.</p>
                        <div class="mt-3 grid grid-cols-2 gap-3">
                          <button type="button" :class="['w-full flex items-center justify-center gap-2 px-3 py-2 rounded-lg border text-[11px] font-bold uppercase tracking-wider', themeStore.isDark ? 'bg-zinc-800/70 border-zinc-700/60 text-zinc-400' : 'bg-zinc-100 border-zinc-200 text-zinc-500']">🔒 Email</button>
                          <button type="button" :class="['w-full flex items-center justify-center gap-2 px-3 py-2 rounded-lg border text-[11px] font-bold uppercase tracking-wider', themeStore.isDark ? 'bg-zinc-800/70 border-zinc-700/60 text-zinc-400' : 'bg-zinc-100 border-zinc-200 text-zinc-500']">🔒 Push</button>
                        </div>
                      </div>
                      <div class="hidden md:block">
                        <p :class="['text-sm font-semibold', themeStore.isDark ? 'text-white' : 'text-zinc-900']">Novos pedidos</p>
                      </div>
                    </div>
                    <div class="md:col-span-5 md:block">
                      <p :class="['text-xs md:text-sm leading-relaxed', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500']">Pedido novo criado manualmente ou sincronizado de marketplaces (iFood, Rappi).</p>
                    </div>
                    <!-- Toggle Email (desktop) -->
                    <div class="md:col-span-1 hidden md:flex justify-center">
                      <button type="button" :class="['w-full h-9 rounded-lg border flex items-center justify-center text-[10px] font-bold uppercase tracking-wider', themeStore.isDark ? 'bg-zinc-800/70 border-zinc-700/60 text-zinc-400 hover:bg-zinc-800' : 'bg-zinc-100 border-zinc-200 text-zinc-500 hover:bg-zinc-200']" title="Bloqueado — sem backend">
                        🔒
                      </button>
                    </div>
                    <!-- Toggle Push (desktop) -->
                    <div class="md:col-span-2 hidden md:flex justify-center">
                      <button type="button" :class="['w-full h-9 rounded-lg border flex items-center justify-center gap-1.5 text-[10px] font-bold uppercase tracking-wider', themeStore.isDark ? 'bg-zinc-800/70 border-zinc-700/60 text-zinc-400 hover:bg-zinc-800' : 'bg-zinc-100 border-zinc-200 text-zinc-500 hover:bg-zinc-200']" title="Bloqueado — sem backend">
                        🔒
                      </button>
                    </div>
                  </div>

                  <!-- Linha 2: Status do pedido -->
                  <div :class="['px-5 sm:px-6 py-4 grid grid-cols-1 md:grid-cols-12 gap-3 md:gap-3 items-start md:items-center transition-colors', themeStore.isDark ? 'hover:bg-zinc-900/50' : 'hover:bg-zinc-50']">
                    <div class="md:col-span-4 flex items-start gap-2.5">
                      <div :class="['w-8 h-8 rounded-lg flex items-center justify-center shrink-0 mt-0.5 md:mt-0 border', themeStore.isDark ? 'bg-violet-500/10 border-violet-500/20 text-violet-300' : 'bg-violet-50 border-violet-100 text-violet-600']">
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"></path>
                        </svg>
                      </div>
                      <div class="md:hidden block w-full">
                        <p :class="['text-sm font-semibold', themeStore.isDark ? 'text-white' : 'text-zinc-900']">Status do pedido</p>
                        <p :class="['text-xs mt-1', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500']">Pedido confirmado, enviado, entregue ou cancelado.</p>
                        <div class="mt-3 grid grid-cols-2 gap-3">
                          <button type="button" :class="['w-full flex items-center justify-center gap-2 px-3 py-2 rounded-lg border text-[11px] font-bold uppercase tracking-wider', themeStore.isDark ? 'bg-zinc-800/70 border-zinc-700/60 text-zinc-400' : 'bg-zinc-100 border-zinc-200 text-zinc-500']">🔒 Email</button>
                          <button type="button" :class="['w-full flex items-center justify-center gap-2 px-3 py-2 rounded-lg border text-[11px] font-bold uppercase tracking-wider', themeStore.isDark ? 'bg-zinc-800/70 border-zinc-700/60 text-zinc-400' : 'bg-zinc-100 border-zinc-200 text-zinc-500']">🔒 Push</button>
                        </div>
                      </div>
                      <div class="hidden md:block">
                        <p :class="['text-sm font-semibold', themeStore.isDark ? 'text-white' : 'text-zinc-900']">Status do pedido</p>
                      </div>
                    </div>
                    <div class="md:col-span-5 md:block">
                      <p :class="['text-xs md:text-sm leading-relaxed', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500']">Confirmação, andamento da entrega, conclusão ou cancelamento de pedidos existentes.</p>
                    </div>
                    <div class="md:col-span-1 hidden md:flex justify-center">
                      <button type="button" :class="['w-full h-9 rounded-lg border flex items-center justify-center text-[10px] font-bold uppercase tracking-wider', themeStore.isDark ? 'bg-zinc-800/70 border-zinc-700/60 text-zinc-400 hover:bg-zinc-800' : 'bg-zinc-100 border-zinc-200 text-zinc-500 hover:bg-zinc-200']">🔒</button>
                    </div>
                    <div class="md:col-span-2 hidden md:flex justify-center">
                      <button type="button" :class="['w-full h-9 rounded-lg border flex items-center justify-center gap-1.5 text-[10px] font-bold uppercase tracking-wider', themeStore.isDark ? 'bg-zinc-800/70 border-zinc-700/60 text-zinc-400 hover:bg-zinc-800' : 'bg-zinc-100 border-zinc-200 text-zinc-500 hover:bg-zinc-200']">🔒</button>
                    </div>
                  </div>

                  <!-- Linha 3: Estoque baixo -->
                  <div :class="['px-5 sm:px-6 py-4 grid grid-cols-1 md:grid-cols-12 gap-3 md:gap-3 items-start md:items-center transition-colors', themeStore.isDark ? 'hover:bg-zinc-900/50' : 'hover:bg-zinc-50']">
                    <div class="md:col-span-4 flex items-start gap-2.5">
                      <div :class="['w-8 h-8 rounded-lg flex items-center justify-center shrink-0 mt-0.5 md:mt-0 border', themeStore.isDark ? 'bg-amber-500/10 border-amber-500/20 text-amber-300' : 'bg-amber-50 border-amber-100 text-amber-600']">
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4"></path>
                        </svg>
                      </div>
                      <div class="md:hidden block w-full">
                        <p :class="['text-sm font-semibold', themeStore.isDark ? 'text-white' : 'text-zinc-900']">Estoque baixo</p>
                        <p :class="['text-xs mt-1', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500']">Ingrediente atingiu ponto de reposição.</p>
                        <div class="mt-3 grid grid-cols-2 gap-3">
                          <button type="button" :class="['w-full flex items-center justify-center gap-2 px-3 py-2 rounded-lg border text-[11px] font-bold uppercase tracking-wider', themeStore.isDark ? 'bg-zinc-800/70 border-zinc-700/60 text-zinc-400' : 'bg-zinc-100 border-zinc-200 text-zinc-500']">🔒 Email</button>
                          <button type="button" :class="['w-full flex items-center justify-center gap-2 px-3 py-2 rounded-lg border text-[11px] font-bold uppercase tracking-wider', themeStore.isDark ? 'bg-zinc-800/70 border-zinc-700/60 text-zinc-400' : 'bg-zinc-100 border-zinc-200 text-zinc-500']">🔒 Push</button>
                        </div>
                      </div>
                      <div class="hidden md:block">
                        <p :class="['text-sm font-semibold', themeStore.isDark ? 'text-white' : 'text-zinc-900']">Estoque baixo</p>
                      </div>
                    </div>
                    <div class="md:col-span-5 md:block">
                      <p :class="['text-xs md:text-sm leading-relaxed', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500']">Quando um ingrediente ou produto atingir o ponto de reposição configurado.</p>
                    </div>
                    <div class="md:col-span-1 hidden md:flex justify-center">
                      <button type="button" :class="['w-full h-9 rounded-lg border flex items-center justify-center text-[10px] font-bold uppercase tracking-wider', themeStore.isDark ? 'bg-zinc-800/70 border-zinc-700/60 text-zinc-400 hover:bg-zinc-800' : 'bg-zinc-100 border-zinc-200 text-zinc-500 hover:bg-zinc-200']">🔒</button>
                    </div>
                    <div class="md:col-span-2 hidden md:flex justify-center">
                      <button type="button" :class="['w-full h-9 rounded-lg border flex items-center justify-center gap-1.5 text-[10px] font-bold uppercase tracking-wider', themeStore.isDark ? 'bg-zinc-800/70 border-zinc-700/60 text-zinc-400 hover:bg-zinc-800' : 'bg-zinc-100 border-zinc-200 text-zinc-500 hover:bg-zinc-200']">🔒</button>
                    </div>
                  </div>

                  <!-- Linha 4: Validade de produto -->
                  <div :class="['px-5 sm:px-6 py-4 grid grid-cols-1 md:grid-cols-12 gap-3 md:gap-3 items-start md:items-center transition-colors', themeStore.isDark ? 'hover:bg-zinc-900/50' : 'hover:bg-zinc-50']">
                    <div class="md:col-span-4 flex items-start gap-2.5">
                      <div :class="['w-8 h-8 rounded-lg flex items-center justify-center shrink-0 mt-0.5 md:mt-0 border', themeStore.isDark ? 'bg-rose-500/10 border-rose-500/20 text-rose-300' : 'bg-rose-50 border-rose-100 text-rose-600']">
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"></path>
                        </svg>
                      </div>
                      <div class="md:hidden block w-full">
                        <p :class="['text-sm font-semibold', themeStore.isDark ? 'text-white' : 'text-zinc-900']">Validade de produto</p>
                        <p :class="['text-xs mt-1', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500']">Produto perto de vencer ou vencido.</p>
                        <div class="mt-3 grid grid-cols-2 gap-3">
                          <button type="button" :class="['w-full flex items-center justify-center gap-2 px-3 py-2 rounded-lg border text-[11px] font-bold uppercase tracking-wider', themeStore.isDark ? 'bg-zinc-800/70 border-zinc-700/60 text-zinc-400' : 'bg-zinc-100 border-zinc-200 text-zinc-500']">🔒 Email</button>
                          <button type="button" :class="['w-full flex items-center justify-center gap-2 px-3 py-2 rounded-lg border text-[11px] font-bold uppercase tracking-wider', themeStore.isDark ? 'bg-zinc-800/70 border-zinc-700/60 text-zinc-400' : 'bg-zinc-100 border-zinc-200 text-zinc-500']">🔒 Push</button>
                        </div>
                      </div>
                      <div class="hidden md:block">
                        <p :class="['text-sm font-semibold', themeStore.isDark ? 'text-white' : 'text-zinc-900']">Validade de produto</p>
                      </div>
                    </div>
                    <div class="md:col-span-5 md:block">
                      <p :class="['text-xs md:text-sm leading-relaxed', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500']">Lote próximo da data de validade (7 dias) ou já vencido — evita perda de estoque.</p>
                    </div>
                    <div class="md:col-span-1 hidden md:flex justify-center">
                      <button type="button" :class="['w-full h-9 rounded-lg border flex items-center justify-center text-[10px] font-bold uppercase tracking-wider', themeStore.isDark ? 'bg-zinc-800/70 border-zinc-700/60 text-zinc-400 hover:bg-zinc-800' : 'bg-zinc-100 border-zinc-200 text-zinc-500 hover:bg-zinc-200']">🔒</button>
                    </div>
                    <div class="md:col-span-2 hidden md:flex justify-center">
                      <button type="button" :class="['w-full h-9 rounded-lg border flex items-center justify-center gap-1.5 text-[10px] font-bold uppercase tracking-wider', themeStore.isDark ? 'bg-zinc-800/70 border-zinc-700/60 text-zinc-400 hover:bg-zinc-800' : 'bg-zinc-100 border-zinc-200 text-zinc-500 hover:bg-zinc-200']">🔒</button>
                    </div>
                  </div>

                  <!-- Linha 5: Faturamento -->
                  <div :class="['px-5 sm:px-6 py-4 grid grid-cols-1 md:grid-cols-12 gap-3 md:gap-3 items-start md:items-center transition-colors opacity-95', themeStore.isDark ? 'hover:bg-zinc-900/50' : 'hover:bg-zinc-50']">
                    <div class="md:col-span-4 flex items-start gap-2.5">
                      <div :class="['w-8 h-8 rounded-lg flex items-center justify-center shrink-0 mt-0.5 md:mt-0 border', themeStore.isDark ? 'bg-emerald-500/10 border-emerald-500/20 text-emerald-300' : 'bg-emerald-50 border-emerald-100 text-emerald-600']">
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1"></path>
                        </svg>
                      </div>
                      <div class="md:hidden block w-full">
                        <div class="flex items-center gap-2 flex-wrap">
                          <p :class="['text-sm font-semibold', themeStore.isDark ? 'text-white' : 'text-zinc-900']">Faturamento</p>
                          <span :class="['text-[9px] font-bold uppercase px-1.5 py-0.5 rounded border', themeStore.isDark ? 'bg-amber-500/10 border-amber-500/20 text-amber-300' : 'bg-amber-50 border-amber-200 text-amber-700']">Gerente/Dono</span>
                        </div>
                        <p :class="['text-xs mt-1', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500']">Fatura, pagamentos, estornos. Cargo hierárquico.</p>
                        <div class="mt-3 grid grid-cols-2 gap-3">
                          <button type="button" :class="['w-full flex items-center justify-center gap-2 px-3 py-2 rounded-lg border text-[11px] font-bold uppercase tracking-wider', themeStore.isDark ? 'bg-zinc-800/70 border-zinc-700/60 text-zinc-400' : 'bg-zinc-100 border-zinc-200 text-zinc-500']">🔒 Email</button>
                          <button type="button" :class="['w-full flex items-center justify-center gap-2 px-3 py-2 rounded-lg border text-[11px] font-bold uppercase tracking-wider', themeStore.isDark ? 'bg-zinc-800/70 border-zinc-700/60 text-zinc-400' : 'bg-zinc-100 border-zinc-200 text-zinc-500']">🔒 Push</button>
                        </div>
                      </div>
                      <div class="hidden md:block space-y-1">
                        <div class="flex items-center gap-2">
                          <p :class="['text-sm font-semibold', themeStore.isDark ? 'text-white' : 'text-zinc-900']">Faturamento</p>
                          <span :class="['text-[9px] font-bold uppercase px-1.5 py-0.5 rounded border', themeStore.isDark ? 'bg-amber-500/10 border-amber-500/20 text-amber-300' : 'bg-amber-50 border-amber-200 text-amber-700']">Hierárquico</span>
                        </div>
                      </div>
                    </div>
                    <div class="md:col-span-5 md:block">
                      <p :class="['text-xs md:text-sm leading-relaxed', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500']">Fatura emitida, pagamento confirmado, estorno ou falha. <strong>Visível só para Gerente e Dono.</strong> Atendentes não veem nem recebem.</p>
                    </div>
                    <div class="md:col-span-1 hidden md:flex justify-center">
                      <button type="button" :class="['w-full h-9 rounded-lg border flex items-center justify-center text-[10px] font-bold uppercase tracking-wider', themeStore.isDark ? 'bg-zinc-800/70 border-zinc-700/60 text-zinc-400 hover:bg-zinc-800' : 'bg-zinc-100 border-zinc-200 text-zinc-500 hover:bg-zinc-200']">🔒</button>
                    </div>
                    <div class="md:col-span-2 hidden md:flex justify-center">
                      <button type="button" :class="['w-full h-9 rounded-lg border flex items-center justify-center gap-1.5 text-[10px] font-bold uppercase tracking-wider', themeStore.isDark ? 'bg-zinc-800/70 border-zinc-700/60 text-zinc-400 hover:bg-zinc-800' : 'bg-zinc-100 border-zinc-200 text-zinc-500 hover:bg-zinc-200']">🔒</button>
                    </div>
                  </div>

                  <!-- Linha 6: Segurança -->
                  <div :class="['px-5 sm:px-6 py-4 grid grid-cols-1 md:grid-cols-12 gap-3 md:gap-3 items-start md:items-center transition-colors', themeStore.isDark ? 'hover:bg-zinc-900/50' : 'hover:bg-zinc-50']">
                    <div class="md:col-span-4 flex items-start gap-2.5">
                      <div :class="['w-8 h-8 rounded-lg flex items-center justify-center shrink-0 mt-0.5 md:mt-0 border', themeStore.isDark ? 'bg-red-500/10 border-red-500/20 text-red-300' : 'bg-red-50 border-red-100 text-red-600']">
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"></path>
                        </svg>
                      </div>
                      <div class="md:hidden block w-full">
                        <div class="flex items-center gap-2 flex-wrap">
                          <p :class="['text-sm font-semibold', themeStore.isDark ? 'text-white' : 'text-zinc-900']">Segurança</p>
                          <span :class="['text-[9px] font-bold uppercase px-1.5 py-0.5 rounded border', themeStore.isDark ? 'bg-amber-500/10 border-amber-500/20 text-amber-300' : 'bg-amber-50 border-amber-200 text-amber-700']">Por usuário</span>
                        </div>
                        <p :class="['text-xs mt-1', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500']">Login novo, senha alterada, chave API.</p>
                        <div class="mt-3 grid grid-cols-2 gap-3">
                          <button type="button" :class="['w-full flex items-center justify-center gap-2 px-3 py-2 rounded-lg border text-[11px] font-bold uppercase tracking-wider', themeStore.isDark ? 'bg-zinc-800/70 border-zinc-700/60 text-zinc-400' : 'bg-zinc-100 border-zinc-200 text-zinc-500']">🔒 Email</button>
                          <button type="button" :class="['w-full flex items-center justify-center gap-2 px-3 py-2 rounded-lg border text-[11px] font-bold uppercase tracking-wider', themeStore.isDark ? 'bg-zinc-800/70 border-zinc-700/60 text-zinc-400' : 'bg-zinc-100 border-zinc-200 text-zinc-500']">🔒 Push</button>
                        </div>
                      </div>
                      <div class="hidden md:block space-y-1">
                        <div class="flex items-center gap-2">
                          <p :class="['text-sm font-semibold', themeStore.isDark ? 'text-white' : 'text-zinc-900']">Segurança</p>
                          <span :class="['text-[9px] font-bold uppercase px-1.5 py-0.5 rounded border', themeStore.isDark ? 'bg-amber-500/10 border-amber-500/20 text-amber-300' : 'bg-amber-50 border-amber-200 text-amber-700']">Por usuário</span>
                        </div>
                      </div>
                    </div>
                    <div class="md:col-span-5 md:block">
                      <p :class="['text-xs md:text-sm leading-relaxed', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500']">Login em dispositivo novo, troca de senha, chave de API alterada, 2FA. <strong>Cada usuário vê só os seus eventos.</strong></p>
                    </div>
                    <div class="md:col-span-1 hidden md:flex justify-center">
                      <button type="button" :class="['w-full h-9 rounded-lg border flex items-center justify-center text-[10px] font-bold uppercase tracking-wider', themeStore.isDark ? 'bg-zinc-800/70 border-zinc-700/60 text-zinc-400 hover:bg-zinc-800' : 'bg-zinc-100 border-zinc-200 text-zinc-500 hover:bg-zinc-200']">🔒</button>
                    </div>
                    <div class="md:col-span-2 hidden md:flex justify-center">
                      <button type="button" :class="['w-full h-9 rounded-lg border flex items-center justify-center gap-1.5 text-[10px] font-bold uppercase tracking-wider', themeStore.isDark ? 'bg-zinc-800/70 border-zinc-700/60 text-zinc-400 hover:bg-zinc-800' : 'bg-zinc-100 border-zinc-200 text-zinc-500 hover:bg-zinc-200']">🔒</button>
                    </div>
                  </div>

                </div>
              </div>

            </div>

            <!-- Aside direito: hierarquia + roadmap -->
            <aside class="space-y-4 xl:space-y-5 h-fit">

              <!-- Card: Hierarquia de cargos -->
              <div :class="['rounded-2xl border p-5 space-y-3.5 shadow-xl', themeStore.isDark ? 'bg-zinc-900/50 border-zinc-800' : 'bg-white border-zinc-200 shadow-zinc-900/5']">
                <div :class="['flex items-center gap-2 text-xs uppercase tracking-[0.18em] font-semibold', themeStore.isDark ? 'text-zinc-500' : 'text-zinc-500']">
                  <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z"></path>
                  </svg>
                  Hierarquia de cargos
                </div>
                <div :class="['text-xs leading-relaxed space-y-2.5', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500']">
                  <p>Quando o backend existir, as permissões vão seguir essa regra:</p>
                  <div class="space-y-2 pt-1">
                    <!-- Dono -->
                    <div :class="['flex items-start gap-2.5 p-2.5 rounded-lg border', themeStore.isDark ? 'bg-zinc-900/70 border-zinc-800' : 'bg-zinc-50 border-zinc-200']">
                      <div :class="['w-6 h-6 rounded-md flex items-center justify-center shrink-0 mt-0.5 border', themeStore.isDark ? 'bg-amber-500/10 border-amber-500/20 text-amber-300' : 'bg-amber-50 border-amber-100 text-amber-600']">
                        <svg class="w-3.5 h-3.5" fill="currentColor" viewBox="0 0 24 24">
                          <path d="M5 16L3 5l5.5 5L12 4l3.5 6L21 5l-2 11H5zm14 3c0 .6-.4 1-1 1H6c-.6 0-1-.4-1-1v-1h14v1z"/>
                        </svg>
                      </div>
                      <div class="min-w-0">
                        <p :class="['text-[11px] font-bold uppercase tracking-wider', themeStore.isDark ? 'text-amber-300' : 'text-amber-700']">Dono</p>
                        <p :class="['text-[11px]', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500']">Tudo. Faturamento, segurança, tokens, pagamentos.</p>
                      </div>
                    </div>
                    <!-- Gerente -->
                    <div :class="['flex items-start gap-2.5 p-2.5 rounded-lg border', themeStore.isDark ? 'bg-zinc-900/70 border-zinc-800' : 'bg-zinc-50 border-zinc-200']">
                      <div :class="['w-6 h-6 rounded-md flex items-center justify-center shrink-0 mt-0.5 border', themeStore.isDark ? 'bg-indigo-500/10 border-indigo-500/20 text-indigo-300' : 'bg-indigo-50 border-indigo-100 text-indigo-600']">
                        <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"></path>
                        </svg>
                      </div>
                      <div class="min-w-0">
                        <p :class="['text-[11px] font-bold uppercase tracking-wider', themeStore.isDark ? 'text-indigo-300' : 'text-indigo-700']">Gerente</p>
                        <p :class="['text-[11px]', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500']">Pedidos, estoque, notificações. Faturamento só leitura.</p>
                      </div>
                    </div>
                    <!-- Atendente -->
                    <div :class="['flex items-start gap-2.5 p-2.5 rounded-lg border', themeStore.isDark ? 'bg-zinc-900/70 border-zinc-800' : 'bg-zinc-50 border-zinc-200']">
                      <div :class="['w-6 h-6 rounded-md flex items-center justify-center shrink-0 mt-0.5 border', themeStore.isDark ? 'bg-zinc-800/60 border-zinc-700/60 text-zinc-400' : 'bg-zinc-100 border-zinc-200 text-zinc-500']">
                        <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"></path>
                        </svg>
                      </div>
                      <div class="min-w-0">
                        <p :class="['text-[11px] font-bold uppercase tracking-wider', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-600']">Atendente</p>
                        <p :class="['text-[11px]', themeStore.isDark ? 'text-zinc-500' : 'text-zinc-400']">Só pedidos (receber/marcar pronto). Nada de estoque ou $.</p>
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Card: Roadmap backend -->
              <div :class="['rounded-2xl border p-5 space-y-3.5 shadow-xl', themeStore.isDark ? 'bg-zinc-900/50 border-zinc-800' : 'bg-white border-zinc-200 shadow-zinc-900/5']">
                <div :class="['flex items-center gap-2 text-xs uppercase tracking-[0.18em] font-semibold', themeStore.isDark ? 'text-zinc-500' : 'text-zinc-500']">
                  <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"></path>
                  </svg>
                  Roadmap backend
                </div>
                <div :class="['text-xs leading-relaxed space-y-2', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500']">
                  <ul class="space-y-1.5">
                    <li class="flex items-start gap-2">
                      <span :class="['mt-0.5 w-1.5 h-1.5 rounded-full shrink-0', themeStore.isDark ? 'bg-zinc-600' : 'bg-zinc-300']"></span>
                      <span>Tabela <code :class="['px-1.5 py-0.5 rounded text-[10px] border', themeStore.isDark ? 'bg-zinc-800/70 border-zinc-700 text-zinc-300' : 'bg-zinc-100 border-zinc-200 text-zinc-700']">notification_preferences</code> (UserId, EventType, EmailOn, PushOn)</span>
                    </li>
                    <li class="flex items-start gap-2">
                      <span :class="['mt-0.5 w-1.5 h-1.5 rounded-full shrink-0', themeStore.isDark ? 'bg-zinc-600' : 'bg-zinc-300']"></span>
                      <span>Service + Policies pra validar hierarquia (atendente não pode desligar pedido novo)</span>
                    </li>
                    <li class="flex items-start gap-2">
                      <span :class="['mt-0.5 w-1.5 h-1.5 rounded-full shrink-0', themeStore.isDark ? 'bg-zinc-600' : 'bg-zinc-300']"></span>
                      <span>Provider Push (Firebase Cloud Messaging) + serviço de email (Resend/SendGrid)</span>
                    </li>
                    <li class="flex items-start gap-2">
                      <span :class="['mt-0.5 w-1.5 h-1.5 rounded-full shrink-0', themeStore.isDark ? 'bg-zinc-600' : 'bg-zinc-300']"></span>
                      <span>WhatsApp Business API — feature premium (Plano Pro ou Empresarial)</span>
                    </li>
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

              <div :class="['rounded-2xl border shadow-xl overflow-hidden', themeStore.isDark ? 'bg-zinc-900/50 border-zinc-800' : 'bg-white border-zinc-200 shadow-zinc-900/5']">
                <div :class="['p-5 sm:p-6 flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4 border-b', themeStore.isDark ? 'border-zinc-800/80' : 'border-zinc-200']">
                  <div class="flex items-start gap-3 flex-1">
                    <div :class="['w-10 h-10 rounded-xl flex items-center justify-center flex-shrink-0 border', themeStore.isDark ? 'bg-violet-500/15 border-violet-500/30 text-violet-300' : 'bg-violet-50 border-violet-200 text-violet-700']">
                      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z"></path>
                      </svg>
                    </div>
                    <div class="space-y-0.5 flex-1">
                      <h2 :class="['text-base sm:text-lg font-semibold tracking-tight', themeStore.isDark ? 'text-white' : 'text-zinc-900']">Tokens de autorização temporários</h2>
                      <p :class="['text-xs sm:text-sm leading-relaxed max-w-2xl', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-600']">Credenciais de curta duração para integrações externas e automações. Tokens são hashados (SHA-256) e só aparecem em texto puro UMA VEZ no momento da criação.</p>
                    </div>
                    <button
                      v-if="!createTokenOpen"
                      @click="createTokenOpen = true; tmpTokensError = null;"
                      :class="['inline-flex items-center gap-2 rounded-xl px-3.5 py-2.5 text-sm font-semibold shrink-0 transition-colors', themeStore.isDark ? 'bg-indigo-500/20 text-indigo-200 hover:bg-indigo-500/30 border border-indigo-500/30' : 'bg-indigo-600 text-white hover:bg-indigo-700 border border-indigo-600 shadow-sm']"
                    >
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"></path>
                      </svg>
                      Criar token
                    </button>
                    <button
                      v-else
                      @click="createTokenOpen = false;"
                      :class="['inline-flex items-center gap-2 rounded-xl px-3.5 py-2.5 text-sm font-semibold shrink-0 transition-colors border', themeStore.isDark ? 'text-zinc-300 hover:text-white bg-zinc-800/60 border-zinc-700 hover:bg-zinc-800' : 'text-zinc-700 hover:text-zinc-900 bg-zinc-100 border-zinc-200 hover:bg-zinc-200']"
                    >
                      Cancelar
                    </button>
                  </div>
                </div>

                <div v-if="createTokenOpen" :class="['p-5 sm:p-6 border-b', themeStore.isDark ? 'border-zinc-800/80 bg-zinc-900/30' : 'border-zinc-200 bg-zinc-50']">
                  <div :class="['rounded-xl border p-4 sm:p-5 space-y-4 border-dashed', themeStore.isDark ? 'bg-zinc-900/50 border-zinc-700/70' : 'bg-white border-zinc-300']">
                    <div :class="['flex items-center gap-2 text-[11px] uppercase tracking-[0.18em] font-semibold', themeStore.isDark ? 'text-zinc-500' : 'text-zinc-500']">
                      <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6V4m0 2a2 2 0 100 4m0-4a2 2 0 110 4m-6 8a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4m6 6v10m6-2a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4"></path>
                      </svg>
                      Nova credencial
                    </div>
                    <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                      <div class="md:col-span-2 space-y-2">
                        <label :class="['text-xs uppercase tracking-widest font-semibold', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500']">Nome do token <span class="text-rose-400">*</span></label>
                        <input
                          v-model="createTokenForm.name"
                          type="text"
                          maxlength="64"
                          placeholder="Ex.: Integração iFood Webhook, ERP Leitor Nota"
                          :class="['w-full rounded-xl px-3.5 py-3 text-sm border font-medium transition-colors outline-none focus:ring-2', themeStore.isDark ? 'bg-zinc-900/40 border-zinc-700/50 text-white placeholder:text-zinc-600 focus:border-indigo-500/50 focus:ring-indigo-500/20' : 'bg-white border-zinc-200 text-zinc-900 placeholder:text-zinc-400 focus:border-indigo-500 focus:ring-indigo-500/20']"
                        />
                      </div>
                      <div class="md:col-span-2 space-y-2">
                        <label :class="['text-xs uppercase tracking-widest font-semibold', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500']">Descrição (opcional)</label>
                        <input
                          v-model="createTokenForm.description!"
                          type="text"
                          maxlength="200"
                          placeholder="Motivo / dono / integração que vai consumir"
                          :class="['w-full rounded-xl px-3.5 py-3 text-sm border transition-colors outline-none focus:ring-2', themeStore.isDark ? 'bg-zinc-900/40 border-zinc-700/50 text-white placeholder:text-zinc-600 focus:border-indigo-500/50 focus:ring-indigo-500/20' : 'bg-white border-zinc-200 text-zinc-900 placeholder:text-zinc-400 focus:border-indigo-500 focus:ring-indigo-500/20']"
                        />
                      </div>
                      <div class="space-y-2">
                        <label :class="['text-xs uppercase tracking-widest font-semibold', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500']">Escopo</label>
                        <select
                          v-model="createTokenForm.scope"
                          :class="['w-full rounded-xl px-3.5 py-3 text-sm border font-medium transition-colors outline-none focus:ring-2', themeStore.isDark ? 'bg-zinc-900/40 border-zinc-700/50 text-white focus:border-indigo-500/50 focus:ring-indigo-500/20' : 'bg-white border-zinc-200 text-zinc-900 focus:border-indigo-500 focus:ring-indigo-500/20']"
                        >
                          <option value="read">Apenas leitura (read)</option>
                          <option value="write">Leitura + escrita (write)</option>
                          <option value="admin">Completo (admin) — perigoso</option>
                        </select>
                      </div>
                      <div class="space-y-2">
                        <label :class="['text-xs uppercase tracking-widest font-semibold', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500']">Validade (horas)</label>
                        <input
                          v-model.number="createTokenForm.validityHours"
                          type="number"
                          min="1"
                          step="1"
                          :class="['w-full rounded-xl px-3.5 py-3 text-sm border font-mono transition-colors outline-none focus:ring-2', themeStore.isDark ? 'bg-zinc-900/40 border-zinc-700/50 text-white focus:border-indigo-500/50 focus:ring-indigo-500/20' : 'bg-white border-zinc-200 text-zinc-900 focus:border-indigo-500 focus:ring-indigo-500/20']"
                        />
                        <p :class="['text-[11px] mt-1', themeStore.isDark ? 'text-zinc-500' : 'text-zinc-500']">Padrão 720h (30 dias). Máximo 2160h (90d) recomendado.</p>
                      </div>
                      <div class="space-y-2 md:col-span-2">
                        <label :class="['text-xs uppercase tracking-widest font-semibold', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500']">Auto-rotação (horas, opcional)</label>
                        <input
                          v-model.number="createTokenForm.autoRotateHours"
                          type="number"
                          min="1"
                          step="1"
                          placeholder="Deixe vazio para não rotacionar automaticamente"
                          :class="['w-full rounded-xl px-3.5 py-3 text-sm border font-mono transition-colors outline-none focus:ring-2', themeStore.isDark ? 'bg-zinc-900/40 border-zinc-700/50 text-white placeholder:text-zinc-600 focus:border-indigo-500/50 focus:ring-indigo-500/20' : 'bg-white border-zinc-200 text-zinc-900 placeholder:text-zinc-400 focus:border-indigo-500 focus:ring-indigo-500/20']"
                        />
                        <p :class="['text-[11px] mt-1', themeStore.isDark ? 'text-zinc-500' : 'text-zinc-500']">Quando definido, o backend emite um novo token com mesmo escopo/permissões antes de expirar.</p>
                      </div>
                    </div>
                    <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 pt-2">
                      <div :class="['text-[11px] flex items-start gap-2 max-w-xl', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-600']">
                        <span class="mt-0.5 shrink-0">⚠️</span>
                        Após criar, o token aparece UMA VEZ. Anote-o imediatamente — não temos como recuperar o valor em texto puro depois (só o hash SHA-256 é salvo).
                      </div>
                      <div class="flex items-center gap-2 sm:justify-end">
                        <button
                          @click="createTokenOpen = false;"
                          :class="['inline-flex items-center gap-2 rounded-xl px-4 py-2.5 text-sm font-semibold border transition-colors', themeStore.isDark ? 'text-zinc-300 hover:text-white bg-zinc-800/60 border-zinc-700 hover:bg-zinc-800' : 'text-zinc-700 hover:text-zinc-900 bg-zinc-100 border-zinc-200 hover:bg-zinc-200']"
                        >
                          Cancelar
                        </button>
                        <button
                          @click="handleCreateToken"
                          :disabled="createTokenSubmitting || !createTokenForm.name.trim()"
                          :class="['inline-flex items-center gap-2 rounded-xl px-4 py-2.5 text-sm font-semibold border transition-colors disabled:opacity-60 disabled:cursor-not-allowed', themeStore.isDark ? 'bg-emerald-500/20 text-emerald-200 hover:bg-emerald-500/30 border-emerald-500/30' : 'bg-emerald-600 text-white hover:bg-emerald-700 border-emerald-600 shadow-sm']"
                        >
                          <svg v-if="!createTokenSubmitting" class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z"></path>
                          </svg>
                          <svg v-else class="w-4 h-4 animate-spin" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"></path>
                          </svg>
                          {{ createTokenSubmitting ? 'Criando…' : 'Gerar token' }}
                        </button>
                      </div>
                    </div>
                  </div>
                </div>

                <div class="p-5 sm:p-6">
                  <div v-if="tmpTokensError" :class="['mb-4 rounded-xl border p-3.5 flex items-start gap-3', themeStore.isDark ? 'border-rose-500/30 bg-rose-500/10 text-rose-200' : 'border-rose-200 bg-rose-50 text-rose-800']">
                    <svg class="w-4.5 h-4.5 mt-0.5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"></path>
                    </svg>
                    <p class="text-xs leading-relaxed">{{ tmpTokensError }}</p>
                  </div>

                  <div v-if="tmpTokensLoading" :class="['rounded-xl border p-8 sm:p-10 text-center space-y-3', themeStore.isDark ? 'border-zinc-800/70 bg-zinc-900/40' : 'border-zinc-200 bg-zinc-50']">
                    <svg class="w-7 h-7 animate-spin mx-auto text-zinc-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"></path>
                    </svg>
                    <p :class="['text-sm', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-600']">Carregando tokens…</p>
                  </div>

                  <div v-else-if="tmpTokens.length === 0 && !createTokenOpen" :class="['rounded-xl border p-8 sm:p-10 text-center space-y-3', themeStore.isDark ? 'border-zinc-800/70 bg-zinc-900/30' : 'border-zinc-200 bg-zinc-50']">
                    <div :class="['mx-auto w-14 h-14 rounded-2xl border flex items-center justify-center shrink-0', themeStore.isDark ? 'bg-zinc-800/80 border-zinc-700/60 text-zinc-400' : 'bg-white border-zinc-200 text-zinc-500']">
                      <svg class="w-7 h-7" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z"></path>
                      </svg>
                    </div>
                    <div class="space-y-1.5">
                      <h3 :class="['text-base font-semibold', themeStore.isDark ? 'text-zinc-100' : 'text-zinc-900']">Nenhum token criado ainda</h3>
                      <p :class="['text-xs sm:text-sm max-w-xl mx-auto leading-relaxed', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-600']">Crie o primeiro token para usar em webhooks, automações e integrações com ERPs, iFood, Rappi, ou qualquer parceiro que precise chamar a API do ISM com escopo limitado.</p>
                    </div>
                    <button
                      @click="createTokenOpen = true;"
                      :class="['inline-flex items-center gap-2 rounded-xl px-4 py-2.5 text-sm font-semibold transition-colors border', themeStore.isDark ? 'bg-indigo-500/20 text-indigo-200 hover:bg-indigo-500/30 border-indigo-500/30' : 'bg-indigo-600 text-white hover:bg-indigo-700 border-indigo-600 shadow-sm']"
                    >
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"></path>
                      </svg>
                      Criar primeiro token
                    </button>
                  </div>

                  <div v-else-if="tmpTokens.length > 0" class="rounded-xl overflow-hidden border" :class="themeStore.isDark ? 'border-zinc-800 bg-zinc-900/30' : 'border-zinc-200 bg-white'">
                    <div class="overflow-x-auto">
                      <table class="w-full text-sm">
                        <thead>
                          <tr :class="themeStore.isDark ? 'bg-zinc-900/70 text-zinc-400' : 'bg-zinc-50 text-zinc-500'">
                            <th class="text-left px-4 py-3 font-semibold text-[11px] uppercase tracking-widest">Nome</th>
                            <th class="text-left px-4 py-3 font-semibold text-[11px] uppercase tracking-widest">Escopo</th>
                            <th class="text-left px-4 py-3 font-semibold text-[11px] uppercase tracking-widest hidden lg:table-cell">Criado por</th>
                            <th class="text-left px-4 py-3 font-semibold text-[11px] uppercase tracking-widest hidden sm:table-cell">Expira em</th>
                            <th class="text-left px-4 py-3 font-semibold text-[11px] uppercase tracking-widest hidden xl:table-cell">Último uso</th>
                            <th class="text-left px-4 py-3 font-semibold text-[11px] uppercase tracking-widest">Status</th>
                            <th class="text-right px-4 py-3 font-semibold text-[11px] uppercase tracking-widest">Ações</th>
                          </tr>
                        </thead>
                        <tbody :class="themeStore.isDark ? 'divide-y divide-zinc-800/70 text-zinc-200' : 'divide-y divide-zinc-200 text-zinc-700'">
                          <tr v-for="t in tmpTokens" :key="t.id" :class="themeStore.isDark ? 'hover:bg-zinc-900/60' : 'hover:bg-zinc-50'">
                            <td class="px-4 py-3.5">
                              <div class="flex flex-col">
                                <p class="font-semibold" :class="themeStore.isDark ? 'text-zinc-100' : 'text-zinc-900'">{{ t.name }}</p>
                                <p v-if="t.description" :class="['text-[11px] mt-0.5 max-w-xs truncate', themeStore.isDark ? 'text-zinc-500' : 'text-zinc-500']">{{ t.description }}</p>
                              </div>
                            </td>
                            <td class="px-4 py-3.5">
                              <span :class="[
                                'inline-flex items-center px-2 py-0.5 rounded-md text-[11px] font-bold tracking-wide border',
                                t.scope === 'admin'
                                  ? (themeStore.isDark ? 'bg-rose-500/10 text-rose-300 border-rose-500/20' : 'bg-rose-50 text-rose-700 border-rose-200')
                                  : t.scope === 'write'
                                  ? (themeStore.isDark ? 'bg-amber-500/10 text-amber-300 border-amber-500/20' : 'bg-amber-50 text-amber-800 border-amber-200')
                                  : (themeStore.isDark ? 'bg-emerald-500/10 text-emerald-300 border-emerald-500/20' : 'bg-emerald-50 text-emerald-700 border-emerald-200')
                              ]">
                                {{ t.scope.toUpperCase() }}
                              </span>
                            </td>
                            <td class="px-4 py-3.5 hidden lg:table-cell">
                              <p class="text-xs">{{ t.createdByUserName || '—' }}</p>
                              <p :class="['text-[11px]', themeStore.isDark ? 'text-zinc-500' : 'text-zinc-500']">{{ formatDateUtc(t.createdAtUtc) }}</p>
                            </td>
                            <td class="px-4 py-3.5 hidden sm:table-cell">
                              <p class="text-xs">{{ formatDateUtc(t.expiresAtUtc) }}</p>
                              <p v-if="t.autoRotateAtUtc" :class="['text-[11px] mt-0.5 flex items-center gap-1', themeStore.isDark ? 'text-violet-400' : 'text-violet-700']">
                                <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"></path>
                                </svg>
                                Rotação {{ formatDateUtc(t.autoRotateAtUtc) }}
                              </p>
                            </td>
                            <td class="px-4 py-3.5 hidden xl:table-cell">
                              <p class="text-xs">{{ t.lastUsedAtUtc ? formatDateUtc(t.lastUsedAtUtc) : 'Nunca usado' }}</p>
                            </td>
                            <td class="px-4 py-3.5">
                              <span
                                v-if="computeTokenStatus(t) === 'active'"
                                :class="['inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[11px] font-bold border', themeStore.isDark ? 'bg-emerald-500/10 text-emerald-300 border-emerald-500/20' : 'bg-emerald-50 text-emerald-700 border-emerald-200']"
                              >
                                <span class="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-pulse"></span>
                                Ativo
                              </span>
                              <span
                                v-else-if="computeTokenStatus(t) === 'rotating-soon'"
                                :class="['inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[11px] font-bold border', themeStore.isDark ? 'bg-violet-500/10 text-violet-300 border-violet-500/20' : 'bg-violet-50 text-violet-700 border-violet-200']"
                              >
                                Rotação em 24h
                              </span>
                              <span
                                v-else-if="computeTokenStatus(t) === 'expired'"
                                :class="['inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[11px] font-bold border', themeStore.isDark ? 'bg-zinc-700/60 text-zinc-300 border-zinc-600/60' : 'bg-zinc-100 text-zinc-700 border-zinc-300']"
                              >
                                Expirado
                              </span>
                              <span
                                v-else
                                :class="['inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[11px] font-bold border', themeStore.isDark ? 'bg-rose-500/10 text-rose-300 border-rose-500/20' : 'bg-rose-50 text-rose-700 border-rose-200']"
                              >
                                Revogado
                              </span>
                            </td>
                            <td class="px-4 py-3.5">
                              <div class="flex items-center justify-end gap-2">
                                <button
                                  @click="handleRotateToken(t.id)"
                                  :disabled="computeTokenStatus(t) === 'revoked'"
                                  :class="['inline-flex items-center gap-1.5 px-2.5 py-1.5 rounded-lg text-[11px] font-bold border transition-colors disabled:opacity-40 disabled:cursor-not-allowed', themeStore.isDark ? 'text-violet-300 hover:text-violet-200 border-violet-500/20 bg-violet-500/10 hover:bg-violet-500/20' : 'text-violet-700 hover:text-violet-900 border-violet-200 bg-violet-50 hover:bg-violet-100']"
                                >
                                  <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"></path>
                                  </svg>
                                  Rotacionar
                                </button>
                                <button
                                  @click="handleRevokeToken(t.id)"
                                  :disabled="computeTokenStatus(t) === 'revoked' || computeTokenStatus(t) === 'expired'"
                                  :class="['inline-flex items-center gap-1.5 px-2.5 py-1.5 rounded-lg text-[11px] font-bold border transition-colors disabled:opacity-40 disabled:cursor-not-allowed', themeStore.isDark ? 'text-rose-300 hover:text-rose-200 border-rose-500/20 bg-rose-500/10 hover:bg-rose-500/20' : 'text-rose-700 hover:text-rose-900 border-rose-200 bg-rose-50 hover:bg-rose-100']"
                                >
                                  <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636"></path>
                                  </svg>
                                  Revogar
                                </button>
                              </div>
                            </td>
                          </tr>
                        </tbody>
                      </table>
                    </div>
                    <div :class="['px-4 py-3 flex items-center justify-between text-[11px] border-t', themeStore.isDark ? 'bg-zinc-950/40 border-t-zinc-800/70 text-zinc-500' : 'bg-zinc-50 border-t-zinc-200 text-zinc-500']">
                      <span>
                        <strong>{{ tmpTokens.length }}</strong> token{{ tmpTokens.length === 1 ? '' : 's' }} no total · SHA-256 hash · RNG criptográfico 48 chars
                      </span>
                      <button
                        @click="loadTmpTokens"
                        :class="['inline-flex items-center gap-1.5 font-semibold hover:underline underline-offset-4', themeStore.isDark ? 'text-zinc-400 hover:text-zinc-200' : 'text-zinc-600 hover:text-zinc-900']"
                      >
                        <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"></path>
                        </svg>
                        Atualizar
                      </button>
                    </div>
                  </div>
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
              <div :class="['rounded-2xl border p-5 space-y-3 shadow-xl', themeStore.isDark ? 'bg-zinc-900/50 border-zinc-800' : 'bg-white border-zinc-200 shadow-zinc-900/5']">
                <div :class="['flex items-center gap-2 text-xs uppercase tracking-[0.18em] font-semibold', themeStore.isDark ? 'text-violet-300' : 'text-violet-700']">
                  <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z"></path>
                  </svg>
                  API tokens temporários
                </div>
                <ul :class="['space-y-2 text-xs list-disc list-inside', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-600']">
                  <li>Prefixo token: <code :class="['px-1.5 py-0.5 rounded text-[11px] border font-mono', themeStore.isDark ? 'bg-zinc-800/70 border-zinc-700 text-violet-200' : 'bg-zinc-100 border-zinc-200 text-violet-800']">ism_</code> + 48 chars aleatórios.</li>
                  <li>NUNCA salvo em texto puro. Apenas hash SHA-256 no banco.</li>
                  <li>Filtro de <em>tenant</em> automático (EF Core QueryFilter por RestaurantId).</li>
                  <li>Validação anônima pronta em <code :class="['px-1.5 py-0.5 rounded text-[11px] border font-mono', themeStore.isDark ? 'bg-zinc-800/70 border-zinc-700 text-zinc-300' : 'bg-zinc-100 border-zinc-200 text-zinc-700']">POST /api/tmp-authorizations/validate</code>.</li>
                  <li>Rotação gera um novo token (mesmo escopo) e revoga o anterior.</li>
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

    <div
      v-if="lastCreatedModal.visible"
      class="fixed inset-0 z-[100] flex items-end sm:items-center justify-center px-4 py-6 bg-black/70 backdrop-blur-sm"
      @click.self="lastCreatedModal.visible = false"
    >
      <div class="w-full sm:max-w-xl rounded-2xl border border-amber-500/30 bg-zinc-950 shadow-2xl overflow-hidden">
        <div class="p-5 sm:p-6 border-b border-zinc-800/80 bg-gradient-to-b from-amber-500/5 to-transparent">
          <div class="flex items-start gap-3">
            <div class="w-11 h-11 rounded-xl bg-amber-500/15 border border-amber-500/30 text-amber-300 flex items-center justify-center shrink-0">
              <svg class="w-5.5 h-5.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"></path>
              </svg>
            </div>
            <div class="space-y-1 flex-1 min-w-0">
              <p class="text-[11px] uppercase tracking-[0.18em] text-amber-300 font-semibold">Mostrado UMA VEZ · Salve imediatamente</p>
              <h3 class="text-lg sm:text-xl font-bold text-white tracking-tight">Token criado: {{ lastCreatedModal.name }}</h3>
              <p class="text-xs sm:text-sm text-zinc-400 leading-relaxed">
                Este valor em texto puro <strong class="text-amber-200 font-semibold">nunca mais será exibido</strong>. Não tem como recuperar depois (só salvamos o hash SHA-256). Copie agora e guarde em local seguro.
              </p>
            </div>
          </div>
        </div>
        <div class="p-5 sm:p-6 space-y-4">
          <div class="rounded-xl border border-dashed border-amber-500/30 bg-amber-500/5 p-4 space-y-3">
            <div class="flex items-center justify-between gap-3">
              <p class="text-[11px] uppercase tracking-[0.18em] text-amber-300 font-semibold">Token completo · ID #{{ lastCreatedModal.tokenId }}</p>
              <button
                @click="copyPlainToken"
                :class="[
                  'inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-[11px] font-bold border transition-colors',
                  lastCreatedCopied
                    ? 'bg-emerald-500/20 text-emerald-200 border-emerald-500/30'
                    : 'bg-amber-500/15 text-amber-200 hover:bg-amber-500/25 border-amber-500/30'
                ]"
              >
                <svg v-if="!lastCreatedCopied" class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z"></path>
                </svg>
                <svg v-else class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path>
                </svg>
                {{ lastCreatedCopied ? 'Copiado!' : 'Copiar token' }}
              </button>
            </div>
            <div class="rounded-lg bg-black/40 border border-zinc-800/80 p-3">
              <code class="block text-[11px] sm:text-xs text-amber-100 font-mono break-all leading-relaxed select-all whitespace-pre-wrap">
                {{ lastCreatedModal.plainToken }}
              </code>
            </div>
          </div>
          <div class="flex flex-col sm:flex-row sm:items-center sm:justify-end gap-3 pt-1">
            <button
              @click="lastCreatedModal.visible = false"
              class="inline-flex items-center justify-center gap-2 rounded-xl px-5 py-3 text-sm font-semibold text-zinc-100 bg-indigo-500/20 hover:bg-indigo-500/30 border border-indigo-500/30 transition-colors w-full sm:w-auto"
            >
              Já salvei, fechar
            </button>
          </div>
        </div>
      </div>
    </div>

  </div>
</template>
