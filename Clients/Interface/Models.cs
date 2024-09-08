using bsky.bot.Clients.Requests;

namespace bsky.bot.Clients.Interface;

public interface ILllmModel
{
    Task<string> Generate(LLMRequest message);
    Task<bool> IsTechContent(string content);
}