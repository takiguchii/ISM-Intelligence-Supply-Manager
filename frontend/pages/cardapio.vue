<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import AppSidebar from '~/components/layout/AppSidebar.vue'
import AppLoader from '~/components/base/AppLoader.vue'
import { useAuthStore } from '~/stores/auth'
import { menuService } from '~/services/modules/menu/menuService'

definePageMeta({
  layout: false
})

interface Category {
  id: number
  name: string
  displayOrder?: number
  isActive?: boolean
  createdAtUtc?: string
  updatedAtUtc?: string
}

interface Ingredient {
  productId: number
  quantity: number
}

interface Dish {
  id: number
  name: string
  description?: string
  categoryId?: number
  category?: Category | null
  price: number
  cost: number
  isActive: boolean
  highlight: boolean
  urlImage?: string
  displayOrder?: number
  ingredients?: Ingredient[]
  createdAtUtc?: string
  updatedAtUtc?: string
}

interface ApiError {
  message?: string
  errors?: Record<string, string[]>
}

const authStore = useAuthStore()
const router = useRouter()
const config = useRuntimeConfig()

const API_BASE =
  config.public?.apiBase ||
  'http://localhost:8080'

const isLoading = ref(true)
const isSidebarOpen = ref(false)

const categories = ref<Category[]>([])
const dishes = ref<Dish[]>([])

const search = ref('')
const selectedCategory = ref<number | null>(null)
const statusFilter = ref<'all' | 'active' | 'inactive'>('all')
const highlightFilter = ref<'all' | 'highlight'>('all')

const showDishModal = ref(false)
const showCategoryModal = ref(false)

const isEditingDish = ref(false)
const isEditingCategory = ref(false)

const editingDishId = ref<number | null>(null)
const editingCategoryId = ref<number | null>(null)

const isSavingDish = ref(false)
const isSavingCategory = ref(false)

const isDeletingDish = ref<number | null>(null)
const isDeletingCategory = ref<number | null>(null)

const errorMessage = ref('')
const successMessage = ref('')

const dishForm = ref({
  name: '',
  description: '',
  categoryId: null as number | null,
  price: 0,
  cost: 0,
  isActive: true,
  highlight: false,
  urlImage: '',
  displayOrder: 0,
  ingredients: [] as Ingredient[]
})

const categoryForm = ref({
  name: '',
  displayOrder: 0,
  isActive: true
})

const newIngredient = ref({
  productId: 0,
  quantity: 0
})

const toggleSidebar = () => {
  isSidebarOpen.value = !isSidebarOpen.value
}

const handleLogout = () => {
  authStore.logout()
  router.push('/login')
}

/**
 * Tenta obter o token de diferentes formatos comuns
 * do authStore/localStorage.
 *
 * Caso seu authStore tenha um nome específico para o token,
 * você pode simplificar esta função.
 */
const getToken = (): string | null => {
  const store = authStore as any

  const token =
    store.token ||
    store.accessToken ||
    store.access_token ||
    store.jwt

  if (token) {
    return token
  }

  if (import.meta.client) {
    const possibleKeys = [
      'token',
      'accessToken',
      'access_token',
      'jwt',
      'auth'
    ]

    for (const key of possibleKeys) {
      const value = localStorage.getItem(key)

      if (!value) continue

      try {
        const parsed = JSON.parse(value)

        if (typeof parsed === 'string') {
          return parsed
        }

        if (parsed?.token) {
          return parsed.token
        }

        if (parsed?.accessToken) {
          return parsed.accessToken
        }

        if (parsed?.access_token) {
          return parsed.access_token
        }
      } catch {
        return value
      }
    }
  }

  return null
}

const getHeaders = () => {
  const token = getToken()

  return {
    'Content-Type': 'application/json',
    ...(token
      ? {
        Authorization: `Bearer ${token}`
      }
      : {})
  }
}

const handleApiError = (error: any) => {
  console.error('Erro na API:', error)

  const data = error?.data as ApiError | undefined

  if (data?.message) {
    errorMessage.value = data.message
    return
  }

  if (data?.errors) {
    const messages = Object.values(data.errors).flat()

    if (messages.length) {
      errorMessage.value = messages.join(', ')
      return
    }
  }

  if (error?.statusMessage) {
    errorMessage.value = error.statusMessage
    return
  }

  errorMessage.value =
    'Não foi possível concluir a operação. Tente novamente.'
}

const normalizeArrayResponse = <T,>(response: any): T[] => {
  if (Array.isArray(response)) {
    return response
  }

  if (Array.isArray(response?.data)) {
    return response.data
  }

  if (Array.isArray(response?.items)) {
    return response.items
  }

  if (Array.isArray(response?.results)) {
    return response.results
  }

  return []
}

const getCategoryName = (categoryId?: number) => {
  if (!categoryId) {
    return 'Sem categoria'
  }

  const category = categories.value.find(
    item => item.id === categoryId
  )

  return category?.name || 'Sem categoria'
}

const getDishCategoryId = (dish: Dish) => {
  return dish.categoryId ?? dish.category?.id ?? null
}

const getDishCategoryName = (dish: Dish) => {
  return (
    dish.category?.name ||
    getCategoryName(getDishCategoryId(dish) ?? undefined)
  )
}

/*
   API - CATEGORIES
*/

const fetchCategories = async () => {
  const restaurantId = getRestaurantId()
  const response = await menuService.getCategories(restaurantId || undefined)
  categories.value = normalizeArrayResponse<Category>(response)
}

