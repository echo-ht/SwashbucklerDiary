using MoXia.Rcl.Services;
using MoXia.Shared;

namespace MoXia.Control
{
    public sealed class McpNoteService(
        IDiaryService diaryService,
        ITagService tagService,
        IAiNoteIndexService indexService,
        IAiNoteAssistant noteAssistant,
        ISettingService settingService,
        IDatabaseScopeAccessor databaseScope)
    {
        private const int MaxContentLength = 500_000;

        public async Task<object> ListNotesAsync(
            LibraryKind library,
            string? query,
            int limit,
            int offset,
            CancellationToken cancellationToken)
        {
            EnsureLibraryAllowed(library);
            using var scope = databaseScope.Use(library);

            var diaries = await diaryService.QueryDiariesAsync().ConfigureAwait(false);
            IEnumerable<DiaryModel> filtered = diaries;
            if (!string.IsNullOrWhiteSpace(query))
            {
                string keyword = query.Trim();
                filtered = filtered.Where(it =>
                    (it.Title?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (it.Content?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            cancellationToken.ThrowIfCancellationRequested();
            var ordered = filtered.OrderByDescending(it => it.UpdateTime).ToList();
            var notes = ordered
                .Skip(offset)
                .Take(limit)
                .Select(it => new
                {
                    id = it.Id,
                    title = GetDisplayTitle(it),
                    snippet = BuildSnippet(it.Content, 220),
                    tags = it.Tags?.Select(tag => tag.Name).Where(name => !string.IsNullOrWhiteSpace(name)).ToArray() ?? [],
                    createdAt = it.CreateTime,
                    updatedAt = it.UpdateTime
                })
                .ToArray();

            return new
            {
                library = LibraryName(library),
                total = ordered.Count,
                returned = notes.Length,
                notes
            };
        }

        public async Task<object> GetNoteAsync(LibraryKind library, Guid id, CancellationToken cancellationToken)
        {
            EnsureLibraryAllowed(library);
            using var scope = databaseScope.Use(library);

            var diary = await GetEditableNoteAsync(id).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return ToNoteDetail(library, diary);
        }

        public async Task<object> CreateNoteAsync(
            LibraryKind library,
            string? title,
            string content,
            IReadOnlyList<string>? tags,
            CancellationToken cancellationToken)
        {
            EnsureLibraryAllowed(library);
            using var scope = databaseScope.Use(library);

            content = NormalizeContent(content);
            DateTime now = DateTime.Now;
            var diary = new DiaryModel
            {
                Id = Guid.NewGuid(),
                Title = NormalizeTitle(title),
                Content = content,
                CreateTime = now,
                UpdateTime = now,
                Tags = await ResolveTagsAsync(tags).ConfigureAwait(false),
                Resources = []
            };

            if (!await diaryService.AddAsync(diary).ConfigureAwait(false))
            {
                throw new McpControlException("创建笔记失败。", 500);
            }

            diary = await GetEditableNoteAsync(diary.Id).ConfigureAwait(false);
            string? indexWarning = await TryIndexAsync(diary, cancellationToken).ConfigureAwait(false);
            return new
            {
                library = LibraryName(library),
                note = ToNoteDetail(library, diary),
                indexed = indexWarning is null,
                warning = indexWarning
            };
        }

        public async Task<object> UpdateNoteAsync(
            LibraryKind library,
            Guid id,
            string? title,
            string? content,
            IReadOnlyList<string>? tags,
            CancellationToken cancellationToken)
        {
            EnsureLibraryAllowed(library);
            using var scope = databaseScope.Use(library);

            var diary = await GetEditableNoteAsync(id).ConfigureAwait(false);
            if (title is not null)
            {
                diary.Title = NormalizeTitle(title);
            }

            if (content is not null)
            {
                diary.Content = NormalizeContent(content);
            }

            if (tags is not null)
            {
                diary.Tags = await ResolveTagsAsync(tags).ConfigureAwait(false);
            }

            diary.UpdateTime = DateTime.Now;
            if (!await diaryService.UpdateIncludesAsync(diary).ConfigureAwait(false))
            {
                throw new McpControlException("更新笔记失败。", 500);
            }

            diary = await GetEditableNoteAsync(diary.Id).ConfigureAwait(false);
            string? indexWarning = await TryIndexAsync(diary, cancellationToken).ConfigureAwait(false);
            return new
            {
                library = LibraryName(library),
                note = ToNoteDetail(library, diary),
                indexed = indexWarning is null,
                warning = indexWarning
            };
        }

        public async Task<object> DeleteNoteAsync(LibraryKind library, Guid id, CancellationToken cancellationToken)
        {
            EnsureLibraryAllowed(library);
            using var scope = databaseScope.Use(library);

            var diary = await GetEditableNoteAsync(id).ConfigureAwait(false);
            if (!await diaryService.DeleteAsync(diary).ConfigureAwait(false))
            {
                throw new McpControlException("删除笔记失败。", 500);
            }

            string? indexWarning = null;
            try
            {
                await indexService.RemoveDiaryAsync(id, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                indexWarning = ex.Message;
            }

            return new
            {
                library = LibraryName(library),
                deleted = true,
                id,
                title = GetDisplayTitle(diary),
                warning = indexWarning
            };
        }

        public async Task<object> SearchNotesAsync(
            LibraryKind library,
            string query,
            int limit,
            CancellationToken cancellationToken)
        {
            EnsureLibraryAllowed(library);
            using var scope = databaseScope.Use(library);

            var hits = await indexService.SearchAsync(query, limit, cancellationToken).ConfigureAwait(false);
            return new
            {
                library = LibraryName(library),
                query,
                count = hits.Count,
                results = hits.Select(it => new
                {
                    id = it.DiaryId,
                    title = it.Title,
                    snippet = it.Snippet,
                    score = Math.Round(it.Score, 4),
                    chunkIndex = it.ChunkIndex
                }).ToArray()
            };
        }

        public async Task<object> AskNotesAsync(
            LibraryKind library,
            string question,
            CancellationToken cancellationToken)
        {
            EnsureLibraryAllowed(library);
            using var scope = databaseScope.Use(library);

            var answer = await noteAssistant.AskAsync(question, [], cancellationToken).ConfigureAwait(false);
            return new
            {
                library = LibraryName(library),
                question,
                answer = answer.Content,
                sources = answer.Sources.Select(it => new
                {
                    id = it.DiaryId,
                    title = it.Title,
                    snippet = it.Snippet,
                    score = Math.Round(it.Score, 4)
                }).ToArray()
            };
        }

        public async Task<object> GetIndexStatusAsync(LibraryKind library, CancellationToken cancellationToken)
        {
            EnsureLibraryAllowed(library);
            using var scope = databaseScope.Use(library);

            var status = await indexService.GetStatusAsync(cancellationToken).ConfigureAwait(false);
            return new
            {
                library = LibraryName(library),
                embeddingMode = indexService.EmbeddingModeLabel,
                status.IndexedDiaryCount,
                status.ChunkCount,
                status.StaleChunkCount,
                status.UnindexedDiaryCount,
                status.EmbeddingSignature,
                status.SignatureCount,
                status.LastIndexedAt
            };
        }

        public async Task<object> RebuildIndexAsync(LibraryKind library, CancellationToken cancellationToken)
        {
            EnsureLibraryAllowed(library);
            using var scope = databaseScope.Use(library);

            var status = await indexService.RebuildAllAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            return new
            {
                library = LibraryName(library),
                rebuilt = true,
                embeddingMode = indexService.EmbeddingModeLabel,
                status.IndexedDiaryCount,
                status.ChunkCount,
                status.StaleChunkCount,
                status.UnindexedDiaryCount,
                status.EmbeddingSignature,
                status.SignatureCount,
                status.LastIndexedAt
            };
        }

        private void EnsureLibraryAllowed(LibraryKind library)
        {
            if (library == LibraryKind.Private && !settingService.Get(it => it.McpAllowPrivateLibrary))
            {
                throw new McpControlException("隐私库未对 MCP 开放。请在墨匣 AI 设置中开启“允许 MCP 访问隐私库”。", 403);
            }
        }

        private async Task<DiaryModel> GetEditableNoteAsync(Guid id)
        {
            var diary = await diaryService.FindAsync(id).ConfigureAwait(false);
            if (diary is null || diary.Template)
            {
                throw new McpControlException($"未找到笔记：{id}", 404);
            }

            return diary;
        }

        private async Task<string?> TryIndexAsync(DiaryModel diary, CancellationToken cancellationToken)
        {
            try
            {
                await indexService.IndexDiaryAsync(diary, cancellationToken).ConfigureAwait(false);
                return null;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private async Task<List<TagModel>> ResolveTagsAsync(IReadOnlyList<string>? tags)
        {
            if (tags is null || tags.Count == 0)
            {
                return [];
            }

            string[] names = tags
                .Select(it => it.Trim())
                .Where(it => it.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(20)
                .ToArray();
            if (names.Length == 0)
            {
                return [];
            }

            var existing = await tagService.QueryAsync(it => it.Name != null && names.Contains(it.Name)).ConfigureAwait(false);
            var byName = existing
                .Where(it => !string.IsNullOrWhiteSpace(it.Name))
                .GroupBy(it => it.Name!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

            var newTags = new List<TagModel>();
            DateTime now = DateTime.Now;
            foreach (string name in names)
            {
                if (byName.ContainsKey(name))
                {
                    continue;
                }

                var tag = new TagModel
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    CreateTime = now,
                    UpdateTime = now
                };
                byName[name] = tag;
                newTags.Add(tag);
            }

            if (newTags.Count > 0 && !await tagService.AddAsync(newTags).ConfigureAwait(false))
            {
                throw new McpControlException("创建标签失败。", 500);
            }

            return names.Select(name => byName[name]).ToList();
        }

        private static object ToNoteDetail(LibraryKind library, DiaryModel diary)
            => new
            {
                library = LibraryName(library),
                id = diary.Id,
                title = GetDisplayTitle(diary),
                content = diary.Content ?? string.Empty,
                tags = diary.Tags?.Select(tag => tag.Name).Where(name => !string.IsNullOrWhiteSpace(name)).ToArray() ?? [],
                createdAt = diary.CreateTime,
                updatedAt = diary.UpdateTime
            };

        private static string LibraryName(LibraryKind library)
            => library == LibraryKind.Private ? "private" : "normal";

        private static string NormalizeTitle(string? title)
        {
            string value = title?.Trim() ?? string.Empty;
            return value.Length <= 200 ? value : value[..200];
        }

        private static string NormalizeContent(string content)
        {
            content = content.Trim();
            if (content.Length == 0)
            {
                throw new McpControlException("笔记正文不能为空。");
            }

            if (content.Length > MaxContentLength)
            {
                throw new McpControlException($"笔记正文不能超过 {MaxContentLength} 个字符。");
            }

            return content;
        }

        private static string GetDisplayTitle(DiaryModel diary)
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
                    if (candidate.Length > 0)
                    {
                        return candidate.Length <= 40 ? candidate : candidate[..40];
                    }
                }
            }

            return "Untitled";
        }

        private static string BuildSnippet(string? content, int length)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return string.Empty;
            }

            string text = string.Join(' ', content.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            return text.Length <= length ? text : text[..length] + "…";
        }
    }
}
