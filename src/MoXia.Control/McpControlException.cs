namespace MoXia.Control
{
    public class McpControlException : Exception
    {
        public McpControlException(string message, int statusCode = 400) : base(message)
        {
            StatusCode = statusCode;
        }

        public int StatusCode { get; }
    }
}
