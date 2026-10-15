# AGENTS

Use README.md for setup and the example READMEs for build and run instructions.

- Keep each example focused on the behavior that it demonstrates.
- Use published Kirie npm and NuGet packages. Do not add implicit sibling-repository dependencies.
- The embedded-host prototype needs a core source checkout. Use its explicit KIRIE_SOURCE_ROOT input.
- Keep WebView and IPC implementation in gd-kirie. Keep plugin implementation in kirie-plugins.
- Borrow the application Eventa context. Do not create a second WebView owner.
- Run tools through mise. Install project tools with mise install.
- Run mise run lint after source changes. Use mise run format for formatting.
- Edit TypeScript sources for generated GDScript. Do not hand-edit generated files.
- Use snake_case and explicit object keys in src-godot TypeScript.
- Do not commit installed addons, generated scripts, native binaries, or build output.
- Use English for documentation. Apply the simple-english skill managed by apm.yml.
- Keep GitHub workflows limited to lint and format.
- Use Conventional Commits subjects for commits and pull requests.
- Change this file only when the user explicitly requests it.
