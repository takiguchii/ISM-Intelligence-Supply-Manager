import { OrderStatus, orderService, type Order } from '~/services/modules/orders/orderService'

export class CashOrderUnavailableError extends Error {}

async function validateCurrentOrder(order: Order): Promise<Order> {
  let current: Order
  try {
    current = await orderService.getById(order.id)
  } catch (cause) {
    const failure = cause as { statusCode?: number; status?: number; response?: { status?: number } }
    if ((failure?.statusCode ?? failure?.status ?? failure?.response?.status) === 404) {
      throw new CashOrderUnavailableError('O pedido não foi encontrado. A fila de recebimento será atualizada.')
    }
    throw new Error('Não foi possível conferir o pedido no servidor. Nenhum recebimento foi registrado. Tente novamente.', { cause })
  }
  if (current.status === OrderStatus.Cancelled) {
    throw new CashOrderUnavailableError('O pedido foi cancelado. Nenhum recebimento foi registrado.')
  }
  if (current.status === OrderStatus.Paid) {
    throw new CashOrderUnavailableError('O pedido já está pago no sistema. Nenhum novo recebimento foi registrado.')
  }
  if (current.status !== OrderStatus.Completed || order.status !== OrderStatus.Completed) {
    throw new CashOrderUnavailableError('Somente pedidos entregues podem ser recebidos. Confira a fila atualizada.')
  }
  if (current.version !== order.version) {
    throw new CashOrderUnavailableError('O pedido foi alterado. Confira os itens e valores atualizados antes de receber novamente.')
  }
  return current
}

export enum PaymentMethod { Cash, Pix, Debit, Credit }
export enum CashMovementKind { Deposit, Withdrawal }

export const paymentLabels: Record<PaymentMethod, string> = {
  [PaymentMethod.Cash]: 'Dinheiro',
  [PaymentMethod.Pix]: 'Pix',
  [PaymentMethod.Debit]: 'Débito',
  [PaymentMethod.Credit]: 'Crédito'
}

export interface Payment {
  method: PaymentMethod
  amount: number
  tendered: number | null
}

export interface Receipt {
  id: number
  orderId: number
  shiftId: number
  operatorId: number
  createdAtUtc: string
  payments: (Payment & { change: number })[]
}

export interface LegacyPayment {
  orderId: number
  tableReference: string
  paidAtUtc: string | null
  amount: number
}

export interface ShiftSummary {
  id: number
  createdAtUtc: string
  closedAtUtc: string | null
  openingAmount: number
  countedAmount: number | null
}

export interface Shift {
  shift: ShiftSummary
  version: number
  openedByUserId: number
  closedByUserId: number | null
  expectedCash: number
  totals: Payment[]
  receipts: Receipt[]
  movements: {
    id: number
    kind: CashMovementKind
    amount: number
    reason: string
    operatorId: number
    createdAtUtc: string
  }[]
}

interface CashState {
  nextId: number
  shifts: Shift[]
}

interface LocalReceipt extends Receipt {
  requestId: string
}

const initialState = (): CashState => ({ nextId: 1, shifts: [] })
const today = () => new Date().toISOString()
export const cashCents = (amount: number) => Math.round(amount * 100)
export const cashOrderTotal = (order: Order) =>
  order.items.reduce((sum, item) => sum + cashCents(item.unitPrice) * item.quantity, 0)

function validateAmount(amount: number) {
  const cents = cashCents(amount)
  if (!Number.isSafeInteger(cents) || amount < 0 || cents / 100 !== amount) {
    throw new Error('Informe um valor válido com até duas casas decimais.')
  }
  return cents
}

function recalculate(details: Shift) {
  const totals = new Map<PaymentMethod, number>()
  let expected = cashCents(details.shift.openingAmount)

  for (const receipt of details.receipts) {
    for (const payment of receipt.payments) {
      const cents = cashCents(payment.amount)
      totals.set(payment.method, (totals.get(payment.method) ?? 0) + cents)
      if (payment.method === PaymentMethod.Cash) expected += cents
    }
  }
  for (const movement of details.movements) {
    const cents = cashCents(movement.amount)
    expected += movement.kind === CashMovementKind.Deposit ? cents : -cents
  }

  details.expectedCash = expected / 100
  details.totals = Array.from(totals, ([method, cents]) => ({ method, amount: cents / 100, tendered: null }))
}

