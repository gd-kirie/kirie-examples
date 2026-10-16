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

## Experimental Web export

The Web preset uses the experimental target in
[Kirie 0.14.1](https://github.com/gd-kirie/gd-kirie/releases/tag/v0.14.1).
Install the dependencies and core addon with the commands in Running before using this target.

Godot 4.7.2 does not officially support C# Web export.
This experiment uses templates from
[godot-dotnet-web-templates](https://github.com/gd-kirie/godot-dotnet-web-templates).
It has no production-support promise.

From the repository root, install the experimental templates, packages, and
.NET `wasm-tools` workload:

```sh
mise x -- pnpm exec kirie doctor --fix web --project examples/eventa-csharp
```

The fixer uses the current Godot editor. It does not install another editor.
Export the Godot app and web content together:

```sh
mise x -- pnpm exec kirie export web --project examples/eventa-csharp
```

Serve `examples/eventa-csharp/dist/kirie/web/` with the HTTP response headers
`Cross-Origin-Opener-Policy: same-origin` and
`Cross-Origin-Embedder-Policy: require-corp`.
These headers allow the WebAssembly threads in the experimental templates.
For a local development server that sets these headers, run:

```sh
mise x -- pnpm exec kirie dev web --project examples/eventa-csharp
```

Open the printed URL and press `Create WebView`.
The scene owns one `KirieNode`, and the C# wrapper borrows that node.
The iframe (an embedded browser page) runs the existing Eventa request flow.
