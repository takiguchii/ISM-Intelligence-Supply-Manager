<script setup lang="ts">
import { computed, onBeforeUnmount, ref } from "vue";
import { confirmReceipt, previewReceipt, type ReceiptPreviewDto, type ReceiptProductMappingDto, type ReceiptConfirmationResultDto } from "~/services/modules/import/importService";
import { productService, type ProductResponse } from "~/services/modules/stock/productService";

const fileInput = ref<HTMLInputElement | null>(null);
const cameraInput = ref<HTMLInputElement | null>(null);
const file = ref<File | null>(null);
const previewUrl = ref("");
const result = ref<ReceiptPreviewDto | null>(null);
const loading = ref(false);
const confirming = ref(false);
const error = ref("");
const rotation = ref(0);
const products = ref<ProductResponse[]>([]);
const productMappings = ref<ReceiptProductMappingDto[]>([]);
const confirmationResult = ref<ReceiptConfirmationResultDto | null>(null);
const idempotencyKey = ref("");
const recognizedSnapshot = ref("");
const editedAfterAnalysis = computed(() => result.value !== null && recognizedSnapshot.value !== JSON.stringify(result.value));
const statusLabel = computed(() => loading.value ? "Analisando imagem…" : confirmationResult.value ? "Entrada registrada" : result.value ? "Prévia pronta · sem lançamento" : file.value?.name ?? "");

function selectFile(event: Event) {
  const selected = (event.target as HTMLInputElement).files?.[0];
  if (!selected) return;
  if (!/^image\/(png|jpeg|webp)$/i.test(selected.type) || selected.size > 12 * 1024 * 1024) {
    error.value = "Escolha uma imagem PNG, JPEG ou WEBP de até 12 MB.";
    return;
  }
  if (previewUrl.value) URL.revokeObjectURL(previewUrl.value);
  file.value = selected;
  previewUrl.value = URL.createObjectURL(selected);
  result.value = null;
  confirmationResult.value = null;
  productMappings.value = [];
  recognizedSnapshot.value = "";
  idempotencyKey.value = "";
  error.value = "";
}

async function analyze() {
  if (!file.value || loading.value) return;
  loading.value = true;
  error.value = "";
  try {
    result.value = await previewReceipt(file.value);
    recognizedSnapshot.value = JSON.stringify(result.value);
    products.value = (await productService.getAll()).filter(product => product.isActive);
    productMappings.value = result.value.items.map((item, lineIndex) => ({
      lineIndex,
      productId: 0,
      quantityInStockUnit: item.quantity ?? 0,
      conversionNote: ""
    }));
    idempotencyKey.value = crypto.randomUUID();
  }
  catch (cause: any) { error.value = cause?.data?.detail || cause?.data || cause?.message || "Não foi possível analisar a imagem."; }
  finally { loading.value = false; }
}

async function confirmImport() {
  if (!file.value || !result.value || confirming.value || confirmationResult.value) return;
  if (result.value.decision !== "PREVIEW_REQUIRES_CONFIRMATION") {
    error.value = "Esta prévia exige revisão e não permite entrada direta.";
    return;
  }
  if (editedAfterAnalysis.value) {
    error.value = "Os campos reconhecidos foram alterados. A confirmação automática usa os dados revalidados pelo servidor; faça uma nova análise antes de confirmar.";
    return;
  }
  if (productMappings.value.some(mapping => !mapping.productId || !mapping.quantityInStockUnit || mapping.quantityInStockUnit <= 0)) {
    error.value = "Associe cada linha a um produto e informe a quantidade na unidade do estoque.";
    return;
  }
  confirming.value = true;
  error.value = "";
  try {
    confirmationResult.value = await confirmReceipt(file.value, {
      confirm: true,
      idempotencyKey: idempotencyKey.value,
      expectedSha256: result.value.sha256,
      productMappings: productMappings.value
    });
  } catch (cause: any) {
    error.value = cause?.data?.detail || cause?.data || cause?.message || "Não foi possível confirmar a entrada.";
  } finally { confirming.value = false; }
}

