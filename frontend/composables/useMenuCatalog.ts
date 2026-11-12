import { computed, onMounted, onUnmounted, ref } from 'vue'
import { menuService, type CategoryDto, type DishDto } from '~/services/modules/menu/menuService'

export function useMenuCatalog() {
  const dishes = ref<DishDto[]>([])
  const categories = ref<CategoryDto[]>([])
  const loading = ref(false)
  const error = ref('')
  let disposed = false
  const categoryNames = computed(() => new Map(categories.value.map(category => [category.id, category.name])))
  async function refresh() {
    if (loading.value || disposed) return
    loading.value = true
    error.value = ''
    try {
      const [menu, groups] = await Promise.all([menuService.getDishes(), menuService.getCategories()])
      if (!disposed) { dishes.value = menu; categories.value = groups }
    } catch (cause) {
      if (!disposed) error.value = (cause as { data?: { detail?: string } })?.data?.detail || 'Não foi possível carregar o cardápio. Tente novamente.'
    } finally { loading.value = false }
  }
  onMounted(() => { void refresh() })
  onUnmounted(() => { disposed = true })
  return { dishes, categories, categoryNames, loading, error, refresh }
}
