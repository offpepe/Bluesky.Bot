using bsky.bot.Clients.Models;

namespace bsky.bot.Clients.Responses;

public readonly record struct GetSuggestionsRequest(
    Actor[] actors 
    );