using Microsoft.AspNetCore.SignalR;
using Voicepia.Shared.Documents;

namespace Voicepia.Api.Hubs;

public sealed class DocumentHub : Hub
{
    public async Task JoinDocument(string documentId, string displayName)
    {
        var userId = Context.UserIdentifier ?? Context.ConnectionId;
        var groupName = GetGroupName(documentId);

        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        var presence = new DocumentPresence(userId, displayName, DateTimeOffset.UtcNow);
        await Clients.OthersInGroup(groupName).SendAsync("UserJoined", presence);
    }

    public async Task LeaveDocument(string documentId)
    {
        var userId = Context.UserIdentifier ?? Context.ConnectionId;
        var groupName = GetGroupName(documentId);

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        await Clients.OthersInGroup(groupName).SendAsync("UserLeft", userId);
    }

    public async Task BroadcastEdit(string documentId, byte[] update)
    {
        await Clients.OthersInGroup(GetGroupName(documentId))
            .SendAsync("ReceiveEdit", update);
    }

    private static string GetGroupName(string documentId) => $"document:{documentId}";
}
