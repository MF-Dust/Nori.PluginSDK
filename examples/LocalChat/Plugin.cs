using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Nori.PluginRuntime;

namespace LocalChat;

/// <summary>
/// Minimal API 2.4 chat provider. A real provider would call its own HTTP endpoint; this one answers
/// locally so the example runs without credentials or network access.
/// </summary>
public sealed class Plugin : INoriPlugin
{
    private IPluginRegistration? _registration;

    public ValueTask ActivateAsync(IPluginContext context, CancellationToken cancellationToken)
    {
        _registration = context.Contributions.Register<IPluginChatProviderContribution>(new LocalProvider());
        return ValueTask.CompletedTask;
    }

    public ValueTask DeactivateAsync(CancellationToken cancellationToken)
    {
        _registration?.Dispose();
        _registration = null;
        return ValueTask.CompletedTask;
    }
}

public sealed class LocalProvider : IPluginChatProviderContribution
{
    public string Id => "local";

    public string Name => "Local echo";

    // Empty endpoint and no key requirement: the host accepts this provider without connection fields.
    public string DefaultBaseUrl => "";

    public bool RequiresApiKey => false;

    public IChatClient CreateClient(PluginChatClientOptions options) => new EchoClient();

    public ValueTask<IReadOnlyList<string>> GetModelsAsync(PluginChatClientOptions options, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyList<string>>(["echo-1"]);
}

/// <summary>Echo client owned and disposed by the host after each request or on revoke.</summary>
public sealed class EchoClient : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string prompt = messages.LastOrDefault()?.Text ?? "";
        return Task.FromResult(new ChatResponse(new Microsoft.Extensions.AI.ChatMessage(ChatRole.Assistant, prompt)));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
        ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string prompt = messages.LastOrDefault()?.Text ?? "";
        foreach (string word in prompt.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new ChatResponseUpdate(ChatRole.Assistant, word + " ");
            await Task.Yield();
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}
