using System.Text.Json.Serialization;
using bsky.bot.Clients.Objects;

namespace bsky.bot.Clients.Responses;

public readonly record struct UploadBlob(Image blob);


