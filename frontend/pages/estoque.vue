<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { productService } from '~/services/modules/stock/productService'
import AppLoader from '~/components/base/AppLoader.vue'

// ==================== CONFIGURAÇÃO ====================
const PAGE_SIZE = 8
const HISTORICO_KEY = 'estoque:historico-busca'

// ==================== ESTADOS ====================
const searchQuery = ref('')
const historicoBusca = ref<string[]>([])
const mostrarHistorico = ref(false)

const currentPage = ref(1)
const totalItens = ref(0)
const paginadoNoServidor = ref(true)

const isModalOpen = ref(false)
const isEditing = ref(false)
const isLoading = ref(true)
const isSaving = ref(false)
const errorMessage = ref('')
const formData = ref<{
  id: number | null
  nome: string
  unidade: string
  quantidade: number
  minimo: number
}>({ id: null, nome: '', unidade: 'kg', quantidade: 0, minimo: 0 })

const itensEstoque = ref<any[]>([])
const itensCriticos = ref<any[]>([])

// Helpers de propriedades
const getItemName = (item: any) => item?.name || item?.nome || ''
const getItemUnit = (item: any) => item?.unit || item?.unidade || ''
const getItemQty = (item: any) => item?.currentQuantity ?? item?.quantidade ?? 0
const getItemMin = (item: any) => item?.minimumQuantity ?? item?.minimo ?? 0

// ==================== HISTÓRICO DE BUSCA ====================
const carregarHistorico = () => {
  try {
    const salvo = JSON.parse(localStorage.getItem(HISTORICO_KEY) || '[]')
    historicoBusca.value = Array.isArray(salvo) ? salvo.slice(0, 3) : []
  } catch {
    historicoBusca.value = []
  }
}

const registrarBusca = (termo: string) => {
  const t = (termo || '').trim()
  if (!t) return
  const lista = historicoBusca.value.filter(i => i.toLowerCase() !== t.toLowerCase())
  lista.unshift(t)
  historicoBusca.value = lista.slice(0, 3)
  localStorage.setItem(HISTORICO_KEY, JSON.stringify(historicoBusca.value))
}

const aplicarBusca = (termo: string) => {
  searchQuery.value = termo
  registrarBusca(termo)
  mostrarHistorico.value = false
}

const aoSairDaBusca = () => {
  registrarBusca(searchQuery.value)
  mostrarHistorico.value = false
}

const limparHistorico = () => {
  historicoBusca.value = []
  localStorage.removeItem(HISTORICO_KEY)
}

// ==================== LISTAGEM E PAGINAÇÃO ====================
const itensFiltrados = computed(() => {
  if (paginadoNoServidor.value) return itensEstoque.value

  let lista = itensEstoque.value
  if (searchQuery.value) {
    const termo = searchQuery.value.toLowerCase()
    lista = lista.filter(i => getItemName(i).toLowerCase().includes(termo))
  }
  const inicio = (currentPage.value - 1) * PAGE_SIZE
  return lista.slice(inicio, inicio + PAGE_SIZE)
})

const totalFiltrado = computed(() => {
  if (paginadoNoServidor.value) return totalItens.value
  if (!searchQuery.value) return itensEstoque.value.length
  const termo = searchQuery.value.toLowerCase()
  return itensEstoque.value.filter(i => getItemName(i).toLowerCase().includes(termo)).length
})

const totalPaginas = computed(() => Math.max(1, Math.ceil(totalFiltrado.value / PAGE_SIZE)))

const paginasVisiveis = computed(() => {
  const total = totalPaginas.value
  let inicio = Math.max(1, currentPage.value - 2)
  const fim = Math.min(total, inicio + 4)
  inicio = Math.max(1, fim - 4)
  return Array.from({ length: fim - inicio + 1 }, (_, i) => inicio + i)
})

const irParaPagina = (p: number) => {
  if (p < 1 || p > totalPaginas.value || p === currentPage.value) return
  currentPage.value = p
  fetchEstoque()
}

