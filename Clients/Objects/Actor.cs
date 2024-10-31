namespace bsky.bot.Clients.Objects;

public readonly record struct Actor(
    string did,
    string handle,
    string description
    );