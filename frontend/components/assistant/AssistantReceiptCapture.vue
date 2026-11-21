<script setup lang="ts">
import { computed, onBeforeUnmount, ref } from "vue";
import { confirmReceipt, previewReceipt, type ReceiptPreviewDto, type ReceiptProductMappingDto, type ReceiptConfirmationResultDto } from "~/services/modules/import/importService";
import { productService, type ProductResponse } from "~/services/modules/stock/productService";
import { useConfirmation } from "~/composables/useConfirmation";

const props = withDefaults(defineProps<{ stockMode?: boolean }>(), { stockMode: false });
const emit = defineEmits<{ confirmed: [] }>();
const confirmation = useConfirmation();

const fileInput = ref<HTMLInputElement | null>(null);
const cameraInput = ref<HTMLInputElement | null>(null);
const file = ref<File | null>(null);
const previewUrl = ref("");
const result = ref<ReceiptPreviewDto | null>(null);
const loading = ref(false);
const confirming = ref(false);
const error = ref("");
const rotation = ref(0);
const dragging = ref(false);
const products = ref<ProductResponse[]>([]);
const productMappings = ref<ReceiptProductMappingDto[]>([]);
const confirmationResult = ref<ReceiptConfirmationResultDto | null>(null);
const idempotencyKey = ref("");
const recognizedSnapshot = ref("");
const editedAfterAnalysis = computed(() => result.value !== null && recognizedSnapshot.value !== JSON.stringify(result.value));
const statusLabel = computed(() => loading.value ? "Lendo documento…" : confirmationResult.value ? "Entrada registrada" : result.value ? "Prévia pronta · sem lançamento" : file.value?.name ?? "");

function openFilePicker() {
  fileInput.value?.click();
}

function openCamera() {
  cameraInput.value?.click();
}

function onDrop(event: DragEvent) {
  event.preventDefault();
  dragging.value = false;
  const dropped = event.dataTransfer?.files?.[0];
  if (dropped) selectFile({ target: { files: [dropped] } } as unknown as Event);
}

function selectFile(event: Event) {
  const selected = (event.target as HTMLInputElement).files?.[0];
  if (!selected) return;
  if (!/^image\/(png|jpeg|webp)$/i.test(selected.type) && selected.type !== "application/pdf" || selected.size > 12 * 1024 * 1024) {
    error.value = "Escolha uma imagem PNG, JPEG, WEBP ou PDF de até 12 MB.";
    return;
  }
  if (previewUrl.value) URL.revokeObjectURL(previewUrl.value);
  file.value = selected;
  previewUrl.value = selected.type === "application/pdf" ? "" : URL.createObjectURL(selected);
  result.value = null;
  confirmationResult.value = null;
  productMappings.value = [];
  recognizedSnapshot.value = "";
  idempotencyKey.value = "";
  error.value = "";
}

async function analyze() {
  if (!file.value || loading.value) return;
  if (!(await confirmation.ask({
    title: props.stockMode ? "Ler documento de entrada" : "Analisar recibo",
    message: "A imagem será enviada ao modelo multimodal para gerar uma prévia. Nenhum estoque será alterado nesta etapa.",
    confirmLabel: "Confirmar leitura"
  }))) return;
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
      conversionNote: "",
      newProductName: props.stockMode ? item.description : null,
      newProductUnit: props.stockMode ? (item.unit ?? "kg") : null,
      newProductMinimumQuantity: props.stockMode ? 0 : null,
      newProductMaximumQuantity: props.stockMode ? 0 : null
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
  if (productMappings.value.some(mapping => !mapping.quantityInStockUnit || mapping.quantityInStockUnit <= 0 || (!mapping.productId && (!props.stockMode || !mapping.newProductName?.trim() || !mapping.newProductUnit)))) {
    error.value = "Informe o destino, nome, unidade e quantidade de cada item antes de confirmar.";
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
    emit("confirmed");
  } catch (cause: any) {
    error.value = cause?.data?.detail || cause?.data || cause?.message || "Não foi possível confirmar a entrada.";
  } finally { confirming.value = false; }
}

onBeforeUnmount(() => { if (previewUrl.value) URL.revokeObjectURL(previewUrl.value); });
</script>

