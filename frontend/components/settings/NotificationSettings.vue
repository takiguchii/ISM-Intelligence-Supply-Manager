<script setup lang="ts">
import { apiClientWithAuth } from "~/services/api/client";
import { settingsService, settingsError, type NotificationPreferences } from "~/services/modules/system/settingsService";
interface Channels { emailConfigured: boolean; whatsAppConfigured: boolean }
interface Scenario { id: string; label: string; description: string }
interface AlertOption { id: number; title: string; category: string }
interface MessagePreview { subject: string; text: string; html: string | null; scenario: string; isTest: boolean; source: string }
interface Delivery { id: number; channel: string; status: string; subject: string; attempts: number; createdAtUtc: string; sentAtUtc: string | null; lastError: string | null; isTest: boolean; scenario: string | null; sourceAlertId: number | null }
const auth = useAuthStore();
const isPlatformAdmin = computed(() => auth.currentUser?.role === 'Admin' && !auth.currentUser.restaurantId);
const preferences = ref<NotificationPreferences | null>(null);
const channels = ref<Channels | null>(null);
const history = ref<Delivery[]>([]);
const scenarios = ref<Scenario[]>([]);
const realAlerts = ref<AlertOption[]>([]);
const selectedScenario = ref("stock-low");
const previewChannel = ref("email");
const preview = ref<MessagePreview | null>(null);
const previewLoading = ref(false);
const previewError = ref("");
let previewVersion = 0;
function scenarioPayload(channel: string) {
  const live = selectedScenario.value.startsWith('alert:');
  return { channel, scenario: live ? 'existing-alert' : selectedScenario.value,
    alertId: live ? Number(selectedScenario.value.slice(6)) : null };
}
async function loadPreview() {
  if (!scenarios.value.length) return;
  const version = ++previewVersion;
  previewLoading.value = true; previewError.value = ""; preview.value = null;
  try {
    const result = await apiClientWithAuth<MessagePreview>("/api/me/notifications/preview", {
      method: "POST", body: scenarioPayload(previewChannel.value)
    });
    if (!disposed && version === previewVersion) preview.value = result;
  } catch (e) { if (!disposed && version === previewVersion) previewError.value = settingsError(e); }
  finally { if (!disposed && version === previewVersion) previewLoading.value = false; }
}
watch([selectedScenario, previewChannel], () => { success.value = ""; void loadPreview(); });
const error = ref("");
const historyError = ref("");
const success = ref("");
const saving = ref(false);
const testing = ref("");
const formElement = ref<HTMLFormElement | null>(null);
const whatsappStatus = ref("");
const whatsappBusy = ref(false);
const whatsappError = ref("");
const qrUrl = ref("");
let pollTimer: ReturnType<typeof setInterval> | undefined;
let disposed = false;
const options = [
  { key: "stock", label: "Estoque", description: "Produtos abaixo do mínimo e risco de ruptura." },
  { key: "suppliers", label: "Fornecedores", description: "Variações de preço e melhores alternativas de compra." },
  { key: "menu", label: "Cardápio", description: "Custos dos pratos e margens desatualizadas." }
] as const;
const statusLabels: Record<string, string> = { Pending: 'Na fila', Sending: 'Enviando', Sent: 'Aceito pelo servidor', Failed: 'Falhou', Cancelled: 'Cancelado' };
const connectionLabels: Record<string, string> = { WORKING: 'Conectado', STOPPED: 'Desconectado', SCAN_QR_CODE: 'Aguardando leitura do QR Code', STARTING: 'Iniciando', FAILED: 'Falha de conexão', NOT_CONFIGURED: 'Aguardando configuração' };
async function loadHistory() {
  try {
    const rows = await apiClientWithAuth<Delivery[]>("/api/me/notifications/deliveries");
    if (!disposed) { history.value = rows; historyError.value = ""; }
  } catch (e) { if (!disposed) historyError.value = settingsError(e); }
}
async function loadWhatsApp() {
  if (!isPlatformAdmin.value || !channels.value?.whatsAppConfigured) return;
  try {
    const result = await apiClientWithAuth<{ status: string }>("/api/notifications/whatsapp/status");
    if (disposed) return;
    whatsappStatus.value = result.status;
    if (result.status === 'WORKING' && qrUrl.value) { URL.revokeObjectURL(qrUrl.value); qrUrl.value = ""; }
  } catch (e) { if (!disposed) whatsappError.value = settingsError(e); }
}
onMounted(async () => {
  try {
    const status = await apiClientWithAuth<Channels>("/api/me/notifications/channels");
    if (disposed) return;
    channels.value = status;
    if (!isPlatformAdmin.value) {
      const [prefs, catalog] = await Promise.all([
        settingsService.notifications(),
        apiClientWithAuth<{ examples: Scenario[]; alerts: AlertOption[] }>("/api/me/notifications/scenarios")
      ]);
      if (disposed) return;
      preferences.value = prefs;
      scenarios.value = catalog.examples;
      realAlerts.value = catalog.alerts;
      await loadPreview();
    }
  } catch (e) { if (!disposed) error.value = settingsError(e); }
  await Promise.all([isPlatformAdmin.value ? Promise.resolve() : loadHistory(), loadWhatsApp()]);
  if (!disposed) pollTimer = setInterval(() => {
    if (!document.hidden) { void loadHistory(); void loadWhatsApp(); }
  }, 5000);
});
onUnmounted(() => {
  disposed = true;
  clearInterval(pollTimer);
  if (qrUrl.value) URL.revokeObjectURL(qrUrl.value);
});
async function save(): Promise<boolean> {
  if (!preferences.value || saving.value || !formElement.value?.reportValidity()) return false;
  saving.value = true; error.value = ""; success.value = "";
  try {
    const payload = { ...preferences.value,
      emailAddress: preferences.value.emailAddress?.trim() || null,
      whatsAppPhone: preferences.value.whatsAppPhone?.trim() || null };
    await settingsService.saveNotifications(payload);
    window.dispatchEvent(new Event("ism-notification-preferences"));
    success.value = "Preferências salvas. Novos alertas serão enviados pelos canais ativados e configurados.";
    return true;
  } catch (e) { error.value = settingsError(e); return false; }
  finally { saving.value = false; }
}
async function test(channel: string) {
  if (testing.value || !await save()) return;
  testing.value = channel;
  try {
    await apiClientWithAuth("/api/me/notifications/test", { method: "POST", body: scenarioPayload(channel) });
    success.value = "Cenário de teste adicionado à fila para o destinatário salvo. Acompanhe o resultado no histórico.";
    await loadHistory();
  } catch (e) { error.value = settingsError(e); }
  finally { testing.value = ""; }
}
async function connectWhatsApp() {
  whatsappBusy.value = true; whatsappError.value = "";
  try {
    await apiClientWithAuth("/api/notifications/whatsapp/connect", { method: "POST" });
    await loadWhatsApp();
  } catch (e) { whatsappError.value = settingsError(e); }
  finally { whatsappBusy.value = false; }
}
async function loadQr() {
  whatsappBusy.value = true; whatsappError.value = "";
  try {
    const blob = await apiClientWithAuth<Blob>("/api/notifications/whatsapp/qr", { responseType: "blob" });
    if (qrUrl.value) URL.revokeObjectURL(qrUrl.value);
    if (!disposed) qrUrl.value = URL.createObjectURL(blob);
  } catch (e) { whatsappError.value = settingsError(e); }
  finally { whatsappBusy.value = false; }
}
</script>
<template>
  <div class="space-y-6">
    <div v-if="!isPlatformAdmin" class="settings-card">
      <h2>Canais de notificação</h2>
      <p class="settings-hint">Receba novos alertas na central, por e-mail ou pelo WhatsApp. O envio externo começa após salvar suas preferências e configurar o serviço.</p>
      <p v-if="error" role="alert" class="text-red-600 dark:text-red-400">{{ error }}</p>
      <p v-if="success" role="status" class="text-emerald-600 dark:text-emerald-400">{{ success }}</p>
      <form v-if="preferences" ref="formElement" @submit.prevent="save">
        <fieldset :disabled="saving || !!testing" class="space-y-5">
          <div class="rounded-xl border border-indigo-300 dark:border-indigo-500/30 bg-indigo-50/50 dark:bg-indigo-500/5 p-4 space-y-4">
            <h3 class="font-semibold">Cenários para apresentação</h3>
            <p class="settings-hint">Escolha uma situação e confira a mensagem antes de enviar. A prévia não dispara notificações. Os exemplos são ilustrativos e não alteram estoque, compras ou preços.</p>
            <label for="notification-scenario">Cenário da mensagem
              <select id="notification-scenario" v-model="selectedScenario">
                <optgroup label="Exemplos ilustrativos"><option v-for="scenario in scenarios" :key="scenario.id" :value="scenario.id">{{ scenario.label }}</option></optgroup>
                <optgroup v-if="realAlerts.length" label="Alertas reais deste restaurante"><option v-for="alert in realAlerts" :key="alert.id" :value="`alert:${alert.id}`">{{ alert.title }}</option></optgroup>
              </select>
            </label>
            <p class="settings-hint">{{ selectedScenario.startsWith('alert:') ? 'Reenvio de teste de um alerta real, limitado ao seu restaurante.' : scenarios.find(s => s.id === selectedScenario)?.description }}</p>
            <label for="notification-preview-channel">Prévia do canal<select id="notification-preview-channel" v-model="previewChannel"><option value="email">E-mail</option><option value="whatsapp">WhatsApp</option></select></label>
            <p v-if="previewLoading" role="status" class="settings-hint">Preparando mensagem…</p>
            <p v-if="previewError" role="alert" class="text-red-600 dark:text-red-400">{{ previewError }}</p>
            <div v-if="preview" class="space-y-3">
              <p class="text-xs font-bold uppercase tracking-wide text-indigo-700 dark:text-indigo-300">{{ preview.source }}</p>
              <p class="text-sm break-words"><strong>Assunto:</strong> {{ preview.subject }}</p>
              <iframe v-if="previewChannel === 'email' && preview.html" :srcdoc="preview.html" title="Prévia do e-mail de notificação" sandbox="" referrerpolicy="no-referrer" class="w-full h-[650px] rounded-xl border border-zinc-200 bg-white" />
              <pre v-else class="whitespace-pre-wrap break-words rounded-xl border border-emerald-500/30 bg-emerald-50 dark:bg-emerald-950/30 p-4 text-sm leading-relaxed font-sans">{{ preview.text }}</pre>
            </div>
            <p class="settings-hint">Os botões de teste abaixo enviam o cenário selecionado para o destinatário de cada canal. Cada envio fica identificado como TESTE.</p>
          </div>
          <div class="rounded-xl border border-zinc-200 dark:border-zinc-800 p-4 space-y-4">
            <label class="!flex-row items-center justify-between"><span>E-mail <span class="block settings-hint">{{ channels?.emailConfigured ? 'Servidor SMTP configurado' : 'Aguardando configuração do SMTP pelo administrador' }}</span></span><input v-model="preferences.emailEnabled" type="checkbox" class="h-5 w-5 accent-indigo-600" /></label>
            <label for="notification-email">E-mail destinatário<input id="notification-email" v-model="preferences.emailAddress" type="email" maxlength="150" :required="preferences.emailEnabled" /></label>
            <button type="button" class="settings-secondary" :disabled="!channels?.emailConfigured || !preferences.emailEnabled || !!testing" @click="test('email')">{{ testing === 'email' ? 'Solicitando…' : 'Enviar cenário por e-mail' }}</button>
          </div>
          <div class="rounded-xl border border-zinc-200 dark:border-zinc-800 p-4 space-y-4">
            <label class="!flex-row items-center justify-between"><span>WhatsApp <span class="block settings-hint">{{ channels?.whatsAppConfigured ? 'Serviço configurado; o número remetente precisa estar conectado' : 'Aguardando configuração e conexão do número remetente' }}</span></span><input v-model="preferences.whatsAppEnabled" type="checkbox" class="h-5 w-5 accent-indigo-600" /></label>
            <label for="notification-whatsapp">WhatsApp destinatário (país + DDD + número)<input id="notification-whatsapp" v-model="preferences.whatsAppPhone" type="tel" pattern="\+[1-9][0-9]{7,14}" maxlength="16" placeholder="+5511999999999" :required="preferences.whatsAppEnabled" /></label>
            <button type="button" class="settings-secondary" :disabled="!channels?.whatsAppConfigured || !preferences.whatsAppEnabled || !!testing" @click="test('whatsapp')">{{ testing === 'whatsapp' ? 'Solicitando…' : 'Enviar cenário por WhatsApp' }}</button>
            <p class="settings-hint">O administrador do servidor conecta o remetente pelo QR Code. Cada usuário escolhe aqui o número em que deseja receber os alertas.</p>
          </div>
          <h3 class="font-semibold">Tipos de alerta</h3>
          <p class="settings-hint">Estas escolhas valem para a central e para os canais externos ativados.</p>
          <label v-for="option in options" :key="option.key" class="!flex-row items-center justify-between rounded-xl border border-zinc-200 dark:border-zinc-800 p-4">
            <span>{{ option.label }}<span class="block settings-hint mt-1">{{ option.description }}</span></span>
            <input v-model="preferences[option.key]" type="checkbox" class="h-5 w-5 accent-indigo-600" />
          </label>
          <button class="settings-primary" type="submit" :disabled="saving">{{ saving ? 'Salvando…' : 'Salvar preferências' }}</button>
        </fieldset>
      </form>
      <p v-else-if="!error" role="status">Carregando preferências…</p>
    </div>
    <div v-if="isPlatformAdmin && channels?.whatsAppConfigured" class="settings-card">
      <h2>Conectar remetente do WhatsApp</h2>
      <p class="settings-hint">Esta conexão é compartilhada pelo servidor. No celular do remetente, abra WhatsApp → Dispositivos conectados → Conectar dispositivo e escaneie o QR Code.</p>
      <p>Status: {{ connectionLabels[whatsappStatus] || whatsappStatus || 'Consultando…' }}</p>
      <p v-if="whatsappError" role="alert" class="text-red-600 dark:text-red-400">{{ whatsappError }}</p>
      <div class="flex flex-wrap gap-3"><button type="button" class="settings-primary" :disabled="whatsappBusy || whatsappStatus === 'WORKING'" @click="connectWhatsApp">Iniciar conexão</button><button type="button" class="settings-secondary" :disabled="whatsappBusy || whatsappStatus !== 'SCAN_QR_CODE'" @click="loadQr">Mostrar / atualizar QR Code</button></div>
      <img v-if="qrUrl" :src="qrUrl" alt="QR Code para conectar o WhatsApp remetente" class="w-64 h-64 rounded-lg bg-white p-2" />
    </div>
    <div v-if="!isPlatformAdmin" class="settings-card">
      <h2>Histórico de envios</h2>
      <p class="settings-hint">“Aceito pelo servidor” confirma o envio ao SMTP ou WhatsApp; não confirma leitura nem chegada à caixa de entrada.</p>
      <p v-if="historyError" role="alert" class="text-red-600 dark:text-red-400">{{ historyError }}</p>
      <p v-if="!history.length && !historyError" class="settings-hint">Nenhum envio registrado. Ative um canal e envie um teste depois da configuração.</p>
      <ul class="divide-y divide-zinc-200 dark:divide-zinc-800"><li v-for="delivery in history" :key="delivery.id" class="py-4 space-y-1"><p class="font-medium">{{ delivery.subject }}</p><p v-if="delivery.isTest" class="text-xs font-semibold text-indigo-600 dark:text-indigo-300">Envio de demonstração{{ delivery.sourceAlertId ? ' · alerta real do restaurante' : ' · cenário ilustrativo' }}</p><p class="settings-hint">{{ delivery.channel === 'email' ? 'E-mail' : 'WhatsApp' }} · {{ statusLabels[delivery.status] || delivery.status }} · {{ new Date(delivery.createdAtUtc).toLocaleString('pt-BR') }} · {{ delivery.attempts }} tentativa(s)</p><p v-if="delivery.lastError" class="text-sm text-amber-700 dark:text-amber-300">{{ delivery.lastError }}</p></li></ul>
    </div>
  </div>
</template>
