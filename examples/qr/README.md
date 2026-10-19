# QR

This example uses `@gd-kirie/qr` and `GdKirie.Qr` on Android and iOS.
It generates QR images, decodes PNG images, and scans one code with the camera.
The camera closes after the first result. Canceling the camera scan returns no result.
Desktop and browser previews cannot generate or scan QR images.

## Setup

This example awaits a coordinated core and QR release.
The current package references are placeholders for that release. They do not provide a working camera installation.
Published core version `0.14.2` lacks the visibility forwarding that the camera preview requires.
The QR packages are also unpublished.

After the coordinated release, update the package references to the released versions before you install this example.
The example uses published packages only. It does not use sibling checkouts.

Install the dependencies from the repository root:

```sh
mise x -- pnpm install
mise x -- pnpm -C examples/qr exec kirie doctor --fix
mise x -- pnpm -C examples/qr exec kirie build dotnet
```

The project enables the Kirie and QR editor plugins.
The QR plugin installs the native QR and NativeCamera dependencies.
Android includes the recognition model in the application. Scanning does not require Google Play Services or a model download.
The upstream iOS binaries require iOS 16 or later and support devices only.

## Run on a device

Export the application:

```sh
mise x -- pnpm -C examples/qr exec kirie export android
mise x -- pnpm -C examples/qr exec kirie export ios
```

For iOS, select your development team in the exported Xcode project before you install the application.
A free Apple Account can use Xcode's Personal Team for device testing; provisioning expires after seven days.
See [Apple's Personal Team instructions](https://developer.apple.com/help/account/basics/about-your-developer-account#enable-a-personal-team-in-xcode).
Allow camera access when the application requests permission.

Generate an image with **Generate PNG**. Then select **Scan this image** to decode it.
You can also select a PNG file or select **Scan with camera**.
Errors appear in the status field. Camera cancellation and images without a QR code have separate status messages.

## Integration

`scripts/Main.cs` creates one Eventa context and attaches the QR host to the existing `KirieNode`.
The web page uses the same application context through `createQrClient`.
The host restores the web page after a camera scan ends.

The plugin reuses [godot-qr](https://github.com/godot-mobile-plugins/godot-qr/tree/v1.2)
and [NativeCamera](https://github.com/godot-mobile-plugins/godot-native-camera/tree/v3.0).
