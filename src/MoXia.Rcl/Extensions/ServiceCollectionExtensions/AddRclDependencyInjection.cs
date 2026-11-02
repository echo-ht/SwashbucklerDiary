using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MoXia.Rcl.Components;
using MoXia.Rcl.Repository;
using MoXia.Rcl.Services;

namespace MoXia.Rcl.Extensions
{
    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRclDependencyInjection(this IServiceCollection services)
        {
            services.TryAddScoped<InputJSModule>();
            services.TryAddScoped<AudioInterop>();
            services.TryAddScoped<ScreenshotJSModule>();
            services.TryAddSingleton<IDatabaseScopeAccessor, DatabaseScopeAccessor>();
            services.TryAddScoped<IAiApiClient, AiApiClient>();
            services.TryAddScoped<IAiChunkRepository, AiChunkRepository>();
            services.TryAddScoped<IAiNoteIndexService, AiNoteIndexService>();
            services.TryAddScoped<IAiNoteAssistant, AiNoteAssistant>();
            return services;
        }
    }
}
