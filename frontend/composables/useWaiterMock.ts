import { computed } from 'vue'

export interface WaiterDish {
  id: number
  name: string
  category: string
  description: string
  price: number
  available: boolean
}

export interface WaiterItem {
  dishId: number
  name: string
  price: number
  quantity: number
  observation: string
}

export type WaiterStatus =
  | 'Enviado'
  | 'Em preparo'
  | 'Pronto'
  | 'Entregue'

export interface WaiterOrder {
  id: number
  table: string
  status: WaiterStatus
  items: WaiterItem[]
}

export function useWaiterMock() {
  // useState mantém os dados entre as páginas do Nuxt.
  const dishes = useState<WaiterDish[]>('waiter-mock-dishes', () => [
    {
      id: 1,
      name: 'Bruschetta Italiana',
      category: 'Entradas',
      description: 'Pão italiano, tomate, manjericão e azeite.',
      price: 32,
      available: true
    },
    {
      id: 2,
      name: 'Dadinhos de Tapioca',
      category: 'Entradas',
      description: 'Porção acompanhada de geleia de pimenta.',
      price: 28.5,
      available: true
    },
    {
      id: 3,
      name: 'Filé Mignon ao Poivre',
      category: 'Pratos principais',
      description: 'Filé com molho de pimenta e risoto de parmesão.',
      price: 89,
      available: true
    },
    {
      id: 4,
      name: 'Salmão Grelhado',
      category: 'Pratos principais',
      description: 'Salmão acompanhado de legumes salteados.',
      price: 78,
      available: false
    },
    {
      id: 5,
      name: 'Suco de Laranja',
      category: 'Bebidas',
      description: 'Suco natural de laranja, copo de 400 ml.',
      price: 12,
      available: true
    },
    {
      id: 6,
      name: 'Água Mineral',
      category: 'Bebidas',
      description: 'Garrafa de 500 ml, sem gás.',
      price: 6,
      available: true
    },
    {
      id: 7,
      name: 'Petit Gâteau',
      category: 'Sobremesas',
      description: 'Bolo de chocolate com sorvete de baunilha.',
      price: 34,
      available: true
    }
  ])

  const table = useState<string>('waiter-mock-table', () => '')
  const cart = useState<WaiterItem[]>('waiter-mock-cart', () => [])

  const orders = useState<WaiterOrder[]>('waiter-mock-orders', () => [
    {
      id: 1001,
      table: '03',
      status: 'Pronto',
      items: [
        {
          dishId: 3,
          name: 'Filé Mignon ao Poivre',
          price: 89,
          quantity: 1,
          observation: 'Carne ao ponto'
        }
      ]
    },
    {
      id: 1002,
      table: '07',
      status: 'Em preparo',
      items: [
        {
          dishId: 2,
          name: 'Dadinhos de Tapioca',
          price: 28.5,
          quantity: 2,
          observation: ''
        },
        {
          dishId: 5,
          name: 'Suco de Laranja',
          price: 12,
          quantity: 2,
          observation: 'Sem açúcar'
        }
      ]
    }
  ])

  const categories = computed(() => [
    'Todos',
    ...new Set(dishes.value.map(dish => dish.category))
  ])

  const cartTotal = computed(() => total(cart.value))

  const activeOrders = computed(() =>
    orders.value.filter(order => order.status !== 'Entregue')
  )

  const readyOrders = computed(() =>
    orders.value.filter(order => order.status === 'Pronto')
  )

  const occupiedTables = computed(
    () => new Set(activeOrders.value.map(order => order.table)).size
  )

  function total(items: WaiterItem[]) {
    return items.reduce(
      (sum, item) => sum + item.price * item.quantity,
      0
    )
  }

  function money(value: number) {
    return value.toLocaleString('pt-BR', {
      style: 'currency',
      currency: 'BRL'
    })
  }

  function addDish(dish: WaiterDish) {
    if (!dish.available) return

    // Linhas separadas permitem observações diferentes para o mesmo prato.
    cart.value.push({
      dishId: dish.id,
      name: dish.name,
      price: dish.price,
      quantity: 1,
      observation: ''
    })
  }

  function changeQuantity(index: number, delta: number) {
    const item = cart.value[index]
    if (!item) return

    const quantity = item.quantity + delta

    if (quantity <= 0) {
      cart.value.splice(index, 1)
      return
    }

    item.quantity = quantity
  }

  function submitOrder(): number | null {
    const reference = table.value.trim()

    if (!reference || !cart.value.length) return null

    const id =
      Math.max(1000, ...orders.value.map(order => order.id)) + 1

    orders.value.unshift({
      id,
      table: reference,
      status: 'Enviado',
      items: cart.value.map(item => ({ ...item }))
    })

    cart.value = []
    table.value = ''

    return id
  }

  function advanceOrder(id: number) {
    const order = orders.value.find(item => item.id === id)
    if (!order) return

    const next: Partial<Record<WaiterStatus, WaiterStatus>> = {
      Enviado: 'Em preparo',
      'Em preparo': 'Pronto',
      Pronto: 'Entregue'
    }

    const status = next[order.status]
    if (status) order.status = status
  }

  return {
    dishes,
    categories,
    table,
    cart,
    orders,
    cartTotal,
    activeOrders,
    readyOrders,
    occupiedTables,
    total,
    money,
    addDish,
    changeQuantity,
    submitOrder,
    advanceOrder
  }
}