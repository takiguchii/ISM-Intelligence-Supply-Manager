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

const particlesCanvas = ref<HTMLCanvasElement | null>(null);
let animationFrameId: number | null = null;

interface Particle {
  x: number;
  y: number;
  vx: number;
  vy: number;
  radius: number;
}

const particles = ref<Particle[]>([]);
const mousePos = ref<{ x: number; y: number; active: boolean }>({ x: 0, y: 0, active: false });

const PARTICLE_COUNT = 130;
const PARTICLE_MIN_SPEED = 0.15;
const PARTICLE_MAX_SPEED = 0.6;
const CONNECTION_DISTANCE = 140;
const MOUSE_REPEL_RADIUS = 160;
const MOUSE_REPEL_STRENGTH = 1.2;

const redirectPath = computed(() => {
  const redirect = route.query.redirect as string;
  return redirect && redirect.startsWith("/") ? redirect : "/";
});

const randomRange = (min: number, max: number) => Math.random() * (max - min) + min;

const initParticles = (width: number, height: number) => {
  const arr: Particle[] = [];
  for (let i = 0; i < PARTICLE_COUNT; i++) {
    const angle = Math.random() * Math.PI * 2;
    const speed = randomRange(PARTICLE_MIN_SPEED, PARTICLE_MAX_SPEED);
    arr.push({
      x: Math.random() * width,
      y: Math.random() * height,
      vx: Math.cos(angle) * speed,
      vy: Math.sin(angle) * speed,
      radius: randomRange(1.4, 2.8)
    });
  }
  particles.value = arr;
};

const drawParticles = () => {
  const canvas = particlesCanvas.value;
  if (!canvas) return;
  const ctx = canvas.getContext("2d");
  if (!ctx) return;

  const width = canvas.width;
  const height = canvas.height;

  ctx.clearRect(0, 0, width, height);

  const mouse = mousePos.value;

  for (let i = 0; i < particles.value.length; i++) {
    const p = particles.value[i];

    if (mouse.active) {
      const dx = p.x - mouse.x;
      const dy = p.y - mouse.y;
      const dist = Math.sqrt(dx * dx + dy * dy);
      if (dist < MOUSE_REPEL_RADIUS && dist > 0) {
        const force = (MOUSE_REPEL_RADIUS - dist) / MOUSE_REPEL_RADIUS;
        p.vx += (dx / dist) * force * MOUSE_REPEL_STRENGTH * 0.08;
        p.vy += (dy / dist) * force * MOUSE_REPEL_STRENGTH * 0.08;
      }
    }

    const currentSpeed = Math.sqrt(p.vx * p.vx + p.vy * p.vy);
    if (currentSpeed > PARTICLE_MAX_SPEED * 2.2) {
      p.vx = (p.vx / currentSpeed) * PARTICLE_MAX_SPEED * 2.2;
      p.vy = (p.vy / currentSpeed) * PARTICLE_MAX_SPEED * 2.2;
    }

    p.vx *= 0.992;
    p.vy *= 0.992;

    const minSpeedX = PARTICLE_MIN_SPEED * 0.4;
    if (Math.abs(p.vx) < minSpeedX) {
      p.vx += (Math.random() - 0.5) * 0.02;
    }
    if (Math.abs(p.vy) < minSpeedX) {
      p.vy += (Math.random() - 0.5) * 0.02;
    }

    p.x += p.vx;
    p.y += p.vy;

    if (p.x < 0) { p.x = 0; p.vx *= -1; }
    if (p.x > width) { p.x = width; p.vx *= -1; }
    if (p.y < 0) { p.y = 0; p.vy *= -1; }
    if (p.y > height) { p.y = height; p.vy *= -1; }
  }

  const points = particles.value;
  for (let i = 0; i < points.length; i++) {
    const a = points[i];
    for (let j = i + 1; j < points.length; j++) {
      const b = points[j];
      const dx = a.x - b.x;
      const dy = a.y - b.y;
      const dist = Math.sqrt(dx * dx + dy * dy);
      if (dist < CONNECTION_DISTANCE) {
        const opacity = 1 - dist / CONNECTION_DISTANCE;
        const lineWidth = 0.5 + opacity * 0.8;
        ctx.beginPath();
        ctx.moveTo(a.x, a.y);
        ctx.lineTo(b.x, b.y);
        ctx.strokeStyle = `rgba(161, 161, 170, ${(opacity * 0.55).toFixed(3)})`;
        ctx.lineWidth = lineWidth;
        ctx.stroke();
      }
    }

    if (mouse.active) {
      const dx = a.x - mouse.x;
      const dy = a.y - mouse.y;
      const dist = Math.sqrt(dx * dx + dy * dy);
      if (dist < MOUSE_REPEL_RADIUS) {
        const opacity = 1 - dist / MOUSE_REPEL_RADIUS;
        ctx.beginPath();
        ctx.moveTo(a.x, a.y);
        ctx.lineTo(mouse.x, mouse.y);
        ctx.strokeStyle = `rgba(212, 212, 216, ${(opacity * 0.9).toFixed(3)})`;
        ctx.lineWidth = 0.8 + opacity * 1.1;
        ctx.stroke();
      }
    }
  }

  for (let i = 0; i < points.length; i++) {
    const p = points[i];
    const isNearMouse = mouse.active &&
      Math.sqrt((p.x - mouse.x) ** 2 + (p.y - mouse.y) ** 2) < MOUSE_REPEL_RADIUS;

    const glowRadius = isNearMouse ? p.radius * 4.5 : p.radius * 2.8;
    const grad = ctx.createRadialGradient(p.x, p.y, 0, p.x, p.y, glowRadius);
    grad.addColorStop(0, `rgba(255, 255, 255, ${isNearMouse ? 0.98 : 0.92})`);
    grad.addColorStop(0.3, `rgba(228, 228, 231, ${isNearMouse ? 0.75 : 0.55})`);
    grad.addColorStop(1, `rgba(161, 161, 170, 0)`);

    ctx.beginPath();
    ctx.arc(p.x, p.y, glowRadius, 0, Math.PI * 2);
    ctx.fillStyle = grad;
    ctx.fill();

    ctx.beginPath();
    ctx.arc(p.x, p.y, p.radius, 0, Math.PI * 2);
    ctx.fillStyle = isNearMouse ? "#fafafa" : "#e4e4e7";
    ctx.fill();
  }

  animationFrameId = requestAnimationFrame(drawParticles);
};