const createCategory = async () => {
  const name = categoryForm.value.name.trim()

  if (!name) {
    errorMessage.value = 'Informe o nome da categoria.'
    return
  }

  isSavingCategory.value = true
  errorMessage.value = ''

  try {
    await menuService.createCategory({
      name,
      displayOrder: categoryForm.value.displayOrder,
      isActive: categoryForm.value.isActive
    })

    successMessage.value = 'Categoria criada com sucesso.'

    closeCategoryModal()
    await fetchCategories()

  } catch (error) {
    handleApiError(error)
  } finally {
    isSavingCategory.value = false
  }
}

const updateCategory = async () => {
  if (!editingCategoryId.value) {
    return
  }

  const name = categoryForm.value.name.trim()

  if (!name) {
    errorMessage.value = 'Informe o nome da categoria.'
    return
  }

  isSavingCategory.value = true
  errorMessage.value = ''

  try {
    await menuService.updateCategory(editingCategoryId.value, {
      name,
      displayOrder: categoryForm.value.displayOrder,
      isActive: categoryForm.value.isActive
    })

    successMessage.value = 'Categoria atualizada com sucesso.'

    closeCategoryModal()
    await fetchCategories()

  } catch (error) {
    handleApiError(error)
  } finally {
    isSavingCategory.value = false
  }
}

const deleteCategory = async (category: Category) => {
  const confirmed = window.confirm(
    `Deseja realmente excluir a categoria "${category.name}"?`
  )

  if (!confirmed) {
    return
  }

  isDeletingCategory.value = category.id
  errorMessage.value = ''

  try {
    await menuService.deleteCategory(category.id)

    if (selectedCategory.value === category.id) {
      selectedCategory.value = null
    }

    successMessage.value = 'Categoria excluída com sucesso.'

    await Promise.all([
      fetchCategories(),
      fetchDishes()
    ])

  } catch (error) {
    handleApiError(error)
  } finally {
    isDeletingCategory.value = null
  }
}

/*
   API - DISHES
*/

const fetchDishes = async () => {
  const restaurantId = getRestaurantId()
  const response = await menuService.getDishes(restaurantId || undefined)
  dishes.value = normalizeArrayResponse<Dish>(response)
}

const createDish = async () => {
  if (!dishForm.value.name.trim()) {
    errorMessage.value = 'Informe o nome do prato.'
    return
  }

  if (!dishForm.value.categoryId) {
    errorMessage.value = 'Selecione uma categoria.'
    return
  }

  if (dishForm.value.price <= 0) {
    errorMessage.value = 'Informe um preço de venda válido.'
    return
  }

  isSavingDish.value = true
  errorMessage.value = ''

  try {
    await menuService.createDish({
      name: dishForm.value.name.trim(),
      description: dishForm.value.description.trim() || undefined,
      categoryId: dishForm.value.categoryId,
      price: Number(dishForm.value.price),
      cost: Number(dishForm.value.cost),
      isActive: dishForm.value.isActive,
      highlight: dishForm.value.highlight,
      urlImage: dishForm.value.urlImage.trim() || undefined,
      displayOrder: Number(dishForm.value.displayOrder),
      ingredients: dishForm.value.ingredients.map(ingredient => ({
        productId: Number(ingredient.productId),
        quantity: Number(ingredient.quantity)
      }))
    })

    successMessage.value =
      'Prato criado com sucesso.'

    closeDishModal()
    await fetchDishes()

  } catch (error) {
    handleApiError(error)
  } finally {
    isSavingDish.value = false
  }
}

const updateDish = async () => {
  if (!editingDishId.value) {
    return
  }

  if (!dishForm.value.name.trim()) {
    errorMessage.value = 'Informe o nome do prato.'
    return
  }

  if (!dishForm.value.categoryId) {
    errorMessage.value = 'Selecione uma categoria.'
    return
  }

  if (dishForm.value.price <= 0) {
    errorMessage.value = 'Informe um preço de venda válido.'
    return
  }

  isSavingDish.value = true
  errorMessage.value = ''

  try {
    await menuService.updateDish(editingDishId.value, {
      name: dishForm.value.name.trim(),
      description: dishForm.value.description.trim() || undefined,
      categoryId: dishForm.value.categoryId,
      price: Number(dishForm.value.price),
      cost: Number(dishForm.value.cost),
      isActive: dishForm.value.isActive,
      highlight: dishForm.value.highlight,
      urlImage: dishForm.value.urlImage.trim() || undefined,
      displayOrder: Number(dishForm.value.displayOrder),
      ingredients: dishForm.value.ingredients.map(ingredient => ({
        productId: Number(ingredient.productId),
        quantity: Number(ingredient.quantity)
      }))
    })

    successMessage.value =
      'Prato atualizado com sucesso.'

    closeDishModal()
    await fetchDishes()

  } catch (error) {
    handleApiError(error)
  } finally {
    isSavingDish.value = false
  }
}

const deleteDish = async (dish: Dish) => {
  const confirmed = window.confirm(
    `Deseja realmente excluir o prato "${dish.name}"?`
  )

  if (!confirmed) {
    return
  }

  isDeletingDish.value = dish.id
  errorMessage.value = ''

  try {
    await menuService.deleteDish(dish.id)

    successMessage.value =
      'Prato excluído com sucesso.'

    await fetchDishes()

  } catch (error) {
    handleApiError(error)
  } finally {
    isDeletingDish.value = null
  }
}

/*
   MODALS
*/

const resetDishForm = () => {
  dishForm.value = {
    name: '',
    description: '',
    categoryId: selectedCategory.value,
    price: 0,
    cost: 0,
    isActive: true,
    highlight: false,
    urlImage: '',
    displayOrder: 0,
    ingredients: []
  }

  newIngredient.value = {
    productId: 0,
    quantity: 0
  }

  editingDishId.value = null
  isEditingDish.value = false
}