export function createCashService(restaurantId: number, operatorId: number) {
  const key = `ism-cash-v1:${restaurantId}`
  function read(): CashState {
    const saved = localStorage.getItem(key)
    return saved === null ? initialState() : JSON.parse(saved)
  }

  async function write<T>(update: (state: CashState) => T): Promise<T> {
    const persist = () => {
      const state = read()
      const result = update(state)
      localStorage.setItem(key, JSON.stringify(state))
      return result
    }
    return navigator.locks ? navigator.locks.request(key, persist) : persist()
  }

  function openShift(state: CashState, id: number, version?: number) {
    const details = state.shifts.find(shift => shift.shift.id === id && !shift.shift.closedAtUtc)
    if (!details) throw new Error('O turno está encerrado. Atualize o caixa.')
    if (version !== undefined && details.version !== version) {
      throw new Error('O turno mudou em outra aba. Atualize o caixa antes de confirmar.')
    }
    return details
  }

  return {
    current: async () => {
      const { shifts } = read()
      return {
        shift: shifts.find(details => !details.shift.closedAtUtc) ?? null,
        receivedOrderIds: shifts.flatMap(details => details.receipts.map(receipt => receipt.orderId))
      }
    },
    get: async (id: number) => {
      const details = read().shifts.find(candidate => candidate.shift.id === id)
      if (!details) throw new Error('Turno não encontrado.')
      return details
    },
    history: async (page: number) => read().shifts.filter(details => details.shift.closedAtUtc)
      .slice((page - 1) * 20, page * 20).map(details => details.shift),
    open: (openingAmount: number) => write(state => {
      validateAmount(openingAmount)
      if (state.shifts.some(details => !details.shift.closedAtUtc)) {
        throw new Error('Já existe um turno aberto neste navegador.')
      }
      const details: Shift = {
        shift: { id: state.nextId++, createdAtUtc: today(), closedAtUtc: null, openingAmount, countedAmount: null },
        version: 0, openedByUserId: operatorId, closedByUserId: null,
        expectedCash: openingAmount, totals: [], receipts: [], movements: []
      }
      state.shifts.unshift(details)
      return details
    }),
    close: (id: number, version: number, countedAmount: number) => write(state => {
      const details = openShift(state, id, version)
      validateAmount(countedAmount)
      details.shift.countedAmount = countedAmount
      details.shift.closedAtUtc = today()
      details.closedByUserId = operatorId
      details.version++
      return details
    }),
    move: (id: number, version: number, kind: CashMovementKind, amount: number, reason: string) => write(state => {
      const details = openShift(state, id, version)
      const cents = validateAmount(amount)
      if (!cents || !reason.trim() || reason.trim().length > 250) {
        throw new Error('Informe um valor positivo e um motivo de até 250 caracteres.')
      }
      if (kind === CashMovementKind.Withdrawal && cents > cashCents(details.expectedCash)) {
        throw new Error('A sangria excede o dinheiro esperado no turno.')
      }
      details.movements.unshift({ id: state.nextId++, kind, amount, reason: reason.trim(), operatorId, createdAtUtc: today() })
      details.version++
      recalculate(details)
      return details
    }),
    receive: async (shiftId: number, order: Order, requestId: string, payments: Payment[]) => {
      // Uma falha na consulta deve impedir a gravação e a emissão do comprovante.
      const current = await validateCurrentOrder(order)
      return write(state => {
        const previous = state.shifts.flatMap(details => details.receipts as LocalReceipt[])
          .find(receipt => receipt.orderId === order.id)
        if (previous) {
          if (previous.requestId === requestId && previous.shiftId === shiftId) return previous
          throw new Error('Este pedido já tem um recebimento registrado neste navegador.')
        }

        const details = openShift(state, shiftId)
        const total = cashOrderTotal(current)
        if (!payments.length || payments.length > 4 || new Set(payments.map(part => part.method)).size !== payments.length ||
            (total === 0 && payments.length !== 1) || payments.reduce((sum, part) => sum + validateAmount(part.amount), 0) !== total) {
          throw new Error('A soma dos pagamentos precisa ser igual ao total do pedido.')
        }
        const parts = payments.map(part => {
          if (!(part.method in paymentLabels) || (total > 0 && part.amount === 0)) {
            throw new Error('Informe uma forma válida e um valor positivo para cada parcela.')
          }
          if (part.method !== PaymentMethod.Cash) return { ...part, tendered: null, change: 0 }
          if (part.tendered === null || validateAmount(part.tendered) < cashCents(part.amount)) {
            throw new Error('O dinheiro entregue precisa cobrir a parcela em dinheiro.')
          }
          return { ...part, change: (cashCents(part.tendered) - cashCents(part.amount)) / 100 }
        })
        const receipt: LocalReceipt = {
          id: state.nextId++, requestId, orderId: order.id, shiftId, operatorId, createdAtUtc: today(), payments: parts
        }
        details.receipts.unshift(receipt)
        details.version++
        recalculate(details)
        return receipt
      })
    }
  }
}

export const cashMoney = (amount: number) =>
  new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(amount)

export const cashDate = (date: string) =>
  new Date(/(?:Z|[+-]\d\d:\d\d)$/.test(date) ? date : `${date}Z`).toLocaleString('pt-BR')
