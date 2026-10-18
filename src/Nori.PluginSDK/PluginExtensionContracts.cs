using Microsoft.Extensions.AI;

namespace Nori.PluginRuntime;

/// <summary>
/// Connection settings handed to a chat provider only when the user selects it. The host's reasoning level and
/// model capability catalogue are not applied to plugin providers; handle provider-specific options in the client.
/// </summary>
public sealed record PluginChatClientOptions(string BaseUrl, string ApiKey, string Model);

/// <summary>
/// Chat provider contributed by a plugin that declares the ai.provider capability (API 2.4).
/// Clients follow Microsoft.Extensions.AI, so text, images, streaming and tool calls keep working
/// through the host's existing conversion. The host owns and disposes every client returned by
/// <see cref="CreateClient"/>; do not return a shared process-wide singleton.
/// </summary>
public interface IPluginChatProviderContribution : IPluginContribution
{
    /// <summary>Stable ID, unique within the plugin: 1–64 ASCII letters, digits, '_' or '-'.</summary>
    string Id { get; }

    /// <summary>Display name shown in the host model settings: non-empty, at most 128 characters, no control characters.</summary>
    string Name { get; }

    /// <summary>
    /// Default endpoint; may be empty for local clients but never null. A non-empty value must be an absolute
    /// http/https URL without user info.
    /// </summary>
    string DefaultBaseUrl => "";

    /// <summary>Whether the user must supply an API key; local clients may return false.</summary>
    bool RequiresApiKey => true;

    /// <summary>Creates an independent client owned by the host. Do not run blocking network work here.</summary>
    IChatClient CreateClient(PluginChatClientOptions options);

    /// <summary>Lists model IDs; the host never stores the returned text in chat history.</summary>
    ValueTask<IReadOnlyList<string>> GetModelsAsync(PluginChatClientOptions options, CancellationToken cancellationToken);
}

/// <summary>Query, launch and wait for external applications (API 2.4); no termination or shell execution.</summary>
[PluginCapability(PluginCapabilityIds.Applications)]
public interface IPluginApplicationCapability : IPluginCapability
{
    /// <summary>Snapshot of running process IDs and names; no command lines, paths or environment variables.</summary>
    Task<IReadOnlyList<PluginApplicationInfo>> GetRunningAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts an executable directly and returns its process ID. Arguments are passed as separate items
    /// without a system shell. On Windows, .bat and .cmd files are rejected because they are implicitly run
    /// through cmd.exe. The host does not take ownership of the process.
    /// </summary>
    Task<int> LaunchAsync(PluginApplicationLaunchRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Waits for the process to exit. A positive ID that no longer exists completes immediately.
    /// Cancellation or plugin revocation only stops the wait; it never terminates the application.
    /// </summary>
    Task WaitForExitAsync(int processId, CancellationToken cancellationToken = default);
}

/// <summary>Information about a running application process.</summary>
/// <param name="ProcessId">Process ID.</param>
/// <param name="Name">Process name without executable path or command line.</param>
public sealed record PluginApplicationInfo(int ProcessId, string Name);

/// <summary>Application launch request; the path must be an existing absolute path.</summary>
public sealed record PluginApplicationLaunchRequest
{
    /// <summary>
    /// Existing executable absolute path. No PATH lookup or file association is performed; Windows batch files
    /// (.bat, .cmd) are rejected.
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>Separate command-line arguments; empty strings allowed, null or control characters rejected.</summary>
    public IReadOnlyList<string> Arguments { get; init; } = [];

    /// <summary>Optional existing absolute working directory; otherwise the host working directory is used.</summary>
    public string? WorkingDirectory { get; init; }
}