const openCreateDishModal = () => {
  errorMessage.value = ''
  resetDishForm()
  showDishModal.value = true
}

const openEditDishModal = (dish: Dish) => {
  errorMessage.value = ''

  editingDishId.value = dish.id
  isEditingDish.value = true

  dishForm.value = {
    name: dish.name || '',
    description: dish.description || '',
    categoryId: getDishCategoryId(dish),
    price: Number(dish.price || 0),
    cost: Number(dish.cost || 0),
    isActive: dish.isActive ?? true,
    highlight: dish.highlight ?? false,
    urlImage: dish.urlImage || '',
    displayOrder: Number(
      dish.displayOrder || 0
    ),
    ingredients: (dish.ingredients || []).map(
      ingredient => ({
        productId: Number(
          ingredient.productId
        ),
        quantity: Number(
          ingredient.quantity
        )
      })
    )
  }

  showDishModal.value = true
}

const closeDishModal = () => {
  showDishModal.value = false
  resetDishForm()
}

const submitDish = async () => {
  if (isEditingDish.value) {
    await updateDish()
    return
  }

  await createDish()
}

const resetCategoryForm = () => {
  categoryForm.value = {
    name: '',
    displayOrder: 0,
    isActive: true
  }

  editingCategoryId.value = null
  isEditingCategory.value = false
}

const openCreateCategoryModal = () => {
  errorMessage.value = ''
  resetCategoryForm()
  showCategoryModal.value = true
}

const openEditCategoryModal = (
  category: Category
) => {
  errorMessage.value = ''

  editingCategoryId.value = category.id
  isEditingCategory.value = true

  categoryForm.value = {
    name: category.name || '',
    displayOrder: Number(
      category.displayOrder || 0
    ),
    isActive: category.isActive ?? true
  }

  showCategoryModal.value = true
}

const closeCategoryModal = () => {
  showCategoryModal.value = false
  resetCategoryForm()
}

const submitCategory = async () => {
  if (isEditingCategory.value) {
    await updateCategory()
    return
  }

  await createCategory()
}

/*
   INGREDIENTS
*/

const addIngredient = () => {
  if (
    newIngredient.value.productId <= 0 ||
    newIngredient.value.quantity <= 0
  ) {
    errorMessage.value =
      'Informe um Product ID e uma quantidade válida.'

    return
  }

  const alreadyExists =
    dishForm.value.ingredients.some(
      ingredient =>
        ingredient.productId ===
        newIngredient.value.productId
    )

  if (alreadyExists) {
    errorMessage.value =
      'Esse produto já foi adicionado aos ingredientes.'

    return
  }

  dishForm.value.ingredients.push({
    productId: Number(
      newIngredient.value.productId
    ),
    quantity: Number(
      newIngredient.value.quantity
    )
  })

  newIngredient.value = {
    productId: 0,
    quantity: 0
  }

  errorMessage.value = ''
}

const removeIngredient = (
  index: number
) => {
  dishForm.value.ingredients.splice(
    index,
    1
  )
}

/*
   FILTERS
*/

const filteredDishes = computed(() => {
  const term =
    search.value.trim().toLowerCase()

  return dishes.value
    .filter(dish => {
      if (!term) {
        return true
      }

      return (
        dish.name
          ?.toLowerCase()
          .includes(term) ||
        dish.description
          ?.toLowerCase()
          .includes(term)
      )
    })
    .filter(dish => {
      if (statusFilter.value === 'all') {
        return true
      }

      if (
        statusFilter.value === 'active'
      ) {
        return dish.isActive
      }

      return !dish.isActive
    })
    .filter(dish => {
      if (
        highlightFilter.value === 'all'
      ) {
        return true
      }

      return dish.highlight
    })
})

const totalDishes = computed(
  () => dishes.value.length
)

const activeDishes = computed(
  () =>
    dishes.value.filter(
      dish => dish.isActive
    ).length
)

const highlightedDishes = computed(
  () =>
    dishes.value.filter(
      dish => dish.highlight
    ).length
)

const averageCost = computed(() => {
  if (!dishes.value.length) {
    return 0
  }

  const total = dishes.value.reduce(
    (sum, dish) =>
      sum + Number(dish.cost || 0),
    0
  )

  return total / dishes.value.length
})

const getMargin = (dish: Dish) => {
  const price = Number(dish.price || 0)
  const cost = Number(dish.cost || 0)

  if (!price) {
    return 0
  }

  return ((price - cost) / price) * 100
}

const formatCurrency = (
  value: number
) => {
  return new Intl.NumberFormat(
    'pt-BR',
    {
      style: 'currency',
      currency: 'BRL'
    }
  ).format(Number(value || 0))
}

const formatMargin = (
  dish: Dish
) => {
  return `${getMargin(dish).toFixed(1)}%`
}

const getMarginClass = (
  dish: Dish
) => {
  const margin = getMargin(dish)

  if (margin >= 60) {
    return 'text-emerald-400'
  }

  if (margin >= 30) {
    return 'text-amber-400'
  }

  return 'text-red-400'
}

const handleCategoryChange = async (
  categoryId: number | null
) => {
  selectedCategory.value =
    categoryId

  await fetchDishes()
}

const clearFilters = async () => {
  search.value = ''
  selectedCategory.value = null
  statusFilter.value = 'all'
  highlightFilter.value = 'all'

  await fetchDishes()
}

/*
   LOAD
*/

