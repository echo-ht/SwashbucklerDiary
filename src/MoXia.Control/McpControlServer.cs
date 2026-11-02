using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MoXia.Rcl.Essentials;
using MoXia.Rcl.Services;
using MoXia.Shared;

namespace MoXia.Control
{
    public sealed class McpControlServer : IAsyncDisposable
    {
        private const int MaxHeaderBytes = 16_384;
        private const int MaxRequestBodyBytes = 1_048_576;
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };

        private static readonly JsonSerializerOptions CompactJsonOptions = new(JsonSerializerDefaults.Web)
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private readonly IServiceProvider _services;
        private readonly ILogger<McpControlServer> _logger;
        private readonly List<string> _discoveryPaths = [];
        private readonly ConcurrentDictionary<Task, byte> _clientTasks = new();
        private readonly object _sync = new();
        private TcpListener? _listener;
        private CancellationTokenSource? _cts;
        private Task? _acceptLoop;
        private string? _token;

        public McpControlServer(IServiceProvider services, ILogger<McpControlServer> logger)
        {
            _services = services;
            _logger = logger;
        }

        public string? Url { get; private set; }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            lock (_sync)
            {
                if (_listener is not null)
                {
                    return Task.CompletedTask;
                }
            }

            try
            {
                var listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start();
                int port = ((IPEndPoint)listener.LocalEndpoint).Port;
                string url = $"http://127.0.0.1:{port}/";

                lock (_sync)
                {
                    _listener = listener;
                    _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    _token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
                    Url = url;
                    WriteDiscoveryFiles(url, _token);
                    _acceptLoop = Task.Run(() => AcceptLoopAsync(listener, _cts.Token), CancellationToken.None);
                }

                _logger.LogInformation("MoXia MCP control server listening on {Url}", url);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MoXia MCP control server could not start");
                StopAsync().GetAwaiter().GetResult();
            }

            return Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            TcpListener? listener;
            CancellationTokenSource? cts;
            Task? loop;
            string? token;
            lock (_sync)
            {
                listener = _listener;
                cts = _cts;
                loop = _acceptLoop;
                token = _token;
                _listener = null;
                _cts = null;
                _acceptLoop = null;
                _token = null;
                Url = null;
            }

            if (cts is not null)
            {
                await cts.CancelAsync().ConfigureAwait(false);
            }

            try
            {
                listener?.Stop();
            }
            catch
            {
            }

