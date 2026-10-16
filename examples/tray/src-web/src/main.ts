import { createApp } from "vue";
import App from "./App.vue";
import { runTrayProbe } from "./tray-probe";

import "@unocss/reset/tailwind.css";
import "@proj-airi/ui/main.css";
import "uno.css";

if (new URLSearchParams(window.location.search).has("tray-probe"))
  runTrayProbe().catch(console.error);
else
  createApp(App).mount("#app");
