<script setup lang="ts">
import { computed, ref, useId, type Ref } from "vue";
import { useAgentMascotMotion } from "~/composables/useAgentMascotMotion";
import type { AgentAttention, AgentKind, AgentMood } from "~/types/agentExperience";

const props = withDefaults(defineProps<{
  agent: AgentKind;
  mood?: AgentMood;
  size?: "xs" | "sm" | "md" | "lg";
  interactive?: boolean;
  attentionLevel?: AgentAttention;
  reducedMotion?: boolean;
  labelledBy?: string;
}>(), {
  mood: "idle",
  size: "md",
  interactive: false,
  attentionLevel: "none",
  reducedMotion: undefined,
  labelledBy: undefined
});

const emit = defineEmits<{ activate: []; focus: []; "expression-end": [] }>();
const mood = computed(() => props.mood);
const interactive = computed(() => props.interactive);
const reducedMotion = computed(() => props.reducedMotion);
const { rootRef, eyeTransform, headTransform, blinking, squashing, motionReduced, triggerSquash } = useAgentMascotMotion({ mood, interactive, reducedMotion: reducedMotion as Ref<boolean | undefined> });
const uid = useId().replace(/[^a-zA-Z0-9_-]/g, "");
const gradientId = `ism-head-gradient-${uid}`;
const shadowId = `ism-head-shadow-${uid}`;
const labelledId = props.labelledBy || `ism-mascot-label-${uid}`;
const isHappy = computed(() => ["happy", "success"].includes(props.mood));
const isWorried = computed(() => ["worried", "error", "waiting-approval"].includes(props.mood));
const isSad = computed(() => props.mood === "sad");
const isSurprised = computed(() => props.mood === "surprised");
const isThinking = computed(() => props.mood === "thinking");
const showRays = computed(() => props.attentionLevel === "warning" || props.attentionLevel === "critical" || isHappy.value || isSurprised.value);
const accent = computed(() => ({ stock: "#F05259", pricing: "#AAB6CC", suppliers: "#4B9DFF", general: "#7184A8" }[props.agent]));
const sizeClass = computed(() => ({ xs: "ism-mascot-xs", sm: "ism-mascot-sm", md: "ism-mascot-md", lg: "ism-mascot-lg" }[props.size]));
const moodLabel = computed(() => ({ idle: "em repouso", curious: "observando", happy: "recuperado", worried: "em atenção", sad: "em situação crítica", surprised: "novo alerta", thinking: "analisando", speaking: "respondendo", "waiting-approval": "aguardando aprovação", success: "operação confirmada", error: "erro", offline: "chat indisponível" }[props.mood]));
const rootElement = ref<HTMLElement | null>(null);

const activate = () => { triggerSquash(); emit("activate"); };
const handleFocus = () => emit("focus");
</script>

