<script setup lang="ts">
import { telegramService, type TelegramStatus } from '~/services/modules/telegram/telegramService';

const props = withDefaults(
  defineProps<{
    embedded?: boolean;
    restaurantId?: number | null;
    superAdmin?: boolean;
  }>(),
  {
    embedded: false,
    restaurantId: null,
    superAdmin: false
  }
);

const emit = defineEmits<{
  connection: [connected: boolean];
}>();

const status = ref<TelegramStatus | null>(null);
const telegramUserId = ref('');
const busy = ref(false);
const testing = ref(false);
const error = ref('');
const success = ref('');
let disposed = false;

async function refresh() {
  if (props.superAdmin && (!props.restaurantId || props.restaurantId <= 0)) {
    status.value = null;
    telegramUserId.value = '';
    emit('connection', false);
    return;
  }

  try {
    const result = await telegramService.status(props.restaurantId, props.superAdmin);
    if (disposed) return;
    status.value = result;
    emit('connection', result.connected);
    if (result.telegramUserId) {
      telegramUserId.value = String(result.telegramUserId);
    }
  } catch (cause: any) {
    if (!disposed) {
      error.value = cause?.data?.detail ?? cause?.data?.message ?? 'Não foi possível carregar as informações do Telegram.';
    }
  }
}

async function handleSave() {
  const cleanId = telegramUserId.value.trim();
  if (!cleanId || !/^\d+$/.test(cleanId) || Number(cleanId) <= 0) {
    error.value = 'Informe um ID numérico de usuário válido do Telegram.';
    return;
  }

  busy.value = true;
  error.value = '';
  success.value = '';

  try {
    const updated = await telegramService.save(Number(cleanId), props.restaurantId, props.superAdmin);
    if (disposed) return;
    status.value = updated;
    emit('connection', updated.connected);
    if (updated.telegramUserId) {
      telegramUserId.value = String(updated.telegramUserId);
    }
    success.value = 'ID do Telegram salvo com sucesso! Agora suas mensagens serão reconhecidas pelo Assistente Orquestrador.';
  } catch (cause: any) {
    if (!disposed) {
      error.value = cause?.data?.detail ?? cause?.data?.message ?? 'Não foi possível salvar o ID do Telegram. Tente novamente.';
    }
  } finally {
    if (!disposed) busy.value = false;
  }
}

async function handleDelete() {
  busy.value = true;
  error.value = '';
  success.value = '';

  try {
    await telegramService.disconnect(props.restaurantId, props.superAdmin);
    if (disposed) return;
    telegramUserId.value = '';
    await refresh();
    success.value = 'ID do Telegram excluído e integração desconectada com sucesso.';
  } catch (cause: any) {
    if (!disposed) {
      error.value = cause?.data?.detail ?? cause?.data?.message ?? 'Não foi possível excluir o ID do Telegram.';
    }
  } finally {
    if (!disposed) busy.value = false;
  }
}

async function handleTest() {
  const cleanId = telegramUserId.value.trim();
  if (!cleanId) {
    error.value = 'Informe e salve seu ID de usuário do Telegram antes de realizar o teste.';
    return;
  }

  testing.value = true;
  error.value = '';
  success.value = '';

  try {
    const res = await telegramService.testConnection(props.restaurantId, props.superAdmin);
    if (disposed) return;
    if (res.success) {
      success.value = res.message;
    } else {
      error.value = res.message;
    }
  } catch (cause: any) {
    if (!disposed) {
      error.value = cause?.data?.detail ?? cause?.data?.message ?? 'Falha ao testar conexão. Verifique se você já abriu a conversa no Telegram e clicou em Começar (/start).';
    }
  } finally {
    if (!disposed) testing.value = false;
  }
}

onMounted(() => {
  refresh();
});

watch(
  () => [props.restaurantId, props.superAdmin],
  () => {
    error.value = '';
    success.value = '';
    refresh();
  }
);

onBeforeUnmount(() => {
  disposed = true;
});
</script>

