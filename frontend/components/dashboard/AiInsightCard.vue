<script setup lang="ts">
import { computed } from "vue";
import { ElMessage } from "element-plus";

const props = defineProps<{
    icon: string;
    agent: string;
    severity: "info" | "warning" | "critical";
    title: string;
    /** Pode conter <strong> para destacar trechos (conteúdo controlado internamente, não vindo do usuário). */
    description: string;
    primaryLabel?: string;
    primarySuccessMessage?: string;
    secondaryLabel?: string;
}>();

const severityMap = {
    info: { color: "text-sky-400", bg: "bg-sky-400/10", border: "border-sky-400/30" },
    warning: { color: "text-amber-400", bg: "bg-amber-400/10", border: "border-amber-400/30" },
    critical: { color: "text-red-400", bg: "bg-red-400/10", border: "border-red-400/30" }
} as const;

const current = computed(() => severityMap[props.severity]);

const handlePrimary = () => {
    if (props.primarySuccessMessage) {
        ElMessage.success(props.primarySuccessMessage);
    }
};
</script>

<template>
    <div class="p-6 rounded-2xl bg-zinc-900/80 border border-zinc-800/80 shadow-lg space-y-4 h-full flex flex-col">
        <div class="flex items-center justify-between">
            <span class="w-10 h-10 rounded-xl flex items-center justify-center border"
                :class="[current.bg, current.border, current.color]">
                <el-icon :size="18">
                    <component :is="icon" />
                </el-icon>
            </span>
            <span class="text-[10px] font-mono uppercase tracking-wider px-2 py-1 rounded-full border"
                :class="[current.color, current.bg, current.border]">
                {{ agent }}
            </span>
        </div>

        <div class="space-y-1.5 flex-1">
            <h3 class="text-sm font-semibold text-white">{{ title }}</h3>
            <!-- eslint-disable-next-line vue/no-v-html -->
            <p class="text-xs text-zinc-400 leading-relaxed" v-html="description"></p>
        </div>

        <div v-if="primaryLabel || secondaryLabel" class="flex items-center gap-2 pt-3 border-t border-zinc-800/60">
            <button v-if="primaryLabel" type="button" @click="handlePrimary"
                class="px-3 py-1.5 text-xs font-semibold rounded-lg bg-white hover:bg-zinc-200 text-zinc-950 transition-colors">
                {{ primaryLabel }}
            </button>
            <button v-if="secondaryLabel" type="button"
                class="px-3 py-1.5 text-xs font-medium rounded-lg bg-zinc-800 hover:bg-zinc-700 text-zinc-200 border border-zinc-700/60 transition-colors">
                {{ secondaryLabel }}
            </button>
        </div>
    </div>
</template>