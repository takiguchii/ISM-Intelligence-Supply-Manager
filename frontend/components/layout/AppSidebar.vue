<script setup lang="ts">
import { ref, computed, watch, onMounted, onBeforeUnmount } from "vue";
import { useAuthStore } from "~/stores/auth";
import { useThemeStore } from "~/stores/theme";
import { navigationForUser, type NavigationItem } from "~/utils/navigation";

const props = withDefaults(
  defineProps<{
    isOpen?: boolean;
    isCollapsed?: boolean;
  }>(),
  {
    isOpen: false,
    isCollapsed: false,
  }
);

const emit = defineEmits<{
  (e: "close"): void;
  (e: "update:isCollapsed", value: boolean): void;
  (e: "toggleCollapse"): void;
}>();

const authStore = useAuthStore();
const themeStore = useThemeStore();
const route = useRoute();
const router = useRouter();

const searchQuery = ref("");
const internalCollapsed = ref(props.isCollapsed);

watch(
  () => props.isCollapsed,
  (val) => {
    internalCollapsed.value = val;
  }
);

const toggleCollapse = () => {
  internalCollapsed.value = !internalCollapsed.value;
  emit("update:isCollapsed", internalCollapsed.value);
  emit("toggleCollapse");
};

// Obter itens de menu para o usuário autenticado
const rawMenuItems = computed(() => navigationForUser(authStore.currentUser));

// Filtragem inteligente de views
const filteredMenuItems = computed(() => {
  const query = searchQuery.value.trim().toLowerCase();
  if (!query) return rawMenuItems.value;

  return rawMenuItems.value.filter(
    (item) =>
      item.label.toLowerCase().includes(query) ||
      item.route.toLowerCase().includes(query) ||
      (item.category && item.category.toLowerCase().includes(query))
  );
});

// Agrupamento por categorias
const groupedMenuItems = computed(() => {
  const groups: { category: string; items: NavigationItem[] }[] = [];
  const items = filteredMenuItems.value;

  items.forEach((item) => {
    const cat = item.category || "VISÃO GERAL";
    let existingGroup = groups.find((g) => g.category === cat);
    if (!existingGroup) {
      existingGroup = { category: cat, items: [] };
      groups.push(existingGroup);
    }
    existingGroup.items.push(item);
  });

  return groups;
});

// Identificar item ativo
const isItemActive = (item: NavigationItem) => {
  if (item.route === "/" || item.route === "/dashboard") {
    return route.path === "/" || route.path === "/dashboard";
  }
  return route.path === item.route || route.path.startsWith(item.route + "/");
};

const selectItem = (item: NavigationItem) => {
  if (!item.route) return;
  router.push(item.route);
  if (typeof window !== "undefined" && window.innerWidth < 1024) {
    emit("close");
  }
};

const userInitials = computed(() => {
  const name = authStore.currentUser?.name || "";
  const parts = name.trim().split(/\s+/);
  if (parts.length === 0 || !parts[0]) return "AD";
  if (parts.length === 1) return parts[0].substring(0, 2).toUpperCase();
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
});

const userRoleFormatted = computed(() => {
  const role = authStore.currentUser?.role;
  if (!role) return "Super Admin";
  if (role === "Admin") return "Super Admin";
  if (role === "Manager") return "Gerente";
  if (role === "Chef") return "Chef Executivo";
  if (role === "Waiter") return "Atendimento";
  return role;
});

const isMobileScreen = ref(false);

const updateScreen = () => {
  if (typeof window !== "undefined") {
    isMobileScreen.value = window.innerWidth < 1024;
  }
};

onMounted(() => {
  updateScreen();
  window.addEventListener("resize", updateScreen);
});

onBeforeUnmount(() => {
  if (typeof window !== "undefined") {
    window.removeEventListener("resize", updateScreen);
  }
});
</script>

