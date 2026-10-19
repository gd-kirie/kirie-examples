import qr from "@gd-kirie/qr";
import Vue from "@vitejs/plugin-vue";
import { defineKirieConfig } from "kirie";
import core from "kirie/plugin/core";
import UnoCSS from "unocss/vite";

export default defineKirieConfig({
  plugins: [core, qr],
  godot: { csproj: "KirieQr.csproj" },
  web: {
    vite: {
      plugins: [Vue(), UnoCSS()],
      build: { emptyOutDir: true, sourcemap: true },
    },
  },
});
