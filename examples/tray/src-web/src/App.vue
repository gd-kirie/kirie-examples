<script setup lang="ts">
import type { Menu, MenuEntry, Resource, TrayIcon } from "@gd-kirie/tray";
import { createContext } from "@gd-kirie/ipc-eventa";
import { CheckMenuItem, createTrayClient } from "@gd-kirie/tray";
import { defineInvoke, defineInvokeEventa } from "@moeru/eventa";
import Button from "@proj-airi/ui/src/components/misc/button.vue";
import { computed, onBeforeUnmount, onMounted, ref, shallowRef, watch } from "vue";

interface MenuNode {
  key: string;
  type: "item" | "check" | "submenu" | "separator";
  text: string;
  enabled: boolean;
  checked: boolean;
  children: MenuNode[];
}

interface TreeRow {
  node: MenuNode;
  path: number[];
  depth: number;
}

const os = window.kirie?.platform.os;
const isDesktop = os === "linux" || os === "macos" || os === "windows";
const eventa = isDesktop ? createContext() : undefined;
const client = eventa ? createTrayClient(eventa.context) : undefined;
let nextId = 1;
const menuItems = ref<MenuNode[]>([
  createNode({ key: "open", text: "Open Kirie" }),
  createNode({ key: "shared-check", text: "Enabled", type: "check", checked: true }),
  createNode({ key: "separator-main", text: "", type: "separator" }),
  createNode({
    key: "tools",
    text: "Tools",
    type: "submenu",
    children: [
      createNode({ key: "refresh", text: "Refresh" }),
      createNode({ key: "settings", text: "Settings", enabled: false }),
    ],
  }),
  createNode({ key: "quit", text: "Quit" }),
]);
const lastClickedId = ref("");
const tray = shallowRef<TrayIcon>();
const busy = ref(false);
const status = ref(client ? "Creating tray…" : "Tray requires a desktop Kirie host");
const lastEvent = ref("");
const tooltip = ref("Kirie Tray");
const visible = ref(true);
const useTemplateIcon = ref(os === "macos");
let stopTrayActivation: (() => void) | undefined;
let menu: Menu | undefined;
let shared: CheckMenuItem;
let ownedEntries: MenuEntry[] = [];

const iconPath = computed(() => useTemplateIcon.value
  ? "res://tray-template.svg"
  : "res://icon.svg");

function flatten(nodes: MenuNode[], parentPath: number[] = []): TreeRow[] {
  return nodes.flatMap((node, index) => {
    const path = [...parentPath, index];
    return [
      { node, path, depth: parentPath.length },
      ...(node.type === "submenu" ? flatten(node.children, path) : []),
    ];
  });
}

const rows = computed(() => flatten(menuItems.value));

function createNode(options: Partial<MenuNode> = {}): MenuNode {
  const key = `item-${nextId++}`;
  return {
    key,
    type: "item",
    text: "New item",
    enabled: true,
    checked: false,
    children: [],
    ...options,
  };
}

function getSiblings(path: number[]): MenuNode[] {
  let siblings = menuItems.value;
  for (const index of path.slice(0, -1))
    siblings = siblings[index].children;
  return siblings;
}

function addRoot(): void {
  menuItems.value.push(createNode());
}

function addAfter(path: number[]): void {
  getSiblings(path).splice(path.at(-1)! + 1, 0, createNode());
}

function addChild(path: number[]): void {
  const node = getSiblings(path)[path.at(-1)!];
  if (node.type !== "submenu")
    return;

  node.children.push(createNode());
}

function removeItem(path: number[]): void {
  getSiblings(path).splice(path.at(-1)!, 1);
}

function moveItem(path: number[], offset: number): void {
  const siblings = getSiblings(path);
  const index = path.at(-1)!;
  const target = index + offset;
  if (target < 0 || target >= siblings.length)
    return;

  [siblings[index], siblings[target]] = [siblings[target], siblings[index]];
}

