using System.Text.Json.Nodes;
using Avalonia.Controls;
using Nori.PluginRuntime;

namespace HelloNori;

public sealed class Plugin : INoriPlugin
{
    private readonly List<IPluginRegistration> _registrations = [];

    public async ValueTask ActivateAsync(IPluginContext context, CancellationToken cancellationToken)
    {
        context.Logger.Info($"Hello from {context.Plugin.Name} {context.Plugin.Version}");

        JsonNode? value = await context.Storage.GetAsync("launchCount", cancellationToken);
        int launchCount = value?.GetValue<int>() ?? 0;
        await context.Storage.SetAsync("launchCount", JsonValue.Create(launchCount + 1), cancellationToken);

        // Actions become assistant tools named plugin__io_nori_hello__<id>.
        _registrations.Add(context.Contributions.Register<IPluginActionContribution>(new GreetAction()));

        // ui.avalonia is optional in manifest.json: register the page only when the host provides it.
        if (context.Capabilities.Statuses.Any(status => status.Id == PluginCapabilityIds.AvaloniaUi && status.Available))
            _registrations.Add(context.Contributions.Register<IPluginPageContribution>(new HelloPageContribution(launchCount + 1)));
    }

    public ValueTask DeactivateAsync(CancellationToken cancellationToken)
    {
        foreach (IPluginRegistration registration in _registrations) registration.Dispose();
        _registrations.Clear();
        return ValueTask.CompletedTask;
    }
}

/// <summary>Read-only action: runs without per-call approval.</summary>
public sealed class GreetAction : IPluginActionContribution
{
    public string Id => "greet";

    public string Description => "Returns a greeting for the given name.";

    public JsonNode? ParametersSchema => new JsonObject
    {
        ["type"] = "object",
        ["properties"] = new JsonObject { ["name"] = new JsonObject { ["type"] = "string" } },
        ["required"] = new JsonArray("name"),
    };

    public PluginActionPermission Permission => PluginActionPermission.Safe;

    public Task<JsonObject?> InvokeAsync(JsonNode? arguments, CancellationToken cancellationToken)
    {
        string name = arguments?["name"]?.GetValue<string>() ?? "Nori";
        return Task.FromResult<JsonObject?>(new JsonObject { ["greeting"] = $"Hello, {name}!" });
    }
}

public sealed class HelloPageContribution(int launchCount) : IPluginPageContribution
{
    public string Id => "overview";

    public string Title => "Hello Nori";

    public IPluginPage CreatePage() => new HelloPage(launchCount);
}

public sealed class HelloPage(int launchCount) : IPluginPage
{
    private readonly StackPanel _root = new()
    {
        Margin = new Avalonia.Thickness(20),
        Spacing = 8,
        Children =
        {
            new TextBlock { Text = "Hello from a native plugin page." },
            new TextBlock { Text = $"Activated {launchCount} time(s)." },
        },
    };

    public Control Control => _root;

    public void Dispose() => _root.Children.Clear();
}
