using bsky.bot.Clients.Models;

namespace bsky.bot.Clients.Requests;

public readonly struct GeminiGenerateTechPostRequest
{
    public GeminiGenerateTechPostRequest() { /* ignore */ }
    public GeminiInstruction systemInstruction { get; } = new GeminiInstruction(
        "user",
        [new GeminiRequestPart("Você é um usuário de uma rede chamada bluesky, esta rede é igual ao twitter. você deve escrever posts sobre engenharia de software, desenvolvimento de sofware, ciência da computação, rotina de desenvolvedor, metodologias de produção e outros tópicos relacionados. use linguagem levemente informal. Escreva posts curtos de no máximo 300 caracteres. Gere posts sempre receber a mensagem '\\''Gere novo post'\\''. Não use hashtags.\\nAs vezes faça perguntas sobre o que desenvolvedores fazem em situações complicadas como escolher stack de projetos novos, lidar com situações dentro de empresas com vários produtos.nunca adicione aspas à mensagem")]
        );

    public GeminiInstruction[] contents { get; } =
    [
        new ("user", [
            new GeminiRequestPart("Gere novo post")
        ])
    ];

    public GenerationConfig generationConfig { get; } = new (
        2,
        64,
        0.95,
        8192,
        "text/plain"
    );

}

