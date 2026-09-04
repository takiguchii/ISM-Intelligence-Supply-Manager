<script setup lang="ts">
import { useAuthStore } from "~/stores/auth";
import type { FetchError } from "ofetch";
import type { ApiError } from "~/types/auth";

const authStore = useAuthStore();
const route = useRoute();
const router = useRouter();
const runtimeConfig = useRuntimeConfig();

const loginForm = ref({
  email: "",
  password: ""
});

const submitting = ref(false);
const errorMessage = ref("");

const redirectPath = computed(() => {
  const redirect = route.query.redirect as string;
  return redirect && redirect.startsWith("/") ? redirect : "/";
});

onMounted(() => {
  authStore.initFromStorage();
  if (authStore.isAuthenticated) {
    router.push(redirectPath.value);
  }
});

const handleExtractError = (err: unknown): string => {
  const fetchError = err as FetchError;
  const data = fetchError.data as ApiError | { message?: string } | undefined;
  if (!data) return "Erro desconhecido. Tente novamente.";
  if ("message" in data && data.message) return data.message;
  if ("detail" in data && data.detail) return data.detail;
  if (fetchError.status === 401) return "E-mail ou senha inválidos.";
  if (fetchError.status === 400) return "Verifique os dados informados.";
  return "Erro ao processar solicitação. Tente novamente.";
};

const handleLogin = async () => {
  errorMessage.value = "";
  if (!loginForm.value.email) {
    errorMessage.value = "E-mail é obrigatório.";
    return;
  }
  if (!loginForm.value.password) {
    errorMessage.value = "Senha é obrigatória.";
    return;
  }

  submitting.value = true;
  try {
    await authStore.login({
      email: loginForm.value.email,
      password: loginForm.value.password
    });
    router.push(redirectPath.value);
  } catch (err) {
    errorMessage.value = handleExtractError(err);
  } finally {
    submitting.value = false;
  }
};
</script>

<template>
  <main class="relative min-h-screen bg-zinc-950 text-zinc-100 flex items-center justify-center overflow-hidden font-sans selection:bg-zinc-800 selection:text-white py-10 px-4">
    <!-- Glow Background Effects (Monocromático Premium) -->
    <div class="absolute -top-40 -left-40 w-96 h-96 bg-indigo-600/10 rounded-full blur-3xl pointer-events-none"></div>
    <div class="absolute -bottom-40 -right-40 w-96 h-96 bg-zinc-700/10 rounded-full blur-3xl pointer-events-none"></div>

    <section class="relative z-10 w-full max-w-md flex flex-col items-center">
      <!-- Title Header -->
      <div class="mb-6 text-center space-y-2">
        <div class="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-zinc-900/90 border border-zinc-800 text-xs font-mono text-zinc-300 shadow-md">
          <span class="w-2 h-2 rounded-full bg-indigo-400 animate-pulse"></span>
          <span class="font-mono text-xs uppercase tracking-widest text-zinc-400">Autenticação</span>
        </div>
        <h1 class="text-3xl font-bold text-white tracking-tight">
          {{ runtimeConfig.public.appName }}
        </h1>
        <p class="text-zinc-400 text-sm">
          Faça login para acessar o painel de gestão
        </p>
      </div>

      <!-- Error Alert -->
      <div v-if="errorMessage" class="w-[340px] mb-4">
        <el-alert
          type="error"
          :closable="false"
          class="rounded-xl"
        >
          {{ errorMessage }}
        </el-alert>
      </div>

      <!-- Vibrant Gradient Animated Login Card -->
      <form class="login wrap shadow-2xl border border-zinc-700/60" @submit.prevent="handleLogin">
        <div class="h1">Login</div>
        <input
          v-model="loginForm.email"
          placeholder="Email"
          id="email"
          name="email"
          type="email"
          autocomplete="email"
        >
        <input
          v-model="loginForm.password"
          placeholder="Password"
          id="password"
          name="password"
          type="password"
          autocomplete="current-password"
        >
        <button type="submit" class="btn" :disabled="submitting">
          {{ submitting ? 'Aguarde...' : 'Login' }}
        </button>
      </form>

      <!-- Default User Hint -->
      <div class="mt-6 text-center text-xs font-mono text-zinc-500">
        <p>Usuário padrão: <span class="text-zinc-300 font-semibold">admin@ism.com.br</span> / <span class="text-zinc-300 font-semibold">admin123</span></p>
      </div>
    </section>
  </main>
</template>

<style scoped>
.login {
  width: 340px;
  height: 400px;
  background: #2c2c2c;
  padding: 47px;
  padding-bottom: 57px;
  color: #fff;
  border-radius: 17px;
  font-size: 1.3em;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, 'Open Sans', 'Helvetica Neue', sans-serif;
  box-sizing: border-box;
}

.login input[type="text"],
.login input[type="email"],
.login input[type="password"] {
  opacity: 1;
  display: block;
  border: none;
  outline: none;
  width: 100%;
  padding: 13px 18px;
  margin: 20px 0 0 0;
  font-size: 0.8em;
  border-radius: 100px;
  background: #3c3c3c;
  color: #fff;
  box-sizing: border-box;
}

.login input:focus {
  animation: bounce 1s;
  -webkit-appearance: none;
}

.login input[type=submit],
.login input[type=button],
.login button[type=submit],
.h1 {
  border: 0;
  outline: 0;
  width: 100%;
  padding: 13px;
  margin: 35px 0 0 0;
  border-radius: 500px;
  font-weight: 600;
  animation: bounce2 1.6s;
  box-sizing: border-box;
}

.h1 {
  padding: 0;
  position: relative;
  top: -35px;
  display: block;
  margin-bottom: -0px;
  font-size: 1.3em;
  font-weight: 700;
}

.btn {
  background: linear-gradient(144deg, #af40ff, #5b42f3 50%, #00ddeb);
  color: #fff;
  padding: 16px !important;
  font-size: 0.9em;
  cursor: pointer;
  transition: all 0.4s ease;
}

.btn:hover {
  background: linear-gradient(144deg, #1e1e1e , 20%,#1e1e1e 50%,#1e1e1e );
  color: rgb(255, 255, 255);
  padding: 16px !important;
  cursor: pointer;
  transition: all 0.4s ease;
}

.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.login input[type=text],
.login input[type=email] {
  animation: bounce 1s;
  -webkit-appearance: none;
}

.login input[type=password] {
  animation: bounce1 1.3s;
}

.ui {
  font-weight: bolder;
  background: -webkit-linear-gradient(#B563FF, #535EFC, #0EC8EE);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  border-bottom: 4px solid transparent;
  border-image: linear-gradient(0.25turn, #535EFC, #0EC8EE, #0EC8EE);
  border-image-slice: 1;
  display: inline;
}

@media only screen and (max-width: 600px) {
  .login {
    width: 85%;
    padding: 2.5em;
  }
}

@keyframes bounce {
  0% {
    transform: translateY(-250px);
    opacity: 0;
  }
}

@keyframes bounce1 {
  0% {
    opacity: 0;
  }

  40% {
    transform: translateY(-100px);
    opacity: 0;
  }
}

@keyframes bounce2 {
  0% {
    opacity: 0;
  }

  70% {
    transform: translateY(-20px);
    opacity: 0;
  }
}
</style>
