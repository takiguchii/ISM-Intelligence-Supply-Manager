<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from "vue";
import type { SystemAlertResponse } from "~/types/system";
import { useSystemAlertCenter } from "~/composables/useSystemAlertCenter";
import { useAssistantUi } from "~/composables/useAssistantUi";
import AgentMascot2D from "~/components/assistant/AgentMascot2D.vue";
import { agentForAlert, alertTypeName, attentionForSeverity, moodForAlert, severityForAlert } from "~/types/agentExperience";

const props = defineProps<{
  isOpen: boolean;
}>();
const isOpen = computed(() => props.isOpen);


const emit = defineEmits<{
  (e: "close"): void;
}>();

const router = useRouter();
const center = useSystemAlertCenter();
const assistantUi = useAssistantUi();
const alerts = center.alerts;
const loading = center.loading;
const errorMessage = center.error;
const unreadCount = center.unreadCount;
const clearingAll = ref(false);
const clearAllError = ref("");
const confirmClear = ref(false);
const auth = useAuthStore();
const canDismiss = computed(() => !!auth.currentUser?.restaurantId && ["Admin", "Manager"].includes(auth.userRole ?? ""));
const fetchAlerts = () => center.refresh(false);

const handleMarkRead = async (alertItem: SystemAlertResponse): Promise<boolean> => {
  if (alertItem.isRead) return true;
  errorMessage.value = "";
  try {
    const updated = await center.markRead(alertItem);
    Object.assign(alertItem, updated);
    return true;
  } catch (error) {
    console.error("Erro ao marcar alerta como lido:", error);
    errorMessage.value = "Não foi possível marcar o alerta como lido. Tente novamente.";
    return false;
  }
};

const handleAction = async (alertItem: SystemAlertResponse) => {
  if (!(await handleMarkRead(alertItem))) return;
  emit("close");
  const alertType = alertTypeName(alertItem.alertType);
  if (alertItem.referenceEntityType === "Product" || alertType.includes("Stock")) {
    router.push("/estoque");
  } else if (alertItem.referenceEntityType === "Supplier" || alertType.includes("Supplier")) {
    router.push("/fornecedores");
  } else if (alertItem.referenceEntityType === "Dish" || alertType.includes("Dish") || alertType.includes("Cmv")) {
    router.push("/cardapio");
  } else {
    router.push("/");
  }
};

const handleDismiss = async (alertItem: SystemAlertResponse) => {
  try { await center.dismiss(alertItem); }
  catch { errorMessage.value = "Não foi possível descartar o alerta. Tente novamente."; }
};

const handleClearAll = async () => {
  if (!canDismiss.value || clearingAll.value || !alerts.value.length) return;
  clearingAll.value = true;
  clearAllError.value = "";
  try { await center.dismissAll(); confirmClear.value = false; }
  catch { clearAllError.value = "Não foi possível limpar os alertas. Tente novamente."; }
  finally { clearingAll.value = false; }
};

const handleConversation = (alertItem: SystemAlertResponse) => {
  emit("close");
  assistantUi.openChat(alertItem);
};

const handleKeydown = (event: KeyboardEvent) => { if (event.key === "Escape" && props.isOpen) emit("close"); };

watch(() => props.isOpen, open => {
  if (open) void center.ensureLoaded();
  else { confirmClear.value = false; clearAllError.value = ""; }
});
onMounted(() => { void center.ensureLoaded(); window.addEventListener("keydown", handleKeydown); });
onBeforeUnmount(() => window.removeEventListener("keydown", handleKeydown));

defineExpose({
  unreadCount,
  fetchAlerts
});
</script>