const resizeCanvas = () => {
  const canvas = particlesCanvas.value;
  if (!canvas) return;
  const dpr = window.devicePixelRatio || 1;
  const width = window.innerWidth;
  const height = window.innerHeight;
  canvas.width = width * dpr;
  canvas.height = height * dpr;
  canvas.style.width = `${width}px`;
  canvas.style.height = `${height}px`;
  const ctx = canvas.getContext("2d");
  if (ctx) ctx.scale(dpr, dpr);
  if (particles.value.length === 0) {
    initParticles(width, height);
  } else {
    particles.value.forEach(p => {
      if (p.x > width) p.x = width * Math.random();
      if (p.y > height) p.y = height * Math.random();
    });
  }
};

const handleCanvasMouseMove = (e: MouseEvent) => {
  mousePos.value = {
    x: e.clientX,
    y: e.clientY,
    active: true
  };
};

const handleCanvasMouseLeave = () => {
  mousePos.value.active = false;
};

onMounted(() => {
  if (process.client) {
    authStore.initFromStorage();
    if (authStore.isAuthenticated) {
      router.push(redirectPath.value);
      return;
    }
    resizeCanvas();
    window.addEventListener("resize", resizeCanvas);
    window.addEventListener("mousemove", handleCanvasMouseMove);
    window.addEventListener("mouseleave", handleCanvasMouseLeave);
    drawParticles();
  }
});

