import { computed } from "vue";
import { useThemeStore } from "~/stores/theme";

/**
 * Cores dos gráficos (Chart.js) conforme o tema ativo.
 * O canvas não herda CSS, então eixos, grade, tooltip e séries precisam ser informados explicitamente.
 */
export const useChartTheme = () => {
  const themeStore = useThemeStore();

  return computed(() =>
    themeStore.isDark
      ? {
          tick: "rgb(113, 113, 122)",
          tickStrong: "rgb(212, 212, 216)",
          grid: "rgba(63, 63, 70, 0.5)",
          pointFill: "rgb(9, 9, 11)",
          positive: "52, 211, 153",
          warning: "251, 191, 36",
          tooltip: {
            backgroundColor: "rgb(24, 24, 27)",
            borderColor: "rgb(63, 63, 70)",
            titleColor: "rgb(228, 228, 231)",
            bodyColor: "rgb(161, 161, 170)"
          }
        }
      : {
          tick: "rgb(113, 113, 122)",
          tickStrong: "rgb(63, 63, 70)",
          grid: "rgb(228, 228, 231)",
          pointFill: "rgb(255, 255, 255)",
          positive: "16, 185, 129",
          warning: "245, 158, 11",
          tooltip: {
            backgroundColor: "rgb(255, 255, 255)",
            borderColor: "rgb(228, 228, 231)",
            titleColor: "rgb(24, 24, 27)",
            bodyColor: "rgb(82, 82, 91)"
          }
        }
  );
};
