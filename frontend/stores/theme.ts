import { defineStore } from "pinia";
import { apiClientWithAuth } from "~/services/api/client";

export interface AppearancePreferences { theme: ThemeMode; density: "comfortable" | "compact"; fontScale: number; reduceMotion: boolean; highContrast: boolean }

const STORAGE_KEY = "ism-theme";

type ThemeMode = "dark" | "light";

export const useThemeStore = defineStore("theme", {
  state: () => ({
    mode: (process.client
      ? (localStorage.getItem(STORAGE_KEY) as ThemeMode | null)
      : null) || ("dark" as ThemeMode),
    forcedMode: null as ThemeMode | null,
    density: "comfortable" as "comfortable" | "compact",
    fontScale: 100,
    reduceMotion: false,
    highContrast: false,
    preferenceVersion: 0
  }),
  getters: {
    isDark: (s) => s.forcedMode ? s.forcedMode === "dark" : s.mode === "dark",
    isLight: (s) => s.forcedMode ? s.forcedMode === "light" : s.mode === "light"
  },
  actions: {
    applyToDocument() {
      if (!process.client) return;
      const root = document.documentElement;
      root.dataset.density = this.density;
      root.style.fontSize = `${this.fontScale}%`;
      root.classList.toggle("ism-reduce-motion", this.reduceMotion);
      root.classList.toggle("ism-high-contrast", this.highContrast);
      const effective = this.forcedMode || this.mode;
      if (effective === "dark") {
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
      if (process.client && localStorage.getItem("auth_token")) void this.savePreferences().catch(() => {});
    },
    preferences(): AppearancePreferences {
      return { theme: this.mode, density: this.density, fontScale: this.fontScale, reduceMotion: this.reduceMotion, highContrast: this.highContrast };
    },
    applyPreferences(preferences: AppearancePreferences) {
      this.mode = preferences.theme;
      this.density = preferences.density;
      this.fontScale = preferences.fontScale;
      this.reduceMotion = preferences.reduceMotion;
      this.highContrast = preferences.highContrast;
      if (process.client) localStorage.setItem(STORAGE_KEY, this.mode);
      this.applyToDocument();
    },
    async loadPreferences() {
      const version = ++this.preferenceVersion;
      const preferences = await apiClientWithAuth<AppearancePreferences>("/api/me/appearance");
      if (version === this.preferenceVersion) this.applyPreferences(preferences);
    },
    async savePreferences(preferences = this.preferences()) {
      ++this.preferenceVersion;
      await apiClientWithAuth("/api/me/appearance", { method: "PUT", body: preferences });
      this.applyPreferences(preferences);
    },
    toggle() {
      this.set(this.mode === "dark" ? "light" : "dark");
    },
    force(mode: ThemeMode | null) {
      this.forcedMode = mode;
      this.applyToDocument();
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
