# Nori Plugin SDK

Developer SDK for the Nori Desktop Plugin Specification, synchronized with the public contracts implemented by `MF-Dust/Nori.Desktop`.

Current baseline:

- Plugin API: **2.0**
- Manifest schema: **1**
- Target framework: **.NET 10 (`net10.0`)**
- Runtime model: trusted in-process .NET plugin
- Host capability currently available: **`ui.webview`**

## Repository layout

```text
src/Nori.PluginSDK/       compile-time contracts
examples/HelloNori/       minimal plugin example
docs/plugin-authoring.md  lifecycle and API guidance
docs/packaging.md         manifest and .noripack layout
```

## Build

```bash
dotnet build Nori.PluginSDK.slnx
```

The SDK project emits an assembly named `Nori.PluginRuntime` because that is the contract assembly name used by Nori Desktop. Its assembly version stays at `0.0.0.0`; Plugin API compatibility is governed by `manifest.json` `apiVersion`, not CLR assembly versioning.

## Reference from a plugin

During development inside this repository:

```xml
<ProjectReference Include="../../src/Nori.PluginSDK/Nori.PluginSDK.csproj" Private="false" />
```

The `Private="false"` part matters: the plugin package must not contain `Nori.PluginRuntime.dll`. Nori Desktop binds the contract reference to its own host assembly and rejects packages that include a duplicate contract DLL.

For package distribution of the SDK:

```bash
dotnet pack src/Nori.PluginSDK/Nori.PluginSDK.csproj -c Release
```

The resulting `Nori.PluginSDK` package carries the contract as a compile-time `ref/net10.0` asset with no runtime asset.

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

See [`examples/HelloNori`](examples/HelloNori) for storage, contributions, and WebView capability usage.

## Public API boundary

The SDK intentionally contains only contracts already present in Nori Desktop:

- `INoriPlugin`, `IPluginContext`, `PluginDescriptor`
- `IPluginLogger`, `IPluginStorage`, `IPluginAssets`
- `IPluginContribution`, `IContributionRegistry`, `IPluginRegistration`
- `IPluginCapability`, `IPluginCapabilities`, `PluginCapabilityStatus`, `PluginCapabilityAttribute`
- `IWebViewCapability`, `PluginWebViewOptions`, `IPluginWebViewWindow`
- `PluginException`

Host implementation details such as package installation, bridge routing, `AssemblyLoadContext`, state management, and window hosting are intentionally absent.
