<template>
  <footer class="mt-auto border-t border-zinc-800/80 bg-zinc-950 py-6 text-center text-xs text-zinc-500">
    <div class="max-w-7xl mx-auto px-4 flex flex-col sm:flex-row items-center justify-between gap-3">
      <p>&copy; 2026 ISM — Intelligence Supply Manager. Todos os direitos reservados.</p>
      <div class="flex items-center gap-4 text-zinc-400">
        <NuxtLink to="/" class="hover:text-white transition-colors">Início</NuxtLink>
        <NuxtLink to="/login" class="hover:text-white transition-colors">Login</NuxtLink>
        <a href="http://localhost:8080/swagger" target="_blank" rel="noopener noreferrer" class="hover:text-white transition-colors">Swagger API</a>
      </div>
    </div>
  </footer>
</template>
