# Plugin authoring

This repository mirrors the public plugin contracts currently implemented by Nori Desktop Plugin API 2.4.

## Runtime model

Nori plugins are trusted .NET in-process extensions. The host uses a collectible `AssemblyLoadContext` for dependency isolation and unload attempts; it is not a security sandbox.

The plugin entry type must:

- target .NET 10;
- have a public parameterless constructor;
- implement `Nori.PluginRuntime.INoriPlugin`;
- avoid references to Nori host internals such as `Nori.Core` and `Nori.Desktop`;
- avoid bundling the contract assembly `Nori.PluginRuntime.dll` or Avalonia assemblies in the plugin package.

`ActivateAsync` receives the complete public host surface. Keep every service obtained from `IPluginContext` scoped to the plugin lifetime. `StoppingToken` is cancelled when the host revokes the context.

## Compatibility

`manifest.json` `apiVersion` uses `major.minor`. The host accepts a plugin when the majors match and the host minor is greater than or equal to the plugin minor. Declare the lowest API version whose contracts you use:

- `2.0`: actions and the core context;
- `2.1`: native pages (`ui.avalonia`);
- `2.3`: action permission levels;
- `2.4`: chat providers (`ai.provider`), embedded settings pages, application interop (`applications`).

## Capabilities

| ID | Contract | Purpose |
| --- | --- | --- |
| `ui.avalonia` | `IPluginPageContribution` / `IPluginPage` | Native Avalonia pages, standalone or embedded in settings |
| `ai.provider` | `IPluginChatProviderContribution` | A chat provider that returns a Microsoft.Extensions.AI `IChatClient` |
| `applications` | `IPluginApplicationCapability` | Process query, launch and exit wait; no termination or shell |

Capabilities must be declared in `manifest.json`, either as `capabilities` (required: the plugin is rejected if the host lacks it) or `optionalCapabilities` (check `context.Capabilities.Statuses` before registering). Registering a page without declaring `ui.avalonia` fails.

## Actions

`IPluginActionContribution` registers an assistant tool named `plugin__<pluginId with . replaced by _>__<actionId>`. Return small JSON results; do not return secrets or full local paths.

Set `Permission` to match the side effects:

| Permission | Host behaviour |
| --- | --- |
| `Safe` | Runs directly. Use for read-only or cleanup actions. |
| `Confirm` | The user approves each call unless their permission gear allows confirm-level tools. |
| `Dangerous` | The user approves each call except in full bypass mode. |

Hosts older than API 2.3 ignore `Permission` and treat every action as safe, so declare `"apiVersion": "2.3"` when you rely on approval.

## Native pages

Register an `IPluginPageContribution` factory in `ActivateAsync`; do not create controls there. The host calls `CreatePage` on the UI thread each time the user opens the page and disposes the page on the UI thread when it closes or the plugin is revoked. Return a new, unattached, non-window `Control` every time. Background work that updates controls must post to `Avalonia.Threading.Dispatcher.UIThread`.

`Placement` (API 2.4) selects the surface:

- `PluginPagePlacement.Window` (default): a standalone host window with host chrome, opened from the plugin card.
- `PluginPagePlacement.Settings`: a navigation entry under the host's plugin-pages group inside the settings window. The page owns its scrolling and layout; the host disposes it synchronously on revocation or shutdown even while settings is hidden.

Declare `"apiVersion": "2.4"` to use `Settings` placement.

## Chat providers

Register an `IPluginChatProviderContribution` when the manifest declares `ai.provider`. The host assigns the stable ID `plugin:<pluginId>:<id>` and only passes the selected connection settings through `PluginChatClientOptions`.

```csharp
public IChatClient CreateClient(PluginChatClientOptions options) => new MyClient(options);
public ValueTask<IReadOnlyList<string>> GetModelsAsync(PluginChatClientOptions options, CancellationToken cancellationToken) => ...;
```

- Return a fresh client per call. The host owns and disposes it after the request or when the plugin is revoked; never return a shared singleton.
- `RequiresApiKey => false` and an empty `DefaultBaseUrl` support local models without connection fields. A non-empty default URL must be absolute `http`/`https` without user info.
- Honour the cancellation token: revocation cancels in-flight requests. `GetModelsAsync` receives an empty `Model`.
- The host never falls back to a built-in protocol when the plugin ID is missing, so the user gets a clear error instead of an unexpected provider.
- Do not bundle `Microsoft.Extensions.AI.Abstractions.dll`; the host shares its own copy, and packages carrying it are rejected.

## Application interop

`context.Capabilities.GetRequired<IPluginApplicationCapability>()` returns a capability that:

- lists running processes as ID and name only;
- starts an existing absolute executable with `UseShellExecute = false` and separate arguments;
- waits for an existing process, completing immediately when it has already exited.

The host never terminates, tracks or reparents a launched process, and revocation only cancels the wait. Explain in your README which external program the plugin starts and why.

## Storage

`IPluginStorage` is a plugin-scoped JSON key/value store. Keys are restricted by the host and values are cloned across the boundary. Use it for small settings and state, not large files or databases.

## Assets

`IPluginAssets` exposes only public package resources. Current public roots are `web/`, `assets/`, `locales/`, plus `icon.png`. Paths are relative and are validated by the host.

## Contributions

`IContributionRegistry` owns registered contribution objects. Keep the returned `IPluginRegistration` and dispose it during deactivation. The host also revokes all contributions when the plugin context is torn down.
