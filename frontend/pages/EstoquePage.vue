<template>
  <!-- Fundo principal mais escuro para bater com a imagem -->
  <div class="min-h-screen bg-[#0a0a0a] text-gray-300 font-sans flex flex-col">
    
    <!-- Container Principal -->
    <main class="w-full max-w-[1200px] mx-auto p-4 md:p-8 flex flex-col gap-6">
      
      <!-- Header do Módulo (Estilo da Imagem) -->
      <section class="bg-[#111111] border border-gray-800/80 p-8 rounded-2xl shadow-sm flex flex-col md:flex-row md:items-start justify-between gap-6">
        <div>
          <!-- Badge estilo o da imagem -->
          <span class="inline-block px-3 py-1 rounded-full border border-gray-800 bg-gray-800/40 text-[11px] font-bold text-gray-400 mb-4 tracking-wider uppercase">
            Estoque ISM
          </span>
          <div class="flex items-center gap-3">
            <h2 class="text-3xl font-extrabold text-white tracking-tight">Controle de Insumos</h2>
          </div>
          <p class="text-gray-500 text-sm mt-2">Gerencie quantidades, monitore mínimos e evite rupturas no seu inventário.</p>
        </div>

        <div class="relative w-full md:w-80 mt-2">
          <span class="absolute inset-y-0 left-0 flex items-center pl-4 text-gray-500">
            <!-- Icone SVG genérico de busca caso a imagem falhe -->
            <svg class="w-4 h-4 opacity-70" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"></path></svg>
          </span>
          <input 
            v-model="searchQuery"
            type="text" 
            placeholder="Buscar itens..." 
            class="w-full bg-[#0a0a0a] text-sm text-gray-200 border border-gray-800 rounded-xl pl-11 pr-4 py-3 focus:ring-1 focus:ring-gray-600 focus:border-gray-600 outline-none transition-all placeholder-gray-600"
          />
        </div>
      </section>

      <!-- Mensagem de Erro da API -->
      <div v-if="errorMessage" class="bg-red-950/30 border border-red-900/50 text-red-400 px-4 py-3 rounded-xl text-sm font-semibold flex items-center justify-between shadow-sm">
        <span>{{ errorMessage }}</span>
        <button @click="errorMessage = ''" class="text-red-400 hover:text-red-300 text-lg">&times;</button>
      </div>

      <!-- Alerta de Itens Críticos -->
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

      <!-- Tabela Principal -->
      <section class="bg-[#111111] border border-gray-800/80 rounded-2xl shadow-sm overflow-hidden">
        <div class="flex flex-col sm:flex-row items-start sm:items-center justify-between p-6 gap-4 border-b border-gray-800/80 bg-[#111111]">
          <div class="flex items-center gap-3">
            <div class="p-2 border border-gray-800 rounded-lg bg-[#0a0a0a]">
              <svg class="w-5 h-5 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4"></path></svg>
            </div>
            <div>
              <h3 class="text-base font-bold text-white">Itens Cadastrados</h3>
              <p v-if="!isLoading" class="text-xs text-gray-500 mt-0.5">{{ itensFiltrados.length }} registros exibidos</p>
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
              
              <!-- Estado de Carregamento (Skeleton) -->
              <template v-if="isLoading">
                <tr v-for="n in 4" :key="n" class="animate-pulse bg-[#111111]">
                  <td class="px-6 py-5"><div class="h-4 bg-gray-800 rounded w-3/4"></div></td>
                  <td class="px-6 py-5"><div class="h-4 bg-gray-800 rounded w-1/2"></div></td>
                  <td class="px-6 py-5"><div class="h-4 bg-gray-800 rounded w-1/2"></div></td>
                  <td class="px-6 py-5"><div class="h-4 bg-gray-800 rounded w-1/2"></div></td>
                  <td class="px-6 py-5"><div class="h-6 bg-gray-800 rounded-full w-20"></div></td>
                  <td class="px-6 py-5 text-right"><div class="h-4 bg-gray-800 rounded w-12 ml-auto"></div></td>
                </tr>
              </template>

              <!-- Estado Vazio -->
              <tr v-else-if="itensFiltrados.length === 0">
                <td colspan="6" class="px-6 py-12 text-center text-gray-500 text-sm">Nenhum insumo corresponde à sua busca ou cadastrado no banco de dados.</td>
              </tr>

              <!-- Dados Reais -->
              <tr v-else v-for="item in itensFiltrados" :key="item.id" class="hover:bg-gray-800/20 transition-colors group">
                <td class="px-6 py-4 font-semibold text-gray-200">{{ item.nome }}</td>
                <td class="px-6 py-4 text-gray-500">{{ item.unidade }}</td>
                <td class="px-6 py-4 font-bold text-gray-200">{{ item.quantidade }}</td>
                <td class="px-6 py-4 text-gray-500">{{ item.minimo }}</td>
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
                  <div class="flex items-center justify-end gap-3 opacity-0 group-hover:opacity-100 transition-opacity">
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
      </section>
    </main>

    <!-- Modal de Cadastro/Edição -->
    <div v-if="isModalOpen" class="fixed inset-0 bg-black/80 backdrop-blur-sm flex items-center justify-center p-4 z-50">
      <div class="bg-[#111111] border border-gray-800 rounded-2xl w-full max-w-md p-6 shadow-2xl relative">
        <h3 class="text-lg font-bold text-white mb-6 border-b border-gray-800/80 pb-4">{{ isEditing ? 'Editar Insumo' : 'Cadastrar Insumo' }}</h3>
        
        <form @submit.prevent="salvarItem" class="space-y-4">
          <div>
            <label class="block text-[11px] font-bold text-gray-500 mb-2 uppercase tracking-wider">Nome do Insumo</label>
            <input v-model="formData.nome" type="text" required class="w-full bg-[#0a0a0a] text-gray-200 border border-gray-800 rounded-xl px-4 py-3 focus:ring-1 focus:ring-gray-600 outline-none transition-all placeholder-gray-700" placeholder="Ex: Filé Mignon" />
          </div>
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-[11px] font-bold text-gray-500 mb-2 uppercase tracking-wider">Unidade</label>
              <input v-model="formData.unidade" type="text" required class="w-full bg-[#0a0a0a] text-gray-200 border border-gray-800 rounded-xl px-4 py-3 focus:ring-1 focus:ring-gray-600 outline-none transition-all placeholder-gray-700" placeholder="kg, L, un" />
            </div>
            <div>
              <label class="block text-[11px] font-bold text-gray-500 mb-2 uppercase tracking-wider">Qtd. Atual</label>
              <input v-model.number="formData.quantidade" type="number" step="0.01" required class="w-full bg-[#0a0a0a] text-gray-200 border border-gray-800 rounded-xl px-4 py-3 focus:ring-1 focus:ring-gray-600 outline-none transition-all" />
            </div>
          </div>
          <div>
            <label class="block text-[11px] font-bold text-gray-500 mb-2 uppercase tracking-wider">Estoque Mínimo</label>
            <input v-model.number="formData.minimo" type="number" step="0.01" required class="w-full bg-[#0a0a0a] text-gray-200 border border-gray-800 rounded-xl px-4 py-3 focus:ring-1 focus:ring-gray-600 outline-none transition-all" />
          </div>
          
          <div class="flex justify-end gap-3 pt-4 mt-6 border-t border-gray-800/80">
            <button type="button" @click="fecharModal" class="px-4 py-2.5 text-sm font-semibold text-gray-500 hover:text-white transition-colors">Cancelar</button>
            <button type="submit" :disabled="isSaving" class="bg-gray-100 text-gray-900 px-6 py-2.5 rounded-xl text-sm font-bold hover:bg-white transition-colors shadow-sm disabled:opacity-50 disabled:cursor-not-allowed">
              {{ isSaving ? 'Salvando...' : (isEditing ? 'Atualizar' : 'Salvar Insumo') }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'

// CONFIGURAÇÃO DA API
const API_URL = 'https://sua-api.com.br/api/estoque'

// ESTADOS
const searchQuery = ref('')
const isModalOpen = ref(false)
const isEditing = ref(false)
const isLoading = ref(true)
const isSaving = ref(false)
const errorMessage = ref('')
const formData = ref({ id: null, nome: '', unidade: '', quantidade: 0, minimo: 0 })

const itensEstoque = ref([])

// COMPUTED PROPERTIES
const itensFiltrados = computed(() => {
  if (!searchQuery.value) return itensEstoque.value
  return itensEstoque.value.filter(item => 
    item.nome.toLowerCase().includes(searchQuery.value.toLowerCase())
  )
})

const itensCriticos = computed(() => {
  return itensEstoque.value.filter(item => item.quantidade <= item.minimo)
})

const nomesItensCriticos = computed(() => {
  const nomes = itensCriticos.value.map(i => i.nome)
  if (nomes.length === 0) return ''
  if (nomes.length === 1) return nomes[0]
  const ultimo = nomes.pop()
  return nomes.join(', ') + ' e ' + ultimo
})

const verificarStatus = (item) => {
  return item.quantidade <= item.minimo ? 'Crítico' : 'Saudável'
}

// FUNÇÕES API
const fetchEstoque = async () => {
  isLoading.value = true
  errorMessage.value = ''
  try {
    const response = await fetch(API_URL)
    if (!response.ok) throw new Error('Falha ao buscar dados.')
    const data = await response.json()
    itensEstoque.value = data
  } catch (error) {
    console.error(error)
    errorMessage.value = 'Erro de conexão. Não foi possível carregar o estoque no momento.'
  } finally {
    isLoading.value = false
  }
}

const salvarItem = async () => {
  isSaving.value = true
  errorMessage.value = ''
  try {
    const method = isEditing.value ? 'PUT' : 'POST'
    const url = isEditing.value ? `${API_URL}/${formData.value.id}` : API_URL

    const response = await fetch(url, {
      method: method,
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(formData.value)
    })
    if (!response.ok) throw new Error('Falha ao salvar.')
    await fetchEstoque()
    fecharModal()
  } catch (error) {
    console.error(error)
    errorMessage.value = 'Erro ao tentar salvar o item.'
  } finally {
    isSaving.value = false
  }
}

const deletarItem = async (id) => {
  if (!confirm('Deseja mesmo remover este item?')) return
  isLoading.value = true
  errorMessage.value = ''
  try {
    const response = await fetch(`${API_URL}/${id}`, { method: 'DELETE' })
    if (!response.ok) throw new Error('Falha ao excluir.')
    itensEstoque.value = itensEstoque.value.filter(item => item.id !== id)
  } catch (error) {
    console.error(error)
    errorMessage.value = 'Erro ao tentar excluir o item.'
  } finally {
    isLoading.value = false
  }
}

const abrirModal = (item = null) => {
  if (item) {
    formData.value = { ...item }
    isEditing.value = true
  } else {
    formData.value = { id: null, nome: '', unidade: '', quantidade: 0, minimo: 0 }
    isEditing.value = false
  }
  isModalOpen.value = true
}

const fecharModal = () => { 
  isModalOpen.value = false 
  errorMessage.value = ''
}

const gerarPedido = () => {
  alert("Conectando ao agente para gerar pedido de: " + nomesItensCriticos.value)
}

onMounted(() => {
  fetchEstoque()
})
</script>