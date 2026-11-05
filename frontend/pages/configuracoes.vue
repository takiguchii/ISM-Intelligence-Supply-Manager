<script setup lang="ts">
import AppearanceSettings from "~/components/settings/AppearanceSettings.vue";
import SettingsStyles from "~/components/settings/SettingsStyles.vue";
import GeneralSettings from "~/components/settings/GeneralSettings.vue";
import AccountSecuritySettings from "~/components/settings/AccountSecuritySettings.vue";
import NotificationSettings from "~/components/settings/NotificationSettings.vue";
import SubscriptionSettings from "~/components/settings/SubscriptionSettings.vue";
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
} from "~/services/modules/import/importService";
import { useThemeStore } from "~/stores/theme";

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();
const themeStore = useThemeStore();

const isLoading = ref(true);

const toggleSidebar = inject<() => void>("toggleSidebar", () => {});
const isSuperAdmin = computed(() => authStore.currentUser?.role === "Admin" && !authStore.currentUser?.restaurantId);
const isRestaurantUser = computed(() => !!authStore.currentUser?.restaurantId);

const targetRestaurantId = ref<number | null>(null);
const restaurants = ref<Array<{ id: number; name: string }>>([]);
const restaurantsError = ref<string | null>(null);

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
    subtitle: "Alertas do estoque, fornecedores e cardápio",
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
    subtitle: "Plano e cobranças demonstrativas",
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
    const savedModel = normalizeGeminiModelName(aiConfig.value.model);
    if (savedModel && MODEL_PRESETS.some((p) => p.value === savedModel)) {
      aiModelPreset.value = savedModel as (typeof MODEL_PRESETS)[number]["value"];
    } else {
      aiModelPreset.value = "gemini-3.5-flash-lite";
    }
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

watch(activeSectionId, id => {
  if (import.meta.client && route.hash !== `#${id}`) router.replace({ hash: `#${id}` });
});

