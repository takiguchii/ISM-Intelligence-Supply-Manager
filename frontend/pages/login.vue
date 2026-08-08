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
  <main class="relative min-h-screen bg-zinc-950 text-zinc-100 flex items-center justify-center overflow-hidden font-sans selection:bg-zinc-800 selection:text-white py-12 px-4">
    <!-- Glow Background Effects -->
    <div class="absolute -top-40 -left-40 w-96 h-96 bg-zinc-800/20 rounded-full blur-3xl pointer-events-none"></div>
    <div class="absolute -bottom-40 -right-40 w-96 h-96 bg-emerald-500/10 rounded-full blur-3xl pointer-events-none"></div>

    <section class="relative z-10 w-full max-w-md flex flex-col items-center">
      <!-- Title Header -->
      <div class="mb-8 text-center space-y-3">
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
      <div v-if="errorMessage" class="w-[300px] mb-6">
        <el-alert
          type="error"
          :closable="false"
          class="rounded-xl"
        >
          {{ errorMessage }}
        </el-alert>
      </div>

      <!-- Flip Card Form Component -->
      <div class="wrapper">
        <div class="card-switch">
          <label class="switch">
            <input class="toggle" type="checkbox">
            <span class="slider"></span>
            <span class="card-side"></span>
            <div class="flip-card__inner">
              <!-- Log in Side -->
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

              <!-- Sign up Side (Sem função / Desativado) -->
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
      </div>

      <!-- Footer Note -->
      <div class="mt-8 text-center text-xs font-mono text-zinc-500">
        <p>Usuário padrão: <span class="text-zinc-300 font-semibold">admin@ism.com.br</span> / <span class="text-zinc-300 font-semibold">admin123</span></p>
      </div>
    </section>
  </main>
</template>

<style scoped>
  .wrapper {
    --input-focus: #2d8cf0;
    --font-color: #fefefe;
    --font-color-sub: #7e7e7e;
    --bg-color: #111;
    --bg-color-alt: #7e7e7e;
    --main-color: #fefefe;
    display: flex;
    flex-direction: column;
    align-items: center;
  }
  
  .switch {
    position: relative;
    display: flex;
    flex-direction: column;
    justify-content: center;
    align-items: center;
    gap: 30px;
    width: 50px;
    height: 20px;
  }

  .card-side::before {
    position: absolute;
    content: 'Log in';
    left: -70px;
    top: 0;
    width: 100px;
    text-decoration: underline;
    color: var(--font-color);
    font-weight: 600;
  }

  .card-side::after {
    position: absolute;
    content: 'Sign up';
    left: 70px;
    top: 0;
    width: 100px;
    text-decoration: none;
    color: var(--font-color);
    font-weight: 600;
  }

  .toggle {
    opacity: 0;
    width: 0;
    height: 0;
  }

  .slider {
    box-sizing: border-box;
    border-radius: 5px;
    border: 2px solid var(--main-color);
    box-shadow: 4px 4px var(--main-color);
    position: absolute;
    cursor: pointer;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background-color: var(--bg-color);
    transition: 0.3s;
  }

  .slider:before {
    box-sizing: border-box;
    position: absolute;
    content: "";
    height: 20px;
    width: 20px;
    border: 2px solid var(--main-color);
    border-radius: 5px;
    left: -2px;
    bottom: 2px;
    background-color: var(--bg-color);
    box-shadow: 0 3px 0 var(--main-color);
    transition: 0.3s;
  }

  .toggle:checked + .slider {
    background-color: var(--input-focus);
  }

  .toggle:checked + .slider:before {
    transform: translateX(30px);
  }

  .toggle:checked ~ .card-side:before {
    text-decoration: none;
  }

  .toggle:checked ~ .card-side:after {
    text-decoration: underline;
  }

  .flip-card__inner {
    width: 300px;
    height: 350px;
    position: relative;
    background-color: transparent;
    perspective: 1000px;
    text-align: center;
    transition: transform 0.8s;
    transform-style: preserve-3d;
    margin-top: 40px;
  }

  .toggle:checked ~ .flip-card__inner {
    transform: rotateY(180deg);
  }

  .toggle:checked ~ .flip-card__front {
    box-shadow: none;
  }

  .flip-card__front, .flip-card__back {
    padding: 20px;
    position: absolute;
    display: flex;
    flex-direction: column;
    justify-content: center;
    -webkit-backface-visibility: hidden;
    backface-visibility: hidden;
    background: var(--bg-color);
    gap: 20px;
    border-radius: 5px;
    border: 2px solid var(--main-color);
    box-shadow: 4px 4px var(--main-color);
    width: 100%;
    height: 100%;
    top: 0;
    left: 0;
  }

  .flip-card__back {
    transform: rotateY(180deg);
  }

  .flip-card__form {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 20px;
  }

  .title {
    margin: 10px 0 10px 0;
    font-size: 25px;
    font-weight: 900;
    text-align: center;
    color: var(--main-color);
  }

  .flip-card__input {
    width: 250px;
    height: 40px;
    border-radius: 5px;
    border: 2px solid var(--main-color);
    background-color: var(--bg-color);
    box-shadow: 4px 4px var(--main-color);
    font-size: 15px;
    font-weight: 600;
    color: var(--font-color);
    padding: 5px 10px;
    outline: none;
  }

  .flip-card__input::placeholder {
    color: var(--font-color-sub);
    opacity: 0.8;
  }

  .flip-card__input:focus {
    border: 2px solid var(--input-focus);
  }

  .flip-card__btn:active, .button-confirm:active {
    box-shadow: 0px 0px var(--main-color);
    transform: translate(3px, 3px);
  }

  .flip-card__btn {
    margin: 10px 0 10px 0;
    width: 120px;
    height: 40px;
    border-radius: 5px;
    border: 2px solid var(--main-color);
    background-color: var(--bg-color);
    box-shadow: 4px 4px var(--main-color);
    font-size: 17px;
    font-weight: 600;
    color: var(--font-color);
    cursor: pointer;
  }
</style>
