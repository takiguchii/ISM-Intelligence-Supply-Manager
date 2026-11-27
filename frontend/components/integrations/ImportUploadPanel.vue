<script setup lang="ts">
import IntegrationIcon from './IntegrationIcon.vue';
const props = defineProps<{ analyzing: boolean; disabled: boolean; fileName: string | null; error: string | null }>();
const emit = defineEmits<{ file: [file: File] }>();
const input = ref<HTMLInputElement | null>(null);
const dragging = ref(false);
function select() { if (!props.disabled && !props.analyzing) input.value?.click(); }
function changed(event: Event) {
  const element = event.target as HTMLInputElement;
  if (element.files?.[0] && !props.disabled && !props.analyzing) emit('file', element.files[0]);
  element.value = '';
}
function dropped(event: DragEvent) {
  dragging.value = false;
  if (event.dataTransfer?.files[0] && !props.disabled && !props.analyzing) emit('file', event.dataTransfer.files[0]);
}
</script>

<template>
  <div class="space-y-4">
    <div class="rounded-xl border-2 border-dashed p-6 text-center sm:p-8" :class="dragging ? 'border-indigo-500 bg-indigo-500/5' : 'border-zinc-300 bg-white dark:border-zinc-700 dark:bg-zinc-900/40'" @dragover.prevent="dragging = true" @dragleave.prevent="dragging = false" @drop.prevent="dropped">
      <div v-if="analyzing" role="status" aria-live="polite"><span class="mx-auto mb-4 block h-7 w-7 animate-spin rounded-full border-2 border-indigo-500 border-t-transparent" aria-hidden="true" /><p class="font-semibold">Analisando arquivo…</p><p class="mt-2 break-all text-sm text-zinc-600 dark:text-zinc-400">{{ fileName }}</p></div>
      <template v-else>
        <span class="mx-auto mb-3 flex h-10 w-10 items-center justify-center rounded-xl bg-indigo-50 dark:bg-indigo-500/10"><IntegrationIcon icon="sheet" /></span>
        <p class="text-sm font-semibold">Arraste um arquivo até aqui</p><p class="mb-4 mt-2 text-sm text-zinc-600 dark:text-zinc-400">Planilhas, documentos, PDFs e fotos. Revise os dados antes de salvar.</p>
        <button type="button" :disabled="disabled" class="rounded-lg bg-indigo-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-indigo-500 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-500 disabled:opacity-50" @click="select">Selecionar arquivo</button>
      </template>
      <input ref="input" type="file" accept=".csv,.xlsx,.xls,.xml,.json,.txt,image/*,.pdf" class="sr-only" tabindex="-1" aria-label="Arquivo para importar" :disabled="disabled || analyzing" @change="changed" />
    </div>
    <p v-if="disabled" class="text-sm text-zinc-600 dark:text-zinc-400">Selecione um restaurante para importar.</p>
    <el-alert v-if="error" role="alert" :title="error" type="error" show-icon :closable="false" />
  </div>
</template>
