<script setup lang="ts">
import { ref, computed } from "vue";
import { importService, PHOTO_CONTENT_PREFIXES, type ImportPreviewDto } from "~/services/modules/importService";

const props = defineProps<{ restaurantId: number | null }>();
const emit = defineEmits<{
  (e: "analyzed", preview: ImportPreviewDto, file: File, isPhoto: boolean): void;
}>();

const ACCEPT = ".csv,.xlsx,.xls,.xml,.json,.txt,image/*,.pdf";
const isPhoto = (file: File) =>
  file.type.startsWith("image/") || file.type === "application/pdf" ||
  /\.(jpe?g|png|webp|gif|pdf)$/i.test(file.name);

const dragging = ref(false);
const analyzing = ref(false);
const error = ref<string | null>(null);
const lastFileName = ref<string | null>(null);

const statusText = computed(() => {
  if (!analyzing.value) return null;
  return "Analisando estrutura... isso pode levar alguns segundos em fotos.";
});

async function handleFile(file: File) {
  error.value = null;
  if (props.restaurantId == null || props.restaurantId <= 0) {
    error.value = "Selecione o restaurante alvo antes de enviar o arquivo.";
    return;
  }

  const photo = isPhoto(file);
  analyzing.value = true;
  lastFileName.value = file.name;
  try {
    const preview = photo
      ? await importService.previewPhoto(file, props.restaurantId)
      : await importService.previewSpreadsheet(file, props.restaurantId);
    emit("analyzed", preview, file, photo);
  } catch (e: any) {
    error.value =
      e?.data?.message ||
      e?.message ||
      "Não foi possível analisar o arquivo. Verifique o formato e tente novamente.";
  } finally {
    analyzing.value = false;
  }
}

function onDrop(evt: DragEvent) {
  evt.preventDefault();
  dragging.value = false;
  const files = evt.dataTransfer?.files;
  if (files && files.length > 0) handleFile(files[0]);
}
</script>

<template>
  <div class="space-y-5">
    <div
      v-if="!analyzing"
      class="border-2 border-dashed rounded-2xl p-12 text-center transition-colors cursor-pointer"
      :class="dragging ? 'border-indigo-400/70 bg-indigo-500/5' : 'border-zinc-700/80 bg-zinc-950/40 hover:border-indigo-500/50'"
      @click="($refs.fileInput as HTMLInputElement).click()"
      @dragover.prevent="dragging = true"
      @dragleave="dragging = false"
      @drop="onDrop"
    >
      <svg class="w-14 h-14 text-zinc-400 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12"></path>
      </svg>
      <p class="text-base text-zinc-200 font-medium">Arraste e solte o arquivo aqui</p>
      <p class="text-sm text-zinc-500 mt-1">ou clique para selecionar</p>

      <div class="mt-6 flex flex-wrap justify-center gap-2 text-xs font-mono text-zinc-400">
        <span class="px-2 py-1 rounded-md bg-zinc-800/80 border border-zinc-700/60">CSV</span>
        <span class="px-2 py-1 rounded-md bg-zinc-800/80 border border-zinc-700/60">XLSX</span>
        <span class="px-2 py-1 rounded-md bg-zinc-800/80 border border-zinc-700/60">XML NF-e*</span>
        <span class="px-2 py-1 rounded-md bg-zinc-800/80 border border-zinc-700/60">JSON*</span>
        <span class="px-2 py-1 rounded-md bg-violet-500/10 border border-violet-500/30 text-violet-300">📷 Foto do caderno / PDF escaneado</span>
      </div>
      <p class="text-xs text-zinc-600 mt-4">* em breve · Fotos são interpretadas por IA de visão</p>

      <input ref="fileInput" type="file" :accept="ACCEPT" class="hidden" @change="(e: Event) => {
        const input = e.target as HTMLInputElement;
        if (input.files && input.files.length > 0) handleFile(input.files[0]);
        input.value = '';
      }" />
    </div>

    <div v-else class="rounded-2xl p-12 text-center border border-indigo-500/30 bg-indigo-500/5">
      <div class="inline-block w-8 h-8 border-2 border-indigo-400 border-t-transparent rounded-full animate-spin"></div>
      <p class="text-sm text-indigo-300 font-medium mt-4">{{ statusText }}</p>
      <p v-if="lastFileName" class="text-xs text-zinc-500 mt-1 font-mono">{{ lastFileName }}</p>
    </div>

    <div v-if="error" class="p-4 rounded-xl bg-red-500/10 border border-red-500/30 text-sm text-red-300 flex items-start gap-2">
      <svg class="w-5 h-5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
      <span>{{ error }}</span>
    </div>
  </div>
</template>
