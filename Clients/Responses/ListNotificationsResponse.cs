using bsky.bot.Clients.Objects;

namespace bsky.bot.Clients.Responses;

public readonly record struct ListNotificationsResponse(Notification[] notifications);