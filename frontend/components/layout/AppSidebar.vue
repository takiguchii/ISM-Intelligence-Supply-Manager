<script setup lang="ts">
import { ref } from "vue";
import { useAuthStore } from "~/stores/auth";

defineProps<{
  isOpen: boolean;
}>();

const emit = defineEmits<{
  (e: "close"): void;
}>();

const authStore = useAuthStore();
const route = useRoute();
const router = useRouter();

const menuItems = [
  { id: "dashboard", label: "Dashboard", icon: "dashboard", route: "/" },
  { id: "financeiro", label: "Financeiro", icon: "wallet", route: "/financeiro" },
  { id: "cardapio", label: "Cardápio", icon: "menu", route: "/cardapio" },
  { id: "estoque", label: "Estoque", icon: "boxes", route: "/estoque" },
  { id: "fornecedores", label: "Fornecedores", icon: "truck", route: "/fornecedores" },
  { id: "funcionarios", label: "Funcionários", icon: "users", route: "/funcionarios" },
  { id: "integracoes", label: "Integrações", icon: "plug", route: "/integracoes" }
];

const activeItem = computed(() => {
  const match = menuItems.find(item => item.route === route.path);
  return match ? match.id : "dashboard";
});

const selectItem = (item: typeof menuItems[number]) => {
  emit("close");
  router.push(item.route);
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
        'fixed top-0 left-0 bottom-0 z-50 w-64 bg-zinc-950 border-r border-zinc-800/80 flex flex-col transition-transform duration-300 ease-in-out',
        isOpen ? 'translate-x-0' : '-translate-x-full'
      ]"
    >
      <!-- Sidebar Header -->
      <div class="h-16 px-6 flex items-center justify-between border-b border-zinc-800/60">
        <div class="flex items-center gap-3">
          <div class="w-8 h-8 rounded-lg bg-zinc-800 border border-zinc-700/60 flex items-center justify-center">
            <span class="font-bold text-xs text-white tracking-widest">ISM</span>
          </div>
          <span class="font-semibold text-sm text-white tracking-tight">Intelligence Supply</span>
        </div>
        <button
          @click="emit('close')"
          class="text-zinc-400 hover:text-white transition-colors p-1 rounded-lg hover:bg-zinc-800/60"
        >
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
          </svg>
        </button>
      </div>

      <!-- Sidebar Navigation Menu -->
      <div class="flex-1 overflow-y-auto px-3 py-6 space-y-1.5 selection:bg-zinc-800">
        <button
          v-for="item in menuItems"
          :key="item.id"
          @click="selectItem(item)"
          :class="[
            'w-full flex items-center gap-3.5 px-4 py-3 rounded-xl text-sm font-medium transition-all duration-200 text-left',
            activeItem === item.id
              ? 'bg-zinc-900 text-white font-semibold shadow-inner border border-zinc-800/80'
              : 'text-zinc-400 hover:text-zinc-100 hover:bg-zinc-900/50'
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

          <!-- Funcionários Icon (Users) -->
          <svg v-else-if="item.icon === 'users'" class="w-5 h-5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z"></path>
          </svg>

          <!-- Integrações Icon (Plug) -->
          <svg v-else-if="item.icon === 'plug'" class="w-5 h-5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 3v2m6-2v2M9 19v2m6-2v2M5 9H3m2 6H3m18-6h-2m2 6h-2M7 19h10a2 2 0 002-2V7a2 2 0 00-2-2H7a2 2 0 00-2 2v10a2 2 0 002 2zM9 9h6v6H9V9z"></path>
          </svg>

          <span>{{ item.label }}</span>
        </button>
      </div>

      <!-- Sidebar Footer User / System Info -->
      <div class="p-4 border-t border-zinc-800/60 bg-zinc-950/80">
        <div class="flex items-center gap-3 p-2 rounded-xl bg-zinc-900/60 border border-zinc-800/60">
          <div class="w-8 h-8 rounded-full bg-zinc-700 flex items-center justify-center text-xs font-bold text-white">
            {{ userInitials }}
          </div>
          <div class="flex-1 min-w-0">
            <p class="text-xs font-medium text-white truncate">{{ authStore.currentUser?.name || "Usuário ISM" }}</p>
            <p class="text-[10px] text-zinc-400 truncate">{{ authStore.currentUser?.role || "Gestor de Suprimentos" }}</p>
          </div>
        </div>
      </div>
    </aside>
  </div>
</template>
