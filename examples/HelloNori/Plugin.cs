using System.Text.Json.Nodes;
using Nori.PluginRuntime;

namespace HelloNori;

public sealed class Plugin : INoriPlugin
{
    private IPluginRegistration? _registration;
    private IPluginWebViewWindow? _window;

    public async ValueTask ActivateAsync(IPluginContext context, CancellationToken cancellationToken)
    {
        context.Logger.Info($"Hello from {context.Plugin.Name} {context.Plugin.Version}");

        JsonNode? value = await context.Storage.GetAsync("launchCount", cancellationToken);
        int launchCount = value?.GetValue<int>() ?? 0;
        await context.Storage.SetAsync("launchCount", JsonValue.Create(launchCount + 1), cancellationToken);

        _registration = context.Contributions.Register(new HelloContribution("hello"));

        if (context.Capabilities.TryGet<IWebViewCapability>(out var webView) && webView is not null)
        {
            _window = await webView.CreateWindowAsync(
                new PluginWebViewOptions
                {
                    Id = "hello",
                    Title = "Hello Nori",
                    EntryPoint = "index.html",
                    Width = 520,
                    Height = 360,
                },
                cancellationToken);

            await _window.ShowAsync(cancellationToken);
        }
    }

    public async ValueTask DeactivateAsync(CancellationToken cancellationToken)
    {
        _registration?.Dispose();
        _registration = null;

        if (_window is not null)
        {
            await _window.CloseAsync(cancellationToken);
            await _window.DisposeAsync();
            _window = null;
        }
    }
}

public sealed record HelloContribution(string Id) : IPluginContribution;
