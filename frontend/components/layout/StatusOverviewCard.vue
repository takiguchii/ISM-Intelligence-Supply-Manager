<script setup lang="ts">
import type { SystemStatus } from "~/types/system";
import { useThemeStore } from "~/stores/theme";

defineProps<{
  status: SystemStatus | null;
  pending: boolean;
}>();

const themeStore = useThemeStore();
</script>

<template>
  <section
    :class="[
      'rounded-[32px] border p-6 backdrop-blur-xl',
      themeStore.isDark
        ? 'border-aqua/20 bg-panel/70 shadow-glow'
        : 'border-indigo-200 bg-white shadow-xl'
    ]"
  >
    <div class="flex items-start justify-between gap-4">
      <div>
        <p
          :class="[
            'font-mono text-xs uppercase tracking-[0.4em]',
            themeStore.isDark ? 'text-aqua/70' : 'text-indigo-600'
          ]"
        >
          Runtime Pulse
        </p>
        <h2
          :class="[
            'mt-3 font-display text-2xl font-semibold',
            themeStore.isDark ? 'text-white' : 'text-zinc-900'
          ]"
        >
          Ambiente containerizado pronto para evoluir
        </h2>
      </div>
      <div class="pulse-orb" />
    </div>

    <div class="mt-6 grid gap-4 md:grid-cols-3">
      <StatusMetricCard
        label="Database"
        :value="pending ? 'Checking...' : status?.databaseConnected ? 'Online' : 'Offline'"
      />
      <StatusMetricCard
        label="Enabled Modules"
        :value="pending ? '--' : status?.enabledModuleCount ?? 0"
        accent="ember"
      />
      <StatusMetricCard
        label="Environment"
        :value="pending ? '--' : status?.environment ?? 'Unknown'"
      />
    </div>

    <div
      :class="[
        'mt-6 rounded-3xl border p-5',
        themeStore.isDark
          ? 'border-white/10 bg-black/20'
          : 'border-zinc-200 bg-zinc-50'
      ]"
    >
      <p
        :class="[
          'font-mono text-xs uppercase tracking-[0.35em]',
          themeStore.isDark ? 'text-mist/70' : 'text-zinc-500'
        ]"
      >
        First Module
      </p>
      <p
        :class="[
          'mt-3 font-display text-xl',
          themeStore.isDark ? 'text-white' : 'text-zinc-900'
        ]"
      >
        {{ pending ? "Loading foundation..." : status?.firstEnabledModule?.name ?? "No module seeded yet" }}
      </p>
      <p
        :class="[
          'mt-2 text-sm',
          themeStore.isDark ? 'text-mist' : 'text-zinc-600'
        ]"
      >
        {{
          pending
            ? "Nuxt, API and MySQL are negotiating their first handshake."
            : status?.firstEnabledModule?.description ?? "Run the initial migration to populate the technical seed data."
        }}
      </p>
    </div>
  </section>
</template>