function indentItem(path: number[]): void {
  const siblings = getSiblings(path);
  const index = path.at(-1)!;
  if (!canIndent(path))
    return;

  const [node] = siblings.splice(index, 1);
  siblings[index - 1].children.push(node);
}

function outdentItem(path: number[]): void {
  if (path.length < 2)
    return;

  const parentPath = path.slice(0, -1);
  const parentIndex = parentPath.at(-1)!;
  const [node] = getSiblings(path).splice(path.at(-1)!, 1);
  getSiblings(parentPath).splice(parentIndex + 1, 0, node);
}

function canMove(path: number[], offset: number): boolean {
  const target = path.at(-1)! + offset;
  return target >= 0 && target < getSiblings(path).length;
}

function canIndent(path: number[]): boolean {
  const index = path.at(-1)!;
  return index > 0 && getSiblings(path)[index - 1].type === "submenu";
}

function changeType(node: MenuNode, event: Event): void {
  const type = (event.target as HTMLSelectElement).value as MenuNode["type"];
  node.type = type;
  if (type !== "submenu")
    node.children = [];
}

async function createEntries(nodes: MenuNode[], owned: MenuEntry[]): Promise<MenuEntry[]> {
  const items: MenuEntry[] = [];
  for (const node of nodes) {
    const options = { id: node.key, text: node.text, enabled: node.enabled };
    let entry: MenuEntry;
    if (node.type === "check" && node.key === shared.id) {
      await shared.setText(node.text);
      await shared.setEnabled(node.enabled);
      await shared.setChecked(node.checked);
      entry = shared;
    } else {
      switch (node.type) {
        case "separator":
          entry = await client!.predefined.separator();
          break;
        case "submenu":
          entry = await client!.createSubmenu({ ...options, items: await createEntries(node.children, owned) });
          break;
        case "check":
          entry = await client!.createCheckMenuItem({ ...options, checked: node.checked });
          break;
        default:
          entry = await client!.createMenuItem(options);
      }
      owned.push(entry);
    }
    items.push(entry);
  }
  return items;
}

async function closeMenu(target: Menu | undefined, entries: MenuEntry[]): Promise<void> {
  await target?.close();
  for (const entry of entries)
    await entry.close();
}

async function syncTrayMenu(): Promise<void> {
  if (!client || !tray.value)
    return;

  const entries: MenuEntry[] = [];
  let nextMenu: Menu | undefined;
  try {
    const items = await createEntries(menuItems.value, entries);
    nextMenu = await client.createMenu({ items });
    await tray.value.setMenu(nextMenu);
  } catch (error) {
    await closeMenu(nextMenu, entries);
    throw error;
  }

  const previousMenu = menu;
  const previousEntries = ownedEntries;
  menu = nextMenu;
  ownedEntries = entries;
  await closeMenu(previousMenu, previousEntries);
  status.value = "Tray menu synced";
}

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

watch(menuItems, (_current, _previous, onCleanup) => {
  if (!tray.value || busy.value)
    return;

  status.value = "Syncing changes…";
  const timer = window.setTimeout(runAction, 120, syncTrayMenu);
  onCleanup(() => window.clearTimeout(timer));
}, { deep: true });

watch(tooltip, (_current, _previous, onCleanup) => {
  if (!tray.value || busy.value)
    return;

  status.value = "Syncing tooltip…";
  const timer = window.setTimeout(runAction, 120, updateTooltip);
  onCleanup(() => window.clearTimeout(timer));
});

async function initializeTray(): Promise<void> {
  if (!client || !eventa)
    return;

  if (!shared) {
    const startPage = defineInvoke(eventa.context, defineInvokeEventa<Resource, null>("example:tray:start-page"));
    const item = client.fromResource(await startPage(null));
    if (!(item instanceof CheckMenuItem))
      throw new Error("The host did not return a check menu item");
    shared = item;
    const node = rows.value.find(row => row.node.key === shared.id)!.node;
    node.text = await shared.text();
    node.checked = await shared.isChecked();
  }
  tray.value = await client.createTray({
    icon: { kind: "resource", path: iconPath.value },
    iconAsTemplate: useTemplateIcon.value,
    tooltip: tooltip.value || null,
  });
  await syncTrayMenu();
  await tray.value.setVisible(visible.value);
  status.value = "Native tray ready";
}