// Busca com debounce
let debounceTimer: any = null
watch(searchQuery, () => {
  clearTimeout(debounceTimer)
  debounceTimer = setTimeout(() => {
    currentPage.value = 1
    fetchEstoque()
  }, 400)
})

// ==================== APOIO ====================
const nomesItensCriticos = computed(() => {
  const nomes = itensCriticos.value.map(getItemName).filter(Boolean)
  if (nomes.length === 0) return ''
  if (nomes.length === 1) return nomes[0]
  const copy = [...nomes]
  const ultimo = copy.pop()
  return copy.join(', ') + ' e ' + ultimo
})

const verificarStatus = (item: any) => (getItemQty(item) <= getItemMin(item) ? 'Crítico' : 'Saudável')

// ==================== API ====================
const fetchEstoque = async () => {
  isLoading.value = true
  errorMessage.value = ''
  try {
    const pagedResult = await productService.getPaged({
      pageNumber: currentPage.value,
      pageSize: PAGE_SIZE,
      search: searchQuery.value
    })

    if (pagedResult && Array.isArray(pagedResult.items)) {
      paginadoNoServidor.value = true
      itensEstoque.value = pagedResult.items
      totalItens.value = pagedResult.totalCount ?? pagedResult.items.length
    } else if (Array.isArray(pagedResult)) {
      paginadoNoServidor.value = false
      itensEstoque.value = pagedResult
      totalItens.value = pagedResult.length
    }

    if (currentPage.value > totalPaginas.value && totalPaginas.value > 0) {
      currentPage.value = totalPaginas.value
    }
  } catch (error) {
    console.error(error)
    errorMessage.value = 'Erro de conexão. Não foi possível carregar o estoque no momento.'
  } finally {
    isLoading.value = false
  }
}

const fetchCriticos = async () => {
  try {
    const pagedResult = await productService.getPaged({
      isCritical: true,
      pageSize: 100
    })
    itensCriticos.value = pagedResult?.items || []
  } catch {
    itensCriticos.value = itensEstoque.value.filter(i => getItemQty(i) <= getItemMin(i))
  }
}

const salvarItem = async () => {
  isSaving.value = true
  errorMessage.value = ''
  try {
    const payload = {
      name: formData.value.nome,
      unit: formData.value.unidade,
      currentQuantity: formData.value.quantidade,
      minimumQuantity: formData.value.minimo
    }

    if (isEditing.value && formData.value.id) {
      await productService.update(formData.value.id, payload)
    } else {
      await productService.create(payload)
    }

    await fetchEstoque()
    await fetchCriticos()
    fecharModal()
  } catch (error) {
    console.error(error)
    errorMessage.value = 'Erro ao tentar salvar o item no estoque.'
  } finally {
    isSaving.value = false
  }
}

const deletarItem = async (id: number) => {
  if (!confirm('Deseja mesmo remover este item?')) return
  errorMessage.value = ''
  try {
    await productService.delete(id)
    await fetchEstoque()
    await fetchCriticos()
  } catch (error) {
    console.error(error)
    errorMessage.value = 'Erro ao tentar excluir o item.'
  }
}

// ==================== MODAL ====================
const abrirModal = (item: any = null) => {
  if (item) {
    formData.value = {
      id: item.id,
      nome: getItemName(item),
      unidade: getItemUnit(item),
      quantidade: getItemQty(item),
      minimo: getItemMin(item)
    }
    isEditing.value = true
  } else {
    formData.value = { id: null, nome: '', unidade: 'kg', quantidade: 0, minimo: 0 }
    isEditing.value = false
  }
  isModalOpen.value = true
}

const fecharModal = () => {
  isModalOpen.value = false
  errorMessage.value = ''
}

const gerarPedido = () => {
  alert('Conectando ao agente para gerar pedido de: ' + nomesItensCriticos.value)
}

onMounted(() => {
  carregarHistorico()
  fetchEstoque().then(fetchCriticos)
})
</script>

