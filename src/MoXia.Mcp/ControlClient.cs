using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MoXia.Shared;

namespace MoXia.Mcp
{
    internal sealed class ControlToolException(string message) : Exception(message)
    {
    }

    internal sealed class ControlUnavailableException(string message, Exception? innerException = null)
        : Exception(message, innerException)
    {
    }

    internal sealed class ControlClient : IDisposable
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private readonly HttpClient _httpClient = new()
        {
            Timeout = TimeSpan.FromMinutes(10)
        };

        public async Task<JsonNode?> CallToolAsync(
            string name,
            JsonElement arguments,
            CancellationToken cancellationToken)
        {
            var payload = new JsonObject
            {
                ["name"] = name,
                ["arguments"] = arguments.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
                    ? new JsonObject()
                    : JsonNode.Parse(arguments.GetRawText())
            };

            try
            {
                var endpoint = FindEndpoint();
                HttpResponseMessage response = await PostAsync(endpoint, payload, cancellationToken).ConfigureAwait(false);
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    response.Dispose();
                    endpoint = FindEndpoint();
                    response = await PostAsync(endpoint, payload, cancellationToken).ConfigureAwait(false);
                }

                using (response)
                {
                    string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        string message = TryReadError(body) ?? $"墨匣控制服务返回 HTTP {(int)response.StatusCode}。";
                        if (response.StatusCode == HttpStatusCode.Unauthorized)
                        {
                            throw new ControlToolException($"{message} 请在墨匣中确认 MCP 开关，或重启墨匣后重试。".Trim());
                        }

                        throw new ControlToolException(message);
                    }

                    try
                    {
                        return JsonNode.Parse(body);
                    }
                    catch (JsonException ex)
                    {
                        throw new ControlUnavailableException("墨匣控制服务返回了无效响应。", ex);
                    }
                }
            }
            catch (ControlToolException)
            {
                throw;
            }
            catch (ControlUnavailableException)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ControlUnavailableException(
                    "无法连接墨匣。请先启动墨匣桌面版，并在 AI 设置中开启 MCP 控制。",
                    ex);
            }
        }

        public async Task<string> GetHealthAsync(CancellationToken cancellationToken)
        {
            var endpoint = FindEndpoint();
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(endpoint.Url, "v1/health"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", endpoint.Token);
            try
            {
                using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    throw new ControlUnavailableException(TryReadError(body) ?? $"HTTP {(int)response.StatusCode}");
                }

                return body;
            }
            catch (ControlUnavailableException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ControlUnavailableException("无法连接墨匣 MCP 控制服务。", ex);
            }
        }

        public DiscoveryInfo FindEndpoint()
        {
            foreach (string path in GetDiscoveryPaths())
            {
                try
                {
                    if (!File.Exists(path))
                    {
                        continue;
                    }

                    var document = JsonSerializer.Deserialize<McpControlDiscoveryDocument>(File.ReadAllText(path), JsonOptions);
                    if (document is null
                        || string.IsNullOrWhiteSpace(document.Url)
                        || string.IsNullOrWhiteSpace(document.Token)
                        || !Uri.TryCreate(document.Url, UriKind.Absolute, out Uri? uri)
                        || uri.Scheme != Uri.UriSchemeHttp
                        || !IsLoopback(uri.Host))
                    {
                        continue;
                    }

                    return new DiscoveryInfo(path, uri, document.Token, document.Pid);
                }
                catch
                {
                }
            }

            throw new ControlUnavailableException(
                "未找到正在运行的墨匣。请启动墨匣桌面版，并确认 AI 设置中的 MCP 控制已开启。");
        }

        public static IEnumerable<string> GetDiscoveryPaths()
        {
            string? explicitPath = Environment.GetEnvironmentVariable(McpToolCatalog.DiscoveryEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(explicitPath))
            {
                yield return explicitPath;
            }

            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrWhiteSpace(home))
            {
                yield return Path.Combine(home, ".moxia", McpToolCatalog.DiscoveryFileName);
            }

            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            foreach (string appId in new[] { "com.moxia.diary", "com.moxia.diary.debug" })
            {
                foreach (string publisher in new[] { "Mo Xia", "MoXia" })
                {
                    if (!string.IsNullOrWhiteSpace(localAppData))
                    {
                        yield return Path.Combine(localAppData, appId, "Data", McpToolCatalog.DiscoveryFileName);
                        yield return Path.Combine(localAppData, publisher, appId, "Data", McpToolCatalog.DiscoveryFileName);
                    }

                    if (!string.IsNullOrWhiteSpace(home))
                    {
                        yield return Path.Combine(home, "Library", "Application Support", publisher, appId, "Data", McpToolCatalog.DiscoveryFileName);
                        yield return Path.Combine(home, "Library", "Application Support", appId, McpToolCatalog.DiscoveryFileName);
                    }
                }
            }
        }

        public void Dispose() => _httpClient.Dispose();

        private async Task<HttpResponseMessage> PostAsync(
            DiscoveryInfo endpoint,
            JsonObject payload,
            CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                new Uri(endpoint.Url, "v1/tools/call"))
            {
                Content = new StringContent(payload.ToJsonString(JsonOptions), Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", endpoint.Token);
            return await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        private static bool IsLoopback(string host)
            => IPAddress.TryParse(host, out IPAddress? address) && IPAddress.IsLoopback(address);

        private static string? TryReadError(string body)
        {
            try
            {
                using var document = JsonDocument.Parse(body);
                if (document.RootElement.TryGetProperty("error", out JsonElement error)
                    && error.TryGetProperty("message", out JsonElement message))
                {
                    return message.GetString();
                }
            }
            catch (JsonException)
            {
            }

            return null;
        }
    }

    internal sealed record DiscoveryInfo(string Path, Uri Url, string Token, int Pid);
}