async function updateIcon(): Promise<void> {
  if (!tray.value)
    return;

  await tray.value.setIcon({ kind: "resource", path: iconPath.value }, useTemplateIcon.value);
  status.value = `Icon set to ${iconPath.value}`;
}

async function updateTooltip(): Promise<void> {
  if (!tray.value)
    return;

  await tray.value.setTooltip(tooltip.value || null);
  status.value = tooltip.value ? "Tooltip updated" : "Tooltip cleared";
}

async function updateVisibility(): Promise<void> {
  if (!tray.value)
    return;

  await tray.value.setVisible(visible.value);
  status.value = visible.value ? "Tray shown" : "Tray hidden";
}

async function destroyTray(): Promise<void> {
  if (!tray.value)
    return;

  await tray.value.close();
  tray.value = undefined;
  await closeMenu(menu, ownedEntries);
  menu = undefined;
  ownedEntries = [];
  status.value = "Tray destroyed";
}

onMounted(() => {
  if (!client)
    return;

  stopTrayActivation = client.onEvent((event) => {
    lastEvent.value = JSON.stringify(event);
    if (event.type !== "menu")
      return;

    lastClickedId.value = event.id;
    const node = rows.value.find(row => row.node.key === event.id)?.node;
    const entry = event.id === shared?.id ? shared : ownedEntries.find(item => item.id === event.id);
    if (node?.type === "check" && entry instanceof CheckMenuItem) {
      entry.isChecked().then((value) => {
        node.checked = value;
      }).catch(reportError);
    }
  });
  runAction(initializeTray);
});

onBeforeUnmount(() => {
  stopTrayActivation?.();
  eventa?.dispose();
});
</script>

