<script setup lang="ts">
import { ref, reactive } from "vue";
import { useAuth } from "~/composables/useAuth";

definePageMeta({
  layout: false
});

const { login, loading, error } = useAuth();
const router = useRouter();

const form = reactive({
  username: "",
  password: "",
  rememberMe: false
});

const showPassword = ref(false);
const loginSuccess = ref(false);

const handleLogin = async () => {
  if (!form.username.trim()) return;
  
  try {
    await login({
      username: form.username,
      password: form.password,
      rememberMe: form.rememberMe
    });
    loginSuccess.value = true;
    setTimeout(() => {
      router.push("/");
    }, 1200);
  } catch (e) {
  }
};
</script>

<template>
  <div class="min-h-screen w-full bg-zinc-950 text-zinc-100 flex items-center justify-center relative overflow-hidden font-sans selection:bg-zinc-700 selection:text-white">
    <div class="absolute inset-0 bg-[linear-gradient(to_right,#27272a15_1px,transparent_1px),linear-gradient(to_bottom,#27272a15_1px,transparent_1px)] bg-[size:4rem_4rem] [mask-image:radial-gradient(ellipse_60%_50%_at_50%_50%,#000_70%,transparent_100%)] pointer-events-none"></div>
    
    <div class="absolute -top-40 -left-40 w-96 h-96 bg-zinc-700/20 rounded-full blur-[120px] pointer-events-none"></div>
    <div class="absolute -bottom-40 -right-40 w-96 h-96 bg-zinc-500/10 rounded-full blur-[120px] pointer-events-none"></div>

    <div class="w-full max-w-md px-6 py-12 relative z-10">
      <div class="bg-zinc-900/80 backdrop-blur-xl border border-zinc-800/80 rounded-2xl p-8 shadow-2xl shadow-black/80 transition-all duration-300">
        
        <div class="text-center space-y-3 mb-8">
          <div class="inline-flex items-center justify-center w-14 h-14 rounded-xl bg-zinc-800 border border-zinc-700/60 shadow-inner mb-2">
            <svg class="w-7 h-7 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10"></path>
            </svg>
          </div>
          <div class="inline-block px-3 py-1 rounded-full bg-zinc-800/80 border border-zinc-700/40 text-[10px] uppercase tracking-widest font-mono text-zinc-400">
            Intelligence Supply Manager
          </div>
          <h1 class="text-2xl font-bold tracking-tight text-white">Acesse sua conta</h1>
          <p class="text-sm text-zinc-400">Informe suas credenciais para entrar no sistema ISM</p>
        </div>

        <div v-if="error" class="mb-6 p-4 rounded-xl bg-zinc-900 border border-zinc-700 text-zinc-300 text-sm flex items-start gap-3 animate-shake">
          <svg class="w-5 h-5 text-zinc-400 shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path>
          </svg>
          <span>{{ error }}</span>
        </div>

        <div v-if="loginSuccess" class="mb-6 p-4 rounded-xl bg-zinc-800 border border-zinc-600 text-white text-sm flex items-center gap-3">
          <svg class="w-5 h-5 text-emerald-400 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path>
          </svg>
          <span>Login realizado com sucesso! Redirecionando...</span>
        </div>

        <form @submit.prevent="handleLogin" class="space-y-5">
          <div class="space-y-2">
            <label for="username" class="block text-xs font-semibold uppercase tracking-wider text-zinc-300">
              Usuário
            </label>
            <div class="relative">
              <div class="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-zinc-500">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"></path>
                </svg>
              </div>
              <input
                id="username"
                v-model="form.username"
                type="text"
                required
                placeholder="Digite seu usuário"
                class="w-full pl-11 pr-4 py-3 bg-zinc-950/80 border border-zinc-800 rounded-xl text-zinc-100 placeholder-zinc-500 text-sm focus:outline-none focus:border-zinc-500 focus:ring-1 focus:ring-zinc-500 transition-all duration-200"
              />
            </div>
          </div>

          <div class="space-y-2">
            <div class="flex items-center justify-between">
              <label for="password" class="block text-xs font-semibold uppercase tracking-wider text-zinc-300">
                Senha
              </label>
              <a href="#" @click.prevent class="text-xs text-zinc-400 hover:text-white transition-colors">
                Esqueceu a senha?
              </a>
            </div>
            <div class="relative">
              <div class="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-zinc-500">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"></path>
                </svg>
              </div>
              <input
                id="password"
                v-model="form.password"
                :type="showPassword ? 'text' : 'password'"
                placeholder="••••••••"
                class="w-full pl-11 pr-11 py-3 bg-zinc-950/80 border border-zinc-800 rounded-xl text-zinc-100 placeholder-zinc-500 text-sm focus:outline-none focus:border-zinc-500 focus:ring-1 focus:ring-zinc-500 transition-all duration-200"
              />
              <button
                type="button"
                @click="showPassword = !showPassword"
                class="absolute inset-y-0 right-0 pr-3.5 flex items-center text-zinc-500 hover:text-zinc-300 transition-colors"
                tabindex="-1"
              >
                <svg v-if="!showPassword" class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"></path>
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"></path>
                </svg>
                <svg v-else class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858-5.908a10.04 10.04 0 012.122-.363c4.478 0 8.268 2.943 9.542 7a10.025 10.025 0 01-4.132 5.411m-6.165-4.451a3 3 0 104.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l18 18"></path>
                </svg>
              </button>
            </div>
          </div>

          <div class="flex items-center justify-between pt-1">
            <label class="flex items-center gap-2.5 cursor-pointer">
              <input
                type="checkbox"
                v-model="form.rememberMe"
                class="w-4 h-4 rounded bg-zinc-950 border-zinc-700 text-zinc-100 focus:ring-zinc-500 focus:ring-offset-zinc-900 cursor-pointer"
              />
              <span class="text-xs text-zinc-400 select-none">Lembrar de mim</span>
            </label>
          </div>

          <button
            type="submit"
            :disabled="loading"
            class="w-full py-3.5 px-4 bg-white hover:bg-zinc-200 active:bg-zinc-300 text-zinc-950 font-semibold rounded-xl text-sm transition-all duration-200 flex items-center justify-center gap-2 shadow-lg shadow-white/5 focus:outline-none focus:ring-2 focus:ring-zinc-400 focus:ring-offset-2 focus:ring-offset-zinc-900 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            <svg v-if="loading" class="animate-spin -ml-1 mr-2 h-4 w-4 text-zinc-950" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
            </svg>
            <span>{{ loading ? "Acessando..." : "Entrar no Sistema" }}</span>
          </button>
        </form>

        <div class="mt-8 pt-6 border-t border-zinc-800/80 text-center">
          <p class="text-xs text-zinc-500">
            &copy; 2026 ISM — Intelligence Supply Manager. Todos os direitos reservados.
          </p>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
@keyframes shake {
  0%, 100% { transform: translateX(0); }
  20%, 60% { transform: translateX(-4px); }
  40%, 80% { transform: translateX(4px); }
}
.animate-shake {
  animation: shake 0.4s ease-in-out;
}
</style>