<template>
  <span
    ref="rootRef"
    class="ism-mascot"
    :class="[sizeClass, { 'ism-mascot-interactive': interactive, 'ism-mascot-reduced': motionReduced, 'ism-mascot-squash': squashing }]"
    :role="interactive ? 'button' : undefined"
    :tabindex="interactive ? 0 : undefined"
    :aria-labelledby="labelledId"
    :aria-label="interactive ? `Abrir contexto do agente ${agent}, ${moodLabel}` : undefined"
    @click="interactive && activate()"
    @focus="handleFocus"
    @keydown.enter.prevent="interactive && activate()"
    @keydown.space.prevent="interactive && activate()"
  >
    <span v-if="interactive" :id="labelledId" class="sr-only">Agente {{ agent }}, {{ moodLabel }}</span>
    <svg viewBox="0 0 112 86" preserveAspectRatio="xMidYMid meet" aria-hidden="true" focusable="false">
      <defs>
        <linearGradient :id="gradientId" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0" stop-color="#FFFFFF"/>
          <stop offset="1" stop-color="#E3ECFF"/>
        </linearGradient>
        <filter :id="shadowId" x="-15%" y="-20%" width="130%" height="150%">
          <feDropShadow dx="0" dy="2" stdDeviation="2" flood-color="#18233B" flood-opacity=".15"/>
        </filter>
      </defs>
      <g class="ism-mascot-head" :transform="headTransform">
        <rect x="4" y="4" width="104" height="78" rx="27" :fill="`url(#${gradientId})`" stroke="#DDE4F0" stroke-width="1.2" :filter="`url(#${shadowId})`"/>
        <path d="M17 14c15-8 63-9 78 0" fill="none" stroke="#FFFFFF" stroke-width="3" stroke-linecap="round" opacity=".55"/>
        <circle cx="18" cy="18" r="6.5" :fill="accent" opacity=".95"/>
        <path d="M15.5 18h5M18 15.5v5" stroke="#FFF" stroke-width="1.2" stroke-linecap="round" opacity=".9"/>
        <g class="ism-mascot-eyes" :transform="eyeTransform">
          <g class="ism-mascot-eye-left" :class="{ 'ism-mascot-eye-happy': isHappy, 'ism-mascot-eye-sad': isSad }">
            <ellipse cx="42" cy="49" :rx="isSurprised ? 5 : 4" :ry="isSurprised ? 7.2 : isSad ? 5.1 : 6.5" fill="#101217"/>
            <path v-if="isHappy" d="M37 49q5-6 10 0" fill="none" stroke="#101217" stroke-width="2.2" stroke-linecap="round"/>
            <path v-if="isWorried || isThinking" d="M37 38q5-3 10 0" fill="none" stroke="#101217" stroke-width="1.7" stroke-linecap="round"/>
          </g>
          <g class="ism-mascot-eye-right" :class="{ 'ism-mascot-eye-happy': isHappy, 'ism-mascot-eye-sad': isSad }">
            <ellipse cx="70" cy="49" :rx="isSurprised ? 5 : 4" :ry="isSurprised ? 7.2 : isSad ? 5.1 : 6.5" fill="#101217"/>
            <path v-if="isHappy" d="M65 49q5-6 10 0" fill="none" stroke="#101217" stroke-width="2.2" stroke-linecap="round"/>
            <path v-if="isWorried || isThinking" d="M65 38q5-3 10 0" fill="none" stroke="#101217" stroke-width="1.7" stroke-linecap="round"/>
          </g>
          <g class="ism-mascot-lids" :class="{ 'ism-mascot-blink': blinking }">
            <path d="M35 49q7-7 14 0" fill="none" stroke="#F5F8FF" stroke-width="5" stroke-linecap="round"/>
            <path d="M63 49q7-7 14 0" fill="none" stroke="#F5F8FF" stroke-width="5" stroke-linecap="round"/>
          </g>
        </g>
        <path v-if="isSad" d="M36 35q6 3 12 0M64 35q6 3 12 0" fill="none" stroke="#101217" stroke-width="1.4" stroke-linecap="round" opacity=".8"/>
        <path v-if="isThinking" d="M87 26h6M89 21h4" stroke="#AAB6CC" stroke-width="1.8" stroke-linecap="round"/>
      </g>
      <g v-if="showRays" class="ism-mascot-rays" :style="{ color: accent }" stroke="currentColor" fill="none" stroke-width="1.8" stroke-linecap="round">
        <path d="M94 24l4-3"/><path d="M98 31h5"/><path v-if="isHappy || isSurprised" d="M94 38l4 3"/>
      </g>
    </svg>
  </span>
</template>

<style scoped>
.ism-mascot { --ism-mascot-w: 92px; display: inline-flex; width: var(--ism-mascot-w); aspect-ratio: 112 / 86; flex: 0 0 auto; align-items: center; justify-content: center; }
.ism-mascot svg { display: block; width: 100%; height: auto; overflow: visible; }
.ism-mascot-head { transform-box: fill-box; transform-origin: center; transition: transform 260ms cubic-bezier(.22,1,.36,1); }
.ism-mascot-eyes, .ism-mascot-lids { transform-box: fill-box; transform-origin: center; transition: transform 240ms cubic-bezier(.22,1,.36,1); }
.ism-mascot-lids { opacity: 0; }
.ism-mascot-blink { opacity: 1; }
.ism-mascot-rays { animation: ism-rays 420ms ease-out both; }
.ism-mascot:not(.ism-mascot-reduced) .ism-mascot-head { animation: ism-breathe 4.4s ease-in-out infinite; }
.ism-mascot-squash .ism-mascot-head { animation: ism-squash 300ms cubic-bezier(.22,1,.36,1) both; }
.ism-mascot-interactive { cursor: pointer; border-radius: 22px; outline: none; }
.ism-mascot-interactive:focus-visible { box-shadow: 0 0 0 3px color-mix(in srgb, currentColor 45%, transparent); }
.ism-mascot-xs { --ism-mascot-w: 38px; }
.ism-mascot-sm { --ism-mascot-w: 52px; }
.ism-mascot-md { --ism-mascot-w: 96px; }
.ism-mascot-lg { --ism-mascot-w: 136px; }
@keyframes ism-breathe { 0%, 100% { transform: translateY(0) scaleX(1) scaleY(1); } 50% { transform: translateY(-1.5px) scaleX(.996) scaleY(1.012); } }
@keyframes ism-squash { 0% { transform: scaleX(1) scaleY(1); } 32% { transform: scaleX(1.035) scaleY(.965); } 64% { transform: scaleX(.99) scaleY(1.015); } 100% { transform: scaleX(1) scaleY(1); } }
@keyframes ism-rays { from { opacity: 0; transform: translateY(3px) scale(.8); } to { opacity: 1; transform: translateY(0) scale(1); } }
@media (max-width: 767px) { .ism-mascot-md { --ism-mascot-w: 76px; } .ism-mascot-lg { --ism-mascot-w: 112px; } }
@media (prefers-reduced-motion: reduce) { .ism-mascot-head, .ism-mascot-rays { animation: none !important; transition: none; } .ism-mascot-interactive { cursor: default; } }
</style>
