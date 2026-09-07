<script setup lang="ts">
import { computed, onMounted, ref, watch } from "vue";
import AppLoader from "~/components/base/AppLoader.vue";
import { useAuthStore } from "~/stores/auth";
import {
  employeeService,
  EMPLOYEE_ROLES,
  type EmployeeResponse
} from "~/services/modules/employees/employeeService";


const PAGE_SIZE = 10;

const ROLE_LABELS: Record<string, string> = {
  Admin: "Administrador",
  Manager: "Gerente",
  Chef: "Chef",
  Waiter: "Garçom"
};

const authStore = useAuthStore();
const router = useRouter();

const isPageLoading = ref(true);
const isSidebarOpen = ref(false);

const employees = ref<EmployeeResponse[]>([]);
const isLoadingList = ref(false);
const listError = ref("");
const successMessage = ref("");

const search = ref("");
const page = ref(1);

const showFormModal = ref(false);
const editingEmployee = ref<EmployeeResponse | null>(null);
const isSaving = ref(false);
const formError = ref("");
const form = ref({
  name: "",
  email: "",
  password: "",
  role: "Waiter",
  isActive: true
});

const employeeToDelete = ref<EmployeeResponse | null>(null);
const isDeleting = ref(false);
const deleteError = ref("");

/* -------------------------------------------------------------- */
/* Contexto do usuário logado                                      */
/* -------------------------------------------------------------- */

const restaurantId = computed<number | null>(
  () => authStore.currentUser?.restaurantId ?? null
);

/** Backend: criar/editar/excluir exige a policy RestaurantManagerOrAbove (Admin ou Manager). */
const canManage = computed(() => {
  const role = authStore.currentUser?.role ?? "";
  return role === "Admin" || role === "Manager";
});

/* -------------------------------------------------------------- */
/* Busca e paginação (client-side: a rota GET /api/users devolve   */
/* a lista completa, sem parâmetros de busca ou paginação)         */
/* -------------------------------------------------------------- */

const filteredEmployees = computed(() => {
  const query = search.value.trim().toLowerCase();
  if (!query) return employees.value;

  return employees.value.filter((employee) =>
    [employee.name, employee.email, roleLabel(employee.role)]
      .join(" ")
      .toLowerCase()
      .includes(query)
  );
});

const totalPages = computed(() =>
  Math.max(1, Math.ceil(filteredEmployees.value.length / PAGE_SIZE))
);

const pagedEmployees = computed(() => {
  const start = (page.value - 1) * PAGE_SIZE;
  return filteredEmployees.value.slice(start, start + PAGE_SIZE);
});

const rangeStart = computed(() =>
  filteredEmployees.value.length === 0 ? 0 : (page.value - 1) * PAGE_SIZE + 1
);

const rangeEnd = computed(() =>
  Math.min(page.value * PAGE_SIZE, filteredEmployees.value.length)
);

watch(search, () => {
  page.value = 1;
});

// Mantém a página válida após excluir um funcionário ou filtrar a lista.
watch(totalPages, (total) => {
  if (page.value > total) page.value = total;
});

const goToPage = (target: number) => {
  page.value = Math.min(Math.max(1, target), totalPages.value);
};

/* -------------------------------------------------------------- */
/* Helpers                                                         */
/* -------------------------------------------------------------- */

function roleLabel(role: string): string {
  return ROLE_LABELS[role] ?? role;
}

const formatDate = (value: string | null | undefined): string => {
  if (!value) return "—";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "—";
  return date.toLocaleDateString("pt-BR");
};

/** Checagem simples de formato (a validação completa é feita pelo backend). */
const isValidEmail = (value: string): boolean => {
  const at = value.indexOf("@");
  if (at <= 0 || at !== value.lastIndexOf("@")) return false;

  const domain = value.slice(at + 1);
  const dot = domain.indexOf(".");
  return dot > 0 && dot < domain.length - 1 && !/\s/.test(value);
};

