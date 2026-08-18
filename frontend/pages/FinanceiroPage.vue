<template>
  <div class="min-h-screen bg-gray-950 text-gray-300 font-sans">

    <main class="mx-auto w-full max-w-[1500px] px-4 py-5 md:px-8 md:py-7">
      <header class="mb-6 flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
        <div>
          <span class="mb-2 inline-flex rounded-full border border-gray-800 bg-gray-900/50 px-3 py-1 text-[11px] font-bold tracking-wider text-gray-400">
            FINANCEIRO ISM
          </span>
          <h1 class="text-3xl font-extrabold tracking-tight text-white md:text-[34px]">
            Gráficos Financeiros
          </h1>
          <p class="mt-1 text-sm text-gray-400">
            Acompanhe receitas, despesas, lucro e fluxo de caixa do seu negócio.
          </p>
        </div>

        <div class="flex flex-col gap-2 sm:flex-row">
          <select
            v-model="periodo"
            @change="fetchDashboardData"
            class="rounded-xl border border-gray-800 bg-gray-900 px-4 py-3 text-sm font-semibold text-gray-300 shadow-sm outline-none transition focus:border-gray-600 focus:ring-2 focus:ring-gray-600/20"
          >
            <option value="2026">Ano de 2026</option>
            <option value="2025">Ano de 2025</option>
            <option value="12m">Últimos 12 meses</option>
          </select>
          <button
            type="button"
            class="rounded-xl border border-gray-700 bg-transparent px-4 py-3 text-sm font-bold text-gray-300 shadow-sm transition hover:bg-gray-800 hover:text-white active:scale-[0.98]"
          >
            Exportar relatório
          </button>
        </div>
      </header>

      <!-- KPIs -->
      <section class="mb-6 grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <template v-if="kpis.length">
          <article
            v-for="card in kpis"
            :key="card.label"
            class="rounded-2xl border border-gray-800 bg-gray-900 p-5 shadow-sm"
          >
            <div class="flex items-start justify-between gap-3">
              <div>
                <p class="text-xs font-bold uppercase tracking-wider text-gray-500">{{ card.label }}</p>
                <p class="mt-2 text-2xl font-extrabold tracking-tight text-white">{{ card.value }}</p>
              </div>
              <span
                class="flex h-10 w-10 items-center justify-center rounded-xl"
                :class="card.iconBg"
              >
                <svg v-if="card.icon === 'up'" class="h-5 w-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M4 16l6-6 4 4 6-8"/>
                  <path d="M14 6h6v6"/>
                </svg>
                <svg v-else-if="card.icon === 'down'" class="h-5 w-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M4 7l6 6 4-4 6 8"/>
                  <path d="M14 18h6v-6"/>
                </svg>
                <svg v-else-if="card.icon === 'wallet'" class="h-5 w-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M3 7.5A2.5 2.5 0 0 1 5.5 5h12A2.5 2.5 0 0 1 20 7.5v9A2.5 2.5 0 0 1 17.5 19h-12A2.5 2.5 0 0 1 3 16.5z"/>
                  <path d="M3 9h14.5a2.5 2.5 0 0 1 0 5H16"/>
                  <circle cx="16" cy="11.5" r=".8"/>
                </svg>
                <svg v-else class="h-5 w-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M12 3v18M17 7.5C17 6.12 14.76 5 12 5S7 6.12 7 7.5 9.24 10 12 10s5 1.12 5 2.5S14.76 15 12 15s-5-1.12-5-2.5"/>
                </svg>
              </span>
            </div>
            <div class="mt-4 flex items-center gap-2 text-xs">
              <span
                class="rounded-full border border-gray-800 px-2 py-1 font-bold text-gray-300"
              >
                {{ card.change }}
              </span>
              <span class="text-gray-500">vs. período anterior</span>
            </div>
          </article>
        </template>
        <template v-else>
          <!-- Loading state skeleton -->
          <div v-for="i in 4" :key="i" class="h-32 rounded-2xl border border-gray-800 bg-gray-900/50 p-5 shadow-sm animate-pulse"></div>
        </template>
      </section>

      <section class="grid grid-cols-1 gap-6 xl:grid-cols-3">
        <!-- Revenue / expenses chart -->
        <article class="rounded-2xl border border-gray-800 bg-gray-900 p-5 shadow-sm xl:col-span-2">
          <div class="mb-5 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <h2 class="text-lg font-extrabold text-white">Receitas x Despesas</h2>
              <p class="mt-1 text-xs text-gray-400">Comparativo mensal em R$</p>
            </div>
            <div class="flex items-center gap-4 text-xs font-semibold text-gray-400">
              <span class="flex items-center gap-2"><i class="h-2.5 w-2.5 rounded-full bg-gray-300"></i>Receitas</span>
              <span class="flex items-center gap-2"><i class="h-2.5 w-2.5 rounded-full bg-gray-700"></i>Despesas</span>
            </div>
          </div>

          <div class="overflow-x-auto">
            <div class="min-w-[680px]">
              <div class="relative h-[330px]">
                <div class="absolute inset-0 flex flex-col justify-between">
                  <span v-for="tick in [100, 75, 50, 25, 0]" :key="tick" class="border-t border-dashed border-gray-800 text-[10px] text-gray-500">
                    <span class="relative -top-2 mr-2 inline-block w-10 text-right">{{ formatCompact((maxChartValue * tick) / 100) }}</span>
                  </span>
                </div>

                <div class="absolute inset-x-14 bottom-8 top-2 flex items-end justify-between gap-3">
                  <div v-for="item in monthlyData" :key="item.month" class="flex h-full flex-1 items-end justify-center gap-1.5">
                    <div
                      class="w-5 rounded-t-md bg-gray-300 transition-all duration-300 hover:bg-white"
                      :style="{ height: `${maxChartValue ? (item.receita / maxChartValue) * 100 : 0}%` }"
                      :title="`Receita ${item.month}: ${formatMoney(item.receita)}`"
                    ></div>
                    <div
                      class="w-5 rounded-t-md bg-gray-700 transition-all duration-300 hover:bg-gray-600"
                      :style="{ height: `${maxChartValue ? (item.despesa / maxChartValue) * 100 : 0}%` }"
                      :title="`Despesa ${item.month}: ${formatMoney(item.despesa)}`"
                    ></div>
                  </div>
                </div>

                <div class="absolute inset-x-14 bottom-0 flex justify-between gap-3">
                  <span v-for="item in monthlyData" :key="`${item.month}-label`" class="flex-1 text-center text-[10px] font-bold text-gray-500">
                    {{ item.month }}
                  </span>
                </div>
              </div>
            </div>
          </div>
        </article>

        <!-- Expense categories -->
        <article class="rounded-2xl border border-gray-800 bg-gray-900 p-5 shadow-sm">
          <div>
            <h2 class="text-lg font-extrabold text-white">Despesas por categoria</h2>
            <p class="mt-1 text-xs text-gray-400">Distribuição do período selecionado</p>
          </div>

          <div class="my-7 flex justify-center">
            <div
              class="relative h-48 w-48 rounded-full"
              :style="{ background: donutGradient }"
            >
              <div class="absolute inset-7 flex flex-col items-center justify-center rounded-full bg-gray-900 text-center">
                <span class="text-[11px] font-bold uppercase tracking-wider text-gray-500">Total</span>
                <strong class="mt-1 text-lg font-extrabold text-white">{{ formatCompact(totalDespesas) }}</strong>
              </div>
            </div>
          </div>

          <div class="space-y-3">
            <div v-if="expenseCategories.length === 0" class="text-center text-xs text-gray-500 py-4">
              Sem dados para exibir
            </div>
            <div v-for="item in expenseCategories" :key="item.nome" class="flex items-center justify-between text-sm">
              <div class="flex items-center gap-2">
                <span class="h-2.5 w-2.5 rounded-full" :class="item.dot"></span>
                <span class="font-semibold text-gray-400">{{ item.nome }}</span>
              </div>
              <div class="text-right">
                <span class="font-extrabold text-white">{{ item.percentual }}%</span>
                <span class="ml-2 text-xs text-gray-500">{{ formatCompact(item.valor) }}</span>
              </div>
            </div>
          </div>
        </article>

        <!-- Cash flow -->
        <article class="rounded-2xl border border-gray-800 bg-gray-900 p-5 shadow-sm xl:col-span-2">
          <div class="mb-5">
            <h2 class="text-lg font-extrabold text-white">Fluxo de caixa</h2>
            <p class="mt-1 text-xs text-gray-400">Saldo acumulado ao longo dos últimos meses</p>
          </div>

          <div class="overflow-x-auto">
            <div class="min-w-[680px]">
              <svg viewBox="0 0 760 280" class="h-[280px] w-full overflow-visible" role="img" aria-label="Gráfico de linha do fluxo de caixa">
                <g v-for="(tick, index) in cashTicks" :key="tick">
                  <line x1="54" :y1="24 + index * 55" x2="740" :y2="24 + index * 55" stroke="#1f2937" stroke-dasharray="4 4" />
                  <text x="45" :y="29 + index * 55" text-anchor="end" font-size="10" fill="#6b7280">{{ formatCompact(tick) }}</text>
                </g>

                <polyline
                  v-if="cashPoints.length"
                  :points="cashAreaPoints"
                  fill="rgba(229,231,235,.05)"
                  stroke="none"
                />
                <polyline
                  v-if="cashPoints.length"
                  :points="cashLinePoints"
                  fill="none"
                  stroke="#e5e7eb"
                  stroke-width="3"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                />

                <g v-for="point in cashPoints" :key="point.month">
                  <circle :cx="point.x" :cy="point.y" r="5" fill="#111827" stroke="#e5e7eb" stroke-width="3" />
                  <text :x="point.x" y="270" text-anchor="middle" font-size="10" font-weight="700" fill="#6b7280">
                    {{ point.month }}
                  </text>
                </g>
              </svg>
            </div>
          </div>
        </article>

        <!-- Financial insights -->
        <article class="rounded-2xl border border-gray-800 bg-gray-900 p-5 shadow-sm">
          <div class="mb-5">
            <h2 class="text-lg font-extrabold text-white">Resumo financeiro</h2>
            <p class="mt-1 text-xs text-gray-400">Indicadores de acompanhamento</p>
          </div>

          <div class="space-y-4">
            <div class="rounded-xl border border-gray-800 bg-gray-800/50 p-4">
              <p class="text-[11px] font-bold uppercase tracking-wider text-gray-500">Margem líquida</p>
              <div class="mt-2 flex items-end justify-between gap-4">
                <strong class="text-2xl font-extrabold text-white">{{ resumoFinanceiro.margemLiquida }}</strong>
                <span class="text-xs font-bold text-gray-300">
                  {{ resumoFinanceiro.margemLiquidaChange }}
                </span>
              </div>
              <div class="mt-3 h-2 overflow-hidden rounded-full bg-gray-800">
                <div class="h-full rounded-full bg-gray-300" :style="{ width: `${resumoFinanceiro.margemLiquidaProgresso}%` }"></div>
              </div>
            </div>

            <div class="rounded-xl border border-gray-800 bg-gray-800/50 p-4">
              <p class="text-[11px] font-bold uppercase tracking-wider text-gray-500">Ponto de equilíbrio</p>
              <div class="mt-2 flex items-end justify-between gap-4">
                <strong class="text-2xl font-extrabold text-white">{{ formatCompact(resumoFinanceiro.pontoEquilibrioValor) }}</strong>
                <span class="text-xs font-bold text-gray-500">mês</span>
              </div>
              <p class="mt-2 text-xs leading-relaxed text-gray-400">
                A receita atual está <span class="font-bold text-gray-200">{{ resumoFinanceiro.pontoEquilibrioAcima }}%</span> acima do ponto de equilíbrio.
              </p>
            </div>

            <div class="rounded-xl border border-gray-800 bg-gray-900/50 p-4">
              <div class="flex items-start gap-3">
                <span class="mt-0.5 text-gray-400">✦</span>
                <div>
                  <p class="text-sm font-extrabold text-white">Insight do período</p>
                  <p class="mt-1 text-xs leading-relaxed text-gray-400">
                    {{ resumoFinanceiro.insightTexto }}
                  </p>
                </div>
              </div>
            </div>
          </div>
        </article>

        <!-- Transactions -->
        <article class="overflow-hidden rounded-2xl border border-gray-800 bg-gray-900 shadow-sm xl:col-span-3">
          <div class="flex flex-col gap-3 border-b border-gray-800 bg-gray-800/50 p-5 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <h2 class="text-lg font-extrabold text-white">Movimentações recentes</h2>
              <p class="mt-1 text-xs text-gray-400">Últimas entradas e saídas registradas</p>
            </div>
            <button type="button" class="text-xs font-bold text-gray-400 hover:text-white hover:underline">Ver todas</button>
          </div>

          <div class="overflow-x-auto">
            <table class="w-full min-w-[760px] text-left text-sm">
              <thead class="border-b border-gray-800 bg-gray-800/50 text-[11px] uppercase tracking-wider text-gray-500">
                <tr>
                  <th class="px-5 py-4 font-bold">Descrição</th>
                  <th class="px-5 py-4 font-bold">Categoria</th>
                  <th class="px-5 py-4 font-bold">Data</th>
                  <th class="px-5 py-4 font-bold">Tipo</th>
                  <th class="px-5 py-4 text-right font-bold">Valor</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-gray-800">
                <tr v-if="transactions.length === 0">
                  <td colspan="5" class="px-5 py-8 text-center text-gray-500 text-xs">Nenhuma movimentação encontrada neste período.</td>
                </tr>
                <tr v-for="item in transactions" :key="item.descricao + item.data" class="transition hover:bg-gray-800/80">
                  <td class="px-5 py-4 font-bold text-white">{{ item.descricao }}</td>
                  <td class="px-5 py-4 text-gray-400">{{ item.categoria }}</td>
                  <td class="px-5 py-4 text-gray-400">{{ item.data }}</td>
                  <td class="px-5 py-4">
                    <span class="rounded-full border border-gray-700 bg-transparent px-2.5 py-1 text-[11px] font-bold text-gray-300">
                      {{ item.tipo }}
                    </span>
                  </td>
                  <td
                    class="px-5 py-4 text-right font-extrabold"
                    :class="item.tipo === 'Entrada' ? 'text-gray-200' : 'text-gray-500'"
                  >
                    {{ item.tipo === 'Entrada' ? '+' : '-' }} {{ formatMoney(item.valor) }}
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </article>
      </section>
    </main>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, onMounted } from 'vue'