const loadPage = async () => {
  const start = Date.now()

  errorMessage.value = ''
  successMessage.value = ''

  try {
    authStore.initFromStorage()

    if (!authStore.isAuthenticated) {
      await router.push('/login')
      return
    }

    await Promise.all([
      fetchCategories(),
      fetchDishes()
    ])

  } catch (error) {
    handleApiError(error)
  } finally {
    const elapsed =
      Date.now() - start

    setTimeout(
      () => {
        isLoading.value = false
      },
      Math.max(
        0,
        700 - elapsed
      )
    )
  }
}

onMounted(loadPage)
</script>

<template>
  <div class="min-h-screen bg-zinc-950 text-zinc-100 flex flex-col font-sans">
    <AppLoader :visible="isLoading" />

    <!-- HEADER -->
    <header
      class="h-16 border-b border-zinc-800/80 bg-zinc-900/60 backdrop-blur-xl sticky top-0 z-30 px-4 sm:px-6 flex items-center justify-between">
      <div class="flex items-center gap-4">
        <button type="button" @click="toggleSidebar"
          class="p-2 rounded-xl text-zinc-300 hover:text-white hover:bg-zinc-800/80 transition" aria-label="Abrir menu">
          <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
          </svg>
        </button>

        <span class="font-bold text-lg text-white tracking-tight">
          ISM
        </span>
      </div>

      <button type="button" @click="handleLogout"
        class="px-4 py-2 text-xs font-semibold rounded-xl bg-zinc-800 hover:bg-zinc-700 text-zinc-200 hover:text-white border border-zinc-700/60 transition">
        Sair
      </button>
    </header>

    <AppSidebar :isOpen="isSidebarOpen" @close="isSidebarOpen = false" />

    <!-- MAIN -->
    <main class="flex-1 max-w-7xl w-full mx-auto px-4 sm:px-6 py-8">
      <!-- PAGE HEADER -->
      <section
        class="p-6 sm:p-8 rounded-2xl bg-gradient-to-r from-zinc-900 via-zinc-900/90 to-zinc-950 border border-zinc-800 shadow-xl mb-6">
        <div class="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-5">
          <div>
            <div class="flex items-center gap-3 mb-3">
              <span
                class="inline-flex items-center justify-center w-10 h-10 rounded-xl bg-zinc-800 border border-zinc-700">
                <svg class="w-5 h-5 text-zinc-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M12 8c-2.21 0-4 1.343-4 3s1.79 3 4 3 4-1.343 4-3-1.79-3-4-3Z" />
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M4 7c0-1.105 3.582-2 8-2s8 .895 8 2v10c0 1.105-3.582 2-8 2s-8-.895-8-2V7Z" />
                </svg>
              </span>

              <div>
                <h1 class="text-3xl sm:text-4xl font-bold text-white tracking-tight">
                  Cardápio
                </h1>

                <p class="text-zinc-400 text-sm mt-1">
                  Gerencie categorias, pratos,
                  custos e receitas.
                </p>
              </div>
            </div>
          </div>

          <div class="flex flex-col sm:flex-row gap-3">
            <button type="button" @click="openCreateCategoryModal"
              class="px-4 py-2.5 rounded-xl border border-zinc-700 bg-zinc-800 hover:bg-zinc-700 text-zinc-200 hover:text-white text-sm font-semibold transition">
              + Categoria
            </button>

            <button type="button" @click="openCreateDishModal"
              class="px-4 py-2.5 rounded-xl bg-white text-zinc-950 hover:bg-zinc-200 text-sm font-semibold transition">
              + Novo prato
            </button>
          </div>
        </div>
      </section>

      <!-- ALERTS -->
      <div v-if="errorMessage"
        class="mb-5 p-4 rounded-xl border border-red-900/60 bg-red-950/30 text-red-300 text-sm flex items-start justify-between gap-4">
        <span>{{ errorMessage }}</span>

        <button type="button" @click="errorMessage = ''" class="text-red-400 hover:text-red-200">
          ×
        </button>
      </div>

      <div v-if="successMessage"
        class="mb-5 p-4 rounded-xl border border-emerald-900/60 bg-emerald-950/30 text-emerald-300 text-sm flex items-start justify-between gap-4">
        <span>{{ successMessage }}</span>

        <button type="button" @click="successMessage = ''" class="text-emerald-400 hover:text-emerald-200">
          ×
        </button>
      </div>

      <!-- STATS -->
      <section class="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        <div class="rounded-2xl border border-zinc-800 bg-zinc-900/70 p-5">
          <p class="text-xs text-zinc-500 uppercase tracking-wider">
            Total de pratos
          </p>

          <p class="text-3xl font-bold text-white mt-2">
            {{ totalDishes }}
          </p>
        </div>

        <div class="rounded-2xl border border-zinc-800 bg-zinc-900/70 p-5">
          <p class="text-xs text-zinc-500 uppercase tracking-wider">
            Pratos ativos
          </p>

          <p class="text-3xl font-bold text-emerald-400 mt-2">
            {{ activeDishes }}
          </p>
        </div>

        <div class="rounded-2xl border border-zinc-800 bg-zinc-900/70 p-5">
          <p class="text-xs text-zinc-500 uppercase tracking-wider">
            Destaques
          </p>

          <p class="text-3xl font-bold text-amber-400 mt-2">
            {{ highlightedDishes }}
          </p>
        </div>

        <div class="rounded-2xl border border-zinc-800 bg-zinc-900/70 p-5">
          <p class="text-xs text-zinc-500 uppercase tracking-wider">
            Custo médio
          </p>

          <p class="text-2xl font-bold text-white mt-3">
            {{ formatCurrency(averageCost) }}
          </p>
        </div>
      </section>

      <!-- CATEGORIES -->
      <section class="rounded-2xl border border-zinc-800 bg-zinc-900/70 p-4 sm:p-5 mb-6">
        <div class="flex items-center justify-between gap-3 mb-4">
          <div>
            <h2 class="text-sm font-semibold text-white">
              Categorias
            </h2>

            <p class="text-xs text-zinc-500 mt-1">
              Filtre o cardápio por categoria.
            </p>
          </div>

          <button type="button" @click="openCreateCategoryModal"
            class="text-xs font-semibold text-zinc-300 hover:text-white">
            Gerenciar
          </button>
        </div>

        <div class="flex gap-2 overflow-x-auto pb-1">
          <button type="button" @click="handleCategoryChange(null)" :class="[
            'shrink-0 px-4 py-2 rounded-xl text-sm font-medium border transition',
            selectedCategory === null
              ? 'bg-white text-zinc-950 border-white'
              : 'bg-zinc-800 text-zinc-300 border-zinc-700 hover:bg-zinc-700'
          ]">
            Todos
          </button>

          <div v-for="category in categories" :key="category.id"
            class="shrink-0 flex items-center gap-1 rounded-xl border border-zinc-700 bg-zinc-800 overflow-hidden">
            <button type="button" @click="handleCategoryChange(category.id)" :class="[
              'px-4 py-2 text-sm font-medium transition',
              selectedCategory === category.id
                ? 'bg-white text-zinc-950'
                : 'text-zinc-300 hover:bg-zinc-700 hover:text-white'
            ]">
              {{ category.name }}
            </button>

            <button type="button" @click="openEditCategoryModal(category)" class="px-2 text-zinc-500 hover:text-white"
              title="Editar categoria">
              ✎
            </button>

            <button type="button" @click="deleteCategory(category)" :disabled="isDeletingCategory === category.id
              " class="px-2 pr-3 text-zinc-500 hover:text-red-400 disabled:opacity-50" title="Excluir categoria">
              {{
                isDeletingCategory === category.id
                  ? '...'
                  : '×'
              }}
            </button>
          </div>
        </div>
      </section>

      <!-- FILTERS -->
      <section class="rounded-2xl border border-zinc-800 bg-zinc-900/70 p-4 mb-6">
        <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-3">
          <!-- SEARCH -->
          <div class="relative md:col-span-2 xl:col-span-1">
            <svg class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-zinc-500" fill="none"
              stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                d="m21 21-4.35-4.35m2.1-5.4a7.5 7.5 0 1 1-15 0 7.5 7.5 0 0 1 15 0Z" />
            </svg>

            <input v-model="search" type="text" placeholder="Buscar prato..."
              class="w-full pl-10 pr-4 py-2.5 rounded-xl bg-zinc-950 border border-zinc-800 text-sm text-white placeholder:text-zinc-600 outline-none focus:border-zinc-600" />
          </div>

          <!-- STATUS -->
          <select v-model="statusFilter"
            class="px-4 py-2.5 rounded-xl bg-zinc-950 border border-zinc-800 text-sm text-zinc-300 outline-none focus:border-zinc-600">
            <option value="all">
              Todos os status
            </option>

            <option value="active">
              Ativos
            </option>

            <option value="inactive">
              Inativos
            </option>
          </select>

          <!-- HIGHLIGHT -->
          <select v-model="highlightFilter"
            class="px-4 py-2.5 rounded-xl bg-zinc-950 border border-zinc-800 text-sm text-zinc-300 outline-none focus:border-zinc-600">
            <option value="all">
              Todos os pratos
            </option>

            <option value="highlight">
              Apenas destaques
            </option>
          </select>

          <button type="button" @click="clearFilters"
            class="px-4 py-2.5 rounded-xl bg-zinc-800 border border-zinc-700 hover:bg-zinc-700 text-sm font-semibold text-zinc-300 hover:text-white transition">
            Limpar filtros
          </button>
        </div>
      </section>

      <!-- DISHES -->
      <section>
        <div class="flex items-center justify-between mb-4">
          <div>
            <h2 class="text-lg font-bold text-white">
              Pratos
            </h2>

            <p class="text-xs text-zinc-500 mt-1">
              {{ filteredDishes.length }}
              resultado(s)
            </p>
          </div>
        </div>

        <!-- EMPTY -->
        <div v-if="filteredDishes.length === 0"
          class="rounded-2xl border border-dashed border-zinc-800 bg-zinc-900/40 py-16 text-center">
          <div
            class="mx-auto w-14 h-14 rounded-2xl bg-zinc-900 border border-zinc-800 flex items-center justify-center mb-4">
            <svg class="w-7 h-7 text-zinc-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
                d="M12 8c-2.21 0-4 1.343-4 3s1.79 3 4 3 4-1.343 4-3-1.79-3-4-3Zm0 0V5m0 9v5m-7-8H2m20 0h-3" />
            </svg>
          </div>

          <h3 class="text-white font-semibold">
            Nenhum prato encontrado
          </h3>

          <p class="text-sm text-zinc-500 mt-1">
            Tente alterar os filtros ou
            cadastre um novo prato.
          </p>

          <button type="button" @click="openCreateDishModal"
            class="mt-5 px-4 py-2 rounded-xl bg-white text-zinc-950 text-sm font-semibold hover:bg-zinc-200 transition">
            Criar primeiro prato
          </button>
        </div>

        <!-- CARDS -->
        <div v-else class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-5">
          <article v-for="dish in filteredDishes" :key="dish.id"
            class="group overflow-hidden rounded-2xl border border-zinc-800 bg-zinc-900/80 hover:border-zinc-700 transition">
            <!-- IMAGE -->
            <div class="h-48 bg-zinc-950 relative overflow-hidden">
              <img v-if="dish.urlImage" :src="dish.urlImage" :alt="dish.name"
                class="w-full h-full object-cover group-hover:scale-105 transition duration-500" />

              <div v-else class="w-full h-full flex items-center justify-center">
                <svg class="w-12 h-12 text-zinc-800" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.3"
                    d="M12 8c-2.21 0-4 1.343-4 3s1.79 3 4 3 4-1.343 4-3-1.79-3-4-3Zm0 0V5m0 9v5m-7-8H2m20 0h-3" />
                </svg>
              </div>

              <!-- STATUS -->
              <div class="absolute top-3 left-3 flex gap-2">
                <span :class="[
                  'px-2.5 py-1 rounded-lg text-[10px] font-bold uppercase tracking-wider backdrop-blur-md border',
                  dish.isActive
                    ? 'bg-emerald-950/80 text-emerald-300 border-emerald-800/60'
                    : 'bg-red-950/80 text-red-300 border-red-800/60'
                ]">
                  {{
                    dish.isActive
                      ? 'Ativo'
                      : 'Inativo'
                  }}
                </span>

                <span v-if="dish.highlight"
                  class="px-2.5 py-1 rounded-lg text-[10px] font-bold uppercase tracking-wider bg-amber-950/80 text-amber-300 border border-amber-800/60 backdrop-blur-md">
                  Destaque
                </span>
              </div>
            </div>

            <!-- CONTENT -->
            <div class="p-5">
              <div class="flex items-start justify-between gap-3">
                <div class="min-w-0">
                  <p class="text-xs text-zinc-500 mb-1">
                    {{ getDishCategoryName(dish) }}
                  </p>

                  <h3 class="text-lg font-bold text-white truncate">
                    {{ dish.name }}
                  </h3>
                </div>

                <span class="shrink-0 text-lg font-bold text-white">
                  {{ formatCurrency(dish.price) }}
                </span>
              </div>

              <p v-if="dish.description" class="text-sm text-zinc-500 mt-3 line-clamp-2">
                {{ dish.description }}
              </p>

              <!-- COST -->
              <div class="grid grid-cols-2 gap-3 mt-5">
                <div class="rounded-xl bg-zinc-950 border border-zinc-800 p-3">
                  <p class="text-[10px] uppercase tracking-wider text-zinc-600">
                    Custo
                  </p>

                  <p class="text-sm font-semibold text-zinc-300 mt-1">
                    {{ formatCurrency(dish.cost) }}
                  </p>
                </div>

                <div class="rounded-xl bg-zinc-950 border border-zinc-800 p-3">
                  <p class="text-[10px] uppercase tracking-wider text-zinc-600">
                    Margem
                  </p>

                  <p :class="[
                    'text-sm font-semibold mt-1',
                    getMarginClass(dish)
                  ]">
                    {{ formatMargin(dish) }}
                  </p>
                </div>
              </div>

              <!-- ACTIONS -->
              <div class="flex items-center gap-2 mt-5 pt-4 border-t border-zinc-800">
                <button type="button" @click="openEditDishModal(dish)"
                  class="flex-1 px-3 py-2 rounded-xl bg-zinc-800 hover:bg-zinc-700 border border-zinc-700 text-xs font-semibold text-zinc-300 hover:text-white transition">
                  Editar
                </button>

                <button type="button" @click="deleteDish(dish)" :disabled="isDeletingDish === dish.id
                  "
                  class="px-3 py-2 rounded-xl bg-zinc-950 hover:bg-red-950/50 border border-zinc-800 hover:border-red-900 text-xs font-semibold text-zinc-500 hover:text-red-400 transition disabled:opacity-50">
                  {{
                    isDeletingDish === dish.id
                      ? '...'
                      : 'Excluir'
                  }}
                </button>
              </div>
            </div>
          </article>
        </div>
      </section>
    </main>

    <!--
         DISH MODAL
    -->
    <Teleport to="body">
      <div v-if="showDishModal" class="fixed inset-0 z-50 flex items-center justify-center p-4">
        <div class="absolute inset-0 bg-black/80 backdrop-blur-sm" @click="closeDishModal" />

        <div
          class="relative w-full max-w-3xl max-h-[90vh] overflow-y-auto rounded-2xl border border-zinc-800 bg-zinc-950 shadow-2xl">
          <!-- MODAL HEADER -->
          <div
            class="sticky top-0 z-10 flex items-center justify-between px-6 py-5 border-b border-zinc-800 bg-zinc-950/95 backdrop-blur-xl">
            <div>
              <h2 class="text-xl font-bold text-white">
                {{
                  isEditingDish
                    ? 'Editar prato'
                    : 'Novo prato'
                }}
              </h2>

              <p class="text-xs text-zinc-500 mt-1">
                Configure os dados do prato e
                sua receita.
              </p>
            </div>

            <button type="button" @click="closeDishModal"
              class="w-9 h-9 rounded-xl bg-zinc-900 border border-zinc-800 text-zinc-400 hover:text-white hover:bg-zinc-800">
              ×
            </button>
          </div>

          <!-- FORM -->
          <form @submit.prevent="submitDish" class="p-6 space-y-6">
            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <!-- NAME -->
              <div class="md:col-span-2">
                <label class="block text-xs font-semibold text-zinc-400 mb-2">
                  Nome do prato
                </label>

                <input v-model="dishForm.name" type="text" required placeholder="Ex.: Hambúrguer artesanal"
                  class="w-full px-4 py-3 rounded-xl bg-zinc-900 border border-zinc-800 text-sm text-white placeholder:text-zinc-600 outline-none focus:border-zinc-600" />
              </div>

              <!-- DESCRIPTION -->
              <div class="md:col-span-2">
                <label class="block text-xs font-semibold text-zinc-400 mb-2">
                  Descrição
                </label>

                <textarea v-model="dishForm.description" rows="3" placeholder="Descrição do prato..."
                  class="w-full px-4 py-3 rounded-xl bg-zinc-900 border border-zinc-800 text-sm text-white placeholder:text-zinc-600 outline-none focus:border-zinc-600 resize-none" />
              </div>

              <!-- CATEGORY -->
              <div>
                <label class="block text-xs font-semibold text-zinc-400 mb-2">
                  Categoria
                </label>

                <select v-model="dishForm.categoryId" required
                  class="w-full px-4 py-3 rounded-xl bg-zinc-900 border border-zinc-800 text-sm text-zinc-300 outline-none focus:border-zinc-600">
                  <option :value="null">
                    Selecione
                  </option>

                  <option v-for="category in categories" :key="category.id" :value="category.id">
                    {{ category.name }}
                  </option>
                </select>
              </div>

              <!-- IMAGE -->
              <div>
                <label class="block text-xs font-semibold text-zinc-400 mb-2">
                  URL da imagem
                </label>

                <input v-model="dishForm.urlImage" type="url" placeholder="https://..."
                  class="w-full px-4 py-3 rounded-xl bg-zinc-900 border border-zinc-800 text-sm text-white placeholder:text-zinc-600 outline-none focus:border-zinc-600" />
              </div>

              <!-- PRICE -->
              <div>
                <label class="block text-xs font-semibold text-zinc-400 mb-2">
                  Preço de venda
                </label>

                <div class="relative">
                  <span class="absolute left-4 top-1/2 -translate-y-1/2 text-zinc-600 text-sm">
                    R$
                  </span>

                  <input v-model.number="dishForm.price" type="number" min="0" step="0.01" required
                    class="w-full pl-10 pr-4 py-3 rounded-xl bg-zinc-900 border border-zinc-800 text-sm text-white outline-none focus:border-zinc-600" />
                </div>
              </div>

              <!-- COST -->
              <div>
                <label class="block text-xs font-semibold text-zinc-400 mb-2">
                  Custo da receita
                </label>

                <div class="relative">
                  <span class="absolute left-4 top-1/2 -translate-y-1/2 text-zinc-600 text-sm">
                    R$
                  </span>

                  <input v-model.number="dishForm.cost" type="number" min="0" step="0.01"
                    class="w-full pl-10 pr-4 py-3 rounded-xl bg-zinc-900 border border-zinc-800 text-sm text-white outline-none focus:border-zinc-600" />
                </div>
              </div>

              <!-- DISPLAY ORDER -->
              <div>
                <label class="block text-xs font-semibold text-zinc-400 mb-2">
                  Ordem de exibição
                </label>

                <input v-model.number="dishForm.displayOrder" type="number" min="0"
                  class="w-full px-4 py-3 rounded-xl bg-zinc-900 border border-zinc-800 text-sm text-white outline-none focus:border-zinc-600" />
              </div>

              <!-- TOGGLES -->
              <div class="md:col-span-2 grid grid-cols-1 sm:grid-cols-2 gap-3">
                <label
                  class="flex items-center justify-between gap-4 p-4 rounded-xl bg-zinc-900 border border-zinc-800 cursor-pointer">
                  <div>
                    <p class="text-sm font-semibold text-white">
                      Prato ativo
                    </p>

                    <p class="text-xs text-zinc-500 mt-1">
                      Disponível no cardápio.
                    </p>
                  </div>

                  <input v-model="dishForm.isActive" type="checkbox" class="w-5 h-5 accent-white" />
                </label>

                <label
                  class="flex items-center justify-between gap-4 p-4 rounded-xl bg-zinc-900 border border-zinc-800 cursor-pointer">
                  <div>
                    <p class="text-sm font-semibold text-white">
                      Prato destaque
                    </p>

                    <p class="text-xs text-zinc-500 mt-1">
                      Marcar como destaque.
                    </p>
                  </div>

                  <input v-model="dishForm.highlight" type="checkbox" class="w-5 h-5 accent-white" />
                </label>
              </div>
            </div>

            <!-- INGREDIENTS -->
            <div class="rounded-2xl border border-zinc-800 bg-zinc-900/50 p-5">
              <div class="flex items-center justify-between gap-3 mb-4">
                <div>
                  <h3 class="text-sm font-bold text-white">
                    Ingredientes
                  </h3>

                  <p class="text-xs text-zinc-500 mt-1">
                    Informe o Product ID e a
                    quantidade utilizada.
                  </p>
                </div>

                <span class="px-2.5 py-1 rounded-lg bg-zinc-800 border border-zinc-700 text-xs text-zinc-400">
                  {{ dishForm.ingredients.length }}
                </span>
              </div>

              <!-- ADD INGREDIENT -->
              <div class="grid grid-cols-1 sm:grid-cols-[1fr_1fr_auto] gap-2 mb-4">
                <input v-model.number="newIngredient.productId" type="number" min="1" placeholder="Product ID"
                  class="w-full px-3 py-2.5 rounded-xl bg-zinc-950 border border-zinc-800 text-sm text-white placeholder:text-zinc-600 outline-none focus:border-zinc-600" />

                <input v-model.number="newIngredient.quantity" type="number" min="0" step="0.001"
                  placeholder="Quantidade"
                  class="w-full px-3 py-2.5 rounded-xl bg-zinc-950 border border-zinc-800 text-sm text-white placeholder:text-zinc-600 outline-none focus:border-zinc-600" />

                <button type="button" @click="addIngredient"
                  class="px-4 py-2.5 rounded-xl bg-zinc-800 hover:bg-zinc-700 border border-zinc-700 text-sm font-semibold text-zinc-200">
                  Adicionar
                </button>
              </div>

              <!-- INGREDIENT LIST -->
              <div v-if="
                dishForm.ingredients.length
              " class="space-y-2">
                <div v-for="(
