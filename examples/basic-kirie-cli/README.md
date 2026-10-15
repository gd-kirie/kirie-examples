# basic-kirie-cli

This example is the first runnable Kirie CLI-managed project.

It is intentionally small and focuses on one path:

1. Godot creates a single WebView
2. the loaded page sends `web_ready` to Godot
3. Godot replies with `godot_ready`
4. both sides log the exchanged payloads

The example supports two startup sources for the same app page:

1. a desktop Kirie dev session served by the CLI-managed Vite dev server
2. the packaged web build loaded from `res://src-web/dist/index.html`

## Layout

- `project.godot`
  the Godot project
- `src-godot`
  the Godot scene, TypeScript-authored controller, generated GDScript, and
  project resources
- `tstogd.json` and `tsconfig.json`
  the isolated TypeScript-to-GDScript configuration
- `kirie.config.ts`
  Kirie CLI configuration for the Godot project and Vite web root
- `src-web`
  a small Vite app for WebView IPC testing

## TypeScript-authored GDScript

The scene controller follows tstogd's
[manual project setup](https://github.com/nnn3d/typescript-to-gdscript/blob/master/docs/configuration.md).
Until the merged upstream changes have a release, this example installs the
[prebuilt demo fork](https://github.com/LemonNekoGH/typescript-to-gdscript/tree/demo/external-package-sources).
The controller uses the `GdKirie` global class from the installed addon.
tstogd generates its typings under `src-godot/_typings` and emits the Godot
class name in the generated script.

## Setup

Install dependencies from the repository root:

```sh
mise x -- pnpm install --frozen-lockfile
```

## Running desktop dev

Desktop runs use the pinned Godot CEF addon. Install it into this example before
running on macOS, Windows, or Linux:

```sh
mise x -- pnpm -C examples/basic-kirie-cli exec kirie doctor --fix plugin:core
```

For projects created with `kirie init`, run `pnpm install` and
`pnpm kirie doctor --fix` after initialization to install both addons.

Start the CLI-managed Vite server and Godot project:

```sh
mise x -- pnpm -F @gd-kirie/basic-kirie-cli run dev
```

The CLI passes the resolved Vite URL to Godot through the `kirie-web-url` launch
option. The package script first regenerates
`src-godot/generated/main.gd` from `src-godot/scripts/main.ts`, then creates a
WebView with that URL when the project starts.

## Running the packaged web build

Build the web app first:

```sh
mise x -- pnpm -F @gd-kirie/basic-kirie-cli run build
```

Open or run the Godot project. The example creates a WebView with
`res://src-web/dist/index.html` when the project starts and performs a minimal
round-trip:

1. the web page sends `web_ready`
2. Godot replies with `godot_ready`

## Running the Godot side manually

Recommended to use [mise](https://mise.jdx.dev/) to manage Godot versions.

After changing the TypeScript controller, regenerate the scene-attached
GDScript:

```bash
mise x -- pnpm -F @gd-kirie/basic-kirie-cli run build:godot
```

`src-godot/generated/` is ignored. Run the generator before opening the Godot
project directly, and edit `src-godot/scripts/main.ts` instead of generated
GDScript.

Run the Godot editor with the current project:

```bash
mise x -- godot ./examples/basic-kirie-cli/project.godot
```

You can also run the game directly from the command line:

```bash
mise x -- godot --path ./examples/basic-kirie-cli
```

## Experimental Web export

Web export requires the CLI and addon from
[gd-kirie PR #124](https://github.com/gd-kirie/gd-kirie/pull/124).
The pinned Kirie 0.13.0 packages do not include this target.
After a release includes that PR, update the package versions and install the
core addon before using these commands.

For a Godot project with a compatible Web export preset, export the Godot app
and Vite content together from the repository root:

```sh
mise x -- pnpm exec kirie export web --project examples/basic-kirie-cli
```

The default entry is `dist/kirie/web/index.html` inside the example directory.
Serve that directory so `src-web/dist/index.html` uses the same origin.
The threaded templates require the HTTP response headers
`Cross-Origin-Opener-Policy: same-origin` and
`Cross-Origin-Embedder-Policy: require-corp`.
For the C# toolchain setup, see the [Eventa C# example](../eventa-csharp/README.md#experimental-web-export).

## Running an exported mobile app

Run these commands from the repository root after installing the addons.
Generate the controller before exporting:

```sh
mise x -- pnpm -C examples/basic-kirie-cli run build:godot
mise x -- pnpm exec kirie export android --project examples/basic-kirie-cli
mise x -- pnpm exec kirie run android --project examples/basic-kirie-cli
```

For an iOS Simulator, use `export ios` and `run ios` with the same project.
For a physical iOS device, pass `--device <device-udid>` to both commands.
Xcode uses the signing identity and provisioning profile from your account.

To use the Vite dev server on mobile:

```sh
mise x -- pnpm exec kirie dev android --project examples/basic-kirie-cli --device <selector>
mise x -- pnpm exec kirie dev ios --project examples/basic-kirie-cli --device <selector>
```

Android dev sessions use `adb reverse` to reach Vite through `127.0.0.1`.
The Android export preset keeps the Internet permission enabled for this URL.