            if (loop is not null)
            {
                try
                {
                    await loop.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
                catch (SocketException)
                {
                }
                catch (ObjectDisposedException)
                {
                }
            }

            Task[] pendingClients = _clientTasks.Keys.ToArray();
            if (pendingClients.Length > 0)
            {
                try
                {
                    await Task.WhenAll(pendingClients).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
                catch
                {
                }
            }

            cts?.Dispose();
            DeleteDiscoveryFiles(token);
        }

        public ValueTask DisposeAsync() => new(StopAsync());

        private async Task AcceptLoopAsync(TcpListener listener, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (SocketException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogWarning(ex, "MCP control connection accept failed");
                    }

                    continue;
                }

                Task clientTask = HandleClientAsync(client, cancellationToken);
                _clientTasks.TryAdd(clientTask, 0);
                _ = clientTask.ContinueWith(
                    static (task, state) => ((ConcurrentDictionary<Task, byte>)state!).TryRemove(task, out _),
                    _clientTasks,
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            using (client)
            {
                client.NoDelay = true;
                NetworkStream? stream = null;
                try
                {
                    stream = client.GetStream();
                    var request = await ReadRequestAsync(stream, cancellationToken).ConfigureAwait(false);
                    var response = await ProcessRequestAsync(request, cancellationToken).ConfigureAwait(false);
                    await WriteResponseAsync(stream, response, cancellationToken).ConfigureAwait(false);
                }
                catch (McpControlException ex)
                {
                    if (stream is not null)
                    {
                        await TryWriteErrorAsync(stream, ex.StatusCode, ex.Message, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "MCP control request failed");
                    if (stream is not null)
                    {
                        await TryWriteErrorAsync(stream, 500, ex.Message, cancellationToken).ConfigureAwait(false);
                    }
                }
                finally
                {
                    if (stream is not null)
                    {
                        await stream.DisposeAsync().ConfigureAwait(false);
                    }
                }
            }
        }

        private async Task<HttpResponseData> ProcessRequestAsync(
            HttpRequestData request,
            CancellationToken cancellationToken)
        {
            if (!IsAuthorized(request.Headers))
            {
                return JsonResponse(401, new { error = new { message = "未授权的 MCP 控制请求。" } });
            }

            string path = request.Target.Split('?', 2)[0];
            if (request.Method == "GET" && path == "/v1/health")
            {
                using var scope = _services.CreateScope();
                var settings = scope.ServiceProvider.GetRequiredService<ISettingService>();
                return JsonResponse(200, new
                {
                    ok = true,
                    app = "MoXia",
                    enabled = settings.Get(it => it.McpEnabled),
                    privateAllowed = settings.Get(it => it.McpAllowPrivateLibrary)
                });
            }

            if (request.Method == "POST" && path == "/v1/tools/call")
            {
                return await HandleToolCallAsync(request, cancellationToken).ConfigureAwait(false);
            }

            return JsonResponse(404, new { error = new { message = "未知的 MCP 控制接口。" } });
        }

        private async Task<HttpResponseData> HandleToolCallAsync(
            HttpRequestData request,
            CancellationToken cancellationToken)
        {
            McpToolCallRequest? toolRequest;
            try
            {
                toolRequest = JsonSerializer.Deserialize<McpToolCallRequest>(request.Body, CompactJsonOptions);
            }
            catch (JsonException)
            {
                return JsonResponse(400, new { error = new { message = "MCP 请求不是有效的 JSON。" } });
            }

            if (toolRequest is null || string.IsNullOrWhiteSpace(toolRequest.Name))
            {
                return JsonResponse(400, new { error = new { message = "MCP 请求缺少工具名称。" } });
            }

            using var scope = _services.CreateScope();
            var settings = scope.ServiceProvider.GetRequiredService<ISettingService>();
            if (!settings.Get(it => it.McpEnabled))
            {
                return JsonResponse(403, new
                {
                    error = new { message = "MCP 控制未启用。请在墨匣 AI 设置中开启 MCP 控制。" }
                });
            }

            var dispatcher = scope.ServiceProvider.GetRequiredService<McpToolDispatcher>();
            try
            {
                var result = await dispatcher.CallAsync(toolRequest.Name, toolRequest.Arguments, cancellationToken)
                    .ConfigureAwait(false);
                return JsonResponse(200, result);
            }
            catch (McpControlException ex)
            {
                return JsonResponse(ex.StatusCode, new { error = new { message = ex.Message } });
            }
        }

        private bool IsAuthorized(IReadOnlyDictionary<string, string> headers)
        {
            string? token;
            lock (_sync)
            {
                token = _token;
            }

            if (string.IsNullOrWhiteSpace(token)
                || !headers.TryGetValue("Authorization", out string? header)
                || string.IsNullOrWhiteSpace(header)
                || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            byte[] provided = Encoding.UTF8.GetBytes(header[7..].Trim());
            byte[] expected = Encoding.UTF8.GetBytes(token);
            return provided.Length == expected.Length
                && CryptographicOperations.FixedTimeEquals(provided, expected);
        }

        private static async Task<HttpRequestData> ReadRequestAsync(
            NetworkStream stream,
            CancellationToken cancellationToken)
        {
            using var headerBuffer = new MemoryStream();
            byte[] oneByte = new byte[1];
            while (headerBuffer.Length < MaxHeaderBytes)
            {
                int read = await stream.ReadAsync(oneByte.AsMemory(0, 1), cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }

                headerBuffer.WriteByte(oneByte[0]);
                byte[] current = headerBuffer.GetBuffer();
                long length = headerBuffer.Length;
                if (length >= 4
                    && current[length - 4] == '\r'
                    && current[length - 3] == '\n'
                    && current[length - 2] == '\r'
                    && current[length - 1] == '\n')
                {
                    break;
                }
            }

            if (headerBuffer.Length == 0)
            {
                throw new McpControlException("MCP 请求为空。");
            }

            if (headerBuffer.Length >= MaxHeaderBytes)
            {
                throw new McpControlException("MCP 请求头过大。", 431);
            }

            string headerText = Encoding.ASCII.GetString(headerBuffer.GetBuffer(), 0, (int)headerBuffer.Length);
            string[] lines = headerText.Split("\r\n", StringSplitOptions.None);
            string[] requestLine = lines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (requestLine.Length < 2)
            {
                throw new McpControlException("MCP 请求行无效。");
            }

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int index = 1; index < lines.Length; index++)
            {
                string line = lines[index];
                int separator = line.IndexOf(':');
                if (separator <= 0)
                {
                    continue;
                }

                headers[line[..separator].Trim()] = line[(separator + 1)..].Trim();
            }

            int contentLength = 0;
            if (headers.TryGetValue("Content-Length", out string? contentLengthText)
                && (!int.TryParse(contentLengthText, out contentLength) || contentLength < 0))
            {
                throw new McpControlException("MCP 请求的 Content-Length 无效。");
            }

            if (contentLength > MaxRequestBodyBytes)
            {
                throw new McpControlException("MCP 请求体过大。", 413);
            }

            byte[] body = new byte[contentLength];
            if (contentLength > 0)
            {
                await stream.ReadExactlyAsync(body, cancellationToken).ConfigureAwait(false);
            }

            return new HttpRequestData(requestLine[0], requestLine[1], headers, body);
        }

        private static async Task WriteResponseAsync(
            NetworkStream stream,
            HttpResponseData response,
            CancellationToken cancellationToken)
        {
            string reason = response.StatusCode switch
            {
                200 => "OK",
                400 => "Bad Request",
                401 => "Unauthorized",
                403 => "Forbidden",
                404 => "Not Found",
                413 => "Payload Too Large",
                431 => "Request Header Fields Too Large",
                500 => "Internal Server Error",
                _ => "Error"
            };
            byte[] header = Encoding.ASCII.GetBytes(
                $"HTTP/1.1 {response.StatusCode} {reason}\r\n" +
                "Content-Type: application/json; charset=utf-8\r\n" +
                $"Content-Length: {response.Body.Length}\r\n" +
                "Cache-Control: no-store\r\n" +
                "Connection: close\r\n\r\n");
            await stream.WriteAsync(header, cancellationToken).ConfigureAwait(false);
            await stream.WriteAsync(response.Body, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TryWriteErrorAsync(
            NetworkStream stream,
            int statusCode,
            string message,
            CancellationToken cancellationToken)
        {
            try
            {
                await WriteResponseAsync(stream, JsonResponse(statusCode, new { error = new { message } }), cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
            }
        }

        private static HttpResponseData JsonResponse(int statusCode, object payload)
            => new(statusCode, JsonSerializer.SerializeToUtf8Bytes(payload, CompactJsonOptions));

        private void WriteDiscoveryFiles(string url, string token)
        {
            var document = new McpControlDiscoveryDocument
            {
                Version = 1,
                App = "MoXia",
                Url = url,
                Token = token,
                Pid = Environment.ProcessId,
                StartedAt = DateTimeOffset.Now
            };
            string json = JsonSerializer.Serialize(document, JsonOptions);

            _discoveryPaths.Clear();
            foreach (string path in GetDiscoveryPaths())
            {
                try
                {
                    string? directory = Path.GetDirectoryName(path);
                    if (string.IsNullOrWhiteSpace(directory))
                    {
                        continue;
                    }

                    Directory.CreateDirectory(directory);
                    if (!OperatingSystem.IsWindows())
                    {
                        File.SetUnixFileMode(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                    }

                    File.WriteAllText(path, json, new UTF8Encoding(false));
                    if (!OperatingSystem.IsWindows())
                    {
                        File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
                    }

                    _discoveryPaths.Add(path);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not write MCP discovery file {Path}", path);
                }
            }
        }

        private IEnumerable<string> GetDiscoveryPaths()
        {
            var paths = new List<string>();
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrWhiteSpace(home))
            {
                paths.Add(Path.Combine(home, ".moxia", McpToolCatalog.DiscoveryFileName));
            }

            try
            {
                var appFileSystem = _services.GetService<IAppFileSystem>();
                if (!string.IsNullOrWhiteSpace(appFileSystem?.AppDataDirectory))
                {
                    paths.Add(Path.Combine(appFileSystem.AppDataDirectory, McpToolCatalog.DiscoveryFileName));
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not resolve app data directory for MCP discovery");
            }

            return paths.Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private void DeleteDiscoveryFiles(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            IEnumerable<string> paths = _discoveryPaths.Count > 0 ? _discoveryPaths : GetDiscoveryPaths();
            foreach (string path in paths)
            {
                try
                {
                    if (!File.Exists(path))
                    {
                        continue;
                    }

                    string content = File.ReadAllText(path);
                    var document = JsonSerializer.Deserialize<McpControlDiscoveryDocument>(content, CompactJsonOptions);
                    if (document?.Token == token)
                    {
                        File.Delete(path);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not remove MCP discovery file {Path}", path);
                }
            }
        }

        private sealed record HttpRequestData(
            string Method,
            string Target,
            IReadOnlyDictionary<string, string> Headers,
            byte[] Body);

        private sealed record HttpResponseData(int StatusCode, byte[] Body);
    }
}
