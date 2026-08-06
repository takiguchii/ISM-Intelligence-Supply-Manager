<Icon name="ph:package-duotone" class="text-vermelho-ISM text-xl" />    
<template>
  <div class="min-h-screen bg-amarelo-ISM text-gray-800 font-sans p-4 md:p-8 flex justify-center items-start">
    <div class="w-full max-w-6xl flex flex-col gap-6">
      
      <header class="flex flex-col md:flex-row md:items-center justify-between gap-4 bg-white p-6 rounded-2xl border border-gray-200 shadow-sm">
        <div>
          <span class="inline-block px-3 py-1 rounded-full border border-vermelho-ISM/30 text-xs font-bold text-vermelho-ISM mb-3 bg-vermelho-ISM/10 tracking-wider">
            ESTOQUE ISM
          </span>
          <h2 class="text-3xl font-extrabold text-gray-900 tracking-tight">Controle de Insumos</h2>
          <p class="text-gray-500 text-sm mt-1">Gerencie quantidades, monitore mínimos e evite rupturas no seu inventário.</p>
        </div>

        <div class="relative w-full md:w-80">
          <span class="absolute inset-y-0 left-0 flex items-center pl-4 text-gray-400"><img src="~/assets/images/pesquisa.png" alt="Ícone" class="w-5 h-5" />
          </span>
          <input 
            v-model="searchQuery"
            type="text" 
            placeholder="Buscar itens..." 
            class="w-full bg-gray-50 text-sm text-gray-900 border border-gray-200 rounded-xl pl-11 pr-4 py-3 focus:ring-2 focus:ring-vermelho-ISM/30 focus:border-transparent outline-none transition-all placeholder-gray-400"
          />
        </div>
      </header>

      <div v-if="itensCriticos.length > 0" class="border border-vermelho-ISM/30 bg-red-50 rounded-2xl p-6 shadow-sm transition-all relative overflow-hidden">
        <div class="flex items-start justify-between flex-col sm:flex-row gap-4">
          <div class="flex items-start gap-4">
            <div class="p-3 bg-vermelho-ISM/10 rounded-xl text-vermelho-ISM text-xl border border-vermelho-ISM/20">
              🚨
            </div>
            <div>
              <div class="flex items-center gap-2 mb-1">
                <span class="text-vermelho-ISM text-xs font-bold tracking-widest">✦ AGENTE DE ESTOQUE (IA)</span>
              </div>
              <h3 class="text-xl font-bold text-gray-900 mb-2">{{ itensCriticos.length }} itens em estado crítico</h3>
              <p class="text-gray-600 text-sm max-w-2xl leading-relaxed">
                Os seguintes itens atingiram ou estão abaixo do nível mínimo tolerável: <span class="text-gray-900 font-semibold">{{ nomesItensCriticos }}</span>. Deseja disparar a ordem de compra?
              </p>
            </div>
          </div>
          <button class="w-full sm:w-auto whitespace-nowrap bg-vermelho-ISM text-white px-5 py-3 rounded-xl text-sm font-bold hover:bg-red-600 active:scale-95 transition-all shadow-sm">
            Gerar Pedido Consolidado
          </button>
        </div>
      </div>

      <div class="bg-white border border-gray-200 rounded-2xl shadow-sm overflow-hidden">
        
        <div class="flex flex-col sm:flex-row items-start sm:items-center justify-between p-6 gap-4 border-b border-gray-100 bg-gray-50/50">
          <div>
            <h3 class="text-lg font-bold text-gray-900">Itens Cadastrados</h3>
            <p class="text-xs text-gray-500 mt-0.5">{{ itensFiltrados.length }} registros exibidos</p>
          </div>
          <button @click="abrirModal()" class="w-full sm:w-auto flex items-center justify-center gap-2 bg-gray-900 text-white px-5 py-2.5 rounded-xl text-sm font-bold hover:bg-gray-800 active:scale-95 transition-all">
            <span class="text-lg leading-none">+</span> Adicionar Novo Item
          </button>
        </div>

        <div class="overflow-x-auto">
          <table class="w-full text-left text-sm">
            <thead class="text-xs text-gray-500 uppercase bg-gray-50 border-b border-gray-200">
              <tr>
                <th class="px-6 py-4 font-bold tracking-wider">Insumo</th>
                <th class="px-6 py-4 font-bold tracking-wider">Unidade</th>
                <th class="px-6 py-4 font-bold tracking-wider">Qtd. Atual</th>
                <th class="px-6 py-4 font-bold tracking-wider">Mínimo</th>
                <th class="px-6 py-4 font-bold tracking-wider">Status</th>
                <th class="px-6 py-4 font-bold tracking-wider text-right">Ações</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100">
              <tr v-if="itensFiltrados.length === 0">
                <td colspan="6" class="px-6 py-12 text-center text-gray-400 font-medium">Nenhum insumo corresponde à sua busca.</td>
              </tr>
              <tr v-for="item in itensFiltrados" :key="item.id" class="hover:bg-gray-50/80 transition-colors group">
                <td class="px-6 py-5 font-semibold text-gray-900">{{ item.nome }}</td>
                <td class="px-6 py-5 text-gray-500">{{ item.unidade }}</td>
                <td class="px-6 py-5 font-bold text-gray-900">{{ item.quantidade }}</td>
                <td class="px-6 py-5 text-gray-500">{{ item.minimo }}</td>
                <td class="px-6 py-5">
                  <span 
                    class="px-3 py-1.5 rounded-full text-xs font-bold border inline-block"
                    :class="verificarStatus(item) === 'Crítico' 
                      ? 'border-red-200 text-red-600 bg-red-50' 
                      : 'border-emerald-200 text-emerald-600 bg-emerald-50'"
                  >
                    {{ verificarStatus(item) }}
                  </span>
                </td>
                <td class="px-6 py-5 text-right">
                  <div class="flex items-center justify-end gap-4 opacity-70 group-hover:opacity-100 transition-opacity">
                    <button @click="abrirModal(item)" class="text-gray-400 hover:text-gray-900 text-base" title="Editar"><img src="~/assets/images/Edit.png" alt="Ícone" class="w-5 h-5" /></button>
                    <button @click="deletarItem(item.id)" class="text-gray-400 hover:text-vermelho-ISM text-base" title="Excluir"><img src="~/assets/images/delete.png" alt="Ícone" class="w-5 h-5" /></button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <div v-if="isModalOpen" class="fixed inset-0 bg-gray-900/60 backdrop-blur-sm flex items-center justify-center p-4 z-50 animate-fadeIn">
      <div class="bg-white border border-gray-200 rounded-2xl w-full max-w-md p-6 shadow-xl relative">
        <h3 class="text-xl font-bold text-gray-900 mb-6 border-b border-gray-100 pb-3">{{ isEditing ? 'Editar Insumo' : 'Cadastrar Insumo' }}</h3>
        
        <form @submit.prevent="salvarItem" class="space-y-4">
          <div>
            <label class="block text-xs font-semibold text-gray-500 mb-1.5 uppercase tracking-wider">Nome do Insumo</label>
            <input v-model="formData.nome" type="text" required class="w-full bg-gray-50 text-gray-900 border border-gray-200 rounded-xl px-4 py-3 focus:ring-2 focus:ring-vermelho-ISM/20 outline-none transition-all" placeholder="Ex: Filé Mignon" />
          </div>
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-semibold text-gray-500 mb-1.5 uppercase tracking-wider">Unidade</label>
              <input v-model="formData.unidade" type="text" required class="w-full bg-gray-50 text-gray-900 border border-gray-200 rounded-xl px-4 py-3 focus:ring-2 focus:ring-vermelho-ISM/20 outline-none transition-all" placeholder="kg, L, un" />
            </div>
            <div>
              <label class="block text-xs font-semibold text-gray-500 mb-1.5 uppercase tracking-wider">Qtd. Atual</label>
              <input v-model.number="formData.quantidade" type="number" step="0.01" required class="w-full bg-gray-50 text-gray-900 border border-gray-200 rounded-xl px-4 py-3 focus:ring-2 focus:ring-vermelho-ISM/20 outline-none transition-all" />
            </div>
          </div>
          <div>
            <label class="block text-xs font-semibold text-gray-500 mb-1.5 uppercase tracking-wider">Estoque Mínimo</label>
            <input v-model.number="formData.minimo" type="number" step="0.01" required class="w-full bg-gray-50 text-gray-900 border border-gray-200 rounded-xl px-4 py-3 focus:ring-2 focus:ring-vermelho-ISM/20 outline-none transition-all" />
          </div>
          
          <div class="flex justify-end gap-3 pt-4 mt-6 border-t border-gray-100">
            <button type="button" @click="fecharModal" class="px-4 py-2.5 text-sm font-medium text-gray-400 hover:text-gray-600 transition-colors">Cancelar</button>
            <button type="submit" class="bg-vermelho-ISM text-white px-6 py-2.5 rounded-xl text-sm font-bold hover:bg-red-600 transition-colors shadow-sm">
              {{ isEditing ? 'Atualizar' : 'Salvar Insumo' }}
            </button>
          </div>
        </form>
      </div>
    </div>

  </div>
