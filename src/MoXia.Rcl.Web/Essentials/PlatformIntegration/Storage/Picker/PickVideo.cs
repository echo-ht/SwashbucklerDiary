using MoXia.Rcl.Essentials;

namespace MoXia.Rcl.Web.Essentials
{
    public partial class PlatformIntegration
    {
        public Task<string?> PickVideoAsync()
            => PickFileAsync(PlatformIntegrationHelper.VideoMimeTypes, PlatformIntegrationHelper.VideoFileExtensions);

        public Task<IEnumerable<string>?> PickMultipleVideoAsync()
            => PickFilesAsync(PlatformIntegrationHelper.VideoMimeTypes, PlatformIntegrationHelper.VideoFileExtensions);
    }
}
