<script setup lang="ts">
import { useAuthStore } from "~/stores/auth";
import { useThemeStore } from "~/stores/theme";
import type { FetchError } from "ofetch";
import type { ApiError } from "~/types/auth";

const authStore = useAuthStore();
const themeStore = useThemeStore();
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
  <main
    :class="[
      'relative min-h-screen flex items-center justify-center overflow-hidden font-sans py-10 px-4 transition-colors duration-200',
      themeStore.isDark
        ? 'bg-zinc-950 text-zinc-100 selection:bg-zinc-800 selection:text-white'
        : 'bg-zinc-50 text-zinc-900 selection:bg-indigo-100 selection:text-indigo-900'
    ]"
  >
    <!-- Glow Background Effects (Monocromático Premium) -->
    <div
      :class="[
        'absolute -top-40 -left-40 w-96 h-96 rounded-full blur-3xl pointer-events-none',
        themeStore.isDark ? 'bg-indigo-600/10' : 'bg-indigo-400/10'
      ]"
    ></div>
    <div
      :class="[
        'absolute -bottom-40 -right-40 w-96 h-96 rounded-full blur-3xl pointer-events-none',
        themeStore.isDark ? 'bg-zinc-700/10' : 'bg-zinc-300/30'
      ]"
    ></div>

    <section class="relative z-10 w-full max-w-md flex flex-col items-center">
      <!-- Title Header -->
      <div class="mb-6 text-center space-y-2">
        <div
          :class="[
            'inline-flex items-center gap-2 px-3 py-1 rounded-full text-xs font-mono shadow-md border',
            themeStore.isDark
              ? 'bg-zinc-900/90 border-zinc-800 text-zinc-300'
              : 'bg-white border-zinc-200 text-zinc-600'
          ]"
        >
          <span
            :class="[
              'w-2 h-2 rounded-full animate-pulse',
              themeStore.isDark ? 'bg-indigo-400' : 'bg-indigo-600'
            ]"
          ></span>
          <span
            :class="[
              'font-mono text-xs uppercase tracking-widest',
              themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500'
            ]"
          >Autenticação</span>
        </div>
        <h1
          :class="[
            'text-3xl font-bold tracking-tight',
            themeStore.isDark ? 'text-white' : 'text-zinc-900'
          ]"
        >
          {{ runtimeConfig.public.appName }}
        </h1>
        <p :class="['text-sm', themeStore.isDark ? 'text-zinc-400' : 'text-zinc-500']">
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
      <div
        :class="[
          'mt-6 text-center text-xs font-mono',
          themeStore.isDark ? 'text-zinc-500' : 'text-zinc-500'
        ]"
      >
        <p>Usuário padrão: <span :class="['font-semibold', themeStore.isDark ? 'text-zinc-300' : 'text-zinc-700']">admin@ism.com.br</span> / <span :class="['font-semibold', themeStore.isDark ? 'text-zinc-300' : 'text-zinc-700']">admin123</span></p>
      </div>
    </section>
  </main>
</template>

<style scoped>
:deep(.dark) .login {
  background: #1f1f23;
  color: #fff;
  border: 1px solid rgba(63, 63, 70, 0.6);
}
:deep(.light) .login {
  background: #ffffff;
  color: #18181b;
  border: 1px solid rgba(228, 228, 231, 0.9);
  box-shadow: 0 20px 45px -15px rgba(0, 0, 0, 0.1);
}
.login {
  width: 340px;
  height: 400px;
  padding: 47px;
  padding-bottom: 57px;
  border-radius: 17px;
  font-size: 1.3em;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, 'Open Sans', 'Helvetica Neue', sans-serif;
  box-sizing: border-box;
}

:deep(.dark) .login input[type="text"],
:deep(.dark) .login input[type="email"],
:deep(.dark) .login input[type="password"] {
  background: #27272a;
  color: #fff;
  border: 1px solid rgba(63, 63, 70, 0.5);
}
:deep(.light) .login input[type="text"],
:deep(.light) .login input[type="email"],
:deep(.light) .login input[type="password"] {
  background: #fafafa;
  color: #18181b;
  border: 1px solid rgba(228, 228, 231, 0.9);
}
.login input[type="text"],
.login input[type="email"],
.login input[type="password"] {
  opacity: 1;
  display: block;
  outline: none;
  width: 100%;
  padding: 13px 18px;
  margin: 20px 0 0 0;
  font-size: 0.8em;
  border-radius: 100px;
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
  background: linear-gradient(135deg, #6366f1 0%, #4f46e5 50%, #4338ca 100%);
  color: #fff;
  padding: 16px !important;
  font-size: 0.9em;
  cursor: pointer;
  transition: all 0.3s ease;
  box-shadow: 0 4px 14px -4px rgba(99, 102, 241, 0.45);
}

.btn:hover {
  background: linear-gradient(135deg, #4f46e5 0%, #4338ca 50%, #3730a3 100%);
  color: #fff;
  padding: 16px !important;
  cursor: pointer;
  transition: all 0.3s ease;
  box-shadow: 0 6px 20px -4px rgba(79, 70, 229, 0.55);
}

.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
  box-shadow: none;
}

.login input[type=text],
.login input[type=email] {
  animation: bounce 1s;
  -webkit-appearance: none;
}

.login input[type=password] {
  animation: bounce1 1.3s;
}

:deep(.dark) .ui {
  font-weight: bolder;
  background: -webkit-linear-gradient(#818cf8, #6366f1, #4f46e5);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
}
:deep(.light) .ui {
  font-weight: bolder;
  background: -webkit-linear-gradient(#4f46e5, #4338ca, #3730a3);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
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
