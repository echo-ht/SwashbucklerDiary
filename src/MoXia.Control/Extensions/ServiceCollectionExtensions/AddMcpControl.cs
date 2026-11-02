using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MoXia.Control.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMcpControl(this IServiceCollection services)
        {
            services.TryAddSingleton<McpControlServer>();
            services.TryAddScoped<McpNoteService>();
            services.TryAddScoped<McpToolDispatcher>();
            return services;
        }
    }
}
