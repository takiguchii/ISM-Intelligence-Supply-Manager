<template>
  <main class="min-h-screen bg-amarelo-ISM px-4 py-10 text-gray-900">
    <div class="mx-auto flex w-full max-w-3xl flex-col items-center">
      <div class="mb-6 flex h-14 w-14 items-center justify-center rounded-full bg-white shadow-[0_1px_3px_rgba(0,0,0,0.08)] ring-1 ring-slate-200">
        <el-icon class="h-7 w-7 text-slate-900">
          <KnifeFork />
        </el-icon>
      </div>

      <h1 class="text-3xl font-bold tracking-tight sm:text-4xl">
        Cardápio
      </h1>
      <p class="mt-3 text-center text-sm text-slate-500 sm:text-base">
        Adicione os itens do seu cardápio com nome, valor e ingredientes.
      </p>

      <section class="mt-8 w-full rounded-2xl border border-slate-200 bg-white p-4 shadow-[0_8px_24px_rgba(15,23,42,0.08)] sm:p-6">
        <div class="grid gap-3 md:grid-cols-[1fr_150px] md:items-center">
          <el-input
            v-model="name"
            size="large"
            placeholder="Nome do alimento"
            class="w-full"
            clearable
            @keyup.enter="addItem"
          />

          <el-input
            v-model.number="price"
            type="number"
            size="large"
            placeholder="Valor (R$)"
            class="w-full"
            min="0"
            step="0.01"
            @keyup.enter="addItem"
          />
        </div>

        <div class="mt-5 rounded-2xl border border-slate-200 p-4">
          <h2 class="mb-3 text-sm font-semibold text-slate-700">
            Ingredientes do prato
          </h2>

          <div class="grid gap-3 md:grid-cols-[1fr_auto] md:items-center">
            <el-input
              v-model="ingredient"
              size="large"
              placeholder="Digite um ingrediente"
              clearable
              @keyup.enter="addIngredient"
            />

            <el-button
              type="primary"
              size="large"
              class="!h-11 !rounded-xl !border-0 !bg-slate-900 !px-5 hover:!bg-black"
              @click="addIngredient"
            >
              Adicionar ingrediente
            </el-button>
          </div>

          <div v-if="ingredients.length > 0" class="mt-4 flex flex-wrap gap-2">
            <div
              v-for="(item, index) in ingredients"
              :key="index"
              class="flex items-center gap-2 rounded-full bg-slate-100 px-3 py-1 text-sm text-slate-700"
            >
              <span>{{ item }}</span>

              <button
                class="text-red-500 transition hover:text-red-700"
                @click="removeIngredient(index)"
              >
                ×
              </button>
            </div>
          </div>

          <p v-else class="mt-3 text-sm text-slate-400">
            Nenhum ingrediente adicionado ainda.
          </p>
        </div>

        <el-button
          type="primary"
          size="large"
          class="mt-5 !h-11 !w-full !rounded-xl !border-0 !bg-vermelho-ISM !px-5 hover:!bg-black"
          @click="addItem"
        >
          <span class="mr-2 text-lg leading-none">＋</span>
          Adicionar prato
        </el-button>
      </section>

      <section class="mt-10 w-full">
        <div v-if="items.length === 0" class="py-10 text-center text-sm text-slate-500 sm:text-base">
          Nenhum item ainda. Adicione o primeiro acima.
        </div>

        <div v-else class="space-y-3">
          <article
            v-for="item in items"
            :key="item.id"
            class="rounded-2xl border border-slate-200 bg-white px-4 py-3 shadow-sm"
          >
            <div class="flex items-center justify-between gap-4">
              <div class="min-w-0">
                <h2 class="truncate text-base font-semibold text-slate-900">
                  {{ item.name }}
                </h2>
                <p class="mt-1 text-sm text-slate-500">
                  Item do cardápio
                </p>
              </div>

              <div class="flex items-center gap-3">
                <div class="text-right">
                  <p class="text-base font-semibold text-slate-900">
                    {{ formatCurrency(item.price) }}
                  </p>
                </div>

                <el-button
                  circle
                  plain
                  type="danger"
                  class="!border-slate-200 !text-slate-500 hover:!text-red-600"
                  @click="removeItem(item.id)"
                >
                  ×
                </el-button>
              </div>
            </div>

            <div v-if="item.ingredients.length > 0" class="mt-4">
              <p class="mb-2 text-sm font-medium text-slate-700">
                Ingredientes:
              </p>

              <div class="flex flex-wrap gap-2">
                <span
                  v-for="(ing, index) in item.ingredients"
                  :key="index"
                  class="rounded-full bg-slate-100 px-3 py-1 text-sm text-slate-700"
                >
                  {{ ing }}
                </span>
              </div>
            </div>
          </article>
        </div>
      </section>
    </div>
  </main>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { KnifeFork } from '@element-plus/icons-vue'

type MenuItem = {
  id: number
  name: string
  price: number
  ingredients: string[]
}

const name = ref('')
const price = ref<number | null>(null)
const items = ref<MenuItem[]>([])

const ingredient = ref('')
const ingredients = ref<string[]>([])

function addIngredient() {
  const value = ingredient.value.trim()

  if (!value) {
    return
  }

  ingredients.value.push(value)
  ingredient.value = ''
}

function removeIngredient(index: number) {
  ingredients.value.splice(index, 1)
}

function addItem() {
  const cleanName = name.value.trim()
  const cleanPrice = price.value

  if (!cleanName || cleanPrice === null || Number.isNaN(cleanPrice) || cleanPrice <= 0) {
    return
  }

  items.value.unshift({
    id: Date.now(),
    name: cleanName,
    price: cleanPrice,
    ingredients: [...ingredients.value],
  })

  name.value = ''
  price.value = null
  ingredient.value = ''
  ingredients.value = []
}

function removeItem(id: number) {
  items.value = items.value.filter((item) => item.id !== id)
}

function formatCurrency(value: number) {
  return value.toLocaleString('pt-BR', {
    style: 'currency',
    currency: 'BRL',
  })
}
</script>