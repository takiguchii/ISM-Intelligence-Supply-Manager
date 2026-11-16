import { computed, onMounted, onUnmounted, ref } from 'vue'
import { orderService, OrderStatus, type Order } from '~/services/modules/orders/orderService'

export function useKitchen() {
  const orders = ref<Order[]>([])
  const loading = ref(false)
  const updatingId = ref<number | null>(null)
  const error = ref('')
  const feedback = ref('')
  const lastUpdated = ref<Date | null>(null)
  let disposed = false
  let revision = 0
  let query = 0
  let timer: ReturnType<typeof setInterval> | undefined
  const activeOrders = computed(() => orders.value
    .filter(order => [OrderStatus.Submitted, OrderStatus.Preparing, OrderStatus.Ready].includes(order.status))
    .sort((a, b) => a.createdAtUtc.localeCompare(b.createdAtUtc) || a.id - b.id))

  function message(cause: unknown) {
    return (cause as { data?: { detail?: string } })?.data?.detail || 'Não foi possível conectar à cozinha. Tente novamente.'
  }

  async function refresh(force = false) {
    if (disposed || (!force && (loading.value || updatingId.value !== null))) return
    const currentQuery = ++query
    const currentRevision = revision
    loading.value = true
    try {
      const list = await orderService.list()
      if (!disposed && currentQuery === query && currentRevision === revision && updatingId.value === null) {
        orders.value = list
        error.value = ''
        lastUpdated.value = new Date()
      }
    } catch (cause) {
      if (!disposed && currentQuery === query && currentRevision === revision) error.value = message(cause)
    } finally {
      if (currentQuery === query) loading.value = false
    }
  }

  async function advance(id: number) {
    if (disposed || updatingId.value !== null) return false
    const order = orders.value.find(order => order.id === id)
    if (!order || ![OrderStatus.Submitted, OrderStatus.Preparing].includes(order.status)) return false
    const next = order.status === OrderStatus.Submitted ? OrderStatus.Preparing : OrderStatus.Ready
    updatingId.value = id
    revision++
    error.value = ''
    feedback.value = ''
    try {
      const saved = await orderService.updateStatus(order, next)
      if (!disposed) {
        orders.value = orders.value.map(item => item.id === saved.id ? saved : item)
        feedback.value = next === OrderStatus.Ready
          ? `Pedido #${saved.id} despachado para o salão. O garçom já pode confirmar a entrega.`
          : `Preparo do pedido #${saved.id} iniciado.`
      }
      return true
    } catch (cause) {
      updatingId.value = null
      // Recupera a versão atual, sem ocultar a mensagem da ação que falhou.
      await refresh(true)
      if (!disposed) error.value = message(cause)
      return false
    } finally {
      updatingId.value = null
    }
  }

  function onVisible() { if (!document.hidden) void refresh() }
  onMounted(() => {
    void refresh()
    timer = setInterval(onVisible, 10000)
    document.addEventListener('visibilitychange', onVisible)
  })
  onUnmounted(() => {
    disposed = true
    if (timer) clearInterval(timer)
    document.removeEventListener('visibilitychange', onVisible)
  })
  return { orders, activeOrders, loading, updatingId, error, feedback, lastUpdated, refresh, advance }
}
