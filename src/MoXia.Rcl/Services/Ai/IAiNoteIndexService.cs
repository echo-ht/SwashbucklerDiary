using MoXia.Shared;

namespace MoXia.Rcl.Services
{
    public sealed record AiIndexProgress(int Completed, int Total, string? CurrentTitle);

    public sealed record AiIndexStatus(
        int IndexedDiaryCount,
        int ChunkCount,
        int StaleChunkCount,
        int UnindexedDiaryCount,
        string? EmbeddingSignature,
        int SignatureCount,
        DateTime? LastIndexedAt);

    public sealed record AiSearchHit(Guid DiaryId, string Title, string Snippet, string Text, double Score, int ChunkIndex);

    public interface IAiNoteIndexService
    {
        string EmbeddingModeLabel { get; }

        Task<AiIndexStatus> GetStatusAsync(CancellationToken cancellationToken = default);

        Task<AiIndexStatus> RebuildAllAsync(IProgress<AiIndexProgress>? progress = null, CancellationToken cancellationToken = default);

        Task<int> IndexDiaryAsync(DiaryModel diary, CancellationToken cancellationToken = default);

        Task RemoveDiaryAsync(Guid diaryId, CancellationToken cancellationToken = default);

        Task ClearAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AiSearchHit>> SearchAsync(string query, int? topK = null, CancellationToken cancellationToken = default);
    }
}