/** Cobre os três formatos de erro da API: { message }, { detail } e ModelState, além de falhas de conexão de rede. */
const extractApiError = (error: any, fallback: string): string => {
  const data = error?.data;
  if (data?.message) return data.message;
  if (data?.detail) return data.detail;

  const validationErrors = data?.errors;
  if (validationErrors && typeof validationErrors === "object") {
    const firstMessage = Object.values(validationErrors).flat()[0];
    if (typeof firstMessage === "string") return firstMessage;
  }

  const message = error?.message || "";
  if (
    typeof message === "string" &&
    (message.toLowerCase().includes("failed to fetch") ||
      message.toLowerCase().includes("fetch failed") ||
      message.toLowerCase().includes("network error") ||
      message.toLowerCase().includes("econnrefused"))
  ) {
    return "Não foi possível conectar ao servidor. Verifique se o backend está em execução.";
  }

  return error?.message || fallback;
};

let successTimeout: ReturnType<typeof setTimeout> | undefined;

const notifySuccess = (message: string) => {
  successMessage.value = message;
  clearTimeout(successTimeout);
  successTimeout = setTimeout(() => (successMessage.value = ""), 4000);
};

/* -------------------------------------------------------------- */
/* Listagem                                                        */
/* -------------------------------------------------------------- */

const loadEmployees = async () => {
  isLoadingList.value = true;
  listError.value = "";

  try {
    employees.value = await employeeService.getByRestaurant(restaurantId.value);
  } catch (error: any) {
    employees.value = [];
    listError.value = extractApiError(error, "Erro ao carregar os funcionários.");
  } finally {
    isLoadingList.value = false;
  }
};

/* -------------------------------------------------------------- */
/* Criação e edição                                                */
/* -------------------------------------------------------------- */

const openCreateModal = () => {
  editingEmployee.value = null;
  form.value = {
    name: "",
    email: "",
    password: "",
    role: "Waiter",
    isActive: true
  };
  formError.value = "";
  showFormModal.value = true;
};

const openEditModal = (employee: EmployeeResponse) => {
  editingEmployee.value = employee;
  form.value = {
    name: employee.name,
    email: employee.email,
    password: "",
    role: employee.role,
    isActive: employee.isActive
  };
  formError.value = "";
  showFormModal.value = true;
};

const closeFormModal = () => {
  if (isSaving.value) return;
  showFormModal.value = false;
};

const validateForm = (): string | null => {
  const isEditing = !!editingEmployee.value;
  const name = form.value.name.trim();
  const email = form.value.email.trim();
  const maxNameLength = isEditing ? 255 : 150;

  if (!name) return "Informe o nome do funcionário.";
  if (name.length > maxNameLength)
    return `O nome deve ter no máximo ${maxNameLength} caracteres.`;
  if (!email) return "Informe o e-mail do funcionário.";
  if (!isValidEmail(email)) return "Informe um e-mail válido.";
  if (email.length > 255) return "O e-mail deve ter no máximo 255 caracteres.";
  if (!isEditing && form.value.password.length < 6)
    return "A senha deve ter pelo menos 6 caracteres.";
  if (!(EMPLOYEE_ROLES as readonly string[]).includes(form.value.role))
    return "Selecione um cargo válido.";

  return null;
};

const saveEmployee = async () => {
  const validationError = validateForm();
  if (validationError) {
    formError.value = validationError;
    return;
  }

  isSaving.value = true;
  formError.value = "";

  try {
    const employee = editingEmployee.value;

    if (employee) {
      await employeeService.update(employee.id, {
        name: form.value.name.trim(),
        email: form.value.email.trim(),
        role: form.value.role,
        // O backend rejeita mover um usuário de restaurante, então mantemos o vínculo atual.
        restaurantId: employee.restaurantId ?? restaurantId.value,
        isActive: form.value.isActive
      });
      notifySuccess("Funcionário atualizado com sucesso.");
    } else {
      await employeeService.create({
        name: form.value.name.trim(),
        email: form.value.email.trim(),
        password: form.value.password,
        role: form.value.role,
        restaurantId: restaurantId.value
      });
      notifySuccess("Funcionário cadastrado com sucesso.");
    }

    showFormModal.value = false;
    await loadEmployees();
  } catch (error: any) {
    formError.value = extractApiError(
      error,
      editingEmployee.value
        ? "Erro ao atualizar o funcionário."
        : "Erro ao cadastrar o funcionário."
    );
  } finally {
    isSaving.value = false;
  }
};

