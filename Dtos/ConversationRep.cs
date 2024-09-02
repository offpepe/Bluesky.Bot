namespace bsky.bot.Dtos;

public readonly record struct ConversationRep(string root, ConversationReply reply);

public record ConversationReply(string value, bool self, ConversationReply? reply);