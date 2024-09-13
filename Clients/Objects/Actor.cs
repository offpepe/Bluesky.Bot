namespace bsky.bot.Clients.Models;

public readonly record struct Actor(
    string did,
    string handle,
    string description
    );