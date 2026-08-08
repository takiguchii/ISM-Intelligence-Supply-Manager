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
    <!-- Glow Background Effects -->
    <div class="absolute -top-40 -left-40 w-96 h-96 bg-zinc-800/20 rounded-full blur-3xl pointer-events-none"></div>
    <div class="absolute -bottom-40 -right-40 w-96 h-96 bg-emerald-500/10 rounded-full blur-3xl pointer-events-none"></div>

    <section class="relative z-10 w-full max-w-md flex flex-col items-center">
      <!-- Title Header -->
      <div class="mb-6 text-center space-y-2">
        <div class="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-zinc-900/90 border border-zinc-800 text-xs font-mono text-zinc-300 shadow-md">
          <span class="w-2 h-2 rounded-full bg-emerald-400 animate-pulse"></span>
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
      <div v-if="errorMessage" class="w-[320px] mb-4">
        <el-alert
          type="error"
          :closable="false"
          class="rounded-xl"
        >
          {{ errorMessage }}
        </el-alert>
      </div>

      <!-- Flip Card Switch Wrapper -->
      <div class="card-switch">
        <label class="switch">
          <input class="toggle" type="checkbox">
          <span class="slider"></span>
          <span class="card-side"></span>
          
          <div class="flip-card__inner">
            <!-- Front (Log in) -->
            <div class="flip-card__front">
              <div class="title">Log in</div>
              <form @submit.prevent="handleLogin" class="flip-card__form">
                <input
                  v-model="loginForm.email"
                  type="email"
                  placeholder="Email"
                  name="email"
                  class="flip-card__input"
                  autocomplete="email"
                >
                <input
                  v-model="loginForm.password"
                  type="password"
                  placeholder="Password"
                  name="password"
                  class="flip-card__input"
                  autocomplete="current-password"
                >
                <button type="submit" class="flip-card__btn" :disabled="submitting">
                  {{ submitting ? 'Aguarde...' : "Let`s go!" }}
                </button>
              </form>
            </div>

            <!-- Back (Sign up - Disabled) -->
            <div class="flip-card__back">
              <div class="title">Sign up</div>
              <form @submit.prevent class="flip-card__form">
                <input type="text" placeholder="Name" class="flip-card__input" disabled>
                <input type="email" placeholder="Email" name="email" class="flip-card__input" disabled>
                <input type="password" placeholder="Password" name="password" class="flip-card__input" disabled>
                <button type="button" class="flip-card__btn opacity-50 cursor-not-allowed" title="Cadastro desativado">Confirm!</button>
              </form>
            </div>
          </div>
        </label>
      </div>

      <!-- Default User Hint -->
      <div class="mt-6 text-center text-xs font-mono text-zinc-500">
        <p>Usuário padrão: <span class="text-zinc-300 font-semibold">admin@ism.com.br</span> / <span class="text-zinc-300 font-semibold">admin123</span></p>
      </div>
    </section>
  </main>
</template>

<style scoped>
.card-switch {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
}

.switch {
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: center;
  width: 50px;
  height: 20px;
  cursor: pointer;
}

.card-side {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  pointer-events: none;
}

.card-side::before {
  position: absolute;
  content: 'Log in';
  left: -75px;
  top: -1px;
  width: 65px;
  text-align: right;
  text-decoration: underline;
  color: #fefefe;
  font-weight: 600;
  font-size: 14px;
  white-space: nowrap;
}

.card-side::after {
  position: absolute;
  content: 'Sign up';
  left: 60px;
  top: -1px;
  width: 65px;
  text-align: left;
  text-decoration: none;
  color: #fefefe;
  font-weight: 600;
  font-size: 14px;
  white-space: nowrap;
}

.toggle {
  display: none;
}

.slider {
  box-sizing: border-box;
  border-radius: 5px;
  border: 2px solid #fefefe;
  box-shadow: 2px 2px 0px #fefefe;
  position: absolute;
  cursor: pointer;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: #111111;
  transition: 0.3s;
}

.slider:before {
  box-sizing: border-box;
  position: absolute;
  content: "";
  height: 14px;
  width: 14px;
  border: 2px solid #fefefe;
  border-radius: 3px;
  left: 1px;
  bottom: 1px;
  background-color: #111111;
  box-shadow: 0 2px 0 #fefefe;
  transition: 0.3s;
}

.toggle:checked + .slider {
  background-color: #2d8cf0;
}

.toggle:checked + .slider:before {
  transform: translateX(26px);
}

.toggle:checked ~ .card-side:before {
  text-decoration: none;
}

.toggle:checked ~ .card-side:after {
  text-decoration: underline;
}

/* 3D Flip Card Container */
.flip-card__inner {
  width: 320px;
  height: 380px;
  position: relative;
  background-color: transparent;
  perspective: 1000px;
  text-align: center;
  transition: transform 0.8s;
  transform-style: preserve-3d;
  margin-top: 45px;
}

.toggle:checked ~ .flip-card__inner {
  transform: rotateY(180deg);
}

.flip-card__front,
.flip-card__back {
  padding: 30px 20px;
  position: absolute;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  -webkit-backface-visibility: hidden;
  backface-visibility: hidden;
  background: #111111;
  gap: 20px;
  border-radius: 8px;
  border: 2px solid #ffffff;
  box-shadow: 4px 4px 0px #ffffff;
  width: 100%;
  height: 100%;
  top: 0;
  left: 0;
  box-sizing: border-box;
}

.flip-card__back {
  transform: rotateY(180deg);
}

.flip-card__form {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 18px;
  width: 100%;
}

.title {
  margin-bottom: 5px;
  font-size: 28px;
  font-weight: 900;
  text-align: center;
  color: #ffffff;
  letter-spacing: -0.5px;
}

.flip-card__input {
  width: 250px;
  height: 42px;
  border-radius: 6px;
  border: 2px solid #ffffff;
  background-color: #111111;
  box-shadow: 4px 4px 0px #ffffff;
  font-size: 14px;
  font-weight: 600;
  color: #ffffff;
  padding: 0 12px;
  outline: none;
  box-sizing: border-box;
  transition: all 0.2s ease;
}

.flip-card__input::placeholder {
  color: #7e7e7e;
  opacity: 0.9;
}

.flip-card__input:focus {
  border: 2px solid #2d8cf0;
  box-shadow: 4px 4px 0px #2d8cf0;
}

.flip-card__btn {
  margin-top: 10px;
  width: 130px;
  height: 42px;
  border-radius: 6px;
  border: 2px solid #ffffff;
  background-color: #111111;
  box-shadow: 4px 4px 0px #ffffff;
  font-size: 16px;
  font-weight: 700;
  color: #ffffff;
  cursor: pointer;
  transition: all 0.15s ease;
}

.flip-card__btn:active {
  box-shadow: 0px 0px 0px #ffffff;
  transform: translate(4px, 4px);
}
</style>
