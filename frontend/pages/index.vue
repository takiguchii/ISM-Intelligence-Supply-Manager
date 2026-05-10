<script setup lang="ts">
const runtimeConfig = useRuntimeConfig();
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
</script>

<template>
  <main class="relative overflow-hidden">
    <div class="hero-grid absolute inset-0 opacity-60" />
    <div class="hero-glow hero-glow-left" />
    <div class="hero-glow hero-glow-right" />

    <section class="relative mx-auto flex min-h-screen w-full max-w-7xl flex-col justify-center px-6 py-16 lg:px-10">
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
              {{ runtimeConfig.public.appName }}
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
