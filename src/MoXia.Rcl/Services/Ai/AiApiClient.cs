using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MoXia.Rcl.Services
{
    public class AiApiClient : IAiApiClient
    {
        private const string ChatEndpointSuffix = "chat/completions";
        private const string EmbeddingEndpointSuffix = "embeddings";
        private static readonly HttpClient HttpClient = new() { Timeout = Timeout.InfiniteTimeSpan };
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private readonly ISettingService _settingService;

        public AiApiClient(ISettingService settingService)
        {
            _settingService = settingService;
        }

        public bool HasChatConfiguration
            => !string.IsNullOrWhiteSpace(_settingService.Get(s => s.AiBaseUrl))
                && !string.IsNullOrWhiteSpace(_settingService.Get(s => s.AiChatModel));

        public string EmbeddingModeLabel
        {
            get
            {
                string model = _settingService.Get(s => s.AiEmbeddingModel);
                return string.IsNullOrWhiteSpace(model) ? "Local hash 384" : model.Trim();
            }
        }

        public async Task<string> CompleteChatAsync(IReadOnlyList<AiChatMessage> messages, CancellationToken cancellationToken = default)
        {
            string baseUrl = _settingService.Get(s => s.AiBaseUrl);
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new AiApiException("请先在 AI 设置中填写兼容 OpenAI 的接口地址。");
            }

            if (messages.Count == 0)
            {
                throw new AiApiException("对话内容为空，无法请求 AI。");
            }

            string model = _settingService.Get(s => s.AiChatModel);
            if (string.IsNullOrWhiteSpace(model))
            {
                throw new AiApiException("请先填写对话模型名称。");
            }

            double temperature = Math.Clamp(_settingService.Get(s => s.AiTemperature), 0, 2);
            var payload = new
            {
                model = model.Trim(),
                messages = messages.Select(it => new { role = it.Role, content = it.Content }),
                temperature,
                stream = false,
            };

            using var request = CreateRequest(
                ResolveEndpoint(baseUrl, ChatEndpointSuffix),
                JsonSerializer.Serialize(payload, JsonOptions));

            string raw = await SendAsync(request, cancellationToken).ConfigureAwait(false);

            try
            {
                using var document = JsonDocument.Parse(raw);
                var root = document.RootElement;
                if (root.TryGetProperty("error", out var error))
                {
                    throw new AiApiException(ReadErrorMessage(error) ?? "AI 服务返回了错误。");
                }

                if (!root.TryGetProperty("choices", out var choices)
                    || choices.ValueKind != JsonValueKind.Array
                    || choices.GetArrayLength() == 0)
                {
                    throw new AiApiException("AI 响应中没有 choices 内容。");
                }

                var message = choices[0].GetProperty("message");
                string? content = ReadContent(message);
                if (string.IsNullOrWhiteSpace(content))
                {
                    throw new AiApiException("AI 返回了空内容。");
                }

                return content.Trim();
            }
            catch (JsonException ex)
            {
                throw new AiApiException("无法解析 AI 服务响应。", ex);
            }
        }

        public async Task<AiEmbeddingBatch> EmbedAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
        {
            if (texts.Count == 0)
            {
                return new AiEmbeddingBatch([], GetLocalSignature(), true);
            }

            string embeddingModel = _settingService.Get(s => s.AiEmbeddingModel);
            bool useLocal = string.IsNullOrWhiteSpace(embeddingModel);
            bool allowFallback = _settingService.Get(s => s.AiLocalEmbeddingFallback);

            if (!useLocal)
            {
                string baseUrl = _settingService.Get(s => s.AiEmbeddingBaseUrl);
                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    baseUrl = _settingService.Get(s => s.AiBaseUrl);
                }

                if (!string.IsNullOrWhiteSpace(baseUrl))
                {
                    try
                    {
                        return await EmbedRemoteAsync(baseUrl, embeddingModel.Trim(), texts, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception) when (allowFallback)
                    {
                        // A chat-only compatible provider may not expose /embeddings.
                    }
                }
                else if (!allowFallback)
                {
                    throw new AiApiException("请填写向量接口地址，或开启本地向量回退。");
                }
            }

            if (!allowFallback)
            {
                throw new AiApiException("当前未配置可用的向量模型，请在 AI 设置中开启本地向量回退。");
            }

            float[][] vectors = texts.Select(AiLocalEmbedder.Embed).ToArray();
            return new AiEmbeddingBatch(vectors, GetLocalSignature(), true);
        }

        private async Task<AiEmbeddingBatch> EmbedRemoteAsync(string baseUrl, string model, IReadOnlyList<string> texts, CancellationToken cancellationToken)
        {
            var payload = new
            {
                model,
                input = texts,
            };

            using var request = CreateRequest(
                ResolveEndpoint(baseUrl, EmbeddingEndpointSuffix),
                JsonSerializer.Serialize(payload, JsonOptions));

            string raw = await SendAsync(request, cancellationToken).ConfigureAwait(false);

            try
            {
                using var document = JsonDocument.Parse(raw);
                var root = document.RootElement;
                if (root.TryGetProperty("error", out var error))
                {
                    throw new AiApiException(ReadErrorMessage(error) ?? "向量服务返回了错误。");
                }

                if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
                {
                    throw new AiApiException("向量响应中没有 data 内容。");
                }

                var vectors = data.EnumerateArray()
                    .Select(item => item.GetProperty("embedding").EnumerateArray().Select(value => value.GetSingle()).ToArray())
                    .ToArray();

                if (vectors.Length != texts.Count || vectors.Any(it => it.Length == 0))
                {
                    throw new AiApiException("向量服务返回的数据数量或维度不正确。");
                }

                int dimension = vectors[0].Length;
                if (vectors.Any(it => it.Length != dimension))
                {
                    throw new AiApiException("向量服务返回了不一致的向量维度。");
                }

                return new AiEmbeddingBatch(vectors, $"api:{model}:{dimension}", false);
            }
            catch (JsonException ex)
            {
                throw new AiApiException("无法解析向量服务响应。", ex);
            }
        }

        private HttpRequestMessage CreateRequest(string url, string json)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };

            string apiKey = _settingService.Get(s => s.AiApiKey);
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
            }

            AddExtraHeaders(request);
            return request;
        }

        private void AddExtraHeaders(HttpRequestMessage request)
        {
            string extraHeaders = _settingService.Get(s => s.AiExtraHeadersJson);
            if (string.IsNullOrWhiteSpace(extraHeaders))
            {
                return;
            }

            try
            {
                using var document = JsonDocument.Parse(extraHeaders);
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return;
                }

                foreach (var property in document.RootElement.EnumerateObject())
                {
                    if (string.IsNullOrWhiteSpace(property.Name) || property.Value.ValueKind != JsonValueKind.String)
                    {
                        continue;
                    }

                    request.Headers.TryAddWithoutValidation(property.Name, property.Value.GetString());
                }
            }
            catch (JsonException)
            {
                // Invalid optional headers should not prevent a request without them.
            }
        }

        private async Task<string> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            int timeoutSeconds = Math.Clamp(_settingService.Get(s => s.AiRequestTimeoutSeconds), 10, 600);
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            try
            {
                using var response = await HttpClient.SendAsync(request, timeoutCts.Token).ConfigureAwait(false);
                string raw = await response.Content.ReadAsStringAsync(timeoutCts.Token).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    string message = TryReadError(raw) ?? $"AI 服务请求失败：HTTP {(int)response.StatusCode}";
                    throw new AiApiException(message);
                }

                return raw;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new AiApiException($"AI 请求超时（{timeoutSeconds} 秒）。");
            }
            catch (HttpRequestException ex)
            {
                throw new AiApiException("无法连接 AI 服务，请检查接口地址、网络或浏览器跨域设置。", ex);
            }
        }

        private static string? TryReadError(string raw)
        {
            try
            {
                using var document = JsonDocument.Parse(raw);
                if (document.RootElement.TryGetProperty("error", out var error))
                {
                    return ReadErrorMessage(error);
                }
            }
            catch (JsonException)
            {
            }

            return string.IsNullOrWhiteSpace(raw) ? null : raw.Trim()[..Math.Min(raw.Trim().Length, 600)];
        }

        private static string? ReadErrorMessage(JsonElement error)
        {
            if (error.ValueKind == JsonValueKind.String)
            {
                return error.GetString();
            }

            if (error.ValueKind == JsonValueKind.Object && error.TryGetProperty("message", out var message))
            {
                return message.GetString();
            }

            return null;
        }

        private static string? ReadContent(JsonElement message)
        {
            if (!message.TryGetProperty("content", out var content))
            {
                return null;
            }

            if (content.ValueKind == JsonValueKind.String)
            {
                return content.GetString();
            }

            if (content.ValueKind == JsonValueKind.Array)
            {
                return string.Join("\n", content.EnumerateArray().Select(item =>
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        return item.GetString();
                    }

                    if (item.TryGetProperty("text", out var text))
                    {
                        return text.GetString();
                    }

                    return item.TryGetProperty("content", out var partContent) ? partContent.GetString() : null;
                }).Where(it => !string.IsNullOrWhiteSpace(it)));
            }

            return null;
        }

        private static string ResolveEndpoint(string baseUrl, string suffix)
        {
            string url = baseUrl.Trim().TrimEnd('/');
            if (url.EndsWith($"/{suffix}", StringComparison.OrdinalIgnoreCase))
            {
                return url;
            }

            if (suffix == EmbeddingEndpointSuffix
                && url.EndsWith($"/{ChatEndpointSuffix}", StringComparison.OrdinalIgnoreCase))
            {
                url = url[..^(ChatEndpointSuffix.Length + 1)].TrimEnd('/');
            }

            return $"{url}/{suffix}";
        }

        private static string GetLocalSignature() => $"local:hash{AiLocalEmbedder.Dimension}";
    }

    internal static class AiLocalEmbedder
    {
        public const int Dimension = 384;

        public static float[] Embed(string text)
        {
            var vector = new float[Dimension];
            if (string.IsNullOrWhiteSpace(text))
            {
                return vector;
            }

            int index = 0;
            while (index < text.Length)
            {
                char current = text[index];
                if (char.IsWhiteSpace(current) || char.IsPunctuation(current) || char.IsSymbol(current))
                {
                    index++;
                    continue;
                }

                if (current <= 0x7F && char.IsLetterOrDigit(current))
                {
                    int start = index;
                    while (index < text.Length && text[index] <= 0x7F && char.IsLetterOrDigit(text[index]))
                    {
                        index++;
                    }

                    string word = text[start..index].ToLowerInvariant();
                    AddFeature(vector, word, 1.0f);
                    if (word.Length > 3)
                    {
                        for (int i = 0; i < word.Length - 2; i++)
                        {
                            AddFeature(vector, word.Substring(i, 3), 0.35f);
                        }
                    }
                    continue;
                }

                string token = char.ToLowerInvariant(current).ToString();
                AddFeature(vector, token, 1.0f);

                if (index + 1 < text.Length)
                {
                    char next = text[index + 1];
                    if (!char.IsWhiteSpace(next) && !char.IsPunctuation(next) && !char.IsSymbol(next))
                    {
                        AddFeature(vector, token + char.ToLowerInvariant(next), 0.55f);
                    }
                }

                index++;
            }

            double length = Math.Sqrt(vector.Sum(value => value * value));
            if (length > 0)
            {
                for (int i = 0; i < vector.Length; i++)
                {
                    vector[i] = (float)(vector[i] / length);
                }
            }

            return vector;
        }

        private static void AddFeature(float[] vector, string token, float weight)
        {
            ulong hash = Fnv1a64(token);
            int bucket = (int)(hash % Dimension);
            float sign = ((hash >> 32) & 1) == 0 ? 1 : -1;
            vector[bucket] += sign * weight;
        }

        private static ulong Fnv1a64(string value)
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offset;
            foreach (char character in value)
            {
                hash ^= character;
                hash *= prime;
            }

            return hash;
        }
    }
}
