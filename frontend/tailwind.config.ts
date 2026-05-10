import type { Config } from "tailwindcss";

export default <Partial<Config>>{
  theme: {
    extend: {
      colors: {
        abyss: "#050816",
        panel: "#10182b",
        ember: "#ff8c42",
        aqua: "#54f3d0",
        mist: "#9eb0d1"
      },
      boxShadow: {
        glow: "0 0 40px rgba(84, 243, 208, 0.18)",
        ember: "0 0 45px rgba(255, 140, 66, 0.18)"
      },
      fontFamily: {
        display: ["Space Grotesk", "sans-serif"],
        mono: ["IBM Plex Mono", "monospace"]
      }
    }
  }
};
