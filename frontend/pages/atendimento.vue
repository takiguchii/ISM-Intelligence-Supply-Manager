<template>
  <div class="flex h-screen bg-gray-50 dark:bg-zinc-950 font-sans relative overflow-hidden">
    
    <!-- ÁREA PRINCIPAL: MENU E FILTROS -->
    <main class="flex-1 flex flex-col h-full w-full relative">
      <header class="bg-white dark:bg-zinc-900 p-4 shadow-sm z-10 flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b dark:border-zinc-800">
        <div>
          <h1 class="text-xl sm:text-2xl font-bold text-gray-800 dark:text-white">Atendimento</h1>
          <p class="text-xs sm:text-sm text-gray-500 dark:text-zinc-400">Selecione os pratos para a comanda</p>
        </div>
        
        <div class="flex items-center gap-2 sm:gap-3 w-full sm:w-auto">
          <label for="mesa" class="font-semibold text-sm sm:text-base text-gray-700 dark:text-zinc-300 whitespace-nowrap">Mesa/Comanda:</label>
          <input 
            id="mesa"
            v-model="comandaMesa"
            :disabled="isSubmitting"
            type="text" 
            placeholder="Ex: 12 ou João" 
            class="flex-1 sm:w-48 border border-gray-300 dark:border-zinc-700 bg-white dark:bg-zinc-800 text-gray-900 dark:text-white rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none transition-all"
          />
        </div>
      </header>

      <div class="bg-white dark:bg-zinc-900 px-4 py-3 border-b dark:border-zinc-800 flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div class="flex gap-2 overflow-x-auto hide-scrollbar w-full md:w-auto pb-1 md:pb-0">
          <button 
            v-for="categoria in categorias" 
            :key="categoria"
            @click="categoriaAtiva = categoria"
            :class="[
              'px-4 py-2 rounded-full text-sm font-semibold whitespace-nowrap transition-colors',
              categoriaAtiva === categoria 
                ? 'bg-blue-600 text-white shadow-md' 
                : 'bg-gray-100 dark:bg-zinc-800 text-gray-600 dark:text-zinc-300 hover:bg-gray-200 dark:hover:bg-zinc-700'
            ]"
          >
            {{ categoria }}
          </button>
        </div>

        <div class="relative w-full md:w-64 flex-shrink-0">
          <div class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
            <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
            </svg>
          </div>
          <input 
            v-model="termoBusca"
            type="text" 
            placeholder="Buscar prato..." 
            class="w-full border border-gray-300 dark:border-zinc-700 bg-gray-50 dark:bg-zinc-800 text-gray-900 dark:text-white rounded-lg pl-10 pr-4 py-2 text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none transition-all"
          />
        </div>
      </div>

      <div class="flex-1 overflow-y-auto p-4 lg:p-6 pb-24 lg:pb-6">
        <p v-if="cardapioLoading" class="text-sm text-gray-500">Carregando cardápio...</p>
        <p v-else-if="cardapioError" role="alert" class="text-sm text-red-500">{{ cardapioError }}</p>
        <div v-else-if="pratosFiltrados.length === 0" class="flex flex-col items-center justify-center h-40 text-gray-400">
          <p class="font-medium text-lg">Nenhum prato encontrado.</p>
        </div>
        <div v-else class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-4 lg:gap-6">
          <div 
            v-for="prato in pratosFiltrados" 
            :key="prato.id"
            data-card-reveal
            class="bg-white dark:bg-zinc-900 rounded-xl shadow-sm border border-gray-200 dark:border-zinc-800 overflow-hidden flex flex-col active:scale-[0.98] transition-transform lg:hover:shadow-lg lg:active:scale-100"
          >
            <div class="p-4 lg:p-5 flex-1">
              <span class="text-xs font-bold text-blue-600 dark:text-blue-400 uppercase tracking-wider">{{ prato.categoria }}</span>
              <h3 class="text-base lg:text-lg font-bold text-gray-900 dark:text-white mt-1">{{ prato.nome }}</h3>
              <p class="text-xs lg:text-sm text-gray-500 dark:text-zinc-400 mt-1 line-clamp-2">{{ prato.descricao }}</p>
            </div>
            <div class="p-4 lg:p-5 bg-gray-50 dark:bg-zinc-800/50 border-t border-gray-100 dark:border-zinc-800 flex items-center justify-between mt-auto">
              <span class="text-lg lg:text-xl font-bold text-green-700 dark:text-green-400">{{ formatarMoeda(prato.precoVenda) }}</span>
              <button 
                @click="adicionarAoCarrinho(prato)"
                class="bg-blue-600 hover:bg-blue-700 text-white rounded-full w-10 h-10 lg:w-12 lg:h-12 flex items-center justify-center shadow-md focus:ring-4 focus:ring-blue-300 dark:focus:ring-blue-800 transition-transform active:scale-90"
                title="Adicionar à Comanda"
              >
                <svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
                </svg>
              </button>
            </div>
          </div>
        </div>
      </div>
    </main>

    <!-- BARRA FLUTUANTE MOBILE -->
    <div 
      v-if="carrinho.length > 0" 
      class="lg:hidden fixed bottom-0 left-0 right-0 p-4 z-30 pointer-events-none transition-transform"
    >
      <button 
        @click="isCartOpenMobile = true"
        class="pointer-events-auto w-full bg-blue-600 text-white rounded-xl shadow-[0_8px_30px_rgb(0,0,0,0.3)] p-4 flex justify-between items-center active:scale-95 transition-transform"
      >
        <div class="flex items-center gap-3">
          <span class="bg-white text-blue-600 font-black w-8 h-8 rounded-full flex items-center justify-center text-sm">
            {{ carrinho.length }}
          </span>
          <span class="font-semibold">Ver Comanda</span>
        </div>
        <span class="font-bold text-xl">{{ formatarMoeda(totalPedido) }}</span>
      </button>
    </div>

    <!-- OVERLAY MOBILE -->
    <div 
      v-if="isCartOpenMobile" 
      @click="isCartOpenMobile = false"
      class="fixed inset-0 bg-black/60 backdrop-blur-sm z-40 lg:hidden transition-opacity"
    ></div>

    <!-- PAINEL LATERAL: PEDIDO EM ANDAMENTO -->
    <aside 
      :class="[
        'bg-white dark:bg-zinc-900 shadow-2xl flex flex-col border-l border-gray-200 dark:border-zinc-800 z-50 transition-transform duration-300 ease-in-out',
        'fixed inset-y-0 right-0 w-[90%] sm:w-[400px]',
        'lg:static lg:w-[400px]',
        isCartOpenMobile ? 'translate-x-0' : 'translate-x-full lg:translate-x-0'
      ]"
    >
      <div class="p-5 border-b border-gray-200 dark:border-zinc-800 bg-gray-50 dark:bg-zinc-900 flex justify-between items-center">
        <div>
          <h2 class="text-xl font-bold text-gray-800 dark:text-white">Pedido Atual</h2>
          <p class="text-sm font-semibold text-blue-600 dark:text-blue-400 mt-1">
            {{ comandaMesa ? `Comanda: ${comandaMesa}` : 'Identifique a mesa' }}
          </p>
        </div>
        
        <div class="flex items-center gap-2">
          <!-- Botão Limpar Comanda -->
          <button 
            v-if="carrinho.length > 0" 
            @click="abrirModalLimpar" 
            class="text-xs font-bold text-red-500 hover:text-red-700 bg-red-50 dark:bg-red-900/30 px-3 py-1.5 rounded-lg transition-colors"
            title="Esvaziar carrinho"
          >
            Limpar
          </button>

          <!-- Botão Fechar no Mobile -->
          <button @click="isCartOpenMobile = false" class="lg:hidden p-2 text-gray-500 hover:text-gray-800 dark:hover:text-white bg-gray-200 dark:bg-zinc-800 rounded-full">
            <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>
      </div>

      <!-- Lista do Carrinho -->
      <div class="flex-1 overflow-y-auto p-4 space-y-4">
        <div v-if="carrinho.length === 0" class="flex flex-col items-center justify-center h-full text-gray-400 dark:text-zinc-600 space-y-3">
          <svg xmlns="http://www.w3.org/2000/svg" class="h-16 w-16" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M16 11V7a4 4 0 00-8 0v4M5 9h14l1 12H4L5 9z" />
          </svg>
          <p class="font-medium text-center">A comanda está vazia.<br>Adicione pratos do cardápio.</p>
        </div>
        
        <div v-for="(item, index) in carrinho" :key="index" class="bg-white dark:bg-zinc-800 border border-gray-200 dark:border-zinc-700 shadow-sm rounded-lg p-4">
          <div class="flex justify-between items-start gap-2">
            <h4 class="font-bold text-gray-800 dark:text-white leading-tight">{{ item.nome }}</h4>
            <span class="font-bold text-gray-900 dark:text-green-400 whitespace-nowrap">{{ formatarMoeda(item.precoVenda * item.quantidade) }}</span>
          </div>
          
          <div class="flex items-center justify-between mt-4">
            <div class="flex items-center bg-gray-100 dark:bg-zinc-900 rounded-lg overflow-hidden border border-gray-200 dark:border-zinc-700">
              <button @click="alterarQuantidade(index, -1)" class="w-8 h-8 flex items-center justify-center text-gray-700 dark:text-zinc-300 hover:bg-gray-200 dark:hover:bg-zinc-700 font-bold active:bg-gray-300">-</button>
              <span class="w-8 h-8 flex items-center justify-center font-bold bg-white dark:bg-zinc-800 dark:text-white">{{ item.quantidade }}</span>
              <button @click="alterarQuantidade(index, 1)" class="w-8 h-8 flex items-center justify-center text-gray-700 dark:text-zinc-300 hover:bg-gray-200 dark:hover:bg-zinc-700 font-bold active:bg-gray-300">+</button>
            </div>
            <button @click="removerItem(index)" class="text-red-500 hover:text-red-700 dark:hover:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/30 px-2 py-1 rounded text-sm font-semibold">Remover</button>
          </div>

          <div class="mt-3">
            <input v-model="item.observacao" :disabled="isSubmitting" type="text" placeholder="Observações (ex: sem cebola)" class="w-full text-sm border border-gray-300 dark:border-zinc-700 rounded-md px-3 py-2 bg-gray-50 dark:bg-zinc-900 dark:text-white focus:bg-white dark:focus:bg-zinc-800 focus:ring-2 focus:ring-blue-500 outline-none transition-colors" />
          </div>
        </div>
      </div>

      <!-- Rodapé do Carrinho -->
      <div class="p-5 bg-white dark:bg-zinc-900 border-t border-gray-200 dark:border-zinc-800 shadow-[0_-4px_6px_-1px_rgba(0,0,0,0.05)] pb-safe">
        <div class="flex justify-between items-end mb-4">
          <span class="font-semibold text-gray-500 dark:text-zinc-400 uppercase tracking-wider text-sm">Valor Total</span>
          <span class="font-black text-2xl lg:text-3xl text-gray-900 dark:text-white">{{ formatarMoeda(totalPedido) }}</span>
        </div>
        <button
          @click="confirmarPedido"
          :disabled="isSubmitting || carrinho.length === 0 || !comandaMesa.trim()"
          class="w-full py-4 rounded-xl font-bold text-lg flex justify-center items-center bg-green-600 text-white disabled:bg-gray-200 dark:disabled:bg-zinc-800 disabled:text-gray-400 disabled:cursor-not-allowed"
        >
          {{ isSubmitting ? 'Enviando pedido...' : 'Confirmar Pedido' }}
        </button>
        <p v-if="orderError" role="alert" class="text-sm text-red-500 text-center mt-3">{{ orderError }}</p>
        <p v-if="!comandaMesa && carrinho.length > 0" class="text-sm font-medium text-red-500 dark:text-red-400 text-center mt-3 animate-pulse">
          * Informe a Mesa/Comanda no topo.
        </p>
      </div>
    </aside>

    <div v-if="confirmedOrder" role="dialog" aria-modal="true" class="fixed inset-0 z-[60] flex items-center justify-center bg-black/60 p-4">
      <div class="bg-white dark:bg-zinc-900 rounded-2xl shadow-2xl w-full max-w-sm p-8 text-center">
        <h3 class="text-xl font-bold text-gray-900 dark:text-white">Pedido enviado</h3>
        <p class="mt-3 text-sm text-gray-600 dark:text-zinc-400">Comanda {{ confirmedOrder.tableReference }} · Pedido #{{ confirmedOrder.id }}</p>
        <p class="mt-2 text-lg font-bold text-green-600">{{ formatarMoeda(confirmedOrder.items.reduce((total, item) => total + item.unitPrice * item.quantity, 0)) }}</p>
        <button @click="confirmedOrder = null" class="mt-5 w-full bg-blue-600 text-white rounded-xl py-3 font-semibold">Novo atendimento</button>
      </div>
    </div>

    <!-- MODAL DE CONFIRMAÇÃO (LIMPAR COMANDA) -->
    <div v-if="showClearModal" class="fixed inset-0 z-[60] flex items-center justify-center bg-black/60 backdrop-blur-sm p-4 transition-opacity">
      <div class="bg-white dark:bg-zinc-900 rounded-2xl shadow-2xl w-full max-w-sm p-8 transform transition-all text-center border border-gray-100 dark:border-zinc-800">
        <div class="w-20 h-20 bg-red-100 dark:bg-red-900/30 text-red-500 rounded-full flex items-center justify-center mx-auto mb-5 shadow-inner">
          <svg xmlns="http://www.w3.org/2000/svg" class="h-10 w-10" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
          </svg>
        </div>
        <h3 class="text-2xl font-bold text-gray-900 dark:text-white mb-2">Limpar Comanda?</h3>
        <p class="text-gray-600 dark:text-zinc-400 mb-8">Tem certeza que deseja remover todos os itens? Esta ação não pode ser desfeita.</p>

        <div class="flex gap-3">
          <button @click="showClearModal = false" class="flex-1 bg-gray-200 hover:bg-gray-300 dark:bg-zinc-800 dark:hover:bg-zinc-700 text-gray-800 dark:text-white font-bold py-3.5 px-4 rounded-xl transition-all active:scale-95">
            Cancelar
          </button>
          <button @click="confirmarLimparPedido" class="flex-1 bg-red-600 hover:bg-red-700 text-white font-bold py-3.5 px-4 rounded-xl transition-all shadow-md active:scale-95">
            Sim, limpar
          </button>
        </div>
      </div>
    </div>

  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { menuService } from '~/services/modules/menu/menuService'
