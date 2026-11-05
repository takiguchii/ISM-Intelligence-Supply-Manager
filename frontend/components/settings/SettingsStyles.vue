<template><slot /></template>
<style>
.settings-card { @apply rounded-2xl border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-900/50 p-5 sm:p-6 space-y-5 shadow-sm; }
.settings-card h2 { @apply text-lg font-semibold text-zinc-900 dark:text-white; }
.settings-hint { @apply text-sm text-zinc-500 dark:text-zinc-400 leading-relaxed; }
.settings-grid { @apply grid grid-cols-1 md:grid-cols-2 gap-5; }
.settings-card label { @apply flex flex-col gap-2 text-sm font-medium text-zinc-700 dark:text-zinc-300; }
.settings-card input:not([type=checkbox]), .settings-card select, .settings-card textarea { @apply w-full rounded-xl border border-zinc-300 dark:border-zinc-700 bg-white dark:bg-zinc-950 px-3 py-3 text-sm text-zinc-900 dark:text-zinc-100 focus:outline-none focus:ring-2 focus:ring-indigo-500; }
.settings-primary { @apply inline-flex items-center justify-center rounded-xl px-4 py-3 text-sm font-semibold bg-indigo-600 hover:bg-indigo-500 text-white disabled:opacity-50 disabled:cursor-not-allowed; }
.settings-secondary { @apply inline-flex items-center justify-center rounded-xl border border-zinc-300 dark:border-zinc-700 px-4 py-2 text-sm text-zinc-700 dark:text-zinc-200 hover:bg-zinc-100 dark:hover:bg-zinc-800 disabled:opacity-50 disabled:cursor-not-allowed; }
</style>
