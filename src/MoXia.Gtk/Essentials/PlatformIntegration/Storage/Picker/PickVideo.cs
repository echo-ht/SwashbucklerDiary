using MoXia.Rcl.Essentials;

namespace MoXia.Gtk.Essentials
{
    public partial class PlatformIntegration
    {
        const string videoFilefilterName = "Video file";

        const string videoFilesfilterName = "Video files";

        static readonly string[] videoPatterns = GetPatterns(PlatformIntegrationHelper.VideoFileExtensions);

        public Task<string?> PickVideoAsync()
            => PickFileAsync(videoFilefilterName, videoPatterns, PlatformIntegrationHelper.VideoFileExtensions);

        public Task<IEnumerable<string>?> PickMultipleVideoAsync()
            => PickMultipleFileAsync(videoFilesfilterName, videoPatterns, PlatformIntegrationHelper.VideoFileExtensions);
    }
}
