using Microsoft.Extensions.Logging;
using MoXia.Rcl.Essentials;
using MoXia.Rcl.Services;

namespace MoXia.Gtk.Services
{
    public class MediaResourceManager : Rcl.Hybird.Services.MediaResourceManager
    {
        public MediaResourceManager(IPlatformIntegration platformIntegration,
            IAppFileSystem appFileSystem,
            II18nService i18nService,
            ISettingService settingService,
            ILogger<MediaResourceManager> logger)
            : base(platformIntegration, appFileSystem, i18nService, settingService, logger)
        {
        }
    }
}
