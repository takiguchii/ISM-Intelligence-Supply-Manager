<script setup lang="ts">
import { useThemeStore, type AppearancePreferences } from "~/stores/theme";
import { settingsError } from "~/services/modules/system/settingsService";
const theme = useThemeStore();
const form = ref<AppearancePreferences>(theme.preferences());
const loading = ref(true);
const saving = ref(false);
const error = ref("");
const success = ref("");
onMounted(async () => {
  try { await theme.loadPreferences(); form.value = theme.preferences(); }
  catch (e) { error.value = settingsError(e); }
  finally { loading.value = false; }
});
async function save() {
  saving.value = true; error.value = ""; success.value = "";
  try { await theme.savePreferences(form.value); success.value = "Aparência salva na sua conta."; }
  catch (e) { error.value = settingsError(e); }
  finally { saving.value = false; }
}
</script>
<template>
  <div class="settings-card">
    <h2>Aparência e acessibilidade</h2>
    <p class="settings-hint">Personalize a interface. As preferências são salvas na sua conta e aplicadas ao entrar.</p>
    <p v-if="error" role="alert" class="text-red-600 dark:text-red-400">{{ error }}</p>
    <p v-if="success" role="status" class="text-emerald-600 dark:text-emerald-400">{{ success }}</p>
    <p v-if="loading" role="status">Carregando aparência…</p>
    <form v-else @submit.prevent="save">
      <fieldset :disabled="saving" class="space-y-5">
        <div class="settings-grid">
          <label for="appearance-theme">Tema<select id="appearance-theme" v-model="form.theme"><option value="dark">Escuro</option><option value="light">Claro</option></select></label>
          <label for="appearance-density">Densidade das tabelas<select id="appearance-density" v-model="form.density"><option value="comfortable">Confortável</option><option value="compact">Compacta</option></select></label>
          <label for="appearance-font">Tamanho da fonte<select id="appearance-font" v-model.number="form.fontScale"><option :value="90">90%</option><option :value="100">100%</option><option :value="115">115%</option></select></label>
        </div>
        <label class="!flex-row items-center gap-3"><input v-model="form.reduceMotion" type="checkbox" class="h-5 w-5 accent-indigo-600" />Reduzir animações</label>
        <label class="!flex-row items-center gap-3"><input v-model="form.highContrast" type="checkbox" class="h-5 w-5 accent-indigo-600" />Aumentar contraste de textos e bordas</label>
        <button type="submit" class="settings-primary" :disabled="saving">{{ saving ? 'Salvando…' : 'Salvar aparência' }}</button>
      </fieldset>
    </form>
  </div>
</template>
