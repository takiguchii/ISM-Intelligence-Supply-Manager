<script setup lang="ts">
import { User, Lock } from "@element-plus/icons-vue";
import { useAuthStore } from "~/stores/auth";
import type { FetchError } from "ofetch";
import type { ApiError } from "~/types/auth";

const authStore = useAuthStore();
const route = useRoute();
const router = useRouter();
const runtimeConfig = useRuntimeConfig();

const loginFormRef = ref<InstanceType<typeof ElForm> | null>(null);

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

const loginRules = {
  email: [
    { required: true, message: "E-mail é obrigatório", trigger: "blur" },
    { type: "email", message: "E-mail inválido", trigger: "blur" }
  ],
  password: [
    { required: true, message: "Senha é obrigatória", trigger: "blur" },
    { min: 6, message: "Senha deve ter pelo menos 6 caracteres", trigger: "blur" }
  ]
};

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
  if (!loginFormRef.value) return;
  try {
    await loginFormRef.value.validate();
  } catch {
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
  <main class="relative min-h-screen bg-zinc-950 text-zinc-100 flex items-center justify-center overflow-hidden font-sans selection:bg-zinc-800 selection:text-white">
    <!-- Glow Background Effects -->
    <div class="absolute -top-40 -left-40 w-96 h-96 bg-zinc-800/20 rounded-full blur-3xl pointer-events-none"></div>
    <div class="absolute -bottom-40 -right-40 w-96 h-96 bg-emerald-500/10 rounded-full blur-3xl pointer-events-none"></div>

    <section class="relative z-10 w-full max-w-md px-4 py-10">
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

      <div class="rounded-2xl border border-zinc-800/80 bg-zinc-900/80 p-8 backdrop-blur-xl shadow-2xl space-y-6">
        <el-alert
          v-if="errorMessage"
          type="error"
          :closable="false"
          class="mb-4 rounded-xl"
        >
          {{ errorMessage }}
        </el-alert>

        <el-form
          ref="loginFormRef"
          :model="loginForm"
          :rules="loginRules"
          label-position="top"
          @submit.prevent="handleLogin"
        >
          <el-form-item label="E-mail" prop="email">
            <el-input
              v-model="loginForm.email"
              type="email"
              placeholder="seu@email.com.br"
              size="large"
              autocomplete="email"
            >
              <template #prefix>
                <el-icon><User /></el-icon>
              </template>
            </el-input>
          </el-form-item>

          <el-form-item label="Senha" prop="password">
            <el-input
              v-model="loginForm.password"
              type="password"
              placeholder="Sua senha"
              size="large"
              show-password
              autocomplete="current-password"
            >
              <template #prefix>
                <el-icon><Lock /></el-icon>
              </template>
            </el-input>
          </el-form-item>

          <el-button
            type="primary"
            size="large"
            class="w-full rounded-xl mt-2 font-semibold"
            :loading="submitting"
            @click="handleLogin"
          >
            Entrar no Sistema
          </el-button>
        </el-form>
      </div>

      <div class="mt-6 text-center text-xs font-mono text-zinc-500">
        <p>Usuário padrão: <span class="text-zinc-400 font-semibold">admin@ism.com.br</span> / <span class="text-zinc-400 font-semibold">admin123</span></p>
      </div>
    </section>
  </main>
</template>
