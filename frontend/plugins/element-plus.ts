import ElementPlus from "element-plus";
import * as ElementPlusIconsVue from "@element-plus/icons-vue";
import type { App } from "vue";

export default defineNuxtPlugin((nuxtApp) => {
  const app = nuxtApp.vueApp as App;
  app.use(ElementPlus);

  for (const [key, component] of Object.entries(ElementPlusIconsVue)) {
    app.component(key, component as never);
  }
});