import { orderService } from '~/services/modules/orders/orderService'
import { useAuthStore } from '~/stores/auth'

definePageMeta({ layout: 'default' })
const authStore = useAuthStore()

// --- ESTADO LOCAL ---
const comandaMesa = ref('')
const categoriaAtiva = ref('Todos')
const termoBusca = ref('')
const carrinho = ref([])
const isCartOpenMobile = ref(false)
const isSubmitting = ref(false)
const orderError = ref('')
const confirmedOrder = ref(null)
const requestId = ref(null)
watch([comandaMesa, carrinho], () => { if (!isSubmitting.value) requestId.value = null }, { deep: true, flush: 'sync' })

// Estados dos Modais
const showClearModal = ref(false)

const pratosBase = ref([])
const cardapioLoading = ref(true)
const cardapioError = ref('')
const categorias = computed(() => ['Todos', ...new Set(pratosBase.value.map(prato => prato.categoria))])

onMounted(async () => {
  try {
    const restaurantId = authStore.currentUser?.restaurantId ?? undefined
    const [dishes, categories] = await Promise.all([
      menuService.getDishes(restaurantId),
      menuService.getCategories(restaurantId)
    ])
    const categoryNames = new Map(categories.filter(category => category.isActive !== false).map(category => [category.id, category.name]))
    pratosBase.value = dishes.filter(dish => dish.isActive && categoryNames.has(dish.categoryId)).map(dish => ({
      id: dish.id,
      nome: dish.name,
      categoria: categoryNames.get(dish.categoryId),
      descricao: dish.description || '',
      precoVenda: dish.price
    }))
  } catch (error) {
    console.error('Erro ao carregar cardápio:', error)
    cardapioError.value = 'Não foi possível carregar o cardápio. Tente novamente mais tarde.'
  } finally {
    cardapioLoading.value = false
  }
})

