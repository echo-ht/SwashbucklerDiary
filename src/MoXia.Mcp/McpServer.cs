using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using MoXia.Shared;

namespace MoXia.Mcp
{
    internal static class McpServer
    {
        private const string DefaultProtocolVersion = "2025-06-18";
        private static readonly HashSet<string> SupportedProtocolVersions =
        [
            "2024-11-05",
            "2025-03-26",
            "2025-06-18"
        ];

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false
        };

        public static async Task<int> RunAsync(string[] args)
        {
            if (args.Contains("--help") || args.Contains("-h"))
            {
                PrintHelp();
                return 0;
            }

            if (args.Contains("--version"))
            {
                Console.WriteLine(GetVersion());
                return 0;
            }

            if (args.Contains("--doctor"))
            {
                return await RunDoctorAsync().ConfigureAwait(false);
            }

            return await RunStdioAsync().ConfigureAwait(false);
        }

        private static async Task<int> RunStdioAsync()
        {
            using var input = new StreamReader(Console.OpenStandardInput(), new UTF8Encoding(false));
            using var output = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false))
            {
                AutoFlush = true,
                NewLine = "\n"
            };
            using var control = new ControlClient();
            using var cancellation = new CancellationTokenSource();

            Console.CancelKeyPress += (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cancellation.Cancel();
            };

            while (!cancellation.IsCancellationRequested)
            {
                string? line;
                try
                {
                    line = await input.ReadLineAsync(cancellation.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (line is null)
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                JsonDocument document;
                try
                {
                    document = JsonDocument.Parse(line);
                }
                catch (JsonException)
                {
                    await WriteAsync(output, ErrorResponse(null, -32700, "Parse error")).ConfigureAwait(false);
                    continue;
                }

                using (document)
                {
                    JsonElement root = document.RootElement;
                    if (root.ValueKind == JsonValueKind.Array)
                    {
                        foreach (JsonElement item in root.EnumerateArray())
                        {
                            JsonObject? response = await HandleMessageAsync(item, control, cancellation.Token).ConfigureAwait(false);
                            if (response is not null)
                            {
                                await WriteAsync(output, response).ConfigureAwait(false);
                            }
                        }
                    }
                    else
                    {
                        JsonObject? response = await HandleMessageAsync(root, control, cancellation.Token).ConfigureAwait(false);
                        if (response is not null)
                        {
                            await WriteAsync(output, response).ConfigureAwait(false);
                        }
                    }
                }
            }

            return 0;
        }

        private static async Task<JsonObject?> HandleMessageAsync(
            JsonElement root,
            ControlClient control,
            CancellationToken cancellationToken)
        {
            if (root.ValueKind != JsonValueKind.Object)
            {
                return ErrorResponse(null, -32600, "Invalid Request");
            }

            bool hasId = root.TryGetProperty("id", out JsonElement id);
            JsonElement idValue = hasId ? id.Clone() : default;
            if (!hasId)
            {
                return null;
            }

            if (!root.TryGetProperty("method", out JsonElement methodElement) || methodElement.ValueKind != JsonValueKind.String)
            {
                return ErrorResponse(idValue, -32600, "Invalid Request");
            }

            string method = methodElement.GetString() ?? string.Empty;
            JsonElement parameters = root.TryGetProperty("params", out JsonElement paramsElement)
                ? paramsElement
                : default;

            try
            {
                return method switch
                {
                    "initialize" => SuccessResponse(idValue, Initialize(parameters)),
                    "ping" => SuccessResponse(idValue, new JsonObject()),
                    "tools/list" => SuccessResponse(idValue, ListTools()),
                    "tools/call" => SuccessResponse(idValue, await CallToolAsync(parameters, control, cancellationToken).ConfigureAwait(false)),
                    _ => ErrorResponse(idValue, -32601, $"Method not found: {method}")
                };
            }
            catch (ControlToolException ex)
            {
                return SuccessResponse(idValue, ToolError(ex.Message));
            }
            catch (ControlUnavailableException ex)
            {
                return ErrorResponse(idValue, -32000, ex.Message);
            }
            catch (OperationCanceledException)
            {
                return ErrorResponse(idValue, -32800, "Request cancelled");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return ErrorResponse(idValue, -32603, ex.Message);
            }
        }

        private static JsonObject Initialize(JsonElement parameters)
        {
            string requested = parameters.ValueKind == JsonValueKind.Object
                && parameters.TryGetProperty("protocolVersion", out JsonElement version)
                && version.ValueKind == JsonValueKind.String
                ? version.GetString() ?? string.Empty
                : string.Empty;
            string protocolVersion = SupportedProtocolVersions.Contains(requested)
                ? requested
                : DefaultProtocolVersion;

            return new JsonObject
            {
                ["protocolVersion"] = protocolVersion,
                ["capabilities"] = new JsonObject
                {
                    ["tools"] = new JsonObject
                    {
                        ["listChanged"] = false
                    }
                },
                ["serverInfo"] = new JsonObject
                {
                    ["name"] = McpToolCatalog.ServerName,
                    ["version"] = GetVersion()
                },
                ["instructions"] = "墨匣本地笔记 MCP。默认只访问普通库；隐私库必须在墨匣 AI 设置中显式开启。删除操作不可撤销。"
            };
        }

        private static JsonObject ListTools()
        {
            var tools = new JsonArray();
            foreach (var definition in McpToolCatalog.Tools)
            {
                tools.Add(new JsonObject
                {
                    ["name"] = definition.Name,
                    ["description"] = definition.Description,
                    ["inputSchema"] = definition.InputSchema.DeepClone()
                });
            }

            return new JsonObject { ["tools"] = tools };
        }

        private static async Task<JsonObject> CallToolAsync(
            JsonElement parameters,
            ControlClient control,
            CancellationToken cancellationToken)
        {
            if (parameters.ValueKind != JsonValueKind.Object
                || !parameters.TryGetProperty("name", out JsonElement nameElement)
                || nameElement.ValueKind != JsonValueKind.String)
            {
                throw new ControlToolException("tools/call 缺少工具名称。");
            }

            string name = nameElement.GetString() ?? string.Empty;
            JsonElement arguments = parameters.TryGetProperty("arguments", out JsonElement argumentsElement)
                ? argumentsElement
                : default;
            JsonNode? result = await control.CallToolAsync(name, arguments, cancellationToken).ConfigureAwait(false);
            if (result is JsonObject resultObject)
            {
                return resultObject;
            }

            return ToolError("墨匣控制服务返回了无效的工具结果。");
        }

        private static JsonObject ToolError(string message)
            => new()
            {
                ["content"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["type"] = "text",
                        ["text"] = message
                    }
                },
                ["isError"] = true
            };

        private static JsonObject SuccessResponse(JsonElement id, JsonNode result)
            => new()
            {
                ["jsonrpc"] = "2.0",
                ["id"] = JsonNode.Parse(id.GetRawText()),
                ["result"] = result
            };

        private static JsonObject ErrorResponse(JsonElement? id, int code, string message)
        {
            var response = new JsonObject
            {
                ["jsonrpc"] = "2.0",
                ["error"] = new JsonObject
                {
                    ["code"] = code,
                    ["message"] = message
                }
            };
            if (id.HasValue)
            {
                response["id"] = JsonNode.Parse(id.Value.GetRawText());
            }
            else
            {
                response["id"] = null;
            }

            return response;
        }

        private static async Task WriteAsync(StreamWriter output, JsonObject response)
        {
            await output.WriteLineAsync(response.ToJsonString(JsonOptions)).ConfigureAwait(false);
        }

        private static async Task<int> RunDoctorAsync()
        {
            using var control = new ControlClient();
            try
            {
                var endpoint = control.FindEndpoint();
                string health = await control.GetHealthAsync(CancellationToken.None).ConfigureAwait(false);
                Console.WriteLine($"Discovery: {endpoint.Path}");
                Console.WriteLine($"Control:   {endpoint.Url}");
                Console.WriteLine($"App PID:   {endpoint.Pid}");
                Console.WriteLine($"Health:    {health}");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        }

        private static string GetVersion()
            => Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

        private static void PrintHelp()
        {
            Console.WriteLine("MoXia MCP server");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  MoXia.Mcp            Run MCP over stdio (for AI clients)");
            Console.WriteLine("  MoXia.Mcp --doctor   Check the running MoXia control endpoint");
            Console.WriteLine("  MoXia.Mcp --version  Print version");
            Console.WriteLine();
            Console.WriteLine($"Set {McpToolCatalog.DiscoveryEnvironmentVariable} to use a custom discovery file.");
        }
    }
}
