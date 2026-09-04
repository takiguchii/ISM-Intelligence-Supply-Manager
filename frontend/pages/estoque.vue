<script setup lang="ts">
import { ref, onMounted } from "vue";
import AppSidebar from "~/components/layout/AppSidebar.vue";
import AppLoader from "~/components/base/AppLoader.vue";
import { useAuthStore } from "~/stores/auth";
import { useThemeStore } from "~/stores/theme";

definePageMeta({ layout: false });

const config = useRuntimeConfig();
const authStore = useAuthStore();
const themeStore = useThemeStore();
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
  <div
    :class="[
      'min-h-screen flex flex-col font-sans transition-colors duration-200',
      themeStore.isDark
        ? 'bg-zinc-950 text-zinc-100 selection:bg-zinc-800 selection:text-white'
        : 'bg-zinc-50 text-zinc-900 selection:bg-indigo-100 selection:text-indigo-900'
    ]"
  >

    <AppLoader :visible="isLoading" />

    <!-- HEADER -->

    <header
      :class="[
        'h-16 border-b px-4 sm:px-6 flex items-center justify-between',
        themeStore.isDark
          ? 'border-zinc-800 bg-zinc-900'
          : 'border-zinc-200 bg-white shadow-sm'
      ]"
    >
      <div class="flex items-center gap-4">

        <button
          @click="isSidebarOpen = !isSidebarOpen"
          :class="[
            'p-2 rounded-lg transition-colors',
            themeStore.isDark ? 'hover:bg-zinc-800 text-zinc-100' : 'hover:bg-zinc-100 text-zinc-900'
          ]"
        >
          ☰
        </button>

        <div class="flex items-center gap-3">
          <span
            :class="[
              'font-bold text-lg',
              themeStore.isDark ? 'text-white' : 'text-zinc-900'
            ]"
          >
            ISM
          </span>

          <span
            :class="[
              'hidden sm:block text-xs border-l pl-3',
              themeStore.isDark
                ? 'text-zinc-500 border-zinc-700'
                : 'text-zinc-500 border-zinc-200'
            ]"
          >
            {{ config.public.appName }}
          </span>
        </div>

      </div>

      <div class="flex items-center gap-4">

        <span
          :class="[
            'hidden md:block text-xs',
            themeStore.isDark ? 'text-zinc-400' : 'text-zinc-600'
          ]"
        >
          {{ authStore.currentUser?.name }}
          ({{ authStore.currentUser?.role }})
        </span>

        <button
          @click="handleLogout"
          :class="[
            'px-4 py-2 rounded-lg text-sm transition-colors',
            themeStore.isDark
              ? 'bg-zinc-800 hover:bg-zinc-700 text-zinc-100'
              : 'bg-zinc-100 hover:bg-zinc-200 text-zinc-900 border border-zinc-200'
          ]"
        >
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
          <h1
            :class="[
              'text-3xl font-bold',
              themeStore.isDark ? 'text-white' : 'text-zinc-900'
            ]"
          >
            Estoque
          </h1>

          <p
            :class="[
              'text-sm mt-1',
              themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500'
            ]"
          >
            Gerencie os produtos e quantidades do estoque.
          </p>
        </div>

        <button @click="showForm = true"
          class="px-5 py-3 rounded-xl bg-emerald-500 hover:bg-emerald-400 text-zinc-950 font-semibold">
          + Novo produto
        </button>
      </div>

      <!-- ERRO -->

      <div v-if="errorMessage" class="mb-5 p-4 rounded-xl bg-red-500/10 border border-red-500/30 text-red-500 dark:text-red-300">
        {{ errorMessage }}
      </div>

      <!-- TABELA -->

      <div
        :class="[
          'overflow-x-auto rounded-2xl border',
          themeStore.isDark
            ? 'border-zinc-800 bg-zinc-900'
            : 'border-zinc-200 bg-white shadow-sm'
        ]"
      >

        <div
          v-if="isLoadingProducts"
          :class="[
            'p-10 text-center',
            themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500'
          ]"
        >
          Carregando produtos...
        </div>

        <table v-else class="w-full text-sm">

          <thead
            :class="[
              'border-b',
              themeStore.isDark ? 'border-zinc-800' : 'border-zinc-200'
            ]"
          >
            <tr
              :class="[
                'text-left',
                themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500'
              ]"
            >

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

            <tr
              v-for="product in products"
              :key="product.id"
              :class="[
                'border-b transition-colors',
                themeStore.isDark
                  ? 'border-zinc-800 hover:bg-zinc-800/40'
                  : 'border-zinc-200 hover:bg-zinc-50'
              ]"
            >

              <td
                class="px-6 py-4 font-medium"
                :class="themeStore.isDark ? 'text-zinc-100' : 'text-zinc-900'"
              >
                {{ product.name }}
              </td>

              <td
                class="px-6 py-4"
                :class="themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500'"
              >
                {{ product.unit }}
              </td>

              <td
                class="px-6 py-4"
                :class="themeStore.isDark ? 'text-zinc-100' : 'text-zinc-900'"
              >
                {{ product.currentQuantity }}
              </td>

              <td
                class="px-6 py-4"
                :class="themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500'"
              >
                {{ product.minimumQuantity }}
              </td>

              <td
                class="px-6 py-4"
                :class="themeStore.isDark ? 'text-zinc-100' : 'text-zinc-900'"
              >
                R$ {{ Number(product.averageCost).toFixed(2) }}
              </td>

              <td class="px-6 py-4">

                <span class="px-2 py-1 rounded-full text-xs" :class="isLowStock(product)
                    ? 'dark:bg-red-950 dark:text-red-300 bg-red-500/10 text-red-600'
                    : 'dark:bg-emerald-950 dark:text-emerald-300 bg-emerald-500/10 text-emerald-700'
                  ">
                  {{ isLowStock(product) ? "Baixo" : "Normal" }}
                </span>

              </td>

              <td class="px-6 py-4">

                <button
                  @click="openDeleteModal(product)"
                  class="dark:text-red-400 dark:hover:text-red-300 text-red-600 hover:text-red-700"
                >
                  Excluir
                </button>

              </td>

            </tr>

            <tr v-if="products.length === 0">

              <td
                colspan="7"
                :class="[
                  'px-6 py-10 text-center',
                  themeStore.isDark ? 'text-zinc-500' : 'text-zinc-400'
                ]"
              >
                Nenhum produto cadastrado.
              </td>

            </tr>

          </tbody>

        </table>

      </div>

    </main>

    <!-- MODAL CADASTRO -->

    <div v-if="showForm" class="fixed inset-0 z-50 flex items-center justify-center bg-black/70 px-4">

      <div
        :class="[
          'w-full max-w-lg rounded-2xl border p-6',
          themeStore.isDark
            ? 'bg-zinc-900 border-zinc-800'
            : 'bg-white border-zinc-200 shadow-xl'
        ]"
      >

        <div class="flex justify-between mb-6">

          <h2
            :class="[
              'text-xl font-bold',
              themeStore.isDark ? 'text-white' : 'text-zinc-900'
            ]"
          >
            Novo produto
          </h2>

          <button
            @click="showForm = false"
            :class="[
              'transition-colors',
              themeStore.isDark
                ? 'text-zinc-400 hover:text-white'
                : 'text-zinc-500 hover:text-zinc-900'
            ]"
          >
            ✕
          </button>

        </div>

        <form @submit.prevent="createProduct" class="space-y-4">

          <input v-model="form.name" required placeholder="Nome"
            :class="[
              'w-full px-4 py-3 rounded-xl outline-none transition-colors',
              themeStore.isDark
                ? 'bg-zinc-950 border border-zinc-800 text-white placeholder:text-zinc-500 focus:border-emerald-500'
                : 'bg-zinc-50 border border-zinc-200 text-zinc-900 placeholder:text-zinc-400 focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500'
            ]"
          />

          <input v-model="form.unit" required placeholder="Unidade"
            :class="[
              'w-full px-4 py-3 rounded-xl outline-none transition-colors',
              themeStore.isDark
                ? 'bg-zinc-950 border border-zinc-800 text-white placeholder:text-zinc-500 focus:border-emerald-500'
                : 'bg-zinc-50 border border-zinc-200 text-zinc-900 placeholder:text-zinc-400 focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500'
            ]"
          />

          <input v-model.number="form.currentQuantity" type="number" min="0" required placeholder="Quantidade atual"
            :class="[
              'w-full px-4 py-3 rounded-xl outline-none transition-colors',
              themeStore.isDark
                ? 'bg-zinc-950 border border-zinc-800 text-white placeholder:text-zinc-500 focus:border-emerald-500'
                : 'bg-zinc-50 border border-zinc-200 text-zinc-900 placeholder:text-zinc-400 focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500'
            ]"
          />

          <input v-model.number="form.minimumQuantity" type="number" min="0" required placeholder="Quantidade mínima"
            :class="[
              'w-full px-4 py-3 rounded-xl outline-none transition-colors',
              themeStore.isDark
                ? 'bg-zinc-950 border border-zinc-800 text-white placeholder:text-zinc-500 focus:border-emerald-500'
                : 'bg-zinc-50 border border-zinc-200 text-zinc-900 placeholder:text-zinc-400 focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500'
            ]"
          />

          <input v-model.number="form.averageCost" type="number" min="0" step="0.01" required placeholder="Custo médio"
            :class="[
              'w-full px-4 py-3 rounded-xl outline-none transition-colors',
              themeStore.isDark
                ? 'bg-zinc-950 border border-zinc-800 text-white placeholder:text-zinc-500 focus:border-emerald-500'
                : 'bg-zinc-50 border border-zinc-200 text-zinc-900 placeholder:text-zinc-400 focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500'
            ]"
          />

          <div class="flex justify-end gap-3 pt-3">

            <button
              type="button"
              @click="showForm = false"
              :class="[
                'px-4 py-2 rounded-lg transition-colors',
                themeStore.isDark
                  ? 'bg-zinc-800 text-zinc-100 hover:bg-zinc-700'
                  : 'bg-zinc-100 text-zinc-700 hover:bg-zinc-200 border border-zinc-200'
              ]"
            >
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
      <div
        :class="[
          'w-full max-w-md rounded-2xl border p-6 shadow-2xl',
          themeStore.isDark
            ? 'border-zinc-700/80 bg-zinc-900 shadow-black/50'
            : 'border-zinc-200 bg-white shadow-zinc-900/10'
        ]"
      >
        <!-- ÍCONE + TÍTULO -->
        <div class="flex items-start gap-4">
          <div class="flex h-11 w-11 shrink-0 items-center justify-center rounded-full bg-red-500/10 text-red-500 dark:text-red-400">
            <svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6" fill="none" viewBox="0 0 24 24"
              stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round"
                d="M12 9v4m0 4h.01M10.29 3.86l-8.07 14a1 1 0 00.87 1.5h17.82a1 1 0 00.87-1.5l-8.07-14a1 1 0 00-1.74 0z" />
            </svg>
          </div>

          <div class="flex-1">
            <h2
              :class="[
                'text-lg font-semibold',
                themeStore.isDark ? 'text-white' : 'text-zinc-900'
              ]"
            >
              Excluir produto?
            </h2>

            <p
              :class="[
                'mt-2 text-sm leading-relaxed',
                themeStore.isDark ? 'text-zinc-400' : 'text-zinc-600'
              ]"
            >
              Tem certeza que deseja excluir
              <strong :class="themeStore.isDark ? 'font-semibold text-white' : 'font-semibold text-zinc-900'">
                {{ productToDelete?.name }}
              </strong>?
            </p>

            <p
              :class="[
                'mt-1 text-xs',
                themeStore.isDark ? 'text-zinc-500' : 'text-zinc-500'
              ]"
            >
              Essa ação não poderá ser desfeita.
            </p>
          </div>

          <!-- FECHAR -->
          <button @click="closeDeleteModal" :disabled="isDeleting"
            :class="[
              'transition disabled:opacity-50',
              themeStore.isDark
                ? 'text-zinc-500 hover:text-white'
                : 'text-zinc-400 hover:text-zinc-700'
            ]"
          >
            <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" fill="none" viewBox="0 0 24 24"
              stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        <!-- BOTÕES -->
        <div class="mt-7 flex justify-end gap-3">
          <button @click="closeDeleteModal" :disabled="isDeleting"
            :class="[
              'rounded-xl px-4 py-2.5 text-sm font-medium transition disabled:cursor-not-allowed disabled:opacity-50',
              themeStore.isDark
                ? 'bg-zinc-800 text-zinc-300 hover:bg-zinc-700 hover:text-white'
                : 'bg-zinc-100 text-zinc-700 hover:bg-zinc-200 border border-zinc-200'
            ]"
          >
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

    <footer
      :class="[
        'border-t py-5 text-center text-xs',
        themeStore.isDark
          ? 'border-zinc-800 bg-zinc-950 text-zinc-500'
          : 'border-zinc-200 bg-white text-zinc-500'
      ]"
    >
      © 2026 ISM — Intelligence Supply Manager.
      Todos os direitos reservados.
    </footer>

  </div>
</template>

<style scoped>

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