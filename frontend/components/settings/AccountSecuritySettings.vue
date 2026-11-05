<script setup lang="ts">
import { apiClientWithAuth } from "~/services/api/client";
import { settingsService, settingsError, type AccountSession } from "~/services/modules/system/settingsService";
const auth = useAuthStore();
const router = useRouter();
const passwordForm = reactive({ current: "", next: "", confirm: "" });
const passwordBusy = ref(false);
const passwordError = ref("");
const sessions = ref<AccountSession[]>([]);
const sessionsLoading = ref(false);
const sessionBusy = ref("");
const sessionError = ref("");
const twoFactorEnabled = ref(false);
const twoFactorLoading = ref(true);
const twoFactorBusy = ref(false);
const twoFactorError = ref("");
const twoFactorPassword = ref("");
const twoFactorCode = ref("");
const setup = ref<{ secret: string; provisioningUri: string } | null>(null);
const recoveryCodes = ref<string[]>([]);
const twoFactorMessage = ref("");

async function loadSessions() {
  sessionsLoading.value = true;
  sessionError.value = "";
  try { sessions.value = await settingsService.sessions(); }
  catch (e) { sessionError.value = settingsError(e); }
  finally { sessionsLoading.value = false; }
}
onMounted(async () => {
  await Promise.all([loadSessions(), (async () => {
    try { twoFactorEnabled.value = (await apiClientWithAuth<{ enabled: boolean }>("/api/auth/two-factor")).enabled; }
    catch (e) { twoFactorError.value = settingsError(e); }
    finally { twoFactorLoading.value = false; }
  })()]);
});
async function changePassword() {
  if (passwordBusy.value) return;
  passwordError.value = "";
  if (passwordForm.next !== passwordForm.confirm) { passwordError.value = "As senhas não coincidem."; return; }
  passwordBusy.value = true;
  try {
    await settingsService.changePassword(passwordForm.current, passwordForm.next);
    passwordForm.current = passwordForm.next = passwordForm.confirm = "";
    auth.clearAuth();
    await router.push("/login?passwordChanged=1");
  } catch (e) { passwordError.value = settingsError(e); }
  finally { passwordBusy.value = false; }
}
async function revoke(session: AccountSession) {
  if (sessionBusy.value) return;
  sessionBusy.value = session.id; sessionError.value = "";
  try {
    await settingsService.revokeSession(session.id);
    if (session.isCurrent) { auth.clearAuth(); await router.push("/login"); }
    else await loadSessions();
  } catch (e) { sessionError.value = settingsError(e); }
  finally { sessionBusy.value = ""; }
}
async function manageTwoFactor(action: "setup" | "enable" | "disable") {
  if (twoFactorBusy.value) return;
  twoFactorBusy.value = true; twoFactorError.value = ""; twoFactorMessage.value = "";
  try {
    const url = `/api/auth/two-factor/${action}`;
    if (action === "setup") {
      setup.value = await apiClientWithAuth(url, { method: "POST", body: { password: twoFactorPassword.value } });
    } else if (action === "enable") {
      const result = await apiClientWithAuth<{ recoveryCodes: string[] }>(url, { method: "POST", body: { code: twoFactorCode.value.trim() } });
      recoveryCodes.value = result.recoveryCodes;
      setup.value = null;
      twoFactorEnabled.value = true;
      twoFactorMessage.value = "Autenticação ativada. Guarde os códigos de recuperação e entre novamente usando o próximo código gerado pelo aplicativo.";
      auth.clearAuth();
    } else {
      await apiClientWithAuth(url, { method: "POST", body: { password: twoFactorPassword.value, code: twoFactorCode.value.trim() } });
      twoFactorEnabled.value = false;
      twoFactorMessage.value = "Autenticação em duas etapas desativada.";
    }
    twoFactorPassword.value = ""; twoFactorCode.value = "";
  } catch (e) { twoFactorError.value = settingsError(e); }
  finally { twoFactorBusy.value = false; }
}
</script>
<template>
  <div class="space-y-6">
    <div class="settings-card">
      <h2>Senha da conta</h2>
      <p class="settings-hint">Ao alterar a senha, todas as sessões serão encerradas. Entre novamente com a nova senha.</p>
      <p v-if="passwordError" role="alert" class="text-red-600 dark:text-red-400">{{ passwordError }}</p>
      <form @submit.prevent="changePassword">
        <fieldset :disabled="passwordBusy || recoveryCodes.length > 0" class="space-y-5">
          <label for="current-password">Senha atual<input id="current-password" v-model="passwordForm.current" type="password" autocomplete="current-password" required maxlength="256" /></label>
          <div class="settings-grid">
            <label for="new-password">Nova senha<input id="new-password" v-model="passwordForm.next" type="password" autocomplete="new-password" required minlength="8" maxlength="128" placeholder="De 8 a 128 caracteres" /></label>
            <label for="confirm-password">Confirmar nova senha<input id="confirm-password" v-model="passwordForm.confirm" type="password" autocomplete="new-password" required minlength="8" maxlength="128" /></label>
          </div>
          <button type="submit" class="settings-primary">{{ passwordBusy ? 'Atualizando…' : 'Atualizar senha' }}</button>
        </fieldset>
      </form>
    </div>
    <div class="settings-card">
      <h2>Autenticação em duas etapas</h2>
      <p class="settings-hint">Use um aplicativo autenticador compatível com TOTP, como Google Authenticator. Funciona sem SMS ou serviços de envio.</p>
      <p v-if="twoFactorLoading" role="status">Carregando…</p>
      <p v-else class="font-medium">Status: {{ twoFactorEnabled ? 'Ativada' : 'Desativada' }}</p>
      <p v-if="twoFactorError" role="alert" class="text-red-600 dark:text-red-400">{{ twoFactorError }}</p>
      <p v-if="twoFactorMessage" role="status" class="text-emerald-600 dark:text-emerald-400">{{ twoFactorMessage }}</p>
      <div v-if="recoveryCodes.length" class="space-y-4">
        <p class="settings-hint">Cada código pode ser usado uma vez no lugar do código do aplicativo. Eles são exibidos apenas agora; guarde-os em local seguro.</p>
        <ul class="grid grid-cols-1 sm:grid-cols-2 gap-2 font-mono text-sm select-all"><li v-for="code in recoveryCodes" :key="code">{{ code }}</li></ul>
        <NuxtLink to="/login" class="settings-primary">Guardei os códigos; entrar novamente</NuxtLink>
      </div>
      <form v-else-if="!twoFactorLoading" @submit.prevent="manageTwoFactor(twoFactorEnabled ? 'disable' : setup ? 'enable' : 'setup')">
        <fieldset :disabled="twoFactorBusy" class="space-y-5">
          <label v-if="!setup || twoFactorEnabled" for="two-factor-password">Senha atual<input id="two-factor-password" v-model="twoFactorPassword" type="password" autocomplete="current-password" required maxlength="256" /></label>
          <div v-if="setup" class="space-y-3">
            <p class="settings-hint">No aplicativo autenticador, adicione uma conta com chave de configuração, tipo “baseada no tempo”, e use esta chave. Confirme com o código de seis dígitos em até 10 minutos.</p>
            <code class="block break-all rounded-xl bg-zinc-100 dark:bg-zinc-950 p-4 select-all">{{ setup.secret }}</code>
          </div>
          <label v-if="setup || twoFactorEnabled" for="two-factor-confirmation">Código do autenticador{{ twoFactorEnabled ? ' ou de recuperação' : '' }}<input id="two-factor-confirmation" v-model="twoFactorCode" autocomplete="one-time-code" required maxlength="32" /></label>
          <div class="flex flex-wrap gap-3">
            <button type="submit" class="settings-primary">{{ twoFactorBusy ? 'Aguarde…' : twoFactorEnabled ? 'Desativar 2FA' : setup ? 'Confirmar ativação' : 'Configurar 2FA' }}</button>
            <button v-if="setup" type="button" class="settings-secondary" @click="setup = null; twoFactorCode = ''">Cancelar</button>
          </div>
        </fieldset>
      </form>
    </div>
    <div class="settings-card">
      <h2>Sessões ativas</h2>
      <p class="settings-hint">Confira os acessos à sua conta e encerre os que não reconhece.</p>
      <p v-if="sessionError" role="alert" class="text-red-600 dark:text-red-400">{{ sessionError }}</p>
      <p v-if="sessionsLoading" role="status">Carregando sessões…</p>
      <ul v-else class="divide-y divide-zinc-200 dark:divide-zinc-800">
        <li v-for="session in sessions" :key="session.id" class="flex flex-wrap items-center justify-between gap-4 py-4">
          <div><p class="font-medium">{{ session.device }} <span v-if="session.isCurrent" class="text-emerald-600 dark:text-emerald-400">· Atual</span></p><p class="settings-hint">Iniciada em {{ new Date(session.createdAtUtc).toLocaleString('pt-BR') }}</p></div>
          <button type="button" class="settings-secondary" :disabled="!!sessionBusy || recoveryCodes.length > 0" @click="revoke(session)">{{ sessionBusy === session.id ? 'Encerrando…' : 'Encerrar sessão' }}</button>
        </li>
      </ul>
      <p v-if="!sessionsLoading && !sessions.length && !sessionError" class="settings-hint">Sua sessão foi iniciada antes do registro de dispositivos. Saia e entre novamente para listá-la aqui.</p>
    </div>
  </div>
</template>
