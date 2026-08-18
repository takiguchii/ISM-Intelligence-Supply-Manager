<template>
  <div class="flex flex-col md:flex-row min-h-screen w-full font-sans bg-gray-50">
    
    <div class="w-full md:w-[350px] lg:w-[400px] bg-vermelho-ISM text-white flex flex-col justify-between p-8 md:p-10 shrink-0 shadow-xl z-10 transition-all duration-500">
      <div>
        <div class="flex items-center gap-2 mb-10">
          <div class="w-10 h-10 bg-amarelo-ISM rounded-xl flex items-center justify-center text-vermelho-ISM font-black text-xl shadow">
            ISM
          </div>
          <span class="font-bold tracking-wide text-lg"></span>
        </div>

        <h2 class="text-3xl lg:text-4xl font-bold mb-6 leading-tight">
          {{ activeTab === 'suppliers' ? 'Conecte-se aos Melhores Fornecedores' : 'Seja um Fornecedor Parceiro' }}
        </h2>
        <p class="text-white/90 text-base leading-relaxed mb-8">
          {{ activeTab === 'suppliers' 
              ? 'Garanta os melhores preços em hortifruti, laticínios e secos para o seu pequeno comércio sem burocracia.' 
              : 'Cadastre sua distribuidora, atenda comércios do seu bairro e aumente o volume de vendas da sua empresa.' 
          }}
        </p>
      </div>
    
    </div>

    <div class="flex-1 p-6 md:p-10 lg:p-12 flex flex-col h-screen overflow-y-auto">
      
      <div v-if="activeTab === 'suppliers'" class="flex flex-col h-full max-w-7xl mx-auto w-full">
        
        <div class="flex flex-col lg:flex-row lg:items-end justify-between gap-6 mb-8">
          <div>
            <h2 class="text-3xl font-bold text-gray-900 mb-2">Fornecedores Recomendados</h2>
            <p class="text-gray-500">Parceiros validados para abastecer o seu negócio</p>
          </div>

          <div class="flex flex-col sm:flex-row gap-4 w-full lg:w-auto">
            <div class="relative w-full sm:w-80">
              <input 
                v-model="searchQuery"
                type="text" 
                placeholder="Buscar produtos ou fornecedores..." 
                class="w-full bg-white text-gray-900 border border-gray-200 rounded-xl px-4 py-3 pl-11 focus:ring-2 focus:ring-vermelho-ISM outline-none shadow-sm transition-all text-sm"
              />
              <svg class="w-5 h-5 text-gray-400 absolute left-3.5 top-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/>
              </svg>
            </div>
            
            <select 
              v-model="selectedCategory"
              class="w-full sm:w-48 bg-white border border-gray-200 text-gray-700 text-sm rounded-xl px-4 py-3 outline-none focus:ring-2 focus:ring-vermelho-ISM shadow-sm"
            >
              <option value="todos">Todas as Categorias</option>
              <option value="hortifruti">Hortifruti & Frescos</option>
              <option value="laticinios">Frios & Laticínios</option>
              <option value="secos">Secos & Molhados</option>
              <option value="bebidas">Bebidas</option>
            </select>
          </div>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6 pb-8">
          <div 
            v-for="supplier in filteredSuppliers" 
            :key="supplier.id"
            class="bg-white p-6 rounded-2xl border border-gray-200 shadow-sm hover:shadow-lg transition-all duration-300 flex flex-col justify-between group"
          >
            <div>
              <div class="flex items-center justify-between mb-4">
                <span class="text-xs font-bold px-3 py-1.5 rounded-lg bg-red-50 text-vermelho-ISM uppercase tracking-wide">
                  {{ supplier.categoryName }}
                </span>
                <span class="text-sm font-bold text-gray-700 flex items-center gap-1 bg-gray-100 px-2.5 py-1 rounded-lg">
                  ⭐ {{ supplier.rating }}
                </span>
              </div>
              <h3 class="font-bold text-gray-900 text-lg mb-2 group-hover:text-vermelho-ISM transition-colors">{{ supplier.name }}</h3>
              <p class="text-sm text-gray-500 mb-6 leading-relaxed">{{ supplier.description }}</p>
            </div>

            <div class="pt-4 border-t border-gray-100 flex items-center justify-between text-sm mt-auto">
              <span class="text-gray-500 flex items-center gap-2">
                <svg class="w-4 h-4 text-amarelo-ISM" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
                <strong class="text-gray-800">{{ supplier.deliveryTime }}</strong>
              </span>
              <button 
                @click="selectSupplier(supplier)"
                class="bg-white border-2 border-vermelho-ISM text-vermelho-ISM font-bold px-4 py-2 rounded-xl hover:bg-vermelho-ISM hover:text-white transition-colors"
              >
                Cotar
              </button>
            </div>
          </div>
          
          <div v-if="filteredSuppliers.length === 0" class="col-span-full flex flex-col items-center justify-center py-20 text-gray-500 bg-white rounded-2xl border border-dashed border-gray-300">
            <svg class="w-12 h-12 text-gray-300 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.172 16.172a4 4 0 015.656 0M9 10h.01M15 10h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
            <p class="text-lg font-semibold text-gray-700">Nenhum fornecedor encontrado</p>
            <p class="text-sm mt-1">Tente ajustar seus filtros ou termo de busca.</p>
          </div>
        </div>

        <div class="mt-auto pt-6 border-t border-gray-200 flex justify-between items-center text-sm text-gray-600">
          <span>Mostrando <strong>{{ filteredSuppliers.length }}</strong> distribuidor(es) na sua região</span>
          <a href="#" class="text-vermelho-ISM font-bold hover:underline flex items-center gap-1">
            Ver tabela de preços coletivos 
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"></path></svg>
          </a>
        </div>
      </div>

            <div class="pt-6 mt-6 border-t border-gray-100">
            </div>
        </div>
      </div>