// --- COMPUTADOS ---
const pratosFiltrados = computed(() => {
  let filtrados = pratosBase.value

  if (categoriaAtiva.value !== 'Todos') {
    filtrados = filtrados.filter(p => p.categoria === categoriaAtiva.value)
  }

  if (termoBusca.value.trim() !== '') {
    const termo = termoBusca.value.toLowerCase()
    filtrados = filtrados.filter(p => 
      p.nome.toLowerCase().includes(termo) || 
      p.descricao.toLowerCase().includes(termo)
    )
  }

  return filtrados
})

const totalPedido = computed(() => {
  return carrinho.value.reduce((total, item) => total + (item.precoVenda * item.quantidade), 0)
})

// --- MÉTODOS ---
const formatarMoeda = (valor) => {
  return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(valor)
}

const adicionarAoCarrinho = (prato) => {
  if (isSubmitting.value) return
  carrinho.value.push({
    ...prato,
    quantidade: 1,
    observacao: ''
  })
}

const alterarQuantidade = (index, delta) => {
  if (isSubmitting.value) return
  const item = carrinho.value[index]
  const novaQuantidade = item.quantidade + delta
  if (novaQuantidade > 0) {
    item.quantidade = novaQuantidade
  } else {
    removerItem(index)
  }
}

const removerItem = (index) => {
  if (isSubmitting.value) return
  carrinho.value.splice(index, 1)
  if (carrinho.value.length === 0) {
    isCartOpenMobile.value = false
  }
}

