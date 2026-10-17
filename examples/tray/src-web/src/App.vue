<script setup lang="ts">
import type { CheckMenuItem, Menu, MenuItem, TrayIcon } from "@gd-kirie/tray";
import { createContext } from "@gd-kirie/ipc-eventa";
import { createTrayClient } from "@gd-kirie/tray";
import { defineInvoke } from "@moeru/eventa";
import Button from "@proj-airi/ui/src/components/misc/button.vue";
import { onBeforeUnmount, onMounted, ref, shallowRef } from "vue";
import { bootstrap } from "./bootstrap";

const os = window.kirie?.platform.os;
const isDesktop = os === "linux" || os === "macos" || os === "windows";
const eventa = isDesktop ? createContext() : undefined;
const client = eventa ? createTrayClient(eventa.context) : undefined;
const status = ref(client ? "Connecting…" : "Tray requires a desktop Kirie host");
const busy = ref(false);
const text = ref("");
const checked = ref(false);
const visible = ref(true);
const included = ref(true);
const lastEvent = ref("");
const tray = shallowRef<TrayIcon>();
let shared: CheckMenuItem;
let menu: Menu;
let item: MenuItem;
let stopEvents: (() => void) | undefined;

function reportError(error: unknown): void {
  console.error(error);
  status.value = error instanceof Error ? error.message : String(error);
}

function runAction(action: () => Promise<void>): void {
  if (busy.value)
    return;

  busy.value = true;
  action().catch(reportError).finally(() => {
    busy.value = false;
  });
}

async function initialize(): Promise<void> {
  if (!client || !eventa)
    return;

  shared = client.fromResource(await defineInvoke(eventa.context, bootstrap)({})) as CheckMenuItem;
  text.value = await shared.text();
  checked.value = await shared.isChecked();
  item = await client.createMenuItem({ text: "Created in browser" });
  menu = await client.createMenu({ items: [shared, item] });
  tray.value = await client.createTray({
    icon: { kind: "resource", path: "res://icon.svg" },
    menu,
    tooltip: "Kirie Tray",
  });
  stopEvents = client.onEvent((event) => {
    lastEvent.value = JSON.stringify(event);
    if (event.type === "menu" && event.id === shared.id)
      shared.isChecked().then((value) => { checked.value = value; }).catch(reportError);
  });
  status.value = "Native tray ready";
}

async function editShared(): Promise<void> {
  await shared.setText(text.value);
  await shared.setChecked(checked.value);
  status.value = "Updated the C# check item";
}

async function toggleItem(): Promise<void> {
  if (included.value)
    await menu.remove(item);
  else
    await menu.append(item);

  included.value = !included.value;
}

async function toggleVisible(): Promise<void> {
  await tray.value!.setVisible(!visible.value);
  visible.value = !visible.value;
}

async function closeTray(): Promise<void> {
  await tray.value!.close();
  tray.value = undefined;
  status.value = "Tray closed. Reload to create another.";
}

onMounted(() => runAction(initialize));
onBeforeUnmount(() => {
  stopEvents?.();
  eventa?.dispose();
});
</script>

<template>
  <main class="min-h-screen bg-neutral-50 p-6 text-neutral-900">
    <div class="mx-auto max-w-xl flex flex-col gap-5">
      <header>
        <h1 class="text-2xl font-semibold">
          Kirie Tray
        </h1>
        <p class="mt-2 text-sm text-neutral-500">
          C# owns a check item. The browser adds it to a native tray menu and edits it.
        </p>
        <p class="mt-3 text-sm" role="status">
          {{ status }}
        </p>
      </header>

      <section class="rounded-xl border border-neutral-200 bg-white p-5">
        <h2 class="mb-4 font-semibold">
          Shared check item
        </h2>
        <label class="flex flex-col gap-2 text-sm">
          Text
          <input v-model="text" class="rounded-lg border border-neutral-200 px-3 py-2" :disabled="busy || !tray">
        </label>
        <label class="my-4 flex items-center gap-2 text-sm">
          <input v-model="checked" type="checkbox" :disabled="busy || !tray">
          Checked
        </label>
        <Button label="Apply changes" size="sm" :disabled="busy || !tray" @click="runAction(editShared)" />
      </section>

      <section class="flex flex-wrap gap-2">
        <Button :label="included ? 'Remove browser item' : 'Add browser item'" size="sm" :disabled="busy || !tray" @click="runAction(toggleItem)" />
        <Button :label="visible ? 'Hide tray' : 'Show tray'" size="sm" :disabled="busy || !tray" @click="runAction(toggleVisible)" />
        <Button label="Close tray" size="sm" :disabled="busy || !tray" @click="runAction(closeTray)" />
      </section>

      <section>
        <h2 class="mb-2 font-semibold">
          Last native event
        </h2>
        <pre class="overflow-auto rounded-lg bg-neutral-100 p-3 text-xs">{{ lastEvent || 'Click the tray or a menu item.' }}</pre>
        <p class="mt-3 text-xs text-neutral-500">
          Reloading replaces the browser tray objects. Closing the host releases them.
        </p>
      </section>
    </div>
  </main>
</template>
