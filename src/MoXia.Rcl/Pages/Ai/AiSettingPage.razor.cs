using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MoXia.Rcl.Components;
using MoXia.Rcl.Services;

namespace MoXia.Rcl.Pages
{
    public partial class AiSettingPage : ImportantComponentBase
    {
        private static bool SupportsMcpControl
            => !OperatingSystem.IsAndroid()
                && !OperatingSystem.IsIOS()
                && !OperatingSystem.IsBrowser();

        private readonly CancellationTokenSource pageCts = new();
        private string baseUrl = string.Empty;
        private string apiKey = string.Empty;
        private string chatModel = string.Empty;
        private string embeddingBaseUrl = string.Empty;
        private string embeddingModel = string.Empty;
        private string extraHeadersJson = string.Empty;
        private string timeoutText = "120";
        private string topKText = "6";
        private string temperatureText = "0.3";
        private string? errorText;
        private string? testResult;
        private bool localEmbeddingFallback = true;
        private bool mcpEnabled;
        private bool mcpAllowPrivateLibrary;
        private bool showApiKey;
        private bool showClearIndex;
        private bool isSaving;
        private bool isTesting;
        private bool isIndexing;
        private AiIndexStatus status = new(0, 0, 0, 0, null, 0, null);
        private AiIndexProgress? indexProgress;

        [Inject]
        private IAiApiClient AiApiClient { get; set; } = default!;

        [Inject]
        private IAiNoteIndexService indexService { get; set; } = default!;

        [Inject]
        private ILogger<AiSettingPage> Logger { get; set; } = default!;

        protected override async Task OnInitializedAsync()
        {
            ReadFields();
            await LoadStatusAsync();
            await base.OnInitializedAsync();
        }

        protected override async ValueTask DisposeAsyncCore()
        {
            pageCts.Cancel();
            pageCts.Dispose();
            await base.DisposeAsyncCore();
        }

        private string IndexStateText
        {
            get
            {
                if (status.ChunkCount == 0)
                {
                    return I18n.T("Not built");
                }

                return status.StaleChunkCount > 0 || status.UnindexedDiaryCount > 0 || status.SignatureCount > 1
                    ? I18n.T("Needs refresh")
                    : I18n.T("Ready");
            }
        }

        private string IndexStateClass
        {
            get
            {
                if (status.ChunkCount == 0)
                {
                    return "is-idle";
                }

                return status.StaleChunkCount > 0 || status.UnindexedDiaryCount > 0 || status.SignatureCount > 1 ? "is-warning" : "is-ready";
            }
        }

        private int IndexProgressPercent
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

        private string IndexProgressText => indexProgress is null
            ? I18n.T("Preparing")
            : $"{indexProgress.Completed}/{indexProgress.Total}";

        private void ReadFields()
        {
            baseUrl = SettingService.Get(s => s.AiBaseUrl);
            apiKey = SettingService.Get(s => s.AiApiKey);
            chatModel = SettingService.Get(s => s.AiChatModel);
            embeddingBaseUrl = SettingService.Get(s => s.AiEmbeddingBaseUrl);
            embeddingModel = SettingService.Get(s => s.AiEmbeddingModel);
            extraHeadersJson = SettingService.Get(s => s.AiExtraHeadersJson);
            timeoutText = SettingService.Get(s => s.AiRequestTimeoutSeconds).ToString();
            topKText = SettingService.Get(s => s.AiTopK).ToString();
            temperatureText = SettingService.Get(s => s.AiTemperature).ToString("0.##");
            localEmbeddingFallback = SettingService.Get(s => s.AiLocalEmbeddingFallback);
            mcpEnabled = SettingService.Get(s => s.McpEnabled);
            mcpAllowPrivateLibrary = SettingService.Get(s => s.McpAllowPrivateLibrary);
        }

        private async Task SaveSettingsAsync()
        {
            await SaveSettingsCoreAsync(true);
        }

