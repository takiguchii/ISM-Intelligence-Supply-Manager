<script setup lang="ts">
import { computed } from "vue";
import { ElMessage } from "element-plus";

const props = defineProps<{
    icon: string;
    agent: string;
    severity: "info" | "warning" | "critical";
    title: string;
    /** Pode conter <strong> para destacar trechos. */
    description: string;
    primaryLabel?: string;
    primarySuccessMessage?: string;
    secondaryLabel?: string;
}>();

const severityMap = {
    info: {
      color: "text-sky-700 dark:text-sky-400",
      bg: "bg-sky-50 dark:bg-sky-400/10",
      border: "border-sky-200 dark:border-sky-400/30"
    },
    warning: {
      color: "text-amber-700 dark:text-amber-400",
      bg: "bg-amber-50 dark:bg-amber-400/10",
      border: "border-amber-200 dark:border-amber-400/30"
    },
    critical: {
      color: "text-red-700 dark:text-red-400",
      bg: "bg-red-50 dark:bg-red-400/10",
      border: "border-red-200 dark:border-red-400/30"
    }
} as const;

const current = computed(() => severityMap[props.severity]);

const handlePrimary = () => {
    if (props.primarySuccessMessage) {
        ElMessage.success(props.primarySuccessMessage);
    }
};
</script>

<template>
    <div class="p-6 rounded-2xl bg-white dark:bg-zinc-900/80 border border-zinc-200 dark:border-zinc-800/80 shadow-sm dark:shadow-lg space-y-4 h-full flex flex-col transition-colors duration-200">
        <div class="flex items-center justify-between">
            <span class="w-10 h-10 rounded-xl flex items-center justify-center border"
                :class="[current.bg, current.border, current.color]">
                <el-icon :size="18">
                    <component :is="icon" />
                </el-icon>
            </span>
            <span class="text-[10px] font-mono uppercase tracking-wider px-2.5 py-1 rounded-full border"
                :class="[current.color, current.bg, current.border]">
                {{ agent }}
            </span>
        </div>

        <div class="space-y-1.5 flex-1">
            <h3 class="text-sm font-bold text-zinc-900 dark:text-white">{{ title }}</h3>
            <!-- eslint-disable-next-line vue/no-v-html -->
            <p class="text-xs text-zinc-600 dark:text-zinc-400 leading-relaxed" v-html="description"></p>
        </div>

        <div v-if="primaryLabel || secondaryLabel" class="flex items-center gap-2 pt-3 border-t border-zinc-200 dark:border-zinc-800/60">
            <button v-if="primaryLabel" type="button" @click="handlePrimary"
                class="px-3 py-1.5 text-xs font-semibold rounded-lg bg-indigo-600 hover:bg-indigo-700 text-white transition-colors">
                {{ primaryLabel }}
            </button>
            <button v-if="secondaryLabel" type="button"
                class="px-3 py-1.5 text-xs font-medium rounded-lg bg-zinc-100 hover:bg-zinc-200 dark:bg-zinc-800 dark:hover:bg-zinc-700 text-zinc-700 dark:text-zinc-200 border border-zinc-300 dark:border-zinc-700/60 transition-colors">
                {{ secondaryLabel }}
            </button>
        </div>
    </div>
</template>