onMounted(async () => {
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
    const { apiClientWithAuth } = await import("~/services/api/client");
    try {
      const list = (await apiClientWithAuth<Array<{ id: number; name: string }>>(`/api/restaurants`, {
        method: "GET"
      })) as Array<{ id: number; name: string }>;
      restaurants.value = list;
    } catch (error) {
      console.error("Erro ao carregar restaurantes:", error);
      restaurantsError.value = "Não foi possível carregar os restaurantes. Recarregue a página para tentar novamente.";
    }
  } else {
    targetRestaurantId.value = authStore.currentUser?.restaurantId ?? null;
  }
  await loadAiConfig();
  await loadTmpTokens();
  isLoading.value = false;
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

    <SettingsStyles />
    <p v-if="restaurantsError" role="alert" class="p-4 text-sm text-red-600 dark:text-red-400">{{ restaurantsError }}</p>
    <div class="flex flex-1 min-h-0 min-w-0">
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
                <option :value="null">Selecione um restaurante</option>
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

          <div v-if="isSuperAdmin && restaurants.length > 0 && ['ai', 'general', 'billing'].includes(activeSectionId)" class="rounded-2xl border border-amber-500/20 dark:bg-amber-500/5 bg-amber-50 p-4 sm:p-5 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
            <div class="flex items-start gap-3">
              <div class="w-10 h-10 rounded-xl bg-amber-500/15 border border-amber-500/30 text-amber-300 dark:text-amber-600 flex items-center justify-center flex-shrink-0">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path>
                </svg>
              </div>
              <div class="space-y-0.5">
                <p class="text-sm font-semibold dark:text-amber-100 text-amber-900">Modo Super Admin</p>
                <p class="text-xs sm:text-sm dark:text-amber-200/80 text-amber-800 leading-relaxed">
                  Você está editando a configuração de outro restaurante. Use o menu ao lado (ou abaixo em mobile) para alternar o alvo.
                </p>
              </div>
            </div>
            <label class="block sm:w-80 shrink-0 space-y-1.5">
              <span class="text-[11px] uppercase tracking-widest dark:text-amber-200/70 text-amber-800 font-semibold">Restaurante alvo</span>
              <select
                v-model="targetRestaurantId"
                class="w-full rounded-xl px-3.5 py-2.5 text-sm dark:bg-zinc-900/80 bg-white border border-amber-400/30 dark:text-amber-50 text-amber-900 focus:outline-none focus:ring-2 focus:ring-amber-400/50"
              >
                <option :value="null">Selecione um restaurante</option>
                <option v-for="r in restaurants" :key="r.id" :value="r.id">{{ r.id }} · {{ r.name }}</option>
              </select>
            </label>
          </div>


        <!-- AI Section -->
        <section v-if="activeSectionId === 'ai'" class="space-y-6">
          <div class="grid grid-cols-1 xl:grid-cols-3 gap-6">
            <div class="xl:col-span-2 rounded-2xl border dark:bg-zinc-900/50 bg-white dark:border-zinc-800 border-zinc-200 shadow-xl overflow-hidden">
              <div class="p-5 sm:p-6 flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4 border-b dark:border-zinc-800/80 border-zinc-200">
                <div class="flex items-start gap-3 flex-1">
                  <div class="w-10 h-10 rounded-xl bg-indigo-500/15 border border-indigo-500/30 dark:text-indigo-300 text-indigo-600 flex items-center justify-center flex-shrink-0">
                    <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 3v4M3 5h4M6 17v4m-2-2h4m5-16l2.286 6.857L21 12l-5.714 2.143L13 21l-2.286-6.857L5 12l5.714-2.143L13 3z"></path>
                    </svg>
                  </div>
                  <div class="space-y-0.5 flex-1">
                    <div class="flex flex-wrap items-center gap-2">
                      <h2 class="text-base sm:text-lg font-semibold dark:text-white text-zinc-900 tracking-tight">
                        {{ isSuperAdmin ? 'Configuração de IA — Restaurante alvo' : 'Configuração de IA do Restaurante' }}
                      </h2>
                      <span
                        v-if="aiConfig"
                        class="text-[10px] sm:text-xs font-semibold uppercase tracking-wider px-2 py-0.5 rounded-full border"
                        :class="
                          aiConfig.hasCustomKey
                            ? 'bg-emerald-500/10 dark:text-emerald-300 text-emerald-700 border-emerald-500/30'
                            : 'dark:bg-zinc-800/70 bg-zinc-100 dark:text-zinc-400 text-zinc-600 dark:border-zinc-700/60 border-zinc-300'
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
                    <p class="text-xs sm:text-sm dark:text-zinc-400 text-zinc-500 leading-relaxed max-w-2xl">
                      Configure sua chave de API de inteligência artificial para importar dados de foto/PDF via OCR com visão computacional.
                      <span v-if="!isSuperAdmin">Você paga apenas pela sua cota — nenhuma chave é compartilhada com outros restaurantes.</span>
                      <span v-else>Cada restaurante tem sua própria chave (nenhum compartilhamento).</span>
                    </p>
                  </div>
                </div>
              </div>

              <div v-if="aiConfigLoading" class="p-6 flex items-center gap-3 text-sm dark:text-zinc-400 text-zinc-500">
                <svg class="w-5 h-5 animate-spin text-indigo-400" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z"></path>
                </svg>
                Carregando configuração…
              </div>

              <div v-else-if="!activeAiRestaurantId" class="p-6 flex items-center gap-3 text-sm dark:text-amber-300 text-amber-800 rounded-xl bg-amber-500/5 border border-amber-500/20 m-5 sm:m-6">
                <svg class="w-5 h-5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"></path>
                </svg>
                <span>{{ isSuperAdmin ? 'Selecione um restaurante alvo para editar sua configuração de IA.' : 'Usuário não está vinculado a um restaurante.' }}</span>
              </div>

              <div v-else class="p-5 sm:p-6 space-y-4 sm:space-y-5">
                <div v-if="aiConfig && aiConfig.warnings && aiConfig.warnings.length > 0" class="rounded-xl border border-amber-500/20 dark:bg-amber-500/5 bg-amber-50 p-4 space-y-1.5">
                  <p class="text-xs uppercase tracking-widest dark:text-amber-300 text-amber-800 font-semibold">Avisos na configuração atual</p>
                  <ul class="list-disc list-inside space-y-1 pl-1 text-xs sm:text-sm dark:text-amber-200/90 text-amber-900">
                    <li v-for="(w, idx) in aiConfig.warnings" :key="idx">{{ w }}</li>
                  </ul>
                </div>

                <div class="space-y-3">
                  <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2">
                    <label class="text-xs uppercase tracking-widest dark:text-zinc-400 text-zinc-500 font-semibold">
                      Chave da API
                    </label>
                    <div v-if="aiConfig?.hasCustomKey" class="inline-flex items-center gap-1.5 text-[11px] font-semibold px-2.5 py-1 rounded-lg bg-emerald-500/10 border border-emerald-500/20 dark:text-emerald-300 text-emerald-700">
                      <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7"></path>
                      </svg>
                      Chave salva · {{ aiConfig?.keyLast4Digits ? `•••• ${aiConfig.keyLast4Digits}` : 'definida' }}
                    </div>
                    <div v-else class="inline-flex items-center gap-1.5 text-[11px] font-semibold px-2.5 py-1 rounded-lg dark:bg-zinc-800/70 bg-zinc-100 dark:border-zinc-700/60 border-zinc-300 dark:text-zinc-400 text-zinc-600">
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
                      class="w-full rounded-xl pl-4 pr-12 py-3.5 text-sm dark:bg-zinc-900/80 bg-white dark:border-zinc-700/70 border-zinc-300 dark:text-zinc-100 text-zinc-900 placeholder:dark:text-zinc-500 placeholder:text-zinc-400 focus:outline-none focus:ring-2 focus:ring-indigo-500/40 focus:border-indigo-500/50 font-mono tracking-tight"
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
                      class="absolute right-2.5 top-1/2 -translate-y-1/2 p-2 rounded-lg dark:text-zinc-400 text-zinc-500 hover:dark:text-white hover:text-zinc-900 hover:dark:bg-zinc-800/80 hover:bg-zinc-100 transition-colors"
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
                  <p class="text-xs dark:text-zinc-500 text-zinc-500 leading-relaxed">
                    Recomendado: obtenha gratuitamente em
                    <a href="https://aistudio.google.com/apikey" target="_blank" class="underline dark:text-indigo-300 text-indigo-600 hover:dark:text-indigo-200 hover:text-indigo-700 font-medium">Google AI Studio (Gemini gratuito)</a>.
                    No momento, apenas o provedor Google Gemini é suportado.
                  </p>
                </div>

                <div class="grid grid-cols-1 lg:grid-cols-5 gap-4 lg:gap-5">
                  <div class="lg:col-span-3 space-y-2">
                    <label class="text-xs uppercase tracking-widest dark:text-zinc-400 text-zinc-500 font-semibold">Modelo Gemini</label>
                    <select
                      v-model="aiModelPreset"
                      class="w-full rounded-xl px-3.5 py-3.5 text-sm dark:bg-zinc-900/80 bg-white dark:border-zinc-700/70 border-zinc-300 dark:text-zinc-100 text-zinc-900 focus:outline-none focus:ring-2 focus:ring-indigo-500/40 focus:border-indigo-500/50"
                    >
                      <option v-for="p in MODEL_PRESETS" :key="p.value" :value="p.value">
                        {{ p.label }}
                        <template v-if="p.tier && p.tier !== 'Custom'"> · {{ p.tier }}</template>
                      </option>
                    </select>
                    <p class="text-xs dark:text-zinc-500 text-zinc-500 leading-relaxed">
                      Dicas:
                      <span v-for="(h, idx) in GEMINI_MODEL_HINTS" :key="idx">
                        <code class="px-1 py-0.5 rounded dark:bg-zinc-800/70 bg-zinc-100 dark:border-zinc-700/60 border-zinc-200 dark:text-zinc-300 text-zinc-700 text-[11px] font-mono mr-1">{{ h.split(" ")[0] }}</code>
                      </span>
                    </p>
                  </div>
                  <div class="lg:col-span-2 space-y-2">
                    <label class="text-xs uppercase tracking-widest dark:text-zinc-400 text-zinc-500 font-semibold">Endpoint base (Google)</label>
                    <input
                      v-model="aiEndpoint"
                      placeholder="https://generativelanguage.googleapis.com/v1"
                      class="w-full rounded-xl px-3.5 py-3.5 text-sm dark:bg-zinc-800/40 bg-zinc-100 dark:border-zinc-700/40 border-zinc-200 dark:text-zinc-400 text-zinc-600 placeholder:dark:text-zinc-600 placeholder:text-zinc-400 focus:outline-none focus:ring-0 focus:border-zinc-700/40 font-mono opacity-90 cursor-not-allowed select-none"
                      maxlength="512"
                      readonly
                      disabled
                      title="Apenas o provedor Google Gemini é suportado nesta versão. Endereço bloqueado para edição."
                    />
                    <p class="text-xs dark:text-zinc-500 text-zinc-500 leading-relaxed">🔒 Trava de segurança: endpoint fixo no Google Gemini.</p>
                  </div>
                </div>

                <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 pt-2">
                  <div class="flex flex-wrap items-center gap-2 sm:gap-3">
                    <button
                      type="button"
                      :disabled="!aiCanSave"
                      @click="saveAiConfig()"
                      class="inline-flex items-center gap-2 rounded-xl px-4 py-2.5 text-sm font-semibold text-white bg-indigo-500 hover:bg-indigo-400 disabled:dark:bg-zinc-700/60 disabled:bg-zinc-200 disabled:dark:text-zinc-400 disabled:text-zinc-500 disabled:cursor-not-allowed border border-indigo-500/30 disabled:dark:border-zinc-700/60 disabled:border-zinc-200 shadow-lg shadow-indigo-500/20 disabled:shadow-none transition-all"
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
                      class="inline-flex items-center gap-2 rounded-xl px-4 py-2.5 text-sm font-semibold dark:text-emerald-200 text-emerald-800 bg-emerald-500/10 hover:bg-emerald-500/15 disabled:dark:bg-zinc-800/60 disabled:bg-zinc-100 disabled:dark:text-zinc-500 disabled:text-zinc-500 disabled:cursor-not-allowed border border-emerald-500/30 disabled:dark:border-zinc-700/60 disabled:border-zinc-200 transition-all"
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
                      class="inline-flex items-center gap-2 rounded-xl px-4 py-2.5 text-sm font-semibold dark:text-rose-200 text-rose-800 bg-rose-500/10 hover:bg-rose-500/15 disabled:dark:bg-zinc-800/60 disabled:bg-zinc-100 disabled:dark:text-zinc-500 disabled:text-zinc-500 disabled:cursor-not-allowed border border-rose-500/30 disabled:dark:border-zinc-700/60 disabled:border-zinc-200 transition-all"
                    >
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path>
                      </svg>
                      Remover chave
                    </button>
                  </div>
                  <div class="flex-1 min-w-0"></div>
                </div>

                <div v-if="aiConfigSaveSuccess" role="status" class="rounded-xl border border-emerald-500/30 bg-emerald-500/10 px-4 py-3 text-sm dark:text-emerald-200 text-emerald-800 flex items-start gap-2.5">
                  <svg class="w-5 h-5 shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"></path>
                  </svg>
                  <span>Configuração salva com sucesso!</span>
                </div>
                <div v-if="aiConfigError" role="alert" class="rounded-xl border border-rose-500/30 bg-rose-500/10 px-4 py-3 text-sm dark:text-rose-200 text-rose-800 flex items-start gap-2.5">
                  <svg class="w-5 h-5 shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path>
                  </svg>
                  <div class="space-y-1.5 flex-1">
                    <p class="font-semibold">Algo deu errado</p>
                    <p class="dark:text-rose-100/90 text-rose-900 leading-relaxed whitespace-pre-wrap break-words">{{ aiConfigError }}</p>
                  </div>
                </div>

                <div
                  v-if="aiConfigTestResult"
                  :class="[
                    'rounded-xl border px-4 py-3.5 text-sm flex items-start gap-3',
                    aiConfigTestResult.success
                      ? 'border-emerald-500/30 bg-emerald-500/10 dark:text-emerald-200 text-emerald-800'
                      : 'border-amber-500/30 bg-amber-500/10 dark:text-amber-100 text-amber-900'
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
                    <ul class="grid grid-cols-1 sm:grid-cols-3 gap-x-6 gap-y-1 text-xs dark:text-emerald-50/90 text-emerald-900 *:break-all">
                      <li><span class="opacity-75">Modelo:</span> <code class="font-mono">{{ aiConfigTestResult.normalizedModel || '—' }}</code></li>
                      <li><span class="opacity-75">HTTP Provedor:</span> {{ aiConfigTestResult.httpStatusFromProvider ?? '—' }}</li>
                      <li><span class="opacity-75">Latência:</span> {{ formatLatency(aiConfigTestResult.latencyMs) || '—' }}</li>
                    </ul>
                    <p v-if="!aiConfigTestResult.success && aiConfigTestResult.errorMessage" class="dark:text-amber-50 text-amber-900 leading-relaxed break-words">
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
              <div class="rounded-2xl border dark:bg-zinc-900/50 bg-white dark:border-zinc-800 border-zinc-200 p-5 space-y-3 shadow-xl">
                <div class="flex items-center gap-2 text-xs uppercase tracking-[0.18em] dark:text-zinc-500 text-zinc-500 font-semibold">
                  <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path>
                  </svg>
                  Dicas rápidas
                </div>
                <ol class="space-y-3 text-sm dark:text-zinc-300 text-zinc-700">
                  <li class="flex gap-3">
                    <span class="w-5 h-5 shrink-0 rounded-full bg-indigo-500/20 dark:text-indigo-300 text-indigo-600 border border-indigo-500/30 text-[11px] font-bold flex items-center justify-center">1</span>
                    <span>Crie sua chave gratuita no <a href="https://aistudio.google.com/apikey" target="_blank" class="underline dark:text-indigo-300 text-indigo-600 hover:dark:text-indigo-200 hover:text-indigo-700 font-medium">Google AI Studio</a>.</span>
                  </li>
                  <li class="flex gap-3">
                    <span class="w-5 h-5 shrink-0 rounded-full bg-indigo-500/20 dark:text-indigo-300 text-indigo-600 border border-indigo-500/30 text-[11px] font-bold flex items-center justify-center">2</span>
                    <span>Cole aqui. Recomendamos o modelo <code class="px-1.5 py-0.5 rounded dark:bg-zinc-800/70 bg-zinc-100 dark:border-zinc-700/60 border-zinc-200 font-mono text-xs dark:text-zinc-300 text-zinc-700">gemini-3.5-flash-lite</code> (melhor free tier).</span>
                  </li>
                  <li class="flex gap-3">
                    <span class="w-5 h-5 shrink-0 rounded-full bg-indigo-500/20 dark:text-indigo-300 text-indigo-600 border border-indigo-500/30 text-[11px] font-bold flex items-center justify-center">3</span>
                    <span>Clique em <strong>Salvar</strong> e depois em <strong>Testar conexão</strong> — não precisa enviar nenhuma foto.</span>
                  </li>
                </ol>
              </div>

              <div class="rounded-2xl border dark:bg-zinc-900/50 bg-white dark:border-zinc-800 border-zinc-200 p-5 space-y-3 shadow-xl">
                <div class="flex items-center gap-2 text-xs uppercase tracking-[0.18em] dark:text-zinc-500 text-zinc-500 font-semibold">
                  <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"></path>
                  </svg>
                  Segurança & privacidade
                </div>
                <ul class="space-y-2 text-xs dark:text-zinc-400 text-zinc-600">
                  <li class="flex gap-2">
                    <span class="dark:text-emerald-300 text-emerald-600 mt-0.5">✓</span>
                    <span>Chave gravada criptografada em banco, nunca exposta em APIs GET (apenas 4 últimos dígitos).</span>
                  </li>
                  <li class="flex gap-2">
                    <span class="dark:text-emerald-300 text-emerald-600 mt-0.5">✓</span>
                    <span>Nenhuma chave compartilhada entre restaurantes.</span>
                  </li>
                  <li class="flex gap-2">
                    <span class="dark:text-emerald-300 text-emerald-600 mt-0.5">✓</span>
                    <span>Cota e faturamento diretamente na sua conta Google (você controla limites).</span>
                  </li>
                </ul>
              </div>

              <nuxt-link to="/integracoes" class="block rounded-2xl border border-indigo-500/30 bg-gradient-to-br from-indigo-500/20 dark:to-zinc-900/40 to-indigo-50 p-5 space-y-2 hover:from-indigo-500/30 transition-all">
                <div class="flex items-center gap-2 text-sm font-semibold dark:text-indigo-200 text-indigo-700">
                  <svg class="w-4.5 h-4.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2"></path>
                  </svg>
                  Ir para Importação de dados
                </div>
                <p class="text-xs dark:text-indigo-100/80 text-indigo-900/70">Tudo certo aqui? Volte para a tela de Integrações e importe uma foto do seu caderno de compras.</p>
              </nuxt-link>
            </aside>
          </div>
        </section>

        <!-- Seção Geral -->
        <section v-else-if="activeSectionId === 'general'" class="space-y-6">
          <GeneralSettings :restaurant-id="activeAiRestaurantId" :super-admin="isSuperAdmin" />
        </section>

        <section v-else-if="activeSectionId === 'appearance'" class="space-y-6">
          <AppearanceSettings />
        </section>

        <!-- Seção Notificações -->
        <section v-else-if="activeSectionId === 'notifications'" class="space-y-6">
          <NotificationSettings />
        </section>

        <!-- Seção Segurança -->
        <section v-else-if="activeSectionId === 'security'" class="space-y-6">
          <div class="grid grid-cols-1 xl:grid-cols-3 gap-6">
            <div class="xl:col-span-2 space-y-6">
              <AccountSecuritySettings />

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
              <div class="settings-card">
                <h2>Proteção da conta</h2>
                <p class="settings-hint">Troque a senha regularmente, ative o autenticador e encerre acessos que você não reconhece.</p>
                <p class="settings-hint">Guarde os códigos de recuperação do 2FA. Cada um pode ser usado apenas uma vez.</p>
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
          <SubscriptionSettings :restaurant-id="activeAiRestaurantId" :super-admin="isSuperAdmin" />
        </section>

        <!-- Fallback genérico (não deve cair aqui) -->
        <section v-else class="rounded-2xl border dark:border-zinc-800 border-zinc-200 dark:bg-zinc-900/40 bg-zinc-50 p-10 sm:p-16 text-center space-y-4">
          <div class="mx-auto w-14 h-14 rounded-2xl dark:bg-zinc-800/80 bg-zinc-200 dark:border-zinc-700/60 border-zinc-300 flex items-center justify-center dark:text-zinc-400 text-zinc-500">
            <svg class="w-7 h-7" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path>
            </svg>
          </div>
          <h3 class="text-lg sm:text-xl font-semibold dark:text-white text-zinc-900 tracking-tight">Seção não encontrada</h3>
          <p class="text-sm dark:text-zinc-400 text-zinc-600 max-w-lg mx-auto leading-relaxed">
            Tente selecionar uma opção no menu ao lado.
          </p>
        </section>
      </main>
    </div>

    <footer class="border-t dark:border-zinc-800/70 border-zinc-200 dark:bg-zinc-950/80 bg-white/90 px-4 sm:px-6 py-5 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 text-xs text-zinc-500">
      <p>© 2026 ISM — Intelligence Supply Manager. Todos os direitos reservados.</p>
      <div class="flex items-center gap-4">
        <nuxt-link to="/" class="hover:dark:text-zinc-200 hover:text-zinc-900">Início</nuxt-link>
        <nuxt-link to="/login" class="hover:dark:text-zinc-200 hover:text-zinc-900">Login</nuxt-link>
        <a href="http://localhost:8080/swagger" target="_blank" class="hover:dark:text-zinc-200 hover:text-zinc-900">Swagger API</a>
      </div>
    </footer>

    <div
      v-if="lastCreatedModal.visible"
      class="fixed inset-0 z-[100] flex items-end sm:items-center justify-center px-4 py-6 bg-black/70 backdrop-blur-sm"
      @click.self="lastCreatedModal.visible = false"
    >
      <div class="w-full sm:max-w-xl rounded-2xl border border-amber-500/30 dark:bg-zinc-950 bg-white shadow-2xl overflow-hidden">
        <div class="p-5 sm:p-6 border-b dark:border-zinc-800/80 border-zinc-200 bg-gradient-to-b from-amber-500/5 to-transparent">
          <div class="flex items-start gap-3">
            <div class="w-11 h-11 rounded-xl bg-amber-500/15 border border-amber-500/30 dark:text-amber-300 text-amber-600 flex items-center justify-center shrink-0">
              <svg class="w-5.5 h-5.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"></path>
              </svg>
            </div>
            <div class="space-y-1 flex-1 min-w-0">
              <p class="text-[11px] uppercase tracking-[0.18em] dark:text-amber-300 text-amber-700 font-semibold">Mostrado UMA VEZ · Salve imediatamente</p>
              <h3 class="text-lg sm:text-xl font-bold dark:text-white text-zinc-900 tracking-tight">Token criado: {{ lastCreatedModal.name }}</h3>
              <p class="text-xs sm:text-sm dark:text-zinc-400 text-zinc-600 leading-relaxed">
                Este valor em texto puro <strong class="dark:text-amber-200 text-amber-700 font-semibold">nunca mais será exibido</strong>. Não tem como recuperar depois (só salvamos o hash SHA-256). Copie agora e guarde em local seguro.
              </p>
            </div>
          </div>
        </div>
        <div class="p-5 sm:p-6 space-y-4">
          <div class="rounded-xl border border-dashed border-amber-500/30 bg-amber-500/5 p-4 space-y-3">
            <div class="flex items-center justify-between gap-3">
              <p class="text-[11px] uppercase tracking-[0.18em] dark:text-amber-300 text-amber-700 font-semibold">Token completo · ID #{{ lastCreatedModal.tokenId }}</p>
              <button
                @click="copyPlainToken"
                :class="[
                  'inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-[11px] font-bold border transition-colors',
                  lastCreatedCopied
                    ? 'bg-emerald-500/20 dark:text-emerald-200 text-emerald-800 border-emerald-500/30'
                    : 'bg-amber-500/15 dark:text-amber-200 text-amber-800 hover:bg-amber-500/25 border-amber-500/30'
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
            <div class="rounded-lg dark:bg-black/40 bg-amber-50 border dark:border-zinc-800/80 border-amber-200 p-3">
              <code class="block text-[11px] sm:text-xs dark:text-amber-100 text-amber-900 font-mono break-all leading-relaxed select-all whitespace-pre-wrap">
                {{ lastCreatedModal.plainToken }}
              </code>
            </div>
          </div>
          <div class="flex flex-col sm:flex-row sm:items-center sm:justify-end gap-3 pt-1">
            <button
              @click="lastCreatedModal.visible = false"
              class="inline-flex items-center justify-center gap-2 rounded-xl px-5 py-3 text-sm font-semibold dark:text-zinc-100 text-indigo-900 bg-indigo-500/20 hover:bg-indigo-500/30 border border-indigo-500/30 transition-colors w-full sm:w-auto"
            >
              Já salvei, fechar
            </button>
          </div>
        </div>
      </div>
    </div>

  </div>
</template>
