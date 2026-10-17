# Native C# Tray example

This Godot project uses `GdKirie.Tray` without a WebView, IPC, or Eventa.
Native menu clicks update the check item text from its current checked state.

Published Tray 0.13.0 predates this API. This example requires a matching Tray release.

Install dependencies and the native addon from the repository root:

```sh
mise x -- pnpm install --frozen-lockfile
mise x -- mkdir -p examples/tray-native/addons/kirie_tray
mise x -- cp -R examples/tray-native/node_modules/@gd-kirie/tray/addon/kirie_tray/. examples/tray-native/addons/kirie_tray
mise x -- dotnet build examples/tray-native/KirieTrayNative.csproj
mise x -- godot --path examples/tray-native --editor --import --quit
mise x -- godot --path examples/tray-native
```

The npm package supplies the native addon. NuGet supplies the C# API.
One subscription receives global menu events by native ID:

```cs
using var events = new TrayEvents();
events.Menu += id =>
{
    if (id == check.Id)
        check.Text = check.Checked ? "Enabled" : "Disabled";
};
```

Run the native object and ownership probe after setup:

```sh
mise run test:tray-sdk
```

The probe uses synthetic global signals. It does not test real system clicks.
Linux does not support `ShowMenu()`. Other platform limits follow the native libraries.
See the [browser example](../tray/README.md).
