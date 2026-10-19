<script setup lang="ts">
import type { QrClient } from "@gd-kirie/qr";
import { createContext } from "@gd-kirie/ipc-eventa";
import { createQrClient } from "@gd-kirie/qr";
import Button from "@proj-airi/ui/src/components/misc/button.vue";
import { onBeforeUnmount, ref } from "vue";

const os = window.kirie?.platform.os;
const supported = os === "android" || os === "ios";
const eventa = supported ? createContext() : undefined;
const qr = eventa ? createQrClient(eventa.context) : undefined;
const text = ref("Hello from Kirie");
const imageUrl = ref("");
const imageName = ref("");
const result = ref("");
const busy = ref(false);
const status = ref(qr ? "Ready" : "QR requires Android or iOS. Desktop and browser previews are unsupported.");
let image: Uint8Array | undefined;

function setImage(png: Uint8Array, name: string): void {
  if (imageUrl.value)
    URL.revokeObjectURL(imageUrl.value);
  image = png;
  imageUrl.value = URL.createObjectURL(new Blob([new Uint8Array(png)], { type: "image/png" }));
  imageName.value = name;
  result.value = "";
}

async function run(operation: (client: QrClient) => Promise<void>): Promise<void> {
  if (!qr || busy.value)
    return;
  busy.value = true;
  result.value = "";
  status.value = "Working…";
  try {
    await operation(qr);
  } catch (error) {
    status.value = typeof error === "object" && error !== null && "message" in error && typeof error.message === "string"
      ? error.message
      : String(error);
  } finally {
    busy.value = false;
  }
}

function generate(): Promise<void> {
  return run(async (qr) => {
    setImage(await qr.generateImage({ text: text.value }), "Generated QR image");
    status.value = "Image generated";
  });
}

async function selectImage(event: Event): Promise<void> {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0];
  if (!file)
    return;
  await run(async () => {
    setImage(new Uint8Array(await file.arrayBuffer()), file.name);
    status.value = "Image selected";
  });
  input.value = "";
}

function scanImage(): Promise<void> {
  return run(async (qr) => {
    if (!image)
      return;
    const decoded = await qr.scanImage(image);
    result.value = decoded ?? "";
    status.value = decoded === null ? "No QR code found" : "Image decoded";
  });
}

function scanCamera(): Promise<void> {
  return run(async (qr) => {
    const decoded = await qr.scan();
    result.value = decoded ?? "";
    status.value = decoded === null ? "Camera scan canceled" : "Camera scan completed";
  });
}

onBeforeUnmount(() => {
  if (imageUrl.value)
    URL.revokeObjectURL(imageUrl.value);
  eventa?.dispose();
});
</script>

<template>
  <main class="min-h-screen bg-neutral-50 p-4 text-neutral-900 sm:p-6 md:p-8">
    <div class="mx-auto max-w-4xl flex flex-col gap-4">
      <header>
        <h1 class="text-2xl font-semibold tracking-tight">
          Kirie QR
        </h1>
        <p class="mt-1 text-sm text-neutral-500">
          Generate a QR image, decode a PNG, or scan one code with the camera.
        </p>
      </header>

      <output class="rounded-lg border border-neutral-200 bg-white px-3 py-2 text-sm text-neutral-600" aria-live="polite">
        {{ status }}
      </output>

      <section class="grid gap-5 rounded-2xl border-2 border-neutral-200 bg-white/70 p-4 shadow-sm md:grid-cols-2 md:p-6" :aria-busy="busy">
        <div class="flex flex-col gap-4">
          <form class="flex flex-col gap-4" @submit.prevent="generate">
            <h2 class="text-base font-semibold">
              QR image
            </h2>
            <label class="flex flex-col gap-1.5 text-xs text-neutral-600 font-medium">
              <span>Text to encode</span>
              <textarea
                v-model="text"
                class="min-h-24 resize-y rounded-lg border-2 border-neutral-100 bg-neutral-50 px-3 py-2 text-sm text-neutral-900 outline-none focus:border-primary-300 focus:bg-white"
                :disabled="busy"
                required
              />
            </label>
            <Button block :disabled="!qr || busy" label="Generate PNG" size="sm" type="submit" variant="primary" />
          </form>

          <label class="flex flex-col gap-1.5 text-xs text-neutral-600 font-medium">
            <span>Or select a PNG image</span>
            <input accept="image/png" class="text-sm" :disabled="!qr || busy" type="file" @change="selectImage">
          </label>
          <Button block :disabled="!qr || !imageUrl || busy" label="Scan this image" size="sm" variant="secondary" @click="scanImage" />

          <div class="border-t border-neutral-200 pt-4">
            <h2 class="text-base font-semibold">
              Camera
            </h2>
            <p class="mb-3 mt-1 text-xs text-neutral-500">
              The camera closes after the first QR code. The scan page also has a cancel button.
            </p>
            <Button block :disabled="!qr || busy" label="Scan with camera" size="sm" variant="primary" @click="scanCamera" />
          </div>
        </div>

        <aside class="flex flex-col gap-4 rounded-xl border border-neutral-200 bg-neutral-50 p-4">
          <h2 class="text-base font-semibold">
            Image and result
          </h2>
          <figure v-if="imageUrl" class="flex flex-col items-center gap-2">
            <img :src="imageUrl" alt="Current PNG for QR recognition" class="max-h-80 max-w-full rounded-lg bg-white object-contain">
            <figcaption class="break-all text-xs text-neutral-500">
              {{ imageName }}
            </figcaption>
          </figure>
          <p v-else class="text-sm text-neutral-500">
            Generate or select an image to see its preview.
          </p>
          <output v-if="result" class="whitespace-pre-wrap break-all rounded-lg border border-neutral-200 bg-white p-3 text-sm" aria-live="polite">
            {{ result }}
          </output>
        </aside>
      </section>
    </div>
  </main>
</template>