onBeforeUnmount(() => { if (previewUrl.value) URL.revokeObjectURL(previewUrl.value); });
</script>

<template>
  <section class="mx-4 mb-3 rounded-2xl border border-indigo-200 bg-indigo-50/80 p-3 dark:border-indigo-400/20 dark:bg-indigo-400/5" aria-label="Analisar recibo por imagem">
    <div class="flex flex-wrap items-center gap-2">
      <input ref="fileInput" type="file" accept="image/png,image/jpeg,image/webp" class="sr-only" @change="selectFile" />
      <input ref="cameraInput" type="file" accept="image/*" capture="environment" class="sr-only" @change="selectFile" />
      <button type="button" class="rounded-lg border border-indigo-300 px-3 py-2 text-xs font-semibold text-indigo-800 hover:bg-white dark:border-indigo-300/20 dark:text-indigo-200 dark:hover:bg-zinc-900" @click="cameraInput?.click()">Tirar foto</button>
      <button type="button" class="rounded-lg border border-indigo-300 px-3 py-2 text-xs font-semibold text-indigo-800 hover:bg-white dark:border-indigo-300/20 dark:text-indigo-200 dark:hover:bg-zinc-900" @click="fileInput?.click()">Anexar imagem</button>
      <span v-if="statusLabel" class="min-w-0 flex-1 truncate text-xs text-zinc-600 dark:text-zinc-300">{{ statusLabel }}</span>
      <button v-if="file" type="button" :disabled="loading" class="rounded-lg bg-indigo-600 px-3 py-2 text-xs font-semibold text-white disabled:opacity-50" @click="analyze">{{ loading ? 'Analisando…' : 'Analisar' }}</button>
    </div>
    <div v-if="previewUrl" class="mt-3 flex items-start gap-3">
      <div class="relative h-28 w-24 shrink-0 overflow-hidden rounded-lg border border-zinc-200 bg-white dark:border-zinc-700 dark:bg-zinc-900">
        <img :src="previewUrl" alt="Prévia da imagem selecionada" class="h-full w-full object-contain transition-transform" :style="{ transform: `rotate(${rotation}deg)` }" />
      </div>
      <button type="button" class="text-xs text-indigo-700 underline dark:text-indigo-300" @click="rotation = (rotation + 90) % 360">Girar 90°</button>
    </div>
    <div v-if="result" class="mt-3 space-y-2 rounded-xl bg-white p-3 text-xs dark:bg-zinc-900">
      <div class="grid grid-cols-2 gap-2">
        <label class="space-y-1"><span class="text-[10px] text-zinc-500">Fornecedor</span><input v-model="result.supplier" class="w-full rounded-lg border border-zinc-200 bg-white px-2 py-1.5 text-xs dark:border-zinc-700 dark:bg-zinc-950" /></label>
        <label class="space-y-1"><span class="text-[10px] text-zinc-500">Total reconhecido</span><input v-model.number="result.total" type="number" step="0.01" class="w-full rounded-lg border border-zinc-200 bg-white px-2 py-1.5 text-xs dark:border-zinc-700 dark:bg-zinc-950" /></label>
      </div>
      <div v-for="(item, index) in result.items" :key="`${index}-${item.description}`" class="grid grid-cols-5 gap-1.5">
        <input v-model="item.description" :aria-label="`Descrição do item ${index + 1}`" class="col-span-2 min-w-0 rounded-lg border border-zinc-200 bg-white px-2 py-1.5 text-xs dark:border-zinc-700 dark:bg-zinc-950" />
        <input v-model.number="item.quantity" :aria-label="`Quantidade do item ${index + 1}`" type="number" step="0.001" class="min-w-0 rounded-lg border border-zinc-200 bg-white px-2 py-1.5 text-xs dark:border-zinc-700 dark:bg-zinc-950" />
        <input v-model="item.unit" :aria-label="`Unidade do item ${index + 1}`" class="min-w-0 rounded-lg border border-zinc-200 bg-white px-2 py-1.5 text-xs dark:border-zinc-700 dark:bg-zinc-950" />
        <input v-model.number="item.unitPrice" :aria-label="`Preço unitário do item ${index + 1}`" type="number" step="0.01" class="min-w-0 rounded-lg border border-zinc-200 bg-white px-2 py-1.5 text-xs dark:border-zinc-700 dark:bg-zinc-950" />
      </div>
      <div v-if="result.decision === 'PREVIEW_REQUIRES_CONFIRMATION' && !confirmationResult" class="space-y-2 border-t border-zinc-200 pt-2 dark:border-zinc-700">
        <p class="font-semibold text-zinc-700 dark:text-zinc-200">Mapeamento para o estoque · confirme unidades e embalagens</p>
        <div v-for="(mapping, index) in productMappings" :key="`mapping-${index}`" class="grid grid-cols-1 gap-1.5 sm:grid-cols-3">
          <label class="space-y-1"><span class="text-[10px] text-zinc-500">Produto da linha {{ index + 1 }}</span>
            <select v-model.number="mapping.productId" class="w-full rounded-lg border border-zinc-200 bg-white px-2 py-1.5 text-xs dark:border-zinc-700 dark:bg-zinc-950">
              <option :value="0">Selecione um produto</option>
              <option v-for="product in products" :key="product.id" :value="product.id">{{ product.name }} ({{ product.unit }})</option>
            </select>
          </label>
          <label class="space-y-1"><span class="text-[10px] text-zinc-500">Quantidade na unidade do estoque</span>
            <input v-model.number="mapping.quantityInStockUnit" type="number" min="0.001" step="0.001" class="w-full rounded-lg border border-zinc-200 bg-white px-2 py-1.5 text-xs dark:border-zinc-700 dark:bg-zinc-950" />
          </label>
          <label class="space-y-1"><span class="text-[10px] text-zinc-500">Justificativa de conversão</span>
            <input v-model="mapping.conversionNote" placeholder="Ex.: 1 pacote de 5 kg = 5 kg" class="w-full rounded-lg border border-zinc-200 bg-white px-2 py-1.5 text-xs dark:border-zinc-700 dark:bg-zinc-950" />
          </label>
        </div>
        <button type="button" :disabled="confirming || editedAfterAnalysis || !productMappings.length" class="rounded-lg bg-emerald-600 px-3 py-2 text-xs font-semibold text-white disabled:opacity-50" @click="confirmImport">{{ confirming ? 'Conferindo e registrando…' : 'Confirmar entrada no estoque' }}</button>
        <p v-if="editedAfterAnalysis" class="text-amber-700 dark:text-amber-300">Campos reconhecidos alterados exigem nova análise antes da confirmação.</p>
      </div>
      <p v-if="confirmationResult" class="rounded-lg bg-emerald-50 p-2 text-emerald-800 dark:bg-emerald-500/10 dark:text-emerald-200">Entrada registrada uma vez. Protocolo {{ confirmationResult.importId }}.</p>
      <p class="text-zinc-600 dark:text-zinc-300">{{ result.items.length }} itens · {{ result.decision }}</p>
      <ul v-if="result.validationIssues.length || result.uncertainFields.length" class="list-disc pl-4 text-amber-700 dark:text-amber-300">
        <li v-for="issue in [...result.validationIssues, ...result.uncertainFields]" :key="issue">{{ issue }}</li>
      </ul>
      <p class="text-zinc-500 dark:text-zinc-400">A análise não altera estoque. A confirmação revalida a imagem e registra auditoria. Recibos sintéticos não são documentos fiscais.</p>
    </div>
    <p v-if="error" role="alert" class="mt-2 text-xs text-rose-700 dark:text-rose-300">{{ error }}</p>
  </section>
</template>
