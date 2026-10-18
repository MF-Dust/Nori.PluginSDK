using System.Text.Json.Nodes;

namespace Nori.PluginRuntime;

/// <summary>Describes the currently loaded plugin. Values originate from the validated manifest.</summary>
public sealed record PluginDescriptor
{
    /// <summary>Unique plugin ID.</summary>
    public required string Id { get; init; }

    /// <summary>Display name.</summary>
    public required string Name { get; init; }

    /// <summary>Plugin description.</summary>
    public required string Description { get; init; }

    /// <summary>Semantic plugin version.</summary>
    public required string Version { get; init; }

    /// <summary>Plugin API version declared by the manifest (major.minor).</summary>
    public required string ApiVersion { get; init; }

    /// <summary>Capabilities declared by the manifest.</summary>
    public IReadOnlyList<string> Capabilities { get; init; } = [];
}

/// <summary>Entry point for a trusted in-process Nori plugin. The AssemblyLoadContext is not a security sandbox.</summary>
public interface INoriPlugin
{
    /// <summary>Activates the plugin and registers its contributions.</summary>
    ValueTask ActivateAsync(IPluginContext context, CancellationToken cancellationToken);

    /// <summary>Deactivates the plugin and releases plugin-owned resources.</summary>
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

/// <summary>
/// Executable action contributed by the plugin to the host.
/// The host registers active plugin actions as AI tools (plugin__&lt;pluginId&gt;__&lt;actionId&gt;)
/// so the assistant can invoke them.
/// </summary>
public interface IPluginActionContribution : IPluginContribution
{
    /// <summary>Action ID (unique within the plugin; used in the host tool name).</summary>
    string Id { get; }

    /// <summary>Model-facing action description.</summary>
    string Description { get; }

    /// <summary>Parameter JSON Schema (null means no parameters).</summary>
    JsonNode? ParametersSchema { get; }

    /// <summary>Executes the action and returns a serializable result.</summary>
    Task<JsonObject?> InvokeAsync(JsonNode? arguments, CancellationToken cancellationToken);

    /// <summary>
    /// Invocation permission level (API 2.3). The host decides from this level whether the user must approve
    /// each call. Plugins that do not implement it default to <see cref="PluginActionPermission.Safe"/>.
    /// </summary>
    PluginActionPermission Permission => PluginActionPermission.Safe;
}

/// <summary>Permission level used when a plugin action is invoked as an AI tool (API 2.3).</summary>
public enum PluginActionPermission
{
    /// <summary>No external side effects; runs directly.</summary>
    Safe,

    /// <summary>Has side effects; requires user confirmation unless the user's permission gear allows it.</summary>
    Confirm,

    /// <summary>High risk; requires user confirmation on every call except in full bypass mode.</summary>
    Dangerous,
}

/// <summary>Lazy native page factory (API 2.1). The host creates pages on the UI thread only.</summary>
public interface IPluginPageContribution : IPluginContribution
{
    /// <summary>Page ID, unique within the plugin.</summary>
    string Id { get; }

    /// <summary>Page title shown by the host.</summary>
    string Title { get; }

    /// <summary>Creates a new page instance with an unattached, non-window control.</summary>
    IPluginPage CreatePage();

    /// <summary>
    /// Where the host surfaces this page (API 2.4). Plugins that do not implement it keep opening a standalone window.
    /// </summary>
    PluginPagePlacement Placement => PluginPagePlacement.Window;
}

/// <summary>Host surface used for a native plugin page (API 2.4).</summary>
public enum PluginPagePlacement
{
    /// <summary>Standalone host window, opened from the plugin settings card.</summary>
    Window,

    /// <summary>Embedded navigation page inside the host's settings window.</summary>
    Settings,
}

/// <summary>Exclusive unattached control and page resources. The host disposes it on the UI thread.</summary>
public interface IPluginPage : IDisposable
{
    Avalonia.Controls.Control Control { get; }
}

/// <summary>Revocable handle for one plugin contribution registration.</summary>
public interface IPluginRegistration : IDisposable
{
}

/// <summary>Contribution registry owned by the current plugin context.</summary>
public interface IContributionRegistry
{
    IPluginRegistration Register<T>(T contribution)
        where T : class, IPluginContribution;
}

/// <summary>Independent declared/granted/available state for a capability.</summary>
public sealed record PluginCapabilityStatus(
    string Id,
    bool Declared,
    bool Granted,
    bool Available);

/// <summary>Marker interface for a host-provided capability.</summary>
public interface IPluginCapability
{
}

/// <summary>Capability lookup.</summary>
public interface IPluginCapabilities
{
    bool TryGet<T>(out T? capability)
        where T : class, IPluginCapability;

    T GetRequired<T>()
        where T : class, IPluginCapability;

    IReadOnlyList<PluginCapabilityStatus> Statuses { get; }
}

/// <summary>Manifest capability ID for a capability interface or implementation.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class PluginCapabilityAttribute(string id) : Attribute
{
    /// <summary>Capability ID.</summary>
    public string Id { get; } = string.IsNullOrWhiteSpace(id)
        ? throw new ArgumentException("Capability ID cannot be empty.", nameof(id))
        : id;
}

/// <summary>UI capability IDs supported by the desktop host.</summary>
public static class PluginCapabilityIds
{
    /// <summary>Native Avalonia pages (API 2.1).</summary>
    public const string AvaloniaUi = "ui.avalonia";

    /// <summary>Chat providers that return a Microsoft.Extensions.AI IChatClient (API 2.4).</summary>
    public const string ChatProvider = "ai.provider";

    /// <summary>Process query, launch and exit-wait interop (API 2.4).</summary>
    public const string Applications = "applications";
}

/// <summary>Plugin boundary error. <see cref="Code"/> is a stable machine-readable error code.</summary>
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

    /// <summary>Host-assembled redacted diagnostics: manifest ID of the failing plugin.</summary>
    public string? DiagnosticPluginId { get; internal set; }

    /// <summary>Host-assembled redacted diagnostics: manifest version of the failing plugin.</summary>
    public string? DiagnosticPluginVersion { get; internal set; }

    /// <summary>Host-assembled redacted diagnostics: host API version (major.minor).</summary>
    public string? DiagnosticHostApiVersion { get; internal set; }

    /// <summary>Host-assembled redacted diagnostics: host product version.</summary>
    public string? DiagnosticHostVersion { get; internal set; }

    /// <summary>Host-assembled redacted diagnostics: root exception type name.</summary>
    public string? DiagnosticExceptionType { get; internal set; }

    /// <summary>Host-assembled redacted diagnostics: truncated TypeLoadException type name.</summary>
    public string? DiagnosticTypeLoadTypeName { get; internal set; }

    /// <summary>Host-assembled redacted diagnostics: assembly file name involved in a FileLoadException.</summary>
    public string? DiagnosticAssemblyName { get; internal set; }
}