</template>

<script setup>
import { ref, computed } from 'vue'

const activeTab = ref('suppliers')

const toggleTab = () => {
  activeTab.value = activeTab.value === 'suppliers' ? 'register' : 'suppliers'
}

const searchQuery = ref('')
const selectedCategory = ref('todos')

const suppliers = ref([
  { id: 1, name: 'HortiFrescos Distribuidora', category: 'hortifruti', categoryName: 'Hortifruti & Frescos', rating: 4.9, deliveryTime: 'Até 12 horas', description: 'Frutas, legumes e verduras fresquinhas entregues diariamente para mercearias e sacolões.' },
  { id: 2, name: 'Laticínios & Frios do Vale', category: 'laticinios', categoryName: 'Frios & Laticínios', rating: 4.8, deliveryTime: '24 horas', description: 'Queijos, presuntos, leites e manteigas fracionados para padarias e pequenos mercados.' },
  { id: 3, name: 'Atacado Central de Secos', category: 'secos', categoryName: 'Secos & Molhados', rating: 4.7, deliveryTime: '24 a 48 horas', description: 'Arroz, feijão, óleos, enlatados e farináceos com condições de atacado para pequenos volumes.' },
  { id: 4, name: 'Bebidas Express SP', category: 'bebidas', categoryName: 'Bebidas', rating: 4.6, deliveryTime: 'Mesmo dia', description: 'Refrigerantes, sucos, águas e cervejas direto das fábricas com entregas agendadas.' },
  { id: 5, name: 'Granja Bandeirantes', category: 'hortifruti', categoryName: 'Hortifruti & Frescos', rating: 4.9, deliveryTime: '24 horas', description: 'Ovos brancos e caipiras de alta qualidade, direto da granja para o seu comércio.' },
  { id: 6, name: 'Doces & Cia Atacadista', category: 'secos', categoryName: 'Secos & Molhados', rating: 4.5, deliveryTime: '48 horas', description: 'Balas, chicletes, chocolates e bomboniere em geral para pontos de venda.' }
])

const filteredSuppliers = computed(() => {
  return suppliers.value.filter(s => {
    const matchesCategory = selectedCategory.value === 'todos' || s.category === selectedCategory.value
    const query = searchQuery.value.toLowerCase()
    const matchesQuery = s.name.toLowerCase().includes(query) || 
                         s.description.toLowerCase().includes(query) ||
                         s.categoryName.toLowerCase().includes(query)
    return matchesCategory && matchesQuery
  })
})

const selectSupplier = (supplier) => {
  alert(`Iniciando cotação/pedido com: ${supplier.name}`)
}

const form = ref({
  companyName: '', cnpj: '', category: '', email: '', phone: '', deliveryTime: ''
})

const submitForm = () => {
  alert(`Cadastro de "${form.value.companyName}" recebido com sucesso!`)
  form.value = { companyName: '', cnpj: '', category: '', email: '', phone: '', deliveryTime: '' }
  activeTab.value = 'suppliers'
}
</script>