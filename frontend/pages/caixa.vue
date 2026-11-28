<script setup lang="ts">
import CashShiftDetails from '~/components/cash/CashShiftDetails.vue'
import { cashDate, cashMoney, cashCents, cashOrderTotal, createCashService, CashOrderUnavailableError, CashMovementKind, PaymentMethod, paymentLabels, type LegacyPayment, type Payment, type Receipt, type Shift, type ShiftSummary } from '~/services/modules/cash/cashService'
import { OrderStatus, orderService, type Order } from '~/services/modules/orders/orderService'
import { BackendPaymentMethod, tabService, type OpenTab } from '~/services/modules/orders/tabService'

const route = useRoute()
const auth = useAuthStore()
function cashService() {
  const user = auth.currentUser
  if (!user?.restaurantId) throw new Error('Selecione um restaurante para acessar o caixa.')
  return createCashService(user.restaurantId, user.id)
}
const active = ref<Shift | null>(null)
const historical = ref<Shift | null>(null)
const history = ref<ShiftSummary[]>([])
const page = ref(1)
const legacy = ref<LegacyPayment[]>([])
const legacyPage = ref(1)
const orders = ref<Order[]>([])
const openTabs = ref<OpenTab[]>([])
const search = ref('')
const tab = ref('queue')
const error = ref('')
const dialogError = ref('')
const busy = ref(false)
const loaded = ref(false)
const openingAmount = ref<number | undefined>(0)
const countedAmount = ref<number>()
const movementAmount = ref<number>()
const movementKind = ref(CashMovementKind.Deposit)
const reason = ref('')
const selected = ref<Order | null>(null)
const selectedTab = ref<OpenTab | null>(null)
const selectedShift = ref<number | null>(null)
const payments = ref<Payment[]>([])
const retry = ref<{ id: string; payload: string } | null>(null)
const receipt = ref<Receipt | null>(null)
const receiptOpen = ref(false)
const dialog = ref<'payment' | 'movement' | 'close' | null>(null)
const dialogOpen = computed({ get: () => dialog.value !== null, set: value => { if (!value) dialog.value = null } })

