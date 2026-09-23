export default defineNuxtConfig({
  compatibilityDate: "2025-01-01",
  devtools: { enabled: true },
  components: [
    {
      path: "~/components",
      pathPrefix: false
    }
  ],
  modules: ["@nuxtjs/tailwindcss", "@pinia/nuxt"],
  css: ["~/assets/styles/main.scss", "element-plus/dist/index.css"],
  runtimeConfig: {
    apiBase: process.env.NUXT_API_BASE ?? "http://backend:8080",
    public: {
      appName: "ISM - Intelligence Supply Manager",
      apiBase: process.env.NUXT_PUBLIC_API_BASE ?? "http://localhost:8080"
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
      ],
      link: [
        { rel: "icon", type: "image/png", href: "/favicon.png" }
      ]
    }
  }
});
