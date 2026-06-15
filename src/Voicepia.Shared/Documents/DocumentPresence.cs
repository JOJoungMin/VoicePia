namespace Voicepia.Shared.Documents;

public sealed record DocumentPresence(string UserId, string DisplayName, DateTimeOffset JoinedAt);
