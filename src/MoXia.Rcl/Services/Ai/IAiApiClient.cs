namespace MoXia.Rcl.Services
{
    public interface IAiApiClient
    {
        bool HasChatConfiguration { get; }

        string EmbeddingModeLabel { get; }

        Task<string> CompleteChatAsync(IReadOnlyList<AiChatMessage> messages, CancellationToken cancellationToken = default);

        Task<AiEmbeddingBatch> EmbedAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default);
    }
}