<template>
  <section
    :class="props.embedded ? 'space-y-5' : 'rounded-2xl border border-zinc-200 bg-white p-5 dark:border-zinc-800 dark:bg-zinc-900/40 sm:p-6 space-y-5'"
    :aria-label="props.embedded ? 'Configuração do Telegram' : undefined"
  >
    <!-- Header -->
    <div class="flex flex-wrap items-center justify-between gap-3">
      <div>
        <h2 class="text-base font-semibold sm:text-lg text-zinc-900 dark:text-zinc-100 flex items-center gap-2">
          <span>Integração Telegram</span>
          <span
            v-if="status"
            role="status"
            class="rounded-full px-2.5 py-0.5 text-xs font-medium"
            :class="status.connected ? 'bg-emerald-50 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-300 ring-1 ring-emerald-500/20' : 'bg-zinc-100 text-zinc-600 dark:bg-zinc-800 dark:text-zinc-400'"
          >
            {{ status.connected ? 'Conectado' : 'Não conectado' }}
          </span>
        </h2>
        <p class="mt-1 text-xs text-zinc-500 dark:text-zinc-400 sm:text-sm">
          Converse com o Assistente Orquestrador do ISM diretamente pelo Telegram, com as mesmas respostas inteligentes e ferramentas do painel.
        </p>
      </div>
    </div>

    <!-- Feedback Alerts -->
    <el-alert v-if="error" type="error" :title="error" :closable="true" @close="error = ''" show-icon />
    <el-alert v-if="success" type="success" :title="success" :closable="true" @close="success = ''" show-icon />

    <p v-if="props.superAdmin && !props.restaurantId" class="text-sm text-zinc-600 dark:text-zinc-400">
      Selecione um restaurante acima para configurar o Telegram.
    </p>

    <div v-else-if="status" class="space-y-5">
      <!-- Card Informativo do Bot (Fixo) -->
      <div class="rounded-xl border border-zinc-200 bg-zinc-50/80 p-4 dark:border-zinc-800 dark:bg-zinc-900/60">
        <div class="grid gap-4 sm:grid-cols-2">
          <div>
            <span class="text-xs font-semibold uppercase tracking-wider text-zinc-500 dark:text-zinc-400">ID do Bot (Fixo)</span>
            <div class="mt-1 flex items-center gap-2">
              <span class="font-mono text-sm font-bold text-zinc-800 dark:text-zinc-200 bg-zinc-200/60 dark:bg-zinc-800 px-2 py-0.5 rounded">
                {{ status.botId ?? 'Não configurado' }}
              </span>
              <span v-if="status.configured" class="inline-flex items-center text-xs text-emerald-600 dark:text-emerald-400 font-medium">
                ● Ativo
              </span>
            </div>
            <p class="mt-1 text-xs text-zinc-500 dark:text-zinc-400">
              Identificador único e fixo da aplicação bot no servidor.
            </p>
          </div>

          <div>
            <span class="text-xs font-semibold uppercase tracking-wider text-zinc-500 dark:text-zinc-400">Canal do Bot</span>
            <div class="mt-1 flex items-center gap-2">
              <span class="text-sm font-semibold text-zinc-900 dark:text-zinc-100">
                {{ status.botUsername ? `@${status.botUsername}` : 'Aguardando configuração' }}
              </span>
              <a
                v-if="status.botUsername"
                :href="status.botUrl || `https://t.me/${status.botUsername}`"
                target="_blank"
                rel="noopener noreferrer"
                class="inline-flex items-center gap-1 rounded-md bg-indigo-50 px-2 py-0.5 text-xs font-medium text-indigo-700 hover:bg-indigo-100 dark:bg-indigo-950/60 dark:text-indigo-300 dark:hover:bg-indigo-900/60 transition"
              >
                <span>Abrir conversa</span>
                <svg class="h-3 w-3" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" /></svg>
              </a>
            </div>
            <p class="mt-1 text-xs text-zinc-500 dark:text-zinc-400">
              Clique no link para iniciar sua conversa com o bot no Telegram.
            </p>
          </div>
        </div>
      </div>

      <!-- Formulário de Configuração do Usuário -->
      <form class="space-y-4" @submit.prevent="handleSave">
        <div>
          <label for="telegram-user-id" class="block text-sm font-semibold text-zinc-900 dark:text-zinc-100">
            ID do seu usuário no Telegram
          </label>
          <div class="mt-1.5 max-w-md">
            <input
              id="telegram-user-id"
              v-model="telegramUserId"
              type="text"
              required
              pattern="[0-9]+"
              maxlength="20"
              inputmode="numeric"
              placeholder="Ex.: 1079867777"
              :disabled="busy || testing"
              class="w-full rounded-lg border border-zinc-300 bg-white px-3.5 py-2.5 text-sm text-zinc-900 placeholder:text-zinc-400 outline-none transition focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/20 disabled:opacity-60 dark:border-zinc-700 dark:bg-zinc-950 dark:text-zinc-100 dark:placeholder:text-zinc-500"
            />
          </div>
          <p class="mt-2 text-xs text-zinc-500 dark:text-zinc-400 flex flex-wrap items-center gap-1.5">
            <span>Informe o seu ID numérico. Apenas mensagens deste ID serão atendidas pelo orquestrador.</span>
            <span class="rounded bg-zinc-100 dark:bg-zinc-800 px-1.5 py-0.5 font-mono text-[11px] text-zinc-600 dark:text-zinc-400">
              Como descobrir meu ID? Envie uma mensagem para <strong>@userinfobot</strong> no Telegram.
            </span>
          </p>
        </div>

        <!-- Botões de Ação -->
        <div class="pt-2 flex flex-wrap items-center gap-3">
          <button
            type="submit"
            :disabled="busy || testing || !status.configured"
            class="inline-flex items-center justify-center rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white shadow-sm transition hover:bg-indigo-500 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-600 disabled:opacity-50"
          >
            <svg v-if="busy" class="mr-2 h-4 w-4 animate-spin" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z"></path>
            </svg>
            Salvar ID
          </button>

          <button
            type="button"
            :disabled="busy || testing || !telegramUserId"
            @click="handleTest"
            class="inline-flex items-center justify-center rounded-lg border border-indigo-200 bg-indigo-50/70 px-4 py-2 text-sm font-medium text-indigo-700 shadow-sm transition hover:bg-indigo-100 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-600 disabled:opacity-50 dark:border-indigo-800 dark:bg-indigo-950/40 dark:text-indigo-300 dark:hover:bg-indigo-900/40"
          >
            <svg v-if="testing" class="mr-2 h-4 w-4 animate-spin" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z"></path>
            </svg>
            Testar Conexão
          </button>

          <button
            v-if="status.connected || telegramUserId"
            type="button"
            :disabled="busy || testing"
            @click="handleDelete"
            class="inline-flex items-center justify-center rounded-lg border border-red-200 bg-white px-4 py-2 text-sm font-medium text-red-600 shadow-sm transition hover:bg-red-50 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-red-600 disabled:opacity-50 dark:border-red-900/60 dark:bg-zinc-900 dark:text-red-400 dark:hover:bg-red-950/30"
          >
            Excluir ID
          </button>
        </div>
      </form>

      <!-- Instruções / Accordion Informativo -->
      <details class="rounded-xl border border-zinc-200 bg-zinc-50/60 p-4 dark:border-zinc-800 dark:bg-zinc-950/40 transition">
        <summary class="cursor-pointer text-xs font-semibold text-zinc-700 dark:text-zinc-300 uppercase tracking-wider">
          Como funciona a integração com o Assistente Orquestrador?
        </summary>
        <div class="mt-3 space-y-2 text-xs sm:text-sm text-zinc-600 dark:text-zinc-400 leading-relaxed">
          <p>
            • <strong>Plug and play:</strong> Basta cadastrar o seu ID numérico do Telegram acima e salvar. Não é necessário gerar links temporários.
          </p>
          <p>
            • <strong>Redirecionamento direto:</strong> Qualquer pergunta enviada para o bot no Telegram é direcionada ao Assistente Orquestrador do ISM, que analisa dados de vendas, faturamento, pedidos, estoque e cardápio em tempo real.
          </p>
          <p>
            • <strong>Segurança multi-tenant:</strong> Apenas o ID autorizado neste restaurante consegue interagir com o bot. Mensagens de IDs não autorizados são bloqueadas imediatamente.
          </p>
          <p>
            • <strong>Comandos úteis:</strong> Envie <code class="bg-zinc-200 dark:bg-zinc-800 px-1 py-0.5 rounded">/nova</code> a qualquer momento no Telegram para reiniciar a conversa.
          </p>
        </div>
      </details>
    </div>
  </section>
</template>