        private async Task<bool> SaveSettingsCoreAsync(bool showSuccess)
        {
            if (isSaving)
            {
                return false;
            }

            errorText = null;
            testResult = null;
            if (!int.TryParse(timeoutText, out int timeout) || timeout < 10 || timeout > 600)
            {
                errorText = I18n.T("Timeout must be between 10 and 600 seconds.");
                return false;
            }

            if (!int.TryParse(topKText, out int topK) || topK < 1 || topK > 20)
            {
                errorText = I18n.T("Top K must be between 1 and 20.");
                return false;
            }

            if (!double.TryParse(temperatureText, out double temperature) || temperature < 0 || temperature > 2)
            {
                errorText = I18n.T("Temperature must be between 0 and 2.");
                return false;
            }

            if (!ValidateExtraHeaders())
            {
                return false;
            }

            isSaving = true;
            StateHasChanged();
            try
            {
                await SettingService.SetAsync(s => s.AiBaseUrl, baseUrl.Trim());
                await SettingService.SetAsync(s => s.AiApiKey, apiKey.Trim());
                await SettingService.SetAsync(s => s.AiChatModel, chatModel.Trim());
                await SettingService.SetAsync(s => s.AiEmbeddingBaseUrl, embeddingBaseUrl.Trim());
                await SettingService.SetAsync(s => s.AiEmbeddingModel, embeddingModel.Trim());
                await SettingService.SetAsync(s => s.AiExtraHeadersJson, extraHeadersJson.Trim());
                await SettingService.SetAsync(s => s.AiRequestTimeoutSeconds, timeout);
                await SettingService.SetAsync(s => s.AiTopK, topK);
                await SettingService.SetAsync(s => s.AiTemperature, temperature);
                await SettingService.SetAsync(s => s.AiLocalEmbeddingFallback, localEmbeddingFallback);
                await SettingService.SetAsync(s => s.McpEnabled, mcpEnabled);
                await SettingService.SetAsync(s => s.McpAllowPrivateLibrary, mcpAllowPrivateLibrary);

                if (showSuccess)
                {
                    await AlertService.SuccessAsync(I18n.T("AI settings saved."));
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to save AI settings");
                errorText = ex.Message;
                return false;
            }
            finally
            {
                isSaving = false;
                StateHasChanged();
            }
        }

        private async Task TestConnectionAsync()
        {
            if (isTesting || !await SaveSettingsCoreAsync(false))
            {
                return;
            }

            isTesting = true;
            testResult = null;
            errorText = null;
            StateHasChanged();
            try
            {
                string reply = await AiApiClient.CompleteChatAsync(
                    [AiChatMessage.User("Reply with exactly: OK")],
                    pageCts.Token);
                var embedding = await AiApiClient.EmbedAsync(["connection test"], pageCts.Token);
                testResult = $"{I18n.T("Connection successful")} · {reply.Trim()} · {embedding.Signature}";
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "AI connection test failed");
                errorText = ex.Message;
            }
            finally
            {
                isTesting = false;
                StateHasChanged();
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

        private async Task ClearIndexAsync()
        {
            showClearIndex = false;
            try
            {
                await indexService.ClearAsync(pageCts.Token);
                await LoadStatusAsync();
                await AlertService.SuccessAsync(I18n.T("Vector index cleared."));
            }
            catch (Exception ex)
            {
                errorText = ex.Message;
            }
        }

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

        private bool ValidateExtraHeaders()
        {
            if (string.IsNullOrWhiteSpace(extraHeadersJson))
            {
                return true;
            }

            try
            {
                using var document = System.Text.Json.JsonDocument.Parse(extraHeadersJson);
                if (document.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object)
                {
                    errorText = I18n.T("Extra headers must be a JSON object.");
                    return false;
                }

                return true;
            }
            catch (System.Text.Json.JsonException)
            {
                errorText = I18n.T("Extra headers contain invalid JSON.");
                return false;
            }
        }

        private void ApplyDeepSeekPreset()
        {
            baseUrl = "https://api.deepseek.com/v1";
            chatModel = "deepseek-chat";
            embeddingBaseUrl = string.Empty;
            embeddingModel = string.Empty;
        }

        private void ApplyOpenAiPreset()
        {
            baseUrl = "https://api.openai.com/v1";
            chatModel = "gpt-4o-mini";
            embeddingBaseUrl = string.Empty;
            embeddingModel = "text-embedding-3-small";
        }

        private void ApplySiliconFlowPreset()
        {
            baseUrl = "https://api.siliconflow.cn/v1";
            chatModel = "deepseek-ai/DeepSeek-V3";
            embeddingBaseUrl = string.Empty;
            embeddingModel = "BAAI/bge-m3";
        }

        private void ApplyOpenRouterPreset()
        {
            baseUrl = "https://openrouter.ai/api/v1";
            chatModel = "deepseek/deepseek-chat";
            embeddingBaseUrl = string.Empty;
            embeddingModel = string.Empty;
        }

        private void ApplyOllamaPreset()
        {
            baseUrl = "http://localhost:11434/v1";
            chatModel = "deepseek-r1:7b";
            embeddingBaseUrl = string.Empty;
            embeddingModel = "nomic-embed-text";
        }
    }
}
