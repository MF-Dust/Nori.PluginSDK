using System.Text.Json.Nodes;

namespace Nori.PluginRuntime;

/// <summary>Describes the currently loaded plugin. Values originate from the validated manifest.</summary>
public sealed record PluginDescriptor
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Version { get; init; }
    public required string ApiVersion { get; init; }
    public IReadOnlyList<string> Capabilities { get; init; } = [];
}

/// <summary>Entry point for a trusted in-process Nori plugin.</summary>
public interface INoriPlugin
{
    ValueTask ActivateAsync(IPluginContext context, CancellationToken cancellationToken);
    ValueTask DeactivateAsync(CancellationToken cancellationToken);
}

/// <summary>Minimal host context exposed to a plugin.</summary>
public interface IPluginContext
{
    PluginDescriptor Plugin { get; }
    IPluginLogger Logger { get; }
    IPluginStorage Storage { get; }
    IPluginAssets Assets { get; }
    IContributionRegistry Contributions { get; }
    IPluginCapabilities Capabilities { get; }
    CancellationToken StoppingToken { get; }
}

/// <summary>Plugin-scoped logger.</summary>
public interface IPluginLogger
{
    void Debug(string message);
    void Info(string message);
    void Warn(string message);
    void Error(string message, Exception? exception = null);
}

/// <summary>Plugin-scoped JSON key/value storage.</summary>
public interface IPluginStorage
{
    ValueTask<JsonNode?> GetAsync(string key, CancellationToken cancellationToken = default);
    ValueTask SetAsync(string key, JsonNode? value, CancellationToken cancellationToken = default);
    ValueTask DeleteAsync(string key, CancellationToken cancellationToken = default);
}

/// <summary>Reads public resources from the installed plugin package.</summary>
public interface IPluginAssets
{
    Stream OpenRead(string relativePath);
    Uri GetUri(string relativePath);
}

/// <summary>Marker interface for a plugin contribution.</summary>
public interface IPluginContribution
{
}

/// <summary>Revocable registration handle for a contribution.</summary>
public interface IPluginRegistration : IDisposable
{
}

/// <summary>Registers contributions owned by the current plugin context.</summary>
public interface IContributionRegistry
{
    IPluginRegistration Register<T>(T contribution)
        where T : class, IPluginContribution;
}

/// <summary>Declared, granted and available state for one plugin capability.</summary>
public sealed record PluginCapabilityStatus(
    string Id,
    bool Declared,
    bool Granted,
    bool Available);

/// <summary>Marker interface for a host capability exposed to plugins.</summary>
public interface IPluginCapability
{
}

/// <summary>Capability lookup for the current plugin.</summary>
public interface IPluginCapabilities
{
    bool TryGet<T>(out T? capability)
        where T : class, IPluginCapability;

    T GetRequired<T>()
        where T : class, IPluginCapability;

    IReadOnlyList<PluginCapabilityStatus> Statuses { get; }
}

/// <summary>Maps a capability contract to its manifest capability ID.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class PluginCapabilityAttribute(string id) : Attribute
{
    public string Id { get; } = string.IsNullOrWhiteSpace(id)
        ? throw new ArgumentException("Capability ID cannot be empty.", nameof(id))
        : id;
}

/// <summary>Capability IDs implemented by the current Plugin API.</summary>
public static class PluginCapabilityIds
{
    public const string WebView = "ui.webview";
}

/// <summary>Stable plugin-boundary exception. <see cref="Code"/> is machine-readable.</summary>
public class PluginException : Exception
{
    public PluginException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public PluginException(string code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }

    public string Code { get; }
}

/// <summary>Host WebView capability for plugin-owned windows.</summary>
[PluginCapability(PluginCapabilityIds.WebView)]
public interface IWebViewCapability : IPluginCapability
{
    Task<IPluginWebViewWindow> CreateWindowAsync(
        PluginWebViewOptions options,
        CancellationToken cancellationToken = default);
}

/// <summary>Options used to create a plugin WebView window.</summary>
public sealed record PluginWebViewOptions
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string EntryPoint { get; init; }
    public double Width { get; init; } = 800;
    public double Height { get; init; } = 600;
    public double? MinWidth { get; init; }
    public double? MinHeight { get; init; }
    public bool CanResize { get; init; } = true;
    public bool Topmost { get; init; }
    public bool ShowInTaskbar { get; init; } = true;
}

/// <summary>Lifecycle handle for a plugin WebView window.</summary>
public interface IPluginWebViewWindow : IAsyncDisposable
{
    string PluginId { get; }
    string Id { get; }
    string Label { get; }
    string? Title { get; }
    bool IsVisible { get; }
    Task ShowAsync(CancellationToken cancellationToken = default);
    Task HideAsync(CancellationToken cancellationToken = default);
    Task CloseAsync(CancellationToken cancellationToken = default);
}
