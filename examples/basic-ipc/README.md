# basic-ipc

This example is the first runnable Kirie integration target.

It is intentionally small and focuses on one path:

1. Godot creates a single WebView
2. the loaded page sends `web_ready` to Godot
3. Godot replies with `godot_ready`
4. both sides log the exchanged payloads

The example now supports two ways to exercise that path:

1. a manual remote page served by the Vite dev server
2. the local probe page loaded from the built `res://src-web/dist/index.html`

## Layout

- `project.godot`
  the Godot project
- `main.tscn`
  the main test scene
- `scripts/main.gd`
  the scene logic
- `src-web`
  a small Vite app for WebView IPC testing

## Running the web side

Install workspace dependencies from the repository root:

```sh
mise x -- pnpm install --frozen-lockfile
```

Run the Vite dev server and use that URL in the project UI.

Example:

```sh
mise x -- pnpm -F @gd-kirie/basic-ipc-web run dev
```

Then open the Godot project and use:

`http://10.0.2.2:5173/`

for an Android emulator, or an appropriate LAN/local address for a device.

## Running the inline probe

Build the web app first:

```sh
mise x -- pnpm -F @gd-kirie/basic-ipc-web run build
```

Open the Godot project and press `Run Probe`.

This creates a WebView if needed, loads
`res://src-web/dist/?mode=probe`, and performs a minimal round-trip:

1. probe sends `web_ready`
2. Godot replies with `godot_ready`
3. probe replies with `web_ack`

## Running the Godot side

Recommended to use [mise](https://mise.jdx.dev/) to manage Godot versions.
Install the Kirie and Godot CEF addons before opening the project:

```sh
mise x -- pnpm exec kirie doctor --fix plugin:core --project examples/basic-ipc
```

Run the Godot editor with the current project:


```bash
mise x -- godot ./examples/basic-ipc/project.godot
```

You can also run the game directly from the command line:

```bash
mise x -- godot --path ./examples/basic-ipc
```

## Running an exported mobile app

Build, install, and launch the Android example:

```bash
mise x -- pnpm exec kirie export android --project examples/basic-ipc
mise x -- pnpm exec kirie run android --project examples/basic-ipc
```

Build, install, and launch the iOS simulator example:

```bash
mise x -- pnpm exec kirie export ios --project examples/basic-ipc
mise x -- pnpm exec kirie run ios --project examples/basic-ipc
```

For a physical iOS device, pass `--device <device-udid>` to both commands.
Xcode uses the signing identity and provisioning profile from your account.
