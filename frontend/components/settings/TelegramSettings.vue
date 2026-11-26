<script setup lang="ts">
import { telegramService, type TelegramStatus, type TelegramLink } from '~/services/modules/telegram/telegramService';
const props = withDefaults(defineProps<{ embedded?: boolean }>(), { embedded: false });
const emit = defineEmits<{ connection: [connected: boolean] }>();
const status = ref<TelegramStatus | null>(null);
const link = ref<TelegramLink | null>(null);
const busy = ref(false);
const botId = ref('');
const telegramUserId = ref('');
const error = ref('');
const success = ref('');
let timer: ReturnType<typeof setInterval> | undefined;
let disposed = false;
let polling = false;
function stopPolling() { if (timer) clearInterval(timer); timer = undefined; }
async function refresh() {
  const result = await telegramService.status();
  if (disposed) return;
  status.value = result;
  emit('connection', result.connected);
  if (!botId.value && result.botId) botId.value = String(result.botId);
  if (result.telegramUserId) telegramUserId.value = String(result.telegramUserId);
  if (result.connected) { link.value = null; stopPolling(); }
}
async function run(action: () => Promise<void>) {
  if (busy.value) return;
  busy.value = true; error.value = ''; success.value = '';
  try { await action(); }
  catch (cause: any) { if (!disposed) error.value = cause?.data?.detail ?? cause?.data?.message ?? 'Não foi possível atualizar o Telegram. Tente novamente.'; }
  finally { if (!disposed) busy.value = false; }
}
async function connect() {
  if (!/^\d+$/.test(botId.value) || !/^\d+$/.test(telegramUserId.value) || !Number.isSafeInteger(Number(botId.value)) || !Number.isSafeInteger(Number(telegramUserId.value)) || Number(botId.value) <= 0 || Number(telegramUserId.value) <= 0) {
    error.value = 'Informe os IDs numéricos do bot e do seu usuário do Telegram.'; return;
  }
  const result = await telegramService.connect(Number(botId.value), Number(telegramUserId.value));
  if (disposed) return;
  link.value = result;
  stopPolling();
  timer = setInterval(async () => {
    if (polling || disposed || busy.value) return;
    if (Date.now() >= Date.parse(result.expiresAtUtc)) {
      stopPolling(); link.value = null; error.value = 'O link expirou. Gere outro para conectar.'; return;
    }
    polling = true;
    try { await refresh(); }
    catch { if (!disposed) error.value = 'Não foi possível verificar a conexão. Use Atualizar.'; }
    finally { polling = false; }
  }, 5000);
}
async function disconnect() {
  await telegramService.disconnect();
  link.value = null; stopPolling(); await refresh(); success.value = 'Telegram desconectado. Novos alertas não serão enviados.';
}
async function sendTest() { await telegramService.test(); success.value = 'Mensagem de teste adicionada à fila. Confira o Telegram em alguns segundos.'; }
async function toggleAlerts() {
  await telegramService.alerts(!status.value?.alertsEnabled); await refresh();
  success.value = status.value?.alertsEnabled ? 'Alertas ativados.' : 'Alertas pausados.';
}
onMounted(() => run(refresh));
onBeforeUnmount(() => { disposed = true; stopPolling(); });
</script>

