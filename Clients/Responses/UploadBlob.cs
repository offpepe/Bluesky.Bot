using System.Text.Json.Serialization;
using bsky.bot.Clients.Models;

namespace bsky.bot.Clients.Responses;

public readonly record struct UploadBlob(Blob blob);


