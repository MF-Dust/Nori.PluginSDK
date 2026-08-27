# Plugin authoring

This repository mirrors the public plugin contracts currently implemented by Nori Desktop Plugin API 2.0.

## Runtime model

Nori plugins are trusted .NET in-process extensions. The host uses a collectible `AssemblyLoadContext` for dependency isolation and unload attempts; it is not a security sandbox.

The plugin entry type must:

- target .NET 10;
- have a public parameterless constructor;
- implement `Nori.PluginRuntime.INoriPlugin`;
- avoid references to Nori host internals such as `Nori.Core` and `Nori.Desktop`;
- avoid bundling the contract assembly `Nori.PluginRuntime.dll` in the plugin package.

`ActivateAsync` receives the complete public host surface. Keep every service obtained from `IPluginContext` scoped to the plugin lifetime. `StoppingToken` is cancelled when the host revokes the context.

## Capabilities

Plugin API 2.0 currently implements one host capability:

| ID | Contract | Purpose |
| --- | --- | --- |
| `ui.webview` | `IWebViewCapability` | Create plugin-owned WebView windows |

Capabilities must be declared in `manifest.json`. `TryGet<T>` is suitable for graceful degradation. `GetRequired<T>` throws `PluginException` when the capability was not declared, was not granted, or is unavailable.

## Storage

`IPluginStorage` is a plugin-scoped JSON key/value store. Keys are restricted by the host and values are cloned across the boundary. Use it for small settings and state, not large files or databases.

## Assets

`IPluginAssets` exposes only public package resources. Current public roots are `web/`, `assets/`, `locales/`, plus `icon.png`. Paths are relative and are validated by the host.

## Contributions

`IContributionRegistry` owns registered contribution objects. Keep the returned `IPluginRegistration` and dispose it during deactivation. The host also revokes all contributions when the plugin context is torn down.

The current public API only defines the contribution marker. Domain-specific contribution contracts have not been published yet.
