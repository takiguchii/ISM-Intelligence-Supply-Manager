<script setup lang="ts">
import { ref, onMounted } from "vue";
import AppSidebar from "~/components/layout/AppSidebar.vue";
import AppLoader from "~/components/base/AppLoader.vue";
import { useAuthStore } from "~/stores/auth";

definePageMeta({ layout: false });

const config = useRuntimeConfig();
const authStore = useAuthStore();
const router = useRouter();

const isLoading = ref(true);
const isSidebarOpen = ref(false);
const isLoadingProducts = ref(false);

const products = ref<any[]>([]);
const errorMessage = ref("");

const showForm = ref(false);
const isSaving = ref(false);

const showDeleteModal = ref(false);
const productToDelete = ref<any>(null);
const isDeleting = ref(false);

const form = ref({
  name: "",
  unit: "",
  currentQuantity: 0,
  minimumQuantity: 0,
  averageCost: 0,
});

/*
   AUTENTICAÇÃO
*/

const handleLogout = () => {
  authStore.logout();
  router.push("/login");
};

/*
   PRODUTOS
*/

const loadProducts = async () => {
  try {
    isLoadingProducts.value = true;
    errorMessage.value = "";

    products.value = await $fetch<any[]>(
      `${config.public.apiBase}/api/stock/products`,
      {
        headers: {
          Authorization: `Bearer ${authStore.token}`,
        },
      }
    );
  } catch (error: any) {
    console.error(error);
    errorMessage.value = "Erro ao carregar produtos.";
  } finally {
    isLoadingProducts.value = false;
  }
};

const createProduct = async () => {
  try {
    isSaving.value = true;
    errorMessage.value = "";

    await $fetch(
      `${config.public.apiBase}/api/stock/products`,
      {
        method: "POST",
        headers: {
          Authorization: `Bearer ${authStore.token}`,
        },
        body: {
          restaurantId: 1,
          ...form.value,
        },
      }
    );

    showForm.value = false;

    form.value = {
      name: "",
      unit: "",
      currentQuantity: 0,
      minimumQuantity: 0,
      averageCost: 0,
    };

    await loadProducts();
  } catch (error: any) {
    console.error(error);
    errorMessage.value =
      error?.data?.detail || "Erro ao cadastrar produto.";
  } finally {
    isSaving.value = false;
  }
};

/*
   EXCLUSÃO
*/

const openDeleteModal = (product: any) => {
  productToDelete.value = product;
  showDeleteModal.value = true;
};

const closeDeleteModal = () => {
  if (isDeleting.value) return;

  showDeleteModal.value = false;
  productToDelete.value = null;
};

const deleteProduct = async () => {
  if (!productToDelete.value) return;

  try {
    isDeleting.value = true;

    await $fetch(
      `${config.public.apiBase}/api/stock/products/${productToDelete.value.id}`,
      {
        method: "DELETE",
        headers: {
          Authorization: `Bearer ${authStore.token}`,
        },
      }
    );

    showDeleteModal.value = false;
    productToDelete.value = null;

    await loadProducts();
  } catch (error: any) {
    console.error(error);
    errorMessage.value =
      error?.data?.detail || "Erro ao excluir produto.";
  } finally {
    isDeleting.value = false;
  }
};

const isLowStock = (product: any) =>
  product.currentQuantity <= product.minimumQuantity;

/*
   INIT
*/

onMounted(async () => {
  authStore.initFromStorage();

  if (!authStore.isAuthenticated) {
    router.push("/login");
    return;
  }

  await loadProducts();

  isLoading.value = false;
});
</script>

