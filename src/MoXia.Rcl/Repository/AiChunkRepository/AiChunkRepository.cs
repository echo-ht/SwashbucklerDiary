using MoXia.Rcl.Services;
using MoXia.Shared;
using SqlSugar;

namespace MoXia.Rcl.Repository
{
    public class AiChunkRepository : BaseRepository<AiChunkModel>, IAiChunkRepository
    {
        public AiChunkRepository(ISqlSugarClient context,
            ISettingService settingService,
            IDatabaseScopeAccessor databaseScope)
            : base(context, settingService, databaseScope)
        {
        }

        public Task<List<AiChunkModel>> GetByDiaryIdAsync(Guid diaryId)
            => Context.Queryable<AiChunkModel>()
                .Where(it => it.DiaryId == diaryId)
                .OrderBy(it => it.ChunkIndex)
                .ToListAsync();

        public Task<List<AiChunkModel>> GetBySignatureAsync(string signature)
            => Context.Queryable<AiChunkModel>()
                .Where(it => it.EmbeddingSignature == signature)
                .ToListAsync();

        public Task DeleteByDiaryIdAsync(Guid diaryId)
            => Context.Deleteable<AiChunkModel>()
                .Where(it => it.DiaryId == diaryId)
                .ExecuteCommandAsync();

        public async Task ReplaceDiaryAsync(Guid diaryId, IReadOnlyList<AiChunkModel> chunks, CancellationToken cancellationToken = default)
        {
            var db = Context;
            await db.Ado.BeginTranAsync().ConfigureAwait(false);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await db.Deleteable<AiChunkModel>()
                    .Where(it => it.DiaryId == diaryId)
                    .ExecuteCommandAsync()
                    .ConfigureAwait(false);

                foreach (var batch in chunks.Chunk(100))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await db.Insertable(batch.ToList()).ExecuteCommandAsync().ConfigureAwait(false);
                }

                await db.Ado.CommitTranAsync().ConfigureAwait(false);
            }
            catch
            {
                await db.Ado.RollbackTranAsync().ConfigureAwait(false);
                throw;
            }
        }

        public async Task ReplaceAllAsync(IReadOnlyList<AiChunkModel> chunks, CancellationToken cancellationToken = default)
        {
            var db = Context;
            await db.Ado.BeginTranAsync().ConfigureAwait(false);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await db.Deleteable<AiChunkModel>().ExecuteCommandAsync().ConfigureAwait(false);

                foreach (var batch in chunks.Chunk(100))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await db.Insertable(batch.ToList()).ExecuteCommandAsync().ConfigureAwait(false);
                }

                await db.Ado.CommitTranAsync().ConfigureAwait(false);
            }
            catch
            {
                await db.Ado.RollbackTranAsync().ConfigureAwait(false);
                throw;
            }
        }
    }
}
