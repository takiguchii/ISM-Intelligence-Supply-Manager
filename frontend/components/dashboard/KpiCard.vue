<script setup lang="ts">
import { computed } from "vue";

const props = defineProps<{
    label: string;
    value: string;
    delta: string;
    trend: "up" | "down";
    icon: string;
    hint?: string;
}>();

const isUp = computed(() => props.trend === "up");
</script>

<template>
    <div class="p-6 rounded-2xl bg-zinc-900/80 border border-zinc-800/80 shadow-lg space-y-4 h-full">
        <div class="flex items-center justify-between">
            <span class="text-xs font-mono uppercase tracking-wider text-zinc-400">{{ label }}</span>
            <span
                class="w-9 h-9 rounded-xl bg-zinc-800/80 border border-zinc-700/60 flex items-center justify-center text-zinc-300">
                <el-icon :size="16">
                    <component :is="icon" />
                </el-icon>
            </span>
        </div>

        <p class="text-2xl font-bold text-white">{{ value }}</p>

        <div class="flex items-center gap-2 flex-wrap">
            <span class="inline-flex items-center gap-1 text-xs font-mono px-2 py-0.5 rounded-full border" :class="isUp
                ? 'text-emerald-400 border-emerald-400/30 bg-emerald-400/10'
                : 'text-red-400 border-red-400/30 bg-red-400/10'">
                <el-icon :size="10">
                    <component :is="isUp ? 'ArrowUpBold' : 'ArrowDownBold'" />
                </el-icon>
                {{ delta }}
            </span>
            <span v-if="hint" class="text-xs text-zinc-500">{{ hint }}</span>
        </div>
    </div>
</template>