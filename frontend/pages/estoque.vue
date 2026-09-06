<template>
  <div class="min-h-screen bg-[#0a0a0a] text-gray-300 font-sans flex flex-col">

    <main class="w-full max-w-[1200px] mx-auto p-4 md:p-8 flex flex-col gap-6">

      <!-- ==================== HEADER ==================== -->
      <section class="bg-[#111111] border border-gray-800/80 p-8 rounded-2xl shadow-sm flex flex-col md:flex-row md:items-start justify-between gap-6">
        <div>
          <span class="inline-block px-3 py-1 rounded-full border border-gray-800 bg-gray-800/40 text-[11px] font-bold text-gray-400 mb-4 tracking-wider uppercase">
            Estoque de Produtos
          </span>
          <div class="flex items-center gap-3">
            <h2 class="text-3xl font-extrabold text-white tracking-tight">Controle de Insumos</h2>
          </div>
          <p class="text-gray-500 text-sm mt-2">Gerencie quantidades, monitore mínimos e evite rupturas no seu inventário.</p>
        </div>

        <!-- BUSCA + HISTÓRICO DAS 3 ÚLTIMAS -->
        <div class="relative w-full md:w-80 mt-2">
          <span class="absolute inset-y-0 left-0 flex items-center pl-4 text-gray-500 z-10">
            <svg class="w-4 h-4 opacity-70" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"></path></svg>
          </span>
          <input
            v-model="searchQuery"
            @focus="mostrarHistorico = true"
            @blur="aoSairDaBusca"
            @keyup.enter="registrarBusca(searchQuery)"
            @keyup.esc="mostrarHistorico = false"
            type="text"
            placeholder="Buscar itens..."
            class="w-full bg-[#0a0a0a] text-sm text-gray-200 border border-gray-800 rounded-xl pl-11 pr-4 py-3 focus:ring-1 focus:ring-gray-600 focus:border-gray-600 outline-none transition-all placeholder-gray-600"
          />

          <div v-if="mostrarHistorico && historicoBusca.length" class="absolute z-20 mt-2 w-full bg-[#111111] border border-gray-800 rounded-xl shadow-2xl overflow-hidden">
            <div class="flex items-center justify-between px-4 pt-3 pb-2">
              <span class="text-[10px] font-bold uppercase tracking-wider text-gray-600">Buscas recentes</span>
              <button @mousedown.prevent="limparHistorico" class="text-[10px] font-bold uppercase tracking-wider text-gray-600 hover:text-gray-400 transition-colors">
                Limpar
              </button>
            </div>
            <button
              v-for="termo in historicoBusca"
              :key="termo"
              @mousedown.prevent="aplicarBusca(termo)"
              class="w-full text-left px-4 py-2.5 text-sm text-gray-400 hover:bg-gray-800/40 hover:text-white transition-colors flex items-center gap-2.5"
            >
              <svg class="w-3.5 h-3.5 text-gray-600 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
              <span class="truncate">{{ termo }}</span>
            </button>
          </div>
        </div>
      </section>

      <!-- ==================== ERRO DA API ==================== -->
      <div v-if="errorMessage" class="bg-red-950/30 border border-red-900/50 text-red-400 px-4 py-3 rounded-xl text-sm font-semibold flex items-center justify-between shadow-sm">
        <span>{{ errorMessage }}</span>
        <button @click="errorMessage = ''" class="text-red-400 hover:text-red-300 text-lg">&times;</button>
      </div>

      <!-- ==================== ALERTA DE CRÍTICOS ==================== -->
      <div v-if="!isLoading && itensCriticos.length > 0" class="border border-red-900/30 bg-[#111111] rounded-2xl p-6 shadow-sm transition-all relative overflow-hidden">
        <div class="flex items-start justify-between flex-col sm:flex-row gap-4">
          <div class="flex items-start gap-4">
            <div class="p-3 bg-red-950/50 rounded-xl text-red-500 text-xl border border-red-900/30">
              🚨
            </div>
            <div>
              <div class="flex items-center gap-2 mb-1">
                <span class="text-red-500 text-[11px] font-bold tracking-widest uppercase">✦ Agente de Estoque (IA)</span>
              </div>
              <h3 class="text-lg font-bold text-white mb-2">{{ itensCriticos.length }} itens em estado crítico</h3>
              <p class="text-gray-400 text-sm max-w-2xl leading-relaxed">
                Os seguintes itens atingiram ou estão abaixo do nível mínimo tolerável: <span class="text-gray-200 font-semibold">{{ nomesItensCriticos }}</span>. Deseja disparar a ordem de compra?
              </p>
            </div>
          </div>
          <button @click="gerarPedido" class="w-full sm:w-auto whitespace-nowrap bg-red-600/90 text-white px-5 py-2.5 rounded-xl text-sm font-bold hover:bg-red-500 active:scale-95 transition-all shadow-sm">
            Gerar Pedido Consolidado
          </button>
        </div>
      </div>

      <!-- ==================== TABELA ==================== -->
      <section class="bg-[#111111] border border-gray-800/80 rounded-2xl shadow-sm overflow-hidden">
        <div class="flex flex-col sm:flex-row items-start sm:items-center justify-between p-6 gap-4 border-b border-gray-800/80 bg-[#111111]">
          <div class="flex items-center gap-3">
            <div class="p-2 border border-gray-800 rounded-lg bg-[#0a0a0a]">
              <svg class="w-5 h-5 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4"></path></svg>
            </div>
            <div>
              <h3 class="text-base font-bold text-white">Itens Cadastrados</h3>
              <p v-if="!isLoading" class="text-xs text-gray-500 mt-0.5">{{ totalFiltrado }} registros encontrados</p>
              <p v-else class="text-xs text-gray-600 mt-0.5 animate-pulse">Carregando registros...</p>
            </div>
          </div>
          <button @click="abrirModal()" :disabled="isLoading" class="w-full sm:w-auto flex items-center justify-center gap-2 bg-[#0a0a0a] border border-gray-700 text-gray-200 px-5 py-2.5 rounded-xl text-sm font-bold hover:bg-gray-800 hover:text-white active:scale-95 transition-all disabled:opacity-50 disabled:cursor-not-allowed">
            Adicionar Novo Item
          </button>
        </div>

        <div class="overflow-x-auto">
          <table class="w-full text-left text-sm">
            <thead class="text-[11px] text-gray-500 uppercase bg-[#0a0a0a]/50 border-b border-gray-800/80">
              <tr>
                <th class="px-6 py-4 font-bold tracking-wider">Insumo</th>
                <th class="px-6 py-4 font-bold tracking-wider">Unidade</th>
                <th class="px-6 py-4 font-bold tracking-wider">Qtd. Atual</th>
                <th class="px-6 py-4 font-bold tracking-wider">Mínimo</th>
                <th class="px-6 py-4 font-bold tracking-wider">Status</th>
                <th class="px-6 py-4 font-bold tracking-wider text-right">Ações</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-800/50">

              <template v-if="isLoading">
                <tr v-for="n in PAGE_SIZE" :key="n" class="animate-pulse bg-[#111111]">
                  <td class="px-6 py-5"><div class="h-4 bg-gray-800 rounded w-3/4"></div></td>
                  <td class="px-6 py-5"><div class="h-4 bg-gray-800 rounded w-1/2"></div></td>
                  <td class="px-6 py-5"><div class="h-4 bg-gray-800 rounded w-1/2"></div></td>
                  <td class="px-6 py-5"><div class="h-4 bg-gray-800 rounded w-1/2"></div></td>
                  <td class="px-6 py-5"><div class="h-6 bg-gray-800 rounded-full w-20"></div></td>
                  <td class="px-6 py-5 text-right"><div class="h-4 bg-gray-800 rounded w-12 ml-auto"></div></td>
                </tr>
              </template>

              <tr v-else-if="itensFiltrados.length === 0">
                <td colspan="6" class="px-6 py-12 text-center text-gray-500 text-sm">
                  Nenhum insumo corresponde à sua busca ou cadastrado no banco de dados.
                </td>
              </tr>

              <tr v-else v-for="item in itensFiltrados" :key="item.id" class="hover:bg-gray-800/20 transition-colors group">
                <td class="px-6 py-4 font-semibold text-gray-200">{{ item.name || item.nome }}</td>
                <td class="px-6 py-4 text-gray-500">{{ item.unit || item.unidade }}</td>
                <td class="px-6 py-4 font-bold text-gray-200">{{ item.currentQuantity ?? item.quantidade }}</td>
                <td class="px-6 py-4 text-gray-500">{{ item.minimumQuantity ?? item.minimo }}</td>
                <td class="px-6 py-4">
                  <span
                    class="px-3 py-1 rounded-full text-[11px] font-bold border inline-block tracking-wider uppercase"
                    :class="verificarStatus(item) === 'Crítico'
                      ? 'border-red-900/30 text-red-400 bg-red-950/30'
                      : 'border-gray-700/50 text-gray-400 bg-gray-800/30'"
                  >
                    {{ verificarStatus(item) }}
                  </span>
                </td>
                <td class="px-6 py-4 text-right">
                  <div class="flex items-center justify-end gap-3 opacity-0 group-hover:opacity-100 focus-within:opacity-100 transition-opacity">
                    <button @click="abrirModal(item)" class="text-gray-500 hover:text-white transition-colors" title="Editar">
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z"></path></svg>
                    </button>
                    <button @click="deletarItem(item.id)" class="text-gray-500 hover:text-red-400 transition-colors" title="Excluir">
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path></svg>
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- PAGINAÇÃO -->
        <div v-if="!isLoading && totalPaginas > 1" class="flex flex-col sm:flex-row items-center justify-between gap-4 px-6 py-4 border-t border-gray-800/80">
          <p class="text-xs text-gray-500">
            Página <span class="text-gray-300 font-semibold">{{ currentPage }}</span> de {{ totalPaginas }} · {{ totalFiltrado }} itens no total
          </p>
          <div class="flex items-center gap-1.5">
            <button
              @click="irParaPagina(currentPage - 1)"
              :disabled="currentPage === 1"
              class="px-3 py-2 rounded-lg text-xs font-bold text-gray-400 border border-gray-800 bg-[#0a0a0a] hover:text-white hover:border-gray-700 transition-all disabled:opacity-30 disabled:cursor-not-allowed"
            >
              Anterior
            </button>
            <button
              v-for="p in paginasVisiveis"
              :key="p"
              @click="irParaPagina(p)"
              class="w-9 py-2 rounded-lg text-xs font-bold border transition-all"
              :class="p === currentPage
                ? 'bg-gray-100 text-gray-900 border-gray-100'
                : 'text-gray-400 border-gray-800 bg-[#0a0a0a] hover:text-white hover:border-gray-700'"
            >
              {{ p }}
            </button>
            <button
              @click="irParaPagina(currentPage + 1)"
              :disabled="currentPage === totalPaginas"
              class="px-3 py-2 rounded-lg text-xs font-bold text-gray-400 border border-gray-800 bg-[#0a0a0a] hover:text-white hover:border-gray-700 transition-all disabled:opacity-30 disabled:cursor-not-allowed"
            >
              Próxima
            </button>
          </div>
        </div>
      </section>
    </main>

    <!-- ==================== MODAL ==================== -->
    <div v-if="isModalOpen" class="fixed inset-0 bg-black/80 backdrop-blur-sm flex items-center justify-center p-4 z-50">
      <div class="bg-[#111111] border border-gray-800 rounded-2xl w-full max-w-md p-6 shadow-2xl relative">
        <h3 class="text-lg font-bold text-white mb-6 border-b border-gray-800/80 pb-4">
          {{ isEditing ? 'Editar Produto' : 'Cadastrar Produto' }}
        </h3>

        <form @submit.prevent="salvarItem" class="space-y-4">

          <!-- NOME -->
          <div>
            <label class="block text-[11px] font-bold text-gray-500 mb-2 uppercase tracking-wider">Nome do Produto</label>
            <input
              v-model="formData.nome"
              type="text"
              required
              class="w-full bg-[#0a0a0a] text-gray-200 border border-gray-800 rounded-xl px-4 py-3 focus:ring-1 focus:ring-gray-600 outline-none transition-all placeholder-gray-700"
              placeholder="Ex: Filé Mignon"
            />
          </div>

          <!-- QUANTIDADE + UNIDADE (define a unidade do produto inteiro) -->
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-[11px] font-bold text-gray-500 mb-2 uppercase tracking-wider">Quantidade</label>
              <input
                v-model.number="formData.quantidade"
                type="number"
                step="0.01"
                min="0"
                required
                class="no-spinner w-full bg-[#0a0a0a] text-gray-200 border border-gray-800 rounded-xl px-4 py-3 focus:ring-1 focus:ring-gray-600 outline-none transition-all"
              />
            </div>
            <div>
              <label class="block text-[11px] font-bold text-gray-500 mb-2 uppercase tracking-wider">Unidade</label>
              <select
                v-model="formData.unidade"
                required
                class="w-full bg-[#0a0a0a] text-gray-200 border border-gray-800 rounded-xl px-4 py-3 focus:ring-1 focus:ring-gray-600 outline-none transition-all"
              >
                <option value="g">g</option>
                <option value="kg">kg</option>
                <option value="L">L</option>
                <option value="mL">mL</option>
                <option value="Un">Un</option>
              </select>
            </div>
          </div>

          <!-- ESTOQUE MÍNIMO + UNIDADE ESPELHADA (bloqueada) -->
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-[11px] font-bold text-gray-500 mb-2 uppercase tracking-wider">Estoque Mínimo</label>
              <input
                v-model.number="formData.minimo"
                type="number"
                step="0.01"
                min="0"
                required
                class="no-spinner w-full bg-[#0a0a0a] text-gray-200 border border-gray-800 rounded-xl px-4 py-3 focus:ring-1 focus:ring-gray-600 outline-none transition-all"
              />
            </div>
            <div>
              <label class="block text-[11px] font-bold text-gray-500 mb-2 uppercase tracking-wider">Unidade</label>
              <div
                aria-readonly="true"
                title="A unidade é definida no campo acima"
                class="w-full bg-[#0a0a0a]/50 text-gray-500 border border-gray-800/60 rounded-xl px-4 py-3 flex items-center justify-between select-none cursor-not-allowed"
              >
                <span>{{ formData.unidade || '—' }}</span>
                <svg xmlns="http://www.w3.org/2000/svg" class="w-3.5 h-3.5 text-gray-700" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <rect x="3" y="11" width="18" height="11" rx="2" />
                  <path d="M7 11V7a5 5 0 0110 0v4" />
                </svg>
              </div>
            </div>
          </div>

          <div class="flex justify-end gap-3 pt-4 mt-6 border-t border-gray-800/80">
            <button type="button" @click="fecharModal" class="px-4 py-2.5 text-sm font-semibold text-gray-500 hover:text-white transition-colors">
              Cancelar
            </button>
            <button
              type="submit"
              :disabled="isSaving"
              class="bg-gray-100 text-gray-900 px-6 py-2.5 rounded-xl text-sm font-bold hover:bg-white transition-colors shadow-sm disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {{ isSaving ? 'Salvando...' : (isEditing ? 'Atualizar' : 'Salvar Produto') }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { productService } from '~/services/modules/stock/productService'

