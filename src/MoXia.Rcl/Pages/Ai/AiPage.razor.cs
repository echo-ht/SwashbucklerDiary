using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MoXia.Rcl.Components;
using MoXia.Rcl.Essentials;
using MoXia.Rcl.Extensions;
using MoXia.Rcl.Services;
using MoXia.Shared;

namespace MoXia.Rcl.Pages
{
    public partial class AiPage : ImportantComponentBase
    {
        private readonly CancellationTokenSource pageCts = new();
        private readonly List<ChatTurn> turns = [];
        private AiIndexStatus status = new(0, 0, 0, 0, null, 0, null);
        private AiIndexProgress? indexProgress;
        private CancellationTokenSource? activeRequest;
        private string question = string.Empty;
        private string? errorText;
        private string draftTitle = string.Empty;
        private string draftContent = string.Empty;
        private bool isSending;
        private bool isIndexing;
        private bool isDrafting;
        private bool isSavingNote;
        private bool showSaveDialog;
        private bool privacyMode;
        private int topK = 6;

        [Inject]
        private IAiNoteAssistant NoteAssistant { get; set; } = default!;

        [Inject]
        private IAiNoteIndexService indexService { get; set; } = default!;

        [Inject]
        private IDiaryService DiaryService { get; set; } = default!;

        [Inject]
        private ILogger<AiPage> Logger { get; set; } = default!;

        protected override async Task OnInitializedAsync()
        {
            privacyMode = SettingService.GetTemp(s => s.PrivacyMode);
            topK = Math.Clamp(SettingService.Get(s => s.AiTopK), 1, 20);
            await LoadStatusAsync();
            await base.OnInitializedAsync();
        }

        protected override async ValueTask DisposeAsyncCore()
        {
            pageCts.Cancel();
            activeRequest?.Cancel();
            activeRequest?.Dispose();
            pageCts.Dispose();
            await base.DisposeAsyncCore();
        }

        private IReadOnlyList<string> Suggestions
        {
            get
            {
                bool chinese = I18n.Culture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
                return chinese
                    ? ["总结最近笔记里的主题", "找出我反复记录的问题", "把相关笔记整理成行动清单"]
                    : ["Summarize recent themes", "Find recurring problems", "Turn related notes into an action list"];
            }
        }

        private string KnowledgeStateText
        {
            get
            {
                if (status.ChunkCount == 0)
                {
                    return I18n.T("Not built");
                }

                if (status.SignatureCount > 1 || status.StaleChunkCount > 0 || status.UnindexedDiaryCount > 0)
                {
                    return I18n.T("Needs refresh");
                }

                return I18n.T("Ready");
            }
        }

        private string KnowledgeStateClass
        {
            get
            {
                if (status.ChunkCount == 0)
                {
                    return "is-idle";
                }

                return status.SignatureCount > 1 || status.StaleChunkCount > 0 || status.UnindexedDiaryCount > 0 ? "is-warning" : "is-ready";
            }
        }

        private int ProgressPercent
        {
            get
            {
                if (indexProgress is null || indexProgress.Total == 0)
                {
                    return 0;
                }

                return (int)Math.Round(indexProgress.Completed * 100d / indexProgress.Total);
            }
        }

        private string ProgressText => indexProgress is null
            ? I18n.T("Preparing")
            : $"{indexProgress.Completed}/{indexProgress.Total}";

