namespace MoXia.Rcl.Services
{
    public class AiApiException : Exception
    {
        public AiApiException(string message) : base(message)
        {
        }

        public AiApiException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
