# Nori Plugin SDK

Developer SDK for the Nori Desktop Plugin Specification, synchronized with the public contracts implemented by `MF-Dust/Nori.Desktop`.

Current baseline:

- Plugin API: **2.4**
- Manifest schema: **1**
- Target framework: **.NET 10 (`net10.0`)**
- Runtime model: trusted in-process .NET plugin
- Host capabilities currently available: **`ui.avalonia`** (native Avalonia 12.1.1 pages), **`ai.provider`** (Microsoft.Extensions.AI chat providers), **`applications`** (process query, launch and exit-wait interop)

## API history

| API | SDK | Contracts |
| --- | --- | --- |
| 2.0 | 2.0.0 | Core lifecycle, storage, assets, contributions, capabilities, `IPluginActionContribution` |
| 2.1 | 2.3.0 | `IPluginPageContribution` / `IPluginPage` with the `ui.avalonia` capability |
| 2.3 | 2.3.0 | `IPluginActionContribution.Permission` / `PluginActionPermission` |
| 2.4 | 2.4.0 | `IPluginChatProviderContribution` / `ai.provider`, `IPluginPageContribution.Placement`, `IPluginApplicationCapability` / `applications` |

SDK 2.4.0 adds the API 2.4 contracts; plugins built for 2.3.0 keep loading. API 2.4 adds:
- `IPluginChatProviderContribution`: the plugin returns a `Microsoft.Extensions.AI.IChatClient`, so text, images, streaming and tools reuse the host conversion.
- `IPluginPageContribution.Placement`: `Window` (default) or `Settings`, which embeds the page in the settings navigation.
- `IPluginApplicationCapability`: list running processes, start an absolute executable with separate arguments, and wait for exit. There is no process termination or shell execution.

SDK 2.3.0 removes the WebView contracts (`IWebViewCapability`, `PluginWebViewOptions`, `IPluginWebViewWindow`, `IPluginWebViewCommandHandler`) that shipped in SDK 2.2.0. Nori Desktop no longer implements `ui.webview`; a plugin requiring it is rejected before its assembly is loaded. API 2.2 is not used by the host.

## Repository layout

```text
src/Nori.PluginSDK/       compile-time contracts
examples/HelloNori/       minimal plugin example (action + native page)
examples/LocalChat/       API 2.4 ai.provider example with a local IChatClient
docs/plugin-authoring.md  lifecycle and API guidance
docs/packaging.md         manifest and .noripack layout
```

## Build

```bash
dotnet build Nori.PluginSDK.slnx
```

The SDK project emits an assembly named `Nori.PluginRuntime` because that is the contract assembly name used by Nori Desktop. Its assembly version stays at `0.0.0.0`; Plugin API compatibility is governed by `manifest.json` `apiVersion`, not CLR assembly versioning.

## Install from NuGet

```bash
dotnet add package Nori.PluginSDK --version 2.4.0
```

or from a project file:

```xml
<PackageReference Include="Nori.PluginSDK" Version="2.4.0" />
```

The package contains the contract assembly only as a compile-time `ref/net10.0` asset. It depends on Avalonia 12.1.1 and Microsoft.Extensions.AI.Abstractions 10.9.0 for compile-time assets only. Plugin packages must not ship `Nori.PluginRuntime.dll`, any Avalonia assembly or `Microsoft.Extensions.AI.Abstractions.dll`; Nori Desktop provides all of them.

Unit tests that execute plugin code outside the host need the contract assembly at run time. Copy it from the package's `ref/net10.0` folder into the test output, and reference Avalonia directly in the test project if page code runs.

Repository releases use tags matching the package version, for example `v2.4.0`. The publish workflow verifies that the tag matches the `<Version>` value in `Nori.PluginSDK.csproj` before pushing to NuGet.org.

## Reference from a plugin

During development inside this repository:

```xml
<ProjectReference Include="../../src/Nori.PluginSDK/Nori.PluginSDK.csproj" Private="false" />
```

The `Private="false"` part matters: the plugin package must not contain `Nori.PluginRuntime.dll`. Nori Desktop binds the contract reference to its own host assembly and rejects packages that include a duplicate contract DLL.

To pack the SDK:

```bash
dotnet pack src/Nori.PluginSDK/Nori.PluginSDK.csproj -c Release
```

## Minimal plugin

```csharp
using Nori.PluginRuntime;

public sealed class Plugin : INoriPlugin
{
    public ValueTask ActivateAsync(IPluginContext context, CancellationToken cancellationToken)
    {
        context.Logger.Info("Hello Nori");
        return ValueTask.CompletedTask;
    }

    public ValueTask DeactivateAsync(CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}
```

See [`examples/HelloNori`](examples/HelloNori) for storage, an assistant action, and a native page, and [`examples/LocalChat`](examples/LocalChat) for an API 2.4 chat provider.

## Public API boundary

The SDK intentionally contains only contracts already present in Nori Desktop:

- `INoriPlugin`, `IPluginContext`, `PluginDescriptor`
- `IPluginLogger`, `IPluginStorage`, `IPluginAssets`
- `IPluginContribution`, `IContributionRegistry`, `IPluginRegistration`
- `IPluginActionContribution`, `PluginActionPermission`
- `IPluginPageContribution`, `IPluginPage`, `PluginPagePlacement`
- `IPluginChatProviderContribution`, `PluginChatClientOptions`
- `IPluginApplicationCapability`, `PluginApplicationInfo`, `PluginApplicationLaunchRequest`
- `IPluginCapability`, `IPluginCapabilities`, `PluginCapabilityStatus`, `PluginCapabilityAttribute`, `PluginCapabilityIds`
- `PluginException`

Host implementation details such as package installation, bridge routing, `AssemblyLoadContext`, state management, and window hosting are intentionally absent.

## Plugins built on this SDK

- [`MF-Dust/Nori.Plugins.Browser`](https://github.com/MF-Dust/Nori.Plugins.Browser): browser automation in an isolated Microsoft Edge session.
