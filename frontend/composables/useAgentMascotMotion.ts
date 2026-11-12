import { computed, nextTick, onBeforeUnmount, onMounted, ref, type Ref } from "vue";
import type { AgentMood } from "~/types/agentExperience";

type MotionOptions = {
  mood: Ref<AgentMood>;
  interactive: Ref<boolean>;
  reducedMotion?: Ref<boolean | undefined>;
};

const clamp = (value: number, min: number, max: number) => Math.min(max, Math.max(min, value));

export const useAgentMascotMotion = ({ mood, interactive, reducedMotion }: MotionOptions) => {
  const rootRef = ref<HTMLElement | null>(null);
  const gazeX = ref(0);
  const gazeY = ref(0);
  const targetX = ref(0);
  const targetY = ref(0);
  const blinking = ref(false);
  const visible = ref(true);
  const mounted = ref(false);
  const prefersReducedMotion = ref(false);
  const squashing = ref(false);
  const pointerActive = ref(false);
  let rafId: number | null = null;
  let blinkTimer: ReturnType<typeof setTimeout> | null = null;
  let saccadeTimer: ReturnType<typeof setTimeout> | null = null;
  let blinkResetTimer: ReturnType<typeof setTimeout> | null = null;
  let squashTimer: ReturnType<typeof setTimeout> | null = null;
  let observer: IntersectionObserver | null = null;
  let mediaQuery: MediaQueryList | null = null;
  let visibilityHandler: (() => void) | null = null;

  const motionReduced = computed(() => Boolean(reducedMotion?.value ?? prefersReducedMotion.value));
  const loopsAllowed = computed(() => mounted.value && visible.value && !motionReduced.value && document.visibilityState !== "hidden");
  const eyeTransform = computed(() => `translate(${gazeX.value.toFixed(2)} ${gazeY.value.toFixed(2)})`);
  const headTransform = computed(() => `rotate(${clamp(gazeX.value * -0.3, -1.2, 1.2).toFixed(2)} 56 43)`);

  const clearTimer = (timer: ReturnType<typeof setTimeout> | null) => { if (timer) clearTimeout(timer); };

  const stopRaf = () => {
    if (rafId !== null && typeof window !== "undefined") window.cancelAnimationFrame(rafId);
    rafId = null;
  };

  const animateGaze = () => {
    if (!mounted.value) return;
    const dx = targetX.value - gazeX.value;
    const dy = targetY.value - gazeY.value;
    if (Math.abs(dx) < 0.02 && Math.abs(dy) < 0.02) {
      gazeX.value = targetX.value;
      gazeY.value = targetY.value;
      stopRaf();
      return;
    }
    gazeX.value += dx * 0.18;
    gazeY.value += dy * 0.18;
    if (typeof window !== "undefined") rafId = window.requestAnimationFrame(animateGaze);
  };

  const requestGaze = (x: number, y: number) => {
    if (motionReduced.value || !interactive.value) return;
    targetX.value = clamp(x, -3.5, 3.5);
    targetY.value = clamp(y, -2.4, 2.4);
    if (rafId === null && typeof window !== "undefined") rafId = window.requestAnimationFrame(animateGaze);
  };

  const handlePointerMove = (event: PointerEvent) => {
    if (!rootRef.value || motionReduced.value || event.pointerType === "touch" || event.pointerType === "pen") return;
    const bounds = rootRef.value.getBoundingClientRect();
    if (!bounds.width || !bounds.height) return;
    const nx = ((event.clientX - (bounds.left + bounds.width / 2)) / (bounds.width * 0.5));
    const ny = ((event.clientY - (bounds.top + bounds.height / 2)) / (bounds.height * 0.5));
    requestGaze(nx * 3.5, ny * 2.4);
  };

  const handlePointerEnter = () => {
    pointerActive.value = true;
    triggerSquash("hover");
  };

  const handlePointerLeave = () => {
    pointerActive.value = false;
    requestGaze(0, 0);
  };

  const scheduleBlink = () => {
    clearTimer(blinkTimer);
    if (!loopsAllowed.value) return;
    const delay = 3500 + Math.random() * 3500;
    blinkTimer = setTimeout(() => {
      if (loopsAllowed.value && !["happy", "success", "sad"].includes(mood.value)) {
        blinking.value = true;
        clearTimer(blinkResetTimer);
        blinkResetTimer = setTimeout(() => { blinking.value = false; scheduleBlink(); }, 180 + Math.random() * 40);
      } else {
        scheduleBlink();
      }
    }, delay);
  };

  const scheduleSaccade = () => {
    clearTimer(saccadeTimer);
    if (!loopsAllowed.value || pointerActive.value || ["thinking", "speaking", "sad"].includes(mood.value)) return;
    saccadeTimer = setTimeout(() => {
      if (loopsAllowed.value && !pointerActive.value) {
        requestGaze((Math.random() * 4 - 2), (Math.random() * 2.4 - 1.2));
        setTimeout(() => { if (!pointerActive.value) requestGaze(0, 0); }, 380 + Math.random() * 220);
      }
      scheduleSaccade();
    }, 4000 + Math.random() * 5000);
  };

  const triggerSquash = (kind: "hover" | "click" = "click") => {
    if (motionReduced.value) return;
    squashing.value = false;
    void nextTick(() => {
      squashing.value = true;
      clearTimer(squashTimer);
      squashTimer = setTimeout(() => { squashing.value = false; }, kind === "hover" ? 300 : 400);
    });
  };

  const stopLoops = () => {
    clearTimer(blinkTimer); clearTimer(saccadeTimer); clearTimer(blinkResetTimer);
    blinkTimer = null; saccadeTimer = null; blinkResetTimer = null;
    blinking.value = false;
    stopRaf();
  };

  const restartLoops = () => {
    stopLoops();
    if (loopsAllowed.value) { scheduleBlink(); scheduleSaccade(); }
  };

  const updateReducedMotion = () => { prefersReducedMotion.value = Boolean(mediaQuery?.matches); restartLoops(); };

  onMounted(() => {
    mounted.value = true;
    mediaQuery = window.matchMedia("(prefers-reduced-motion: reduce)");
    updateReducedMotion();
    mediaQuery.addEventListener?.("change", updateReducedMotion);
    visibilityHandler = () => { visible.value = document.visibilityState !== "hidden"; if (!visible.value) stopLoops(); else restartLoops(); };
    document.addEventListener("visibilitychange", visibilityHandler);
    observer = new IntersectionObserver(entries => { visible.value = entries[0]?.isIntersecting ?? true; if (visible.value) restartLoops(); else stopLoops(); }, { threshold: 0.05 });
    if (rootRef.value) observer.observe(rootRef.value);
    rootRef.value?.addEventListener("pointerenter", handlePointerEnter);
    rootRef.value?.addEventListener("pointermove", handlePointerMove, { passive: true });
    rootRef.value?.addEventListener("pointerleave", handlePointerLeave);
    restartLoops();
  });

  onBeforeUnmount(() => {
    stopLoops();
    clearTimer(squashTimer);
    observer?.disconnect();
    observer = null;
    mediaQuery?.removeEventListener?.("change", updateReducedMotion);
    if (visibilityHandler) document.removeEventListener("visibilitychange", visibilityHandler);
    rootRef.value?.removeEventListener("pointerenter", handlePointerEnter);
    rootRef.value?.removeEventListener("pointermove", handlePointerMove);
    rootRef.value?.removeEventListener("pointerleave", handlePointerLeave);
    mounted.value = false;
  });

  return { rootRef, eyeTransform, headTransform, blinking, squashing, motionReduced, triggerSquash };
};
