<script setup lang="ts">
import { computed, onMounted, ref } from "vue";

import AppSidebar from "~/components/layout/AppSidebar.vue";
import AppLoader from "~/components/base/AppLoader.vue";
import { useAuthStore } from "~/stores/auth";

definePageMeta({
  layout: false,
});

const authStore = useAuthStore();
const router = useRouter();

const isLoading = ref(true);
const isSidebarOpen = ref(false);

const toggleSidebar = () => {
  isSidebarOpen.value = !isSidebarOpen.value;
};

const handleLogout = () => {
  authStore.logout();
  router.push("/login");
};

onMounted(async () => {
  const start = Date.now();

  authStore.initFromStorage();

  if (!authStore.isAuthenticated) {
    router.push("/login");
    return;
  }

  setTimeout(() => {
    isLoading.value = false;
  }, Math.max(0, 1200 - (Date.now() - start)));
});

/* Dados temporários — substituir pela API posteriormente */
const financialSummary = ref({
  revenue: 0,
  expenses: 0,
  accountsReceivable: 0,
  accountsPayable: 0,
});

const balance = computed(() => {
  return (
    financialSummary.value.revenue - financialSummary.value.expenses
  );
});

const formatCurrency = (value: number) => {
  return new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency: "BRL",
  }).format(value);
};
</script>

