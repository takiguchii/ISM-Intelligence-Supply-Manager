import type { Directive } from "vue";

export default defineNuxtPlugin((nuxtApp) => {
  const cleanups = new WeakMap<HTMLElement, () => void>();
  const directive: Directive<HTMLElement> = {
    mounted(root) {
      if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;

      const seen = new WeakSet<Element>();
      let batchIndex = 0;
      let resetTimer: ReturnType<typeof setTimeout> | undefined;
      const observer = new IntersectionObserver((entries) => {
        for (const entry of entries) {
          if (!entry.isIntersecting) continue;
          const card = entry.target as HTMLElement;
          card.style.setProperty("--card-delay", `${Math.min(batchIndex++, 8) * 100}ms`);
          card.classList.add("ism-card-visible");
          observer.unobserve(card);
          clearTimeout(resetTimer);
          resetTimer = setTimeout(() => { batchIndex = 0; }, 200);
        }
      }, { threshold: 0.05 });

      const scan = () => {
        for (const card of root.querySelectorAll<HTMLElement>("div.rounded-2xl, section.rounded-2xl, article.rounded-2xl, [data-card-reveal]")) {
          if (seen.has(card) || card.parentElement?.closest(".rounded-2xl") || card.closest('[role="dialog"]')) continue;
          seen.add(card);
          card.classList.add("ism-card-reveal");
          observer.observe(card);
        }
      };
      scan();
      const mutations = new MutationObserver(scan);
      mutations.observe(root, { childList: true, subtree: true });
      cleanups.set(root, () => {
        mutations.disconnect();
        observer.disconnect();
        clearTimeout(resetTimer);
      });
    },
    unmounted(root) { cleanups.get(root)?.(); }
  };
  nuxtApp.vueApp.directive("card-reveal", directive);
});
