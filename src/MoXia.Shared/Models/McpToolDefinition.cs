using System.Text.Json.Nodes;

namespace MoXia.Shared
{
    public sealed record McpToolDefinition(string Name, string Description, JsonObject InputSchema);

    public sealed class McpControlDiscoveryDocument
    {
        public int Version { get; set; }

        public string App { get; set; } = "MoXia";

        public string Url { get; set; } = string.Empty;

        public string Token { get; set; } = string.Empty;

        public int Pid { get; set; }

        public DateTimeOffset StartedAt { get; set; }
    }

    public static class McpToolCatalog
    {
        public const string ServerName = "moxia-mcp";
        public const string DiscoveryEnvironmentVariable = "MOXIA_MCP_DISCOVERY";
        public const string DiscoveryFileName = "mcp-control.json";

        public static IReadOnlyList<McpToolDefinition> Tools { get; } = BuildTools();

        private static IReadOnlyList<McpToolDefinition> BuildTools()
        {
            return
            [
                new(
                    "list_notes",
                    "列出墨匣笔记，可按关键词过滤。library=private 时，需要在墨匣设置中开启“允许 MCP 访问隐私库”。",
                    Schema(null,
                        ("library", LibraryProperty()),
                        ("query", StringProperty("可选的标题/正文关键词。")),
                        ("limit", IntegerProperty("返回数量，1-50，默认 20。", 1, 50)),
                        ("offset", IntegerProperty("跳过数量，默认 0。", 0, 100000)))),
                new(
                    "get_note",
                    "按 ID 读取一篇墨匣笔记的完整 Markdown 内容与标签。",
                    Schema(["library", "id"],
                        ("library", LibraryProperty()),
                        ("id", StringProperty("笔记 GUID。")))),
                new(
                    "create_note",
                    "创建一篇墨匣笔记，并在保存后尝试更新向量索引。",
                    Schema(["library", "content"],
                        ("library", LibraryProperty()),
                        ("title", StringProperty("笔记标题。")),
                        ("content", StringProperty("Markdown 正文。")),
                        ("tags", StringArrayProperty("可选标签，最多 20 个。")))),
                new(
                    "update_note",
                    "更新一篇墨匣笔记的标题、正文或标签，并尝试增量更新向量索引。",
                    Schema(["library", "id"],
                        ("library", LibraryProperty()),
                        ("id", StringProperty("笔记 GUID。")),
                        ("title", StringProperty("新的标题；省略时保持不变。")),
                        ("content", StringProperty("新的 Markdown 正文；省略时保持不变。")),
                        ("tags", StringArrayProperty("新的标签列表；省略时保持不变，传空数组可清空。")))),
                new(
                    "delete_note",
                    "永久删除一篇墨匣笔记及其索引、标签关系和资源关系。此操作不可撤销。",
                    Schema(["library", "id"],
                        ("library", LibraryProperty()),
                        ("id", StringProperty("要删除的笔记 GUID。")))),
                new(
                    "search_notes",
                    "在指定笔记库的向量索引中做语义检索。若索引尚未建立，请先调用 rebuild_index。",
                    Schema(["library", "query"],
                        ("library", LibraryProperty()),
                        ("query", StringProperty("自然语言检索问题。")),
                        ("limit", IntegerProperty("返回数量，1-20，默认 6。", 1, 20)))),
                new(
                    "ask_notes",
                    "基于指定笔记库的向量检索结果回答问题，并返回引用来源。",
                    Schema(["library", "question"],
                        ("library", LibraryProperty()),
                        ("question", StringProperty("要基于笔记回答的问题。")))),
                new(
                    "get_index_status",
                    "查看指定笔记库的 AI 向量索引状态。",
                    Schema(["library"],
                        ("library", LibraryProperty()))),
                new(
                    "rebuild_index",
                    "重建指定笔记库的 AI 向量索引。笔记较多或使用远端向量模型时可能耗时较长。",
                    Schema(["library"],
                        ("library", LibraryProperty())))
            ];
        }

        private static JsonObject Schema(
            string[]? required,
            params (string Name, JsonNode Schema)[] properties)
        {
            var props = new JsonObject();
            foreach (var (name, propertySchema) in properties)
            {
                props[name] = propertySchema;
            }

            var schema = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = props,
                ["additionalProperties"] = false
            };

            if (required is { Length: > 0 })
            {
                var requiredArray = new JsonArray();
                foreach (string name in required)
                {
                    requiredArray.Add(name);
                }

                schema["required"] = requiredArray;
            }

            return schema;
        }

        private static JsonObject LibraryProperty()
            => new()
            {
                ["type"] = "string",
                ["enum"] = new JsonArray("normal", "private"),
                ["default"] = "normal",
                ["description"] = "normal=普通库；private=隐私库（需要设置开关）。"
            };

        private static JsonObject StringProperty(string description)
            => new()
            {
                ["type"] = "string",
                ["description"] = description
            };

        private static JsonObject IntegerProperty(string description, int minimum, int maximum)
            => new()
            {
                ["type"] = "integer",
                ["description"] = description,
                ["minimum"] = minimum,
                ["maximum"] = maximum
            };

        private static JsonObject StringArrayProperty(string description)
            => new()
            {
                ["type"] = "array",
                ["description"] = description,
                ["maxItems"] = 20,
                ["items"] = new JsonObject { ["type"] = "string" }
            };
    }
}
