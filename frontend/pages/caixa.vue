<script setup lang="ts">
import CashShiftDetails from '~/components/cash/CashShiftDetails.vue'
import { cashDate, cashMoney, cashCents, cashOrderTotal, createCashService, CashMovementKind, PaymentMethod, paymentLabels, type LegacyPayment, type Payment, type Receipt, type Shift, type ShiftSummary } from '~/services/modules/cash/cashService'
import { OrderStatus, orderService, type Order } from '~/services/modules/orders/orderService'

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
const receivedOrderIds = ref<number[]>([])
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
const selectedShift = ref<number | null>(null)
const payments = ref<Payment[]>([])
const retry = ref<{ id: string; payload: string } | null>(null)
const receipt = ref<Receipt | null>(null)
const receiptOpen = ref(false)
const dialog = ref<'payment' | 'movement' | 'close' | null>(null)
const dialogOpen = computed({ get: () => dialog.value !== null, set: value => { if (!value) dialog.value = null } })

const methods = [PaymentMethod.Cash, PaymentMethod.Pix, PaymentMethod.Debit, PaymentMethod.Credit]
const orderTotal = cashOrderTotal
const allocated = computed(() => payments.value.reduce((total, payment) => total + cashCents(payment.amount), 0))
const queue = computed(() => orders.value.filter(order =>
  order.status === OrderStatus.Completed && !receivedOrderIds.value.includes(order.id) && `${order.id} ${order.tableReference}`.toLowerCase().includes(search.value.trim().replace(/^#/, '').toLowerCase())))
const pendingAmount = computed(() => queue.value.reduce((total, order) => total + orderTotal(order), 0) / 100)
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
  const [current, list] = await Promise.all([cashService().current(), orderService.list()])
  active.value = current.shift
  orders.value = list
  receivedOrderIds.value = current.receivedOrderIds
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

function startPayment(order: Order) {
  if (!active.value) return
  selected.value = order
  selectedShift.value = active.value.shift.id
  payments.value = [{ method: PaymentMethod.Pix, amount: orderTotal(order) / 100, tendered: null }]
  dialogError.value = ''
  dialog.value = 'payment'
}

function addPayment() {
  const method = methods.find(candidate => !payments.value.some(payment => payment.method === candidate))
  if (method === undefined || !selected.value) return
  payments.value.push({ method, amount: Math.max(0, orderTotal(selected.value) - allocated.value) / 100, tendered: null })
}

function showReceipt(value: Receipt) {
  receipt.value = value
  receiptOpen.value = true
}

async function confirmPayment() {
  const order = selected.value
  const shift = selectedShift.value
  if (!order || shift === null) return
  const parts = payments.value.map(payment => ({
    method: payment.method, amount: payment.amount,
    tendered: payment.method === PaymentMethod.Cash ? payment.tendered ?? null : null
  }))
  if (allocated.value !== orderTotal(order) || parts.some(part => !Number.isFinite(part.amount) || part.amount < 0 || (part.amount === 0 && parts.length > 1) ||
    (part.method === PaymentMethod.Cash && (part.tendered === null || part.tendered < part.amount)))) {
    dialogError.value = 'Confira o total, os valores das parcelas e o dinheiro entregue.'
    return
  }
  const payload = JSON.stringify({ shift, order: order.id, parts })
  if (retry.value?.payload !== payload) retry.value = { payload, id: crypto.randomUUID() }
  const saved = await cashService().receive(shift, order, retry.value.id, parts)
  receivedOrderIds.value = [...receivedOrderIds.value, order.id]
  retry.value = null
  dialog.value = null
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
  const order = queue.value.find(item => item.id === Number(route.query.pedido))
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
      Caixa local. Turnos e recebimentos ficam neste navegador. O status dos pedidos e o financeiro do ISM continuam sendo gerenciados pelo sistema.
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
            <p v-if="loaded" class="text-sm text-zinc-500">{{ queue.length }} pedidos · {{ cashMoney(pendingAmount) }}</p>
          </div>
          <p v-if="!loaded && busy" class="text-sm text-zinc-500">Carregando pedidos…</p>
          <p v-else-if="loaded && !queue.length" class="rounded-xl border border-zinc-200 p-4 text-sm text-zinc-500 dark:border-zinc-700">Nenhum pedido entregue aguardando pagamento.</p>
          <div v-else class="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
            <article v-for="order in queue" :key="order.id" class="space-y-3 rounded-xl border border-zinc-200 p-4 dark:border-zinc-700">
              <div>
                <h2 class="font-semibold">Comanda {{ order.tableReference }}</h2>
                <p class="text-sm text-zinc-500">Pedido #{{ order.id }} · {{ cashDate(order.createdAtUtc) }}</p>
              </div>
              <p class="text-lg font-semibold">{{ cashMoney(orderTotal(order) / 100) }}</p>
              <el-button type="primary" :disabled="!active || busy" @click="startPayment(order)">Receber</el-button>
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

    <el-dialog v-if="dialogOpen" v-model="dialogOpen" :title="dialogTitle" width="min(94vw, 640px)" :close-on-click-modal="false" :close-on-press-escape="!busy" :show-close="!busy">
      <form class="space-y-4" @submit.prevent="run(dialog === 'payment' ? confirmPayment : confirmShiftAction, true)">
        <el-alert v-if="dialogError" :title="dialogError" type="error" show-icon :closable="false" />
        <template v-if="dialog === 'payment' && selected">
          <div>
            <h2 class="font-semibold">Pedido #{{ selected.id }} · Comanda {{ selected.tableReference }}</h2>
            <ul class="my-2 space-y-1">
              <li v-for="(item, index) in selected.items" :key="index" class="flex justify-between gap-3">
                <span>{{ item.quantity }}× {{ item.dishName }}</span>
                <span>{{ cashMoney(item.unitPrice * item.quantity) }}</span>
              </li>
            </ul>
            <p class="text-lg font-semibold">Total {{ cashMoney(orderTotal(selected) / 100) }}</p>
          </div>
          <fieldset v-for="(payment, index) in payments" :key="index" :disabled="busy" class="grid gap-2 rounded-xl border border-zinc-200 p-3 sm:grid-cols-2 dark:border-zinc-700">
            <legend class="px-1 text-sm font-medium">Parcela {{ index + 1 }}</legend>
            <label class="grid gap-1">Forma de pagamento
              <el-select v-model="payment.method">
                <el-option v-for="method in methods" :key="method" :label="paymentLabels[method]" :value="method"
                  :disabled="payments.some((part, partIndex) => partIndex !== index && part.method === method)" />
              </el-select>
            </label>
            <label class="grid gap-1">Valor da parcela
              <el-input-number v-model="payment.amount" :precision="2" :step="0.01" :min="0" :controls="false" class="!w-full" />
            </label>
            <template v-if="payment.method === PaymentMethod.Cash">
              <label class="grid gap-1">Dinheiro entregue
                <el-input-number v-model="payment.tendered" :precision="2" :step="0.01" :min="0" :controls="false" class="!w-full" />
              </label>
              <p class="self-end py-2">Troco {{ cashMoney(Math.max(0, (payment.tendered ?? 0) - (payment.amount || 0))) }}</p>
            </template>
            <button v-if="payments.length > 1" type="button" class="justify-self-start text-red-600" @click="payments.splice(index, 1)">Remover parcela</button>
          </fieldset>
          <div class="flex flex-wrap items-center justify-between gap-2">
            <el-button :disabled="busy || payments.length === methods.length" @click="addPayment">Dividir pagamento</el-button>
            <p>Restante {{ cashMoney((orderTotal(selected) - (allocated || 0)) / 100) }}</p>
          </div>
        </template>

        <template v-else-if="dialog === 'movement'">
          <label class="grid gap-1">Tipo
            <el-select v-model="movementKind" :disabled="busy">
              <el-option label="Suprimento" :value="CashMovementKind.Deposit" />
              <el-option label="Sangria" :value="CashMovementKind.Withdrawal" />
            </el-select>
          </label>
          <label class="grid gap-1">Valor
            <el-input-number v-model="movementAmount" :precision="2" :step="0.01" :min="0" :controls="false" :disabled="busy" />
          </label>
          <label class="grid gap-1">Motivo
            <el-input v-model="reason" maxlength="250" :disabled="busy" />
          </label>
        </template>

        <template v-else-if="dialog === 'close' && active">
          <p>Dinheiro esperado {{ cashMoney(active.expectedCash) }}</p>
          <label class="grid gap-1">Dinheiro contado
            <el-input-number v-model="countedAmount" :precision="2" :step="0.01" :min="0" :controls="false" :disabled="busy" />
          </label>
          <p v-if="countedAmount != null">Diferença {{ cashMoney(countedAmount - active.expectedCash) }}</p>
        </template>

        <div class="flex justify-end">
          <el-button :disabled="busy" @click="dialog = null">Voltar</el-button>
          <el-button type="primary" native-type="submit" :loading="busy">
            {{ dialog === 'payment' ? 'Confirmar recebimento' : dialog === 'close' ? 'Confirmar fechamento' : 'Registrar' }}
          </el-button>
        </div>
      </form>
    </el-dialog>

    <el-dialog v-if="receiptOpen" v-model="receiptOpen" title="Comprovante interno" width="min(94vw, 480px)">
      <div v-if="receipt" class="space-y-3">
        <p class="font-semibold">Recebimento #{{ receipt.id }} · Pedido #{{ receipt.orderId }}</p>
        <p>Turno #{{ receipt.shiftId }} · Operador #{{ receipt.operatorId }}</p>
        <p>{{ cashDate(receipt.createdAtUtc) }}</p>
        <ul class="space-y-2">
          <li v-for="payment in receipt.payments" :key="payment.method">
            {{ paymentLabels[payment.method] }} · {{ cashMoney(payment.amount) }}
            <p v-if="payment.tendered !== null" class="text-sm text-zinc-500">Entregue {{ cashMoney(payment.tendered) }} · Troco {{ cashMoney(payment.change) }}</p>
          </li>
        </ul>
        <p class="font-semibold">Total {{ cashMoney(receipt.payments.reduce((sum, part) => sum + part.amount, 0)) }}</p>
      </div>
    </el-dialog>
  </main>
</template>
