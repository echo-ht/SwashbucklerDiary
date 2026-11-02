namespace MoXia.Rcl.Services
{
    public interface IAiNoteAssistant
    {
        Task<AiAssistantAnswer> AskAsync(
            string question,
            IReadOnlyList<AiChatMessage> history,
            CancellationToken cancellationToken = default);

        Task<AiNoteDraft> ConversationToNoteAsync(
            IReadOnlyList<AiChatMessage> history,
            CancellationToken cancellationToken = default);
    }
}
