# Tray browser example

This desktop example shares a C# check item with a browser through Eventa.
The browser creates a tray menu, edits the shared item, and receives native events.

Run setup from the repository root:

```sh
mise x -- pnpm install --frozen-lockfile
mise x -- pnpm -C examples/tray exec kirie doctor --fix plugin:core
mise x -- pnpm -C examples/tray exec kirie doctor --fix plugin:tray
mise x -- pnpm -C examples/tray exec kirie build dotnet
mise x -- pnpm -C examples/tray run dev
```

The CLI installs the core and Tray addons from npm packages.
The browser example uses the `GdKirie.Tray.EventaAdapter` NuGet package.

A bootstrap request replaces the previous browser adapter after a reload.
The C# check item remains alive. Host shutdown releases the adapter and its objects.

Run the browser probe after setup:

```sh
mise run test:tray-desktop
```

The probe checks shared-object edits, handle reuse, native IDs, and boundary errors.
Native clicks and platform-specific behavior need desktop acceptance tests.
See the [Tray API and platform limits](https://github.com/gd-kirie/kirie-plugins/tree/main/packages/tray)
and the [native C# example](../tray-native/README.md).
