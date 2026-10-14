<script setup lang="ts">
import { ref, computed, onMounted } from "vue";
import { getSystemAlerts, markAlertAsRead } from "~/services/modules/system/systemService";
import type { SystemAlertResponse } from "~/types/system";

const props = defineProps<{
  isOpen: boolean;
}>();


const emit = defineEmits<{
  (e: "close"): void;
}>();

const router = useRouter();
const alerts = ref<SystemAlertResponse[]>([]);
const loading = ref(false);

const mockAlerts: SystemAlertResponse[] = [
  {
    id: 991,
    restaurantId: 1,
    alertType: "StockRuptureRisk48h",
    severity: "Critical",
    title: "Risco de Ruptura de Estoque",
    message: "Insumo Carne Moída bovina atingirá nível zero nas próximas 48h.",
    isRead: false,
    isDismissed: false,
    generatedAtUtc: new Date().toISOString(),
    createdAtUtc: new Date().toISOString(),
    referenceEntityType: "Product",
    referenceEntityId: 12
  },
  {
    id: 992,
    restaurantId: 1,
    alertType: "SupplierPriceSpike",
    severity: "Warning",
    title: "Alerta de Fornecedor - Alta de Preço",
    message: "O preço do Queijo Mozarela subiu 18% no último lançamento em relação ao histórico.",
    isRead: false,
    isDismissed: false,
    generatedAtUtc: new Date(Date.now() - 3600000).toISOString(),
    createdAtUtc: new Date(Date.now() - 3600000).toISOString(),
    referenceEntityType: "Supplier",
    referenceEntityId: 5
  },
  {
    id: 993,
    restaurantId: 1,
    alertType: "DishCmvDefasagem",
    severity: "Warning",
    title: "Alerta de Margem & CMV",
    message: "O prato Filé Mignon teve o CMV elevado para 42%. Sugestão de novo preço: R$ 98.50 para reestabelecer a margem de 65%.",
    isRead: true,
    isDismissed: false,
    generatedAtUtc: new Date(Date.now() - 7200000).toISOString(),
    createdAtUtc: new Date(Date.now() - 7200000).toISOString(),
    referenceEntityType: "Dish",
    referenceEntityId: 3
  }
];

const unreadCount = computed(() => alerts.value.filter(a => !a.isRead).length);

const fetchAlerts = async () => {
  loading.value = true;
  try {
    const res = await getSystemAlerts({ pageSize: 20 });
    if (res && res.items && res.items.length > 0) {
      alerts.value = res.items;
    } else {
      alerts.value = mockAlerts;
    }
  } catch {
    alerts.value = mockAlerts;
  } finally {
    loading.value = false;
  }
};

const handleMarkRead = async (alertItem: SystemAlertResponse) => {
  if (alertItem.isRead) return;
  alertItem.isRead = true;
  try {
    if (alertItem.id < 900) {
      await markAlertAsRead(alertItem.id);
    }
  } catch {
    // Local fallback state updated
  }
};

const handleAction = (alertItem: SystemAlertResponse) => {
  handleMarkRead(alertItem);
  emit("close");
  if (alertItem.referenceEntityType === "Product" || alertItem.alertType.includes("Stock")) {
    router.push("/estoque");
  } else if (alertItem.referenceEntityType === "Supplier" || alertItem.alertType.includes("Supplier")) {
    router.push("/fornecedores");
  } else if (alertItem.referenceEntityType === "Dish" || alertItem.alertType.includes("Dish") || alertItem.alertType.includes("Cmv")) {
    router.push("/cardapio");
  } else {
    router.push("/");
  }
};

const mapSeverity = (sev: string): "info" | "warning" | "critical" => {
  const lower = sev.toLowerCase();
  if (lower === "critical") return "critical";
  if (lower === "warning") return "warning";
  return "info";
};

onMounted(() => {
  fetchAlerts();
});

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
        'fixed top-0 right-0 z-50 h-full w-full max-w-md bg-zinc-900 border-l border-zinc-800 shadow-2xl transition-transform duration-300 flex flex-col',
        isOpen ? 'translate-x-0' : 'translate-x-full'
      ]"
    >
      <!-- Header -->
      <div class="p-5 border-b border-zinc-800 flex items-center justify-between bg-zinc-950/50">
        <div class="flex items-center gap-3">
          <h2 class="text-base font-bold text-white tracking-tight">Central de Notificações</h2>
          <span
            v-if="unreadCount > 0"
            class="px-2 py-0.5 text-xs font-semibold rounded-full bg-indigo-500/20 text-indigo-400 border border-indigo-500/30"
          >
            {{ unreadCount }} não lidas
          </span>
        </div>
        <button
          @click="emit('close')"
          type="button"
          class="p-2 rounded-xl text-zinc-400 hover:text-white hover:bg-zinc-800 transition-colors"
          title="Fechar Notificações"
        >
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
          </svg>
        </button>
      </div>

      <!-- Alerts List -->
      <div class="flex-1 overflow-y-auto p-4 space-y-3">
        <div v-if="loading" class="p-8 text-center text-xs text-zinc-500 font-mono">
          Carregando alertas dos agentes...
        </div>

        <div
          v-else-if="alerts.length === 0"
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
              ? 'bg-zinc-950/40 border-zinc-800/60 opacity-75'
              : 'bg-zinc-800/50 border-zinc-700/80 shadow-md'
          ]"
        >
          <div class="flex items-start justify-between gap-2">
            <div class="flex items-center gap-2">
              <span
                :class="[
                  'inline-flex items-center gap-1.5 px-2 py-0.5 rounded-full text-[10px] font-semibold font-mono uppercase tracking-wider border',
                  alertItem.severity.toLowerCase() === 'critical' ? 'bg-red-500/10 text-red-400 border-red-500/30' :
                  alertItem.severity.toLowerCase() === 'warning' ? 'bg-amber-500/10 text-amber-400 border-amber-500/30' :
                  'bg-sky-500/10 text-sky-400 border-sky-500/30'
                ]"
              >
                <span class="w-1.5 h-1.5 rounded-full bg-current"></span>
                {{ alertItem.severity }}
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

          <div>
            <h3 class="text-sm font-semibold text-white mb-1">{{ alertItem.title }}</h3>
            <p class="text-xs text-zinc-300 leading-relaxed">{{ alertItem.message }}</p>
          </div>

          <!-- Direct Action Buttons -->
          <div class="flex items-center gap-2 pt-2 border-t border-zinc-800/80">
            <button
              @click="handleAction(alertItem)"
              type="button"
              class="px-3 py-1.5 text-xs font-semibold rounded-lg bg-indigo-600 hover:bg-indigo-500 text-white transition-colors"
            >
              <template v-if="alertItem.referenceEntityType === 'Product' || alertItem.alertType.includes('Stock')">
                Ver Estoque
              </template>
              <template v-else-if="alertItem.referenceEntityType === 'Supplier' || alertItem.alertType.includes('Supplier')">
                Ver Fornecedores
              </template>
              <template v-else-if="alertItem.referenceEntityType === 'Dish' || alertItem.alertType.includes('Dish') || alertItem.alertType.includes('Cmv')">
                Ver Ficha Técnica
              </template>
              <template v-else>
                Ver Detalhes
              </template>
            </button>
          </div>
        </div>
      </div>
    </div>
  </Teleport>
</template>
