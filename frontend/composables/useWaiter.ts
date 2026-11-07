import { computed, onMounted, onUnmounted, ref } from 'vue'
import { menuService } from '~/services/modules/menu/menuService'
import { orderService, OrderStatus, type Order, type CreateOrder } from '~/services/modules/orders/orderService'

export interface WaiterDish {
  id: number
  name: string
  category: string
  description: string
  price: number
  available: boolean
  urlImage?: string
}
export interface WaiterItem {
  dishId: number
  name: string
  price: number
  quantity: number
  observation: string
}
export type WaiterStatus = 'Enviado' | 'Em preparo' | 'Pronto' | 'Entregue' | 'Pago' | 'Cancelado'
const labels: Record<OrderStatus, WaiterStatus> = {
  [OrderStatus.Submitted]: 'Enviado', [OrderStatus.Preparing]: 'Em preparo',
  [OrderStatus.Ready]: 'Pronto', [OrderStatus.Completed]: 'Entregue',
  [OrderStatus.Paid]: 'Pago', [OrderStatus.Cancelled]: 'Cancelado'
}
export const isActiveOrder = (status: WaiterStatus) => ['Enviado', 'Em preparo', 'Pronto'].includes(status)
export function waiterError(error: unknown): string {
  const detail = (error as { data?: { detail?: string } })?.data?.detail
  return detail || 'Não foi possível conectar ao servidor. Tente novamente.'
}

export function useWaiter() {
  const auth = useAuthStore()
  // O rascunho é compartilhado entre rotas e isolado por usuário/restaurante.
  const owner = `${auth.currentUser?.restaurantId}:${auth.currentUser?.id}`
  const table = useState<string>(`waiter-table:${owner}`, () => '')
  const cart = useState<WaiterItem[]>(`waiter-cart:${owner}`, () => [])
  const pending = useState<{ fingerprint: string; request: CreateOrder } | null>(`waiter-request:${owner}`, () => null)
  const isSubmitting = useState<boolean>(`waiter-submitting:${owner}`, () => false)
  const dishes = ref<WaiterDish[]>([])
  const rawOrders = ref<Order[]>([])
  const menuLoading = ref(false)
  const ordersLoading = ref(false)
  const menuError = ref('')
  const ordersError = ref('')
  let disposed = false
  let timer: ReturnType<typeof setInterval> | undefined
  let mutationInFlight = false
  let revision = 0
  const orders = computed(() => rawOrders.value.map(order => ({
    ...order, status: labels[order.status],
    items: order.items.map(item => ({ dishId: item.dishId, name: item.dishName, price: item.unitPrice, quantity: item.quantity, observation: item.observation }))
  })))
  const categories = computed(() => ['Todos', ...new Set(dishes.value.map(dish => dish.category))])
  const activeOrders = computed(() => orders.value.filter(order => isActiveOrder(order.status)))
  const readyOrders = computed(() => orders.value.filter(order => order.status === 'Pronto'))
  const occupiedTables = computed(() => new Set(activeOrders.value.map(order => order.tableReference)).size)
  const total = (items: WaiterItem[]) => items.reduce((sum, item) => sum + item.price * item.quantity, 0)
  const cartTotal = computed(() => total(cart.value))
  const money = (value: number) => value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

  async function refreshMenu() {
    if (menuLoading.value || disposed) return
    menuLoading.value = true
    menuError.value = ''
    try {
      const [list, groups] = await Promise.all([menuService.getDishes(), menuService.getCategories()])
      if (disposed) return
      const names = new Map(groups.map(group => [group.id, group.name]))
      dishes.value = list.map(dish => ({ id: dish.id, name: dish.name, description: dish.description || '', price: dish.price,
        category: names.get(dish.categoryId ?? 0) || 'Sem categoria', available: dish.isActive, urlImage: dish.urlImage }))
    } catch (error) { if (!disposed) menuError.value = waiterError(error) }
    finally { menuLoading.value = false }
  }
  async function refreshOrders() {
    if (ordersLoading.value || mutationInFlight || disposed) return
    const startedAtRevision = revision
    ordersLoading.value = true
    try {
      const list = await orderService.list()
      // Uma consulta iniciada antes da mutação não pode sobrescrever sua resposta.
      if (!disposed && !mutationInFlight && revision === startedAtRevision) rawOrders.value = list
      if (!disposed) ordersError.value = ''
    } catch (error) { if (!disposed) ordersError.value = waiterError(error) }
    finally { ordersLoading.value = false }
  }
  function addDish(dish: WaiterDish) {
    if (!dish.available || isSubmitting.value) return
    cart.value.push({ dishId: dish.id, name: dish.name, price: dish.price, quantity: 1, observation: '' })
  }
  function changeQuantity(index: number, delta: number) {
    if (isSubmitting.value) return
    const item = cart.value[index]
    if (!item) return
    const quantity = item.quantity + delta
    if (quantity <= 0) cart.value.splice(index, 1)
    else item.quantity = Math.min(1000, quantity)
  }
  async function submitOrder() {
    if (isSubmitting.value) return null
    const content = { tableReference: table.value.trim(), items: cart.value.map(item => ({ dishId: item.dishId, quantity: item.quantity, observation: item.observation.trim() })) }
    if (!content.tableReference || content.tableReference.length > 100 || content.items.length < 1 || content.items.length > 100 ||
      content.items.some(item => !Number.isInteger(item.quantity) || item.quantity < 1 || item.quantity > 1000 || item.observation.length > 500)) {
      const error = new Error('Informe a mesa, de 1 a 100 itens e observações de até 500 caracteres.')
      error.name = 'WaiterValidationError'
      throw error
    }
    const fingerprint = JSON.stringify(content)
    if (pending.value?.fingerprint !== fingerprint) pending.value = { fingerprint, request: { ...content, requestId: crypto.randomUUID() } }
    isSubmitting.value = true
    revision++
    mutationInFlight = true
    try {
      const saved = await orderService.create(pending.value!.request)
      rawOrders.value = [saved, ...rawOrders.value.filter(order => order.id !== saved.id)]
      cart.value = []
      table.value = ''
      pending.value = null
      return saved.id
    } finally { isSubmitting.value = false; mutationInFlight = false }
  }
  async function confirmDelivery(id: number) {
    if (mutationInFlight) return false
    const order = rawOrders.value.find(item => item.id === id)
    if (!order || order.status !== OrderStatus.Ready) return false
    revision++
    mutationInFlight = true
    try {
      const saved = await orderService.updateStatus(order, OrderStatus.Completed)
      rawOrders.value = rawOrders.value.map(item => item.id === saved.id ? saved : item)
      return true
    } catch (error) {
      // Atualiza a versão após conflitos, preservando o erro original para a interface.
      try {
        const latest = await orderService.list()
        if (!disposed) { rawOrders.value = latest; ordersError.value = '' }
      } catch (refreshError) { if (!disposed) ordersError.value = waiterError(refreshError) }
      throw error
    } finally { mutationInFlight = false }
  }
  onMounted(() => {
    void refreshMenu()
    void refreshOrders()
    timer = setInterval(() => { if (!document.hidden) void refreshOrders() }, 10000)
  })
  onUnmounted(() => { disposed = true; if (timer) clearInterval(timer) })
  return { dishes, categories, table, cart, orders, cartTotal, activeOrders, readyOrders, occupiedTables,
    total, money, addDish, changeQuantity, submitOrder, confirmDelivery, isSubmitting,
    menuLoading, ordersLoading, menuError, ordersError, refreshMenu, refreshOrders }
}