        private async Task LoadStatusAsync()
        {
            try
            {
                status = await indexService.GetStatusAsync(pageCts.Token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to read AI index status");
            }
        }

        private Task SendSuggestionAsync(string suggestion)
        {
            question = suggestion;
            return SendAsync();
        }

        private async Task SendOrCancelAsync()
        {
            if (isSending)
            {
                activeRequest?.Cancel();
                return;
            }

            await SendAsync();
        }

        private async Task SendAsync()
        {
            string prompt = question.Trim();
            if (prompt.Length == 0 || isSending)
            {
                return;
            }

            question = string.Empty;
            errorText = null;
            turns.Add(new ChatTurn(AiChatMessage.User(prompt), []));
            isSending = true;
            StateHasChanged();
            await ScrollToBottomAsync();

            activeRequest?.Dispose();
            activeRequest = CancellationTokenSource.CreateLinkedTokenSource(pageCts.Token);
            try
            {
                var history = turns.Select(it => it.Message).ToList();
                var answer = await NoteAssistant.AskAsync(prompt, history, activeRequest.Token);
                turns.Add(new ChatTurn(AiChatMessage.Assistant(answer.Content), answer.Sources.ToList()));
            }
            catch (OperationCanceledException)
            {
                if (!pageCts.IsCancellationRequested)
                {
                    errorText = I18n.T("The request was stopped.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "AI chat failed");
                errorText = ex.Message;
            }
            finally
            {
                isSending = false;
                StateHasChanged();
                await ScrollToBottomAsync();
            }
        }

        private async Task RebuildIndexAsync()
        {
            if (isIndexing)
            {
                return;
            }

            isIndexing = true;
            indexProgress = null;
            errorText = null;
            StateHasChanged();

            var progress = new Progress<AiIndexProgress>(value =>
            {
                indexProgress = value;
                InvokeAsync(StateHasChanged);
            });

            try
            {
                status = await indexService.RebuildAllAsync(progress, pageCts.Token);
                await AlertService.SuccessAsync(I18n.T("Vector index updated."));
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "AI index rebuild failed");
                errorText = ex.Message;
            }
            finally
            {
                isIndexing = false;
                indexProgress = null;
                await LoadStatusAsync();
                StateHasChanged();
            }
        }

        private async Task GenerateNoteDraftAsync()
        {
            if (isDrafting || turns.Count == 0)
            {
                return;
            }

            isDrafting = true;
            errorText = null;
            StateHasChanged();

            activeRequest?.Dispose();
            activeRequest = CancellationTokenSource.CreateLinkedTokenSource(pageCts.Token);
            try
            {
                var draft = await NoteAssistant.ConversationToNoteAsync(turns.Select(it => it.Message).ToList(), activeRequest.Token);
                draftTitle = draft.Title;
                draftContent = draft.Content;
                showSaveDialog = true;
            }
            catch (OperationCanceledException)
            {
                if (!pageCts.IsCancellationRequested)
                {
                    errorText = I18n.T("The request was stopped.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "AI conversation note generation failed");
                errorText = ex.Message;
            }
            finally
            {
                isDrafting = false;
                StateHasChanged();
            }
        }

        private async Task SaveNoteAsync((string Title, string Content) draft)
        {
            if (isSavingNote)
            {
                return;
            }

            isSavingNote = true;
            var diary = new DiaryModel
            {
                Id = Guid.NewGuid(),
                Title = string.IsNullOrWhiteSpace(draft.Title) ? I18n.T("AI conversation note") : draft.Title.Trim(),
                Content = draft.Content.Trim(),
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now,
                Tags = [],
                Resources = [],
            };

            try
            {
                if (!await DiaryService.AddAsync(diary))
                {
                    await AlertService.ErrorAsync(I18n.T("Add failed"));
                    return;
                }

                try
                {
                    await indexService.IndexDiaryAsync(diary, pageCts.Token);
                    await AlertService.SuccessAsync(I18n.T("Note saved and added to the vector index."));
                }
                catch (OperationCanceledException) when (pageCts.IsCancellationRequested)
                {
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "AI note was saved but indexing failed");
                    await AlertService.WarningAsync(I18n.T("Note saved, but indexing failed."), ex.Message);
                }

                showSaveDialog = false;
                await LoadStatusAsync();
                StateHasChanged();
            }
            finally
            {
                isSavingNote = false;
            }
        }

        private void NewConversation()
        {
            if (isSending)
            {
                return;
            }

            turns.Clear();
            question = string.Empty;
            errorText = null;
            StateHasChanged();
        }

        private void OpenNote(Guid diaryId)
        {
            To($"read/{diaryId}", false);
        }

        private async Task ScrollToBottomAsync()
        {
            try
            {
                await Task.Delay(30);
                await JS.EvaluateJavascript("const el = document.querySelector('.ai-message-list'); if (el) el.scrollTop = el.scrollHeight;");
            }
            catch
            {
            }
        }

        private sealed record ChatTurn(AiChatMessage Message, List<AiChatSource> Sources);
    }
}