<template>
  <div>
    <!-- Backdrop overlay para mobile com animação fluida -->
    <div
      v-if="isOpen && isMobileScreen"
      @click="emit('close')"
      class="fixed inset-0 bg-black/60 backdrop-blur-sm z-40 lg:hidden sidebar-fade-enter"
    ></div>

    <!-- Sidebar Container com física de animação ultra fluida (120fps) -->
    <aside
      :class="[
        'fixed top-0 left-0 bottom-0 z-50 flex flex-col select-none border-r ism-sidebar-transition',
        themeStore.isDark
          ? 'bg-[#0f1115] border-zinc-800/80 text-zinc-300'
          : 'bg-[#fafafa] border-zinc-200 text-zinc-700 shadow-xl shadow-zinc-200/50',
        // Largura retraída vs expandida
        internalCollapsed ? 'w-[72px]' : 'w-[256px]',
        // Visibilidade em mobile
        isMobileScreen ? (isOpen ? 'translate-x-0' : '-translate-x-full') : 'translate-x-0'
      ]"
    >
      <!-- Botão circular de retração/expansão -->
      <button
        @click="toggleCollapse"
        type="button"
        :aria-label="internalCollapsed ? 'Expandir menu lateral' : 'Retrair menu lateral'"
        :class="[
          'absolute -right-3 top-6 z-20 hidden lg:flex items-center justify-center w-6 h-6 rounded-full border shadow-md transition-all duration-300 hover:scale-110 active:scale-95',
          themeStore.isDark
            ? 'bg-zinc-800 text-zinc-200 border-zinc-700 hover:bg-zinc-700'
            : 'bg-white text-zinc-800 border-zinc-300 hover:bg-zinc-100'
        ]"
      >
        <svg
          class="w-3.5 h-3.5 transition-transform duration-500 ease-[cubic-bezier(0.16,1,0.3,1)]"
          :class="internalCollapsed ? 'rotate-180' : 'rotate-0'"
          fill="none"
          stroke="currentColor"
          stroke-width="2.5"
          viewBox="0 0 24 24"
        >
          <path stroke-linecap="round" stroke-linejoin="round" d="M15 19l-7-7 7-7" />
        </svg>
      </button>

      <!-- Topo: Perfil do Usuário com Foto / Avatar, Nome, Cargo e Status Online -->
      <div
        :class="[
          'pt-5 pb-3 transition-colors duration-300 border-b',
          themeStore.isDark ? 'border-zinc-800/50' : 'border-zinc-200',
          internalCollapsed ? 'px-2 flex flex-col items-center' : 'px-4'
        ]"
      >
        <div class="flex items-center gap-3">
          <!-- Avatar com indicador de status online -->
          <div class="relative shrink-0">
            <div
              :class="[
                'w-10 h-10 rounded-full overflow-hidden ring-2 ring-emerald-500/40 flex items-center justify-center font-semibold text-sm shadow-md',
                themeStore.isDark
                  ? 'bg-gradient-to-tr from-zinc-800 to-zinc-700 text-zinc-100'
                  : 'bg-gradient-to-tr from-zinc-200 to-zinc-100 text-zinc-800'
              ]"
            >
              <img
                src="/logos/ism-sidebar-logo.png"
                alt="Avatar"
                class="w-full h-full object-cover"
                @error="($event.target as HTMLElement).style.display = 'none'"
              />
              <span class="avatar-fallback">{{ userInitials }}</span>
            </div>
            <!-- Ponto Verde Indicador Online -->
            <span
              :class="[
                'absolute bottom-0 right-0 w-3 h-3 bg-emerald-500 border-2 rounded-full ring-1 ring-emerald-400',
                themeStore.isDark ? 'border-[#0f1115]' : 'border-[#fafafa]'
              ]"
              title="Online"
            ></span>
          </div>

          <!-- Informações do Usuário (Ocultadas se colapsado) -->
          <div
            v-if="!internalCollapsed"
            class="min-w-0 flex-1 overflow-hidden transition-all duration-300"
          >
            <h3
              :class="[
                'text-sm font-bold truncate leading-tight',
                themeStore.isDark ? 'text-white' : 'text-zinc-900'
              ]"
            >
              {{ authStore.currentUser?.name || "Takiguchi" }}
            </h3>
            <div class="flex items-center gap-1.5 mt-0.5 text-[11px]">
              <span
                :class="[
                  'truncate font-medium',
                  themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500'
                ]"
              >
                {{ userRoleFormatted }}
              </span>
              <span :class="themeStore.isDark ? 'text-zinc-600' : 'text-zinc-300'">•</span>
              <span class="text-emerald-500 dark:text-emerald-400 font-medium shrink-0 flex items-center gap-1">
                Online
              </span>
            </div>
          </div>
        </div>

        <!-- Barra de Busca Inteligente de Views -->
        <div
          v-if="!internalCollapsed"
          class="mt-3.5 relative transition-all duration-300"
        >
          <div class="relative flex items-center">
            <div
              :class="[
                'absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none',
                themeStore.isDark ? 'text-zinc-400' : 'text-zinc-400'
              ]"
            >
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
            </div>
            <input
              v-model="searchQuery"
              type="text"
              placeholder="Buscar menu..."
              :class="[
                'w-full pl-9 pr-7 py-1.5 text-xs rounded-lg border focus:outline-none focus:ring-1 transition-all duration-200',
                themeStore.isDark
                  ? 'bg-zinc-900/90 text-zinc-100 placeholder-zinc-500 border-zinc-800/80 focus:border-emerald-500/80 focus:ring-emerald-500/50'
                  : 'bg-white text-zinc-900 placeholder-zinc-400 border-zinc-300 focus:border-emerald-500 focus:ring-emerald-500/40 shadow-sm'
              ]"
            />
            <button
              v-if="searchQuery"
              @click="searchQuery = ''"
              type="button"
              :class="[
                'absolute inset-y-0 right-0 pr-2.5 flex items-center',
                themeStore.isDark ? 'text-zinc-400 hover:text-zinc-200' : 'text-zinc-400 hover:text-zinc-600'
              ]"
            >
              <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
        </div>
      </div>

      <!-- Área de Navegação com Scroll Suave -->
      <div
        class="flex-1 overflow-y-auto overflow-x-hidden py-3 scrollbar-none space-y-4"
        :class="internalCollapsed ? 'px-2' : 'px-3'"
      >
        <!-- Iteração por grupos de categoria -->
        <div
          v-for="group in groupedMenuItems"
          :key="group.category"
          class="space-y-1"
        >
          <!-- Título da Categoria (Exibido apenas quando expandido) -->
          <div
            v-if="!internalCollapsed"
            :class="[
              'px-2 pt-2 pb-1 text-[10px] font-bold tracking-wider uppercase select-none transition-colors duration-200',
              themeStore.isDark ? 'text-zinc-500' : 'text-zinc-400'
            ]"
          >
            {{ group.category }}
          </div>
          <!-- Linha separadora quando colapsado -->
          <div
            v-else
            :class="[
              'w-8 h-[1px] mx-auto my-2 transition-colors duration-200',
              themeStore.isDark ? 'bg-zinc-800/60' : 'bg-zinc-200'
            ]"
          ></div>

          <!-- Itens da Categoria -->
          <button
            v-for="item in group.items"
            :key="item.id"
            @click="selectItem(item)"
            :title="internalCollapsed ? item.label : undefined"
            type="button"
            :class="[
              'group relative w-full flex items-center rounded-xl text-xs font-medium transition-all duration-200',
              internalCollapsed
                ? 'justify-center p-2.5 my-1'
                : 'gap-3 px-3 py-2.5',
              isItemActive(item)
                ? (
                    themeStore.isDark
                      ? 'bg-emerald-600/20 text-emerald-400 font-semibold border border-emerald-500/30 shadow-[inset_0_1px_0_0_rgba(255,255,255,0.05)]'
                      : 'bg-emerald-50 text-emerald-700 font-semibold border border-emerald-200 shadow-sm'
                  )
                : (
                    themeStore.isDark
                      ? 'text-zinc-400 hover:text-zinc-100 hover:bg-zinc-800/50 border border-transparent'
                      : 'text-zinc-600 hover:text-zinc-900 hover:bg-zinc-100 border border-transparent'
                  )
            ]"
          >
            <!-- Ícone SVG correspondente ao modelo -->
            <div
              :class="[
                'shrink-0 flex items-center justify-center transition-all duration-200 group-hover:scale-105',
                isItemActive(item)
                  ? (themeStore.isDark ? 'text-emerald-400' : 'text-emerald-600')
                  : (themeStore.isDark ? 'text-zinc-400 group-hover:text-zinc-200' : 'text-zinc-500 group-hover:text-zinc-800')
              ]"
            >
              <!-- Dashboard -->
              <svg v-if="item.icon === 'dashboard'" class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                <rect x="3" y="3" width="7" height="7" rx="1.5" />
                <rect x="14" y="3" width="7" height="7" rx="1.5" />
                <rect x="14" y="14" width="7" height="7" rx="1.5" />
                <rect x="3" y="14" width="7" height="7" rx="1.5" />
              </svg>

              <!-- Financeiro / Wallet -->
              <svg v-else-if="item.icon === 'wallet'" class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z" />
              </svg>

              <!-- Atendimento / Bandeja -->
              <svg v-else-if="item.icon === 'tray'" class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" d="M4 19h16M4 15h16M4 11h16M8 7h8" />
              </svg>

              <!-- Cozinha / Chef Hat -->
              <svg v-else-if="item.icon === 'chef-hat'" class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" d="M6 19h12M6 21h12M5 19v-4a4 4 0 013.8-3.99A4 4 0 0112 7a4 4 0 013.2 4.01A4 4 0 0119 15v4" />
              </svg>

              <!-- Pedidos / Clipboard -->
              <svg v-else-if="item.icon === 'clipboard-list'" class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4" />
              </svg>

              <!-- Cardápio / Menu Book -->
              <svg v-else-if="item.icon === 'menu'" class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253" />
              </svg>

              <!-- Estoque / Boxes -->
              <svg v-else-if="item.icon === 'boxes'" class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
              </svg>

              <!-- Fornecedores / Truck -->
              <svg v-else-if="item.icon === 'truck'" class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" d="M9 17a2 2 0 11-4 0 2 2 0 014 0zM19 17a2 2 0 11-4 0 2 2 0 014 0z" />
                <path stroke-linecap="round" stroke-linejoin="round" d="M13 16V6a1 1 0 00-1-1H4a1 1 0 00-1 1v10a1 1 0 001 1h1m8-1a1 1 0 01-1 1H9m4-1V8h4l3 3v5h-2m-6 0h2" />
              </svg>

              <!-- Funcionários / Users -->
              <svg v-else-if="item.icon === 'users'" class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" d="M17 20h5v-2a4 4 0 00-4-4h-1m-4 6H2v-2a4 4 0 014-4h3a4 4 0 014 4v2zm-5-9a4 4 0 100-8 4 4 0 000 8z" />
              </svg>

              <!-- Integrações / Plug -->
              <svg v-else-if="item.icon === 'plug'" class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" d="M9 3v2m6-2v2M9 19v2m6-2v2M5 9H3m2 6H3m18-6h-2m2 6h-2M7 19h10a2 2 0 002-2V7a2 2 0 00-2-2H7a2 2 0 00-2 2v10a2 2 0 002 2zM9 9h6v6H9V9z" />
              </svg>

              <!-- Configurações / Gear -->
              <svg v-else-if="item.icon === 'gear'" class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
                <path stroke-linecap="round" stroke-linejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
              </svg>

              <!-- Fallback -->
              <svg v-else class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                <circle cx="12" cy="12" r="9" />
              </svg>
            </div>

            <!-- Rótulo do Item (exibido apenas se expandido) -->
            <span
              v-if="!internalCollapsed"
              class="truncate text-left flex-1"
            >
              {{ item.label }}
            </span>
          </button>
        </div>

        <!-- Feedback de busca vazia -->
        <div
          v-if="!internalCollapsed && groupedMenuItems.length === 0"
          :class="[
            'px-4 py-8 text-center text-xs',
            themeStore.isDark ? 'text-zinc-500' : 'text-zinc-400'
          ]"
        >
          Nenhuma view encontrada para "{{ searchQuery }}"
        </div>
      </div>
    </aside>
  </div>
</template>

<style scoped>
/* Transição com aceleração de hardware 120fps e timing premium */
.ism-sidebar-transition {
  will-change: width, transform;
  transform: translateZ(0);
  backface-visibility: hidden;
  transition-property: width, transform, background-color, border-color, box-shadow;
  transition-duration: 400ms;
  transition-timing-function: cubic-bezier(0.16, 1, 0.3, 1);
}

.sidebar-fade-enter {
  animation: sidebarBackdropFade 350ms cubic-bezier(0.16, 1, 0.3, 1) forwards;
}

@keyframes sidebarBackdropFade {
  from {
    opacity: 0;
  }
  to {
    opacity: 1;
  }
}

/* Ocultar barra de rolagem mantendo funcionalidade */
.scrollbar-none::-webkit-scrollbar {
  display: none;
}
.scrollbar-none {
  -ms-overflow-style: none;
  scrollbar-width: none;
}
</style>
