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
const { rootRef, eyeTransform, headTransform, blinking, squashing, motionReduced, triggerSquash } = useAgentMascotMotion({ mood, interactive: computed(() => true), reducedMotion: reducedMotion as Ref<boolean | undefined> });
const uid = useId().replace(/[^a-zA-Z0-9_-]/g, "");
const gradientId = `ism-head-gradient-${uid}`;
const shadowId = `ism-head-shadow-${uid}`;
const labelledId = props.labelledBy || `ism-mascot-label-${uid}`;
const isHappy = computed(() => ["happy", "success"].includes(props.mood));
const isWorried = computed(() => ["worried", "error", "waiting-approval"].includes(props.mood));
const isSad = computed(() => ["sad", "offline"].includes(props.mood));
const isSurprised = computed(() => props.mood === "surprised");
const isThinking = computed(() => props.mood === "thinking");
const showRays = computed(() => props.attentionLevel === "warning" || props.attentionLevel === "critical" || isHappy.value || isSurprised.value);
const palette = computed(() => ({ stock: ["#FF8587", "#F45D67"], pricing: ["#FFF08A", "#FFD84D"], suppliers: ["#C9A6FF", "#8B5CE6"], general: ["#FFFFFF", "#DCE6F5"] }[props.agent]));
const accent = computed(() => palette.value[1]);
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
          <stop offset="0" :stop-color="palette[0]"/>
          <stop offset="1" :stop-color="palette[1]"/>
        </linearGradient>
        <filter :id="shadowId" x="-15%" y="-20%" width="130%" height="150%">
          <feDropShadow dx="0" dy="2" stdDeviation="2" flood-color="#18233B" flood-opacity=".15"/>
        </filter>
      </defs>
      <g :transform="headTransform"><g class="ism-mascot-head">
        <rect x="4" y="4" width="104" height="78" rx="27" :fill="`url(#${gradientId})`" stroke="#DDE4F0" stroke-width="1.2" :filter="`url(#${shadowId})`"/>
        <path d="M17 14c15-8 63-9 78 0" fill="none" stroke="#FFFFFF" stroke-width="3" stroke-linecap="round" opacity=".55"/>
        <g class="ism-mascot-eyes" :transform="eyeTransform">
          <g class="ism-mascot-eye-left" :class="{ 'ism-mascot-eye-happy': isHappy, 'ism-mascot-eye-sad': isSad }">
            <ellipse v-if="!isHappy" cx="42" :cy="isThinking ? 39 : 49" :rx="isSurprised ? 4.6 : 3.8" :ry="isSurprised ? 7 : isSad ? 4.2 : isWorried ? 5.8 : 6.2" fill="#101217"/>
            <path v-if="isWorried" d="M37 41q5-3 10-1" fill="none" stroke="#101217" stroke-width="1.8" stroke-linecap="round"/>
            <path v-if="isHappy" d="M37 49q5-8 10 0" fill="none" stroke="#101217" stroke-width="2.4" stroke-linecap="round"/>
          </g>
          <g class="ism-mascot-eye-right" :class="{ 'ism-mascot-eye-happy': isHappy, 'ism-mascot-eye-sad': isSad }">
            <ellipse v-if="!isHappy" cx="70" :cy="isThinking ? 39 : 49" :rx="isSurprised ? 4.6 : 3.8" :ry="isSurprised ? 7 : isSad ? 4.2 : isWorried ? 6.6 : 6.2" fill="#101217"/>
            <path v-if="isWorried" d="M65 40q5 2 10 0" fill="none" stroke="#101217" stroke-width="1.8" stroke-linecap="round"/>
            <path v-if="isHappy" d="M65 49q5-8 10 0" fill="none" stroke="#101217" stroke-width="2.4" stroke-linecap="round"/>
          </g>
          <g class="ism-mascot-lids" :class="{ 'ism-mascot-blink': blinking }">
            <path d="M35 49q7-7 14 0" fill="none" stroke="#F5F8FF" stroke-width="5" stroke-linecap="round"/>
            <path d="M63 49q7-7 14 0" fill="none" stroke="#F5F8FF" stroke-width="5" stroke-linecap="round"/>
          </g>
        </g>
      </g></g>
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
.ism-mascot:not(.ism-mascot-reduced) .ism-mascot-head { animation: ism-breathe 4.8s ease-in-out infinite; }
.ism-mascot-squash .ism-mascot-head { animation: ism-squash 300ms cubic-bezier(.22,1,.36,1) both; }
.ism-mascot-interactive { cursor: pointer; border-radius: 22px; outline: none; }
.ism-mascot-interactive:focus-visible { box-shadow: 0 0 0 3px color-mix(in srgb, currentColor 45%, transparent); }
.ism-mascot-xs { --ism-mascot-w: 38px; }
.ism-mascot-sm { --ism-mascot-w: 52px; }
.ism-mascot-md { --ism-mascot-w: 96px; }
.ism-mascot-lg { --ism-mascot-w: 136px; }
@keyframes ism-breathe { 0%, 100% { transform: translateY(0) scaleX(1) scaleY(1); } 50% { transform: translateY(-1.8px) scaleX(.985) scaleY(1.018); } }
@keyframes ism-squash { 0% { transform: scaleX(1) scaleY(1); } 32% { transform: scaleX(1.035) scaleY(.965); } 64% { transform: scaleX(.99) scaleY(1.015); } 100% { transform: scaleX(1) scaleY(1); } }
@keyframes ism-rays { from { opacity: 0; transform: translateY(3px) scale(.8); } to { opacity: 1; transform: translateY(0) scale(1); } }
@media (max-width: 767px) { .ism-mascot-md { --ism-mascot-w: 76px; } .ism-mascot-lg { --ism-mascot-w: 112px; } }
@media (prefers-reduced-motion: reduce) { .ism-mascot-head, .ism-mascot-rays { animation: none !important; transition: none; } .ism-mascot-interactive { cursor: default; } }
</style>
