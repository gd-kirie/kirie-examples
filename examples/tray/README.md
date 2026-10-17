# Tray browser example

This desktop example shares a C# check item with a browser through Eventa.
The browser creates a tray menu, edits the shared item, and receives native events.

Published Tray 0.13.0 predates this API. This example requires a matching Tray release.
The core npm and NuGet dependencies remain at 0.13.0.

Run setup from the repository root:

```sh
mise x -- pnpm install --frozen-lockfile
mise x -- pnpm -C examples/tray exec kirie doctor --fix plugin:core
mise x -- pnpm -C examples/tray exec kirie doctor --fix plugin:tray
mise x -- pnpm -C examples/tray exec kirie build dotnet
mise x -- pnpm -C examples/tray run dev
```

The CLI installs the addons from npm. The `GdKirie.Tray` NuGet package includes the Eventa adapter.
The browser handles menu events by their native IDs:

```ts
const stop = client.onEvent((event) => {
  if (event.type === "menu" && event.id === shared.id)
    console.log(event.id);
});
// Call stop() before disposing the browser Eventa context.
```

Run the browser probe after setup:

```sh
mise run test:tray-desktop
```

The probe covers shared state, reference release, and synthetic global signals.
It does not test real system clicks. Tray events and menu behavior depend on the desktop platform.
Closing a handle does not cancel global events. Call the unsubscribe function explicitly.
See the [native C# example](../tray-native/README.md).
