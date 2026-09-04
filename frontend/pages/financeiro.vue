<script setup lang="ts">
import { computed, onMounted, ref } from "vue";

import AppSidebar from "~/components/layout/AppSidebar.vue";
import AppLoader from "~/components/base/AppLoader.vue";
import { useAuthStore } from "~/stores/auth";
import { useThemeStore } from "~/stores/theme";

definePageMeta({
  layout: false,
});

const authStore = useAuthStore();
const themeStore = useThemeStore();
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
  <div :class="['min-h-screen flex flex-col font-sans transition-colors duration-200', themeStore.isDark ? 'bg-zinc-950 text-zinc-100 selection:bg-zinc-800 selection:text-white' : 'bg-zinc-50 text-zinc-900 selection:bg-indigo-100 selection:text-indigo-900']">
    <AppLoader :visible="isLoading" />

    <!-- Header -->
    <header
      :class="['h-16 border-b backdrop-blur-xl sticky top-0 z-30 px-4 sm:px-6 flex items-center justify-between', themeStore.isDark ? 'border-zinc-800/80 bg-zinc-900/60' : 'border-zinc-200 bg-white/80']">
      <div class="flex items-center gap-4">
        <button @click="toggleSidebar"
          :class="['p-2 rounded-xl transition', themeStore.isDark ? 'text-zinc-300 hover:text-white hover:bg-zinc-800/80' : 'text-zinc-600 hover:text-zinc-900 hover:bg-zinc-100']" aria-label="Abrir menu">
          <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
          </svg>
        </button>

        <span :class="['font-bold text-lg tracking-tight', themeStore.isDark ? 'text-white' : 'text-zinc-900']">
          ISM
        </span>
      </div>

      <button @click="handleLogout"
        :class="['px-4 py-2 text-xs font-semibold rounded-xl border transition', themeStore.isDark ? 'bg-zinc-800 hover:bg-zinc-700 text-zinc-200 hover:text-white border-zinc-700/60' : 'bg-zinc-100 hover:bg-zinc-200 text-zinc-700 hover:text-zinc-900 border-zinc-200']">
        Sair
      </button>
    </header>

    <!-- Sidebar -->
    <AppSidebar :isOpen="isSidebarOpen" @close="isSidebarOpen = false" />

    <!-- Main -->
    <main class="flex-1 max-w-7xl w-full mx-auto px-4 sm:px-6 py-8">
      <!-- Hero -->
      <section
        :class="['p-8 rounded-2xl border shadow-xl mb-8', themeStore.isDark ? 'bg-gradient-to-r from-zinc-900 via-zinc-900/90 to-zinc-950 border-zinc-800' : 'bg-gradient-to-r from-white via-white/90 to-zinc-50 border-zinc-200']">
        <div class="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-6">
          <div>
            <div
              :class="['inline-flex items-center gap-2 px-3 py-1.5 rounded-full border text-xs font-medium mb-4', themeStore.isDark ? 'bg-zinc-800/80 border-zinc-700/60 text-zinc-300' : 'bg-zinc-100 border-zinc-200 text-zinc-700']">
              <span class="w-2 h-2 rounded-full bg-amber-400"></span>

              Módulo financeiro
            </div>

            <h1 :class="['text-3xl sm:text-4xl font-bold tracking-tight mb-3', themeStore.isDark ? 'text-white' : 'text-zinc-900']">
              Financeiro
            </h1>

            <p :class="['text-sm sm:text-base max-w-2xl', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-600']">
              Acompanhe o faturamento, despesas, contas a pagar,
              contas a receber e os principais indicadores financeiros
              do negócio.
            </p>
          </div>

          <div class="flex items-center gap-3">
            <button
              :class="['px-4 py-2.5 rounded-xl border text-sm font-semibold transition', themeStore.isDark ? 'bg-zinc-800 hover:bg-zinc-700 border-zinc-700/60 text-zinc-200' : 'bg-zinc-100 hover:bg-zinc-200 border-zinc-200 text-zinc-700']">
              Exportar
            </button>

            <button
              :class="['px-4 py-2.5 rounded-xl text-sm font-semibold transition', themeStore.isDark ? 'bg-white hover:bg-zinc-200 text-zinc-950' : 'bg-indigo-600 hover:bg-indigo-700 text-white']">
              + Nova movimentação
            </button>
          </div>
        </div>
      </section>

      <!-- Resumo financeiro -->
      <section class="mb-8">
        <div class="flex items-center justify-between mb-4">
          <div>
            <h2 :class="['text-lg font-semibold', themeStore.isDark ? 'text-white' : 'text-zinc-900']">
              Resumo financeiro
            </h2>

            <p :class="['text-sm mt-1', themeStore.isDark ? 'text-zinc-500' : 'text-zinc-500']">
              Visão geral do período atual
            </p>
          </div>

          <span :class="['text-xs px-3 py-1.5 rounded-lg border', themeStore.isDark ? 'bg-zinc-900 border-zinc-800 text-zinc-400' : 'bg-white border-zinc-200 text-zinc-600']">
            Este mês
          </span>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4">
          <!-- Faturamento -->
          <div :class="['p-5 rounded-2xl border transition hover:shadow-md', themeStore.isDark ? 'bg-zinc-900/80 border-zinc-800 hover:border-zinc-700' : 'bg-white border-zinc-200 hover:border-zinc-300']">
            <div class="flex items-center justify-between mb-5">
              <div
                class="w-10 h-10 rounded-xl bg-emerald-500/10 border border-emerald-500/20 flex items-center justify-center">
                <svg class="w-5 h-5 text-emerald-400 dark:text-emerald-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              </div>

              <span class="text-xs font-medium text-emerald-400 dark:text-emerald-600">
                Receita
              </span>
            </div>

            <p :class="['text-sm mb-1', themeStore.isDark ? 'text-zinc-500' : 'text-zinc-500']">
              Faturamento
            </p>

            <p :class="['text-2xl font-bold', themeStore.isDark ? 'text-white' : 'text-zinc-900']">
              {{ formatCurrency(financialSummary.revenue) }}
            </p>
          </div>

          <!-- Despesas -->
          <div :class="['p-5 rounded-2xl border transition hover:shadow-md', themeStore.isDark ? 'bg-zinc-900/80 border-zinc-800 hover:border-zinc-700' : 'bg-white border-zinc-200 hover:border-zinc-300']">
            <div class="flex items-center justify-between mb-5">
              <div class="w-10 h-10 rounded-xl bg-red-500/10 border border-red-500/20 flex items-center justify-center">
                <svg class="w-5 h-5 text-red-400 dark:text-red-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M17 9V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-2m4-6h-6m0 0l3-3m-3 3l3 3" />
                </svg>
              </div>

              <span class="text-xs font-medium text-red-400 dark:text-red-600">
                Saídas
              </span>
            </div>

            <p :class="['text-sm mb-1', themeStore.isDark ? 'text-zinc-500' : 'text-zinc-500']">
              Despesas
            </p>

            <p :class="['text-2xl font-bold', themeStore.isDark ? 'text-white' : 'text-zinc-900']">
              {{ formatCurrency(financialSummary.expenses) }}
            </p>
          </div>

          <!-- A receber -->
          <div :class="['p-5 rounded-2xl border transition hover:shadow-md', themeStore.isDark ? 'bg-zinc-900/80 border-zinc-800 hover:border-zinc-700' : 'bg-white border-zinc-200 hover:border-zinc-300']">
            <div class="flex items-center justify-between mb-5">
              <div
                class="w-10 h-10 rounded-xl bg-blue-500/10 border border-blue-500/20 flex items-center justify-center">
                <svg class="w-5 h-5 text-blue-400 dark:text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M3 10h18M7 15h1m4 0h1m-9 4h16a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                </svg>
              </div>

              <span class="text-xs font-medium text-blue-400 dark:text-blue-600">
                Entrada
              </span>
            </div>

            <p :class="['text-sm mb-1', themeStore.isDark ? 'text-zinc-500' : 'text-zinc-500']">
              A receber
            </p>

            <p :class="['text-2xl font-bold', themeStore.isDark ? 'text-white' : 'text-zinc-900']">
              {{ formatCurrency(financialSummary.accountsReceivable) }}
            </p>
          </div>

          <!-- A pagar -->
          <div :class="['p-5 rounded-2xl border transition hover:shadow-md', themeStore.isDark ? 'bg-zinc-900/80 border-zinc-800 hover:border-zinc-700' : 'bg-white border-zinc-200 hover:border-zinc-300']">
            <div class="flex items-center justify-between mb-5">
              <div
                class="w-10 h-10 rounded-xl bg-amber-500/10 border border-amber-500/20 flex items-center justify-center">
                <svg class="w-5 h-5 text-amber-400 dark:text-amber-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              </div>

              <span class="text-xs font-medium text-amber-400 dark:text-amber-600">
                Pendências
              </span>
            </div>

            <p :class="['text-sm mb-1', themeStore.isDark ? 'text-zinc-500' : 'text-zinc-500']">
              A pagar
            </p>

            <p :class="['text-2xl font-bold', themeStore.isDark ? 'text-white' : 'text-zinc-900']">
              {{ formatCurrency(financialSummary.accountsPayable) }}
            </p>
          </div>
        </div>
      </section>

      <!-- Resultado + Movimentações -->
      <section class="grid grid-cols-1 lg:grid-cols-3 gap-6 mb-8">
        <!-- Resultado -->
        <div :class="['lg:col-span-1 p-6 rounded-2xl border', themeStore.isDark ? 'bg-zinc-900/80 border-zinc-800' : 'bg-white border-zinc-200']">
          <div class="flex items-center justify-between mb-6">
            <div>
              <h2 :class="['text-lg font-semibold', themeStore.isDark ? 'text-white' : 'text-zinc-900']">
                Resultado
              </h2>

              <p class="text-sm text-zinc-500 mt-1">
                Receita - despesas
              </p>
            </div>

            <div :class="['w-10 h-10 rounded-xl flex items-center justify-center', themeStore.isDark ? 'bg-zinc-800' : 'bg-zinc-100']">
              <svg :class="['w-5 h-5', themeStore.isDark ? 'text-zinc-300' : 'text-zinc-600']" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                  d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </div>
          </div>

          <div class="mb-6">
            <p :class="['text-3xl font-bold', themeStore.isDark ? 'text-white' : 'text-zinc-900']">
              {{ formatCurrency(balance) }}
            </p>

            <p class="text-xs text-zinc-500 mt-2">
              Resultado acumulado do período
            </p>
          </div>

          <div class="space-y-4">
            <div>
              <div class="flex justify-between text-sm mb-2">
                <span :class="[themeStore.isDark ? 'text-zinc-400' : 'text-zinc-600']">
                  Entradas
                </span>

                <span class="text-emerald-400 dark:text-emerald-600 font-medium">
                  {{ formatCurrency(financialSummary.revenue) }}
                </span>
              </div>

              <div :class="['h-2 rounded-full overflow-hidden', themeStore.isDark ? 'bg-zinc-800' : 'bg-zinc-200']">
                <div class="h-full rounded-full bg-emerald-500 w-0"></div>
              </div>
            </div>

            <div>
              <div class="flex justify-between text-sm mb-2">
                <span :class="[themeStore.isDark ? 'text-zinc-400' : 'text-zinc-600']">
                  Saídas
                </span>

                <span class="text-red-400 dark:text-red-600 font-medium">
                  {{ formatCurrency(financialSummary.expenses) }}
                </span>
              </div>

              <div :class="['h-2 rounded-full overflow-hidden', themeStore.isDark ? 'bg-zinc-800' : 'bg-zinc-200']">
                <div class="h-full rounded-full bg-red-500 w-0"></div>
              </div>
            </div>
          </div>
        </div>

        <!-- Movimentações -->
        <div :class="['lg:col-span-2 p-6 rounded-2xl border', themeStore.isDark ? 'bg-zinc-900/80 border-zinc-800' : 'bg-white border-zinc-200']">
          <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 mb-6">
            <div>
              <h2 :class="['text-lg font-semibold', themeStore.isDark ? 'text-white' : 'text-zinc-900']">
                Movimentações recentes
              </h2>

              <p class="text-sm text-zinc-500 mt-1">
                Últimas entradas e saídas registradas
              </p>
            </div>

            <button :class="['text-xs font-semibold transition', themeStore.isDark ? 'text-zinc-300 hover:text-white' : 'text-zinc-600 hover:text-zinc-900']">
              Ver todas →
            </button>
          </div>

          <!-- Empty state -->
          <div
            :class="['min-h-[220px] flex flex-col items-center justify-center text-center border border-dashed rounded-xl', themeStore.isDark ? 'border-zinc-800' : 'border-zinc-300']">
            <div :class="['w-12 h-12 rounded-xl flex items-center justify-center mb-4', themeStore.isDark ? 'bg-zinc-800/80' : 'bg-zinc-100']">
              <svg :class="['w-6 h-6 text-zinc-500']" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                  d="M9 17v-2m3 2v-4m3 4v-6m2 10H7a2 2 0 01-2-2V5a2 2 0 012-2h10a2 2 0 012 2v12a2 2 0 01-2 2z" />
              </svg>
            </div>

            <h3 :class="['text-sm font-semibold', themeStore.isDark ? 'text-zinc-300' : 'text-zinc-700']">
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
          <h2 :class="['text-lg font-semibold', themeStore.isDark ? 'text-white' : 'text-zinc-900']">
            Contas
          </h2>

          <p class="text-sm text-zinc-500 mt-1">
            Controle das obrigações e valores pendentes
          </p>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
          <!-- Contas a pagar -->
          <div :class="['p-6 rounded-2xl border', themeStore.isDark ? 'bg-zinc-900/80 border-zinc-800' : 'bg-white border-zinc-200']">
            <div class="flex items-start justify-between mb-6">
              <div>
                <div class="flex items-center gap-3">
                  <div
                    class="w-10 h-10 rounded-xl bg-red-500/10 border border-red-500/20 flex items-center justify-center">
                    <svg class="w-5 h-5 text-red-400 dark:text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                        d="M17 9V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-2m4-6h-6m0 0l3-3m-3 3l3 3" />
                    </svg>
                  </div>

                  <div>
                    <h3 :class="['font-semibold', themeStore.isDark ? 'text-white' : 'text-zinc-900']">
                      Contas a pagar
                    </h3>

                    <p class="text-xs text-zinc-500 mt-1">
                      Despesas pendentes
                    </p>
                  </div>
                </div>
              </div>

              <span class="text-xs px-2.5 py-1 rounded-lg bg-red-500/10 text-red-400 dark:text-red-600 border border-red-500/20">
                0 pendentes
              </span>
            </div>

            <div class="flex items-end justify-between">
              <div>
                <p class="text-xs text-zinc-500 mb-1">
                  Total pendente
                </p>

                <p :class="['text-2xl font-bold', themeStore.isDark ? 'text-white' : 'text-zinc-900']">
                  {{ formatCurrency(financialSummary.accountsPayable) }}
                </p>
              </div>

              <button
                :class="['px-3 py-2 rounded-xl border text-xs font-semibold transition', themeStore.isDark ? 'bg-zinc-800 hover:bg-zinc-700 border-zinc-700/60 text-zinc-300 hover:text-white' : 'bg-zinc-100 hover:bg-zinc-200 border-zinc-200 text-zinc-700 hover:text-zinc-900']">
                Gerenciar
              </button>
            </div>
          </div>

          <!-- Contas a receber -->
          <div :class="['p-6 rounded-2xl border', themeStore.isDark ? 'bg-zinc-900/80 border-zinc-800' : 'bg-white border-zinc-200']">
            <div class="flex items-start justify-between mb-6">
              <div>
                <div class="flex items-center gap-3">
                  <div
                    class="w-10 h-10 rounded-xl bg-emerald-500/10 border border-emerald-500/20 flex items-center justify-center">
                    <svg class="w-5 h-5 text-emerald-400 dark:text-emerald-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                        d="M3 10h18M7 15h1m4 0h1m-9 4h16a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                    </svg>
                  </div>

                  <div>
                    <h3 :class="['font-semibold', themeStore.isDark ? 'text-white' : 'text-zinc-900']">
                      Contas a receber
                    </h3>

                    <p class="text-xs text-zinc-500 mt-1">
                      Valores que ainda serão recebidos
                    </p>
                  </div>
                </div>
              </div>

              <span
                class="text-xs px-2.5 py-1 rounded-lg bg-emerald-500/10 text-emerald-400 dark:text-emerald-600 border border-emerald-500/20">
                0 pendentes
              </span>
            </div>

            <div class="flex items-end justify-between">
              <div>
                <p class="text-xs text-zinc-500 mb-1">
                  Total a receber
                </p>

                <p :class="['text-2xl font-bold', themeStore.isDark ? 'text-white' : 'text-zinc-900']">
                  {{ formatCurrency(financialSummary.accountsReceivable) }}
                </p>
              </div>

              <button
                :class="['px-3 py-2 rounded-xl border text-xs font-semibold transition', themeStore.isDark ? 'bg-zinc-800 hover:bg-zinc-700 border-zinc-700/60 text-zinc-300 hover:text-white' : 'bg-zinc-100 hover:bg-zinc-200 border-zinc-200 text-zinc-700 hover:text-zinc-900']">
                Gerenciar
              </button>
            </div>
          </div>
        </div>
      </section>

      <!-- DRE -->
      <section :class="['p-6 rounded-2xl border', themeStore.isDark ? 'bg-zinc-900/80 border-zinc-800' : 'bg-white border-zinc-200']">
        <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-6">
          <div>
            <h2 :class="['text-lg font-semibold', themeStore.isDark ? 'text-white' : 'text-zinc-900']">
              DRE
            </h2>

            <p class="text-sm text-zinc-500 mt-1">
              Demonstração do Resultado do Exercício
            </p>
          </div>

          <span :class="['text-xs px-3 py-1.5 rounded-full border', themeStore.isDark ? 'bg-zinc-800/80 border-zinc-700/60 text-zinc-400' : 'bg-zinc-100 border-zinc-200 text-zinc-600']">
            Em desenvolvimento
          </span>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <div :class="['p-4 rounded-xl border', themeStore.isDark ? 'bg-zinc-950/60 border-zinc-800' : 'bg-zinc-50 border-zinc-200']">
            <p class="text-xs text-zinc-500 mb-2">
              Receita operacional
            </p>

            <p :class="['text-lg font-bold', themeStore.isDark ? 'text-zinc-200' : 'text-zinc-900']">
              {{ formatCurrency(financialSummary.revenue) }}
            </p>
          </div>

          <div :class="['p-4 rounded-xl border', themeStore.isDark ? 'bg-zinc-950/60 border-zinc-800' : 'bg-zinc-50 border-zinc-200']">
            <p class="text-xs text-zinc-500 mb-2">
              Custos e despesas
            </p>

            <p :class="['text-lg font-bold', themeStore.isDark ? 'text-zinc-200' : 'text-zinc-900']">
              {{ formatCurrency(financialSummary.expenses) }}
            </p>
          </div>

          <div :class="['p-4 rounded-xl border', themeStore.isDark ? 'bg-zinc-950/60 border-zinc-800' : 'bg-zinc-50 border-zinc-200']">
            <p class="text-xs text-zinc-500 mb-2">
              Resultado líquido
            </p>

            <p class="text-lg font-bold" :class="balance >= 0
              ? 'text-emerald-400 dark:text-emerald-600'
              : 'text-red-400 dark:text-red-600'
              ">
              {{ formatCurrency(balance) }}
            </p>
          </div>
        </div>

        <div :class="['mt-5 p-4 rounded-xl border border-dashed', themeStore.isDark ? 'bg-zinc-950/40 border-zinc-800' : 'bg-zinc-50 border-zinc-300']">
          <p class="text-xs text-zinc-500 leading-relaxed">
            O detalhamento da DRE será disponibilizado após a
            integração com os dados financeiros da empresa.
          </p>
        </div>
      </section>
    </main>
  </div>
</template>