<template>
  <ConfirmationDialog :state="confirmation.state" @confirm="confirmation.accept" @cancel="confirmation.cancel" />
  <section
    :class="props.stockMode ? 'receipt-panel h-full rounded-2xl bg-zinc-950/35 p-4 text-zinc-100 sm:p-5' : 'mx-4 mb-3 rounded-2xl border border-indigo-200 bg-indigo-50/80 p-3 dark:border-indigo-400/20 dark:bg-indigo-400/5'"
    aria-label="Analisar recibo por imagem"
  >
    <input ref="fileInput" type="file" accept="image/png,image/jpeg,image/webp,application/pdf" class="sr-only" @change="selectFile" />
    <input ref="cameraInput" type="file" accept="image/*" capture="environment" class="sr-only" @change="selectFile" />

    <template v-if="props.stockMode">
      <div class="flex items-start gap-3">
        <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-indigo-500/15 text-indigo-300">
          <svg viewBox="0 0 24 24" class="h-5 w-5" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true">
            <path stroke-linecap="round" stroke-linejoin="round" d="M7 3.75h6.25L18.5 9v10.25A1.75 1.75 0 0 1 16.75 21h-9.5A1.75 1.75 0 0 1 5.5 19.25v-13.5A2 2 0 0 1 7.5 3.75Z" />
            <path stroke-linecap="round" stroke-linejoin="round" d="M13 3.75V9h5.25M8.5 14.25h7M8.5 17.25h4" />
          </svg>
        </div>
        <div class="min-w-0 flex-1">
          <div class="flex items-center justify-between gap-3">
            <div>
              <p class="text-sm font-semibold text-white">Importar nota de entrada</p>
              <p class="mt-1 text-xs leading-5 text-zinc-400">A IA prepara os itens para você revisar antes de lançar no estoque.</p>
            </div>
            <span v-if="statusLabel" class="hidden max-w-[45%] truncate rounded-full bg-zinc-900 px-2.5 py-1 text-[10px] font-medium text-zinc-400 sm:block">{{ statusLabel }}</span>
          </div>
        </div>
      </div>

      <Transition name="receipt-fade" mode="out-in">
        <div
          v-if="!file && !result"
          key="empty"
          class="mt-5 rounded-2xl bg-zinc-900/75 p-5 text-center transition-colors duration-200 hover:bg-zinc-900"
          :class="dragging ? 'bg-indigo-500/10 ring-1 ring-indigo-400/50' : ''"
          @click="openFilePicker"
          @dragover.prevent="dragging = true"
          @dragleave="dragging = false"
          @drop="onDrop"
        >
          <div class="mx-auto flex h-16 w-16 items-center justify-center rounded-2xl bg-indigo-500/10 text-indigo-300">
            <svg viewBox="0 0 24 24" class="h-9 w-9" fill="none" stroke="currentColor" stroke-width="1.5" aria-hidden="true">
              <path stroke-linecap="round" stroke-linejoin="round" d="M7 3.75h6.25L18.5 9v3.25M13 3.75V9h5.25M8.5 14.5h7M8.5 17.5h4" />
              <path stroke-linecap="round" stroke-linejoin="round" d="M14.75 17.25h4.5a2.25 2.25 0 1 0-.45-4.455A3.75 3.75 0 0 0 11.55 14a2.75 2.75 0 0 0 .45 5.46h2.75" />
              <path stroke-linecap="round" stroke-linejoin="round" d="m16.25 15.25-1.5-1.5-1.5 1.5M14.75 13.75v5" />
            </svg>
          </div>
          <p class="mt-4 text-sm font-semibold text-zinc-100">Arraste sua nota aqui</p>
          <p class="mt-1 text-xs text-zinc-500">ou selecione um arquivo para começar</p>
          <p class="mt-4 text-[11px] text-zinc-500">PNG, JPG, WEBP ou PDF · até 12 MB</p>
          <div class="mt-5 flex flex-col justify-center gap-2 sm:flex-row">
            <button type="button" class="rounded-xl bg-white px-4 py-2.5 text-xs font-semibold text-zinc-950 transition hover:bg-zinc-200" @click.stop="openFilePicker">Escolher arquivo</button>
            <button type="button" class="rounded-xl bg-zinc-800 px-4 py-2.5 text-xs font-semibold text-zinc-200 transition hover:bg-zinc-700" @click.stop="openCamera">Usar câmera</button>
          </div>
        </div>

        <div v-else-if="file && !result" key="selected" class="mt-5 rounded-2xl bg-zinc-900/75 p-4">
          <div class="flex items-center gap-3">
            <div class="h-16 w-16 shrink-0 overflow-hidden rounded-xl bg-zinc-800">
              <img v-if="previewUrl" :src="previewUrl" alt="Prévia da nota selecionada" class="h-full w-full object-cover" :style="{ transform: `rotate(${rotation}deg)` }" />
              <div v-else class="flex h-full items-center justify-center text-xs font-bold text-rose-300">PDF</div>
            </div>
            <div class="min-w-0 flex-1">
              <p class="truncate text-sm font-semibold text-zinc-100">{{ file.name }}</p>
              <p class="mt-1 text-xs text-zinc-500">{{ (file.size / 1024 / 1024).toFixed(2) }} MB · pronto para leitura</p>
            </div>
            <button type="button" class="rounded-lg px-2 py-1.5 text-xs font-medium text-zinc-400 transition hover:bg-zinc-800 hover:text-white" @click="openFilePicker">Trocar</button>
          </div>
          <div class="mt-4 flex flex-wrap items-center justify-between gap-3 border-t border-zinc-800 pt-4">
            <button v-if="previewUrl" type="button" class="text-xs font-medium text-indigo-300 transition hover:text-indigo-200" @click="rotation = (rotation + 90) % 360">Girar imagem 90°</button>
            <span v-else class="text-xs text-zinc-500">A primeira página será usada na leitura.</span>
            <button type="button" :disabled="loading" class="ml-auto rounded-xl bg-white px-4 py-2.5 text-xs font-semibold text-zinc-950 transition hover:bg-zinc-200 disabled:cursor-wait disabled:opacity-60" @click="analyze">{{ loading ? 'Lendo documento…' : 'Confirmar e ler documento' }}</button>
          </div>
        </div>
      </Transition>
    </template>

    <template v-else>
      <div class="flex flex-wrap items-center gap-2">
        <button type="button" class="rounded-lg border border-indigo-300 px-3 py-2 text-xs font-semibold text-indigo-800 hover:bg-white dark:border-indigo-300/20 dark:text-indigo-200 dark:hover:bg-zinc-900" @click="openCamera">Tirar foto</button>
        <button type="button" class="rounded-lg border border-indigo-300 px-3 py-2 text-xs font-semibold text-indigo-800 hover:bg-white dark:border-indigo-300/20 dark:text-indigo-200 dark:hover:bg-zinc-900" @click="openFilePicker">Anexar nota</button>
        <span v-if="statusLabel" class="min-w-0 flex-1 truncate text-xs text-zinc-600 dark:text-zinc-300">{{ statusLabel }}</span>
        <button v-if="file" type="button" :disabled="loading" class="rounded-lg bg-indigo-600 px-3 py-2 text-xs font-semibold text-white disabled:opacity-50" @click="analyze">{{ loading ? 'Lendo documento…' : 'Analisar' }}</button>
      </div>
      <div v-if="previewUrl" class="mt-3 flex items-start gap-3">
        <div class="relative h-28 w-24 shrink-0 overflow-hidden rounded-lg border border-zinc-200 bg-white dark:border-zinc-700 dark:bg-zinc-900">
          <img :src="previewUrl" alt="Prévia da imagem selecionada" class="h-full w-full object-contain transition-transform" :style="{ transform: `rotate(${rotation}deg)` }" />
        </div>
        <button type="button" class="text-xs text-indigo-700 underline dark:text-indigo-300" @click="rotation = (rotation + 90) % 360">Girar 90°</button>
      </div>
    </template>

    <div v-if="result" class="mt-5 space-y-4 rounded-2xl bg-zinc-900/80 p-4 text-xs sm:p-5">
      <div class="flex items-start justify-between gap-3">
        <div>
          <p class="text-sm font-semibold text-white">Prévia para conferência</p>
          <p class="mt-1 text-xs text-zinc-500">Revise os campos reconhecidos antes de confirmar a entrada.</p>
        </div>
        <span class="rounded-full bg-emerald-500/10 px-2.5 py-1 text-[10px] font-semibold text-emerald-300">{{ result.items.length }} itens</span>
      </div>
      <div class="grid grid-cols-1 gap-3 sm:grid-cols-2">
        <label class="space-y-1.5"><span class="text-[10px] font-semibold uppercase tracking-wide text-zinc-500">Fornecedor</span><input v-model="result.supplier" class="w-full rounded-xl border border-zinc-800 bg-zinc-950 px-3 py-2 text-xs text-zinc-100 outline-none transition focus:border-indigo-400" /></label>
        <label class="space-y-1.5"><span class="text-[10px] font-semibold uppercase tracking-wide text-zinc-500">Total reconhecido</span><input v-model.number="result.total" type="number" step="0.01" class="w-full rounded-xl border border-zinc-800 bg-zinc-950 px-3 py-2 text-xs text-zinc-100 outline-none transition focus:border-indigo-400" /></label>
      </div>
      <div class="space-y-2">
        <p class="text-[10px] font-semibold uppercase tracking-wide text-zinc-500">Itens encontrados</p>
        <div v-for="(item, index) in result.items" :key="`${index}-${item.description}`" class="grid grid-cols-2 gap-2 rounded-xl bg-zinc-950/70 p-2 sm:grid-cols-[minmax(0,2fr)_minmax(90px,0.8fr)_minmax(70px,0.6fr)_minmax(90px,0.8fr)]">
          <input v-model="item.description" :aria-label="`Descrição do item ${index + 1}`" class="min-w-0 rounded-lg border border-zinc-800 bg-zinc-900 px-2.5 py-2 text-xs text-zinc-100 outline-none focus:border-indigo-400" />
          <input v-model.number="item.quantity" :aria-label="`Quantidade do item ${index + 1}`" type="number" step="0.001" class="min-w-0 rounded-lg border border-zinc-800 bg-zinc-900 px-2.5 py-2 text-xs text-zinc-100 outline-none focus:border-indigo-400" />
          <input v-model="item.unit" :aria-label="`Unidade do item ${index + 1}`" class="min-w-0 rounded-lg border border-zinc-800 bg-zinc-900 px-2.5 py-2 text-xs text-zinc-100 outline-none focus:border-indigo-400" />
          <input v-model.number="item.unitPrice" :aria-label="`Preço unitário do item ${index + 1}`" type="number" step="0.01" class="min-w-0 rounded-lg border border-zinc-800 bg-zinc-900 px-2.5 py-2 text-xs text-zinc-100 outline-none focus:border-indigo-400" />
        </div>
      </div>
      <div v-if="result.decision === 'PREVIEW_REQUIRES_CONFIRMATION' && !confirmationResult" class="space-y-2 border-t border-zinc-200 pt-2 dark:border-zinc-700">
        <p class="font-semibold text-zinc-700 dark:text-zinc-200">{{ props.stockMode ? 'Confira cada insumo antes de lançar no estoque' : 'Mapeamento para o estoque · confirme unidades e embalagens' }}</p>
        <div v-for="(mapping, index) in productMappings" :key="`mapping-${index}`" class="grid grid-cols-1 gap-2 rounded-xl bg-zinc-950/60 p-3 sm:grid-cols-3">
          <label class="space-y-1"><span class="text-[10px] text-zinc-500">Destino da linha {{ index + 1 }}</span>
            <select v-model.number="mapping.productId" class="w-full rounded-lg border border-zinc-800 bg-zinc-900 px-2.5 py-2 text-xs text-zinc-100 outline-none focus:border-indigo-400">
              <option :value="0">{{ props.stockMode ? 'Criar novo insumo' : 'Selecione um produto' }}</option>
              <option v-for="product in products" :key="product.id" :value="product.id">{{ product.name }} ({{ product.unit }})</option>
            </select>
          </label>
          <label class="space-y-1"><span class="text-[10px] text-zinc-500">Quantidade na unidade do estoque</span>
            <input v-model.number="mapping.quantityInStockUnit" type="number" min="0.001" step="0.001" class="w-full rounded-lg border border-zinc-800 bg-zinc-900 px-2.5 py-2 text-xs text-zinc-100 outline-none focus:border-indigo-400" />
          </label>
          <label class="space-y-1"><span class="text-[10px] text-zinc-500">Justificativa de conversão</span>
            <input v-model="mapping.conversionNote" placeholder="Ex.: 1 pacote de 5 kg = 5 kg" class="w-full rounded-lg border border-zinc-800 bg-zinc-900 px-2.5 py-2 text-xs text-zinc-100 outline-none placeholder:text-zinc-600 focus:border-indigo-400" />
          </label>
          <template v-if="props.stockMode && !mapping.productId">
            <label class="space-y-1"><span class="text-[10px] text-zinc-500">Nome do novo insumo</span><input v-model="mapping.newProductName" class="w-full rounded-lg border border-zinc-800 bg-zinc-900 px-2.5 py-2 text-xs text-zinc-100 outline-none focus:border-indigo-400" /></label>
            <label class="space-y-1"><span class="text-[10px] text-zinc-500">Unidade</span><select v-model="mapping.newProductUnit" class="w-full rounded-lg border border-zinc-800 bg-zinc-900 px-2.5 py-2 text-xs text-zinc-100 outline-none focus:border-indigo-400"><option value="g">g</option><option value="kg">kg</option><option value="L">L</option><option value="mL">mL</option><option value="Un">Un</option></select></label>
            <label class="space-y-1"><span class="text-[10px] text-zinc-500">Estoque mínimo</span><input v-model.number="mapping.newProductMinimumQuantity" type="number" min="0" step="0.001" class="w-full rounded-lg border border-zinc-800 bg-zinc-900 px-2.5 py-2 text-xs text-zinc-100 outline-none focus:border-indigo-400" /></label>
            <label class="space-y-1"><span class="text-[10px] text-zinc-500">Estoque máximo</span><input v-model.number="mapping.newProductMaximumQuantity" type="number" min="0" step="0.001" class="w-full rounded-lg border border-zinc-800 bg-zinc-900 px-2.5 py-2 text-xs text-zinc-100 outline-none focus:border-indigo-400" /></label>
          </template>
        </div>
        <button type="button" :disabled="confirming || editedAfterAnalysis || !productMappings.length" class="rounded-xl bg-emerald-500 px-4 py-2.5 text-xs font-semibold text-zinc-950 transition hover:bg-emerald-400 disabled:opacity-50" @click="confirmImport">{{ confirming ? 'Conferindo e registrando…' : 'Confirmar entrada no estoque' }}</button>
        <p v-if="editedAfterAnalysis" class="text-amber-700 dark:text-amber-300">Campos reconhecidos alterados exigem nova análise antes da confirmação.</p>
      </div>
      <p v-if="confirmationResult" class="rounded-xl bg-emerald-500/10 p-3 text-emerald-200">Entrada registrada uma vez. Protocolo {{ confirmationResult.importId }}.</p>
      <p class="text-zinc-500">{{ result.items.length }} itens · {{ result.decision }}</p>
      <ul v-if="result.validationIssues.length || result.uncertainFields.length" class="list-disc pl-4 text-amber-700 dark:text-amber-300">
        <li v-for="issue in [...result.validationIssues, ...result.uncertainFields]" :key="issue">{{ issue }}</li>
      </ul>
      <p class="text-zinc-500">A análise não altera estoque. A confirmação revalida a imagem e registra auditoria. Recibos sintéticos não são documentos fiscais.</p>
    </div>
    <p v-if="error" role="alert" class="mt-2 text-xs text-rose-700 dark:text-rose-300">{{ error }}</p>
  </section>
</template>

<style scoped>
.receipt-panel {
  animation: receipt-panel-in 220ms ease-out both;
}

.receipt-fade-enter-active,
.receipt-fade-leave-active {
  transition: opacity 180ms ease, transform 180ms ease;
}

.receipt-fade-enter-from,
.receipt-fade-leave-to {
  opacity: 0;
  transform: translateY(5px);
}

@keyframes receipt-panel-in {
  from { opacity: 0; transform: translateY(5px); }
  to { opacity: 1; transform: translateY(0); }
}

@media (prefers-reduced-motion: reduce) {
  .receipt-panel,
  .receipt-fade-enter-active,
  .receipt-fade-leave-active {
    animation: none;
    transition: none;
  }
}
</style>
