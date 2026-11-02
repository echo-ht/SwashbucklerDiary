using MoXia.Shared;

namespace MoXia.Rcl.Repository
{
    public interface IAiChunkRepository : IBaseRepository<AiChunkModel>
    {
        Task<List<AiChunkModel>> GetByDiaryIdAsync(Guid diaryId);

        Task<List<AiChunkModel>> GetBySignatureAsync(string signature);

        Task DeleteByDiaryIdAsync(Guid diaryId);

        Task ReplaceDiaryAsync(Guid diaryId, IReadOnlyList<AiChunkModel> chunks, CancellationToken cancellationToken = default);

        Task ReplaceAllAsync(IReadOnlyList<AiChunkModel> chunks, CancellationToken cancellationToken = default);
    }
}
