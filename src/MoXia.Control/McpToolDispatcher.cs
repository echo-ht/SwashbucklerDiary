using System.Text.Json;
using MoXia.Shared;

namespace MoXia.Control
{
    public sealed class McpToolDispatcher(McpNoteService noteService)
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public async Task<McpToolCallResult> CallAsync(
            string name,
            JsonElement arguments,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new McpControlException("缺少工具名称。");
            }

            if (arguments.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            {
                arguments = JsonDocument.Parse("{}").RootElement.Clone();
            }

            if (arguments.ValueKind != JsonValueKind.Object)
            {
                throw new McpControlException("工具参数必须是 JSON 对象。");
            }

            object? result = name switch
            {
                "list_notes" => await ListNotesAsync(arguments, cancellationToken),
                "get_note" => await GetNoteAsync(arguments, cancellationToken),
                "create_note" => await CreateNoteAsync(arguments, cancellationToken),
                "update_note" => await UpdateNoteAsync(arguments, cancellationToken),
                "delete_note" => await DeleteNoteAsync(arguments, cancellationToken),
                "search_notes" => await SearchNotesAsync(arguments, cancellationToken),
                "ask_notes" => await AskNotesAsync(arguments, cancellationToken),
                "get_index_status" => await GetIndexStatusAsync(arguments, cancellationToken),
                "rebuild_index" => await RebuildIndexAsync(arguments, cancellationToken),
                _ => throw new McpControlException($"未知工具：{name}", 404)
            };

            return McpToolCallResult.Success(JsonSerializer.Serialize(result, JsonOptions));
        }

        private async Task<object> ListNotesAsync(JsonElement arguments, CancellationToken cancellationToken)
        {
            LibraryKind library = GetLibrary(arguments);
            string? query = GetOptionalString(arguments, "query");
            int limit = Math.Clamp(GetOptionalInt(arguments, "limit") ?? 20, 1, 50);
            int offset = Math.Max(0, GetOptionalInt(arguments, "offset") ?? 0);
            return await noteService.ListNotesAsync(library, query, limit, offset, cancellationToken);
        }

        private async Task<object> GetNoteAsync(JsonElement arguments, CancellationToken cancellationToken)
        {
            LibraryKind library = GetLibrary(arguments);
            Guid id = GetRequiredGuid(arguments, "id");
            return await noteService.GetNoteAsync(library, id, cancellationToken);
        }

        private async Task<object> CreateNoteAsync(JsonElement arguments, CancellationToken cancellationToken)
        {
            LibraryKind library = GetLibrary(arguments);
            string content = GetRequiredString(arguments, "content");
            string? title = GetOptionalString(arguments, "title");
            IReadOnlyList<string>? tags = GetOptionalStringArray(arguments, "tags");
            return await noteService.CreateNoteAsync(library, title, content, tags, cancellationToken);
        }

        private async Task<object> UpdateNoteAsync(JsonElement arguments, CancellationToken cancellationToken)
        {
            LibraryKind library = GetLibrary(arguments);
            Guid id = GetRequiredGuid(arguments, "id");
            string? title = GetOptionalString(arguments, "title");
            string? content = GetOptionalString(arguments, "content");
            IReadOnlyList<string>? tags = GetOptionalStringArray(arguments, "tags");
            return await noteService.UpdateNoteAsync(library, id, title, content, tags, cancellationToken);
        }

        private async Task<object> DeleteNoteAsync(JsonElement arguments, CancellationToken cancellationToken)
        {
            LibraryKind library = GetLibrary(arguments);
            Guid id = GetRequiredGuid(arguments, "id");
            return await noteService.DeleteNoteAsync(library, id, cancellationToken);
        }

        private async Task<object> SearchNotesAsync(JsonElement arguments, CancellationToken cancellationToken)
        {
            LibraryKind library = GetLibrary(arguments);
            string query = GetRequiredString(arguments, "query");
            int limit = Math.Clamp(GetOptionalInt(arguments, "limit") ?? 6, 1, 20);
            return await noteService.SearchNotesAsync(library, query, limit, cancellationToken);
        }

        private async Task<object> AskNotesAsync(JsonElement arguments, CancellationToken cancellationToken)
        {
            LibraryKind library = GetLibrary(arguments);
            string question = GetRequiredString(arguments, "question");
            return await noteService.AskNotesAsync(library, question, cancellationToken);
        }

        private async Task<object> GetIndexStatusAsync(JsonElement arguments, CancellationToken cancellationToken)
        {
            LibraryKind library = GetLibrary(arguments);
            return await noteService.GetIndexStatusAsync(library, cancellationToken);
        }

        private async Task<object> RebuildIndexAsync(JsonElement arguments, CancellationToken cancellationToken)
        {
            LibraryKind library = GetLibrary(arguments);
            return await noteService.RebuildIndexAsync(library, cancellationToken);
        }

        private static LibraryKind GetLibrary(JsonElement arguments)
        {
            string value = GetOptionalString(arguments, "library") ?? "normal";
            return value.Trim().ToLowerInvariant() switch
            {
                "normal" => LibraryKind.Normal,
                "private" => LibraryKind.Private,
                _ => throw new McpControlException("library 只能是 normal 或 private。")
            };
        }

        private static string GetRequiredString(JsonElement arguments, string name)
        {
            string? value = GetOptionalString(arguments, name);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new McpControlException($"缺少参数：{name}。");
            }

            return value.Trim();
        }

        private static string? GetOptionalString(JsonElement arguments, string name)
        {
            if (!arguments.TryGetProperty(name, out JsonElement value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                return null;
            }

            if (value.ValueKind != JsonValueKind.String)
            {
                throw new McpControlException($"参数 {name} 必须是字符串。");
            }

            return value.GetString();
        }

        private static Guid GetRequiredGuid(JsonElement arguments, string name)
        {
            string value = GetRequiredString(arguments, name);
            if (!Guid.TryParse(value, out Guid id))
            {
                throw new McpControlException($"参数 {name} 不是有效的 GUID。");
            }

            return id;
        }

        private static int? GetOptionalInt(JsonElement arguments, string name)
        {
            if (!arguments.TryGetProperty(name, out JsonElement value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                return null;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int number))
            {
                return number;
            }

            if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out number))
            {
                return number;
            }

            throw new McpControlException($"参数 {name} 必须是整数。");
        }

        private static IReadOnlyList<string>? GetOptionalStringArray(JsonElement arguments, string name)
        {
            if (!arguments.TryGetProperty(name, out JsonElement value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                return null;
            }

            if (value.ValueKind != JsonValueKind.Array)
            {
                throw new McpControlException($"参数 {name} 必须是字符串数组。");
            }

            var result = new List<string>();
            foreach (JsonElement item in value.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String)
                {
                    throw new McpControlException($"参数 {name} 必须是字符串数组。");
                }

                string? text = item.GetString()?.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    result.Add(text);
                }
            }

            if (result.Count > 20)
            {
                throw new McpControlException($"参数 {name} 最多包含 20 个标签。");
            }

            return result;
        }
    }
}