<template>
  <div class="min-h-screen bg-zinc-950 text-zinc-100 flex flex-col">

    <AppLoader :visible="isLoading" />

    <!-- HEADER -->

    <header class="h-16 border-b border-zinc-800 bg-zinc-900 px-4 sm:px-6 flex items-center justify-between">
      <div class="flex items-center gap-4">

        <button @click="isSidebarOpen = !isSidebarOpen" class="p-2 rounded-lg hover:bg-zinc-800">
          ☰
        </button>

        <div class="flex items-center gap-3">
          <span class="font-bold text-lg">
            ISM
          </span>

          <span class="hidden sm:block text-xs text-zinc-500 border-l border-zinc-700 pl-3">
            {{ config.public.appName }}
          </span>
        </div>

      </div>

      <div class="flex items-center gap-4">

        <span class="hidden md:block text-xs text-zinc-400">
          {{ authStore.currentUser?.name }}
          ({{ authStore.currentUser?.role }})
        </span>

        <button @click="handleLogout" class="px-4 py-2 rounded-lg bg-zinc-800 hover:bg-zinc-700 text-sm">
          Sair
        </button>

      </div>
    </header>

    <AppSidebar :isOpen="isSidebarOpen" @close="isSidebarOpen = false" />

    <!-- CONTEÚDO -->

    <main class="flex-1 max-w-7xl w-full mx-auto px-4 sm:px-6 py-8">

      <!-- TÍTULO -->

      <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-8">
        <div>
          <h1 class="text-3xl font-bold">
            Estoque
          </h1>

          <p class="text-zinc-400 text-sm mt-1">
            Gerencie os produtos e quantidades do estoque.
          </p>
        </div>

        <button @click="showForm = true"
          class="px-5 py-3 rounded-xl bg-emerald-500 hover:bg-emerald-400 text-zinc-950 font-semibold">
          + Novo produto
        </button>
      </div>

      <!-- ERRO -->

      <div v-if="errorMessage" class="mb-5 p-4 rounded-xl bg-red-950/50 border border-red-900 text-red-300">
        {{ errorMessage }}
      </div>

      <!-- TABELA -->

      <div class="overflow-x-auto rounded-2xl border border-zinc-800 bg-zinc-900">

        <div v-if="isLoadingProducts" class="p-10 text-center text-zinc-400">
          Carregando produtos...
        </div>

        <table v-else class="w-full text-sm">

          <thead class="border-b border-zinc-800">
            <tr class="text-left text-zinc-400">

              <th class="px-6 py-4">
                Produto
              </th>

              <th class="px-6 py-4">
                Unidade
              </th>

              <th class="px-6 py-4">
                Quantidade
              </th>

              <th class="px-6 py-4">
                Mínimo
              </th>

              <th class="px-6 py-4">
                Custo médio
              </th>

              <th class="px-6 py-4">
                Status
              </th>

              <th class="px-6 py-4">
                Ações
              </th>

            </tr>
          </thead>

          <tbody>

            <tr v-for="product in products" :key="product.id" class="border-b border-zinc-800 hover:bg-zinc-800/40">

              <td class="px-6 py-4 font-medium">
                {{ product.name }}
              </td>

              <td class="px-6 py-4 text-zinc-400">
                {{ product.unit }}
              </td>

              <td class="px-6 py-4">
                {{ product.currentQuantity }}
              </td>

              <td class="px-6 py-4 text-zinc-400">
                {{ product.minimumQuantity }}
              </td>

              <td class="px-6 py-4">
                R$ {{ Number(product.averageCost).toFixed(2) }}
              </td>

              <td class="px-6 py-4">

                <span class="px-2 py-1 rounded-full text-xs" :class="isLowStock(product)
                    ? 'bg-red-950 text-red-300'
                    : 'bg-emerald-950 text-emerald-300'
                  ">
                  {{ isLowStock(product) ? "Baixo" : "Normal" }}
                </span>

              </td>

              <td class="px-6 py-4">

                <button @click="openDeleteModal(product)" class="text-red-400 hover:text-red-300">
                  Excluir
                </button>

              </td>

            </tr>

            <tr v-if="products.length === 0">

              <td colspan="7" class="px-6 py-10 text-center text-zinc-500">
                Nenhum produto cadastrado.
              </td>

            </tr>

          </tbody>

        </table>

      </div>

    </main>

    <!-- MODAL CADASTRO -->

    <div v-if="showForm" class="fixed inset-0 z-50 flex items-center justify-center bg-black/70 px-4">

      <div class="w-full max-w-lg rounded-2xl bg-zinc-900 border border-zinc-800 p-6">

        <div class="flex justify-between mb-6">

          <h2 class="text-xl font-bold">
            Novo produto
          </h2>

          <button @click="showForm = false" class="text-zinc-400 hover:text-white">
            ✕
          </button>

        </div>

        <form @submit.prevent="createProduct" class="space-y-4">

          <input v-model="form.name" required placeholder="Nome" class="input" />

          <input v-model="form.unit" required placeholder="Unidade" class="input" />

          <input v-model.number="form.currentQuantity" type="number" min="0" required placeholder="Quantidade atual"
            class="input" />

          <input v-model.number="form.minimumQuantity" type="number" min="0" required placeholder="Quantidade mínima"
            class="input" />

          <input v-model.number="form.averageCost" type="number" min="0" step="0.01" required placeholder="Custo médio"
            class="input" />

          <div class="flex justify-end gap-3 pt-3">

            <button type="button" @click="showForm = false" class="px-4 py-2 rounded-lg bg-zinc-800">
              Cancelar
            </button>

            <button type="submit" :disabled="isSaving"
              class="px-5 py-2 rounded-lg bg-emerald-500 text-zinc-950 font-semibold disabled:opacity-50">
              {{ isSaving ? "Salvando..." : "Cadastrar" }}
            </button>

          </div>

        </form>

      </div>

    </div>

    <!-- MODAL EXCLUSÃO -->
     <Transition name="modal">
    <div v-if="showDeleteModal"
      class="fixed inset-0 z-[60] flex items-center justify-center bg-black/80 backdrop-blur-md px-4"
      @click.self="closeDeleteModal">
      <div class="w-full max-w-md rounded-2xl border border-zinc-700/80 bg-zinc-900 p-6 shadow-2xl shadow-black/50">
        <!-- ÍCONE + TÍTULO -->
        <div class="flex items-start gap-4">
          <div class="flex h-11 w-11 shrink-0 items-center justify-center rounded-full bg-red-500/10 text-red-400">
            <svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6" fill="none" viewBox="0 0 24 24"
              stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round"
                d="M12 9v4m0 4h.01M10.29 3.86l-8.07 14a1 1 0 00.87 1.5h17.82a1 1 0 00.87-1.5l-8.07-14a1 1 0 00-1.74 0z" />
            </svg>
          </div>

          <div class="flex-1">
            <h2 class="text-lg font-semibold text-white">
              Excluir produto?
            </h2>

            <p class="mt-2 text-sm leading-relaxed text-zinc-400">
              Tem certeza que deseja excluir
              <strong class="font-semibold text-white">
                {{ productToDelete?.name }}
              </strong>?
            </p>

            <p class="mt-1 text-xs text-zinc-500">
              Essa ação não poderá ser desfeita.
            </p>
          </div>

          <!-- FECHAR -->
          <button @click="closeDeleteModal" :disabled="isDeleting"
            class="text-zinc-500 transition hover:text-white disabled:opacity-50">
            <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" fill="none" viewBox="0 0 24 24"
              stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        <!-- BOTÕES -->
        <div class="mt-7 flex justify-end gap-3">
          <button @click="closeDeleteModal" :disabled="isDeleting"
            class="rounded-xl bg-zinc-800 px-4 py-2.5 text-sm font-medium text-zinc-300 transition hover:bg-zinc-700 hover:text-white disabled:cursor-not-allowed disabled:opacity-50">
            Cancelar
          </button>

          <button @click="deleteProduct" :disabled="isDeleting"
            class="flex items-center gap-2 rounded-xl bg-red-600 px-5 py-2.5 text-sm font-semibold text-white shadow-lg shadow-red-600/20 transition-all duration-200 hover:bg-red-500 hover:shadow-red-500/30 active:scale-95 disabled:cursor-not-allowed disabled:opacity-50">
            <!-- ÍCONE LIXEIRA -->
            <svg v-if="!isDeleting" xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" fill="none" viewBox="0 0 24 24"
              stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round"
                d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6M9 7V4a1 1 0 011-1h4a1 1 0 011 1v3m-9 0h14" />
            </svg>

            <!-- LOADING -->
            <svg v-else class="h-4 w-4 animate-spin" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />

              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z" />
            </svg>

            {{ isDeleting ? "Excluindo..." : "Excluir produto" }}
          </button>
        </div>
      </div>
    </div>
    </Transition>

    <!-- FOOTER -->

    <footer class="border-t border-zinc-800 bg-zinc-950 py-5 text-center text-xs text-zinc-500">
      © 2026 ISM — Intelligence Supply Manager.
      Todos os direitos reservados.
    </footer>

  </div>
</template>

<style scoped>
.input {
  @apply w-full px-4 py-3 rounded-xl bg-zinc-950 border border-zinc-800 text-white outline-none focus:border-emerald-500;
}

.modal-enter-active,
.modal-leave-active {
  transition: opacity 0.2s ease;
}

.modal-enter-active > div,
.modal-leave-active > div {
  transition:
    opacity 0.25s ease,
    transform 0.25s cubic-bezier(0.16, 1, 0.3, 1);
}

.modal-enter-from,
.modal-leave-to {
  opacity: 0;
}

.modal-enter-from > div {
  opacity: 0;
  transform: scale(0.92) translateY(15px);
}

.modal-leave-to > div {
  opacity: 0;
  transform: scale(0.96) translateY(5px);
}
</style>