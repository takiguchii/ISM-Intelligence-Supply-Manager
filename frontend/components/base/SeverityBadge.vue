<script setup lang="ts">
import type { InsightSeverity } from "~/composables/useDashboardMetrics";

defineProps<{
  severity: InsightSeverity;
}>();

const labels: Record<InsightSeverity, string> = {
  info: "Informativo",
  warning: "Atenção",
  critical: "Crítico"
};
</script>

<template>
  <span
    :class="`severity-${severity}`"
    class="inline-flex shrink-0 items-center gap-1.5 rounded-full border border-severity-line bg-severity-soft px-2 py-1 font-mono text-[10px] font-semibold uppercase tracking-wider text-severity"
  >
    <span class="h-1.5 w-1.5 rounded-full bg-severity" aria-hidden="true"></span>
    {{ labels[severity] }}
  </span>
</template>
