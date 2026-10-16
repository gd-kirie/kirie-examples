# Native C# Tray example

This Godot project uses `GdKirie.Tray` without a WebView, IPC, or Eventa.
Native menu clicks update the check item text from its current checked state.

Install dependencies and the native addon from the repository root:

```sh
mise x -- pnpm install --frozen-lockfile
mkdir -p examples/tray-native/addons
cp -R examples/tray-native/node_modules/@gd-kirie/tray/addon/kirie_tray/. examples/tray-native/addons/kirie_tray
mise x -- dotnet build examples/tray-native/KirieTrayNative.csproj
mise x -- godot --path examples/tray-native --editor --import
mise x -- godot --path examples/tray-native
```

The npm package supplies the GDExtension. NuGet supplies the C# SDK.
Godot selects the library for the current desktop platform.

Run the native object and ownership probe after setup:

```sh
mise run test:tray-sdk
```

See the [C# SDK](https://github.com/gd-kirie/kirie-plugins/tree/main/packages/GdKirie.Tray)
and the [browser example](../tray/README.md).
