<script setup lang="ts">
import { ref, computed } from "vue";
import {
  importService,
  type ImportPreviewDto,
  type ImportResultDto,
  type CategoryConfirmationDto
} from "~/services/modules/import/importService";

const props = defineProps<{
  preview: ImportPreviewDto;
  file: File;
  isPhoto: boolean;
  restaurantId: number | null;
}>();

const emit = defineEmits<{ (e: "back"): void; (e: "confirmed", results: ImportResultDto[]): void }>();

const PAGE_SIZE = 8;
const pageByCategory = ref<Record<string, number>>({});
const confirming = ref(false);
const confirmErrors = ref<string[]>([]);

// Somente categorias que o backend já sabe importar entram na confirmação
const importableCategories = computed(() =>
  props.preview.categories.filter((c) => importService.canConfirmCategory(c.category))
);
const futureCategories = computed(() =>
  props.preview.categories.filter((c) => !importService.canConfirmCategory(c.category))
);
const canConfirm = computed(() => importableCategories.value.length > 0);

function pageOf(cat: CategoryConfirmationDto) {
  return Math.max(1, pageByCategory.value[cat.category] ?? 1);
}
function setPage(cat: CategoryConfirmationDto, p: number) {
  pageByCategory.value[cat.category] = p;
}
function totalPages(cat: CategoryConfirmationDto) {
  return Math.max(1, Math.ceil(cat.mappedFields.length / PAGE_SIZE));
}

function fieldKeys(cat: CategoryConfirmationDto): string[] {
  const keys = new Set<string>();
  cat.mappedFields.forEach((row) => Object.keys(row).forEach((k) => keys.add(k)));
  return [...keys];
}

const categoryMeta: Record<string, { label: string; icon: string; badgeClass: string }> = {
  estoque: { label: "Estoque / Produtos", icon: "M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4", badgeClass: "bg-emerald-500/10 border-emerald-500/20 text-emerald-400" },
  fornecedores: { label: "Fornecedores", icon: "M3 3h2l.4 2M7 13h10l4-8H5.4M7 13L5.4 5M7 13l-2.293 2.293c-.63.63-.184 1.707.707 1.707H17m0 0a2 2 0 100 4 2 2 0 000-4zm-8 2a2 2 0 11-4 0 2 2 0 014 0z", badgeClass: "bg-indigo-500/10 border-indigo-500/20 text-indigo-400" },
  financas: { label: "Finanças", icon: "M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z", badgeClass: "bg-amber-500/10 border-amber-500/20 text-amber-400" }
};
const metaOf = (cat: string) => categoryMeta[cat] ?? { label: cat, icon: "M4 6h16M4 12h16M4 18h16", badgeClass: "bg-zinc-500/10 border-zinc-500/20 text-zinc-400" };

async function confirm() {
  confirming.value = true;
  confirmErrors.value = [];
  const results: ImportResultDto[] = [];
  try {
    for (const cat of importableCategories.value) {
      try {
        results.push(await importService.confirmCategory(cat.category, props.file, props.restaurantId));
      } catch (e: any) {
        confirmErrors.value.push(`${metaOf(cat.category).label}: ${e?.data?.message || e?.message || "falha ao importar"}`);
      }
    }
    emit("confirmed", results);
  } finally {
    confirming.value = false;
  }
}
</script>

