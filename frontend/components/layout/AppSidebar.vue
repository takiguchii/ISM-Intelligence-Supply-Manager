<script setup lang="ts">
import { ref, watchEffect, computed, onMounted as vueOnMounted, onBeforeUnmount as vueOnBeforeUnmount } from "vue";
import { useAuthStore } from "~/stores/auth";
import { useThemeStore } from "~/stores/theme";

const props = defineProps<{
  isOpen: boolean;
}>();

const emit = defineEmits<{
  (e: "close"): void;
}>();

const authStore = useAuthStore();
const themeStore = useThemeStore();
const route = useRoute();

const isDesktopView = ref(false);
const closeButtonTitle = computed(() =>
  isDesktopView.value ? "Minimizar menu lateral" : "Fechar menu lateral"
);

vueOnMounted(() => {
  if (typeof window !== "undefined") {
    isDesktopView.value = window.innerWidth >= 1024;
    const updateView = () => {
      isDesktopView.value = window.innerWidth >= 1024;
    };
    window.addEventListener("resize", updateView);
    vueOnBeforeUnmount(() => {
      window.removeEventListener("resize", updateView);
    });
  }
});

const menuItems = [
  { id: "dashboard", label: "Dashboard", icon: "dashboard", route: "/" },
  { id: "financeiro", label: "Financeiro", icon: "wallet", route: "/financeiro" },
  { id: "cardapio", label: "Cardápio", icon: "menu", route: "/cardapio" },
  { id: "estoque", label: "Estoque", icon: "boxes", route: "/estoque" },
  { id: "fornecedores", label: "Fornecedores", icon: "truck", route: "/fornecedores" },
  { id: "integracoes", label: "Integrações", icon: "plug", route: "/integracoes" },
  { id: "configuracoes", label: "Configurações", icon: "gear", route: "/configuracoes" }
];

const router = useRouter();

const activeItem = computed(() => {
  const match = menuItems.find((m) => route.path === m.route || route.path.startsWith(m.route + "/"));
  return match?.id ?? menuItems[0].id;
});

const selectItem = (item: typeof menuItems[number]) => {
  router.push(item.route);
  if (window.innerWidth < 1024) emit("close");
};

const userInitials = computed(() => {
  const name = authStore.currentUser?.name || "";
  const parts = name.trim().split(/\s+/);
  if (parts.length === 0 || !parts[0]) return "US";
  if (parts.length === 1) return parts[0].substring(0, 2).toUpperCase();
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
});
</script>