<template>
  <Teleport to="body">
    <!-- Backdrop -->
    <div
      v-if="isOpen"
      @click="emit('close')"
      class="fixed inset-0 z-40 bg-black/60 backdrop-blur-sm transition-opacity"
    ></div>

    <!-- Panel Drawer -->
    <div
      :class="[
        'fixed top-0 right-0 z-50 h-full w-full max-w-md bg-white dark:bg-zinc-900 border-l border-zinc-200 dark:border-zinc-800 shadow-2xl transition-transform duration-300 flex flex-col',
        isOpen ? 'translate-x-0' : 'translate-x-full'
      ]"
      role="dialog"
      aria-modal="true"
      aria-labelledby="notification-drawer-title"
    >
      <!-- Header -->
      <div class="p-4 sm:p-5 border-b border-zinc-200 dark:border-zinc-800 flex items-center justify-between gap-3 bg-zinc-50/80 dark:bg-zinc-950/50">
        <div class="min-w-0">
          <h2 id="notification-drawer-title" class="text-base font-bold text-zinc-900 dark:text-white tracking-tight">Notificações Inteligentes</h2>
          <span
            v-if="unreadCount > 0"
            class="mt-0.5 block text-xs font-medium text-indigo-700 dark:text-indigo-300"
          >
            {{ unreadCount }} não lidas
          </span>
        </div>
        <div class="flex shrink-0 items-center gap-2">
        <button v-if="canDismiss && alerts.length" type="button" class="rounded-lg border border-zinc-200 px-2.5 py-2 text-[11px] font-semibold text-zinc-600 hover:bg-zinc-100 disabled:opacity-50 dark:border-zinc-700 dark:text-zinc-300 dark:hover:bg-zinc-800" :disabled="clearingAll" @click="confirmClear = !confirmClear">Limpar todas</button>
        <button
          @click="emit('close')"
          type="button"
          class="p-2 rounded-xl text-zinc-500 hover:text-zinc-900 hover:bg-zinc-100 dark:text-zinc-400 dark:hover:text-white dark:hover:bg-zinc-800 transition-colors"
          title="Fechar Notificações"
          aria-label="Fechar notificações"
        >
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
          </svg>
        </button>
        </div>
      </div>

      <!-- Alerts List -->
      <div class="flex-1 overflow-y-auto p-4 space-y-3">
        <div v-if="confirmClear" class="space-y-3 rounded-xl border border-zinc-200 p-3 text-sm dark:border-zinc-700">
          <p>Descartar todas as notificações deste restaurante, incluindo as não lidas?</p>
          <div class="flex flex-wrap gap-2">
            <button type="button" class="rounded-lg bg-red-700 px-3 py-2 text-xs font-semibold text-white disabled:opacity-50" :disabled="clearingAll" @click="handleClearAll">{{ clearingAll ? 'Limpando…' : 'Confirmar limpeza' }}</button>
            <button type="button" class="rounded-lg px-3 py-2 text-xs font-semibold" :disabled="clearingAll" @click="confirmClear = false">Cancelar</button>
          </div>
        </div>
        <p v-if="clearAllError" role="alert" class="rounded-lg border border-red-200 bg-red-50 p-3 text-xs text-red-700 dark:border-red-900/60 dark:bg-red-950/30 dark:text-red-300">{{ clearAllError }}</p>
        <div v-if="loading" class="p-8 text-center text-xs text-zinc-500 font-mono">
          Carregando alertas dos agentes...
        </div>

        <div v-if="errorMessage" role="alert" class="p-4 text-sm text-red-400">
          {{ errorMessage }}
          <button type="button" class="ml-2 underline" @click="fetchAlerts">Tentar novamente</button>
        </div>

        <div
          v-if="!loading && !errorMessage && alerts.length === 0"
          class="p-8 text-center text-sm text-zinc-400"
        >
          Nenhum alerta pendente no momento.
        </div>

        <div
          v-for="alertItem in alerts"
          :key="alertItem.id"
          :class="[
            'p-4 rounded-xl border transition-all duration-200 flex flex-col gap-3',
            alertItem.isRead
              ? 'bg-zinc-50 border-zinc-200 opacity-75 dark:bg-zinc-950/40 dark:border-zinc-800/60'
              : 'bg-white border-zinc-200 shadow-md dark:bg-zinc-800/50 dark:border-zinc-700/80'
          ]"
        >
          <div class="flex items-start justify-between gap-2">
            <div class="flex items-center gap-2">
              <span
                :class="[
                  'inline-flex items-center gap-1.5 px-2 py-0.5 rounded-full text-[10px] font-semibold font-mono uppercase tracking-wider border',
                  severityForAlert(alertItem) === 'critical' ? 'bg-red-500/10 text-red-400 border-red-500/30' :
                  severityForAlert(alertItem) === 'warning' ? 'bg-amber-500/10 text-amber-400 border-amber-500/30' :
                  'bg-sky-500/10 text-sky-400 border-sky-500/30'
                ]"
              >
                <span class="w-1.5 h-1.5 rounded-full bg-current"></span>
                {{ severityForAlert(alertItem) }}
              </span>
              <span class="text-[11px] font-mono text-zinc-400">
                {{ new Date(alertItem.createdAtUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) }}
              </span>
            </div>
            <button
              v-if="!alertItem.isRead"
              @click="handleMarkRead(alertItem)"
              class="text-[10px] font-medium text-indigo-400 hover:text-indigo-300 transition-colors"
            >
              Marcar lida
            </button>
          </div>

          <div class="flex items-start gap-3">
            <AgentMascot2D :agent="agentForAlert(alertItem)" :mood="moodForAlert(alertItem)" size="sm" :attention-level="attentionForSeverity(severityForAlert(alertItem))" />
            <div class="min-w-0 flex-1">
              <p class="mb-1 text-[10px] font-bold uppercase tracking-wider text-zinc-500">{{ agentForAlert(alertItem) === 'stock' ? 'Estoque' : agentForAlert(alertItem) === 'pricing' ? 'Precificação' : 'Fornecedores' }}</p>
              <h3 class="text-sm font-semibold text-zinc-900 dark:text-white">{{ alertItem.title }}</h3>
              <p class="mt-1 text-xs leading-relaxed text-zinc-600 dark:text-zinc-300">{{ alertItem.message }}</p>
            </div>
          </div>

          <!-- Direct Action Buttons -->
          <div class="flex flex-wrap items-center gap-2 pt-2 border-t border-zinc-800/80">
            <button
              @click="handleAction(alertItem)"
              type="button"
              class="px-3 py-1.5 text-xs font-semibold rounded-lg bg-indigo-600 hover:bg-indigo-500 text-white transition-colors"
            >
              <template v-if="alertItem.referenceEntityType === 'Product' || alertTypeName(alertItem.alertType).includes('Stock')">
                Ver Estoque
              </template>
              <template v-else-if="alertItem.referenceEntityType === 'Supplier' || alertTypeName(alertItem.alertType).includes('Supplier')">
                Ver Fornecedores
              </template>
              <template v-else-if="alertItem.referenceEntityType === 'Dish' || alertTypeName(alertItem.alertType).includes('Dish') || alertTypeName(alertItem.alertType).includes('Cmv')">
                Ver Ficha Técnica
              </template>
              <template v-else>
                Ver Detalhes
              </template>
            </button>
            <button type="button" class="px-3 py-1.5 text-xs font-semibold rounded-lg border border-zinc-700 text-zinc-300 hover:bg-zinc-800" @click="handleConversation(alertItem)">Conversar</button>
            <button v-if="canDismiss" type="button" class="ml-auto px-2 py-1.5 text-[11px] text-zinc-400 hover:text-white disabled:opacity-50" :disabled="clearingAll" @click="handleDismiss(alertItem)">Descartar</button>
          </div>
        </div>
      </div>
    </div>
  </Teleport>
</template>
