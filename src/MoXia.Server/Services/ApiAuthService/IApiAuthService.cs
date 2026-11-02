namespace MoXia.Server.Services
{
    public interface IApiAuthService
    {
        void UpdateVersion();
        string GenerateApiKey();
        bool ValidateApiKey(string apiKey);
    }
}
