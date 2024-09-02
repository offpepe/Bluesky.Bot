using bsky.bot.Clients.Requests;

namespace bsky.bot.Clients.Models;

public readonly record struct Record(
    Reply? reply,
    string createdAt,
    string text
    );