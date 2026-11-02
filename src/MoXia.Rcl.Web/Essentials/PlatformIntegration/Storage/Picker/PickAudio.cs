using MoXia.Rcl.Essentials;

namespace MoXia.Rcl.Web.Essentials
{
    public partial class PlatformIntegration
    {
        public Task<string?> PickAudioAsync()
            => PickFileAsync(PlatformIntegrationHelper.AudioMimeTypes, PlatformIntegrationHelper.AudioFileExtensions);

        public Task<IEnumerable<string>?> PickMultipleAudioAsync()
            => PickFilesAsync(PlatformIntegrationHelper.AudioMimeTypes, PlatformIntegrationHelper.AudioFileExtensions);
    }
}
