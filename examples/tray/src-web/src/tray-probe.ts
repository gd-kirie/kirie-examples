import { sendText } from "@gd-kirie/ipc";
import { createContext } from "@gd-kirie/ipc-eventa";
import { CheckMenuItem, createTrayClient, TrayError } from "@gd-kirie/tray";
import { defineInvoke, defineInvokeEventa, defineInvokes } from "@moeru/eventa";
import { bootstrap, probeEvents } from "./bootstrap";

type AdapterReply = { ok: true; value: unknown } | { ok: false; error: { code: string; message: string } };

function assert(condition: boolean, message: string): asserts condition {
  if (!condition)
    throw new Error(message);
}

async function rejectsClosedHandle(request: Promise<unknown>): Promise<void> {
  try {
    await request;
  } catch (error) {
    assert(error instanceof TrayError && error.code === "invalid-handle", "Closed handle lost its native error");
    return;
  }
  throw new Error("A closed handle unexpectedly succeeded");
}

export async function runTrayProbe(): Promise<void> {
  const eventa = createContext();
  const client = createTrayClient(eventa.context);
  let stopEvents: (() => void) | undefined;
  let result = "kirie-tray-probe:passed";
  try {
    const shared = client.fromResource(await defineInvoke(eventa.context, bootstrap)({}));
    assert(shared instanceof CheckMenuItem && shared.id === "shared-check", "C# check item was not exposed");
    assert(await shared.isChecked(), "C# initial state was not preserved");
    let sharedEvents = 0;
    let unexposedEvents = 0;
    let trayEvents = 0;
    let eventError = "";
    stopEvents = client.onEvent((event) => {
      if ("handle" in event || "item" in event || "tray" in event)
        eventError = "Global events contain browser resources";
      if (event.type === "menu") {
        if (event.id === shared.id)
          sharedEvents++;
        if (event.id === "unexposed-item")
          unexposedEvents++;
      } else if (event.id === "unexposed-tray") {
        if (event.type !== "Click" || event.button !== "Left" || event.buttonState !== "Up" ||
          event.position.x !== 12 || event.position.y !== 34 ||
          event.rect.position.x !== 10 || event.rect.position.y !== 30 ||
          event.rect.size.width !== 16 || event.rect.size.height !== 16) {
          eventError = "Tray events changed upstream names or geometry";
        }
        trayEvents++;
      }
    });
    function assertEvents(batches: number): void {
      assert(sharedEvents === batches && unexposedEvents === batches * 2 && trayEvents === batches && !eventError, eventError || "Global event delivery or unsubscribe failed");
    }
    const emitEvents = defineInvoke(eventa.context, probeEvents);
    await emitEvents({});
    assertEvents(1);
    const submenu = await client.createSubmenu({ id: "probe-submenu", text: "Tools", items: [shared] });
    const menu = await client.createMenu({ id: "probe-menu", items: [submenu] });
    const tray = await client.createTray({
      id: "probe-tray",
      icon: { kind: "resource", path: "res://icon.svg" },
      menu,
    });
    assert(submenu.id === "probe-submenu" && menu.id === "probe-menu" && tray.id === "probe-tray", "Native creation IDs were not forwarded");
    const first = await client.createMenuItem({ id: "same-id", text: "First" });
    const second = await client.createMenuItem({ id: first.id, text: "Second" });
    assert(first.id === second.id && first.handle !== second.handle, "Native IDs became unique browser identities");

    const shortcut = await client.createMenuItem({ text: "Shortcut", accelerator: { kind: "physical", value: "Ctrl+KeyA" } });
    await shortcut.setAccelerator({ kind: "logical", value: "Ctrl+a" });
    await shortcut.setAccelerator(null);
    await client.predefined.separator();
    await client.predefined.sectionHeader("Actions");
    await client.predefined.about(undefined, { name: "Kirie Tray" });
    const copy = await client.predefined.copy();
    const defaultCopy = await client.predefined.copy(null);
    const emptyCopy = await client.predefined.copy("");
    assert(await copy.text() === await defaultCopy.text() && await emptyCopy.text() === "", "Predefined text defaults changed");
    if (window.kirie?.platform.os === "macos") {
      const icon = await client.createIconMenuItem({ text: "Native icon", icon: { kind: "native", name: "Folder" } });
      await icon.setIcon({ kind: "native", name: "StatusAvailable" });
    }

    const [queried] = await submenu.items();
    const [queriedAgain] = await submenu.items();
    assert(queried.handle === shared.handle && queriedAgain.handle === shared.handle, "Repeated queries created duplicate browser handles");
    await queried.setText("Changed in browser");
    await shared.setChecked(false);
    assert(await queriedAgain.text() === "Changed in browser", "Query wrappers did not share native state");
    await shared.close();
    await rejectsClosedHandle(queried.text());
    await tray.close();
    await emitEvents({});
    assertEvents(2);
    stopEvents();
    await emitEvents({});
    assertEvents(2);
    const [retained] = await submenu.items();
    assert(retained instanceof CheckMenuItem && retained.handle !== shared.handle, "Parent query did not rebind the retained native item");
    assert(!await retained.isChecked(), "Closing a browser handle lost native state");
    const removed = await submenu.removeAt(0);
    assert(removed?.handle === retained.handle, "removeAt did not reuse the live handle");
    assert((await submenu.items()).length === 0, "removeAt did not remove the item");
    await submenu.append(retained);
    const replacement = await client.createTray({ id: tray.id, menu });
    assert(replacement.id === tray.id, "The upstream ID did not survive tray recreation");
    await replacement.setVisible(false);
    await replacement.setVisible(true);
    await replacement.setMenu(null);
    await replacement.setMenu(menu);

    const emptyMenu = await client.createMenu();
    assert((await emptyMenu.items()).length === 0, "Omitted items did not create an empty menu");
    for (const enabled of [undefined, true, false]) {
      const options = { text: "Default probe", enabled };
      const emptySubmenu = await client.createSubmenu(options);
      assert((await emptySubmenu.items()).length === 0, "Omitted items did not create an empty submenu");
      const entries = [
        emptySubmenu,
        await client.createMenuItem(options),
        await client.createCheckMenuItem(options),
        await client.createIconMenuItem(options),
      ];
      for (const entry of entries)
        assert(await entry.isEnabled() === (enabled ?? true), `${entry.kind} lost enabled=${enabled}`);
    }

    const raw = defineInvokes(eventa.context, {
      setIcon: defineInvokeEventa<AdapterReply, unknown>("kirie:tray:tray-set-icon"),
      setMenu: defineInvokeEventa<AdapterReply, unknown>("kirie:tray:tray-set-menu"),
      setVisible: defineInvokeEventa<AdapterReply, unknown>("kirie:tray:tray-set-visible"),
      setAccelerator: defineInvokeEventa<AdapterReply, unknown>("kirie:tray:item-set-accelerator"),
      createCheck: defineInvokeEventa<AdapterReply, unknown>("kirie:tray:check-create"),
      createMenu: defineInvokeEventa<AdapterReply, unknown>("kirie:tray:menu-create"),
      createSubmenu: defineInvokeEventa<AdapterReply, unknown>("kirie:tray:submenu-create"),
      createTray: defineInvokeEventa<AdapterReply, unknown>("kirie:tray:tray-create"),
    });
    const malformed = [
      await raw.setAccelerator({ handle: shortcut.handle, accelerator: { kind: "physical", value: "Ctrl+Shift+C+A" } }),
      await raw.setAccelerator({ handle: shortcut.handle, accelerator: { kind: "logical", value: "Ctrl+Shift+C+A" } }),
      await raw.setIcon({ handle: replacement.handle }),
      await raw.setMenu({ handle: replacement.handle }),
      await raw.setVisible({ handle: replacement.handle, visible: "yes" }),
      await raw.createCheck({ options: { checked: true } }),
      await raw.createCheck({ options: { text: "Invalid enabled", enabled: null } }),
      await raw.createMenu({ options: { items: null } }),
      await raw.createSubmenu({ options: { text: "Invalid items", items: null } }),
      await raw.createSubmenu({ options: { text: "Invalid enabled", enabled: null } }),
    ];
    for (const reply of malformed)
      assert(!reply.ok && reply.error.code === "invalid-argument", "Malformed input lost its adapter error");

    const nullOptions = await raw.createTray({ options: { menuOnLeftClick: null, menuOnRightClick: null } });
    assert(nullOptions.ok, "Nullable builder booleans did not preserve upstream defaults");

    const resetShared = client.fromResource(await defineInvoke(eventa.context, bootstrap)({}));
    await rejectsClosedHandle(replacement.setVisible(false));
    assert(resetShared instanceof CheckMenuItem && !await resetShared.isChecked(), "Adapter replacement lost the C# object");
    assert(await resetShared.text() === "Changed in browser", "Adapter replacement lost browser edits");
  } catch (error) {
    result = `kirie-tray-probe:failed:${error instanceof Error ? error.message : String(error)}`;
  } finally {
    stopEvents?.();
    eventa.dispose();
  }
  sendText(result);
}
