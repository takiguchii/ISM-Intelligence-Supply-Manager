export default defineNuxtConfig({
  compatibilityDate: "2025-01-01",
  devtools: { enabled: true },
  modules: ["@nuxtjs/tailwindcss"],
  css: ["~/assets/styles/main.scss", "element-plus/dist/index.css"],
  runtimeConfig: {
    apiBase: process.env.NUXT_API_BASE ?? "http://backend:8080",
    public: {
      appName: "ISM - Intelligence Supply Manager"
    }
  },
  app: {
    head: {
      title: "ISM - Intelligence Supply Manager",
      meta: [
        {
          name: "description",
          content: "Foundation environment for the ISM vertical SaaS platform."
        }
      ]
    }
  }
});
