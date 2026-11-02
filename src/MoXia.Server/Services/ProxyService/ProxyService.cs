using MoXia.Rcl.Services;

namespace MoXia.Server.Services
{
    public class ProxyService : IProxyService
    {
        public string ProxyUrl { get; } = "api/proxy?url=";
    }
}