const methods = [PaymentMethod.Cash, PaymentMethod.Pix, PaymentMethod.Debit, PaymentMethod.Credit]
const backendPaymentMethod = (method: PaymentMethod) => ({
  [PaymentMethod.Cash]: BackendPaymentMethod.Cash,
  [PaymentMethod.Pix]: BackendPaymentMethod.Pix,
  [PaymentMethod.Debit]: BackendPaymentMethod.DebitCard,
  [PaymentMethod.Credit]: BackendPaymentMethod.CreditCard
}[method])
const orderTotal = cashOrderTotal
const allocated = computed(() => payments.value.reduce((total, payment) => total + cashCents(payment.amount), 0))
const queue = computed(() => openTabs.value.filter(tab => {
  const term = search.value.trim().replace(/^#/, '').toLowerCase()
  if (!term) return true
  return `${tab.tableReference} ${tab.orderIds.join(' ')}`.toLowerCase().includes(term)
}))
const pendingAmount = computed(() => queue.value.reduce((total, tab) => total + cashCents(tab.total), 0) / 100)
const dialogTitle = computed(() => ({
  payment: 'Receber pedido', movement: 'Registrar movimentação', close: 'Fechar turno'
})[dialog.value ?? 'payment'])

function errorMessage(cause: unknown) {
  const data = (cause as { data?: { detail?: string; title?: string } })?.data
  return data?.detail ?? data?.title ?? (cause instanceof Error ? cause.message : 'Não foi possível concluir a operação. Tente novamente.')
}

async function run(action: () => Promise<void>, inDialog = false) {
  if (busy.value) return
  const feedback = inDialog ? dialogError : error
  feedback.value = ''
  busy.value = true
  try { await action() }
  catch (cause) { feedback.value = errorMessage(cause) }
  finally { busy.value = false }
}

async function load() {
  const [current, list, tabs] = await Promise.all([cashService().current(), orderService.list(), tabService.listOpen()])
  active.value = current.shift
  orders.value = list
  openTabs.value = tabs
  loaded.value = true
}

async function loadHistory(nextPage = 1) {
  const result = await cashService().history(nextPage)
  if (result.length || nextPage === 1) {
    page.value = nextPage
    history.value = result
  }
}

async function loadLegacy(nextPage = 1) {
  const list = await orderService.list()
  const result = list.filter(order => order.status === OrderStatus.Paid)
    .slice((nextPage - 1) * 20, nextPage * 20)
    .map(order => ({ orderId: order.id, tableReference: order.tableReference, paidAtUtc: order.paidAtUtc, amount: orderTotal(order) / 100 }))
  if (result.length || nextPage === 1) {
    legacyPage.value = nextPage
    legacy.value = result
  }
}

function startPayment(tab: OpenTab) {
  if (!active.value) return
  selectedTab.value = tab
  selected.value = orders.value.find(order => order.id === tab.orderIds[0]) ?? null
  selectedShift.value = active.value.shift.id
  payments.value = [{ method: PaymentMethod.Pix, amount: tab.total, tendered: null }]
  dialogError.value = ''
  dialog.value = 'payment'
}

function addPayment() {
  const method = methods.find(candidate => !payments.value.some(payment => payment.method === candidate))
  if (method === undefined || !selectedTab.value) return
  payments.value.push({ method, amount: Math.max(0, cashCents(selectedTab.value.total) - allocated.value) / 100, tendered: null })
}

function showReceipt(value: Receipt) {
  receipt.value = value
  receiptOpen.value = true
}

async function confirmPayment() {
  const order = selected.value
  const tab = selectedTab.value
  const shift = selectedShift.value
  if (!order || !tab || shift === null) return
  const parts = payments.value.map(payment => ({
    method: payment.method, amount: payment.amount,
    tendered: payment.method === PaymentMethod.Cash ? payment.tendered ?? null : null
  }))
  if (parts.length !== 1 || allocated.value !== cashCents(tab.total) || parts.some(part => !Number.isFinite(part.amount) || part.amount < 0 || (part.amount === 0 && parts.length > 1) ||
    (part.method === PaymentMethod.Cash && (part.tendered === null || part.tendered < part.amount)))) {
    dialogError.value = 'Confira o total, os valores das parcelas e o dinheiro entregue.'
    return
  }
  if (!tab.canPay) {
    dialogError.value = 'A comanda possui pedidos ainda em preparo. Aguarde a cozinha concluir.'
    return
  }
  const payload = JSON.stringify({ shift, tab: tab.id, parts })
  if (retry.value?.payload !== payload) retry.value = { payload, id: crypto.randomUUID() }
  let saved: Receipt
  try {
    const paid = await tabService.pay(tab.id, retry.value.id, backendPaymentMethod(parts[0].method), tab.total)
    saved = {
      id: paid.tabId, orderId: order.id, shiftId: shift, operatorId: auth.currentUser?.id ?? 0,
      createdAtUtc: paid.paidAtUtc,
      payments: [{ ...parts[0], amount: paid.total, change: parts[0].method === PaymentMethod.Cash ? Math.max(0, (parts[0].tendered ?? 0) - paid.total) : 0 }]
    }
  } catch (cause) {
    if (!(cause instanceof CashOrderUnavailableError)) throw cause
    dialog.value = null
    selected.value = null
    selectedTab.value = null
    selectedShift.value = null
    retry.value = null
    error.value = cause.message
    try { await load() }
    catch { error.value += ' Não foi possível atualizar a fila. Use Atualizar para tentar novamente.' }
    return
  }
  retry.value = null
  dialog.value = null
  selectedTab.value = null
  showReceipt(saved)
  try { await load() }
  catch (cause) { error.value = errorMessage(cause) }
}

function startShiftAction(action: 'movement' | 'close') {
  dialogError.value = ''
  movementKind.value = CashMovementKind.Deposit
  movementAmount.value = undefined
  countedAmount.value = undefined
  reason.value = ''
  dialog.value = action
}

async function confirmShiftAction() {
  const current = active.value
  if (!current) return
  if (dialog.value === 'movement') {
    if (!movementAmount.value || !reason.value.trim()) {
      dialogError.value = 'Informe o valor e o motivo da movimentação.'
      return
    }
    active.value = await cashService().move(current.shift.id, current.version, movementKind.value, movementAmount.value, reason.value)
  } else {
    if (countedAmount.value === undefined || countedAmount.value === null) {
      dialogError.value = 'Informe o dinheiro contado.'
      return
    }
    historical.value = await cashService().close(current.shift.id, current.version, countedAmount.value)
    active.value = null
    tab.value = 'history'
    openingAmount.value = 0
    dialog.value = null
    try { await loadHistory() }
    catch (cause) { error.value = errorMessage(cause) }
  }
  dialog.value = null
}

async function openShift() {
  if (openingAmount.value === undefined || openingAmount.value === null) {
    error.value = 'Informe o saldo inicial.'
    return
  }
  active.value = await cashService().open(openingAmount.value)
  tab.value = 'queue'
  openFromQuery()
}

function openFromQuery() {
  const order = queue.value.find(item => item.orderIds.includes(Number(route.query.pedido)))
  if (order && active.value && !dialog.value) startPayment(order)
}

onMounted(() => run(async () => {
  await load()
  openFromQuery()
}))

watch(() => route.query.pedido, openFromQuery)
watch(tab, value => {
  if (value === 'history' && !busy.value) void run(() => loadHistory())
  if (value === 'legacy' && !busy.value) void run(() => loadLegacy())
})
</script>

<template>
  <main class="mx-auto w-full max-w-7xl space-y-6 px-4 py-8 dark:text-zinc-100 sm:px-6">
    <header class="flex flex-wrap items-center justify-between gap-4">
      <div>
        <h1 class="text-2xl font-bold">Operação de caixa</h1>
        <p v-if="active" class="text-sm text-zinc-500">Turno #{{ active.shift.id }} · Aberto em {{ cashDate(active.shift.createdAtUtc) }}</p>
        <p v-else-if="loaded" class="text-sm text-zinc-500">Nenhum turno aberto</p>
      </div>
      <div class="flex flex-wrap gap-2">
        <el-button :loading="busy" @click="run(load)">Atualizar</el-button>
        <template v-if="active">
          <el-button :disabled="busy" @click="startShiftAction('movement')">Suprimento ou sangria</el-button>
          <el-button :disabled="busy" @click="startShiftAction('close')">Fechar turno</el-button>
        </template>
      </div>
    </header>

    <p class="rounded-xl border border-amber-200 bg-amber-50 p-3 text-sm text-amber-950 dark:border-amber-800 dark:bg-amber-950 dark:text-amber-100">
      Turnos são mantidos neste navegador; recebimentos, status dos pedidos e indicadores financeiros são persistidos no servidor.
    </p>
    <el-alert v-if="error" :title="error" type="error" show-icon :closable="false" />
    <form v-if="loaded && !active" class="flex flex-wrap items-end gap-3 rounded-2xl border border-zinc-200 p-4 dark:border-zinc-700" @submit.prevent="run(openShift)">
      <label class="grid gap-1 text-sm font-medium">
        Saldo inicial em dinheiro
        <el-input-number v-model="openingAmount" :precision="2" :step="0.01" :min="0" :controls="false" />
      </label>
      <el-button type="primary" native-type="submit" :loading="busy">Abrir turno</el-button>
    </form>

    <el-tabs v-model="tab" stretch>
      <el-tab-pane label="Fila de recebimento" name="queue">
        <section class="space-y-4">
          <div class="flex flex-wrap items-center justify-between gap-3">
            <el-input v-model="search" clearable class="!max-w-xs" placeholder="Buscar mesa, comanda ou pedido" aria-label="Buscar mesa, comanda ou pedido" />
            <p v-if="loaded" class="text-sm text-zinc-500">{{ queue.length }} comandas · {{ cashMoney(pendingAmount) }}</p>
          </div>
          <p v-if="!loaded && busy" class="text-sm text-zinc-500">Carregando pedidos…</p>
          <p v-else-if="loaded && !queue.length" class="rounded-xl border border-zinc-200 p-4 text-sm text-zinc-500 dark:border-zinc-700">Nenhum pedido entregue aguardando pagamento.</p>
          <div v-else class="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
            <article v-for="tab in queue" :key="tab.id" class="space-y-3 rounded-xl border border-zinc-200 p-4 dark:border-zinc-700">
              <div>
                <h2 class="font-semibold">Comanda {{ tab.tableReference }}</h2>
                <p class="text-sm text-zinc-500">Pedidos #{{ tab.orderIds.join(', #') }}</p>
              </div>
              <p class="text-lg font-semibold">{{ cashMoney(tab.total) }}</p>
              <p v-if="!tab.canPay" class="text-sm text-amber-700">Há pedido em preparo.</p>
              <el-button type="primary" :disabled="!active || busy || !tab.canPay" @click="startPayment(tab)">Receber</el-button>
            </article>
          </div>
        </section>
      </el-tab-pane>
      <el-tab-pane label="Turno atual" name="current">
        <CashShiftDetails v-if="active" :details="active" @receipt="showReceipt" />
        <p v-else class="text-sm text-zinc-500">Abra um turno para registrar recebimentos.</p>
      </el-tab-pane>
      <el-tab-pane label="Histórico" name="history">
        <div class="grid gap-5 lg:grid-cols-[16rem_1fr]">
          <div class="space-y-3">
            <p v-if="!history.length" class="text-sm text-zinc-500">Nenhum turno encerrado.</p>
            <ul class="space-y-2">
              <li v-for="entry in history" :key="entry.id">
                <button type="button" class="w-full rounded-xl border border-zinc-200 p-3 text-left hover:border-indigo-500 dark:border-zinc-700"
                  :disabled="busy" :aria-pressed="historical?.shift.id === entry.id"
                  @click="run(async () => { historical = await cashService().get(entry.id) })">
                  <span class="block font-semibold">Turno #{{ entry.id }}</span>
                  <span v-if="entry.closedAtUtc" class="text-sm text-zinc-500">{{ cashDate(entry.closedAtUtc) }}</span>
                </button>
              </li>
            </ul>
            <div v-if="history.length" class="flex flex-wrap gap-2">
              <el-button size="small" :disabled="busy || page === 1" @click="run(() => loadHistory(page - 1))">Anterior</el-button>
              <el-button size="small" :disabled="busy || history.length < 20" @click="run(() => loadHistory(page + 1))">Próxima</el-button>
            </div>
          </div>
          <section v-if="historical" class="space-y-3">
            <h2 class="font-semibold">Turno #{{ historical.shift.id }} · Operador de fechamento #{{ historical.closedByUserId }}</h2>
            <CashShiftDetails :details="historical" @receipt="showReceipt" />
          </section>
        </div>
      </el-tab-pane>
      <el-tab-pane label="Recebimentos anteriores" name="legacy">
        <p v-if="!legacy.length" class="text-sm text-zinc-500">Nenhum recebimento anterior encontrado.</p>
        <ul v-else class="divide-y divide-zinc-200 dark:divide-zinc-700">
          <li v-for="entry in legacy" :key="entry.orderId" class="flex flex-wrap justify-between gap-3 py-3">
            <div>
              <p class="font-medium">Pedido #{{ entry.orderId }} · Comanda {{ entry.tableReference }}</p>
              <p class="text-sm text-zinc-500">
                Forma não registrada<span v-if="entry.paidAtUtc"> · {{ cashDate(entry.paidAtUtc) }}</span>
              </p>
            </div>
            <p class="font-semibold">{{ cashMoney(entry.amount) }}</p>
          </li>
        </ul>
        <div v-if="legacy.length" class="mt-3 flex gap-2">
          <el-button size="small" :disabled="busy || legacyPage === 1" @click="run(() => loadLegacy(legacyPage - 1))">Anterior</el-button>
          <el-button size="small" :disabled="busy || legacy.length < 20" @click="run(() => loadLegacy(legacyPage + 1))">Próxima</el-button>
        </div>
      </el-tab-pane>
    </el-tabs>

    <el-dialog class="ism-cash-dialog" append-to-body align-center v-if="dialogOpen" v-model="dialogOpen" :title="dialogTitle" :width="dialog === 'payment' ? 'min(94vw, 680px)' : 'min(94vw, 480px)'" :close-on-click-modal="false" :close-on-press-escape="!busy" :show-close="!busy">
      <form class="cash-dialog-form" @submit.prevent="run(dialog === 'payment' ? confirmPayment : confirmShiftAction, true)">
        <div class="cash-dialog-body space-y-4">
          <el-alert v-if="dialogError" :title="dialogError" type="error" show-icon :closable="false" />
          <template v-if="dialog === 'payment' && selected && selectedTab">
            <div class="cash-panel cash-panel-accent">
              <h2 class="text-sm font-semibold">Comanda {{ selectedTab.tableReference }} · Pedidos #{{ selectedTab.orderIds.join(', #') }}</h2>
              <ul class="my-3 max-h-32 space-y-2 overflow-auto text-sm">
                <template v-for="order in orders.filter(item => selectedTab?.orderIds.includes(item.id))" :key="order.id">
                  <li v-for="(item, index) in order.items" :key="`${order.id}-${index}`" class="flex justify-between gap-3">
                    <span>{{ item.quantity }}× {{ item.dishName }}</span>
                    <span class="shrink-0 tabular-nums">{{ cashMoney(item.unitPrice * item.quantity) }}</span>
                  </li>
                </template>
              </ul>
              <div class="cash-total"><span>Total da comanda</span><strong>{{ cashMoney(selectedTab.total) }}</strong></div>
            </div>
            <section v-for="(payment, index) in payments" :key="index" class="cash-panel space-y-3"
              :aria-label="`Pagamento ${index + 1}`">
              <div class="flex flex-wrap items-center justify-between gap-2">
                <h3 class="font-semibold">Pagamento {{ index + 1 }}</h3>
                <el-button v-if="payments.length > 1" text type="danger" :disabled="busy" @click="payments.splice(index, 1)">
                  Remover
                </el-button>
              </div>
              <fieldset :disabled="busy" class="grid min-w-0 gap-3 sm:grid-cols-2">
                <label class="cash-field">Forma de pagamento
                  <el-select popper-class="ism-cash-options" v-model="payment.method" :disabled="busy">
                    <el-option v-for="method in methods" :key="method" :label="paymentLabels[method]" :value="method"
                      :disabled="payments.some((part, partIndex) => partIndex !== index && part.method === method)" />
                  </el-select>
                </label>
                <label class="cash-field">Valor do pagamento
                  <el-input-number v-model="payment.amount" :precision="2" :step="0.01" :min="0" :controls="false" :disabled="busy" />
                </label>
                <template v-if="payment.method === PaymentMethod.Cash">
                  <label class="cash-field">Dinheiro entregue
                    <el-input-number v-model="payment.tendered" :precision="2" :step="0.01" :min="0" :controls="false" :disabled="busy" />
                  </label>
                  <div class="cash-result self-end" aria-live="polite">
                    <span>Troco</span>
                    <strong>{{ cashMoney(Math.max(0, (payment.tendered ?? 0) - (payment.amount || 0))) }}</strong>
                  </div>
                </template>
              </fieldset>
            </section>
            <div class="flex flex-wrap items-center justify-between gap-3">
              <el-button :disabled="busy || payments.length === methods.length" @click="addPayment">Dividir pagamento</el-button>
              <p class="cash-result" aria-live="polite">
                <span>{{ allocated > cashCents(selectedTab.total) ? 'Valor excedente' : 'Restante' }}</span>
                <strong>{{ cashMoney(Math.abs(cashCents(selectedTab.total) - (allocated || 0)) / 100) }}</strong>
              </p>
            </div>
          </template>

          <template v-else-if="dialog === 'movement'">
            <label class="cash-field">Tipo de movimentação
              <el-select popper-class="ism-cash-options" v-model="movementKind" :disabled="busy">
                <el-option label="Suprimento" :value="CashMovementKind.Deposit" />
                <el-option label="Sangria" :value="CashMovementKind.Withdrawal" />
              </el-select>
            </label>
            <label class="cash-field">Valor em dinheiro
              <el-input-number v-model="movementAmount" :precision="2" :step="0.01" :min="0" :controls="false" :disabled="busy" />
            </label>
            <label class="cash-field">Motivo
              <el-input v-model="reason" type="textarea" :rows="3" maxlength="250" show-word-limit resize="vertical" :disabled="busy" />
            </label>
          </template>

          <template v-else-if="dialog === 'close' && active">
            <div class="cash-panel cash-panel-accent cash-total"><span>Dinheiro esperado</span><strong>{{ cashMoney(active.expectedCash) }}</strong></div>
            <label class="cash-field">Dinheiro contado
              <el-input-number v-model="countedAmount" :precision="2" :step="0.01" :min="0" :controls="false" :disabled="busy" />
            </label>
            <div v-if="countedAmount != null" class="cash-result" aria-live="polite">
              <span>Diferença no caixa</span>
              <strong>{{ cashMoney(countedAmount - active.expectedCash) }}</strong>
            </div>
          </template>

        </div>
        <div class="cash-dialog-actions">
          <el-button :disabled="busy" @click="dialog = null">Voltar</el-button>
          <el-button type="primary" native-type="submit" :loading="busy">
            {{ dialog === 'payment' ? 'Confirmar recebimento' : dialog === 'close' ? 'Confirmar fechamento' : 'Registrar' }}
          </el-button>
        </div>
      </form>
    </el-dialog>

    <el-dialog class="ism-cash-dialog" append-to-body align-center v-if="receiptOpen" v-model="receiptOpen" title="Comprovante interno" width="min(94vw, 480px)">
      <div v-if="receipt" class="cash-dialog-form">
        <div class="cash-dialog-body space-y-4">
          <div class="cash-panel cash-panel-accent">
            <h2 class="font-semibold">Pedido #{{ receipt.orderId }} · Recebimento #{{ receipt.id }}</h2>
            <p class="cash-muted mt-1">Turno #{{ receipt.shiftId }} · Operador #{{ receipt.operatorId }}</p>
            <p class="cash-muted">{{ cashDate(receipt.createdAtUtc) }}</p>
          </div>
          <div class="cash-panel">
            <ul class="space-y-3">
              <li v-for="payment in receipt.payments" :key="payment.method">
                <div class="flex justify-between gap-3">
                  <span>{{ paymentLabels[payment.method] }}</span>
                  <strong class="tabular-nums">{{ cashMoney(payment.amount) }}</strong>
                </div>
                <p v-if="payment.tendered !== null" class="cash-muted mt-1 text-xs">
                  Entregue {{ cashMoney(payment.tendered) }} · Troco {{ cashMoney(payment.change) }}
                </p>
              </li>
            </ul>
            <div class="cash-total mt-4 border-t pt-3">
              <span>Total recebido</span>
              <strong>{{ cashMoney(receipt.payments.reduce((sum, part) => sum + part.amount, 0)) }}</strong>
            </div>
          </div>
        </div>
        <div class="cash-dialog-actions">
          <el-button type="primary" @click="receiptOpen = false">Fechar comprovante</el-button>
        </div>
      </div>
    </el-dialog>
  </main>
</template>

<style>
.ism-cash-dialog.el-dialog {
  --cash-ink: var(--el-text-color-primary);
  --cash-muted: var(--el-text-color-secondary);
  --cash-border: var(--el-border-color-light);
  --cash-panel: var(--el-fill-color-extra-light);
  padding: 0;
  border: 1px solid var(--cash-border);
  border-radius: 20px;
  overflow: hidden;
  box-shadow: var(--el-box-shadow-dark);
}

html.dark .ism-cash-dialog,
html.dark .ism-cash-options {
  --el-bg-color: #18181b;
  --el-bg-color-overlay: #18181b;
  --el-text-color-primary: #fafafa;
  --el-text-color-regular: #e4e4e7;
  --el-text-color-secondary: #a1a1aa;
  --el-text-color-placeholder: #a1a1aa;
  --el-border-color: #52525b;
  --el-border-color-light: #3f3f46;
  --el-fill-color-blank: #18181b;
  --el-fill-color-light: #27272a;
  --el-fill-color-extra-light: #202024;
  --el-fill-color: #2c2c31;
  --el-color-primary-light-7: #284977;
  --el-color-primary-light-9: #19273b;
}

.ism-cash-dialog .el-dialog__header {
  margin: 0;
  padding: 18px 54px 16px 22px;
  border-bottom: 1px solid var(--cash-border);
}

.ism-cash-dialog .el-dialog__title {
  font-size: 19px;
  font-weight: 650;
  color: var(--cash-ink);
}

.ism-cash-dialog .el-dialog__headerbtn {
  height: 54px;
}

.ism-cash-dialog .cash-dialog-form {
  display: flex;
  flex-direction: column;
  max-height: calc(90dvh - 70px);
  color: var(--cash-ink);
}

.ism-cash-dialog .cash-dialog-body {
  min-height: 0;
  overflow-y: auto;
  padding: 18px 22px;
}

.ism-cash-dialog .cash-dialog-actions {
  display: flex;
  flex-wrap: wrap;
  justify-content: flex-end;
  gap: 8px;
  padding: 14px 22px;
  border-top: 1px solid var(--cash-border);
  background: var(--cash-panel);
}

.ism-cash-dialog .cash-dialog-actions .el-button {
  margin: 0;
}

.ism-cash-dialog .cash-panel {
  padding: 14px;
  border: 1px solid var(--cash-border);
  border-radius: 14px;
  background: var(--cash-panel);
}

.ism-cash-dialog .cash-panel-accent {
  background: var(--el-color-primary-light-9);
  border-color: var(--el-color-primary-light-7);
}

.ism-cash-dialog .cash-total,
.ism-cash-dialog .cash-result {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  justify-content: space-between;
  gap: 6px 12px;
  border-color: var(--cash-border);
}

.ism-cash-dialog .cash-total strong {
  font-size: 19px;
  font-variant-numeric: tabular-nums;
}

.ism-cash-dialog .cash-result {
  padding: 7px 10px;
  border-radius: 8px;
  background: var(--el-fill-color);
}

.ism-cash-dialog .cash-field {
  display: grid;
  min-width: 0;
  gap: 5px;
  font-size: 13px;
  font-weight: 550;
}

.ism-cash-dialog .cash-field .el-input-number {
  width: 100%;
}

.ism-cash-dialog .cash-field .el-input-number input {
  text-align: start;
}

.ism-cash-dialog .cash-muted {
  color: var(--cash-muted);
}

@media (max-width: 480px) {
  .ism-cash-dialog .el-dialog__header,
  .ism-cash-dialog .cash-dialog-body,
  .ism-cash-dialog .cash-dialog-actions {
    padding-left: 14px;
  }

  .ism-cash-dialog .cash-dialog-body,
  .ism-cash-dialog .cash-dialog-actions {
    padding-right: 14px;
  }
}
</style>
