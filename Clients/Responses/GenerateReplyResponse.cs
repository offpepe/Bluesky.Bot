namespace bsky.bot.Clients.Responses;

public readonly record struct GenerateReplyResponse(string response, int[] context, bool done);