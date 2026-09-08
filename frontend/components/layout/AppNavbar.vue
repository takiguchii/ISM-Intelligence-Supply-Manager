<script setup lang="ts">
import { ref, onMounted, onUnmounted } from "vue";
import { useAuthStore } from "~/stores/auth";
import ThemeToggle from "~/components/base/ThemeToggle.vue";

const emit = defineEmits<{
  (e: "toggleSidebar"): void;
}>();

const runtimeConfig = useRuntimeConfig();
const authStore = useAuthStore();
const router = useRouter();

const isLogoutExpanded = ref(false);
let collapseTimeout: any = null;

const handleLogoutClick = (event: MouseEvent) => {
  const isTouchDevice = typeof window !== "undefined" && ("ontouchstart" in window || navigator.maxTouchPoints > 0);
  const isMobileScreen = typeof window !== "undefined" && window.innerWidth < 768;

  // On mobile / touch screens: 1st tap expands and reveals "Sair", 2nd tap performs logout
  if ((isTouchDevice || isMobileScreen) && !isLogoutExpanded.value) {
    event.preventDefault();
    event.stopPropagation();
    isLogoutExpanded.value = true;

    clearTimeout(collapseTimeout);
    collapseTimeout = setTimeout(() => {
      isLogoutExpanded.value = false;
    }, 4000);
    return;
  }

  // Action: logout on 2nd tap (or direct click on desktop)
  clearTimeout(collapseTimeout);
  isLogoutExpanded.value = false;
  authStore.logout();
  router.push("/login");
};

const handleClickOutside = (e: MouseEvent) => {
  const target = e.target as HTMLElement | null;
  if (isLogoutExpanded.value && target && !target.closest(".Btn")) {
    isLogoutExpanded.value = false;
  }
};

onMounted(() => {
  if (typeof window !== "undefined") {
    window.addEventListener("click", handleClickOutside);
  }
});

onUnmounted(() => {
  if (typeof window !== "undefined") {
    window.removeEventListener("click", handleClickOutside);
  }
  clearTimeout(collapseTimeout);
});
</script>

<template>
  <header class="h-16 border-b border-zinc-800/80 bg-zinc-900/60 backdrop-blur-xl sticky top-0 z-30 px-4 sm:px-6 flex items-center justify-between">
    <div class="flex items-center gap-4">
      <button
        @click="emit('toggleSidebar')"
        type="button"
        class="p-2 rounded-xl text-zinc-300 hover:text-white hover:bg-zinc-800/80 transition-all duration-200 focus:outline-none focus:ring-2 focus:ring-zinc-600"
        title="Abrir Menu Lateral"
        aria-label="Abrir Menu Lateral"
      >
        <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"></path>
        </svg>
      </button>

      <NuxtLink to="/" class="flex items-center gap-3">
        <span class="font-bold text-lg text-white tracking-tight">ISM</span>
        <span class="hidden sm:inline-block text-xs uppercase tracking-widest text-zinc-400 font-mono border-l border-zinc-700/60 pl-3">
          {{ runtimeConfig.public.appName }}
        </span>
      </NuxtLink>
    </div>

    <div class="flex items-center gap-3">
      <ThemeToggle />
      <div v-if="authStore.isAuthenticated" class="flex items-center gap-3">
        <span class="hidden md:inline-block text-xs text-zinc-400 font-medium">
          {{ authStore.currentUser?.name }} ({{ authStore.currentUser?.role }})
        </span>
        <button
          :class="['Btn', isLogoutExpanded ? 'is-expanded' : '']"
          @click="handleLogoutClick"
          type="button"
          title="Sair"
          aria-label="Sair"
        >
          <div class="sign">
            <svg viewBox="0 0 512 512">
              <path d="M377.9 105.9L500.7 228.7c7.2 7.2 11.3 17.1 11.3 27.3s-4.1 20.1-11.3 27.3L377.9 406.1c-6.4 6.4-15 9.9-24 9.9c-18.7 0-33.9-15.2-33.9-33.9l0-62.1-128 0c-17.7 0-32-14.3-32-32l0-64c0-17.7 14.3-32 32-32l128 0 0-62.1c0-18.7 15.2-33.9 33.9-33.9c9 0 17.6 3.6 24 9.9zM160 96L96 96c-17.7 0-32 14.3-32 32l0 256c0 17.7 14.3 32 32 32l64 0c17.7 0 32 14.3 32 32s-14.3 32-32 32l-64 0c-53 0-96-43-96-96L0 128C0 75 43 32 96 32l64 0c17.7 0 32 14.3 32 32s-14.3 32-32 32z"></path>
            </svg>
          </div>
          <div class="text">Sair</div>
        </button>
      </div>
      <NuxtLink
        v-else
        to="/login"
        class="px-4 py-2 text-xs font-semibold rounded-xl bg-zinc-800 hover:bg-zinc-700 text-zinc-200 hover:text-white border border-zinc-700/60 transition-all duration-200 flex items-center gap-2"
      >
        <svg class="w-4 h-4 text-zinc-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 16l-4-4m0 0l4-4m-4 4h14m-5 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h7a3 3 0 013 3v1"></path>
        </svg>
        <span>Login</span>
      </NuxtLink>
    </div>
  </header>
</template>

<style scoped>
.Btn {
  display: flex;
  align-items: center;
  justify-content: flex-start;
  width: 42px;
  height: 42px;
  border: none;
  border-radius: 50%;
  cursor: pointer;
  position: relative;
  overflow: hidden;
  transition-duration: .3s;
  box-shadow: 2px 2px 10px rgba(0, 0, 0, 0.199);
  background-color: rgb(255, 65, 65);
}

/* plus/exit sign */
.sign {
  width: 100%;
  transition-duration: .3s;
  display: flex;
  align-items: center;
  justify-content: center;
}

.sign svg {
  width: 17px;
  height: 17px;
}

.sign svg path {
  fill: white;
}

/* text */
.text {
  position: absolute;
  right: 0%;
  width: 0%;
  opacity: 0;
  color: white;
  font-size: 0.95em;
  font-weight: 600;
  transition-duration: .3s;
  white-space: nowrap;
}

/* hover effect on desktop & is-expanded on mobile */
.Btn:hover,
.Btn.is-expanded {
  width: 110px;
  border-radius: 40px;
  transition-duration: .3s;
}

.Btn:hover .sign,
.Btn.is-expanded .sign {
  width: 30%;
  transition-duration: .3s;
  padding-left: 14px;
}

/* hover / expanded text reveal */
.Btn:hover .text,
.Btn.is-expanded .text {
  opacity: 1;
  width: 70%;
  transition-duration: .3s;
  padding-right: 14px;
}

/* button click effect */
.Btn:active {
  transform: translate(2px, 2px);
}
</style>
