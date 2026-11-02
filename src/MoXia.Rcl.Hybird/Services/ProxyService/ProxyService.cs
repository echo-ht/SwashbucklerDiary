using EmbedIO;
using EmbedIO.WebApi;
using MoXia.Rcl.Hybird.Extensions;
using MoXia.Rcl.Services;
using System.Net;

namespace MoXia.Rcl.Hybird.Services
{
    public class ProxyService : IProxyService, IDisposable
    {
        private WebServer? server;
        public string ProxyUrl { get; }

        public ProxyService()
        {
            var baseUrl = $"http://localhost:{IPAddress.Loopback.GetAvailablePort()}/";
            ProxyUrl = $"{baseUrl}api/proxy/?url=";
            StartWebServer(baseUrl);
        }

        private void StartWebServer(string url)
        {
            if (server is not null)
            {
                return;
            }

            server = new WebServer(o => o
                    .WithUrlPrefix(url))
                    .WithCors()
                    .WithWebApi("/", m => m.WithController<ProxyController>());

            server.RunAsync();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                server?.Dispose();
                server = null;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

    }
}
