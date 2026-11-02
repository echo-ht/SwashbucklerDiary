using MoXia.Rcl.Repository;
using MoXia.Shared;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MoXia.Rcl.Services
{
    public class AiNoteIndexService : IAiNoteIndexService
    {
        private const int EmbeddingBatchSize = 16;
        private readonly IDiaryService _diaryService;
        private readonly IAiChunkRepository _chunkRepository;
        private readonly IAiApiClient _aiApiClient;
        private readonly ISettingService _settingService;
        private readonly II18nService _i18n;
        private static readonly SemaphoreSlim Gate = new(1, 1);

        public AiNoteIndexService(
            IDiaryService diaryService,
            IAiChunkRepository chunkRepository,
            IAiApiClient aiApiClient,
            ISettingService settingService,
            II18nService i18n)
        {
            _diaryService = diaryService;
            _chunkRepository = chunkRepository;
            _aiApiClient = aiApiClient;
            _settingService = settingService;
            _i18n = i18n;
        }

        public string EmbeddingModeLabel => _aiApiClient.EmbeddingModeLabel;

        public async Task<AiIndexStatus> GetStatusAsync(CancellationToken cancellationToken = default)
        {
            var chunks = await _chunkRepository.GetListAsync().ConfigureAwait(false);
            var diaries = await _diaryService.QueryDiariesAsync().ConfigureAwait(false);
            var indexableDiaries = diaries
                .Where(it => !it.Template && !string.IsNullOrWhiteSpace(it.Content))
                .ToList();

            if (chunks.Count == 0)
            {
                return new AiIndexStatus(0, 0, 0, indexableDiaries.Count, null, 0, null);
            }

            var indexedDiaryIds = chunks.Select(it => it.DiaryId).Distinct().ToHashSet();
            var diaryMap = diaries.ToDictionary(it => it.Id);
            int stale = chunks.Count(chunk => !IsChunkCurrent(chunk, diaryMap));
            int unindexed = indexableDiaries.Count(it => !indexedDiaryIds.Contains(it.Id));
            int indexedDiaryCount = indexableDiaries.Count(it => indexedDiaryIds.Contains(it.Id));
            string? signature = chunks
                .Where(it => !string.IsNullOrWhiteSpace(it.EmbeddingSignature))
                .GroupBy(it => it.EmbeddingSignature)
                .OrderByDescending(group => group.Count())
                .Select(group => group.Key)
                .FirstOrDefault();
            int signatureCount = chunks.Select(it => it.EmbeddingSignature).Where(it => !string.IsNullOrWhiteSpace(it)).Distinct().Count();

            return new AiIndexStatus(
                indexedDiaryCount,
                chunks.Count,
                stale,
                unindexed,
                signature,
                signatureCount,
                chunks.Max(it => it.UpdateTime));
        }

        public async Task<AiIndexStatus> RebuildAllAsync(IProgress<AiIndexProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            await Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var diaries = await _diaryService.QueryDiariesAsync().ConfigureAwait(false);

                var pending = diaries
                    .Where(it => !it.Template && !string.IsNullOrWhiteSpace(it.Content))
                    .SelectMany(diary => AiMarkdownChunker.Split(diary.Content)
                        .Select((text, index) => new PendingChunk(diary, index, text)))
                    .ToList();

                var chunks = new List<AiChunkModel>(pending.Count);
                int completed = 0;
                foreach (var batch in pending.Chunk(EmbeddingBatchSize))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string? currentTitle = batch.FirstOrDefault() is { } first ? GetDisplayTitle(first.Diary) : null;
                    var batchList = batch.ToList();
                    var embedding = await _aiApiClient.EmbedAsync(
                        batchList.Select(it => BuildEmbeddingText(GetDisplayTitle(it.Diary), it.Text)).ToList(),
                        cancellationToken).ConfigureAwait(false);

                    chunks.AddRange(batchList.Select((item, index) => CreateChunk(
                        item.Diary,
                        item.Index,
                        item.Text,
                        embedding.Signature,
                        embedding.Vectors[index])));

                    completed += batchList.Count;
                    progress?.Report(new AiIndexProgress(completed, pending.Count, currentTitle));
                }

                await _chunkRepository.ReplaceAllAsync(chunks, cancellationToken).ConfigureAwait(false);
                return await GetStatusAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                Gate.Release();
            }
        }

        public async Task<int> IndexDiaryAsync(DiaryModel diary, CancellationToken cancellationToken = default)
        {
            if (diary.Template || string.IsNullOrWhiteSpace(diary.Content))
            {
                await RemoveDiaryAsync(diary.Id, cancellationToken).ConfigureAwait(false);
                return 0;
            }

            var pieces = AiMarkdownChunker.Split(diary.Content);
            if (pieces.Count == 0)
            {
                await RemoveDiaryAsync(diary.Id, cancellationToken).ConfigureAwait(false);
                return 0;
            }

            await Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var embedding = await _aiApiClient.EmbedAsync(
                    pieces.Select(it => BuildEmbeddingText(GetDisplayTitle(diary), it)).ToList(),
                    cancellationToken).ConfigureAwait(false);

                var chunks = pieces.Select((text, index) => CreateChunk(diary, index, text, embedding.Signature, embedding.Vectors[index])).ToList();
                await _chunkRepository.ReplaceDiaryAsync(diary.Id, chunks, cancellationToken).ConfigureAwait(false);

                return chunks.Count;
            }
            finally
            {
                Gate.Release();
            }
        }

        public async Task RemoveDiaryAsync(Guid diaryId, CancellationToken cancellationToken = default)
        {
            await Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _chunkRepository.DeleteByDiaryIdAsync(diaryId).ConfigureAwait(false);
            }
            finally
            {
                Gate.Release();
            }
        }

        public async Task ClearAsync(CancellationToken cancellationToken = default)
        {
            await Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _chunkRepository.DeleteAsync().ConfigureAwait(false);
            }
            finally
            {
                Gate.Release();
            }
        }

        public async Task<IReadOnlyList<AiSearchHit>> SearchAsync(string query, int? topK = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return [];
            }

            int limit = Math.Clamp(topK ?? _settingService.Get(s => s.AiTopK), 1, 20);
            var questionEmbedding = await _aiApiClient.EmbedAsync([query.Trim()], cancellationToken).ConfigureAwait(false);
            if (questionEmbedding.Vectors.Count == 0)
            {
                return [];
            }

            var chunks = await _chunkRepository.GetBySignatureAsync(questionEmbedding.Signature).ConfigureAwait(false);
            if (chunks.Count == 0)
            {
                return [];
            }

            float[] questionVector = questionEmbedding.Vectors[0];
            var ranked = chunks
                .Select(chunk => new { Chunk = chunk, Score = CosineSimilarity(questionVector, ParseVector(chunk.EmbeddingJson)) })
                .Where(it => it.Score > 0)
                .OrderByDescending(it => it.Score)
                .Take(limit * 6)
                .ToList();

            if (ranked.Count == 0)
            {
                return [];
            }

            var diaryIds = ranked.Select(it => it.Chunk.DiaryId).Distinct().ToList();
            var diaries = await _diaryService.QueryAsync(it => diaryIds.Contains(it.Id)).ConfigureAwait(false);
            var diaryMap = diaries.ToDictionary(it => it.Id);

            var hits = new List<AiSearchHit>();
            foreach (var item in ranked)
            {
                if (!IsChunkCurrent(item.Chunk, diaryMap))
                {
                    continue;
                }

                var diary = diaryMap[item.Chunk.DiaryId];
                string title = GetDisplayTitle(diary);
                string snippet = item.Chunk.Text.Length <= 240 ? item.Chunk.Text : item.Chunk.Text[..240] + "…";
                hits.Add(new AiSearchHit(diary.Id, title, snippet, item.Chunk.Text, item.Score, item.Chunk.ChunkIndex));

                if (hits.Count >= limit)
                {
                    break;
                }
            }

            return hits;
        }

        private static bool IsChunkCurrent(AiChunkModel chunk, IReadOnlyDictionary<Guid, DiaryModel> diaryMap)
        {
            if (!diaryMap.TryGetValue(chunk.DiaryId, out var diary) || diary.Template)
            {
                return false;
            }

            return Math.Abs((diary.UpdateTime - chunk.DiaryUpdateTime).TotalSeconds) < 1;
        }

        private AiChunkModel CreateChunk(DiaryModel diary, int index, string text, string signature, float[] vector)
        {
            var now = DateTime.Now;
            return new AiChunkModel
            {
                Id = Guid.NewGuid(),
                DiaryId = diary.Id,
                DiaryTitle = GetDisplayTitle(diary),
                ChunkIndex = index,
                Text = text,
                EmbeddingJson = JsonSerializer.Serialize(vector),
                EmbeddingSignature = signature,
                ContentHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))),
                DiaryUpdateTime = diary.UpdateTime,
                CreateTime = now,
                UpdateTime = now,
            };
        }

        private static string BuildEmbeddingText(string? title, string text)
            => string.IsNullOrWhiteSpace(title) ? text : $"{title}\n{text}";

        private string GetDisplayTitle(DiaryModel diary)
        {
            if (!string.IsNullOrWhiteSpace(diary.Title))
            {
                return diary.Title.Trim();
            }

            if (!string.IsNullOrWhiteSpace(diary.Content))
            {
                foreach (string line in diary.Content.Replace("\r\n", "\n").Split('\n'))
                {
                    string candidate = line.Trim().TrimStart('#').Trim();
                    if (candidate.Length == 0)
                    {
                        continue;
                    }

                    return candidate.Length <= 40 ? candidate : candidate[..40];
                }
            }

            return _i18n.T("Untitled");
        }

        private static float[] ParseVector(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<float[]>(json) ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
        }

        private static double CosineSimilarity(float[] left, float[] right)
        {
            if (left.Length == 0 || right.Length == 0 || left.Length != right.Length)
            {
                return 0;
            }

            double dot = 0;
            double leftLength = 0;
            double rightLength = 0;
            for (int index = 0; index < left.Length; index++)
            {
                dot += left[index] * right[index];
                leftLength += left[index] * left[index];
                rightLength += right[index] * right[index];
            }

            if (leftLength == 0 || rightLength == 0)
            {
                return 0;
            }

            return dot / (Math.Sqrt(leftLength) * Math.Sqrt(rightLength));
        }

        private sealed record PendingChunk(DiaryModel Diary, int Index, string Text);
    }
}
