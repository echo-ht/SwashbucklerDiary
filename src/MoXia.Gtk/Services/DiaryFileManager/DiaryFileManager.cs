using MoXia.Rcl.Essentials;
using MoXia.Rcl.Services;

namespace MoXia.Gtk.Services
{
    public class DiaryFileManager : Rcl.Services.DiaryFileManager
    {
        public DiaryFileManager(IAppFileSystem appFileSystem,
            IPlatformIntegration platformIntegration,
            II18nService i18nService,
            IMediaResourceManager mediaResourceManager,
            IDiaryService diaryService,
            IResourceService resourceService,
            ISettingService settingService,
            IAvatarService avatarService)
            : base(appFileSystem, platformIntegration, i18nService, mediaResourceManager, diaryService, resourceService, settingService, avatarService)
        {
        }

        protected override string DatabasePath => SQLiteConstants.DatabasePath;

        protected override string PrivacyDatabasePath => SQLiteConstants.PrivacyDatabasePath;
    }
}