</template>

<script setup>
import { ref, computed } from 'vue'

const searchQuery = ref('')
const isModalOpen = ref(false)
const isEditing = ref(false)

const formData = ref({ id: null, nome: '', unidade: '', quantidade: 0, minimo: 0 })

const itensEstoque = ref([
  { id: 1, nome: 'Camarão 21/25', unidade: 'kg', quantidade: 2.1, minimo: 8 },
  { id: 2, nome: 'Funghi Porcini', unidade: 'kg', quantidade: 4.8, minimo: 3 },
  { id: 3, nome: 'Azeite Extra-Virgem', unidade: 'L', quantidade: 12, minimo: 6 },
  { id: 4, nome: 'Queijo Parmesão', unidade: 'kg', quantidade: 1.5, minimo: 5 },
])

const itensFiltrados = computed(() => {
  if (!searchQuery.value) return itensEstoque.value
  return itensEstoque.value.filter(item => 
    item.nome.toLowerCase().includes(searchQuery.value.toLowerCase())
  )
})

const verificarStatus = (item) => {
  return item.quantidade <= item.minimo ? 'Crítico' : 'Saudável'
}

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

const abrirModal = (item = null) => {
  if (item) {
    formData.value = { ...item }
    isEditing.value = true
  } else {
    formData.value = { id: Date.now(), nome: '', unidade: '', quantidade: 0, minimo: 0 }
    isEditing.value = false
  }
  isModalOpen.value = true
}

const fecharModal = () => { isModalOpen.value = false }

const salvarItem = () => {
  if (isEditing.value) {
    const index = itensEstoque.value.findIndex(i => i.id === formData.value.id)
    if (index !== -1) itensEstoque.value[index] = { ...formData.value }
  } else {
    itensEstoque.value.unshift({ ...formData.value })
  }
  fecharModal()
}

const deletarItem = (id) => {
  if (confirm('Deseja mesmo remover este item do estoque?')) {
    itensEstoque.value = itensEstoque.value.filter(item => item.id !== id)
  }
}
</script>