<template>
  <div>
    <!-- Backdrop overlay for mobile screen sizes -->
    <div
      v-if="isOpen"
      @click="emit('close')"
      class="fixed inset-0 bg-black/60 backdrop-blur-sm z-40 lg:hidden transition-opacity duration-300"
    ></div>

    <!-- Sidebar Drawer Container -->
    <aside
      :class="[
        'fixed top-0 left-0 bottom-0 z-50 w-64 flex flex-col transition-all duration-300 ease-in-out border-r',
        themeStore.isDark
          ? 'bg-zinc-950 border-zinc-800/80'
          : 'bg-white border-zinc-200 shadow-xl',
        isOpen ? 'translate-x-0' : '-translate-x-full'
      ]"
    >
      <!-- Sidebar Header -->
      <div
        :class="[
          'h-16 px-6 flex items-center justify-between border-b',
          themeStore.isDark ? 'border-zinc-800/60' : 'border-zinc-200'
        ]"
      >
        <div class="flex items-center gap-3">
          <div
            :class="[
              'w-8 h-8 rounded-lg flex items-center justify-center border',
              themeStore.isDark
                ? 'bg-zinc-800 border-zinc-700/60'
                : 'bg-zinc-100 border-zinc-200'
            ]"
          >
            <span
              :class="[
                'font-bold text-xs tracking-widest',
                themeStore.isDark ? 'text-white' : 'text-zinc-800'
              ]"
            >ISM</span>
          </div>
          <span
            :class="[
              'font-semibold text-sm tracking-tight',
              themeStore.isDark ? 'text-white' : 'text-zinc-800'
            ]"
          >Intelligence Supply</span>
        </div>
        <div class="flex items-center gap-1">
          <button
            @click="themeStore.toggle()"
            :class="[
              'p-2 rounded-lg transition-colors',
              themeStore.isDark
                ? 'text-zinc-400 hover:text-white hover:bg-zinc-800/60'
                : 'text-zinc-500 hover:text-zinc-800 hover:bg-zinc-100'
            ]"
            :title="themeStore.isDark ? 'Ativar modo claro' : 'Ativar modo escuro'"
          >
            <svg
              v-if="themeStore.isDark"
              class="w-4 h-4"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M12 3v1m0 16v1m9-9h-1M4 12H3m15.364 6.364l-.707-.707M6.343 6.343l-.707-.707m12.728 0l-.707.707M6.343 17.657l-.707.707M16 12a4 4 0 11-8 0 4 4 0 018 0z"
              />
            </svg>
            <svg
              v-else
              class="w-4 h-4"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z"
              />
            </svg>
          </button>
          <button
            @click="emit('close')"
            :class="[
              'transition-colors p-1.5 rounded-lg',
              themeStore.isDark
                ? 'text-zinc-400 hover:text-white hover:bg-zinc-800/60'
                : 'text-zinc-500 hover:text-zinc-800 hover:bg-zinc-100'
            ]"
            :title="closeButtonTitle"
          >
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
            </svg>
          </button>
        </div>
      </div>

      <!-- Sidebar Navigation Menu -->
      <div
        :class="[
          'flex-1 overflow-y-auto px-3 py-6 space-y-1.5',
          themeStore.isDark ? 'selection:bg-zinc-800' : 'selection:bg-indigo-100'
        ]"
      >
        <button
          v-for="item in menuItems"
          :key="item.id"
          @click="selectItem(item)"
          :class="[
            'w-full flex items-center gap-3.5 px-4 py-3 rounded-xl text-sm font-medium transition-all duration-200 text-left',
            activeItem === item.id
              ? (
                  themeStore.isDark
                    ? 'bg-zinc-900 text-white font-semibold shadow-inner border border-zinc-800/80'
                    : 'bg-indigo-50 text-indigo-700 font-semibold border border-indigo-100'
                )
              : (
                  themeStore.isDark
                    ? 'text-zinc-400 hover:text-zinc-100 hover:bg-zinc-900/50'
                    : 'text-zinc-600 hover:text-zinc-900 hover:bg-zinc-100'
                )
          ]"
        >
          <!-- Dashboard Icon -->
          <svg v-if="item.icon === 'dashboard'" class="w-5 h-5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4h6v6H4V4zm10 0h6v6h-6V4zM4 14h6v6H4v-6zm10 0h6v6h-6v-6z"></path>
          </svg>

          <!-- Financeiro Icon (Wallet) -->
          <svg v-else-if="item.icon === 'wallet'" class="w-5 h-5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z"></path>
          </svg>

          <!-- Cardápio Icon (Fork & Knife / Book) -->
          <svg v-else-if="item.icon === 'menu'" class="w-5 h-5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253"></path>
          </svg>

          <!-- Estoque Icon (3D Cube) -->
          <svg v-else-if="item.icon === 'boxes'" class="w-5 h-5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4"></path>
          </svg>

          <!-- Fornecedores Icon (Truck) -->
          <svg v-else-if="item.icon === 'truck'" class="w-5 h-5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 17a2 2 0 11-4 0 2 2 0 014 0zM19 17a2 2 0 11-4 0 2 2 0 014 0z"></path>
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16V6a1 1 0 00-1-1H4a1 1 0 00-1 1v10a1 1 0 001 1h1m8-1a1 1 0 01-1 1H9m4-1V8h4l3 3v5h-2m-6 0h2"></path>
          </svg>

          <!-- Integrações Icon (Plug) -->
          <svg v-else-if="item.icon === 'plug'" class="w-5 h-5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 3v2m6-2v2M9 19v2m6-2v2M5 9H3m2 6H3m18-6h-2m2 6h-2M7 19h10a2 2 0 002-2V7a2 2 0 00-2-2H7a2 2 0 00-2 2v10a2 2 0 002 2zM9 9h6v6H9V9z"></path>
          </svg>

          <!-- Configurações Icon (Gear) -->
          <svg v-else-if="item.icon === 'gear'" class="w-5 h-5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z"></path>
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"></path>
          </svg>

          <span>{{ item.label }}</span>
        </button>
      </div>

      <!-- Sidebar Footer User / System Info -->
      <div
        :class="[
          'p-4 border-t',
          themeStore.isDark
            ? 'border-zinc-800/60 bg-zinc-950/80'
            : 'border-zinc-200 bg-zinc-50/80'
        ]"
      >
        <div
          :class="[
            'flex items-center gap-3 p-2 rounded-xl border',
            themeStore.isDark
              ? 'bg-zinc-900/60 border-zinc-800/60'
              : 'bg-white border-zinc-200 shadow-sm'
          ]"
        >
          <div
            :class="[
              'w-8 h-8 rounded-full flex items-center justify-center text-xs font-bold',
              themeStore.isDark
                ? 'bg-zinc-700 text-white'
                : 'bg-indigo-100 text-indigo-700'
            ]"
          >
            {{ userInitials }}
          </div>
          <div class="flex-1 min-w-0">
            <p
              :class="[
                'text-xs font-medium truncate',
                themeStore.isDark ? 'text-white' : 'text-zinc-800'
              ]"
            >{{ authStore.currentUser?.name || "Usuário ISM" }}</p>
            <p
              :class="[
                'text-[10px] truncate',
                themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500'
              ]"
            >{{ authStore.currentUser?.role || "Gestor de Suprimentos" }}</p>
          </div>
        </div>
      </div>
    </aside>
  </div>
</template>