const periodo = ref('2026')

// --- ESTADOS VAZIOS PARA CONEXÃO COM A API ---
const kpis = ref<any[]>([])
const monthlyData = ref<any[]>([])
const expenseCategories = ref<any[]>([])
const transactions = ref<any[]>([])
const cashSeries = ref<any[]>([])

const resumoFinanceiro = ref({
  margemLiquida: '0%',
  margemLiquidaChange: '0%',
  margemLiquidaPositiva: true,
  margemLiquidaProgresso: 0,
  pontoEquilibrioValor: 0,
  pontoEquilibrioAcima: 0,
  insightTexto: 'Carregando insights do período...'
})

// --- COMPUTEDS (Protegidos contra arrays vazios) ---

const maxChartValue = computed(() => {
  const receitas = monthlyData.value.map(item => item.receita)
  return receitas.length ? Math.max(...receitas, 10000) : 100000
})

const totalDespesas = computed(() => {
  return expenseCategories.value.reduce((sum, item) => sum + item.valor, 0)
})

const donutGradient = computed(() => {
  if (expenseCategories.value.length === 0) return 'conic-gradient(#1f2937 0% 100%)'
  
  let start = 0
  const stops = expenseCategories.value.map((item) => {
    const end = start + item.percentual
    const color = item.dot === 'bg-gray-300' ? '#d1d5db' : item.dot === 'bg-gray-500' ? '#6b7280' : item.dot === 'bg-gray-700' ? '#374151' : '#1f2937'
    const segment = `${color} ${start}% ${end}%`
    start = end
    return segment
  })
  return `conic-gradient(${stops.join(', ')})`
})

