namespace MoXia.Control
{
    public sealed class McpToolCallRequest
    {
        public string Name { get; set; } = string.Empty;

        public System.Text.Json.JsonElement Arguments { get; set; }
    }

    public sealed record McpToolContent(string Type, string Text);

    public sealed record McpToolCallResult(IReadOnlyList<McpToolContent> Content, bool IsError)
    {
        public static McpToolCallResult Success(string text)
            => new([new McpToolContent("text", text)], false);
    }
}