/* -------------------------------------------------------------- */
/* Exclusão                                                        */
/* -------------------------------------------------------------- */

const openDeleteModal = (employee: EmployeeResponse) => {
  employeeToDelete.value = employee;
  deleteError.value = "";
};

const closeDeleteModal = () => {
  if (isDeleting.value) return;
  employeeToDelete.value = null;
};

const deleteEmployee = async () => {
  const employee = employeeToDelete.value;
  if (!employee) return;

  isDeleting.value = true;
  deleteError.value = "";

  try {
    await employeeService.delete(employee.id);
    employeeToDelete.value = null;
    notifySuccess("Funcionário excluído com sucesso.");
    await loadEmployees();
  } catch (error: any) {
    deleteError.value = extractApiError(error, "Erro ao excluir o funcionário.");
  } finally {
    isDeleting.value = false;
  }
};

/* -------------------------------------------------------------- */

const handleLogout = () => {
  authStore.logout();
  router.push("/login");
};

onMounted(async () => {
  authStore.initFromStorage();

  if (!authStore.isAuthenticated) {
    router.push("/login");
    return;
  }

  await loadEmployees();
  isPageLoading.value = false;
});
</script>

<template>
  <div class="max-w-7xl w-full mx-auto px-4 sm:px-6 py-8 space-y-6">
    <AppLoader :visible="isPageLoading" />

    <!-- Feedback -->
    <div
      v-if="successMessage"
      class="px-4 py-3 rounded-xl bg-emerald-500/10 border border-emerald-500/30 text-emerald-300 text-sm flex items-center justify-between gap-4"
    >
      <span>{{ successMessage }}</span>
      <button type="button" @click="successMessage = ''" class="text-emerald-400 hover:text-emerald-200 font-bold">
        ✕
      </button>
    </div>

    <!-- Cabeçalho -->
    <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
      <div>
        <div class="inline-flex items-center gap-2 px-3 py-1 rounded-full border border-zinc-800 bg-zinc-900/80 text-[11px] font-bold uppercase tracking-widest text-zinc-400 mb-2">
          Equipe
        </div>
        <h1 class="text-2xl sm:text-3xl md:text-4xl font-extrabold text-white tracking-tight">Funcionários</h1>
        <p class="text-sm text-zinc-400 mt-1">
          Equipe vinculada ao restaurante, com cargos e acesso ao sistema.
        </p>
      </div>

      <button
        v-if="canManage"
        type="button"
        @click="openCreateModal"
        class="shrink-0 px-4 py-2.5 rounded-xl bg-white text-zinc-950 hover:bg-zinc-200 text-sm font-semibold transition self-start sm:self-auto shadow-lg shadow-white/5 flex items-center gap-2 active:scale-95"
      >
        <span class="text-base leading-none font-bold">+</span>
        Adicionar funcionário
      </button>
    </div>

      <!-- Busca -->
      <div class="flex flex-col sm:flex-row sm:items-center gap-3 mb-4">
        <div class="relative flex-1 max-w-md">
          <svg
            class="w-4 h-4 text-zinc-500 absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
          <label for="employee-search" class="sr-only">Buscar funcionários</label>
          <input
            id="employee-search"
            v-model="search"
            type="search"
            placeholder="Buscar por nome, e-mail ou cargo"
            class="w-full pl-9 pr-3 py-2.5 bg-zinc-900/60 border border-zinc-800 rounded-lg text-sm text-zinc-100 placeholder-zinc-500 focus:outline-none focus:border-zinc-600 transition"
          />
        </div>

        <span class="text-xs text-zinc-500 sm:ml-auto">
          {{ filteredEmployees.length }}
          {{ filteredEmployees.length === 1 ? "funcionário" : "funcionários" }}
        </span>
      </div>

      <!-- Lista -->
      <div class="rounded-lg border border-zinc-800 bg-zinc-900/40 overflow-hidden">
        <!-- Erro ao carregar -->
        <div v-if="listError" class="px-4 py-10 text-center">
          <p class="text-sm text-red-300">{{ listError }}</p>
          <button
            type="button"
            @click="loadEmployees"
            class="mt-4 px-4 py-2 rounded-lg border border-zinc-700 text-xs font-semibold text-zinc-200 hover:bg-zinc-800 transition"
          >
            Tentar novamente
          </button>
        </div>

        <!-- Loading inicial (nas recargas a tabela permanece visível) -->
        <div
          v-else-if="isLoadingList && employees.length === 0"
          class="px-4 py-12 text-center text-sm text-zinc-500"
        >
          Carregando funcionários...
        </div>

        <!-- Sem resultados na busca -->
        <div v-else-if="filteredEmployees.length === 0 && search.trim()" class="px-4 py-12 text-center">
          <p class="text-sm text-zinc-400">Nenhum funcionário encontrado para "{{ search.trim() }}".</p>
          <button
            type="button"
            @click="search = ''"
            class="mt-3 text-xs font-semibold text-zinc-300 hover:text-white underline underline-offset-4"
          >
            Limpar busca
          </button>
        </div>

        <!-- Lista vazia -->
        <div v-else-if="filteredEmployees.length === 0" class="px-4 py-12 text-center">
          <p class="text-sm text-zinc-400">Nenhum funcionário cadastrado.</p>
          <button
            v-if="canManage"
            type="button"
            @click="openCreateModal"
            class="mt-3 text-xs font-semibold text-zinc-300 hover:text-white underline underline-offset-4"
          >
            Adicionar o primeiro funcionário
          </button>
        </div>

        <template v-else>
          <div class="overflow-x-auto">
            <table class="w-full min-w-[720px] text-sm">
              <thead>
                <tr class="text-left text-[11px] uppercase tracking-wider text-zinc-500 border-b border-zinc-800">
                  <th class="px-4 py-3 font-semibold">Nome</th>
                  <th class="px-4 py-3 font-semibold">E-mail</th>
                  <th class="px-4 py-3 font-semibold">Cargo</th>
                  <th class="px-4 py-3 font-semibold">Status</th>
                  <th class="px-4 py-3 font-semibold">Cadastro</th>
                  <th v-if="canManage" class="px-4 py-3 font-semibold text-right">Ações</th>
                </tr>
              </thead>

              <tbody>
                <tr
                  v-for="employee in pagedEmployees"
                  :key="employee.id"
                  class="border-b border-zinc-800/60 last:border-b-0 hover:bg-zinc-900/60 transition-colors"
                >
                  <td class="px-4 py-3 font-medium text-zinc-100">{{ employee.name }}</td>
                  <td class="px-4 py-3 text-zinc-400">{{ employee.email }}</td>
                  <td class="px-4 py-3 text-zinc-300">{{ roleLabel(employee.role) }}</td>
                  <td class="px-4 py-3">
                    <span class="inline-flex items-center gap-2 text-zinc-300">
                      <span
                        class="w-1.5 h-1.5 rounded-full"
                        :class="employee.isActive ? 'bg-emerald-400' : 'bg-zinc-600'"
                      ></span>
                      {{ employee.isActive ? "Ativo" : "Inativo" }}
                    </span>
                  </td>
                  <td class="px-4 py-3 text-zinc-500">{{ formatDate(employee.createdAtUtc) }}</td>
                  <td v-if="canManage" class="px-4 py-3">
                    <div class="flex items-center justify-end gap-3">
                      <button
                        type="button"
                        @click="openEditModal(employee)"
                        class="text-xs font-semibold text-zinc-300 hover:text-white transition"
                      >
                        Editar
                      </button>
                      <button
                        type="button"
                        @click="openDeleteModal(employee)"
                        class="text-xs font-semibold text-red-400 hover:text-red-300 transition"
                      >
                        Excluir
                      </button>
                    </div>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <!-- Paginação -->
          <div
            class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 px-4 py-3 border-t border-zinc-800 text-xs text-zinc-500"
          >
            <span>
              Mostrando {{ rangeStart }}–{{ rangeEnd }} de {{ filteredEmployees.length }}
            </span>

            <div class="flex items-center gap-2">
              <button
                type="button"
                :disabled="page === 1"
                @click="goToPage(page - 1)"
                class="px-3 py-1.5 rounded-xl border border-zinc-700/60 bg-zinc-800 text-zinc-300 hover:bg-zinc-700 hover:text-white disabled:opacity-40 disabled:hover:bg-transparent transition text-xs font-semibold"
              >
                Anterior
              </button>
              <span class="px-2 text-zinc-400 text-xs">Página {{ page }} de {{ totalPages }}</span>
              <button
                type="button"
                :disabled="page === totalPages"
                @click="goToPage(page + 1)"
                class="px-3 py-1.5 rounded-xl border border-zinc-700/60 bg-zinc-800 text-zinc-300 hover:bg-zinc-700 hover:text-white disabled:opacity-40 disabled:hover:bg-transparent transition text-xs font-semibold"
              >
                Próxima
              </button>
            </div>
          </div>
        </template>
      </div>

    <!-- Modal: adicionar / editar -->
    <div
      v-if="showFormModal"
      class="fixed inset-0 z-50 bg-black/80 backdrop-blur-sm flex items-center justify-center p-4"
      @click.self="closeFormModal"
    >
      <div class="w-full max-w-md bg-zinc-900 border border-zinc-800 rounded-2xl shadow-2xl overflow-hidden">
        <div class="flex items-center justify-between px-6 py-4 border-b border-zinc-800">
          <h2 class="text-base font-bold text-white">
            {{ editingEmployee ? "Editar funcionário" : "Adicionar funcionário" }}
          </h2>
          <button
            type="button"
            @click="closeFormModal"
            class="text-zinc-500 hover:text-white transition"
            aria-label="Fechar"
          >
            ✕
          </button>
        </div>

        <form class="px-6 py-5 space-y-4" @submit.prevent="saveEmployee">
          <div>
            <label for="employee-name" class="block text-xs font-semibold text-zinc-400 mb-1.5">Nome</label>
            <input
              id="employee-name"
              v-model="form.name"
              type="text"
              autocomplete="off"
              class="w-full px-3.5 py-2.5 bg-zinc-950 border border-zinc-800 rounded-xl text-sm text-white focus:outline-none focus:border-zinc-600 transition"
            />
          </div>

          <div>
            <label for="employee-email" class="block text-xs font-semibold text-zinc-400 mb-1.5">E-mail</label>
            <input
              id="employee-email"
              v-model="form.email"
              type="email"
              autocomplete="off"
              class="w-full px-3.5 py-2.5 bg-zinc-950 border border-zinc-800 rounded-xl text-sm text-white focus:outline-none focus:border-zinc-600 transition"
            />
          </div>

          <div v-if="!editingEmployee">
            <label for="employee-password" class="block text-xs font-semibold text-zinc-400 mb-1.5">
              Senha de acesso
            </label>
            <input
              id="employee-password"
              v-model="form.password"
              type="password"
              autocomplete="new-password"
              class="w-full px-3.5 py-2.5 bg-zinc-950 border border-zinc-800 rounded-xl text-sm text-white focus:outline-none focus:border-zinc-600 transition"
            />
            <p class="mt-1.5 text-[11px] text-zinc-500">Mínimo de 6 caracteres.</p>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label for="employee-role" class="block text-xs font-semibold text-zinc-400 mb-1.5">Cargo</label>
              <select
                id="employee-role"
                v-model="form.role"
                class="w-full px-3.5 py-2.5 bg-zinc-950 border border-zinc-800 rounded-xl text-sm text-white focus:outline-none focus:border-zinc-600 transition"
              >
                <option v-for="role in EMPLOYEE_ROLES" :key="role" :value="role">
                  {{ ROLE_LABELS[role] }}
                </option>
              </select>
            </div>

            <div v-if="editingEmployee">
              <label for="employee-status" class="block text-xs font-semibold text-zinc-400 mb-1.5">Status</label>
              <select
                id="employee-status"
                v-model="form.isActive"
                class="w-full px-3.5 py-2.5 bg-zinc-950 border border-zinc-800 rounded-xl text-sm text-white focus:outline-none focus:border-zinc-600 transition"
              >
                <option :value="true">Ativo</option>
                <option :value="false">Inativo</option>
              </select>
            </div>
          </div>

          <p v-if="formError" class="px-3 py-2 rounded-xl bg-red-500/10 border border-red-500/30 text-xs text-red-300">
            {{ formError }}
          </p>

          <div class="flex items-center justify-end gap-3 pt-3 border-t border-zinc-800">
            <button
              type="button"
              @click="closeFormModal"
              class="px-4 py-2.5 rounded-xl text-xs sm:text-sm font-semibold text-zinc-400 hover:text-white bg-zinc-800 hover:bg-zinc-700 border border-zinc-700/60 transition"
            >
              Cancelar
            </button>
            <button
              type="submit"
              :disabled="isSaving"
              class="px-5 py-2.5 rounded-xl bg-white text-zinc-950 hover:bg-zinc-200 text-xs sm:text-sm font-semibold transition shadow-lg shadow-white/5 disabled:opacity-50"
            >
              {{ isSaving ? "Salvando..." : editingEmployee ? "Salvar alterações" : "Cadastrar" }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- Modal: confirmar exclusão -->
    <div
      v-if="employeeToDelete"
      class="fixed inset-0 z-50 bg-black/80 backdrop-blur-sm flex items-center justify-center p-4"
      @click.self="closeDeleteModal"
    >
      <div class="w-full max-w-sm bg-zinc-900 border border-zinc-800 rounded-2xl shadow-2xl p-6 space-y-4">
        <h2 class="text-base font-bold text-white">Excluir funcionário</h2>
        <p class="text-sm text-zinc-400">
          <strong class="text-zinc-200">{{ employeeToDelete.name }}</strong> perderá o acesso ao sistema.
          Esta ação não pode ser desfeita.
        </p>

        <p
          v-if="deleteError"
          class="px-3 py-2 rounded-xl bg-red-500/10 border border-red-500/30 text-xs text-red-300"
        >
          {{ deleteError }}
        </p>

        <div class="flex items-center justify-end gap-3 pt-3 border-t border-zinc-800">
          <button
            type="button"
            @click="closeDeleteModal"
            class="px-4 py-2.5 rounded-xl text-xs sm:text-sm font-semibold text-zinc-400 hover:text-white bg-zinc-800 hover:bg-zinc-700 border border-zinc-700/60 transition"
          >
            Cancelar
          </button>
          <button
            type="button"
            :disabled="isDeleting"
            @click="deleteEmployee"
            class="px-4 py-2.5 rounded-xl bg-red-600 hover:bg-red-500 text-white text-xs sm:text-sm font-semibold transition shadow-lg shadow-red-600/20 disabled:opacity-50"
          >
            {{ isDeleting ? "Excluindo..." : "Excluir" }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
