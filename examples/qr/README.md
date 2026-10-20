# QR

This example uses `@gd-kirie/qr` and `GdKirie.Qr` on Android and iOS.
It generates QR images, decodes PNG images, and scans one code with the camera.
The camera closes after the first result. Canceling the camera scan returns no result.
Desktop and browser previews cannot generate or scan QR images.

## Setup

See the plugin's [installation requirements](https://github.com/gd-kirie/kirie-plugins/blob/main/packages/qr/README.md#install).
Set this example's npm and NuGet references to the required releases before installation.

Install the dependencies from the repository root:

```sh
mise x -- pnpm install
mise x -- pnpm -C examples/qr exec kirie doctor --fix
mise x -- pnpm -C examples/qr exec kirie build dotnet
```

The project enables the Kirie and QR editor plugins.
The QR plugin installs the native QR and NativeCamera dependencies and activates them for iOS exports.
See the [platform requirements and native limits](https://github.com/gd-kirie/kirie-plugins/blob/main/packages/GdKirie.Qr/README.md#native-dependencies-and-limitations).

## Run on a device

Export the application:

```sh
mise x -- pnpm -C examples/qr exec kirie export android
mise x -- pnpm -C examples/qr exec kirie export ios --device <UDID>
```

For iOS, configure your development team in Godot's export preset before export.
A free Apple Account can use Xcode's Personal Team for device testing. Provisioning expires after seven days.
See [Apple's Personal Team instructions](https://developer.apple.com/help/account/basics/about-your-developer-account#enable-a-personal-team-in-xcode).
When the application requests permission, allow camera access.

Select **Generate PNG**. Then select **Scan this image** to decode it.
You can also select a PNG file or select **Scan with camera**.
Errors appear in the status field. Camera cancellation and images without a QR code have separate status messages.

## Integration

`scripts/Main.cs` creates one Eventa context and attaches the QR host to the existing `KirieNode`.
The web page uses the same application context through `createQrClient`.
The host restores the web page after a camera scan ends.
