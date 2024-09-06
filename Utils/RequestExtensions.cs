using System.Net.Http.Headers;
using bsky.bot.Clients.Models;
using bsky.bot.Clients.Requests;

namespace bsky.bot.Utils;

public static class RequestExtensions
{
    public static string ConvertRequestToString(this LLMRequest request) => request.contents.Length switch
    {
        0 => string.Empty,
        1 => request.contents[0].parts[0].text,
        _ => request.contents.Aggregate(string.Empty,
            (current, instruction) => current + $"{instruction.role}: {instruction.parts[0].text}\n")
    };

}