onBeforeUnmount(() => {
  if (process.client) {
    window.removeEventListener("resize", resizeCanvas);
    window.removeEventListener("mousemove", handleCanvasMouseMove);
    window.removeEventListener("mouseleave", handleCanvasMouseLeave);
    if (animationFrameId != null) {
      cancelAnimationFrame(animationFrameId);
    }
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
  <main class="relative min-h-screen w-full overflow-hidden bg-zinc-950 text-zinc-100 font-sans selection:bg-white/15 selection:text-white">
    <canvas
      ref="particlesCanvas"
      class="fixed inset-0 z-0 block"
      aria-hidden="true"
    ></canvas>

    <div class="fixed inset-0 z-[1] pointer-events-none bg-[radial-gradient(ellipse_at_center,rgba(255,255,255,0.06)_0%,transparent_55%)]"></div>

    <div class="fixed inset-0 z-[1] pointer-events-none opacity-[0.03]" style="background-image: linear-gradient(rgba(255,255,255,0.9) 1px, transparent 1px), linear-gradient(90deg, rgba(255,255,255,0.9) 1px, transparent 1px); background-size: 48px 48px;"></div>

    <div class="relative z-10 min-h-screen w-full flex items-center justify-center px-4 sm:px-6 py-10">
      <div class="w-full sm:max-w-md lg:max-w-md">
        <div class="relative group">
          <div class="absolute -inset-1 bg-gradient-to-r from-white/10 via-zinc-300/8 to-white/10 rounded-3xl blur-xl opacity-60 group-hover:opacity-100 transition-opacity duration-700"></div>

          <div class="absolute -inset-[1px] bg-gradient-to-r from-zinc-300/50 via-zinc-200/30 to-zinc-300/50 rounded-3xl opacity-30 group-hover:opacity-60 transition-opacity duration-500"></div>

          <div class="relative bg-zinc-950/85 backdrop-blur-2xl border border-zinc-700/50 shadow-[0_0_80px_-15px_rgba(255,255,255,0.25)] rounded-3xl p-8 sm:p-10">
            <div class="flex flex-col items-center mb-8">
              <div class="mb-6">
                <div class="w-16 h-16 rounded-2xl bg-zinc-900 flex items-center justify-center shadow-[0_0_40px_rgba(255,255,255,0.12)] border border-zinc-700 relative overflow-hidden">
                  <div class="absolute inset-0 bg-[linear-gradient(135deg,rgba(255,255,255,0.18)_0%,transparent_50%,transparent_100%)]"></div>
                  <span class="relative text-2xl font-black text-white tracking-tighter drop-shadow-[0_1px_0_rgba(0,0,0,0.4)]">ISM</span>
                </div>
              </div>

              <div class="text-center space-y-2 w-full">
                <div class="inline-flex items-center gap-2 px-3.5 py-1.5 rounded-full text-[11px] font-mono bg-zinc-900/70 border border-zinc-700/60 text-zinc-300 mx-auto shadow-[inset_0_1px_0_rgba(255,255,255,0.06)]">
                  <span class="w-1.5 h-1.5 rounded-full animate-pulse bg-zinc-300 shadow-[0_0_8px_rgba(255,255,255,0.8)]"></span>
                  <span class="font-mono uppercase tracking-[0.18em]">Acesso ao sistema</span>
                </div>
                <h1 class="text-2xl sm:text-3xl font-bold tracking-tight mt-5">
                  <span class="bg-gradient-to-r from-white via-zinc-100 to-zinc-300 bg-clip-text text-transparent drop-shadow-[0_0_30px_rgba(255,255,255,0.15)]">
                    {{ runtimeConfig.public.appName }}
                  </span>
                </h1>
                <p class="text-sm text-zinc-400">
                  Faça login para continuar
                </p>
              </div>
            </div>

            <div v-if="errorMessage" class="mb-6 w-full">
              <div class="flex items-start gap-3 p-4 rounded-xl bg-white/5 border border-zinc-700/60 text-zinc-200 text-sm backdrop-blur">
                <svg class="w-5 h-5 flex-shrink-0 mt-0.5 text-zinc-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <span class="leading-relaxed">{{ errorMessage }}</span>
              </div>
            </div>

            <form class="space-y-5" @submit.prevent="handleLogin">
              <div class="space-y-2">
                <label for="email" class="block text-sm font-medium text-zinc-300">
                  E-mail
                </label>
                <div class="relative group/input">
                  <div class="absolute -inset-[1px] rounded-xl bg-gradient-to-r from-zinc-400/0 via-zinc-300/50 to-zinc-400/0 opacity-0 group-focus-within/input:opacity-100 blur-sm transition-opacity duration-300 pointer-events-none"></div>
                  <div class="absolute inset-y-0 left-0 flex items-center pl-4 pointer-events-none">
                    <svg class="w-5 h-5 text-zinc-500 group-focus-within/input:text-zinc-300 transition-colors duration-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                    </svg>
                  </div>
                  <input
                    id="email"
                    name="email"
                    type="email"
                    autocomplete="email"
                    v-model="loginForm.email"
                    placeholder="seu@email.com.br"
                    class="relative w-full h-12 pl-12 pr-4 rounded-xl bg-zinc-900/70 border border-zinc-700/60 text-zinc-100 placeholder:text-zinc-600 focus:outline-none focus:ring-2 focus:ring-zinc-400/30 focus:border-zinc-400/60 transition-all duration-200 autofill:bg-zinc-900/70 autofill:text-zinc-100 autofill:shadow-[inset_0_0_0px_1000px_rgba(9,9,11,0.92)]"
                  />
                </div>
              </div>

              <div class="space-y-2">
                <label for="password" class="block text-sm font-medium text-zinc-300">
                  Senha
                </label>
                <div class="relative group/input">
                  <div class="absolute -inset-[1px] rounded-xl bg-gradient-to-r from-zinc-400/0 via-zinc-300/50 to-zinc-400/0 opacity-0 group-focus-within/input:opacity-100 blur-sm transition-opacity duration-300 pointer-events-none"></div>
                  <div class="absolute inset-y-0 left-0 flex items-center pl-4 pointer-events-none">
                    <svg class="w-5 h-5 text-zinc-500 group-focus-within/input:text-zinc-300 transition-colors duration-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                    </svg>
                  </div>
                  <input
                    id="password"
                    name="password"
                    type="password"
                    autocomplete="current-password"
                    v-model="loginForm.password"
                    placeholder="••••••••"
                    class="relative w-full h-12 pl-12 pr-4 rounded-xl bg-zinc-900/70 border border-zinc-700/60 text-zinc-100 placeholder:text-zinc-600 focus:outline-none focus:ring-2 focus:ring-zinc-400/30 focus:border-zinc-400/60 transition-all duration-200 autofill:bg-zinc-900/70 autofill:text-zinc-100 autofill:shadow-[inset_0_0_0px_1000px_rgba(9,9,11,0.92)]"
                    @keyup.enter="handleLogin"
                  />
                </div>
              </div>

              <button
                type="submit"
                :disabled="submitting"
                class="relative w-full h-12 mt-2 rounded-xl font-semibold text-zinc-950 overflow-hidden transition-all duration-300 disabled:opacity-60 disabled:cursor-not-allowed group/btn shadow-[0_10px_40px_-12px_rgba(255,255,255,0.35)]"
              >
                <span class="absolute inset-0 bg-gradient-to-br from-zinc-100 via-white to-zinc-200 transition-all duration-300 group-hover/btn:from-white group-hover/btn:via-zinc-50 group-hover/btn:to-zinc-100"></span>
                <span class="absolute inset-0 opacity-0 group-hover/btn:opacity-100 transition-opacity duration-500 bg-[linear-gradient(135deg,rgba(255,255,255,0)_0%,rgba(255,255,255,0.6)_50%,rgba(255,255,255,0)_100%)] translate-x-[-100%] group-hover/btn:translate-x-[100%] transition-transform duration-900"></span>
                <span class="absolute inset-0 shadow-[inset_0_1px_0_rgba(255,255,255,0.8)] rounded-xl pointer-events-none"></span>
                <span class="relative flex items-center justify-center gap-2 w-full h-full">
                  <svg v-if="submitting" class="w-5 h-5 animate-spin text-zinc-950" fill="none" viewBox="0 0 24 24">
                    <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                    <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                  </svg>
                  <span>{{ submitting ? 'Aguarde...' : 'Entrar no sistema' }}</span>
                  <svg v-if="!submitting" class="w-4 h-4 opacity-0 -translate-x-2 group-hover/btn:opacity-100 group-hover/btn:translate-x-0 transition-all duration-300 text-zinc-950" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M13 7l5 5m0 0l-5 5m5-5H6" />
                  </svg>
                </span>
              </button>
            </form>

            <div class="mt-8 pt-6 border-t border-zinc-800">
              <div class="flex items-center gap-3 p-3.5 rounded-xl bg-zinc-900/50 border border-zinc-800 relative overflow-hidden">
                <div class="absolute inset-0 bg-gradient-to-r from-zinc-500/0 via-zinc-400/10 to-zinc-500/0 translate-x-[-100%] animate-[shimmer_3s_ease-in-out_infinite]"></div>
                <div class="relative flex-shrink-0 w-9 h-9 rounded-lg bg-zinc-800 flex items-center justify-center border border-zinc-700">
                  <svg class="w-4.5 h-4.5 text-zinc-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                </div>
                <div class="relative flex-1 min-w-0">
                  <div class="text-[11px] font-medium text-zinc-500 mb-0.5 uppercase tracking-wider">Credenciais de demonstração</div>
                  <div class="text-xs text-zinc-300 font-mono truncate">
                    <span class="text-zinc-100 font-semibold">admin@ism.com.br</span>
                    <span class="text-zinc-600 mx-1.5">/</span>
                    <span class="text-zinc-100 font-semibold">admin123</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <p class="mt-8 text-center text-xs text-zinc-600">
          © {{ new Date().getFullYear() }} ISM — Todos os direitos reservados
        </p>
      </div>
    </div>
  </main>
</template>

<style scoped>
@keyframes shimmer {
  0% {
    transform: translateX(-100%);
  }
  50%, 100% {
    transform: translateX(100%);
  }
}

input:-webkit-autofill,
input:-webkit-autofill:hover,
input:-webkit-autofill:focus,
input:-webkit-autofill:active {
  -webkit-box-shadow: 0 0 0 30px rgb(9, 9, 11) inset !important;
  -webkit-text-fill-color: rgb(244, 244, 245) !important;
  caret-color: rgb(244, 244, 245) !important;
  transition: background-color 5000s ease-in-out 0s;
}
</style>