<template>
  <section :class="props.embedded ? 'space-y-4' : 'rounded-2xl border border-zinc-200 bg-white p-5 dark:border-zinc-800 dark:bg-zinc-900/40 sm:p-6'" :aria-label="props.embedded ? 'Configuração do Telegram' : undefined" :aria-labelledby="props.embedded ? undefined : 'telegram-title'">
    <div v-if="!props.embedded" class="flex flex-wrap items-center justify-between gap-3">
      <div>
        <h2 id="telegram-title" class="text-lg font-semibold">Telegram</h2>
        <p class="mt-1 text-sm text-zinc-500 dark:text-zinc-400">Converse com o assistente, consulte pedidos e cardápio e receba alertas do seu restaurante.</p>
      </div>
      <span v-if="status" role="status" class="rounded-full px-3 py-1 text-xs font-medium" :class="status.connected ? 'bg-emerald-50 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-300' : 'bg-zinc-100 text-zinc-600 dark:bg-zinc-800 dark:text-zinc-300'">{{ status.connected ? 'Conectado' : 'Não conectado' }}</span>
    </div>
    <el-alert v-if="error" class="mt-4" type="error" :title="error" :closable="false" show-icon />
    <el-alert v-if="success" class="mt-4" type="success" :title="success" :closable="false" show-icon />
    <p v-if="status && !status.configured" class="mt-4 text-sm">O administrador precisa configurar o bot no servidor para habilitar esta integração.</p>
    <template v-if="status?.configured || status?.connected">
      <p class="mt-4 text-sm">Bot: <strong>@{{ status.botUsername }}</strong>. A conexão é individual e usa suas permissões da ISM.</p>
      <form id="telegram-identity" class="mt-5 grid gap-4 sm:grid-cols-2" @submit.prevent="run(connect)">
        <div><label for="telegram-bot-id" class="mb-2 block text-sm font-semibold">ID do bot</label><input type="text" required pattern="[0-9]+" maxlength="16" class="w-full rounded-lg border border-zinc-300 bg-white px-3 py-2.5 text-base text-zinc-900 outline-none transition focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/20 disabled:opacity-60 dark:border-zinc-700 dark:bg-zinc-950 dark:text-zinc-100 sm:text-sm" id="telegram-bot-id" v-model="botId" inputmode="numeric" placeholder="Ex.: 9208918507" :disabled="busy || status.connected || !!link" aria-describedby="telegram-bot-help" /><p id="telegram-bot-help" class="mt-2 text-xs text-zinc-500 dark:text-zinc-400">Preenchido com o bot configurado. Este número não é o token secreto.</p></div>
        <div><label for="telegram-user-id" class="mb-2 block text-sm font-semibold">Seu ID de usuário no Telegram</label><input type="text" required pattern="[0-9]+" maxlength="16" class="w-full rounded-lg border border-zinc-300 bg-white px-3 py-2.5 text-base text-zinc-900 outline-none transition focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/20 disabled:opacity-60 dark:border-zinc-700 dark:bg-zinc-950 dark:text-zinc-100 sm:text-sm" id="telegram-user-id" v-model="telegramUserId" inputmode="numeric" placeholder="Envie /id ao bot para consultar" :disabled="busy || status.connected || !!link" aria-describedby="telegram-user-help" /><p id="telegram-user-help" class="mt-2 text-xs text-zinc-500 dark:text-zinc-400">Envie /id para @{{ status.botUsername }}. Confirme o vínculo pela mesma conta do Telegram.</p></div>
      </form>
      <div class="mt-4 flex flex-wrap gap-3">
        <button v-if="!status.connected" type="submit" form="telegram-identity" :disabled="busy" :aria-busy="busy" class="rounded-lg bg-indigo-600 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-indigo-500 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-500 disabled:opacity-60">{{ link ? 'Gerar outro link' : 'Conectar Telegram' }}</button>
        <template v-else>
          <button type="button" class="rounded-lg border border-zinc-300 bg-white px-4 py-2.5 text-sm font-medium text-zinc-700 transition hover:bg-zinc-50 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-500 disabled:opacity-50 dark:border-zinc-700 dark:bg-zinc-900 dark:text-zinc-200 dark:hover:bg-zinc-800" :aria-busy="busy" :disabled="busy || (!status.configured && !status.alertsEnabled)" @click="run(toggleAlerts)">{{ status.alertsEnabled ? 'Pausar alertas do Telegram' : 'Ativar alertas do Telegram' }}</button>
          <button type="button" class="rounded-lg border border-zinc-300 bg-white px-4 py-2.5 text-sm font-medium text-zinc-700 transition hover:bg-zinc-50 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-500 disabled:opacity-50 dark:border-zinc-700 dark:bg-zinc-900 dark:text-zinc-200 dark:hover:bg-zinc-800" :disabled="busy || !status.alertsEnabled || !status.configured" @click="run(sendTest)">Enviar teste pelo Telegram</button>
          <button type="button" class="rounded-lg border border-zinc-300 bg-white px-4 py-2.5 text-sm font-medium text-zinc-700 transition hover:bg-zinc-50 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-500 disabled:opacity-50 dark:border-zinc-700 dark:bg-zinc-900 dark:text-zinc-200 dark:hover:bg-zinc-800" :disabled="busy" @click="run(disconnect)">Desconectar Telegram</button>
        </template>
        <button type="button" class="rounded-lg border border-zinc-300 bg-white px-4 py-2.5 text-sm font-medium text-zinc-700 transition hover:bg-zinc-50 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-500 disabled:opacity-50 dark:border-zinc-700 dark:bg-zinc-900 dark:text-zinc-200 dark:hover:bg-zinc-800" v-if="link" :disabled="busy" @click="run(disconnect)">Cancelar vínculo</button>
        <button type="button" class="rounded-lg border border-zinc-300 bg-white px-4 py-2.5 text-sm font-medium text-zinc-700 transition hover:bg-zinc-50 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-500 disabled:opacity-50 dark:border-zinc-700 dark:bg-zinc-900 dark:text-zinc-200 dark:hover:bg-zinc-800" :disabled="busy" @click="run(refresh)">Atualizar conexão</button>
      </div>
      <div v-if="link" class="mt-4 rounded-xl bg-indigo-50 p-4 dark:bg-indigo-950/40">
        <p class="text-sm">Abra o Telegram e pressione <strong>Iniciar</strong> para autorizar o vínculo. Este link é pessoal, vale por cinco minutos e só pode ser usado uma vez.</p>
        <a :href="link.url" target="_blank" rel="noopener noreferrer" class="mt-3 inline-block font-semibold text-indigo-600 underline dark:text-indigo-300">Abrir bot e conectar minha conta</a>
      </div>
      <details class="mt-5 rounded-xl bg-zinc-50 p-4 dark:bg-zinc-950/50"><summary class="cursor-pointer text-sm font-semibold">O que posso fazer pelo Telegram?</summary><ul class="mt-3 space-y-2 text-sm text-zinc-600 dark:text-zinc-400"><li>Consultar pedidos, cardápio e alertas pelo painel do bot.</li><li>Conversar com o assistente e os especialistas através do orquestrador ISM.</li><li>Receber alertas de estoque, fornecedores e cardápio.</li></ul></details>
      <p class="mt-4 text-sm text-zinc-500 dark:text-zinc-400">Os alertas começam pausados. Ao ativá-los, valem suas preferências de estoque, fornecedores e cardápio. Use /ajuda no bot para ver os comandos. Revise alterações operacionais no ISM.</p>
    </template>
  </section>
</template>