const cashMin = computed(() => {
  const values = cashSeries.value.map(c => c.value)
  return values.length ? Math.min(...values) * 0.9 : 0
})

const cashMax = computed(() => {
  const values = cashSeries.value.map(c => c.value)
  return values.length ? Math.max(...values) * 1.1 : 100000
})

const cashTicks = computed(() => {
  const max = cashMax.value
  const min = cashMin.value
  const step = (max - min) / 4 || 25000
  return [max, max - step, max - 2 * step, max - 3 * step, min]
})

const cashPoints = computed(() => {
  if (cashSeries.value.length === 0) return []
  const diff = cashMax.value - cashMin.value || 1
  return cashSeries.value.map((point, index) => ({
    month: point.month,
    value: point.value,
    x: 80 + index * 85,
    y: 24 + ((cashMax.value - point.value) / diff) * 220,
  }))
})

const cashLinePoints = computed(() => cashPoints.value.map(point => `${point.x},${point.y}`).join(' '))

const cashAreaPoints = computed(() => {
  if (cashPoints.value.length === 0) return ''
  const first = cashPoints.value[0]
  const last = cashPoints.value[cashPoints.value.length - 1]
  return `${first.x},244 ${cashPoints.value.map(point => `${point.x},${point.y}`).join(' ')} ${last.x},244`
})

// --- HELPERS DE FORMATAÇÃO ---

const formatMoney = (value: number) => {
  return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value || 0)
}

const formatCompact = (value: number) => {
  const val = value || 0
  if (val >= 1000) return `R$ ${(val / 1000).toFixed(0)}k`
  return `R$ ${val.toFixed(0)}`
}

// --- CHAMADA A API DO BACKEND ---

const fetchDashboardData = async () => {
  try {

  } catch (error) {
    console.error("Erro ao carregar dashboard financeiro:", error)
  }
}

onMounted(() => {
  fetchDashboardData()
})
</script>