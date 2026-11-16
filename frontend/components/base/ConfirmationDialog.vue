<script setup lang="ts">
import { ElDialog } from 'element-plus'
import type { ConfirmationState } from '~/composables/useConfirmation'

defineProps<{ state: ConfirmationState }>()
defineEmits<{ confirm: []; cancel: [] }>()
</script>

<template>
  <ElDialog :model-value="state.open" :title="state.title" class="ism-confirm-dialog" align-center
    :show-close="false" :close-on-click-modal="false" :append-to-body="true" :destroy-on-close="true"
    @update:model-value="value => { if (!value) $emit('cancel') }">
    <template #header="{ titleId }">
      <div class="flex items-start gap-4 px-6 pt-6 sm:px-7 sm:pt-7">
        <span class="flex h-12 w-12 shrink-0 items-center justify-center rounded-2xl border"
          :class="state.tone === 'danger' ? 'border-red-200 bg-red-50 text-red-600 dark:border-red-900 dark:bg-red-950/60 dark:text-red-400' : state.tone === 'success' ? 'border-emerald-200 bg-emerald-50 text-emerald-600 dark:border-emerald-900 dark:bg-emerald-950/60 dark:text-emerald-400' : 'border-indigo-200 bg-indigo-50 text-indigo-600 dark:border-indigo-900 dark:bg-indigo-950/60 dark:text-indigo-400'">
          <svg v-if="state.tone === 'danger'" class="h-6 w-6" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" d="m10.3 4.2-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.7-2.8l-8-14a2 2 0 0 0-3.4 0Z"/><path stroke-linecap="round" d="M12 9v4m0 4h.01"/></svg>
          <svg v-else class="h-6 w-6" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><rect x="4" y="3" width="16" height="18" rx="3"/><path stroke-linecap="round" stroke-linejoin="round" d="m8 12 3 3 5-6"/></svg>
        </span>
        <div class="min-w-0 flex-1 pt-0.5">
          <p class="mb-1 text-[10px] font-semibold uppercase tracking-[0.18em] text-zinc-500">{{ state.tone === 'danger' ? 'Ação permanente' : 'Confirmação' }}</p>
          <h2 :id="titleId" class="text-xl font-semibold leading-tight tracking-tight text-zinc-900 dark:text-zinc-100">{{ state.title }}</h2>
        </div>
        <button type="button" aria-label="Fechar confirmação" class="-mr-1 -mt-1 flex h-9 w-9 shrink-0 items-center justify-center rounded-lg text-zinc-400 transition hover:bg-zinc-100 hover:text-zinc-700 focus-visible:outline focus-visible:outline-2 focus-visible:outline-indigo-500 dark:hover:bg-zinc-800 dark:hover:text-zinc-200" @click="$emit('cancel')">
          <svg class="h-5 w-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true"><path stroke-linecap="round" d="m6 6 12 12M6 18 18 6"/></svg>
        </button>
      </div>
    </template>
    <div class="px-6 pb-6 pt-4 sm:px-7">
      <p class="text-sm leading-relaxed text-zinc-600 dark:text-zinc-400">{{ state.message }}</p>
      <div v-if="state.context || state.detail" class="mt-5 rounded-xl border border-zinc-200 bg-zinc-50 px-4 py-3 dark:border-zinc-700/70 dark:bg-zinc-950/60">
        <p v-if="state.context" class="break-words text-sm font-medium text-zinc-900 dark:text-zinc-100">{{ state.context }}</p>
        <p v-if="state.detail" class="mt-1 break-words text-xs leading-relaxed text-zinc-500 dark:text-zinc-400">{{ state.detail }}</p>
      </div>
    </div>
    <template #footer>
      <div class="flex flex-col-reverse gap-3 border-t border-zinc-200/80 bg-zinc-50/70 px-6 py-4 dark:border-zinc-800 dark:bg-zinc-950/30 sm:flex-row sm:justify-end sm:px-7">
        <button type="button" class="min-h-11 rounded-xl border border-zinc-300 bg-white px-5 py-2.5 text-sm font-semibold text-zinc-700 transition hover:bg-zinc-100 focus-visible:outline focus-visible:outline-2 focus-visible:outline-indigo-500 dark:border-zinc-700 dark:bg-zinc-900 dark:text-zinc-300 dark:hover:bg-zinc-800" @click="$emit('cancel')">Voltar</button>
        <button type="button" class="min-h-11 rounded-xl px-5 py-2.5 text-sm font-semibold text-white shadow-sm transition focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-500"
          :class="state.tone === 'danger' ? 'bg-red-600 hover:bg-red-700' : state.tone === 'success' ? 'bg-emerald-600 hover:bg-emerald-700' : 'bg-indigo-600 hover:bg-indigo-700'"
          @click="$emit('confirm')">{{ state.confirmLabel || 'Confirmar' }}</button>
      </div>
    </template>
  </ElDialog>
</template>

<style>
.ism-confirm-dialog.el-dialog {
  --el-dialog-bg-color: #fff;
  width: min(480px, calc(100vw - 32px));
  max-height: calc(100dvh - 32px);
  overflow: auto;
  padding: 0;
  border: 1px solid #e4e4e7;
  border-radius: 24px;
  box-shadow: 0 32px 96px rgb(0 0 0 / 28%);
}
.dark .ism-confirm-dialog.el-dialog { --el-dialog-bg-color: #18181b; border-color: #3f3f46; }
.ism-confirm-dialog .el-dialog__header,
.ism-confirm-dialog .el-dialog__body,
.ism-confirm-dialog .el-dialog__footer { margin: 0; padding: 0; }
.el-overlay:has(.ism-confirm-dialog) { background: rgb(9 9 11 / 65%); backdrop-filter: blur(5px); }
</style>
