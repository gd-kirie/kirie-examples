# eventa-csharp

Beginner-friendly Kirie Eventa demo project for Godot C# projects.

This example requires the .NET 10 SDK because `GdKirie.EventaAdapter` directly
depends on the upstream `Eventa` package, which targets `net10.0`. Projects that
target `net8.0` or `net9.0` are expected to fail package restore or build when
they reference the adapter.

The example is intentionally separate from exported Android and iOS integration
tests. It demonstrates the first Eventa adapter path:

1. Godot creates a single WebView.
2. The WebView page emits `web:ready` through `@gd-kirie/ipc-eventa`.
3. The C# scene receives that Eventa event through `GdKirie.EventaAdapter`.
4. The WebView invokes `godot:echo`, handled by C#.
5. The C# scene can invoke `web:echo`, handled by the WebView.

## Running

Install dependencies and build the example from the repository root:

```sh
mise x -- pnpm install --frozen-lockfile
mise x -- pnpm -F @gd-kirie/eventa-csharp-web run build
```

Install the Kirie and Godot CEF addons, then build C#:

```sh
mise x -- pnpm exec kirie doctor --fix plugin:core --project examples/eventa-csharp
mise x -- dotnet build examples/eventa-csharp/KirieEventaCSharp.csproj
```

Open the Godot project:

```sh
mise x -- godot ./examples/eventa-csharp/project.godot
```

Run the scene and press `Create WebView`.

Build, install, and launch the exported Android example:

```sh
mise x -- pnpm exec kirie export android --project examples/eventa-csharp
mise x -- pnpm exec kirie run android --project examples/eventa-csharp
```

Build, install, and launch the exported iOS simulator example:

```sh
mise x -- pnpm exec kirie export ios --project examples/eventa-csharp
mise x -- pnpm exec kirie run ios --project examples/eventa-csharp
```

For a physical iOS device, pass `--device <device-udid>` to both commands.
Xcode uses the signing identity and provisioning profile from your account.
