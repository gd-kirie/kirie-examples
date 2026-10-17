# Kirie examples

This repository contains runnable examples for [Kirie](https://github.com/gd-kirie/gd-kirie).
The examples use Kirie 0.14.1 core npm and NuGet packages.
The Tray and Notifications plugins remain at 0.13.0.
The embedded-host prototype also needs core source and a custom Godot build.

## Setup

Install tools and dependencies from this directory:

```sh
mise trust
mise install
mise x -- pnpm install --frozen-lockfile
mise x -- apm install --frozen
```

APM installs the pinned [simple-english skill](https://github.com/AminBlg/SimpleEnglish/tree/a6fcb4fde098b33617cc1578d151ebf58774b883/skills/simple-english).
The generated skill directory is ignored by Git.
Run the setup command in an example README before you build or open that project.

## Examples

- [Basic IPC](examples/basic-ipc/README.md): Send messages between Godot and a web page.
- [Basic CLI](examples/basic-kirie-cli/README.md): Build and run an application with the Kirie CLI.
- [Basic Platform](examples/basic-platform/README.md): Use window controls and global shortcuts.
- [Eventa C#](examples/eventa-csharp/README.md): Use events and requests from C# and JavaScript.
- [Tray](examples/tray/README.md): Create a system tray icon and menu.
- [Notifications](examples/notifications/README.md): Show notifications and handle activation.
- [Embedded host](examples/basic-host-app-embedded/README.md): Embed Godot and Kirie in a native application.

## Development

Run these tasks from the repository root:

```sh
mise run lint
mise run format
```

ESLint covers JavaScript, TypeScript, Vue, JSON, CSS, and HTML.
The [.NET formatter](https://learn.microsoft.com/dotnet/core/tools/dotnet-format)
checks C# whitespace without a native application build.
GitHub Actions runs only lint and format.
Run `mise run release` to update workspace package versions with bumpp.
