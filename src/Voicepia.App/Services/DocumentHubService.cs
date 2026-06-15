using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;

namespace Voicepia.App.Services;

public sealed class DocumentHubService : IAsyncDisposable
{
    private readonly HubConnection _connection;

    public DocumentHubService(IOptions<ApiOptions> options)
    {
        var hubUrl = new Uri(new Uri(options.Value.BaseUrl), "/hubs/document");

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();
    }

    public HubConnectionState State => _connection.State;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_connection.State == HubConnectionState.Disconnected)
        {
            await _connection.StartAsync(cancellationToken);
        }
    }

    public async Task JoinDocumentAsync(string documentId, string displayName, CancellationToken cancellationToken = default)
    {
        await ConnectAsync(cancellationToken);
        await _connection.InvokeAsync("JoinDocument", documentId, displayName, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}