<template>
  <div class="max-w-7xl w-full mx-auto px-4 sm:px-6 py-8 space-y-6">
    <AppLoader :visible="isLoading" />

    <!-- ==================== HEADER ==================== -->
    <section class="p-6 sm:p-8 rounded-2xl bg-gradient-to-r from-zinc-900 via-zinc-900/90 to-zinc-950 border border-zinc-800 shadow-xl flex flex-col md:flex-row md:items-start justify-between gap-6">
      <div>
        <span class="inline-flex items-center px-3 py-1 rounded-full border border-zinc-800 bg-zinc-900/80 text-[11px] font-bold text-zinc-400 mb-4 tracking-wider uppercase">
          Estoque de Produtos
        </span>
        <div class="flex items-center gap-3">
          <h1 class="text-2xl sm:text-3xl md:text-4xl font-extrabold text-white tracking-tight">Controle de Insumos</h1>
        </div>
        <p class="text-zinc-400 text-xs sm:text-sm mt-2 max-w-2xl">
          Gerencie quantidades, monitore níveis mínimos e evite rupturas no seu inventário com apoio da inteligência artificial.
        </p>
      </div>

      <!-- BUSCA + HISTÓRICO DAS 3 ÚLTIMAS -->
      <div class="relative w-full md:w-80 mt-2">
        <span class="absolute inset-y-0 left-0 flex items-center pl-3.5 text-zinc-500 z-10 pointer-events-none">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"></path></svg>
        </span>
        <input
          v-model="searchQuery"
          @focus="mostrarHistorico = true"
          @blur="aoSairDaBusca"
          @keyup.enter="registrarBusca(searchQuery)"
          @keyup.esc="mostrarHistorico = false"
          type="text"
          placeholder="Buscar insumos..."
          class="w-full bg-zinc-950/80 text-sm text-zinc-200 border border-zinc-800 rounded-xl pl-10 pr-4 py-2.5 focus:border-zinc-600 focus:ring-1 focus:ring-zinc-600 outline-none transition-all placeholder-zinc-500"
        />

        <div v-if="mostrarHistorico && historicoBusca.length" class="absolute z-20 mt-2 w-full bg-zinc-900 border border-zinc-800 rounded-xl shadow-2xl overflow-hidden">
          <div class="flex items-center justify-between px-4 pt-3 pb-2">
            <span class="text-[10px] font-bold uppercase tracking-wider text-zinc-500">Buscas recentes</span>
            <button @mousedown.prevent="limparHistorico" class="text-[10px] font-bold uppercase tracking-wider text-zinc-500 hover:text-zinc-300 transition-colors">
              Limpar
            </button>
          </div>
          <button
            v-for="termo in historicoBusca"
            :key="termo"
            @mousedown.prevent="aplicarBusca(termo)"
            class="w-full text-left px-4 py-2.5 text-sm text-zinc-400 hover:bg-zinc-800 hover:text-white transition-colors flex items-center gap-2.5"
          >
            <svg class="w-3.5 h-3.5 text-zinc-500 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
            <span class="truncate">{{ termo }}</span>
          </button>
        </div>
      </div>
    </section>

    <!-- ==================== ERRO DA API ==================== -->
    <div v-if="errorMessage" class="bg-red-500/10 border border-red-500/30 text-red-300 px-4 py-3 rounded-xl text-sm font-medium flex items-center justify-between shadow-sm">
      <span>{{ errorMessage }}</span>
      <button @click="errorMessage = ''" class="text-red-400 hover:text-red-200 font-bold ml-4">&times;</button>
    </div>

    <!-- ==================== ALERTA DE CRÍTICOS ==================== -->
    <div v-if="!isLoading && itensCriticos.length > 0" class="border border-red-500/30 bg-red-500/10 rounded-2xl p-6 shadow-xl transition-all relative overflow-hidden">
      <div class="flex items-start justify-between flex-col sm:flex-row gap-4">
        <div class="flex items-start gap-4">
          <div class="p-3 bg-red-500/20 rounded-xl text-red-400 text-xl border border-red-500/30 shrink-0">
            🚨
          </div>
          <div>
            <div class="flex items-center gap-2 mb-1">
              <span class="text-red-400 text-[11px] font-bold tracking-widest uppercase">✦ Agente de Estoque (IA)</span>
            </div>
            <h3 class="text-lg font-bold text-white mb-2">{{ itensCriticos.length }} itens em estado crítico</h3>
            <p class="text-zinc-300 text-sm max-w-2xl leading-relaxed">
              Os seguintes itens atingiram ou estão abaixo do nível mínimo tolerável: <span class="text-white font-semibold">{{ nomesItensCriticos }}</span>. Deseja disparar a ordem de compra?
            </p>
          </div>
        </div>
        <button @click="gerarPedido" class="w-full sm:w-auto whitespace-nowrap bg-red-600 text-white px-5 py-2.5 rounded-xl text-sm font-semibold hover:bg-red-500 active:scale-95 transition-all shadow-lg shadow-red-600/20">
          Gerar Pedido Consolidado
        </button>
      </div>
    </div>

    <!-- ==================== TABELA ==================== -->
    <section class="bg-zinc-900/40 border border-zinc-800 rounded-2xl shadow-xl overflow-hidden">
      <div class="flex flex-col sm:flex-row items-start sm:items-center justify-between p-6 gap-4 border-b border-zinc-800 bg-zinc-900/70">
        <div class="flex items-center gap-3">
          <div class="p-2 border border-zinc-800 rounded-xl bg-zinc-950">
            <svg class="w-5 h-5 text-zinc-400" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4"></path></svg>
          </div>
          <div>
            <h3 class="text-base font-bold text-white">Itens Cadastrados</h3>
            <p v-if="!isLoading" class="text-xs text-zinc-400 mt-0.5">{{ totalFiltrado }} registros encontrados</p>
            <p v-else class="text-xs text-zinc-500 mt-0.5 animate-pulse">Carregando registros...</p>
          </div>
        </div>
        <button
          @click="abrirModal()"
          :disabled="isLoading"
          class="bg-white text-zinc-950 hover:bg-zinc-200 font-semibold px-4 py-2.5 rounded-xl text-xs sm:text-sm transition-all duration-200 shadow-lg shadow-white/5 flex items-center gap-2 active:scale-95 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          <span class="text-base leading-none font-bold">+</span>
          Novo item
        </button>
      </div>

      <div class="overflow-x-auto">
        <table class="w-full text-left text-sm">
          <thead class="text-xs text-zinc-400 uppercase bg-zinc-900/80 border-b border-zinc-800 tracking-wider">
            <tr>
              <th class="px-6 py-4 font-semibold">Insumo</th>
              <th class="px-6 py-4 font-semibold">Unidade</th>
              <th class="px-6 py-4 font-semibold">Qtd. Atual</th>
              <th class="px-6 py-4 font-semibold">Mínimo</th>
              <th class="px-6 py-4 font-semibold">Status</th>
              <th class="px-6 py-4 font-semibold text-right">Ações</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-zinc-800/60">
            <template v-if="isLoading">
              <tr v-for="n in PAGE_SIZE" :key="n" class="animate-pulse bg-zinc-900/20">
                <td class="px-6 py-5"><div class="h-4 bg-zinc-800 rounded w-3/4"></div></td>
                <td class="px-6 py-5"><div class="h-4 bg-zinc-800 rounded w-1/2"></div></td>
                <td class="px-6 py-5"><div class="h-4 bg-zinc-800 rounded w-1/2"></div></td>
                <td class="px-6 py-5"><div class="h-4 bg-zinc-800 rounded w-1/2"></div></td>
                <td class="px-6 py-5"><div class="h-6 bg-zinc-800 rounded-full w-20"></div></td>
                <td class="px-6 py-5 text-right"><div class="h-4 bg-zinc-800 rounded w-12 ml-auto"></div></td>
              </tr>
            </template>

            <tr v-else-if="itensFiltrados.length === 0">
              <td colspan="6" class="px-6 py-12 text-center text-zinc-400 text-sm">
                Nenhum insumo corresponde à sua busca ou cadastrado no banco de dados.
              </td>
            </tr>

            <tr v-else v-for="item in itensFiltrados" :key="item.id" class="hover:bg-zinc-800/30 transition-colors group">
              <td class="px-6 py-4 font-semibold text-zinc-100">{{ item.name || item.nome }}</td>
              <td class="px-6 py-4 text-zinc-400">{{ item.unit || item.unidade }}</td>
              <td class="px-6 py-4 font-bold text-zinc-100">{{ item.currentQuantity ?? item.quantidade }}</td>
              <td class="px-6 py-4 text-zinc-400">{{ item.minimumQuantity ?? item.minimo }}</td>
              <td class="px-6 py-4">
                <span
                  class="px-3 py-1 rounded-full text-[11px] font-bold border inline-block tracking-wider uppercase"
                  :class="verificarStatus(item) === 'Crítico'
                    ? 'border-red-500/30 text-red-400 bg-red-500/10'
                    : 'border-emerald-500/30 text-emerald-400 bg-emerald-500/10'"
                >
                  {{ verificarStatus(item) }}
                </span>
              </td>
              <td class="px-6 py-4 text-right">
                <div class="flex items-center justify-end gap-2">
                  <button
                    @click="abrirModal(item)"
                    class="p-2 rounded-lg bg-zinc-800/80 hover:bg-zinc-700 text-zinc-300 hover:text-white border border-zinc-700/40 transition-colors"
                    title="Editar"
                  >
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z"></path></svg>
                  </button>
                  <button
                    @click="deletarItem(item.id)"
                    class="p-2 rounded-lg bg-red-500/10 hover:bg-red-500/20 text-red-400 hover:text-red-300 border border-red-500/20 transition-colors"
                    title="Excluir"
                  >
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path></svg>
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- PAGINAÇÃO -->
      <div v-if="!isLoading && totalPaginas > 1" class="flex flex-col sm:flex-row items-center justify-between gap-4 px-6 py-4 border-t border-zinc-800 bg-zinc-900/60">
        <p class="text-xs text-zinc-400">
          Página <span class="text-white font-semibold">{{ currentPage }}</span> de {{ totalPaginas }} · {{ totalFiltrado }} itens no total
        </p>
        <div class="flex items-center gap-1.5">
          <button
            @click="irParaPagina(currentPage - 1)"
            :disabled="currentPage === 1"
            class="px-3 py-1.5 rounded-xl text-xs font-semibold text-zinc-300 border border-zinc-700/60 bg-zinc-800 hover:bg-zinc-700 hover:text-white transition-all disabled:opacity-40 disabled:cursor-not-allowed"
          >
            Anterior
          </button>
          <button
            v-for="p in paginasVisiveis"
            :key="p"
            @click="irParaPagina(p)"
            class="w-8 h-8 rounded-xl text-xs font-bold border transition-all flex items-center justify-center"
            :class="p === currentPage
              ? 'bg-white text-zinc-950 border-white font-extrabold'
              : 'text-zinc-400 border-zinc-800 bg-zinc-900 hover:text-white hover:border-zinc-700'"
          >
            {{ p }}
          </button>
          <button
            @click="irParaPagina(currentPage + 1)"
            :disabled="currentPage === totalPaginas"
            class="px-3 py-1.5 rounded-xl text-xs font-semibold text-zinc-300 border border-zinc-700/60 bg-zinc-800 hover:bg-zinc-700 hover:text-white transition-all disabled:opacity-40 disabled:cursor-not-allowed"
          >
            Próxima
          </button>
        </div>
      </div>
    </section>

    <!-- ==================== MODAL ==================== -->
    <div v-if="isModalOpen" class="fixed inset-0 bg-black/80 backdrop-blur-sm flex items-center justify-center p-4 z-50">
      <div class="bg-zinc-900 border border-zinc-800 rounded-2xl w-full max-w-md p-6 shadow-2xl relative space-y-4">
        <h3 class="text-lg font-bold text-white border-b border-zinc-800 pb-4">
          {{ isEditing ? 'Editar Insumo' : 'Novo Insumo' }}
        </h3>

        <form @submit.prevent="salvarItem" class="space-y-4">
          <!-- NOME -->
          <div>
            <label class="block text-[11px] font-bold text-zinc-400 mb-2 uppercase tracking-wider">Nome do Insumo</label>
            <input
              v-model="formData.nome"
              type="text"
              required
              class="w-full bg-zinc-950 text-zinc-100 border border-zinc-800 rounded-xl px-4 py-2.5 focus:border-zinc-600 focus:ring-1 focus:ring-zinc-600 outline-none transition-all placeholder-zinc-600 text-sm"
              placeholder="Ex: Filé Mignon"
            />
          </div>

          <!-- QUANTIDADE + UNIDADE -->
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-[11px] font-bold text-zinc-400 mb-2 uppercase tracking-wider">Quantidade</label>
              <input
                v-model.number="formData.quantidade"
                type="number"
                step="0.01"
                min="0"
                required
                class="no-spinner w-full bg-zinc-950 text-zinc-100 border border-zinc-800 rounded-xl px-4 py-2.5 focus:border-zinc-600 focus:ring-1 focus:ring-zinc-600 outline-none transition-all text-sm"
              />
            </div>
            <div>
              <label class="block text-[11px] font-bold text-zinc-400 mb-2 uppercase tracking-wider">Unidade</label>
              <select
                v-model="formData.unidade"
                required
                class="w-full bg-zinc-950 text-zinc-100 border border-zinc-800 rounded-xl px-4 py-2.5 focus:border-zinc-600 focus:ring-1 focus:ring-zinc-600 outline-none transition-all text-sm"
              >
                <option value="g">g</option>
                <option value="kg">kg</option>
                <option value="L">L</option>
                <option value="mL">mL</option>
                <option value="Un">Un</option>
              </select>
            </div>
          </div>

          <!-- ESTOQUE MÍNIMO -->
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-[11px] font-bold text-zinc-400 mb-2 uppercase tracking-wider">Estoque Mínimo</label>
              <input
                v-model.number="formData.minimo"
                type="number"
                step="0.01"
                min="0"
                required
                class="no-spinner w-full bg-zinc-950 text-zinc-100 border border-zinc-800 rounded-xl px-4 py-2.5 focus:border-zinc-600 focus:ring-1 focus:ring-zinc-600 outline-none transition-all text-sm"
              />
            </div>
            <div>
              <label class="block text-[11px] font-bold text-zinc-400 mb-2 uppercase tracking-wider">Unidade</label>
              <div
                aria-readonly="true"
                class="w-full bg-zinc-950/50 text-zinc-500 border border-zinc-800/60 rounded-xl px-4 py-2.5 flex items-center justify-between select-none cursor-not-allowed text-sm"
              >
                <span>{{ formData.unidade || '—' }}</span>
                <svg xmlns="http://www.w3.org/2000/svg" class="w-3.5 h-3.5 text-zinc-600" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <rect x="3" y="11" width="18" height="11" rx="2" />
                  <path d="M7 11V7a5 5 0 0110 0v4" />
                </svg>
              </div>
            </div>
          </div>

          <div class="flex justify-end gap-3 pt-4 border-t border-zinc-800">
            <button
              type="button"
              @click="fecharModal"
              class="px-4 py-2.5 text-xs sm:text-sm font-semibold text-zinc-400 hover:text-white rounded-xl bg-zinc-800 hover:bg-zinc-700 border border-zinc-700/60 transition-colors"
            >
              Cancelar
            </button>
            <button
              type="submit"
              :disabled="isSaving"
              class="bg-white text-zinc-950 hover:bg-zinc-200 px-5 py-2.5 rounded-xl text-xs sm:text-sm font-semibold transition-all shadow-lg shadow-white/5 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {{ isSaving ? 'Salvando...' : (isEditing ? 'Atualizar Insumo' : 'Salvar Insumo') }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<style scoped>
.no-spinner::-webkit-outer-spin-button,
.no-spinner::-webkit-inner-spin-button {
  -webkit-appearance: none;
  margin: 0;
}

.no-spinner {
  -moz-appearance: textfield;
  appearance: textfield;
}
</style>