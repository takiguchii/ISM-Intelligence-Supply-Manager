<script setup lang="ts">
import { settingsService, settingsError, type RestaurantSettings } from "~/services/modules/system/settingsService";
const props = defineProps<{ restaurantId: number | null; superAdmin: boolean }>();
const form = ref<RestaurantSettings | null>(null);
const loading = ref(false);
const saving = ref(false);
const error = ref("");
const success = ref("");
let loadVersion = 0;
watch(() => [props.restaurantId, props.superAdmin], async () => {
  const version = ++loadVersion;
  form.value = null;
  error.value = "";
  success.value = "";
  if (!props.restaurantId) return;
  loading.value = true;
  try {
    const profile = await settingsService.getProfile(props.restaurantId, props.superAdmin);
    if (version === loadVersion) form.value = profile;
  } catch (e) { if (version === loadVersion) error.value = settingsError(e); }
  finally { if (version === loadVersion) loading.value = false; }
}, { immediate: true });

async function save() {
  if (!form.value || saving.value) return;
  error.value = "";
  success.value = "";
  saving.value = true;
  const version = loadVersion;
  try {
    const payload = { ...form.value, name: form.value.name.trim(), cnpj: form.value.cnpj.replace(/\D/g, ""), contactEmail: form.value.contactEmail?.trim() || null };
    if (!payload.name || payload.cnpj.length !== 14) throw new Error("Informe o nome e um CNPJ com 14 dígitos.");
    await settingsService.saveProfile(payload.id, props.superAdmin, payload);
    if (version === loadVersion) { form.value = payload; success.value = "Configurações salvas."; }
  } catch (e: any) { if (version === loadVersion) error.value = e?.data ? settingsError(e) : e.message; }
  finally { saving.value = false; }
}
async function uploadLogo(event: Event) {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0];
  if (!file || !form.value) return;
  error.value = "";
  const version = loadVersion;
  if (!["image/png", "image/jpeg", "image/webp"].includes(file.type) || file.size > 500000) {
    error.value = "Selecione PNG, JPEG ou WebP de até 500 KB."; input.value = ""; return;
  }
  try {
    const dataUrl = await new Promise<string>((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = () => resolve(String(reader.result));
      reader.onerror = reject;
      reader.readAsDataURL(file);
    });
    if (version === loadVersion && form.value) form.value.logoDataUrl = dataUrl;
  } catch { error.value = "Não foi possível ler a imagem."; }
}
const textFields = [
  { key: "address", label: "Endereço completo", type: "text", max: 300 },
  { key: "phone", label: "Telefone", type: "tel", max: 30 },
  { key: "contactEmail", label: "E-mail de contato", type: "email", max: 150 }
] as const;
</script>

<template>
  <div class="settings-card">
    <h2>Informações do restaurante</h2>
    <p class="settings-hint">Dados do estabelecimento e preferências para sua operação.</p>
    <p v-if="!restaurantId" class="settings-hint">Selecione um restaurante para editar as configurações.</p>
    <p v-if="loading" role="status">Carregando configurações…</p>
    <p v-if="error" role="alert" class="text-red-600 dark:text-red-400">{{ error }}</p>
    <p v-if="success" role="status" class="text-emerald-600 dark:text-emerald-400">{{ success }}</p>
    <form v-if="form" @submit.prevent="save">
      <fieldset :disabled="saving || loading" class="space-y-5 disabled:opacity-60">
        <div class="settings-grid">
          <label for="restaurant-name">Nome fantasia<input id="restaurant-name" v-model="form.name" required maxlength="120" /></label>
          <label for="restaurant-cnpj">CNPJ<input id="restaurant-cnpj" v-model="form.cnpj" required maxlength="18" inputmode="numeric" /></label>
          <label v-for="field in textFields" :key="field.key" :for="`restaurant-${field.key}`">{{ field.label }}<input :id="`restaurant-${field.key}`" v-model="form[field.key]" :type="field.type" :maxlength="field.max" /></label>
          <label for="restaurant-timezone">Fuso horário
            <select id="restaurant-timezone" v-model="form.timeZone">
              <option v-for="zone in ['America/Sao_Paulo', 'America/Manaus', 'America/Rio_Branco', 'America/Noronha', 'UTC']" :key="zone" :value="zone">{{ zone }}</option>
            </select>
          </label>
          <label for="restaurant-currency">Moeda<select id="restaurant-currency" v-model="form.currency"><option value="BRL">BRL — Real</option><option value="USD">USD — Dólar</option><option value="EUR">EUR — Euro</option></select></label>
          <label for="restaurant-language">Idioma preferido<select id="restaurant-language" v-model="form.language"><option value="pt-BR">Português (Brasil)</option><option value="en-US">Inglês</option><option value="es-ES">Espanhol</option></select></label>
        </div>
        <p class="settings-hint">Moeda, idioma e fuso são preferências do cadastro. Alterá-los não converte valores nem traduz o sistema.</p>
        <label for="restaurant-hours">Horário de funcionamento<textarea id="restaurant-hours" v-model="form.openingHours" rows="3" maxlength="1000" placeholder="Segunda a sexta: 11h às 23h; sábado: 12h às 00h" /></label>
        <div class="settings-grid">
          <label for="restaurant-service">Taxa de serviço padrão (%)<input id="restaurant-service" v-model.number="form.serviceFeePercent" type="number" min="0" max="100" step="0.01" required /></label>
          <label for="restaurant-delivery">Taxa de entrega padrão<input id="restaurant-delivery" v-model.number="form.deliveryFee" type="number" min="0" max="10000" step="0.01" required /></label>
          <label for="restaurant-packaging">Taxa de embalagem padrão<input id="restaurant-packaging" v-model.number="form.packagingFee" type="number" min="0" max="10000" step="0.01" required /></label>
        </div>
        <p class="settings-hint">Taxas e horários ficam registrados para consulta; os pedidos existentes não são recalculados.</p>
        <label for="restaurant-logo">Logo do restaurante<input id="restaurant-logo" type="file" accept="image/png,image/jpeg,image/webp" @change="uploadLogo" /></label>
        <p class="settings-hint">PNG, JPEG ou WebP de até 500 KB.</p>
        <div v-if="form.logoDataUrl" class="flex items-center gap-4"><img :src="form.logoDataUrl" alt="Logo do restaurante" class="h-20 w-20 rounded-xl object-contain" /><button type="button" class="settings-secondary" @click="form.logoDataUrl = null">Remover logo</button></div>
        <button type="submit" class="settings-primary">{{ saving ? 'Salvando…' : 'Salvar alterações' }}</button>
      </fieldset>
    </form>
  </div>
</template>
