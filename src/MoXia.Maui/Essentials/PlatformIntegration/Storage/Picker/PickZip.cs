using MoXia.Rcl.Essentials;

namespace MoXia.Maui.Essentials
{
    public partial class PlatformIntegration
    {
#if WINDOWS
        static readonly string[] zipTypes = PlatformIntegrationHelper.ZipFileExtensions;
#elif ANDROID || TIZEN
        static readonly string[] zipTypes = PlatformIntegrationHelper.ZipMimeTypes;
#elif MACCATALYST || IOS
        static readonly string[] zipTypes = [.. GetUTTypeIdentifiers(PlatformIntegrationHelper.ZipFileExtensions)];
#endif
        public Task<string?> PickZipFileAsync()
            => PickFileAsync(zipTypes, PlatformIntegrationHelper.ZipFileExtensions);
    }
}