ingredient, index
                  ) in dishForm.ingredients" :key="`${ingredient.productId}-${index}`"
                  class="flex items-center justify-between gap-4 px-4 py-3 rounded-xl bg-zinc-950 border border-zinc-800">
                  <div>
                    <p class="text-sm font-semibold text-white">
                      Produto #{{
                        ingredient.productId
                      }}
                    </p>

                    <p class="text-xs text-zinc-500 mt-1">
                      Quantidade:
                      {{
                        ingredient.quantity
                      }}
                    </p>
                  </div>

                  <button type="button" @click="
                    removeIngredient(index)
                    " class="text-xs font-semibold text-zinc-500 hover:text-red-400">
                    Remover
                  </button>
                </div>
              </div>

              <div v-else
                class="py-6 text-center text-xs text-zinc-600 border border-dashed border-zinc-800 rounded-xl">
                Nenhum ingrediente adicionado.
              </div>
            </div>

            <!-- ACTIONS -->
            <div class="flex flex-col-reverse sm:flex-row sm:justify-end gap-3 pt-2">
              <button type="button" @click="closeDishModal"
                class="px-5 py-2.5 rounded-xl bg-zinc-900 hover:bg-zinc-800 border border-zinc-800 text-sm font-semibold text-zinc-300">
                Cancelar
              </button>

              <button type="submit" :disabled="isSavingDish"
                class="px-5 py-2.5 rounded-xl bg-white hover:bg-zinc-200 text-zinc-950 text-sm font-bold disabled:opacity-50 disabled:cursor-not-allowed">
                {{
                  isSavingDish
                    ? 'Salvando...'
                    : isEditingDish
                      ? 'Salvar alterações'
                      : 'Criar prato'
                }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </Teleport>

    <!--
         CATEGORY MODAL
    -->
    <Teleport to="body">
      <div v-if="showCategoryModal" class="fixed inset-0 z-50 flex items-center justify-center p-4">
        <div class="absolute inset-0 bg-black/80 backdrop-blur-sm" @click="closeCategoryModal" />

        <div class="relative w-full max-w-md rounded-2xl border border-zinc-800 bg-zinc-950 shadow-2xl">
          <div class="flex items-center justify-between px-6 py-5 border-b border-zinc-800">
            <div>
              <h2 class="text-xl font-bold text-white">
                {{
                  isEditingCategory
                    ? 'Editar categoria'
                    : 'Nova categoria'
                }}
              </h2>

              <p class="text-xs text-zinc-500 mt-1">
                Organize os pratos do seu
                cardápio.
              </p>
            </div>

            <button type="button" @click="closeCategoryModal"
              class="w-9 h-9 rounded-xl bg-zinc-900 border border-zinc-800 text-zinc-400 hover:text-white">
              ×
            </button>
          </div>

          <form @submit.prevent="submitCategory" class="p-6 space-y-5">
            <!-- NAME -->
            <div>
              <label class="block text-xs font-semibold text-zinc-400 mb-2">
                Nome
              </label>

              <input v-model="categoryForm.name" type="text" required placeholder="Ex.: Hambúrgueres"
                class="w-full px-4 py-3 rounded-xl bg-zinc-900 border border-zinc-800 text-sm text-white placeholder:text-zinc-600 outline-none focus:border-zinc-600" />
            </div>

            <!-- ORDER -->
            <div>
              <label class="block text-xs font-semibold text-zinc-400 mb-2">
                Ordem de exibição
              </label>

              <input v-model.number="categoryForm.displayOrder
                " type="number" min="0"
                class="w-full px-4 py-3 rounded-xl bg-zinc-900 border border-zinc-800 text-sm text-white outline-none focus:border-zinc-600" />
            </div>

            <!-- ACTIVE -->
            <label
              class="flex items-center justify-between gap-4 p-4 rounded-xl bg-zinc-900 border border-zinc-800 cursor-pointer">
              <div>
                <p class="text-sm font-semibold text-white">
                  Categoria ativa
                </p>

                <p class="text-xs text-zinc-500 mt-1">
                  Permitir utilização da
                  categoria.
                </p>
              </div>

              <input v-model="categoryForm.isActive
                " type="checkbox" class="w-5 h-5 accent-white" />
            </label>

            <!-- ACTIONS -->
            <div class="flex flex-col-reverse sm:flex-row sm:justify-end gap-3 pt-2">
              <button type="button" @click="closeCategoryModal"
                class="px-5 py-2.5 rounded-xl bg-zinc-900 hover:bg-zinc-800 border border-zinc-800 text-sm font-semibold text-zinc-300">
                Cancelar
              </button>

              <button type="submit" :disabled="isSavingCategory"
                class="px-5 py-2.5 rounded-xl bg-white hover:bg-zinc-200 text-zinc-950 text-sm font-bold disabled:opacity-50">
                {{
                  isSavingCategory
                    ? 'Salvando...'
                    : isEditingCategory
                      ? 'Salvar alterações'
                      : 'Criar categoria'
                }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </Teleport>
  </div>
</template>