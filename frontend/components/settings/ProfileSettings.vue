<script setup lang="ts">
import { settingsError, settingsService } from "~/services/modules/system/settingsService";

const auth = useAuthStore();
const name = ref("");
const avatarDataUrl = ref<string | null>(null);
const saving = ref(false);
const reading = ref(false);
const error = ref("");
const success = ref("");
const photoInput = ref<HTMLInputElement | null>(null);
const initials = computed(() => name.value.trim().split(/\s+/).filter(Boolean).slice(0, 2).map(part => part[0]).join("").toUpperCase());

watch(() => auth.currentUser?.id, () => {
  name.value = auth.currentUser?.name ?? "";
  avatarDataUrl.value = auth.currentUser?.avatarDataUrl ?? null;
}, { immediate: true });

async function selectPhoto(event: Event) {
  const input = event.currentTarget as HTMLInputElement;
  const file = input.files?.[0];
  input.value = "";
  if (!file) return;
  error.value = "";
  success.value = "";
  if (!["image/jpeg", "image/png", "image/webp"].includes(file.type) || file.size > 10 * 1024 * 1024) {
    error.value = "Escolha uma imagem JPEG, PNG ou WebP de até 10 MB.";
    return;
  }
  reading.value = true;
  try {
    const image = await createImageBitmap(file);
    try {
      const canvas = document.createElement("canvas");
      canvas.width = canvas.height = 256;
      const context = canvas.getContext("2d", { alpha: false });
      if (!context) throw new Error("Prévia indisponível.");
      const edge = Math.min(image.width, image.height);
      context.fillStyle = "#fff";
      context.fillRect(0, 0, 256, 256);
      context.drawImage(image, (image.width - edge) / 2, (image.height - edge) / 2, edge, edge, 0, 0, 256, 256);
      avatarDataUrl.value = canvas.toDataURL("image/jpeg", 0.88);
    } finally {
      image.close();
    }
  } catch {
    error.value = "Não foi possível abrir a foto. Escolha outro arquivo.";
  } finally {
    reading.value = false;
  }
}

async function save() {
  const userId = auth.currentUser?.id;
  if (!userId || saving.value || reading.value) return;
  error.value = "";
  success.value = "";
  if (!name.value.trim()) {
    error.value = "Informe seu nome.";
    return;
  }
  saving.value = true;
  try {
    const profile = await settingsService.saveMyProfile({ name: name.value.trim(), avatarDataUrl: avatarDataUrl.value });
    auth.applyProfile(userId, profile);
    name.value = profile.name;
    avatarDataUrl.value = profile.avatarDataUrl;
    success.value = "Perfil atualizado.";
  } catch (cause) {
    error.value = settingsError(cause);
  } finally {
    saving.value = false;
  }
}
</script>

<template>
  <form class="settings-card" @submit.prevent="save">
    <fieldset :disabled="saving || reading" class="space-y-5">
      <div class="flex flex-wrap items-center gap-4">
        <div class="grid h-20 w-20 shrink-0 place-items-center overflow-hidden rounded-full bg-indigo-100 text-xl font-semibold text-indigo-800 dark:bg-indigo-950 dark:text-indigo-200">
          <img v-if="avatarDataUrl" :src="avatarDataUrl" alt="Prévia da foto de perfil" class="h-full w-full object-cover" />
          <span v-else aria-hidden="true">{{ initials || '•' }}</span>
        </div>
        <div class="space-y-2">
          <h2>Foto de perfil</h2>
          <input ref="photoInput" type="file" accept="image/jpeg,image/png,image/webp" class="hidden" @change="selectPhoto" />
          <div class="flex flex-wrap gap-2">
            <button type="button" class="settings-secondary" @click="photoInput?.click()">Escolher foto</button>
            <button v-if="avatarDataUrl" type="button" class="settings-secondary" @click="avatarDataUrl = null; success = ''">Remover foto</button>
          </div>
        </div>
      </div>
      <div class="settings-grid">
        <label for="profile-name">Nome exibido<input id="profile-name" v-model="name" autocomplete="name" required maxlength="150" /></label>
        <label for="profile-email">E-mail de acesso<input id="profile-email" :value="auth.currentUser?.email" type="email" readonly /></label>
      </div>
      <p v-if="error" role="alert" class="text-sm text-red-600 dark:text-red-400">{{ error }}</p>
      <p v-if="success" role="status" class="text-sm text-emerald-700 dark:text-emerald-400">{{ success }}</p>
      <button type="submit" class="settings-primary">{{ saving ? 'Salvando…' : 'Salvar perfil' }}</button>
    </fieldset>
  </form>
</template>