<template>
  <div class="space-y-5">
    <!-- Resumo do arquivo analisado -->
    <div class="p-4 rounded-xl bg-zinc-900/60 border border-zinc-800/80 flex flex-wrap items-center gap-x-6 gap-y-2 text-xs">
      <div><span class="text-zinc-500 font-mono uppercase tracking-wider">Arquivo:</span> <span class="text-white">{{ preview.fileName }}</span></div>
      <div><span class="text-zinc-500 font-mono uppercase tracking-wider">Linhas:</span> <span class="text-white">{{ preview.totalRows }}</span></div>
      <div v-if="isPhoto"><span class="px-2 py-0.5 rounded-md bg-violet-500/10 border border-violet-500/30 text-violet-300">📷 extraído por IA de visão</span></div>
      <button @click="emit('back')" class="ml-auto text-xs underline text-zinc-400 hover:text-white">Enviar outro arquivo</button>
    </div>

    <!-- Colunas não reconhecidas -->
    <div v-if="preview.unmappedColumns.length > 0" class="p-4 rounded-xl bg-zinc-900/40 border border-zinc-800/60">
      <div class="text-xs font-semibold text-zinc-300 mb-2 flex items-center gap-2">
        <svg class="w-4 h-4 text-amber-400" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
        Colunas não reconhecidas (serão ignoradas)
      </div>
      <div class="flex flex-wrap gap-2">
        <span v-for="col in preview.unmappedColumns" :key="col" class="text-xs px-2 py-1 rounded-md bg-zinc-800/80 border border-zinc-700/60 font-mono text-zinc-400">{{ col }}</span>
      </div>
    </div>

    <!-- Nada reconhecido -->
    <div v-if="preview.categories.length === 0" class="p-8 rounded-2xl border border-red-500/30 bg-red-500/5 text-center">
      <p class="text-sm text-red-300 font-medium">Não foi possível identificar nenhuma categoria de dados neste arquivo.</p>
      <p class="text-xs text-zinc-500 mt-2">Verifique os cabeçalhos da planilha ou tente uma foto mais nítida.</p>
    </div>

    <!-- Cards por categoria -->
    <div v-for="cat in preview.categories" :key="cat.category" class="rounded-2xl bg-zinc-900/80 border shadow-lg overflow-hidden"
         :class="cat.canImport ? 'border-zinc-800/80' : 'border-amber-500/30'">
      <div class="p-4 flex items-center justify-between border-b border-zinc-800/60">
        <div class="flex items-center gap-3">
          <div class="w-9 h-9 rounded-xl flex items-center justify-center border" :class="metaOf(cat.category).badgeClass">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="metaOf(cat.category).icon"></path></svg>
          </div>
          <div>
            <div class="font-semibold text-white text-sm">{{ metaOf(cat.category).label }}</div>
            <div class="text-xs text-zinc-500">{{ cat.rowCount }} linha(s) · entidade {{ cat.targetEntity }}</div>
          </div>
        </div>
        <span class="inline-flex text-xs px-2 py-1 rounded-md border"
              :class="!importService.canConfirmCategory(cat.category)
                ? 'bg-zinc-700/20 border-zinc-600/30 text-zinc-400'
                : cat.canImport
                  ? 'bg-emerald-500/10 border-emerald-500/20 text-emerald-300'
                  : 'bg-amber-500/10 border-amber-500/20 text-amber-300'">
          {{ !importService.canConfirmCategory(cat.category) ? "Em breve" : cat.canImport ? "Pronto para importar" : `${cat.rowsWithErrors.length} linha(s) com erro` }}
        </span>
      </div>

      <!-- Tabela de dados mapeados com paginação -->
      <div v-if="cat.mappedFields.length > 0" class="overflow-x-auto">
        <table class="w-full text-xs">
          <thead>
            <tr class="bg-zinc-950/50 text-xs uppercase tracking-widest text-zinc-500">
              <th class="text-left px-4 py-2 w-14">#</th>
              <th v-for="key in fieldKeys(cat)" :key="key" class="text-left px-4 py-2">{{ key }}</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-zinc-800/40 text-zinc-200">
            <tr v-for="(row, idx) in cat.mappedFields.slice((pageOf(cat) - 1) * PAGE_SIZE, pageOf(cat) * PAGE_SIZE)" :key="idx">
              <td class="px-4 py-2 font-mono text-zinc-500">{{ (pageOf(cat) - 1) * PAGE_SIZE + idx + 1 }}</td>
              <td v-for="key in fieldKeys(cat)" :key="key" class="px-4 py-2">{{ row[key] || "—" }}</td>
            </tr>
          </tbody>
        </table>
        <!-- Paginador -->
        <div v-if="totalPages(cat) > 1" class="flex items-center justify-between px-4 py-2 border-t border-zinc-800/40 bg-zinc-950/30">
          <span class="text-xs text-zinc-500">Página {{ pageOf(cat) }} de {{ totalPages(cat) }}</span>
          <div class="flex gap-2">
            <button class="px-3 py-1 rounded-md bg-zinc-800 border border-zinc-700/60 text-zinc-200 text-xs disabled:opacity-40" :disabled="pageOf(cat) <= 1" @click="setPage(cat, pageOf(cat) - 1)">← Anterior</button>
            <button class="px-3 py-1 rounded-md bg-zinc-800 border border-zinc-700/60 text-zinc-200 text-xs disabled:opacity-40" :disabled="pageOf(cat) >= totalPages(cat)" @click="setPage(cat, pageOf(cat) + 1)">Próxima →</button>
          </div>
        </div>
      </div>

      <!-- Erros por linha -->
      <div v-if="cat.errors.length > 0" class="p-4 space-y-1.5 border-t border-amber-500/20 bg-amber-500/5">
        <p v-for="err in cat.errors.slice(0, 5)" :key="err" class="text-xs text-amber-200/90">⚠ {{ err }}</p>
        <p v-if="cat.errors.length > 5" class="text-xs text-amber-300/60">+{{ cat.errors.length - 5 }} outros erros...</p>
      </div>
    </div>

    <!-- Ações -->
    <div class="flex items-center justify-between pt-2">
      <button @click="emit('back')" class="px-4 py-2 rounded-xl bg-zinc-800 hover:bg-zinc-700 text-zinc-200 text-xs font-medium border border-zinc-700/60">← Voltar</button>
      <button
        @click="confirm"
        :disabled="confirming || !canConfirm"
        class="px-6 py-2.5 rounded-xl bg-emerald-500/90 hover:bg-emerald-500 disabled:opacity-40 disabled:hover:bg-emerald-500/90 text-white text-sm font-semibold transition-all shadow shadow-emerald-500/20 flex items-center gap-2"
      >
        <span v-if="confirming" class="inline-block w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
        Confirmar importação
      </button>
    </div>

    <div v-if="futureCategories.length > 0" class="text-xs text-zinc-500 text-right">
      Categorias marcadas como "Em breve" ainda não possuem gravação no sistema.
    </div>
    <div v-if="confirmErrors.length > 0" class="p-4 rounded-xl bg-red-500/10 border border-red-500/30 space-y-1">
      <p v-for="err in confirmErrors" :key="err" class="text-xs text-red-300">✕ {{ err }}</p>
    </div>
  </div>
</template>