// ==================== CONFIGURAÇÃO ====================
const PAGE_SIZE = 6
const HISTORICO_KEY = 'estoque:historico-busca'

// ==================== ESTADOS ====================
const searchQuery = ref('')
const historicoBusca = ref([])
const mostrarHistorico = ref(false)

const currentPage = ref(1)
const totalItens = ref(0)
const paginadoNoServidor = ref(true)

const isModalOpen = ref(false)
const isEditing = ref(false)
const isLoading = ref(true)
const isSaving = ref(false)
const errorMessage = ref('')
const formData = ref({ id: null, nome: '', unidade: 'kg', quantidade: 0, minimo: 0 })

const itensEstoque = ref([])
const itensCriticos = ref([])

// Helpers de propriedades
const getItemName = (item) => item?.name || item?.nome || ''
const getItemUnit = (item) => item?.unit || item?.unidade || ''
const getItemQty = (item) => item?.currentQuantity ?? item?.quantidade ?? 0
const getItemMin = (item) => item?.minimumQuantity ?? item?.minimo ?? 0

// ==================== HISTÓRICO DE BUSCA ====================
const carregarHistorico = () => {
  try {
    const salvo = JSON.parse(localStorage.getItem(HISTORICO_KEY))
    historicoBusca.value = Array.isArray(salvo) ? salvo.slice(0, 3) : []
  } catch {
    historicoBusca.value = []
  }
}

const registrarBusca = (termo) => {
  const t = (termo || '').trim()
  if (!t) return
  const lista = historicoBusca.value.filter(i => i.toLowerCase() !== t.toLowerCase())
  lista.unshift(t)
  historicoBusca.value = lista.slice(0, 3)
  localStorage.setItem(HISTORICO_KEY, JSON.stringify(historicoBusca.value))
}

const aplicarBusca = (termo) => {
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

  // Fallback: a API devolveu tudo, então filtra e corta no cliente
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

const irParaPagina = (p) => {
  if (p < 1 || p > totalPaginas.value || p === currentPage.value) return
  currentPage.value = p
  fetchEstoque()
}

// Busca com debounce: volta pra página 1 e consulta o banco via API
let debounceTimer = null
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

const verificarStatus = (item) => (getItemQty(item) <= getItemMin(item) ? 'Crítico' : 'Saudável')

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

// Busca de itens em estado crítico via API server-side
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

const deletarItem = async (id) => {
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
const abrirModal = (item = null) => {
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

<style scoped>
/* Remove o contador (spinner) dos inputs numéricos */
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