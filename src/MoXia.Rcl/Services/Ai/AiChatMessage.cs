namespace MoXia.Rcl.Services
{
    public class AiChatMessage
    {
        public string Role { get; set; } = "user";

        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public static AiChatMessage System(string content)
            => new() { Role = "system", Content = content };

        public static AiChatMessage User(string content)
            => new() { Role = "user", Content = content };

        public static AiChatMessage Assistant(string content)
            => new() { Role = "assistant", Content = content };
    }

    public sealed record AiChatSource(Guid DiaryId, string Title, string Snippet, double Score);

    public sealed record AiAssistantAnswer(string Content, IReadOnlyList<AiChatSource> Sources);

    public sealed record AiNoteDraft(string Title, string Content);

    public sealed record AiEmbeddingBatch(IReadOnlyList<float[]> Vectors, string Signature, bool IsLocal);
}
