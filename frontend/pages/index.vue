<script setup lang="ts">
import { ArrowDown, SwitchButton } from "@element-plus/icons-vue";
import { useAuthStore } from "~/stores/auth";

const runtimeConfig = useRuntimeConfig();
const authStore = useAuthStore();
const { data, pending, error, refresh } = await useSystemStatus();

const status = computed(() => data.value);
const headline = computed(() =>
  error.value
    ? "A fundação já está estruturada, só falta a primeira resposta do backend."
    : "Arquitetura limpa, colaboração previsível e ambiente pronto para escalar."
);

const quickNotes = [
  "Backend .NET 8 em arquitetura hexagonal por camadas.",
  "Nuxt 3 com Tailwind, SCSS e Element Plus.",
  "MySQL isolado em container com volume persistente.",
  "Swagger, hot reload e fluxo colaborativo documentados."
];

const userInitials = computed(() => {
  const name = authStore.currentUser?.name || "";
  const parts = name.trim().split(/\s+/);
  if (parts.length === 0) return "U";
  if (parts.length === 1) return parts[0].substring(0, 2).toUpperCase();
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
});

const handleLogout = () => {
  authStore.logout();
};

const handleDropdownCommand = (command: string | number | object) => {
  if (command === "logout") {
    handleLogout();
  }
};
</script>

<template>
  <main class="relative overflow-hidden">
    <div class="hero-grid absolute inset-0 opacity-60" />
    <div class="hero-glow hero-glow-left" />
    <div class="hero-glow hero-glow-right" />

    <header class="relative z-10 border-b border-white/10 bg-white/5 backdrop-blur-sm">
      <div class="mx-auto flex w-full max-w-7xl items-center justify-between px-6 py-4 lg:px-10">
        <div class="flex items-center gap-3">
          <div class="flex h-9 w-9 items-center justify-center rounded-lg bg-aqua/15 text-aqua shadow-glow-sm">
            <span class="font-display text-lg font-bold">I</span>
          </div>
          <div>
            <h2 class="font-display text-lg font-semibold text-white">
              {{ runtimeConfig.public.appName }}
            </h2>
            <p class="font-mono text-[10px] uppercase tracking-widest text-mist/60">
              Dashboard Principal
            </p>
          </div>
        </div>

        <div v-if="authStore.isAuthenticated" class="flex items-center gap-4">
          <el-dropdown trigger="click" @command="handleDropdownCommand">
            <div class="flex cursor-pointer items-center gap-3 rounded-full border border-white/10 bg-white/5 px-3 py-1.5 backdrop-blur transition hover:border-aqua/40 hover:bg-white/10">
              <div class="flex h-8 w-8 items-center justify-center rounded-full bg-gradient-to-br from-aqua to-blue-600 text-sm font-semibold text-white">
                {{ userInitials }}
              </div>
              <div class="hidden text-left sm:block">
                <p class="text-sm font-medium text-white">
                  {{ authStore.currentUser?.name }}
                </p>
                <p class="font-mono text-[10px] uppercase tracking-wider text-aqua/80">
                  {{ authStore.currentUser?.role }}
                </p>
              </div>
              <el-icon class="text-mist"><ArrowDown /></el-icon>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item disabled>
                  <div class="flex flex-col">
                    <span class="text-sm font-medium">{{ authStore.currentUser?.email }}</span>
                    <span class="text-xs text-mist/60">ID: {{ authStore.currentUser?.id }}</span>
                  </div>
                </el-dropdown-item>
                <el-dropdown-item divided command="logout">
                  <div class="flex items-center gap-2">
                    <el-icon><SwitchButton /></el-icon>
                    <span>Sair</span>
                  </div>
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </div>
    </header>

    <section class="relative mx-auto flex w-full max-w-7xl flex-col px-6 py-12 lg:px-10">
      <div class="grid items-center gap-12 lg:grid-cols-[1.1fr_0.9fr]">
        <div class="space-y-8">
          <div class="inline-flex items-center gap-3 rounded-full border border-aqua/20 bg-white/5 px-4 py-2 backdrop-blur">
            <span class="h-2.5 w-2.5 rounded-full bg-aqua shadow-glow" />
            <span class="font-mono text-xs uppercase tracking-[0.35em] text-aqua/80">
              Phase 1 Foundation
            </span>
          </div>

          <div class="space-y-5">
            <p class="font-display text-5xl font-semibold leading-tight text-white md:text-6xl">
              Olá, {{ authStore.currentUser?.name || "Usuário" }}
            </p>
            <p class="max-w-2xl text-lg leading-8 text-mist">
              {{ headline }}
            </p>
          </div>

          <div class="grid gap-3 sm:grid-cols-2">
            <div
              v-for="note in quickNotes"
              :key="note"
              class="rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-sm text-mist backdrop-blur"
            >
              {{ note }}
            </div>
          </div>

          <div class="flex flex-wrap gap-4">
            <el-button type="primary" size="large" round @click="refresh()">
              Atualizar status
            </el-button>
            <a
              class="inline-flex items-center rounded-full border border-white/15 px-5 py-3 text-sm text-white/90 transition hover:border-aqua/50 hover:text-aqua"
              href="http://localhost:8080/swagger"
              target="_blank"
              rel="noreferrer"
            >
              Abrir Swagger
            </a>
          </div>

          <p v-if="error" class="text-sm text-amber-300">
            {{ error.message }}
          </p>
        </div>

        <StatusOverviewCard :status="status" :pending="pending" />
      </div>
    </section>
  </main>
</template>
