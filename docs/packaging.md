# Packaging

A `.noripack` file is a ZIP archive with `manifest.json` at the root.

Supported layout:

```text
manifest.json
README.md       optional
LICENSE         optional
icon.png        optional
lib/            managed plugin entry point and private dependencies
web/            public package files (not hosted as pages)
assets/         public assets
locales/        public localization resources
runtimes/       private runtime dependencies
```

Any other top-level entry is rejected. Private native helpers or driver files may live under `lib/` or `runtimes/`.

## Important contract rules

- Do not place `Nori.PluginRuntime.dll` anywhere inside the package. Nori Desktop supplies that assembly and rejects packages that carry their own current or legacy contract assembly.
- Do not ship Avalonia assemblies (renamed copies are detected too). Nori Desktop shares its Avalonia 12.1.1 assemblies with plugins.
- References to `Nori.Core`, `Nori.Desktop`, or `Microsoft.AspNetCore*` are rejected.

When developing from this repository, the example uses a project reference with `Private="false"` so the contract DLL is compile-time only. When the SDK is consumed as a NuGet package, the package is intentionally packed with a `ref/net10.0` compile asset and no runtime asset.

## Minimal manifest

```json
{
  "schemaVersion": 1,
  "id": "io.nori.example",
  "name": "Nori Example",
  "description": "Nori plugin",
  "version": "1.0.0",
  "authors": [{ "name": "Nori" }],
  "apiVersion": "2.3",
  "minHostVersion": "1.0.0",
  "runtime": {
    "kind": "dotnet",
    "assembly": "lib/Nori.Example.dll",
    "entryType": "Nori.Example.Plugin"
  },
  "capabilities": [],
  "optionalCapabilities": ["ui.avalonia"],
  "platforms": ["windows", "linux", "macos"],
  "dependencies": []
}
```

Plugin IDs follow `^[a-z0-9]+(\\.[a-z0-9_-]+)+$`. Plugin versions and `minHostVersion` use SemVer 2.0. `apiVersion` uses `major.minor`.

The host API major must equal the plugin API major and the host minor must be greater than or equal to the plugin minor.
