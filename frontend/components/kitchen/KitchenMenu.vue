<script setup lang="ts">
import { computed, ref } from 'vue'
import { useMenuCatalog } from '~/composables/useMenuCatalog'

const { dishes, categories, categoryNames, loading, error, refresh } = useMenuCatalog()
const search = ref('')
const category = ref('')
const normalize = (value: string) => value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase().trim()
const filtered = computed(() => dishes.value.filter(dish =>
  (!category.value || dish.categoryId === Number(category.value)) &&
  normalize(`${dish.name} ${dish.description ?? ''} ${categoryNames.value.get(dish.categoryId ?? 0) ?? ''}`).includes(normalize(search.value))))
const money = (value: number) => value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
</script>

<template>
  <div class="mx-auto w-full max-w-7xl space-y-6 px-4 py-8 text-zinc-900 dark:text-zinc-100 sm:px-6">
    <header class="flex flex-wrap items-start justify-between gap-4">
      <div>
        <p class="text-xs font-semibold uppercase tracking-widest text-indigo-600 dark:text-indigo-400">Consulta da cozinha</p>
        <h1 class="mt-2 text-3xl font-bold">Cardápio</h1>
        <p class="mt-2 text-sm text-zinc-500">Consulte os pratos, descrições, preços e disponibilidade do restaurante.</p>
      </div>
      <div class="flex flex-wrap gap-3">
        <NuxtLink to="/cozinha" class="rounded-lg border border-zinc-300 px-4 py-2 text-sm dark:border-zinc-700">Voltar à cozinha</NuxtLink>
        <button type="button" :disabled="loading" class="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white disabled:opacity-50" @click="refresh">Atualizar cardápio</button>
      </div>
    </header>
    <p v-if="error" role="alert" class="rounded-xl bg-red-50 p-4 text-red-700 dark:bg-red-950 dark:text-red-200">{{ error }}</p>
    <div class="grid gap-4 sm:grid-cols-2">
      <label class="text-sm font-medium">Buscar prato
        <input v-model="search" type="search" placeholder="Nome, descrição ou categoria" class="mt-2 min-h-11 w-full rounded-lg border border-zinc-300 bg-white px-3 py-2 dark:border-zinc-700 dark:bg-zinc-900" />
      </label>
      <label class="text-sm font-medium">Categoria
        <select v-model="category" class="mt-2 min-h-11 w-full rounded-lg border border-zinc-300 bg-white px-3 py-2 dark:border-zinc-700 dark:bg-zinc-900">
          <option value="">Todas as categorias</option>
          <option v-for="group in categories" :key="group.id" :value="String(group.id)">{{ group.name }}</option>
        </select>
      </label>
    </div>
    <p v-if="loading" role="status" class="text-sm text-zinc-500">Carregando cardápio...</p>
    <p v-else-if="!error && !filtered.length" class="text-sm text-zinc-500">Nenhum prato encontrado.</p>
    <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      <article v-for="dish in filtered" :key="dish.id" class="min-w-0 rounded-2xl border border-zinc-200 bg-white p-5 dark:border-zinc-800 dark:bg-zinc-900">
        <div class="flex flex-wrap items-center justify-between gap-2 text-xs">
          <span class="text-indigo-600 dark:text-indigo-400">{{ categoryNames.get(dish.categoryId ?? 0) || 'Sem categoria' }}</span>
          <span class="rounded-full px-2 py-1" :class="dish.isActive ? 'bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-200' : 'bg-zinc-100 text-zinc-600 dark:bg-zinc-800 dark:text-zinc-400'">{{ dish.isActive ? 'Disponível' : 'Indisponível' }}</span>
        </div>
        <h2 class="mt-4 break-words text-lg font-semibold">{{ dish.name }}</h2>
        <p class="my-3 whitespace-pre-line break-words text-sm text-zinc-500 dark:text-zinc-400">{{ dish.description || 'Descrição não informada.' }}</p>
        <strong>{{ money(dish.price) }}</strong>
      </article>
    </div>
  </div>
</template>
