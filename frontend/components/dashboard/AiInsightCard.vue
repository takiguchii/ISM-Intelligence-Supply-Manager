<script setup lang="ts">
import { ElMessage } from "element-plus";
import SeverityBadge from "~/components/base/SeverityBadge.vue";
import type { InsightSeverity } from "~/composables/useDashboardMetrics";

const props = defineProps<{
    icon: string;
    agent: string;
    severity: InsightSeverity;
    title: string;
    /** Pode conter <strong> para destacar trechos (conteúdo controlado internamente, não vindo do usuário). */
    description: string;
    primaryLabel?: string;
    primarySuccessMessage?: string;
    secondaryLabel?: string;
}>();

const handlePrimary = () => {
    if (props.primarySuccessMessage) {
        ElMessage.success(props.primarySuccessMessage);
    }
};
</script>

<template>
    <div class="p-6 rounded-2xl bg-white dark:bg-zinc-900/80 border border-zinc-200 dark:border-zinc-800/80 shadow-sm dark:shadow-lg space-y-4 h-full flex flex-col"
        :class="`severity-${severity}`">
        <div class="flex items-center justify-between gap-3">
            <div class="flex items-center gap-3 min-w-0">
                <span
                    class="w-10 h-10 shrink-0 rounded-xl flex items-center justify-center border border-severity-line bg-severity-soft text-severity">
                    <el-icon :size="18">
                        <component :is="icon" />
                    </el-icon>
                </span>
                <span class="text-[10px] font-mono uppercase tracking-wider text-zinc-500 truncate">
                    {{ agent }}
                </span>
            </div>
            <SeverityBadge :severity="severity" />
        </div>

        <div class="space-y-1.5 flex-1">
            <h3 class="text-sm font-semibold text-zinc-900 dark:text-white">{{ title }}</h3>
            <!-- eslint-disable-next-line vue/no-v-html -->
            <p class="text-xs text-zinc-600 dark:text-zinc-400 leading-relaxed" v-html="description"></p>
        </div>

        <div v-if="primaryLabel || secondaryLabel" class="flex items-center gap-2 pt-3 border-t border-zinc-200 dark:border-zinc-800/60">
            <button v-if="primaryLabel" type="button" @click="handlePrimary"
                class="px-3 py-1.5 text-xs font-semibold rounded-lg bg-zinc-900 dark:bg-white hover:bg-zinc-800 dark:hover:bg-zinc-200 text-white dark:text-zinc-950 transition-colors">
                {{ primaryLabel }}
            </button>
            <button v-if="secondaryLabel" type="button"
                class="px-3 py-1.5 text-xs font-medium rounded-lg bg-zinc-100 dark:bg-zinc-800 hover:bg-zinc-200 dark:hover:bg-zinc-700 text-zinc-800 dark:text-zinc-200 border border-zinc-300 dark:border-zinc-700/60 transition-colors">
                {{ secondaryLabel }}
            </button>
        </div>
    </div>
</template>