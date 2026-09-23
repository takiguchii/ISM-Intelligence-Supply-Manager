<script setup lang="ts">
defineProps<{
    eyebrow?: string;
    title: string;
    description?: string;
}>();
</script>

<template>
    <div class="flex flex-col lg:flex-row lg:items-end lg:justify-between gap-4">
        <div class="space-y-3 max-w-2xl">
            <div v-if="eyebrow"
                class="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-zinc-100 dark:bg-zinc-800/80 border border-zinc-300 dark:border-zinc-700/60 text-xs font-mono text-zinc-700 dark:text-zinc-300">
                <span class="w-2 h-2 rounded-full bg-emerald-400 animate-pulse"></span>
                {{ eyebrow }}
            </div>
            <h1 class="text-2xl sm:text-3xl font-bold text-zinc-900 dark:text-white tracking-tight">{{ title }}</h1>
            <p v-if="description" class="text-zinc-600 dark:text-zinc-400 text-sm sm:text-base leading-relaxed">
                {{ description }}
            </p>
        </div>
        <div v-if="$slots.actions" class="flex flex-wrap gap-3 shrink-0">
            <slot name="actions" />
        </div>
    </div>
</template>