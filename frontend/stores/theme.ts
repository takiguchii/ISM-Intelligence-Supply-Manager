import { defineStore } from "pinia";

const STORAGE_KEY = "ism-theme";

type ThemeMode = "dark" | "light";

export const useThemeStore = defineStore("theme", {
  state: () => ({
    mode: (process.client
      ? (localStorage.getItem(STORAGE_KEY) as ThemeMode | null)
      : null) || ("dark" as ThemeMode)
  }),
  getters: {
    isDark: (s) => s.mode === "dark",
    isLight: (s) => s.mode === "light"
  },
  actions: {
    applyToDocument() {
      if (!process.client) return;
      const root = document.documentElement;
      if (this.mode === "dark") {
        root.classList.add("dark");
        root.classList.remove("light");
        root.style.colorScheme = "dark";
      } else {
        root.classList.remove("dark");
        root.classList.add("light");
        root.style.colorScheme = "light";
      }
    },
    set(mode: ThemeMode) {
      this.mode = mode;
      if (process.client) localStorage.setItem(STORAGE_KEY, mode);
      this.applyToDocument();
    },
    toggle() {
      this.set(this.mode === "dark" ? "light" : "dark");
    },
    init() {
      if (!process.client) return;
      const stored = localStorage.getItem(STORAGE_KEY) as ThemeMode | null;
      if (stored === "dark" || stored === "light") {
        this.mode = stored;
      }
      this.applyToDocument();
    }
  }
});
