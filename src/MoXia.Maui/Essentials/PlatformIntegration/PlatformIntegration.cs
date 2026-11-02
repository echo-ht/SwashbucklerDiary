using Microsoft.Extensions.Logging;
using MoXia.Rcl.Essentials;
using MoXia.Rcl.Services;

namespace MoXia.Maui.Essentials
{
    public partial class PlatformIntegration : IPlatformIntegration
    {
        private readonly ILogger _logger;

        private readonly II18nService _i18n;

        public PlatformIntegration(ILogger<PlatformIntegration> logger,
            II18nService i18n)
        {
            _logger = logger;
            _i18n = i18n;
        }
    }
}