<template>
  <div class="min-h-screen bg-zinc-950 text-zinc-100 flex flex-col font-sans">
    <AppLoader :visible="isLoading" />

    <!-- Header -->
    <header
      class="h-16 border-b border-zinc-800/80 bg-zinc-900/60 backdrop-blur-xl sticky top-0 z-30 px-4 sm:px-6 flex items-center justify-between">
      <div class="flex items-center gap-4">
        <button @click="toggleSidebar"
          class="p-2 rounded-xl text-zinc-300 hover:text-white hover:bg-zinc-800/80 transition" aria-label="Abrir menu">
          <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
          </svg>
        </button>

        <span class="font-bold text-lg text-white tracking-tight">
          ISM
        </span>
      </div>

      <button @click="handleLogout"
        class="px-4 py-2 text-xs font-semibold rounded-xl bg-zinc-800 hover:bg-zinc-700 text-zinc-200 hover:text-white border border-zinc-700/60 transition">
        Sair
      </button>
    </header>

    <!-- Sidebar -->
    <AppSidebar :isOpen="isSidebarOpen" @close="isSidebarOpen = false" />

    <!-- Main -->
    <main class="flex-1 max-w-7xl w-full mx-auto px-4 sm:px-6 py-8">
      <!-- Hero -->
      <section
        class="p-8 rounded-2xl bg-gradient-to-r from-zinc-900 via-zinc-900/90 to-zinc-950 border border-zinc-800 shadow-xl mb-8">
        <div class="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-6">
          <div>
            <div
              class="inline-flex items-center gap-2 px-3 py-1.5 rounded-full bg-zinc-800/80 border border-zinc-700/60 text-zinc-300 text-xs font-medium mb-4">
              <span class="w-2 h-2 rounded-full bg-amber-400"></span>

              Módulo financeiro
            </div>

            <h1 class="text-3xl sm:text-4xl font-bold text-white tracking-tight mb-3">
              Financeiro
            </h1>

            <p class="text-zinc-400 text-sm sm:text-base max-w-2xl">
              Acompanhe o faturamento, despesas, contas a pagar,
              contas a receber e os principais indicadores financeiros
              do negócio.
            </p>
          </div>

          <div class="flex items-center gap-3">
            <button
              class="px-4 py-2.5 rounded-xl bg-zinc-800 hover:bg-zinc-700 border border-zinc-700/60 text-sm font-semibold text-zinc-200 transition">
              Exportar
            </button>

            <button
              class="px-4 py-2.5 rounded-xl bg-white hover:bg-zinc-200 text-zinc-950 text-sm font-semibold transition">
              + Nova movimentação
            </button>
          </div>
        </div>
      </section>

      <!-- Resumo financeiro -->
      <section class="mb-8">
        <div class="flex items-center justify-between mb-4">
          <div>
            <h2 class="text-lg font-semibold text-white">
              Resumo financeiro
            </h2>

            <p class="text-sm text-zinc-500 mt-1">
              Visão geral do período atual
            </p>
          </div>

          <span class="text-xs px-3 py-1.5 rounded-lg bg-zinc-900 border border-zinc-800 text-zinc-400">
            Este mês
          </span>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4">
          <!-- Faturamento -->
          <div class="p-5 rounded-2xl bg-zinc-900/80 border border-zinc-800 hover:border-zinc-700 transition">
            <div class="flex items-center justify-between mb-5">
              <div
                class="w-10 h-10 rounded-xl bg-emerald-500/10 border border-emerald-500/20 flex items-center justify-center">
                <svg class="w-5 h-5 text-emerald-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              </div>

              <span class="text-xs font-medium text-emerald-400">
                Receita
              </span>
            </div>

            <p class="text-sm text-zinc-500 mb-1">
              Faturamento
            </p>

            <p class="text-2xl font-bold text-white">
              {{ formatCurrency(financialSummary.revenue) }}
            </p>
          </div>

          <!-- Despesas -->
          <div class="p-5 rounded-2xl bg-zinc-900/80 border border-zinc-800 hover:border-zinc-700 transition">
            <div class="flex items-center justify-between mb-5">
              <div class="w-10 h-10 rounded-xl bg-red-500/10 border border-red-500/20 flex items-center justify-center">
                <svg class="w-5 h-5 text-red-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M17 9V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-2m4-6h-6m0 0l3-3m-3 3l3 3" />
                </svg>
              </div>

              <span class="text-xs font-medium text-red-400">
                Saídas
              </span>
            </div>

            <p class="text-sm text-zinc-500 mb-1">
              Despesas
            </p>

            <p class="text-2xl font-bold text-white">
              {{ formatCurrency(financialSummary.expenses) }}
            </p>
          </div>

          <!-- A receber -->
          <div class="p-5 rounded-2xl bg-zinc-900/80 border border-zinc-800 hover:border-zinc-700 transition">
            <div class="flex items-center justify-between mb-5">
              <div
                class="w-10 h-10 rounded-xl bg-blue-500/10 border border-blue-500/20 flex items-center justify-center">
                <svg class="w-5 h-5 text-blue-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M3 10h18M7 15h1m4 0h1m-9 4h16a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                </svg>
              </div>

              <span class="text-xs font-medium text-blue-400">
                Entrada
              </span>
            </div>

            <p class="text-sm text-zinc-500 mb-1">
              A receber
            </p>

            <p class="text-2xl font-bold text-white">
              {{ formatCurrency(financialSummary.accountsReceivable) }}
            </p>
          </div>

          <!-- A pagar -->
          <div class="p-5 rounded-2xl bg-zinc-900/80 border border-zinc-800 hover:border-zinc-700 transition">
            <div class="flex items-center justify-between mb-5">
              <div
                class="w-10 h-10 rounded-xl bg-amber-500/10 border border-amber-500/20 flex items-center justify-center">
                <svg class="w-5 h-5 text-amber-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              </div>

              <span class="text-xs font-medium text-amber-400">
                Pendências
              </span>
            </div>

            <p class="text-sm text-zinc-500 mb-1">
              A pagar
            </p>

            <p class="text-2xl font-bold text-white">
              {{ formatCurrency(financialSummary.accountsPayable) }}
            </p>
          </div>
        </div>
      </section>

      <!-- Resultado + Movimentações -->
      <section class="grid grid-cols-1 lg:grid-cols-3 gap-6 mb-8">
        <!-- Resultado -->
        <div class="lg:col-span-1 p-6 rounded-2xl bg-zinc-900/80 border border-zinc-800">
          <div class="flex items-center justify-between mb-6">
            <div>
              <h2 class="text-lg font-semibold text-white">
                Resultado
              </h2>

              <p class="text-sm text-zinc-500 mt-1">
                Receita - despesas
              </p>
            </div>

            <div class="w-10 h-10 rounded-xl bg-zinc-800 flex items-center justify-center">
              <svg class="w-5 h-5 text-zinc-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                  d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </div>
          </div>

          <div class="mb-6">
            <p class="text-3xl font-bold text-white">
              {{ formatCurrency(balance) }}
            </p>

            <p class="text-xs text-zinc-500 mt-2">
              Resultado acumulado do período
            </p>
          </div>

          <div class="space-y-4">
            <div>
              <div class="flex justify-between text-sm mb-2">
                <span class="text-zinc-400">
                  Entradas
                </span>

                <span class="text-emerald-400 font-medium">
                  {{ formatCurrency(financialSummary.revenue) }}
                </span>
              </div>

              <div class="h-2 rounded-full bg-zinc-800 overflow-hidden">
                <div class="h-full rounded-full bg-emerald-500 w-0"></div>
              </div>
            </div>

            <div>
              <div class="flex justify-between text-sm mb-2">
                <span class="text-zinc-400">
                  Saídas
                </span>

                <span class="text-red-400 font-medium">
                  {{ formatCurrency(financialSummary.expenses) }}
                </span>
              </div>

              <div class="h-2 rounded-full bg-zinc-800 overflow-hidden">
                <div class="h-full rounded-full bg-red-500 w-0"></div>
              </div>
            </div>
          </div>
        </div>

        <!-- Movimentações -->
        <div class="lg:col-span-2 p-6 rounded-2xl bg-zinc-900/80 border border-zinc-800">
          <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 mb-6">
            <div>
              <h2 class="text-lg font-semibold text-white">
                Movimentações recentes
              </h2>

              <p class="text-sm text-zinc-500 mt-1">
                Últimas entradas e saídas registradas
              </p>
            </div>

            <button class="text-xs font-semibold text-zinc-300 hover:text-white transition">
              Ver todas →
            </button>
          </div>

          <!-- Empty state -->
          <div
            class="min-h-[220px] flex flex-col items-center justify-center text-center border border-dashed border-zinc-800 rounded-xl">
            <div class="w-12 h-12 rounded-xl bg-zinc-800/80 flex items-center justify-center mb-4">
              <svg class="w-6 h-6 text-zinc-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                  d="M9 17v-2m3 2v-4m3 4v-6m2 10H7a2 2 0 01-2-2V5a2 2 0 012-2h10a2 2 0 012 2v12a2 2 0 01-2 2z" />
              </svg>
            </div>

            <h3 class="text-sm font-semibold text-zinc-300">
              Nenhuma movimentação encontrada
            </h3>

            <p class="text-xs text-zinc-500 max-w-sm mt-2">
              As movimentações financeiras cadastradas
              aparecerão aqui.
            </p>
          </div>
        </div>
      </section>

      <!-- Contas -->
      <section class="mb-8">
        <div class="mb-4">
          <h2 class="text-lg font-semibold text-white">
            Contas
          </h2>

          <p class="text-sm text-zinc-500 mt-1">
            Controle das obrigações e valores pendentes
          </p>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
          <!-- Contas a pagar -->
          <div class="p-6 rounded-2xl bg-zinc-900/80 border border-zinc-800">
            <div class="flex items-start justify-between mb-6">
              <div>
                <div class="flex items-center gap-3">
                  <div
                    class="w-10 h-10 rounded-xl bg-red-500/10 border border-red-500/20 flex items-center justify-center">
                    <svg class="w-5 h-5 text-red-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                        d="M17 9V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-2m4-6h-6m0 0l3-3m-3 3l3 3" />
                    </svg>
                  </div>

                  <div>
                    <h3 class="font-semibold text-white">
                      Contas a pagar
                    </h3>

                    <p class="text-xs text-zinc-500 mt-1">
                      Despesas pendentes
                    </p>
                  </div>
                </div>
              </div>

              <span class="text-xs px-2.5 py-1 rounded-lg bg-red-500/10 text-red-400 border border-red-500/20">
                0 pendentes
              </span>
            </div>

            <div class="flex items-end justify-between">
              <div>
                <p class="text-xs text-zinc-500 mb-1">
                  Total pendente
                </p>

                <p class="text-2xl font-bold text-white">
                  {{ formatCurrency(financialSummary.accountsPayable) }}
                </p>
              </div>

              <button
                class="px-3 py-2 rounded-xl bg-zinc-800 hover:bg-zinc-700 border border-zinc-700/60 text-xs font-semibold text-zinc-300 hover:text-white transition">
                Gerenciar
              </button>
            </div>
          </div>

          <!-- Contas a receber -->
          <div class="p-6 rounded-2xl bg-zinc-900/80 border border-zinc-800">
            <div class="flex items-start justify-between mb-6">
              <div>
                <div class="flex items-center gap-3">
                  <div
                    class="w-10 h-10 rounded-xl bg-emerald-500/10 border border-emerald-500/20 flex items-center justify-center">
                    <svg class="w-5 h-5 text-emerald-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                        d="M3 10h18M7 15h1m4 0h1m-9 4h16a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                    </svg>
                  </div>

                  <div>
                    <h3 class="font-semibold text-white">
                      Contas a receber
                    </h3>

                    <p class="text-xs text-zinc-500 mt-1">
                      Valores que ainda serão recebidos
                    </p>
                  </div>
                </div>
              </div>

              <span
                class="text-xs px-2.5 py-1 rounded-lg bg-emerald-500/10 text-emerald-400 border border-emerald-500/20">
                0 pendentes
              </span>
            </div>

            <div class="flex items-end justify-between">
              <div>
                <p class="text-xs text-zinc-500 mb-1">
                  Total a receber
                </p>

                <p class="text-2xl font-bold text-white">
                  {{ formatCurrency(financialSummary.accountsReceivable) }}
                </p>
              </div>

              <button
                class="px-3 py-2 rounded-xl bg-zinc-800 hover:bg-zinc-700 border border-zinc-700/60 text-xs font-semibold text-zinc-300 hover:text-white transition">
                Gerenciar
              </button>
            </div>
          </div>
        </div>
      </section>

      <!-- DRE -->
      <section class="p-6 rounded-2xl bg-zinc-900/80 border border-zinc-800">
        <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-6">
          <div>
            <h2 class="text-lg font-semibold text-white">
              DRE
            </h2>

            <p class="text-sm text-zinc-500 mt-1">
              Demonstração do Resultado do Exercício
            </p>
          </div>

          <span class="text-xs px-3 py-1.5 rounded-full bg-zinc-800/80 border border-zinc-700/60 text-zinc-400">
            Em desenvolvimento
          </span>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <div class="p-4 rounded-xl bg-zinc-950/60 border border-zinc-800">
            <p class="text-xs text-zinc-500 mb-2">
              Receita operacional
            </p>

            <p class="text-lg font-bold text-zinc-200">
              {{ formatCurrency(financialSummary.revenue) }}
            </p>
          </div>

          <div class="p-4 rounded-xl bg-zinc-950/60 border border-zinc-800">
            <p class="text-xs text-zinc-500 mb-2">
              Custos e despesas
            </p>

            <p class="text-lg font-bold text-zinc-200">
              {{ formatCurrency(financialSummary.expenses) }}
            </p>
          </div>

          <div class="p-4 rounded-xl bg-zinc-950/60 border border-zinc-800">
            <p class="text-xs text-zinc-500 mb-2">
              Resultado líquido
            </p>

            <p class="text-lg font-bold" :class="balance >= 0
              ? 'text-emerald-400'
              : 'text-red-400'
              ">
              {{ formatCurrency(balance) }}
            </p>
          </div>
        </div>

        <div class="mt-5 p-4 rounded-xl bg-zinc-950/40 border border-dashed border-zinc-800">
          <p class="text-xs text-zinc-500 leading-relaxed">
            O detalhamento da DRE será disponibilizado após a
            integração com os dados financeiros da empresa.
          </p>
        </div>
      </section>
    </main>
  </div>
</template>