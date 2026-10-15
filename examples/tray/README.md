# tray

A desktop example for the Kirie Tray plugin. It demonstrates the Tray lifecycle,
an editable native menu, and menu activation events.

Run these setup commands from the repository root:

```sh
mise x -- pnpm install
mise x -- pnpm -C examples/tray exec kirie doctor --fix
mise x -- pnpm -C examples/tray exec kirie build dotnet
```

Start the Kirie-managed Vite server and Godot project together:

```sh
mise x -- pnpm -C examples/tray run dev
```

The CLI installs the core and Tray addons from the published packages.
The Tray package includes the native libraries for all supported desktop targets.
Godot loads and exports the library for the selected platform.