<template>
  <main class="min-h-screen bg-neutral-50 p-5 text-neutral-900 md:p-8">
    <div class="mx-auto max-w-5xl flex flex-col gap-5">
      <header class="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 class="text-2xl font-semibold tracking-tight">
            Kirie Tray
          </h1>
          <p class="mt-1 text-sm text-neutral-500">
            Edit the native tray and menu. The shared-check item comes from C#.
          </p>
        </div>
        <p class="font-mono text-xs text-neutral-500" role="status">
          {{ status }}
        </p>
      </header>

      <section class="rounded-2xl border-2 border-neutral-200 bg-white/70 p-5 shadow-sm md:p-6">
        <div class="mb-5 flex flex-wrap items-start justify-between gap-3">
          <div>
            <h2 class="text-base font-semibold">
              Tray controls
            </h2>
            <p class="mt-1 text-xs text-neutral-500">
              Create, update, hide, show, and destroy the native tray.
            </p>
          </div>
          <div class="flex gap-2">
            <button
              class="rounded-lg border border-blue-600 bg-blue-600 px-3 py-2 text-xs text-white font-medium enabled:hover:border-blue-700 enabled:hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-40"
              type="button"
              :disabled="!client || tray !== undefined || busy"
              @click="runAction(initializeTray)"
            >
              Create
            </button>
            <button
              class="rounded-lg border border-neutral-300 bg-white px-3 py-2 text-xs text-red-700 font-medium enabled:hover:border-neutral-400 enabled:hover:bg-neutral-100 disabled:cursor-not-allowed disabled:opacity-40"
              type="button"
              :disabled="!tray || busy"
              @click="runAction(destroyTray)"
            >
              Destroy
            </button>
          </div>
        </div>

        <div class="mb-5 grid gap-3 sm:grid-cols-3">
          <div class="flex flex-col gap-1 rounded-xl border border-neutral-200 bg-neutral-50 p-3 text-sm">
            <span class="text-xs text-neutral-500">Platform</span>
            <strong>{{ os ?? "browser" }}</strong>
          </div>
          <div class="flex flex-col gap-1 rounded-xl border border-neutral-200 bg-neutral-50 p-3 text-sm">
            <span class="text-xs text-neutral-500">Lifecycle</span>
            <strong>{{ tray ? "created" : "not created" }}</strong>
          </div>
          <div class="flex flex-col gap-1 rounded-xl border border-neutral-200 bg-neutral-50 p-3 text-sm">
            <span class="text-xs text-neutral-500">Last activation</span>
            <strong class="font-mono">{{ lastClickedId || "none" }}</strong>
          </div>
        </div>

        <div class="grid gap-4 lg:grid-cols-3">
          <label class="flex flex-col gap-2 text-xs text-neutral-600 font-medium">
            <span>Icon</span>
            <select
              v-model="useTemplateIcon"
              class="rounded-lg border border-neutral-200 bg-neutral-50 px-2.5 py-2 text-neutral-800 outline-none"
              :disabled="!tray || busy"
              @change="runAction(updateIcon)"
            >
              <option :value="false">Regular icon</option>
              <option v-if="os === 'macos'" :value="true">macOS template icon</option>
            </select>
            <code class="text-[11px] text-neutral-500 font-normal">{{ iconPath }}</code>
          </label>

          <label class="flex flex-col gap-2 text-xs text-neutral-600 font-medium">
            <span>Tooltip</span>
            <input
              v-model="tooltip"
              class="rounded-lg border border-neutral-200 bg-neutral-50 px-2.5 py-2 text-neutral-800 outline-none"
              :disabled="!tray || busy"
              placeholder="Clear the field to remove the tooltip"
            >
          </label>

          <label class="flex flex-col gap-2 text-xs text-neutral-600 font-medium">
            <span>Visibility</span>
            <span class="min-h-9 inline-flex items-center gap-1.5 text-xs text-neutral-600 font-normal">
              <input
                v-model="visible"
                type="checkbox"
                :disabled="!tray || busy"
                @change="runAction(updateVisibility)"
              >
              {{ visible ? "Visible" : "Hidden" }}
            </span>
          </label>
        </div>
      </section>

      <fieldset :disabled="busy || !client" class="min-w-0 rounded-2xl border-2 border-neutral-200 bg-white/70 p-5 shadow-sm md:p-6">
        <div class="mb-4 flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 class="text-base font-semibold">
              Menu tree
            </h2>
            <p class="mt-1 text-xs text-neutral-500">
              Edit item, check, submenu, separator, enabled, and checked states.
            </p>
          </div>
          <Button label="Add root item" size="sm" variant="primary" @click="addRoot" />
        </div>

        <div v-if="rows.length" class="flex flex-col gap-2">
          <article
            v-for="row in rows"
            :key="row.node.key"
            class="block gap-3 rounded-xl border border-neutral-200 bg-white p-3 shadow-sm md:flex md:items-center"
            :style="{ marginLeft: `${row.depth * 20}px` }"
          >
            <div class="min-w-0 flex flex-1 flex-wrap items-center gap-2">
              <span class="w-14 shrink-0 font-mono text-xs text-neutral-400">
                {{ row.depth === 0 ? "root" : `level ${row.depth}` }}
              </span>
              <select
                :value="row.node.type"
                :aria-label="`Type for ${row.node.text || 'separator'}`"
                class="rounded-lg border border-neutral-200 bg-neutral-50 px-2 py-2 text-sm outline-none"
                @change="changeType(row.node, $event)"
              >
                <option value="item">
                  Item
                </option>
                <option value="check">
                  Check
                </option>
                <option value="submenu">
                  Submenu
                </option>
                <option value="separator">
                  Separator
                </option>
              </select>
              <input
                v-if="row.node.type !== 'separator'"
                v-model="row.node.text"
                :aria-label="`Label for ${row.node.text}`"
                class="min-w-32 flex-1 rounded-lg border border-neutral-200 bg-neutral-50 px-3 py-2 text-sm outline-none transition focus:border-primary-400 focus:ring-2 focus:ring-primary-100"
              >
              <span v-else class="min-w-32 flex-1 text-sm text-neutral-400">
                Native separator
              </span>
              <label v-if="row.node.type !== 'separator'" class="min-h-9 inline-flex items-center gap-1.5 text-xs text-neutral-600 font-normal">
                <input v-model="row.node.enabled" type="checkbox">
                Enabled
              </label>
              <label v-if="row.node.type === 'check'" class="min-h-9 inline-flex items-center gap-1.5 text-xs text-neutral-600 font-normal">
                <input v-model="row.node.checked" type="checkbox">
                Checked
              </label>
              <span
                v-if="row.node.key === lastClickedId"
                class="shrink-0 rounded-md bg-emerald-50 px-2 py-1 text-xs font-medium text-emerald-700"
              >
                Last clicked
              </span>
            </div>
            <div class="mt-2 flex flex-wrap justify-end gap-1.5 md:mt-0 md:w-96 md:shrink-0">
              <button class="rounded-lg border border-neutral-200 px-2.5 py-1.5 text-xs leading-4 text-neutral-600 transition-colors enabled:hover:border-neutral-400 enabled:hover:bg-neutral-100 disabled:cursor-not-allowed disabled:opacity-35" type="button" @click="addAfter(row.path)">
                Add after
              </button>
              <button class="rounded-lg border border-neutral-200 px-2.5 py-1.5 text-xs leading-4 text-neutral-600 transition-colors enabled:hover:border-neutral-400 enabled:hover:bg-neutral-100 disabled:cursor-not-allowed disabled:opacity-35" type="button" :disabled="row.node.type !== 'submenu'" @click="addChild(row.path)">
                Add child
              </button>
              <button class="rounded-lg border border-neutral-200 px-2.5 py-1.5 text-xs leading-4 text-neutral-600 transition-colors enabled:hover:border-neutral-400 enabled:hover:bg-neutral-100 disabled:cursor-not-allowed disabled:opacity-35" type="button" :disabled="!canMove(row.path, -1)" @click="moveItem(row.path, -1)">
                Up
              </button>
              <button class="rounded-lg border border-neutral-200 px-2.5 py-1.5 text-xs leading-4 text-neutral-600 transition-colors enabled:hover:border-neutral-400 enabled:hover:bg-neutral-100 disabled:cursor-not-allowed disabled:opacity-35" type="button" :disabled="!canMove(row.path, 1)" @click="moveItem(row.path, 1)">
                Down
              </button>
              <button class="rounded-lg border border-neutral-200 px-2.5 py-1.5 text-xs leading-4 text-neutral-600 transition-colors enabled:hover:border-neutral-400 enabled:hover:bg-neutral-100 disabled:cursor-not-allowed disabled:opacity-35" type="button" :disabled="!canIndent(row.path)" @click="indentItem(row.path)">
                Indent
              </button>
              <button class="rounded-lg border border-neutral-200 px-2.5 py-1.5 text-xs leading-4 text-neutral-600 transition-colors enabled:hover:border-neutral-400 enabled:hover:bg-neutral-100 disabled:cursor-not-allowed disabled:opacity-35" type="button" :disabled="row.depth === 0" @click="outdentItem(row.path)">
                Outdent
              </button>
              <button class="rounded-lg border border-neutral-200 px-2.5 py-1.5 text-xs leading-4 text-red-700 transition-colors enabled:hover:border-neutral-400 enabled:hover:bg-neutral-100" type="button" @click="removeItem(row.path)">
                Delete
              </button>
            </div>
          </article>
        </div>

        <div v-else class="rounded-xl border border-dashed border-neutral-300 p-8 text-center">
          <p class="text-sm text-neutral-500">
            The tray menu is empty.
          </p>
          <button class="mt-3 text-sm font-medium text-primary-600" type="button" @click="addRoot">
            Add the first item
          </button>
        </div>
      </fieldset>

      <section>
        <h2 class="mb-2 text-base font-semibold">
          Last native event
        </h2>
        <pre class="overflow-auto rounded-lg bg-neutral-100 p-3 text-xs">{{ lastEvent || "Click the tray or a menu item." }}</pre>
      </section>
    </div>
  </main>
</template>
