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
  css: [
    "~/assets/styles/main.scss",
    "element-plus/dist/index.css",
    // Variáveis do Element Plus para `html.dark` (ElMessage, popovers, etc.).
    "element-plus/theme-chalk/dark/css-vars.css"
  ],
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
      ],
      script: [
        {
          // Aplica o tema salvo antes da primeira pintura (mesma regra de stores/theme.ts:
          // chave "ism-theme", padrão escuro e /login sempre escuro), evitando o flash do tema errado.
          innerHTML:
            "(function(){try{var d=location.pathname==='/login'||localStorage.getItem('ism-theme')!=='light';" +
            "var r=document.documentElement;r.classList.add(d?'dark':'light');r.style.colorScheme=d?'dark':'light'}catch(e){}})()"
        }
      ]
    }
  }
});
