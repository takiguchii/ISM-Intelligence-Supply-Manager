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
    { required: true, message: "E-mail é obrigatório", trigger: ["blur", "change"] },
    { type: "email", message: "E-mail inválido", trigger: ["blur", "change"] }
  ],
  password: [
    { required: true, message: "Senha é obrigatória", trigger: ["blur", "change"] },
    { min: 6, message: "Senha deve ter pelo menos 6 caracteres", trigger: ["blur", "change"] }
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
  <main class="relative min-h-screen overflow-hidden">
    <div class="hero-grid absolute inset-0 opacity-40" />
    <div class="hero-glow hero-glow-left" />
    <div class="hero-glow hero-glow-right" />

    <section class="relative flex min-h-screen items-center justify-center px-4 py-10">
      <div class="w-full max-w-md">
        <div class="mb-8 text-center">
          <div class="mb-4 inline-flex items-center gap-3 rounded-full border border-aqua/20 bg-white/5 px-4 py-2 backdrop-blur">
            <span class="h-2.5 w-2.5 rounded-full bg-aqua shadow-glow" />
            <span class="font-mono text-xs uppercase tracking-[0.35em] text-aqua/80">
              Entrar
            </span>
          </div>
          <h1 class="font-display text-4xl font-semibold text-white">
            {{ runtimeConfig.public.appName }}
          </h1>
          <p class="mt-2 text-mist">
            Faça login para acessar o painel
          </p>
        </div>

        <div class="rounded-2xl border border-white/10 bg-white/5 p-6 backdrop-blur-xl shadow-2xl">
          <el-alert
            v-if="errorMessage"
            type="error"
            :closable="false"
            class="mb-5 rounded-lg"
          >
            {{ errorMessage }}
          </el-alert>

          <el-form
            ref="loginFormRef"
            v-model="loginForm"
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
              class="w-full rounded-lg"
              :loading="submitting"
              @click="handleLogin"
            >
              Entrar
            </el-button>
          </el-form>
        </div>

        <div class="mt-6 text-center text-xs text-mist/60">
          <p>Usuário padrão: admin@ism.com.br / admin123</p>
        </div>
      </div>
    </section>
  </main>
</template>
