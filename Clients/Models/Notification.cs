namespace bsky.bot.Clients.Models;

public readonly record struct Notification(
    string uri,
    string cid,
    Author author,
    Viewer viewer,
    string reason,
    bool isRead,
    Record? record
);

public readonly record struct Author(string did, string handle, string displayName, string dvatar);

public readonly record struct Viewer(bool muted, bool blockedBy, string following);