const abrirModalLimpar = () => {
  if (isSubmitting.value) return
  showClearModal.value = true
}

const confirmarLimparPedido = () => {
  if (isSubmitting.value) return
  carrinho.value = []
  isCartOpenMobile.value = false
  showClearModal.value = false
}


const confirmarPedido = async () => {
  if (isSubmitting.value || carrinho.value.length === 0 || !comandaMesa.value.trim()) return
  requestId.value ||= crypto.randomUUID()
  isSubmitting.value = true
  orderError.value = ''
  try {
    const order = await orderService.create({
      requestId: requestId.value,
      tableReference: comandaMesa.value.trim(),
      items: carrinho.value.map(item => ({ dishId: item.id, quantity: item.quantidade, observation: item.observacao }))
    })
    confirmedOrder.value = order
    isCartOpenMobile.value = false
    carrinho.value = []
    comandaMesa.value = ''
    categoriaAtiva.value = 'Todos'
    termoBusca.value = ''
    requestId.value = null
  } catch (error) {
    console.error('Erro ao enviar pedido:', error)
    orderError.value = error?.data?.detail || error?.data?.message || 'Não foi possível enviar o pedido. Tente novamente.'
  } finally {
    isSubmitting.value = false
  }
}
</script>

<style scoped>
.hide-scrollbar::-webkit-scrollbar {
  display: none;
}
.hide-scrollbar {
  -ms-overflow-style: none;
  scrollbar-width: none;
}
</style>
