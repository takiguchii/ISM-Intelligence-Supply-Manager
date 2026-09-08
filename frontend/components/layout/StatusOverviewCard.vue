<script setup lang="ts">
import type { SystemStatus } from "~/types/system";

defineProps<{
  status: SystemStatus | null;
  pending: boolean;
}>();
</script>

<template>
  <section class="rounded-[32px] border border-aqua/20 bg-panel/70 p-6 shadow-glow backdrop-blur-xl">
    <div class="flex items-start justify-between gap-4">
      <div>
        <p class="font-mono text-xs uppercase tracking-[0.4em] text-aqua/70">
          Runtime Pulse
        </p>
        <h2 class="mt-3 font-display text-2xl font-semibold text-white">
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

    <div class="mt-6 rounded-3xl border border-white/10 bg-black/20 p-5">
      <p class="font-mono text-xs uppercase tracking-[0.35em] text-mist/70">
        First Module
      </p>
      <p class="mt-3 font-display text-xl text-white">
        {{ pending ? "Loading foundation..." : status?.firstEnabledModule?.name ?? "No module seeded yet" }}
      </p>
      <p class="mt-2 text-sm text-mist">
        {{
          pending
            ? "Nuxt, API and MySQL are negotiating their first handshake."
            : status?.firstEnabledModule?.description ?? "Run the initial migration to populate the technical seed data."
        }}
      </p>
    </div>
  </section>
</template>
